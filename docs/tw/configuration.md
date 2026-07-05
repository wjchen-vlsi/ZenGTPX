# 設定檔

ZenGTPX 以 `zen7.cfg` 作為主要設定檔。

## 檔案位置

請將設定檔放在 `ZenGTPX.exe` 同一層：

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

未指定 `--config` 時，ZenGTPX 會嘗試從執行檔所在目錄載入 `zen7.cfg`。

## 範本

範本位於：

```text
config\zen7.cfg
config\zen7_zh-TW.cfg
```

`zen7.cfg` 是預設部署範本。`zen7_zh-TW.cfg` 包含相同鍵值，並提供較完整的繁體中文註解、建議值與範圍。

## 格式

```cfg
key = value
```

以 `#` 開頭的行是註解。值後方也允許行內註解。

## 棋力模式

ZenGTPX 支援三種棋力模式：

| Mode | 用途 | 主要設定 |
| --- | --- | --- |
| `rank` | 預設模式，比照 Zen7 GUI 指定等級。 | `rankPreset` |
| `fixed-time` | 比照 Zen7 GUI 指定思考時間。 | `maxTimeSeconds` |
| `advanced` | 直接調整底層參數。 | `maxTimeSeconds`, `maxSimulations`, `pnLevel`, `pnWeight`, `vnMixRate` |

預設值：

```cfg
mode = rank
rankPreset = 9d
```

`mode = rank` 使用內建 Zen7 GUI 等級表。允許的 `rankPreset` 值：

```text
6k, 5k, 4k, 3k, 2k, 1k, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d
```

`mode = fixed-time` 使用 Zen7 GUI 指定思考時間的基準參數：

```cfg
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

以 `maxTimeSeconds` 控制每手思考秒數。

只有在需要直接調整底層參數時，才使用 `mode = advanced`。

## 常用設定

```cfg
zenDll = Zen.dll
gtpName = KataGo
boardSize = 19
komi = 7.5
handicap = 0
runtimeTimeOverride = disabled
threads = 4
resignThreshold = 0.1
```

建議範圍：

| Key | 範圍 | 建議 |
| --- | --- | --- |
| `gtpName` | 非空字串 | LizzieYzy Next 多候選點顯示建議 `KataGo`；一般 GTP scripts 可用 `ZenGTPX` |
| `boardSize` | `1` to `25` | `19` |
| `komi` | 任意數值 | `6.5` 或 `7.5` |
| `handicap` | `0` 或更大 | `0` |
| `finalScoreRule` | `japanese`, `territory`, `chinese`, 或 `area` | 類 Zen7 對局用 `japanese`；中國規則計分用 `area` |
| `runtimeTimeOverride` | `enabled` 或 `disabled` | LizzieYzy Next rank preset 對局建議 `disabled` |
| `threads` | 正整數 | 保守值 `1-4`，8 核心 CPU 可用 `8`，較強棋力可測 `10-12` |
| `resignThreshold` | `0.0` to `1.0` | `0.03-0.10` |

## 進階鍵值

這些鍵值只有在 `mode = advanced` 時直接使用；`mode = rank` 與 `mode = fixed-time` 會從 GUI 相容 preset 推導。

| Key | 範圍 | 參考值 |
| --- | --- | --- |
| `maxTimeSeconds` | `0` 或更大 | `1-5` 快棋，`30-60` 較強對局，`300+` 分析 |
| `maxSimulations` | 正整數 | `100` 煙霧測試，`2700` GUI 5d，`6000` GUI 9d，`1000000` fixed-time |
| `pnLevel` | 建議 `0` 到 `3` | 強設定使用 `3` |
| `pnWeight` | GUI 等級表為 `0.30` 到 `0.75`；fixed-time 為 `1.0` | 依模式而定 |
| `vnMixRate` | GUI 等級表為 `1.0` 到 `4.4`；fixed-time 為 `0.75` | 依模式而定 |

## GTP 行為備註

- `gtpName` 只控制 GTP `name` 回應。預設部署設定使用 `KataGo`，因為 LizzieYzy Next 會在 KataGo-compatible 路徑啟用多候選點分析顯示。
- `gtpName = KataGo` 不代表 ZenGTPX 支援 KataGo `analysis` JSON protocol。
- `runtimeTimeOverride = disabled` 時，ZenGTPX 會忽略 GUI runtime 時間控制命令：`time_settings`、`time_left` 與 `kata-set-param maxTime`。此語意同時適用於 `genmove` 與 analysis mode 引擎對局。
- `runtimeTimeOverride = enabled` 時，`time_settings` / `time_left` 會在支援時轉送到 Zen 原生時間 API，且 `kata-set-param maxTime N` 且 `N > 0` 時會套用 GUI runtime 每手時間上限。
- 啟用 runtime time override 時，`kata-set-param maxTime 0` 與 `time_settings 0 0 0` 會恢復啟動時由 `zen7.cfg` / CLI 得到的 effective `maxTimeSeconds`。
- `final_score` 使用 ZenGTP.py 相容公式，依 Zen territory statistics 估算。`finalScoreRule = japanese` / `territory` 使用 territory scoring；`chinese` / `area` 使用 area scoring。
- `final_status_list alive|dead|seki` 是相容性 stub，會回傳空清單；ZenGTPX 目前不提供可靠的死子判定。
- `zengtp_final_score_detail` 會回傳 scripts 與診斷用的估算拆解，包含 configured、area、territory 與 capture-adjusted 數值。
- 設定檔中的 `handicap` 是被動對局參數，不會自行放置棋子或改變棋盤狀態。
- 實際讓子放置仍由 GTP `fixed_handicap`、`set_free_handicap` 或 `place_free_handicap` 控制。
