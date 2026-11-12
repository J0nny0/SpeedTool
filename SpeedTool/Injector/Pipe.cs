using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using SpeedTool.Platform.Debugging;

namespace SpeedTool.Injector;

public sealed class Pipe : IDisposable
{
    public Pipe(string script)
    {
        var portEnv = Environment.GetEnvironmentVariable("SPEEDTOOL_TCP_PORT");
        if(portEnv != null)
        {
            StartTcpListener(script, int.Parse(portEnv));
            return;
        }

        client = new NamedPipeClientStream(".", "SpeedToolPipe", PipeDirection.InOut, PipeOptions.Asynchronous);
        connectCancellation = new();
        connectionTask = client.ConnectAsync(connectCancellation.Token);
        connectionTask.ContinueWith(x =>
        {
            DebugLog.SharedInstance.Write("Pipe connected");
            pipeReader = new PipeReader(client);
            pipeWriter = new StreamWriter(client);
            SendScript(script);
        });
    }

    private void StartTcpListener(string script, int port)
    {
        tcpListener = new TcpListener(IPAddress.Loopback, port);
        tcpListener.Start();
        DebugLog.SharedInstance.Write($"Pipe: waiting for injected timer on TCP 127.0.0.1:{port}");
        tcpListener.AcceptTcpClientAsync().ContinueWith(t =>
        {
            if(t.IsFaulted)
                return;
            tcpClient = t.Result;
            DebugLog.SharedInstance.Write("Pipe: injected timer connected over TCP");
            pipeReader = new PipeReader(tcpClient.GetStream());
            pipeWriter = new StreamWriter(tcpClient.GetStream());
            SendScript(script);
        });
    }

    public event EventHandler<string>? OnMessage;

    public bool IsOpened
    {
        get
        {
            if(pipeReader == null)
                return true;
            return pipeReader.IsOK;
        }
    }

    public bool IsConnected
    {
        get
        {
            if(tcpListener != null)
                return tcpClient != null && tcpClient.Connected && pipeReader != null;
            return client != null && client.IsConnected;
        }
    }

    public void SendScript(string script)
    {
        foreach(var str in script.Split('\n'))
        {
            pipeWriter?.WriteLine("script " + str);
        }

        pipeWriter?.WriteLine("script_load");
        pipeWriter?.Flush();
    }

    public void SendString(string str)
    {
        pipeWriter?.WriteLine(str);
        pipeWriter?.Flush();
    }

    public void Dispose()
    {
        connectCancellation?.Cancel();
        pipeReader?.Dispose();
        //pipeWriter?.Dispose();
        tcpClient?.Dispose();
        tcpListener?.Stop();
        client?.Dispose();
    }

    public void Cycle()
    {
        if(!IsConnected || pipeReader == null)
            return;
        while(pipeReader.HasData)
        {
            var line = pipeReader.ReadLine();
            if(line != null && line != "")
                ParseData(line);
        }
    }

    private void ParseData(string data)
    {
        if(data.StartsWith("debug_message "))
        {
            DebugLog.SharedInstance.Write($"InjectedTimer said: {data.Substring(14)}");
            return;
        }
        OnMessage?.Invoke(this, data);
    }

    Task? connectionTask;
    CancellationTokenSource? connectCancellation;

    private TcpListener? tcpListener;
    private TcpClient? tcpClient;

    private PipeReader? pipeReader;
    private StreamWriter? pipeWriter;
    private NamedPipeClientStream? client;

    private const string TEST_CODE =
    "function on_load()\n" +
    "   debug_message_address(module_base_address('kernel32.dll'))\n" +
    "   debug_message('hello from luad')\n" +
    "end\n"+

    "function on_frame()\n" +
    "end";
}
