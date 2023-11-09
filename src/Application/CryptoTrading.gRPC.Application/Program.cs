using CryptoTrading.gRPC.Application.IOC;
using CryptoTrading.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var tmp = builder.Configuration.GetSection("CoinMarketCap");

CoinMarketCapEnvironment coinMarketCapEnvironment = new ();
builder.Configuration.GetRequiredSection("CoinMarketCap").Bind(coinMarketCapEnvironment);
builder.Services.AddSingleton(coinMarketCapEnvironment);

BrokerConfig brokerConfig = new ();
builder.Configuration.GetSection("BusConfiguration").Bind(brokerConfig);
builder.Services.AddSingleton(brokerConfig);

CacheRefreshConfiguration cacheRefreshConfiguration = new ();
builder.Configuration.GetSection("CacheConfiguration").Bind(cacheRefreshConfiguration);
builder.Services.AddSingleton(cacheRefreshConfiguration);

builder.Services
    .InjectCryptoQuoteProvider()
    .InjectCache()
    .InjectBus();

var app = builder.Build();

app.AddHftGrpcServiceMapping();

app.Run();