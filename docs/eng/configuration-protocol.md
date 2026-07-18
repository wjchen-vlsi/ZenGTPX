# Configuration Protocol v1

ZenGTPX 0.96 adds a namespaced GTP extension for engine configuration. It lets a GUI discover settings, build a generic editor, apply an atomic profile, and read the values that actually reached Zen7.

The protocol is optional. Standard GTP play, analysis, ReadBoard synchronization, and existing KataGo-compatible commands do not require it.

## Capability discovery

Use `list_commands` or `known_command` instead of the `name` response. The packaged configuration reports `name` as `KataGo` for LizzieYzy Next display compatibility.

```text
known_command zengtp_config_schema
= true
```

Protocol v1 commands:

```text
zengtp_config_version
zengtp_config_schema
zengtp_config_get
zengtp_config_set
zengtp_config_save
zengtp_config_reset
```

All command bodies are single-line JSON. Success uses the normal GTP `=` response; protocol errors use `?` with a machine-readable JSON error object.

## Version and ownership

```text
zengtp_config_version
= {"protocol":"zengtp-config","version":1,"persistenceOwner":"client","commands":[...]}
```

`persistenceOwner = client` means ZenGTPX never writes a shared `zen7.cfg` in response to protocol commands. LizzieYzy Next or another client stores the returned profile per engine instance and reapplies it after process startup.

## Schema

`zengtp_config_schema` returns:

- protocol name and version;
- `batchSemantics = atomic`;
- field type, enum/range, default, basic/advanced group, active mode, apply phase, and restart requirement;
- the same current state returned by `zengtp_config_get`.

Protocol v1 fields:

| Field | Type/range | Group | Active mode |
| --- | --- | --- | --- |
| `mode` | `rank`, `fixed-time`, `advanced` | basic | all |
| `rankPreset` | `6k` through `9d` | basic | rank |
| `maxTimeSeconds` | number, `>= 0` | basic | fixed-time, advanced |
| `maxSimulations` | integer, `>= 1` | advanced | advanced |
| `pnLevel` | integer, `0..3` | advanced | advanced |
| `pnWeight` | number, `>= 0` | advanced | advanced |
| `vnMixRate` | number, `0..1` | advanced | advanced |
| `threads` | integer, `>= 1` | advanced | all; rank is capped at 4 effectively |
| `finalScoreRule` | `japanese`, `territory`, `chinese`, `area` | advanced | all |
| `resignThreshold` | number, `0..1` | advanced | all |

Every v1 field has `requiresRestart = false` and `apply = next-search`. ZenGTPX stops an active analysis before applying the batch. It does not clear the board, replay moves, change komi, or change handicap state.

## Selected and effective values

```text
zengtp_config_get
= {"protocol":"zengtp-config","version":1,"operation":"get","state":{"selected":{...},"effective":{...},"dirty":false,"restartRequired":false,"persistenceOwner":"client"}}
```

- `selected` is the profile chosen by the user. Inactive advanced values are preserved for a later mode switch.
- `effective` is the configuration currently used by the engine after mode derivation and transient GTP overrides.
- `dirty` compares `selected` with the process-local saved snapshot.
- `restartRequired` is false for every v1 field.

Example: in rank mode, selecting `threads = 12` preserves 12 in `selected`, while `effective.threads` is 4 to match the Zen7 GUI rank path.

## Atomic batch update

Pass one JSON object to `zengtp_config_set`:

```text
zengtp_config_set {"mode":"fixed-time","maxTimeSeconds":5,"threads":8}
= {"protocol":"zengtp-config","version":1,"operation":"set","state":{...}}
```

The entire object is validated before any native setter is called. If validation or application fails, the selected profile is unchanged and ZenGTPX attempts to restore the previous effective native settings.

Inactive fields may be included and are retained in `selected`; mode derivation decides which values become effective:

- `rank` derives time, simulations, PN weight, and VN mix from `rankPreset`; effective threads are capped at 4.
- `fixed-time` keeps the selected time and threads and derives the full-strength search baseline.
- `advanced` uses the selected low-level values directly.

## Save and reset

```text
zengtp_config_save
= {"protocol":"zengtp-config","version":1,"operation":"save","persistenceOwner":"client","profile":{...},"state":{...}}
```

`save` creates a process-local save point, clears `dirty`, and returns the complete profile for the client to persist. It performs no file I/O.

Reset targets:

```text
zengtp_config_reset saved
zengtp_config_reset startup
zengtp_config_reset defaults
```

- `saved` (default) restores the process-local save point.
- `startup` restores the effective profile loaded from config and CLI when this process started.
- `defaults` restores built-in ZenGTPX defaults while retaining non-profile startup settings such as the DLL path and GTP name.

## Priority and instance isolation

For profile fields, the effective priority is:

```text
transient game GTP commands
> Configuration Protocol process profile
> CLI
> zen7.cfg / JSON config
> built-in defaults
```

Transient commands such as `time_settings`, `kata-set-param maxTime`, and `kata-set-rules` may temporarily change the effective session value without changing the selected/saved profile.

Each ZenGTPX process owns its configuration service and save point. Two engine processes can therefore use different profiles without shared mutable state. The client is responsible for storing the two returned profiles separately.

## Errors

Example unknown parameter:

```text
zengtp_config_set {"unknown":1}
? {"protocol":"zengtp-config","version":1,"error":{"code":"unknown_parameter","parameter":"unknown","message":"unknown configuration parameter"}}
```

Error codes in v1:

- `invalid_arguments`
- `invalid_json`
- `invalid_type`
- `invalid_value`
- `unknown_parameter`
- `duplicate_parameter`
- `apply_failed`

Clients must use `error.code` and must not parse the human-readable `message`.
