// Small program to translate Button Inputs into serial so that i can make a MacroPad on my macbook

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

void setup() {
  pinMode(button1, INPUT_PULLUP);
  pinMode(button2, INPUT_PULLUP);
  pinMode(button3, INPUT_PULLUP);
  pinMode(button4, INPUT_PULLUP);
  pinMode(button5, INPUT_PULLUP);

  Serial.begin(115200);
}

void loop() {
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
