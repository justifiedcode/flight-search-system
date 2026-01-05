using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Incoming;
using Microsoft.AspNetCore.Mvc;

namespace FlightSearch.Host.Controllers;

/// <summary>
/// API Controller - Handles HTTP requests and processes them in-memory via background services
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FlightSearchController : ControllerBase
{
    private readonly IFlightSearchPort _flightSearchService;
    private readonly ILogger<FlightSearchController> _logger;

    public FlightSearchController(
        IFlightSearchPort flightSearchService,
        ILogger<FlightSearchController> logger)
    {
        _flightSearchService = flightSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate a flight search - dispatches to in-memory channels and returns immediately
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<object>> SearchFlights([FromBody] ApiFlightSearchRequest request)
    {
        try
        {
            // Validate incoming request
            if (string.IsNullOrEmpty(request.Origin) || string.IsNullOrEmpty(request.Destination))
                return BadRequest("Origin and Destination are required");

            if (request.DepartureDate < DateTime.Today)
                return BadRequest("Departure date cannot be in the past");

            if (request.Passengers <= 0 || request.Passengers > 9)
                return BadRequest("Passengers must be between 1 and 9");

            // Convert to internal DTO
            var searchRequest = new FlightSearchRequestDto(
              Guid.NewGuid().ToString(),
               request.Origin,
           request.Destination,
                   request.DepartureDate,
                         request.Passengers,
           request.CabinClass ?? "economy"
                     );

            // Call service - dispatches to in-memory channels
            var searchId = await _flightSearchService.StartSearchAsync(searchRequest);

            _logger.LogInformation("?? Initiated search {SearchId} - Background services will process it", searchId);

            return Accepted(new
            {
                searchId = searchId,
                status = "initiated",
                statusUrl = $"/api/flightsearch/status/{searchId}",
                message = "Search request dispatched to background services",
                processing = new
                {
                    architecture = "Single process with in-memory channels",
                    responders = "Background services call provider APIs",
                    aggregator = "Background service collects responses"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating flight search");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get search status and results - reads from in-memory database
    /// </summary>
    [HttpGet("status/{searchId}")]
    public async Task<ActionResult<ApiSearchStatusResponse>> GetSearchStatus(string searchId)
    {
        try
        {
            var status = await _flightSearchService.GetSearchStatusAsync(searchId);

            var apiResponse = new ApiSearchStatusResponse
            {
                SearchId = status.SearchId,
                Status = status.Status,
                Results = status.Results.Select(f => new ApiFlightOffer
                {
                    Provider = f.Provider,
                    FlightNumber = f.FlightNumber,
                    Airline = f.Airline,
                    DepartureTime = f.DepartureTime,
                    ArrivalTime = f.ArrivalTime,
                    Origin = f.Origin,
                    Destination = f.Destination,
                    Price = f.Price,
                    Currency = f.Currency,
                    Duration = f.Duration,
                    Stops = f.Stops
                }).ToList(),
                Errors = status.Errors,
                Progress = status.Progress
            };

            return Ok(apiResponse);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Search {searchId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search status");
            return StatusCode(500, "An error occurred while checking search status");
        }
    }

    /// <summary>
    /// Get architecture information
    /// </summary>
    [HttpGet("info")]
    public ActionResult<object> GetInfo()
    {
        return Ok(new
        {
            pattern = "Scatter-Gather",
            architecture = "Hexagonal with In-Memory Channels",
            description = "Single process with background services and in-memory messaging",
            flow = new[]
            {
     "1. API Controller ? FlightSearchService ? InMemory Dispatcher Channel",
        "2. Responder Services ? Listen to Channel ? Call Provider APIs",
       "3. Provider APIs ? InMemory Response Channel ? Aggregator Service",
         "4. Aggregator Service ? FlightSearchService ? Database ? API Response"
    },
            components = new
            {
                webApi = "ASP.NET Core Web API - HTTP endpoints",
                backgroundServices = "Hosted services for async processing",
                messaging = "In-memory channels for reliable messaging",
                database = "In-memory database for state management"
            },
            benefits = new[]
            {
          "Instant messaging - no file I/O delays",
     "Reliable processing - no race conditions",
           "Observable flow - easy debugging",
                "Single process - simple deployment"
 }
        });
    }

    /// <summary>
    /// Get sample request
    /// </summary>
    [HttpGet("sample")]
    public ActionResult<ApiFlightSearchRequest> GetSample()
    {
        return Ok(new ApiFlightSearchRequest
        {
            Origin = "JFK",
            Destination = "LAX",
            DepartureDate = DateTime.Today.AddDays(7),
            Passengers = 2,
            CabinClass = "economy"
        });
    }
}

// External API models
public class ApiFlightSearchRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public int Passengers { get; set; }
    public string? CabinClass { get; set; }
}

public class ApiSearchStatusResponse
{
    public string SearchId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<ApiFlightOffer> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int Progress { get; set; }
}

public class ApiFlightOffer
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
    public int Duration { get; set; }
    public int Stops { get; set; }
}