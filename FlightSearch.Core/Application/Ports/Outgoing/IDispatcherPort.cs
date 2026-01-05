using FlightSearch.Core.Application.DataSets;

namespace FlightSearch.Core.Application.Ports.Outgoing;

/// <summary>
/// Dispatcher port for publishing search requests (SNS-like behavior)
/// This port is used to broadcast search requests to multiple responders
/// </summary>
public interface IDispatcherPort
{
    Task DispatchSearchRequestAsync<T>(T message) where T : class;
}