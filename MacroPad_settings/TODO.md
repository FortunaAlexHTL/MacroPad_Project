# MacroPad settings TODOs

The [companion app](../MacroPad_application/TODO.md) loads JSON from Application Support and discovers MacroPads by handshake; the user reports single-board operation working. Complete its single-board connection checks and configuration validation before developing the settings editor. As of 2026-10-08, the settings app remains a `Hello, World!` placeholder; no editor features below are complete. Finish the project with a shared macOS installer distributed through GitHub Releases.

The implemented data model uses a `Config` record for `SerialPort`, `BaudRate`, and `MacrosFile`, plus `Macro` records with the JSON property names `buttonCode`, `ActionType`, `keys`, and `ApplicationPath`. Application paths are stored directly in macros; no application-identifier table or `-1` padding is needed. The companion app's `ReadConfig` and `ReadMacros` methods deserialize these files, and `Main` resolves `MacrosFile` relative to the configuration file's directory. Validation of the deserialized values remains unfinished. `Config.SerialPort` is currently unused because discovery supplies the port; settle the configuration format before building an editor for it. Both apps should use `~/Library/Application Support/MacroPad/`. The companion app creates that directory but does not yet supply missing JSON files.

`Arduino(Port, Name)` records hold discovered devices. Mega firmware supports `WHO_ARE_YOU?` and `SET_NAME:` with EEPROM-backed names; the C# discovery code reads names, but there is no settings-side rename workflow yet.

## 1. Start development of the settings app

- [ ] Replace the `Hello, World!` placeholder with an initial settings workflow; decide whether the first interface will be a console menu or a graphical app.
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
