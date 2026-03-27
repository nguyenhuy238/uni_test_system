using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using System.Text.Json;

namespace UniTestSystem.Application
{
    public class TranscriptService : ITranscriptService
    {
        private const string TranscriptLockEntityName = "TranscriptLock";
        private const string TranscriptLockActionLock = "Lock";
        private const string TranscriptLockActionUnlock = "Unlock";
        private const string SchoolScope = "school";
        private const string ActionGradeLocked = "GradeLocked";
        private const string ActionGradeUnlocked = "GradeUnlocked";

        private readonly IRepository<Enrollment> _enrollmentRepo;
        private readonly IRepository<Transcript> _transcriptRepo;
        private readonly IRepository<Course> _courseRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<StudentClass> _classRepo;
        private readonly IRepository<Faculty> _facultyRepo;
        private readonly IRepository<ExamSchedule> _examScheduleRepo;
        private readonly IRepository<Session> _sessionRepo;
        private readonly IRepository<SessionLog> _sessionLogRepo;
        private readonly IRepository<AuditEntry> _auditRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IReportExportService _exportService;
        private readonly ISettingsService _settingsService;

        public TranscriptService(
            IRepository<Enrollment> enrollmentRepo,
            IRepository<Transcript> transcriptRepo,
            IRepository<Course> courseRepo,
            IRepository<Student> studentRepo,
            IRepository<StudentClass> classRepo,
            IRepository<Faculty> facultyRepo,
            IRepository<ExamSchedule> examScheduleRepo,
            IRepository<Session> sessionRepo,
            IRepository<SessionLog> sessionLogRepo,
            IRepository<AuditEntry> auditRepo,
            IRepository<User> userRepo,
            IReportExportService exportService,
            ISettingsService settingsService)
        {
            _enrollmentRepo = enrollmentRepo;
            _transcriptRepo = transcriptRepo;
            _courseRepo = courseRepo;
            _studentRepo = studentRepo;
            _classRepo = classRepo;
            _facultyRepo = facultyRepo;
            _examScheduleRepo = examScheduleRepo;
            _sessionRepo = sessionRepo;
            _sessionLogRepo = sessionLogRepo;
            _auditRepo = auditRepo;
            _userRepo = userRepo;
            _exportService = exportService;
            _settingsService = settingsService;
        }

        public async Task<TranscriptAdminPageResult> GetAdminTranscriptPageAsync(TranscriptAdminQuery query)
        {
            var facultyId = query.FacultyId;
            var classId = query.ClassId;
            var semester = query.Semester;

            var rows = await GetAdminTranscriptRowsAsync(facultyId, classId, semester);
            var faculties = (await _facultyRepo.GetAllAsync(x => !x.IsDeleted))
                .OrderBy(x => x.Name)
                .Select(x => new TranscriptLookupResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            var classes = (await _classRepo.GetAllAsync(x => !x.IsDeleted))
                .OrderBy(x => x.Name)
                .Select(x => new TranscriptLookupResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            var semesters = await GetAvailableSemestersAsync();
            var schoolLocked = await IsSchoolTranscriptLockedAsync();
            var facultyLockMap = await GetFacultyTranscriptLockMapAsync();
            var selectedFacultyLocked = false;
            if (!string.IsNullOrWhiteSpace(facultyId))
                selectedFacultyLocked = facultyLockMap.TryGetValue(facultyId, out var locked) && locked;

            return new TranscriptAdminPageResult
            {
                Rows = rows,
                Faculties = faculties,
                Classes = classes,
                Semesters = semesters,
                SchoolTranscriptLocked = schoolLocked,
                FacultyTranscriptLockMap = facultyLockMap,
                SelectedFacultyTranscriptLocked = selectedFacultyLocked
            };
        }

        public async Task<StudentTranscriptDetailsResult?> GetStudentTranscriptDetailsAsync(string studentId, string? semester = null)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return null;

            var allGrades = await GetStudentGradesAsync(studentId);
            var summary = await GetStudentTranscriptSummaryAsync(studentId);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == studentId);
            if (user == null && summary == null && allGrades.Count == 0)
                return null;

            var grades = string.IsNullOrWhiteSpace(semester)
                ? allGrades
                : allGrades
                    .Where(x => string.Equals(x.Semester, semester, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var schoolLocked = await IsSchoolTranscriptLockedAsync();
            var student = await _studentRepo.FirstOrDefaultAsync(x => x.Id == studentId);
            var facultyLocked = false;
            string? facultyName = null;
            if (!string.IsNullOrWhiteSpace(student?.StudentClassId))
            {
                var studentClass = await _classRepo.FirstOrDefaultAsync(x => x.Id == student.StudentClassId);
                if (!string.IsNullOrWhiteSpace(studentClass?.FacultyId))
                {
                    facultyLocked = await IsFacultyTranscriptLockedAsync(studentClass.FacultyId);
                    facultyName = (await _facultyRepo.FirstOrDefaultAsync(x => x.Id == studentClass.FacultyId))?.Name;
                }
            }

            var semesters = allGrades
                .Select(x => x.Semester)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            return new StudentTranscriptDetailsResult
            {
                StudentId = studentId,
                StudentName = user?.Name ?? studentId,
                Summary = summary,
                Grades = grades,
                Semester = semester,
                Semesters = semesters,
                IsTranscriptLocked = schoolLocked || facultyLocked,
                SchoolLocked = schoolLocked,
                FacultyLocked = facultyLocked,
                FacultyLockName = facultyName
            };
        }

        public async Task<TranscriptExportResult> ExportAdminTranscriptOverviewXlsxAsync(TranscriptAdminQuery query)
        {
            var rows = await GetAdminTranscriptRowsAsync(query.FacultyId, query.ClassId, query.Semester);
            var facultyName = await ResolveFacultyNameAsync(query.FacultyId);
            var className = await ResolveClassNameAsync(query.ClassId);
            return new TranscriptExportResult
            {
                Content = _exportService.ExportTranscriptOverviewExcel(rows, facultyName, className, query.Semester),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"transcripts-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"
            };
        }

        public async Task<TranscriptExportResult> ExportAdminTranscriptOverviewPdfAsync(TranscriptAdminQuery query)
        {
            var rows = await GetAdminTranscriptRowsAsync(query.FacultyId, query.ClassId, query.Semester);
            var facultyName = await ResolveFacultyNameAsync(query.FacultyId);
            var className = await ResolveClassNameAsync(query.ClassId);
            var settings = await _settingsService.GetAsync();
            return new TranscriptExportResult
            {
                Content = _exportService.ExportTranscriptOverviewPdf(rows, settings, facultyName, className, query.Semester),
                ContentType = "application/pdf",
                FileName = $"transcripts-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"
            };
        }

        public async Task<TranscriptExportResult> ExportStudentTranscriptXlsxAsync(string studentId, string? semester = null)
        {
            var data = await BuildStudentExportDataAsync(studentId, semester);
            return new TranscriptExportResult
            {
                Content = _exportService.ExportStudentTranscriptExcel(data.Grades, data.Summary, data.StudentName, studentId),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"student-transcript-{studentId}.xlsx"
            };
        }

        public async Task<TranscriptExportResult> ExportStudentTranscriptPdfAsync(string studentId, string? semester = null)
        {
            var data = await BuildStudentExportDataAsync(studentId, semester);
            var settings = await _settingsService.GetAsync();
            return new TranscriptExportResult
            {
                Content = _exportService.ExportStudentTranscriptPdf(data.Grades, data.Summary, data.StudentName, studentId, settings),
                ContentType = "application/pdf",
                FileName = $"student-transcript-{studentId}.pdf"
            };
        }

        public async Task<TranscriptExportResult> ExportMyTranscriptXlsxAsync(string studentId)
        {
            var data = await BuildStudentExportDataAsync(studentId, semester: null);
            return new TranscriptExportResult
            {
                Content = _exportService.ExportStudentTranscriptExcel(data.Grades, data.Summary, data.StudentName, studentId),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"my-transcript-{studentId}.xlsx"
            };
        }

        public async Task<TranscriptExportResult> ExportMyTranscriptPdfAsync(string studentId)
        {
            var data = await BuildStudentExportDataAsync(studentId, semester: null);
            var settings = await _settingsService.GetAsync();
            return new TranscriptExportResult
            {
                Content = _exportService.ExportStudentTranscriptPdf(data.Grades, data.Summary, data.StudentName, studentId, settings),
                ContentType = "application/pdf",
                FileName = $"my-transcript-{studentId}.pdf"
            };
        }

        public async Task<FinalizeGradeResult> FinalizeGradeAsync(FinalizeGradeCommand command)
        {
            try
            {
                decimal resolvedFinalScore;
                if (command.ExamScore.HasValue || command.AssignmentScore.HasValue)
                {
                    if (!command.ExamScore.HasValue || !command.AssignmentScore.HasValue)
                        throw new Exception("Both exam score and assignment score are required when weighted formula is used.");

                    resolvedFinalScore = CalculateWeightedFinalScore(
                        command.AssignmentScore.Value,
                        command.ExamScore.Value,
                        command.AssignmentWeight ?? 30m,
                        command.ExamWeight ?? 70m);
                }
                else if (command.FinalScore.HasValue)
                {
                    resolvedFinalScore = command.FinalScore.Value;
                }
                else
                {
                    throw new Exception("Final score is required.");
                }

                await FinalizeCourseGradeAsync(command.EnrollmentId, resolvedFinalScore);
                return new FinalizeGradeResult
                {
                    Success = true,
                    ResolvedFinalScore = resolvedFinalScore
                };
            }
            catch (Exception ex)
            {
                return new FinalizeGradeResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<YearEndFinalizeResult> FinalizeYearEndAsync(string academicYear, string? facultyId, string staffId)
        {
            var preview = await PreviewYearEndAsync(academicYear, facultyId);
            if (!preview.Prerequisites.IsReady)
            {
                return new YearEndFinalizeResult
                {
                    AcademicYear = preview.AcademicYear,
                    FacultyId = preview.FacultyId,
                    FacultyName = preview.FacultyName,
                    Success = false,
                    Messages = preview.Prerequisites.MissingItems.ToList(),
                    Prerequisites = preview.Prerequisites
                };
            }

            var normalizedAcademicYear = NormalizeAcademicYear(academicYear);
            var now = DateTime.UtcNow;
            var actor = string.IsNullOrWhiteSpace(staffId) ? "system" : staffId.Trim();

            foreach (var row in preview.Students)
            {
                var existing = await _transcriptRepo.FirstOrDefaultAsync(t =>
                    !t.IsDeleted &&
                    t.StudentId == row.StudentId &&
                    t.AcademicYear == normalizedAcademicYear);

                if (existing == null)
                {
                    var created = new Transcript
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        StudentId = row.StudentId,
                        AcademicYear = normalizedAcademicYear,
                        GPA = row.YearEndGpa4,
                        TotalCredits = row.TotalCreditsEarned,
                        YearEndGpa4 = row.YearEndGpa4,
                        YearEndGpa10 = row.YearEndGpa10,
                        YearEndTotalCreditsEarned = row.TotalCreditsEarned,
                        AcademicStatus = row.AcademicStatus,
                        IsYearEndFinalized = true,
                        IsYearEndLocked = true,
                        YearEndFinalizedAt = now,
                        YearEndFinalizedBy = actor,
                        CalculatedAt = now,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _transcriptRepo.InsertAsync(created);
                    continue;
                }

                existing.GPA = row.YearEndGpa4;
                existing.TotalCredits = row.TotalCreditsEarned;
                existing.YearEndGpa4 = row.YearEndGpa4;
                existing.YearEndGpa10 = row.YearEndGpa10;
                existing.YearEndTotalCreditsEarned = row.TotalCreditsEarned;
                existing.AcademicStatus = row.AcademicStatus;
                existing.IsYearEndFinalized = true;
                existing.IsYearEndLocked = true;
                existing.YearEndFinalizedAt = now;
                existing.YearEndFinalizedBy = actor;
                existing.CalculatedAt = now;
                existing.UpdatedAt = now;
                await _transcriptRepo.UpdateAsync(existing);
            }

            await SaveScopeLockAsync(GetYearEndScope(normalizedAcademicYear, facultyId), lockState: true, actor, $"Year-end finalized: {normalizedAcademicYear}");

            return new YearEndFinalizeResult
            {
                AcademicYear = preview.AcademicYear,
                FacultyId = preview.FacultyId,
                FacultyName = preview.FacultyName,
                Success = true,
                YearTranscriptLocked = true,
                FinalizedAtUtc = now,
                FinalizedStudents = preview.Students.Count,
                WarningStudents = preview.WarningStudents.Count(x => string.Equals(x.AcademicStatus, "Warning", StringComparison.OrdinalIgnoreCase)),
                FailedStudents = preview.WarningStudents.Count(x => string.Equals(x.AcademicStatus, "Fail", StringComparison.OrdinalIgnoreCase)),
                Messages = new List<string> { "Year-end finalization completed and locked." },
                Prerequisites = preview.Prerequisites
            };
        }

        public async Task<YearEndSummaryResult?> GetYearEndSummaryAsync(string studentId, string academicYear)
        {
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(academicYear))
                return null;

            var normalizedAcademicYear = NormalizeAcademicYear(academicYear);
            var transcript = await _transcriptRepo.FirstOrDefaultAsync(t =>
                !t.IsDeleted &&
                t.StudentId == studentId &&
                t.AcademicYear == normalizedAcademicYear &&
                t.IsYearEndFinalized);

            if (transcript == null)
                return null;

            return new YearEndSummaryResult
            {
                StudentId = studentId,
                AcademicYear = normalizedAcademicYear,
                YearEndGpa4 = transcript.YearEndGpa4 ?? transcript.GPA,
                YearEndGpa10 = transcript.YearEndGpa10 ?? 0m,
                AcademicStatus = transcript.AcademicStatus ?? "Pass",
                TotalCreditsEarned = transcript.YearEndTotalCreditsEarned ?? transcript.TotalCredits,
                IsLocked = transcript.IsYearEndLocked,
                FinalizedAtUtc = transcript.YearEndFinalizedAt
            };
        }

        public async Task<YearEndPreviewResult> PreviewYearEndAsync(string academicYear, string? facultyId)
        {
            if (string.IsNullOrWhiteSpace(academicYear))
                throw new Exception("Academic year is required.");

            var normalizedAcademicYear = NormalizeAcademicYear(academicYear);
            var settings = await _settingsService.GetAsync();

            var failThreshold = settings.FailGpaThreshold > 0 ? settings.FailGpaThreshold : 1.0m;
            var warningThreshold = settings.WarningGpaThreshold > 0 ? settings.WarningGpaThreshold : 2.0m;
            if (warningThreshold < failThreshold)
                warningThreshold = failThreshold;
            var academicYearRange = ResolveAcademicYearRange(normalizedAcademicYear);

            var facultyName = await ResolveFacultyNameAsync(facultyId);
            var targetScope = GetYearEndScope(normalizedAcademicYear, facultyId);

            var allStudents = await _studentRepo.GetAllAsync(s => !s.IsDeleted);
            var allClasses = await _classRepo.GetAllAsync(c => !c.IsDeleted);
            var allFaculties = await _facultyRepo.GetAllAsync(f => !f.IsDeleted);

            var classById = allClasses.ToDictionary(c => c.Id, c => c, StringComparer.Ordinal);
            var facultyById = allFaculties.ToDictionary(f => f.Id, f => f, StringComparer.Ordinal);

            var scopedStudents = allStudents
                .Where(student => string.IsNullOrWhiteSpace(facultyId) || StudentBelongsToFaculty(student, facultyId, classById))
                .ToList();
            var scopedStudentIds = scopedStudents.Select(s => s.Id).Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

            var enrollmentSpec = new Specification<Enrollment>(e => !e.IsDeleted && scopedStudentIds.Contains(e.StudentId))
                .Include(e => e.Course!);
            var scopedEnrollments = await _enrollmentRepo.ListAsync(enrollmentSpec);

            var yearEnrollments = scopedEnrollments
                .Where(e => SemesterBelongsToAcademicYear(e.Semester, normalizedAcademicYear))
                .ToList();

            var courseIdsInYear = yearEnrollments
                .Select(e => e.CourseId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            var studentsWithYearData = scopedStudents
                .Where(s => yearEnrollments.Any(e => string.Equals(e.StudentId, s.Id, StringComparison.Ordinal)))
                .ToList();

            var previewRows = new List<YearEndStudentSummaryVm>();
            foreach (var student in studentsWithYearData)
            {
                var yearlyRows = yearEnrollments
                    .Where(e => string.Equals(e.StudentId, student.Id, StringComparison.Ordinal))
                    .ToList();

                var gradedRows = yearlyRows
                    .Where(e => e.GradePoint.HasValue && e.FinalScore.HasValue && (e.Course?.Credits ?? 0) > 0)
                    .ToList();

                var gradedCredits = gradedRows.Sum(e => e.Course?.Credits ?? 0);
                var weightedGpa4Sum = gradedRows.Sum(e => (e.GradePoint ?? 0m) * (e.Course?.Credits ?? 0));
                var weightedGpa10Sum = gradedRows.Sum(e => (e.FinalScore ?? 0m) * (e.Course?.Credits ?? 0));

                var yearEndGpa4 = gradedCredits > 0 ? Math.Round(weightedGpa4Sum / gradedCredits, 2) : 0m;
                var yearEndGpa10 = gradedCredits > 0 ? Math.Round(weightedGpa10Sum / gradedCredits, 2) : 0m;

                var hasOutstandingDebt = yearlyRows.Any(e =>
                    !e.FinalScore.HasValue ||
                    e.FinalScore.Value < 4m ||
                    string.Equals(e.Grade, "F", StringComparison.OrdinalIgnoreCase) ||
                    (e.GradePoint.HasValue && e.GradePoint.Value <= 0m));

                var totalCreditsEarned = scopedEnrollments
                    .Where(e => string.Equals(e.StudentId, student.Id, StringComparison.Ordinal))
                    .Where(IsEnrollmentPassed)
                    .Sum(e => e.Course?.Credits ?? 0);

                var status = ResolveAcademicStatus(
                    yearEndGpa4,
                    hasOutstandingDebt,
                    warningThreshold,
                    failThreshold,
                    settings.TreatOutstandingDebtAsFail);

                classById.TryGetValue(student.StudentClassId ?? "", out var studentClass);
                Faculty? faculty = null;
                if (!string.IsNullOrWhiteSpace(studentClass?.FacultyId))
                    facultyById.TryGetValue(studentClass.FacultyId, out faculty);

                previewRows.Add(new YearEndStudentSummaryVm
                {
                    StudentId = student.Id,
                    StudentName = student.Name,
                    ClassId = studentClass?.Id,
                    ClassName = studentClass?.Name ?? "(Unassigned)",
                    FacultyId = faculty?.Id,
                    FacultyName = faculty?.Name ?? "(Unassigned)",
                    YearEndGpa4 = yearEndGpa4,
                    YearEndGpa10 = yearEndGpa10,
                    TotalCreditsEarned = totalCreditsEarned,
                    HasOutstandingDebt = hasOutstandingDebt,
                    AcademicStatus = status
                });
            }

            previewRows = previewRows
                .OrderBy(r => r.FacultyName)
                .ThenBy(r => r.ClassName)
                .ThenBy(r => r.StudentName)
                .ToList();

            var allRelevantSchedules = courseIdsInYear.Any()
                ? await _examScheduleRepo.GetAllAsync(s => !s.IsDeleted && courseIdsInYear.Contains(s.CourseId))
                : new List<ExamSchedule>();

            allRelevantSchedules = allRelevantSchedules
                .Where(s => IsInAcademicYearRange(s.StartTime, academicYearRange) || IsInAcademicYearRange(s.EndTime, academicYearRange))
                .ToList();

            var incompleteExamSchedules = allRelevantSchedules.Count(s => s.EndTime > DateTime.UtcNow);

            var sessionSpec = new Specification<Session>(s =>
                    !s.IsDeleted &&
                    scopedStudentIds.Contains(s.UserId) &&
                    s.Status != SessionStatus.NotStarted)
                .Include("Test")
                .Include("StudentAnswers.Question");
            var scopedSessions = await _sessionRepo.ListAsync(sessionSpec);

            var yearSessions = scopedSessions
                .Where(s => !string.IsNullOrWhiteSpace(s.Test?.CourseId) &&
                            courseIdsInYear.Contains(s.Test.CourseId) &&
                            IsInAcademicYearRange(s.EndAt ?? s.StartAt, academicYearRange))
                .ToList();

            var pendingEssayCount = yearSessions.Count(IsSessionPendingEssayGrading);

            var unlockedSemesterTranscriptCount = 0;
            if (yearSessions.Count > 0)
            {
                var sessionIds = yearSessions.Select(s => s.Id).Distinct(StringComparer.Ordinal).ToList();
                var lockLogs = (await _sessionLogRepo.GetAllAsync(l =>
                        sessionIds.Contains(l.SessionId) &&
                        (l.ActionType == ActionGradeLocked || l.ActionType == ActionGradeUnlocked)))
                    .OrderByDescending(l => l.Timestamp)
                    .ToList();

                var latestLockMap = lockLogs
                    .GroupBy(l => l.SessionId, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().ActionType == ActionGradeLocked, StringComparer.Ordinal);

                unlockedSemesterTranscriptCount = sessionIds.Count(id => !latestLockMap.TryGetValue(id, out var isLocked) || !isLocked);
            }

            var prerequisites = new YearEndPrerequisiteVm
            {
                AllExamSchedulesCompleted = incompleteExamSchedules == 0,
                AllEssaysGraded = pendingEssayCount == 0,
                SemesterTranscriptsLocked = unlockedSemesterTranscriptCount == 0,
                IncompleteExamScheduleCount = incompleteExamSchedules,
                PendingEssayCount = pendingEssayCount,
                UnlockedSemesterTranscriptCount = unlockedSemesterTranscriptCount
            };

            if (!yearEnrollments.Any())
                prerequisites.MissingItems.Add("No enrollments found for the selected academic year and scope.");
            if (incompleteExamSchedules > 0)
                prerequisites.MissingItems.Add($"There are {incompleteExamSchedules} exam schedule(s) not completed.");
            if (pendingEssayCount > 0)
                prerequisites.MissingItems.Add($"There are {pendingEssayCount} pending essay grading session(s).");
            if (unlockedSemesterTranscriptCount > 0)
                prerequisites.MissingItems.Add($"There are {unlockedSemesterTranscriptCount} session(s) with unlocked semester transcript.");
            if (await IsScopeLockedAsync(targetScope))
                prerequisites.MissingItems.Add("Year-end transcript for this scope is already locked.");

            var warnings = previewRows
                .Where(r => string.Equals(r.AcademicStatus, "Warning", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.AcademicStatus, "Fail", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new YearEndPreviewResult
            {
                AcademicYear = normalizedAcademicYear,
                FacultyId = facultyId,
                FacultyName = facultyName,
                GeneratedAtUtc = DateTime.UtcNow,
                Students = previewRows,
                WarningStudents = warnings,
                TotalStudents = previewRows.Count,
                Prerequisites = prerequisites
            };
        }

        public async Task<Transcript> CalculateGPAAsync(string studentId)
        {
            var spec = new Specification<Enrollment>(e =>
                    e.StudentId == studentId &&
                    !e.IsDeleted &&
                    e.GradePoint.HasValue)
                .Include(e => e.Course!);
            var enrollments = await _enrollmentRepo.ListAsync(spec);

            if (!enrollments.Any()) return new Transcript { StudentId = studentId };

            int totalCredits = enrollments.Sum(e => e.Course?.Credits ?? 0);
            decimal weightedGradePoints = enrollments.Sum(e => (e.GradePoint ?? 0) * (e.Course?.Credits ?? 0));

            decimal gpa = totalCredits > 0 ? weightedGradePoints / totalCredits : 0;

            var transcript = await _transcriptRepo.FirstOrDefaultAsync(x => x.StudentId == studentId && string.IsNullOrEmpty(x.AcademicYear) && !x.IsDeleted)
                             ?? new Transcript { StudentId = studentId };

            transcript.GPA = Math.Round(gpa, 2);
            transcript.TotalCredits = totalCredits;
            transcript.CalculatedAt = DateTime.UtcNow;
            transcript.UpdatedAt = DateTime.UtcNow;

            await _transcriptRepo.UpsertAsync(x => x.StudentId == studentId && string.IsNullOrEmpty(x.AcademicYear) && !x.IsDeleted, transcript);
            return transcript;
        }

        public async Task<List<Transcript>> GetAllTranscriptsAsync()
        {
            var spec = new Specification<Transcript>(t => !t.IsDeleted && string.IsNullOrEmpty(t.AcademicYear))
                .Include(t => t.Student!);
            var transcripts = await _transcriptRepo.ListAsync(spec);
            return transcripts
                .OrderByDescending(t => t.GPA)
                .ToList();
        }

        public async Task<List<TranscriptAdminRowVm>> GetAdminTranscriptRowsAsync(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var transcriptSpec = new Specification<Transcript>(t => !t.IsDeleted && string.IsNullOrEmpty(t.AcademicYear))
                .Include(t => t.Student!);
            var transcripts = await _transcriptRepo.ListAsync(transcriptSpec);

            var students = (await _studentRepo.GetAllAsync(s => !s.IsDeleted))
                .Select(s => new { s.Id, s.StudentClassId })
                .ToList();

            var classes = await _classRepo.GetAllAsync(c => !c.IsDeleted);
            var faculties = await _facultyRepo.GetAllAsync(f => !f.IsDeleted);
            var enrollments = (await _enrollmentRepo.GetAllAsync(e => !e.IsDeleted))
                .Select(e => new { e.StudentId, e.Semester })
                .ToList();

            var studentClassMap = students.ToDictionary(x => x.Id, x => x.StudentClassId);
            var classMap = classes.ToDictionary(x => x.Id, x => x);
            var facultyMap = faculties.ToDictionary(x => x.Id, x => x);

            var semesterByStudent = enrollments
                .GroupBy(x => x.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Semester)
                          .Where(s => !string.IsNullOrWhiteSpace(s))
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .ToHashSet(StringComparer.OrdinalIgnoreCase));

            var rows = new List<TranscriptAdminRowVm>();
            foreach (var transcript in transcripts)
            {
                studentClassMap.TryGetValue(transcript.StudentId, out var sidClassId);

                StudentClass? studentClass = null;
                if (!string.IsNullOrWhiteSpace(sidClassId))
                    classMap.TryGetValue(sidClassId, out studentClass);

                Faculty? faculty = null;
                if (!string.IsNullOrWhiteSpace(studentClass?.FacultyId))
                    facultyMap.TryGetValue(studentClass.FacultyId, out faculty);

                if (!string.IsNullOrWhiteSpace(classId) &&
                    !string.Equals(sidClassId, classId, StringComparison.Ordinal))
                    continue;

                if (!string.IsNullOrWhiteSpace(facultyId) &&
                    !string.Equals(faculty?.Id, facultyId, StringComparison.Ordinal))
                    continue;

                if (!string.IsNullOrWhiteSpace(semester))
                {
                    if (!semesterByStudent.TryGetValue(transcript.StudentId, out var set) || !set.Contains(semester))
                        continue;
                }

                rows.Add(new TranscriptAdminRowVm
                {
                    StudentId = transcript.StudentId,
                    StudentName = transcript.Student?.Name ?? transcript.StudentId,
                    ClassId = sidClassId,
                    ClassName = studentClass?.Name ?? "(Unassigned)",
                    FacultyId = faculty?.Id,
                    FacultyName = faculty?.Name ?? "(Unassigned)",
                    GPA = transcript.GPA,
                    TotalCredits = transcript.TotalCredits,
                    CalculatedAt = transcript.CalculatedAt
                });
            }

            return rows
                .OrderBy(r => r.FacultyName)
                .ThenBy(r => r.ClassName)
                .ThenBy(r => r.StudentName)
                .ToList();
        }

        public async Task<List<string>> GetAvailableSemestersAsync()
        {
            return (await _enrollmentRepo.GetAllAsync(e => !e.IsDeleted && !string.IsNullOrWhiteSpace(e.Semester)))
                .Select(e => e.Semester)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public decimal CalculateWeightedFinalScore(decimal assignmentScore, decimal examScore, decimal assignmentWeightPercent, decimal examWeightPercent)
        {
            if (assignmentScore < 0 || assignmentScore > 10)
                throw new Exception("Assignment score must be between 0 and 10.");
            if (examScore < 0 || examScore > 10)
                throw new Exception("Exam score must be between 0 and 10.");

            var assignmentWeight = Math.Clamp(assignmentWeightPercent, 0m, 100m);
            var examWeight = Math.Clamp(examWeightPercent, 0m, 100m);
            var totalWeight = assignmentWeight + examWeight;
            if (totalWeight <= 0m)
                throw new Exception("Total weight must be greater than 0.");

            var finalScore = ((assignmentScore * assignmentWeight) + (examScore * examWeight)) / totalWeight;
            return Math.Round(Math.Clamp(finalScore, 0m, 10m), 2);
        }

        public async Task<bool> FinalizeCourseGradeAsync(string enrollmentId, decimal finalScore)
        {
            var spec = new Specification<Enrollment>(e => e.Id == enrollmentId)
                .Include(e => e.Course!);
            var enrollment = await _enrollmentRepo.FirstOrDefaultAsync(spec)
                ?? throw new Exception("Enrollment not found");

            if (finalScore < 0 || finalScore > 10)
                throw new Exception("Final score must be between 0 and 10.");

            await EnsureEnrollmentEditableAsync(enrollment);

            enrollment.FinalScore = Math.Round(finalScore, 2);
            
            // Map 10-point scale to Grade and 4.0 scale
            if (finalScore >= 8.5m) { enrollment.Grade = "A"; enrollment.GradePoint = 4.0m; }
            else if (finalScore >= 7.0m) { enrollment.Grade = "B"; enrollment.GradePoint = 3.0m; }
            else if (finalScore >= 5.5m) { enrollment.Grade = "C"; enrollment.GradePoint = 2.0m; }
            else if (finalScore >= 4.0m) { enrollment.Grade = "D"; enrollment.GradePoint = 1.0m; }
            else { enrollment.Grade = "F"; enrollment.GradePoint = 0.0m; }

            await _enrollmentRepo.UpdateAsync(enrollment);

            // Recalculate GPA for student
            await CalculateGPAAsync(enrollment.StudentId);
            
            return true;
        }

        public Task<bool> IsSchoolTranscriptLockedAsync() => IsScopeLockedAsync(SchoolScope);

        public async Task<bool> IsFacultyTranscriptLockedAsync(string facultyId)
        {
            if (string.IsNullOrWhiteSpace(facultyId))
                return false;
            return await IsScopeLockedAsync(GetFacultyScope(facultyId));
        }

        public async Task<Dictionary<string, bool>> GetFacultyTranscriptLockMapAsync()
        {
            var logs = (await _auditRepo.GetAllAsync(x =>
                    x.EntityName == TranscriptLockEntityName &&
                    x.EntityId.StartsWith("faculty:") &&
                    (x.Action == TranscriptLockActionLock || x.Action == TranscriptLockActionUnlock)))
                .OrderByDescending(x => x.At)
                .ThenByDescending(x => x.Id)
                .ToList();

            return logs
                .GroupBy(x => x.EntityId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key["faculty:".Length..],
                    g => g.First().Action == TranscriptLockActionLock,
                    StringComparer.OrdinalIgnoreCase);
        }

        public Task LockSchoolTranscriptAsync(string actor, string? note = null) =>
            SaveScopeLockAsync(SchoolScope, lockState: true, actor, note);

        public Task UnlockSchoolTranscriptAsync(string actor, string? note = null) =>
            SaveScopeLockAsync(SchoolScope, lockState: false, actor, note);

        public Task LockFacultyTranscriptAsync(string facultyId, string actor, string? note = null) =>
            SaveScopeLockAsync(GetFacultyScope(facultyId), lockState: true, actor, note);

        public Task UnlockFacultyTranscriptAsync(string facultyId, string actor, string? note = null) =>
            SaveScopeLockAsync(GetFacultyScope(facultyId), lockState: false, actor, note);

        public async Task<List<Enrollment>> GetStudentGradesAsync(string studentId)
        {
            var spec = new Specification<Enrollment>(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.Course!);
            var enrollments = await _enrollmentRepo.ListAsync(spec);
            return enrollments
                .OrderBy(e => e.Semester)
                .ToList();
        }

        public async Task<Transcript?> GetStudentTranscriptSummaryAsync(string studentId)
        {
            return await _transcriptRepo.FirstOrDefaultAsync(x => x.StudentId == studentId && !x.IsDeleted && string.IsNullOrEmpty(x.AcademicYear));
        }

        private async Task<(List<Enrollment> Grades, Transcript? Summary, string StudentName)> BuildStudentExportDataAsync(string studentId, string? semester)
        {
            var all = await GetStudentGradesAsync(studentId);
            var grades = string.IsNullOrWhiteSpace(semester)
                ? all
                : all.Where(x => string.Equals(x.Semester, semester, StringComparison.OrdinalIgnoreCase)).ToList();

            var summary = await GetStudentTranscriptSummaryAsync(studentId);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == studentId);
            return (grades, summary, user?.Name ?? studentId);
        }

        private async Task<string?> ResolveFacultyNameAsync(string? facultyId)
        {
            if (string.IsNullOrWhiteSpace(facultyId))
                return null;
            return (await _facultyRepo.FirstOrDefaultAsync(x => x.Id == facultyId))?.Name;
        }

        private async Task<string?> ResolveClassNameAsync(string? classId)
        {
            if (string.IsNullOrWhiteSpace(classId))
                return null;
            return (await _classRepo.FirstOrDefaultAsync(x => x.Id == classId))?.Name;
        }

        private static string NormalizeAcademicYear(string academicYear)
        {
            return (academicYear ?? string.Empty).Trim();
        }

        private static string GetYearEndScope(string academicYear, string? facultyId)
        {
            var normalizedYear = NormalizeAcademicYear(academicYear);
            if (string.IsNullOrWhiteSpace(facultyId))
                return $"year:{normalizedYear}:school";
            return $"year:{normalizedYear}:faculty:{facultyId.Trim()}";
        }

        private static bool StudentBelongsToFaculty(Student student, string facultyId, IReadOnlyDictionary<string, StudentClass> classById)
        {
            if (string.IsNullOrWhiteSpace(facultyId) || string.IsNullOrWhiteSpace(student.StudentClassId))
                return false;

            return classById.TryGetValue(student.StudentClassId, out var studentClass) &&
                   string.Equals(studentClass.FacultyId, facultyId, StringComparison.Ordinal);
        }

        private static bool SemesterBelongsToAcademicYear(string? semester, string academicYear)
        {
            if (string.IsNullOrWhiteSpace(semester) || string.IsNullOrWhiteSpace(academicYear))
                return false;

            var cleanSemester = semester.Trim();
            var cleanAcademicYear = NormalizeAcademicYear(academicYear);

            if (cleanSemester.Contains(cleanAcademicYear, StringComparison.OrdinalIgnoreCase))
                return true;

            var years = System.Text.RegularExpressions.Regex.Matches(cleanAcademicYear, "\\d{4}")
                .Select(m => m.Value)
                .ToList();

            if (years.Count >= 2)
            {
                var firstYear = years[0];
                var secondYear = years[1];
                if (cleanSemester.Contains(firstYear, StringComparison.OrdinalIgnoreCase) ||
                    cleanSemester.Contains(secondYear, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return cleanSemester.StartsWith(cleanAcademicYear, StringComparison.OrdinalIgnoreCase);
        }

        private static (DateTime StartUtc, DateTime EndUtc) ResolveAcademicYearRange(string academicYear)
        {
            var cleanAcademicYear = NormalizeAcademicYear(academicYear);
            var years = System.Text.RegularExpressions.Regex.Matches(cleanAcademicYear, "\\d{4}")
                .Select(m => int.TryParse(m.Value, out var year) ? year : 0)
                .Where(year => year > 0)
                .ToList();

            if (years.Count >= 2)
            {
                var first = years[0];
                var second = years[1];
                var start = new DateTime(first, 7, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = new DateTime(second, 7, 1, 0, 0, 0, DateTimeKind.Utc);
                if (end > start)
                    return (start, end);
            }

            if (years.Count == 1)
            {
                var year = years[0];
                var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return (start, start.AddYears(1));
            }

            var fallbackStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (fallbackStart, fallbackStart.AddYears(1));
        }

        private static bool IsInAcademicYearRange(DateTime dateUtc, (DateTime StartUtc, DateTime EndUtc) range)
        {
            return dateUtc >= range.StartUtc && dateUtc < range.EndUtc;
        }

        private static bool IsEnrollmentPassed(Enrollment enrollment)
        {
            if (enrollment.FinalScore.HasValue && enrollment.FinalScore.Value >= 4m)
                return true;

            if (enrollment.GradePoint.HasValue && enrollment.GradePoint.Value >= 1m)
                return true;

            return !string.Equals(enrollment.Grade, "F", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(enrollment.Grade);
        }

        private static string ResolveAcademicStatus(decimal gpa4, bool hasOutstandingDebt, decimal warningThreshold, decimal failThreshold, bool debtAsFail)
        {
            if (gpa4 < failThreshold || (debtAsFail && hasOutstandingDebt))
                return "Fail";

            if (gpa4 < warningThreshold)
                return "Warning";

            return "Pass";
        }

        private static bool IsSessionPendingEssayGrading(Session session)
        {
            if (session.StudentAnswers == null || session.StudentAnswers.Count == 0)
                return false;

            var hasEssay = session.StudentAnswers.Any(a => a.Question != null && a.Question.Type == QType.Essay);
            if (!hasEssay)
                return false;

            var hasUngradedEssay = session.StudentAnswers.Any(a => a.Question != null && a.Question.Type == QType.Essay && a.GradedAt == null);
            return hasUngradedEssay || session.Status != SessionStatus.Graded;
        }

        private static string GetFacultyScope(string facultyId) => $"faculty:{facultyId}";

        private async Task<bool> IsScopeLockedAsync(string scope)
        {
            var latest = (await _auditRepo.GetAllAsync(x =>
                    x.EntityName == TranscriptLockEntityName &&
                    x.EntityId == scope &&
                    (x.Action == TranscriptLockActionLock || x.Action == TranscriptLockActionUnlock)))
                .OrderByDescending(x => x.At)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            return latest?.Action == TranscriptLockActionLock;
        }

        private async Task SaveScopeLockAsync(string scope, bool lockState, string actor, string? note)
        {
            if (string.IsNullOrWhiteSpace(scope))
                throw new Exception("Lock scope is required.");

            var currentlyLocked = await IsScopeLockedAsync(scope);
            if (currentlyLocked == lockState)
                return;

            var payload = JsonSerializer.Serialize(new
            {
                Scope = scope,
                Note = note,
                At = DateTime.UtcNow
            });

            await _auditRepo.InsertAsync(new AuditEntry
            {
                At = DateTime.UtcNow,
                Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
                Action = lockState ? TranscriptLockActionLock : TranscriptLockActionUnlock,
                EntityName = TranscriptLockEntityName,
                EntityId = scope,
                After = payload
            });
        }

        private async Task EnsureEnrollmentEditableAsync(Enrollment enrollment)
        {
            if (await IsSchoolTranscriptLockedAsync())
                throw new Exception("Transcript is locked at school level.");

            var student = await _studentRepo.FirstOrDefaultAsync(s => s.Id == enrollment.StudentId);
            var classId = student?.StudentClassId;
            if (!string.IsNullOrWhiteSpace(classId))
            {
                var studentClass = await _classRepo.FirstOrDefaultAsync(c => c.Id == classId);
                var facultyId = studentClass?.FacultyId;

                if (!string.IsNullOrWhiteSpace(facultyId) && await IsFacultyTranscriptLockedAsync(facultyId))
                    throw new Exception("Transcript is locked at faculty level.");
            }

            var yearLockedTranscripts = await _transcriptRepo.GetAllAsync(t =>
                !t.IsDeleted &&
                t.StudentId == enrollment.StudentId &&
                t.IsYearEndFinalized &&
                t.IsYearEndLocked &&
                !string.IsNullOrWhiteSpace(t.AcademicYear));

            var matchingYearLock = yearLockedTranscripts.FirstOrDefault(t =>
                !string.IsNullOrWhiteSpace(t.AcademicYear) &&
                SemesterBelongsToAcademicYear(enrollment.Semester, t.AcademicYear!));

            if (matchingYearLock != null)
                throw new Exception($"Transcript is locked by finalized year-end for academic year {matchingYearLock.AcademicYear}.");
        }
    }
}
