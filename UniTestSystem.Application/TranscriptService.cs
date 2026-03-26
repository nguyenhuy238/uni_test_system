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

        private readonly IRepository<Enrollment> _enrollmentRepo;
        private readonly IRepository<Transcript> _transcriptRepo;
        private readonly IRepository<Course> _courseRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<StudentClass> _classRepo;
        private readonly IRepository<Faculty> _facultyRepo;
        private readonly IRepository<AuditEntry> _auditRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IExportService _exportService;
        private readonly ISettingsService _settingsService;

        public TranscriptService(
            IRepository<Enrollment> enrollmentRepo,
            IRepository<Transcript> transcriptRepo,
            IRepository<Course> courseRepo,
            IRepository<Student> studentRepo,
            IRepository<StudentClass> classRepo,
            IRepository<Faculty> facultyRepo,
            IRepository<AuditEntry> auditRepo,
            IRepository<User> userRepo,
            IExportService exportService,
            ISettingsService settingsService)
        {
            _enrollmentRepo = enrollmentRepo;
            _transcriptRepo = transcriptRepo;
            _courseRepo = courseRepo;
            _studentRepo = studentRepo;
            _classRepo = classRepo;
            _facultyRepo = facultyRepo;
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

            var transcript = await _transcriptRepo.FirstOrDefaultAsync(x => x.StudentId == studentId) 
                             ?? new Transcript { StudentId = studentId };

            transcript.GPA = Math.Round(gpa, 2);
            transcript.TotalCredits = totalCredits;
            transcript.CalculatedAt = DateTime.UtcNow;
            transcript.UpdatedAt = DateTime.UtcNow;

            await _transcriptRepo.UpsertAsync(x => x.Id == transcript.Id, transcript);
            return transcript;
        }

        public async Task<List<Transcript>> GetAllTranscriptsAsync()
        {
            var spec = new Specification<Transcript>(t => !t.IsDeleted)
                .Include(t => t.Student!);
            var transcripts = await _transcriptRepo.ListAsync(spec);
            return transcripts
                .OrderByDescending(t => t.GPA)
                .ToList();
        }

        public async Task<List<TranscriptAdminRowVm>> GetAdminTranscriptRowsAsync(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var transcriptSpec = new Specification<Transcript>(t => !t.IsDeleted)
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

            await EnsureTranscriptEditableAsync(enrollment.StudentId);

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
            return await _transcriptRepo.FirstOrDefaultAsync(x => x.StudentId == studentId && !x.IsDeleted);
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

        private async Task EnsureTranscriptEditableAsync(string studentId)
        {
            if (await IsSchoolTranscriptLockedAsync())
                throw new Exception("Transcript is locked at school level.");

            var student = await _studentRepo.FirstOrDefaultAsync(s => s.Id == studentId);
            var classId = student?.StudentClassId;

            if (string.IsNullOrWhiteSpace(classId))
                return;

            var studentClass = await _classRepo.FirstOrDefaultAsync(c => c.Id == classId);
            var facultyId = studentClass?.FacultyId;

            if (!string.IsNullOrWhiteSpace(facultyId) && await IsFacultyTranscriptLockedAsync(facultyId))
                throw new Exception("Transcript is locked at faculty level.");
        }
    }
}
