using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("frontend");
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/quote/{ticker}", async (string ticker, IHttpClientFactory clientFactory) =>
{
    if (string.IsNullOrWhiteSpace(ticker))
    {
        return Results.BadRequest(new { error = "Ticker is required." });
    }

    var symbol = ticker.Trim().ToUpperInvariant();
    var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=1d";

    var client = clientFactory.CreateClient();

    try
    {
        using var response = await client.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { error = $"Ticker '{symbol}' was not found." });
        }

        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(contentStream);

        var root = document.RootElement;
        var error = root.GetProperty("chart").GetProperty("error");
        if (error.ValueKind != JsonValueKind.Null)
        {
            return Results.NotFound(new { error = $"Ticker '{symbol}' was not found." });
        }

        var result = root.GetProperty("chart").GetProperty("result")[0];
        var metadata = result.GetProperty("meta");

        if (!metadata.TryGetProperty("regularMarketPrice", out var priceElement) || priceElement.ValueKind != JsonValueKind.Number)
        {
            return Results.NotFound(new { error = $"No market price available for '{symbol}'." });
        }

        var currency = metadata.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString() : "USD";
        var exchangeName = metadata.TryGetProperty("exchangeName", out var exchangeElement) ? exchangeElement.GetString() : "Unknown";

        return Results.Ok(new
        {
            ticker = symbol,
            price = priceElement.GetDecimal(),
            currency,
            exchange = exchangeName,
            fetchedAtUtc = DateTime.UtcNow
        });
    }
    catch (HttpRequestException)
    {
        return Results.Problem("Failed to reach market data provider.");
    }
    catch (JsonException)
    {
        return Results.Problem("Unexpected response from market data provider.");
    }
});

app.Run();
