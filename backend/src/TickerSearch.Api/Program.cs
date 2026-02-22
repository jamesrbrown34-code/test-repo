using TickerSearch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddHttpClient<YahooFinanceClient>(client =>
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/quote/{ticker}", async (string ticker, YahooFinanceClient financeClient, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(ticker))
    {
        return Results.BadRequest(new { error = "Ticker is required." });
    }

    try
    {
        var quote = await financeClient.GetLatestQuoteAsync(ticker, cancellationToken);
        if (quote is null)
        {
            return Results.NotFound(new { error = $"Ticker '{ticker.Trim().ToUpperInvariant()}' was not found or has no active market price." });
        }

        return Results.Ok(quote);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("Failed to reach Yahoo Finance.", statusCode: StatusCodes.Status502BadGateway);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("Request to Yahoo Finance timed out.", statusCode: StatusCodes.Status504GatewayTimeout);
    }
});

app.Run();
