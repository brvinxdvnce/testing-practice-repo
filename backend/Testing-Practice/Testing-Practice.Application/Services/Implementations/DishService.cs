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
        if (dish.Name == "") return new Dish();
        ValidateNutrients(dish);
        await RecalculateDishPropertiesAsync(dish);
        await _dishRepository.AddAsync(dish);
        return dish;
    }

    public async Task UpdateAsync(Dish dish, bool isCategoryExplicitlySet)
    {
        ProcessMacros(dish, isCategoryExplicitlySet);
        ValidateNutrients(dish);
        await RecalculateDishPropertiesAsync(dish);
        
        dish.UpdateModifiedDate();
        await _dishRepository.UpdateAsync(dish);
    }
    
    public async Task<IEnumerable<Dish>> GetAllAsync(
        string? search,
        DishCategory? category,
        ProductFlags? flags,
        string? sortBy,
        bool sortDesc)
    {
        var dishes = await _dishRepository.GetAllAsync();

        // Поиск по подстроке (регистронезависимый)
        if (!string.IsNullOrWhiteSpace(search))
        {
            dishes = dishes.Where(d => d.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Фильтр по категории
        if (category.HasValue)
        {
            dishes = dishes.Where(d => d.Category == category.Value);
        }

        // Фильтр по флагам (если указан хотя бы один)
        if (flags.HasValue && flags.Value != 0)
        {
            dishes = dishes.Where(d => (d.Flags & flags.Value) == flags.Value);
        }

        // Сортировка
        dishes = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? dishes.OrderByDescending(d => d.Name) : dishes.OrderBy(d => d.Name),
            "calories" => sortDesc ? dishes.OrderByDescending(d => d.Calories) : dishes.OrderBy(d => d.Calories),
            "proteins" => sortDesc ? dishes.OrderByDescending(d => d.Proteins) : dishes.OrderBy(d => d.Proteins),
            "fats" => sortDesc ? dishes.OrderByDescending(d => d.Fats) : dishes.OrderBy(d => d.Fats),
            "carbohydrates" => sortDesc ? dishes.OrderByDescending(d => d.Carbohydrates) : dishes.OrderBy(d => d.Carbohydrates),
            _ => dishes.OrderBy(d => d.Name) // по умолчанию по имени
        };

        return dishes.ToList();
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var dish = await _dishRepository.GetByIdAsync(id);
        if (dish == null)
            throw new KeyNotFoundException($"Блюдо с id {id} не найдено.");

        await _dishRepository.DeleteAsync(id);
    }
    
    private void ProcessMacros(Dish dish, bool isCategoryExplicitlySet)
    {
        /*foreach (var macro in _macrosMap)
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
        }*/
        
        /*
        string originalName = dish.Name;
        DishCategory? appliedCategory = null;
    
        // Проходим по всем макросам
        foreach (var macro in _macrosMap)
        {
            if (originalName.Contains(macro.Key, StringComparison.OrdinalIgnoreCase))
            {
                // Удаляем макрос из названия (и лишние пробелы)
                originalName = Regex.Replace(originalName, Regex.Escape(macro.Key), "", RegexOptions.IgnoreCase).Trim();
            
                // Запоминаем категорию только если она еще не была установлена
                if (appliedCategory == null && !isCategoryExplicitlySet)
                {
                    appliedCategory = macro.Value;
                }
            }
        }
    
        // Применяем изменения
        dish.Name = originalName;
    
        // Устанавливаем категорию, если нашли хотя бы один макрос и категория не была задана явно
        if (appliedCategory.HasValue && !isCategoryExplicitlySet)
        {
            dish.Category = appliedCategory.Value;
        }*/
        
        if (isCategoryExplicitlySet) 
        {
            // Если категория задана явно, всё равно нужно удалить все макросы из названия
            foreach (var macro in _macrosMap)
            {
                dish.Name = Regex.Replace(dish.Name, Regex.Escape(macro.Key), "", RegexOptions.IgnoreCase).Trim();
            }
            return;
        }

        // Находим позиции всех макросов в строке
        var foundMacros = new List<(int Index, string Macro, DishCategory Category)>();

        foreach (var macro in _macrosMap)
        {
            int index = dish.Name.IndexOf(macro.Key, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                foundMacros.Add((index, macro.Key, macro.Value));
            }
        }

        // Если макросы найдены
        if (foundMacros.Any())
        {
            // Сортируем по позиции в строке (по возрастанию индекса)
            var firstMacro = foundMacros.OrderBy(m => m.Index).First();

            // Устанавливаем категорию по первому встречному макросу
            dish.Category = firstMacro.Category;

            // Удаляем ВСЕ макросы из названия (не только первый)
            string processedName = dish.Name;
            foreach (var macro in _macrosMap)
            {
                processedName = Regex.Replace(processedName, Regex.Escape(macro.Key), "", RegexOptions.IgnoreCase)
                    .Trim();
            }

            dish.Name = processedName;
        }
    }

    // Требования 2.2 и 2.4: Расчет КБЖУ и Флагов
    /*public async Task RecalculateDishPropertiesAsync(Dish dish)
    {
        if (dish.Ingredients == null || !dish.Ingredients.Any()) return;

        // Получаем актуальные данные о продуктах из БД
        var productIds = dish.Ingredients.Select(i => i.ProductId).Distinct();
        var products = (await _dishRepository.GetProductsByIdsAsync(productIds)).ToDictionary(p => p.Id);

        double calcCal = 0, calcProt = 0, calcFat = 0, calcCarb = 0;
        
        // Изначально предполагаем, что у блюда есть все флаги. 
        // Побитовое "И" (Bitwise AND) оставит только те флаги, которые есть у ВСЕХ продуктов.
        ProductFlags combinedFlags = (ProductFlags.Vegan | ProductFlags.GlutenFree | ProductFlags.SugarFree);

        foreach (var ingredient in dish.Ingredients)
        {
            if (products.TryGetValue(ingredient.ProductId, out var p))
            {
                // Формула: (Значение на 100г * Количество в блюде) / 100
                calcCal += (p.Calories * ingredient.Amount) / 100;
                calcProt += (p.Proteins * ingredient.Amount) / 100;
                calcFat += (p.Fats * ingredient.Amount) / 100;
                calcCarb += (p.Carbohydrates * ingredient.Amount) / 100;
                combinedFlags &= p.Flags;
            }
        }

        if (dish.Calories == 0) dish.Calories = calcCal;
        if (dish.Proteins == 0) dish.Proteins = calcProt;
        if (dish.Fats == 0) dish.Fats = calcFat;
        if (dish.Carbohydrates == 0) dish.Carbohydrates = calcCarb;

        dish.Flags = (ProductFlags)combinedFlags; 
    }
    */
    
    public async Task RecalculateDishPropertiesAsync(Dish dish)
    {
        // Если ингредиентов нет, считать нечего
        if (dish.Ingredients == null || !dish.Ingredients.Any()) return;

        // Проверяем, нужно ли нам вообще что-то считать (если всё уже заполнено, экономим ресурсы)
        bool needsNutrients = dish.Calories == 0 || dish.Proteins == 0 || dish.Fats == 0 || dish.Carbohydrates == 0;
        bool needsFlags = dish.Flags == 0; // Или другое дефолтное значение

        if (!needsNutrients && !needsFlags) return;

        var productIds = dish.Ingredients.Select(i => i.ProductId).Distinct();
        var products = (await _dishRepository.GetProductsByIdsAsync(productIds)).ToDictionary(p => p.Id);

        double calcCal = 0, calcProt = 0, calcFat = 0, calcCarb = 0;
        ProductFlags combinedFlags = (ProductFlags.Vegan | ProductFlags.GlutenFree | ProductFlags.SugarFree);

        foreach (var ingredient in dish.Ingredients)
        {
            if (products.TryGetValue(ingredient.ProductId, out var p))
            {
                calcCal += (p.Calories * ingredient.Amount) / 100;
                calcProt += (p.Proteins * ingredient.Amount) / 100;
                calcFat += (p.Fats * ingredient.Amount) / 100;
                calcCarb += (p.Carbohydrates * ingredient.Amount) / 100;
            
                // Побитовое И: флаг останется, только если он есть у ВСЕХ продуктов
                combinedFlags &= p.Flags;
            }
        }

        // Применяем расчеты только к тем полям, которые равны 0
        if (dish.Calories == 0) dish.Calories = calcCal;
        if (dish.Proteins == 0) dish.Proteins = calcProt;
        if (dish.Fats == 0) dish.Fats = calcFat;
        if (dish.Carbohydrates == 0) dish.Carbohydrates = calcCarb;

        // Если флаги не были установлены (равны 0), ставим вычисленные
        if (dish.Flags == 0) 
        {
            dish.Flags = combinedFlags;
        }
    }
    
    private void ValidateNutrients(Dish dish)
    {
        // Проверка суммы БЖУ на порцию (приведенную к 100г веса порции)
        double sumBjuPerPortion = (dish.Proteins + dish.Fats + dish.Carbohydrates);
        double portionWeight = dish.PortionSize; // Предполагаем, что это вес в граммах

        if (portionWeight > 0)
        {
            double bjuPer100g = (sumBjuPerPortion / portionWeight) * 100;
            if (bjuPer100g > 100.001) // Небольшой допуск на точность float
                throw new ArgumentException("Сумма БЖУ на 100 грамм блюда не может превышать 100.");
        }
    }
    
     public async Task RecalculateDishPropеrtiesAsync(Dish dish)
{
    // Если ингредиентов нет, считать нечего
    if (dish.Ingredients == null || !dish.Ingredients.Any()) return;

    // Проверяем, нужно ли нам вообще что-то считать (если всё уже заполнено, экономим ресурсы)
    bool needsNutrients = dish.Calories == 0 || dish.Proteins == 0 || dish.Fats == 0 || dish.Carbohydrates == 0;
    bool needsFlags = dish.Flags == 0;

    if (!needsNutrients && !needsFlags) return;

    var productIds = dish.Ingredients.Select(i => i.ProductId).Distinct();
    var products = (await _dishRepository.GetProductsByIdsAsync(productIds)).ToDictionary(p => p.Id);

    double calcCal = 0, calcProt = 0, calcFat = 0, calcCarb = 0;
    ProductFlags combinedFlags = (ProductFlags.Vegan | ProductFlags.GlutenFree | ProductFlags.SugarFree);

    foreach (var ingredient in dish.Ingredients)
    {
        // **ВАЛИДАЦИЯ: пропускаем ингредиенты с невалидным количеством**
        if (ingredient.Amount == null || ingredient.Amount <= 0) continue;

        if (products.TryGetValue(ingredient.ProductId, out var p))
        {
            // **ВАЛИДАЦИЯ: пропускаем продукты с отрицательными КБЖУ**
            // (оставляем 0 как допустимое значение - например, вода)
            double calories = p.Calories > 0 ? p.Calories : 0;
            double proteins = p.Proteins > 0 ? p.Proteins : 0;
            double fats = p.Fats > 0 ? p.Fats : 0;
            double carbs = p.Carbohydrates > 0 ? p.Carbohydrates : 0;
            
            calcCal += (calories * ingredient.Amount) / 100;
            calcProt += (proteins * ingredient.Amount) / 100;
            calcFat += (fats * ingredient.Amount) / 100;
            calcCarb += (carbs * ingredient.Amount) / 100;
        
            // Побитовое И: флаг останется, только если он есть у ВСЕХ продуктов
            combinedFlags &= p.Flags;
        }
    }

    // Применяем расчеты только к тем полям, которые равны 0
    if (dish.Calories == 0) dish.Calories = calcCal;
    if (dish.Proteins == 0) dish.Proteins = calcProt;
    if (dish.Fats == 0) dish.Fats = calcFat;
    if (dish.Carbohydrates == 0) dish.Carbohydrates = calcCarb;

    // Если флаги не были установлены (равны 0), ставим вычисленные
    if (dish.Flags == 0) 
    {
        dish.Flags = combinedFlags;
    }
}
}