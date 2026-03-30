using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;

namespace Testing_Practice.Domain.Repositories;

public interface IDishRepository
{
    Task<Dish?> GetByIdAsync(Guid id);
    Task AddAsync(Dish dish);
    Task UpdateAsync(Dish dish);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Dish>> GetAllAsync(
        string? searchTerm = null, 
        DishCategory? category = null, 
        ProductFlags? flags = null);
    Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<Guid> productIds);
}