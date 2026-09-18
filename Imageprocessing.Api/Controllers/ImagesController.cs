using Imageprocessing.Api.Services;
using  Microsoft.AspNetCore.Mvc;
namespace Imageprocessing.Api.Controllers;
[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly ImageService _imageService;
    private readonly ILogger<ImagesController> _logger;
    public ImagesController(ImageService imageService, ILogger<ImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }
    [HttpPost]
    public async Task<IActionResult> CreateImage(IFormFile file,CancellationToken ct){
    var result = await _imageService.CreateAsync(file, ct);
if (result.IsSuccess)
{
    return AcceptedAtAction(nameof(GetStatus),new{id = result.Value}, new { id = result.Value });
}
else
{
    return result.Error switch
    {
        ImageServiceError.InvalidFile => BadRequest(result.ErrorMessage),
        _ => StatusCode(StatusCodes.Status500InternalServerError, "unexpected error occurred"),
    };
}    
}
[HttpGet("{id}")]
public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
{
var result = await _imageService.GetStatusAsync(id, ct);
return result.IsSuccess ? Ok(result.Value) : result.Error switch
{
    ImageServiceError.NotFound => NotFound(),
    _ => StatusCode(StatusCodes.Status500InternalServerError, "unexpected error occurred"),
};

}

 [HttpGet("{id}/thumbnails/{size}")]
    public async Task<IActionResult> GetThumbnail(Guid id, string size, CancellationToken ct)
    {
        var result = await  _imageService.GetThumbnailPathAsync(id, size, ct);

        if (!result.IsSuccess)
        {
            return NotFound();
        }

        return PhysicalFile(result.Value!, "image/jpeg");
    }
}

