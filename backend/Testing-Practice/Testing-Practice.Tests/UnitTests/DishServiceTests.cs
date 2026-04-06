using Moq;
using Testing_Practice.Application.Services.Implementations;
using Testing_Practice.Application.Services.Interfaces;
using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;

namespace Testing_Practice.Tests.UnitTests;

/*
public partial class DishCalculatorServiceTests : IDisposable
{
    private readonly Mock<IProductRepository> _productRepositoryMock; 
    private readonly Mock<IDishRepository> _dishRepositoryMock;
    private readonly IDishService _dishService;

    public DishCalculatorServiceTests(Mock<IDishRepository> dishRepositoryMock)
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _dishRepositoryMock = new Mock<IDishRepository>();
        _dishService = new DishService(_dishRepositoryMock.Object, _productRepositoryMock.Object);
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
        _dishService.CalculateMacros(dish);

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
        _dishService.CalculateMacros(dish);

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
        var exception = Assert.Throws<ArgumentException>(() => _dishService.CalculateMacros(dish));
        Assert.Contains("Размер порции должен быть больше 0", exception.Message);
    }
}
*/

/*public class DishServiceTests
{
    private readonly Mock<IDishRepository> _dishRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly DishService _dishService;

    public DishServiceTests()
    {
        _dishRepositoryMock = new Mock<IDishRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _dishService = new DishService(_dishRepositoryMock.Object, _productRepositoryMock.Object);
    }

    /*[Fact]
    public async Task CreateAsync_WithMacroInName_ShouldSetCategoryAndCleanName()
    {
        // Arrange
        var dish = new Dish { Name = "!суп Борщ украинский" };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product>());

        // Act
        await _dishService.CreateAsync(dish, isCategoryExplicitlySet: false);

        // Assert
        Assert.Equal("Борщ украинский", dish.Name);
        Assert.Equal(DishCategory.Soup, dish.Category);
    }#1#

    /*[Fact]
    public async Task CreateAsync_ExplicitCategory_ShouldOverrideMacro()
    {
        // Arrange
        var dish = new Dish { Name = "!десерт Стейк", Category = DishCategory.Second };

        // Act
        await _dishService.CreateAsync(dish, isCategoryExplicitlySet: true);

        // Assert
        Assert.Equal("Стейк", dish.Name);
        Assert.Equal(DishCategory.Second, dish.Category); // Приоритет ручного ввода
    }#1#

    [Fact]
    public async Task RecalculateDishPropertiesAsync_ShouldCalculateCorrectKBJU()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product 
        { 
            Id = productId, 
            Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 2,
            Flags = ProductFlags.Vegan // Для чистоты теста
        };

        var dish = new Dish
        {
            Ingredients = new List<DishIngredient> 
            { 
                new() { ProductId = productId, Amount = 250 } // 2.5 * 100г
            }
        };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(250, dish.Calories);   // 100 * 2.5
        Assert.Equal(25, dish.Proteins);    // 10 * 2.5
        Assert.Equal(12.5, dish.Fats);      // 5 * 2.5
        Assert.Equal(5, dish.Carbohydrates);// 2 * 2.5
    }

    /*[Fact]
    public async Task RecalculateDishPropertiesAsync_ShouldResetFlags_IfOneProductLacksThem()
    {
        // Arrange
        var p1 = new Product { Id = Guid.NewGuid(), Flags = ProductFlags.Vegan | ProductFlags.GlutenFree };
        var p2 = new Product { Id = Guid.NewGuid(), Flags = ProductFlags.GlutenFree }; // НЕ Веган

        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = p1.Id, Amount = 100 },
                new() { ProductId = p2.Id, Amount = 100 }
            }
        };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { p1, p2 });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.True(dish.Flags.HasFlag(ProductFlags.GlutenFree), "Должен остаться GlutenFree");
        Assert.False(dish.Flags.HasFlag(ProductFlags.Vegan), "Флаг Vegan должен быть снят");
    }#1#
}*/

public class DishServiceTests
{
    private readonly Mock<IDishRepository> _dishRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly DishService _dishService;

    public DishServiceTests()
    {
        _dishRepositoryMock = new Mock<IDishRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _dishService = new DishService(_dishRepositoryMock.Object, _productRepositoryMock.Object);
    }

    /*[Fact]
    public async Task CreateAsync_WithMacroInName_ShouldSetCategoryAndCleanName()
    {
        // Arrange
        var dish = new Dish { Name = "!суп Борщ" };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product>());

        // Act
        await _dishService.CreateAsync(dish, isCategoryExplicitlySet: false);

        // Assert
        Assert.Equal("Борщ", dish.Name);
        Assert.Equal(DishCategory.Soup, dish.Category);
    }*/

    /*[Fact]
    public async Task CreateAsync_ExplicitCategory_ShouldOverrideMacro()
    {
        // Arrange
        var dish = new Dish { Name = "!десерт Стейк", Category = DishCategory.Second };

        // Act
        await _dishService.CreateAsync(dish, isCategoryExplicitlySet: true);

        // Assert
        Assert.Equal("Стейк", dish.Name);
        Assert.Equal(DishCategory.Second, dish.Category); // Приоритет ручного ввода
    }*/

    [Fact]
    public async Task RecalculateDishPropertiesAsync_ShouldCalculateCorrectKBJU()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product 
        { 
            Id = productId, 
            Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 2,
            Flags = ProductFlags.Vegan // Для чистоты теста
        };

        var dish = new Dish
        {
            Ingredients = new List<DishIngredient> 
            { 
                new() { ProductId = productId, Amount = 250 } // 2.5 * 100г
            }
        };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(250, dish.Calories);   // 100 * 2.5
        Assert.Equal(25, dish.Proteins);    // 10 * 2.5
        Assert.Equal(12.5, dish.Fats);      // 5 * 2.5
        Assert.Equal(5, dish.Carbohydrates);// 2 * 2.5
    }

    [Fact]
    public async Task RecalculateDishPropertiesAsync_ShouldResetFlags_IfOneProductLacksThem()
    {
        // Arrange
        var p1 = new Product { Id = Guid.NewGuid(), Flags = ProductFlags.Vegan | ProductFlags.GlutenFree };
        var p2 = new Product { Id = Guid.NewGuid(), Flags = ProductFlags.GlutenFree }; // НЕ Веган

        var dish = new Dish
        {
            Ingredients = new List<DishIngredient> 
            { 
                new() { ProductId = p1.Id, Amount = 100 },
                new() { ProductId = p2.Id, Amount = 100 }
            }
        };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { p1, p2 });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.True(dish.Flags.HasFlag(ProductFlags.GlutenFree), "Должен остаться GlutenFree");
        Assert.False(dish.Flags.HasFlag(ProductFlags.Vegan), "Флаг Vegan должен быть снят");
    }
    
    // Если продукт не найден в репозитории, он должен игнорироваться
    // (т.к. GetProductsByIdsAsync возвращает только существующие).
    [Fact]
    public async Task RecalculateDishPropertiesAsync_ProductNotFound_SkipsIngredient()
    {
        // Arrange
        var existingProductId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();

        var existingProduct = new Product { Id = existingProductId, Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 20 };
        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = existingProductId, Amount = 100 },
                new() { ProductId = missingProductId, Amount = 50 }
            }
        };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.Is<IEnumerable<Guid>>(ids =>
            ids.Contains(existingProductId) && ids.Contains(missingProductId))))
            .ReturnsAsync(new List<Product> { existingProduct });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Вклад отсутствующего продукта не добавляется, только существующий (100 ккал)
        Assert.Equal(100, dish.Calories, 2);
        Assert.Equal(10, dish.Proteins, 2);
        Assert.Equal(5, dish.Fats, 2);
        Assert.Equal(20, dish.Carbohydrates, 2);
    }

    // Если состав блюда пуст, все макронутриенты должны быть обнулены.
    [Fact]
    public async Task RecalculateDishPropertiesAsync_EmptyIngredients_ReturnsZero()
    {
        // Arrange
        var dish = new Dish { Ingredients = new List<DishIngredient>() };

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(0, dish.Calories, 2);
        Assert.Equal(0, dish.Proteins, 2);
        Assert.Equal(0, dish.Fats, 2);
        Assert.Equal(0, dish.Carbohydrates, 2);
    }

    // Проверка расчёта при максимально допустимых значениях БЖУ продукта (100).
    [Fact]
    public async Task RecalculateDishPropertiesAsync_ProductWithMaxNutrients_ReturnsCorrect()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Calories = 100,
            Proteins = 100,
            Fats = 100,
            Carbohydrates = 100
        };
        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = product.Id, Amount = 75 }
            }
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(product.Id))))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        // 75 г = 0.75 от 100 г → каждый показатель умножается на 0.75
        Assert.Equal(75, dish.Calories, 2);
        Assert.Equal(75, dish.Proteins, 2);
        Assert.Equal(75, dish.Fats, 2);
        Assert.Equal(75, dish.Carbohydrates, 2);
    }

    // Если данные продукта некорректны (сумма БЖУ > 100), расчёт всё равно должен выполняться
    // корректно как арифметическая операция.
    [Fact]
    public async Task RecalculateDishPropertiesAsync_ProductWithSumGreaterThan100_CalculatesCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Calories = 200,
            Proteins = 80,
            Fats = 50,
            Carbohydrates = 40   // сумма = 170 > 100
        };
        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = product.Id, Amount = 200 }
            }
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(product.Id))))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Коэффициент = 200 / 100 = 2
        Assert.Equal(400, dish.Calories, 2);
        Assert.Equal(160, dish.Proteins, 2);
        Assert.Equal(100, dish.Fats, 2);
        Assert.Equal(80, dish.Carbohydrates, 2);
    }

    // При расчёте с несколькими ингредиентами суммарная погрешность не должна превышать
    // разумный порог (используется допуск 1e-9).
    [Fact]
    public async Task RecalculateDishPropertiesAsync_ManyIngredients_PrecisionIsMaintained()
    {
        // Arrange
        const int count = 100;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Calories = 33.3333333333,
            Proteins = 33.3333333333,
            Fats = 33.3333333333,
            Carbohydrates = 33.3333333333
        };
        var ingredients = new List<DishIngredient>();
        for (int i = 0; i < count; i++)
        {
            ingredients.Add(new DishIngredient { ProductId = product.Id, Amount = 1 });
        }
        var dish = new Dish { Ingredients = ingredients };

        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(product.Id))))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Ожидаемое значение для каждого макронутриента: (33.333... * 1/100) * 100 = 33.333...
        double expected = 33.3333333333; // теоретическое значение
        Assert.Equal(expected, dish.Calories, 9);
        Assert.Equal(expected, dish.Proteins, 9);
        Assert.Equal(expected, dish.Fats, 9);
        Assert.Equal(expected, dish.Carbohydrates, 9);
    }

    // Метод RecalculateDishPropertiesAsync не должен изменять объекты продуктов, полученные из репозитория.
    [Fact]
    public async Task RecalculateDishPropertiesAsync_DoesNotModifyProductObjects()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = new Product
        {
            Id = productId,
            Calories = 100,
            Proteins = 10,
            Fats = 5,
            Carbohydrates = 20
        };
        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = productId, Amount = 150 }
            }
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(productId))))
            .ReturnsAsync(new List<Product> { originalProduct });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Сравниваем все поля продукта с исходными значениями
        Assert.Equal(100, originalProduct.Calories);
        Assert.Equal(10, originalProduct.Proteins);
        Assert.Equal(5, originalProduct.Fats);
        Assert.Equal(20, originalProduct.Carbohydrates);
    }

    // Проверка расчёта при минимально возможном положительном количестве (0.0001 г).
    [Theory]
    [InlineData(0.0001, 0.0001)] // 0.0001 г * 100 ккал/100г = 0.0001 ккал
    [InlineData(0.00001, 0.00001)]
    public async Task RecalculateDishPropertiesAsync_VerySmallAmount_ReturnsProportionalValue(double amount, double expectedCalories)
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Calories = 100 };
        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = product.Id, Amount = amount }
            }
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(product.Id))))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(expectedCalories, dish.Calories, 10);
    }

    // Если у продукта отрицательные показатели КБЖУ (например, ошибка данных),
    // расчёт должен корректно отработать и дать отрицательные значения.
    [Fact]
    public async Task RecalculateDishPropertiesAsync_NegativeNutrients_ReturnsNegativeValues()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Calories = -100,
            Proteins = -10,
            Fats = -5,
            Carbohydrates = -20
        };
        var dish = new Dish
        {
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = product.Id, Amount = 200 }
            }
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(product.Id))))
            .ReturnsAsync(new List<Product> { product });

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(-200, dish.Calories, 2);
        Assert.Equal(-20, dish.Proteins, 2);
        Assert.Equal(-10, dish.Fats, 2);
        Assert.Equal(-40, dish.Carbohydrates, 2);
    }
}