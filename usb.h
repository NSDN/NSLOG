#ifndef __USB_H
#define __USB_H

#include <stdint.h>

#define HID_BUF_SIZE 42

void usbDevInit();

uint8_t getHIDData(uint8_t index);
void setHIDData(uint8_t index, uint8_t data);
__bit hasHIDData();
void requestHIDData();
void pushHIDData();
uint8_t* fetchHIDData();

void hidPrint(const char* str);

#endif
