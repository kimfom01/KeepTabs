using KeepTabs.Infrastructure.Database.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KeepTabs.Infrastructure.Database;

/// <summary>
/// Ensures auditing and domain-event interceptors participate in every
/// <see cref="ApplicationDbContext"/> options instance, including pooled contexts.
/// </summary>
internal sealed class PersistenceInterceptorOptionsSetup : IConfigureOptions<DbContextOptions<ApplicationDbContext>>
{
    private readonly AuditableEntityInterceptor _auditableEntityInterceptor;
    private readonly DispatchDomainEventsInterceptor _dispatchDomainEventsInterceptor;

    public PersistenceInterceptorOptionsSetup(
        AuditableEntityInterceptor auditableEntityInterceptor,
        DispatchDomainEventsInterceptor dispatchDomainEventsInterceptor)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
        _dispatchDomainEventsInterceptor = dispatchDomainEventsInterceptor;
    }

    public void Configure(DbContextOptions<ApplicationDbContext> options)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>(options);
        builder.AddInterceptors(_auditableEntityInterceptor, _dispatchDomainEventsInterceptor);
    }
}
