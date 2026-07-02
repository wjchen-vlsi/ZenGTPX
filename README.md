# ZenGTPX

ZenGTPX 是 Windows x86 GTP engine wrapper，用於讓支援 GTP 的圍棋 GUI 載入 Zen7 `Zen.dll`。

`Zen.dll` 具版權限制，本 repository 與 GitHub Release 不提供。使用者需自行取得合法的 `Zen.dll`。

## Installation

下載 Release 後，將 `Zen.dll` 放在 `ZenGTPX.exe` 同一層：

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

## Quick Test

在 `ZenGTPX.exe` 所在目錄執行：

```powershell
.\ZenGTPX.exe
```

若同目錄存在 `zen7.cfg`，ZenGTPX 會自動讀取它。也可明確指定設定檔：

```powershell
.\ZenGTPX.exe --config .\zen7.cfg
```

## Configuration

設定檔範本位於 `config\`：

```text
config\zen7.cfg
config\zen7_zh-TW.cfg
```

可依需要複製其中一份為執行目錄的 `zen7.cfg`。

參數說明見 [docs/configuration.md](docs/configuration.md)。

## LizzieYzy Next

將 `ZenGTPX.exe` 加入 LizzieYzy Next 作為一般 GTP engine。設定重點：

- Engine path：`ZenGTPX.exe`
- Working directory：`ZenGTPX.exe` 所在目錄
- Arguments：通常可留空；若需要可指定 `--config zen7.cfg`

詳細設定見 [docs/lizzieyzy-next.md](docs/lizzieyzy-next.md)。

## Troubleshooting

常見問題見 [docs/troubleshooting.md](docs/troubleshooting.md)。

## Build From Source

一般使用者建議直接使用 GitHub Release。若需自行建置：

```powershell
.\publish.ps1
```

本機建置輸出位於 `dist\`，此目錄不應提交到 git。
