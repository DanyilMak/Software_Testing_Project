namespace QuizPlatform.Api.Services;

public interface IEmailService
{
    Task SendResultAsync(string userName, double score);
}