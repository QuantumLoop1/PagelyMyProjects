using NotionClone.BLL.DTOs;
using NotionClone.DAL.Entities;
using NotionClone.DAL.Repositories;

namespace NotionClone.BLL.Services;

public sealed class TaskService : ITaskService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Todo",
        "Doing",
        "Done"
    };

    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<List<TaskDto>> GetTasksAsync(Guid userId)
    {
        var tasks = await _taskRepository.GetAllForUserAsync(userId);
        return tasks.Select(Map).ToList();
    }

    public async Task<TaskSummaryDto> GetDashboardAsync(Guid userId)
    {
        var tasks = await _taskRepository.GetAllForUserAsync(userId);
        var total = tasks.Count;
        var todo = tasks.Count(task => string.Equals(task.Status, "Todo", StringComparison.OrdinalIgnoreCase));
        var doing = tasks.Count(task => string.Equals(task.Status, "Doing", StringComparison.OrdinalIgnoreCase));
        var done = tasks.Count(task => string.Equals(task.Status, "Done", StringComparison.OrdinalIgnoreCase));
        var completion = total == 0 ? 0 : Math.Round(done * 100.0 / total, 2);

        return new TaskSummaryDto
        {
            Total = total,
            Todo = todo,
            Doing = doing,
            Done = done,
            CompletionPercent = completion
        };
    }

    public async Task<List<TaskCalendarDayDto>> GetCalendarAsync(Guid userId)
    {
        var tasks = await _taskRepository.GetAllForUserAsync(userId);

        return tasks
            .Where(task => task.ScheduledFor.HasValue)
            .GroupBy(task => task.ScheduledFor!.Value.Date)
            .OrderBy(group => group.Key)
            .Select(group => new TaskCalendarDayDto
            {
                Date = group.Key,
                Tasks = group
                    .OrderBy(task => task.Status)
                    .ThenBy(task => task.Order)
                    .Select(Map)
                    .ToList()
            })
            .ToList();
    }

    public async Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequestDto request)
    {
        var title = NormalizeTitle(request.Title);
        var status = NormalizeStatus(request.Status ?? "Todo");
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Description = NormalizeOptional(request.Description),
            Status = status,
            Order = await _taskRepository.GetNextOrderAsync(userId, status),
            ScheduledFor = NormalizeScheduledFor(request.ScheduledFor),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task);
        await _taskRepository.SaveChangesAsync();
        return Map(task);
    }

    public async Task<TaskDto> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequestDto request)
    {
        var task = await _taskRepository.GetByIdAsync(userId, taskId);
        if (task is null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        if (request.Title is not null)
        {
            task.Title = NormalizeTitle(request.Title);
        }

        if (request.Description is not null)
        {
            task.Description = NormalizeOptional(request.Description);
        }

        if (request.Status is not null)
        {
            task.Status = NormalizeStatus(request.Status);
        }

        if (request.ScheduledFor.HasValue)
        {
            task.ScheduledFor = NormalizeScheduledFor(request.ScheduledFor);
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _taskRepository.SaveChangesAsync();
        return Map(task);
    }

    public async Task<TaskDto> MoveAsync(Guid userId, Guid taskId, MoveTaskRequestDto request)
    {
        var task = await _taskRepository.GetByIdAsync(userId, taskId);
        if (task is null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        var status = NormalizeStatus(request.Status);
        task.Status = status;
        task.Order = request.Order ?? await _taskRepository.GetNextOrderAsync(userId, status);
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();
        return Map(task);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId)
    {
        var task = await _taskRepository.GetByIdAsync(userId, taskId);
        if (task is null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        _taskRepository.Delete(task);
        await _taskRepository.SaveChangesAsync();
    }

    private static TaskDto Map(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Order = task.Order,
            ScheduledFor = task.ScheduledFor,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    private static string NormalizeTitle(string? title)
    {
        var value = title?.Trim();
        return string.IsNullOrWhiteSpace(value) ? "Untitled task" : value;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizeStatus(string status)
    {
        var normalized = status.Trim();
        if (!ValidStatuses.Contains(normalized))
        {
            throw new ArgumentException("Invalid task status.");
        }

        return char.ToUpperInvariant(normalized[0]) + normalized[1..].ToLowerInvariant();
    }

    private static DateTime? NormalizeScheduledFor(DateTime? scheduledFor)
    {
        if (!scheduledFor.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(scheduledFor.Value.Date, DateTimeKind.Utc);
    }
}
