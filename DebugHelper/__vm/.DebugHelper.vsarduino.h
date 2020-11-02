/* 
	Editor: https://www.visualmicro.com/
			This file is for intellisense purpose only.
			Visual micro (and the arduino ide) ignore this code during compilation. This code is automatically maintained by visualmicro, manual changes to this file will be overwritten
			The contents of the _vm sub folder can be deleted prior to publishing a project
			All non-arduino files created by visual micro and all visual studio project or solution files can be freely deleted and are not required to compile a sketch (do not delete your own code!).
			Note: debugger breakpoints are stored in '.sln' or '.asln' files, knowledge of last uploaded breakpoints is stored in the upload.vmps.xml file. Both files are required to continue a previous debug session without needing to compile and upload again
	
	Hardware: Generic STM32F1 series, Platform=stm32, Package=STM32
*/

#if defined(_VMICRO_INTELLISENSE)

#ifndef _VSARDUINO_H_
#define _VSARDUINO_H_
#define STM32F1xx
#define ARDUINO 10805
#define ARDUINO_BLUEPILL_F103C8
#define ARDUINO_ARCH_STM32
#define STM32F103xB
#define BL_LEGACY_LEAF
#define VECT_TAB_OFFSET 0x5000
#define __cplusplus 201103L

#define __inline__
#define __asm__(x)
#define __extension__
#define __ATTR_PURE__
#define __ATTR_CONST__
#define __inline__
#define __volatile__

#define GCC_VERSION 60300
#define __GNUC__ 4
#define  __GNUC_MINOR__ 9
#define  __GNUC_PATCHLEVEL__ 2
#define _Static_assert(x)

#undef __cplusplus
#define __cplusplus 201103L
typedef bool _Bool;
#define __ARMCC_VERSION 400678
#define __attribute__(noinline)

extern void at_quick_exit();
void at_quick_exit() {}

extern void quick_exit();
void quick_exit() {}


extern float fabs(double _Xx);
float fabs(double _Xx) {}

extern float fabsf(double _Xx);
float fabsf(double _Xx) {}

extern float fabsl(double _Xx);
float fabsl(double _Xx) {}



typedef int _Bool;
typedef int intmax_t;
typedef int uintmax_t;
typedef int __intmax_t;
typedef unsigned int __uintmax_t;
//#define __INTPTR_TYPE__ +4
#define _INTPTR_EQ_LONG
typedef int __intptr_t;
typedef int __uintptr_t;
#define __INTPTR_TYPE__ int

#define __INT8_TYPE__ int
#define __INT16_TYPE__ int
#define __INT32_TYPE__ long
#define __INT64_TYPE__ double
#define __UINT8_TYPE__ unsigned int
#define __UINT16_TYPE__ unsigned int
#define __UINT32_TYPE__ unsigned long
#define __UINT64_TYPE__ unsigned long

#define __INT_LEAST8_TYPE__ int
#define __INT_LEAST16_TYPE__ int
#define __INT_LEAST32_TYPE__ long
#define __INT_LEAST64_TYPE__ double
#define __UINT_LEAST8_TYPE__ unsigned int
#define __UINT_LEAST16_TYPE__ unsigned int
#define __UINT_LEAST32_TYPE__ unsigned long
#define __UINT_LEAST64_TYPE__ unsigned long

#define __INT_FAST8_TYPE__ int
#define __INT_FAST16_TYPE__ int
#define __INT_FAST32_TYPE__ long
#define __INT_FAST64_TYPE__ double
#define __UINT_FAST8_TYPE__ unsigned int
#define __UINT_FAST16_TYPE__ unsigned int
#define __UINT_FAST32_TYPE__ unsigned long
#define __UINT_FAST64_TYPE__ unsigned long

#define __INTMAX_TYPE__ int;
#define __UINTMAX_TYPE__ unsigned int;

typedef int  false_type;
typedef int true_type;


_PTR 	 memchr(const _PTR, int, size_t);
_PTR 	 memchr(const _PTR, int, size_t) {}
int 	  memcmp(const _PTR, const _PTR, size_t);
_PTR 	  memcpy(_PTR __restrict, const _PTR __restrict, size_t);
_PTR	  memmove(_PTR, const _PTR, size_t);
_PTR	  memset(_PTR, int, size_t);
_PTR	  memset(_PTR, int, size_t) {}
char* strcat(char* __restrict, const char* __restrict);
char* strcat(char* __restrict, const char* __restrict) {}
char* strchr(const char*, int);
char* strchr(const char*, int) {};
int	  strcmp(const char*, const char*);
int	  strcmp(const char*, const char*) {}
int	  strcoll(const char*, const char*);
int	  strcoll(const char*, const char*) {}
char* strcpy(char* __restrict, const char* __restrict);
char* strcpy(char* __restrict, const char* __restrict) {}
size_t	  strcspn(const char*, const char*);
size_t	  strcspn(const char*, const char*) {}
char* strerror(int);
char* strerror(int) {}
size_t	  strlen(const char*);
size_t	  strlen(const char*) {}
char* strncat(char* __restrict, const char* __restrict, size_t);
char* strncat(char* __restrict, const char* __restrict, size_t) {}
int	  strncmp(const char*, const char*, size_t);
int	  strncmp(const char*, const char*, size_t) {}
char* strncpy(char* __restrict, const char* __restrict, size_t);
char* strncpy(char* __restrict, const char* __restrict, size_t) {}
char* strpbrk(const char*, const char*);
char* strpbrk(const char*, const char*) {}
char* strrchr(const char*, int);
char* strrchr(const char*, int) {}
size_t	  strspn(const char*, const char*);
size_t	  strspn(const char*, const char*) {}
char* strstr(const char*, const char*);
char* strstr(const char*, const char*) {}
#ifndef _REENT_ONLY
char* strtok(char* __restrict, const char* __restrict);
char* strtok(char* __restrict, const char* __restrict) {}
#endif
size_t	  strxfrm(char* __restrict, const char* __restrict, size_t);
size_t	  strxfrm(char* __restrict, const char* __restrict, size_t) {}

#if __POSIX_VISIBLE >= 200809
int	 strcoll_l(const char*, const char*, locale_t);
int	 strcoll_l(const char*, const char*, locale_t) {}
char* strerror_l(int, locale_t);
char* strerror_l(int, locale_t) {}
size_t	 strxfrm_l(char* __restrict, const char* __restrict, size_t, locale_t);
size_t	 strxfrm_l(char* __restrict, const char* __restrict, size_t, locale_t) {}
#endif

#if __MISC_VISIBLE || __POSIX_VISIBLE
char* strtok_r(char* __restrict, const char* __restrict, char** __restrict);
char* strtok_r(char* __restrict, const char* __restrict, char** __restrict) {}
#endif

#if __BSD_VISIBLE
int	  timingsafe_bcmp(const void*, const void*, size_t);
int	  timingsafe_bcmp(const void*, const void*, size_t) {}
int	  timingsafe_memcmp(const void*, const void*, size_t);
int	  timingsafe_memcmp(const void*, const void*, size_t) {}
#endif

#if __MISC_VISIBLE || __POSIX_VISIBLE
//_PTR	  memccpy (_PTR __restrict, const _PTR __restrict, int, size_t);
#endif

#if __GNU_VISIBLE
_PTR	  mempcpy(_PTR, const _PTR, size_t);
_PTR	  memmem(const _PTR, size_t, const _PTR, size_t);
_PTR 	  memrchr(const _PTR, int, size_t);
_PTR 	  rawmemchr(const _PTR, int);
#endif

#if __POSIX_VISIBLE >= 200809
char* stpcpy(char* __restrict, const char* __restrict);
char* stpcpy(char* __restrict, const char* __restrict) {}
char* stpncpy(char* __restrict, const char* __restrict, size_t);
char* stpncpy(char* __restrict, const char* __restrict, size_t) {}
#endif

#if __GNU_VISIBLE
char* strcasestr(const char*, const char*);
char* strchrnul(const char*, int);
#endif

#if __MISC_VISIBLE || __POSIX_VISIBLE >= 200809 || __XSI_VISIBLE >= 4
char* strdup(const char*);
char* strdup(const char*) {}
#endif
char* _strdup_r(struct _reent*, const char*);
char* _strdup_r(struct _reent*, const char*) {}
#if __POSIX_VISIBLE >= 200809
char* strndup(const char*, size_t);
char* strndup(const char*, size_t) {}
#endif
char* _strndup_r(struct _reent*, const char*, size_t);
char* _strndup_r(struct _reent*, const char*, size_t) {}









#include "arduino.h"
#include <variant.h> 
#include <variant.cpp> 
#undef cli
#define cli()
#include "DebugHelper.ino"
#endif
#endif
