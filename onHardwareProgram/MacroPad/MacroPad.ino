// Small program to translate Button Inputs into serial so that i can make a MacroPad on my macbook
// Description: Sends button press codes and handles device identification and saved names over USB serial.

#include <EEPROM.h>
#include <string.h>

const int MaxNameLength = 24;
const int CommandBufferSize = 9 + MaxNameLength + 1; // SET_NAME: + name + ending zero.
const byte SavedNameMarker = 165;

char deviceName[MaxNameLength + 1] = "UNNAMED";
char commandBuffer[CommandBufferSize];
int commandLength = 0;
bool commandTooLong = false;

//Definiton of variables
const int button1 = 6;
const int button2 = 5;
const int button3 = 4;
const int button4 = 3;
const int button5 = 2;

int buttonState1;
int buttonState2;
int buttonState3;
int buttonState4;
int buttonState5;

int prevButtonState1 = HIGH;
int prevButtonState2 = HIGH;
int prevButtonState3 = HIGH;
int prevButtonState4 = HIGH;
int prevButtonState5 = HIGH;

// Configures all five button pins with internal pull-up resistors and starts serial communication.
// Each button connects its input pin to ground when pressed.
void setup() {
  pinMode(button1, INPUT_PULLUP);
  pinMode(button2, INPUT_PULLUP);
  pinMode(button3, INPUT_PULLUP);
  pinMode(button4, INPUT_PULLUP);
  pinMode(button5, INPUT_PULLUP);

  Serial.begin(115200);
  LoadDeviceName();
}

// Reads the buttons and sends codes 10, 20, 30, 40, or 50 on released-to-pressed transitions.
// Saves each button's state for the next iteration; mechanical switch bounce is not filtered.
void loop() {
  ReadSerialCommand();

  buttonState1 = digitalRead(button1);
  buttonState2 = digitalRead(button2);
  buttonState3 = digitalRead(button3);
  buttonState4 = digitalRead(button4);
  buttonState5 = digitalRead(button5);

  if(prevButtonState1 == HIGH && buttonState1 == LOW)
  {
    Serial.println(10);
  }

  if(prevButtonState2 == HIGH && buttonState2 == LOW)
  {
    Serial.println(20);
  }

  if(prevButtonState3 == HIGH && buttonState3 == LOW)
  {
    Serial.println(30);
  }

  if(prevButtonState4 == HIGH && buttonState4 == LOW)
  {
    Serial.println(40);
  }

  if(prevButtonState5 == HIGH && buttonState5 == LOW)
  {
    Serial.println(50);
  }

  prevButtonState1 = buttonState1;
  prevButtonState2 = buttonState2;
  prevButtonState3 = buttonState3;
  prevButtonState4 = buttonState4;
  prevButtonState5 = buttonState5;
}

// Accepts 1-24 letters, digits, underscores, or hyphens so names cannot contain protocol separators.
bool IsValidName(const char name[]) {
  int length = 0;
  bool valid = true;

  while(length <= MaxNameLength && name[length] != '\0' && valid)
  {
    char character = name[length];
    valid = (character >= 'A' && character <= 'Z') ||
            (character >= 'a' && character <= 'z') ||
            (character >= '0' && character <= '9') ||
            character == '_' || character == '-';
    length++;
  }

  return valid && length > 0 && length <= MaxNameLength;
}

// EEPROM address 0 marks a completed save; addresses 1-25 hold the name and its ending zero.
// An unused or invalid saved name leaves the default UNNAMED in place.
void LoadDeviceName() {
  if(EEPROM.read(0) == SavedNameMarker)
  {
    char savedName[MaxNameLength + 1];
    EEPROM.get(1, savedName);

    if(IsValidName(savedName))
    {
      strcpy(deviceName, savedName);
    }
  }
}

// Only called with a valid name. update() avoids rewriting bytes that already match.
void SaveDeviceName(const char newName[]) {
  if(strcmp(deviceName, newName) != 0)
  {
    // Clear the marker first: an interrupted save must not look complete on the next startup.
    EEPROM.update(0, 0);
    strcpy(deviceName, newName);

    int nameLength = strlen(deviceName);
    for(int i = 0; i <= nameLength; i++)
    {
      EEPROM.update(i + 1, deviceName[i]);
    }

    EEPROM.update(0, SavedNameMarker);
  }
}

// Handles one complete command. Numeric button messages remain separate from command replies.
void HandleCommand(const char command[]) {
  if(strcmp(command, "WHO_ARE_YOU?") == 0)
  {
    Serial.print("MACROPAD:");
    Serial.println(deviceName);
  }
  else if(strncmp(command, "SET_NAME:", 9) == 0)
  {
    const char* newName = command + 9; // Skip SET_NAME: to reach the name.

    if(IsValidName(newName))
    {
      SaveDeviceName(newName);
      Serial.print("OK:");
      Serial.println(deviceName);
    }
    else
    {
      Serial.println("ERROR:INVALID_NAME");
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
