using Newtonsoft.Json;

namespace CryptoTrading.Infrastructure;

public class CoinMarketBaseRequestListData<T>
{
    [JsonProperty("status")]
    public required CoinMarketCapStatusDto CoinMarketCapStatus { get; set; }
    
    [JsonProperty("data")]
    public required List<T> Data { get; set; }
}

public class CoinMarketBaseRequestDictionaryData<T>
{
    [JsonProperty("status")]
    public required CoinMarketCapStatusDto CoinMarketCapStatus { get; set; }
    
    [JsonProperty("data")]
    public required IDictionary<string,T> Data { get; set; }
}

public class CoinMarketCapStatusDto
{
    [JsonProperty("error_message")]
    public string? ErrorMessage { get; set; }
}