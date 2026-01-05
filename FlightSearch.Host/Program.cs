using FlightSearch.Core.Application.Ports.Incoming;
using FlightSearch.Core.Application.Ports.Outgoing;
using FlightSearch.Core.Application.UseCases;
using FlightSearch.Adapters.Database;
using FlightSearch.Adapters.SNS;
using FlightSearch.Adapters.SQS;
using FlightSearch.Adapters.Amadeus;
using FlightSearch.Adapters.Skyscanner;
using FlightSearch.Adapters.Aviationstack;
using FlightSearch.Host.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for standalone HTML testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowHtmlFiles", policy =>
  {
      policy.AllowAnyOrigin()
     .AllowAnyMethod()
         .AllowAnyHeader();
  });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Flight Search API - Simulation Architecture",
        Version = "v1",
        Description = "Hexagonal architecture with simulated flight providers and configurable delays"
    });
});

// ===== HEXAGONAL ARCHITECTURE - DRIVEN ADAPTERS (SECONDARY PORTS) =====

// Database Adapter
builder.Services.AddSingleton<IDatabasePort, InMemoryDatabaseAdapter>();

// Message Infrastructure Adapters (used directly by Host services)
builder.Services.AddSingleton<InMemorySNSAdapter>();
builder.Services.AddSingleton<InMemorySQSAdapter>();

// Message Ports (used by Core services)
builder.Services.AddSingleton<IDispatcherPort>(provider => provider.GetRequiredService<InMemorySNSAdapter>());
builder.Services.AddSingleton<IResponsePort>(provider => provider.GetRequiredService<InMemorySQSAdapter>());

// ===== SIMULATED FLIGHT PROVIDER ADAPTERS =====
// All providers are now simulation-only with configurable delays from appsettings
builder.Services.AddTransient<IFlightProviderPort, AmadeusFlightAdapter>();
builder.Services.AddTransient<IFlightProviderPort, SkyscannerFlightAdapter>();
builder.Services.AddTransient<IFlightProviderPort, AviationstackFlightAdapter>();

// ===== HEXAGONAL ARCHITECTURE - CORE DOMAIN SERVICES =====
builder.Services.AddSingleton<FlightSearchService>();
builder.Services.AddSingleton<IFlightSearchPort>(provider => provider.GetRequiredService<FlightSearchService>());

builder.Services.AddSingleton<InquiryService>();
builder.Services.AddSingleton<IInquiryPort>(provider => provider.GetRequiredService<InquiryService>());

// ===== HEXAGONAL ARCHITECTURE - DRIVING ADAPTERS (PRIMARY PORTS) =====
// Background Services (Driving Adapters that handle infrastructure and call core)
builder.Services.AddHostedService<AmadeusResponderService>();
builder.Services.AddHostedService<SkyscannerResponderService>();
builder.Services.AddHostedService<AviationstackResponderService>();
builder.Services.AddHostedService<AggregatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
      {
          c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight Search API - Simulation Architecture v1");
          c.RoutePrefix = string.Empty;
      });
}

// Enable CORS for standalone HTML files
app.UseCors("AllowHtmlFiles");

// Enable static file serving for HTML interface
app.UseStaticFiles();

app.UseHttpsRedirection();
app.MapControllers();

// Log startup information
app.Logger.LogInformation("🚀 Flight Search Application started - SIMULATION MODE");
app.Logger.LogInformation("📐 Architecture: Clean Hexagonal with Simulated Providers");
app.Logger.LogInformation("🎯 Pattern: Infrastructure in Host, Business Logic in Core");
app.Logger.LogInformation("🌐 CORS: Enabled for standalone HTML testing");
app.Logger.LogInformation("📁 Static Files: Available at /flight-search.html");
app.Logger.LogInformation("✅ ARCHITECTURE LAYERS:");
app.Logger.LogInformation("   🔵 CORE DOMAIN: Pure business logic (no infrastructure dependencies)");
app.Logger.LogInformation("      📋 Services: FlightSearchService, InquiryService");
app.Logger.LogInformation("   🟢 PRIMARY PORTS: IFlightSearchPort, IInquiryPort");
app.Logger.LogInformation("   🟡 DRIVING ADAPTERS: Infrastructure → Core");
app.Logger.LogInformation(" 🌐 HTTP: FlightSearchController");
app.Logger.LogInformation("    📡 Background: ResponderServices (handle SNS consumption + call Core)");
app.Logger.LogInformation("      📥 Background: AggregatorService (handle SQS consumption + call Core)");
app.Logger.LogInformation("   🔴 SECONDARY PORTS: IDispatcherPort, IResponsePort, IDatabasePort, IFlightProviderPort");
app.Logger.LogInformation("   🟠 DRIVEN ADAPTERS: Core → Infrastructure");
app.Logger.LogInformation("    📡 SNS, 📥 SQS, 💾 Database");
app.Logger.LogInformation("🎭 SIMULATED PROVIDERS:");
app.Logger.LogInformation("   🛩️ Amadeus: 8-12s delay, 5% failure rate");
app.Logger.LogInformation("   ✈️ Skyscanner: 15-20s delay, 10% failure rate");
app.Logger.LogInformation("   🛫 Aviationstack: 25-30s delay, 5% failure rate");
app.Logger.LogInformation("🎨 HTML INTERFACE:");
app.Logger.LogInformation("   📱 Standalone HTML: https://localhost:5001/flight-search.html");
app.Logger.LogInformation("   ✨ Features: Real-time progress, scatter-gather visualization");
app.Logger.LogInformation("✨ BENEFITS:");
app.Logger.LogInformation("✅ No external API dependencies");
app.Logger.LogInformation("   ✅ Configurable delays and failure rates");
app.Logger.LogInformation("   ✅ Predictable testing environment");
app.Logger.LogInformation("   ✅ Perfect scatter-gather demonstration");

app.Run();