using Testing_Practice.Domain.Models;

namespace Testing_Practice.Infrastructure.Persistence.Contexts;

using Microsoft.EntityFrameworkCore;
using Testing_Practice.Domain.Models;

public class RecipesDbContext : DbContext
{
    public RecipesDbContext(DbContextOptions<RecipesDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Dish> Dishes { get; set; }
    public DbSet<DishIngredient> DishIngredients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            
            entity.Property(p => p.Category).HasConversion<string>();
            entity.Property(p => p.CookingRequirement).HasConversion<string>();
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            entity.Property(d => d.Category).HasConversion<string>();
        });

        modelBuilder.Entity<DishIngredient>(entity =>
        {
            entity.HasKey(di => di.Id);

            entity.HasOne(di => di.Dish)
                .WithMany(d => d.Ingredients)
                .HasForeignKey(di => di.DishId);

            entity.HasOne(di => di.Product)
                .WithMany()
                .HasForeignKey(di => di.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}