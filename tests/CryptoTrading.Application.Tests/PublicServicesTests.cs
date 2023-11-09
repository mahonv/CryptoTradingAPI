using System.Globalization;
using CryptoTrading.Domain;
using CryptoTrading.gRPC.Application.IOC;
using CryptoTrading.Infrastructure;
using DotNet.Testcontainers.Builders;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Hft.HftApi.ApiContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Shouldly;
using Testcontainers.Redpanda;
using Error = Hft.HftApi.ApiContract.Error;
using ErrorCode = Hft.HftApi.ApiContract.ErrorCode;

namespace CryptoTrading.Application.Tests;

public class PublicServicesTests
{
    private WebApplication _webApplication = null!;
    private ICryptoProvider _cryptoProvider;
    private RedpandaContainer _redpandaContainer;
    private string _redpandaAddress;

    private List<CryptoCurrenyInfo> _fakeCryptoList = new()
    {
        new() { Name = "Bitcoin", Symbol = "BTC", CoinMarketCapId = 1 },
        new() { Name = "Etherium", Symbol = "ETH", CoinMarketCapId = 1027 },
        new() { Name = "Ambiguous1", Symbol = "AMB", CoinMarketCapId = 1028 },
        new() { Name = "Ambiguous2", Symbol = "AMB", CoinMarketCapId = 1029 }
    };

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _cryptoProvider = Substitute.For<ICryptoProvider>();
        _cryptoProvider.GetCryptoCurrencies().Returns(_fakeCryptoList);

        var builder = WebApplication.CreateBuilder();

        _redpandaContainer = new RedpandaBuilder()
            .WithImage("docker.redpanda.com/redpandadata/redpanda:v22.2.1")
            .WithAutoRemove(true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8081))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Successfully started Redpanda!"))
            .Build();
        await _redpandaContainer.StartAsync();

        _redpandaAddress = _redpandaContainer.GetBootstrapAddress();
        var brokerConfig = new BrokerConfig
        {
            BaseUrl = _redpandaAddress
        };
        builder.Configuration.Bind(brokerConfig);
        builder.Services.AddSingleton(brokerConfig);

        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_cryptoProvider);

        var cacheConfig = new CacheRefreshConfiguration { GlobalRefresh = 100 };
        builder.Configuration.Bind(cacheConfig);
        builder.Services.AddSingleton(cacheConfig);
        
        builder.Services
            .InjectCache()
            .InjectBus();

        _webApplication = builder.Build();

        _webApplication.AddHftGrpcServiceMapping();
        await _webApplication.StartAsync();
    }

    [SetUp]
    public void Setup()
    {
        _cryptoProvider.ClearSubstitute();
        _cryptoProvider.GetCryptoCurrencies().Returns(_fakeCryptoList);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _webApplication.StopAsync();
        await _webApplication.DisposeAsync();

        await _redpandaContainer.StopAsync();
        await _redpandaContainer.DisposeAsync();
    }

    [Test]
    public async Task RetrieveAssets()
    {
        using var channel = GetLocalGrpcChannel();

        var publicServicesClient = new PublicService.PublicServiceClient(channel);

        var response = await publicServicesClient.GetAssetsAsync(new Empty());

        response.ShouldBe(new AssetsResponse(new AssetsResponse
        {
            Payload =
            {
                _fakeCryptoList.Select(c => new Asset
                {
                    Symbol = c.Symbol,
                    Name = c.Name
                })
            },
            Error = new Error { Code = ErrorCode.Success }
        }));
    }

    [Test]
    public async Task NotFoundAsset()
    {
        using var channel = GetLocalGrpcChannel();

        var publicServicesClient = new PublicService.PublicServiceClient(channel);

        var response = await publicServicesClient.GetAssetAsync(new AssetRequest { AssetId = "JAT" });

        response.ShouldBe(new AssetResponse
        {
            Error = new Error { Code = ErrorCode.ItemNotFound, Message = "Missing currency symbol" }
        });
    }

    [Test]
    public async Task RetrieveNonAmbiguousAsset()
    {
        using var channel = GetLocalGrpcChannel();

        var publicServicesClient = new PublicService.PublicServiceClient(channel);

        var response = await publicServicesClient.GetAssetAsync(new AssetRequest { AssetId = "BTC" });

        response.ShouldBe(new AssetResponse
        {
            Payload = new Asset { Name = "Bitcoin", Symbol = "BTC" },
            Error = new Error { Code = ErrorCode.Success }
        });
    }

    [Test]
    public async Task RetrieveAmbiguousAsset()
    {
        using var channel = GetLocalGrpcChannel();

        var publicServicesClient = new PublicService.PublicServiceClient(channel);

        var response = await publicServicesClient.GetAssetAsync(new AssetRequest { AssetId = "AMB" });

        response.ShouldBe(new AssetResponse
        {
            Payload = new Asset { Name = "Ambiguous1", Symbol = "AMB" },
            Error = new Error
                { Code = ErrorCode.Success, Message = "Ambiguous response due to multiple assets with same symbol" }
        });
    }

    [Test]
    public async Task CanStreamQuotes()
    {
        _cryptoProvider.ClearSubstitute();
        _cryptoProvider.GetCryptoCurrencies().Returns(new List<CryptoCurrenyInfo>
        {
            new() { Name = "Bitcoin", Symbol = "BTC", CoinMarketCapId = 1 },
            new() { Name = "Etherium", Symbol = "ETH", CoinMarketCapId = 1027 },
        });

        _cryptoProvider.GetCryptoQuotes(Arg.Is<IList<long>>(i => !i.Any()))
            .Returns(new List<CryptoCurrencyPriceUpdate>());
        _cryptoProvider.GetCryptoQuotes(Arg.Is<IList<long>>(i => new List<long> { 1 }.SequenceEqual(i)))
            .Returns(new List<CryptoCurrencyPriceUpdate>
            {
                new()
                {
                    Price = 1247.56m.ToString(CultureInfo.InvariantCulture), Timestamp = DateTimeOffset.UtcNow,
                    PairName = "BTC-USD"
                }
            });

        using var channel = GetLocalGrpcChannel();

        var publicServicesClient = new PublicService.PublicServiceClient(channel);
        var cancellationToken = new CancellationTokenSource();

        cancellationToken.CancelAfter(TimeSpan.FromSeconds(10));
        
        var msgReceived = new List<PriceUpdate>();
        var consumerTask = Task.Run(async () =>
        {
            await Task.Delay(500, cancellationToken.Token);
            using var call = publicServicesClient.GetPriceUpdates(new PriceUpdatesRequest { AssetPairIds = { "BTC" } });

            try
            {
                await foreach (var msg in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken.Token))
                {
                    msgReceived.Add(msg);
                }
            }
            catch (RpcException e) when (e.StatusCode is StatusCode.Cancelled)
            {
            }
        }, cancellationToken.Token);

        await consumerTask.ShouldNotThrowAsync();

        msgReceived.ShouldNotBeEmpty();
        msgReceived.First().AssetPairSymbol.ShouldBe("BTC-USD");
        msgReceived.First().Price.ShouldBe("1247.56");
    }

    private static GrpcChannel GetLocalGrpcChannel()
        => GrpcChannel.ForAddress(new Uri("http://localhost:5000"),
            new GrpcChannelOptions { UnsafeUseInsecureChannelCallCredentials = true, HttpClient = new HttpClient() });
}