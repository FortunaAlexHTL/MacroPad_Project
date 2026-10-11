# MacroPad serial protocol

The Mega 2560 sketch is `onHardwareProgram/MacroPad/MacroPad.ino`. Serial communication uses **115200 baud**, with newline-terminated messages. CRLF is also accepted. Close the companion app before using Serial Monitor, since only one process should own a board's port.

## Device IDs

An assigned ID is exactly **six printable ASCII characters** (codes 32–126): letters, digits, punctuation, and spaces. Matching is exact and case-sensitive. Leading/trailing spaces, colons, and leading zeroes are significant. `000000` is reserved for an unconfigured board and cannot be assigned with `SET_ID` or used in macro mappings.

IDs are strings, not numbers. The app uses `Arduino.Id` and JSON `ArduinoDeviceId`. There is no separate device-name field. IDs must be unique across connected boards; the companion app refuses a second active connection with the same ID. The board cannot determine whether another board already uses an ID. The companion already generates A–Z/0–9 IDs for invalid or unconfigured identity replies and waits for matching `OK:<id>` confirmation. It checks collisions against current connections only; excluding the reserved value from generation and checking saved offline devices remain unfinished. The settings app workflow is still pending.

## Commands

| Send | Reply |
| --- | --- |
| `WHO_ARE_YOU?` | `MACROPAD:<id>` (or `MACROPAD:000000` before setup) |
| `SET_ID:Ab1:!?` | `OK:Ab1:!?` |
| `SET_ID:000001` | `OK:000001` |
| `SET_ID:abc` | `ERROR:INVALID_ID` |
| `SET_ID:000000` | `ERROR:INVALID_ID` |
| `SET_NAME:DeskPad` | Unsupported legacy command; may produce `ERROR:COMMAND_TOO_LONG` because it exceeds the buffer |
| `HELLO` | `ERROR:UNKNOWN_COMMAND` |

`SET_ID:` is **seven characters**, so the firmware compares seven characters and starts the payload at offset seven. The maximum payload line is 13 characters: seven for the command and six for the ID. Longer lines produce `ERROR:COMMAND_TOO_LONG`; the rest of that line is discarded. Blank lines are ignored. IDs cannot contain control characters, newline, or non-ASCII characters.

The companion app splits replies at the **first colon only**, and removes CRLF without trimming spaces. For example, `MACROPAD: A:b! ` contains the six-character ID ` A:b! `, including both spaces. Preserve those spaces in JSON and setup commands. Quote and backslash characters must be escaped using normal JSON rules in configuration files.

Button messages remain `10`, `20`, `30`, `40`, and `50`. During discovery, unrelated complete lines are skipped while waiting for a valid identity. The controller allows two seconds for startup and then two seconds for identification; it advances these stages from the main loop rather than sleeping through them. Assignment has its own two-second confirmation deadline. Opening and writing are still synchronous operations; the assignment write does not yet have the same error handling as the identification write. A successful probe is closed before the listening connection is opened, which can reset the board again.

## EEPROM migration

The current ID save marker is 166 at address 0. Addresses 1–7 store the six-character ID and a terminating zero. This overlaps the former name area; saving an ID does not preserve that old record. Other EEPROM bytes are not erased. IDs previously stored at addresses 32–39 are not read or migrated by this sketch. Back up any needed identity/mapping information before changing layouts and verify the reported ID after uploading.

Saving clears the marker first, writes changed bytes with `EEPROM.update`, and writes the marker last. Missing or invalid stored data leaves the default `000000`. The firmware itself does not generate IDs; the companion can assign one during discovery.

## Manual setup and verification

1. Upload the sketch for Arduino Mega 2560 when ready; this documentation update does not upload it.
2. Open Serial Monitor at 115200 baud with Newline, and allow about two seconds for startup.
3. Send `WHO_ARE_YOU?`, then `SET_ID:000001` for the first board. Choose a different ID for every additional board.
4. Confirm `OK:000001`, unplug/reconnect, then confirm `WHO_ARE_YOU?` returns `MACROPAD:000001`.
5. In the existing `~/Library/Application Support/MacroPad/macros.json`, replace legacy `ArduinoDeviceName` fields with `ArduinoDeviceId` using the board's actual assigned ID. Do not just rename the property while leaving an old name as its value.
6. Close Serial Monitor, start the companion app, and test configured buttons. The bundled example mappings target `000001`; recovery may replace invalid user files. Automatic assignment of another ID creates empty presets that still need editing.

For old configurations, make a backup before editing. Invalid/missing IDs fail macro validation and can trigger backup/default recovery instead of silently matching a name. The settings app's setup workflow and a general automatic JSON migration are not implemented yet.
