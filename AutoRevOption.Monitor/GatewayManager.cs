// GatewayManager.cs — Check, launch, and monitor IB Gateway process

using System.Diagnostics;
using System.Net.Sockets;

namespace AutoRevOption.Monitor;

public class GatewayManager
{
    private readonly IBKRCredentials _credentials;
    private Process? _gatewayProcess;

    public GatewayManager(IBKRCredentials credentials)
    {
        _credentials = credentials;
    }

    /// <summary>
    /// Check if IB Gateway is running and accepting connections
    /// </summary>
    public bool IsGatewayRunning()
    {
        // Check if port is listening
        if (!IsPortOpen(_credentials.Host, _credentials.Port))
        {
            return false;
        }

        // Check if ibgateway process exists
        var processes = Process.GetProcessesByName("ibgateway");
        if (processes.Length == 0)
        {
            processes = Process.GetProcessesByName("java");
            // Check if any java process has ibgateway in command line
            return processes.Any(p =>
            {
                try
                {
                    return p.MainModule?.FileName?.Contains("ibgateway", StringComparison.OrdinalIgnoreCase) ?? false;
                }
                catch
                {
                    return false;
                }
            });
        }

        return true;
    }

    /// <summary>
    /// Check if a port is open and accepting connections
    /// </summary>
    private bool IsPortOpen(string host, int port)
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
    /// Launch IB Gateway if not running
    /// </summary>
    public async Task<bool> EnsureGatewayRunningAsync()
    {
        Console.WriteLine("[Gateway] Checking IB Gateway status...");

        if (IsGatewayRunning())
        {
            Console.WriteLine("[Gateway] ✅ IB Gateway is already running");
            return true;
        }

        Console.WriteLine("[Gateway] ⚠️  IB Gateway is not running");

        if (!_credentials.AutoLaunch)
        {
            Console.WriteLine("[Gateway] AutoLaunch is disabled. Please start IB Gateway manually.");
            Console.WriteLine($"[Gateway] Expected at: {_credentials.GatewayPath}");
            return false;
        }

        return await LaunchGatewayAsync();
    }

    /// <summary>
    /// Launch IB Gateway process
    /// </summary>
    private async Task<bool> LaunchGatewayAsync()
    {
        if (string.IsNullOrEmpty(_credentials.GatewayPath) || !File.Exists(_credentials.GatewayPath))
        {
            Console.WriteLine($"[Gateway] ❌ Gateway executable not found: {_credentials.GatewayPath}");
            Console.WriteLine("[Gateway] Please update GatewayPath in secrets.json");
            Console.WriteLine("[Gateway] Common paths:");
            Console.WriteLine("  Windows: C:\\Jts\\ibgateway\\latest\\ibgateway.exe");
            Console.WriteLine("  Mac:     /Applications/IB Gateway.app/Contents/MacOS/ibgateway");
            Console.WriteLine("  Linux:   ~/Jts/ibgateway/latest/ibgateway");
            return false;
        }

        try
        {
            Console.WriteLine($"[Gateway] 🚀 Launching IB Gateway from: {_credentials.GatewayPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = _credentials.GatewayPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            _gatewayProcess = Process.Start(startInfo);

            if (_gatewayProcess == null)
            {
                Console.WriteLine("[Gateway] ❌ Failed to start IB Gateway process");
                return false;
            }

            Console.WriteLine($"[Gateway] Process started (PID: {_gatewayProcess.Id})");
            Console.WriteLine("[Gateway] ⏳ Waiting for Gateway to initialize...");
            Console.WriteLine("[Gateway] NOTE: You must manually log in to IB Gateway!");

            // Wait for port to become available (with timeout)
            var timeout = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < timeout)
            {
                await Task.Delay(2000);

                if (IsPortOpen(_credentials.Host, _credentials.Port))
                {
                    Console.WriteLine($"[Gateway] ✅ Gateway is ready on port {_credentials.Port}");
                    return true;
                }

                Console.Write(".");
            }

            Console.WriteLine();
            Console.WriteLine("[Gateway] ⚠️  Timeout waiting for Gateway to start");
            Console.WriteLine("[Gateway] Please check that you've logged in to IB Gateway");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gateway] ❌ Error launching Gateway: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Monitor Gateway process and restart if needed
    /// </summary>
    public async Task MonitorGatewayAsync(CancellationToken ct)
    {
        Console.WriteLine("[Gateway] Starting 24x7 monitoring...");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!IsGatewayRunning())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Gateway] ⚠️  Gateway connection lost!");

                    if (_credentials.AutoReconnect)
                    {
                        Console.WriteLine($"[Gateway] Attempting to reconnect in {_credentials.ReconnectDelaySeconds}s...");
                        await Task.Delay(TimeSpan.FromSeconds(_credentials.ReconnectDelaySeconds), ct);

                        await EnsureGatewayRunningAsync();
                    }
                }

                // Check every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gateway] Monitor error: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }

        Console.WriteLine("[Gateway] Monitoring stopped");
    }

    /// <summary>
    /// Get status information
    /// </summary>
    public string GetStatus()
    {
        var running = IsGatewayRunning();
        var portOpen = IsPortOpen(_credentials.Host, _credentials.Port);

        if (running && portOpen)
            return $"✅ Running (port {_credentials.Port} open)";

        if (portOpen)
            return $"⚠️  Port {_credentials.Port} open but process not detected";

        return $"❌ Not running (port {_credentials.Port} closed)";
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public void Dispose()
    {
        if (_gatewayProcess != null && !_gatewayProcess.HasExited)
        {
            Console.WriteLine("[Gateway] Note: IB Gateway process is still running");
            Console.WriteLine("[Gateway] You may want to close it manually or let it continue");
        }
    }
}
