# 疑難排解

## 找不到 Zen.dll

確認 `Zen.dll` 與 `ZenGTPX.exe` 在同一層：

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

或在 `zen7.cfg` 中設定正確路徑：

```cfg
zenDll = Zen.dll
```

## Engine 無法啟動

確認：

- 使用 Windows。
- 使用 x86 相容的 `Zen.dll`。
- `zen7.cfg` 與 `Zen.dll` 路徑正確。
- GUI 的 working directory 設為 `ZenGTPX.exe` 所在目錄。

## LizzieYzy Next 無法使用 Engine

確認 ZenGTPX 是作為一般 GTP engine 加入。

ZenGTPX 支援第一版 `lz-analyze` / `kata-analyze` GTP analysis commands，但不支援 KataGo `analysis` JSON protocol。不要把 ZenGTPX 設成 KataGo analysis JSON engine。

後續版本會補更多錯誤案例。
