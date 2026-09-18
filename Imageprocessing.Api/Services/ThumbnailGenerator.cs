using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Imageprocessing.Api.Services
{
    public class ThumbnailGenerator
    {
        private static readonly Dictionary<string, Size> Sizes = new()
        {
            { "small", new Size(100, 100) },
            { "medium", new Size(300, 300) },
            { "large", new Size(600, 600) },
        };

        public async Task<IReadOnlyDictionary<string, byte[]>> GenerateThumbnailsAsync(
            Stream imageStream,
            CancellationToken cancellationToken = default
        )
        {
            // Load the image from the stream — this is where a corrupt/invalid
            // file throws, which the caller (worker) catches and maps to Failed.
            using var image = await Image.LoadAsync(imageStream, cancellationToken);

            var thumbnails = new Dictionary<string, byte[]>();

            foreach (var (name, size) in Sizes)
            {
                using var thumbnail = image.Clone(ctx =>
                    ctx.Resize(
                        new ResizeOptions
                        {
                            Size = size,
                            Mode = ResizeMode.Max, // preserves aspect ratio, fits within box, no upscaling
                        }
                    )
                );

                using var ms = new MemoryStream();
                await thumbnail.SaveAsJpegAsync(ms, cancellationToken);
                thumbnails[name] = ms.ToArray();
            }

            return thumbnails;
        }
    }
}
