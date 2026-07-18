# Configuration Protocol v1

ZenGTPX 0.96 新增 namespaced GTP 配置擴充，讓 GUI 可自動發現設定、建立通用編輯介面、原子套用 profile，並查詢 Zen7 真正使用的有效值。

此協議是可選功能。標準 GTP 對局、分析、ReadBoard 同步及既有 KataGo 相容命令都不依賴它。

## 能力偵測

請使用 `list_commands` 或 `known_command`，不要依賴 `name`。發行設定為了 LizzieYzy Next 多候選顯示，預設 `name` 會回 `KataGo`。

```text
known_command zengtp_config_schema
= true
```

Protocol v1 命令：

```text
zengtp_config_version
zengtp_config_schema
zengtp_config_get
zengtp_config_set
zengtp_config_save
zengtp_config_reset
```

所有 command body 都是單行 JSON。成功使用標準 GTP `=`；協議錯誤使用 `?`，body 為可由程式解析的 JSON error object。

## 版本與持久化責任

```text
zengtp_config_version
= {"protocol":"zengtp-config","version":1,"persistenceOwner":"client","commands":[...]}
```

`persistenceOwner = client` 表示協議命令不會寫入共享 `zen7.cfg`。LizzieYzy Next 或其他 client 應為每個 engine instance 分別保存回傳的 profile，並在新 process 啟動後重新套用。

## Schema

`zengtp_config_schema` 回傳：

- 協議名稱與版本；
- `batchSemantics = atomic`；
- 欄位型別、enum／範圍、預設值、basic／advanced 分組、有效模式、套用階段及 restart requirement；
- 與 `zengtp_config_get` 相同的目前狀態。

Protocol v1 欄位：

| 欄位 | 型別／範圍 | 分組 | 有效模式 |
| --- | --- | --- | --- |
| `mode` | `rank`、`fixed-time`、`advanced` | basic | 全部 |
| `rankPreset` | `6k` 到 `9d` | basic | rank |
| `maxTimeSeconds` | number，`>= 0` | basic | fixed-time、advanced |
| `maxSimulations` | integer，`>= 1` | advanced | advanced |
| `pnLevel` | integer，`0..3` | advanced | advanced |
| `pnWeight` | number，`>= 0` | advanced | advanced |
| `vnMixRate` | number，`0..1` | advanced | advanced |
| `threads` | integer，`>= 1` | advanced | 全部；rank 的 effective value 最多為 4 |
| `finalScoreRule` | `japanese`、`territory`、`chinese`、`area` | advanced | 全部 |
| `resignThreshold` | number，`0..1` | advanced | 全部 |

所有 v1 欄位都是 `requiresRestart = false`、`apply = next-search`。ZenGTPX 會先停止當前 analysis，再套用整批設定；不會清空棋盤、重播手順、修改貼目或讓子狀態。

## Selected 與 effective values

```text
zengtp_config_get
= {"protocol":"zengtp-config","version":1,"operation":"get","state":{"selected":{...},"effective":{...},"dirty":false,"restartRequired":false,"persistenceOwner":"client"}}
```

- `selected`：使用者選擇的 profile；暫時不生效的 advanced values 仍會保留，供之後切換模式。
- `effective`：經 mode 推導與暫時性 GTP override 後，引擎目前真正使用的設定。
- `dirty`：`selected` 是否不同於目前 process 的 saved snapshot。
- `restartRequired`：v1 欄位固定為 false。

例如 rank 模式選擇 `threads = 12` 時，`selected.threads` 保留 12；為符合 Zen7 GUI rank path，`effective.threads` 為 4。

## 原子批次設定

將一個 JSON object 傳給 `zengtp_config_set`：

```text
zengtp_config_set {"mode":"fixed-time","maxTimeSeconds":5,"threads":8}
= {"protocol":"zengtp-config","version":1,"operation":"set","state":{...}}
```

ZenGTPX 會先驗證完整 object，再呼叫任何 native setter。若驗證或套用失敗，selected profile 不變，並會嘗試還原先前的 effective native settings。

可傳入目前模式未使用的欄位；它們會保留在 `selected`，由 mode 決定真正 effective values：

- `rank`：由 `rankPreset` 推導時間、計算量、PN/VN；effective threads 最多為 4。
- `fixed-time`：保留選擇的時間與 threads，搜尋參數使用全力基準。
- `advanced`：直接使用選擇的低階參數。

## Save 與 reset

```text
zengtp_config_save
= {"protocol":"zengtp-config","version":1,"operation":"save","persistenceOwner":"client","profile":{...},"state":{...}}
```

`save` 建立目前 process 的 save point、清除 `dirty`，並回傳完整 profile 供 client 持久化；不執行檔案寫入。

Reset target：

```text
zengtp_config_reset saved
zengtp_config_reset startup
zengtp_config_reset defaults
```

- `saved`（預設）：還原 process-local save point。
- `startup`：還原此 process 啟動時由 config 與 CLI 算出的 effective profile。
- `defaults`：還原 ZenGTPX 內建預設 profile；DLL path、GTP name 等非 profile 啟動設定仍保留。

## 優先序與多實例隔離

Profile 欄位的有效優先序：

```text
暫時性對局 GTP command
> Configuration Protocol process profile
> CLI
> zen7.cfg / JSON config
> built-in defaults
```

`time_settings`、`kata-set-param maxTime`、`kata-set-rules` 等暫時性 command 可改變目前 effective session value，但不修改 selected／saved profile。

每個 ZenGTPX process 都有獨立配置服務與 save point。兩個 engine process 可使用不同 profile，沒有共享可變狀態；client 必須分別保存兩份回傳 profile。

## 錯誤

未知參數範例：

```text
zengtp_config_set {"unknown":1}
? {"protocol":"zengtp-config","version":1,"error":{"code":"unknown_parameter","parameter":"unknown","message":"unknown configuration parameter"}}
```

v1 error code：

- `invalid_arguments`
- `invalid_json`
- `invalid_type`
- `invalid_value`
- `unknown_parameter`
- `duplicate_parameter`
- `apply_failed`

Client 必須判斷 `error.code`，不得解析 human-readable `message`。
