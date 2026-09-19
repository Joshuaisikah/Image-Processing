using Imageprocessing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Imageprocessing.Api.Services;

public class ImageProcessingWorker : BackgroundService
{
    private readonly ILogger<ImageProcessingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IImageProcessingQueue _queue;
    private readonly ThumbnailGenerator _thumbnailGenerator;
    private readonly IWebHostEnvironment _env;

    public ImageProcessingWorker(
        ILogger<ImageProcessingWorker> logger,
        IServiceScopeFactory scopeFactory,
        IImageProcessingQueue queue,
        ThumbnailGenerator thumbnailGenerator,
        IWebHostEnvironment env
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _queue = queue;
        _thumbnailGenerator = thumbnailGenerator;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start the consumer FIRST (not awaited yet — this starts it running
        // concurrently). It needs to already be draining the channel before
        // recovery tries to push a potentially large backlog into it, or
        // EnqueueAsync blocking on a full channel with nothing reading from
        // it yet would deadlock the whole worker.
        var consumeTask = RunConsumeLoop(stoppingToken);

        await RunStartupRecovery(stoppingToken);

        // Keep ExecuteAsync alive for as long as the consumer runs (normally
        // forever, until the app shuts down / stoppingToken is cancelled).
        await consumeTask;
    }

    private async Task RunStartupRecovery(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unfinishedJobs = await dbContext
            .ImageJobs.Where(j =>
                j.Status == ImageStatus.Pending || j.Status == ImageStatus.Processing
            )
            .ToListAsync(stoppingToken);

        foreach (var job in unfinishedJobs)
        {
            // Reset Processing -> Pending so the status a client sees stays
            // honest: Processing should only mean "actively being worked on
            // right now," not "sitting in the queue waiting its turn again."
            if (job.Status == ImageStatus.Processing)
            {
                job.Status = ImageStatus.Pending;
            }

            // EnqueueAsync waits if the channel is full, instead of silently
            // dropping the job the way TryEnqueue would. Safe now because
            // RunConsumeLoop is already running concurrently and draining
            // the channel as this loop enqueues.
            await _queue.EnqueueAsync(job.Id, stoppingToken);
        }

        if (unfinishedJobs.Count > 0)
        {
            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation(
                "Re-enqueued {Count} unfinished jobs on startup.",
                unfinishedJobs.Count
            );
        }
    }

    private async Task RunConsumeLoop(CancellationToken stoppingToken)
    {
        var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads");

        await foreach (var imageId in _queue.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var job = await dbContext.ImageJobs.FindAsync(new object[] { imageId }, stoppingToken);

            if (job is null)
            {
                _logger.LogWarning("Image job {ImageId} not found.", imageId);
                continue;
            }

            try
            {
                // Guard: original file must exist
                if (!File.Exists(job.OriginalPath))
                {
                    job.Status = ImageStatus.Failed;
                    job.FailureReason = "Original file missing on disk";
                    job.CompletedAt = DateTimeOffset.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                job.Status = ImageStatus.Processing;
                await dbContext.SaveChangesAsync(stoppingToken);

                Dictionary<string, byte[]> thumbnails;
                using (
                    var sourceStream = new FileStream(
                        job.OriginalPath,
                        FileMode.Open,
                        FileAccess.Read
                    )
                )
                {
                    var result = await _thumbnailGenerator.GenerateThumbnailsAsync(
                        sourceStream,
                        stoppingToken
                    );
                    thumbnails = new Dictionary<string, byte[]>(result);
                }

                foreach (var (size, bytes) in thumbnails)
                {
                    var outPath = ThumbnailPathHelper.GetPath(uploadsRoot, imageId, size);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                    await File.WriteAllBytesAsync(outPath, bytes, stoppingToken);
                }

                job.Status = ImageStatus.Completed;
                job.CompletedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Image job {ImageId} completed.", imageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image job {ImageId}.", imageId);

                job.Status = ImageStatus.Failed;
                job.FailureReason = ex.Message;
                job.CompletedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);
                // loop continues — worker stays alive
            }
        }
    }
}
