using System.Text.Json;
using KeepTabs.Application.Settings.Dtos;
using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Application.Settings;

/// <summary>
/// Stores settings as one JSON document per area in the AppSettings table.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const string SmtpKey = "smtp";
    private const string TelegramKey = "telegram";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SettingsService(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<SmtpSettings> GetSmtpAsync(CancellationToken cancellationToken = default)
    {
        var stored = await ReadAsync<SmtpSettings>(SmtpKey, cancellationToken);

        return stored ?? new SmtpSettings(false, string.Empty, 587, string.Empty, string.Empty, string.Empty, true);
    }

    public async Task<SmtpSettingsResponse> GetSmtpViewAsync(CancellationToken cancellationToken = default)
    {
        var current = await GetSmtpAsync(cancellationToken);

        return ToView(current);
    }

    public async Task<TelegramSettings> GetTelegramAsync(CancellationToken cancellationToken = default)
    {
        var stored = await ReadAsync<TelegramSettings>(TelegramKey, cancellationToken);

        return stored ?? new TelegramSettings(false, string.Empty);
    }

    public async Task<TelegramSettingsResponse> GetTelegramViewAsync(CancellationToken cancellationToken = default)
    {
        var current = await GetTelegramAsync(cancellationToken);

        return new TelegramSettingsResponse(current.Enabled, !string.IsNullOrEmpty(current.BotToken));
    }

    public async Task<TelegramSettingsResponse> UpdateTelegramAsync(
        UpdateTelegramSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var current = await GetTelegramAsync(cancellationToken);
        var merged = current with
        {
            Enabled = request.Enabled,
            BotToken = string.IsNullOrEmpty(request.BotToken) ? current.BotToken : request.BotToken.Trim(),
        };

        await WriteAsync(TelegramKey, merged, cancellationToken);

        return new TelegramSettingsResponse(merged.Enabled, !string.IsNullOrEmpty(merged.BotToken));
    }

    public async Task<SmtpSettingsResponse> UpdateSmtpAsync(
        UpdateSmtpSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var current = await GetSmtpAsync(cancellationToken);
        var merged = current with
        {
            Enabled = request.Enabled,
            Host = request.Host.Trim(),
            Port = request.Port,
            Username = request.Username.Trim(),
            Password = string.IsNullOrEmpty(request.Password) ? current.Password : request.Password,
            From = request.From.Trim(),
            EnableSsl = request.EnableSsl,
        };

        await WriteAsync(SmtpKey, merged, cancellationToken);

        return ToView(merged);
    }

    private static SmtpSettingsResponse ToView(SmtpSettings settings)
    {
        return new SmtpSettingsResponse(
            settings.Enabled,
            settings.Host,
            settings.Port,
            settings.Username,
            !string.IsNullOrEmpty(settings.Password),
            settings.From,
            settings.EnableSsl);
    }

    private async Task<T?> ReadAsync<T>(string key, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);
        if (setting?.Value is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(setting.Value, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private async Task WriteAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);
        var payload = JsonSerializer.Serialize(value, JsonOptions);

        if (setting is null)
        {
            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = payload,
                UpdatedAt = _timeProvider.GetUtcNow(),
            });
        }
        else
        {
            setting.Value = payload;
            setting.UpdatedAt = _timeProvider.GetUtcNow();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
