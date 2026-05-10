using NotionClone.BLL.DTOs;
using NotionClone.DAL.Entities;
using NotionClone.DAL.Repositories;

namespace NotionClone.BLL.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPageRepository _pageRepository;

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IPageRepository pageRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _pageRepository = pageRepository;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new ArgumentException("Email is already registered.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = request.Name.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        await CreateDefaultPagesAsync(user.Id);

        return await CreateSessionAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await CreateSessionAsync(user);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshRequestDto request)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);

        if (storedToken is null || storedToken.IsRevoked || storedToken.IsExpired)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();

        return await CreateSessionAsync(storedToken.User);
    }

    public async Task LogoutAsync(RefreshRequestDto request)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);

        if (storedToken is null || storedToken.IsRevoked)
        {
            // Токен уже недействителен или не найден — считаем logout успешным
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();
    }

    private async Task<AuthResponseDto> CreateSessionAsync(AppUser user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var now = DateTime.UtcNow;

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.Add(RefreshTokenLifetime)
        });

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = now.Add(AccessTokenLifetime),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name
            }
        };
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private async Task CreateDefaultPagesAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var dashboardPage = new Page
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Dashboard",
            Content = string.Empty,
            Icon = "F4CA",
            Status = "Todo",
            Order = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        var calendarPage = new Page
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Calendar",
            Content = string.Empty,
            Icon = "F5D3",
            Status = "Todo",
            Order = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _pageRepository.AddAsync(dashboardPage);
        await _pageRepository.AddAsync(calendarPage);
        await _pageRepository.SaveChangesAsync();
    }
}
