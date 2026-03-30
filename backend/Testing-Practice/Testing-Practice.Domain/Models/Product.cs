using System.ComponentModel.DataAnnotations;
using Testing_Practice.Domain.Enums;

namespace Testing_Practice.Domain.Models;

public class Product
{
    [Key]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Название обязательно для заполнения")]
    [MinLength(2, ErrorMessage = "Минимальная длина названия — 2 символа")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5, ErrorMessage = "Максимальное количество фотографий — 5")]
    public List<string>? Photos { get; set; } = [];

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Калорийность не может быть отрицательной")]
    public double Calories { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Белки не могут быть отрицательными")]
    public double Proteins { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Жиры не могут быть отрицательными")]
    public double Fats { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Углеводы не могут быть отрицательными")]
    public double Carbohydrates { get; set; }

    public string? Description { get; set; }

    [Required]
    public ProductCategory Category { get; set; }

    [Required]
    public CookingRequirement CookingRequirement { get; set; }

    public ProductFlags Flags { get; set; } = ProductFlags.None;

    [Required]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; private set; }

    // Метод для обновления данных (чтобы управлять датой редактирования)
    public void UpdateModifiedDate()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
