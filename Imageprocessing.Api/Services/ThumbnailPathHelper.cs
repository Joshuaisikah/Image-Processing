namespace Imageprocessing.Api.Services;

public static class ThumbnailPathHelper
{
    public static string GetPath(string uploadsRoot, Guid imageId, string size) =>
        Path.Combine(uploadsRoot, $"{imageId}_{size}.jpg");
}
