namespace TickerSearch.Api.Models;

public sealed record QuoteResponse(
    string Ticker,
    decimal Price,
    string Currency,
    string Exchange,
    DateTime FetchedAtUtc
);
