/*
 Name:		stm32_ads1220.ino
 Created:	31.10.2020 22:02:04
 Author:	Павел
*/

#include "Protocentral_ADS1220.h"
#include <RTClock.h>

/* Constant definitions */

#define PIN_ADC_CS PA4
#define PIN_ADC_DRDY PB0
#define WAIT_FOR_PC 10000 //mS
#define PC_BUFFER_LEN 32 //bytes
#define REFERENCE_VOLTAGE 2.048 //V
#define ADC_FULL_SCALE 0x7FFFFF //3-byte-wide integer
#define ACQUISITION_TIMER_NUMBER 2
#define ACQUISITION_PERIOD 25E4 //uS, 25E4 = 0.25 S = 4 Hz

int adc_module_channels[] = { MUX_AIN0_AIN1, MUX_AIN2_AIN3 };
float calibration_coefficients[] = { 1, 1 };
float calibration_offset[] = { 0.000005, 0.000005 };

/* Globals */

ADS1220 adc_module;
USBSerial usb_serial;
RTClock rtc_clock;
HardwareTimer acquisition_timer(ACQUISITION_TIMER_NUMBER);

time_t acquisition_limit;

#define STATUS_ACQUISITION _BV(0)
#define STATUS_RTC_ALARM _BV(1)
#define STATUS_STOP_REQ _BV(2)
#define STATUS_TIMER_OVF _BV(3)
#define STATUS_START_REQ _BV(4)
volatile uint8_t status = 0;

/* ISRs */

void rtc_alarm_isr()
{
	status |= STATUS_RTC_ALARM;
}

void acq_timer_isr()
{
	status |= STATUS_TIMER_OVF;
}

/* Prototypes */

template<typename T> void usb_serial_print(T);
template<typename T> void usb_serial_println(T);
void acquisition();
void process_command();

/* Functions */

template<typename T, size_t s> constexpr size_t arraySize(T(&)[s]) { return s; }

template<typename T> void usb_serial_println(T arg, int i)
{
	if (usb_serial) usb_serial.println(arg, i);
}
template<typename T> void usb_serial_println(T arg)
{
	if (usb_serial) usb_serial.println(arg);
}
template<typename T> void usb_serial_print(T arg)
{
	if (usb_serial) usb_serial.print(arg);
}

void reset_rtc(time_t limit)
{
	rtc_clock.setTime(0);
	rtc_clock.setAlarmTime(limit);
	status &= ~STATUS_RTC_ALARM;
}

void acquisition()
{
	if (status & STATUS_ACQUISITION)
	{
		usb_serial_println("RECURSION!");
		return;
	}
	float buffer[arraySize(adc_module_channels)];
	for (uint8_t i = 0; i < arraySize(buffer); i++)
	{
		buffer[i] = ADS1220_NO_DATA;
	}
	status |= STATUS_ACQUISITION;
	reset_rtc(acquisition_limit);
	acquisition_timer.refresh();
	while (!(status & (STATUS_STOP_REQ | STATUS_RTC_ALARM)))
	{
		for (size_t i = 0; i < arraySize(adc_module_channels); i++)
		{
			adc_module.select_mux_channels(adc_module_channels[i]);
			adc_module.start_conversion(); //Discard the first conversion after MUX switching
			if (buffer[i] != ADS1220_NO_DATA)
			{
				buffer[i] = (buffer[i] * REFERENCE_VOLTAGE) / ADC_FULL_SCALE; //While waiting, compute last result for this channel
				buffer[i] = buffer[i] * calibration_coefficients[i] + calibration_offset[i];
			}
			usb_serial_print(i); usb_serial_print(": "); //Print current channel index
			adc_module.read_result_blocking();
			adc_module.start_conversion();
			if (buffer[i] != ADS1220_NO_DATA)
			{
				usb_serial_println(buffer[i], 6); //And print it (microvolts precision)
			}
			else
			{
				usb_serial_println("NOT_READY!");
			}
			buffer[i] = adc_module.read_result_blocking(); //Use second result
		}
		//Wait for next point time
		if (usb_serial.available()) process_command(); //While waiting again, check for any commands (A = STOP in particular)
		while (!(status & STATUS_TIMER_OVF));
		status &= ~STATUS_TIMER_OVF;
	}
	usb_serial_println("FINISHED.");
	status &= ~(STATUS_ACQUISITION | STATUS_STOP_REQ);
}

void process_command()
{
	static char buf[PC_BUFFER_LEN];
	static size_t len;
	len += usb_serial.readBytes(buf + len, arraySize(buf) - len);
	if (len == PC_BUFFER_LEN)
	{
		usb_serial_println("OVERFLOW!");
		usb_serial.flush();
		return;
	}
	if (len < 2) return;
	if (buf[len - 1] != '\n') return;
	len -= (buf[len - 2] == '\r') ? 2 : 1;
	bool has_args = len > 1;
	switch (*buf)
	{
	case 'A':
		if (status & STATUS_ACQUISITION)
		{
			status |= STATUS_STOP_REQ;
		}
		else
		{
			if (has_args) acquisition_limit = atoi(buf + 1);
			status |= STATUS_START_REQ;
		}
		break;
	default:
		usb_serial_println("UNKNOWN!");
		break;
	}
	usb_serial.flush();
	len = 0;
	usb_serial_println("PARSED.");
}

/* Main */

void setup() {
	static_assert(arraySize(calibration_coefficients) >= arraySize(adc_module_channels), "Missing calibration coefficients!");
	static_assert(arraySize(calibration_offset) >= arraySize(adc_module_channels), "Missing calibration offsets!");

	adc_module.begin(PIN_ADC_CS, PIN_ADC_DRDY);
	adc_module.set_pga_gain(PGA_GAIN_1);
	adc_module.set_data_rate(DR_20SPS);
	rtc_clock.attachAlarmInterrupt(rtc_alarm_isr);
	acquisition_timer.attachInterrupt(0, acq_timer_isr);
	acquisition_timer.setPeriod(ACQUISITION_PERIOD);
	pinMode(LED_BUILTIN, OUTPUT_OPEN_DRAIN);
	digitalWrite(LED_BUILTIN, LOW);
	for (uint32_t i = 0; i < WAIT_FOR_PC; i++)
	{
		if (usb_serial) break;
		delay(1);
	}
	delay(1000);
	digitalWrite(LED_BUILTIN, HIGH);
	usb_serial_println("Goodnight moon.");
}

void loop() {
	process_command();
	if (status & STATUS_START_REQ)
	{
		status &= ~STATUS_START_REQ;
		acquisition();
	}
	delay(1);
}
