// GatewayProcessManager.cs — Manages IBKR Client Portal Gateway as a persistent background service

using System.Diagnostics;
using System.Net.Sockets;

namespace AutoRevOption.Client;

/// <summary>
/// Manages the IBKR Client Portal Gateway process
/// Ensures a single persistent gateway instance runs in the background
/// Multiple processes can share the same gateway session
/// </summary>
public class GatewayProcessManager : IDisposable
{
    private readonly string _gatewayPath;
    private readonly string _javaPath;
    private Process? _gatewayProcess;
    private bool _disposed;
    private static readonly object _lock = new object();
    private static GatewayProcessManager? _instance;

    // Singleton pattern to ensure only one gateway per machine
    public static GatewayProcessManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    var clientLibPath = Path.GetDirectoryName(typeof(GatewayProcessManager).Assembly.Location);
                    var gatewayPath = Path.GetFullPath(Path.Combine(clientLibPath ?? "", "..", "..", "..", ".ClientPortal"));
                    var javaPath = @"C:\Program Files\Java\jdk-25\bin\java.exe";

                    _instance = new GatewayProcessManager(gatewayPath, javaPath);
                }
                return _instance;
            }
        }
    }

    private GatewayProcessManager(string gatewayPath, string javaPath)
    {
        _gatewayPath = gatewayPath;
        _javaPath = javaPath;
    }

    /// <summary>
    /// Start the gateway if not already running
    /// Returns true if gateway is running (either already or newly started)
    /// </summary>
    public async Task<bool> EnsureGatewayRunningAsync()
    {
        // Check if gateway is already running
        if (IsGatewayRunning())
        {
            Console.WriteLine("[Gateway] Client Portal Gateway is already running");
            return true;
        }

        Console.WriteLine("[Gateway] Starting Client Portal Gateway...");

        if (!File.Exists(_javaPath))
        {
            Console.WriteLine($"[Gateway] ERROR: Java not found at {_javaPath}");
            return false;
        }

        if (!Directory.Exists(_gatewayPath))
        {
            Console.WriteLine($"[Gateway] ERROR: Gateway not found at {_gatewayPath}");
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _javaPath,
                Arguments = "-server -Dvertx.disableDnsResolver=true -Djava.net.preferIPv4Stack=true " +
                           "-Dvertx.logger-delegate-factory-class-name=io.vertx.core.logging.SLF4JLogDelegateFactory " +
                           "-Dnologback.statusListenerClass=ch.qos.logback.core.status.OnConsoleStatusListener " +
                           "-Dnolog4j.debug=true -Dnolog4j2.debug=true " +
                           "-classpath \"root;dist\\ibgroup.web.core.iblink.router.clientportal.gw.jar;build\\lib\\runtime\\*\" " +
                           "ibgroup.web.core.clientportal.gw.GatewayStart",
                WorkingDirectory = _gatewayPath,
                UseShellExecute = false,
                CreateNoWindow = true,  // Run in background
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _gatewayProcess = Process.Start(startInfo);

            if (_gatewayProcess == null)
            {
                Console.WriteLine("[Gateway] ERROR: Failed to start gateway process");
                return false;
            }

            // Wait for gateway to start (check port 5000)
            Console.WriteLine("[Gateway] Waiting for gateway to start on port 5000...");

            for (int i = 0; i < 30; i++)  // Wait up to 30 seconds
            {
                await Task.Delay(1000);

                if (IsPortOpen("localhost", 5000))
                {
                    Console.WriteLine("[Gateway] ✅ Gateway started successfully on https://localhost:5000");
                    return true;
                }
            }

            Console.WriteLine("[Gateway] ERROR: Gateway did not start within 30 seconds");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gateway] ERROR starting gateway: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if gateway is running by checking port 5000
    /// </summary>
    public bool IsGatewayRunning()
    {
        return IsPortOpen("localhost", 5000);
    }

    /// <summary>
    /// Stop the gateway process if it was started by this manager
    /// Note: Only stops if we started it, not if it was already running
    /// </summary>
    public void StopGateway()
    {
        if (_gatewayProcess != null && !_gatewayProcess.HasExited)
        {
            Console.WriteLine("[Gateway] Stopping gateway process...");
            try
            {
                _gatewayProcess.Kill(entireProcessTree: true);
                _gatewayProcess.WaitForExit(5000);
                Console.WriteLine("[Gateway] Gateway stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gateway] Error stopping gateway: {ex.Message}");
            }
        }
    }

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

    public void Dispose()
    {
        if (_disposed) return;

        // Note: We DON'T stop the gateway on dispose
        // The gateway should keep running for other processes
        // Only stop explicitly if needed

        _disposed = true;
    }
}
