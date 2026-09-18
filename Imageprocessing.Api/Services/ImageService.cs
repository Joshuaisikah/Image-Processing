using Imageprocessing.Api.DTOs;
using Imageprocessing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Imageprocessing.Api.Services
{
    public class ImageService
    {
        private readonly IImageProcessingQueue _imageProcessingQueue;
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ImageService> _logger;

        public ImageService(
            IImageProcessingQueue imageProcessingQueue,
            AppDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ImageService> logger
        )
        {
            _imageProcessingQueue = imageProcessingQueue;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<ImageServiceResult<Guid>> CreateAsync(
            IFormFile file,
            CancellationToken cancellationToken = default
        )
        {
            //validate file size is less than 10 MB
            if (file.Length > 10 * 1024 * 1024)
            {
                return ImageServiceResult<Guid>.Failure(
                    ImageServiceError.InvalidFile,
                    "File size exceeds the maximum limit of 10 MB."
                );
            }
            //validate file type is image
            if (!file.ContentType.StartsWith("image/"))
            {
                return ImageServiceResult<Guid>.Failure(
                    ImageServiceError.InvalidFile,
                    "Invalid file type. Only image files are allowed."
                );
            }
            var imageId = Guid.NewGuid();
            var filePath = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                "uploads",
                $"{imageId}{Path.GetExtension(file.FileName)}"
            );
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var ImageEntity = new ImageJob
            {
                Id = imageId,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                OriginalPath = filePath,
                Status = ImageStatus.Pending,

                CreatedAt = DateTimeOffset.UtcNow,
            };

            _dbContext.ImageJobs.Add(ImageEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error saving file:{filepname} to disk:{filepath}",
                    imageId,
                    filePath
                );
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception clenupEx)
                {
                    _logger.LogError(
                        clenupEx,
                        "Error cleaning up file:{filepath} after failed save",
                        filePath
                    );
                }
                ImageEntity.Status = ImageStatus.Failed;
                ImageEntity.FailureReason = "Error saving file to disk";
                await _dbContext.SaveChangesAsync(cancellationToken);
                return ImageServiceResult<Guid>.Failure(
                    ImageServiceError.StorageError,
                    "Error saving file to disk"
                );
            }
        //lets enqueue the image for processing
var queued = _imageProcessingQueue.TryEnqueue(imageId);
        if(!queued){
_logger.LogWarning("Failed to enqueue image job {ImageId} for processing.", imageId);
               }
            return ImageServiceResult<Guid>.Success(imageId);
        }

        public async Task<ImageServiceResult<ImageStatusResponse>> GetStatusAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            var imageJob = await _dbContext
                .ImageJobs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (imageJob == null)
            {
                return ImageServiceResult<ImageStatusResponse>.Failure(
                    ImageServiceError.NotFound,
                    $"Image found with id {id}"
                );
            }
            return ImageServiceResult<ImageStatusResponse>.Success(
                new ImageStatusResponse
                {
                    Id = imageJob.Id,
                    Status = imageJob.Status,
                    FailureReason = imageJob.FailureReason,
                    CreatedAt = imageJob.CreatedAt,
                    CompletedAt = imageJob.CompletedAt,
                }
            );
        }

        public async Task<ImageServiceResult<string>> GetThumbnailPathAsync(
            Guid id,
            string size,
            CancellationToken cancellationToken = default
        )
        {
            var imageJob = await _dbContext
                .ImageJobs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (imageJob == null || imageJob.Status != ImageStatus.Completed)
            {
                return ImageServiceResult<string>.Failure(
                    ImageServiceError.NotFound,
                    $"Image not found or not completed with id {id}"
                );
            }
            var thumbnailPath = ThumbnailPathHelper.GetPath(
                Path.Combine(_webHostEnvironment.ContentRootPath, "uploads"),
                id,
                size
            );
            if (!File.Exists(thumbnailPath))
            {
                return ImageServiceResult<string>.Failure(
                    ImageServiceError.NotFound,
                    $"Thumbnail not found for image id {id} and size {size}"
                );
            }
            return ImageServiceResult<string>.Success(thumbnailPath);
        }
    }
}
