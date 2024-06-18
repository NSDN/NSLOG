# include "usb.h"

#include <stdlib.h>
#include <string.h>
#include "ch552.h"

#define THIS_ENDP0_SIZE DEFAULT_ENDP0_SIZE

const uint8_c usbDevDesc[] = {
    0x12,               // 描述符长度(18字节)
    0x01,               // 描述符类型
    0x00, 0x02,         //本设备所用USB版本(2.0)
    0x00,               // 类代码
    0x00,               // 子类代码
    0x00,               // 设备所用协议
    THIS_ENDP0_SIZE,    // 端点0最大包长
    0x32, 0x32,         // 厂商ID
    0x2E, 0x00,         // 产品ID, NSDN-PCB 0046
    0x00, 0x01,         // 设备版本号 (1.0)
    0x01,               // 描述厂商信息的字符串描述符的索引值
    0x02,               // 描述产品信息的字串描述符的索引值
    0x03,               // 描述设备序列号信息的字串描述符的索引值
    0x01                // 可能的配置数
};

const uint8_c usbCfgDesc[] = {
    0x09,        //   bLength
    0x02,        //   bDescriptorType (Configuration)
    0x29, 0x00,  //   wTotalLength 41
    0x01,        //   bNumInterfaces 1
    0x01,        //   bConfigurationValue
    0x04,        //   iConfiguration (String Index)
    0xA0,        //   bmAttributes Remote Wakeup
    0x64,        //   bMaxPower 200mA

/* -------------------------------- */
//  Custom HID
    0x09,        //   bLength
    0x04,        //   bDescriptorType (Interface)
    0x00,        //   bInterfaceNumber 0
    0x00,        //   bAlternateSetting
    0x02,        //   bNumEndpoints 2
    0x03,        //   bInterfaceClass
    0x00,        //   bInterfaceSubClass
    0x00,        //   bInterfaceProtocol
    0x05,        //   iInterface (String Index)

    0x09,        //   bLength
    0x21,        //   bDescriptorType (HID)
    0x10, 0x01,  //   bcdHID 1.10
    0x00,        //   bCountryCode
    0x01,        //   bNumDescriptors
    0x22,        //   bDescriptorType[0] (HID)
    0x24, 0x00,  //   wDescriptorLength[0] 36

    0x07,        // bLength
    0x05,        // bDescriptorType (Endpoint)
    0x83,        // bEndpointAddress (IN/D2H)
    0x03,        // bmAttributes (Interrupt)
    0x40, 0x00,  // wMaxPacketSize 64
    0x00,        // bInterval 0 (unit depends on device speed)

    0x07,        // bLength
    0x05,        // bDescriptorType (Endpoint)
    0x03,        // bEndpointAddress (OUT/H2D)
    0x03,        // bmAttributes (Interrupt)
    0x40, 0x00,  // wMaxPacketSize 64
    0x00,        // bInterval 0 (unit depends on device speed)
};

/*HID类报表描述符*/
const uint8_c CustomRepDesc[] = {
    0x06, 0x00, 0xFF,  // Usage Page (Vendor Defined 0xFF00)
    0x09, 0x01,        // Usage (0x01)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0xAA,        //   Report ID (170)
    0x95, HID_BUF_SIZE,//   Report Count (XX)
    0x75, 0x08,        //   Report Size (8)
    0x25, 0x01,        //   Logical Maximum (1)
    0x15, 0x00,        //   Logical Minimum (0)
    0x09, 0x01,        //   Usage (0x01)
    0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
    0x85, 0x55,        //   Report ID (85)
    0x95, HID_BUF_SIZE,//   Report Count (XX)
    0x75, 0x08,        //   Report Size (8)
    0x25, 0x01,        //   Logical Maximum (1)
    0x15, 0x00,        //   Logical Minimum (0)
    0x09, 0x01,        //   Usage (0x01)
    0x91, 0x02,        //   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
    0xC0,              // End Collection
};

const uint8_c usbLangDesc[] = { 0x04, 0x03, 0x04, 0x08 };
const uint8_c usbManuDesc[] = { 0x0A, 0x03, 'N', 0, 'S', 0, 'D', 0, 'N', 0 };
const uint8_c usbProdDesc[] = { 0x1A, 0x03,
    'N',0x00,'S',0x00,'L',0x00,'O',0x00,'G',0x00,' ',0x00,
    'D',0x00,'e',0x00,'v',0x00,'i',0x00,'c',0x00,'e',0x00
};
const uint8_c usbSerialDesc[] = { 0x0A, 0x03, '0', 0, '0', 0, '0', 0, '0', 0 };
const uint8_c usbCfgStrDesc[] = { 0x12, 0x03, 'N', 0, 'S', 0, 'D', 0, 'N', 0, ' ', 0, 'U', 0, 'S', 0, 'B', 0 };
const uint8_c usbCusStrDesc[] = { 0x18, 0x03, 'N', 0, 'S', 0, 'D', 0, 'N', 0, ' ', 0, 'C', 0, 'u', 0, 's', 0, 't', 0, 'o', 0, 'm', 0 };

// START at 0x0000
uint8_x __at (0x0000) Ep0Buffer[THIS_ENDP0_SIZE];                                           //端点0 OUT&IN缓冲区，必须是偶地址
uint8_x __at (0x0008) Ep3Buffer[2 * MAX_PACKET_SIZE];                                       //端点3 OUT&IN缓冲区,必须是偶地址'
// END at 0x0088
uint8_x HIDInput[HID_BUF_SIZE] = { 0 };                                                     //自定义HID接收缓冲
uint8_x HIDOutput[HID_BUF_SIZE] = { 0 };                                                    //自定义HID发送缓冲
uint8_t             SetupReqCode, SetupLen, Count, FLAG, UsbConfig;
uint8_t*            pDescr;                                                                 //USB配置标志
volatile __bit      HIDIN = 0;
#define UsbSetupBuf ((PUSB_SETUP_REQ)Ep0Buffer)

void __usbDeviceInterrupt() __interrupt (INT_NO_USB) __using (1) {
    uint8_t len;
	if (UIF_TRANSFER) {                                                                     // USB传输完成
		switch (USB_INT_ST & (MASK_UIS_TOKEN | MASK_UIS_ENDP)) {                            // 分析操作令牌和端点号
				case UIS_TOKEN_OUT | 3:                                                     // endpoint 3# 中断端点下传
					if (U_TOG_OK) {                                                         // 不同步的数据包将丢弃
						len = USB_RX_LEN;
						if (Ep3Buffer[0] == 0x55 && (len - 1) <= sizeof(HIDInput) && !HIDIN) {
							memset(HIDInput, 0x00, sizeof(HIDInput));
							memcpy(HIDInput, Ep3Buffer + 1, len - 1);
							HIDIN = 1;
						}
					}
					break;
				case UIS_TOKEN_IN | 3:                                                      //endpoint 3# 中断端点上传
					UEP3_T_LEN = 0;                                                         //预使用发送长度一定要清空
                    UEP3_CTRL = UEP3_CTRL & ~ MASK_UEP_T_RES | UEP_T_RES_NAK;               //默认应答NAK
					break;
				case UIS_TOKEN_SETUP | 0:                                                   // endpoint 0# SETUP
					len = USB_RX_LEN;
					if (len == sizeof(USB_SETUP_REQ)) {                                     // SETUP包长度
						SetupLen = UsbSetupBuf->wLengthL;
						if (UsbSetupBuf->wLengthH || SetupLen > 0x7F) SetupLen = 0x7F;      // 限制总长度
						len = 0;                                                            // 默认为成功并且上传0长度
						SetupReqCode = UsbSetupBuf->bRequest;
						if ((UsbSetupBuf->bRequestType & USB_REQ_TYP_MASK) != USB_REQ_TYP_STANDARD) { /* 非标准请求 */
                            switch (SetupReqCode) {
                                case 0x01://GetReport
                                    break;
                                case 0x02://GetIdle
                                    break;
                                case 0x03://GetProtocol
                                    break;
                                case 0x09://SetReport
                                    break;
                                case 0x0A://SetIdle
                                    break;
                                case 0x0B://SetProtocol
                                    break;
                                default:
                                    len = 0xFF;  								            /*命令不支持*/
                                    break;
					        }
						} else {                                                            // 标准请求
							switch (SetupReqCode) {                                         // 请求码
								case USB_GET_DESCRIPTOR:
									switch (UsbSetupBuf->wValueH) {
										case 1:                                             // 设备描述符
											pDescr = (uint8_t*) (&usbDevDesc[0]);
											len = sizeof(usbDevDesc);
											break;
										case 2:                                             // 配置描述符
											pDescr = (uint8_t*) (&usbCfgDesc[0]);
											len = sizeof(usbCfgDesc);
											break;
										case 3:                                             // 字符串描述符
											switch(UsbSetupBuf->wValueL) {
                                                case 0:
													pDescr = (uint8_t*) (&usbLangDesc[0]);
													len = sizeof(usbLangDesc);
													break;
												case 1:
													pDescr = (uint8_t*) (&usbManuDesc[0]);
													len = sizeof(usbManuDesc);
													break;
												case 2:
													pDescr = (uint8_t*) (&usbProdDesc[0]);
													len = sizeof(usbProdDesc);
													break;
												case 3:
                                                    pDescr = (uint8_t*) (&usbSerialDesc[0]);
                                                    len = sizeof(usbSerialDesc);
                                                    break;
                                                case 4:
                                                    pDescr = (uint8_t*) (&usbCfgStrDesc[0]);
                                                    len = sizeof(usbCfgStrDesc);
                                                    break;
                                                case 5:
                                                    pDescr = (uint8_t*) (&usbCusStrDesc[0]);
                                                    len = sizeof(usbCusStrDesc);
                                                    break;
												default:
													len = 0xFF;                             // 不支持的字符串描述符
													break;
											}
											break;
                                        case 0x22:                                          //报表描述符
                                            if (UsbSetupBuf->wIndexL == 0) {                //接口0报表描述符
                                                pDescr = (uint8_t*) (&CustomRepDesc[0]);    //数据准备上传
                                                len = sizeof(CustomRepDesc);
                                            } else {
                                                len = 0xff;                                 //本程序只有1个接口，这句话正常不可能执行
                                            }
                                            break;
										default:
											len = 0xFF;                                     // 不支持的描述符类型
											break;
									}
									if (SetupLen > len) SetupLen = len;                     // 限制总长度
									len = SetupLen >= THIS_ENDP0_SIZE ? THIS_ENDP0_SIZE : SetupLen;  // 本次传输长度
									memcpy(Ep0Buffer, pDescr, len);                         /* 加载上传数据 */
									SetupLen -= len;
									pDescr += len;
									break;
								case USB_SET_ADDRESS:
									SetupLen = UsbSetupBuf->wValueL;                        // 暂存USB设备地址
									break;
								case USB_GET_CONFIGURATION:
									Ep0Buffer[0] = UsbConfig;
									if (SetupLen >= 1) len = 1;
									break;
								case USB_SET_CONFIGURATION:
									UsbConfig = UsbSetupBuf->wValueL;
									break;
								case USB_CLEAR_FEATURE:
									if ((UsbSetupBuf->bRequestType & USB_REQ_RECIP_MASK) == USB_REQ_RECIP_ENDP) {  // 端点
										switch (UsbSetupBuf->wIndexL) {
											case 0x83:
												UEP3_CTRL = UEP3_CTRL & ~ ( bUEP_T_TOG | MASK_UEP_T_RES ) | UEP_T_RES_NAK;
												break;
                                            case 0x03:
												UEP3_CTRL = UEP3_CTRL & ~ ( bUEP_R_TOG | MASK_UEP_R_RES ) | UEP_R_RES_ACK;
												break;
											default:
                                            len = 0xFF;                                                             // 不支持的端点
												break;
										}
									}
									if ((UsbSetupBuf->bRequestType & USB_REQ_RECIP_MASK) == USB_REQ_RECIP_DEVICE) { // 设备
                                        break;
                                    } else {
                                        len = 0xFF;                                                                 // 不是端点不支持
                                    }
									break;
                                case USB_SET_FEATURE:                                                               /* Set Feature */
                                    if ((UsbSetupBuf->bRequestType & 0x1F) == 0x00) {                               /* 设置设备 */
                                        if ((((uint16_t) UsbSetupBuf->wValueH << 8) | UsbSetupBuf->wValueL) == 0x01)  {
                                            if (usbCfgDesc[7] & 0x20) {
                                                /* 设置唤醒使能标志 */
                                            } else {
                                                len = 0xFF;                                                         /* 操作失败 */
                                            }
                                        } else {
                                            len = 0xFF;                                                             /* 操作失败 */
                                        }
                                    } else if ((UsbSetupBuf->bRequestType & 0x1F) == 0x02) {                        /* 设置端点 */
                                        if ((((uint16_t) UsbSetupBuf->wValueH << 8) | UsbSetupBuf->wValueL ) == 0x00) {
                                            switch (((uint16_t) UsbSetupBuf->wIndexH << 8 ) | UsbSetupBuf->wIndexL) {
                                            case 0x83:
												UEP3_CTRL = UEP3_CTRL & (~bUEP_T_TOG) | UEP_T_RES_STALL;            /* 设置端点3 IN STALL */
												break;
                                            case 0x03:
												UEP3_CTRL = UEP3_CTRL & (~bUEP_R_TOG) | UEP_R_RES_STALL;            /* 设置端点3 OUT STALL */
												break;
                                            default:
                                                len = 0xFF;                                                         //操作失败
                                                break;
                                            }
                                        } else {
                                            len = 0xFF;                                                             //操作失败
                                        }
                                    } else {
                                        len = 0xFF;                                                                 //操作失败
                                    }
                                    break;
								case USB_GET_INTERFACE:
									Ep0Buffer[0] = 0x00;
									if (SetupLen >= 1) len = 1;
									break;
								case USB_GET_STATUS:
									Ep0Buffer[0] = 0x00;
									Ep0Buffer[1] = 0x00;
									if (SetupLen >= 2) len = 2;
									else len = SetupLen;
									break;
								default:
									len = 0xFF;                                                                     // 操作失败
									break;
							}
						}
					} else {
						len = 0xFF;                                                                                 // SETUP包长度错误
					}
					if (len == 0xFF) {                                                                              // 操作失败
						SetupReqCode = 0xFF;
						UEP0_CTRL = bUEP_R_TOG | bUEP_T_TOG | UEP_R_RES_STALL | UEP_T_RES_STALL;                    // STALL
					} else if (len <= THIS_ENDP0_SIZE) {                                                            // 上传数据或者状态阶段返回0长度包
						UEP0_T_LEN = len;
						UEP0_CTRL = bUEP_R_TOG | bUEP_T_TOG | UEP_R_RES_ACK | UEP_T_RES_ACK;                        // 默认数据包是DATA1
					} else {                                                                                        // 下传数据或其它
						UEP0_T_LEN = 0;                                                                             // 虽然尚未到状态阶段，但是提前预置上传0长度数据包以防主机提前进入状态阶段
						UEP0_CTRL = bUEP_R_TOG | bUEP_T_TOG | UEP_R_RES_ACK | UEP_T_RES_ACK;                        // 默认数据包是DATA1
					}
					break;
				case UIS_TOKEN_IN | 0:                                                                              // endpoint 0# IN
					switch (SetupReqCode) {
						case USB_GET_DESCRIPTOR:
							len = SetupLen >= THIS_ENDP0_SIZE ? THIS_ENDP0_SIZE : SetupLen;                         // 本次传输长度
							memcpy(Ep0Buffer, pDescr, len);                                                         /* 加载上传数据 */
							SetupLen -= len;
							pDescr += len;
							UEP0_T_LEN = len;
							UEP0_CTRL ^= bUEP_T_TOG;                                                                // 翻转
							break;
						case USB_SET_ADDRESS:
							USB_DEV_AD = USB_DEV_AD & bUDA_GP_BIT | SetupLen;
							UEP0_CTRL = UEP_R_RES_ACK | UEP_T_RES_NAK;
							break;
						default:
							UEP0_T_LEN = 0;                                                                         // 状态阶段完成中断或者是强制上传0长度数据包结束控制传输
							UEP0_CTRL = UEP_R_RES_ACK | UEP_T_RES_NAK;
							break;
					}
					break;
				case UIS_TOKEN_OUT | 0:                                                                             // endpoint 0# OUT
					UEP0_CTRL = UEP_R_RES_ACK | UEP_T_RES_NAK;                                              		// 准备下一控制传输
                    UEP0_CTRL ^= bUEP_R_TOG;                                                                        //同步标志位翻转
					break;
				default:
					break;
			}
		UIF_TRANSFER = 0;                                                                                           // 清中断标志
	} else if (UIF_BUS_RST) {                                                                                       // USB总线复位
		UEP0_CTRL = UEP_R_RES_ACK | UEP_T_RES_NAK;
        UEP3_CTRL = bUEP_AUTO_TOG | UEP_R_RES_ACK | UEP_T_RES_NAK;
		USB_DEV_AD = 0x00;
		UIF_SUSPEND = 0;
		UIF_TRANSFER = 0;
		UIF_BUS_RST = 0;                                                                                            // 清中断标志
	} else if (UIF_SUSPEND) {                                                                                       // USB总线挂起/唤醒完成
		UIF_SUSPEND = 0;
		if (USB_MIS_ST & bUMS_SUSPEND) {                                                                          // 挂起
			while (XBUS_AUX & bUART0_TX);                                                                         // 等待发送完成
			SAFE_MOD = 0x55;
			SAFE_MOD = 0xAA;
			WAKE_CTRL = bWAK_BY_USB | bWAK_RXD0_LO;                                                                 // USB或者RXD0有信号时可被唤醒
			PCON |= PD;                                                                                             // 睡眠
			SAFE_MOD = 0x55;
			SAFE_MOD = 0xAA;
			WAKE_CTRL = 0x00;
		} else {                                                                                                    // 唤醒

		}
	} else {                                                                                                        // 意外的中断,不可能发生的情况
		USB_INT_FG = 0xFF;                                                                                          // 清中断标志
	}
}

void usbDevInit() {
	IE_USB = 0;
	USB_CTRL = 0x00;                                                           // 先设定USB设备模式
	UDEV_CTRL = bUD_PD_DIS;                                                    // 禁止DP/DM下拉电阻
    UDEV_CTRL &= ~bUD_LOW_SPEED;                                               //选择全速12M模式，默认方式
    USB_CTRL &= ~bUC_LOW_SPEED;

    UEP0_DMA = (uint16_t) (&Ep0Buffer[0]);                                      //端点0数据传输地址
    UEP4_1_MOD &= ~(bUEP4_RX_EN | bUEP4_TX_EN);                                 //端点0单64字节收发缓冲区
    UEP0_CTRL = UEP_R_RES_ACK | UEP_T_RES_NAK;                                  //OUT事务返回ACK，IN事务返回NAK
    UEP3_DMA = (uint16_t) (&Ep3Buffer[0]);                                      //端点3数据传输地址s
    UEP2_3_MOD = UEP2_3_MOD & ~bUEP3_BUF_MOD | bUEP3_RX_EN | bUEP3_TX_EN;       //端点3收发使能 128字节缓冲区
    UEP3_CTRL = bUEP_AUTO_TOG | UEP_R_RES_ACK | UEP_T_RES_NAK;                  //OUT事务返回ACK，IN事务返回NAK

	USB_DEV_AD = 0x00;
	USB_CTRL |= bUC_DEV_PU_EN | bUC_INT_BUSY | bUC_DMA_EN;                      // 启动USB设备及DMA，在中断期间中断标志未清除前自动返回NAK
    USB_CTRL |= bUC_SYS_CTRL1;
	UDEV_CTRL |= bUD_PORT_EN;                                                   // 允许USB端口
	USB_INT_FG = 0xFF;                                                          // 清中断标志
	USB_INT_EN = bUIE_SUSPEND | bUIE_TRANSFER | bUIE_BUS_RST;
	IE_USB = 1;

    UEP3_T_LEN = 0;

    FLAG = 1;
}

void Enp3IntIn( ) {
    Ep3Buffer[MAX_PACKET_SIZE] = 0xAA;
    memcpy(Ep3Buffer + MAX_PACKET_SIZE + 1, HIDOutput, sizeof(HIDOutput));      //加载上传数据
    UEP3_T_LEN = sizeof(HIDOutput) + 1;                                         //上传数据长度
    UEP3_CTRL = UEP3_CTRL & ~ MASK_UEP_T_RES | UEP_T_RES_ACK;                   //有数据时上传数据并应答ACK
}

uint8_t getHIDData(uint8_t index) {
    return HIDInput[index % sizeof(HIDInput)];
}

void setHIDData(uint8_t index, uint8_t data) {
    HIDOutput[index % sizeof(HIDOutput)] = data;
}

__bit hasHIDData() {
    return HIDIN;
}

void requestHIDData() {
    HIDIN = 0;
}

void pushHIDData() {
    Enp3IntIn();
}

uint8_t* fetchHIDData() {
    return HIDInput;
}

void hidPrint(const char* str) {
	memset(HIDOutput, 0x00, sizeof(HIDOutput));
	strcpy(HIDOutput, str);
	Enp3IntIn();
}
