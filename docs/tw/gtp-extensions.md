# ZenGTPX GTP 擴充指令

本文件說明 ZenGTPX 提供給 scripts 與診斷用途的專屬 GTP 指令。
這些指令不是 GTP v2 的一部分，也不應被視為 Lizzie 或 KataGo analysis protocol。

## Lizzie / KataGo-style GTP analysis commands

ZenGTPX 支援第一版供 GUI 顯示候選點用的 GTP analysis commands：

- `lz-analyze [interval]`
- `kata-analyze [color] [interval]`
- `analyze [color] [interval]`
- `lz-genmove_analyze [color] [interval]`
- `kata-genmove_analyze [color] [interval]`
- `genmove_analyze [color] [interval]`
- `stop`

使用一般執行檔入口時，analysis commands 會先以 `=` 確認 GTP 指令，然後持續在背景輸出 `info move ...` 分析行，直到收到 `stop` 或會改變棋盤的指令。
選用的 `interval` 參數比照 KataGo / Leela-style centiseconds。例如 `kata-analyze B 10` 表示在底層 Zen search 持續進行時，ZenGTPX 會約每 100 ms 取樣並輸出 analysis。
候選點座標、playouts、winrate 與 PV 文字來自 `ZenGetTopMoveInfo(index)`。
`prior` 由 `ZenGetPolicyKnowledge` 取得的正 policy values，在回傳候選集合內正規化而來。
`analyze` 會作為 KataGo-style alias 接受，用於 GUI 相容。

`kata-analyze` 會輸出單行，內含多個 KataGo-style `info move` entries：

```text
info move Q16 visits 1700 winrate 0.5342 scoreLead 1.0 scoreMean 1.0 prior 0.625 order 0 pv Q16 D4 info move D4 visits 850 winrate 0.4980 scoreLead -0.1 scoreMean -0.1 prior 0.375 order 1 pv D4 Q16
=
```

`lz-analyze` 會以 0..10000 格式輸出 Leela-style `winrate`：

```text
info move Q16 visits 1700 winrate 5342 pv Q16 D4
=
```

目前限制：

- `stop` 會要求取消背景串流，並中斷進行中的 Zen analysis loop。原生清理仍取決於 `ZenStopThinking` 是否返回。
- 候選點座標、playouts、winrate 與 PV 文字來自 `ZenGetTopMoveInfo(index)`。
- Analysis output 來自同一次連續 Zen thinking session 的取樣。若 Zen native calls 或 GUI I/O 花費較久，實際輸出頻率可能慢於指定 interval。
- `prior` 是由 Zen policy knowledge 推導，並在回傳候選點間正規化；不應視為 KataGo-equivalent neural policy prior。
- `scoreLead` 與 `scoreMean` 是由 winrate 推導的相容性 placeholder，因為 Zen7 目前未透過 wrapped API 暴露可靠的等價數值。
- LizzieYzy Next 若要顯示多候選點，請在 `zen7.cfg` 使用 `gtpName = KataGo`；這只會改變 GTP `name` 回應。
- ZenGTPX 不實作 KataGo `analysis` JSON protocol。

`lz-genmove_analyze`、`kata-genmove_analyze` 與 `genmove_analyze` 是 GUI 引擎對局模式的相容性指令。
它們會像 `genmove` 一樣搜尋一手，輸出一行 analysis 資訊，最後在同一個 GTP response 內以以下格式結束：

```text
play <vertex>
```

選出的手會套用到內部棋盤狀態，行為與 `genmove` 一致。
選用的 `interval` 參數會為了 GUI 相容性接受；但 `genmove_analyze` 目前仍只在搜尋期間輸出單次 analysis snapshot，不是整段落子搜尋過程的長時間串流。
`genmove_analyze` 會作為 KataGo-style alias 接受，用於未使用 `kata-` 前綴的 GUI 路徑。

## SGF 載入相容性

ZenGTPX 為 GUI 棋盤同步相容性接受 `loadsgf <filename> [moveNumber]`。
第一版刻意只支援 LizzieYzy Next readboard snapshot 等工具需要的子集：

- `SZ[n]` 棋盤大小；未指定時預設 19。
- `KM[x]` 貼目；存在時套用。
- 主線 `B[xy]` / `W[xy]` 落子，包含空值 pass。
- 基本 `AB[xy]` / `AW[xy]` setup stones。
- 選用非負 `moveNumber`，只重放前 N 手主線落子。

ZenGTPX 不實作完整 SGF 編輯語義、變化選擇、標記、註解或 SGF 儲存。

## KataGo-compatible time commands

ZenGTPX 為 GUI 相容性接受 `kata-time_settings` 與 `kata-set-param maxTime`。

相容行為：

- `kata-time_settings none` 表示 GUI 要求 ZenGTPX 不使用 runtime time control。收到此命令後，後續 `kata-set-param maxTime <seconds>` 只會為了 `kata-get-param` 相容性記錄參數值，不會覆蓋 engine 的 configured max time。
- 任一非 `none` 的 `kata-time_settings ...` 會重新允許 `kata-set-param maxTime <seconds>` 作為 runtime max-time override。
- 標準 `time_settings ...` 也會重新允許 runtime max-time handling。

此語意刻意比「每個 KataGo-compatible command 完全獨立處理」更嚴格，因為 LizzieYzy Next 可能先送 `kata-time_settings none`，接著仍送 generic `kata-set-param maxTime` 值。

ZenGTPX 也為 GUI 相容性接受 `kata-get-rules` 與 `kata-set-rules`。完整 KataGo rules object 會收斂成 ZenGTPX 支援的計分規則：

- `AREA` / `CHINESE` 映射為 `finalScoreRule = area`。
- `TERRITORY` / `JAPANESE` 映射為 `finalScoreRule = japanese`。
- `KOREAN` 映射為 `finalScoreRule = japanese`。

`kata-get-rules` 會依目前映射後的規則回報 `AREA` 或 `TERRITORY`。此功能只影響 ZenGTPX final-score rule selection；不是完整 KataGo rules engine。

## `zengtp_last_search_info`

回傳最近一次成功 `genmove` 記錄的搜尋摘要。

典型用法：

```text
genmove b
= D16

zengtp_last_search_info
= move D16 playouts 6000 winrate 0.5342 time 1.235
```

回應欄位：

| Field | 說明 |
| --- | --- |
| `move` | 前一次 `genmove` 的實際回應：GTP vertex、`pass` 或 `resign`。 |
| `playouts` | `ZenGetTopMoveInfo(0)` 回報的 playout count。 |
| `winrate` | `ZenGetTopMoveInfo(0)` 回報的 winrate，格式為 `0.0000` 到 `1.0000`。 |
| `time` | 前一次 `genmove` 的 ZenGTPX 思考時間，單位秒。 |

此指令不接受參數。

若目前沒有搜尋摘要，engine 會回傳：

```text
? no search info available
```

當棋盤狀態被 `boardsize`、`clear_board`、`play`、`undo` 等指令重設或手動改變時，search info 會被清除。

相容性備註：

- `genmove` 仍維持標準 GTP 回應，不包含搜尋診斷。
- GUI clients 不會收到此資訊，除非它們明確呼叫此指令。
- 支援時，`list_commands` 會列出此指令。

## `zengtp_final_score_detail`

回傳 ZenGTPX 目前 final-score 估算所使用的拆解資料。
此指令供 scripts 與診斷使用，不是完整規則判定指令。

`final_score` 仍是標準 GTP 面向的短回應，例如 `B+3.5` 或 `W+0.5`。
`zengtp_final_score_detail` 則暴露目前估算的推導方式。

範例：

```text
zengtp_final_score_detail
= rule japanese configuredEstimate W+6.5 areaEstimate W+6.5 areaMargin -6.5 territoryEstimate W+6.5 territoryMargin -6.5 captureAdjustedEstimate W+6.5 captureAdjustedMargin -6.5 threshold 300 komi 6.5 blackArea 0 whiteArea 0 blackTerritoryScore 0 whiteTerritoryScore 0 blackAlive 0 blackCapture 0 blackTerritory 0 whiteAlive 0 whiteCapture 0 whiteTerritory 0 capturedBlackPrisoners 0 capturedWhitePrisoners 0
```

回應欄位：

| Field | 說明 |
| --- | --- |
| `rule` | 目前的 `finalScoreRule` 設定值。 |
| `configuredEstimate` | 依設定規則由 `final_score` 回傳的值。 |
| `areaEstimate` | Chinese/area 估算：活棋子、territory statistics 判定的被吃棋子與 territory，扣除 komi。 |
| `areaMargin` | prisoner adjustment 前的黑減白再扣 komi。 |
| `territoryEstimate` | 使用 ZenGTP.py 相容公式的 Japanese/territory 估算。 |
| `territoryMargin` | `territoryEstimate` 的黑減白再扣 komi。 |
| `captureAdjustedEstimate` | 診斷值：`areaMargin + capturedWhitePrisoners - capturedBlackPrisoners`。此值不作為標準 `final_score` 回應。 |
| `captureAdjustedMargin` | `captureAdjustedEstimate` 背後的數值。 |
| `threshold` | 既有估算路徑使用的 territory statistics threshold。 |
| `komi` | 目前 komi。 |
| `blackArea` / `whiteArea` | 估算使用的 area counts。 |
| `blackTerritoryScore` / `whiteTerritoryScore` | Territory scoring counts：territory 加上由 territory statistics 分類的被吃棋子兩倍，再加原生 prisoners。 |
| `blackAlive` / `whiteAlive` | territory-statistics 估算判定為活棋的棋子數。 |
| `blackCapture` / `whiteCapture` | territory-statistics 估算判定為被吃的棋子數。 |
| `blackTerritory` / `whiteTerritory` | 估算判定為 territory 的空點數。 |
| `capturedBlackPrisoners` / `capturedWhitePrisoners` | Zen 原生 prisoner counters。 |

此指令不接受參數，並會在讀取估算前停止任何進行中的背景分析串流。
wrapped territory API 暴露的是 19x19 matrix，因此此指令只支援 board size 19 以下。

相容性備註：

- `areaEstimate` 與 `territoryEstimate` 仍是由 Zen territory statistics 產生的估算。
- `captureAdjustedEstimate` 只用於協助檢查 prisoner counters，不應視為正式 Japanese、Chinese 或 territory scoring 結果。
- 完整 final-score adjudicator 仍需要明確規則、死子處理、pass/end-state policy 與 scoring validation。

## `final_status_list`

ZenGTPX 接受：

- `final_status_list alive`
- `final_status_list dead`
- `final_status_list seki`

目前每個指令都會回傳成功但空的 response：

```text
final_status_list dead
=
```

這是相容性 stub，用於會在終局附近查詢棋子狀態的 GTP clients。
它不表示 ZenGTPX 具備可靠的死子或 seki 判定。

相容性備註：

- 此指令會由 `list_commands` 宣告，以避免 clients 探測時得到 `unknown command`。
- 空清單是刻意保守的行為：ZenGTPX 不會自行編造 Zen7 未透過 wrapped API 暴露的死活狀態資料。
- 完整 final-status support 仍取決於尚未完成的完整終局計分工作。

## `zengtp_policy [count]`

回傳目前棋盤上 Zen policy knowledge 的前幾個正值點。
此指令是 ZenGTPX 診斷擴充，不屬於 KataGo analysis protocol。

選用的 `count` 參數必須為正數。省略時，ZenGTPX 最多回傳 20 個點。
此指令會在讀取 policy matrix 前停止任何進行中的背景分析串流。

範例：

```text
zengtp_policy 5
= boardSize 9 count 5 max 1000 selectedSum 4301
D6 1000 0.0700
F6 906 0.0635
D4 874 0.0612
F3 779 0.0546
F4 742 0.0520
```

回應欄位：

| Field | 說明 |
| --- | --- |
| `boardSize` | 目前 board size。 |
| `count` | 回傳點數。 |
| `max` | 回傳 raw policy value 中的最高值。 |
| `selectedSum` | 回傳 top-N point set 的 raw policy values 總和。 |

後續每行格式：

```text
<vertex> <rawValue> <normalized>
```

`rawValue` 來自 `ZenGetPolicyKnowledge`。
`normalized` 是該點正 policy value 除以目前棋盤上所有正 policy values。
此值可作為相對 heatmap 值，但不保證等同 KataGo neural policy prior 語意。

wrapped Zen API 暴露的是 19x19 policy matrix，因此此指令只支援 board size 19 以下。

## `territory`

回傳目前 `ZenGetTerritoryStatictics` matrix，格式為 LizzieYzy Zen estimate mode 使用的 legacy ZenGTP 格式。

此指令提供 GUI 相容性。它不是 GTP v2、KataGo ownership analysis，也不是 ZenGTPX 的 structured diagnostic command set。

範例：

```text
territory
=
# 8 -1 -4 -7 1 -3 9 4 8
# 7 0 9 7 -1 -1 1 11 5
# -1 5 2 7 -3 -7 -19 -17 -1
...
territory
```

每個 matrix row 都以 `#` 開頭，以符合歷史 `ZenGTP.py` 輸出形狀，供 LizzieYzy Zen estimate parser 使用。
正負值都是 Zen territory statistics values。
它們不是完整 final-score adjudication，不是 KataGo ownership values，也不作為真正的 `scoreLead` 或 `scoreMean`。

此指令不接受參數，並會在讀取 territory matrix 前停止任何進行中的背景分析串流。

## `zengtp_territory`

回傳目前 `ZenGetTerritoryStatictics` matrix。
此指令是 ZenGTPX 診斷擴充，不屬於 KataGo ownership analysis。

範例：

```text
zengtp_territory
= boardSize 9
8 -1 -4 -7 1 -3 9 4 8
7 0 9 7 -1 -1 1 11 5
-1 5 2 7 -3 -7 -19 -17 -1
0 -16 -13 -19 -17 -36 -29 -28 -7
-1 -8 -26 0 -31 -45 -43 -20 -20
10 7 -16 -3 -16 -66 -28 -13 -14
20 -11 1 -8 -69 -34 -46 -11 -13
19 5 12 -19 -55 -57 -41 -24 -12
28 8 -1 -4 -39 -36 -26 -19 -3
```

回應從上到下每列一行，每列從左到右包含每個 board column 的整數。
正負值都是 Zen territory statistics values。
它們不是完整 final-score adjudication，不是 KataGo ownership values，也不作為真正的 `scoreLead` 或 `scoreMean`。

此指令不接受參數，並會在讀取 territory matrix 前停止任何進行中的背景分析串流。
wrapped Zen API 暴露的是 19x19 territory matrix，因此此指令只支援 board size 19 以下。
