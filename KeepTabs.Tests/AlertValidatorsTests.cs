using KeepTabs.Application.Alerts;
using KeepTabs.Application.Alerts.Dtos;
using KeepTabs.Application.Settings;
using KeepTabs.Application.Settings.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Tests;

public sealed class AlertValidatorsTests
{
    private static CreateAlertRuleRequest Rule(
        AlertType type = AlertType.Email,
        string target = "ops@example.com") =>
        new(Guid.NewGuid(), type, AlertTriggerType.OnDown, 3, 30, target, true);

    [Theory]
    [InlineData(AlertType.Email, "ops@example.com", true)]
    [InlineData(AlertType.Email, "not-an-email", false)]
    [InlineData(AlertType.Webhook, "https://hooks.example.com/x", true)]
    [InlineData(AlertType.Webhook, "not-a-url", false)]
    [InlineData(AlertType.Telegram, "-1001234567890", true)]
    [InlineData(AlertType.Telegram, "@alerts", true)]
    [InlineData(AlertType.Telegram, "not a target!", false)]
    [InlineData(AlertType.Telegram, "@ab", false)]
    public void CreateValidatesTargetsPerChannel(AlertType type, string target, bool expectedValid)
    {
        var validator = new CreateAlertRuleRequestValidator();

        Assert.Equal(expectedValid, validator.Validate(Rule(type, target)).IsValid);
    }

    [Fact]
    public void UpdateAcceptsTelegramTypeChange()
    {
        var validator = new UpdateAlertRuleRequestValidator();
        var request = new UpdateAlertRuleRequest(AlertType.Telegram, null, null, null, null, null);

        Assert.True(validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", true)]
    [InlineData("", true)]
    [InlineData("not-a-token", false)]
    [InlineData("123456", false)]
    public void TelegramTokenShapeValidated(string token, bool expectedValid)
    {
        var validator = new UpdateTelegramSettingsRequestValidator();
        var request = new UpdateTelegramSettingsRequest(true, token);

        Assert.Equal(expectedValid, validator.Validate(request).IsValid);
    }
}
