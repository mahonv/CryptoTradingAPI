namespace CryptoTrading.gRPC.Application.Services;

public class GreeterService // : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;

    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    
}