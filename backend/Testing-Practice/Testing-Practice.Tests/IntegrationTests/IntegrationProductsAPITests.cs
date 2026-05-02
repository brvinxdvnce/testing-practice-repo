using Testing_Practice.Tests.IntegrationTests.Fixtures;

namespace Testing_Practice.Tests.IntegrationTests.Utils;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Testing_Practice.DTOs;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Enums;

public partial class IntegrationTests : IClassFixture<IntegrationTestsFixture>
{
    private static readonly List<Guid> CreatedProductIds = new();
    private static readonly List<Guid> CreatedDishIds = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public IntegrationTests(IntegrationTestsFixture fixture)
    {
        _client = fixture.HttpClient;
    }

    internal static void TrackCreatedProductId(Guid id)
    {
        if (id == Guid.Empty || CreatedProductIds.Contains(id)) return;
        CreatedProductIds.Add(id);
    }

    internal static void TrackCreatedDishId(Guid id)
    {
        if (id == Guid.Empty || CreatedDishIds.Contains(id)) return;
        CreatedDishIds.Add(id);
    }

    internal static async Task CleanupCreatedEntitiesAsync(HttpClient client)
    {
        var dishIds = CreatedDishIds.ToArray();
        var productIds = CreatedProductIds.ToArray();
        CreatedDishIds.Clear();
        CreatedProductIds.Clear();

        foreach (var dishId in dishIds)
            await client.DeleteAsync($"/api/dishes/{dishId}");

        foreach (var productId in productIds)
            await client.DeleteAsync($"/api/products/{productId}");
    }

    // GET по несуществующему id → 404
    [Fact]
    public async Task Get_Product_ByInvalidId_ReturnsNotFound()
    {
        var nonExistentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var response = await _client.GetAsync($"/api/products/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // GET по id → корректное тело
    [Fact]
    public async Task Get_Product_ById_ReturnsCorrectBody()
    {
        var createDto = new ProductCreateDto(
            Name: "ById",
            Photos: null,
            Calories: 210,
            Proteins: 20,
            Fats: 5,
            Carbohydrates: 30,
            Description: null,
            Category: ProductCategory.Meat,
            CookingRequirement: CookingRequirement.RequiresCooking,
            Flags: ProductFlags.GlutenFree
        );

        var createResponse = await _client.PostAsJsonAsync("/api/products", createDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(created);
        TrackCreatedProductId(created!.Id);

        var response = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(body);
        TrackCreatedProductId(body!.Id);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(createDto.Name, body.Name);
        Assert.Equal(createDto.Calories, body.Calories);
        Assert.Equal(createDto.Proteins, body.Proteins);
        Assert.Equal(createDto.Fats, body.Fats);
        Assert.Equal(createDto.Carbohydrates, body.Carbohydrates);
        Assert.Equal(createDto.Category, body.Category);
        Assert.Equal(createDto.CookingRequirement, body.CookingRequirement);
        Assert.Equal(createDto.Flags, body.Flags);
    }

    // Фильтр по категории
    [Fact]
    public async Task Get_Products_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        var vegeDto = new ProductCreateDto(
            "Овощ", null, 120, 5, 1, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var vegeResp = await _client.PostAsJsonAsync("/api/products", vegeDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, vegeResp.StatusCode);
        var vegetable = await vegeResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(vegetable);
        TrackCreatedProductId(vegetable!.Id);

        var meatDto = new ProductCreateDto(
            "Мясо", null, 220, 20, 12, 1, null,
            ProductCategory.Meat, CookingRequirement.RequiresCooking, ProductFlags.None
        );
        var meatResp = await _client.PostAsJsonAsync("/api/products", meatDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, meatResp.StatusCode);
        var meat = await meatResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(meat);
        TrackCreatedProductId(meat!.Id);

        var response = await _client.GetAsync($"/api/products?category={ProductCategory.Vegetables}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<Product>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains(body!, p => p.Id == vegetable.Id);
        Assert.DoesNotContain(body!, p => p.Id == meat.Id);
    }

    // Фильтр по флагам
    [Fact]
    public async Task Get_Products_WithFlagsFilter_ReturnsOnlyMatchingFlags()
    {
        var veganDto = new ProductCreateDto(
            "Веган", null, 140, 14, 4, 10, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.Vegan
        );
        var veganResp = await _client.PostAsJsonAsync("/api/products", veganDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, veganResp.StatusCode);
        var vegan = await veganResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(vegan);
        TrackCreatedProductId(vegan!.Id);

        var plainDto = new ProductCreateDto(
            "Обычный", null, 150, 15, 5, 12, null,
            ProductCategory.Meat, CookingRequirement.RequiresCooking, ProductFlags.None
        );
        var plainResp = await _client.PostAsJsonAsync("/api/products", plainDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, plainResp.StatusCode);
        var plain = await plainResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(plain);
        TrackCreatedProductId(plain!.Id);

        var response = await _client.GetAsync($"/api/products?flags={ProductFlags.Vegan}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<Product>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains(body!, p => p.Id == vegan.Id);
        Assert.DoesNotContain(body!, p => p.Id == plain.Id);
    }

    // Поиск по названию
    [Fact]
    public async Task Get_Products_WithSearchFilter_ReturnsOnlyMatchingProduct()
    {
        var targetDto = new ProductCreateDto(
            "Целевойррр", null, 130, 13, 3, 11, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var tResp = await _client.PostAsJsonAsync("/api/products", targetDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, tResp.StatusCode);
        var target = await tResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(target);
        TrackCreatedProductId(target!.Id);

        var otherDto = new ProductCreateDto(
            "ВВв", null, 210, 20, 5, 7, null,
            ProductCategory.Meat, CookingRequirement.RequiresCooking, ProductFlags.None
        );
        var oResp = await _client.PostAsJsonAsync("/api/products", otherDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, oResp.StatusCode);
        var other = await oResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(other);
        TrackCreatedProductId(other!.Id);

        var response = await _client.GetAsync("/api/products?search=Целевой");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<Product>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains(body!, p => p.Id == target.Id);
        Assert.DoesNotContain(body!, p => p.Id == other.Id);
    }

    // POST – некорректные значения (сумма БЖУ > 100 или отрицательные)
    [Theory]
    [InlineData(0, 40, 40, 30)]      // сумма 110 > 100
    [InlineData(-10, 10, 10, 10)]
    [InlineData(10, -10, 10, 10)]
    [InlineData(10, 10, -10, 10)]
    [InlineData(10, 10, 10, -10)]
    [InlineData(-0.1, -0.1, -0.1, -0.1)]
    public async Task Post_Product_WithInvalidData_ReturnsBadRequest(
        double calories, double proteins, double fats, double carbohydrates)
    {
        var dto = new ProductCreateDto(
            "Invalid", null,
            calories, proteins, fats, carbohydrates,
            null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );

        var response = await _client.PostAsJsonAsync("/api/products", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // POST – слишком короткое имя
    [Fact]
    public async Task Post_Product_WithTooShortName_ReturnsBadRequest()
    {
        var dto = new ProductCreateDto(
            "A", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );

        var response = await _client.PostAsJsonAsync("/api/products", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // POST – имя длиной 2 символа принимается
    [Fact]
    public async Task Post_Product_WithNameLengthTwo_ReturnsCreated()
    {
        var dto = new ProductCreateDto(
            "AB", null, 90, 9, 4, 12, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.Vegan
        );

        var response = await _client.PostAsJsonAsync("/api/products", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(body);
        TrackCreatedProductId(body!.Id);
        Assert.Equal(dto.Name, body.Name);
    }

    // POST – граничные значения КБЖУ
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1000, 0, 0, 0)]
    [InlineData(500, 100, 0, 0)]     // сумма ровно 100 — допустимо
    [InlineData(0.1, 0.1, 0.1, 0.1)]
    public async Task Post_Product_WithBoundaryValues_ReturnsCreated(
        double calories, double proteins, double fats, double carbohydrates)
    {
        var dto = new ProductCreateDto(
            "valid", null,
            calories, proteins, fats, carbohydrates,
            null,
            ProductCategory.Cereals, CookingRequirement.ReadyToEat, ProductFlags.SugarFree
        );

        var response = await _client.PostAsJsonAsync("/api/products", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(body);
        TrackCreatedProductId(body!.Id);
        Assert.True(body.Id != Guid.Empty);
        Assert.Equal(dto.Name, body.Name);
        Assert.Equal(dto.Calories, body.Calories);
        Assert.Equal(dto.Proteins, body.Proteins);
        Assert.Equal(dto.Fats, body.Fats);
        Assert.Equal(dto.Carbohydrates, body.Carbohydrates);
        Assert.Equal(dto.Category, body.Category);
        Assert.Equal(dto.CookingRequirement, body.CookingRequirement);
        Assert.Equal(dto.Flags, body.Flags);
    }

    // PUT – несуществующий продукт → 404
    [Fact]
    public async Task Put_Product_WithInvalidId_ReturnsNotFound()
    {
        var updateDto = new ProductUpdateDto { Name = "aaaa" };
        var nonExistentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var response = await _client.PutAsJsonAsync($"/api/products/{nonExistentId}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE – несуществующий продукт → 404
    [Fact]
    public async Task Delete_Product_ByInvalidId_ReturnsNotFound()
    {
        var nonExistentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var response = await _client.DeleteAsync($"/api/products/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // PUT – полное обновление с проверкой сохранения в БД
    [Fact]
    public async Task Put_Product_WithBoundaryValues_ReturnsOkAndPersistsUpdate()
    {
        var createDto = new ProductCreateDto(
            "Old", null, 120, 12, 3, 10, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var createResp = await _client.PostAsJsonAsync("/api/products", createDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(created);
        TrackCreatedProductId(created!.Id);

        var updateDto = new ProductUpdateDto
        {
            Name = "Updated",
            Calories = 1000,
            Proteins = 100,
            Fats = 0,
            Carbohydrates = 0,
            Category = ProductCategory.Sweets,
            CookingRequirement = CookingRequirement.RequiresCooking,
            Flags = ProductFlags.Vegan | ProductFlags.GlutenFree,
            Photos = new List<string> { "photo1.jpg" },
            Description = "New description"
        };

        var updateResp = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var body = await getResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(body);
        TrackCreatedProductId(body!.Id);
        Assert.Equal(updateDto.Name, body.Name);
        Assert.Equal(updateDto.Calories, body.Calories);
        Assert.Equal(updateDto.Proteins, body.Proteins);
        Assert.Equal(updateDto.Fats, body.Fats);
        Assert.Equal(updateDto.Carbohydrates, body.Carbohydrates);
        Assert.Equal(updateDto.Category, body.Category);
        Assert.Equal(updateDto.CookingRequirement, body.CookingRequirement);
        Assert.Equal(updateDto.Flags, body.Flags);
        Assert.Equal(updateDto.Description, body.Description);
        Assert.NotNull(body.Photos);
        Assert.Single(body.Photos);
    }

    // PUT – слишком короткое имя → 400
    [Fact]
    public async Task Put_Product_WithTooShortName_ReturnsBadRequest()
    {
        var createDto = new ProductCreateDto(
            "sasasa", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var createResp = await _client.PostAsJsonAsync("/api/products", createDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(created);
        TrackCreatedProductId(created!.Id);

        var updateDto = new ProductUpdateDto { Name = "X" };
        var updateResp = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);
    }

    // DELETE – успешное удаление и последующая проверка
    [Fact]
    public async Task Delete_Product_ReturnsNoContentAndThenNotFound()
    {
        var createDto = new ProductCreateDto(
            "Delete", null, 130, 13, 4, 10, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );

        var createResp = await _client.PostAsJsonAsync("/api/products", createDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(created);
        TrackCreatedProductId(created!.Id);

        var deleteResp = await _client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getDeletedResp = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResp.StatusCode);
    }
}

public sealed class IntegrationTestsFixture : IDisposable
{
    internal HttpClient HttpClient;

    public IntegrationTestsFixture()
    {
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5006")
        };
    }
    
    public void Dispose()
    {
        IntegrationTests.CleanupCreatedEntitiesAsync(HttpClient).GetAwaiter().GetResult();
        HttpClient.Dispose();
    }
}