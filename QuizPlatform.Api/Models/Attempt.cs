using System.Text.Json.Serialization;
namespace QuizPlatform.Api.Models;

public class Attempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public double Score { get; set; }
    public bool IsPassed { get; set; }
    
    [JsonIgnore]
    public Quiz? Quiz { get; set; }
}