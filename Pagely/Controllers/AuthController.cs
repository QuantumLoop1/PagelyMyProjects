using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotionClone.BLL.DTOs;
using NotionClone.BLL.Services;

namespace Pagely.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto request)
    {
        var response = await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<AuthResponseDto>.FromSuccess(response));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.FromSuccess(response));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout([FromBody] RefreshRequestDto request)
    {
        await _authService.LogoutAsync(request);
        return Ok(ApiResponse<bool>.FromSuccess(true));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh([FromBody] RefreshRequestDto request)
    {
        var response = await _authService.RefreshAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.FromSuccess(response));
    }
}
