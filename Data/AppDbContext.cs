using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Tasks)
            .WithOne(t => t.Project!)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);

        // Seed minimal data to help with testing
        modelBuilder.Entity<Project>().HasData(
            new Project { Id = 1, Name = "Challenge XPTO", Description = "Projeto exemplo seed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem { Id = 1, Title = "Configurar ambiente", Status = WorkStatus.Todo, ProjectId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Id = 2, Title = "Criar endpoints CRUD", Status = WorkStatus.InProgress, ProjectId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Project p)
            {
                if (entry.State == EntityState.Added) p.CreatedAt = DateTime.UtcNow;
                p.UpdatedAt = DateTime.UtcNow;
            }
            if (entry.Entity is TaskItem t)
            {
                if (entry.State == EntityState.Added) t.CreatedAt = DateTime.UtcNow;
                t.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
