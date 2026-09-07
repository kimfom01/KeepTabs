using FluentValidation;
using KeepTabs.Application.Settings.Dtos;

namespace KeepTabs.Application.Settings;

/// <summary>
/// Shape validation for Telegram configuration updates. An empty token keeps
/// the stored one; a supplied token must look like a Bot API token.
/// </summary>
public sealed class UpdateTelegramSettingsRequestValidator : AbstractValidator<UpdateTelegramSettingsRequest>
{
    public UpdateTelegramSettingsRequestValidator()
    {
        When(request => !string.IsNullOrWhiteSpace(request.BotToken), () =>
        {
            RuleFor(request => request.BotToken.Trim())
                .Matches(@"^\d+:[\w\-]{30,}$")
                .WithMessage("Bot token must look like 123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11.")
                .MaximumLength(512);
        });
    }
}
