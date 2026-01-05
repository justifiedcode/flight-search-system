using FlightSearch.Core.Application.Ports.Outgoing;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace FlightSearch.Adapters.SQS;

/// <summary>
/// In-memory SQS adapter that implements IResponsePort
/// Uses Channel<T> for thread-safe, async messaging within a single process
/// Simulates SQS behavior with reliable message delivery to single consumer
/// </summary>
public class InMemorySQSAdapter : IResponsePort
{
    private readonly Channel<object> _responseChannel;
    private readonly ChannelWriter<object> _writer;
    private readonly ILogger<InMemorySQSAdapter> _logger;

    public InMemorySQSAdapter(ILogger<InMemorySQSAdapter> logger)
    {
        _logger = logger;

        // Create unbounded channel for maximum throughput
        var options = new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _responseChannel = Channel.CreateUnbounded<object>(options);
        _writer = _responseChannel.Writer;
    }

    public ChannelReader<object> Reader => _responseChannel.Reader;

    public async Task PublishResponseAsync<T>(T message) where T : class
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

            await _writer.WriteAsync(envelope);

            _logger.LogInformation("?? SQS: Published response {MessageType} with ID {MessageId}",
                typeof(T).Name, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error publishing response via SQS");
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