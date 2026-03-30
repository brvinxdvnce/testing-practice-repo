using Testing_Practice.Domain.Enums;

namespace Testing_Practice.DTOs;

// ProductUpdateDto.cs
public class ProductUpdateDto
{
    public string? Name { get; set; }
    public double? Calories { get; set; }
    public double? Proteins { get; set; }
    public double? Fats { get; set; }
    public double? Carbohydrates { get; set; }
    public ProductCategory? Category { get; set; }
    public string? Description { get; set; }
    public CookingRequirement? CookingRequirement { get; set; }
    public ProductFlags? Flags { get; set; }
    public List<string>? Photos { get; set; }
}