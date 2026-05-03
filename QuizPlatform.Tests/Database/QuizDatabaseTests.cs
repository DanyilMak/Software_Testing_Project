using Microsoft.EntityFrameworkCore;
using QuizPlatform.Api.Data;
using QuizPlatform.Api.Models;
using Shouldly;
using Testcontainers.PostgreSql;

namespace QuizPlatform.Tests.Database;

public class QuizDatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    private QuizDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<QuizDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        _context = new QuizDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task SaveQuiz_AssignsIdAndSavesCorrectly()
    {
        // Arrange
        var quiz = new Quiz { Title = "DB Quiz", PassScore = 70 };

        // Act
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        // Assert
        quiz.Id.ShouldBeGreaterThan(0);
        var savedQuiz = await _context.Quizzes.FindAsync(quiz.Id);
        savedQuiz.ShouldNotBeNull();
        savedQuiz.Title.ShouldBe("DB Quiz");
    }

    [Fact]
    public async Task CascadeDelete_WhenQuizDeleted_DeletesQuestionsAndAnswers()
    {
        // Arrange
        var quiz = new Quiz { Title = "To Delete" };
        var question = new Question { Text = "Q1", Quiz = quiz };
        var answer = new Answer { Text = "A1", Question = question };
        
        quiz.Questions.Add(question);
        question.Answers.Add(answer);
        
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        // Act
        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        // Assert
        var qCount = await _context.Questions.CountAsync(q => q.QuizId == quiz.Id);
        var aCount = await _context.Answers.CountAsync(a => a.QuestionId == question.Id);
        
        qCount.ShouldBe(0);
        aCount.ShouldBe(0);
    }

    [Fact]
    public async Task InsertQuestion_WithoutQuizId_ThrowsDbUpdateException()
    {
        // Arrange
        var question = new Question { Text = "Orphan", Type = QuestionType.SingleChoice };

        // Act
        _context.Questions.Add(question);
        
        // Assert
        await Should.ThrowAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveAttempt_StoresUtcDateCorrectly()
    {
        // Arrange
        var quiz = new Quiz { Title = "Q" };
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        var attempt = new Attempt 
        { 
            QuizId = quiz.Id, 
            UserName = "TimeUser", 
            StartedAt = DateTime.UtcNow
        };

        // Act
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Assert
        var savedAttempt = await _context.Attempts.FindAsync(attempt.Id);
        savedAttempt.ShouldNotBeNull();
        savedAttempt.StartedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Attempt_DefaultScoreIsZero()
    {
        // Arrange
        var quiz = new Quiz { Title = "Q" };
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        var attempt = new Attempt { QuizId = quiz.Id, UserName = "User1", StartedAt = DateTime.UtcNow };

        // Act
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Assert
        var savedAttempt = await _context.Attempts.FindAsync(attempt.Id);
        savedAttempt!.Score.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateQuiz_ModifiesRecordSuccessfully()
    {
        // Arrange
        var quiz = new Quiz { Title = "Old Title" };
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        // Act
        quiz.Title = "New Title";
        await _context.SaveChangesAsync();

        // Assert
        var updatedQuiz = await _context.Quizzes.FindAsync(quiz.Id);
        updatedQuiz!.Title.ShouldBe("New Title");
    }
}