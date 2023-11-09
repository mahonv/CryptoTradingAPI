namespace CryptoTrading.Domain;

public interface ICryptoCacheService
{
    public void RequestQuotes(IList<string> quoteSymbols);
}