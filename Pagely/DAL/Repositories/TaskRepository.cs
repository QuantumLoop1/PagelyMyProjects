using Microsoft.EntityFrameworkCore;
using NotionClone.DAL.Entities;

namespace NotionClone.DAL.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<TaskItem>> GetAllForUserAsync(Guid userId)
    {
        return _context.Tasks
            .Where(task => task.UserId == userId)
            .OrderBy(task => task.Status)
            .ThenBy(task => task.Order)
            .ThenBy(task => task.CreatedAt)
            .ToListAsync();
    }

    public Task<TaskItem?> GetByIdAsync(Guid userId, Guid taskId)
    {
        return _context.Tasks.FirstOrDefaultAsync(task => task.UserId == userId && task.Id == taskId);
    }

    public async Task<int> GetNextOrderAsync(Guid userId, string status)
    {
        var maxOrder = await _context.Tasks
            .Where(task => task.UserId == userId && task.Status == status)
            .Select(task => (int?)task.Order)
            .MaxAsync();

        return maxOrder.HasValue ? maxOrder.Value + 1 : 0;
    }

    public async Task AddAsync(TaskItem task)
    {
        await _context.Tasks.AddAsync(task);
    }

    public void Delete(TaskItem task)
    {
        _context.Tasks.Attach(task);
        _context.Tasks.Remove(task);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
