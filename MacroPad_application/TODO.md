# MacroPad application — remaining work

Reviewed 2026-10-11. Only unfinished work is listed. Completed discovery/refactoring, default files, the firmware cooldown, and the ID migration are recorded in [the current review](../ID_MIGRATION_2026-10-11.md). Older completion records remain historical.

## Start here: configuration and Settings, one step at a time

1. **Separate loading outcomes.** Replace the ambiguous `ReadAllFiles()` boolean with explicit `Success`, `NeedsSetup`, and `Failed` outcomes. Keep the intention of opening Settings after defaults are created, but report setup as setup rather than a loading error. Do not claim defaults were restored when they were not.
2. **Make macro saves safe.** Write validated JSON to a temporary file beside the destination, then replace the original only after writing succeeds. Handle `IOException` and `UnauthorizedAccessException`, preserve the previous working file, and clean up failed temporary writes. Test interruption and denied access.
3. **Complete mapping validation.** Check duplicate device/button pairs, supported buttons and action types, non-null key arrays, supported keycodes and application targets before use. Null mappings and invalid IDs are already rejected; do not reimplement those checks. Decide how intentionally empty setup presets are represented.
4. **Connect Settings.** Replace both startup-recovery and newly-created-preset placeholders in `Program.cs` with a usable setup workflow. A preset existing does not mean its actions are configured. Coordinate saving and companion restart.

The detailed sections below retain the other release work. Complete one item and test it before starting the next.

## 1. Finish device setup and ID handling

- [ ] Harden the existing automatic assignment: exclude reserved `000000` from generated IDs, check saved/offline device IDs as well as current connections, and handle `SET_ID` write failures without stopping other boards. Generation and matching `OK:<id>` confirmation already exist; duplicate active IDs are rejected but not reassigned. Generated IDs use A–Z/0–9; accepted IDs still allow all six printable ASCII characters.
- [ ] Coordinate serial ownership with the settings app. Provisioning uses `SET_ID:<id>`; there is no separate display-name field.
- [ ] Back up and explicitly migrate existing user `ArduinoDeviceName` mappings to assigned `ArduinoDeviceId` values. Current loading gives an error for invalid/missing IDs rather than silently guessing a migration.
- [ ] Verify ID save/reload on physical hardware with the current EEPROM layout (marker 0, data 1–7), including upgrade from name/address-32 firmware, changing an assigned ID, and power loss. Keep existing mappings coordinated when deliberately changing an ID.

## 2. Harden discovery and connection lifetime

- [ ] Verify physical two-board operation: same button code with different IDs/actions, one board joining or reconnecting while another remains responsive. Only one physical board and simulated multiple-board behavior have been tested so far.
- [ ] Bound both discovery and listening buffers; discard overlong lines safely and recover on a newline.
- [ ] Add graceful cancellation and guaranteed cleanup of active connections and a pending discovery port on exit.
- [ ] Remove or explicitly gate `/tmp/cu.usbmodem*` development enumeration and virtual-device disconnect checks before release.
- [ ] Review synchronous port open/write behavior: staged waiting avoids intentional sleeps, but OS operations and a write timeout can still pause the main loop.
- [ ] Exercise identity behind queued button messages near the reply deadline, repeated malformed replies, port disappearance between scan/open, denied ports, and failed opening after identification.
- [ ] Decide whether to retain the discovery port when promoting a board to listening, avoiding a second open/reset and discarded trailing messages. Keep ownership and buffered data explicit.
- [ ] Record changed-port reconnection, multiple-line and partial-line tests, duplicate IDs, and candidate ordering under failures. Do not equate simulations with USB-driver verification.

## 3. Complete configuration validation and recovery

- [ ] Implement the separate load outcomes and safe macro-save sequence described in **Start here**; update startup handling and error messages together.
- [ ] Validate `Config(BaudRate)` against firmware baud 115200. File names are fixed `config.json` and `macros.json` in Application Support.
- [ ] Complete mapping validation described above and collect errors before executing a key sequence. Decide whether missing button mappings and empty keyboard presets mean setup is required.
- [ ] Finish file-access handling in `ReadConfig`, `AreConfigurationFilesReadable`, default copying and backup-directory creation. `ReadMacros` and restoration already handle expected read/copy failures; other paths can still throw on I/O, permissions, missing templates or failed logging.
- [ ] Change `AreConfigurationFilesReadable` to request only read access instead of `File.Open(..., FileMode.Open)` read/write access; keep access failure distinct from invalid JSON.
- [ ] Make restoration copying safe against interrupted replacement. Decide whether an invalid newest backup should lead to trying an older valid backup; currently restoration checks only the newest file.
- [ ] Decide how to recognize complete config/macros backup pairs and limit retained backups. The two copies can currently leave a partial pair; restoration selects each file independently.
- [ ] Add repeatable validation, denied-access, interrupted-save and recovery checks to the project workflow. Preserve existing user files during upgrades and explain legacy property/ID migration failures.

## 4. Action failures and diagnostics

- [ ] Handle expected macro-execution failures separately from serial errors; report unsupported actions and include device ID/button context.
- [ ] Attempt release of keys/modifiers already pressed when a shortcut fails. Do not automatically replay partial actions.
- [ ] Use `ProcessStartInfo.ArgumentList`, inspect the macOS `open` exit result, and dispose the process object.
- [ ] Bound or rotate the existing timestamped log and avoid reading/rewriting its entire history for every message. Add consistent console fallback so logging failure cannot stop startup or prevent connection cleanup; only some recovery paths currently protect against this.
- [ ] Verify application paths with spaces, missing applications, invalid shortcuts, modifier combinations, and missing Accessibility permission.

## 5. Firmware checks

- [ ] Test the user's 100 ms press-edge cooldown under bounce, held buttons, rapid intentional presses, and serial commands. Decide whether stable-state debounce is needed; the existing cooldown can suppress real quick presses.
- [ ] Verify physical `SET_ID` persistence and invalid/overlong command recovery. Test punctuation, colons, boundary spaces, six spaces, and ASCII boundaries with matching JSON.

## 6. Settings and documentation integration

- [ ] Implement the [settings editor](../MacroPad_settings/TODO.md), including the two `Program.cs` launch placeholders, shared ID/validation rules, and restart-after-save behavior.
- [ ] Fix solution filename casing: `MacroPad_settings/MacroPad_settings.csproj` should match tracked `MacroPad_settings/MacroPad_Settings.csproj`.
- [ ] Review release instructions against the final installed app, including first-run setup, backup recovery and save/restart behavior.

## 7. Installer and GitHub Release

- [ ] Choose supported macOS versions and architectures; publish self-contained Apple Silicon and optionally Intel builds.
- [ ] Package both apps, defaults, and keycode reference so no SDK or IDE is required.
- [ ] Build the `.pkg`, preserve user settings during upgrades, and make launch at login opt-in.
- [ ] Sign/notarize distribution and verify the downloaded install experience.
- [ ] Test clean install, Accessibility setup, first use, settings edits, upgrades, and uninstall under a fresh account or clean Mac.
- [ ] Publish tested GitHub Release assets with architecture labels, firmware/setup instructions, and release notes.

## 8. After V1.0 — optional cleanup

- [ ] Reassess `ConfigurationManager` responsibilities if it continues growing. Backup/restoration already live in `BackupManager`; do not split classes merely to reduce a line count. Consider separating preset creation or validation only if it improves clarity.
- [ ] Stop `IsDeviceConfigured()` after the first matching ID. Its `stopLoop` is currently set to false on a match, so the full array is scanned. Use a loop condition or flag, not a loop `break`; this is a minor efficiency improvement.
