using System.Net;
using CryptoTrading.gRPC.Application.IOC;
using Grpc.Net.Client;
using Hft.HftApi.ApiContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CryptoTrading.Application.Tests;

public class MonitoringTests
{
    private WebApplication _webApplication = null!;

    [SetUp]
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddGrpc();
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
    public async Task MonitoringTestShouldContainHostAndOs()
    {
        using var channel = GrpcChannel.ForAddress(new Uri("http://localhost:5000"),
            new GrpcChannelOptions { UnsafeUseInsecureChannelCallCredentials = true, HttpClient = new HttpClient()});

        var monitoringClient = new Monitoring.MonitoringClient(channel);
        var aliveResponse = await monitoringClient.IsAliveAsync(new IsAliveRequest());

        aliveResponse.ShouldNotBeNull();
        aliveResponse.Env.ShouldBe(Environment.OSVersion.Platform is PlatformID.Unix ? "UNIX" :"WINDOWS");
        aliveResponse.Hostname.ShouldBe(Dns.GetHostName());
    }
}