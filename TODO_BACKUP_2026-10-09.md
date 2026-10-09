# TODO backup — 2026-10-09

Original roadmap text preserved before the remaining-work-only refresh. This is a historical backup, not the current task list. Some statements describe older code; use [completed work and review notes](COMPLETED_WORK_2026-10-09.md) for the reviewed status.

## Original MacroPad_application/TODO.md

```markdown
# MacroPad application TODOs

Development order: make the current single-board discovery and connection reliable, validate configuration and add error reporting, develop the [settings app](../MacroPad_settings/TODO.md), then package both apps for a GitHub Release. Multiple-board listening is a later extension.

Last reviewed against the code on 2026-10-08. Completed items describe implemented behavior or explicitly identified verification; unchecked items remain unfinished.

## 1. Macro records and JSON loading

The jagged-array exercise has been superseded by `Macro[]` records. Each macro contains its own variable-length `int[]` of keys. No fixed-width rows or `-1` padding are needed.

- [x] Define the `Macro` record in `Models/Macro.cs`.
- [x] Replace fixed-width text rows with a JSON macro array and deserialize it through `ConfigurationReader`. The former source-tree `Configuration/macros.json` has been removed from the repository; startup loads the configured macro file from the user configuration location.
- [x] Select the first matching record with `MacroPadController.FindMacro`, returning `null` when no match exists.
- [x] Pass the selected record directly to `MacController.ExecuteMacro`.
- [x] Implement separate keyboard and application branches, including a check for a blank application path.
- [x] Store `ApplicationPath` directly in each application macro, replacing the separate application-identifier lookup.
- [x] Build the companion app and verify macro selection for known codes, unknown codes, and an empty array.
- [x] Document the configuration reader, macro lookup, native keyboard helpers, and Arduino functions.
- [x] Add XML summaries to the newer `GetConfigurationDirectoryPath` and `GetArduinos` helpers; keep `Main`, classes, and fields without XML summaries.

`Main` loads configuration and macros, discovers boards, opens connections for all discovered boards, and reads button messages only from `arduino[0]`. The user reports the single-board test working on 2026-10-08. Multiple-board listening is not implemented. Rediscovery after read failures is implemented, but the remaining recovery bugs below are not resolved or hardware-verified.

Validation of snapshot `99e71fc`: both C# projects built with zero warnings/errors and the firmware compiled for the Mega 2560. No hardware actions were run during that verification.

## 2. General configuration

- [x] Introduce JSON configuration with `SerialPort`, `BaudRate`, and `MacrosFile`. `Config.SerialPort` remains in the record but is no longer used to choose a connection; the former source-tree JSON file has been removed from the repository.
- [x] Define the configuration record as `Config` in `Models/Config.cs` (the implemented name replaces the proposed `AppConfiguration`).
- [x] Add `ReadConfig(string path)` to `ConfigurationReader`, returning a `Config` record (the implemented name replaces the proposed `ReadConfiguration`).
- [x] Resolve a relative `MacrosFile` against the configuration directory using `Path.Combine`.
- [x] Load general configuration first in `Main`, then load macros through its configured path.
- [x] Read the baud rate from configuration and obtain port paths through discovery.
- [x] Resolve the user home directory and create `~/Library/Application Support/MacroPad/`; load `config.json` there independently of the working directory.
- [ ] Ship default JSON files in the source tree and copy them to Application Support only when missing. Creating the directory currently does not create either configuration file.
- [ ] Decide whether to retain configurable `BaudRate` and `MacrosFile`. Remove the unused `SerialPort` property and update JSON examples once the configuration format is settled; discovery alone does not remove the remaining uses of `config.json`.
- [ ] Validate an appropriate baud rate and a nonblank, existing macro-file location. Keep the baud rate consistent with the firmware's `115200` setting.
- [ ] Handle missing files and invalid JSON with clear messages before starting normal operation.

## 3. Next milestone: reliable discovery and single-board operation

### Implemented discovery and firmware

- [x] Define `Arduino(Port, Name)` in `Models/Arduino.cs`.
- [x] Enumerate serial ports, filter `/dev/cu.usbmodem` candidates, and send `WHO_ARE_YOU?` after a startup delay.
- [x] Set discovery read/write timeouts and report timeout, access, and I/O failures.
- [x] Collect recognized `MACROPAD` replies into a temporary array and copy the populated entries into the returned `Arduino[]`.
- [x] Add newline-based `WHO_ARE_YOU?` and `SET_NAME:` commands to the Mega firmware at `onHardwareProgram/MacroPad/MacroPad.ino`.
- [x] Validate firmware names, save changes in EEPROM, and reload the saved name at startup, with an `UNNAMED` fallback.
- [x] Preserve the five button codes and read incoming command characters without waiting for a complete line.

### Implemented connection recovery

- [x] Handle zero discovered boards without accessing `arduino[0]`; print search progress, wait one second, and scan again instead of ending startup.
- [x] Check that a handshake reply has exactly two fields, the `MACROPAD` prefix, and a nonblank name before accessing `response[1]`.
- [x] Dispose of discovery connections on success and failure with a `using SerialPort` declaration.
- [x] Catch access and I/O errors while reopening discovered ports in `Main`.
- [x] Catch I/O and closed-port errors around `ReadLine()` only; print the exception message and clear `isListening`.
- [x] Skip macro execution after a failed read and return to the outer discovery loop without replaying the previous message.
- [x] Add disposal of the previous listening connections before rescanning. Cleanup after a partially completed opening loop still needs the fix below.

### Do next, in this order

1. [ ] **Fix the listening flag after an opening failure.** The catch sets `isListening = false`, but the following array-length check sets it back to true. Enter the listening loop only after the chosen connection opens successfully.
2. [ ] **Make cleanup safe after partial opening.** If opening one board fails, later array entries can remain null. Dispose only initialized connections, including the failed connection, without throwing during cleanup.
3. [ ] **Delay retries after opening failures.** The one-second delay currently applies only when discovery returns no boards. Apply a deliberate retry delay to failed openings too, and define how the user stops retrying.
4. [ ] **Finish discovery reply handling.** Keep looking for the identity reply within a bounded total time if a button code or unrelated line arrives first. The current code reads only one line per candidate. Validate the returned name against the firmware's length and character rules.
5. [ ] **Test recovery on hardware.** Start with no board connected, then connect, press, unplug, reconnect, and press again. Also test an occupied port and unplugging between discovery and reopening. A successful build does not verify these cases.

### Remaining connection work
- [ ] For the current single-board milestone, open only the chosen board for button handling. Discovery may return several boards, but `Main` currently opens all of them and reads only the first. Do not rely on discovery order as a permanent device identity.
- [ ] Allow startup time before sending commands after reopening, which can reset the Mega again. Keeping the matching connection open is a later alternative to the current scan-close-reopen flow.
- [ ] In addition to the recovery test above, test missing/malformed identification replies and a button pressed during discovery.
- [ ] Verify saved-name persistence on the physical Mega after unplugging. Firmware support is implemented; this specific hardware check is not established by the current single-board success report.

### Later: multiple boards and setup

- [ ] Implement user-requested naming in the settings app with `SET_NAME:`, checking `OK:` or `ERROR:` replies. Do not rename boards automatically from their scan index.
- [ ] Define how duplicate names and `UNNAMED` boards are selected during setup before relying on names to remember a device.
- [ ] Decide how macros are associated with each board; the current `Macro[]` is shared and lookup uses only button code.
- [ ] Read multiple boards without letting an idle board's blocking `ReadLine()` prevent processing messages from another. A simple loop of blocking reads is not sufficient.

### Configuration, error reporting, and reliable execution

- [ ] Validate loaded macros: reject null entries, duplicate button codes, unsupported action types, missing keyboard arrays, invalid keycodes, and missing application paths. Validate the full key sequence before pressing modifiers.
- [ ] Collect configuration validation messages so the user can fix all detected problems before startup continues. Successful deserialization alone is not validation.
- [ ] Add a small error logger recording timestamp, operation/context, and full exception details, with concise console messages, bounded log growth, and console fallback if writing the log fails.
- [ ] Add expected-error handling at configuration startup and macro execution boundaries. Serial discovery, opening, and reading already have catches; correct the remaining connection-state bugs above. Avoid duplicate logging and blanket catch-and-continue behavior.
- [ ] Stop startup for invalid configuration; retry loading only after the user has had a chance to correct it.
- [ ] Report unsupported action types explicitly in `ExecuteMacro`.
- [x] Check native keyboard-event creation for failure and release created events with `CFRelease` after use, including on error paths.
- [ ] Attempt to release keys/modifiers pressed by the app if execution fails midway. Do not automatically replay a partially executed macro.
- [ ] Use `ProcessStartInfo.ArgumentList` for configured application paths and check the `open` process's result so failed launches are reported.
- [x] Read button codes from the first discovered board using `ReadLine` before macro selection and execution.
- [x] Use `int.TryParse` for incoming messages, report malformed messages, and skip unknown button codes without terminating the listening loop.
- [x] Complete a basic single-board check of the discovered connection (user-reported working on 2026-10-08).
- [ ] Dispose of the serial connection on shutdown and provide a graceful way to exit the listening loop.
- [ ] Complete the retry policy: rediscovery after read failure is implemented, but retries are currently indefinite, opening failures need a delay, and there is no graceful cancellation. Decide on bounded attempts or explicitly cancellable background searching; report persistent failures without replaying macros.
- [ ] Verify failure handling for missing files, invalid JSON/records, unavailable ports, disconnects, failed application launches, and interrupted shortcut execution. Also check valid application paths containing spaces.
- [ ] Add firmware debounce so one physical press reliably produces one event.
- [ ] Update README setup instructions and examples to reflect JSON, Application Support, discovery, saved names, direct application paths, and the new `onHardwareProgram/MacroPad/MacroPad.ino` path. Update `onHardwareProgram/SerialProtocol.md`: C# discovery is now implemented, though still needs the checks above.

## 4. Work with the settings app

- [ ] Keep `config.json` and `macros.json` formats consistent between the two apps, including property-name casing and validation rules.
- [ ] Define how the settings app locates its keycode reference; it currently remains in `MacroPad_settings/keycodes.ini`.
- [ ] Document restart-after-save behavior in the settings app, or implement an explicit reload mechanism. The companion app currently loads configuration and macros only at startup.
- [ ] Verify that settings saved by the editor load correctly and trigger the intended actions in the companion app.

## 5. Final project milestone: installer and GitHub Release

Complete this after both apps work together. Coordinate the setup experience with the [settings app's installer tasks](../MacroPad_settings/TODO.md#3-final-project-milestone-full-installer).

- [ ] Choose supported macOS versions and publish self-contained builds for Apple Silicon (`osx-arm64`) and Intel (`osx-x64`) if supporting both.
- [ ] Package both apps as launchable Mac applications, including the runtime, keycode reference, and default JSON files.
- [ ] Build a native `.pkg` installer using macOS packaging tools rather than a custom installer interface.
- [ ] Complete first-launch configuration setup under `~/Library/Application Support/MacroPad/` for both apps: directory creation is implemented, but initial JSON creation is not. Preserve existing user settings during upgrades.
- [ ] Provide first-run setup in the settings app and make launch at login an explicit user choice.
- [ ] Sign the applications and installer, notarize the distribution, and verify the downloaded installation experience.
- [ ] Verify installation, first launch, settings changes, button actions, upgrades, and uninstallation on a clean Mac or separate user account.
- [ ] Publish the tested packages as GitHub Release assets with release notes, architecture labels, firmware instructions, and Accessibility setup guidance.

```

## Original MacroPad_settings/TODO.md

```markdown
# MacroPad settings TODOs

The [companion app](../MacroPad_application/TODO.md) loads JSON from Application Support and discovers MacroPads by handshake; the user reports single-board operation working. Complete the companion app's ordered connection fixes (listening state, partial cleanup, retry delay, and reply handling), then configuration validation before developing the settings editor. As of 2026-10-08, the settings app remains a `Hello, World!` placeholder; no editor features below are complete. Finish the project with a shared macOS installer distributed through GitHub Releases.

The implemented data model uses a `Config` record for `SerialPort`, `BaudRate`, and `MacrosFile`, plus `Macro` records with the JSON property names `buttonCode`, `ActionType`, `keys`, and `ApplicationPath`. Application paths are stored directly in macros; no application-identifier table or `-1` padding is needed. The companion app's `ReadConfig` and `ReadMacros` methods deserialize these files, and `Main` resolves `MacrosFile` relative to the configuration file's directory. Validation of the deserialized values remains unfinished. `Config.SerialPort` is currently unused because discovery supplies the port; settle the configuration format before building an editor for it. Both apps should use `~/Library/Application Support/MacroPad/`. The companion app creates that directory but does not yet supply missing JSON files.

`Arduino(Port, Name)` records hold discovered devices. Mega firmware supports `WHO_ARE_YOU?` and `SET_NAME:` with EEPROM-backed names; the C# discovery code reads names, but there is no settings-side rename workflow yet.

## First settings milestone

After the companion app's connection fixes and configuration validation:

1. [ ] Start with a small console menu using the current teaching style.
2. [ ] Load the existing configuration from Application Support and display all five button mappings.
3. [ ] Edit one button's action in memory and validate it before saving.
4. [ ] Save readable JSON while preserving the last valid settings if saving fails.
5. [ ] Restart the companion app and verify the edited action with the board.

Then add board selection and renaming. A graphical interface and installer remain later work.

## 1. Start development of the settings app

- [ ] Replace the `Hello, World!` placeholder with the console workflow above; revisit a graphical interface after editing and saving work.
- [ ] Keep the existing C# coding style: explicit types, arrays, simple loops, and small helper methods. Do not use `break` to exit loops.
- [ ] Load `config.json` and deserialize `macros.json` into records, matching the companion app's property names and validation rules.
- [ ] Resolve the macro-file path relative to the configuration file and use the same user configuration location as the companion app.
- [ ] Read the keycode reference from its configured file location and show readable key names when editing shortcuts.
- [ ] Display the current action assigned to each of the five buttons.
- [ ] Allow users to choose a keyboard shortcut or an application launch for each button.
- [ ] Allow users to edit key sequences and modifiers, using the lengths and modifier codes supported by the companion app.
- [ ] Allow users to select application targets and save their paths directly in `ApplicationPath`.
- [ ] List boards confirmed by the `MACROPAD:<name>` handshake, showing their names and current ports; let the user select a board if several are connected.
- [ ] Let the user name or rename a selected board with `SET_NAME:<name>`, checking the acknowledgement before reporting success. Enforce the firmware's 1-24 ASCII letters, digits, underscores, or hyphens.
- [ ] Handle `UNNAMED` and duplicate names during setup; do not assign permanent names from the order of discovered ports.
- [ ] Coordinate serial ownership with the companion app so both apps do not try to use the same port at once.
- [ ] Expose only configuration choices retained by the companion app. A changing USB port should be discovered, not stored as a permanent device identity.
- [ ] Defer per-board macro profiles until multiple-board support and the device-to-profile association are defined.

## 2. Save settings and verify integration

- [ ] Validate input before saving, including unique button codes, supported action types, keycodes, required application paths, file locations, retained serial settings, and device names.
- [ ] Present all detected validation problems together and use the companion app's agreed validation rules. Log file-reading/saving failures and allow retry after correction without overwriting valid settings.
- [ ] Serialize records back to readable, indented JSON. Preserve the agreed property names, use `null` for unused application paths, and use an empty key array for application actions.
- [ ] Preserve existing valid settings when an edit is cancelled or saving fails; provide clear success and error messages.
- [ ] Explain that changes currently require restarting the companion app; coordinate any future reload feature with it.
- [ ] Verify that loading and saving unchanged settings preserves their meaning.
- [ ] Check an edited keyboard shortcut and application launch end to end with the companion app and connected board.

## 3. Final project milestone: full installer

This is the same shared `.pkg` installer and GitHub Release milestone tracked in the [companion app TODOs](../MacroPad_application/TODO.md#5-final-project-milestone-installer-and-github-release).

- [ ] Include the settings app and its keycode reference in the installer alongside the companion app.
- [ ] Provide first-run setup for the Arduino connection, file locations, initial button assignments, and application targets.
- [ ] Create initial JSON files under `~/Library/Application Support/MacroPad/` when needed, without overwriting an existing user's settings.
- [ ] Guide users through connecting the board, preparing its firmware, granting macOS Accessibility permission, and testing their first button.
- [ ] Make it easy to reopen the settings app after installation and explain how to start the companion app.
- [ ] Verify that a new user can install, configure, and use the MacroPad without editing source files or opening an IDE for either C# app.
- [ ] Include first-run setup instructions in the GitHub Release notes alongside the installer downloads.

```
