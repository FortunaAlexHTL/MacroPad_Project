# Completed work and project review — 2026-10-09

For the current connection architecture and completed refactoring, see the [October 10 checkpoint](REFACTOR_CHECKPOINT_2026-10-10.md). The earlier findings below are historical.

This document preserves implemented work for a future README and records review findings without changing source code. The [original roadmaps](TODO_BACKUP_2026-10-09.md) are backed up separately, including their historical completed checkboxes. Active remaining work is in the [application TODO](MacroPad_application/TODO.md) and [settings TODO](MacroPad_settings/TODO.md).

“Implemented” below means present in the reviewed source. Build results, isolated checks, and user-reported hardware behavior are identified separately; none establishes that every failure case is handled.

## Implemented project structure

| Component | Current role |
| --- | --- |
| `MacroPad_application/Program.cs` | Startup configuration, discovery, opening connections, listening, and rediscovery after reported read failures |
| `ConfigurationReader.cs` | Reads JSON into `Config` and `Macro[]` using `System.Text.Json` |
| `Models/Config.cs` | General configuration record |
| `Models/Macro.cs` | One macro record with its own variable-length key array |
| `Models/Arduino.cs` | Discovered port and device-name record |
| `Controllers/MacroPadController.cs` | Finds the first macro matching a button code |
| `Controllers/MacController.cs` | Posts macOS keyboard events or starts an application through `open` |
| `MacroPad_settings/Program.cs` | Console-project scaffold; prints `Hello, World!` |
| `MacroPad_settings/keycodes.ini` | Semicolon-separated key-name/keycode reference; not yet read by either app |
| `onHardwareProgram/MacroPad/MacroPad.ino` | Mega button input, serial command handling, and persistent device naming |
| `onHardwareProgram/SerialProtocol.md` | Protocol explanation and manual-test instructions; C# integration status needs updating |
| `demoVideo/MacroPad_Demo.MOV` | Existing demonstration video, preserved |

Both C# projects target `net10.0` with nullable checking and implicit usings. The companion app references `System.IO.Ports` version `10.0.12`; the settings project has no package references. The solution groups both projects, with the filename-casing issue recorded below.

## Completed macro and configuration work

- Replaced fixed-width semicolon mapping rows and `-1` padding with JSON records. The earlier jagged-array idea was superseded by `Macro[]`, where each record contains its own `int[]`.
- Defined the exact macro properties `buttonCode`, `ActionType`, `keys`, and `ApplicationPath`. JSON property casing should be preserved while the two apps share this format.
- Defined `Config(SerialPort, BaudRate, MacrosFile)` and `Arduino(Port, Name)` records. `SerialPort` remains in the configuration model but discovery now supplies the actual port.
- Implemented `ReadConfig` and `ReadMacros`, including rejection of a whole JSON document that deserializes to null. Individual values and array entries are not yet validated.
- Resolved the current user's home directory through `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)`.
- Created and used `~/Library/Application Support/MacroPad/` for `config.json`. `Path.Combine` resolves a relative `MacrosFile` there; an absolute macro path remains absolute.
- Loaded configuration and macros once at startup. The application does not currently reload files while running.
- Implemented first-match macro lookup, nullable results for an unknown code, and direct passing of the selected record to the action controller.
- Used `int.TryParse` for button messages. Invalid numeric messages are reported, and an unknown button code does not execute a macro.
- Stored application targets directly in macro records, replacing the separate application-identifier table.

Local configuration was inspected read-only on 2026-10-09: both files exist, baud is `115200`, five mappings load, and the configured application target exists. The local keyboard arrays are `[55, 8]`, `[55, 9]`, `[74]`, and `[72]`; these are observed local settings, not shipped repository defaults. No JSON defaults are currently tracked in the repository.

## Completed macOS action work

- Implemented separate `Keyboard` and `Application` branches.
- Recognized left/right Command (`55`/`54`), Shift (`56`/`60`), Option (`58`/`61`), and Control (`59`/`62`). Other reference entries are not automatically treated as modifiers.
- Pressed modifiers first, pressed/released ordinary keys in sequence with modifier flags, and released modifiers in reverse order on the normal path.
- Added native event creation, flag setting, and posting through CoreGraphics interop.
- Checked native event creation for a zero handle and released each successfully created event with `CFRelease` inside `finally`.
- Launched configured applications with macOS `open -a` and reported a blank application path. Launch-result checking is still missing.
- Added XML documentation to C# helper methods, preserving the convention of no XML summaries on `Main`, classes, or fields; Arduino functions have explanatory comments.

Native event memory ownership is handled in the current helpers. This does not guarantee held-key cleanup after a partially failed macro, Accessibility permission, or successful application launching. The user's observation of managed memory dropping after startup is not a native-memory leak test.

## Completed discovery and recovery foundations

- Enumerated serial ports and filtered `/dev/cu.usbmodem` candidates for the current macOS/Mega setup.
- Opened each candidate using configured baud, used two-second read/write timeouts, allowed two seconds for startup, and sent `WHO_ARE_YOU?` with a newline.
- Used `Trim()` and `Split(':')` to inspect one reply, requiring exactly two fields, the `MACROPAD` prefix, and a nonblank device name.
- Collected matches in a temporary array, then copied only populated entries to the returned `Arduino[]`.
- Reported discovered names. Names are descriptive labels, not enforced unique device identifiers.
- Used `using SerialPort` in discovery so each candidate is disposed on success or exception. Caught timeout, access, and I/O errors while probing.
- Avoided indexing the first port when discovery returns no devices. Continued searching instead of terminating startup.
- Added catches for access/I/O errors when reopening listening connections and I/O/closed-port errors around `ReadLine()`.
- Kept macro execution outside the serial-read catch and skipped it when no line was read.
- Returned to discovery after reported read failure and attempted to dispose previous connections before rescanning.

These are foundations, not a completed reconnection feature. `Main` opens all matches but reads only the first. The listening flag and partial cleanup defects below remain. The normal listening port has the default infinite read timeout; the discovery timeout does not transfer to the new object.

## Completed Mega 2560 firmware work

| Button | Pin | Sent code |
| --- | --- | --- |
| 1 | 6 | `10` |
| 2 | 5 | `20` |
| 3 | 4 | `30` |
| 4 | 3 | `40` |
| 5 | 2 | `50` |

- Configured `INPUT_PULLUP`; each button connects its input pin to ground.
- Sent newline-terminated decimal codes at `115200` baud on HIGH-to-LOW transitions. Holding a button does not intentionally repeat; mechanical bounce is not filtered.
- Added a bounded character buffer for serial commands. One available character is read per loop iteration so a partial incoming command does not make the code wait for its remainder.
- Used newline as the command terminator, ignored carriage returns, ignored empty lines, and discarded overlong lines before accepting the next command.
- Implemented `WHO_ARE_YOU?` → `MACROPAD:<name>`.
- Implemented `SET_NAME:<name>` → `OK:<name>` for valid names, with `ERROR:INVALID_NAME`, `ERROR:UNKNOWN_COMMAND`, and `ERROR:COMMAND_TOO_LONG` responses as applicable.
- Limited names to 1-24 ASCII letters, digits, underscores, or hyphens. The maximum command payload is 33 characters (`SET_NAME:` plus a 24-character name).
- Loaded names from EEPROM at startup with an `UNNAMED` fallback. Address 0 holds the completed-save marker; addresses 1-25 hold the name and ending zero.
- Saved changed names with `EEPROM.update`, clearing the marker before writing and setting it after the completed save. Repeating the current name does not trigger another save.

Naming changes the identity returned by the firmware, not the macOS `/dev/cu...` path. Every board can use the same firmware. A settings-side rename interface and duplicate-name policy are still pending.

## Verification evidence

Checks performed on the reviewed 2026-10-09 sources during this conversation:

- Companion project build: passed, zero warnings and errors.
- Settings project build: passed, zero warnings and errors. This verifies the scaffold only.
- Mega 2560 firmware compilation (`arduino:avr:mega:cpu=atmega2560`): passed; 3,898 bytes of program storage and 371 bytes of global dynamic memory reported.
- An isolated temporary C# check loaded the user's actual configuration and verified selection of all five records, unknown-code lookup, and empty-array lookup. It did not execute macros or open serial ports.
- The same isolated check confirmed missing validation: `{}` is accepted as a `Config` with baud zero and null macro path; a keyboard macro without `keys` is accepted; `[null]` loads and causes `NullReferenceException` during lookup. Malformed JSON raises `JsonException`, which startup does not yet handle.
- The user previously reported the basic connected single-board flow working on 2026-10-08. This is user-reported hardware evidence, not a test of every recovery path.

No firmware upload, live serial session, keyboard injection, or application launch was performed for this review. Reconnection, debounce, EEPROM persistence on the physical board, permissions, and installation still need the checks in the active roadmaps. No permanent test framework was introduced.

## Review findings retained for future work

| Priority | Location | Finding and consequence |
| --- | --- | --- |
| First | `Program.Main`, after the opening catch | `arduino.Length > 0` sets `isListening` back to true even if opening failed. Array allocation is not evidence of a successful connection. |
| First | `Program.Main`, cleanup/opening loops | A partial failure can leave null entries; unconditional `Dispose()` then throws. Already opened ports also need deterministic cleanup on shutdown. |
| Next | `Program.Main`, selection | All discovered ports are opened but only index zero is read. Other boards are occupied without their messages being handled. |
| Next | `Program.GetArduinos` | Only one reply is read, so a button message can hide a valid identification reply. Names receive structural checks but not the firmware's full validation. |
| Next | `Program.GetArduinos`, status/timing | “Connection successful” is printed after probe connections close and before listening ports reopen. A trailing two-second delay now exists on every scan; the old TODO saying opening retries have no delay was stale. That wait does not cover a reset caused by the later reopen. |
| Next | `ConfigurationReader` and startup | Missing/default/null fields are not validated; local files make this checkout usable, but a clean installation has no default JSON to load. |
| Before wider use | `MacController.ExecuteMacro` | A failure after pressing modifiers can leave keys held; unsupported actions are silent; `open` results and its process object are not handled. |
| Before settings integration | Settings project | The project is still a scaffold; loading, editing, validation, saving, and device setup have not been implemented. |
| Before clean-checkout documentation | `MacroPad.slnx` | The settings project reference uses `MacroPad_settings.csproj`, while the tracked file is `MacroPad_Settings.csproj`. Current filesystem tolerance does not make those spellings portable. |
| Before release | README and protocol guide | README describes removed INI files, old paths, and outdated behavior. The protocol guide still treats C# discovery as future work. Their instructions must be updated before use as setup documentation. |

## Material to carry into the future README

Use the implemented-feature sections above for the project description, architecture, wiring, protocol, and data model. Use the active TODOs for limitations rather than presenting planned features as complete. Keep a clear distinction between the local five-mapping example and future distributable defaults.

The existing `.gitignore` excludes build output, IDE state, temporary files, and local `AGENTS.md`. Preserve the demonstration video and attribution. The installer remains a future shared `.pkg`/GitHub Release milestone; there is no settings editor or installer to advertise yet.


## Follow-up: controller separation and multiple-board implementation

This follow-up supersedes the earlier single-board source description; the original review above is retained as a historical snapshot.

- Discovery now lives in `ArduinoController.GetArduinos`; `MacController` has been renamed `MacOSController`; configuration-directory resolution moved to `ConfigurationReader`.
- `Config` now contains only `BaudRate` and `MacrosFile`.
- `Macro` now uses `ArduinoDeviceName`, `ButtonCode`, `ActionType`, `Keys`, and `ApplicationPath`. `FindMacro` matches both device name and button code.
- Connections are allocated unconditionally, opened independently with a per-board catch, and failed objects are disposed. Cleanup skips null entries. Successful opens report the board name.
- The reader now visits every connection, sets a 500 ms timeout, and passes each board's name into macro lookup. This is implemented polling, not yet reliable multiple-board execution: stale data can cross board boundaries, the last opening result controls global listening, malformed nonnumeric messages can throw, and one failed board restarts the whole group.
- Discovery includes up to five attempts for unrecognized replies. Exceptions still exit those attempts, and every attempt reopens the port. Graceful cancellation and a single-connection identity-read deadline have not been implemented despite earlier checklist marks.
- The current companion project builds with zero warnings/errors. No live hardware session or macro execution was performed for this follow-up.

Remaining fixes are ordered in the refreshed TODO files, beginning with resetting received data separately for each board.
