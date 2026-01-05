using FlightSearch.Core.Application.Ports.Outgoing;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace FlightSearch.Adapters.SNS;

/// <summary>
/// In-memory SNS adapter that implements IDispatcherPort
/// Uses Channel<T> for thread-safe, async messaging within a single process
/// Simulates SNS behavior with immediate delivery to multiple subscribers
/// </summary>
public class InMemorySNSAdapter : IDispatcherPort
{
    private readonly List<Channel<object>> _subscriberChannels;
    private readonly ILogger<InMemorySNSAdapter> _logger;
    private readonly object _lock = new();

    public InMemorySNSAdapter(ILogger<InMemorySNSAdapter> logger)
    {
        _logger = logger;
        _subscriberChannels = new List<Channel<object>>();
    }

    /// <summary>
    /// Creates a new subscriber channel for message consumption
    /// </summary>
    public ChannelReader<object> CreateSubscriber()
    {
        lock (_lock)
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            var channel = Channel.CreateUnbounded<object>(options);
            _subscriberChannels.Add(channel);

            _logger.LogInformation("?? SNS: Created new subscriber channel. Total subscribers: {Count}", _subscriberChannels.Count);

            return channel.Reader;
        }
    }

    public async Task DispatchSearchRequestAsync<T>(T message) where T : class
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();
            var envelope = new MessageEnvelope<T>
            {
                Id = messageId,
                Type = typeof(T).Name,
                Timestamp = DateTime.UtcNow,
                Data = message
            };

            // Broadcast to all subscribers
            var activeChannels = new List<Channel<object>>();
            lock (_lock)
            {
                activeChannels.AddRange(_subscriberChannels);
            }

            _logger.LogInformation("?? SNS: Broadcasting message {MessageType} with ID {MessageId} to {SubscriberCount} subscribers",
                      typeof(T).Name, messageId, activeChannels.Count);

            // Send to all active subscriber channels
            foreach (var channel in activeChannels)
            {
                try
                {
                    await channel.Writer.WriteAsync(envelope);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write to subscriber channel, removing it");
                    lock (_lock)
                    {
                        _subscriberChannels.Remove(channel);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error dispatching message via SNS");
            throw;
        }
    }
}

/// <summary>
/// Message envelope for type safety and metadata
/// </summary>
public class MessageEnvelope<T>
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public T Data { get; set; } = default!;
}