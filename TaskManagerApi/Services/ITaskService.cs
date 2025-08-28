
public interface ITaskService
{
    Task<IList<TaskItemDto>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);
    Task<int> CreateAsync(TaskItem item);
    Task<TaskItem?> UpdateAsync(int id, TaskItem item);
    Task DeleteAsync(int id);
    Task<IEnumerable<TaskItem>> GetTasksByUserIdAsync(int userId);
}
