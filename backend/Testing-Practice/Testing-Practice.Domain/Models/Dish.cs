using Testing_Practice.Domain.Enums;

namespace Testing_Practice.Domain.Models;

using System.ComponentModel.DataAnnotations;

public class Dish
{
    [Key]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Название блюда обязательно")]
    [MinLength(2, ErrorMessage = "Минимальная длина — 2 символа")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5, ErrorMessage = "Максимальное количество фотографий — 5")]
    public List<string>? Photos { get; set; } = new List<string>();

    [Required]
    [Range(0, double.MaxValue)]
    public double Calories { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public double Proteins { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public double Fats { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public double Carbohydrates { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Размер порции должен быть больше 0")]
    public double PortionSize { get; set; }

    [Required]
    public DishCategory Category { get; set; }

    [Required]
    public List<DishIngredient> Ingredients { get; set; } = new List<DishIngredient>();

    public ProductFlags Flags { get; set; } = ProductFlags.None;

    [Required]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public void UpdateModifiedDate() => UpdatedAt = DateTime.UtcNow;
}
