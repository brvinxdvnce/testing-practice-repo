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
        // Очистка ресурсов, сброс моков (опционально, т.к. xUnit создает новый инстанс класса на каждый тест)
        _dishRepositoryMock.Invocations.Clear();
        _productRepositoryMock.Invocations.Clear();
    }

    /// <summary>
    /// Эквивалентное разбиение (EP): Класс "Пустые ингредиенты".
    /// Если ингредиентов нет (null или пусто), расчет не должен производиться.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_NoIngredients_DoesNothing()
    {
        // Arrange
        var dish = new Dish { Ingredients = new List<DishIngredient>(), Calories = 0 };

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(0, dish.Calories);
        _dishRepositoryMock.Verify(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never, 
            "Не должны обращаться к БД, если нет ингредиентов");
    }

    /// <summary>
    /// Эквивалентное разбиение (EP): Класс "КБЖУ и флаги уже заданы".
    /// Если все макросы > 0 и флаги не 0, вычисления скипаются ради экономии ресурсов.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_NutrientsAlreadySet_SkipsCalculation()
    {
        // Arrange
        var dish = new Dish
        {
            Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 20,
            Flags = ProductFlags.Vegan,
            Ingredients = new List<DishIngredient> { new DishIngredient { ProductId = Guid.NewGuid(), Amount = 100 } }
        };

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        _dishRepositoryMock.Verify(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never,
            "Вычисление должно быть пропущено, если КБЖУ и Флаги уже установлены");
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
            Calories = 0, // Устанавливаем в 0, чтобы триггернуть расчет
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

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Используем дельту 0.001 для сравнения double (защита от потери точности float)
        Assert.Equal(expectedDishCalories, dish.Calories, 3);
    }

    /// <summary>
    /// Генератор данных для параметризованного теста выше.
    /// </summary>
    public static IEnumerable<object[]> GetCalculationTestData()
    {
        // BVA (Граничные значения)
        // [Калорийность продукта на 100г, Вес в блюде, Ожидаемый результат]
        yield return new object[] { 100.0, 0.01, 0.01 };      // Минимально допустимый вес (0.01г по валидации)
        yield return new object[] { 0.0, 500.0, 0.0 };        // Продукт с 0 калорий (например, вода)
        yield return new object[] { 250.0, 100.0, 250.0 };    // Стандартный вес (100г), равен калорийности продукта
        yield return new object[] { 900.0, 999.99, 8999.91 }; // Большой вес и высокая калорийность (масло)
        
        // EP (Эквивалентное разбиение)
        yield return new object[] { 333.33, 150.0, 499.995 }; // Дробные значения КБЖУ (обычный продукт)
    }

    /// <summary>
    /// Эквивалентное разбиение (EP): Класс "Частичное заполнение".
    /// Если хотя бы один макрос 0, расчет идет, но перезаписываются ТОЛЬКО те макросы, которые равны 0.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_PartialNutrientsSet_OverridesOnlyZeroValues()
    {
        // Arrange
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

        // Act
        await _dishService.RecalculateDishPropertiesAsync(dish);

        // Assert
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
        Assert.Equal(expectedFlags, dish.Flags);
    }
}















/*
public class DishServiceTests
{
    private readonly Mock<IDishRepository> _dishRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly DishService _sut;

    public DishServiceTests()
    {
        _dishRepositoryMock = new Mock<IDishRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _sut = new DishService(_dishRepositoryMock.Object, _productRepositoryMock.Object);
    }

    #region Тесты для RecalculateDishPropertiesAsync

    /// <summary>
    /// Эквивалентный класс: отсутствие ингредиентов.
    /// Ожидаемое поведение: метод ничего не пересчитывает и не обращается к репозиторию.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_WhenNoIngredients_ShouldDoNothingAndNotCallRepository()
    {
        // Arrange
        var dish = new Dish { Ingredients = new List<Product>() };

        // Act
        await _sut.RecalculateDishPropertiesAsync(dish);

        // Assert
        _dishRepositoryMock.Verify(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
        Assert.Equal(0, dish.Calories);
        Assert.Equal(0, dish.Proteins);
        Assert.Equal(0, dish.Fats);
        Assert.Equal(0, dish.Carbohydrates);
        Assert.Equal((ProductFlags)0, dish.Flags);
    }

    /// <summary>
    /// Эквивалентный класс: все поля nutrients и flags уже заданы (не нули).
    /// Ожидание: early exit, репозиторий не вызывается.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_WhenAllNutrientsAndFlagsNonZero_ShouldSkipCalculation()
    {
        // Arrange
        var dish = new Dish
        {
            Calories = 100,
            Proteins = 10,
            Fats = 5,
            Carbohydrates = 20,
            Flags = ProductFlags.Vegan,
            Ingredients = new List<DishIngredient> { new DishIngredient { ProductId = Guid.NewGuid(), Amount = 100 } }
        };

        // Act
        await _sut.RecalculateDishPropertiesAsync(dish);

        // Assert
        _dishRepositoryMock.Verify(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
        Assert.Equal(100, dish.Calories);
        Assert.Equal(10, dish.Proteins);
        Assert.Equal(5, dish.Fats);
        Assert.Equal(20, dish.Carbohydrates);
        Assert.Equal(ProductFlags.Vegan, dish.Flags);
    }

    /// <summary>
    /// Граничное значение: количество ингредиентов = 1.
    /// Проверка правильности расчёта КБЖУ.
    /// </summary>
    [Theory]
    [InlineData(100, 200, 10, 20, 30, 40, 20, 40, 60, 80)] // amount=100, кбжу продукта на 100г
    [InlineData(50, 200, 10, 20, 30, 40, 10, 20, 30, 40)] // amount=50
    [InlineData(0, 200, 10, 20, 30, 40, 0, 0, 0, 0)]      // граница amount = 0
    public async Task RecalculateDishPropertiesAsync_SingleProduct_CalculatesCorrectly(
        double amount, double productCal, double productProt, double productFat, double productCarb,
        double expectedCal, double expectedProt, double expectedFat, double expectedCarb)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0, Proteins = 0, Fats = 0, Carbohydrates = 0, Flags = 0,
            Ingredients = new List<DishIngredient> { new DishIngredient { ProductId = productId, Amount = amount } }
        };
        var product = new Product
        {
            Id = productId,
            Calories = productCal,
            Proteins = productProt,
            Fats = productFat,
            Carbohydrates = productCarb,
            Flags = ProductFlags.Vegan | ProductFlags.GlutenFree
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new[] { product });

        // Act
        await _sut.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(expectedCal, dish.Calories, 2);
        Assert.Equal(expectedProt, dish.Proteins, 2);
        Assert.Equal(expectedFat, dish.Fats, 2);
        Assert.Equal(expectedCarb, dish.Carbohydrates, 2);
        // Флаги должны быть вычислены как пересечение флагов всех продуктов
        Assert.Equal(ProductFlags.Vegan | ProductFlags.GlutenFree, dish.Flags);
    }

    /// <summary>
    /// Эквивалентный класс: несколько ингредиентов.
    /// Проверка суммирования КБЖУ и побитового И флагов.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_MultipleProducts_CalculatesCorrectly()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0, Proteins = 0, Fats = 0, Carbohydrates = 0, Flags = 0,
            Products = new List<Product>
            {
                new Product { ProductId = product1Id, Amount = 100 },
                new Product { ProductId = product2Id, Amount = 200 }
            }
        };
        var product1 = new Product
        {
            Id = product1Id,
            Calories = 10, Proteins = 1, Fats = 2, Carbohydrates = 3,
            Flags = ProductFlags.Vegan | ProductFlags.GlutenFree
        };
        var product2 = new Product
        {
            Id = product2Id,
            Calories = 20, Proteins = 2, Fats = 4, Carbohydrates = 6,
            Flags = ProductFlags.Vegan | ProductFlags.SugarFree
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new[] { product1, product2 });

        // Act
        await _sut.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Ожидания: (10*100/100)+(20*200/100)=10+40=50 калорий и т.д.
        Assert.Equal(50, dish.Calories);
        Assert.Equal(1*100/100 + 2*200/100, dish.Proteins); // 1 + 4 = 5
        Assert.Equal(2*100/100 + 4*200/100, dish.Fats);     // 2 + 8 = 10
        Assert.Equal(3*100/100 + 6*200/100, dish.Carbohydrates); // 3 + 12 = 15
        // Общие флаги: Vegan (есть у обоих), GlutenFree нет у второго, SugarFree нет у первого → только Vegan
        Assert.Equal(ProductFlags.Vegan, dish.Flags);
    }

    /// <summary>
    /// Эквивалентный класс: продукт не найден в БД – такой ингредиент игнорируется.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_WhenProductNotFound_IgnoresThatProduct()
    {
        // Arrange
        var existingProductId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0, Proteins = 0, Fats = 0, Carbohydrates = 0, Flags = 0,
            Products = new List<Product>
            {
                new Product { ProductId = existingProductId, Amount = 100 },
                new Product { ProductId = missingProductId, Amount = 50 }
            }
        };
        var existingProduct = new Product
        {
            Id = existingProductId,
            Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 20,
            Flags = ProductFlags.Vegan
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new[] { existingProduct }); // missingProduct отсутствует

        // Act
        await _sut.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Учитывается только existingProduct
        Assert.Equal(100, dish.Calories);
        Assert.Equal(10, dish.Proteins);
        Assert.Equal(5, dish.Fats);
        Assert.Equal(20, dish.Carbohydrates);
        Assert.Equal(ProductFlags.Vegan, dish.Flags);
    }

    /// <summary>
    /// Граничные значения: часть полей nutrients уже ненулевые – они не должны перезаписываться.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_WhenSomeNutrientsNonZero_ShouldKeepThem()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 999,   // уже задано, не должно измениться
            Proteins = 0,     // будет вычислено
            Fats = 0,         // будет вычислено
            Carbohydrates = 0,// будет вычислено
            Flags = 0,
            Products = new List<Product> { new Product { ProductId = productId, Amount = 100 } }
        };
        var product = new Product
        {
            Id = productId,
            Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 20,
            Flags = ProductFlags.Vegan
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new[] { product });

        // Act
        await _sut.RecalculateDishPropertiesAsync(dish);

        // Assert
        Assert.Equal(999, dish.Calories);   // не изменилось
        Assert.Equal(10, dish.Proteins);    // пересчитано
        Assert.Equal(5, dish.Fats);
        Assert.Equal(20, dish.Carbohydrates);
        Assert.Equal(ProductFlags.Vegan, dish.Flags);
    }

    /// <summary>
    /// Эквивалентный класс: dish.Flags != 0 – не должен перезаписываться.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_WhenFlagsAlreadySet_ShouldNotOverwrite()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0, Proteins = 0, Fats = 0, Carbohydrates = 0,
            Flags = ProductFlags.GlutenFree, // уже установлен
            Products = new List<Product> { new Product { ProductId = productId, Amount = 100 } }
        };
        var product = new Product
        {
            Id = productId,
            Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 20,
            Flags = ProductFlags.Vegan // не содержит GlutenFree
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new[] { product });

        // Act
        await _sut.RecalculateDishPropertiesAsync(dish);

        // Assert
        // Флаги не должны измениться, даже если вычисленные не содержат GlutenFree
        Assert.Equal(ProductFlags.GlutenFree, dish.Flags);
    }

    /// <summary>
    /// Проверка комбинации флагов: все флаги присутствуют у всех продуктов.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_WhenAllFlagsPresent_CombinedFlagsContainsAll()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0, Proteins = 0, Fats = 0, Carbohydrates = 0, Flags = 0,
            Products = new List<Product> { new Product { ProductId = productId, Amount = 100 } }
        };
        var product = new Product
        {
            Id = productId,
            Calories = 100, Proteins = 10, Fats = 5, Carbohydrates = 20,
            Flags = ProductFlags.Vegan | ProductFlags.GlutenFree | ProductFlags.SugarFree
        };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new[] { product });

        await _sut.RecalculateDishPropertiesAsync(dish);
        Assert.Equal(ProductFlags.Vegan | ProductFlags.GlutenFree | ProductFlags.SugarFree, dish.Flags);
    }

    /// <summary>
    /// Проверка комбинации флагов: ни один флаг не присутствует у всех продуктов.
    /// </summary>
    [Fact]
    public async Task RecalculateDishPropertiesAsync_WhenNoCommonFlags_CombinedFlagsIsZero()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var dish = new Dish
        {
            Calories = 0, Proteins = 0, Fats = 0, Carbohydrates = 0, Flags = 0,
            Products = new List<Product>
            {
                new Product { ProductId = product1Id, Amount = 100 },
                new Product { ProductId = product2Id, Amount = 100 }
            }
        };
        var product1 = new Product { Id = product1Id, Flags = ProductFlags.Vegan };
        var product2 = new Product { Id = product2Id, Flags = ProductFlags.GlutenFree };
        _dishRepositoryMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new[] { product1, product2 });

        await _sut.RecalculateDishPropertiesAsync(dish);
        Assert.Equal((ProductFlags)0, dish.Flags);
    }

    #endregion

    // При необходимости можно добавить тесты для ValidateNutrients,
    // но по условию достаточно покрыть расчёт калорийности.
}
*/



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

    [Fact]
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
    }

    [Fact]
    public async Task CreateAsync_ExplicitCategory_ShouldOverrideMacro()
    {
        // Arrange
        var dish = new Dish { Name = "!десерт Стейк", Category = DishCategory.Second };

        // Act
        await _dishService.CreateAsync(dish, isCategoryExplicitlySet: true);

        // Assert
        Assert.Equal("Стейк", dish.Name);
        Assert.Equal(DishCategory.Second, dish.Category); // Приоритет ручного ввода
    }

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
}*/