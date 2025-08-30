using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Utils;

public class PeriodicBackgroundTask : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(10);
    private readonly ILogger<PeriodicBackgroundTask> _logger;
    private readonly HashSet<long> _processedPolicyIds = new();

    public PeriodicBackgroundTask(IServiceProvider services, ILogger<PeriodicBackgroundTask> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PeriodicBackgroundTask started.");

        using PeriodicTimer timer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                var policiesToLog = await db.Policies
                    .Where(p => !_processedPolicyIds.Contains(p.Id))
                    .Where(p => p.EndDate.ToDateTime(TimeOnly.MinValue) <= now
                            && p.EndDate.ToDateTime(TimeOnly.MinValue) >= now.AddHours(-1))
                    .ToListAsync(stoppingToken);

                foreach (var policy in policiesToLog)
                {
                    _logger.LogInformation($"Policy {policy.Id} expired at {policy.EndDate}");
                    _processedPolicyIds.Add(policy.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking expired policies.");
            }
        }
    }
}
