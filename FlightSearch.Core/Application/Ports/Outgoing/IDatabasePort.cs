using FlightSearch.Core.Domain;

namespace FlightSearch.Core.Application.Ports.Outgoing;

/// <summary>
/// Database port for persisting and retrieving flight search aggregates
/// </summary>
public interface IDatabasePort
{
    Task SaveSearchAsync(FlightSearchAggregate search);
    Task<FlightSearchAggregate?> GetSearchAsync(string searchId);
    Task UpdateSearchAsync(FlightSearchAggregate search);
}