using FluentValidation;
using KeepTabs.Application.StatusPages.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Application.StatusPages;

/// <summary>
/// Format validation for status-page creation. Ownership and slug uniqueness are checked by the service.
/// </summary>
public sealed class CreateStatusPageRequestValidator : AbstractValidator<CreateStatusPageRequest>
{
    public CreateStatusPageRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(Domain.StatusPage.MaxNameLength);

        RuleFor(request => request.Slug)
            .MaximumLength(Domain.StatusPage.MaxSlugLength)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug may only contain lowercase letters, numbers, and single hyphens.")
            .When(request => !string.IsNullOrWhiteSpace(request.Slug));
    }
}
