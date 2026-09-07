using FluentValidation;
using KeepTabs.Application.StatusPages.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Application.StatusPages;

/// <summary>
/// Format validation for supplied status-page update fields.
/// </summary>
public sealed class UpdateStatusPageRequestValidator : AbstractValidator<UpdateStatusPageRequest>
{
    public UpdateStatusPageRequestValidator()
    {
        When(request => request.Name is not null, () =>
        {
            RuleFor(request => request.Name!)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(Domain.StatusPage.MaxNameLength);
        });

        When(request => request.Slug is not null, () =>
        {
            RuleFor(request => request.Slug!)
                .NotEmpty()
                .WithMessage("Slug is required.")
                .MaximumLength(Domain.StatusPage.MaxSlugLength)
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug may only contain lowercase letters, numbers, and single hyphens.");
        });
    }
}
