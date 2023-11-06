using CryptoTrading.Domain;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Hft.HftApi.ApiContract;

namespace CryptoTrading.gRPC.Application.Services;

public class PublicServices : PublicService.PublicServiceBase
{
    private readonly ICryptoProvider _provider;

    public PublicServices(ICryptoProvider provider)
    {
        _provider = provider;
    }

    public override Task<AssetsResponse> GetAssets(Empty request, ServerCallContext context)
    {
        return base.GetAssets(request, context);
    }

    public override Task<AssetResponse> GetAsset(AssetRequest request, ServerCallContext context)
    {
        return base.GetAsset(request, context);
    }

    public override Task GetPriceUpdates(PriceUpdatesRequest request, IServerStreamWriter<PriceUpdate> responseStream, ServerCallContext context)
    {
        return base.GetPriceUpdates(request, responseStream, context);
    }
}