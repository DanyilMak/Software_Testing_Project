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
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);
            
            services.AddScoped(_ => Substitute.For<IEmailService>());
        });
    }
}