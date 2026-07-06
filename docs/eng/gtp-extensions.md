# ZenGTPX GTP Extensions

This document describes ZenGTPX-specific GTP commands for scripts and diagnostics.
These commands are not part of GTP v2 and should not be treated as Lizzie or KataGo analysis protocols.

## Lizzie / KataGo-style GTP analysis commands

ZenGTPX supports first-pass GTP analysis commands for GUI candidate display:

- `lz-analyze [interval]`
- `kata-analyze [color] [interval]`
- `analyze [color] [interval]`
- `lz-genmove_analyze [color] [interval]`
- `kata-genmove_analyze [color] [interval]`
- `genmove_analyze [color] [interval]`
- `stop`

With the normal executable entrypoint, analysis commands acknowledge the GTP command with `=`, then continue emitting background `info move ...` analysis lines until `stop` or a board-changing command is received.
The optional `interval` argument follows KataGo/Leela-style centiseconds. For example, `kata-analyze B 10` asks ZenGTPX to sample and emit analysis roughly every 100 ms while the underlying Zen search continues.
Candidate coordinates, playouts, winrate, and PV text are read from `ZenGetTopMoveInfo(index)`.
`prior` is derived from `ZenGetPolicyKnowledge` by normalizing positive policy values across the returned candidate set.
`analyze` is accepted as a KataGo-style alias for GUI compatibility.

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
- Analysis output is sampled from a continuous Zen thinking session. Exact output cadence may be slower than the requested interval if Zen native calls or GUI I/O take longer.
- `prior` is Zen policy knowledge-derived and normalized across returned candidate moves. It should not be treated as a KataGo-equivalent neural policy prior.
- `scoreLead` and `scoreMean` are compatibility placeholders derived from winrate because Zen7 does not currently expose reliable equivalent values through the wrapped API.
- For LizzieYzy Next multi-candidate display, use `gtpName = KataGo` in `zen7.cfg`; this only changes the GTP `name` response.
- ZenGTPX does not implement KataGo `analysis` JSON protocol.

`lz-genmove_analyze`, `kata-genmove_analyze`, and `genmove_analyze` are compatibility commands for GUI engine-game mode.
They search a move like `genmove`, emit one analysis line, then finish the same GTP response with:

```text
play <vertex>
```

The chosen move is applied to the internal board state, matching `genmove` behavior.
The optional `interval` argument is accepted for GUI compatibility, but ZenGTPX currently emits a single final analysis snapshot during `genmove_analyze` rather than a long-running stream throughout the move search.
`genmove_analyze` is accepted as a KataGo-style alias for GUIs that do not use the `kata-` prefix.

## KataGo-compatible time commands

ZenGTPX accepts `kata-time_settings` and `kata-set-param maxTime` for GUI compatibility.

Compatibility behavior:

- `kata-time_settings none` means the GUI requested no runtime time control for ZenGTPX. After this command, `kata-set-param maxTime <seconds>` is recorded for `kata-get-param` compatibility but does not override the engine's configured max time.
- Any non-`none` `kata-time_settings ...` re-enables `kata-set-param maxTime <seconds>` as a runtime max-time override.
- Standard `time_settings ...` also re-enables runtime max-time handling.

This is intentionally stricter than treating every KataGo-compatible command independently, because LizzieYzy Next may send `kata-time_settings none` and then still send its generic `kata-set-param maxTime` value.

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

## `zengtp_final_score_detail`

Returns the breakdown used by ZenGTPX's current final-score estimate.
This command is for scripts and diagnostics. It is not a full ruleset adjudication command.

`final_score` remains the standard GTP-facing short response, for example `B+3.5` or `W+0.5`.
`zengtp_final_score_detail` exposes how the current estimate was derived.

Example:

```text
zengtp_final_score_detail
= rule japanese configuredEstimate W+6.5 areaEstimate W+6.5 areaMargin -6.5 territoryEstimate W+6.5 territoryMargin -6.5 captureAdjustedEstimate W+6.5 captureAdjustedMargin -6.5 threshold 300 komi 6.5 blackArea 0 whiteArea 0 blackTerritoryScore 0 whiteTerritoryScore 0 blackAlive 0 blackCapture 0 blackTerritory 0 whiteAlive 0 whiteCapture 0 whiteTerritory 0 capturedBlackPrisoners 0 capturedWhitePrisoners 0
```

Response fields:

| Field | Description |
| --- | --- |
| `rule` | Current `finalScoreRule` config value. |
| `configuredEstimate` | The value returned by `final_score` for the configured rule. |
| `areaEstimate` | Chinese/area estimate: alive stones plus captured stones classified by territory statistics plus territory, minus komi. |
| `areaMargin` | Black minus white minus komi, before prisoner adjustment. |
| `territoryEstimate` | Japanese/territory estimate using the ZenGTP.py-compatible formula. |
| `territoryMargin` | Black minus white minus komi for `territoryEstimate`. |
| `captureAdjustedEstimate` | Diagnostic value: `areaMargin + capturedWhitePrisoners - capturedBlackPrisoners`. This is not used as the standard `final_score` response. |
| `captureAdjustedMargin` | Numeric value behind `captureAdjustedEstimate`. |
| `threshold` | Territory statistics threshold used by the existing estimate path. |
| `komi` | Current komi. |
| `blackArea` / `whiteArea` | Area counts used by the estimate. |
| `blackTerritoryScore` / `whiteTerritoryScore` | Territory scoring counts: territory plus twice captured stones classified by territory statistics plus native prisoners. |
| `blackAlive` / `whiteAlive` | Stones counted alive by the territory-statistics estimate. |
| `blackCapture` / `whiteCapture` | Stones classified as captured by the territory-statistics estimate. |
| `blackTerritory` / `whiteTerritory` | Empty points classified as territory by the estimate. |
| `capturedBlackPrisoners` / `capturedWhitePrisoners` | Native prisoner counters from Zen. |

The command takes no arguments and stops any active background analysis stream before reading the estimate.
The wrapped territory API exposes a 19x19 matrix, so this command is supported only up to board size 19.

Compatibility notes:

- `areaEstimate` and `territoryEstimate` are still estimates from Zen territory statistics.
- `captureAdjustedEstimate` is exposed only to help inspect prisoner counters and should not be treated as an official Japanese, Chinese, or territory scoring result.
- A complete final-score adjudicator would still need explicit rules, dead-stone handling, pass/end-state policy, and scoring validation.

## `final_status_list`

ZenGTPX accepts:

- `final_status_list alive`
- `final_status_list dead`
- `final_status_list seki`

Each currently returns an empty successful response:

```text
final_status_list dead
=
```

This is a compatibility stub for GTP clients that query final stone status near the end of a game.
It does not mean ZenGTPX has reliable dead-stone or seki adjudication.

Compatibility notes:

- The command is advertised by `list_commands` to avoid `unknown command` behavior in clients that probe it.
- The empty list is intentionally conservative: ZenGTPX does not invent dead/alive status data that Zen7 does not expose through the wrapped API.
- Full final-status support remains tied to the unresolved complete final scoring work.

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

## `territory`

Returns the current `ZenGetTerritoryStatictics` matrix in the legacy ZenGTP format used by LizzieYzy's Zen estimate mode.

This command is provided for GUI compatibility. It is not part of GTP v2, KataGo ownership analysis, or ZenGTPX's structured diagnostic command set.

Example:

```text
territory
=
# 8 -1 -4 -7 1 -3 9 4 8
# 7 0 9 7 -1 -1 1 11 5
# -1 5 2 7 -3 -7 -19 -17 -1
...
territory
```

Each matrix row starts with `#`, matching the historical `ZenGTP.py` output shape expected by LizzieYzy's Zen estimate parser.
Positive and negative values are Zen territory statistics values.
They are not full final-score adjudication, not KataGo ownership values, and are not used as true `scoreLead` or `scoreMean`.

The command takes no arguments and stops any active background analysis stream before reading the territory matrix.

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
