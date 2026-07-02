# Configuration

ZenGTPX 使用 `zen7.cfg` 作為主要設定檔。

## File Location

建議將設定檔放在 `ZenGTPX.exe` 同一層：

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

未指定 `--config` 時，ZenGTPX 會自動嘗試讀取同目錄的 `zen7.cfg`。

## Templates

範本位於：

```text
config\zen7.cfg
config\zen7_zh-TW.cfg
```

## Common Settings

常用參數：

```cfg
zenDll = Zen.dll
boardSize = 19
komi = 7.5
threads = 1
maxTimeSeconds = 1.0
maxSimulations = 100
resignThreshold = 0.1
```

後續版本會補完整參數說明。
