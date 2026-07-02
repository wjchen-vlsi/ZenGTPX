using System.Runtime.InteropServices;

namespace ZenGTPX.Zen;

internal sealed class AnsiString : IDisposable
{
    public IntPtr Pointer { get; }

    public AnsiString(string value)
    {
        Pointer = Marshal.StringToHGlobalAnsi(value);
    }

    public void Dispose()
    {
        if (Pointer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(Pointer);
        }
    }
}
