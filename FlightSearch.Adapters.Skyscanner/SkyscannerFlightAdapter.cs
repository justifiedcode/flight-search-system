using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Outgoing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlightSearch.Adapters.Skyscanner;

/// <summary>
/// Skyscanner flight provider adapter (Simulation Only)
/// Simulates Skyscanner API responses with configurable delays and failure rates
/// </summary>
public class SkyscannerFlightAdapter : IFlightProviderPort
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SkyscannerFlightAdapter> _logger;

    public string ProviderId => "Skyscanner";

    public SkyscannerFlightAdapter(
        IConfiguration configuration,
        ILogger<SkyscannerFlightAdapter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FlightProviderResponseDto> SearchFlightsAsync(FlightProviderRequestDto request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("✈️ Skyscanner: Simulating flight search for {Origin} → {Destination} on {Date}",
          request.Origin, request.Destination, request.DepartureDate.ToString("yyyy-MM-dd"));

            // Get simulation configuration
            var minDelay = _configuration.GetValue<int>("FlightProviders:Skyscanner:SimulationDelayMs:Min", 15000);
            var maxDelay = _configuration.GetValue<int>("FlightProviders:Skyscanner:SimulationDelayMs:Max", 20000);
            var failureRate = _configuration.GetValue<double>("FlightProviders:Skyscanner:FailureRate", 0.10);

            // Simulate occasional failures
            if (Random.Shared.NextDouble() < failureRate)
            {
                await Task.Delay(Random.Shared.Next(3000, 6000)); // Quick failure
                throw new InvalidOperationException("Skyscanner service temporarily unavailable");
            }

            // Simulate network delay
            var delay = Random.Shared.Next(minDelay, maxDelay);
            _logger.LogDebug("✈️ Skyscanner: Simulating {DelayMs}ms response time", delay);
            await Task.Delay(delay);

            // Generate mock flight data
            var mockFlights = GenerateSkyscannerFlights(request);

            stopwatch.Stop();
            _logger.LogInformation("✅ Skyscanner: Simulation completed - {FlightCount} flights in {ElapsedMs}ms",
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
            _logger.LogError(ex, "❌ Skyscanner: Simulation error");

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

    private List<FlightOfferDto> GenerateSkyscannerFlights(FlightProviderRequestDto request)
    {
        var flights = new List<FlightOfferDto>();
        var random = new Random(request.Destination.GetHashCode() + request.Origin.GetHashCode());
        var flightCount = random.Next(3, 6); // 3-5 flights

        var airlines = new[] { "Lufthansa", "Emirates", "Qatar Airways", "Singapore Airlines", "Air France" };
        var flightNumbers = new[] { "LH001", "EK205", "QR915", "SQ25", "AF447" };

        for (int i = 0; i < flightCount; i++)
        {
            var airline = airlines[random.Next(airlines.Length)];
            var flightNumber = flightNumbers[random.Next(flightNumbers.Length)];
            var departureTime = request.DepartureDate.AddHours(random.Next(8, 22));
            var duration = random.Next(200, 520); // 3.5-8.5 hours
            var stops = random.Next(0, 2); // 0-1 stops
            var basePrice = random.Next(180, 750);
            var price = basePrice + (stops * 75); // Add cost for stops

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