using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Serilog.Core;

namespace TaskBoard.IntegrationTests.Support;

/// <summary>
/// Hosts the real API pipeline (routing, serialization, DI, EF Core, SQL Server) against an
/// isolated test database, with a controllable clock and captured logs.
/// </summary>
internal sealed class TaskBoardApiFactory(string connectionString, string environment = "Testing")
    : WebApplicationFactory<Program>
{
    public const string FrontendOrigin = "http://localhost:4200";

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));

    public CapturingLogSink LogSink { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Cors:AllowedOrigins:0"] = FrontendOrigin,
                ["Serilog:MinimumLevel:Default"] = "Warning",
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<ILogEventSink>(LogSink);
        });
    }
}
