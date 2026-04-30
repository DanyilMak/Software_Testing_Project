using System.Net;
using System.Net.Http.Json;
using QuizPlatform.Api.Models;
using Shouldly;

namespace QuizPlatform.Tests.Integration;

public class QuizEndpointsTests : IClassFixture<QuizApiFactory>
{
    private readonly HttpClient _client;

    public QuizEndpointsTests(QuizApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetQuizzes_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/quizzes");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.ToString().ShouldStartWith("application/json");
    }

    [Fact]
    public async Task CreateQuiz_ValidData_ReturnsCreated()
    {
        // Arrange
        var newQuiz = new Quiz { Title = "Integration Quiz", TimeLimit = 15, PassScore = 50, IsPublished = true };

        // Act
        var response = await _client.PostAsJsonAsync("/api/quizzes", newQuiz);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdQuiz = await response.Content.ReadFromJsonAsync<Quiz>();
        createdQuiz.ShouldNotBeNull();
        createdQuiz.Title.ShouldBe("Integration Quiz");
    }

    [Fact]
    public async Task GetQuiz_NonExisting_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/quizzes/9999");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddQuestion_InvalidData_ReturnsBadRequest()
    {
        // Arrange: Питання без відповідей (порушує бізнес-правило)
        var invalidQuestion = new Question { Type = QuestionType.SingleChoice, Answers = new List<Answer>() };

        // Act
        var response = await _client.PostAsJsonAsync("/api/quizzes/1/questions", invalidQuestion);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddQuestion_ValidData_ReturnsOk()
    {
        // Arrange
        var quizResponse = await _client.PostAsJsonAsync("/api/quizzes", new Quiz { Title = "Q" });
        var quiz = await quizResponse.Content.ReadFromJsonAsync<Quiz>();
        
        var validQuestion = new Question 
        { 
            Type = QuestionType.TrueFalse, 
            Answers = new List<Answer> 
            { 
                new Answer { Text = "True", IsCorrect = true }, 
                new Answer { Text = "False", IsCorrect = false } 
            } 
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/quizzes/{quiz!.Id}/questions", validQuestion);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartAttempt_NewUser_ReturnsOk()
    {
        // Arrange
        var quizRes = await _client.PostAsJsonAsync("/api/quizzes", new Quiz { Title = "Q" });
        var quiz = await quizRes.Content.ReadFromJsonAsync<Quiz>();

        // Act
        var response = await _client.PostAsync($"/api/quizzes/{quiz!.Id}/start?userName=testUser1", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var attempt = await response.Content.ReadFromJsonAsync<Attempt>();
        attempt!.UserName.ShouldBe("testUser1");
    }

    [Fact]
    public async Task GetResults_NonExistingAttempt_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/attempts/9999/results");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsOkAndJson()
    {
        // Act
        var response = await _client.GetAsync("/api/quizzes/1/leaderboard");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotBeNull();
    }
}