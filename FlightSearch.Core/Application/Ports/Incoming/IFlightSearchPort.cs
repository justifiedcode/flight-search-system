using FlightSearch.Core.Application.DataSets;

namespace FlightSearch.Core.Application.Ports.Incoming;

/// <summary>
/// Primary port - Incoming requests from driving adapters (API)
/// </summary>
public interface IFlightSearchPort
{
    Task<string> StartSearchAsync(FlightSearchRequestDto request);
    Task<SearchStatusResponseDto> GetSearchStatusAsync(string searchId);
    Task ProcessProviderResponseAsync(ProviderSearchResponseDto response);
}