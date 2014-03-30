#include <Wire.h>          // for I2C interface
#include <SD.h>            // for microSD shield interface

#define DEVICE (0x53)      // Device 

byte _buff[6];
File dataFile;
char fileName[64];
char formattedData[64];

char userName[25];
char strBuff[64];

word checksum = 0;
byte highcheck = 0;
byte lowcheck = 0;

byte command = 1;
byte parameter = 0;
byte highbyte = 0;
byte lowbyte = 0;

byte index = 0;
byte inChar;

byte pcCommand;

// address declaration
char POWER_CTL = 0x2D;  //Power Control Register
char DATA_FORMAT = 0x31;
char DATAX0 = 0x32; //X-Axis Data 0
char DATAX1 = 0x33; //X-Axis Data 1
char DATAY0 = 0x34; //Y-Axis Data 0
char DATAY1 = 0x35; //Y-Axis Data 1
char DATAZ0 = 0x36; //Z-Axis Data 0
char DATAZ1 = 0x37; //Z-Axis Data 1

// Pin declaration
int RXLED = 17;
int PWMPORT = 5;
int SD_CS = 10;

// state byte indicating the current working 
// state of pedometer
// 0x00 -- uninitialized
// 0x01 -- initialized
// 0x10 -- connected
// 0x02 -- recording

byte state = 0x00;

// alarm function
// TODO break it into two functions which will
// be called to change frequency
void alarm()
{
  tone(PWMPORT, 500);
  delay(500);
  tone(PWMPORT, 200);
  delay(500);
}

void setup()
{
  // for debug use only
  delay(1000);
  
  pinMode(RXLED, OUTPUT);                 // Set RX LED as an output
  pinMode(SD_CS, OUTPUT);                 // Set pin 18 as output (SD card CS)
 
  Serial.begin(9600);                     // This pipes to the serial monitor
  Serial.println("USB serial connection established.");
  
  Serial1.begin(9600);                    // Set up fingerprint sensor interface
 
  // Initialize I2C interface
  Wire.begin();
  Serial.println("I2C connection established.");
    
  if (!SD.begin(SD_CS)) {
    Serial.println("SD card SPI initialization failed!");
    return;
  }
  Serial.println("SD card SPI connection established.");
 
  // Put the ADXL345 into +/- 4G range by writing the value 0x01 to the DATA_FORMAT register.
  writeTo(DATA_FORMAT, 0x01);
  // Put the ADXL345 into Measurement Mode by writing 0x08 to the POWER_CTL register.
  writeTo(POWER_CTL, 0x08);
  Serial.println("Accelerometer configured.");
}

void loop()
{
  if(Serial1.available()){
    index = 0;
    while(Serial1.available() > 0){
      inChar = Serial1.read(); // Read a character
      Serial.print(inChar, HEX);
    }
  }
  command = 0x12;
  parameter = 0x01;
  sendCommandToFingerPrint();
}

void writeTo(byte address, byte val) {
  Wire.beginTransmission(DEVICE); // start transmission to device 
  Wire.write(address);             // send register address
  Wire.write(val);                 // send value to write
  Wire.endTransmission();         // end transmission
}

// Reads num bytes starting from address register on device in to _buff array
void readFrom(byte address, int num, byte _buff[]) {
  Wire.beginTransmission(DEVICE); // start transmission to device 
  Wire.write(address);             // sends address to read from
  Wire.endTransmission();         // end transmission

  Wire.beginTransmission(DEVICE); // start transmission to device
  Wire.requestFrom(DEVICE, num, false);    // request 6 bytes from device

  int i = 0;
  
  while(Wire.available())         // device may send less than requested (abnormal)
  { 
    _buff[i] = Wire.read();    // receive a byte
    i++;
  }
  Wire.endTransmission();         // end transmission
}

void sendCommandToFingerPrint(){
  valueToWORD(parameter); 
  calcChecksum(command, highbyte, lowbyte); //This function will calculate the checksum which tells the device that it received all the data
  Serial1.write(0x55); //Command start code 1
  Serial1.write(0xaa); //Command start code 2
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

void calcChecksum(byte c, byte h, byte l){
  checksum = 256 + c + h + l; //adds up all the bytes sent
  highcheck = highByte(checksum); //then turns this checksum which is a word into 2 bytes
  lowcheck = lowByte(checksum);
}

void valueToWORD(int v){ //turns the word you put into it (the paramter in the code above) to two bytes
  highbyte = highByte(v); //the high byte is the first byte in the word
  lowbyte = lowByte(v); //the low byte is the last byte in the word (there are only 2 in a word)
}

