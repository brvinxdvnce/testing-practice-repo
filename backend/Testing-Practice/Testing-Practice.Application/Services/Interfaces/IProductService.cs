using Testing_Practice.Domain.Models;

namespace Testing_Practice.Application.Services.Interfaces;

public interface IProductService
{
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
}