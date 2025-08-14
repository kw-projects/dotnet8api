using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TaskItems")]
public class TaskItem
{
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public required string Title { get; set; }
    [Required]
    [MaxLength(500)]
    public required string Description { get; set; }
    public bool IsCompleted { get; set; }
    public required DateTime CreatedAt { get; set; }
    public int? UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }
}