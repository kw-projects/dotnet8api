public class TaskItemDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required bool IsCompleted { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required int UserId { get; set; }
    public required string UserName { get; set; }
    public required string UserEmail { get; set; }
}