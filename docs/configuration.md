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

`maxTimeSeconds = 1.0` 與 `maxSimulations = 100` 是啟動與協定測試用設定，不適合用來評估 Zen7 / 天頂圍棋棋力。

若要進行實戰棋力測試，請參考 [Zen7 Strength Presets](zen7-strength-presets.md)。例如 9d 參數、30 秒上限：

```cfg
maxTimeSeconds = 30.0
maxSimulations = 6000
resignThreshold = 0.03
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

後續版本會補完整參數說明與 `rankPreset` convenience field。
