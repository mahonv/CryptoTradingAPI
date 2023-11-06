namespace CryptoTrading.Domain;

public interface ICryptoProvider
{
    public Task<IList<CryptoCurrenyInfo>> GetCryptoCurrencies();
}

public class CryptoCurrenyInfo
{
    public long CoinMarketCapId { get; set; }
    public required string Name { get; set; }
    public required string Symbol { get; set; }
    
}