# MacroPad serial commands

The Mega 2560 uses **115200 baud**. Every command ends with a newline (`\n`).
Button presses still send the decimal codes `10`, `20`, `30`, `40`, and `50`.

## Try it in Arduino Serial Monitor

1. Close the C# companion app so it is not using the serial port.
2. Open `MacroPad.ino` in Arduino IDE. If the IDE asks to put the sketch into
   a matching `MacroPad` folder, use a copy for this test.
3. Select **Arduino Mega or Mega 2560**, select the board's port, and upload.
4. Open Serial Monitor, select **115200 baud** and **Newline**. Wait about two
   seconds for the board to start. **Both NL & CR** also works.
5. Send the commands below one at a time.

| Send | Expected reply |
| --- | --- |
| `WHO_ARE_YOU?` | `MACROPAD:UNNAMED` on a board with no saved name |
| `SET_NAME:DeskPad` | `OK:DeskPad` |
| `WHO_ARE_YOU?` | `MACROPAD:DeskPad` |
| `SET_NAME:` | `ERROR:INVALID_NAME` |
| `SET_NAME:Desk Pad` | `ERROR:INVALID_NAME` |
| `HELLO` | `ERROR:UNKNOWN_COMMAND` |

Unplug and reconnect the board, reopen Serial Monitor, wait for startup, and
send `WHO_ARE_YOU?` again. It should still reply `MACROPAD:DeskPad`.
Also check that the five buttons still send their original codes.

Names accept 1-24 ASCII letters, digits, underscores, or hyphens. Commands are
case-sensitive. Blank lines are ignored. A line longer than 33 characters
returns `ERROR:COMMAND_TOO_LONG` and is discarded; the following line works normally.

## How the code works

- `ReadSerialCommand()` collects characters in an array. When it receives a
  newline, it passes the completed text to `HandleCommand()`.
- `HandleCommand()` recognizes the question or rename command and sends a reply.
- `IsValidName()` checks the length and allowed characters before saving.
- `SaveDeviceName()` stores a changed name in EEPROM, which survives unplugging.
- `LoadDeviceName()` reads it during `setup()`, falling back to `UNNAMED` if no
  valid saved name exists.

In C++, these text arrays end with a zero character, written `\0`. It marks
the end of the text; it is different from the newline `\n` sent over serial.
`strcmp(a, b) == 0` means two strings match. `strncmp(a, b, 9) == 0` compares
only the first nine characters. `command + 9` points to the text after `SET_NAME:`.

EEPROM address 0 holds a marker saying that a save finished. Addresses 1-25
hold the name and ending zero. The marker is cleared before changing the name
and written last, so an interrupted save falls back to `UNNAMED`.
`EEPROM.update()` writes a byte only if it differs from the stored value.

## Connecting C# next

On an already open `SerialPort`, `arduino.WriteLine("WHO_ARE_YOU?")` sends the
question and `arduino.WriteLine("SET_NAME:DeskPad")` sends the rename command.
The C# application still needs reply handling before using these in its normal
flow: its current read loop expects only button numbers.

Use a finite read timeout when waiting for a reply and allow startup time after
opening the port, because opening it can reset the Mega. Button codes can arrive
between replies, so do not assume the first received line is the requested reply.
Only one part of the app should read from the port at a time.

After the manual test works, the next step is C# handshake handling on the known
port, followed by automatic port discovery. The saved name does not change the
macOS `/dev/cu...` path. Multiple boards can currently have the same name.
