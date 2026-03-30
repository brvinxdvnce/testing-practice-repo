using Microsoft.AspNetCore.Mvc;
using Testing_Practice.Application.Services.Interfaces;
using Testing_Practice.Domain.Enums;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Repositories;
using Testing_Practice.DTOs;

namespace Testing_Practice.Controllers;


[ApiController]
[Route("api/dishes")]
public class DishesController : ControllerBase
{
    private readonly IDishService _dishService;
    private readonly IDishRepository _dishRepository;

    public DishesController(IDishService dishService, IDishRepository dishRepository)
    {
        _dishService = dishService;
        _dishRepository = dishRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Dish>> Create(DishCreateDto dto)
    {
        var dish = new Dish
        {
            Name = dto.Name,
            PortionSize = dto.PortionSize,
            Photos = dto.Photos,
            // Если категория в DTO передана, используем её
            Category = dto.Category ?? DishCategory.Snack, 
            Ingredients = dto.Ingredients.Select(i => new DishIngredient 
            { 
                ProductId = i.ProductId, 
                Amount = i.Amount 
            }).ToList()
        };

        // Флаг: была ли категория выбрана пользователем вручную (для логики макросов)
        bool isCategoryExplicitlySet = dto.Category.HasValue;

        await _dishService.CreateAsync(dish, isCategoryExplicitlySet);

        // Если пользователь передал свои значения КБЖУ, перекрываем расчетные (Требование 2.2)
        if (dto.Calories.HasValue) dish.Calories = dto.Calories.Value;
        if (dto.Proteins.HasValue) dish.Proteins = dto.Proteins.Value;
        if (dto.Fats.HasValue) dish.Fats = dto.Fats.Value;
        if (dto.Carbohydrates.HasValue) dish.Carbohydrates = dto.Carbohydrates.Value;

        return CreatedAtAction(nameof(GetById), new { id = dish.Id }, dish);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Dish>> GetById(Guid id)
    {
        var dish = await _dishRepository.GetByIdAsync(id);
        if (dish == null) return NotFound();
        return Ok(dish);
    }
}