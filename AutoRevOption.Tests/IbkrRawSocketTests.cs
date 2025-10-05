// IbkrRawSocketTests.cs — Low-level socket test to diagnose Gateway communication

using System.Net.Sockets;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace AutoRevOption.Tests;

public class IbkrRawSocketTests
{
    private readonly ITestOutputHelper _output;

    public IbkrRawSocketTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestRawSocketConnection()
    {
        _output.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
        _output.WriteLine("║                   RAW SOCKET CONNECTION TEST                               ║");
        _output.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
        _output.WriteLine("");

        var host = "127.0.0.1";
        var port = 4001;
        var timeout = 5000; // 5 seconds

        _output.WriteLine($"🔌 Testing TCP connection to {host}:{port}");
        _output.WriteLine("");

        try
        {
            using var client = new TcpClient();
            _output.WriteLine("⏳ Attempting to connect...");

            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeout);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _output.WriteLine($"❌ Connection timed out after {timeout}ms");
                _output.WriteLine("");
                _output.WriteLine("💡 Diagnostic: Port is not listening or firewall is blocking");
                return;
            }

            if (!client.Connected)
            {
                _output.WriteLine("❌ Failed to connect");
                return;
            }

            _output.WriteLine("✅ TCP connection established");
            _output.WriteLine("");

            // Get the network stream
            var stream = client.GetStream();
            stream.ReadTimeout = 2000;
            stream.WriteTimeout = 2000;

            _output.WriteLine("📡 Sending TWS API handshake...");
            _output.WriteLine("   Format: API=<version>\\0");

            // TWS API handshake: Send "API=<version>\0"
            // The TWS API client version we're using
            var handshake = Encoding.UTF8.GetBytes("API=9.72\0");
            await stream.WriteAsync(handshake);
            await stream.FlushAsync();

            _output.WriteLine($"   Sent: API=9.72 ({handshake.Length} bytes)");
            _output.WriteLine("");

            _output.WriteLine("⏳ Waiting for Gateway response (2 second timeout)...");

            // Try to read response
            var buffer = new byte[4096];
            var bytesRead = 0;

            try
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    _output.WriteLine($"✅ Received {bytesRead} bytes from Gateway");
                    _output.WriteLine("");
                    _output.WriteLine("📥 Response (hex):");
                    _output.WriteLine($"   {BitConverter.ToString(buffer, 0, Math.Min(bytesRead, 100))}");
                    _output.WriteLine("");
                    _output.WriteLine("📥 Response (ASCII):");
                    var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    _output.WriteLine($"   {response.Replace("\0", "[NULL]")}");
                    _output.WriteLine("");
                    _output.WriteLine("✅ Gateway is responding on TWS API protocol");
                }
                else
                {
                    _output.WriteLine("⚠️  Gateway closed connection immediately (0 bytes received)");
                    _output.WriteLine("");
                    _output.WriteLine("💡 Possible causes:");
                    _output.WriteLine("   - Gateway rejected the API version");
                    _output.WriteLine("   - Wrong port (try 7496 for TWS, 4001 for live Gateway, 4002 for paper)");
                    _output.WriteLine("   - Master API client ID is filtering connections");
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.TimedOut)
            {
                _output.WriteLine("❌ Timeout - Gateway did not send any response");
                _output.WriteLine("");
                _output.WriteLine("💡 This is the CORE PROBLEM:");
                _output.WriteLine("   - TCP connection succeeds (socket connects)");
                _output.WriteLine("   - But Gateway sends ZERO bytes back");
                _output.WriteLine("   - This suggests Gateway API is not processing our connection");
                _output.WriteLine("");
                _output.WriteLine("🔍 Next steps:");
                _output.WriteLine("   1. Check IB Gateway logs:");
                _output.WriteLine("      C:\\Users\\{YourUsername}\\Jts\\api.*.log");
                _output.WriteLine("      C:\\Users\\{YourUsername}\\Jts\\ibgateway.*.log");
                _output.WriteLine("");
                _output.WriteLine("   2. Try changing Master API client ID:");
                _output.WriteLine("      - Current: 10");
                _output.WriteLine("      - Try: blank/empty (allow all clients)");
                _output.WriteLine("      - Try: 0 (allow all clients)");
                _output.WriteLine("");
                _output.WriteLine("   3. Verify Gateway API settings:");
                _output.WriteLine("      - Socket port: 4001");
                _output.WriteLine("      - Read-Only API: UNCHECKED");
                _output.WriteLine("");
                _output.WriteLine("   4. Restart Gateway:");
                _output.WriteLine("      - Completely close Gateway");
                _output.WriteLine("      - Wait 10 seconds");
                _output.WriteLine("      - Restart and log in");
                _output.WriteLine("      - Verify API settings");
                _output.WriteLine("      - Try connection again");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Exception: {ex.GetType().Name}");
            _output.WriteLine($"   Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                _output.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }
    }
}
