#include <Wire.h>          // for I2C interface
#include <SD.h>            // for microSD shield interface

#define DEVICE (0x53)      // accelerometer id 

byte _buff[6];
File dataFile;
char fileName[64];
char formattedData[64];

char userName[25];
char strBuff[64];

word checksum = 0;
byte highcheck = 0;
byte lowcheck = 0;

byte highbyte = 0;
byte lowbyte = 0;

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

// time in millis when authentication is performed
long lastCheckedMillis = 0;
long curMillis;
byte curUser = 20;


char inData[20]; // Allocate some space for the string
char inChar; // Where to store the character read

// alarm function
// TODO break it into two functions which will
// be called to change frequency
void alarm()
{
  tone(PWMPORT, 500);
}

// dealarm function
void dealarm()
{
  noTone(PWMPORT);
}

// produce unsuccessful fingerprint sound
void unsuccessSound()
{
  delay(100);
  tone(PWMPORT, 659, 200);
  delay(100);
  tone(PWMPORT, 523, 200);
  delay(100);
  tone(PWMPORT, 440, 200);
  delay(100);
  noTone(PWMPORT);
}

// produce successful fingerprint sound
void successSound()
{
  delay(100);
  tone(PWMPORT, 440, 200);
  delay(100);
  tone(PWMPORT, 493, 200);
  delay(100);
  tone(PWMPORT, 523, 200);
  delay(100);
  tone(PWMPORT, 587, 200);
  delay(100);
  tone(PWMPORT, 659, 200);
  delay(100);
  noTone(PWMPORT);
}

void setup()
{
  // for debug use only
  delay(2000);
  
  pinMode(RXLED, OUTPUT);                 // Set RX LED as an output
  pinMode(SD_CS, OUTPUT);                 // Set pin 18 as output (SD card CS)
 
  Serial.begin(115200);                     // This pipes to the serial monitor
  // Serial.println("USB serial connection established.");
  
  Serial1.begin(9600);                    // Set up fingerprint sensor interface
 
  // Initialize I2C interface
  Wire.begin();
  // Serial.println("I2C connection established.");
    
  if (!SD.begin(SD_CS)) {
    // Serial.println("SD card SPI initialization failed!");
    return;
  }
  // Serial.println("SD card SPI connection established.");
 
  // Put the ADXL345 into +/- 4G range by writing the value 0x01 to the DATA_FORMAT register.
  writeTo(DATA_FORMAT, 0x01);
  // Serial.println("data format");
  // Put the ADXL345 into Measurement Mode by writing 0x08 to the POWER_CTL register.
  writeTo(POWER_CTL, 0x08);
  // Serial.println("Accelerometer configured.");
}

void loop()
{
  if (state == 0x10) {
    if (readPCCommand()) {
      switch (pcCommand) {
        case 'a':                       // access device mode
          if (isInitialized())
          {
            Serial.write('o');
          }
          else
          {
            Serial.write('n');
          }
          break;
        case 'c':
          clearHistory();
          break;
        case 'd':                       // request for data synchronization
          transferNewData();
          break;
        case 'e':                       // terminate connection
          terminateConnection();        
          break;
        case 'n':
          countNewFiles();              // return the number of new files
          break;
        case 'u':                       // add new user
          addNewUser();
          break;
        case 'i':                       // initialize device
          initialize();
          break;
        case 't':
          printProfile();
          break;
        case 'p':                       // for debug purpose
          updateProfile(byte(1), byte(0));
          break;
        default:
          break;
      }
    }
  } else {  
    checkConnection();                          // only check for connection when it is not connected
    if (state == 0x00) {                        // uninitialized
      if (isInitialized()) 
      {
        state = 0x01;
      }
    }
    else if (state == 0x01) {                   // initialized not recording
       alarm();
       byte id = authenticateUser();
       //Serial.println(id);
       if (id < 20) {                           // if id is non zero, user has been authen
         dealarm();
         successSound();
         curMillis = millis();
         lastCheckedMillis = curMillis;
         // Serial.println("user detected");
         // Serial.println(id);
         if (curUser != id) {
           if(!createFile(id)) {
             // Serial.println("created file");
             return;
           }
           curUser = id;
         }
         state = 0x02;                          // start recording
       }
       if (id == 21) {                          // no fingerprint match
         unsuccessSound();
       }
    }
    else if (state == 0x02) {                      // recording
      if (checkDeadline()) {
        state = 0x01;                             // change to initialized state
      }
      readAccel();
    }
  }
}

void terminateConnection()
{
  curUser = 20;
  state = 0x00;
}

void clearHistory()
{
  SD.remove("profile.txt");
}

bool checkDeadline()
{
  curMillis = millis();

  if (curMillis - lastCheckedMillis > 10000)
  {
    return true;
  }
  return false;
}

void printProfile()
{
  byte dummy;

  dataFile = SD.open("profile.txt");
  while (dataFile.available())
  {
    dummy = dataFile.read();
    Serial.println(dummy);
  }
  dataFile.close();
}

/*
 * add a new user entry to profile.txt
 * busy-wait for the PC client to transfer
 * user id
 */
void addNewUser()
{
  byte uid;
  while (!Serial.available()){}

  uid = Serial.read();
  // Serial.println(uid);
  recordNewUser(uid);
}

/*
 * add new user entry to profile
 */
void recordNewUser(byte uid)
{
  if (regFingerprintSerial(uid))
  {
    dataFile = SD.open("profile.txt", FILE_WRITE);
      
    dataFile.write(uid);
    dataFile.write(',');
    dataFile.write(byte(0));
    dataFile.write('\r');
    dataFile.write('\n');
    dataFile.close();
    
    //Serial.println(uid);
    Serial.write(uid);            // notify PC client
    return;
  }
  //Serial.println(255);
  Serial.write('f');
}


/*
 * read one byte off serial port 
 * if successful, return true
 * if not, return false
 */
bool readPCCommand()
{
  if (Serial.available())
  {
    pcCommand = Serial.read();
    return true;
  }
  return false;
}

/*
 * check if device is connected with PC client
 * if a character 'a' is received
 * the device will return a character 
 * indicating the status of device
 * 'o' for "old"
 * 'n' for "new"
 */
void checkConnection()
{
  if (Serial.available() > 0 && Serial.read() == 'a')
  {
    state = 0x10;                // change the state into "connected"
    dealarm();                   // in case no fingerprint is scanned
    turnOffLED();
    if (isInitialized())
    {
      Serial.write('o');
    }
    else
    {
      Serial.write('n');
    }
  }
}

void countNewFiles()
{
  byte dummy, userid, index, numOfNewFiles = 0;
  
  dataFile = SD.open("profile.txt");
  while (dataFile.available())
  {
    userid = dataFile.read();
    dummy = dataFile.read();
    index = dataFile.read();
    dummy = dataFile.read();
    dummy = dataFile.read();  
   
    sprintf(fileName, "%d_%d.txt", userid, index);
    while (SD.exists(fileName))
    {
      index += 1;
      numOfNewFiles += 1;
      sprintf(fileName, "%d_%d.txt", userid, index);  
    }
  }
  Serial.write(numOfNewFiles);
}

/*
 * transfer all newly-recorded data
 * check profile.txt for indices of new data files
 * loop through all users with new data
 * and transfer data from each new data file
 */
void transferNewData()
{
  byte dummy, userid, index;

  dataFile = SD.open("profile.txt");
  while (dataFile.available())
  {
    userid = dataFile.read();
    dummy = dataFile.read();
    index = dataFile.read();
    dummy = dataFile.read();
    dummy = dataFile.read();

    while (transferFileData(userid, index))
    {
      index += 1;
    }
    updateProfile(userid, index);
  }
  notifyFileEnd();
}

/*
 * transfer data from one data file
 * user name is specified in
 * index is specified in 
 * data format:
 *  
 */
bool transferFileData(byte userId, byte index)
{
  sprintf(fileName, "%d_%d.txt", userId, index);

  if (SD.exists(fileName))
  {
    notifyFileStart();
    Serial.write(userId);
    Serial.write(index);

    dataFile = SD.open(fileName);
    while (dataFile.available())
    {
      Serial.write(dataFile.read());
    }
    dataFile.close();

    return true;
  }

  return false;
}

/*
 * update the profile txt file to reflect
 * which files have been synced to PC
 */
void updateProfile(byte userid, byte index)
{
  byte uid = 0, dummy;
  byte cur, buffer[8];

  dataFile = SD.open("profile.txt", FILE_WRITE);
  dataFile.seek(0);
  while (dataFile.available())
  {
    uid = dataFile.read();
    dummy = dataFile.read();

    if (uid == userid)
    {
      dataFile.write(index);
    }
    else 
    {
      dummy = dataFile.read();
    }
    dummy = dataFile.read();
    dummy = dataFile.read();
  }
  dataFile.close();
}

/*
 * indicate the start of a data file transfer
 * two consecutive character '+' are transferred
 * followed by one byte of user id and one byte of index
 */
void notifyFileStart()
{
  Serial.write(0x2B);
  Serial.write(0x2B);
}

/*
 * indicate the end of a data file transfer
 * two consecutive character '-' are transferred
 * after all data bytes are being transferred
 */
void notifyFileEnd()
{
  Serial.write(0x2D);
  Serial.write(0x2D);
}

void initialize()
{
  dataFile = SD.open("profile.txt", FILE_WRITE);
  
  delay(1000);
  
  dataFile.close();
  Serial.write('i');
}

bool isInitialized()
{
  if (SD.exists("profile.txt") && (SD.open("profile.txt").size() > 0))
  {
    return true;
  }
  return false;
}

boolean createFile(byte userId)
{
  int index = 0;

  do 
  {
    sprintf(fileName, "%d_%d.txt", userId, index);
    index += 1;
  } while (SD.exists(fileName));

  dataFile = SD.open(fileName, FILE_WRITE);
  
  delay(1000);
  
  if(dataFile) 
  {
    dataFile.close();
    return true;
  }
  return false;
}

void readAccel() {
  uint8_t howManyBytesToRead = 6;
  
  readFrom( DATAX0, howManyBytesToRead, _buff); //read the acceleration data from the ADXL345
                                                  // each axis reading comes in 10 bit resolution, ie 2 bytes.  Least Significat Byte first!!
                                                  // thus we are converting both bytes in to one int
  int x = (((int)_buff[1]) << 8) | _buff[0];   
  int y = (((int)_buff[3]) << 8) | _buff[2];
  int z = (((int)_buff[5]) << 8) | _buff[4];
  
  sprintf(formattedData, "%ld,%d,%d,%d", millis(), x, y, z);

  dataFile = SD.open(fileName, FILE_WRITE);
  dataFile.println(formattedData);
  dataFile.close();
}

/*
 * read message from fingerprint sensor
 * total number of bytes is 12
 * 
 */
bool readMsg()
{
  while (Serial1.available() < 12) {}
  byte i = 0;
  word checkSum;
  
  for (i = 0; i < 12; ++i) {
    while (!Serial1.available()) {}
    inChar = Serial1.read();
    // Serial.println(inChar, HEX);
    inData[i] = inChar;
    
    if (i == 0 && inChar != 0x55) {
      return false;
    }
    
    if (i == 1 && inChar != 0xFFFFFFAA) {
      return false;
    }
  }
  
  checkSum = 256;
  for (i = 0; i < 10; ++i) {
    checkSum += inData[i];
  }
  
  if (highByte(checkSum) != inData[11] || lowByte(checkSum) != inData[10]) {
    return false;
  }  
  return true;
}

/*
 * identify the user through fingerprint
 * return the user id in one byte
 */
byte authenticateUser()
{
  byte value;
  sendCommandToFingerPrint(0x12, 0x01);        // turn on led
  if (!readMsg()) {
    return 20;
  }
  
  sendCommandToFingerPrint(0x26, 0x00);
  if (!readMsg()) {
    return 20;
  }
  if (inData[8] == 0x31) {
    return 20;
  }
  if (inData[4] != 0) {
    return 20;  
  }
  
  sendCommandToFingerPrint(0x60, 0x00);        // capture image
  if (!readMsg()) {
    return 20;
  }
  if (inData[8] == 0x31) {
    return 20;
  }
  
  sendCommandToFingerPrint(0x51, 0x00);
  if (!readMsg()) {
    return 20;
  }
  if (inData[8] == 0x31) {
    return 21;
  }
  if (inData[5] == 0x10 && inData[4] == 0x12) {
    return 21;
  }
  
  value = inData[4];
  sendCommandToFingerPrint(0x12, 0x00);        // turn off led
  readMsg();
  
  return value;
}

void turnOffLED() {
  sendCommandToFingerPrint(0x12, 0x00);        // turn off led
  readMsg();
}

bool regFingerprintSerial(byte id) {
  byte value = 0x01;
  sendCommandToFingerPrint(0x21, id);          // check if the id has been enrolled
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x30) {
    return false;
  }
  sendCommandToFingerPrint(0x12, 0x01);        // turn on led
  if (!readMsg()) {
    return false;
  }
  sendCommandToFingerPrint(0x22, id);          // start enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  while (value != 0x00) {                     // keep checking for finger until it is detected
    sendCommandToFingerPrint(0x26, 0x00);
    if (!readMsg()) {
      return false;
    }
    if (inData[8] == 0x31) {
      return false;
    }
    value = inData[4];
  }
  // Serial.println("=====");
  sendCommandToFingerPrint(0x60, 0x00);        // capture first image
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommandToFingerPrint(0x23, id);          // first enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommandToFingerPrint(0x60, 0x00);        // capture second image
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommandToFingerPrint(0x24, id);          // second enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommandToFingerPrint(0x60, 0x00);        // capture third image
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommandToFingerPrint(0x25, id);          // third enrollment
  if (!readMsg()) {
    return false;
  }
  if (inData[8] == 0x31) {
    return false;
  }
  sendCommandToFingerPrint(0x12, 0x00);        // turn off led
  readMsg();
  return true;
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

// compose a message of 12 bytes and send to fingerprint sensor
void sendCommandToFingerPrint(byte com, byte val){
  valueToWORD(val); 
  calcChecksum(com, highbyte, lowbyte); //This function will calculate the checksum which tells the device that it received all the data
  Serial1.write(0x55); //Command start code 1
  Serial1.write(0xaa); //Command start code 2
  Serial1.write(0x01); // This is the first byte for the device ID. It is the word 0x0001
  Serial1.write(0x00); // Second byte of Device ID. Notice the larger byte is first. I'm assuming this is because the datasheet says "Multi-byte item is represented as Little Endian"
  Serial1.write(lowbyte); //writing the largest byte of the Parameter
  Serial1.write(highbyte); //Writing the second largest byte of the Parameter
  Serial1.write(0x00); //The datasheet says the parameter is a DWORD, but it never seems to go over the value of a word
  Serial1.write(0x00); //so I'm just sending it a word of data. These are the 2 remaining bytes of the Dword
  Serial1.write(com); //write the command byte
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

