using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.DTOs;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS (liberado para dev)
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Error handling: ProblemDetails minimal
app.UseExceptionHandler(a => a.Run(async context =>
{
    var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var problem = Results.Problem(
        detail: ex?.Message,
        title: "Erro inesperado",
        statusCode: StatusCodes.Status500InternalServerError
    );
    await problem.ExecuteAsync(context);
}));

// Apply migrations / create db automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var hasAnyMigration = (await db.Database.GetAppliedMigrationsAsync()).Any()
                          || (await db.Database.GetPendingMigrationsAsync()).Any();

    if (hasAnyMigration)
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        // Cria o schema se não houver migrações
        await db.Database.EnsureCreatedAsync();

        // Seed manual quando EnsureCreated é usado (HasData só aplica via migrations)
        if (!await db.Projects.AnyAsync())
        {
            var p = new TaskManager.Api.Models.Project
            {
                Name = "Challenge XPTO",
                Description = "Projeto exemplo seed"
            };
            db.Projects.Add(p);
            await db.SaveChangesAsync();

            db.Tasks.AddRange(
                new TaskManager.Api.Models.TaskItem { Title = "Configurar ambiente", Status = TaskManager.Api.Models.WorkStatus.Todo, ProjectId = p.Id },
                new TaskManager.Api.Models.TaskItem { Title = "Criar endpoints CRUD", Status = TaskManager.Api.Models.WorkStatus.InProgress, ProjectId = p.Id }
            );
            await db.SaveChangesAsync();
        }
    }
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Redirect("/swagger"));

// Group routes
var projects = app.MapGroup("/api/projects").WithTags("Projects");
var tasks = app.MapGroup("/api/tasks").WithTags("Tasks");

/************** PROJECTS CRUD **************/

// List all projects (with tasks, paging)
projects.MapGet("/", async (AppDbContext db, int page = 1, int pageSize = 20) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    var query = db.Projects.Include(p => p.Tasks).OrderByDescending(p => p.Id);
    var total = await query.CountAsync();
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return Results.Ok(new { total, page, pageSize, items });
});

// Get by id
projects.MapGet("/{id:int}", async (int id, AppDbContext db) =>
{
    var project = await db.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id);
    return project is null ? Results.NotFound() : Results.Ok(project);
});

// Create
projects.MapPost("/", async (ProjectCreateDto input, AppDbContext db) =>
{
    var project = new Project { Name = input.Name, Description = input.Description };
    db.Projects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/projects/{project.Id}", project);
});

// Update
projects.MapPut("/{id:int}", async (int id, ProjectUpdateDto input, AppDbContext db) =>
{
    var project = await db.Projects.FindAsync(id);
    if (project is null) return Results.NotFound();

    project.Name = input.Name;
    project.Description = input.Description;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Delete (cascade deletes tasks)
projects.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var project = await db.Projects.FindAsync(id);
    if (project is null) return Results.NotFound();
    db.Remove(project);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

/************** TASKS CRUD **************/

// List tasks with filters (projectId, status, search, ordering, paging)
tasks.MapGet("/", async (AppDbContext db,
    int? projectId,
    WorkStatus? status,
    string? search,
    string? orderBy,
    int page = 1,
    int pageSize = 20) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    var query = db.Tasks.AsQueryable();

    if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
    if (status.HasValue) query = query.Where(t => t.Status == status.Value);
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(t => t.Title.Contains(search) || (t.Notes != null && t.Notes.Contains(search)));

    query = orderBy?.ToLower() switch
    {
        "duedate" => query.OrderBy(t => t.DueDate),
        "status" => query.OrderBy(t => t.Status),
        "created" => query.OrderByDescending(t => t.CreatedAt),
        _ => query.OrderByDescending(t => t.Id)
    };

    var total = await query.CountAsync();
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return Results.Ok(new { total, page, pageSize, items });
});

// Get by id
tasks.MapGet("/{id:int}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

// Create
tasks.MapPost("/", async (TaskCreateDto input, AppDbContext db) =>
{
    var exists = await db.Projects.AnyAsync(p => p.Id == input.ProjectId);
    if (!exists) return Results.BadRequest(new { error = "ProjectId inválido." });

    var task = new TaskItem
    {
        Title = input.Title,
        Notes = input.Notes,
        DueDate = input.DueDate,
        Status = input.Status,
        ProjectId = input.ProjectId
    };

    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tasks/{task.Id}", task);
});

// Update
tasks.MapPut("/{id:int}", async (int id, TaskUpdateDto input, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();

    // Validate project
    var exists = await db.Projects.AnyAsync(p => p.Id == input.ProjectId);
    if (!exists) return Results.BadRequest(new { error = "ProjectId inválido." });

    task.Title = input.Title;
    task.Notes = input.Notes;
    task.DueDate = input.DueDate;
    task.Status = input.Status;
    task.ProjectId = input.ProjectId;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Soft Delete
tasks.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();
    task.IsDeleted = true;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
