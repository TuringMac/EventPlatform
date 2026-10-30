using EventPlatform.Common.Infrastructure.Options;
using EventPlatform.Users.Api;
using EventPlatform.Users.Application;
using EventPlatform.Users.Infrastructure;
using EventPlatform.Users.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Издатель JWT не настроен.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Потребитель JWT не настроен.")
            .Validate(options => Encoding.UTF8.GetByteCount(options.Key) >= 32, "Ключ JWT должен быть не менее 32 байт.")
            .Validate(options => options.Lifetime > 0, "Время жизни JWT должно быть положительным.")
            .ValidateOnStart();

// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPresentation();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()    // Метрики HTTP-запросов
            .AddRuntimeInstrumentation()       // Метрики среды выполнения (CPU, память, GC)
            .AddPrometheusExporter();          // Экспорт в формате Prometheus
    })
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            // Задаём имя нашего сервиса в системе мониторинга
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("UsersService"))
            // Автоматически собираем все входящие HTTP-запросы к нашему API
            .AddAspNetCoreInstrumentation()
            // Автоматически отслеживаем все исходящие HTTP-вызовы через HttpClient
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            // Настраиваем экспорт по протоколу OTLP
            .AddOtlpExporter(options =>
            {
                // Настройка пакетного процессора
                // Отправка каждые 2 секунды
                options.BatchExportProcessorOptions.ScheduledDelayMilliseconds = 2000;
                // Тайм-аут ответа 3 секунды
                options.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = 3000;
            });
    })
    .WithLogging(logging =>
        logging
            .AddOtlpExporter(options =>
            {
                // Настройка пакетного процессора
                // Отправка каждые 2 секунды
                options.BatchExportProcessorOptions.ScheduledDelayMilliseconds = 2000;
                // Тайм-аут ответа 3 секунды
                options.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = 3000;
            })
            .AddConsoleExporter());
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console(new CompactJsonFormatter()));

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

var app = builder.Build();

// ProblemDetails
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Direct Swagger UI to consume the native JSON file
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "My API v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapPrometheusScrapingEndpoint();
app.MapControllers();

app.Run();
