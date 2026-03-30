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

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _productRepository.GetByIdAsync(id);
    }
    
    public async Task<Product> CreateAsync(Product product)
    {
        ValidateProduct(product);
        await _productRepository.AddAsync(product);
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        ValidateProduct(product);
        product.UpdateModifiedDate();
        await _productRepository.UpdateAsync(product);
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
    
    private void ValidateProduct(Product p)
    {
        // ТЗ: Минимальная длина названия 2 символа
        if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Trim().Length < 2)
            throw new ArgumentException("Название должно содержать минимум 2 символа.");

        // ТЗ 1.1: Сумма БЖУ на 100 грамм не может превышать 100
        if ((p.Proteins + p.Fats + p.Carbohydrates) > 100)
            throw new ArgumentException("Сумма белков, жиров и углеводов не может превышать 100г на 100г продукта.");

        // Проверка на отрицательные значения (согласно атрибутам)
        if (p.Calories < 0 || p.Proteins < 0 || p.Fats < 0 || p.Carbohydrates < 0)
            throw new ArgumentException("КБЖУ не могут быть отрицательными.");
    }
}