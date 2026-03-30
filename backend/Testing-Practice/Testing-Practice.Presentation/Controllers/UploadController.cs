using Microsoft.AspNetCore.Mvc;

namespace Testing_Practice.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("dish")]
    public async Task<IActionResult> UploadDishImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");
    
        // Проверка MIME-типа
        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("Файл должен быть изображением");
    
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest("Недопустимый формат файла");
    
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("Файл не должен превышать 5 МБ");
    
        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadPath = Path.Combine(_env.WebRootPath, "images", "dishes");
        Directory.CreateDirectory(uploadPath);
        var filePath = Path.Combine(uploadPath, fileName);
    
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
    
        var url = $"/images/dishes/{fileName}";
        return Ok(new { url });
    }
}