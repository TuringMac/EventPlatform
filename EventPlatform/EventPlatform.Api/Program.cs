using EventPlatform.Api;
using EventPlatform.Application;
using EventPlatform.Infrastructure;
using EventPlatform.Infrastructure.DbContexts;
using EventPlatform.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Издатель JWT не настроен.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Потребитель JWT не настроен.")
            .Validate(options => Encoding.UTF8.GetByteCount(options.Key) >= 32, "Ключ JWT должен быть не менее 32 байт.")
            .Validate(options => options.Lifetime > 0, "Время жизни JWT должно быть положительным.")
            .ValidateOnStart();
builder.Services.AddOptions<BookingOptions>()
            .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
            .Validate(options => options.PerUserLimit > 0, "Лимит бронирований на пользователя должен быть положительным.")
            .ValidateOnStart();

// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPresentation();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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

app.MapControllers();

app.Run();
