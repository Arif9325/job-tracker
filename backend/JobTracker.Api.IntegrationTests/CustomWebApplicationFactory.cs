using JobTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Api.IntegrationTests;

// Spins up the REAL app (Program.cs, real routing, real middleware
// pipeline, real JWT auth) in-process, but swaps the SQLite database
// for a fresh, isolated in-memory one — so these tests exercise actual
// HTTP requests through the actual API, without needing a real database
// file or a running server.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // A unique name per factory instance keeps every test class's data
    // isolated from every other test class running in the same process.
    private readonly string _dbName = $"IntegrationTestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}
