using System.ComponentModel.DataAnnotations;

namespace KeepTabs.Domain;

/// <summary>
/// Key-value application setting (notification configuration and similar).
/// </summary>
public class AppSetting
{
    [MaxLength(200)]
    public string Key { get; set; } = default!;

    public string? Value { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
