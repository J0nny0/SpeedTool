
namespace SpeedTool.Injector;

using System.Collections.Concurrent;
using System.IO;

/// <summary>
/// Helper reader to asynchronously read from a stream
/// </summary>
public sealed class PipeReader : IDisposable
{
    public PipeReader(Stream stream)
    {
        sr = new StreamReader(stream);
        workerThread = new Thread(Worker);
        workerThread.Start();
    }

    public bool IsOK
    {
        get
        {
            return !IsClosing;
        }
    }

    public void Dispose()
    {
        sr.Dispose();
        workerThread.Join();
    }

    public bool HasData
    {
        get
        {
            return !lines.IsEmpty;
        }
    }

    public string ReadLine()
    {
        string? ret;
        while(!lines.TryDequeue(out ret));
        return ret;
    }

    private void Worker()
    {
        while(!IsClosing)
        {
            try
            {
            var line = sr.ReadLine();
            if(line != null && line != "")
                lines.Enqueue(line);
            if(line == null)
                IsClosing = true;
            }
            catch
            {
                IsClosing = true;
            }
        }
    }

    bool IsClosing = false;
    Thread workerThread;
    ConcurrentQueue<string> lines = new();

    private StreamReader sr;
}
