# MacroPad application TODOs

Development order: finish general JSON configuration, restore Arduino input and improve error handling, develop the [settings app](../MacroPad_settings/TODO.md), then package both apps for a GitHub Release.

## 1. Macro records and JSON loading

The jagged-array exercise has been superseded by `Macro[]` records. Each macro contains its own variable-length `int[]` of keys. No fixed-width rows or `-1` padding are needed.

- [x] Define the `Macro` record in `Models/Macro.cs`.
- [x] Store the five button mappings in `Configuration/macros.json` and deserialize them through `ConfigurationReader`.
- [x] Select the first matching record with `MacroPadController.FindMacro`, returning `null` when no match exists.
- [x] Pass the selected record directly to `MacController.ExecuteMacro`.
- [x] Implement separate keyboard and application branches, including a check for a blank application path.
- [x] Store `ApplicationPath` directly in each application macro, replacing the separate application-identifier lookup.
- [x] Build the companion app and verify macro selection for known codes, unknown codes, and an empty array.

Current execution is a single action using a fixed button code in `Main`. Application launching is implemented, but a successful build is not an end-to-end hardware or launch test.

## 2. Next milestone: general configuration

- [x] Create `Configuration/config.json` with `SerialPort`, `BaudRate`, and `MacrosFile`.
- [ ] Define an `AppConfiguration` record in `Models/AppConfiguration.cs` matching those JSON properties.
- [ ] Add `ReadConfiguration(string path)` to `ConfigurationReader` and return the deserialized record.
- [ ] Define one predictable starting location for `config.json`. Resolve `MacrosFile` relative to the configuration file's directory, rather than the current working directory.
- [ ] Load general configuration first in `Main`, then load macros through its configured path. Replace the direct `../../../Configuration/macros.json` dependency.
- [ ] Validate a nonblank serial port, an appropriate baud rate, and an existing macro-file location. Keep the baud rate consistent with the firmware's `115200` setting.
- [ ] Handle missing files and invalid JSON with clear messages before starting normal operation.
- [ ] Print and verify the loaded settings without sending keyboard events or opening applications.

Application paths already belong to individual macros; do not add an `ApplicationsFile` setting or recreate `apps.ini` for the current design.

## 3. Arduino input and reliable execution

- [ ] Validate loaded macros: reject null entries, duplicate button codes, unsupported action types, missing keyboard arrays, invalid keycodes, and missing application paths. Validate the full key sequence before pressing modifiers.
- [ ] Report unsupported action types explicitly in `ExecuteMacro`.
- [x] Check native keyboard-event creation for failure and release created events with `CFRelease` after use, including on error paths.
- [ ] Ensure modifiers are released if execution fails after pressing them.
- [ ] Use `ProcessStartInfo.ArgumentList` for configured application paths and check the `open` process's result so failed launches are reported.
- [ ] Restore the serial connection using the configured port and baud rate. Read a new message before executing each macro; do not repeat a fixed test action in an unrestricted loop.
- [ ] Parse incoming button codes safely and handle malformed messages and unknown codes without terminating the app.
- [ ] Dispose of the serial connection on shutdown and handle disconnection; decide how reconnection should work.
- [ ] Verify keyboard shortcuts and application launches with the connected board, including paths containing spaces and unavailable applications.
- [ ] Add firmware debounce so one physical press reliably produces one event.
- [ ] Update README setup instructions and examples to reflect JSON, records, direct application paths, and the restored serial flow.

## 4. Work with the settings app

- [ ] Keep `config.json` and `macros.json` formats consistent between the two apps, including property-name casing and validation rules.
- [ ] Define how the settings app locates its keycode reference; it currently remains in `MacroPad_settings/keycodes.ini`.
- [ ] Decide how saved changes take effect: restart the companion app or implement an explicit reload mechanism.
- [ ] Verify that settings saved by the editor load correctly and trigger the intended actions in the companion app.

## 5. Final project milestone: installer and GitHub Release

Complete this after both apps work together. Coordinate the setup experience with the [settings app's installer tasks](../MacroPad_settings/TODO.md#3-final-project-milestone-full-installer).

- [ ] Choose supported macOS versions and publish self-contained builds for Apple Silicon (`osx-arm64`) and Intel (`osx-x64`) if supporting both.
- [ ] Package both apps as launchable Mac applications, including the runtime, keycode reference, and default JSON files.
- [ ] Build a native `.pkg` installer using macOS packaging tools rather than a custom installer interface.
- [ ] On first launch, create editable configuration under `~/Library/Application Support/MacroPad/` for both apps to use. Preserve existing user settings during upgrades.
- [ ] Provide first-run setup in the settings app and make launch at login an explicit user choice.
- [ ] Sign the applications and installer, notarize the distribution, and verify the downloaded installation experience.
- [ ] Verify installation, first launch, settings changes, button actions, upgrades, and uninstallation on a clean Mac or separate user account.
- [ ] Publish the tested packages as GitHub Release assets with release notes, architecture labels, firmware instructions, and Accessibility setup guidance.
