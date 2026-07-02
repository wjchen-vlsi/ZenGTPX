# Zen7 / 天頂圍棋棋力設定

本文件整理 ZenGTPX 使用 Zen7 / 天頂圍棋7 時，與棋力直接相關的設定。重點是把「段位 preset」、「實戰長考設定」、「分析設定」分開，避免把 `9d` 誤解成 Zen7 的絕對最強模式。

## 結論

`9d` 是舊版 `ZenGTP.py` 內建段位表中的最高段位 preset：

```cfg
threads = 8
maxTimeSeconds = 60.0
maxSimulations = 6000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

如果目標是讓 Zen7 盡量強，而不是嚴格使用段位表的 `9d`，應該使用長考設定：

```cfg
threads = 8
maxTimeSeconds = 60.0
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

`threads` 必須納入棋力設定。ZenGTP.py 舊版預設是 `4`，ZenGTPX 目前公開 config 預設為 `1` 是保守穩定值，不是最佳棋力值。以 Ryzen 7 3700X 這類 8 核心 / 16 執行緒 CPU 來說，實戰棋力測試建議從 `threads = 8` 開始。

## 來源與限制

主要技術來源：

- ZenGTP source: <https://github.com/yzyray/ZenGTP/blob/main/ZenGTP.py>
- 天頂の囲碁7 Zen product page: <https://book.mynavi.jp/tencho7/>
- KataGo source project: <https://github.com/lightvector/KataGo>

`ZenGTP.py` 的 Help 內建 Zen7 棋力表，列出 `6k` 到 `9d`，也列出 `5s+` 與 `analyse`。這些參數可以作為 Zen7 wrapper 設定依據，但不代表固定棋力保證。

實際棋力會受以下因素影響：

- CPU 型號與散熱。
- `threads` 設定。
- 每手時間。
- 搜尋上限 `maxSimulations`。
- 當前局面複雜度。
- ZenGTPX wrapper 的停止條件與 GTP GUI 傳入的用時設定。

## 核心參數

| ZenGTPX config | ZenGTP.py 參數 | 作用 |
| --- | --- | --- |
| `threads` | `-t` / `--threads` | Zen 原生搜尋執行緒數，會呼叫 `ZenSetNumberOfThreads`。 |
| `maxTimeSeconds` | `--maxtime` | 每手最大思考秒數，會呼叫 `ZenSetMaxTime`。 |
| `maxSimulations` | `--maxsim` | 最大搜尋模擬數 / playouts，會呼叫 `ZenSetNumberOfSimulations`。 |
| `pnLevel` | `--pnlevel` | Policy Network 等級。Zen7 高段位設定使用 `3`。 |
| `pnWeight` | `--pnweight` | Policy Network 權重。Zen7 `9d` 與長考設定使用 `1.0`。 |
| `vnMixRate` | `--vnrate` | Value Network 混合比例。Zen7 `9d` 與長考設定使用 `0.75`。 |
| `resignThreshold` | `--resign` | 認輸門檻。比賽測試可設低一點，例如 `0.03`。 |
| `strength` | `-s` / `--strength` | wrapper 依 top move visits 停止搜尋的條件，不是 Zen.dll 直接參數。 |

## 段位 Preset 表

以下是舊版 `ZenGTP.py` Help 內建的 Zen7 段位表。

| Preset | `maxSimulations` | `pnLevel` | `pnWeight` | `vnMixRate` | `maxTimeSeconds` |
| --- | ---: | ---: | ---: | ---: | ---: |
| 6k | 1000 | 0 | 1.6 | 0.30 | 60 |
| 5k | 1100 | 0 | 1.4 | 0.30 | 60 |
| 4k | 1200 | 0 | 1.0 | 0.30 | 60 |
| 3k | 1300 | 1 | 2.4 | 0.30 | 60 |
| 2k | 1400 | 1 | 2.0 | 0.30 | 60 |
| 1k | 1500 | 1 | 1.6 | 0.30 | 60 |
| 1d | 1800 | 1 | 1.3 | 0.35 | 60 |
| 2d | 2000 | 1 | 1.0 | 0.40 | 60 |
| 3d | 2200 | 2 | 2.0 | 0.45 | 60 |
| 4d | 2400 | 2 | 1.5 | 0.50 | 60 |
| 5d | 2700 | 2 | 1.0 | 0.55 | 60 |
| 6d | 3000 | 3 | 4.4 | 0.60 | 60 |
| 7d | 3500 | 3 | 2.8 | 0.65 | 60 |
| 8d | 4000 | 3 | 1.4 | 0.70 | 60 |
| 9d | 6000 | 3 | 1.0 | 0.75 | 60 |

`9d` 是此表最高段位，但不是 Zen7 的最大搜尋設定。

## 長考與分析設定

`ZenGTP.py` Help 另列出兩組非段位設定：

| Mode | `maxSimulations` | `pnLevel` | `pnWeight` | `vnMixRate` | `maxTimeSeconds` | 用途 |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `5s+` | 1000000 | 3 | 1.0 | 0.75 | 5 秒以上 | 實戰長考。用時間限制控制每手多久。 |
| `analyse` | 1000000 | 3 | 1.0 | 0.75 | 3600 | 分析或超長考。 |

ZenGTPX 設定時不建議把這兩組叫做 `9d`。應直接寫明：

```cfg
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

再依用途設定 `maxTimeSeconds`。

## Threads 設定建議

`threads` 是 Zen7 棋力設定的一部分。它控制 Zen 原生搜尋使用多少 CPU 執行緒。

一般建議：

| 使用情境 | 建議 `threads` |
| --- | ---: |
| 保守穩定、降低 CPU 佔用 | 1-2 |
| 接近 ZenGTP.py 舊版預設 | 4 |
| 一般對局 / LizzieYzy Next 搭配使用 | 8 |
| 想提高棋力但保留系統流暢 | 10-12 |
| 長時間分析，且機器主要給 ZenGTPX 使用 | 12-14 |

不建議一開始直接設滿所有邏輯執行緒。Zen7 的 MCTS 搜尋不一定會隨 threads 線性變強，過高可能造成同步成本、搜尋效率折損、CPU 競爭與散熱降頻。

## AMD Ryzen 7 3700X 建議

若你指的是 AMD Ryzen 7 3700X，官方規格是 8 核心 / 16 執行緒。

建議值：

```cfg
threads = 8
```

這是 Ryzen 7 3700X 上較合理的實戰起點。它使用實體核心數等級的平行度，通常比 `threads = 4` 更適合棋力測試，也不會直接把 16 個 SMT 邏輯執行緒全部吃滿。

若要測 Zen7 上限，可以依序測：

```cfg
threads = 8
threads = 10
threads = 12
```

若是單純長時間分析、不做其他工作，可再試：

```cfg
threads = 14
```

不建議預設使用：

```cfg
threads = 16
```

原因是 `16` 是 SMT 邏輯執行緒數，不是實體核心數。Zen7 這類 CPU 搜尋工作通常更適合先以實體核心數附近測試。對 3700X 而言，`8` 適合一般對局，`12` 適合作為分析用途測試上限，`16` 只適合短時間 benchmark，不建議作為日常對局設定。

## 推薦設定

### 穩定 9d 參考設定

```cfg
threads = 8
maxTimeSeconds = 60.0
maxSimulations = 6000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
resignThreshold = 0.03
```

### 強棋力實戰設定

```cfg
threads = 8
maxTimeSeconds = 60.0
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
resignThreshold = 0.03
```

Ryzen 7 3700X 若要提高棋力，可再測：

```cfg
threads = 10
```

若 CPU 溫度與系統反應都正常，再測：

```cfg
threads = 12
```

長時間分析且不做其他工作，可短時間測：

```cfg
threads = 14
```

### 快速測試設定

```cfg
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

若目標是現代覆盤、勝率、目差、讓子與高穩定度分析，仍應以 KataGo 為主。Zen7 比較適合用來比較 2017 年前後的商業圍棋 AI 棋力與風格。
