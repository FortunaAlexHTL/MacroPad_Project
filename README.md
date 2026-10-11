# MacroPad

Five Arduino buttons trigger keyboard shortcuts or application launches on macOS through a C#/.NET 10 companion app.

[Demo](demoVideo/MacroPad_Demo.MOV) · [Firmware](onHardwareProgram/MacroPad/MacroPad.ino) · [Serial protocol and ID setup](onHardwareProgram/SerialProtocol.md) · [Example macros](MacroPad_application/Configuration/macros.json)

## Wiring and firmware

Use an Arduino Mega 2560 with each momentary button connected between its input pin and GND. Firmware enables `INPUT_PULLUP` and sends newline-terminated button codes at **115200 baud**.

| Button | Pin | Code |
| --- | --- | --- |
| 1 | 6 | 10 |
| 2 | 5 | 20 |
| 3 | 4 | 30 |
| 4 | 3 | 40 |
| 5 | 2 | 50 |

Upload `onHardwareProgram/MacroPad/MacroPad.ino` using the Mega 2560 board target. The current sketch uses a 100 ms cooldown after each accepted press edge. This reduces repeated presses from bounce but can also suppress rapid intentional presses; it is not a stable-state debounce algorithm.

## Assign a device ID

Each board uses a six-character printable ASCII ID, not a device name. Letters, digits, punctuation, and spaces are allowed; case and spaces matter. `000000` means unconfigured and cannot be used for macros. IDs must differ between boards.

Close the companion app and open Arduino Serial Monitor at 115200 baud with Newline. Wait for startup, then send `SET_ID:000001` for the first board. Confirm `OK:000001` and `WHO_ARE_YOU?` → `MACROPAD:000001`. Close Serial Monitor before running the app.

The ID is stored in EEPROM. Old saved names are preserved separately but not reused as IDs. Automatic assignment through a settings tool is not implemented. See the [protocol guide](onHardwareProgram/SerialProtocol.md) for migration, special characters, and persistence checks.

## Configuration and running

Install the .NET 10 SDK for development. The app creates `~/Library/Application Support/MacroPad/` and copies bundled defaults when either configuration file is missing. Existing files are preserved. Both files are read at startup; restart after editing them.

`config.json` now contains only:

```json
{
  "BaudRate": 115200
}
```

`macros.json` contains an array of mappings. For example:

```json
[
  {
    "ArduinoDeviceId": "000001",
    "ButtonCode": 10,
    "ActionType": "Keyboard",
    "Keys": [55, 8],
    "ApplicationPath": null
  }
]
```

Use `Keyboard` with a variable-length macOS keycode array, or `Application` with an empty `Keys` array and an application path. The [bundled mappings](MacroPad_application/Configuration/macros.json) target ID `000001`; review the actions and application paths before use. IDs are strings, preserving leading zeroes. The old `ArduinoDeviceName` property is no longer accepted as an identity; assign a board ID and update existing mappings explicitly.

Supported modifiers are left/right Command (55/54), Shift (56/60), Option (58/61), and Control (59/62). Ordinary keys run in sequence while the modifiers are held. See the [keycode reference](MacroPad_settings/keycodes.ini).

Allow the host terminal or IDE in macOS System Settings → Privacy & Security → Accessibility, then run from the repository root:

```sh
dotnet run --project MacroPad_application/MacroPad_application.csproj
```

The app scans candidate ports approximately every three seconds when its previous batch has finished, identifies boards, and keeps existing connections while checking new candidates. It handles partial serial messages and removes connections after detected read failures. Keyboard actions target the focused application.

## Project structure

- `Program.cs`: configuration and application coordination, message routing.
- `Controllers/ArduinoController.cs`: staged discovery, duplicate-ID checks, adding/removing connections.
- `Connections/ArduinoConnection.cs`: one board's serial port, listening state, and receive buffer.
- `Controllers/MacroPadController.cs`: lookup by `(ArduinoDeviceId, ButtonCode)`.
- `Controllers/MacOSController.cs`: keyboard events and application launches.
- `ConfigurationReader.cs`: JSON loading and missing-file defaults.
- `MacroPad_settings`: console scaffold; an editor is not implemented yet.

## Development status

This is a development build, not an installer release. One physical board has been tested by the author; multiple-board logic has been simulated, not established by physical multi-board tests. Button 5's earlier missing presses were resolved by repairing a loose breadboard connection.

Remaining work includes setup-time unique ID generation, fuller configuration validation, bounded input buffers, graceful shutdown, action-failure recovery, the settings editor, and a tested macOS `.pkg`. Missing-file defaults exist, but malformed JSON and other file-access failures still need better handling. Current development code also searches `/tmp/cu.usbmodem*` for virtual boards; remove or explicitly gate that before distribution.

See the [application roadmap](MacroPad_application/TODO.md), [settings roadmap](MacroPad_settings/TODO.md), and [ID migration review](ID_MIGRATION_2026-10-11.md).

Created by Fortuna Alexandru Cristian.
