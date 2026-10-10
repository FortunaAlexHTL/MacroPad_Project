# MacroPad application — remaining work

Reviewed 2026-10-10. Only unfinished work is listed here, grouped by responsibility. Resume with section 1, then the persistent-ID design in section 7 before finalizing configuration and settings schemas. The [refactor checkpoint](../REFACTOR_CHECKPOINT_2026-10-10.md) records newly completed work and the next mentoring step. [Completed work and review notes](../COMPLETED_WORK_2026-10-09.md) and the [original TODO backup](../TODO_BACKUP_2026-10-09.md) are preserved in the repository root.

## 1. Next: discover boards while existing connections stay active

- [x] First, separate candidate enumeration from probing: use `GetNewPortNames` to identify ports not already in use, without opening them or changing the active connection array. The timer currently only prints a message.
- [x] Design discovery so startup waits and reply timeouts do not stop reading healthy boards. Do not call the current blocking `GetArduinos` directly from the active reading loop. Choose the smallest approach the user understands before implementing it.
- [x] Add confirmed connections to the existing collection and remove/dispose inactive entries without replacing healthy objects or mixing their buffers and identities. `OpenConnections` currently creates a new array; assigning it during an active scan would discard the old collection.
- [ ] Verify plugging in or reconnecting one board while another stays active, with continued button processing and no duplicate opens. Rescan timing should avoid repeatedly probing a busy or unresponsive port.
- [ ] Bound each connection's incoming buffer and define how to discard an oversized line and recover at the next newline. A device sending text without a newline currently grows the buffer indefinitely.
- [ ] Add graceful cancellation and guaranteed cleanup on exit, including discovery waits. Keep partially executed macros out of automatic retries.

## 2. Improve discovery and cover remaining failure cases

- [ ] Read past button messages on the same open discovery connection until identification succeeds or a total deadline expires. Each current attempt reads one line and reopening can reset the board.
- [ ] Validate discovered names against the firmware's 1-24 ASCII letters, digits, underscores, or hyphens. Until persistent IDs are implemented, reject ambiguous duplicate names and handle `UNNAMED` explicitly before matching macros by name.
- [ ] Make startup/retry timing explicit: two seconds per probe startup, up to two seconds per read, and two seconds after each scan. A silent candidate can take roughly 20 seconds plus scan delay; reopening for normal listening can reset it again.
- [ ] Exercise the revised retry policy: timeout/malformed reply retries up to five times, valid identity stops, access/I/O/disposed-port failure skips that port. Confirm discovery proceeds to later candidates. This policy is implemented but its failure paths were not independently exercised in this review.
- [ ] Verify distinct device names with the same button code and different actions; confirm only the sending board's action executes.
- [ ] Verify failed-first/failed-last opening attempts, simultaneous presses, fragmented lines, multiple lines in one read, oversized lines, malformed input without a colon, and unknown numeric codes.
- [ ] Recheck no-board startup, occupied ports, unplugging between discovery and opening, and reconnecting at a changed port path. Record exact setups and outcomes; user-reported basic multi-board/unplug tests are recorded in the root completion notes.

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

- [ ] Coordinate renaming with saved macros while matching still uses `ArduinoDeviceName`; after ID migration, keep mappings attached to the ID regardless of display-name changes.
- [ ] Include the device name (and later ID) in missing-mapping diagnostics; the lookup documentation now describes both matching fields.
- [ ] Define a persistent device ID assigned once during setup, separate from the editable display name. Decide the command/reply format before changing firmware or C#.
- [ ] Generate and provision IDs from setup, persist them in EEPROM without overwriting the existing name storage, and confirm saved identity before reporting success. Handle missing, invalid, and duplicate IDs and interrupted setup.
- [ ] Extend discovery records and handshake parsing to carry the ID, then migrate macro lookup and JSON validation to `(DeviceId, ButtonCode)`. Choose the final JSON property name together; `DeviceId` is a proposal, not the current schema.
- [ ] Migrate existing name-based mappings deliberately; do not silently assign ambiguous mappings to duplicate names. Verify two boards with the same display name trigger only their own macros and retain IDs after reconnect/rename.

## 8. Final milestone: installer and GitHub Release

Coordinate with the [settings installation tasks](../MacroPad_settings/TODO.md#4-installer-integration).

- [ ] Choose supported macOS versions and architectures; publish self-contained builds for Apple Silicon and, if supported, Intel.
- [ ] Package both apps for users to launch without the SDK or IDE, with the keycode reference and default configuration included.
- [ ] Build the shared native `.pkg` installer and first-run setup; preserve existing configuration during upgrades and make launch at login an explicit choice.
- [ ] Sign the applications and installer, notarize the distribution, and verify the downloaded installation experience.
- [ ] Verify installation, Accessibility setup, first use, settings changes, upgrades, and uninstallation on a clean Mac or separate user account.
- [ ] Publish tested GitHub Release assets with architecture labels, release notes, firmware instructions, and setup guidance.
