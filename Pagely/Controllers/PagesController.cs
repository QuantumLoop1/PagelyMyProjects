using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotionClone.BLL.DTOs;
using NotionClone.BLL.Services;

namespace Pagely.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class PagesController : ControllerBase
{
    private readonly IPageService _pageService;

    public PagesController(IPageService pageService)
    {
        _pageService = pageService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PageDto>>>> GetRootPages()
    {
        var userId = GetUserId();
        var pages = await _pageService.GetRootPagesAsync(userId);
        return Ok(ApiResponse<List<PageDto>>.FromSuccess(pages));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PageDto>>> GetPage(Guid id)
    {
        var userId = GetUserId();
        var page = await _pageService.GetByIdAsync(userId, id);
        return Ok(ApiResponse<PageDto>.FromSuccess(page));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PageDto>>> CreatePage([FromBody] CreatePageRequestDto request)
    {
        var userId = GetUserId();
        var page = await _pageService.CreateAsync(userId, request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<PageDto>.FromSuccess(page));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PageDto>>> UpdatePage(Guid id, [FromBody] UpdatePageRequestDto request)
    {
        var userId = GetUserId();
        var page = await _pageService.UpdateAsync(userId, id, request);
        return Ok(ApiResponse<PageDto>.FromSuccess(page));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePage(Guid id)
    {
        var userId = GetUserId();
        await _pageService.DeleteAsync(userId, id);
        return Ok(ApiResponse<bool>.FromSuccess(true));
    }

    [HttpPatch("{id:guid}/move")]
    public async Task<ActionResult<ApiResponse<PageDto>>> MovePage(Guid id, [FromBody] MovePageRequestDto request)
    {
        var userId = GetUserId();
        var page = await _pageService.MoveAsync(userId, id, request);
        return Ok(ApiResponse<PageDto>.FromSuccess(page));
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
