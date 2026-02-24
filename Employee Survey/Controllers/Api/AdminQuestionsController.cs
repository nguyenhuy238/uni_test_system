using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Survey.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,HR,Manager")]
[Route("api/admin/questions")]
public class AdminQuestionsController : ControllerBase
{
    private readonly IRepository<Question> _questions;

    public AdminQuestionsController(IRepository<Question> questions)
    {
        _questions = questions;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _questions.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var q = await _questions.FirstOrDefaultAsync(x => x.Id == id);
        if (q == null) return NotFound();
        return Ok(q);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Question question)
    {
        await _questions.InsertAsync(question);
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Question question)
    {
        var existing = await _questions.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();

        question.Id = id;
        await _questions.UpsertAsync(x => x.Id == id, question);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _questions.DeleteAsync(x => x.Id == id);
        return NoContent();
    }
}
