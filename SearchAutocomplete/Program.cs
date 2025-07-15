using Microsoft.EntityFrameworkCore;
using Serilog;
using SearchAutocomplete.Application.Interfaces;
using SearchAutocomplete.Application.Services;
using SearchAutocomplete.Domain.Interfaces;
using SearchAutocomplete.Infrastructure.Data;
using SearchAutocomplete.Infrastructure.Middleware;
using SearchAutocomplete.Infrastructure.Repositories;
using SearchAutocomplete.Infrastructure.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/search-autocomplete-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<SearchDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IKinhThanhRepository, KinhThanhRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();

// Register application services
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IAutocompleteService, AutocompleteService>();

// Register infrastructure services
builder.Services.AddScoped<ResilienceService>();
builder.Services.AddScoped<SearchAutocomplete.Infrastructure.Logging.PerformanceLogger>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add global exception middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors();

app.UseRouting();

app.UseAuthorization();

// Map controllers
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
    
    try
    {
        var seeder = new DatabaseSeeder(context, logger);
        await seeder.SeedAsync();
        Log.Information("Database initialized and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error occurred while initializing and seeding database");
    }
}

Log.Information("Search Autocomplete application starting up");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
