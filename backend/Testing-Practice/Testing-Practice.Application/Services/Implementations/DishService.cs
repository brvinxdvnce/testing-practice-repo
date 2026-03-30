using System.Text.RegularExpressions;
using Testing_Practice.Application.Services.Interfaces;
using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;

namespace Testing_Practice.Application.Services.Implementations;

public class DishService : IDishService
{
    private readonly IDishRepository _dishRepository;
    private readonly IProductRepository _productRepository;

    // Словарь для маппинга макросов в категории
    private readonly Dictionary<string, DishCategory> _macrosMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "!десерт", DishCategory.Dessert },
        { "!первое", DishCategory.First },
        { "!второе", DishCategory.Second },
        { "!напиток", DishCategory.Drink },
        { "!салат", DishCategory.Salad },
        { "!суп", DishCategory.Soup },
        { "!перекус", DishCategory.Snack }
    };

    public DishService(IDishRepository dishRepository, IProductRepository productRepository)
    {
        _dishRepository = dishRepository;
        _productRepository = productRepository;
    }

    public async Task<Dish> CreateAsync(Dish dish, bool isCategoryExplicitlySet)
    {
        ProcessMacros(dish, isCategoryExplicitlySet);
        await RecalculateDishPropertiesAsync(dish); // Считаем КБЖУ и Флаги
        
        await _dishRepository.AddAsync(dish);
        return dish;
    }

    public async Task UpdateAsync(Dish dish, bool isCategoryExplicitlySet)
    {
        ProcessMacros(dish, isCategoryExplicitlySet);
        await RecalculateDishPropertiesAsync(dish);
        
        dish.UpdateModifiedDate();
        await _dishRepository.UpdateAsync(dish);
    }

    // --- БИЗНЕС-ЛОГИКА ---

    // Требование 2.3: Обработка макросов
    private void ProcessMacros(Dish dish, bool isCategoryExplicitlySet)
    {
        foreach (var macro in _macrosMap)
        {
            if (dish.Name.Contains(macro.Key, StringComparison.OrdinalIgnoreCase))
            {
                // Удаляем макрос из названия (и лишние пробелы)
                dish.Name = Regex.Replace(dish.Name, Regex.Escape(macro.Key), "", RegexOptions.IgnoreCase).Trim();

                // Если категория не была задана явно в форме (или мы хотим дать приоритет форме), применяем макрос
                if (!isCategoryExplicitlySet)
                {
                    dish.Category = macro.Value;
                }
                break; // Применяем только первый найденный макрос
            }
        }
    }

    // Требования 2.2 и 2.4: Расчет КБЖУ и Флагов
    public async Task RecalculateDishPropertiesAsync(Dish dish)
    {
        if (dish.Ingredients == null || !dish.Ingredients.Any()) return;

        // Получаем актуальные данные о продуктах из БД
        var productIds = dish.Ingredients.Select(i => i.ProductId).Distinct();
        var products = (await _dishRepository.GetProductsByIdsAsync(productIds)).ToDictionary(p => p.Id);

        double totalCalories = 0, totalProteins = 0, totalFats = 0, totalCarbs = 0;
        
        // Изначально предполагаем, что у блюда есть все флаги. 
        // Побитовое "И" (Bitwise AND) оставит только те флаги, которые есть у ВСЕХ продуктов.
        ProductFlags combinedFlags = (ProductFlags.Vegan | ProductFlags.GlutenFree | ProductFlags.SugarFree);

        foreach (var ingredient in dish.Ingredients)
        {
            if (products.TryGetValue(ingredient.ProductId, out var product))
            {
                // Формула: (Значение на 100г * Количество в блюде) / 100
                totalCalories += (product.Calories * ingredient.Amount) / 100;
                totalProteins += (product.Proteins * ingredient.Amount) / 100;
                totalFats += (product.Fats * ingredient.Amount) / 100;
                totalCarbs += (product.Carbohydrates * ingredient.Amount) / 100;

                // Убираем флаги, которых нет у текущего продукта
                combinedFlags &= product.Flags; 
            }
        }

        // Записываем черновые значения КБЖУ (если пользователь не передал свои ручные значения)
        // В реальном API вы, скорее всего, будете проверять, передал ли пользователь эти поля
        dish.Calories = totalCalories;
        dish.Proteins = totalProteins;
        dish.Fats = totalFats;
        dish.Carbohydrates = totalCarbs;

        // Кастуем ProductFlags обратно в DishFlags (так как они идентичны по значениям)
        dish.Flags = (ProductFlags)combinedFlags; 
    }
}