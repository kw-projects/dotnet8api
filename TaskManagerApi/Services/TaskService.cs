using TaskManagerApi.DATA;
using Microsoft.EntityFrameworkCore;

public class TaskService : ITaskService
{
    private readonly ILogger<TaskService> _logger;
    private readonly TaskDbContext _taskDbContext;

    public TaskService(ILogger<TaskService> logger, TaskDbContext taskDbContext)
    {
        _logger = logger;
        _taskDbContext = taskDbContext;
    }

    public async Task<IList<TaskItem>> GetAllAsync()
    {
        return await _taskDbContext.Tasks.ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        var item = await _taskDbContext.Tasks.FirstOrDefaultAsync(x => x.Id == id);
        _logger.LogInformation($"Retrieved task item: {item?.Title}");
        return item;
    }

    public async Task<int> CreateAsync(TaskItem item)
    {
        _taskDbContext.Tasks.Add(item);
        await _taskDbContext.SaveChangesAsync();
        _logger.LogInformation($"Created task item id: {item.Id}");
        return item.Id;
    }

    public async Task<TaskItem?> UpdateAsync(int id, TaskItem updatedItem)
    {
        var item = await _taskDbContext.Tasks.FindAsync(id);
        if (item == null)
        {
            _logger.LogWarning($"Task item not found: {id}");
            return null;
        }

        item.Title = updatedItem.Title;
        item.Description = updatedItem.Description;
        item.IsCompleted = updatedItem.IsCompleted;

        _taskDbContext.Tasks.Update(item);
        await _taskDbContext.SaveChangesAsync();
        _logger.LogInformation($"Updated task item id: {item.Id}");

        return item;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _taskDbContext.Tasks.FindAsync(id);
        if (item != null)
        {
            _taskDbContext.Tasks.Remove(item);
            await _taskDbContext.SaveChangesAsync();
            _logger.LogInformation($"Deleted task item id: {id}");
        }
    }
    
    public async Task<IEnumerable<TaskItem>> GetTasksByUserIdAsync(int userId)
    {        
        return await _taskDbContext.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }
}
