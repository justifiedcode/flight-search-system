using FlightSearch.Core.Application.Ports.Outgoing;
using FlightSearch.Core.Domain;
using System.Collections.Concurrent;

namespace FlightSearch.Adapters.Database;

/// <summary>
/// Database adapter that implements the IDatabasePort interface
/// Plugs into the database port like a USB device
/// </summary>
public class InMemoryDatabaseAdapter : IDatabasePort
{
    private readonly ConcurrentDictionary<string, FlightSearchAggregate> _searches = new();

    public async Task SaveSearchAsync(FlightSearchAggregate search)
    {
        _searches[search.SearchId] = search;
        await Task.CompletedTask;
    }

    public async Task<FlightSearchAggregate?> GetSearchAsync(string searchId)
    {
        _searches.TryGetValue(searchId, out var search);
        return await Task.FromResult(search);
    }

    public async Task UpdateSearchAsync(FlightSearchAggregate search)
    {
        _searches[search.SearchId] = search;
        await Task.CompletedTask;
    }
}