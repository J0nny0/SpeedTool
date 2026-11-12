using System.Net;
using System.Net.Sockets;
using SpeedTool.Injector;

namespace SpeedTool.Tests;

public sealed class PipeTcpTests
{
    [Fact]
    public void PipeTcpTransportRoundTrip()
    {
        var port = GetFreePort();
        Environment.SetEnvironmentVariable("SPEEDTOOL_TCP_PORT", port.ToString());
        try
        {
            using var pipe = new Pipe("function on_load()\nend");
            using var tcp = new TcpClient();
            tcp.Connect("127.0.0.1", port);

            string? received = null;
            pipe.OnMessage += (_, msg) => received = msg;

            using var reader = new StreamReader(tcp.GetStream());
            while(reader.ReadLine() != "script_load")
            {
            }

            using var writer = new StreamWriter(tcp.GetStream()) { AutoFlush = true };
            writer.WriteLine("timer 12345");

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while(received == null && DateTime.UtcNow < deadline)
            {
                pipe.Cycle();
                Thread.Sleep(10);
            }

            Assert.Equal("timer 12345", received);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SPEEDTOOL_TCP_PORT", null);
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
