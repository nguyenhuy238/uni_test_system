using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace UniTestSystem.Application
{
    public class GradingService : IGradingService
    {
        private readonly IRepository<Session> _sRepo;
        private readonly IRepository<StudentAnswer> _saRepo;
        private readonly IRepository<Test> _tRepo;

        public GradingService(IRepository<Session> sRepo, IRepository<StudentAnswer> saRepo, IRepository<Test> tRepo)
        {
            _sRepo = sRepo;
            _saRepo = saRepo;
            _tRepo = tRepo;
        }

        public async Task<List<Session>> GetPendingGradingSessionsAsync(string lecturerId)
        {
            // Fetch sessions that have essays and are not yet finalized.
            // In a real scenario, we'd filter by lecturer's assigned courses.
            return await _sRepo.Query()
                .Include(s => s.Test)
                .Include(s => s.User)
                .Where(s => (s.Status == SessionStatus.Submitted || s.Status == SessionStatus.AutoSubmitted || s.Status == SessionStatus.Graded))
                .Where(s => s.StudentAnswers.Any(sa => sa.Question.Type == QType.Essay))
                .OrderByDescending(s => s.EndAt)
                .ToListAsync();
        }

        public async Task<Session> GetSessionForGradingAsync(string sessionId)
        {
            return await _sRepo.Query()
                .Include(s => s.User)
                .Include(s => s.Test)
                    .ThenInclude(t => t.TestQuestions)
                .Include(s => s.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                .FirstOrDefaultAsync(s => s.Id == sessionId) 
                ?? throw new Exception("Session not found");
        }

        public async Task GradeEssayAsync(string sessionId, string questionId, decimal score, string? comment)
        {
            var sa = await _saRepo.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.QuestionId == questionId)
                ?? throw new Exception("Answer not found");

            sa.Score = score;
            sa.Comment = comment;
            sa.GradedAt = DateTime.UtcNow;

            await _saRepo.UpdateAsync(sa);
            
            // Re-calculate TotalScore
            await RecalculateTotalScoreAsync(sessionId);
        }

        private async Task RecalculateTotalScoreAsync(string sessionId)
        {
            var s = await _sRepo.Query()
                .Include(x => x.StudentAnswers)
                .FirstOrDefaultAsync(x => x.Id == sessionId);

            if (s != null)
            {
                // Manual score is the sum of scores of questions that were manually graded (Essays)
                // Note: In this system, sa.Score is already the points (not a 0..1 scale)
                s.ManualScore = s.StudentAnswers
                    .Where(x => x.Question.Type == QType.Essay && x.GradedAt != null)
                    .Sum(x => x.Score);
                
                s.TotalScore = s.AutoScore + s.ManualScore;
                if (s.MaxScore > 0)
                {
                    s.Percent = Math.Round((s.TotalScore / s.MaxScore) * 100, 2);
                }
                
                await _sRepo.UpdateAsync(s);
            }
        }

        public async Task FinalizeGradingAsync(string sessionId)
        {
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == sessionId) 
                ?? throw new Exception("Session not found");

            s.Status = SessionStatus.Graded;
            s.GradedAt = DateTime.UtcNow;

            await _sRepo.UpdateAsync(s);
        }
    }
}
