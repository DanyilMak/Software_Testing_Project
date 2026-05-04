using Microsoft.EntityFrameworkCore;
using QuizPlatform.Api.Data;
using QuizPlatform.Api.Models;

namespace QuizPlatform.Api.Services;

public class QuizService
{
    private readonly QuizDbContext _context;
    private readonly IEmailService _emailService;

    public QuizService(QuizDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task ValidateAndAddQuestionAsync(int quizId, Question question)
    {
        // Кожне питання має містити щонайменше 2 відповіді
        if (question.Answers.Count < 2)
            throw new ArgumentException("Питання повинно мати мінімум 2 відповіді.");

        // SingleChoice повинен мати рівно 1 правильну відповідь
        if (question.Type == QuestionType.SingleChoice && question.Answers.Count(a => a.IsCorrect) != 1)
            throw new ArgumentException("Питання SingleChoice повинно мати рівно 1 правильну відповідь.");

        question.QuizId = quizId;
        _context.Questions.Add(question);
        await _context.SaveChangesAsync();
    }

    public async Task<Attempt> StartAttemptAsync(int quizId, string userName)
    {
        // Не можна розпочати нову спробу, якщо одна вже в процесі
        var activeAttempt = await _context.Attempts
            .FirstOrDefaultAsync(a => a.UserName == userName && a.QuizId == quizId && a.FinishedAt == null);

        if (activeAttempt != null)
            throw new InvalidOperationException("Ви вже маєте активну спробу для цієї вікторини.");

        var attempt = new Attempt
        {
            QuizId = quizId,
            UserName = userName,
            StartedAt = DateTime.UtcNow
        };

        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();
        return attempt;
    }

    public async Task<Attempt> SubmitAttemptAsync(int attemptId, List<int> selectedAnswerIds)
    {
        var attempt = await _context.Attempts.Include(a => a.Quiz).FirstOrDefaultAsync(a => a.Id == attemptId) 
            ?? throw new KeyNotFoundException("Спробу не знайдено.");

        if (attempt.FinishedAt != null)
            throw new InvalidOperationException("Ця спроба вже завершена.");

        // Перевірка часового ліміту
        var timeTaken = DateTime.UtcNow - attempt.StartedAt;
        if (timeTaken.TotalMinutes > attempt.Quiz.TimeLimit)
            throw new InvalidOperationException("Час на виконання вікторини вичерпано.");

        attempt.FinishedAt = DateTime.UtcNow;

        var quizQuestions = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.QuizId == attempt.QuizId)
            .ToListAsync();

        double totalPoints = quizQuestions.Sum(q => q.Points);
        double earnedPoints = 0;

        foreach (var question in quizQuestions)
        {
            var correctAnswers = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList();
            var userAnswersForQuestion = selectedAnswerIds.Intersect(question.Answers.Select(a => a.Id)).ToList();

            //якщо користувач обрав усі правильні відповіді і жодної неправильної
            if (correctAnswers.Count == userAnswersForQuestion.Count && correctAnswers.All(userAnswersForQuestion.Contains))
            {
                earnedPoints += question.Points;
            }
        }

        // Score = (набрані бали / загальна кількість балів) х 100
        attempt.Score = totalPoints > 0 ? (earnedPoints / totalPoints) * 100 : 0;
        attempt.IsPassed = attempt.Score >= attempt.Quiz.PassScore;

        await _context.SaveChangesAsync();
        await _emailService.SendResultAsync(attempt.UserName, attempt.Score);
        return attempt;
    }
}