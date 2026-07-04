# ZenGTPX GTP Extensions

This document describes ZenGTPX-specific GTP commands for scripts and diagnostics.
These commands are not part of GTP v2 and should not be treated as Lizzie or KataGo analysis protocols.

## Lizzie / KataGo-style GTP analysis commands

ZenGTPX supports first-pass GTP analysis commands for GUI candidate display:

- `lz-analyze [visits]`
- `kata-analyze <color> [visits]`
- `stop`

With the normal executable entrypoint, both commands acknowledge the GTP command with `=`, then continue emitting background `info move ...` analysis lines until `stop` or a board-changing command is received.
Candidate coordinates, playouts, winrate, and PV text are read from `ZenGetTopMoveInfo(index)`.
`prior` is derived from `ZenGetPolicyKnowledge` by normalizing positive policy values across the returned candidate set.

`kata-analyze` emits one line containing multiple KataGo-style `info move` entries:

```text
info move Q16 visits 1700 winrate 0.5342 scoreLead 1.0 scoreMean 1.0 prior 0.625 order 0 pv Q16 D4 info move D4 visits 850 winrate 0.4980 scoreLead -0.1 scoreMean -0.1 prior 0.375 order 1 pv D4 Q16
=
```

`lz-analyze` emits Leela-style `winrate` values in 0..10000 format:

```text
info move Q16 visits 1700 winrate 5342 pv Q16 D4
=
```

Current limitations:

- `stop` requests cancellation of the background stream and interrupts the active Zen analysis loop. Native cleanup still depends on `ZenStopThinking` returning.
- Candidate coordinates, playouts, winrate, and PV text come from `ZenGetTopMoveInfo(index)`.
- `prior` is Zen policy knowledge-derived and normalized across returned candidate moves. It should not be treated as a KataGo-equivalent neural policy prior.
- `scoreLead` and `scoreMean` are compatibility placeholders derived from winrate because Zen7 does not currently expose reliable equivalent values through the wrapped API.
- For LizzieYzy Next multi-candidate display, use `gtpName = KataGo` in `zen7.cfg`; this only changes the GTP `name` response.
- ZenGTPX does not implement KataGo `analysis` JSON protocol.

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
