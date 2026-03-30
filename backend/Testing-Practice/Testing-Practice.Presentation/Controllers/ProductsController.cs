using Testing_Practice.Application.Services.Interfaces;
using Testing_Practice.Domain.Repositories;

namespace Testing_Practice.Controllers;

using Microsoft.AspNetCore.Mvc;
using Testing_Practice.Domain.Models;
using Testing_Practice.Domain.Enums;
using Testing_Practice.DTOs;


[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductRepository _productRepository;

    public ProductsController(IProductService productService, IProductRepository productRepository)
    {
        _productService = productService;
        _productRepository = productRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll(
        [FromQuery] string? search, [FromQuery] ProductCategory? category, 
        [FromQuery] CookingRequirement? cooking, [FromQuery] ProductFlags? flags,
        [FromQuery] string? sortBy = "name")
    {
        var products = await _productRepository.GetAllAsync(search, category, cooking, flags, sortBy);
        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(ProductCreateDto dto)
    {
        var product = new Product 
        { 
            Name = dto.Name, Calories = dto.Calories, Proteins = dto.Proteins,
            Fats = dto.Fats, Carbohydrates = dto.Carbohydrates, Category = dto.Category,
            Description = dto.Description, CookingRequirement = dto.CookingRequirement,
            Flags = dto.Flags, Photos = dto.Photos
        };

        await _productService.CreateAsync(product);
        return CreatedAtAction(nameof(GetAll), new { id = product.Id }, product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try 
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}