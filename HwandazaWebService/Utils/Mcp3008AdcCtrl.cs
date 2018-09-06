using System;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.System.Threading;

namespace HwandazaWebService.Utils
{
    public sealed class Mcp3008AdcCtrl
    {
        /*
        Initializes the MCP3008 ADC controller.
        This contains the routines to detect the levels of the water tank, fish pond and the lawn moisture content
        The ADC TransferFullDuplex method has a lock so that only one module can access it at any one time
            */

        // ADC chip operation constants
        private const byte Mcp3008SingleEnded = 0x08;
        private const byte Mcp3008Differential = 0x00;

        // These are used when we calculate the voltage from the ADC units
        private readonly float _referenceVoltage;
        private const uint Min = 0;
        private const uint Max = 1023;

        private const string SpiControllerName = "SPI0"; /* Friendly name for Raspberry Pi 2 SPI controller          */
        private const Int32 SpiChipSelectLine = 0; /* Line 0 maps to physical pin number 24 on the Rpi2        */
        private SpiDevice _spiDevice;
        private bool _isSpiInitialized;
        private float _referenceVolgate;
        private static readonly object SpinLock = new object();

        public float MainWaterPumpAdcFloatValue => AdcToVoltage(MainWaterPumpAdcIntValue);
        public float LawnIrrigatorAdcFloatValue => AdcToVoltage(LawnIrrigatorAdcIntValue);
        public float FishPondPumpAdcFloatValue => AdcToVoltage(FishPondPumpAdcIntValue);

        public int MainWaterPumpAdcIntValue { get; private set; }
        public int LawnIrrigatorAdcIntValue { get; private set; }
        public int FishPondPumpAdcIntValue { get; private set; }

        private const byte MainWAterPumpAdcChannel = 0;
        private const byte FishPondPumpAdcChannel = 1;
        private const byte LawnIrrigatorAdcChannel = 2;

        private ThreadPoolTimer _poolTimerAdcController;

        public Mcp3008AdcCtrl(float referenceVolgate)
        {
            // Store the reference voltage value for later use in the voltage calculation.
            _referenceVoltage = referenceVolgate;
            _isSpiInitialized = false;
        }

        public bool IsSpiInitialized()
        {
            return _isSpiInitialized;
        }

        public async void InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SpiChipSelectLine)
                               {
                                   ClockFrequency = 3600000, // 0.5MHz clock rate
                                   Mode = SpiMode.Mode0 // The ADC expects idle-low clock polarity so we use Mode0
                               };

                var spiAqs = SpiDevice.GetDeviceSelector(SpiControllerName);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                _spiDevice = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);
                _isSpiInitialized = true;

                MainWaterPumpAdcIntValue = ReadAdc(MainWAterPumpAdcChannel);
                FishPondPumpAdcIntValue = ReadAdc(FishPondPumpAdcChannel);
                LawnIrrigatorAdcIntValue = ReadAdc(LawnIrrigatorAdcChannel);

                //Timer to periodically read the ADC values every 500 Ms
                _poolTimerAdcController = ThreadPoolTimer.CreatePeriodicTimer(Adc3008TimerAdcControl,
                    TimeSpan.FromMilliseconds(Const.HalfSecondDelayMs));
            }

            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        private void Adc3008TimerAdcControl(ThreadPoolTimer timer)
        {
            //We let the controller reads sequentically so we avoid having the need to use locks if
            //calls to read the ADC are coming from multiple objects
            // Read from the ADC chip the current values of the water level and moisture content.
            MainWaterPumpAdcIntValue = ReadAdc(MainWAterPumpAdcChannel);
          //  Debug.WriteLine(MainWaterPumpAdcIntValue);
           // Debug.WriteLine("Main Pump Voltage=" + MainWaterPumpAdcFloatValue);
            
            FishPondPumpAdcIntValue = ReadAdc(FishPondPumpAdcChannel);
          //  Debug.WriteLine(FishPondPumpAdcIntValue);
           // Debug.WriteLine("Fish Pond Voltage=" + FishPondPumpAdcFloatValue);

            LawnIrrigatorAdcIntValue = ReadAdc(LawnIrrigatorAdcChannel);
          //  Debug.WriteLine(LawnIrrigatorAdcIntValue);
           // Debug.WriteLine("Lawn Irrigator Voltage=" + LawnIrrigatorAdcFloatValue);
        }


        /// <summary>
        ///  Returns the ADC value (uint) as a float voltage based on the configured reference voltage
        /// </summary>
        /// <param name="adc"> the ADC value to convert</param>
        /// <returns>The computed voltage based on the reference voltage</returns>
        private float AdcToVoltage(int adc)
        {
            return (float) adc*_referenceVoltage/(float) Max;
        }

        /// <summary> 
        /// This method does the actual work of communicating over the SPI bus with the chip.
        /// To line everything up for ease of reading back (on byte boundary) we 
        /// will pad the command start bit with 7 leading "0" bits
        ///
        /// Write 0000 000S GDDD xxxx xxxx xxxx
        /// Read  ???? ???? ???? ?N98 7654 3210
        /// S = start bit
        /// G = Single / Differential
        /// D = Chanel data 
        /// ? = undefined, ignore
        /// N = 0 "Null bit"
        /// 9-0 = 10 data bits
        /// </summary>
        private int ReadAdc(byte whichChannel)
        {
            byte command = whichChannel;
            command |= Mcp3008SingleEnded;
            command <<= 4;

            byte[] commandBuf = new byte[] {0x01, command, 0x00};

            byte[] readBuf = new byte[] {0x00, 0x00, 0x00};

            lock (SpinLock)
            {
                _spiDevice.TransferFullDuplex(commandBuf, readBuf);
            }

            int sample = readBuf[2] + ((readBuf[1] & 0x03) << 8);
            int s2 = sample & 0x3FF;
            //Debug.Assert(sample == s2);

            return sample;
        }
    }
}
