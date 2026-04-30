using System.Text.Json.Serialization;
namespace QuizPlatform.Api.Models;

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    [JsonIgnore]
    public Question? Question { get; set; }
}