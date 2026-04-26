/*using System.Net;
using System.Net.Http.Json;
using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.DTOs;
using Testing_Practice.Tests.IntegrationTests.Fixtures;
using Testing_Practice.Tests.IntegrationTests.Utils;

namespace Testing_Practice.Tests.IntegrationTests.ControllersTests;

[Collection("IntegrationTests")]
public class DishesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public DishesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region GET /api/dishes - GetAll

    [Theory]
    [InlineData("name", false)]          // допустимый sortBy, по возрастанию
    [InlineData("calories", true)]       // допустимый, по убыванию
    [InlineData("invalid", false)]       // недопустимый – сервер должен вернуть 400 или игнорировать
    [InlineData("", false)]              // пустая строка – должно работать как отсутствие сортировки
    [Trait("EQ", "sortBy")]
    [Trait("BVA", "границы перечисления")]
    public async Task GetAll_WithSortBy_ReturnsOk(string sortBy, bool sortDesc)
    {
        // Act
        var response = await _client.GetAsync($"/api/dishes?sortBy={sortBy}&sortDesc={sortDesc}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        Assert.NotNull(dishes);
    }

    [Fact]
    [Trait("EQ", "category")]
    public async Task GetAll_WithCategoryFilter_ReturnsFiltered()
    {
        // Arrange – создаём несколько блюд с разными категориями
        var secondCourse = TestDataBuilder.CreateValidDishDto(category: DishCategory.Second);
        var soup = TestDataBuilder.CreateValidDishDto(category: DishCategory.Soup);
        await CreateDishAsync(secondCourse);
        await CreateDishAsync(soup);
        
        // Act
        var response = await _client.GetAsync("/api/dishes?category=Second");
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        
        // Assert
        Assert.All(dishes, d => Assert.Equal(DishCategory.Second, d.Category));
    }

    #endregion

    #region PUT /api/dishes/{id} - Update

    [Fact]
    [Trait("BVA", "Photos > 5")]
    public async Task Update_WithMoreThan5Photos_ReturnsBadRequest()
    {
        // Arrange
        var dish = await CreateDishAsync(TestDataBuilder.CreateValidDishDto());
        var updateDto = TestDataBuilder.CreateValidUpdateDto();
        updateDto.Photos = Enumerable.Range(1, 6).Select(i => $"photo{i}.jpg").ToList();
        
        // Act
        var response = await _client.PutAsJsonAsync($"/api/dishes/{dish.Id}", updateDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("превышен лимит", error);
    }

    [Theory]
    [InlineData(0, true)]   // граничное значение – порция 0 (ожидается ошибка валидации)
    [InlineData(1, false)]  // минимальная допустимая порция
    [InlineData(10000, false)] // большая допустимая порция
    [Trait("BVA", "PortionSize")]
    public async Task Update_WithPortionSize_RespectsBoundaries(int portionSize, bool shouldFail)
    {
        var dish = await CreateDishAsync(TestDataBuilder.CreateValidDishDto());
        var updateDto = TestDataBuilder.CreateValidUpdateDto();
        updateDto.PortionSize = portionSize;
        
        var response = await _client.PutAsJsonAsync($"/api/dishes/{dish.Id}", updateDto);
        
        if (shouldFail)
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        else
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region POST /api/dishes - Create

    [Fact]
    [Trait("EQ", "Calories override")]
    public async Task Create_WhenCaloriesProvided_DoesNotRecalculate()
    {
        // Arrange
        var dto = TestDataBuilder.CreateValidDishDto(calories: 999, proteins: 50);
        
        // Act
        var createdDish = await CreateDishAsync(dto);
        
        // Assert
        Assert.Equal(999, createdDish.Calories);
        Assert.Equal(50, createdDish.Proteins);
    }

    [Fact]
    [Trait("EQ", "Calories null")]
    public async Task Create_WhenCaloriesNull_RecalculatesFromIngredients()
    {
        // Arrange – ингредиенты: 150г курицы (165 ккал/100г) + 50г риса (130 ккал/100г)
        // Ожидаемые калории = (150*165/100)+(50*130/100) = 247.5 + 65 = 312.5
        var dto = TestDataBuilder.CreateValidDishDto(
            calories: null, proteins: null, fats: null, carbohydrates: null);
        
        // Act
        var createdDish = await CreateDishAsync(dto);
        
        // Assert – используем double с допуском из-за возможных округлений
        Assert.Equal(312.5, createdDish.Calories, 0.001);
        Assert.Equal(47.85, createdDish.Proteins, 0.001); // 31*1.5 + 2.7*0.5 = 46.5 + 1.35 = 47.85
    }

    #endregion

    #region DELETE /api/dishes/{id}

    [Fact]
    public async Task Delete_ExistingDish_ReturnsNoContent()
    {
        var dish = await CreateDishAsync(TestDataBuilder.CreateValidDishDto());
        
        var response = await _client.DeleteAsync($"/api/dishes/{dish.Id}");
        
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Проверяем, что блюдо действительно удалено
        var getResponse = await _client.GetAsync($"/api/dishes/{dish.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    [Trait("BVA", "несуществующий GUID")]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/dishes/{nonExistingId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task<Dish> CreateDishAsync(DishCreateDto dto)
    {
        var response = await _client.PostAsJsonAsync("/api/dishes", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Dish>();
    }

    #endregion
}

/*
[Collection("IntegrationTests")]
public class DishesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public DishesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region GET /api/dishes - GetAll

    [Theory]
    [InlineData("name", false)]          // допустимый sortBy, по возрастанию
    [InlineData("calories", true)]       // допустимый, по убыванию
    [InlineData("invalid", false)]       // недопустимый – сервер должен вернуть 400 или игнорировать
    [InlineData("", false)]              // пустая строка – должно работать как отсутствие сортировки
    [Trait("EQ", "sortBy")]
    [Trait("BVA", "границы перечисления")]
    public async Task GetAll_WithSortBy_ReturnsOk(string sortBy, bool sortDesc)
    {
        // Act
        var response = await _client.GetAsync($"/api/dishes?sortBy={sortBy}&sortDesc={sortDesc}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        Assert.NotNull(dishes);
        // Дополнительно можно проверить порядок (при корректной реализации сервиса)
    }

    [Fact]
    [Trait("EQ", "category")]
    public async Task GetAll_WithCategoryFilter_ReturnsFiltered()
    {
        // Arrange – создаём несколько блюд с разными категориями
        var hotDish = TestDataBuilder.CreateValidDishDto(category: DishCategory.HotMeal);
        var soup = TestDataBuilder.CreateValidDishDto(category: DishCategory.Soup);
        await CreateDishAsync(hotDish);
        await CreateDishAsync(soup);
        
        // Act
        var response = await _client.GetAsync("/api/dishes?category=HotMeal");
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        
        // Assert
        Assert.All(dishes, d => Assert.Equal(DishCategory.HotMeal, d.Category));
    }

    #endregion

    #region PUT /api/dishes/{id} - Update

    [Fact]
    [Trait("BVA", "Photos > 5")]
    public async Task Update_WithMoreThan5Photos_ReturnsBadRequest()
    {
        // Arrange
        var dish = await CreateDishAsync(TestDataBuilder.CreateValidDishDto());
        var updateDto = TestDataBuilder.CreateValidUpdateDto();
        updateDto.Photos = Enumerable.Range(1, 6).Select(i => $"photo{i}.jpg").ToList();
        
        // Act
        var response = await _client.PutAsJsonAsync($"/api/dishes/{dish.Id}", updateDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("превышен лимит", error);
    }

    [Theory]
    [InlineData(0, true)]   // граничное значение – порция 0 (ожидается ошибка валидации)
    [InlineData(1, false)]  // минимальная допустимая порция
    [InlineData(10000, false)] // большая допустимая порция
    [Trait("BVA", "PortionSize")]
    public async Task Update_WithPortionSize_RespectsBoundaries(int portionSize, bool shouldFail)
    {
        var dish = await CreateDishAsync(TestDataBuilder.CreateValidDishDto());
        var updateDto = TestDataBuilder.CreateValidUpdateDto();
        updateDto.PortionSize = portionSize;
        
        var response = await _client.PutAsJsonAsync($"/api/dishes/{dish.Id}", updateDto);
        
        if (shouldFail)
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        else
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region POST /api/dishes - Create

    [Fact]
    [Trait("EQ", "Calories override")]
    public async Task Create_WhenCaloriesProvided_DoesNotRecalculate()
    {
        // Arrange
        var dto = TestDataBuilder.CreateValidDishDto();
        dto.Calories = 999;   // явно заданные калории
        dto.Proteins = 50;
        
        // Act
        var createdDish = await CreateDishAsync(dto);
        
        // Assert
        Assert.Equal(999, createdDish.Calories);
        Assert.Equal(50, createdDish.Proteins);
        // Маленькая проверка, что сервис не перезаписал переданные значения
    }

    [Fact]
    [Trait("EQ", "Calories null")]
    public async Task Create_WhenCaloriesNull_RecalculatesFromIngredients()
    {
        // Arrange – ингредиенты: 150г курицы (165 ккал/100г) + 50г риса (130 ккал/100г)
        // Ожидаемые калории = (150*165/100)+(50*130/100) = 247.5 + 65 = 312.5
        var dto = TestDataBuilder.CreateValidDishDto();
        dto.Calories = null;
        dto.Proteins = null;
        dto.Fats = null;
        dto.Carbohydrates = null;
        
        // Act
        var createdDish = await CreateDishAsync(dto);
        
        // Assert
        Assert.Equal(312.5m, createdDish.Calories);
        Assert.Equal(31*1.5m + 2.7m*0.5m, createdDish.Proteins); // 46.5+1.35=47.85
    }

    #endregion

    #region DELETE /api/dishes/{id}

    [Fact]
    public async Task Delete_ExistingDish_ReturnsNoContent()
    {
        var dish = await CreateDishAsync(TestDataBuilder.CreateValidDishDto());
        
        var response = await _client.DeleteAsync($"/api/dishes/{dish.Id}");
        
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Проверяем, что блюдо действительно удалено
        var getResponse = await _client.GetAsync($"/api/dishes/{dish.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    [Trait("BVA", "несуществующий GUID")]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/dishes/{nonExistingId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task<Dish> CreateDishAsync(DishCreateDto dto)
    {
        var response = await _client.PostAsJsonAsync("/api/dishes", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Dish>();
    }

    #endregion
}#1#*/