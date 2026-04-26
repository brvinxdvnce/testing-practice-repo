using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.DTOs;

namespace Testing_Practice.Tests.IntegrationTests.Utils;

using Testing_Practice.Infrastructure.Persistence.Contexts;
using Testing_Practice.Infrastructure.Repositories;
using Testing_Practice.Application.Services.Implementations;

public static class TestDataBuilder
{
    public static List<Product> GetDefaultProducts() => new()
    {
        new Product
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Куриное филе",
            Calories = 165,      // на 100 г продукта
            Proteins = 31,
            Fats = 3.6,
            Carbohydrates = 0,
            Category = ProductCategory.Meat,
            CookingRequirement = CookingRequirement.RequiresCooking,
            Flags = ProductFlags.None
        },
        new Product
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Рис",
            Calories = 130,
            Proteins = 2.7,
            Fats = 0.3,
            Carbohydrates = 28,
            Category = ProductCategory.Cereals,
            CookingRequirement = CookingRequirement.RequiresCooking,
            Flags = ProductFlags.None
        },
    };

    public static DishCreateDto CreateValidDishDto(
        string name = "Тестовое блюдо",
        List<string>? photos = null,
        double portionSize = 200,
        DishCategory? category = DishCategory.First,
        List<IngredientDto>? ingredients = null,
        ProductFlags? flags = null,
        double? calories = null,
        double? proteins = null,
        double? fats = null,
        double? carbohydrates = null)
    {
        photos ??= new List<string> { "photo1.jpg" };
        ingredients ??= new List<IngredientDto>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 150),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), 50)
        };

        return new DishCreateDto(
            Name: name,
            Photos: photos,
            PortionSize: portionSize,
            Category: category,
            Ingredients: ingredients,
            Flags: flags,
            Calories: calories,
            Proteins: proteins,
            Fats: fats,
            Carbohydrates: carbohydrates
        );
    }

    public static DishUpdateDto CreateValidUpdateDto() => new()
    {
        Name = "Обновлённое блюдо",
        PortionSize = 250,
        Category = DishCategory.Soup,
        Ingredients = new List<IngredientDto>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 200)
        },
        Photos = new List<string> { "new_photo.jpg" },
        Calories = 500,
        Proteins = 40,
        Fats = 20,
        Carbohydrates = 10,
        Flags = ProductFlags.Vegan
    };
}




/*
public static class TestDataBuilder
{
    public static List<Product> GetDefaultProducts() => new()
    {
        new Product { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Куриное филе", CaloriesPer100g = 165, ProteinsPer100g = 31, FatsPer100g = 3.6m, CarbohydratesPer100g = 0 },
        new Product { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Рис", CaloriesPer100g = 130, ProteinsPer100g = 2.7m, FatsPer100g = 0.3m, CarbohydratesPer100g = 28m },
    };

    public static DishCreateDto CreateValidDishDto(
        string name = "Тестовое блюдо",
        int portionSize = 200,
        DishCategory category = DishCategory.First,
        List<IngredientDto>? ingredients = null)
    {
        ingredients ??= new List<IngredientDto>
        {
            new() { ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Amount = 150 },
            new() { ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Amount = 50 }
        };
        return new DishCreateDto
        {
            Name = name,
            PortionSize = portionSize,
            Category = category,
            Ingredients = ingredients,
            Photos = new List<string> { "photo1.jpg" }
        };
    }

    public static DishUpdateDto CreateValidUpdateDto() => new()
    {
        Name = "Обновлённое блюдо",
        PortionSize = 250,
        Category = DishCategory.Soup,
        Ingredients = new List<IngredientDto>
        {
            new IngredientDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), 200 )
        },
        Photos = new List<string> { "new_photo.jpg" },
        Calories = 500,
        Proteins = 40,
        Fats = 20,
        Carbohydrates = 10,
        Flags = ProductFlags.Vegan
    };
}*/