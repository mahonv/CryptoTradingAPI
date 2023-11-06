using CryptoTrading.Domain;
using CryptoTrading.gRPC.Application.IOC;
using CryptoTrading.gRPC.Application.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Hft.HftApi.ApiContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace CryptoTrading.Application.Tests;

public class PublicServicesTests
{
    private WebApplication _webApplication = null!;
    private ICryptoProvider _cryptoProvider;

    private List<CryptoCurrenyInfo> _fakeCryptoList = new()
    {
        new() { Name = "Bitcoin", Symbol = "BTC" },
        new() { Name = "Etherium", Symbol = "ETH" },
        new() { Name = "Ambiguous1", Symbol = "AMB" },
        new() { Name = "Ambiguous2", Symbol = "AMB" }
    };

    [SetUp]
    public async Task Setup()
    {
        _cryptoProvider = Substitute.For<ICryptoProvider>();
        _cryptoProvider.GetCryptoCurrencies().Returns(_fakeCryptoList);

        var builder = WebApplication.CreateBuilder();


        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_cryptoProvider);

        _webApplication = builder.Build();

        _webApplication.AddHftGrpcServiceMapping();
        await _webApplication.StartAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _webApplication.StopAsync();
        await _webApplication.DisposeAsync();
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
            Error = new Error
                { Code = ErrorCode.ItemNotFound, Message = "Missing currency symbol" }
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

    private static GrpcChannel GetLocalGrpcChannel()
        => GrpcChannel.ForAddress(new Uri("http://localhost:5000"),
            new GrpcChannelOptions { UnsafeUseInsecureChannelCallCredentials = true, HttpClient = new HttpClient() });
}