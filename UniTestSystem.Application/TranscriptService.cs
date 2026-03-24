using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace UniTestSystem.Application
{
    public class TranscriptService : ITranscriptService
    {
        private readonly IRepository<Enrollment> _enrollmentRepo;
        private readonly IRepository<Transcript> _transcriptRepo;
        private readonly IRepository<Course> _courseRepo;

        public TranscriptService(
            IRepository<Enrollment> enrollmentRepo,
            IRepository<Transcript> transcriptRepo,
            IRepository<Course> courseRepo)
        {
            _enrollmentRepo = enrollmentRepo;
            _transcriptRepo = transcriptRepo;
            _courseRepo = courseRepo;
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

        public async Task<bool> FinalizeCourseGradeAsync(string enrollmentId, decimal finalScore)
        {
            var enrollment = await _enrollmentRepo.Query()
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId)
                ?? throw new Exception("Enrollment not found");

            enrollment.FinalScore = finalScore;
            
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
    }
}
