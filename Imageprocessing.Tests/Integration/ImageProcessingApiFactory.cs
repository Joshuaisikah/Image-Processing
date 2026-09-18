using Imageprocessing.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Imageprocessing.Tests.Integration;

public class ImageProcessingApiFactory : WebApplicationFactory<Program>
{
    // Fixed name so every resolution uses the exact same in-memory store
    private const string DbName = "ImageProcessingTests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove every existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(AppDbContext));

            // Also remove any IDbContextOptionsConfiguration that might re-add Npgsql
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                    && d.ServiceType.Name.Contains("DbContext")
                )
                .ToList();
            foreach (var d in efDescriptors)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(DbName);
            });

            services.Configure<HostOptions>(opts =>
            {
                opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });
        });

        builder.UseEnvironment("Development");
    }
}
