# MacroPad settings — remaining work

Reviewed 2026-10-10. Only unfinished work is listed here. The connection-object refactor is complete; begin the editor after agreeing on identity and configuration-validation rules. See the [refactor checkpoint](../REFACTOR_CHECKPOINT_2026-10-10.md). [Completed work and review notes](../COMPLETED_WORK_2026-10-09.md) and the [original TODO backup](../TODO_BACKUP_2026-10-09.md) are in the repository root.

## 1. First milestone: edit one mapping and save it

- [ ] Replace `Hello, World!` with a small console menu; keep a graphical interface as a later decision.
- [ ] Load configuration and macros from the same Application Support location as the companion app, resolving a relative macro path against that directory. Follow the agreed configuration format using the current `Config(BaudRate, MacrosFile)` fields.
- [ ] Select a configured device by name and display its five button mappings.
- [ ] Allow editing one button for the selected device as either a keyboard shortcut or an application launch. Store the selection in memory before saving; allow cancellation.
- [ ] Validate the edited configuration using the companion app's rules and show all detected problems together.
- [ ] Save readable, indented JSON with the agreed property-name casing. Preserve the last valid file if writing fails, and do not overwrite settings after a cancelled edit.
- [ ] Show success/error messages and explain that the companion app currently needs restarting after a save.
- [ ] Verify loading and saving without edits preserves the mappings, then verify one edited shortcut and one application launch on the connected board.

## 2. Complete the editing workflow

- [ ] Decide how the installed app locates its bundled keycode reference and load it. Show readable key names while preserving numeric macOS keycodes; ensure the reference covers every key the editor offers.
- [ ] Support variable-length key sequences and the modifier codes supported by the companion app. Explain that ordinary keys run in sequence under the modifiers.
- [ ] Let users select application targets and save their paths directly in `ApplicationPath`; validate missing targets and paths containing spaces.
- [ ] Handle missing/unreadable files and malformed JSON without destroying the existing configuration. Log read/save failures and allow correction followed by retry.
- [ ] Expose only general settings retained by the companion app. Coordinate default-file creation and preserve existing user files.
- [ ] Use current JSON names (`ArduinoDeviceName`, `ButtonCode`, `ActionType`, `Keys`, `ApplicationPath`); migrate or clearly reject the older lowercase format. Use empty key arrays for application actions and null application paths for keyboard actions.

## 3. Device setup and naming

- [ ] Coordinate port ownership with the companion app so both apps do not try to use the same board simultaneously.
- [ ] Display devices confirmed by the `MACROPAD:<name>` handshake, including their current port paths, and let the user select the board to configure.
- [ ] Send `SET_NAME:<name>` and confirm the matching `OK:<name>` reply before reporting success. Handle `ERROR:` replies, timeouts, disconnects, and button messages arriving between command replies.
- [ ] Enforce the firmware's 1-24 ASCII letters, digits, underscores, or hyphens. Handle `UNNAMED` and duplicate names without identifying boards by discovery order.
- [ ] Verify that a saved name is reported again after unplugging and reconnecting.
- [ ] Save and validate mappings by `(ArduinoDeviceName, ButtonCode)`: equal button codes on different boards are allowed, but duplicate pairs are not. The companion app already uses this lookup; its refactored multiple-board loop is implemented, with basic hardware checks reported passing by the user. Migrate this pairing to persistent IDs together with the companion app.
- [ ] When renaming a board, coordinate updating its saved macro names until ID-based lookup is implemented; handle a failed save or rename without claiming both succeeded.
- [ ] Add setup-time provisioning of a persistent unique ID, separate from the display name. Read an existing ID first; assign and confirm one only when absent. Coordinate EEPROM storage and protocol changes with firmware and the companion app.
- [ ] Once ID-based matching is implemented, allow duplicate display names and distinguish devices in the menu by their IDs. Validate uniqueness by `(DeviceId, ButtonCode)` using the agreed final schema, preserve IDs across renaming, and migrate existing name-based mappings explicitly.

## 4. Installer integration

Use the shared [installer and release plan](../MacroPad_application/TODO.md#8-final-milestone-installer-and-github-release).

- [ ] Package the settings app and keycode reference alongside the companion app so users can launch both without an IDE.
- [ ] Provide first-run setup for a discovered board, initial mappings, application targets, and any retained file-location choices.
- [ ] Coordinate creation of missing default JSON files in `~/Library/Application Support/MacroPad/` without overwriting existing settings or upgrade data.
- [ ] Guide users through Mega firmware preparation, macOS Accessibility permission, and testing the first button.
- [ ] Make the settings tool easy to reopen and explain how to start/restart the companion app and opt into launch at login.
- [ ] Verify installation, editing, cancellation, failed saves, and upgrades under a fresh user account; add the resulting setup instructions to the GitHub Release documentation.
