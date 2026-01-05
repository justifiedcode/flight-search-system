namespace FlightSearch.Core.Application.DataSets;

// DTOs for data transfer between layers
public record FlightSearchRequestDto(
    string SearchId,
    string Origin,
    string Destination,
    DateTime DepartureDate,
    int Passengers,
    string CabinClass
);

public record ProviderSearchRequestDto(
    string SearchId,
    string ProviderId,
    string Origin,
    string Destination,
    DateTime DepartureDate,
    int Passengers,
    string CabinClass
);

public record ProviderSearchResponseDto(
    string SearchId,
    string ProviderId,
    List<FlightOfferDto> Flights,
    bool Success,
    string? Error
);

public record SearchCompletedDto(
    string SearchId,
    List<FlightOfferDto> AllFlights,
  List<string> Errors
);

public record SearchStatusResponseDto(
    string SearchId,
    string Status, // "pending", "completed", "timeout"
    List<FlightOfferDto> Results,
    List<string> Errors,
    int Progress // 0-100
);

public class FlightOfferDto
{
    public string Provider { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int Duration { get; set; } // minutes
    public int Stops { get; set; }
}

// External HTTP request/response models
public class FlightProviderRequestDto
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public int Passengers { get; set; }
    public string CabinClass { get; set; } = string.Empty;
}

public class FlightProviderResponseDto
{
    public string Provider { get; set; } = string.Empty;
    public List<FlightOfferDto> Flights { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TimeSpan ResponseTime { get; set; }
}