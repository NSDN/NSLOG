#ifndef __LCD_H
#define __LCD_H

#include <stdint.h>

__sbit __at (0x97)         P17;
__sbit __at (0x96)         P16;
__sbit __at (0x95)         P15;
__sbit __at (0x94)         P14;

#define LEN     P14
#define LBK     P15
#define SDA     P16
#define SCL     P17

void lcdInit();
void lcdDraw(uint8_t x, uint8_t y, char c);
void lcdPrint(uint8_t x, uint8_t y, char* str);
void lcdClear();

#endif
