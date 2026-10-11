# ID migration and development review — 2026-10-11

## Contract

The project now identifies a board by exactly six printable ASCII characters (32–126), held as a string. Spaces, punctuation, colons, case, and leading zeroes are significant. `000000` is the reserved unconfigured marker. There is no separate display-name field. The companion records use `Arduino.Id` and `Macro.ArduinoDeviceId`; matching uses the exact ID plus button code.

The firmware command is `SET_ID:<id>`, acknowledged by `OK:<id>`. Identification stays `WHO_ARE_YOU?` → `MACROPAD:<id>`. The command prefix has seven characters, fixing the unfinished nine-character comparison/offset inherited from SET_NAME. Both host parsers split at the first colon and preserve spaces rather than trimming the ID.

EEPROM marker 166 at address 32 identifies the new layout; addresses 33–39 hold the ID and terminating zero. Old name storage at addresses 0–25 remains untouched and is not used as an ID. Unconfigured or invalid IDs are not accepted as macro targets. A duplicate ID on another active port is rejected before opening that listening connection. IDs are not automatically generated or globally guaranteed unique yet.

## Existing user work preserved

- Staged startup/identity discovery, candidate batches, adding connections, and removal of inactive connections replaced the old blocking discovery helpers.
- `ArduinoConnection` moved into `Connections` with its namespace updated.
- Configuration now contains only BaudRate. Fixed `config.json` and `macros.json` names are used under Application Support. Missing files are copied from templates bundled in build and publish output; existing files are preserved.
- The user's Arduino button-array implementation and 100 ms press cooldown remain unchanged. This is not the same as requiring an input to be stable for a debounce period.
- The user's temporary virtual-device support remains in place, explicitly marked for removal or gating before release.

## Assistant changes in this review

Finished the ID command/storage migration, added consistent six-printable-character validation, rejected duplicate active device IDs, preserved punctuation/spaces in serial parsing, and finished ID parameter names and diagnostics. Invalid or missing IDs in JSON now stop startup with migration guidance instead of silently failing macro lookup; legacy files are not overwritten. A startup null check makes configuration failure explicit and removes nullable warnings.

The template macros now target the example assigned ID `000001`; their button actions and application path were preserved. README, protocol instructions, and the two remaining-work roadmaps were updated. Historical records remain historical, with a correction to the October 10 physical-test claim.

## Review findings still requiring work

- `AreFilesReadable` tests file access using an overload that requests read/write access, not just read access. Only missing files are caught there; malformed JSON, access errors, and other file failures still need explicit handling.
- If macros disappear after the existence check, `ReadMacros` can catch FileNotFoundException and return an empty array while the caller reports success. This is recorded for the next configuration lesson, not silently refactored here.
- Baud rate, duplicate macro pairs, keys, actions, and targets still need fuller validation. The ID checks added here are not general validation.
- Discovery/listening buffers remain unbounded. Discovery waits use elapsed time, but serial open/write calls are synchronous. Probe-close/listener-open can restart a board and discard remaining probe data.
- Graceful cancellation, pending-discovery cleanup on exit, action-error recovery, the settings editor, and installer/release validation remain pending.
- Six spaces is a valid printable ID, although setup-generated IDs should favor easy-to-read characters. JSON escaping is required for quote/backslash characters.

## Verification

- 150 C# assertions passed using the current linked source with a simulated serial API and a macOS-action stub. Covered all ASCII validation boundaries, reserved/short/long/non-ASCII IDs, fragmented identity with a colon and boundary spaces, listening-message preservation, Program identity routing, duplicate-ID refusal without opening a second port, per-ID macro selection, invalid/legacy JSON rejection without overwrite, and loading all five defaults.
- 145 host C++ assertions passed against the actual sketch with Serial/EEPROM stubs. Covered the printable range, command parsing and responses, CRLF, boundary spaces and colons, six spaces, reserved/short/overlong IDs, overlong-line recovery, save/reload, avoiding repeated writes for an unchanged ID, preserving legacy name bytes, and rejecting unterminated saved data.
- Arduino Mega 2560 compilation passed using the installed AVR core: 3,812 bytes flash and 375 bytes global RAM. This is a compile, not a firmware upload or physical EEPROM test.
- Both C# projects build with zero warnings and errors; the companion build was repeated after adding the startup guard. Final diff whitespace checks, XML documentation parsing, and updated documentation-link checks passed.
- Temporary harnesses live outside the repository. No real serial port was opened, no macOS action executed, no firmware uploaded, and no installed user configuration changed during this review.

## Corrected hardware evidence

The user has one physical board and reports successful startup, button operation, unplugging, and reconnection. Physical multiple-board testing remains unverified; multiple-board behavior has been simulated. Earlier wording implying physical two-board testing was incorrect.

Button 5's missing presses were resolved by fixing a loose breadboard connection, as reported by the user. That issue is no longer a pending software investigation.

## Next use

When ready to deploy, upload the updated sketch, close the companion app, and assign an ID in Serial Monitor, for example `SET_ID:000001`. Confirm it survives reconnecting. Back up and update the existing Application Support macros to use `ArduinoDeviceId` with that exact ID, then close Serial Monitor and restart the companion. Existing files are intentionally not replaced by updated bundled defaults. See [the protocol guide](onHardwareProgram/SerialProtocol.md).

Resume mentoring with setup-time ID generation/provisioning or the configuration error cases above, one small step at a time. Code changes should again be written by the user unless explicitly delegated. Do not treat this migration as a completed settings app or release.
