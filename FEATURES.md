# 功能說明

ZenGTPX 是 Zen7 `Zen.dll` 的 Windows x86 GTP Engine Wrapper，主要用於在 LizzieYzy Next 中以一般 GTP Engine 方式載入 Zen7。

## 已支援

- 將 Zen7 `Zen.dll` 包裝為標準 GTP Engine。
- 以 Windows x86 self-contained executable 發行。
- 支援在 LizzieYzy Next 中作為一般 GTP Engine 使用。
- 透過 GTP analysis commands 顯示候選點與勝率。
- 支援人機對局。
- 支援 ZenGTPX 與其他 GTP Engine 自動對局。
- 支援透過 LizzieYzy Next 的 ReadBoard / Fox 同步機制進行網路棋盤同步對局。
- 支援 `rank`、`fixed-time` 與 `advanced` 棋力設定模式。
- 支援 Configuration Protocol v1，讓 GUI 自動發現、查詢並在不中斷對局狀態下套用每個 engine instance 的配置 profile。
- 提供英文與繁體中文設定檔範本。
- 提供搜尋、policy、territory 與 final-score 檢查用的診斷 GTP commands。
- 提供 LizzieYzy Next 所需的相容命令，包含 `kata-analyze`、`lz-analyze`、`kata-genmove_analyze`、`loadsgf`，以及部分 KataGo-style parameter/rules commands。

## 不包含

- Zen7 或 `Zen.dll`。
- KataGo `analysis` JSON protocol。
- 跨平台版本。
- 完整 SGF editor 語意。
- 完整且可靠的 dead-stone / seki 判定。
- Zen7 授權或資源的替代品。

## 發行狀態

`v0.96` 已完成 Configuration Protocol v1 的引擎端實作與本機驗證，可交由 LizzieYzy Next 進行整合驗收；後續將以小版本持續補強，穩定後再推進至 `v1.x`。
