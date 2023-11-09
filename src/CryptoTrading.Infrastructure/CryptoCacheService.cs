using CryptoTrading.Domain;
using Microsoft.Extensions.Hosting;

namespace CryptoTrading.Infrastructure;

public class CacheRefreshConfiguration
{
    public ulong GlobalRefresh { get; set; } = (ulong)TimeSpan.FromMinutes(2).TotalMilliseconds;
}

public class CryptoCacheService : BackgroundService, ICryptoCacheService
{
    private readonly ICryptoProvider _cryptoProvider;
    private readonly BrokerProducer _brokerProducer;

    private readonly CacheRefreshConfiguration _refreshConfiguration;
    private IDictionary<string, IGrouping<string, CryptoCurrenyInfo>> _currenciesDictionary;
    private HashSet<string> _requestedCurrencyQuotes;


    public CryptoCacheService(ICryptoProvider cryptoProvider, CacheRefreshConfiguration refreshConfiguration,
        BrokerProducer brokerProducer)
    {
        _cryptoProvider = cryptoProvider;
        _refreshConfiguration = refreshConfiguration;
        _brokerProducer = brokerProducer;
        _requestedCurrencyQuotes = new HashSet<string>();
        _currenciesDictionary = new Dictionary<string, IGrouping<string, CryptoCurrenyInfo>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var ids = (from rq in _requestedCurrencyQuotes
                    join dc in _currenciesDictionary
                        on rq equals dc.Key
                    select dc.Value.Select(e => e.CoinMarketCapId))
                .SelectMany(id => id).ToList();

            var currenciesTask = _cryptoProvider.GetCryptoCurrencies();
            var quoteTask = _cryptoProvider.GetCryptoQuotes(ids);

            await Task.WhenAll(currenciesTask, quoteTask);

            if (quoteTask.Result.Any())
            {
                foreach (var update in quoteTask.Result)
                {
                    var pairSplit = update.PairName.Split("-");
                    await _brokerProducer.Publish($"cryptocurrency.quote.{pairSplit[1]}.{pairSplit[0]}", stoppingToken,
                        update);
                }
            }

            _currenciesDictionary = currenciesTask.Result.Select(_ => new CryptoCurrenyInfo
                    { Symbol = _.Symbol, Name = _.Name, CoinMarketCapId = _.CoinMarketCapId })
                .GroupBy(_ => _.Symbol)
                .ToDictionary(_ => _.Key);

            await Task.Delay(TimeSpan.FromMilliseconds(_refreshConfiguration.GlobalRefresh), stoppingToken);
        }
    }

    public void RequestQuotes(IList<string> quoteSymbols)
        => _requestedCurrencyQuotes.UnionWith(quoteSymbols.ToHashSet());
}