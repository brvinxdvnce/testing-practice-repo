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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Dish>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] DishCategory? category,
        [FromQuery] ProductFlags? flags,
        [FromQuery] string? sortBy,      // "name", "calories", "proteins", "fats", "carbohydrates"
        [FromQuery] bool sortDesc = false)
    {
        var dishes = await _dishService.GetAllAsync(search, category, flags, sortBy, sortDesc);
        return Ok(dishes);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Dish>> Update(Guid id, DishUpdateDto dto)
    {
        var existing = await _dishRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        // Обновляем поля из DTO
        if (dto.Name != null) existing.Name = dto.Name;
        if (dto.PortionSize.HasValue) existing.PortionSize = dto.PortionSize.Value;
        if (dto.Photos != null) existing.Photos = dto.Photos;
        if (dto.Category.HasValue) existing.Category = dto.Category.Value;
        if (dto.Ingredients != null)
        {
            existing.Ingredients = dto.Ingredients.Select(i => new DishIngredient
            {
                ProductId = i.ProductId,
                Amount = i.Amount
            }).ToList();
        }

        bool isCategoryExplicitlySet = dto.Category.HasValue;

        await _dishService.UpdateAsync(existing, isCategoryExplicitlySet);

        // Перезаписываем ручными значениями КБЖУ, если переданы
        if (dto.Calories.HasValue) existing.Calories = dto.Calories.Value;
        if (dto.Proteins.HasValue) existing.Proteins = dto.Proteins.Value;
        if (dto.Fats.HasValue) existing.Fats = dto.Fats.Value;
        if (dto.Carbohydrates.HasValue) existing.Carbohydrates = dto.Carbohydrates.Value;

        if (dto.Flags.HasValue) existing.Flags = dto.Flags.Value;
        
        return Ok(existing);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _dishService.DeleteAsync(id);
        return NoContent();
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

        if (dto.Flags.HasValue) dish.Flags = dto.Flags.Value;
        
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