using BlazorApp1.Components;
using BlazorApp1.Data;
using BlazorApp1.Features.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton<AnalysisStores>();
builder.Services.AddSingleton<SimulationResultsStore>();
builder.Services.AddSingleton<PresentationPreferencesStore>();
builder.Services.AddScoped<IQuoteDataSource, CsvQuoteDataSource>();
builder.Services.AddScoped<IQuoteDataSource, DatabaseQuoteDataSource>();
builder.Services.AddScoped<TechnicalAnalysisService>();
builder.Services.AddScoped<TradingSimulationService>();
builder.Services.AddScoped<AnalysisApplicationService>();
builder.Services.AddScoped<QuoteDataSetService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var seeder = scope.ServiceProvider.GetRequiredService<QuoteDataSetService>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    await seeder.SeedExampleDataAsync(env);
}

app.Run();



