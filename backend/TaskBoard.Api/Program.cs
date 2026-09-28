using System.Text.Json.Serialization;
using Serilog;
using TaskBoard.Api.ErrorHandling;
using TaskBoard.Api.Extensions;
using TaskBoard.Api.Filters;
using TaskBoard.Application;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddFrontendCors();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(SwaggerDocument.Name, new() { Title = "TaskBoard API", Version = "1.0" });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"));
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint($"/swagger/{SwaggerDocument.Name}/swagger.json", "TaskBoard API"));
}

app.UseCors(CorsExtensions.FrontendPolicy);

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDevelopmentDataAsync();
}

await app.RunAsync();

internal static class SwaggerDocument
{
    public const string Name = "taskboard";
}
