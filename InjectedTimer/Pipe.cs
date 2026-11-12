using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;

namespace InjectedTimer
{
    public sealed class Pipe : IDisposable
    {
        public Pipe(string tcpPort)
        {
            toSend = new ConcurrentQueue<string>();

            if(tcpPort != null && tcpPort != "")
            {
                ConnectTcp(int.Parse(tcpPort));
                return;
            }

            server = new NamedPipeServerStream("SpeedToolPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            server.WaitForConnection();
            connected = true;
            pipeWriter = new StreamWriter(server);
            pipeReader = new PipeReader(server);
            Console.Error.WriteLine("Test");
        }

        private void ConnectTcp(int port)
        {
            tcpClient = new TcpClient();
            while(true)
            {
                try
                {
                    tcpClient.Connect("127.0.0.1", port);
                    break;
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }
            connected = true;
            pipeWriter = new StreamWriter(tcpClient.GetStream());
            pipeReader = new PipeReader(tcpClient.GetStream());
        }

        public event EventHandler<string> OnIncomingCmd;

        public bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        public bool IsOk
        {
            get
            {
                if(!connected)
                    return true;
                if(tcpClient != null)
                    return tcpClient.Connected;
                return server.IsConnected;
            }
        }

        public void Dispose()
        {
            if(tcpClient != null)
                tcpClient.Dispose();
            else
                server.Dispose();
        }

        public void SendString(string str)
        {
            if(connected)
                toSend.Enqueue(str);
        }

        public void Cycle()
        {
            if(tcpClient != null ? !tcpClient.Connected : !server.IsConnected)
                return;

            while(!toSend.IsEmpty)
            {
                SendNext();
            }
            pipeWriter.Flush();

            if(!pipeReader.IsOK)
            {
                SendString("debug_message PipeReader is not OK");
            }

            if(pipeReader.HasData)
            {
                var cmd = pipeReader.ReadLine();
                OnIncomingCmd?.Invoke(this, cmd);
            }
        }

        private void SendNext()
        {
            if(toSend.IsEmpty)
                return;
            string res;
            while(!toSend.TryDequeue(out res));
            pipeWriter.WriteLine(res);
        }

        bool connected = false;

        ConcurrentQueue<string> toSend;

        private StreamWriter pipeWriter;
        PipeReader pipeReader;
        private NamedPipeServerStream server;
        private TcpClient tcpClient;
    }
}
