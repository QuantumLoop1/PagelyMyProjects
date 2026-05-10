using NotionClone.DAL.Entities;

namespace NotionClone.DAL.Repositories;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetAllForUserAsync(Guid userId);
    Task<TaskItem?> GetByIdAsync(Guid userId, Guid taskId);
    Task<int> GetNextOrderAsync(Guid userId, string status);
    Task AddAsync(TaskItem task);
    void Delete(TaskItem task);
    Task SaveChangesAsync();
}
