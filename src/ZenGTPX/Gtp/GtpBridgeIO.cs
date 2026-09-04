using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ZenGTPX.Gtp;

public static class GtpBridgeIO
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DuplicateHandle(
        IntPtr hSourceProcessHandle, IntPtr hSourceHandle,
        IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
        uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetStdHandle(int nStdHandle, SafeFileHandle hHandle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _open_osfhandle(SafeFileHandle osfhandle, int flags);

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int _open(string filename, int oflag);

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _dup2(int fd1, int fd2);

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _close(int fd);

    private const int STDOUT_HANDLE = -11;
    private const int STDERR_HANDLE = -12;
    private const uint DUPLICATE_SAME_ACCESS = 2;
    private const int STDOUT_FD = 1;
    private const int STDERR_FD = 2;

    private const int O_WRONLY = 0x0001;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint OPEN_EXISTING = 3;

    public static void Initialize(StreamWriter? zenNativeLogWriter)
    {
        IntPtr hOriginalStdout = GetStdHandle(STDOUT_HANDLE);
        IntPtr hOriginalStderr = GetStdHandle(STDERR_HANDLE);

        DuplicateHandle(
            GetCurrentProcess(), hOriginalStdout,
            GetCurrentProcess(), out IntPtr hSafeStdout,
            0, false, DUPLICATE_SAME_ACCESS);

        DuplicateHandle(
            GetCurrentProcess(), hOriginalStderr,
            GetCurrentProcess(), out IntPtr hSafeStderr,
            0, false, DUPLICATE_SAME_ACCESS);

        StreamWriter safeStdoutWriter = new StreamWriter(
            new FileStream(
                new SafeFileHandle(hSafeStdout, true),
                FileAccess.Write
            ),
            new UTF8Encoding(false)
        )
        {
            AutoFlush = true
        };

        StreamWriter safeStderrWriter = new StreamWriter(
            new FileStream(
                new SafeFileHandle(hSafeStderr, true),
                FileAccess.Write
            ),
            new UTF8Encoding(false)
        )
        {
            AutoFlush = true
        };

        Console.SetOut(safeStdoutWriter);
        Console.SetError(safeStderrWriter);

        if (zenNativeLogWriter is not null && zenNativeLogWriter.BaseStream is FileStream fs)
        {
            SafeFileHandle safeLogHandle = fs.SafeFileHandle;

            SetStdHandle(STDOUT_HANDLE, safeLogHandle);
            SetStdHandle(STDERR_HANDLE, safeLogHandle);

            int logFd = _open_osfhandle(safeLogHandle, 0);
            if (logFd != -1)
            {
                _dup2(logFd, STDOUT_FD);
                _dup2(logFd, STDERR_FD);
            }
        }
        else
        {
            try
            {
                int nullFd = _open("NUL", O_WRONLY);
                if (nullFd != -1)
                {
                    _dup2(nullFd, STDOUT_FD);
                    _dup2(nullFd, STDERR_FD);
                    _close(nullFd);
                }

                SafeFileHandle hNul = CreateFile("NUL", GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (!hNul.IsInvalid)
                {
                    SetStdHandle(STDOUT_HANDLE, hNul);
                    SetStdHandle(STDERR_HANDLE, hNul);
                }
            }
            catch (Exception) {}
        }
    }
}

