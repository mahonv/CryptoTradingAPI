using CryptoTrading.Domain;
using CryptoTrading.gRPC.Application.Services;
using CryptoTrading.Infrastructure;

namespace CryptoTrading.gRPC.Application.IOC;

public static class Ioc
{
    public static void AddHftGrpcServiceMapping(this WebApplication app)
    {
        app.MapGrpcService<MonitoringServices>();
        app.MapGrpcService<PublicServices>();
        app.MapGet("/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
    }

    public static IServiceCollection InjectCryptoQuoteProvider(this IServiceCollection collection)
        => collection.AddSingleton<ICryptoProvider, CoinMarketCapDataProvider>();

    public static IServiceCollection InjectCache(this IServiceCollection collection)
        => collection
            .AddSingleton<ICryptoCacheService, CryptoCacheService>()
            .AddHostedService(_ => (_.GetRequiredService<ICryptoCacheService>() as CryptoCacheService)!);

    public static IServiceCollection InjectBus(this IServiceCollection collection)
        => collection.AddSingleton<BrokerProducer>();
}