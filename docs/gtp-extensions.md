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

## `zengtp_policy [count]`

Returns the top positive Zen policy knowledge points for the current board.
This command is a ZenGTPX diagnostic extension and is not part of KataGo analysis protocol.

The optional `count` argument must be positive. If omitted, ZenGTPX returns up to 20 points.
The command stops any active background analysis stream before reading the policy matrix.

Example:

```text
zengtp_policy 5
= boardSize 9 count 5 max 1000 selectedSum 4301
D6 1000 0.0700
F6 906 0.0635
D4 874 0.0612
F3 779 0.0546
F4 742 0.0520
```

Response fields:

| Field | Description |
| --- | --- |
| `boardSize` | Current board size. |
| `count` | Number of returned points. |
| `max` | Highest returned raw policy value. |
| `selectedSum` | Sum of raw policy values in the returned top-N point set. |

Each following line is:

```text
<vertex> <rawValue> <normalized>
```

`rawValue` comes from `ZenGetPolicyKnowledge`.
`normalized` is the point's positive policy value divided by all positive policy values on the current board.
It is useful as a relative heatmap value, but it is not guaranteed to match KataGo neural policy prior semantics.

The wrapped Zen API exposes a 19x19 policy matrix, so this command is supported only up to board size 19.

## `zengtp_territory`

Returns the current `ZenGetTerritoryStatictics` matrix.
This command is a ZenGTPX diagnostic extension and is not part of KataGo ownership analysis.

Example:

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

The response contains one row per board row, from top to bottom, and each row contains one integer per board column, from left to right.
Positive and negative values are Zen territory statistics values.
They are not full final-score adjudication, not KataGo ownership values, and are not used as true `scoreLead` or `scoreMean`.

The command takes no arguments and stops any active background analysis stream before reading the territory matrix.
The wrapped Zen API exposes a 19x19 territory matrix, so this command is supported only up to board size 19.
