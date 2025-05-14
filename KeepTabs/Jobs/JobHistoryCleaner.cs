using KeepTabs.Database;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Jobs;

public class JobHistoryCleaner
{
    private readonly KeepTabsDbContext _context;
    private readonly ILogger<JobHistoryCleaner> _logger;

    public JobHistoryCleaner(KeepTabsDbContext context, ILogger<JobHistoryCleaner> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanUp(CancellationToken cancellationToken)
    {
        var expiryDateUtc = DateTime.UtcNow.AddDays(-1);
        _logger.LogInformation("Cleaning up job history for entries before {@ExpiryDateUtc}", expiryDateUtc);
        
        var outdatedHistory = await _context.ResponseStatuses
            .Where(x => x.CreatedAtUtc < expiryDateUtc)
            .ToListAsync(cancellationToken);

        _context.RemoveRange(outdatedHistory);
        await _context.SaveChangesAsync(cancellationToken);
    }
}