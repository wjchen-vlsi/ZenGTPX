using System.Runtime.InteropServices;
using System.Text;

namespace ZenGTPX.Zen;

internal sealed class ZenNative : IDisposable
{
    private readonly IntPtr _module;

    private readonly ZenClearBoard _clearBoard;
    private readonly ZenGetBoardColor _getBoardColor;
    private readonly ZenGetNumBlackPrisoners _getNumBlackPrisoners;
    private readonly ZenGetNumWhitePrisoners _getNumWhitePrisoners;
    private readonly ZenGetPolicyKnowledge _getPolicyKnowledge;
    private readonly ZenGetTerritoryStatistics _getTerritoryStatistics;
    private readonly ZenGetTopMoveInfo _getTopMoveInfo;
    private readonly ZenInitialize _initialize;
    private readonly ZenIsInitialized _isInitialized;
    private readonly ZenIsThinking _isThinking;
    private readonly ZenPass _pass;
    private readonly ZenPlay _play;
    private readonly ZenReadGeneratedMove _readGeneratedMove;
    private readonly ZenSetBoardSize _setBoardSize;
    private readonly ZenSetKomi _setKomi;
    private readonly ZenSetMaxTime _setMaxTime;
    private readonly ZenSetNextColor _setNextColor;
    private readonly ZenSetNumberOfSimulations _setNumberOfSimulations;
    private readonly ZenSetNumberOfThreads _setNumberOfThreads;
    private readonly ZenSetPnLevel _setPnLevel;
    private readonly ZenSetPnWeight _setPnWeight;
    private readonly ZenSetVnMixRate _setVnMixRate;
    private readonly ZenStartThinking _startThinking;
    private readonly ZenStopThinking _stopThinking;
    private readonly ZenTimeLeft _timeLeft;
    private readonly ZenTimeSettings _timeSettings;
    private readonly ZenUndo _undo;

    private ZenNative(
        IntPtr module,
        ZenClearBoard clearBoard,
        ZenGetBoardColor getBoardColor,
        ZenGetNumBlackPrisoners getNumBlackPrisoners,
        ZenGetNumWhitePrisoners getNumWhitePrisoners,
        ZenGetPolicyKnowledge getPolicyKnowledge,
        ZenGetTerritoryStatistics getTerritoryStatistics,
        ZenGetTopMoveInfo getTopMoveInfo,
        ZenInitialize initialize,
        ZenIsInitialized isInitialized,
        ZenIsThinking isThinking,
        ZenPass pass,
        ZenPlay play,
        ZenReadGeneratedMove readGeneratedMove,
        ZenSetBoardSize setBoardSize,
        ZenSetKomi setKomi,
        ZenSetMaxTime setMaxTime,
        ZenSetNextColor setNextColor,
        ZenSetNumberOfSimulations setNumberOfSimulations,
        ZenSetNumberOfThreads setNumberOfThreads,
        ZenSetPnLevel setPnLevel,
        ZenSetPnWeight setPnWeight,
        ZenSetVnMixRate setVnMixRate,
        ZenStartThinking startThinking,
        ZenStopThinking stopThinking,
        ZenTimeLeft timeLeft,
        ZenTimeSettings timeSettings,
        ZenUndo undo)
    {
        _module = module;
        _clearBoard = clearBoard;
        _getBoardColor = getBoardColor;
        _getNumBlackPrisoners = getNumBlackPrisoners;
        _getNumWhitePrisoners = getNumWhitePrisoners;
        _getPolicyKnowledge = getPolicyKnowledge;
        _getTerritoryStatistics = getTerritoryStatistics;
        _getTopMoveInfo = getTopMoveInfo;
        _initialize = initialize;
        _isInitialized = isInitialized;
        _isThinking = isThinking;
        _pass = pass;
        _play = play;
        _readGeneratedMove = readGeneratedMove;
        _setBoardSize = setBoardSize;
        _setKomi = setKomi;
        _setMaxTime = setMaxTime;
        _setNextColor = setNextColor;
        _setNumberOfSimulations = setNumberOfSimulations;
        _setNumberOfThreads = setNumberOfThreads;
        _setPnLevel = setPnLevel;
        _setPnWeight = setPnWeight;
        _setVnMixRate = setVnMixRate;
        _startThinking = startThinking;
        _stopThinking = stopThinking;
        _timeLeft = timeLeft;
        _timeSettings = timeSettings;
        _undo = undo;
    }

    public bool IsInitialized => _isInitialized() != 0;

    public bool IsThinking => _isThinking() != int.MinValue;

    public static ZenNative Load(string dllPath)
    {
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException("Zen.dll was not found.", dllPath);
        }

        var module = NativeMethods.LoadLibrary(dllPath);
        if (module == IntPtr.Zero)
        {
            throw new InvalidOperationException($"LoadLibrary failed for {dllPath}. Win32Error={Marshal.GetLastWin32Error()}");
        }

        try
        {
            return new ZenNative(
                module,
                Get<ZenClearBoard>(module, 2, "ZenClearBoard"),
                Get<ZenGetBoardColor>(module, 5, "ZenGetBoardColor"),
                Get<ZenGetNumBlackPrisoners>(module, 8, "ZenGetNumBlackPrisoners"),
                Get<ZenGetNumWhitePrisoners>(module, 9, "ZenGetNumWhitePrisoners"),
                Get<ZenGetPolicyKnowledge>(module, 10, "ZenGetPolicyKnowledge"),
                Get<ZenGetTerritoryStatistics>(module, 11, "ZenGetTerritoryStatictics"),
                Get<ZenGetTopMoveInfo>(module, 12, "ZenGetTopMoveInfo"),
                Get<ZenInitialize>(module, 13, "ZenInitialize"),
                Get<ZenIsInitialized>(module, 14, "ZenIsInitialized"),
                Get<ZenIsThinking>(module, 17, "ZenIsThinking"),
                Get<ZenPass>(module, 19, "ZenPass"),
                Get<ZenPlay>(module, 20, "ZenPlay"),
                Get<ZenReadGeneratedMove>(module, 21, "ZenReadGeneratedMove"),
                Get<ZenSetBoardSize>(module, 22, "ZenSetBoardSize"),
                Get<ZenSetKomi>(module, 23, "ZenSetKomi"),
                Get<ZenSetMaxTime>(module, 24, "ZenSetMaxTime"),
                Get<ZenSetNextColor>(module, 25, "ZenSetNextColor"),
                Get<ZenSetNumberOfSimulations>(module, 26, "ZenSetNumberOfSimulations"),
                Get<ZenSetNumberOfThreads>(module, 27, "ZenSetNumberOfThreads"),
                Get<ZenSetPnLevel>(module, 28, "ZenSetPnLevel"),
                Get<ZenSetPnWeight>(module, 29, "ZenSetPnWeight"),
                Get<ZenSetVnMixRate>(module, 30, "ZenSetVnMixRate"),
                Get<ZenStartThinking>(module, 31, "ZenStartThinking"),
                Get<ZenStopThinking>(module, 32, "ZenStopThinking"),
                Get<ZenTimeLeft>(module, 33, "ZenTimeLeft"),
                Get<ZenTimeSettings>(module, 34, "ZenTimeSettings"),
                Get<ZenUndo>(module, 35, "ZenUndo"));
        }
        catch
        {
            NativeMethods.FreeLibrary(module);
            throw;
        }
    }

    public void Initialize(IntPtr path) => _initialize(path);

    public void ClearBoard() => _clearBoard();

    public int GetBoardColor(int x, int y) => _getBoardColor(x, y);

    public int GetNumBlackPrisoners() => _getNumBlackPrisoners();

    public int GetNumWhitePrisoners() => _getNumWhitePrisoners();

    public int[,] GetPolicyKnowledge()
    {
        var values = new int[19 * 19];
        _getPolicyKnowledge(values);

        var result = new int[19, 19];
        for (var y = 0; y < 19; y++)
        {
            for (var x = 0; x < 19; x++)
            {
                result[y, x] = values[(y * 19) + x];
            }
        }

        return result;
    }

    public int[,] GetTerritoryStatistics()
    {
        var values = new int[19 * 19];
        _getTerritoryStatistics(values);

        var result = new int[19, 19];
        for (var y = 0; y < 19; y++)
        {
            for (var x = 0; x < 19; x++)
            {
                result[y, x] = values[(y * 19) + x];
            }
        }

        return result;
    }

    public ZenTopMove GetTopMoveInfo(int index)
    {
        var x = 0;
        var y = 0;
        var playouts = 0;
        var winrate = 0.0f;
        var text = new byte[256];
        _getTopMoveInfo(index, ref x, ref y, ref playouts, ref winrate, text, text.Length);

        var length = Array.IndexOf(text, (byte)0);
        if (length < 0)
        {
            length = text.Length;
        }

        return new ZenTopMove(x, y, playouts, winrate, Encoding.ASCII.GetString(text, 0, length));
    }

    public void Pass(int color) => _pass(color);

    public bool Play(int x, int y, int color) => _play(x, y, color) != 0;

    public ZenGeneratedMove ReadGeneratedMove()
    {
        var x = 0;
        var y = 0;
        byte pass = 0;
        byte resign = 0;
        _readGeneratedMove(ref x, ref y, ref pass, ref resign);
        return new ZenGeneratedMove(x, y, pass != 0, resign != 0);
    }

    public void SetBoardSize(int boardSize) => _setBoardSize(boardSize);

    public void SetKomi(float komi) => _setKomi(komi);

    public void SetMaxTime(float seconds) => _setMaxTime(seconds);

    public void SetNextColor(int color) => _setNextColor(color);

    public void SetNumberOfSimulations(int count) => _setNumberOfSimulations(count);

    public void SetNumberOfThreads(int threads) => _setNumberOfThreads(threads);

    public void SetPnLevel(int level) => _setPnLevel(level);

    public void SetPnWeight(float weight) => _setPnWeight(weight);

    public void SetVnMixRate(float rate) => _setVnMixRate(rate);

    public void StartThinking(int color) => _startThinking(color);

    public void StopThinking() => _stopThinking();

    public void TimeLeft(int color, int time, int stones) => _timeLeft(color, time, stones);

    public void TimeSettings(int mainTime, int byoyomiTime, int periods) => _timeSettings(mainTime, byoyomiTime, periods);

    public bool Undo(int count) => _undo(count) != 0;

    public void Dispose()
    {
        NativeMethods.FreeLibrary(_module);
    }

    private static TDelegate Get<TDelegate>(IntPtr module, int ordinal, string name)
        where TDelegate : Delegate
    {
        var address = NativeMethods.GetProcAddress(module, new IntPtr(ordinal));
        if (address == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Ordinal {ordinal} ({name}) was not found. Win32Error={Marshal.GetLastWin32Error()}");
        }

        return Marshal.GetDelegateForFunctionPointer<TDelegate>(address);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenClearBoard();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ZenGetBoardColor(int x, int y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ZenGetNumBlackPrisoners();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ZenGetNumWhitePrisoners();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenGetPolicyKnowledge([In, Out] int[] values);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenGetTerritoryStatistics([In, Out] int[] values);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenGetTopMoveInfo(int index, ref int x, ref int y, ref int playouts, ref float winrate, [Out] byte[] text, int textLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenInitialize(IntPtr path);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte ZenIsInitialized();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ZenIsThinking();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenPass(int color);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte ZenPlay(int x, int y, int color);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenReadGeneratedMove(ref int x, ref int y, ref byte pass, ref byte resign);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetBoardSize(int boardSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetKomi(float komi);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetMaxTime(float seconds);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetNextColor(int color);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetNumberOfSimulations(int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetNumberOfThreads(int threads);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetPnLevel(int level);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetPnWeight(float weight);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenSetVnMixRate(float rate);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenStartThinking(int color);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenStopThinking();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenTimeLeft(int color, int time, int stones);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ZenTimeSettings(int mainTime, int byoyomiTime, int periods);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte ZenUndo(int count);
}

internal readonly record struct ZenTopMove(int X, int Y, int Playouts, float Winrate, string Text);

internal readonly record struct ZenGeneratedMove(int X, int Y, bool Pass, bool Resign);

internal static partial class NativeMethods
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32", SetLastError = true)]
    internal static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr procName);

    [DllImport("kernel32", SetLastError = true)]
    internal static extern int FreeLibrary(IntPtr hModule);
}
