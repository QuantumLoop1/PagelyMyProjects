using NotionClone.BLL.DTOs;

namespace NotionClone.BLL.Services;

public interface IPageService
{
    Task<List<PageDto>> GetRootPagesAsync(Guid userId);
    Task<PageDto> GetByIdAsync(Guid userId, Guid pageId);
    Task<PageDto> CreateAsync(Guid userId, CreatePageRequestDto request);
    Task<PageDto> UpdateAsync(Guid userId, Guid pageId, UpdatePageRequestDto request);
    Task DeleteAsync(Guid userId, Guid pageId);
    Task<PageDto> MoveAsync(Guid userId, Guid pageId, MovePageRequestDto request);
}
