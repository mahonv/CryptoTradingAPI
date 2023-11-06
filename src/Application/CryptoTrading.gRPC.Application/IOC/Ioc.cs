using CryptoTrading.gRPC.Application.Services;
using Hft.HftApi.ApiContract;

namespace CryptoTrading.gRPC.Application.IOC;

public static class Ioc
{
    public static void AddHftGrpcServiceMapping(this WebApplication app)
    {
        app.MapGrpcService<MonitoringService>();
        app.MapGrpcService<PublicService.PublicServiceBase>();
        app.MapGet("/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

    }
}