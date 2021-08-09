#include "lcd.h"
#include "sys.h"

void bus_init() {
    SDA = 0; LEN = 0; SCL = 1;
    for (uint8_t i = 0; i < 8; i++) {
        SCL = 0; delay_us(1);
        SCL = 1; delay_us(1);
    }
}

void write_byte(uint8_t v) {
    for (uint8_t i = 0; i < 8; i++) {
        SCL = 0;
        SDA = (v & 0x80);
        delay_us(1);
        SCL = 1;
        delay_us(1);
        v <<= 1;
    }
}

void write_cmd(uint8_t v) {
    bus_init();
    write_byte(v);
    SCL = 0; // RS = 0
    LEN = 0;
    delay_us(5);
    LEN = 1;
    delay_us(5);
    LEN = 0;

    delay(2); // wait for busy, verified
}

void write_dat(uint8_t v) {
    bus_init();
    write_byte(v);
    SCL = 1; // RS = 1;
    LEN = 0;
    delay_us(5);
    LEN = 1;
    delay_us(5);
    LEN = 0;

    delay(2); // wait for busy, verified
}

void lcdInit() {
	write_cmd(0x38);
	write_cmd(0x08);
	write_cmd(0x06);
	write_cmd(0x01);
	write_cmd(0x0C);
	write_cmd(0x80);
}

void lcdDraw(uint8_t x, uint8_t y, char c) {
	write_cmd(0x80 + ((y & 0x01) ? 0x40 : 0) + ((y > 1) ? 0x14 : 0) + x);
	write_dat(c);
}

void lcdPrint(uint8_t x, uint8_t y, char* str) {
	uint8_t i; uint8_t tx = x, ty = y;
	for (i = 0; str[i] != '\0'; i++) {
		if (str[i] == '\n') {
			tx = x;
			ty = 1 - ty;
		} else {
			lcdDraw(tx + i, ty, str[i]);
		}
	}
}

void lcdClear() {
	write_cmd(0x01);
	delay(10);
}
