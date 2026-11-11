using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.Windowing;
using SpeedTool.Platform.Debugging;

namespace SpeedTool.Platform;

internal static unsafe class GlfwClipboard
{
    public static void WireUp(ImGuiIOPtr io, IView view)
    {
        try
        {
            if(view.Native?.Glfw is not { } glfwWindow)
            {
                DebugLog.SharedInstance.Write("Clipboard: native GLFW window is unavailable, clipboard stays disabled");
                return;
            }

            io.ClipboardUserData = glfwWindow;
            io.GetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<void*, byte*>)&GetText;
            io.SetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<void*, byte*, void>)&SetText;
            DebugLog.SharedInstance.Write("Clipboard: ImGui clipboard wired to GLFW");
        }
        catch(Exception ex)
        {
            DebugLog.SharedInstance.Write("Clipboard: failed to wire clipboard: " + ex.Message);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static byte* GetText(void* userData)
    {
        try
        {
            var text = GlfwProvider.GLFW.Value.GetClipboardString((WindowHandle*)userData) ?? "";
            var size = Encoding.UTF8.GetByteCount(text) + 1;
            EnsureCapacity(size);
            fixed(char* chars = text)
                Encoding.UTF8.GetBytes(chars, text.Length, buffer, capacity - 1);
            buffer[size - 1] = 0;
            return buffer;
        }
        catch
        {
            buffer[0] = 0;
            return buffer;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void SetText(void* userData, byte* text)
    {
        try
        {
            GlfwProvider.GLFW.Value.SetClipboardString((WindowHandle*)userData, PtrToString(text));
        }
        catch
        {
        }
    }

    static GlfwClipboard()
    {
        buffer = (byte*)Marshal.AllocHGlobal(INITIAL_CAPACITY);
        buffer[0] = 0;
        capacity = INITIAL_CAPACITY;
    }

    private static void EnsureCapacity(int size)
    {
        if(capacity >= size)
            return;

        var fresh = (byte*)Marshal.AllocHGlobal(size);
        Buffer.MemoryCopy(buffer, fresh, size, capacity);
        Marshal.FreeHGlobal((nint)buffer);
        buffer = fresh;
        capacity = size;
    }

    private static string PtrToString(byte* text)
    {
        int len = 0;
        while(text[len] != 0)
            len++;
        return Encoding.UTF8.GetString(text, len);
    }

    private const int INITIAL_CAPACITY = 4096;

    private static byte* buffer;
    private static int capacity;
}
