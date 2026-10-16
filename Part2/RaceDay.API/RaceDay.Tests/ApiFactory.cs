using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;

namespace RaceDay.Tests;

/// <summary>
/// Starts the real API inside the test, but with an empty in-memory database
/// instead of SQL Server, so the tests never touch the real data.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    // A fresh database name per factory keeps test classes from sharing data
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public ApiFactory()
    {
        // Test-only settings. The real secret key lives in user secrets, which the tests do not use.
        Environment.SetEnvironmentVariable("Jwt__Key", "unit-test-signing-key-that-is-long-enough-1234567890");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "RaceDay.API");
        Environment.SetEnvironmentVariable("Jwt__Audience", "RaceDay.Clients");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<RaceDayContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
