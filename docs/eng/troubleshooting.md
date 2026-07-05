# Troubleshooting

## Zen.dll Was Not Found

Make sure `Zen.dll` is in the same directory as `ZenGTPX.exe`:

```text
ZenGTPX.exe
Zen.dll
zen7.cfg
```

Or set the correct path in `zen7.cfg`:

```cfg
zenDll = Zen.dll
```

## Engine Does Not Start

Check:

- Windows is being used.
- `Zen.dll` is x86-compatible.
- `zen7.cfg` and `Zen.dll` paths are correct.
- The GUI working directory is set to the directory containing `ZenGTPX.exe`.

## LizzieYzy Next Cannot Use The Engine

Make sure ZenGTPX is added as a normal GTP engine.

ZenGTPX supports first-pass `lz-analyze` / `kata-analyze` GTP analysis commands, but it does not support KataGo `analysis` JSON protocol. Do not configure ZenGTPX as a KataGo analysis JSON engine.

More error cases will be added in later versions.

