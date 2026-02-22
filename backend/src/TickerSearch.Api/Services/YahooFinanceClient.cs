using System.Text.Json;
using TickerSearch.Api.Models;

namespace TickerSearch.Api.Services;

public sealed class YahooFinanceClient(HttpClient httpClient)
{
    public async Task<QuoteResponse?> GetLatestQuoteAsync(string ticker, CancellationToken cancellationToken)
    {
        var symbol = ticker.Trim().ToUpperInvariant();
        var requestUri = $"v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=1d";

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var chart = document.RootElement.GetProperty("chart");
        var error = chart.GetProperty("error");
        if (error.ValueKind != JsonValueKind.Null)
        {
            return null;
        }

        var result = chart.GetProperty("result")[0].GetProperty("meta");

        if (!result.TryGetProperty("regularMarketPrice", out var priceElement) ||
            priceElement.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        var currency = result.TryGetProperty("currency", out var currencyElement)
            ? currencyElement.GetString() ?? "USD"
            : "USD";

        var exchange = result.TryGetProperty("exchangeName", out var exchangeElement)
            ? exchangeElement.GetString() ?? "Unknown"
            : "Unknown";

        return new QuoteResponse(
            symbol,
            priceElement.GetDecimal(),
            currency,
            exchange,
            DateTime.UtcNow
        );
    }
}
