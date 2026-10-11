# MacroPad application — remaining work

Reviewed 2026-10-11. Only unfinished work is listed. Completed discovery/refactoring, default files, the firmware cooldown, and the ID migration are recorded in [the current review](../ID_MIGRATION_2026-10-11.md). Older completion records remain historical.

## 1. Finish device setup and ID handling

- [ ] Implement setup-time generation/assignment of unique six-character printable ASCII IDs, excluding reserved `000000`. Read an existing ID first and confirm `OK:<id>` before saving matching macros; duplicate active IDs are rejected but not automatically reassigned.
- [ ] Coordinate serial ownership with the settings app. Provisioning uses `SET_ID:<id>`; there is no separate display-name field.
- [ ] Back up and explicitly migrate existing user `ArduinoDeviceName` mappings to assigned `ArduinoDeviceId` values. Current loading gives an error for invalid/missing IDs rather than silently guessing a migration.
- [ ] Verify ID save/reload on physical hardware, including upgrade from old name firmware, changing an assigned ID, and retaining it after power loss. Keep existing mappings coordinated when deliberately changing an ID.

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

- [ ] Validate `Config(BaudRate)` against firmware baud 115200. File names are now fixed `config.json` and `macros.json` in Application Support; `MacrosFile` is no longer a setting.
- [ ] Handle malformed JSON, missing templates, unreadable files/directories, and permission errors with useful messages and a bounded retry or exit. Missing-file copying is implemented; it does not repair corrupted JSON.
- [ ] Fix `AreFilesReadable` requesting read/write access through `File.Open(..., FileMode.Open)` when only reading is needed; handle access failures explicitly.
- [ ] Handle a macro file disappearing after the existence check: `ReadMacros` currently returns an empty array after FileNotFoundException, and the caller can incorrectly report success.
- [ ] Validate duplicate `(ArduinoDeviceId, ButtonCode)` pairs, supported button codes/actions, non-null key arrays, valid macOS keycodes, and application targets. Null entries and invalid IDs are now rejected; other macro validation remains incomplete.
- [ ] Collect errors before executing any key sequence. Decide whether missing button mappings are allowed and keep editor behavior consistent.
- [ ] Add repeatable validation/save-failure checks and explicit migration errors for older property casing. Preserve existing user files and defaults during upgrades.

## 4. Action failures and diagnostics

- [ ] Handle expected macro-execution failures separately from serial errors; report unsupported actions and include device ID/button context.
- [ ] Attempt release of keys/modifiers already pressed when a shortcut fails. Do not automatically replay partial actions.
- [ ] Use `ProcessStartInfo.ArgumentList`, inspect the macOS `open` exit result, and dispose the process object.
- [ ] Add bounded timestamped error logging with console fallback.
- [ ] Verify application paths with spaces, missing applications, invalid shortcuts, modifier combinations, and missing Accessibility permission.

## 5. Firmware checks

- [ ] Test the user's 100 ms press-edge cooldown under bounce, held buttons, rapid intentional presses, and serial commands. Decide whether stable-state debounce is needed; the existing cooldown can suppress real quick presses.
- [ ] Verify physical `SET_ID` persistence and invalid/overlong command recovery. Test punctuation, colons, boundary spaces, six spaces, and ASCII boundaries with matching JSON.

## 6. Settings and documentation integration

- [ ] Implement the [settings editor](../MacroPad_settings/TODO.md), using shared ID and validation rules and restart-after-save behavior.
- [ ] Fix solution filename casing: `MacroPad_settings/MacroPad_settings.csproj` should match tracked `MacroPad_settings/MacroPad_Settings.csproj`.
- [ ] Document new discovery/configuration helpers as their behavior stabilizes; review release instructions against the final installed app.

## 7. Installer and GitHub Release

- [ ] Choose supported macOS versions and architectures; publish self-contained Apple Silicon and optionally Intel builds.
- [ ] Package both apps, defaults, and keycode reference so no SDK or IDE is required.
- [ ] Build the `.pkg`, preserve user settings during upgrades, and make launch at login opt-in.
- [ ] Sign/notarize distribution and verify the downloaded install experience.
- [ ] Test clean install, Accessibility setup, first use, settings edits, upgrades, and uninstall under a fresh account or clean Mac.
- [ ] Publish tested GitHub Release assets with architecture labels, firmware/setup instructions, and release notes.
