namespace Testing_Practice.Tests.UnitTests;

public partial class DishCalculatorServiceTests
{
    // ============================
    // 1. Обработка отсутствующих продуктов
    // ============================

    /// <summary>
    /// Если продукт не найден в репозитории, он должен игнорироваться
    /// (или выбрасываться исключение в зависимости от бизнес-логики).
    /// Текущая реализация игнорирует null – это тестируемое поведение.
    /// </summary>
    [Fact]
    public void CalculateMacros_ProductNotFound_SkipsIngredient()
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

        _productRepositoryMock.Setup(r => r.GetById(existingProductId)).Returns(existingProduct);
        _productRepositoryMock.Setup(r => r.GetById(missingProductId)).Returns((Product)null!);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        // Вклад отсутствующего продукта не добавляется, только существующий (100 ккал)
        Assert.Equal(100, dish.Calories, 2);
        Assert.Equal(10, dish.Proteins, 2);
        Assert.Equal(5, dish.Fats, 2);
        Assert.Equal(20, dish.Carbohydrates, 2);
    }

    // ============================
    // 2. Пустой состав
    // ============================

    /// <summary>
    /// Если состав блюда пуст, все макронутриенты должны быть обнулены.
    /// (По ТЗ минимальное количество записей в составе = 1, но в юнит-тесте проверяем поведение)
    /// </summary>
    [Fact]
    public void CalculateMacros_EmptyIngredients_ReturnsZero()
    {
        // Arrange
        var dish = new Dish { Ingredients = new List<DishIngredient>() };

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        Assert.Equal(0, dish.Calories, 2);
        Assert.Equal(0, dish.Proteins, 2);
        Assert.Equal(0, dish.Fats, 2);
        Assert.Equal(0, dish.Carbohydrates, 2);
    }

    // ============================
    // 3. Максимальные значения БЖУ продуктов (граничные)
    // ============================

    /// <summary>
    /// Проверка расчёта при максимально допустимых значениях БЖУ продукта (100).
    /// Применяется анализ граничных значений (BVA) – верхняя граница.
    /// </summary>
    [Fact]
    public void CalculateMacros_ProductWithMaxNutrients_ReturnsCorrect()
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
        _productRepositoryMock.Setup(r => r.GetById(product.Id)).Returns(product);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        // 75 г = 0.75 от 100 г → каждый показатель умножается на 0.75
        Assert.Equal(75, dish.Calories, 2);
        Assert.Equal(75, dish.Proteins, 2);
        Assert.Equal(75, dish.Fats, 2);
        Assert.Equal(75, dish.Carbohydrates, 2);
    }

    // ============================
    // 4. Сумма БЖУ продукта > 100 (хотя по ТЗ такого быть не должно)
    // ============================

    /// <summary>
    /// Если данные продукта некорректны (сумма БЖУ > 100), расчёт всё равно должен выполняться
    /// корректно как арифметическая операция.
    /// </summary>
    [Fact]
    public void CalculateMacros_ProductWithSumGreaterThan100_CalculatesCorrectly()
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
        _productRepositoryMock.Setup(r => r.GetById(product.Id)).Returns(product);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        // Коэффициент = 200 / 100 = 2
        Assert.Equal(400, dish.Calories, 2);
        Assert.Equal(160, dish.Proteins, 2);
        Assert.Equal(100, dish.Fats, 2);
        Assert.Equal(80, dish.Carbohydrates, 2);
    }

    // ============================
    // 5. Проверка накопления погрешности при большом количестве ингредиентов
    // ============================

    /// <summary>
    /// При расчёте с несколькими ингредиентами суммарная погрешность не должна превышать
    /// разумный порог (используется допуск 1e-9, но для демонстрации – 0.001).
    /// </summary>
    [Fact]
    public void CalculateMacros_ManyIngredients_PrecisionIsMaintained()
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

        _productRepositoryMock.Setup(r => r.GetById(product.Id)).Returns(product);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        // Ожидаемое значение для каждого макронутриента: (33.333... * 1/100) * 100 = 33.333...
        double expected = 33.3333333333; // теоретическое значение
        Assert.Equal(expected, dish.Calories, 9);
        Assert.Equal(expected, dish.Proteins, 9);
        Assert.Equal(expected, dish.Fats, 9);
        Assert.Equal(expected, dish.Carbohydrates, 9);
    }

    // ============================
    // 6. Неизменность входных объектов (отсутствие побочных эффектов)
    // ============================

    /// <summary>
    /// Метод CalculateMacros не должен изменять объекты продуктов, полученные из репозитория.
    /// </summary>
    [Fact]
    public void CalculateMacros_DoesNotModifyProductObjects()
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
        _productRepositoryMock.Setup(r => r.GetById(productId)).Returns(originalProduct);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        // Сравниваем все поля продукта с исходными значениями
        Assert.Equal(100, originalProduct.Calories);
        Assert.Equal(10, originalProduct.Proteins);
        Assert.Equal(5, originalProduct.Fats);
        Assert.Equal(20, originalProduct.Carbohydrates);
    }

    // ============================
    // 7. Граничные значения: очень малые количества (BVA)
    // ============================

    /// <summary>
    /// Проверка расчёта при минимально возможном положительном количестве (0.0001 г).
    /// </summary>
    [Theory]
    [InlineData(0.0001, 0.0001)] // 0.0001 г * 100 ккал/100г = 0.0001 ккал
    [InlineData(0.00001, 0.00001)]
    public void CalculateMacros_VerySmallAmount_ReturnsProportionalValue(double amount, double expectedCalories)
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
        _productRepositoryMock.Setup(r => r.GetById(product.Id)).Returns(product);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        Assert.Equal(expectedCalories, dish.Calories, 10);
    }

    // ============================
    // 8. Отрицательные значения БЖУ (эквивалентное разбиение – недопустимые данные)
    // ============================

    /// <summary>
    /// Если у продукта отрицательные показатели КБЖУ (например, ошибка данных),
    /// расчёт должен корректно отработать и дать отрицательные значения.
    /// </summary>
    [Fact]
    public void CalculateMacros_NegativeNutrients_ReturnsNegativeValues()
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
        _productRepositoryMock.Setup(r => r.GetById(product.Id)).Returns(product);

        // Act
        _calculatorService.CalculateMacros(dish);

        // Assert
        Assert.Equal(-200, dish.Calories, 2);
        Assert.Equal(-20, dish.Proteins, 2);
        Assert.Equal(-10, dish.Fats, 2);
        Assert.Equal(-40, dish.Carbohydrates, 2);
    }
}