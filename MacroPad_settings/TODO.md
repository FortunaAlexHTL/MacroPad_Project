# MacroPad settings TODOs

Begin settings-app development after the [companion app's JSON configuration and Arduino integration](../MacroPad_application/TODO.md). The settings app currently remains a `Hello, World!` placeholder. Finish the project with a shared macOS installer distributed through GitHub Releases.

The agreed data model uses `config.json` for `SerialPort`, `BaudRate`, and `MacrosFile`, plus `macros.json` for `Macro` records. Each macro contains its button code, action type, variable-length keys, and nullable `ApplicationPath`. Application paths are stored directly in macros; no application-identifier table or `-1` padding is needed.

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
- [ ] List available serial ports and let the user choose the Arduino port rather than assuming the first port is correct.
- [ ] Allow users to configure the port, baud rate, and macro-file location without changing source code.

## 2. Save settings and verify integration

- [ ] Validate input before saving, including unique button codes, supported action types, keycodes, required application paths, file locations, and serial settings.
- [ ] Serialize records back to readable, indented JSON. Preserve the agreed property names, use `null` for unused application paths, and use an empty key array for application actions.
- [ ] Preserve existing valid settings when an edit is cancelled or saving fails; provide clear success and error messages.
- [ ] Explain when saved settings take effect and provide the agreed restart or reload workflow.
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
