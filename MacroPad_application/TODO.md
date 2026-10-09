# MacroPad application — remaining work

Reviewed 2026-10-09. Only unfinished work is listed here, in recommended order. [Completed work and review notes](../COMPLETED_WORK_2026-10-09.md) and the [original TODO backup](../TODO_BACKUP_2026-10-09.md) are preserved in the repository root.

## 1. Fix the multiple-board reading loop, one step at a time

- [x] **First: reset `receivedData` inside the loop for each board.** It is currently reset only before the loop. After board A supplies a message, a timeout or read failure on board B leaves A's message in the variable and can execute it using B's name.
- [x] Set `isListening` according to whether any connection is open, rather than overwriting it after each opening attempt. Currently the last board's result wins: a failed last connection prevents listening to earlier successful ones.
- [x] Skip null, disposed, or closed connections when reading. Handle the failed connection separately so one disconnected board does not force all healthy boards to close and restart discovery.
- [x] Parse nonnumeric messages safely before indexing fields. `receivedData.Split(':')[1]` currently throws for text without a colon. Distinguish identity/status replies from malformed button messages by prefix and field count.
- [x] Reduce waiting behind idle boards. Each sequential `ReadLine()` can currently wait 500 ms, so an active board can be delayed by the others. Preserve partial lines per board if moving to reading available data; receiving some bytes does not guarantee a full line.
- [ ] Add discovery of newly connected boards while other boards remain active. Discovery currently runs only when global listening stops; preserve healthy connections during rescans.
- [ ] Add graceful cancellation of both loops and guaranteed cleanup on exit. A finite read timeout helps responsiveness but does not itself provide an exit path.

## 2. Finish discovery and verify multiple-board recovery

- [ ] Make the discovery retries match the intended behavior: the five-attempt loop currently retries malformed replies, but timeout/access/I/O exceptions exit the whole attempt loop because the catch is outside it. Define which failures should retry and which should skip the port.
- [ ] Read past button messages on the same open discovery connection until identification succeeds or a total deadline expires. Reopening for each attempt can reset the board and adds startup delay.
- [ ] Validate discovered names against the firmware's 1-24 ASCII letters, digits, underscores, or hyphens; reject or resolve duplicate names and `UNNAMED` boards before using names to select macros.
- [ ] Make startup/retry timing explicit. Discovery still sleeps two seconds after every pass, and reopening for normal listening can reset the board again. Distinguish startup waits, empty-scan delays, and error retry delays.
- [ ] Update `ArduinoController.GetArduinos` documentation to describe the new attempt loop and its actual exception behavior.
- [ ] Verify two distinct board names using the same button code: only the sending board's macro should run, including when another board is idle, times out, or disconnects.
- [ ] Verify failed-first/failed-last opening attempts, simultaneous presses, partial messages, malformed input without a colon, and connecting another board while listening. The earlier live test used one Mega and does not verify this revised loop.
- [ ] Recheck no-board startup, occupied ports, unplugging between discovery and opening, and reconnecting at a changed path against the current code. Keep user-reported checks distinct from verified multiple-board cases.

## 3. Make configuration portable and validate it before execution

- [ ] Add distributable default JSON files to the source tree and supply them to `~/Library/Application Support/MacroPad/` only when missing. Directory creation alone does not make a fresh installation runnable; preserve existing settings.
- [ ] Document the retained `Config(BaudRate, MacrosFile)` format for the editor and default files. The unused `SerialPort` property has already been removed.
- [ ] Validate configuration fields: require a usable macro-file path and a baud rate matching the firmware's `115200`. Decide and document whether absolute macro-file paths remain allowed.
- [ ] Handle missing/unreadable files, inaccessible directories, invalid JSON, and invalid records with useful startup messages. Stop normal operation until configuration is corrected, then allow an explicit retry or restart.
- [ ] Validate every macro: reject null entries, duplicate `(ArduinoDeviceName, ButtonCode)` pairs, unsupported button codes, missing/invalid device names, unsupported action types, missing or unusable key arrays, unsupported keycodes, and missing application targets. Require the five firmware mappings for each configured board, or explicitly define an unassigned-button option in both apps. The same button code on two differently named boards is valid.
- [ ] Collect validation problems so the user can fix them together. Check the entire key sequence before pressing any key; fitting into `ushort` alone does not establish a valid macOS keycode.
- [ ] Add repeatable checks for malformed/missing fields, null entries, duplicate device/button pairs, unknown action types, and out-of-range values. Check lookup by both device name and button code without producing real keyboard events.
- [ ] Define migration or a clear validation error for older JSON using `buttonCode`/`keys` without `ArduinoDeviceName`. Current properties are `ArduinoDeviceName`, `ButtonCode`, `ActionType`, `Keys`, and `ApplicationPath`; default deserialization does not silently migrate old casing correctly.

## 4. Finish action failure handling and diagnostics

- [ ] Handle expected macro-execution errors separately from serial errors, with the button/action identified in the message. Report unsupported action types rather than silently doing nothing.
- [ ] Attempt to release keys and modifiers already pressed when a shortcut fails midway. Do not replay a partially executed macro; native event memory cleanup does not release held keys.
- [ ] Pass application arguments through `ProcessStartInfo.ArgumentList`, observe the `open` process's result, and dispose its process object. Distinguish failure to start `open` from `open` rejecting the target application.
- [ ] Add a small error logger with timestamp, operation, and exception details. Use concise user messages, bounded log growth, and a console fallback if logging fails.
- [ ] Verify valid application paths containing spaces, missing applications, invalid key sequences, interrupted shortcuts, and left/right modifier combinations. Check behavior when Accessibility permission is missing.

## 5. Finish firmware behavior and its hardware checks

- [ ] Add button debouncing while preserving press-edge behavior and the existing pin/code assignments. A held button should not intentionally repeat.
- [ ] Investigate the missed button-5 messages observed during live testing: inspect the switch and pin 2/GND connections, then repeat controlled presses. The cause is not established. Also verify rapid presses, holding/releasing buttons, and serial commands during button use.
- [ ] Verify a valid 24-character name and an actual rename/save cycle with restoration of the original name. The earlier live test already verified existing-name retention across USB reconnect, invalid-name rejection, oversized-command recovery, fragmented commands, and repeated identification; do not treat those as untested.

## 6. Integrate the settings app and correct project documentation

- [ ] Agree on JSON property names and validation rules with the [settings app](../MacroPad_settings/TODO.md); keep file locations consistent and choose one owner of a serial connection during setup.
- [ ] Document restart-after-save behavior, or implement an explicit reload mechanism. Verify an edited shortcut and application action end to end after settings are saved.
- [ ] Fix the solution's settings-project path casing: `MacroPad_settings/MacroPad_settings.csproj` must match the tracked `MacroPad_settings/MacroPad_Settings.csproj`.
- [ ] Rewrite README setup and examples using the completed-work notes: JSON, Application Support, automatic discovery, the new ArduinoController/MacOSController structure and device-specific JSON records, supported modifiers, and `onHardwareProgram/MacroPad/MacroPad.ino`. Remove broken INI links and old hardcoded-path instructions; describe recovery limitations accurately.
- [ ] Update `onHardwareProgram/SerialProtocol.md` for the current sketch location and implemented C# discovery. Keep remaining setup/rename integration clearly identified.

## 7. Complete device-specific settings integration

- [ ] Define how renaming a board updates the `ArduinoDeviceName` values in its saved macros, so existing mappings do not stop matching after a rename.
- [ ] Update `FindMacro` summaries and the empty `arduinoDeviceName` parameter description to explain matching both fields; include the device name in missing-mapping diagnostics.
- [ ] Keep device records and serial connections associated with the same board when adding/removing connections. Avoid relying on independently rearranged array indexes.

## 8. Final milestone: installer and GitHub Release

Coordinate with the [settings installation tasks](../MacroPad_settings/TODO.md#4-installer-integration).

- [ ] Choose supported macOS versions and architectures; publish self-contained builds for Apple Silicon and, if supported, Intel.
- [ ] Package both apps for users to launch without the SDK or IDE, with the keycode reference and default configuration included.
- [ ] Build the shared native `.pkg` installer and first-run setup; preserve existing configuration during upgrades and make launch at login an explicit choice.
- [ ] Sign the applications and installer, notarize the distribution, and verify the downloaded installation experience.
- [ ] Verify installation, Accessibility setup, first use, settings changes, upgrades, and uninstallation on a clean Mac or separate user account.
- [ ] Publish tested GitHub Release assets with architecture labels, release notes, firmware instructions, and setup guidance.
