# MacroPad application — remaining work

Reviewed 2026-10-09. Only unfinished work is listed here, in recommended order. [Completed work and review notes](../COMPLETED_WORK_2026-10-09.md) and the [original TODO backup](../TODO_BACKUP_2026-10-09.md) are preserved in the repository root.

## 1. Fix the current single-board connection flow first

- [x] Set `isListening` to true only after the selected connection opens successfully. The array-length check currently overwrites the false value set by the opening-error catch.
- [x] Make cleanup safe after a partially completed opening loop: dispose every initialized connection, including failed ones, and skip null array entries. A failure before the last board currently leaves entries that make the next cleanup throw.
- [x] Open every board discovered.
- [x] Report “connected” only after the listening connection opens. The message in `GetArduinos` currently describes discovery, whose connections have already been closed.
- [ ] Make startup waits and retry delays deliberate. There is now a two-second wait after every discovery pass, plus one second when no boards are found; replace this scattered timing with clear startup/retry behavior. The end-of-scan delay occurs before reopening and does not wait for any reset caused by reopening.
- [ ] Add graceful cancellation of searching and listening, and guaranteed cleanup on exit. Account for `ReadLine()` blocking while the board is idle; a loop flag alone cannot interrupt it. If finite read timeouts are introduced, treat an idle timeout separately from a broken connection.

## 2. Finish discovery and recovery handling

- [ ] Read until a valid identity reply arrives or a total discovery deadline expires. A button code arriving first currently causes the board to be missed; unrelated lines must not keep discovery running indefinitely.
- [ ] Validate the returned name against the firmware's 1-24 ASCII letters, digits, underscores, or hyphens. The existing prefix/field-count/nonblank checks do not enforce those rules.
- [ ] Define whether persistent connection failures use bounded retries or cancellable background searching, and give clear status messages without replaying previous macros.
- [ ] Verify startup without a board, connection after startup, an occupied port, unplugging between discovery and reopening, disconnection during listening, and successful use after reconnecting.
- [ ] Verify missing/malformed replies, a button press during discovery, and partial opening failures with more than one candidate. Confirm that failed connections do not remain occupied.

## 3. Make configuration portable and validate it before execution

- [ ] Add distributable default JSON files to the source tree and supply them to `~/Library/Application Support/MacroPad/` only when missing. Directory creation alone does not make a fresh installation runnable; preserve existing settings.
- [ ] Settle the configuration format before building the editor. Remove the unused `Config.SerialPort` field and decide whether `BaudRate` and `MacrosFile` remain configurable; update examples and readers together.
- [ ] Validate configuration fields: require a usable macro-file path and a baud rate matching the firmware's `115200`. Decide and document whether absolute macro-file paths remain allowed.
- [ ] Handle missing/unreadable files, inaccessible directories, invalid JSON, and invalid records with useful startup messages. Stop normal operation until configuration is corrected, then allow an explicit retry or restart.
- [ ] Validate every macro: reject null entries, duplicate/unsupported button codes, unsupported action types, missing or unusable key arrays, unsupported keycodes, and missing application targets. Require a mapping for each of the five firmware codes, or explicitly define an unassigned-button option in both apps.
- [ ] Collect validation problems so the user can fix them together. Check the entire key sequence before pressing any key; fitting into `ushort` alone does not establish a valid macOS keycode.
- [ ] Add repeatable checks for malformed/missing fields, null entries, duplicate codes, unknown action types, and out-of-range values, alongside valid and unknown-code lookups. Keep these checks independent of real keyboard output.

## 4. Finish action failure handling and diagnostics

- [ ] Handle expected macro-execution errors separately from serial errors, with the button/action identified in the message. Report unsupported action types rather than silently doing nothing.
- [ ] Attempt to release keys and modifiers already pressed when a shortcut fails midway. Do not replay a partially executed macro; native event memory cleanup does not release held keys.
- [ ] Pass application arguments through `ProcessStartInfo.ArgumentList`, observe the `open` process's result, and dispose its process object. Distinguish failure to start `open` from `open` rejecting the target application.
- [ ] Add a small error logger with timestamp, operation, and exception details. Use concise user messages, bounded log growth, and a console fallback if logging fails.
- [ ] Verify valid application paths containing spaces, missing applications, invalid key sequences, interrupted shortcuts, and left/right modifier combinations. Check behavior when Accessibility permission is missing.

## 5. Finish firmware behavior and its hardware checks

- [ ] Add button debouncing while preserving press-edge behavior and the existing pin/code assignments. A held button should not intentionally repeat.
- [ ] Verify all five button codes, rapid presses, holding/releasing buttons, and serial commands arriving while buttons are used on the physical Mega 2560.
- [ ] Verify device-name persistence after unplugging, invalid/empty/maximum-length names, overlong-command rejection, and acceptance of the next valid command. Firmware implementations exist; these physical checks still need confirmation.

## 6. Integrate the settings app and correct project documentation

- [ ] Agree on JSON property names and validation rules with the [settings app](../MacroPad_settings/TODO.md); keep file locations consistent and choose one owner of a serial connection during setup.
- [ ] Document restart-after-save behavior, or implement an explicit reload mechanism. Verify an edited shortcut and application action end to end after settings are saved.
- [ ] Fix the solution's settings-project path casing: `MacroPad_settings/MacroPad_settings.csproj` must match the tracked `MacroPad_settings/MacroPad_Settings.csproj`.
- [ ] Rewrite README setup and examples using the completed-work notes: JSON, Application Support, automatic discovery, records/controllers, supported modifiers, and `onHardwareProgram/MacroPad/MacroPad.ino`. Remove broken INI links and old hardcoded-path instructions; describe recovery limitations accurately.
- [ ] Update `onHardwareProgram/SerialProtocol.md` for the current sketch location and implemented C# discovery. Keep remaining setup/rename integration clearly identified.

## 7. Later extension: multiple boards

- [ ] Define stable board selection and handling of duplicate names or `UNNAMED` devices with the settings app. Never assign persistent names from scan order.
- [ ] Define how each board selects its macro profile; current lookup uses only button code in one shared `Macro[]`.
- [ ] Read multiple boards without letting one idle board's blocking read hold up the others. Define how one board disconnecting affects the remaining connections.

## 8. Final milestone: installer and GitHub Release

Coordinate with the [settings installation tasks](../MacroPad_settings/TODO.md#4-installer-integration).

- [ ] Choose supported macOS versions and architectures; publish self-contained builds for Apple Silicon and, if supported, Intel.
- [ ] Package both apps for users to launch without the SDK or IDE, with the keycode reference and default configuration included.
- [ ] Build the shared native `.pkg` installer and first-run setup; preserve existing configuration during upgrades and make launch at login an explicit choice.
- [ ] Sign the applications and installer, notarize the distribution, and verify the downloaded installation experience.
- [ ] Verify installation, Accessibility setup, first use, settings changes, upgrades, and uninstallation on a clean Mac or separate user account.
- [ ] Publish tested GitHub Release assets with architecture labels, release notes, firmware instructions, and setup guidance.
