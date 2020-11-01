//////////////////////////////////////////////////////////////////////////////////////////
//
//    Arduino library for the ADS1220 24-bit ADC breakout board
//
//    Author: Ashwin Whitchurch
//	
//		MODIFIED: Kutukov Pavel, 2020
//
//    Copyright (c) 2018 ProtoCentral
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

#include "Protocentral_ADS1220.h"

//#define BOARD_SENSYTHING ST_1_3

ADS1220::ADS1220() 								// Constructors
{

}

void ADS1220::write_register(uint8_t address, uint8_t value)
{
	digitalWrite(m_cs_pin,LOW);
	delay(5);
	SPI.transfer(WREG|(address<<2));
	SPI.transfer(value);
	delay(5);
	digitalWrite(m_cs_pin,HIGH);
}

uint8_t ADS1220::read_register(uint8_t address)
{
	uint8_t data;

	digitalWrite(m_cs_pin,LOW);
	delay(5);
	SPI.transfer(RREG|(address<<2));
	data = SPI.transfer(SPI_MASTER_DUMMY);
	delay(5);
	digitalWrite(m_cs_pin,HIGH);

	return data;
}

void ADS1220::begin(uint8_t cs_pin, uint8_t drdy_pin)
{
	m_drdy_pin=drdy_pin;
	m_cs_pin=cs_pin;

	pinMode(m_cs_pin, OUTPUT);
	pinMode(m_drdy_pin, INPUT);

#if defined(BOARD_SENSYTHING)
	SPI.begin(18, 35, 23, 19);
#else
	SPI.begin();
#endif
	SPI.setBitOrder(MSBFIRST);
	SPI.setDataMode(SPI_MODE1);

	delay(100);
	reset();
	delay(100);

	digitalWrite(m_cs_pin,LOW);

	m_config_reg0 = 0x00;   //Default settings: AINP=AIN0, AINN=AIN1, Gain 1, PGA enabled
	m_config_reg1 = 0x04;   //Default settings: DR=20 SPS, Mode=Normal, Conv mode=continuous, Temp Sensor disabled, Current Source off
	m_config_reg2 = 0x10;   //Default settings: Vref internal, 50/60Hz rejection, power open, IDAC off
	m_config_reg3 = 0x00;   //Default settings: IDAC1 disabled, IDAC2 disabled, DRDY pin only

	write_register( CONFIG_REG0_ADDRESS , m_config_reg0);
	write_register( CONFIG_REG1_ADDRESS , m_config_reg1);
	write_register( CONFIG_REG2_ADDRESS , m_config_reg2);
	write_register( CONFIG_REG3_ADDRESS , m_config_reg3);

	delay(100);

	Config_Reg0 = read_register(CONFIG_REG0_ADDRESS);
	Config_Reg1 = read_register(CONFIG_REG1_ADDRESS);
	Config_Reg2 = read_register(CONFIG_REG2_ADDRESS);
	Config_Reg3 = read_register(CONFIG_REG3_ADDRESS);

	Serial.println("Config_Reg : ");
	Serial.println(Config_Reg0,HEX);
	Serial.println(Config_Reg1,HEX);
	Serial.println(Config_Reg2,HEX);
	Serial.println(Config_Reg3,HEX);
	Serial.println(" ");

	digitalWrite(m_cs_pin,HIGH);

	delay(100);

	//Start_Conv();
	delay(100);
}

void ADS1220::SPI_Command(unsigned char data_in)
{
	digitalWrite(m_cs_pin, LOW);
	delay(2);
	digitalWrite(m_cs_pin, HIGH);
	delay(2);
	digitalWrite(m_cs_pin, LOW);
	delay(2);
	SPI.transfer(data_in);
	delay(2);
	digitalWrite(m_cs_pin, HIGH);
}

void ADS1220::reset() //Don't repeat the class name
{
	SPI_Command(RESET);
}

void ADS1220::start_conversion()
{
	SPI_Command(START);
}

void ADS1220::PGA_ON(void)
{
	m_config_reg0 &= ~_BV(0);
	write_register(CONFIG_REG0_ADDRESS,m_config_reg0);
}

void ADS1220::PGA_OFF(void)
{
	m_config_reg0 |= _BV(0);
	write_register(CONFIG_REG0_ADDRESS,m_config_reg0);
}

void ADS1220::set_data_rate(int datarate)
{
	m_config_reg1 &= ~REG_CONFIG1_DR_MASK;
	m_config_reg1 |= datarate;
	write_register(CONFIG_REG1_ADDRESS,m_config_reg1);
}

void ADS1220::select_mux_channels(int channels_conf)
{
	m_config_reg0 &= ~REG_CONFIG0_MUX_MASK;
	m_config_reg0 |= channels_conf;
	write_register(CONFIG_REG0_ADDRESS,m_config_reg0);
}

void ADS1220::set_pga_gain(int pgagain)
{
	m_config_reg0 &= ~REG_CONFIG0_PGA_GAIN_MASK;
	m_config_reg0 |= pgagain ;
	write_register(CONFIG_REG0_ADDRESS,m_config_reg0);
}

uint8_t * ADS1220::get_config_reg()
{
	static uint8_t config_Buff[4];

	m_config_reg0 = read_register(CONFIG_REG0_ADDRESS);
	m_config_reg1 = read_register(CONFIG_REG1_ADDRESS);
	m_config_reg2 = read_register(CONFIG_REG2_ADDRESS);
	m_config_reg3 = read_register(CONFIG_REG3_ADDRESS);

	config_Buff[0] = m_config_reg0 ;
	config_Buff[1] = m_config_reg1 ;
	config_Buff[2] = m_config_reg2 ;
	config_Buff[3] = m_config_reg3 ;

	return config_Buff;
}

int32_t ADS1220::read_result()
{
	byte SPI_Buff[3]; //Why was it static?
	//If your care about speed so much, that stack usage becomes a consideration, you probably should not use arduino stuff altogether.
	//And I can't think of another reason, since the contents of the buffer are not required to be retained between calls.

	int32_t result = ADS1220_NO_DATA; // MOD: use a single return variable, return NO_DATA pattern in case the device is not ready

	if (is_ready())             //If the device is ready, then read corresponding registers
	{
		digitalWrite(m_cs_pin,LOW);                         //Select chip (/CS)
		delayMicroseconds(100);
		for (int i = 0; i < 3; i++)
		{
		  SPI_Buff[i] = SPI.transfer(SPI_MASTER_DUMMY); //Receive 3 bytes (24 bits)
		}
		delayMicroseconds(100);
		digitalWrite(m_cs_pin,HIGH);                  //De-select chip (/CS)

		//Convert received buffer to a 32-bit integer, the representation seems to be big-endian

		/*result = SPI_Buff[0];
		result = (result << 8) | SPI_Buff[1];
		result = (result << 8) | SPI_Buff[2];                                 // Converting 3 bytes to a 24 bit int
		result <<= 8;
		result >>= 8;                      // Converting 24 bit two's complement to 32 bit two's complement
		*/
		
		result = static_cast<int32_t>(SPI_Buff[0]) << 24; //This approach might save some shifts for 8-bit AVR, have to look at the LSS file
		result |= static_cast<int32_t>(SPI_Buff[1]) << 16;
		result |= static_cast<int16_t>(SPI_Buff[2]) << 8; //bitwise OR and left shift don't care about signedness, so might as well cast to a smaller type, the compiler will sort it out on 32-bit platforms
		result >>= 8; //Here the difference between left and right arithmetic shifts is exploited (>> doesn't shift the sign bit, but << is equal to logical shift)
	}
	return result;
}

int32_t ADS1220::read_result_blocking()
{
	//Do not copy-paste! Nest!
	int32_t result;
	while ((result = read_result(), result) == ADS1220_NO_DATA);
	return result;
}

int32_t ADS1220::single_shot_blocking()
{
	start_conversion();
	return read_result_blocking();
}

bool ADS1220::is_ready()
{
	return digitalRead(m_drdy_pin) == LOW;
}

void ADS1220::set_mode(uint8_t mode)
{
	if (mode == ADS1220_MODE_CONTINUOUS)
	{
		m_config_reg1 |= _BV(2);
		write_register(CONFIG_REG1_ADDRESS, m_config_reg1);
	}
	else if (mode == ADS1220_MODE_SINGLE_SHOT)
	{
		m_config_reg1 &= ~_BV(2);
		write_register(CONFIG_REG1_ADDRESS, m_config_reg1);
	}
}
