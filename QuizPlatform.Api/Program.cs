using Microsoft.EntityFrameworkCore;
using QuizPlatform.Api.Data;
using QuizPlatform.Api.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.EnvironmentName == "Testing")
{
    // Якщо це інтеграційні тести - використовуємо швидку базу в пам'яті
    builder.Services.AddDbContext<QuizDbContext>(options =>
        options.UseInMemoryDatabase("IntegrationDb"));
}
else
{
    // Якщо це реальний запуск - використовуємо PostgreSQL
    builder.Services.AddDbContext<QuizDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<QuizService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<QuizDbContext>();

    if (app.Environment.EnvironmentName == "Testing")
    {
        context.Database.EnsureCreated(); // Для бази в пам'яті просто створюємо схему
    }
    else
    {
        context.Database.Migrate(); // Для PostgreSQL застосовуємо міграції
    }
    
    // Наповнюємо базу даними (10 000+ записів)
    await DataSeeder.SeedDataAsync(context); 
}

app.Run();

// Робимо клас доступним для тестового проєкту
public partial class Program { }