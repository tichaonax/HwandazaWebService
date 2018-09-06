using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using HwandazaWebService.Utils;

namespace HwandazaWebService.RaspiModules
{
    /*
    WaterPumpManualOverideSwitchPin is used to manually turn the pump on/off but still runs for 180 seconds
    WaterPumpPowerPin = 21;
    WaterPumpLedPin = 18;
    WaterPumpManualOverideSwitchPin = 23;
    */
    public sealed class MainWaterPump : IModule
    {
        static class Const
        {
            public const int SixtyMinutesDelayMs = 3600000;
            public const int ThirtyMinutesDelayMs = 1800000;
            public const int TwentyMinutesDelayMs = 1200000;
            public const int FifteenMinutesDelayMs = 900000;
            public const int TenMinutesDelayMs = 600000;
            public const int FiveMinutesDelayMs = 300000;
            public const int FourMinutes = 240000;
            public const int ThreeMinutes = 180000;
            public const int TwoMinutes = 120000;
            public const int SeventySecondsDelayMs = 70000;
            public const int OneMinuteDelayMs = 60000;
            public const int TenSecondsDelayMs = 10000;
            public const int FiveSecondsDelayMs = 5000;
            public const int ThreeSecondsDelayMs = 3000;
            public const int OneSecondDelayMs = 1000;
            public const int HalfSecondDelayMs = 500;
            public const int QuarterSecondDelayMs = 250;
            public const int FiftyMsDelayMs = 50;
            public const string Running = "Running";
            public const string Stopped = "Stopped";

            public const string MainWaterPump = "mainwaterpump";
            public const string FishPondPump = "fishpondpump";
            public const string RandomLights = "randomlights";
            public const string LawnIrrigator = "lawnirrigator";
            public const string Operations = "operations";

            public const string CommandOn = "ON";
            public const string CommandOff = "OFF";
            public const string CommandOperations = "OPERATIONS";
            public const string CommandStatus = "STATUS";
        }

        private readonly Mcp3008AdcCtrl _mcpAdcController;
        private const int WaterPumpPowerPin = 21;
        private const int WaterPumpLedPin = 18;
        private const int WaterPumpManualOverideSwitchPin = 23;
        private readonly GpioController _gpio;
        private ThreadPoolTimer _poolTimerModule;
        private ThreadPoolTimer _poolTimerTankLevel;
        private GpioPin _gpioPinWaterPumpPower;
        private GpioPin _gpioPinWaterPumpLed;
        private GpioPin _waterPumpManualOverideSwitch;

        private bool _isManualOverideSwitch;
        private bool _isRunning;

        private GpioPinValue _gpioPinValueWaterPumpLed = GpioPinValue.High;

        private const float WaterPumpEmpty = 2.6F;
        private const float WaterPumpFull = 3.0F;
        private static readonly object SpinLock = new object();

        public MainWaterPump(Mcp3008AdcCtrl mcpAdcController)
        {
            _mcpAdcController = mcpAdcController;
            _gpio = GpioController.GetDefault();
            _isRunning = false;
            _isManualOverideSwitch = false;
        }

        public void InitializeGPIO()
        {
            //Indicator LED when pump is running
            _gpioPinWaterPumpLed = _gpio.OpenPin(WaterPumpLedPin);
            _gpioPinWaterPumpLed.Write(GpioPinValue.Low);
            _gpioPinWaterPumpLed.SetDriveMode(GpioPinDriveMode.Output);

            //Power pin to turn pump on and off
            _gpioPinWaterPumpPower = _gpio.OpenPin(WaterPumpPowerPin);
            _gpioPinWaterPumpPower.Write(GpioPinValue.High);
            _gpioPinWaterPumpPower.SetDriveMode(GpioPinDriveMode.Output);

            //Pin to manually turn pump on and off
            _waterPumpManualOverideSwitch = _gpio.OpenPin(WaterPumpManualOverideSwitchPin);
            _waterPumpManualOverideSwitch.SetDriveMode(
                _waterPumpManualOverideSwitch.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                    ? GpioPinDriveMode.InputPullUp
                    : GpioPinDriveMode.Input);

            _waterPumpManualOverideSwitch.DebounceTimeout = TimeSpan.FromMilliseconds(Const.FiftyMsDelayMs);
            //Register for the ValueChanged event so our MainWaterPump.ButtonPressed 
            // function is called when the button is pressed
            _waterPumpManualOverideSwitch.ValueChanged += ButtonPressed;

            //Timer for how fast the water pump LED blinks
            _poolTimerModule = ThreadPoolTimer.CreatePeriodicTimer(ModuleTimerControl, TimeSpan.FromMilliseconds(Const.HalfSecondDelayMs));

            //Timer to periodically check the status of the water tank level every one minute
            _poolTimerTankLevel = ThreadPoolTimer.CreatePeriodicTimer(TankLevelTimerControl, TimeSpan.FromMilliseconds(Const.TenSecondsDelayMs));
            Schedule();
        }

        public void ModuleTimerControl(ThreadPoolTimer timer)
        {
            //Togle the led as long as the pump is running
            if (_isRunning)
            {
                _gpioPinValueWaterPumpLed = (_gpioPinValueWaterPumpLed == GpioPinValue.High)
                    ? GpioPinValue.Low
                    : GpioPinValue.High;
                _gpioPinWaterPumpLed.Write(_gpioPinValueWaterPumpLed);
            }
            else
            {
                //keep making sure the pump is off
                _gpioPinValueWaterPumpLed = GpioPinValue.Low;
                _gpioPinWaterPumpLed.Write(GpioPinValue.Low);
                _gpioPinWaterPumpPower.Write(GpioPinValue.High);
            }
        }

        private void TankLevelTimerControl(ThreadPoolTimer timer)
        {
            //If the tank is full stop the pump unless override
            if ((IsWaterTankFull() && !_isManualOverideSwitch))
            {
                Stop();
            }
        }

        public void ButtonPressed(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            //Toggle tha status of the pump
            switch (e.Edge)
            {
                case GpioPinEdge.FallingEdge:
                    ButtonPressed();
                    break;
            }
        }

        public async void Run()
        {
            //pump water only if the tank is empty or _isManualOverideSwitch
            if ((IsWaterTankEmpty() && ShouldRunToday()) || _isManualOverideSwitch)
            {
                _gpioPinWaterPumpPower.Write(GpioPinValue.Low);
            
                lock (SpinLock)
                {
                    _isRunning = true;
                }
                _gpioPinValueWaterPumpLed = GpioPinValue.High;
                
                //run pump for 240s and then stop
                await Task.Delay(Convert.ToInt32(Const.FourMinutes));
            }
            Stop();
        }

        public void Stop()
        {
            _isManualOverideSwitch = false;
            _gpioPinWaterPumpPower.Write(GpioPinValue.High);
        
            lock (SpinLock)
            {
                _isRunning = false;
            }
            _gpioPinValueWaterPumpLed = GpioPinValue.Low;
            _gpioPinWaterPumpLed.Write(GpioPinValue.Low);
        }

        private void Schedule()
        {
            //Only schedule water pump 24 hours
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 15, 0), this);
            //scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 5, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 5, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 5, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 5, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 6, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 6, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 6, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 6, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 7, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 7, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 7, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 7, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 8, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 8, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 8, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 8, 45, 0), this);

            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 9, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 9, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 9, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 9, 45, 0), this);

            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 10, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 10, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 10, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 10, 45, 0), this);

            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 11, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 11, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 11, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 11, 45, 0), this);

            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 12, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 12, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 12, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 12, 45, 0), this);

            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 13, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 13, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 13, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 13, 45, 0), this);

            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 14, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 14, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 14, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 14, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 15, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 15, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 15, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 15, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 16, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 16, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 16, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 16, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 17, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 17, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 17, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 17, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 18, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 18, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 18, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 18, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 19, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 19, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 19, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 19, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 20, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 20, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 20, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 20, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 45, 0), this);

            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 15, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 45, 0), this);
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public bool ShouldRunToday()
        {
            return true;
        }

        public IModule Module()
        {
            return this;
        }

        public ModuleStatus ModuleStatus()
        {
            return new ModuleStatus()
                   {
                       AdcVoltage = ReadAdcLevel(),
                       IsRunning = _isRunning,
                       StatusText = _isRunning ? Const.Running : Const.Stopped
                   };
        }

        public float ReadAdcLevel()
        {
            if (_mcpAdcController.IsSpiInitialized())
            {
                return _mcpAdcController.MainWaterPumpAdcFloatValue;
            }
            return (WaterPumpEmpty - 0.1F);
        }

        public void ManualOverideSwitch()
        {
            _isManualOverideSwitch = true;
        }

        public void ButtonPressed()
        {
            if (!_isRunning)
            {
                _isManualOverideSwitch = true;
                Run();
            }
            else
            {
                Stop();
            }
        }

        public bool IsWaterTankEmpty()
        {
            return ReadAdcLevel() < WaterPumpEmpty;
        }

        public bool IsWaterTankFull()
        {
            return ReadAdcLevel() > WaterPumpFull;
        }
    }
}
