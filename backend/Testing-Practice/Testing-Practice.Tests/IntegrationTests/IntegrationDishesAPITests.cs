using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Json;
using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.DTOs;
using Testing_Practice.Tests.IntegrationTests.Utils;

namespace Testing_Practice.Tests.IntegrationTests;

public partial class IntegrationTests : IClassFixture<IntegrationTestsFixture>
{

    // GET по заведомо несуществующему Guid > 404
    [Fact]
    public async Task Get_Dish_ByInvalidId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/dishes/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // GET по id > корректное тело ответа
    [Fact]
    public async Task Get_Dish_ById_ReturnsDishBody()
    {
        // 1. Создаём продукт
        var productDto = new ProductCreateDto(
            "Макароны", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var productResponse = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var product = await productResponse.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(product);
        TrackCreatedProductId(product!.Id);

        // 2. Создаём блюдо
        var dishDto = new DishCreateDto(
            "Миска с макаронами",
            null,
            PortionSize: 100,
            Category: null,
            Ingredients: new List<IngredientDto> { new IngredientDto(product.Id, 100) },
            Flags: null,
            Calories: null, Proteins: null, Fats: null, Carbohydrates: null
        );
        var createResponse = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(created);
        TrackCreatedDishId(created!.Id);

        // 3. Получаем блюдо по id
        var getResponse = await _client.GetAsync($"/api/dishes/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = await getResponse.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(body);
        TrackCreatedDishId(body!.Id);

        Assert.Equal(created.Id, body.Id);
        Assert.Equal(dishDto.Name, body.Name);
        Assert.Equal(dishDto.PortionSize, body.PortionSize);
        Assert.NotEmpty(body.Ingredients);
        Assert.Equal(product.Id, body.Ingredients[0].ProductId);
        Assert.Equal(100, body.Ingredients[0].Amount);
    }

    // GET список всех блюд, проверяем наличие созданного
    [Fact]
    public async Task Get_Dishes_List_ReturnsDishesBody()
    {
        var productDto = new ProductCreateDto(
            "Помидор", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var productResponse = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var product = await productResponse.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(product);
        TrackCreatedProductId(product!.Id);

        var dishDto = new DishCreateDto(
            "Салат с помидором",
            null,
            100,
            null,
            new List<IngredientDto> { new IngredientDto(product.Id, 100) },
            null, null, null, null, null
        );
        var createResponse = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var dish = await createResponse.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(dish);
        TrackCreatedDishId(dish!.Id);

        var listResponse = await _client.GetAsync("/api/dishes");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var body = await listResponse.Content.ReadFromJsonAsync<List<Dish>>(JsonOptions);
        Assert.NotNull(body);
        var found = body!.FirstOrDefault(d => d.Id == dish.Id);
        Assert.NotNull(found);
        Assert.Equal(dish.Name, found!.Name);
    }

    // Фильтр по категории
    [Fact]
    public async Task Get_Dishes_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        var productDto = new ProductCreateDto(
            "ФильтрБлюдПродукт", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var pResp = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, pResp.StatusCode);
        var product = await pResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(product);
        TrackCreatedProductId(product!.Id);

        // Салат
        var saladDto = new DishCreateDto(
            "ФильтрБлюдСалат", null, 100,
            Category: DishCategory.Salad,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var saladResp = await _client.PostAsJsonAsync("/api/dishes", saladDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, saladResp.StatusCode);
        var salad = await saladResp.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(salad);
        TrackCreatedDishId(salad!.Id);

        // Второе
        var secondDto = new DishCreateDto(
            "ФильтрБлюдВторое", null, 120,
            Category: DishCategory.Second,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var secondResp = await _client.PostAsJsonAsync("/api/dishes", secondDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, secondResp.StatusCode);
        var second = await secondResp.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(second);
        TrackCreatedDishId(second!.Id);

        var filterResponse = await _client.GetAsync($"/api/dishes?category={DishCategory.Salad}");
        Assert.Equal(HttpStatusCode.OK, filterResponse.StatusCode);
        var body = await filterResponse.Content.ReadFromJsonAsync<List<Dish>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains(body!, d => d.Id == salad.Id);
        Assert.DoesNotContain(body!, d => d.Id == second.Id);
    }

    // Поиск по названию
    [Fact]
    public async Task Get_Dishes_WithSearchFilter_ReturnsOnlyMatchingDish()
    {
        var productDto = new ProductCreateDto(
            "ПоискБлюдПродукт", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var pResp = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, pResp.StatusCode);
        var product = await pResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(product);
        TrackCreatedProductId(product!.Id);

        // Целевое блюдо с уникальным словом
        var targetDto = new DishCreateDto(
            "ПоискБлюдЦелевое", null, 100, null,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var tResp = await _client.PostAsJsonAsync("/api/dishes", targetDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, tResp.StatusCode);
        var target = await tResp.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(target);
        TrackCreatedDishId(target!.Id);

        // Фоновое блюдо с другим названием
        var otherDto = new DishCreateDto(
            "ПоискБлюдФон", null, 115, null,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var oResp = await _client.PostAsJsonAsync("/api/dishes", otherDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, oResp.StatusCode);
        var other = await oResp.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(other);
        TrackCreatedDishId(other!.Id);

        var searchResponse = await _client.GetAsync("/api/dishes?search=Целевое");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var body = await searchResponse.Content.ReadFromJsonAsync<List<Dish>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains(body!, d => d.Id == target.Id);
        Assert.DoesNotContain(body!, d => d.Id == other.Id);
    }

    // Успешное создание с разными допустимыми КБЖУ
    [Theory]
    [InlineData(0.1, 0.1, 0.1, 0.1)]
    [InlineData(120, 12, 6, 0.1)]
    [InlineData(100, 20, 10, 10)]
    public async Task Post_Dish_ReturnsCreatedBody(double calories, double proteins, double fats, double carbohydrates)
    {
        var productDto = new ProductCreateDto(
            "Яйцо", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var pResp = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, pResp.StatusCode);
        var product = await pResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(product);
        TrackCreatedProductId(product!.Id);

        var dishDto = new DishCreateDto(
            "Яичный перекус",
            new List<string> {  },
            PortionSize: 120,
            Category: DishCategory.Snack,
            Ingredients: new List<IngredientDto> { new(product.Id, 120) },
            Flags: null,
            Calories: calories,
            Proteins: proteins,
            Fats: fats,
            Carbohydrates: carbohydrates
        );

        var response = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        Assert.NotNull(body);
        TrackCreatedDishId(body!.Id);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(dishDto.Name, body.Name);
        Assert.Equal(calories, body.Calories, 2);
        Assert.Equal(proteins, body.Proteins, 2);
        Assert.Equal(fats, body.Fats, 2);
        Assert.Equal(carbohydrates, body.Carbohydrates, 2);
        Assert.Equal(120, body.PortionSize);
        Assert.Equal(DishCategory.Snack, body.Category);
        Assert.NotEmpty(body.Ingredients);
        Assert.Equal(product.Id, body.Ingredients[0].ProductId);
        Assert.Equal(120, body.Ingredients[0].Amount);
    }

    // Пустой состав > 400
    [Fact]
    public async Task Post_Dish_WithEmptyComposition_ReturnsBadRequest()
    {
        var dishDto = new DishCreateDto(
            "Пустое блюдо", null, 100,
            Category: DishCategory.Salad,
            Ingredients: new List<IngredientDto>(),
            Flags: null,
            Calories: 100, Proteins: 10, Fats: 5, Carbohydrates: 20
        );

        var response = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Слишком короткое имя > 400
    [Fact]
    public async Task Post_Dish_WithTooShortName_ReturnsBadRequest()
    {
        var productDto = new ProductCreateDto(
            "aaa", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None
        );
        var pResp = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, pResp.StatusCode);
        var product = await pResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        Assert.NotNull(product);
        TrackCreatedProductId(product!.Id);

        var dishDto = new DishCreateDto(
            "X", null, 100, null,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var response = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Неизвестный продукт > 400
    [Fact]
    public async Task Post_Dish_WithUnknownProduct_ReturnsBadRequest()
    {
        var dishDto = new DishCreateDto(
            "notfound", null, 100, null,
            new List<IngredientDto> { new(Guid.NewGuid(), 100) },
            null, null, null, null, null
        );
        var response = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

   
    // PUT с коротким именем > BadRequest
    [Fact]
    public async Task Put_Dish_WithTooShortName_ReturnsBadRequest()
    {
        var productDto = new ProductCreateDto("updShort", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None);
        var pResp = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        var product = await pResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        TrackCreatedProductId(product!.Id);

        var createDto = new DishCreateDto(
            "DishForShortUpdate", null, 100, null,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var cResp = await _client.PostAsJsonAsync("/api/dishes", createDto, JsonOptions);
        var dish = await cResp.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        TrackCreatedDishId(dish!.Id);

        var updateDto = new DishUpdateDto { Name = "X" };
        var putResponse = await _client.PutAsJsonAsync($"/api/dishes/{dish.Id}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
    }

    // Удаление блюда > 204 + последующий 404
    [Fact]
    public async Task Delete_Dish_ReturnsNoContentAndRemovesBody()
    {
        var productDto = new ProductCreateDto("Огурец", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None);
        var pResp = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        var product = await pResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        TrackCreatedProductId(product!.Id);

        var dishDto = new DishCreateDto(
            "Миска с огурцом", null, 100, null,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var cResp = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        var dish = await cResp.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        TrackCreatedDishId(dish!.Id);

        var delResp = await _client.DeleteAsync($"/api/dishes/{dish.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getAfter = await _client.GetAsync($"/api/dishes/{dish.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfter.StatusCode);
    }

    // Удаление продукта, который используется в блюде > 409 Conflict
    [Fact]
    public async Task Delete_Product_WhenUsedInDish_ReturnsConflict()
    {
        var productDto = new ProductCreateDto("Говядина", null, 100, 10, 5, 20, null,
            ProductCategory.Vegetables, CookingRequirement.ReadyToEat, ProductFlags.None);
        var pResp = await _client.PostAsJsonAsync("/api/products", productDto, JsonOptions);
        var product = await pResp.Content.ReadFromJsonAsync<Product>(JsonOptions);
        TrackCreatedProductId(product!.Id);

        var dishDto = new DishCreateDto(
            "Говяжий суп", null, 100, null,
            new List<IngredientDto> { new(product.Id, 100) },
            null, null, null, null, null
        );
        var dResp = await _client.PostAsJsonAsync("/api/dishes", dishDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, dResp.StatusCode);
        var dish = await dResp.Content.ReadFromJsonAsync<Dish>(JsonOptions);
        TrackCreatedDishId(dish!.Id);

        // Попытка удалить используемый продукт
        var delResp = await _client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.Conflict, delResp.StatusCode);
    }
}