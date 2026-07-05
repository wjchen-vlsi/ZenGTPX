# LizzieYzy Next Setup

Add ZenGTPX to LizzieYzy Next as a normal GTP engine.

## Files

Prepare the files in the same directory:

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

## Engine Settings

Recommended settings:

```text
Name: ZenGTPX
Engine path: <full path to ZenGTPX.exe>
Working directory: <directory containing ZenGTPX.exe>
Arguments:
```

If the GUI requires explicit arguments, use:

```text
--config zen7.cfg
```

## Notes

- ZenGTPX should still be added as a normal GTP engine first.
- ZenGTPX supports first-pass background streaming `lz-analyze` / `kata-analyze` / `analyze` GTP analysis commands for candidate and winrate display.
- ZenGTPX supports `lz-genmove_analyze` / `kata-genmove_analyze` / `genmove_analyze` for LizzieYzy Next engine-game mode.
- To display multiple candidates, keep `gtpName = KataGo` in `zen7.cfg`. This only affects the GTP `name` response and is used by LizzieYzy Next's KataGo-compatible display path.
- Do not configure ZenGTPX as a KataGo analysis JSON engine; ZenGTPX does not support the `katago analysis` JSON protocol.
- `final_score` uses a ZenGTP.py-compatible formula based on `finalScoreRule`; the default is `japanese`, and it can be changed to `chinese` / `area`. The result still depends on Zen territory statistics for dead-stone classification.
- `Zen.dll` must be legally provided by the user.
