# MacroPad application TODOs

Development order: make the current single-board discovery and connection reliable, validate configuration and add error reporting, develop the [settings app](../MacroPad_settings/TODO.md), then package both apps for a GitHub Release. Multiple-board listening is a later extension.

Last reviewed against the code on 2026-10-08. Completed items describe implemented behavior or explicitly identified verification; unchecked items remain unfinished.

## 1. Macro records and JSON loading

The jagged-array exercise has been superseded by `Macro[]` records. Each macro contains its own variable-length `int[]` of keys. No fixed-width rows or `-1` padding are needed.

- [x] Define the `Macro` record in `Models/Macro.cs`.
- [x] Replace fixed-width text rows with a JSON macro array and deserialize it through `ConfigurationReader`. The former source-tree `Configuration/macros.json` is now deleted in the working tree; startup loads the configured macro file from the user configuration location.
- [x] Select the first matching record with `MacroPadController.FindMacro`, returning `null` when no match exists.
- [x] Pass the selected record directly to `MacController.ExecuteMacro`.
- [x] Implement separate keyboard and application branches, including a check for a blank application path.
- [x] Store `ApplicationPath` directly in each application macro, replacing the separate application-identifier lookup.
- [x] Build the companion app and verify macro selection for known codes, unknown codes, and an empty array.
- [x] Document the configuration reader, macro lookup, native keyboard helpers, and Arduino functions.
- [ ] Add XML summaries to the newer `GetConfigurationDirectoryPath` and `GetArduinos` helpers; keep `Main`, classes, and fields without XML summaries.

`Main` loads configuration and macros, discovers boards, opens connections for all discovered boards, and reads button messages only from `arduino[0]`. The user reports the single-board test working on 2026-10-08. Multiple-board listening and failure handling are not verified.

## 2. General configuration

- [x] Introduce JSON configuration with `SerialPort`, `BaudRate`, and `MacrosFile`. `Config.SerialPort` remains in the record but is no longer used to choose a connection; the former source-tree JSON file is deleted in the working tree.
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

### Do next, in this order

- [ ] Handle zero discovered boards before accessing `arduino[0]`: display a clear message and end startup cleanly.
- [ ] Validate the full handshake reply before indexing `response[1]`: require the expected prefix, number of fields, and a valid nonempty name.
- [ ] Keep looking for the identity reply within a bounded total discovery time if a button code or unrelated line arrives first. The current code reads only one line per candidate.
- [ ] Dispose of each discovery `SerialPort` on both success and failure. The current `Close()` is skipped when a read or write throws; a `using` declaration inside the `try` provides cleanup.
- [ ] For the current single-board milestone, open only the chosen board for button handling. Discovery may return several boards, but `Main` currently opens all of them and reads only the first. Do not rely on discovery order as a permanent device identity.
- [ ] Make reopening deliberate: it can reset the Mega again. Handle a board disappearing between discovery and normal listening, and allow startup time before any new commands. Keeping the matching connection open is a later alternative to the current scan-close-reopen flow.
- [ ] Verify the single-board flow with no board connected, an occupied port, a missing/malformed reply, a button pressed during discovery, and a disconnect during use.
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
- [ ] Catch expected errors at startup, connection, and action-execution boundaries where recovery decisions can be made. Avoid duplicate logging and blanket catch-and-continue behavior.
- [ ] Stop startup for invalid configuration; retry loading only after the user has had a chance to correct it.
- [ ] Report unsupported action types explicitly in `ExecuteMacro`.
- [x] Check native keyboard-event creation for failure and release created events with `CFRelease` after use, including on error paths.
- [ ] Attempt to release keys/modifiers pressed by the app if execution fails midway. Do not automatically replay a partially executed macro.
- [ ] Use `ProcessStartInfo.ArgumentList` for configured application paths and check the `open` process's result so failed launches are reported.
- [x] Read button codes from the first discovered board using `ReadLine` before macro selection and execution.
- [x] Use `int.TryParse` for incoming messages, report malformed messages, and skip unknown button codes without terminating the listening loop.
- [x] Complete a basic single-board check of the discovered connection (user-reported working on 2026-10-08).
- [ ] Dispose of the serial connection on shutdown and provide a graceful way to exit the listening loop.
- [ ] Handle serial disconnection with bounded reconnection attempts and a delay between attempts. Dispose of failed connections; report persistent failures and never replay the previous macro after reconnecting.
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
