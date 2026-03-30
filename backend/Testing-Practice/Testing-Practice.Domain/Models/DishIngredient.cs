using System.ComponentModel.DataAnnotations;

namespace Testing_Practice.Domain.Models;

public class DishIngredient
{
    [Key]
    public Guid Id { get; set; }

    public Guid DishId { get; set; }
    public Dish Dish { get; set; } = null!;

    [Required]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Количество продукта должно быть больше 0")]
    public double Amount { get; set; }
}
