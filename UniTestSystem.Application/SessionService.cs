using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public class SessionService : ISessionService
{
    private static readonly SessionStatus[] SubmittedStatuses =
    {
        SessionStatus.Submitted,
        SessionStatus.AutoSubmitted,
        SessionStatus.Graded
    };

    private readonly IRepository<Session> _sessionRepo;
    private readonly IRepository<Test> _testRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Question> _questionRepo;
    private readonly IRepository<SessionLog> _sessionLogRepo;
    private readonly IRepository<ExamSchedule> _examScheduleRepo;
    private readonly TestService _testService;
    private readonly AssessmentService _assessmentService;
    private readonly ExamAccessTokenService _examAccessTokenService;
    private readonly SessionDeviceGuardService _sessionDeviceGuardService;
    private readonly IGradingService _gradingService;
    private readonly IExamHubNotifier _examHubNotifier;

    public SessionService(
        IRepository<Session> sessionRepo,
        IRepository<Test> testRepo,
        IRepository<User> userRepo,
        IRepository<Question> questionRepo,
        IRepository<SessionLog> sessionLogRepo,
        IRepository<ExamSchedule> examScheduleRepo,
        TestService testService,
        AssessmentService assessmentService,
        ExamAccessTokenService examAccessTokenService,
        SessionDeviceGuardService sessionDeviceGuardService,
        IGradingService gradingService,
        IExamHubNotifier examHubNotifier)
    {
        _sessionRepo = sessionRepo;
        _testRepo = testRepo;
        _userRepo = userRepo;
        _questionRepo = questionRepo;
        _sessionLogRepo = sessionLogRepo;
        _examScheduleRepo = examScheduleRepo;
        _testService = testService;
        _assessmentService = assessmentService;
        _examAccessTokenService = examAccessTokenService;
        _sessionDeviceGuardService = sessionDeviceGuardService;
        _gradingService = gradingService;
        _examHubNotifier = examHubNotifier;
    }

    public async Task<SessionServiceResult<StartSessionData>> StartSessionAsync(StartSessionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId) || string.IsNullOrWhiteSpace(command.TestId))
        {
            return Failure<StartSessionData>(SessionServiceStatus.BadRequest, "INVALID_INPUT", "Missing user or test.");
        }

        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == command.TestId);
        if (test == null || !test.IsPublished)
        {
            var status = command.ReturnNotFoundForUnavailableTest
                ? SessionServiceStatus.NotFound
                : SessionServiceStatus.Forbidden;
            return Failure<StartSessionData>(status, "TEST_UNAVAILABLE", "Test is unavailable.");
        }

        var now = DateTime.UtcNow;
        var availableIds = await _assessmentService.GetAvailableTestIdsAsync(command.UserId, now);
        if (!availableIds.Contains(command.TestId))
        {
            return Failure<StartSessionData>(SessionServiceStatus.Forbidden, "TEST_NOT_AVAILABLE", "Test is not available.");
        }

        if (!string.IsNullOrWhiteSpace(command.ScheduleId))
        {
            var schedule = await _examScheduleRepo.FirstOrDefaultAsync(s => s.Id == command.ScheduleId && !s.IsDeleted);
            if (schedule == null || !string.Equals(schedule.TestId, command.TestId, StringComparison.Ordinal))
            {
                return Failure<StartSessionData>(SessionServiceStatus.Forbidden, "SCHEDULE_INVALID", "Schedule is invalid.");
            }

            var validToken = _examAccessTokenService.Validate(
                command.AccessToken ?? string.Empty,
                command.UserId,
                command.TestId,
                command.ScheduleId,
                out _);
            if (!validToken)
            {
                return Failure<StartSessionData>(SessionServiceStatus.Forbidden, "ACCESS_TOKEN_INVALID", "Invalid access token.");
            }

            if (schedule.IsManuallyLocked)
            {
                return Failure<StartSessionData>(
                    SessionServiceStatus.Forbidden,
                    "SCHEDULE_MANUALLY_LOCKED",
                    "Lịch thi tạm thời bị khóa bởi quản trị viên. Vui lòng liên hệ phòng thi.");
            }

            if (now < schedule.StartTime || now > schedule.EndTime)
            {
                return Failure<StartSessionData>(
                    SessionServiceStatus.Forbidden,
                    "SCHEDULE_OUTSIDE_WINDOW",
                    "Exam schedule is outside the allowed time window.");
            }
        }

        var requestFingerprint = GetRequestFingerprint(command.RequestContext);
        var hasOtherActiveDevice = await _sessionDeviceGuardService.HasActiveSessionOnOtherDeviceAsync(command.UserId, requestFingerprint);
        if (hasOtherActiveDevice)
        {
            return Failure<StartSessionData>(SessionServiceStatus.Conflict, "ACTIVE_ON_OTHER_DEVICE", "Session is active on another device.");
        }

        var sessionsOfUser = (await _sessionRepo.GetAllAsync())
            .Where(s => s.UserId == command.UserId && s.TestId == command.TestId)
            .ToList();

        if (command.BlockRestartAfterSubmit)
        {
            var latestSubmitted = sessionsOfUser
                .Where(s => SubmittedStatuses.Contains(s.Status))
                .OrderByDescending(s => s.EndAt ?? s.StartAt)
                .FirstOrDefault();
            if (latestSubmitted != null)
            {
                var data = new StartSessionData
                {
                    SessionId = latestSubmitted.Id,
                    IsLatestSubmitted = true,
                    DurationMinutes = Math.Max(1, test.DurationMinutes),
                    RemainingSeconds = ComputeRemainingSeconds(latestSubmitted, test.DurationMinutes)
                };
                return Success(data);
            }
        }

        var inProgress = sessionsOfUser
            .Where(s => s.Status == SessionStatus.InProgress)
            .OrderByDescending(s => s.StartAt)
            .FirstOrDefault();
        if (inProgress != null)
        {
            var allowed = await EnsureSessionDeviceAsync(inProgress.Id, command.RequestContext);
            if (!allowed)
            {
                return Failure<StartSessionData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
            }

            var sessionForStartData = inProgress;
            if (command.IncludeQuestionPayload)
            {
                sessionForStartData = await GetSessionWithQuestionGraphAsync(inProgress.Id) ?? inProgress;
                var sessionTest = await GetTestWithQuestionsAsync(sessionForStartData.TestId) ?? test;
                var repaired = await EnsureSessionAnswersInitializedAsync(sessionForStartData, sessionTest);
                if (repaired)
                {
                    sessionForStartData = await GetSessionWithQuestionGraphAsync(inProgress.Id) ?? sessionForStartData;
                }
            }

            return Success(await BuildStartSessionDataAsync(sessionForStartData, test, command.IncludeQuestionPayload));
        }

        var started = await _testService.StartAsync(command.TestId, command.UserId);
        var bound = await EnsureSessionDeviceAsync(started.Id, command.RequestContext);
        if (!bound)
        {
            return Failure<StartSessionData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        var startData = await BuildStartSessionDataAsync(started, test, command.IncludeQuestionPayload);
        await NotifySessionStartedSafeAsync(started, test);

        return Success(startData);
    }

    public async Task<SessionServiceResult<ResumeSessionData>> ResumeSessionAsync(ResumeSessionCommand command)
    {
        var session = await GetSessionWithQuestionGraphAsync(command.SessionId);
        if (session == null)
        {
            return Failure<ResumeSessionData>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        var test = await GetTestWithQuestionsAsync(session.TestId);
        if (test == null)
        {
            return Failure<ResumeSessionData>(SessionServiceStatus.NotFound, "TEST_NOT_FOUND", "Test not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<ResumeSessionData>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var allowed = await EnsureSessionDeviceAsync(session.Id, command.RequestContext);
        if (!allowed)
        {
            return Failure<ResumeSessionData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        var repaired = await EnsureSessionAnswersInitializedAsync(session, test);
        if (repaired)
        {
            session = await GetSessionWithQuestionGraphAsync(session.Id) ?? session;
        }

        session.LastActivityAt = DateTime.UtcNow;
        if (!session.TimerStartedAt.HasValue)
        {
            session.TimerStartedAt = DateTime.UtcNow;
        }

        await _sessionRepo.UpsertAsync(x => x.Id == session.Id, session);

        return Success(new ResumeSessionData
        {
            Session = session,
            TestTitle = test.Title,
            DurationMinutes = test.DurationMinutes,
            RemainingSeconds = ComputeRemainingSeconds(session, test.DurationMinutes)
        });
    }

    public async Task<SessionServiceResult<SaveAnswerData>> SaveAnswerAsync(SaveAnswerCommand command)
    {
        var session = await GetSessionWithAnswersAsync(command.SessionId);
        if (session == null)
        {
            return Failure<SaveAnswerData>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<SaveAnswerData>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var allowed = await EnsureSessionDeviceAsync(session.Id, command.RequestContext);
        if (!allowed)
        {
            return Failure<SaveAnswerData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        if (session.Status != SessionStatus.InProgress)
        {
            if (SubmittedStatuses.Contains(session.Status))
            {
                return Failure<SaveAnswerData>(SessionServiceStatus.Conflict, "SESSION_ALREADY_SUBMITTED", "Session has already been submitted.");
            }

            return Failure<SaveAnswerData>(SessionServiceStatus.BadRequest, "SESSION_NOT_ACTIVE", "Session is not active.");
        }

        var allQuestions = await _questionRepo.GetAllAsync();
        var questionMap = allQuestions.ToDictionary(x => x.Id, x => x);

        var updatedCount = 0;
        var now = DateTime.UtcNow;
        foreach (var studentAnswer in session.StudentAnswers)
        {
            if (!command.Answers.TryGetValue(studentAnswer.QuestionId, out var raw))
            {
                continue;
            }

            if (!questionMap.TryGetValue(studentAnswer.QuestionId, out var question))
            {
                continue;
            }

            var effectiveClientTimestamp = command.QuestionClientTimestamps.TryGetValue(studentAnswer.QuestionId, out var perQuestionTimestamp)
                ? NormalizeToUtc(perQuestionTimestamp)
                : NormalizeToUtc(command.ClientTimestamp);

            if (effectiveClientTimestamp.HasValue && studentAnswer.AnsweredAt >= effectiveClientTimestamp.Value)
            {
                continue;
            }

            var value = (raw ?? string.Empty).Trim();
            switch (question.Type)
            {
                case QType.MCQ:
                    studentAnswer.SelectedOptionId = string.IsNullOrEmpty(value) ? null : value;
                    break;
                case QType.TrueFalse:
                    if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        value = "True";
                    }

                    if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        value = "False";
                    }

                    studentAnswer.SelectedOptionId = string.IsNullOrEmpty(value) ? null : value;
                    break;
                case QType.Essay:
                    studentAnswer.EssayAnswer = value;
                    break;
                default:
                    studentAnswer.EssayAnswer = value;
                    break;
            }

            studentAnswer.AnsweredAt = effectiveClientTimestamp ?? now;
            updatedCount++;
        }

        session.LastActivityAt = now;
        await _sessionRepo.UpsertAsync(x => x.Id == session.Id, session);

        return Success(new SaveAnswerData
        {
            UpdatedCount = updatedCount,
            At = now
        });
    }

    public async Task<SessionServiceResult<SubmitSessionData>> SubmitSessionAsync(SubmitSessionCommand command)
    {
        var session = await GetSessionWithAnswersAsync(command.SessionId);
        if (session == null)
        {
            return Failure<SubmitSessionData>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<SubmitSessionData>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var allowed = await EnsureSessionDeviceAsync(session.Id, command.RequestContext);
        if (!allowed)
        {
            return Failure<SubmitSessionData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        var submitted = await _testService.SubmitAsync(command.SessionId, command.Answers);
        var submitData = MapSubmitSessionData(submitted);

        await NotifySessionSubmittedSafeAsync(session, submitData);

        return Success(submitData);
    }

    public async Task<SubmitSessionData?> AutoSubmitAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var session = await GetSessionWithAnswersAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        if (SubmittedStatuses.Contains(session.Status))
        {
            return MapSubmitSessionData(session);
        }

        var answers = session.StudentAnswers.ToDictionary(
            answer => answer.QuestionId,
            answer => !string.IsNullOrWhiteSpace(answer.EssayAnswer)
                ? answer.EssayAnswer
                : answer.SelectedOptionId);

        var submitted = await _testService.SubmitAsync(sessionId, answers);
        submitted.Status = SessionStatus.AutoSubmitted;
        submitted.LastActivityAt = DateTime.UtcNow;
        await _sessionRepo.UpsertAsync(x => x.Id == submitted.Id, submitted);

        var submitData = MapSubmitSessionData(submitted);
        await SafeNotifyAsync(() => _examHubNotifier.NotifyAutoSubmittedAsync(sessionId));

        return submitData;
    }

    private static SubmitSessionData MapSubmitSessionData(Session session)
    {
        return new SubmitSessionData
        {
            Id = session.Id,
            TotalScore = session.TotalScore,
            MaxScore = session.MaxScore,
            Percent = session.Percent,
            IsPassed = session.IsPassed,
            Status = session.Status
        };
    }

    public async Task<SessionServiceResult<GetSessionResultData>> GetSessionResultAsync(GetSessionResultCommand command)
    {
        var session = await GetSessionWithQuestionGraphAsync(command.SessionId);
        if (session == null)
        {
            return Failure<GetSessionResultData>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<GetSessionResultData>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var currentTestId = session.TestId;
        await _sessionRepo.DeleteAsync(x => x.UserId == command.UserId
                                            && x.TestId == currentTestId
                                            && x.Id != session.Id
                                            && !SubmittedStatuses.Contains(x.Status));

        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == session.TestId);
        var hasPendingRegrade = await _gradingService.HasPendingRegradeRequestAsync(command.SessionId);

        return Success(new GetSessionResultData
        {
            Session = session,
            TestTitle = test?.Title ?? "Result",
            HasPendingRegrade = hasPendingRegrade
        });
    }

    public async Task<SessionServiceResult<SessionTimerData>> PauseSessionAsync(SessionTimerCommand command)
    {
        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == command.SessionId);
        if (session == null)
        {
            return Failure<SessionTimerData>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<SessionTimerData>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var allowed = await EnsureSessionDeviceAsync(session.Id, command.RequestContext);
        if (!allowed)
        {
            return Failure<SessionTimerData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == session.TestId);
        if (test == null)
        {
            return Failure<SessionTimerData>(SessionServiceStatus.NotFound, "TEST_NOT_FOUND", "Test not found.");
        }

        if (session.TimerStartedAt.HasValue)
        {
            var delta = (int)Math.Floor((DateTime.UtcNow - session.TimerStartedAt.Value).TotalSeconds);
            session.ConsumedSeconds = Math.Max(0, session.ConsumedSeconds + Math.Max(0, delta));
            session.TimerStartedAt = null;
            session.LastActivityAt = DateTime.UtcNow;
            await _sessionRepo.UpsertAsync(x => x.Id == session.Id, session);
        }

        return Success(new SessionTimerData
        {
            RemainingSeconds = ComputeRemainingSeconds(session, test.DurationMinutes),
            Running = false
        });
    }

    public async Task<SessionServiceResult<SessionTimerData>> ResumeTimerAsync(SessionTimerCommand command)
    {
        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == command.SessionId);
        if (session == null)
        {
            return Failure<SessionTimerData>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<SessionTimerData>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var allowed = await EnsureSessionDeviceAsync(session.Id, command.RequestContext);
        if (!allowed)
        {
            return Failure<SessionTimerData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == session.TestId);
        if (test == null)
        {
            return Failure<SessionTimerData>(SessionServiceStatus.NotFound, "TEST_NOT_FOUND", "Test not found.");
        }

        if (command.RequireInProgressState)
        {
            var remaining = ComputeRemainingSeconds(session, test.DurationMinutes);
            if (remaining <= 0 || session.Status != SessionStatus.InProgress)
            {
                return Success(new SessionTimerData
                {
                    RemainingSeconds = Math.Max(0, remaining),
                    Running = false
                });
            }

            if (!session.TimerStartedAt.HasValue)
            {
                session.TimerStartedAt = DateTime.UtcNow;
                session.LastActivityAt = DateTime.UtcNow;
                await _sessionRepo.UpsertAsync(x => x.Id == session.Id, session);
            }

            return Success(new SessionTimerData
            {
                RemainingSeconds = ComputeRemainingSeconds(session, test.DurationMinutes),
                Running = true
            });
        }

        if (!session.TimerStartedAt.HasValue)
        {
            session.TimerStartedAt = DateTime.UtcNow;
        }

        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepo.UpsertAsync(x => x.Id == session.Id, session);

        return Success(new SessionTimerData
        {
            RemainingSeconds = ComputeRemainingSeconds(session, test.DurationMinutes),
            Running = true
        });
    }

    public async Task<SessionServiceResult<SessionTouchData>> TouchSessionAsync(SessionTouchCommand command)
    {
        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == command.SessionId);
        if (session == null)
        {
            return Failure<SessionTouchData>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<SessionTouchData>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var allowed = await EnsureSessionDeviceAsync(session.Id, command.RequestContext);
        if (!allowed)
        {
            return Failure<SessionTouchData>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepo.UpsertAsync(x => x.Id == session.Id, session);

        return Success(new SessionTouchData
        {
            At = session.LastActivityAt,
            RemainingSeconds = await GetRemainingSecondsOrDefaultAsync(session),
            Running = session.TimerStartedAt.HasValue
        });
    }

    public async Task<SessionServiceResult<bool>> LogEventAsync(SessionLogEventCommand command)
    {
        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == command.SessionId);
        if (session == null)
        {
            return Failure<bool>(SessionServiceStatus.NotFound, "SESSION_NOT_FOUND", "Session not found.");
        }

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
        {
            return Failure<bool>(SessionServiceStatus.Forbidden, "SESSION_FORBIDDEN", "Session does not belong to current user.");
        }

        var allowed = await EnsureSessionDeviceAsync(session.Id, command.RequestContext);
        if (!allowed)
        {
            return Failure<bool>(SessionServiceStatus.Conflict, "SESSION_BOUND_OTHER_DEVICE", "Session is bound to another device.");
        }

        var log = new SessionLog
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = command.SessionId,
            ActionType = command.ActionType,
            Detail = command.Detail,
            Timestamp = DateTime.UtcNow,
            IPAddress = command.RequestContext.IpAddress
        };

        await _sessionLogRepo.InsertAsync(log);

        var blurCount = string.Equals(command.ActionType, "Blur", StringComparison.OrdinalIgnoreCase)
            ? await _sessionLogRepo.CountAsync(x => x.SessionId == command.SessionId && x.ActionType == "Blur")
            : 0;

        var severity = ClassifyAntiCheatSeverity(command.ActionType, command.Detail, blurCount);
        if (string.Equals(severity, "Critical", StringComparison.Ordinal))
        {
            var violationCount = await CountCriticalViolationsAsync(command.SessionId);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == session.UserId);
            var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == session.TestId);

            var payload = new AntiCheatAlertPayload
            {
                SessionId = session.Id,
                UserId = session.UserId,
                UserName = user?.Name ?? user?.Email ?? session.UserId,
                TestId = session.TestId,
                TestTitle = test?.Title ?? "Unknown",
                ActionType = command.ActionType,
                Detail = command.Detail,
                Timestamp = log.Timestamp,
                ViolationCount = violationCount,
                Severity = "Critical"
            };

            await SafeNotifyAsync(() => _examHubNotifier.NotifyAntiCheatAlertAsync(session.TestId, payload));
        }

        return Success(true);
    }

    public async Task<List<AdminSessionItem>> GetAdminSessionsAsync()
    {
        var sessions = await _sessionRepo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();
        var tests = await _testRepo.GetAllAsync();

        return sessions
            .Select(s =>
            {
                var user = users.FirstOrDefault(x => x.Id == s.UserId);
                var test = tests.FirstOrDefault(x => x.Id == s.TestId);
                return new AdminSessionItem
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = user?.Name ?? "Unknown",
                    UserEmail = user?.Email,
                    TestId = s.TestId,
                    TestTitle = test?.Title ?? "Unknown",
                    StartAt = s.StartAt,
                    EndAt = s.EndAt,
                    Status = s.Status,
                    LastActivityAt = s.LastActivityAt,
                    TotalScore = s.TotalScore,
                    MaxScore = s.MaxScore,
                    Percent = s.Percent,
                    IsPassed = s.IsPassed
                };
            })
            .OrderByDescending(x => x.LastActivityAt)
            .ToList();
    }

    public async Task<List<Session>> GetActiveSessionsForTimerAsync()
    {
        return await _sessionRepo.GetAllAsync(s =>
            s.Status == SessionStatus.InProgress &&
            s.TimerStartedAt != null &&
            !s.IsDeleted);
    }

    public async Task<bool> TerminateSessionAsync(string id)
    {
        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (session == null) return false;

        await _sessionRepo.DeleteAsync(x => x.Id == id);
        return true;
    }

    private async Task<StartSessionData> BuildStartSessionDataAsync(Session session, Test test, bool includeQuestionPayload)
    {
        var data = new StartSessionData
        {
            SessionId = session.Id,
            DurationMinutes = Math.Max(1, test.DurationMinutes),
            RemainingSeconds = ComputeRemainingSeconds(session, test.DurationMinutes)
        };

        if (includeQuestionPayload)
        {
            data.Questions = await BuildQuestionPayloadAsync(session);
        }

        return data;
    }

    private async Task<List<SessionQuestionDto>> BuildQuestionPayloadAsync(Session session)
    {
        var allQuestions = await _questionRepo.GetAllAsync();
        var questionMap = allQuestions.ToDictionary(x => x.Id, x => x);

        return session.StudentAnswers
            .Select(answer => questionMap.TryGetValue(answer.QuestionId, out var question) ? question : null)
            .Where(question => question != null)
            .Select(question => new SessionQuestionDto
            {
                Id = question!.Id,
                Type = question.Type,
                Content = question.Content,
                Options = question.Options.ToList()
            })
            .ToList();
    }

    private async Task NotifySessionStartedSafeAsync(Session session, Test test)
    {
        await SafeNotifyAsync(async () =>
        {
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == session.UserId);
            var item = new AdminSessionItem
            {
                Id = session.Id,
                UserId = session.UserId,
                UserName = user?.Name ?? "Unknown",
                UserEmail = user?.Email,
                TestId = session.TestId,
                TestTitle = test.Title,
                StartAt = session.StartAt,
                EndAt = session.EndAt,
                Status = session.Status,
                LastActivityAt = session.LastActivityAt,
                TotalScore = session.TotalScore,
                MaxScore = session.MaxScore,
                Percent = session.Percent,
                IsPassed = session.IsPassed
            };

            await _examHubNotifier.NotifySessionStartedAsync(item);
        });
    }

    private async Task NotifySessionSubmittedSafeAsync(Session session, SubmitSessionData submitData)
    {
        await SafeNotifyAsync(async () =>
        {
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == session.UserId);
            var userName = user?.Name ?? user?.Email ?? session.UserId;
            await _examHubNotifier.NotifySessionSubmittedAsync(session.TestId, submitData, userName);
        });
    }

    private static async Task SafeNotifyAsync(Func<Task> notifyAction)
    {
        try
        {
            await notifyAction();
        }
        catch
        {
            // Ignore realtime notification failures to keep core session flow stable.
        }
    }

    private async Task<int> CountCriticalViolationsAsync(string sessionId)
    {
        var logs = await _sessionLogRepo.GetAllAsync(x => x.SessionId == sessionId);
        var orderedLogs = logs
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Id)
            .ToList();

        var blurOccurrence = 0;
        var criticalCount = 0;

        foreach (var log in orderedLogs)
        {
            if (string.Equals(log.ActionType, "Blur", StringComparison.OrdinalIgnoreCase))
            {
                blurOccurrence++;
            }

            if (IsCriticalAntiCheat(log.ActionType, log.Detail, blurOccurrence))
            {
                criticalCount++;
            }
        }

        return criticalCount;
    }

    private static string ClassifyAntiCheatSeverity(string actionType, string? detail, int blurCount)
    {
        if (IsCriticalAntiCheat(actionType, detail, blurCount))
        {
            return "Critical";
        }

        if (string.Equals(actionType, "CopyBlocked", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actionType, "CutBlocked", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actionType, "PasteBlocked", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actionType, "ContextMenuBlocked", StringComparison.OrdinalIgnoreCase))
        {
            return "Warning";
        }

        if (string.Equals(actionType, "Blur", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actionType, "Focus", StringComparison.OrdinalIgnoreCase))
        {
            return "Info";
        }

        return "Info";
    }

    private static bool IsCriticalAntiCheat(string actionType, string? detail, int blurCount)
    {
        if (string.Equals(actionType, "ShortcutBlocked", StringComparison.OrdinalIgnoreCase))
        {
            return IsCriticalShortcut(detail);
        }

        return string.Equals(actionType, "Blur", StringComparison.OrdinalIgnoreCase) && blurCount >= 3;
    }

    private static bool IsCriticalShortcut(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return false;
        }

        var normalized = detail.Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized.Contains("F12", StringComparison.Ordinal) ||
               normalized.Contains("CTRL+U", StringComparison.Ordinal);
    }

    private async Task<int> GetRemainingSecondsOrDefaultAsync(Session session)
    {
        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == session.TestId);
        var durationMinutes = test?.DurationMinutes ?? 30;
        return ComputeRemainingSeconds(session, durationMinutes);
    }

    private async Task<bool> EnsureSessionAnswersInitializedAsync(Session session, Test test)
    {
        if (session.Status != SessionStatus.InProgress || session.StudentAnswers.Count > 0)
        {
            return false;
        }

        var testQuestionIds = test.TestQuestions
            .Select(tq => tq.QuestionId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (testQuestionIds.Count == 0 && test.QuestionSnapshots.Count > 0)
        {
            testQuestionIds = test.QuestionSnapshots
                .OrderBy(x => x.Order)
                .Select(x => x.OriginalQuestionId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        if (testQuestionIds.Count == 0)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var questionId in testQuestionIds)
        {
            session.StudentAnswers.Add(new StudentAnswer
            {
                SessionId = session.Id,
                QuestionId = questionId,
                Score = 0m,
                AnsweredAt = now
            });
        }

        session.UpdatedAt = now;
        await _sessionRepo.UpsertAsync(x => x.Id == session.Id, session);
        return true;
    }

    private Task<Session?> GetSessionWithAnswersAsync(string sessionId)
    {
        var spec = new Specification<Session>(x => x.Id == sessionId)
            .Include(s => s.StudentAnswers);
        return _sessionRepo.FirstOrDefaultAsync(spec);
    }

    private Task<Session?> GetSessionWithQuestionGraphAsync(string sessionId)
    {
        var spec = new Specification<Session>(x => x.Id == sessionId)
            .Include(s => s.StudentAnswers)
            .Include("StudentAnswers.Question.Options");
        return _sessionRepo.FirstOrDefaultAsync(spec);
    }

    private Task<Test?> GetTestWithQuestionsAsync(string testId)
    {
        var spec = new Specification<Test>(x => x.Id == testId)
            .Include(x => x.TestQuestions)
            .Include(x => x.QuestionSnapshots);
        return _testRepo.FirstOrDefaultAsync(spec);
    }

    private async Task<bool> EnsureSessionDeviceAsync(string sessionId, SessionRequestContext context)
    {
        var requestFingerprint = GetRequestFingerprint(context);
        return await _sessionDeviceGuardService.EnsureSessionDeviceAsync(
            sessionId,
            requestFingerprint,
            context.UserAgent,
            context.IpAddress);
    }

    private string GetRequestFingerprint(SessionRequestContext context)
    {
        return _sessionDeviceGuardService.GetRequestFingerprint(context.UserAgent, context.IpAddress);
    }

    private static int ComputeRemainingSeconds(Session session, int durationMinutes)
    {
        var total = Math.Max(1, durationMinutes) * 60;
        var runningDelta = session.TimerStartedAt.HasValue
            ? (int)Math.Floor((DateTime.UtcNow - session.TimerStartedAt.Value).TotalSeconds)
            : 0;
        var consumed = Math.Max(0, session.ConsumedSeconds + Math.Max(0, runningDelta));
        return Math.Max(0, total - consumed);
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var timestamp = value.Value;
        return timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
        };
    }

    private static SessionServiceResult<T> Success<T>(T data)
    {
        return new SessionServiceResult<T>
        {
            Status = SessionServiceStatus.Success,
            Data = data
        };
    }

    private static SessionServiceResult<T> Failure<T>(SessionServiceStatus status, string errorCode, string message)
    {
        return new SessionServiceResult<T>
        {
            Status = status,
            ErrorCode = errorCode,
            Message = message
        };
    }
}
