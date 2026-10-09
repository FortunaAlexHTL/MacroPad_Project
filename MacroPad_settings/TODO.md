# MacroPad settings — remaining work

Reviewed 2026-10-09. Only unfinished work is listed here. Begin after the companion app's connection and configuration-validation fixes. [Completed work and review notes](../COMPLETED_WORK_2026-10-09.md) and the [original TODO backup](../TODO_BACKUP_2026-10-09.md) are in the repository root.

## 1. First milestone: edit one mapping and save it

- [ ] Replace `Hello, World!` with a small console menu; keep a graphical interface as a later decision.
- [ ] Load configuration and macros from the same Application Support location as the companion app, resolving a relative macro path against that directory. Follow the agreed configuration format rather than exposing the unused serial-port setting.
- [ ] Display the action assigned to each of the five buttons.
- [ ] Allow editing one button as either a keyboard shortcut or an application launch. Store the selection in memory before saving; allow cancellation.
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
- [ ] Keep current JSON names (`buttonCode`, `ActionType`, `keys`, `ApplicationPath`) unless both apps and examples are intentionally migrated together. Use empty key arrays for application actions and null application paths for keyboard actions.

## 3. Device setup and naming

- [ ] Coordinate port ownership with the companion app so both apps do not try to use the same board simultaneously.
- [ ] Display devices confirmed by the `MACROPAD:<name>` handshake, including their current port paths, and let the user select the board to configure.
- [ ] Send `SET_NAME:<name>` and confirm the matching `OK:<name>` reply before reporting success. Handle `ERROR:` replies, timeouts, disconnects, and button messages arriving between command replies.
- [ ] Enforce the firmware's 1-24 ASCII letters, digits, underscores, or hyphens. Handle `UNNAMED` and duplicate names without identifying boards by discovery order.
- [ ] Verify that a saved name is reported again after unplugging and reconnecting.
- [ ] Add per-board macro profiles only after the companion app defines device identity, profile association, and multiple-board listening.

## 4. Installer integration

Use the shared [installer and release plan](../MacroPad_application/TODO.md#8-final-milestone-installer-and-github-release).

- [ ] Package the settings app and keycode reference alongside the companion app so users can launch both without an IDE.
- [ ] Provide first-run setup for a discovered board, initial mappings, application targets, and any retained file-location choices.
- [ ] Coordinate creation of missing default JSON files in `~/Library/Application Support/MacroPad/` without overwriting existing settings or upgrade data.
- [ ] Guide users through Mega firmware preparation, macOS Accessibility permission, and testing the first button.
- [ ] Make the settings tool easy to reopen and explain how to start/restart the companion app and opt into launch at login.
- [ ] Verify installation, editing, cancellation, failed saves, and upgrades under a fresh user account; add the resulting setup instructions to the GitHub Release documentation.
