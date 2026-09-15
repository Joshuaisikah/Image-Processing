using Imageprocessing.Api.Services;
using Imageprocessing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddSingleton<IImageProcessingQueue, ImageProcessingQueue>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Image Processing API")
            .WithTheme(ScalarTheme.Purple) // or Moon, Solarized, BluePlanet, etc.
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithSearchHotKey("l"); // press L to focus search
    });
}
app.MapControllers();
app.Run();
