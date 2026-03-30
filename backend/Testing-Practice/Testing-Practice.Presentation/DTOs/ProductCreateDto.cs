using Testing_Practice.Domain.Enums;

namespace Testing_Practice.DTOs;

public record ProductCreateDto(
        string Name,
        List<string>? Photos,
        double Calories,
        double Proteins, 
        double Fats,
        double Carbohydrates,
        string? Description, 
        ProductCategory Category,
        CookingRequirement CookingRequirement, 
        ProductFlags Flags);
