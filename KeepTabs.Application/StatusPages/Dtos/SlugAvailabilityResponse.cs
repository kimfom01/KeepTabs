namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>Slug availability result for live form feedback.</summary>
public sealed record SlugAvailabilityResponse(
    string Slug,
    bool Available,
    string Suggestion);
