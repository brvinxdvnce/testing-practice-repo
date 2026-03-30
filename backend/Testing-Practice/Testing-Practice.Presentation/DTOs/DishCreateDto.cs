using Testing_Practice.Domain.Enums;

namespace Testing_Practice.DTOs;

public record DishCreateDto(
    string Name, List<string>? Photos,
    double PortionSize, 
    DishCategory? Category,
    List<IngredientDto> Ingredients,
    double? Calories,
    double? Proteins,
    double? Fats,
    double? Carbohydrates);