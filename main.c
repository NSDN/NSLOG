#include "ch552.h"
#include "sys.h"
#include "usb.h"
#include "lcd.h"

#include <string.h>

void __usbDeviceInterrupt() __interrupt (INT_NO_USB) __using (1);
extern uint8_t FLAG;

uint8_t* ptr;
char strBuf[16]; uint8_t tmp;

void main() {
    LEN = 0; LBK = 0; SDA = 0; SCL = 1;

    sysClockConfig();
    delay(5);

    usbDevInit();
    EA = 1;

    delay(500);
    usbReleaseAll();
    usbPushKeydata();
    requestHIDData();

    lcdInit();
    lcdClear();
    LBK = 1;

    lcdPrint(2, 0, "NSLOG Device");
    delay(400);
    memset(strBuf, '-', 8);
    strBuf[8] = '\0';
    for (uint8_t i = 0; i < 9; i++) {
        lcdPrint(4, 1, strBuf);
        if (i < 8) {
            strBuf[i] = '=';
            delay(200);
        }
    }
    memset(strBuf, '\0', 16);
    delay(1000);
    lcdClear();

    while (1) {
        if (hasHIDData()) {
            ptr = fetchHIDData();
            switch (ptr[0]) {
                case 0x00:
                    lcdClear();
                    break;
                case 0x01:
                    memcpy(strBuf, ptr, 16);
                    lcdPrint(0, 0, strBuf);
                    memcpy(strBuf, ptr + 16, 16);
                    lcdPrint(0, 1, strBuf);
                    break;
                default:
                    tmp = ptr[0];
                    if (tmp & 0x20) {
                        tmp &= 0x1F;
                        lcdDraw(tmp % 16, tmp / 16, ptr[1]);
                    }
                    break;
            }

            requestHIDData();
        }
    }
    
}
