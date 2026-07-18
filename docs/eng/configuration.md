# Configuration

ZenGTPX uses `zen7.cfg` as the primary configuration file.

GUI clients can override the ten play-strength and scoring profile fields for the current process through [Configuration Protocol v1](./configuration-protocol.md). Protocol save operations are client-owned and never rewrite this file.

## File Location

Place the configuration file next to `ZenGTPX.exe`:

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

When `--config` is not specified, ZenGTPX tries to load `zen7.cfg` from the executable directory.

## Templates

Templates are located in:

```text
config\zen7.cfg
config\zen7_zh-TW.cfg
```

`zen7.cfg` is the default deployment template. `zen7_zh-TW.cfg` contains the same keys with more detailed Traditional Chinese comments, recommended values, and ranges.

## Format

```cfg
key = value
```

Lines beginning with `#` are comments. Inline comments after values are also allowed.

## Strength Modes

ZenGTPX supports three strength modes:

| Mode | Purpose | Main settings |
| --- | --- | --- |
| `rank` | Default Zen7 GUI-compatible rank mode. | `rankPreset` |
| `fixed-time` | Zen7 GUI-compatible specified thinking time mode. | `maxTimeSeconds` |
| `advanced` | Direct low-level tuning. | `maxTimeSeconds`, `maxSimulations`, `pnLevel`, `pnWeight`, `vnMixRate` |

Default:

```cfg
mode = rank
rankPreset = 9d
```

`mode = rank` uses the built-in Zen7 GUI rank table. Allowed `rankPreset` values:

```text
6k, 5k, 4k, 3k, 2k, 1k, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d
```

`mode = fixed-time` uses the Zen7 GUI specified-time baseline:

```cfg
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

Set `maxTimeSeconds` to control seconds per move.

Use `mode = advanced` only when you want to tune low-level parameters directly.

## Common Settings

```cfg
zenDll = Zen.dll
gtpName = KataGo
tracePath = gtp_logs/zengtpx-trace-{timestamp}-{pid}.log
boardSize = 19
komi = 0.5
handicap = 0
finalScoreRule = chinese
threads = 8
resignThreshold = 0.1
```

Recommended ranges:

| Key | Range | Recommended |
| --- | --- | --- |
| `gtpName` | non-empty string | `KataGo` for LizzieYzy Next multi-candidate display; `ZenGTPX` for generic GTP scripts |
| `tracePath` | path or empty | Keep the development default to capture GUI GTP traces; set empty to disable |
| `boardSize` | `1` to `25` | `19` |
| `komi` | any number | `0.5` for Chinese-rule online games; `6.5` or `7.5` when your GUI/ruleset requires standard komi |
| `handicap` | `0` or greater | `0` |
| `finalScoreRule` | `japanese`, `territory`, `chinese`, or `area` | `chinese` for the packaged template; `japanese` for Zen7-like territory scoring |
| `threads` | positive integer | `1-4` conservative, `8` on 8-core CPUs, `10-12` for stronger play |
| `resignThreshold` | `0.0` to `1.0` | `0.03-0.10` |

## Advanced Keys

These are used directly only with `mode = advanced`; `mode = rank` and `mode = fixed-time` derive them from GUI-compatible presets.

| Key | Range | Reference values |
| --- | --- | --- |
| `maxTimeSeconds` | `0` or greater | `1-5` fast play, `30-60` stronger play, `300+` analysis |
| `maxSimulations` | positive integer | `100` smoke, `2700` GUI 5d, `6000` GUI 9d, `1000000` fixed-time |
| `pnLevel` | `0` to `3` recommended | `3` for strong settings |
| `pnWeight` | `1.0` to `4.4` in GUI rank table; `1.0` in fixed-time | depends on mode |
| `vnMixRate` | `0.30` to `0.75` in GUI rank table; `0.75` in fixed-time | depends on mode |

## GTP Behavior Notes

- `gtpName` controls the GTP `name` response only. The default deployment config uses `KataGo` because LizzieYzy Next enables multi-candidate analysis display on its KataGo-compatible path.
- `gtpName = KataGo` does not mean ZenGTPX supports KataGo `analysis` JSON protocol.
- `tracePath` writes a raw GTP trace without touching stdout. Relative paths are resolved from the executable directory. Supported tokens: `{timestamp}`, `{date}`, `{pid}`. `ZENGTPX_TRACE_PATH` still overrides this setting when present.
- `time_settings` / `time_left` are forwarded to Zen native time API where supported; `time_settings` also updates the wrapper's per-move deadline.
- `final_score` uses ZenGTP.py-compatible scoring over Zen territory statistics. `finalScoreRule = japanese` / `territory` uses territory scoring; `chinese` / `area` uses area scoring.
- `final_status_list alive|dead|seki` is a compatibility stub that returns an empty list; ZenGTPX does not currently provide reliable dead-stone adjudication.
- `zengtp_final_score_detail` returns the estimate breakdown for scripts and diagnostics, including configured, area, territory, and capture-adjusted values.
- Config `handicap` is a passive game parameter. It does not place stones or change board state by itself.
- Actual handicap placement remains controlled by GTP `fixed_handicap`, `set_free_handicap`, or `place_free_handicap`.
