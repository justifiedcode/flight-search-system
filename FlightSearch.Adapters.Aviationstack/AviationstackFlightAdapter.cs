using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Outgoing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlightSearch.Adapters.Aviationstack;

/// <summary>
/// Aviationstack flight provider adapter (Simulation Only)
/// Simulates Aviationstack API responses with configurable delays and failure rates
/// </summary>
public class AviationstackFlightAdapter : IFlightProviderPort
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AviationstackFlightAdapter> _logger;

    public string ProviderId => "Aviationstack";

    public AviationstackFlightAdapter(
      IConfiguration configuration,
        ILogger<AviationstackFlightAdapter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FlightProviderResponseDto> SearchFlightsAsync(FlightProviderRequestDto request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("🛫 Aviationstack: Simulating flight search for {Origin} → {Destination} on {Date}",
            request.Origin, request.Destination, request.DepartureDate.ToString("yyyy-MM-dd"));

            // Get simulation configuration
            var minDelay = _configuration.GetValue<int>("FlightProviders:Aviationstack:SimulationDelayMs:Min", 25000);
            var maxDelay = _configuration.GetValue<int>("FlightProviders:Aviationstack:SimulationDelayMs:Max", 30000);
            var failureRate = _configuration.GetValue<double>("FlightProviders:Aviationstack:FailureRate", 0.05);

            // Simulate occasional failures
            if (Random.Shared.NextDouble() < failureRate)
            {
                await Task.Delay(Random.Shared.Next(4000, 7000)); // Quick failure
                throw new InvalidOperationException("Aviationstack service temporarily unavailable");
            }

            // Simulate network delay
            var delay = Random.Shared.Next(minDelay, maxDelay);
            _logger.LogDebug("🛫 Aviationstack: Simulating {DelayMs}ms response time", delay);
            await Task.Delay(delay);

            // Generate mock flight data
            var mockFlights = GenerateAviationstackFlights(request);

            stopwatch.Stop();
            _logger.LogInformation("✅ Aviationstack: Simulation completed - {FlightCount} flights in {ElapsedMs}ms",
            mockFlights.Count, stopwatch.ElapsedMilliseconds);

            return new FlightProviderResponseDto
            {
                Provider = ProviderId,
                Flights = mockFlights,
                Success = true,
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ Aviationstack: Simulation error");

            return new FlightProviderResponseDto
            {
                Provider = ProviderId,
                Flights = new List<FlightOfferDto>(),
                Success = false,
                Error = ex.Message,
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    private List<FlightOfferDto> GenerateAviationstackFlights(FlightProviderRequestDto request)
    {
        var flights = new List<FlightOfferDto>();
        var random = new Random((request.Origin + request.Destination).GetHashCode());
        var flightCount = random.Next(1, 4); // 1-3 flights (fewer than others)

        var airlines = new[] { "Southwest Airlines", "JetBlue Airways", "Alaska Airlines", "Frontier Airlines" };
        var flightNumbers = new[] { "WN101", "B6205", "AS150", "F9420" };

        for (int i = 0; i < flightCount; i++)
        {
            var airline = airlines[random.Next(airlines.Length)];
            var flightNumber = flightNumbers[random.Next(flightNumbers.Length)];
            var departureTime = request.DepartureDate.AddHours(random.Next(10, 18));
            var duration = random.Next(150, 420); // 2.5-7 hours
            var stops = random.Next(0, 2); // 0-1 stops
            var basePrice = random.Next(150, 600);
            var price = basePrice + (stops * 40); // Add cost for stops

            flights.Add(new FlightOfferDto
            {
                Provider = ProviderId,
                FlightNumber = flightNumber,
                Airline = airline,
                DepartureTime = departureTime,
                ArrivalTime = departureTime.AddMinutes(duration),
                Origin = request.Origin,
                Destination = request.Destination,
                Price = price,
                Currency = "USD",
                Duration = duration,
                Stops = stops
            });
        }

        return flights;
    }
}