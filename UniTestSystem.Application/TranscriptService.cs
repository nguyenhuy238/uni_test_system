using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using Microsoft.EntityFrameworkCore;
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

        public TranscriptService(
            IRepository<Enrollment> enrollmentRepo,
            IRepository<Transcript> transcriptRepo,
            IRepository<Course> courseRepo,
            IRepository<Student> studentRepo,
            IRepository<StudentClass> classRepo,
            IRepository<Faculty> facultyRepo,
            IRepository<AuditEntry> auditRepo)
        {
            _enrollmentRepo = enrollmentRepo;
            _transcriptRepo = transcriptRepo;
            _courseRepo = courseRepo;
            _studentRepo = studentRepo;
            _classRepo = classRepo;
            _facultyRepo = facultyRepo;
            _auditRepo = auditRepo;
        }

        public async Task<Transcript> CalculateGPAAsync(string studentId)
        {
            var enrollments = await _enrollmentRepo.Query()
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId && !e.IsDeleted && e.GradePoint.HasValue)
                .ToListAsync();

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
            return await _transcriptRepo.Query()
                .Include(t => t.Student)
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.GPA)
                .ToListAsync();
        }

        public async Task<List<TranscriptAdminRowVm>> GetAdminTranscriptRowsAsync(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var transcripts = await _transcriptRepo.Query()
                .Include(t => t.Student)
                .Where(t => !t.IsDeleted)
                .ToListAsync();

            var students = await _studentRepo.Query()
                .Where(s => !s.IsDeleted)
                .Select(s => new { s.Id, s.StudentClassId })
                .ToListAsync();

            var classes = await _classRepo.Query()
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            var faculties = await _facultyRepo.Query()
                .Where(f => !f.IsDeleted)
                .ToListAsync();

            var enrollments = await _enrollmentRepo.Query()
                .Where(e => !e.IsDeleted)
                .Select(e => new { e.StudentId, e.Semester })
                .ToListAsync();

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
            return await _enrollmentRepo.Query()
                .Where(e => !e.IsDeleted && !string.IsNullOrWhiteSpace(e.Semester))
                .Select(e => e.Semester)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
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
            var enrollment = await _enrollmentRepo.Query()
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId)
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
            var logs = await _auditRepo.Query()
                .Where(x => x.EntityName == TranscriptLockEntityName &&
                            x.EntityId.StartsWith("faculty:") &&
                            (x.Action == TranscriptLockActionLock || x.Action == TranscriptLockActionUnlock))
                .OrderByDescending(x => x.At)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

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
            return await _enrollmentRepo.Query()
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId && !e.IsDeleted)
                .OrderBy(e => e.Semester)
                .ToListAsync();
        }

        public async Task<Transcript?> GetStudentTranscriptSummaryAsync(string studentId)
        {
            return await _transcriptRepo.Query()
                .FirstOrDefaultAsync(x => x.StudentId == studentId && !x.IsDeleted);
        }

        private static string GetFacultyScope(string facultyId) => $"faculty:{facultyId}";

        private async Task<bool> IsScopeLockedAsync(string scope)
        {
            var latest = await _auditRepo.Query()
                .Where(x => x.EntityName == TranscriptLockEntityName &&
                            x.EntityId == scope &&
                            (x.Action == TranscriptLockActionLock || x.Action == TranscriptLockActionUnlock))
                .OrderByDescending(x => x.At)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

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

            var classId = await _studentRepo.Query()
                .Where(s => s.Id == studentId)
                .Select(s => s.StudentClassId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(classId))
                return;

            var facultyId = await _classRepo.Query()
                .Where(c => c.Id == classId)
                .Select(c => c.FacultyId)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(facultyId) && await IsFacultyTranscriptLockedAsync(facultyId))
                throw new Exception("Transcript is locked at faculty level.");
        }
    }
}
