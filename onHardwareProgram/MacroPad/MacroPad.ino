// Small program to translate Button Inputs into serial so that i can make a MacroPad on my macbook
// Description: Sends button press codes and handles device identification and saved six-character IDs over USB serial.

#include <EEPROM.h>
#include <string.h>

const int DeviceIdLength = 6;
const int CommandBufferSize = 7 + DeviceIdLength + 1; // SET_ID: + six characters + ending zero.
const byte SavedIdMarker = 166;
const int SavedIdAddress = 0; // Keep the old name at addresses 0-25 untouched.

char deviceId[DeviceIdLength + 1] = "000000";
char commandBuffer[CommandBufferSize];
int commandLength = 0;
bool commandTooLong = false;

//Definiton of variables
const unsigned long debounceDelay = 100;
const int buttonCount = 5;

const int buttonPins[buttonCount] = {6, 5, 4, 3, 2};
const int buttonCodes[buttonCount] = {10, 20, 30, 40, 50};
int buttonStates[buttonCount] = {HIGH, HIGH, HIGH, HIGH, HIGH};
int prevButtonStates[buttonCount] = {HIGH, HIGH, HIGH, HIGH, HIGH};
unsigned long lastButtonPresses[buttonCount] = {0};

// Configures all five button pins with internal pull-up resistors and starts serial communication.
// Each button connects its input pin to ground when pressed.
void setup() {
  for(int i = 0; i < buttonCount; i++)
  {
      pinMode(buttonPins[i], INPUT_PULLUP);
  }

  Serial.begin(115200);
  LoadDeviceId();
}

// Reads the buttons and sends codes 10, 20, 30, 40, or 50 on released-to-pressed transitions.
// Suppresses further press edges for 100 ms after an accepted press; this is a cooldown,
// not a stable-state debounce. Rapid intentional presses inside that interval are ignored.
void loop() {
  ReadSerialCommand();

  for(int i = 0; i < buttonCount; i++)
  {
    buttonStates[i] = digitalRead(buttonPins[i]);

    if(prevButtonStates[i] == HIGH && buttonStates[i] == LOW)
    {
      if(millis() - lastButtonPresses[i] >= debounceDelay)
      {
        Serial.println(buttonCodes[i]);
        lastButtonPresses[i] = millis();
      }
    }

    prevButtonStates[i] = buttonStates[i];
  }
}

// Accepts exactly six printable ASCII characters; 000000 is reserved for a board awaiting setup.
bool IsValidId(const char id[]) {
  int length = 0;
  bool valid = true;

  while(length <= DeviceIdLength && id[length] != '\0' && valid)
  {
    valid = id[length] >= ' ' && id[length] <= '~';
    length++;
  }

  return valid && length == DeviceIdLength && strcmp(id, "000000") != 0;
}

// EEPROM address 32 marks a completed ID save; addresses 33-39 hold six characters and zero.
// An unused or invalid saved ID leaves 000000 in place; legacy name storage is not loaded.
void LoadDeviceId() {
  if(EEPROM.read(SavedIdAddress) == SavedIdMarker)
  {
    char savedId[DeviceIdLength + 1];
    EEPROM.get(SavedIdAddress + 1, savedId);

    if(IsValidId(savedId))
    {
      strcpy(deviceId, savedId);
    }
  }
}

// Only called with a valid ID. update() avoids rewriting bytes that already match.
void SaveDeviceId(const char newId[]) {
  if(strcmp(deviceId, newId) != 0)
  {
    // Clear the marker first: an interrupted save must not look complete on the next startup.
    EEPROM.update(SavedIdAddress, 0);
    strcpy(deviceId, newId);

    int idLength = strlen(deviceId);
    for(int i = 0; i <= idLength; i++)
    {
      EEPROM.update(SavedIdAddress + i + 1, deviceId[i]);
    }

    EEPROM.update(SavedIdAddress, SavedIdMarker);
  }
}

// Handles one complete command. Numeric button messages remain separate from command replies.
void HandleCommand(const char command[]) {
  if(strcmp(command, "WHO_ARE_YOU?") == 0)
  {
    Serial.print("MACROPAD:");
    Serial.println(deviceId);
  }
  else if(strncmp(command, "SET_ID:", 7) == 0)
  {
    const char* newId = command + 7; // Skip SET_ID: to reach the six-character ID.

    if(IsValidId(newId))
    {
      SaveDeviceId(newId);
      Serial.print("OK:");
      Serial.println(deviceId);
    }
    else
    {
      Serial.println("ERROR:INVALID_ID");
    }
  }
  else
  {
    Serial.println("ERROR:UNKNOWN_COMMAND");
  }
}

// Reads one available character per loop, so a partial command does not pause button checking.
// A newline finishes the command; an optional carriage return is ignored for Serial Monitor.
void ReadSerialCommand() {
  if(Serial.available() > 0)
  {
    char character = Serial.read();

    if(character == '\n')
    {
      if(commandTooLong)
      {
        Serial.println("ERROR:COMMAND_TOO_LONG");
      }
      else if(commandLength > 0)
      {
        commandBuffer[commandLength] = '\0';
        HandleCommand(commandBuffer);
      }

      commandLength = 0;
      commandTooLong = false;
    }
    else if(character != '\r')
    {
      if(commandLength < CommandBufferSize - 1 && !commandTooLong)
      {
        commandBuffer[commandLength] = character;
        commandLength++;
      }
      else
      {
        // Discard the rest of this line instead of accepting a truncated command.
        commandTooLong = true;
      }
    }
  }
}
