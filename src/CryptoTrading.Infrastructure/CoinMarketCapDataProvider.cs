using CryptoTrading.Domain;
using Flurl.Http;
using Newtonsoft.Json.Linq;

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
        var response = await _flurlClient.Request().AppendPathSegment("v1/cryptocurrency/map").GetJsonAsync<CoinMarketBaseRequest<CryptoCurrencyMapDto>>();

        return response.Data.Select(c => new CryptoCurrenyInfo{CoinMarketCapId = c.Id,Name = c.Name, Symbol = c.Symbol}).ToList();
    }
}