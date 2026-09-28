using Microsoft.AspNetCore.Cors.Infrastructure;

namespace TaskBoard.Api.Extensions;

internal static class CorsExtensions
{
    public const string FrontendPolicy = "Frontend";

    /// <summary>
    /// Allows only the configured frontend origins (<c>Cors:AllowedOrigins</c>). No origin is
    /// allowed when the setting is absent. The API uses no cookies or credentials.
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services)
    {
        services.AddCors();

        services.AddOptions<CorsOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                var allowedOrigins = configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? [];

                options.AddPolicy(FrontendPolicy, policy => policy
                    .WithOrigins(allowedOrigins)
                    .WithMethods(
                        HttpMethods.Get,
                        HttpMethods.Post,
                        HttpMethods.Put,
                        HttpMethods.Patch,
                        HttpMethods.Delete)
                    .AllowAnyHeader());
            });

        return services;
    }
}
