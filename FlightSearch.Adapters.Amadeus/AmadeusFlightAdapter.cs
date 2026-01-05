using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Outgoing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlightSearch.Adapters.Amadeus;

/// <summary>
/// Amadeus flight provider adapter (Simulation Only)
/// Simulates Amadeus API responses with configurable delays and failure rates
/// </summary>
public class AmadeusFlightAdapter : IFlightProviderPort
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AmadeusFlightAdapter> _logger;

    public string ProviderId => "Amadeus";

    public AmadeusFlightAdapter(
        IConfiguration configuration,
   ILogger<AmadeusFlightAdapter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FlightProviderResponseDto> SearchFlightsAsync(FlightProviderRequestDto request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("🛩️ Amadeus: Simulating flight search for {Origin} → {Destination} on {Date}",
            request.Origin, request.Destination, request.DepartureDate.ToString("yyyy-MM-dd"));

            // Get simulation configuration
            var minDelay = _configuration.GetValue<int>("FlightProviders:Amadeus:SimulationDelayMs:Min", 8000);
            var maxDelay = _configuration.GetValue<int>("FlightProviders:Amadeus:SimulationDelayMs:Max", 12000);
            var failureRate = _configuration.GetValue<double>("FlightProviders:Amadeus:FailureRate", 0.05);

            // Simulate occasional failures
            if (Random.Shared.NextDouble() < failureRate)
            {
                await Task.Delay(Random.Shared.Next(2000, 5000)); // Quick failure
                throw new InvalidOperationException("Amadeus service temporarily unavailable");
            }

            // Simulate network delay
            var delay = Random.Shared.Next(minDelay, maxDelay);
            _logger.LogDebug("🛩️ Amadeus: Simulating {DelayMs}ms response time", delay);
            await Task.Delay(delay);

            // Generate mock flight data
            var mockFlights = GenerateAmadeusFlights(request);

            stopwatch.Stop();
            _logger.LogInformation("✅ Amadeus: Simulation completed - {FlightCount} flights in {ElapsedMs}ms",
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
            _logger.LogError(ex, "❌ Amadeus: Simulation error");

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

    private List<FlightOfferDto> GenerateAmadeusFlights(FlightProviderRequestDto request)
    {
        var flights = new List<FlightOfferDto>();
        var random = new Random(request.Origin.GetHashCode() + request.Destination.GetHashCode());
        var flightCount = random.Next(2, 5); // 2-4 flights

        var airlines = new[] { "American Airlines", "Delta Air Lines", "United Airlines", "British Airways" };
        var flightNumbers = new[] { "AA123", "AA456", "AA789", "AA321" };

        for (int i = 0; i < flightCount; i++)
        {
            var airline = airlines[random.Next(airlines.Length)];
            var flightNumber = flightNumbers[random.Next(flightNumbers.Length)];
            var departureTime = request.DepartureDate.AddHours(random.Next(6, 20));
            var duration = random.Next(180, 480); // 3-8 hours
            var stops = random.Next(0, 3); // 0-2 stops
            var basePrice = random.Next(200, 800);
            var price = basePrice + (stops * 50); // Add cost for stops

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