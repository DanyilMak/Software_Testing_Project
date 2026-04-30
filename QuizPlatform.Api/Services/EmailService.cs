namespace QuizPlatform.Api.Services;

public class EmailService : IEmailService
{
    public Task SendResultAsync(string userName, double score) => Task.CompletedTask;
}