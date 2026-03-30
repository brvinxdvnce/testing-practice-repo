using Moq;
using Testing_Practice.Application.Services.Implementations;
using Testing_Practice.Application.Services.Interfaces;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;

namespace Testing_Practice.Tests.UnitTests;

// Тестовый набор для проверки калькулятора КБЖУ блюд.
// Покрывает требования п. 2.2 и правила автоматического расчета.

public partial class DishCalculatorServiceTests : IDisposable
{
    private readonly Mock<IProductRepository> _productRepositoryMock; 
    private readonly IDishService _calculatorService;

    public DishCalculatorServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _calculatorService = new DishService(_productRepositoryMock.Object);
    }

    
    // Очистка ресурсов после каждого теста.
    public void Dispose()
    {
        // Сброс настроек мока, чтобы тесты не влияли друг на друга
        _productRepositoryMock.Reset();
    }

    
    // Тестирование расчета калорийности одного ингредиента.
    // Применяются техники: Эквивалентное разбиение (EP) и Анализ граничных значений (BVA).
    // Формула: (Калорийность на 100г * Количество в порции) / 100.
    // <param name="productCals">Калорийность продукта на 100 г</param>
    // <param name="amountInGrams">Количество продукта в блюде (г)</param>
    // <param name="expectedCals">Ожидаемая калорийность</param>
    // <param name="testDescription">Описание тест-кейса (EP/BVA)</param>
    [Theory]
    [InlineData(100.0, 150.0, 150.0, "EP: Обычное значение внутри допустимого диапазона")]
    [InlineData(250.5, 200.0, 501.0, "EP: Дробная калорийность, стандартный вес")]
    [InlineData(0.0, 100.0, 0.0,     "BVA: Нулевая калорийность продукта (нижняя граница - вода/соль)")]
    [InlineData(900.0, 0.01, 0.09,   "BVA: Минимально допустимый вес порции > 0 (0.01г)")]
    [InlineData(100.0, 10000.0, 10000.0, "EP: Очень большое количество ингредиента")]
    public void CalculateDishMacros_SingleIngredient_ReturnsCorrectCalories(
        double productCals, double amountInGrams, double expectedCals, string testDescription)
    {
        // Arrange
        var productId = Guid.NewGuid();
        
        var mockProduct = new Product 
        { 
            Id = productId, 
            Name = "Тестовый продукт", 
            Calories = productCals 
        };

        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            Name = "Тестовое блюдо",
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = productId, Amount = amountInGrams }
            }
        };

        // Настраиваем заглушку (Stub): репозиторий возвращает нужный продукт
        _productRepositoryMock
            .Setup(repo => repo.GetByIdAsync(productId))
            .Returns(mockProduct);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        // Проверка с точностью до 2 знаков после запятой из-за специфики double
        Assert.Equal(expectedCals, dish.Calories, 2);
    }
    
    // Тестирование расчета общего КБЖУ для блюда из нескольких ингредиентов.
    // Проверяет корректность суммирования (Σ) по всем макронутриентам.
    [Fact]
    public void CalculateDishMacros_MultipleIngredients_SumsAllMacrosCorrectly()
    {
        // Arrange
        var product1 = new Product { Id = Guid.NewGuid(), Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 20 };
        var product2 = new Product { Id = Guid.NewGuid(), Calories = 200, Proteins = 0, Fats = 20, Carbohydrates = 5 };

        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = product1.Id, Amount = 150 }, // Коэф. 1.5
                new DishIngredient { ProductId = product2.Id, Amount = 50 }   // Коэф. 0.5
            }
        };

        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(product1.Id)).Returns(product1);
        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(product2.Id)).Returns(product2);

        // Ожидаемые значения:
        // Калории: (100 * 1.5) + (200 * 0.5) = 150 + 100 = 250
        // Белки: (10 * 1.5) + (0 * 0.5) = 15 + 0 = 15
        // Жиры: (5 * 1.5) + (20 * 0.5) = 7.5 + 10 = 17.5
        // Углеводы: (20 * 1.5) + (5 * 0.5) = 30 + 2.5 = 32.5

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        Assert.Equal(250.0, dish.Calories, 2);
        Assert.Equal(15.0, dish.Proteins, 2);
        Assert.Equal(17.5, dish.Fats, 2);
        Assert.Equal(32.5, dish.Carbohydrates, 2);
    }
    
    // Тестирование негативного сценария: вес ингредиента меньше или равен нулю.
    // Анализ граничных значений (BVA) - недопустимые данные.
    [Theory]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    public void CalculateDishMacros_InvalidAmount_ThrowsArgumentException(double invalidAmount)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Calories = 100 };
        
        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new DishIngredient { ProductId = productId, Amount = invalidAmount }
            }
        };

        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(productId)).Returns(product);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _calculatorService.CalculateMacros(dish));
        Assert.Contains("Размер порции должен быть больше 0", exception.Message);
    }
}