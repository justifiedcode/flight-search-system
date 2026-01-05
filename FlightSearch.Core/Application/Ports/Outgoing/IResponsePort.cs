using FlightSearch.Core.Application.DataSets;

namespace FlightSearch.Core.Application.Ports.Outgoing;

/// <summary>
/// Response port for publishing provider responses (SQS-like behavior)
/// This port is used to send provider responses back to the aggregator
/// </summary>
public interface IResponsePort
{
    Task PublishResponseAsync<T>(T message) where T : class;
}