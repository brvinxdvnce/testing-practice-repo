using Microsoft.EntityFrameworkCore;
using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;
using Testing_Practice.Infrastructure.Persistence.Contexts;

namespace Testing_Practice.Infrastructure.Repositories;

public class DishRepository : IDishRepository
{
    private readonly RecipesDbContext _context;
    public DishRepository(RecipesDbContext context) => _context = context;

    public async Task<Dish?> GetByIdAsync(Guid id) =>
        await _context.Dishes
            .Include(d => d.Ingredients)
            .ThenInclude(di => di.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(Dish dish)
    {
        await _context.Dishes.AddAsync(dish);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Dish dish)
    {
        if (dish?.Photos?.Count > 5)
            throw new Exception("Превышен лимит количества фотографий");
        _context.Dishes.Update(dish);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var dish = await _context.Dishes.FindAsync(id);
        if (dish != null)
        {
            _context.Dishes.Remove(dish);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Dish>> GetAllAsync(
        string? searchTerm = null, 
        DishCategory? category = null, 
        ProductFlags? flags = null)
    {
        var query = _context.Dishes.Include(d => d.Ingredients).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(d => d.Name.ToLower().Contains(searchTerm.ToLower()));

        if (category.HasValue) query = query.Where(d => d.Category == category);
        if (flags.HasValue) query = query.Where(d => d.Flags.HasFlag(flags.Value));

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<Guid> productIds) =>
        await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
}