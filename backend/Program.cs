using Microsoft.EntityFrameworkCore;
using Todo.Api.Data;
using Todo.Api.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    postgres => postgres.EnableRetryOnFailure(3)));

var app = builder.Build();
app.UseExceptionHandler();

// Simple local demo: create the schema on first run, without a separate CLI step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/api/health", async (TodoDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ok" })
        : Results.Problem("Không kết nối được PostgreSQL.", statusCode: 503));

app.MapGet("/api/todos", async (TodoDbContext db) =>
    await db.Todos.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync());

app.MapGet("/api/todos/{id:int}", async (int id, TodoDbContext db) =>
    await db.Todos.FindAsync(id) is { } item ? Results.Ok(item) : Results.NotFound());

app.MapPost("/api/todos", async (TodoRequest request, TodoDbContext db) =>
{
    if (ValidateTitle(request.Title) is { } error) return error;
    var item = new TodoItem { Title = request.Title!.Trim(), IsCompleted = request.IsCompleted };
    db.Todos.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/todos/{item.Id}", item);
});

app.MapPut("/api/todos/{id:int}", async (int id, TodoRequest request, TodoDbContext db) =>
{
    if (ValidateTitle(request.Title) is { } error) return error;
    var item = await db.Todos.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.Title = request.Title!.Trim();
    item.IsCompleted = request.IsCompleted;
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

app.MapDelete("/api/todos/{id:int}", async (int id, TodoDbContext db) =>
{
    var item = await db.Todos.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.Todos.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

static IResult? ValidateTitle(string? title) =>
    string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200
        ? Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["title"] = ["Tên công việc phải có từ 1 đến 200 ký tự."]
        })
        : null;
