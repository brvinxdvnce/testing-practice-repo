using Testing_Practice.Application.Services.Interfaces;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;

namespace Testing_Practice.Application.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await _productRepository.AddAsync(product);
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        product.UpdateModifiedDate();
        await _productRepository.UpdateAsync(product);
    }

    public Task DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(Guid id)
    {
        // Реализация требования 1.5: Проверка перед удалением
        bool isUsed = await _productRepository.IsUsedInAnyDishAsync(id);
        
        if (isUsed)
        {
            var usedInDishes = await _productRepository.GetDishNamesUsingProductAsync(id);
            string dishNames = string.Join(", ", usedInDishes);
            
            // Выбрасываем исключение, которое потом перехватит контроллер и вернет пользователю красивую ошибку (например, HTTP 409 Conflict)
            throw new InvalidOperationException(
                $"Невозможно удалить продукт, так как он используется в следующих блюдах: {dishNames}");
        }

        await _productRepository.DeleteAsync(id);
    }
}