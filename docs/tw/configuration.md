# 設定檔

ZenGTPX 以 `zen7.cfg` 作為主要設定檔。

GUI client 可透過 [Configuration Protocol v1](./configuration-protocol.md) 覆蓋目前 process 的十個棋力／規則 profile 欄位。協議的 save 由 client 負責，不會回寫此設定檔。

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
tracePath = gtp_logs/zengtpx-trace-{timestamp}-{pid}.log
boardSize = 19
komi = 0.5
handicap = 0
finalScoreRule = chinese
threads = 8
resignThreshold = 0.1
```

建議範圍：

| Key | 範圍 | 建議 |
| --- | --- | --- |
| `gtpName` | 非空字串 | LizzieYzy Next 多候選點顯示建議 `KataGo`；一般 GTP scripts 可用 `ZenGTPX` |
| `tracePath` | 路徑或留空 | 開發期保留預設值以自動記錄 GUI GTP trace；留空可停用 |
| `boardSize` | `1` to `25` | `19` |
| `komi` | 任意數值 | 中國規則網路對局預設 `0.5`；若 GUI / 規則設定需要標準貼目，可用 `6.5` 或 `7.5` |
| `handicap` | `0` 或更大 | `0` |
| `finalScoreRule` | `japanese`, `territory`, `chinese`, 或 `area` | 發行範本預設 `chinese`；類 Zen7 territory scoring 可用 `japanese` |
| `threads` | 正整數 | 保守值 `1-4`，8 核心 CPU 可用 `8`，較強棋力可測 `10-12` |
| `resignThreshold` | `0.0` to `1.0` | `0.03-0.10` |

## 進階鍵值

這些鍵值只有在 `mode = advanced` 時直接使用；`mode = rank` 與 `mode = fixed-time` 會從 GUI 相容 preset 推導。

| Key | 範圍 | 參考值 |
| --- | --- | --- |
| `maxTimeSeconds` | `0` 或更大 | `1-5` 快棋，`30-60` 較強對局，`300+` 分析 |
| `maxSimulations` | 正整數 | `100` 煙霧測試，`2700` GUI 5d，`6000` GUI 9d，`1000000` fixed-time |
| `pnLevel` | 建議 `0` 到 `3` | 強設定使用 `3` |
| `pnWeight` | GUI 等級表為 `1.0` 到 `4.4`；fixed-time 為 `1.0` | 依模式而定 |
| `vnMixRate` | GUI 等級表為 `0.30` 到 `0.75`；fixed-time 為 `0.75` | 依模式而定 |

## GTP 行為備註

- `gtpName` 只控制 GTP `name` 回應。預設部署設定使用 `KataGo`，因為 LizzieYzy Next 會在 KataGo-compatible 路徑啟用多候選點分析顯示。
- `gtpName = KataGo` 不代表 ZenGTPX 支援 KataGo `analysis` JSON protocol。
- `tracePath` 會寫入原始 GTP trace，不會污染 stdout。相對路徑會從執行檔所在目錄解析。支援 token：`{timestamp}`、`{date}`、`{pid}`。若有設定 `ZENGTPX_TRACE_PATH`，環境變數仍會優先覆蓋此設定。
- `time_settings` / `time_left` 會在支援時轉送到 Zen 原生時間 API；`time_settings` 也會更新 wrapper 的每手期限。
- `final_score` 使用 ZenGTP.py 相容公式，依 Zen territory statistics 估算。`finalScoreRule = japanese` / `territory` 使用 territory scoring；`chinese` / `area` 使用 area scoring。
- `final_status_list alive|dead|seki` 是相容性 stub，會回傳空清單；ZenGTPX 目前不提供可靠的死子判定。
- `zengtp_final_score_detail` 會回傳 scripts 與診斷用的估算拆解，包含 configured、area、territory 與 capture-adjusted 數值。
- 設定檔中的 `handicap` 是被動對局參數，不會自行放置棋子或改變棋盤狀態。
- 實際讓子放置仍由 GTP `fixed_handicap`、`set_free_handicap` 或 `place_free_handicap` 控制。
