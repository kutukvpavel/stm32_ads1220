
//////////////////////////////////////////////////////////////////////////////////////////
//
//    Arduino library for the ADS1220 24-bit ADC breakout board
//
//    Author: Ashwin Whitchurch
//    Copyright (c) 2018 ProtoCentral
//
//		MODIFIED: Kutukov Pavel, 2020
//
//    Based on original code from Texas Instruments
//
//    This software is licensed under the MIT License(http://opensource.org/licenses/MIT).
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
//   NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//   WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//   For information on how to use, visit https://github.com/Protocentral/Protocentral_ADS1220/
//
/////////////////////////////////////////////////////////////////////////////////////////

#pragma once

#include <Arduino.h>
#include <SPI.h>

//ADS1220 SPI commands
#define SPI_MASTER_DUMMY    0xFF
#define RESET               0x06   //Send the RESET command (06h) to make sure the ADS1220 is properly reset after power-up
#define START               0x08    //Send the START/SYNC command (08h) to start converting in continuous conversion mode
#define WREG  0x40
#define RREG  0x20

//Config registers
#define CONFIG_REG0_ADDRESS 0x00
#define CONFIG_REG1_ADDRESS 0x01
#define CONFIG_REG2_ADDRESS 0x02
#define CONFIG_REG3_ADDRESS 0x03

#define REG_CONFIG1_DR_MASK       0xE0
#define REG_CONFIG0_PGA_GAIN_MASK 0x0E
#define REG_CONFIG0_MUX_MASK      0xF0

#define DR_20SPS    0x00
#define DR_45SPS    0x20
#define DR_90SPS    0x40
#define DR_175SPS   0x60
#define DR_330SPS   0x80
#define DR_600SPS   0xA0
#define DR_1000SPS  0xC0

#define PGA_GAIN_1   0x00
#define PGA_GAIN_2   0x02
#define PGA_GAIN_4   0x04
#define PGA_GAIN_8   0x06
#define PGA_GAIN_16  0x08
#define PGA_GAIN_32  0x0A
#define PGA_GAIN_64  0x0C
#define PGA_GAIN_128 0x0E

#define MUX_AIN0_AIN1   0x00
#define MUX_AIN0_AIN2   0x10
#define MUX_AIN0_AIN3   0x20
#define MUX_AIN1_AIN2   0x30
#define MUX_AIN1_AIN3   0x40
#define MUX_AIN2_AIN3   0x50
#define MUX_AIN1_AIN0   0x60
#define MUX_AIN3_AIN2   0x70
#define MUX_AIN0_AVSS   0x80
#define MUX_AIN1_AVSS   0x90
#define MUX_AIN2_AVSS   0xA0
#define MUX_AIN3_AVSS   0xB0

#define MUX_SE_CH0      0x80
#define MUX_SE_CH1      0x90
#define MUX_SE_CH2      0xA0
#define MUX_SE_CH3      0xB0

#ifndef _BV
#define _BV(bit) (1<<(bit))
#endif

//Added:
#define ADS1220_NO_DATA static_cast<int32_t>(0xFFFFFFFF) //Since conversion result is 24-bit wide, this value can never be encountered during normal operation.
#define ADS1220_MODE_CONTINUOUS 0
#define ADS1220_MODE_SINGLE_SHOT 1


class ADS1220 //Do not use names that are too long to type.
{
private:
	  uint8_t m_config_reg0;
	  uint8_t m_config_reg1;
	  uint8_t m_config_reg2;
	  uint8_t m_config_reg3;

	  uint8_t Config_Reg0;
	  uint8_t Config_Reg1;
	  uint8_t Config_Reg2;
	  uint8_t Config_Reg3;

	  uint8_t m_drdy_pin=6;
	  uint8_t m_cs_pin=7;
  public: //Use single case convention throughout the entire class (snake case)

	  ADS1220();
	  void begin(uint8_t cs_pin, uint8_t drdy_pin);
	  void reset(void);

	  //SPI RAW
	  void SPI_Command(unsigned char data_in);
	  void write_register(uint8_t address, uint8_t value);
	  uint8_t read_register(uint8_t address);

	  //Conversion API
	  void start_conversion(void);
	  int32_t read_result();
	  int32_t read_result_blocking(); //Actually wait for result
	  int32_t single_shot_blocking(); //Don't see any need to implement a merged single_shot_single_ended version
	  bool is_ready();

	  //Configuration API
	  uint8_t * get_config_reg(void);
	  void PGA_OFF(void);
	  void PGA_ON(void);
	  void set_mode(uint8_t mode);
	  void set_data_rate(int datarate);
	  void set_pga_gain(int pgagain);
	  void select_mux_channels(int channels_conf);
};
