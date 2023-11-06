using CryptoTrading.Infrastructure;
using Shouldly;

namespace CryptoProviderIntegrationTests;

public class Tests
{
    private CoinMarketCapDataProvider _dataProvider = null!;

    [SetUp]
    public void Setup()
    {
        _dataProvider = new CoinMarketCapDataProvider(new CoinMarketCapEnvironment
        {
            BaseUrl = "https://pro-api.coinmarketcap.com",
            ApiKey = "your api key"
        });
    }

    [Test]
    public async Task ShouldRetrieveCoinsMap()
    {
        var response = await _dataProvider.GetCryptoCurrencies();

        response.ShouldNotBeEmpty();
        // due to non unique "symbol" firstly group by symbol then create the dictionary
        var cryptoDictionary = response.GroupBy(_ => _.Symbol).ToDictionary(e => e.Key);

        cryptoDictionary.ShouldContainKey("BTC");
        var btc = cryptoDictionary["BTC"].First();
        btc.Name.ShouldBe("Bitcoin");
        btc.CoinMarketCapId.ShouldBe(1);
        cryptoDictionary.ShouldContainKey("ETH");
        cryptoDictionary.ShouldContainKey("DOGE");
    }
}