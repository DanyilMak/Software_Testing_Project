using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuizPlatform.Api.Controllers;
using QuizPlatform.Api.Data;
using QuizPlatform.Api.Models;
using QuizPlatform.Api.Services;
using Shouldly;

namespace QuizPlatform.Tests.Unit;

public class QuizControllerTests : IDisposable
{
    private readonly QuizDbContext _context;
    private readonly QuizController _sut;

    public QuizControllerTests()
    {
        // Arrange (загальний)
        var options = new DbContextOptionsBuilder<QuizDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new QuizDbContext(options);
        var emailMock = Substitute.For<IEmailService>();
        var service = new QuizService(_context, emailMock);
        
        _sut = new QuizController(_context, service);
    }

    [Fact]
    public async Task GetPublishedQuizzes_ReturnsOnlyPublishedQuizzes()
    {
        // Arrange
        _context.Quizzes.AddRange(
            new Quiz { Title = "Q1", IsPublished = true },
            new Quiz { Title = "Q2", IsPublished = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPublishedQuizzes() as OkObjectResult;

        // Assert
        result.ShouldNotBeNull();
        var quizzes = result.Value as List<Quiz>;
        quizzes.ShouldNotBeNull();
        quizzes.Count.ShouldBe(1);
        quizzes.First().Title.ShouldBe("Q1");
    }

    [Fact]
    public async Task GetQuiz_NonExistingId_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetQuiz(999);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetQuiz_ExistingId_HidesCorrectAnswersBeforeReturning()
    {
        // Arrange
        var quiz = new Quiz 
        { 
            Id = 1, 
            Questions = new List<Question>
            {
                new Question 
                { 
                    Answers = new List<Answer> 
                    { 
                        new Answer { IsCorrect = true }, // Справжня правильна відповідь
                        new Answer { IsCorrect = false }
                    } 
                }
            }
        };
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetQuiz(1) as OkObjectResult;

        // Assert
        result.ShouldNotBeNull();
        var returnedQuiz = result.Value as Quiz;
        var answers = returnedQuiz!.Questions.SelectMany(q => q.Answers).ToList();
        
        answers.Count.ShouldBe(2);
        answers.ShouldAllBe(a => a.IsCorrect == false); // Контролер мав приховати правильну відповідь!
    }

    [Fact]
    public async Task CreateQuiz_ValidQuiz_ReturnsCreatedAtAction()
    {
        // Arrange
        var quiz = new Quiz { Title = "New Quiz" };

        // Act
        var result = await _sut.CreateQuiz(quiz) as CreatedAtActionResult;

        // Assert
        result.ShouldNotBeNull();
        result.ActionName.ShouldBe(nameof(QuizController.GetQuiz));
        var createdQuiz = result.Value as Quiz;
        createdQuiz.ShouldNotBeNull();
        createdQuiz.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetResults_ExistingAttempt_ReturnsOk()
    {
        // Arrange
        var attempt = new Attempt { Id = 1, UserName = "Player1" };
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetResults(1) as OkObjectResult;

        // Assert
        result.ShouldNotBeNull();
        var returnedAttempt = result.Value as Attempt;
        returnedAttempt!.UserName.ShouldBe("Player1");
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsTop10AttemptsOrderedByScoreDesc()
    {
        // Arrange
        var attempts = new List<Attempt>
        {
            new Attempt { QuizId = 1, Score = 50, FinishedAt = DateTime.UtcNow },
            new Attempt { QuizId = 1, Score = 100, FinishedAt = DateTime.UtcNow },
            new Attempt { QuizId = 1, Score = 75, FinishedAt = DateTime.UtcNow }
        };
        _context.Attempts.AddRange(attempts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetLeaderboard(1) as OkObjectResult;

        // Assert
        result.ShouldNotBeNull();
        var leaderboard = result.Value as List<Attempt>;
        leaderboard.ShouldNotBeNull();
        leaderboard.Count.ShouldBe(3);
        leaderboard[0].Score.ShouldBe(100); // Найкращий бал має бути першим
        leaderboard[1].Score.ShouldBe(75);
        leaderboard[2].Score.ShouldBe(50);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}