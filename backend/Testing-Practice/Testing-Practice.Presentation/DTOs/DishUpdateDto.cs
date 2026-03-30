using Testing_Practice.Domain.Enums;

namespace Testing_Practice.DTOs;

public class DishUpdateDto
{
    public string? Name { get; set; }
    public double? PortionSize { get; set; }
    public List<string>? Photos { get; set; }
    public DishCategory? Category { get; set; }
    public List<IngredientDto>? Ingredients { get; set; }
    public ProductFlags? Flags { get; set; }
    public double? Calories { get; set; }
    public double? Proteins { get; set; }
    public double? Fats { get; set; }
    public double? Carbohydrates { get; set; }
}