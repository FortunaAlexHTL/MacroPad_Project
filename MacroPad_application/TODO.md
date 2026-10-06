# MacroPad application TODOs

Development order: update the companion app's data handling and configuration, develop the [settings app](../MacroPad_settings/TODO.md), then create a full installer for other users.

## 1. Replace the two-dimensional array with a jagged array

- [ ] Change the macro table from `int[,]` to `int[][]` in `Main`, `ReadFile`, and `MacroController`.
- [ ] Allocate each row separately and use `macro.Length` and `macro[i].Length` when processing it.
- [ ] Adapt indexing and copying loops while keeping explicit types, simple helper methods, and the existing coding style. Do not use `break` to exit loops.
- [ ] Decide how variable-length rows will be stored in `settings.ini` and how existing six-field rows with trailing `-1` values will be supported. Keep the settings app and documentation consistent with that decision.
- [ ] Review `MaxLength` and temporary array sizes so longer or shorter rows are handled deliberately.
- [ ] Check keyboard shortcuts, application launches, missing button mappings, and invalid or empty rows after the conversion.

## 2. Read external configuration from files

- [ ] Define a configuration file for machine-specific and user-configurable values, including the Arduino serial port, baud rate, button-mapping file location, and application-target file location.
- [ ] Define a predictable location for the main configuration file so startup can find it without a developer-specific absolute path.
- [ ] Load configuration before opening the serial port or reading button mappings. Replace the hardcoded device path, baud rate, `SettingsFilePath`, and `AppsFilePath` with values read from the file.
- [ ] Review other external inputs and file locations used by both apps, including the settings app's keycode reference, and document where each is configured.
- [ ] Define how relative paths are resolved so starting from an IDE, terminal, or installer-created launcher uses the same files.
- [ ] Validate required values and report missing files, invalid settings, and unavailable serial ports with clear messages.
- [ ] Keep the configured baud rate consistent with the firmware and keep application launch targets in `apps.ini`.
- [ ] Document the configuration format, provide portable example values, and verify that another user can configure the app without editing C# source.

## 3. Work with the settings app

- [ ] Agree on shared file formats and locations with `MacroPad_settings` before implementing its save behavior.
- [ ] Decide how saved changes take effect: restart the companion app or implement an explicit reload mechanism.
- [ ] Verify that settings saved by the editor load correctly and trigger the intended actions in the companion app.

## 4. Final project milestone: full installer

Complete this after both apps work together. Coordinate the setup experience with the [settings app's installer tasks](../MacroPad_settings/TODO.md#3-final-project-milestone-full-installer).

- [ ] Choose the supported macOS versions and processor architectures, the distribution format, and how the .NET runtime will be supplied.
- [ ] Package both apps, the keycode reference, and default configuration files so users do not need the source repository or IDE.
- [ ] Install editable configuration in an appropriate writable location and preserve existing user settings during upgrades.
- [ ] Provide convenient launchers and decide whether running the companion app at login should be an optional installation setting.
- [ ] Prepare distribution signing/notarization as needed and document firmware upload, board connection, and macOS Accessibility setup.
- [ ] Verify installation, first launch, settings changes, button actions, upgrades, and uninstallation on a clean Mac or separate user account.
