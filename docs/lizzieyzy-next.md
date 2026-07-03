# LizzieYzy Next Setup

ZenGTPX 應作為一般 GTP engine 加入 LizzieYzy Next。

## Files

先準備同一層目錄：

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

## Engine Settings

建議設定：

```text
Name: ZenGTPX
Engine path: <ZenGTPX.exe 的完整路徑>
Working directory: <ZenGTPX.exe 所在目錄>
Arguments:
```

若 GUI 需要明確參數，可使用：

```text
--config zen7.cfg
```

## Notes

- 不要將 ZenGTPX 設為 analysis engine。
- 第一版目標是一般 GTP 對弈流程。
- `final_score` 為 Zen territory statistics 推算的估分，不是完整終局數子裁判。
- `Zen.dll` 必須由使用者自行合法提供。
