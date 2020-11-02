/* 
	Editor: https://www.visualmicro.com/
			This file is for intellisense purpose only.
			Visual micro (and the arduino ide) ignore this code during compilation. This code is automatically maintained by visualmicro, manual changes to this file will be overwritten
			The contents of the _vm sub folder can be deleted prior to publishing a project
			All non-arduino files created by visual micro and all visual studio project or solution files can be freely deleted and are not required to compile a sketch (do not delete your own code!).
			Note: debugger breakpoints are stored in '.sln' or '.asln' files, knowledge of last uploaded breakpoints is stored in the upload.vmps.xml file. Both files are required to continue a previous debug session without needing to compile and upload again
	
	Hardware: Generic STM32F103C series, Platform=STM32F1, Package=Arduino_STM32
*/

#if defined(_VMICRO_INTELLISENSE)

#ifndef _VSARDUINO_H_
#define _VSARDUINO_H_
#define DEBUG_LEVEL DEBUG_NONE
#define BOARD_generic_stm32f103c
#define VECT_TAB_ADDR 0x8002000
#define ERROR_LED_PORT GPIOC
#define ERROR_LED_PIN 13
#define F_CPU 72000000L
#define ARDUINO 10805
#define ARDUINO_GENERIC_STM32F103C
#define ARDUINO_ARCH_STM32F1
#define SERIAL_USB
#define GENERIC_BOOTLOADER
#define MCU_STM32F103C8
#define __STM32F1__
#define __cplusplus 201103L

#define __inline__
#define __asm__(x)
#define __extension__
#define __ATTR_PURE__
#define __ATTR_CONST__
#define __inline__
#define __volatile__

#define __GNUC__ 6
#define  __GNUC_MINOR__ 3
#define  __GNUC_PATCHLEVEL__ 0
#define _Static_assert(x)
#define ___int8_t_defined
#define ___int16_t_defined
#define ___int32_t_defined
#define ___int64_t_defined


#undef __cplusplus
#define __cplusplus 201103L
typedef bool _Bool;
#define __ARMCC_VERSION 400678
#define __attribute__(noinline)

#include "arduino.h"
#include <pins_arduino.h> 
#include <variant.h> 
#include <board.cpp> 
#undef cli
#define cli()
#include "stm32_ads1220.ino"
#endif
#endif
