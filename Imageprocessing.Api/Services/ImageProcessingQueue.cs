using System.Threading.Channels;

namespace Imageprocessing.Api.Services
{
    public class ImageProcessingQueue : IImageProcessingQueue
    {
        private readonly Channel<Guid> _channel;

        public ImageProcessingQueue()
        {
            var opttions = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            };
            _channel = Channel.CreateBounded<Guid>(opttions);
        }

        public bool TryEnqueue(Guid jobId)
        {
            return _channel.Writer.TryWrite(jobId);
        }

        public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }

        public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(jobId, cancellationToken);
        }
    }
}
