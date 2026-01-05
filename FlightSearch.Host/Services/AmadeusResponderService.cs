using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Incoming;
using FlightSearch.Adapters.SNS;
using System.Threading.Channels;

namespace FlightSearch.Host.Services;

/// <summary>
/// Amadeus responder background service that subscribes to SNS broadcasts
/// This service handles infrastructure concerns (message consumption) and calls core business logic
/// </summary>
public class AmadeusResponderService : BackgroundService
{
    private readonly IInquiryPort _inquiryService;
    private readonly ChannelReader<object> _messageReader;
    private readonly ILogger<AmadeusResponderService> _logger;

    public AmadeusResponderService(
        IInquiryPort inquiryService,
        InMemorySNSAdapter snsAdapter,
        ILogger<AmadeusResponderService> logger)
    {
        _inquiryService = inquiryService;
        _messageReader = snsAdapter.CreateSubscriber(); // Each service gets its own channel
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("??? Amadeus Responder Service starting - subscribed to SNS broadcasts");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("?? Amadeus Responder Service running - listening for broadcast messages");
        _logger.LogInformation("?? Provider: Amadeus - OAuth 2.0 authentication, complex JSON");

        try
        {
            // Infrastructure concern: Listen to our dedicated subscriber channel
            await foreach (var message in _messageReader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessDispatchMessage(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Amadeus: Error processing broadcast message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("?? Amadeus Responder Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Fatal error in Amadeus responder service");
        }
    }

    /// <summary>
    /// Infrastructure logic: Parse message and delegate to core business logic
    /// </summary>
    private async Task ProcessDispatchMessage(object message)
    {
        if (message is MessageEnvelope<ProviderSearchRequestDto> envelope)
        {
            var request = envelope.Data;

            _logger.LogDebug("?? Amadeus: Received broadcast for SearchId: {SearchId}, Provider: {ProviderId}",
                request.SearchId, request.ProviderId);

            // Business rule: Only process Amadeus or ALL_PROVIDERS requests
            if (request.ProviderId != "Amadeus" && request.ProviderId != "ALL_PROVIDERS")
            {
                _logger.LogDebug("?? Amadeus: Skipping message - not for Amadeus provider");
                return; // Not for this provider
            }

            _logger.LogInformation("?? SCATTER (Amadeus): Processing request for SearchId: {SearchId}",
                request.SearchId);

            try
            {
                // Create Amadeus-specific request
                var amadeusRequest = new ProviderSearchRequestDto(
                    request.SearchId, "Amadeus", request.Origin, request.Destination,
                    request.DepartureDate, request.Passengers, request.CabinClass);

                // Call core business logic via incoming port
                await _inquiryService.ProcessProviderInquiryAsync(amadeusRequest);

                _logger.LogInformation("? Amadeus: Completed processing for SearchId: {SearchId}", request.SearchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Amadeus: Error processing request for SearchId: {SearchId}", request.SearchId);
            }
        }
        else
        {
            _logger.LogDebug("?? Amadeus: Received non-ProviderSearchRequestDto message, ignoring");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Amadeus Responder Service stopping");
        await base.StopAsync(cancellationToken);
    }
}