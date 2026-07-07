# Zen7 Strength Presets

This document summarizes the ZenGTPX settings that directly affect playing strength when using Zen7 / Tencho no Igo 7.
ZenGTPX defaults to a model close to the native Zen7 GUI: specify a rank or specify thinking time; use advanced mode only when low-level tuning is needed.

## Summary

Default mode:

```cfg
mode = rank
rankPreset = 9d
threads = 4
```

This applies the `9d` entry from the Zen7 GUI rank table:

```cfg
maxTimeSeconds = 60.0
maxSimulations = 6000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

To match the Zen7 GUI "specified thinking time" path, use:

```cfg
mode = fixed-time
maxTimeSeconds = 5.0
threads = 8
```

This mode applies:

```cfg
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

To tune low-level parameters directly, use:

```cfg
mode = advanced
maxTimeSeconds = 60.0
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

## Strength Modes

| Mode | Purpose | Main settings |
| --- | --- | --- |
| `rank` | Matches the Zen7 GUI "specified rank" path. | `rankPreset` |
| `fixed-time` | Matches the Zen7 GUI "specified thinking time" path. | `maxTimeSeconds` |
| `advanced` | Sets low-level search parameters directly. | `maxTimeSeconds`, `maxSimulations`, `pnLevel`, `pnWeight`, `vnMixRate` |

`rank` is the default mode. It uses the built-in Zen7 GUI strength table and fixes `maxTimeSeconds = 60.0`.

`fixed-time` is not a rank preset. It uses a near full-strength baseline and lets `maxTimeSeconds` control time per move.

`advanced` is intended for tests, match experiments, long thinking, or strength tuning. The user is responsible for the parameter combination.

## Zen7 GUI Rank Preset Table

The table below comes from verification of native Zen7 GUI calls into `Zen.dll`.

| Preset | `maxSimulations` | `pnLevel` | `pnWeight` | `vnMixRate` | `maxTimeSeconds` |
| --- | ---: | ---: | ---: | ---: | ---: |
| 6k | 1000 | 0 | 1.6 | 0.30 | 60 |
| 5k | 1100 | 0 | 1.4 | 0.30 | 60 |
| 4k | 1200 | 0 | 1.0 | 0.30 | 60 |
| 3k | 1300 | 1 | 2.4 | 0.30 | 60 |
| 2k | 1400 | 1 | 2.0 | 0.30 | 60 |
| 1k | 1600 | 1 | 1.6 | 0.30 | 60 |
| 1d | 1800 | 1 | 1.3 | 0.35 | 60 |
| 2d | 2000 | 1 | 1.0 | 0.40 | 60 |
| 3d | 2200 | 2 | 2.0 | 0.45 | 60 |
| 4d | 2400 | 2 | 1.5 | 0.50 | 60 |
| 5d | 2700 | 2 | 1.0 | 0.55 | 60 |
| 6d | 3000 | 3 | 4.4 | 0.60 | 60 |
| 7d | 3500 | 3 | 2.8 | 0.65 | 60 |
| 8d | 4000 | 3 | 1.4 | 0.70 | 60 |
| 9d | 6000 | 3 | 1.0 | 0.75 | 60 |

`9d` is the highest rank in the GUI table, but it is not Zen7's maximum search setting.

## Fixed-Time Settings

The Zen7 GUI "specified thinking time" path uses:

| `maxSimulations` | `pnLevel` | `pnWeight` | `vnMixRate` | `maxTimeSeconds` |
| ---: | ---: | ---: | ---: | ---: |
| 1000000 | 3 | 1.0 | 0.75 | User-specified |

ZenGTPX configuration:

```cfg
mode = fixed-time
maxTimeSeconds = 5.0
```

Recommendations:

| Use case | `maxTimeSeconds` |
| --- | ---: |
| Fast GUI play | 1-5 |
| Stronger practical play | 30-60 |
| Long thinking / analysis | 300+ |

## Thread Recommendations

`threads` is part of the strength configuration. It controls how many CPU threads Zen native search uses.

| Scenario | Recommended `threads` |
| --- | ---: |
| Conservative, stable, lower CPU usage | 1-2 |
| Close to the old ZenGTP.py default | 4 |
| Normal play / 8-core CPU baseline | 8 |
| Higher strength while keeping the system responsive | 10-12 |
| Long analysis when the machine is mainly dedicated to ZenGTPX | 12-14 |

Note: to stay close to the native Zen7 GUI rank path, `mode = rank` limits threads to at most 4. To use 8 or more threads, use `mode = fixed-time` or `mode = advanced`.

Do not start by setting all logical processors. Zen7 MCTS search does not necessarily scale linearly with threads; overly high values can introduce synchronization cost, reduce search efficiency, increase CPU contention, and trigger thermal throttling.

## AMD Ryzen 7 3700X Recommendation

Ryzen 7 3700X has 8 cores / 16 threads. For `fixed-time` or `advanced` mode, a practical starting point is:

```cfg
threads = 8
```

To test higher strength, try in order:

```cfg
threads = 10
threads = 12
```

For long analysis with no other major workload, short tests can try:

```cfg
threads = 14
```

Not recommended as the daily default:

```cfg
threads = 16
```

`16` is the SMT logical thread count, not the physical core count. Start testing around the physical core count first.

## Recommended Configurations

### GUI 9d

```cfg
mode = rank
rankPreset = 9d
threads = 4
resignThreshold = 0.03
```

### GUI Specified Thinking Time

```cfg
mode = fixed-time
maxTimeSeconds = 5.0
threads = 8
resignThreshold = 0.03
```

### Advanced Strong Play

```cfg
mode = advanced
threads = 8
maxTimeSeconds = 60.0
maxSimulations = 1000000
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
resignThreshold = 0.03
```

### Fast Test

```cfg
mode = advanced
threads = 1
maxTimeSeconds = 1.0
maxSimulations = 100
pnLevel = 3
pnWeight = 1.0
vnMixRate = 0.75
```

This is only suitable for testing startup, GTP protocol handling, and wrapper flow. It does not represent playing strength.

## Compared With KataGo

Zen7 / Tencho no Igo 7 belongs to the DeepZenGo generation. It can still be useful as an older strong engine or human-play reference, but it should not be expected to match modern KataGo under the same hardware and time limits.

For modern review, winrate, score lead, handicap, and highly stable analysis, KataGo should remain the primary engine.
