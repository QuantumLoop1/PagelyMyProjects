using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotionClone.BLL.DTOs;
using NotionClone.BLL.Services;

namespace Pagely.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetTasks()
    {
        var userId = GetUserId();
        var tasks = await _taskService.GetTasksAsync(userId);
        return Ok(ApiResponse<List<TaskDto>>.FromSuccess(tasks));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<TaskSummaryDto>>> GetDashboard()
    {
        var userId = GetUserId();
        var summary = await _taskService.GetDashboardAsync(userId);
        return Ok(ApiResponse<TaskSummaryDto>.FromSuccess(summary));
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<ApiResponse<List<TaskCalendarDayDto>>>> GetCalendar()
    {
        var userId = GetUserId();
        var calendar = await _taskService.GetCalendarAsync(userId);
        return Ok(ApiResponse<List<TaskCalendarDayDto>>.FromSuccess(calendar));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask([FromBody] CreateTaskRequestDto request)
    {
        var userId = GetUserId();
        var task = await _taskService.CreateAsync(userId, request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TaskDto>.FromSuccess(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(Guid id, [FromBody] UpdateTaskRequestDto request)
    {
        var userId = GetUserId();
        var task = await _taskService.UpdateAsync(userId, id, request);
        return Ok(ApiResponse<TaskDto>.FromSuccess(task));
    }

    [HttpPatch("{id:guid}/move")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> MoveTask(Guid id, [FromBody] MoveTaskRequestDto request)
    {
        var userId = GetUserId();
        var task = await _taskService.MoveAsync(userId, id, request);
        return Ok(ApiResponse<TaskDto>.FromSuccess(task));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(Guid id)
    {
        var userId = GetUserId();
        await _taskService.DeleteAsync(userId, id);
        return Ok(ApiResponse<bool>.FromSuccess(true));
    }

    private Guid GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdValue, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Invalid token.");
    }
}
