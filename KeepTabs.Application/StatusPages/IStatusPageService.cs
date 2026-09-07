using KeepTabs.Application.StatusPages.Dtos;

namespace KeepTabs.Application.StatusPages;

/// <summary>
/// Status-page use cases. Management is scoped to the authenticated owner;
/// published pages are readable anonymously by slug.
/// </summary>
public interface IStatusPageService
{
    Task<GetStatusPageResponse> CreateAsync(
        string userId,
        CreateStatusPageRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GetStatusPageResponse>> ListAsync(
        string userId,
        CancellationToken cancellationToken = default);
    Task<GetStatusPageResponse?> GetByIdAsync(
        string userId,
        Guid statusPageId,
        CancellationToken cancellationToken = default);
    Task<GetStatusPageResponse?> UpdateAsync(
        string userId,
        Guid statusPageId,
        UpdateStatusPageRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(
        string userId,
        Guid statusPageId,
        CancellationToken cancellationToken = default);
    Task<PublicStatusPageResponse?> GetPublicAsync(
        string slug,
        CancellationToken cancellationToken = default);
    Task<SlugAvailabilityResponse> CheckSlugAsync(
        string? slug,
        string? name,
        Guid? excludeId,
        CancellationToken cancellationToken = default);
}
