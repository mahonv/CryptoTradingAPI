using System.Globalization;
using CryptoTrading.Domain;
using Flurl.Http;

namespace CryptoTrading.Infrastructure;

public class CoinMarketCapEnvironment
{
    public string ApiKey { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
}

public class CoinMarketCapDataProvider : ICryptoProvider
{
    private readonly IFlurlClient _flurlClient;

    public CoinMarketCapDataProvider(CoinMarketCapEnvironment configuration)
    {
        _flurlClient = new FlurlClient();
        _flurlClient.BaseUrl = configuration.BaseUrl;
        _flurlClient.WithHeader("X-CMC_PRO_API_KEY", configuration.ApiKey);
        _flurlClient.WithTimeout(TimeSpan.FromSeconds(5));
        _flurlClient.Settings.AllowedHttpStatusRange = "200-299";
    }

    public async Task<IList<CryptoCurrenyInfo>> GetCryptoCurrencies()
    {
        var response = await _flurlClient.Request()
            .AppendPathSegment("v1/cryptocurrency/map")
            .GetJsonAsync<CoinMarketBaseRequestListData<CryptoCurrencyMapDto>>();

        return response.Data.Select(c => new CryptoCurrenyInfo
            { CoinMarketCapId = c.Id, Name = c.Name, Symbol = c.Symbol }).ToList();
    }

    public async Task<IList<CryptoCurrencyPriceUpdate>> GetCryptoQuotes(IList<long> symbols)
    {
        if (!symbols.Any()) return new List<CryptoCurrencyPriceUpdate>();
        
        var response = await _flurlClient.Request().AppendPathSegment("v2/cryptocurrency/quotes/latest")
            .SetQueryParam("id", string.Join(",", symbols.Select(e => e.ToString())))
            .GetJsonAsync<CoinMarketBaseRequestDictionaryData<CryptoCurrencyQuoteDto>>();

        return response.Data.Select((k) => new CryptoCurrencyPriceUpdate
        {
            Price = k.Value.Quote["USD"].Price.ToString(CultureInfo.InvariantCulture),
            Timestamp  = k.Value.LastUpdate,
            PairName = $"{k.Value.Symbol}-USD"
        }).ToList();
    }
}