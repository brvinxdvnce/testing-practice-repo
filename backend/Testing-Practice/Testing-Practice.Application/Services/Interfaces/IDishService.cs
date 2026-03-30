using Testing_Practice.Domain.Models;

namespace Testing_Practice.Application.Services.Interfaces;

public interface IDishService
{
    Task<Dish> CreateAsync(Dish dish, bool isCategoryExplicitlySet);
    Task UpdateAsync(Dish dish, bool isCategoryExplicitlySet);
    Task RecalculateDishPropertiesAsync(Dish dish);
}