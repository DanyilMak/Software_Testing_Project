using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizPlatform.Api.Data;
using QuizPlatform.Api.Models;
using QuizPlatform.Api.Services;

namespace QuizPlatform.Api.Controllers;

[ApiController]
[Route("api")]
public class QuizController : ControllerBase
{
    private readonly QuizDbContext _context;
    private readonly QuizService _quizService;

    public QuizController(QuizDbContext context, QuizService quizService)
    {
        _context = context;
        _quizService = quizService;
    }

    [HttpGet("quizzes")]
    public async Task<IActionResult> GetPublishedQuizzes() =>
        Ok(await _context.Quizzes.Where(q => q.IsPublished).ToListAsync());

    [HttpPost("quizzes")]
    public async Task<IActionResult> CreateQuiz([FromBody] Quiz quiz)
    {
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, quiz);
    }

    [HttpGet("quizzes/{id}")]
    public async Task<IActionResult> GetQuiz(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null) return NotFound();

        // Приховуємо правильні відповіді перед відправкою клієнту
        foreach (var answer in quiz.Questions.SelectMany(q => q.Answers))
        {
            answer.IsCorrect = false; 
        }

        return Ok(quiz);
    }

    [HttpPost("quizzes/{id}/questions")]
    public async Task<IActionResult> AddQuestion(int id, [FromBody] Question question)
    {
        try
        {
            await _quizService.ValidateAndAddQuestionAsync(id, question);
            return Ok(question);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("quizzes/{id}/start")]
    public async Task<IActionResult> StartAttempt(int id, [FromQuery] string userName)
    {
        try
        {
            var attempt = await _quizService.StartAttemptAsync(id, userName);
            return Ok(attempt);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("attempts/{id}/submit")]
    public async Task<IActionResult> SubmitAttempt(int id, [FromBody] List<int> selectedAnswerIds)
    {
        try
        {
            var result = await _quizService.SubmitAttemptAsync(id, selectedAnswerIds);
            return Ok(result);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("attempts/{id}/results")]
    public async Task<IActionResult> GetResults(int id)
    {
        var attempt = await _context.Attempts.FindAsync(id);
        if (attempt == null) return NotFound();
        return Ok(attempt);
    }

    [HttpGet("quizzes/{id}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(int id)
    {
        var leaderboard = await _context.Attempts
            .Where(a => a.QuizId == id && a.FinishedAt != null)
            .OrderByDescending(a => a.Score)
            .Take(10)
            .ToListAsync();
        return Ok(leaderboard);
    }
}