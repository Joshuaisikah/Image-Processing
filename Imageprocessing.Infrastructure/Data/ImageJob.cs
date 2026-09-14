namespace Imageprocessing.Infrastructure.Data
{
    public class ImageJob
    {
        public Guid Id { get; set; }
        public ImageStatus status { set; get; }
        public string? FailureReason { set; get; }

        // original file info
        public string OriginalFileName { set; get; } = null!;
        public string ContentType { get; set; } = null!;
        public string OriginalPath { set; get; } = null!;
        public DateTimeOffset CreatedAt { set; get; }
        public DateTimeOffset? CompletedAt { set; get; }
    }
}
