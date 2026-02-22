using System.Net;
using System.Text;
using TickerSearch.Api.Services;

namespace TickerSearch.Api.Tests;

public sealed class YahooFinanceClientTests
{
    [Fact]
    public async Task GetLatestQuoteAsync_ReturnsQuote_WhenYahooPayloadContainsPrice()
    {
        var json = """
                   {
                     "chart": {
                       "result": [
                         {
                           "meta": {
                             "regularMarketPrice": 189.42,
                             "currency": "USD",
                             "exchangeName": "NMS"
                           }
                         }
                       ],
                       "error": null
                     }
                   }
                   """;

        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://query1.finance.yahoo.com/")
        };

        var sut = new YahooFinanceClient(httpClient);

        var result = await sut.GetLatestQuoteAsync(" aapl ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("AAPL", result.Ticker);
        Assert.Equal(189.42m, result.Price);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("NMS", result.Exchange);
        Assert.Contains("v8/finance/chart/AAPL?interval=1d&range=1d", handler.RequestUris);
    }

    [Fact]
    public async Task GetLatestQuoteAsync_UsesDefaultCurrencyAndExchange_WhenValuesMissing()
    {
        var json = """
                   {
                     "chart": {
                       "result": [
                         {
                           "meta": {
                             "regularMarketPrice": 91.1
                           }
                         }
                       ],
                       "error": null
                     }
                   }
                   """;

        var sut = CreateSut(HttpStatusCode.OK, json);

        var result = await sut.GetLatestQuoteAsync("msft", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("Unknown", result.Exchange);
    }

    [Fact]
    public async Task GetLatestQuoteAsync_ReturnsNull_WhenYahooReturnsErrorPayload()
    {
        var json = """
                   {
                     "chart": {
                       "result": null,
                       "error": {
                         "code": "Not Found",
                         "description": "No data found"
                       }
                     }
                   }
                   """;

        var sut = CreateSut(HttpStatusCode.OK, json);

        var result = await sut.GetLatestQuoteAsync("invalid", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestQuoteAsync_ThrowsYahooRateLimitException_AfterThree429Responses()
    {
        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://query1.finance.yahoo.com/")
        };

        var sut = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<YahooRateLimitException>(() =>
            sut.GetLatestQuoteAsync("amd", CancellationToken.None));

        Assert.Contains("rate limit", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, handler.Calls);
    }

    private static YahooFinanceClient CreateSut(HttpStatusCode statusCode, string json)
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://query1.finance.yahoo.com/")
        };

        return new YahooFinanceClient(httpClient);
    }

    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            return Task.FromResult(CloneResponse(response));
        }
    }

    private sealed class SequenceHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured for request.");
            }

            return Task.FromResult(CloneResponse(_responses.Dequeue()));
        }
    }

    private static HttpResponseMessage CloneResponse(HttpResponseMessage source)
    {
        var clone = new HttpResponseMessage(source.StatusCode);
        if (source.Content is not null)
        {
            var body = source.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var mediaType = source.Content.Headers.ContentType?.MediaType ?? "application/json";
            clone.Content = new StringContent(body, Encoding.UTF8, mediaType);
        }

        return clone;
    }
}
