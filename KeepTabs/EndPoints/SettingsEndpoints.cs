using KeepTabs.Application.Settings;
using KeepTabs.Application.Settings.Dtos;
using KeepTabs.Domain.Common;
using KeepTabs.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KeepTabs.EndPoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("settings")
            .WithTags("Settings")
            .RequireAuthorization(AuthorizationPolicies.UserAccess);

        group.MapGet("/smtp", GetSmtpSettings)
            .WithName("GetSmtpSettings")
            .WithSummary("Get SMTP settings")
            .WithDescription("Returns the stored SMTP configuration. The password itself is never returned.")
            .Produces<SmtpSettingsResponse>();

        group.MapPut("/smtp", UpdateSmtpSettings)
            .AddEndpointFilter<ValidationFilter<UpdateSmtpSettingsRequest>>()
            .WithName("UpdateSmtpSettings")
            .WithSummary("Update SMTP settings")
            .WithDescription("Stores the SMTP configuration used for email alert delivery. An empty password keeps the stored one.")
            .Produces<SmtpSettingsResponse>()
            .ProducesValidationProblem();

        group.MapGet("/telegram", GetTelegramSettings)
            .WithName("GetTelegramSettings")
            .WithSummary("Get Telegram settings")
            .WithDescription("Returns the stored Telegram bot configuration. The token itself is never returned.")
            .Produces<TelegramSettingsResponse>();

        group.MapPut("/telegram", UpdateTelegramSettings)
            .AddEndpointFilter<ValidationFilter<UpdateTelegramSettingsRequest>>()
            .WithName("UpdateTelegramSettings")
            .WithSummary("Update Telegram settings")
            .WithDescription("Stores the bot token used for Telegram alert delivery. An empty token keeps the stored one.")
            .Produces<TelegramSettingsResponse>()
            .ProducesValidationProblem();
    }

    private static async Task<Ok<SmtpSettingsResponse>> GetSmtpSettings(
        IUser currentUser,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        _ = RequireUserId(currentUser);

        return TypedResults.Ok(await settingsService.GetSmtpViewAsync(cancellationToken));
    }

    private static async Task<Results<Ok<SmtpSettingsResponse>, ValidationProblem>> UpdateSmtpSettings(
        UpdateSmtpSettingsRequest request,
        IUser currentUser,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        _ = RequireUserId(currentUser);

        return TypedResults.Ok(await settingsService.UpdateSmtpAsync(request, cancellationToken));
    }

    private static async Task<Ok<TelegramSettingsResponse>> GetTelegramSettings(
        IUser currentUser,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        _ = RequireUserId(currentUser);

        return TypedResults.Ok(await settingsService.GetTelegramViewAsync(cancellationToken));
    }

    private static async Task<Results<Ok<TelegramSettingsResponse>, ValidationProblem>> UpdateTelegramSettings(
        UpdateTelegramSettingsRequest request,
        IUser currentUser,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        _ = RequireUserId(currentUser);

        return TypedResults.Ok(await settingsService.UpdateTelegramAsync(request, cancellationToken));
    }

    private static string RequireUserId(IUser currentUser)
    {
        return currentUser.Id ?? throw new UnauthorizedAccessException();
    }
}
