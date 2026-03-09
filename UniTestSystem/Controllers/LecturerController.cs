using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace UniTestSystem.Controllers
{
    [Authorize(Roles = "Lecturer,Admin")]
    public class LecturerController : Controller
    {
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<Test> _testRepo;

        public LecturerController(IRepository<Question> questionRepo, IRepository<Test> testRepo)
        {
            _questionRepo = questionRepo;
            _testRepo = testRepo;
        }

        public async Task<IActionResult> Dashboard()
        {
            var questions = await _questionRepo.GetAllAsync();
            var tests = await _testRepo.GetAllAsync();

            ViewBag.TotalQuestions = questions.Count;
            ViewBag.TotalTests = tests.Count;
            ViewBag.PendingQuestions = questions.Count(q => q.Status == QuestionStatus.Pending);

            return View();
        }
    }
}
