# LizzieYzy Next 設定

ZenGTPX 應作為一般 GTP engine 加入 LizzieYzy Next。

## 檔案

先準備同一層目錄：

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

## Engine 設定

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

## 備註

- ZenGTPX 仍應優先作為一般 GTP engine 加入。
- 已支援第一版 background streaming `lz-analyze` / `kata-analyze` / `analyze` GTP analysis commands，用於候選點與勝率顯示。
- 已支援 `lz-genmove_analyze` / `kata-genmove_analyze` / `genmove_analyze`，供 LizzieYzy Next 引擎對局模式取得落子。
- 若要顯示多候選點，`zen7.cfg` 建議保持 `gtpName = KataGo`。這只影響 GTP `name` 回應，用於 LizzieYzy Next 的 KataGo-compatible 顯示路徑。
- 不要將 ZenGTPX 設為 KataGo analysis JSON engine；ZenGTPX 不支援 `katago analysis` JSON protocol。
- `final_score` 依 `finalScoreRule` 使用 ZenGTP.py 相容公式計算；預設 `japanese`，可改 `chinese` / `area`。結果仍依賴 Zen territory statistics 的死子判斷。
- `Zen.dll` 必須由使用者自行合法提供。
