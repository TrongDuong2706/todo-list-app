namespace Todo.Api.Models;

public sealed class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed record TodoRequest(string? Title, bool IsCompleted = false);
