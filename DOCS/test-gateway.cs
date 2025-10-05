// Quick standalone Gateway connection test
// Run: dotnet run test-gateway.cs

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  IB Gateway Raw Socket Connection Test");
        Console.WriteLine("═══════════════════════════════════════\n");

        var host = "127.0.0.1";
        var port = 4001;

        Console.WriteLine($"Connecting to {host}:{port}...");

        try
        {
            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask || !client.Connected)
            {
                Console.WriteLine("❌ Connection failed or timed out\n");
                Console.WriteLine("Check:");
                Console.WriteLine("  - Is IB Gateway running?");
                Console.WriteLine("  - Is port 4001 correct?");
                Console.WriteLine("  - Run: netstat -an | findstr :4001");
                return;
            }

            Console.WriteLine("✅ TCP connected\n");

            var stream = client.GetStream();
            stream.ReadTimeout = 3000;
            stream.WriteTimeout = 3000;

            Console.WriteLine("Sending TWS API handshake: API=9.72");
            var handshake = Encoding.UTF8.GetBytes("API=9.72\0");
            await stream.WriteAsync(handshake);
            await stream.FlushAsync();
            Console.WriteLine($"Sent {handshake.Length} bytes\n");

            Console.WriteLine("Waiting for Gateway response (3s timeout)...");
            var buffer = new byte[4096];

            try
            {
                var bytesRead = await stream.ReadAsync(buffer);

                if (bytesRead > 0)
                {
                    Console.WriteLine($"✅ Received {bytesRead} bytes:\n");
                    Console.WriteLine("Hex: " + BitConverter.ToString(buffer, 0, Math.Min(bytesRead, 100)));
                    Console.WriteLine("ASCII: " + Encoding.ASCII.GetString(buffer, 0, bytesRead).Replace("\0", "[NULL]"));
                    Console.WriteLine("\n✅ SUCCESS - Gateway is responding!");
                }
                else
                {
                    Console.WriteLine("❌ Gateway closed connection (0 bytes)\n");
                    Console.WriteLine("Possible causes:");
                    Console.WriteLine("  - Wrong API version");
                    Console.WriteLine("  - Wrong port");
                    Console.WriteLine("  - Master API client ID filtering");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Read timeout/error: {ex.Message}\n");
                Console.WriteLine("💡 DIAGNOSIS:");
                Console.WriteLine("  - Socket connects successfully");
                Console.WriteLine("  - But Gateway sends NO response");
                Console.WriteLine("  - This means Gateway API is not processing connections\n");
                Console.WriteLine("Next steps:");
                Console.WriteLine("  1. Check IB Gateway logs:");
                Console.WriteLine("     C:\\Users\\{YourUsername}\\Jts\\api.*.log");
                Console.WriteLine("     C:\\Users\\{YourUsername}\\Jts\\ibgateway.*.log\n");
                Console.WriteLine("  2. Try Master API client ID:");
                Console.WriteLine("     - Set to blank/empty (not 10)");
                Console.WriteLine("     - Or set to 0\n");
                Console.WriteLine("  3. Verify API settings:");
                Console.WriteLine("     - Socket port: 4001");
                Console.WriteLine("     - Read-Only API: UNCHECKED\n");
                Console.WriteLine("  4. Restart Gateway completely");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
        }
    }
}
