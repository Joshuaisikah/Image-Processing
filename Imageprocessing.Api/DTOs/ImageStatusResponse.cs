using Imageprocessing.Infrastructure.Data;

namespace Imageprocessing.Api.DTOs;

public class ImageStatusResponse
{
    public Guid Id { get; set; }
    public ImageStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
