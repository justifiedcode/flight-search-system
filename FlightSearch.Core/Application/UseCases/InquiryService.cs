using FlightSearch.Core.Application.DataSets;
using FlightSearch.Core.Application.Ports.Incoming;
using FlightSearch.Core.Application.Ports.Outgoing;
using Microsoft.Extensions.Logging;

namespace FlightSearch.Core.Application.UseCases;

/// <summary>
/// Inquiry service that handles provider search requests from responder services
/// This service coordinates between responders and flight providers
/// </summary>
public class InquiryService : IInquiryPort
{
    private readonly IEnumerable<IFlightProviderPort> _flightProviders;
    private readonly IResponsePort _responsePort;
    private readonly ILogger<InquiryService> _logger;

    public InquiryService(
        IEnumerable<IFlightProviderPort> flightProviders,
        IResponsePort responsePort,
     ILogger<InquiryService> logger)
    {
        _flightProviders = flightProviders;
        _responsePort = responsePort;
        _logger = logger;
    }

    public async Task ProcessProviderInquiryAsync(ProviderSearchRequestDto request)
    {
        _logger.LogInformation("?? Processing provider inquiry for SearchId: {SearchId}, Provider: {ProviderId}",
            request.SearchId, request.ProviderId);

        try
        {
            // Find the specific provider
            var provider = _flightProviders.FirstOrDefault(p => p.ProviderId == request.ProviderId);
            if (provider == null)
            {
                _logger.LogWarning("?? Provider {ProviderId} not found for SearchId: {SearchId}",
           request.ProviderId, request.SearchId);

                // Send error response
                var errorResponse = new ProviderSearchResponseDto(
         request.SearchId, request.ProviderId, new List<FlightOfferDto>(),
        false, $"Provider {request.ProviderId} not found");
                await _responsePort.PublishResponseAsync(errorResponse);
                return;
            }

            // Convert to provider request format
            var providerRequest = new FlightProviderRequestDto
            {
                Origin = request.Origin,
                Destination = request.Destination,
                DepartureDate = request.DepartureDate,
                Passengers = request.Passengers,
                CabinClass = request.CabinClass
            };

            // Call the provider
            var providerResponse = await provider.SearchFlightsAsync(providerRequest);

            // Convert to response format and publish
            var response = new ProviderSearchResponseDto(
               request.SearchId,
               provider.ProviderId,
            providerResponse.Flights,
               providerResponse.Success,
          providerResponse.Error
        );

            await _responsePort.PublishResponseAsync(response);

            _logger.LogInformation("? Published response from {ProviderId} for SearchId: {SearchId} - Success: {Success}, Flights: {FlightCount}",
              provider.ProviderId, request.SearchId, providerResponse.Success, providerResponse.Flights.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error processing provider inquiry for {ProviderId}, SearchId: {SearchId}",
 request.ProviderId, request.SearchId);

            // Send error response
            var errorResponse = new ProviderSearchResponseDto(
                   request.SearchId, request.ProviderId, new List<FlightOfferDto>(),
               false, ex.Message);
            await _responsePort.PublishResponseAsync(errorResponse);
        }
    }
}