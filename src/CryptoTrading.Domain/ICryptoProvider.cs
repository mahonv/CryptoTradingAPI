namespace CryptoTrading.Domain;

public interface ICryptoProvider
{
    public Task<IList<CryptoCurrenyInfo>> GetCryptoCurrencies();
    public Task<IList<CryptoCurrencyPriceUpdate>> GetCryptoQuotes(IList<long> symbols);
}

public class CryptoCurrenyInfo
{
    public long CoinMarketCapId { get; set; }
    public required string Name { get; set; }
    public required string Symbol { get; set; }
}

public class CryptoCurrencyPriceUpdate
{
    public required string Price { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public required string PairName { get; set; }
}