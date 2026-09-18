using System.Drawing;

namespace Imageprocessing.Api.Services;

public static class ThumbnailSizes
{
    public const string Small = "small";
    public const string Medium = "medium";
    public const string Large = "large";

    public static readonly IReadOnlyDictionary<string, Size> Dimensions = new Dictionary<
        string,
        Size
    >
    {
        { Small, new Size(100, 100) },
        { Medium, new Size(300, 300) },
        { Large, new Size(600, 600) },
    };
}
