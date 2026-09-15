using Imageprocessing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Imageprocessing.Api.Services;

public class ImageProcessingWorker : BackgroundService
{
    private readonly ILogger<ImageProcessingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ImageProcessingWorker(
        ILogger<ImageProcessingWorker> logger,
        IServiceScopeFactory scopeFactory
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var imageProcessingQueue =
                scope.ServiceProvider.GetRequiredService<IImageProcessingQueue>();
            var pendingJobs = await dbContext
                .ImageJobs.Where(j => j.Status == ImageStatus.Pending)
                .ToListAsync(stoppingToken);
            foreach (var job in pendingJobs)
            {
                imageProcessingQueue.TryEnqueue(job.Id);
            }
        }
    }
}
