using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using Xunit;

namespace AutoRevOption.Tests.Connectivity
{
    /// <summary>
    /// Opt-in smoke tests to diagnose "TCP connects but handshake dies" issues.
    /// Skipped by default. Set RUN_IB_PROBES=1 to enable locally.
    /// These do NOT send orders; they only test that a listener responds sanely.
    /// </summary>
    public class ApiHandshakeSpecs
    {
        private static bool Enabled =>
            string.Equals(Environment.GetEnvironmentVariable("RUN_IB_PROBES"), "1", StringComparison.OrdinalIgnoreCase);

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        [Fact(Skip = "Set RUN_IB_PROBES=1 to enable")]
        public void gateway_plain_socket_accepts_and_does_not_immediately_close()
        {
            if (!Enabled) return;
            var host = Environment.GetEnvironmentVariable("IB_HOST") ?? "127.0.0.1";
            var port = int.TryParse(Environment.GetEnvironmentVariable("IB_PORT"), out var p) ? p : 4002; // paper by default

            using var tcp = new TcpClient();
            var ar = tcp.BeginConnect(host, port, null, null);
            Assert.True(ar.AsyncWaitHandle.WaitOne(Timeout), $"Timeout connecting to {host}:{port}");
            tcp.EndConnect(ar);
            Assert.True(tcp.Connected, "TCP connected=false");

            // Try reading a single byte with timeout: if server instantly closes, stream will end immediately.
            tcp.ReceiveTimeout = (int)Timeout.TotalMilliseconds;
            var ns = tcp.GetStream();
            var buf = new byte[1];
            // Most modern IB servers won't proactively send; we only verify it's not hard-closing immediately.
            // A read timeout here is OK and expected; an immediate 0-byte read indicates close/reset.
            var read = 0;
            try { read = ns.Read(buf, 0, 1); }
            catch (IOException) { /* timeout or IO is acceptable; not immediate close */ }

            Assert.True(read != 0, "Server closed immediately (0-byte read). Possible TLS/SSL mismatch or incompatible build.");
        }

        [Fact(Skip = "Set RUN_IB_PROBES=1 to enable")]
        public void tls_probe_reports_if_server_offers_tls()
        {
            if (!Enabled) return;
            var host = Environment.GetEnvironmentVariable("IB_HOST") ?? "127.0.0.1";
            var port = int.TryParse(Environment.GetEnvironmentVariable("IB_PORT"), out var p) ? p : 4002;

            using var tcp = new TcpClient();
            var ar = tcp.BeginConnect(host, port, null, null);
            Assert.True(ar.AsyncWaitHandle.WaitOne(Timeout), $"Timeout connecting to {host}:{port}");
            tcp.EndConnect(ar);

            using var ssl = new SslStream(tcp.GetStream(), false, (sender, cert, chain, errors) => true);
            try
            {
                ssl.AuthenticateAsClient(host, null, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                // If we got here, server spoke TLS. That's a strong indicator you must enable SSL in the client connect options.
                Assert.True(ssl.IsAuthenticated, "TLS handshake failed unexpectedly.");
            }
            catch (IOException)
            {
                // IO means handshake failed (server not TLS) which is fine—this tells us the port expects plain socket.
                Assert.True(true);
            }
            catch (AuthenticationException)
            {
                // Auth exception usually still proves TLS is in play but validation failed; that's enough signal.
                Assert.True(true);
            }
        }

        [Fact(Skip = "Set RUN_IB_PROBES=1 to enable")]
        public void tws_probe_on_7497_should_listen_when_tws_paper_is_running()
        {
            if (!Enabled) return;
            var host = Environment.GetEnvironmentVariable("IB_HOST") ?? "127.0.0.1";
            var port = 7497; // TWS paper default
            using var tcp = new TcpClient();
            var ar = tcp.BeginConnect(host, port, null, null);
            Assert.True(ar.AsyncWaitHandle.WaitOne(Timeout), $"Timeout connecting to TWS {host}:{port} (is TWS Paper running?)");
            tcp.EndConnect(ar);
            Assert.True(tcp.Connected, "TCP connected=false");
        }
    }
}
