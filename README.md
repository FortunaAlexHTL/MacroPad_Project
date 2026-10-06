# Macro Pad

**Five physical buttons. Your everyday Mac actions, one press away.**

A DIY macro pad that connects an Arduino-compatible board to a macOS companion app. The board sends button presses over USB serial, and the C# app turns them into keyboard shortcuts or application launches.

Built with **Arduino · C# · .NET 10 · macOS CoreGraphics**.

[Watch the demo](demoVideo/MacroPad_Demo.MOV) · [Arduino firmware](onHardwareProgram/MacroPad.ino) · [Button mappings](MacroPad_application/settings.ini) · [Keycode reference](MacroPad_settings/keycodes.ini)

## What it does

- Connects five physical buttons to configurable Mac actions.
- Sends keyboard shortcuts with Command, Shift, Option, and Control.
- Launches applications through the macOS `open` command.
- Stores button assignments and application targets in simple text files.
- Uses a serial connection at **115200 baud** between the board and the Mac.

```text
Button press → Arduino → USB serial → C# companion app → Mac action
```

The companion app must stay running to handle button presses. The board sends serial messages; the Mac app creates the keyboard events.

## Hardware and wiring

You will need an Arduino-compatible board with USB serial, five momentary push buttons, connecting wires, and a USB data cable. A breadboard is useful for prototyping.

Connect each button between its assigned digital pin and **GND**. The firmware enables `INPUT_PULLUP`, so an unpressed button reads `HIGH` and a pressed button reads `LOW`.

| Button | Digital pin | Serial message | Included mapping |
| :--- | :---: | :---: | :--- |
| 1 | 6 | `10` | Command + C |
| 2 | 5 | `20` | Command + V |
| 3 | 4 | `30` | Open JetBrains Rider |
| 4 | 3 | `40` | Send keycode `74` |
| 5 | 2 | `50` | Send keycode `72` |

Each press sends a newline-terminated message. Holding a button does not intentionally repeat the action; the firmware detects the transition from released to pressed. Mechanical button bounce can still cause extra events because software debouncing is not implemented yet.

## Getting started

### 1. Prepare the tools

- A Mac to run the companion app, which calls the macOS CoreGraphics framework.
- The **.NET 10 SDK**, matching the projects' `net10.0` target.
- The Arduino IDE and board support for your chosen board.

### 2. Upload the firmware

Open [`onHardwareProgram/MacroPad.ino`](onHardwareProgram/MacroPad.ino) in the Arduino IDE, select your board and serial port, and upload it. If the IDE asks to place the sketch in a folder named `MacroPad`, accept that prompt.

To check the wiring, open Serial Monitor at **115200 baud** and press each button. You should see `10`, `20`, `30`, `40`, or `50` on separate lines. Close Serial Monitor before starting the companion app so it can use the serial port.

### 3. Set the local paths and serial port

The current app uses hardcoded paths. Before running it, edit [`MacroPad_application/Program.cs`](MacroPad_application/Program.cs):

| Setting | What to enter |
| :--- | :--- |
| `SettingsFilePath` | Absolute path to your checkout's `MacroPad_application/settings.ini` |
| `AppsFilePath` | Absolute path to your checkout's `MacroPad_application/apps.ini` |
| First argument to `new SerialPort(...)` | Your board's serial device, replacing `/dev/cu.usbmodem11401` |

Find available serial devices in Terminal:

```sh
ls /dev/cu.*
```

Choose the device that appears when you connect the board. Keep the baud rate at `115200` to match the firmware.

### 4. Allow keyboard control

In **System Settings → Privacy & Security → Accessibility**, allow the terminal or IDE you use to run the companion app to control your Mac. Restart that terminal or IDE if macOS requests it.

### 5. Run the companion app

From the repository root:

```sh
dotnet restore MacroPad_application/MacroPad_application.csproj
dotnet run --project MacroPad_application/MacroPad_application.csproj
```

When the app prints `MacroPad is running!`, press a button to trigger its assigned action. Keyboard events are sent to the currently focused application. Use **Control + C** in the running terminal to stop the companion app.

## Customize your buttons

### Keyboard shortcuts

[`settings.ini`](MacroPad_application/settings.ini) contains one semicolon-separated row per button. Each row has **six integer fields**:

```text
button_code; action_type; value_1; value_2; value_3; value_4
```

- `button_code` matches the message sent by the Arduino.
- `action_type` is `0` for keyboard actions or `1` for application launches.
- Keyboard actions support up to four keycodes, including modifiers.
- Use `-1` for unused trailing fields. The first `-1` ends the action's values.

For example, these rows assign **Command + C** to button 1 and **Command + Shift + V** to button 2:

```ini
10; 0; 55; 8; -1; -1
20; 0; 55; 56; 9; -1
```

| Modifier | Supported keycode |
| :--- | :---: |
| Command | `55` |
| Shift | `56` |
| Option | `58` |
| Control | `59` |

See [`keycodes.ini`](MacroPad_settings/keycodes.ini) for the included keycode reference. The companion app recognizes only the four modifier codes above as modifiers, even though the reference also lists right-hand variants. Keycodes represent physical keys, so the resulting character or shortcut can depend on your keyboard layout and the focused app.

Modifiers are held while the remaining keys are pressed and released in sequence. A row with multiple ordinary keys therefore sends a sequence under the same modifiers.

### Application launches

Use action type `1` and an application identifier in `settings.ini`:

```ini
30; 1; 127; -1; -1; -1
```

Then define that identifier in [`apps.ini`](MacroPad_application/apps.ini):

```ini
127;-a /Applications/Rider.app
```

The text after the semicolon is passed to macOS `open`. Change it to the application you want to launch. Quote paths containing spaces, for example:

```ini
128;-a "/Applications/Visual Studio Code.app"
```

To use this second entry, set the corresponding button's application identifier to `128`.

### Configuration rules

- Keep a valid mapping for every button the firmware can send.
- Use exactly six integer fields per row in `settings.ini`, with unused fields filled by `-1`.
- Do not add blank lines, comments, or section headers. Despite their `.ini` extension, these files use a custom semicolon-separated format.
- Use matching application identifiers in both files, with no surrounding spaces in the identifier field in `apps.ini`.
- Restart the companion app after changing button mappings; `settings.ini` is loaded at startup. Application targets are read when an app-launch action runs.

## Project layout

```text
MacroPad_project/
├── onHardwareProgram/
│   └── MacroPad.ino                 # Button inputs and serial messages
├── MacroPad_application/
│   ├── Program.cs                   # Serial listener and macOS actions
│   ├── MacroPad_application.csproj  # .NET 10 console project
│   ├── settings.ini                 # Button-to-action mappings
│   └── apps.ini                     # Application launch targets
├── MacroPad_settings/
│   ├── Program.cs                   # Placeholder for a settings tool
│   ├── MacroPad_Settings.csproj     # Settings tool project
│   └── keycodes.ini                 # Keycode reference
├── demoVideo/
│   └── MacroPad_Demo.MOV            # Project demo
└── MacroPad.slnx                    # Solution file
```

## Troubleshooting

| Symptom | What to check |
| :--- | :--- |
| Serial port cannot be opened | Connect the board, confirm its device path, and close Serial Monitor or any other app using that port. |
| Configuration file cannot be found | Update both absolute paths in `Program.cs` to point to your checkout. |
| Button presses do not produce shortcuts | Check Accessibility permission for the terminal or IDE, then confirm the intended app has focus. |
| An application does not launch | Check the application identifier, installation path, and quoting in `apps.ini`. |
| One press triggers multiple actions | Check the wiring and switch bounce; the current firmware has no debounce logic. |
| The app stops after a button press | Check the six-field mapping and serial message. Invalid or unmapped input is not handled gracefully yet. |

## Current status

This is a working prototype with manual configuration. The `MacroPad_settings` project currently prints `Hello, World!`; it does not edit settings yet. Serial reconnection, configuration validation, software debouncing, and automatic startup are not implemented.

**Created by Fortuna Alexandru Cristian.**
