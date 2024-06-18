#include "ch552.h"
#include "sys.h"
#include "usb.h"
#include "lcd.h"

#include <string.h>

void __usbDeviceInterrupt() __interrupt (INT_NO_USB) __using (1);
extern uint8_t FLAG;

uint8_t* ptr;
char strBuf[21]; uint8_t tmp;

void main() {
    LEN = 0; LBK = 0; SDA = 0; SCL = 1;

    sysClockConfig();
    delay(5);

    usbDevInit();
    EA = 1;

    delay(500);
    requestHIDData();

    lcdInit();
    lcdClear();
    LBK = 1;

    lcdPrint(4, 1, "NSLOG Device");
    delay(400);
    memset(strBuf, '-', 8);
    strBuf[8] = '\0';
    for (uint8_t i = 0; i < 9; i++) {
        lcdPrint(6, 2, strBuf);
        if (i < 8) {
            strBuf[i] = '=';
            delay(100);
        }
    }
    memset(strBuf, '\0', 21);
    delay(500);
    lcdClear();

    while (1) {
        if (hasHIDData()) {
            ptr = fetchHIDData();
            switch (ptr[0]) {
                case 0x01:
                    memcpy(strBuf, &ptr[1], 20);
                    lcdPrint(0, 0, strBuf);
                    break;
                case 0x02:
                    memcpy(strBuf, &ptr[1], 20);
                    lcdPrint(0, 1, strBuf);
                    break;
                case 0x03:
                    memcpy(strBuf, &ptr[1], 20);
                    lcdPrint(0, 2, strBuf);
                    break;
                case 0x04:
                    memcpy(strBuf, &ptr[1], 20);
                    lcdPrint(0, 3, strBuf);
                    break;
                case 0xFE:
                    lcdClear();
                    break;
                case 0xFF:
                    lcdDraw(ptr[1], ptr[2], ptr[3]);
                    break;
            }

            requestHIDData();
        }
    }
    
}
