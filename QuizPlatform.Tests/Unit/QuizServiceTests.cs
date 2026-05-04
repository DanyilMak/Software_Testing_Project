using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuizPlatform.Api.Data;
using QuizPlatform.Api.Models;
using QuizPlatform.Api.Services;
using Shouldly;

namespace QuizPlatform.Tests.Unit;

public class QuizServiceTests : IDisposable
{
    private readonly QuizDbContext _context;
    private readonly IEmailService _emailServiceMock;
    private readonly QuizService _sut;

    public QuizServiceTests()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<QuizDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new QuizDbContext(options);
        _emailServiceMock = Substitute.For<IEmailService>();
        _sut = new QuizService(_context, _emailServiceMock);
    }

    [Fact]
    public async Task ValidateAndAddQuestionAsync_LessThanTwoAnswers_ThrowsArgumentException()
    {
        // Arrange
        var question = new Question
        {
            Type = QuestionType.MultipleChoice,
            Answers = new List<Answer> { new Answer { IsCorrect = true } }
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() => 
            _sut.ValidateAndAddQuestionAsync(1, question));
            
        exception.Message.ShouldBe("Питання повинно мати мінімум 2 відповіді.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ValidateAndAddQuestionAsync_SingleChoiceInvalidCorrectAnswers_ThrowsArgumentException(int correctCount)
    {
        // Arrange
        var answers = new List<Answer>
        {
            new Answer { IsCorrect = correctCount > 0 },
            new Answer { IsCorrect = correctCount > 1 }
        };

        var question = new Question
        {
            Type = QuestionType.SingleChoice,
            Answers = answers
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() => 
            _sut.ValidateAndAddQuestionAsync(1, question));

        exception.Message.ShouldBe("Питання SingleChoice повинно мати рівно 1 правильну відповідь.");
    }

    [Fact]
    public async Task ValidateAndAddQuestionAsync_ValidData_SavesToDatabase()
    {
        // Arrange
        var question = new Question
        {
            Type = QuestionType.SingleChoice,
            Answers = new List<Answer>
            {
                new Answer { IsCorrect = true },
                new Answer { IsCorrect = false }
            }
        };

        // Act
        await _sut.ValidateAndAddQuestionAsync(99, question);

        // Assert
        var savedQuestion = await _context.Questions.FirstOrDefaultAsync(q => q.QuizId == 99);
        savedQuestion.ShouldNotBeNull();
        savedQuestion.Answers.Count.ShouldBe(2);
    }

    [Fact]
    public async Task StartAttemptAsync_UserAlreadyHasActiveAttempt_ThrowsInvalidOperationException()
    {
        // Arrange
        _context.Attempts.Add(new Attempt { QuizId = 1, UserName = "testUser", FinishedAt = null });
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => 
            _sut.StartAttemptAsync(1, "testUser"));

        exception.Message.ShouldBe("Ви вже маєте активну спробу для цієї вікторини.");
    }

    [Fact]
    public async Task StartAttemptAsync_ValidRequest_CreatesNewAttempt()
    {
        // Act
        var result = await _sut.StartAttemptAsync(1, "newUser");

        // Assert
        result.ShouldNotBeNull();
        result.UserName.ShouldBe("newUser");
        result.FinishedAt.ShouldBeNull();
        
        var savedAttempt = await _context.Attempts.FindAsync(result.Id);
        savedAttempt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SubmitAttemptAsync_AttemptAlreadyFinished_ThrowsInvalidOperationException()
    {
        // Arrange
        var attempt = new Attempt { Id = 1, FinishedAt = DateTime.UtcNow, Quiz = new Quiz() };
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => 
            _sut.SubmitAttemptAsync(1, new List<int>()));

        exception.Message.ShouldBe("Ця спроба вже завершена.");
    }

    [Fact]
    public async Task SubmitAttemptAsync_TimeLimitExceeded_ThrowsInvalidOperationException()
    {
        // Arrange
        var quiz = new Quiz { Id = 1, TimeLimit = 10 };
        var attempt = new Attempt 
        { 
            Id = 1, 
            QuizId = 1, 
            StartedAt = DateTime.UtcNow.AddMinutes(-15),
            Quiz = quiz 
        };
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => 
            _sut.SubmitAttemptAsync(1, new List<int>()));

        exception.Message.ShouldBe("Час на виконання вікторини вичерпано.");
    }

    [Fact]
    public async Task SubmitAttemptAsync_ValidSubmission_CalculatesScoreAndSendsEmail()
    {
        // Arrange
        var quiz = new Quiz { Id = 1, TimeLimit = 30, PassScore = 50 };
        var question = new Question 
        { 
            Id = 1, QuizId = 1, Points = 10, 
            Answers = new List<Answer> 
            { 
                new Answer { Id = 1, IsCorrect = true },
                new Answer { Id = 2, IsCorrect = false }
            } 
        };
        var attempt = new Attempt { Id = 1, QuizId = 1, UserName = "proGamer", StartedAt = DateTime.UtcNow, Quiz = quiz };
        
        _context.Quizzes.Add(quiz);
        _context.Questions.Add(question);
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.SubmitAttemptAsync(1, new List<int> { 1 });

        // Assert
        result.Score.ShouldBe(100);
        result.IsPassed.ShouldBeTrue();
        result.FinishedAt.ShouldNotBeNull();
        
        await _emailServiceMock.Received(1).SendResultAsync("proGamer", 100);
    }

    [Fact]
    public async Task SubmitAttemptAsync_InvalidAttemptId_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(() => 
            _sut.SubmitAttemptAsync(999, new List<int>()));

        exception.Message.ShouldBe("Спробу не знайдено.");
    }

    [Fact]
    public async Task SubmitAttemptAsync_ScoreBelowPassScore_SetsIsPassedToFalse()
    {
        // Arrange
        var quiz = new Quiz { Id = 2, TimeLimit = 30, PassScore = 80 }; // Прохідний бал 80%
        var question = new Question 
        { 
            Id = 2, QuizId = 2, Points = 10, 
            Answers = new List<Answer> 
            { 
                new Answer { Id = 3, IsCorrect = true },
                new Answer { Id = 4, IsCorrect = false }
            } 
        };
        var attempt = new Attempt { Id = 2, QuizId = 2, UserName = "noobGamer", StartedAt = DateTime.UtcNow, Quiz = quiz };
        
        _context.Quizzes.Add(quiz);
        _context.Questions.Add(question);
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.SubmitAttemptAsync(2, new List<int> { 4 });

        // Assert
        result.Score.ShouldBe(0);
        result.IsPassed.ShouldBeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}