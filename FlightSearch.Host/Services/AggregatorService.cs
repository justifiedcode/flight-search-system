using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Incoming;
using FlightSearch.Adapters.SQS;

namespace FlightSearch.Host.Services;

/// <summary>
/// Aggregator background service that directly consumes SQS response messages
/// This service handles infrastructure concerns (message consumption) and calls core business logic
/// </summary>
public class AggregatorService : BackgroundService
{
    private readonly IFlightSearchPort _flightSearchService;
    private readonly InMemorySQSAdapter _sqsAdapter;
    private readonly ILogger<AggregatorService> _logger;

    public AggregatorService(
        IFlightSearchPort flightSearchService,
   InMemorySQSAdapter sqsAdapter,
        ILogger<AggregatorService> logger)
    {
        _flightSearchService = flightSearchService;
        _sqsAdapter = sqsAdapter;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Aggregator Service starting - directly consuming SQS messages");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("?? Aggregator Service running - infrastructure handles message consumption");

        try
        {
            // Infrastructure concern: Direct message consumption from SQS adapter
            await foreach (var message in _sqsAdapter.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessResponseMessage(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Aggregator: Error processing SQS message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("?? Aggregator Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Fatal error in aggregator service");
        }
    }

    /// <summary>
    /// Infrastructure logic: Parse message and delegate to core business logic
    /// </summary>
    private async Task ProcessResponseMessage(object message)
    {
        if (message is MessageEnvelope<ProviderSearchResponseDto> envelope)
        {
            var response = envelope.Data;

            _logger.LogInformation("?? GATHER: Processing response from {ProviderId} for SearchId: {SearchId} - Success: {Success}, Flights: {FlightCount}",
             response.ProviderId, response.SearchId, response.Success, response.Flights?.Count ?? 0);

            try
            {
                // Call core business logic via incoming port
                await _flightSearchService.ProcessProviderResponseAsync(response);

                _logger.LogInformation("? Aggregator: Successfully processed response from {ProviderId} for SearchId: {SearchId}",
            response.ProviderId, response.SearchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Aggregator: Error processing response from {ProviderId} for SearchId: {SearchId}",
            response.ProviderId, response.SearchId);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Aggregator Service stopping");
        await base.StopAsync(cancellationToken);
    }
}