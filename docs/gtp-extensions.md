# ZenGTPX GTP Extensions

This document describes ZenGTPX-specific GTP commands for scripts and diagnostics.
These commands are not part of GTP v2 and should not be treated as Lizzie or KataGo analysis protocols.

## `zengtp_last_search_info`

Returns the search summary recorded by the most recent successful `genmove`.

Typical use:

```text
genmove b
= D16

zengtp_last_search_info
= move D16 playouts 6000 winrate 0.5342 time 1.235
```

Response fields:

| Field | Description |
| --- | --- |
| `move` | The actual previous `genmove` response: GTP vertex, `pass`, or `resign`. |
| `playouts` | The playout count reported by `ZenGetTopMoveInfo(0)`. |
| `winrate` | The winrate reported by `ZenGetTopMoveInfo(0)`, formatted as `0.0000` to `1.0000`. |
| `time` | Elapsed ZenGTPX thinking time for the previous `genmove`, in seconds. |

The command takes no arguments.

If no current search summary is available, the engine returns:

```text
? no search info available
```

Search info is cleared when board state is reset or manually changed by commands such as `boardsize`, `clear_board`, `play`, and `undo`.

Compatibility notes:

- `genmove` remains a standard GTP response and does not include search diagnostics.
- GUI clients will not receive this information unless they explicitly call this command.
- `list_commands` advertises this command when it is supported.
