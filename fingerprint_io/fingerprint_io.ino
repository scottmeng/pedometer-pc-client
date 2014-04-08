byte highbyte = 0;
byte lowbyte = 0;
byte command = 1;
int value = 0;
word checksum = 0;
byte highcheck = 0;
byte lowcheck = 0;
char inData[20]; // Allocate some space for the string
char inChar; // Where to store the character read
byte index = 0; // Index into array; where to store the character
boolean ledon = false;

bool isMsg;

int step;
byte id;

void setup() {
  Serial.begin(9600);
  Serial1.begin(9600);
}

void loop(){
  /*
  if(Serial1.available()==12){
    if (readMsg()) {
      if (inData[8] == 0x30) {
        // acknowledgement
        // Serial.println("success");
        // Serial.println(int(inData[4]));
      } else {
        // error
        // Serial.println("error");
        // Serial.print(inData[5], HEX);
        // Serial.println(inData[4], HEX);
      }
      if (step > 0 && step < 9) {
        step +=1;
        regFingerprint();
      }
      if (step >= 9) {
        step = 0;
      }
    }
  }
  */
  
  // read from port 0, send to port 1:
  if (Serial.available()) {
    byte dummy = Serial.read();
    step = 1;
    id = 6;
    if(regFingerprintSerial()) {
      Serial.println("success");
    } else {
      Serial.println("failure");
    }
  }
}

bool regFingerprintSerial() {
  byte value = 0x01;
  sendCommand(0x21, id);          // check if the id has been enrolled
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x30) {
    return false;
  }
  sendCommand(0x12, 0x01);        // turn on led
  if (!readMsg()) {
    return false;
  }
  sendCommand(0x22, id);          // start enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  while (value != 0x00) {         // keep checking for finger until it is detected
    sendCommand(0x26, 0x00);
    if (!readMsg()) {
      return false;
    }
    if (inData[8] == 0x31) {
      return false;
    }
    value = inData[4];
  }
  sendCommand(0x60, 0x00);        // capture first image
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommand(0x23, id);          // first enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommand(0x60, 0x00);        // capture second image
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommand(0x24, id);          // second enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommand(0x60, 0x00);        // capture third image
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommand(0x25, id);          // third enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommand(0x12, 0x00);        // turn off led
  return true;
}

bool regFingerprint() {
  Serial.println(step);
  switch (step) {
    case 1:
      sendCommand(0x21, id);      // check if id has been enrolled
      break;
    case 2:
      if (inData[8] == 0x30) {
        return false;             // id has been enrolled
      }
      sendCommand(0x12, 0x01);    // turn on LED
      break;
    case 3:
      sendCommand(0x22, id);
      break;
    case 4:
      sendCommand(0x60, 0x00);
      break;
    case 5:
      if (inData[8] == 0x31) {
        return false;
      }
      sendCommand(0x23, 0x00);
      break;
    case 6:
      sendCommand(0x60, 0x00);
      break;
    case 7:
      if (inData[8] == 0x31) {
        return false;
      }
      sendCommand(0x24, 0x00);
      break;
    case 8:
      sendCommand(0x60, 0x00);
      break;
    case 9:
      if (inData[8] == 0x31) {
        return false;
      }
      sendCommand(0x25, 0x00);
      break;
    default:
      break;
  }

  return true;
}

bool readMsg()
{
  while (Serial1.available() < 12) {}
  Serial.println("=====");
  int i = 0;
  bool isCorrect = true;
  word checkSum;
  
  for (i = 0; i < 12; ++i) {
    while (!Serial1.available()) {}
    inChar = Serial1.read();
    Serial.println(inChar, HEX);
    inData[i] = inChar;
    
    if (i == 0 && inChar != 0x55) {
      isCorrect = false;
    }
    
    if (i == 1 && inChar != 0xFFFFFFAA) {
      isCorrect = false;
    }
  }
  
  checkSum = 256;
  for (i = 0; i < 10; ++i) {
    checkSum += inData[i];
  }
  
  if (highByte(checkSum) != inData[11] || lowByte(checkSum) != inData[10]) {
    Serial.println("wrong with sum");
    isCorrect = false;
  }  
  Serial.println("=====");
  return isCorrect;
}

void sendCommand(byte com, byte val) {
  command = com;
  value = int(val);

  valueToWORD(value); //This value is the parameter being send to the device. 0 will turn the LED off, while 1 will turn it on.
  calcChecksum(command, highbyte, lowbyte); //This function will calculate the checksum which tells the device that it received all the data
  Serial1.write(0x55); //Command start code 1
  Serial1.write(0xAA); //Command start code 2
  Serial1.write(0x01); // This is the first byte for the device ID. It is the word 0x0001
  Serial1.write(0x00); // Second byte of Device ID. Notice the larger byte is first. I'm assuming this is because the datasheet says "Multi-byte item is represented as Little Endian"
  Serial1.write(lowbyte); //writing the largest byte of the Parameter
  Serial1.write(highbyte); //Writing the second largest byte of the Parameter
  Serial1.write(0x00); //The datasheet says the parameter is a DWORD, but it never seems to go over the value of a word
  Serial1.write(0x00); //so I'm just sending it a word of data. These are the 2 remaining bytes of the Dword
  Serial1.write(command); //write the command byte
  Serial1.write(0x00); //again, the commands don't go over a byte, but it is sent as a word, so I'm only sending a byte
  Serial1.write(lowcheck); //Writes the largest byte of the checksum
  Serial1.write(highcheck); //writes the smallest byte of the checksum

}

void valueToWORD(int v){ //turns the word you put into it (the paramter in the code above) to two bytes
  highbyte = highByte(v); //the high byte is the first byte in the word
  lowbyte = lowByte(v); //the low byte is the last byte in the word (there are only 2 in a word)
}

void calcChecksum(byte c, byte h, byte l){
  checksum = 256 + c + h + l; //adds up all the bytes sent
  highcheck = highByte(checksum); //then turns this checksum which is a word into 2 bytes
  lowcheck = lowByte(checksum);
}
