using Newtonsoft.Json;

namespace CryptoTrading.Infrastructure;

public class CryptoCurrencyQuoteDto
{
    [JsonProperty("symbol")] public string Symbol { get; set; } = null!;
    [JsonProperty("last_updated")] public DateTimeOffset LastUpdate { get; set; }
    [JsonProperty("quote")] public IDictionary<string, QuoteDto> Quote { get; set; } = null!;
}

public class QuoteDto
{
    [JsonProperty("Price")] public decimal Price { get; set; }
}