using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Incoming;
using FlightSearch.Adapters.SNS;
using System.Threading.Channels;

namespace FlightSearch.Host.Services;

/// <summary>
/// Skyscanner responder background service that subscribes to SNS broadcasts
/// This service handles infrastructure concerns (message consumption) and calls core business logic
/// </summary>
public class SkyscannerResponderService : BackgroundService
{
    private readonly IInquiryPort _inquiryService;
    private readonly ChannelReader<object> _messageReader;
    private readonly ILogger<SkyscannerResponderService> _logger;

    public SkyscannerResponderService(
        IInquiryPort inquiryService,
        InMemorySNSAdapter snsAdapter,
   ILogger<SkyscannerResponderService> logger)
    {
        _inquiryService = inquiryService;
        _messageReader = snsAdapter.CreateSubscriber(); // Each service gets its own channel
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Skyscanner Responder Service starting - subscribed to SNS broadcasts");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("?? Skyscanner Responder Service running - listening for broadcast messages");
        _logger.LogInformation("?? Provider: Skyscanner - RapidAPI headers, market/locale specific");

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
                    _logger.LogError(ex, "? Skyscanner: Error processing broadcast message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("?? Skyscanner Responder Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Fatal error in Skyscanner responder service");
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

            _logger.LogDebug("?? Skyscanner: Received broadcast for SearchId: {SearchId}, Provider: {ProviderId}",
                request.SearchId, request.ProviderId);

            // Business rule: Only process Skyscanner or ALL_PROVIDERS requests
            if (request.ProviderId != "Skyscanner" && request.ProviderId != "ALL_PROVIDERS")
            {
                _logger.LogDebug("?? Skyscanner: Skipping message - not for Skyscanner provider");
                return; // Not for this provider
            }

            _logger.LogInformation("?? SCATTER (Skyscanner): Processing request for SearchId: {SearchId}",
                request.SearchId);

            try
            {
                // Create Skyscanner-specific request
                var skyscannerRequest = new ProviderSearchRequestDto(
                     request.SearchId, "Skyscanner", request.Origin, request.Destination,
                 request.DepartureDate, request.Passengers, request.CabinClass);

                // Call core business logic via incoming port
                await _inquiryService.ProcessProviderInquiryAsync(skyscannerRequest);

                _logger.LogInformation("? Skyscanner: Completed processing for SearchId: {SearchId}", request.SearchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Skyscanner: Error processing request for SearchId: {SearchId}", request.SearchId);
            }
        }
        else
        {
            _logger.LogDebug("?? Skyscanner: Received non-ProviderSearchRequestDto message, ignoring");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Skyscanner Responder Service stopping");
        await base.StopAsync(cancellationToken);
    }
}