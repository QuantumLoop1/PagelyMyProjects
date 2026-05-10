using NotionClone.BLL.DTOs;
using NotionClone.DAL.Entities;
using NotionClone.DAL.Repositories;

namespace NotionClone.BLL.Services;

public sealed class PageService : IPageService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Todo",
        "Doing",
        "Done"
    };

    private readonly IPageRepository _pageRepository;

    public PageService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<List<PageDto>> GetRootPagesAsync(Guid userId)
    {
        var pages = await _pageRepository.GetAllForUserAsync(userId);
        var tree = BuildTree(pages);
        return tree.Where(page => page.ParentId is null).ToList();
    }

    public async Task<PageDto> GetByIdAsync(Guid userId, Guid pageId)
    {
        var pages = await _pageRepository.GetAllForUserAsync(userId);
        var tree = BuildTree(pages);
        var page = FindPage(tree, pageId);
        return page ?? throw new KeyNotFoundException("Page not found.");
    }

    public async Task<PageDto> CreateAsync(Guid userId, CreatePageRequestDto request)
    {
        var title = NormalizeTitle(request.Title);
        var content = request.Content ?? string.Empty;

        if (request.ParentId.HasValue)
        {
            var parent = await _pageRepository.GetByIdAsync(userId, request.ParentId.Value);
            if (parent is null)
            {
                throw new KeyNotFoundException("Parent page not found.");
            }
        }

        var page = new Page
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Content = content,
            Icon = NormalizeOptional(request.Icon),
            CoverColor = NormalizeOptional(request.CoverColor),
            Status = NormalizeStatus(request.Status ?? "Todo"),
            ScheduledFor = NormalizeScheduledFor(request.ScheduledFor),
            ParentId = request.ParentId,
            Order = request.Order ?? await _pageRepository.GetNextOrderAsync(userId, request.ParentId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _pageRepository.AddAsync(page);
        await _pageRepository.SaveChangesAsync();

        return Map(page);
    }

    public async Task<PageDto> UpdateAsync(Guid userId, Guid pageId, UpdatePageRequestDto request)
    {
        var page = await _pageRepository.GetByIdAsync(userId, pageId);
        if (page is null)
        {
            throw new KeyNotFoundException("Page not found.");
        }

        if (request.Title is not null)
        {
            page.Title = NormalizeTitle(request.Title);
        }

        if (request.Content is not null)
        {
            page.Content = request.Content;
        }

        if (request.Icon is not null)
        {
            page.Icon = NormalizeOptional(request.Icon);
        }

        if (request.CoverColor is not null)
        {
            page.CoverColor = NormalizeOptional(request.CoverColor);
        }

        if (request.Status is not null)
        {
            page.Status = NormalizeStatus(request.Status);
        }

        if (request.ScheduledFor.HasValue)
        {
            page.ScheduledFor = NormalizeScheduledFor(request.ScheduledFor);
        }

        page.UpdatedAt = DateTime.UtcNow;
        await _pageRepository.SaveChangesAsync();

        return Map(page);
    }

    public async Task DeleteAsync(Guid userId, Guid pageId)
    {
        var pages = await _pageRepository.GetAllForUserAsync(userId);
        var page = pages.FirstOrDefault(item => item.Id == pageId);
        if (page is null)
        {
            throw new KeyNotFoundException("Page not found.");
        }

        var descendants = GetDescendantIds(pageId, pages);
        var toDelete = pages.Where(item => item.Id == pageId || descendants.Contains(item.Id)).ToList();

        await _pageRepository.DeleteRangeAsync(toDelete);
        await _pageRepository.SaveChangesAsync();
    }

    public async Task<PageDto> MoveAsync(Guid userId, Guid pageId, MovePageRequestDto request)
    {
        var pages = await _pageRepository.GetAllForUserAsync(userId);
        var page = pages.FirstOrDefault(item => item.Id == pageId);
        if (page is null)
        {
            throw new KeyNotFoundException("Page not found.");
        }

        if (request.ParentId == pageId)
        {
            throw new ArgumentException("A page cannot be its own parent.");
        }

        var descendants = GetDescendantIds(pageId, pages);
        if (request.ParentId.HasValue && descendants.Contains(request.ParentId.Value))
        {
            throw new ArgumentException("A page cannot be moved under one of its descendants.");
        }

        if (request.ParentId.HasValue && pages.All(item => item.Id != request.ParentId.Value))
        {
            throw new KeyNotFoundException("New parent page not found.");
        }

        page.ParentId = request.ParentId;
        page.Order = request.Order ?? await _pageRepository.GetNextOrderAsync(userId, request.ParentId);
        page.UpdatedAt = DateTime.UtcNow;

        await _pageRepository.SaveChangesAsync();
        return Map(page);
    }

    private static List<PageDto> BuildTree(IEnumerable<Page> pages)
    {
        var orderedPages = pages
            .OrderBy(page => page.ParentId)
            .ThenBy(page => page.Order)
            .ThenBy(page => page.CreatedAt)
            .ToList();

        var nodes = orderedPages.ToDictionary(page => page.Id, Map);
        var roots = new List<PageDto>();

        foreach (var page in orderedPages)
        {
            var node = nodes[page.Id];
            if (page.ParentId.HasValue && nodes.TryGetValue(page.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        SortChildren(roots);
        return roots;
    }

    private static void SortChildren(IEnumerable<PageDto> pages)
    {
        foreach (var page in pages)
        {
            page.Children = page.Children
                .OrderBy(child => child.Order)
                .ThenBy(child => child.CreatedAt)
                .ToList();

            SortChildren(page.Children);
        }
    }

    private static PageDto? FindPage(IEnumerable<PageDto> pages, Guid pageId)
    {
        foreach (var page in pages)
        {
            if (page.Id == pageId)
            {
                return page;
            }

            var child = FindPage(page.Children, pageId);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private static HashSet<Guid> GetDescendantIds(Guid parentId, IEnumerable<Page> pages)
    {
        var lookup = pages.GroupBy(page => page.ParentId).ToDictionary(group => group.Key, group => group.ToList());
        var descendants = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(parentId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!lookup.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (descendants.Add(child.Id))
                {
                    stack.Push(child.Id);
                }
            }
        }

        return descendants;
    }

    private static PageDto Map(Page page)
    {
        return new PageDto
        {
            Id = page.Id,
            Title = page.Title,
            Content = page.Content,
            Icon = page.Icon,
            CoverColor = page.CoverColor,
            Status = page.Status,
            ScheduledFor = page.ScheduledFor,
            ParentId = page.ParentId,
            Order = page.Order,
            CreatedAt = page.CreatedAt,
            UpdatedAt = page.UpdatedAt,
            Children = new List<PageDto>()
        };
    }

    private static string NormalizeTitle(string? title)
    {
        var value = title?.Trim();
        return string.IsNullOrWhiteSpace(value) ? "Untitled" : value;
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
            throw new ArgumentException("Invalid page status.");
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
