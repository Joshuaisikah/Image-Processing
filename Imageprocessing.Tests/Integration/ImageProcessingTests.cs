using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Imageprocessing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Imageprocessing.Tests.Integration;

public class ImageProcessingTests : IClassFixture<ImageProcessingApiFactory>
{
    private readonly HttpClient _client;
    private readonly ImageProcessingApiFactory _factory;

    public ImageProcessingTests(ImageProcessingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static ByteArrayContent CreateTestImage(
        string contentType = "image/jpeg",
        int sizeKb = 1
    )
    {
        // Minimal valid 1×1 pixel JPEG
        byte[] bytes =
        {
            0xFF,
            0xD8,
            0xFF,
            0xE0,
            0x00,
            0x10,
            0x4A,
            0x46,
            0x49,
            0x46,
            0x00,
            0x01,
            0x01,
            0x00,
            0x00,
            0x01,
            0x00,
            0x01,
            0x00,
            0x00,
            0xFF,
            0xDB,
            0x00,
            0x43,
            0x00,
            0x08,
            0x06,
            0x06,
            0x07,
            0x06,
            0x05,
            0x08,
            0x07,
            0x07,
            0x07,
            0x09,
            0x09,
            0x08,
            0x0A,
            0x0C,
            0x14,
            0x0D,
            0x0C,
            0x0B,
            0x0B,
            0x0C,
            0x19,
            0x12,
            0x13,
            0x0F,
            0x14,
            0x1D,
            0x1A,
            0x1F,
            0x1E,
            0x1D,
            0x1A,
            0x1C,
            0x1C,
            0x20,
            0x24,
            0x2E,
            0x27,
            0x20,
            0x22,
            0x2C,
            0x23,
            0x1C,
            0x1C,
            0x28,
            0x37,
            0x29,
            0x2C,
            0x30,
            0x31,
            0x34,
            0x34,
            0x34,
            0x1F,
            0x27,
            0x39,
            0x3D,
            0x38,
            0x32,
            0x3C,
            0x2E,
            0x33,
            0x34,
            0x32,
            0xFF,
            0xC0,
            0x00,
            0x0B,
            0x08,
            0x00,
            0x01,
            0x00,
            0x01,
            0x01,
            0x01,
            0x11,
            0x00,
            0xFF,
            0xC4,
            0x00,
            0x1F,
            0x00,
            0x00,
            0x01,
            0x05,
            0x01,
            0x01,
            0x01,
            0x01,
            0x01,
            0x01,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x01,
            0x02,
            0x03,
            0x04,
            0x05,
            0x06,
            0x07,
            0x08,
            0x09,
            0x0A,
            0x0B,
            0xFF,
            0xC4,
            0x00,
            0xB5,
            0x10,
            0x00,
            0x02,
            0x01,
            0x03,
            0x03,
            0x02,
            0x04,
            0x03,
            0x05,
            0x05,
            0x04,
            0x04,
            0x00,
            0x00,
            0x01,
            0x7D,
            0x01,
            0x02,
            0x03,
            0x00,
            0x04,
            0x11,
            0x05,
            0x12,
            0x21,
            0x31,
            0x41,
            0x06,
            0x13,
            0x51,
            0x61,
            0x07,
            0x22,
            0x71,
            0x14,
            0x32,
            0x81,
            0x91,
            0xA1,
            0x08,
            0x23,
            0x42,
            0xB1,
            0xC1,
            0x15,
            0x52,
            0xD1,
            0xF0,
            0x24,
            0x33,
            0x62,
            0x72,
            0x82,
            0x09,
            0x0A,
            0x16,
            0x17,
            0x18,
            0x19,
            0x1A,
            0x25,
            0x26,
            0x27,
            0x28,
            0x29,
            0x2A,
            0x34,
            0x35,
            0x36,
            0x37,
            0x38,
            0x39,
            0x3A,
            0x43,
            0x44,
            0x45,
            0x46,
            0x47,
            0x48,
            0x49,
            0x4A,
            0x53,
            0x54,
            0x55,
            0x56,
            0x57,
            0x58,
            0x59,
            0x5A,
            0x63,
            0x64,
            0x65,
            0x66,
            0x67,
            0x68,
            0x69,
            0x6A,
            0x73,
            0x74,
            0x75,
            0x76,
            0x77,
            0x78,
            0x79,
            0x7A,
            0x83,
            0x84,
            0x85,
            0x86,
            0x87,
            0x88,
            0x89,
            0x8A,
            0x92,
            0x93,
            0x94,
            0x95,
            0x96,
            0x97,
            0x98,
            0x99,
            0x9A,
            0xA2,
            0xA3,
            0xA4,
            0xA5,
            0xA6,
            0xA7,
            0xA8,
            0xA9,
            0xAA,
            0xB2,
            0xB3,
            0xB4,
            0xB5,
            0xB6,
            0xB7,
            0xB8,
            0xB9,
            0xBA,
            0xC2,
            0xC3,
            0xC4,
            0xC5,
            0xC6,
            0xC7,
            0xC8,
            0xC9,
            0xCA,
            0xD2,
            0xD3,
            0xD4,
            0xD5,
            0xD6,
            0xD7,
            0xD8,
            0xD9,
            0xDA,
            0xE1,
            0xE2,
            0xE3,
            0xE4,
            0xE5,
            0xE6,
            0xE7,
            0xE8,
            0xE9,
            0xEA,
            0xF1,
            0xF2,
            0xF3,
            0xF4,
            0xF5,
            0xF6,
            0xF7,
            0xF8,
            0xF9,
            0xFA,
            0xFF,
            0xDA,
            0x00,
            0x08,
            0x01,
            0x01,
            0x00,
            0x00,
            0x3F,
            0x00,
            0xFB,
            0xD5,
            0xDB,
            0x20,
            0xA8,
            0xF1,
            0x45,
            0x00,
            0xFF,
            0xD9,
        };

        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }

    [Fact]
    public async Task POST_returns_202_with_Location_and_is_fast()
    {
        using var form = new MultipartFormDataContent();
        form.Add(CreateTestImage(), "file", "test.jpg");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.PostAsync("/api/images", form);
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/images/");
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task Status_transitions_Pending_to_Done()
    {
        using var form = new MultipartFormDataContent();
        form.Add(CreateTestImage(), "file", "test.jpg");

        var post = await _client.PostAsync("/api/images", form);
        post.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await post.Content.ReadFromJsonAsync<IdResponse>();
        var id = body!.Id;

        // ---- TEMPORARY DEBUG ----
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var exists = await db.ImageJobs.AnyAsync(x => x.Id == id);
            Console.WriteLine($"=== Job {id} exists in DB right after POST? {exists} ===");
            var all = await db.ImageJobs.Select(x => x.Id).ToListAsync();
            Console.WriteLine($"=== All IDs in DB: {string.Join(", ", all)} ===");
        }
        ImageStatus status = ImageStatus.Pending;

        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(250);

            var statusResponse = await _client.GetAsync($"/api/images/{id}");
            if (statusResponse.StatusCode != HttpStatusCode.OK)
            {
                var error = await statusResponse.Content.ReadAsStringAsync();
                throw new Exception(
                    $"GET /api/images/{id} returned {statusResponse.StatusCode}: {error}"
                );
            }

            var statusBody = await statusResponse.Content.ReadFromJsonAsync<StatusResponse>();
            status = statusBody!.Status;

            if (status is ImageStatus.Completed or ImageStatus.Failed)
                break;
        }

        status.Should().Be(ImageStatus.Completed);
    }

    [Fact]
    public async Task Upload_20_images_all_reach_Done()
    {
        var tasks = Enumerable
            .Range(0, 20)
            .Select(async i =>
            {
                using var form = new MultipartFormDataContent();
                form.Add(CreateTestImage(), "file", $"test-{i}.jpg");
                var response = await _client.PostAsync("/api/images", form);
                response.StatusCode.Should().Be(HttpStatusCode.Accepted);
                var body = await response.Content.ReadFromJsonAsync<IdResponse>();
                return body!.Id;
            });

        var ids = await Task.WhenAll(tasks);

        foreach (var id in ids)
        {
            ImageStatus status = ImageStatus.Pending;

            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(500);
                var res = await _client.GetAsync($"/api/images/{id}");
                res.StatusCode.Should().Be(HttpStatusCode.OK);

                var body = await res.Content.ReadFromJsonAsync<StatusResponse>();
                status = body!.Status;

                if (status is ImageStatus.Completed or ImageStatus.Failed)
                    break;
            }

            status.Should().Be(ImageStatus.Completed, $"image {id} should be Done");
        }
    }

    [Fact]
    public async Task Thumbnail_returns_404_while_Pending()
    {
        using var form = new MultipartFormDataContent();
        form.Add(CreateTestImage(), "file", "test.jpg");

        var post = await _client.PostAsync("/api/images", form);
        var body = await post.Content.ReadFromJsonAsync<IdResponse>();
        var id = body!.Id;

        var thumb = await _client.GetAsync($"/api/images/{id}/thumbnails/small");
        thumb.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Corrupt_file_ends_as_Failed_and_worker_survives()
    {
        var junk = new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        junk.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        // ... rest stays the same, expects Failed

        using var form = new MultipartFormDataContent();
        form.Add(junk, "file", "corrupt.jpg");

        var post = await _client.PostAsync("/api/images", form);
        post.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await post.Content.ReadFromJsonAsync<IdResponse>();
        var id = body!.Id;

        ImageStatus status = ImageStatus.Pending;
        string? reason = null;

        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(250);
            var res = await _client.GetAsync($"/api/images/{id}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var statusBody = await res.Content.ReadFromJsonAsync<StatusResponse>();
            status = statusBody!.Status;
            reason = statusBody.FailureReason;

            if (status is ImageStatus.Completed or ImageStatus.Failed)
                break;
        }

        status.Should().Be(ImageStatus.Failed);
        reason.Should().NotBeNullOrWhiteSpace();
    }

    // DTOs
    private record IdResponse(Guid Id);

    private record StatusResponse(
        Guid Id,
        ImageStatus Status,
        string? FailureReason,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt
    );
}
