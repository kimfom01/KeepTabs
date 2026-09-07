using KeepTabs.Application.Alerts.Dtos;

namespace KeepTabs.Application.Alerts;

/// <summary>
/// Alert-rule use cases. Every operation is scoped to the authenticated owner
/// through the owning monitor.
/// </summary>
public interface IAlertService
{
    Task<GetAlertRuleResponse> CreateAsync(
        string userId,
        CreateAlertRuleRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GetAlertRuleResponse>> ListAsync(
        string userId,
        Guid? monitorId,
        CancellationToken cancellationToken = default);
    Task<GetAlertRuleResponse?> GetByIdAsync(
        string userId,
        Guid alertRuleId,
        CancellationToken cancellationToken = default);
    Task<GetAlertRuleResponse?> UpdateAsync(
        string userId,
        Guid alertRuleId,
        UpdateAlertRuleRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(
        string userId,
        Guid alertRuleId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertLogResponse>> GetLogsAsync(
        string userId,
        Guid? monitorId,
        CancellationToken cancellationToken = default);
    Task<TestAlertResponse?> TestAsync(
        string userId,
        Guid alertRuleId,
        CancellationToken cancellationToken = default);
}
