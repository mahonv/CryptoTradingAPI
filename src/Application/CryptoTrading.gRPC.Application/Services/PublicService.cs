using System.Text.Json;
using Confluent.Kafka;
using CryptoTrading.Domain;
using CryptoTrading.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Hft.HftApi.ApiContract;
using Polly;
using Error = Hft.HftApi.ApiContract.Error;
using ErrorCode = Hft.HftApi.ApiContract.ErrorCode;
using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace CryptoTrading.gRPC.Application.Services;

public class PublicServices : PublicService.PublicServiceBase
{
    private readonly ICryptoProvider _provider;
    private readonly BrokerConfig _brokerConfig;
    private readonly ICryptoCacheService _cryptoCacheService;

    public PublicServices(ICryptoProvider provider, BrokerConfig brokerConfig, ICryptoCacheService cryptoCacheService)
    {
        _provider = provider;
        _brokerConfig = brokerConfig;
        _cryptoCacheService = cryptoCacheService;
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

    public override async Task GetPriceUpdates(PriceUpdatesRequest request,
        IServerStreamWriter<PriceUpdate> responseStream,
        ServerCallContext context)
    {
        _cryptoCacheService.RequestQuotes(request.AssetPairIds);
        
        var config = new ConsumerConfig
        {
            BootstrapServers = _brokerConfig.BaseUrl,
            GroupId = "consumer-1",
            EnableAutoOffsetStore = false,
            EnableAutoCommit = true,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnablePartitionEof = true,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
        };

        var policy = Policy
            .Handle<ConsumeException>()
            .WaitAndRetryForeverAsync(
                (_)=> TimeSpan.FromMilliseconds(200),
                (exception, _) => { });
        
        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
            .SetValueDeserializer(Deserializers.Utf8).Build();
        
        consumer.Subscribe(request.AssetPairIds.Select(asset => $"cryptocurrency.quote.USD.{asset}"));
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await  policy.ExecuteAsync(async () =>
            {
                var msg = consumer.Consume(context.CancellationToken);
               
                if (msg?.Message?.Value is null) return;
                var json = JsonSerializer.Deserialize<CryptoCurrencyPriceUpdate>(msg.Message.Value);
                await responseStream.WriteAsync(new PriceUpdate{Price = json.Price, Timestamp =Timestamp.FromDateTimeOffset(json.Timestamp), AssetPairSymbol = json.PairName});
            });
        }
    }
}