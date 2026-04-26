using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testing_Practice.Tests.IntegrationTests.Utils;

namespace Testing_Practice.Tests.IntegrationTests.Fixtures;

// Fixtures/TestWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
/*
using DishesApi.Data;          // ваш DbContext
using DishesApi.Services;
using DishesApi.Repositories;
*/

using Testing_Practice.Infrastructure.Persistence.Contexts;
using Testing_Practice.Infrastructure.Repositories;
using Testing_Practice.Application.Services.Implementations;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Удаляем регистрацию реального DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RecipesDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Добавляем InMemory базу
            services.AddDbContext<RecipesDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            // Перерегистрируем репозитории и сервисы (если нужно, оставляем реальные реализации)
            // Примечание: реальный IDishService использует репозиторий и логику пересчёта КБЖУ.
            // Мы используем те же реализации, но с InMemory БД.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RecipesDbContext>();
            db.Database.EnsureCreated();
            
            // Заполняем тестовые данные (продукты, категории)
            SeedTestData(db);
        });
    }

    private void SeedTestData(RecipesDbContext db)
    {
        // Добавляем продукты, необходимые для расчёта КБЖУ
        db.Products.AddRange(TestDataBuilder.GetDefaultProducts());
        db.SaveChanges();
    }
}