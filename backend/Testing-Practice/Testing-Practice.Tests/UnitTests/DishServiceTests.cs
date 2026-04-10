using Moq;
using Testing_Practice.Application.Services.Implementations;
using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;

namespace Testing_Practice.Tests.Application.Services;

public class DishServiceTests : IDisposable
{
    private readonly Mock<IDishRepository> _dishRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly DishService _dishService;

    // Setup: Выполняется перед КАЖДЫМ тестом (в xUnit конструктор заменяет [SetUp])
    public DishServiceTests()
    {
        _dishRepositoryMock = new Mock<IDishRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _dishService = new DishService(_dishRepositoryMock.Object, _productRepositoryMock.Object);
    }

    // Teardown: Выполняется после КАЖДОГО теста (в xUnit Dispose заменяет [TearDown])
    public void Dispose()
    {
        // Очистка ресурсов, сброс моков
        _dishRepositoryMock.Invocations.Clear();
        _productRepositoryMock.Invocations.Clear();
    }

    /// <summary>
    /// Пустые ингредиенты
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_NoIngredients_DoesNothing()
    {
        var dish = new Dish { Ingredients = new List<DishIngredient>(), Calories = 0 };

        await _dishService.RecalculateDishPropertiesAsync(dish);

        Assert.Equal(0, dish.Calories);
        _dishRepositoryMock.Verify(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never, 
            "Не должны обращаться к БД, если нет ингредиентов");
    }

    /// <summary>
    /// Анализ граничных значений: Обработка количества продукта, близкого к нулевому пределу.
    /// Формула: (Calories * Amount) / 100
    /// </summary>
    [Theory]
    [InlineData(-0.0001, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.0001, 0.0001)]  // 100 калорий * 0.0001г / 100 = 0.0001
    public async Task RecalculateDishPropertiesAsync_QuantityNearZero_HandlesCorrectly(double quantity, double expectedCalories)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0,
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = productId, Amount = quantity }
            }
        };

        var product = new Product { Id = productId, Calories = 100, Proteins = 0, Fats = 0, Carbohydrates = 0 };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropеrtiesAsync(dish);

        // Assert
        Assert.Equal(expectedCalories, dish.Calories, 7); // Используем высокую точность 1e-7
    }
    
    /// <summary>
    /// Анализ граничных значений: Учет ограничений по калориям на 100 г.
    /// </summary>
    [Theory]
    [InlineData(-0.0001, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.0001, 0.0001)]
    public async Task RecalculateDishPropertiesAsync_CaloriesPer100gBoundary_HandlesCorrectly(double caloriesPer100g, double expectedCalories)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0,
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = productId, Amount = 100.0 }
            }
        };

        var product = new Product 
        { 
            Id = productId, 
            Calories = caloriesPer100g, 
            Proteins = 0, 
            Fats = 0, 
            Carbohydrates = 0 
        };
    
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropеrtiesAsync(dish);

        // Assert
        Assert.Equal(expectedCalories, dish.Calories, 7);
    }

    /// <summary>
    /// Параметризованный тест (Data-Driven Test).
    /// Анализ граничных значений (BVA) и Эквивалентное разбиение (EP) для расчетов.
    /// Формула: (Macro * Amount) / 100.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetCalculationTestData))]
    public async Task RecalculateDishPropertiesAsync_ValidIngredients_CalculatesCorrectly(
        double productCalories, double ingredientAmount, double expectedDishCalories)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0,
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = productId, Amount = ingredientAmount }
            }
        };

        var mockProducts = new List<Product>
        {
            new Product { Id = productId, Calories = productCalories, Proteins = 0, Fats = 0, Carbohydrates = 0 }
        };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(mockProducts);

        await _dishService.RecalculateDishPropertiesAsync(dish);

        Assert.Equal(expectedDishCalories, dish.Calories, 3);
    }

    /// <summary>
    /// Анализ граничных значений: Учет лимитов белков, жиров и углеводов.
    /// Тестирует граничные значения (0 и 33.3333)
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(33.3333)]
    public async Task RecalculateDishPropertiesAsync_MacroBoundaries_HandlesCorrectly(double macroPer100g)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0,
            Proteins = 0,
            Fats = 0,
            Carbohydrates = 0,
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = productId, Amount = 100.0 }
            }
        };

        var product = new Product 
        { 
            Id = productId, 
            Calories = 0, 
            Proteins = macroPer100g, 
            Fats = macroPer100g, 
            Carbohydrates = macroPer100g 
        };
        
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(macroPer100g, dish.Proteins, 7);
        Assert.Equal(macroPer100g, dish.Fats, 7);
        Assert.Equal(macroPer100g, dish.Carbohydrates, 7);
    }

    /// <summary>
    /// Анализ граничных значений: Корректный расчет макронутриентов для внутренних значений диапазона.
    /// </summary>
    [Theory]
    [InlineData(0.0001)]
    [InlineData(16.5)]
    [InlineData(33.3332)]
    public async Task RecalculateDishPropertiesAsync_MacroInsideRange_CalculatesCorrectly(double macroPer100g)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0,
            Proteins = 0,
            Fats = 0,
            Carbohydrates = 0,
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = productId, Amount = 100.0 }
            }
        };

        var product = new Product 
        { 
            Id = productId, 
            Calories = 0, 
            Proteins = macroPer100g, 
            Fats = macroPer100g, 
            Carbohydrates = macroPer100g 
        };
        
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(macroPer100g, dish.Proteins, 7);
        Assert.Equal(macroPer100g, dish.Fats, 7);
        Assert.Equal(macroPer100g, dish.Carbohydrates, 7);
    }
    
    /// <summary>
    /// Генератор данных для параметризованного теста выше.
    /// </summary>
    public static IEnumerable<object[]> GetCalculationTestData()
    {
        // [Калорийность продукта на 100г, Вес в блюде, Ожидаемый результат]
        yield return new object[] { 100.0, 0.01, 0.01 };      // Минимально допустимый вес (0.01г по валидации)
        yield return new object[] { 0.0, 500.0, 0.0 };        // Продукт с 0 калорий (например, вода)
        yield return new object[] { 250.0, 100.0, 250.0 };    // Стандартный вес (100г), равен калорийности продукта
        yield return new object[] { 900.0, 999.99, 8999.91 }; // Большой вес и высокая калорийность
        yield return new object[] { 333.33, 150.0, 499.995 }; // Дробные значения КБЖУ (обычный продукт)
    }

    /// <summary>
    /// Если хотя бы один макрос 0, расчет идет, но перезаписываются ТОЛЬКО те макросы, которые равны 0.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_PartialNutrientsSet_OverridesOnlyZeroValues()
    {
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 500,  // Уже задано, не должно измениться
            Proteins = 0,    // Должно рассчитаться
            Fats = 10,       // Уже задано
            Carbohydrates = 0, // Должно рассчитаться
            Ingredients = new List<DishIngredient> { new DishIngredient { ProductId = productId, Amount = 100 } }
        };

        var product = new Product { Id = productId, Calories = 100, Proteins = 25, Fats = 5, Carbohydrates = 50 };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        await _dishService.RecalculateDishPropertiesAsync(dish);

        Assert.Equal(500, dish.Calories); // Осталось старым
        Assert.Equal(10, dish.Fats);      // Осталось старым
        Assert.Equal(25, dish.Proteins);  // Рассчитано: (25 * 100)/100
        Assert.Equal(50, dish.Carbohydrates); // Рассчитано: (50 * 100)/100
    }

    /// <summary>
    /// Тестирование побитовых операций (Bitwise AND).
    /// Флаг у блюда должен остаться только если он есть у ВСЕХ продуктов.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_FlagsCalculation_PerformsBitwiseAndCorrectly()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        
        var dish = new Dish
        {
            Flags = ProductFlags.None, // 0
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = product1Id, Amount = 100 },
                new DishIngredient { ProductId = product2Id, Amount = 100 }
            }
        };

        var products = new List<Product>
        {
            // У первого продукта: Веган, Без глютена, Без сахара
            new Product { Id = product1Id, Flags = ProductFlags.Vegan | ProductFlags.GlutenFree | ProductFlags.SugarFree },
            // У второго продукта: Веган, Без глютена (сахар есть)
            new Product { Id = product2Id, Flags = ProductFlags.Vegan | ProductFlags.GlutenFree }
        };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(products);

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Ожидаем: Vegan | GlutenFree (SugarFree отвалится из-за побитового И)
        var expectedFlags = ProductFlags.Vegan | ProductFlags.GlutenFree;
        //var expectedFlags = ProductFlags.SugarFree;
        Assert.Equal(expectedFlags, dish.Flags);
    }
}