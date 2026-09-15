namespace Imageprocessing.Api.Services
{
    public enum ImageServiceError
    {
        InvalidFile,
        QueueFull,
        NotFound,
        StorageError,
    }

    public class ImageServiceResult<T>
    {
        public bool IsSuccess { get; init; }
        public T? Value { get; init; }
        public ImageServiceError? Error { get; init; }
        public string? ErrorMessage { get; init; }

        public static ImageServiceResult<T> Success(T value) =>
            new ImageServiceResult<T> { IsSuccess = true, Value = value };

        public static ImageServiceResult<T> Failure(
            ImageServiceError error,
            string? errorMessage = null
        ) =>
            new ImageServiceResult<T>
            {
                IsSuccess = false,
                Error = error,
                ErrorMessage = errorMessage,
            };
    }
}
