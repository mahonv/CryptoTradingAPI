using CryptoTrading.Domain;
using Google.Protobuf.Collections;
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

    public override async Task<AssetsResponse> GetAssets(Empty request, ServerCallContext context)
    {
        var currencies = await _provider.GetCryptoCurrencies();

        return new AssetsResponse(new AssetsResponse
        {
            Payload =
            {
                currencies.Select(c => new Asset
                {
                    Symbol = c.Symbol,
                    Name = c.Name,
                })
            },
            Error = new Error
            {
                Code = ErrorCode.Success
            }
        });
    }

    public override async Task<AssetResponse> GetAsset(AssetRequest request, ServerCallContext context)
    {
        var currencies = await _provider.GetCryptoCurrencies();
        var currenciesDictionary = currencies.GroupBy(_ => _.Symbol).ToDictionary(_ => _.Key);

        if (!currenciesDictionary.ContainsKey(request.AssetId))
        {
            return new AssetResponse
            {
                Error = new Error { Code = ErrorCode.ItemNotFound, Message = "Missing currency symbol" },
            };
        }

        var key = currenciesDictionary[request.AssetId];
        var asset = key.First();
        var mappedAsset = new Asset
        {
            Symbol = asset.Symbol,
            Name = asset.Name
        };

        return key.Count() switch
        {
            > 1 => new AssetResponse()
            {
                Payload = mappedAsset,
                Error = new Error
                {
                    Code = ErrorCode.Success,
                    Message = "Ambiguous response due to multiple assets with same symbol"
                }
            },
            _ => new AssetResponse
            {
                Payload = mappedAsset,
                Error = new Error
                {
                    Code = ErrorCode.Success
                }
            }
        };
    }

    public override Task GetPriceUpdates(PriceUpdatesRequest request, IServerStreamWriter<PriceUpdate> responseStream,
        ServerCallContext context)
    {
        return base.GetPriceUpdates(request, responseStream, context);
    }
}