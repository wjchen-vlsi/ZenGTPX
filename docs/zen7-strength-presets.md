# Zen7 / 天頂圍棋棋力設定

本文件整理 ZenGTPX 使用 Zen7 / 天頂圍棋7 時，與棋力直接相關的設定。ZenGTPX 預設採用接近 Zen7 原生 GUI 的設定模型：指定棋力或指定思考時間；需要細部調整時再使用 advanced 模式。

## 結論

預設模式：

```cfg
mode = rank
rankPreset = 9d
threads = 4
```

這會套用 Zen7 GUI 等級表中的 `9d`：

```cfg
maxTimeSeconds = 60.0
maxSimulations = 6000
pnLevel = 3
pnWeight = 0.75
vnMixRate = 1.0
```

如果目標是比照 Zen7 GUI 的「指定思考時間」，使用：

```cfg
mode = fixed-time
maxTimeSeconds = 5.0
threads = 8
```

此模式會套用：

```cfg
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

若要直接微調底層參數，使用：

```cfg
mode = advanced
maxTimeSeconds = 60.0
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

## 棋力模式

| Mode | 用途 | 主要參數 |
| --- | --- | --- |
| `rank` | 比照 Zen7 GUI「指定等級」。 | `rankPreset` |
| `fixed-time` | 比照 Zen7 GUI「指定思考時間」。 | `maxTimeSeconds` |
| `advanced` | 直接設定底層搜尋參數。 | `maxTimeSeconds`, `maxSimulations`, `pnLevel`, `pnWeight`, `vnMixRate` |

`rank` 是預設模式。它會使用內建 Zen7 GUI 棋力表，並固定 `maxTimeSeconds = 60.0`。

`fixed-time` 不是段位 preset。它使用接近全力搜尋的基準參數，再由 `maxTimeSeconds` 控制每手時間。

`advanced` 適合測試、對局實驗、長考或棋力調校；使用者需自行負責參數組合。

## Zen7 GUI Rank Preset 表

以下表格來自原生 Zen7 GUI 對 `Zen.dll` 的呼叫點驗證。

| Preset | `maxSimulations` | `pnLevel` | `pnWeight` | `vnMixRate` | `maxTimeSeconds` |
| --- | ---: | ---: | ---: | ---: | ---: |
| 6k | 1000 | 0 | 0.30 | 1.6 | 60 |
| 5k | 1100 | 0 | 0.30 | 1.4 | 60 |
| 4k | 1200 | 0 | 0.30 | 1.0 | 60 |
| 3k | 1300 | 1 | 0.30 | 2.4 | 60 |
| 2k | 1400 | 1 | 0.30 | 2.0 | 60 |
| 1k | 1600 | 1 | 0.30 | 1.6 | 60 |
| 1d | 1800 | 1 | 0.35 | 1.3 | 60 |
| 2d | 2000 | 1 | 0.40 | 1.0 | 60 |
| 3d | 2200 | 2 | 0.45 | 2.0 | 60 |
| 4d | 2400 | 2 | 0.50 | 1.5 | 60 |
| 5d | 2700 | 2 | 0.55 | 1.0 | 60 |
| 6d | 3000 | 3 | 0.60 | 4.4 | 60 |
| 7d | 3500 | 3 | 0.65 | 2.8 | 60 |
| 8d | 4000 | 3 | 0.70 | 1.4 | 60 |
| 9d | 6000 | 3 | 0.75 | 1.0 | 60 |

`9d` 是 GUI 等級表最高段位，但不是 Zen7 的最大搜尋設定。

## Fixed-Time 設定

Zen7 GUI 的「指定思考時間」路徑使用：

| `maxSimulations` | `pnLevel` | `pnWeight` | `vnMixRate` | `maxTimeSeconds` |
| ---: | ---: | ---: | ---: | ---: |
| 1000000 | 3 | 1.0 | 0.75 | 使用者指定 |

ZenGTPX 寫法：

```cfg
mode = fixed-time
maxTimeSeconds = 5.0
```

建議：

| 用途 | `maxTimeSeconds` |
| --- | ---: |
| 快速 GUI 對弈 | 1-5 |
| 較強實戰 | 30-60 |
| 長考 / 分析 | 300+ |

## Threads 設定建議

`threads` 是棋力設定的一部分。它控制 Zen 原生搜尋使用多少 CPU 執行緒。

| 使用情境 | 建議 `threads` |
| --- | ---: |
| 保守穩定、降低 CPU 佔用 | 1-2 |
| 接近舊版 ZenGTP.py 預設 | 4 |
| 一般對局 / 8 核心 CPU 起點 | 8 |
| 提高棋力但保留系統流暢 | 10-12 |
| 長時間分析，且機器主要給 ZenGTPX 使用 | 12-14 |

注意：`mode = rank` 為了貼近 Zen7 原生 GUI 等級路徑，會把 threads 限制在最多 4。若要使用 8 或更高 threads，請使用 `mode = fixed-time` 或 `mode = advanced`。

不建議一開始直接設滿所有邏輯執行緒。Zen7 的 MCTS 搜尋不一定會隨 threads 線性變強，過高可能造成同步成本、搜尋效率折損、CPU 競爭與散熱降頻。

## AMD Ryzen 7 3700X 建議

Ryzen 7 3700X 是 8 核心 / 16 執行緒。若使用 `fixed-time` 或 `advanced` 模式，實戰起點建議：

```cfg
threads = 8
```

若要提高棋力，可依序測：

```cfg
threads = 10
threads = 12
```

長時間分析且不做其他工作，可短時間測：

```cfg
threads = 14
```

不建議日常預設使用：

```cfg
threads = 16
```

`16` 是 SMT 邏輯執行緒數，不是實體核心數；通常應先從實體核心數附近測試。

## 推薦設定

### GUI 9d

```cfg
mode = rank
rankPreset = 9d
threads = 4
resignThreshold = 0.03
```

### GUI 指定思考時間

```cfg
mode = fixed-time
maxTimeSeconds = 5.0
threads = 8
resignThreshold = 0.03
```

### Advanced 強棋力實戰

```cfg
mode = advanced
threads = 8
maxTimeSeconds = 60.0
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
resignThreshold = 0.03
```

### 快速測試

```cfg
mode = advanced
threads = 1
maxTimeSeconds = 1.0
maxSimulations = 100
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

這只適合測啟動、GTP 協定與 wrapper 流程，不代表棋力。

## 與 KataGo 比較

Zen7 / 天頂圍棋7 屬於 DeepZenGo 世代。它可以作為舊世代強引擎或人機對局參考，但不應預期在相同硬體與時間限制下接近現代 KataGo。

若目標是現代覆盤、勝率、目差、讓子與高穩定度分析，仍應以 KataGo 為主。
