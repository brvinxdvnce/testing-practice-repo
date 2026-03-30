using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;

namespace Testing_Practice.Application.Services.Interfaces;

public interface IDishService
{
    Task<Dish> CreateAsync(Dish dish, bool isCategoryExplicitlySet);
    Task UpdateAsync(Dish dish, bool isCategoryExplicitlySet);
    Task RecalculateDishPropertiesAsync(Dish dish);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Dish>> GetAllAsync(
        string? search,
        DishCategory? category,
        ProductFlags? flags,
        string? sortBy,
        bool sortDesc);
}