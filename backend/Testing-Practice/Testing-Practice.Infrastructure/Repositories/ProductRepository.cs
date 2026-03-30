using Testing_Practice.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;
using Testing_Practice.Infrastructure.Persistence.Contexts;
using Testing_Practice.Domain.Enums;

namespace Testing_Practice.Infrastructure.Repositories;



public class ProductRepository : IProductRepository
{
    private readonly RecipesDbContext _context;
    public ProductRepository(RecipesDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id) => 
        await _context.Products.FindAsync(id);

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Product>> GetAllAsync(
        string? searchTerm, 
        ProductCategory? category, 
        CookingRequirement? cooking, 
        ProductFlags? flags, 
        string? sortBy)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()));

        if (category.HasValue) query = query.Where(p => p.Category == category);
        if (cooking.HasValue) query = query.Where(p => p.CookingRequirement == cooking);
        if (flags.HasValue) query = query.Where(p => p.Flags.HasFlag(flags.Value));

        query = sortBy?.ToLower() switch
        {
            "calories" => query.OrderBy(p => p.Calories),
            "proteins" => query.OrderBy(p => p.Proteins),
            "fats" => query.OrderBy(p => p.Fats),
            "carbohydrates" => query.OrderBy(p => p.Carbohydrates),
            _ => query.OrderBy(p => p.Name)
        };

        return await query.ToListAsync();
    }

    public async Task<bool> IsUsedInAnyDishAsync(Guid productId) =>
        await _context.DishIngredients.AnyAsync(di => di.ProductId == productId);

    public async Task<IEnumerable<string>> GetDishNamesUsingProductAsync(Guid productId) =>
        await _context.DishIngredients
            .Where(di => di.ProductId == productId)
            .Select(di => di.Dish.Name)
            .Distinct()
            .ToListAsync();
}