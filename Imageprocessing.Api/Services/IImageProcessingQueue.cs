namespace Imageprocessing.Api.Services
{
    public interface IImageProcessingQueue
    {
        /// <summary>
        ///  Attempts to engueue a job ID for processing. Returns true if the job ID was successfully enqueued, or false if the queue is full.
        /// </summary>
        bool TryEnqueue(Guid jobId);

        IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default);
        Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);
    }
}
