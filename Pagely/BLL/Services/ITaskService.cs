using NotionClone.BLL.DTOs;

namespace NotionClone.BLL.Services;

public interface ITaskService
{
    Task<List<TaskDto>> GetTasksAsync(Guid userId);
    Task<TaskSummaryDto> GetDashboardAsync(Guid userId);
    Task<List<TaskCalendarDayDto>> GetCalendarAsync(Guid userId);
    Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequestDto request);
    Task<TaskDto> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequestDto request);
    Task<TaskDto> MoveAsync(Guid userId, Guid taskId, MoveTaskRequestDto request);
    Task DeleteAsync(Guid userId, Guid taskId);
}
