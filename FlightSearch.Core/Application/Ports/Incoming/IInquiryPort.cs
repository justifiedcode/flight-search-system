using FlightSearch.Core.Application.DataSets;

namespace FlightSearch.Core.Application.Ports.Incoming;

/// <summary>
/// Inquiry port for provider search requests
/// This port is called by driving adapters (responder services) to initiate provider searches
/// </summary>
public interface IInquiryPort
{
    Task ProcessProviderInquiryAsync(ProviderSearchRequestDto request);
}