using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Incoming;
using FlightSearch.Core.Application.Ports.Outgoing;
using FlightSearch.Core.Domain;
using Microsoft.Extensions.Configuration;

namespace FlightSearch.Core.Application.UseCases;

public class FlightSearchService : IFlightSearchPort
{
    private readonly IDatabasePort _database;
    private readonly IDispatcherPort _dispatcherPort;
    private readonly IConfiguration _configuration;

    public FlightSearchService(
        IDatabasePort database,
        IDispatcherPort dispatcherPort,
        IConfiguration configuration)
    {
        _database = database;
        _dispatcherPort = dispatcherPort;
        _configuration = configuration;
    }

    public async Task<string> StartSearchAsync(FlightSearchRequestDto request)
    {
        var searchId = request.SearchId;

        // Create and save search aggregate (Domain logic)
        var search = new FlightSearchAggregate(
            searchId, request.Origin, request.Destination,
            request.DepartureDate, request.Passengers, request.CabinClass);

        // Save to database via outgoing port
        await _database.SaveSearchAsync(search);

        // SCATTER: Dispatch search request via SNS-like dispatcher
        var searchInitiated = new ProviderSearchRequestDto(
            searchId, "ALL_PROVIDERS", request.Origin, request.Destination,
            request.DepartureDate, request.Passengers, request.CabinClass);

        await _dispatcherPort.DispatchSearchRequestAsync(searchInitiated);

        // Schedule timeout (configurable from appsettings)
        var timeoutSeconds = _configuration?.GetValue<int>("SearchTimeout:TimeoutSeconds") ?? 45;
        _ = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)).ContinueWith(async _ =>
          {
              var currentSearch = await _database.GetSearchAsync(searchId);
              if (currentSearch?.Status == SearchStatus.Pending)
              {
                  currentSearch.Timeout();
                  await _database.UpdateSearchAsync(currentSearch);
              }
          });

        return searchId;
    }

    public async Task<SearchStatusResponseDto> GetSearchStatusAsync(string searchId)
    {
        // Fetch from database via outgoing port
        var search = await _database.GetSearchAsync(searchId);
        if (search == null)
            throw new KeyNotFoundException($"Search {searchId} not found");

        return new SearchStatusResponseDto(
            searchId,
            search.Status.ToString().ToLower(),
            search.GetAllFlights(),
            search.Errors,
            search.GetProgress()
        );
    }

    public async Task ProcessProviderResponseAsync(ProviderSearchResponseDto response)
    {
        // GATHER: Process provider response
        var search = await _database.GetSearchAsync(response.SearchId);
        if (search == null) return;

        // Domain logic: Add provider response to aggregate
        search.AddProviderResponse(response.ProviderId, response.Flights, response.Success, response.Error);
        await _database.UpdateSearchAsync(search);

        // If search is complete, publish completion message via dispatcher
        if (search.Status == SearchStatus.Completed)
        {
            var completion = new SearchCompletedDto(search.SearchId, search.GetAllFlights(), search.Errors);
            await _dispatcherPort.DispatchSearchRequestAsync(completion);
        }
    }
}