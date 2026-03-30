using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;

namespace Testing_Practice.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Product>> GetAllAsync(
        string? searchTerm,
        ProductCategory? category, 
        CookingRequirement? cooking, 
        ProductFlags? flags,
        string? sortBy);
    Task<bool> IsUsedInAnyDishAsync(Guid productId);
    Task<IEnumerable<string>> GetDishNamesUsingProductAsync(Guid productId);
}