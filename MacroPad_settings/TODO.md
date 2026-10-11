# MacroPad settings — remaining work

Reviewed 2026-10-11. The settings project remains a console scaffold. The companion now uses `ArduinoDeviceId`, not names. See [the ID migration review](../ID_MIGRATION_2026-10-11.md) and [protocol](../onHardwareProgram/SerialProtocol.md).

## 1. Device setup

- [ ] Coordinate exclusive port ownership with the companion app during setup.
- [ ] Identify boards through `WHO_ARE_YOU?` / `MACROPAD:<id>`; show ID and port, preserving all six characters exactly. `000000` means setup is required.
- [ ] Coordinate with the companion’s existing automatic A–Z/0–9 ID assignment instead of creating competing setup flows. Exclude `000000` and check stored IDs as well as connected boards; preserve an assigned ID unless deliberately changed. Continue accepting six printable ASCII characters.
- [ ] Send `SET_ID:<id>`, parse the first colon only, and confirm matching `OK:<id>`. Handle error replies, timeouts, disconnects, and intervening button messages.
- [ ] Verify EEPROM persistence and update stored macro IDs deliberately when reassigning a device. No separate naming workflow is planned.

## 2. Edit one mapping and save safely

- [ ] Replace Hello World with a small console menu; leave a GUI for a later decision.
- [ ] Load `config.json` (`BaudRate`) and `macros.json` from the same Application Support directory as the companion app.
- [ ] Select a device by ID and display its five button mappings. Edit keyboard or application actions in memory and allow cancellation.
- [ ] Use exact JSON fields `ArduinoDeviceId`, `ButtonCode`, `ActionType`, `Keys`, and `ApplicationPath`. Escape quotes/backslashes normally; preserve significant spaces and leading zeroes in IDs.
- [ ] Validate ID/button uniqueness and all action fields consistently with the companion app. Provide migration guidance for legacy name-based mappings; never invent their new IDs silently.
- [ ] Save validated, indented JSON through a temporary file and replacement, handling I/O and permission failures while preserving the previous valid file. Explain that the companion app must restart after edits.
- [ ] Verify unchanged round trips and one edited shortcut/application action end to end; test cancellation and failed saves.

## 3. Complete the workflow

- [ ] Connect the companion’s startup `NeedsSetup` flow and new-device preset flow to the editor. Keep setup-required, failed loading, and successful loading distinct; an empty preset is not finished configuration.

- [ ] Load the bundled keycode reference, display readable keys, and retain numeric macOS keycodes.
- [ ] Support variable-length shortcuts and left/right modifiers; explain sequential ordinary-key behavior.
- [ ] Select and validate application paths including paths with spaces.
- [ ] Handle unreadable/corrupt files and defaults safely; coordinate file creation with the companion and preserve existing settings.

## 4. Installer integration

Follow the [shared release plan](../MacroPad_application/TODO.md#7-installer-and-github-release).

- [ ] Package the editor, keycode reference, and defaults with the companion; support first-run ID provisioning and mappings.
- [ ] Guide firmware upload, Accessibility setup, and first-button testing.
- [ ] Make settings easy to reopen; explain companion restart and optional login launch.
- [ ] Test installation, editing, cancellation, failed saves, and upgrades under a fresh account.
