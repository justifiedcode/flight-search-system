using FlightSearch.Core.Application.DataSets;

namespace FlightSearch.Core.Application.Ports.Outgoing;

/// <summary>
/// Flight provider port for calling external flight provider APIs
/// Each provider adapter implements this interface
/// </summary>
public interface IFlightProviderPort
{
    Task<FlightProviderResponseDto> SearchFlightsAsync(FlightProviderRequestDto request);
    string ProviderId { get; }
}