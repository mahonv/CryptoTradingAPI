using CryptoTrading.gRPC.Application.IOC;
using CryptoTrading.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

var tmp = builder.Configuration.GetSection("CoinMarketCap");

CoinMarketCapEnvironment coinMarketCapEnvironment = new ();
builder.Configuration.GetRequiredSection("CoinMarketCap").Bind(coinMarketCapEnvironment);
builder.Services.AddSingleton(coinMarketCapEnvironment);

builder.Services.InjectCryptoQuoteProvider();

var app = builder.Build();

app.AddHftGrpcServiceMapping();

app.Run();