using System.Net;
using Grpc.Core;
using Hft.HftApi.ApiContract;

namespace CryptoTrading.gRPC.Application.Services;

public class MonitoringServices : Monitoring.MonitoringBase
{
    public override Task<IsAliveResponse> IsAlive(IsAliveRequest request, ServerCallContext context)
    {
        return Task.FromResult(new IsAliveResponse
        {
            Env = GetOs(),
            Hostname = Dns.GetHostName()
        });
    }

    private static string GetOs()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => "UNIX",
            PlatformID.MacOSX => "MACOS",
            PlatformID.Xbox => "XBOX",
            PlatformID.WinCE or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32Windows
                or PlatformID.Win32NT => "WINDOWS",
            _ => "OTHER"
        };
    }
}