using FlightSearch.Core.Application.DataSets;

namespace FlightSearch.Core.Domain;

public class FlightSearchAggregate
{
    public string SearchId { get; private set; } = string.Empty;
    public string Origin { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public DateTime DepartureDate { get; private set; }
    public int Passengers { get; private set; }
    public string CabinClass { get; private set; } = string.Empty;
    public SearchStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public List<ProviderResult> ProviderResults { get; private set; } = new();
    public List<string> Errors { get; private set; } = new();

    // Expected providers (for scatter-gather completion)
    public List<string> ExpectedProviders { get; private set; } = new() { "Amadeus", "Skyscanner", "Aviationstack" };

    public FlightSearchAggregate() { } // For persistence

    public FlightSearchAggregate(
        string searchId,
        string origin,
        string destination,
        DateTime departureDate,
        int passengers,
        string cabinClass)
    {
        SearchId = searchId;
        Origin = origin;
        Destination = destination;
        DepartureDate = departureDate;
        Passengers = passengers;
        CabinClass = cabinClass;
        Status = SearchStatus.Pending;
        StartedAt = DateTime.UtcNow;
    }

    public void AddProviderResponse(string providerId, List<FlightOfferDto> flights, bool success, string? error)
    {
        if (Status != SearchStatus.Pending) return;

        ProviderResults.Add(new ProviderResult
        {
            ProviderId = providerId,
            Flights = flights,
            Success = success,
            Error = error
        });

        if (!success && !string.IsNullOrEmpty(error))
        {
            Errors.Add($"{providerId}: {error}");
        }

        // Check if all providers responded (GATHER complete)
        if (ProviderResults.Count >= ExpectedProviders.Count)
        {
            Status = SearchStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void Timeout()
    {
        if (Status == SearchStatus.Pending)
        {
            Status = SearchStatus.Timeout;
            CompletedAt = DateTime.UtcNow;
        }
    }

    public List<FlightOfferDto> GetAllFlights() => ProviderResults
            .Where(p => p.Success)
            .SelectMany(p => p.Flights)
            .OrderBy(f => f.Price)
            .ToList();

    public int GetProgress() =>
        ExpectedProviders.Count > 0 ? (ProviderResults.Count * 100 / ExpectedProviders.Count) : 0;
}

public class ProviderResult
{
    public string ProviderId { get; set; } = string.Empty;
    public List<FlightOfferDto> Flights { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public enum SearchStatus
{
    Pending,
    Completed,
    Timeout
}