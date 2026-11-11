using Microsoft.Win32.SafeHandles;

namespace SpeedTool.Platform.Linux;

internal sealed class ProcessMemoryReader : IDisposable
{
    public ProcessMemoryReader(int pid)
    {
        handle = File.OpenHandle($"/proc/{pid}/mem");
    }

    public long FailedReads { get; private set; }

    public long LastFailedAddress { get; private set; }

    public bool TryRead(long address, Span<byte> buffer)
    {
        try
        {
            return RandomAccess.Read(handle, buffer, address) == buffer.Length;
        }
        catch
        {
            FailedReads++;
            LastFailedAddress = address;
            return false;
        }
    }

    public void Dispose()
    {
        handle.Dispose();
    }

    private readonly SafeFileHandle handle;
}
