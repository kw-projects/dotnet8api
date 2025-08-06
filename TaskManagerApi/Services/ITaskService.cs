
public interface ITaskService
{
    Task<IList<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);
    Task<int> CreateAsync(TaskItem item);
    Task<TaskItem?> UpdateAsync(int id, TaskItem item);
    Task DeleteAsync(int id);
}
