using AutoFixture;
using Microsoft.EntityFrameworkCore;
using QuizPlatform.Api.Models;
using System.Diagnostics.CodeAnalysis;

namespace QuizPlatform.Api.Data;

[ExcludeFromCodeCoverage]
public static class DataSeeder
{
    public static async Task SeedDataAsync(QuizDbContext context)
    {
        if (await context.Quizzes.AnyAsync()) return;

        var fixture = new Fixture();
        var random = new Random();

        var quizzes = new List<Quiz>();
        for (int i = 0; i < 100; i++)
        {
            var quiz = fixture.Build<Quiz>()
                .Without(q => q.Id)
                .Without(q => q.Questions)
                .With(q => q.CreatedAt, DateTime.UtcNow.AddDays(-random.Next(1, 100))) 
                .With(q => q.TimeLimit, random.Next(10, 60)) 
                .With(q => q.PassScore, random.Next(50, 80)) 
                .With(q => q.IsPublished, true)
                .Create();
            
            quizzes.Add(quiz);
        }
        context.Quizzes.AddRange(quizzes);
        await context.SaveChangesAsync();

        var questions = new List<Question>();
        var answers = new List<Answer>();

        foreach (var quiz in quizzes)
        {
            for (int i = 0; i < 10; i++)
            {
                var question = fixture.Build<Question>()
                    .Without(q => q.Id)
                    .Without(q => q.Quiz)
                    .Without(q => q.Answers)
                    .With(q => q.QuizId, quiz.Id)
                    .With(q => q.Type, QuestionType.SingleChoice) 
                    .With(q => q.Points, 10) 
                    .Create();
                
                questions.Add(question);
            }
        }
        context.Questions.AddRange(questions);
        await context.SaveChangesAsync();

        foreach (var question in questions)
        {
            for (int i = 0; i < 4; i++)
            {
                var answer = fixture.Build<Answer>()
                    .Without(a => a.Id)
                    .Without(a => a.Question)
                    .With(a => a.QuestionId, question.Id)
                    .With(a => a.IsCorrect, i == 0) 
                    .Create();
                
                answers.Add(answer);
            }
        }
        context.Answers.AddRange(answers);
        await context.SaveChangesAsync();

        // Генеруємо 5000 спроб (по 50 на кожну вікторину)
        var attempts = new List<Attempt>();
        foreach (var quiz in quizzes)
        {
            for (int i = 0; i < 50; i++)
            {
                var startedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));
                var score = random.Next(40, 100);

                var attempt = fixture.Build<Attempt>()
                    .Without(a => a.Id)
                    .Without(a => a.Quiz)
                    .With(a => a.QuizId, quiz.Id)
                    .With(a => a.StartedAt, startedAt)
                    .With(a => a.FinishedAt, startedAt.AddMinutes(quiz.TimeLimit - random.Next(1, 5)))
                    .With(a => a.Score, score)
                    .With(a => a.IsPassed, score >= quiz.PassScore)
                    .Create();
                
                attempts.Add(attempt);
            }
        }
        context.Attempts.AddRange(attempts);
        await context.SaveChangesAsync();
    }
}