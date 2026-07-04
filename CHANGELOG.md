# CHANGELOG

## Unreleased

### Added
- Support `fixed_handicap` and `place_free_handicap` GTP commands with deterministic standard star-point placement.
- Forward GTP `time_settings` and `time_left` to Zen native time APIs while preserving the wrapper move deadline.
- Return a Zen territory-statistics based area-score estimate for `final_score`.
- Return empty compatibility lists for `final_status_list alive/dead/seki`.
- Support first-pass background streaming `lz-analyze` and `kata-analyze` GTP analysis commands backed by `ZenGetTopMoveInfo`.
- Support minimal KataGo GTP compatibility commands needed by LizzieYzy Next analysis probing.
- Support `set_free_handicap` as a GTP-compatible free handicap placement command.
- Support ZenGTPX diagnostic commands for policy, territory, and final-score estimate breakdowns.
- Harden GTP lifecycle state handling for occupied moves, generated moves, pass moves, handicap placement, undo, and clear-board flows.
- Reject negative `time_left` stone counts before forwarding time updates to the engine.
- Document the version history baseline for ZenGTPX.

## 0.1.0

### Added
- Initial GTP session loop over stdin/stdout.
- Basic GTP command support: `protocol_version`, `name`, `version`, `list_commands`, `known_command`, `boardsize`, `clear_board`, `komi`, `play`, `genmove`, `undo`, `time_settings`, `time_left`, `showboard`, `final_score`, and `quit`.
- Configuration loading from defaults, `.cfg`, JSON, and CLI arguments.

## Planned

### 0.3.0
- Faster native-search cancellation for `stop`.
