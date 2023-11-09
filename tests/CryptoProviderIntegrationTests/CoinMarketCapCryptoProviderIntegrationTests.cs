using CryptoTrading.Infrastructure;
using Shouldly;
using static System.Globalization.CultureInfo;

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
            ApiKey = ""
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
    
    [Test]
    public async Task ShouldRetrieveCoinsQuote()
    {
        var response = await _dataProvider.GetCryptoQuotes(new List<long>{1});
        
        response.ShouldNotBeEmpty();
        var priceUpdate = response.First();
        priceUpdate.Price.ShouldNotBeEmpty();
        priceUpdate.PairName.ShouldBe("BTC-USD");
        priceUpdate.Timestamp.ShouldNotBe(DateTimeOffset.MinValue);
        decimal.Parse(priceUpdate.Price, InvariantCulture).ShouldBePositive();
    }
}