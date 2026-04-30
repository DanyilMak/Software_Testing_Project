using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using QuizPlatform.Api.Services;

namespace QuizPlatform.Tests.Integration;

public class QuizApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Кажемо застосунку, що він працює в тестовому режимі
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Базу даних ми тепер налаштуємо в Program.cs,
            // тому тут лише підміняємо EmailService на мок (заглушку)
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);
            
            services.AddScoped(_ => Substitute.For<IEmailService>());
        });
    }
}