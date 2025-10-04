// GatewayManager.cs — Lightweight Gateway checker for Minimal console

using System.Net.Sockets;

namespace AutoRevOption;

public class GatewayChecker
{
    /// <summary>
    /// Quick check if IB Gateway is running on specified port
    /// </summary>
    public static bool IsGatewayRunning(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

            if (success)
            {
                client.EndConnect(result);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Display Gateway status message
    /// </summary>
    public static void ShowGatewayStatus(string host, int port)
    {
        var isRunning = IsGatewayRunning(host, port);

        if (isRunning)
        {
            Console.WriteLine($"[Gateway] ✅ IB Gateway is running on {host}:{port}");
        }
        else
        {
            Console.WriteLine($"[Gateway] ❌ IB Gateway is NOT running on {host}:{port}");
            Console.WriteLine("[Gateway] Please start IB Gateway and log in:");
            Console.WriteLine($"[Gateway]   1. Launch IB Gateway (Paper Trading for port 7497)");
            Console.WriteLine($"[Gateway]   2. Log in with IBKR credentials");
            Console.WriteLine($"[Gateway]   3. Configure → Settings → API → Settings");
            Console.WriteLine($"[Gateway]   4. Enable 'Enable ActiveX and Socket Clients'");
            Console.WriteLine($"[Gateway]   5. Verify Socket Port = {port}");
            Console.WriteLine();
        }
    }
}
