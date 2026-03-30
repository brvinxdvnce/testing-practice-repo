using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Testing_Practice.Application.Services.Implementations;
using Testing_Practice.Application.Services.Interfaces;
using Testing_Practice.Domain.Repositories;
using Testing_Practice.Infrastructure.Persistence.Contexts;
using Testing_Practice.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDishService, DishService>();

builder.Services.AddOpenApi();

builder.Services.AddControllers();

builder.Services.AddDbContext<RecipesDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
