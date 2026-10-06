# MacroPad settings TODOs

Begin settings-app development after the [companion app's jagged-array and configuration changes](../MacroPad_application/TODO.md). Finish the project with a full installer that makes both apps easy for other people to install and use.

## 1. Start development of the settings app

- [ ] Replace the `Hello, World!` placeholder with an initial settings workflow; decide whether the first interface will be a console menu or a graphical app.
- [ ] Keep the existing C# coding style: explicit types, arrays, simple loops, and small helper methods. Do not use `break` to exit loops.
- [ ] Load the companion app's configuration, button mappings, and application targets using the agreed file locations and formats.
- [ ] Read the keycode reference from its configured file location and show readable key names when editing shortcuts.
- [ ] Display the current action assigned to each of the five buttons.
- [ ] Allow users to choose a keyboard shortcut or an application launch for each button.
- [ ] Allow users to edit key sequences and modifiers, using the lengths and modifier codes supported by the companion app.
- [ ] Allow users to select application targets and maintain the corresponding identifiers in `apps.ini`.
- [ ] Allow users to configure the Arduino port, baud rate, and external file locations without changing source code.

## 2. Save settings and verify integration

- [ ] Validate input before saving, including button codes, action types, keycodes, application identifiers, file paths, and serial settings.
- [ ] Save files in the format supported by the companion app's jagged-array reader, including the agreed treatment of trailing `-1` values.
- [ ] Preserve existing valid settings when an edit is cancelled or saving fails; provide clear success and error messages.
- [ ] Explain when saved settings take effect and provide the agreed restart or reload workflow.
- [ ] Verify that loading and saving unchanged settings preserves their meaning.
- [ ] Check an edited keyboard shortcut and application launch end to end with the companion app and connected board.

## 3. Final project milestone: full installer

This is the same shared installer milestone tracked in the [companion app TODOs](../MacroPad_application/TODO.md#4-final-project-milestone-full-installer).

- [ ] Include the settings app and its keycode reference in the installer alongside the companion app.
- [ ] Provide first-run setup for the Arduino connection, file locations, initial button assignments, and application targets.
- [ ] Create initial configuration files when needed, without overwriting an existing user's settings.
- [ ] Guide users through connecting the board, preparing its firmware, granting macOS Accessibility permission, and testing their first button.
- [ ] Make it easy to reopen the settings app after installation and explain how to start the companion app.
- [ ] Verify that a new user can install, configure, and use the MacroPad without editing source files or opening an IDE for either C# app.
