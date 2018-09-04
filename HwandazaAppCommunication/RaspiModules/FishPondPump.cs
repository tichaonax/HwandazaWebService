using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using HwandazaAppCommunication.Utils;

namespace HwandazaAppCommunication.RaspiModules
{
    public sealed class FishPondPump : IModule
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
        private const int PondPumpPowerPin = 5;
        private const int PondPumpLedPin = 13;
        private const int PondPumpManualOverideSwitchPin = 6;
        private readonly GpioController _gpio;
        private ThreadPoolTimer _poolTimerModule;
        private ThreadPoolTimer _poolTimerPondLevel;
        private GpioPin _gpioPinFishPondPumpPower;
        private GpioPin _gpioPinFishPondPumpLed;
        private GpioPin _pondPumpManualOverideSwitch;
        private static readonly object SpinLock = new object();

        private bool _isManualOverideSwitch;
        private bool _isRunning;

        private const float FishPondEmpty = 1.6F;
        private const float FishPondFull = 3.0F;

        private GpioPinValue _gpioPinValueFishPondPumpLed = GpioPinValue.Low;

        public FishPondPump(Mcp3008AdcCtrl mcpAdcController)
        {
            _mcpAdcController = mcpAdcController;
            _gpio = GpioController.GetDefault();
            _isRunning = false;
            _isManualOverideSwitch = false;
        }

        public void InitializeGPIO()
        {
            //Indicator LED when pump is running
            _gpioPinFishPondPumpLed = _gpio.OpenPin(PondPumpLedPin);
            _gpioPinFishPondPumpLed.Write(GpioPinValue.Low);
            _gpioPinFishPondPumpLed.SetDriveMode(GpioPinDriveMode.Output);

            //Power pin to turn pump on and off
            _gpioPinFishPondPumpPower = _gpio.OpenPin(PondPumpPowerPin);
            _gpioPinFishPondPumpPower.Write(GpioPinValue.High);
            _gpioPinFishPondPumpPower.SetDriveMode(GpioPinDriveMode.Output);

            _pondPumpManualOverideSwitch = _gpio.OpenPin(PondPumpManualOverideSwitchPin);

            _pondPumpManualOverideSwitch.SetDriveMode(
                _pondPumpManualOverideSwitch.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                    ? GpioPinDriveMode.InputPullUp
                    : GpioPinDriveMode.Input);

            _pondPumpManualOverideSwitch.DebounceTimeout = TimeSpan.FromMilliseconds(Const.FiftyMsDelayMs);
            //Register for the ValueChanged event so our FishPondPump.ButtonPressed 
            // function is called when the button is pressed
            _pondPumpManualOverideSwitch.ValueChanged += ButtonPressed;

            //Timer for how fast the water pump LED blinks
            _poolTimerModule = ThreadPoolTimer.CreatePeriodicTimer(ModuleTimerControl, TimeSpan.FromMilliseconds(Const.QuarterSecondDelayMs));

            //Timer to periodically check the status of the pond level every 1 minute
            _poolTimerPondLevel = ThreadPoolTimer.CreatePeriodicTimer(PondLevelTimerControl, TimeSpan.FromMilliseconds(Const.TenSecondsDelayMs));
            Schedule();
        }

        public void ModuleTimerControl(ThreadPoolTimer timer)
        {
            if (_isRunning)
            {
                _gpioPinValueFishPondPumpLed = (_gpioPinValueFishPondPumpLed == GpioPinValue.High)
                    ? GpioPinValue.Low
                    : GpioPinValue.High;
                _gpioPinFishPondPumpLed.Write(_gpioPinValueFishPondPumpLed);
            }
            else
            {
                //keep making sure the pump is off
                _gpioPinValueFishPondPumpLed = GpioPinValue.Low;
                _gpioPinFishPondPumpLed.Write(GpioPinValue.Low);
                _gpioPinFishPondPumpPower.Write(GpioPinValue.High);
            }
        }

        private void PondLevelTimerControl(ThreadPoolTimer timer)
        {
            if (IsFishPondFull() && !_isManualOverideSwitch)
            {
                Stop();
            }
        }

        public void ButtonPressed(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            //Trun on the fish pond pumb to fill the water. It should run no more than 30 minutes but can stop anytime 
            //the pond is full or when the button is pressed again to stop
            // toggle the state of the LED every time the button is pressed
            switch (e.Edge)
            {
                case GpioPinEdge.FallingEdge:
                   ButtonPressed();
                    break;
            }
        }

        public async void Run()
        {
            //Pump water only the level is good or manual override
            if ((IsFishPondEmpty() && ShouldRunToday()) || _isManualOverideSwitch)
            {
                _gpioPinFishPondPumpPower.Write(GpioPinValue.Low);
               
                lock (SpinLock)
                {
                    _isRunning = true;
                }
                _gpioPinValueFishPondPumpLed = GpioPinValue.High;
                //run pump for30 minutes
                await Task.Delay(Convert.ToInt32(Const.SixtyMinutesDelayMs));
            }
            Stop();
        }

        public void Stop()
        {
            _isManualOverideSwitch = false;
            _gpioPinFishPondPumpPower.Write(GpioPinValue.High);

            lock (SpinLock)
            {
                _isRunning = false;
            }
            _gpioPinValueFishPondPumpLed = GpioPinValue.Low;
            _gpioPinFishPondPumpLed.Write(GpioPinValue.Low);
        }

        public void Schedule()
        {
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 10, 10, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 14, 10, 0), this);
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
                return _mcpAdcController.FishPondPumpAdcFloatValue;
            }
            return (FishPondEmpty - 0.1F);
        }
        
        public bool IsFishPondEmpty()
        {
            return ReadAdcLevel() < FishPondEmpty;
        }

        public bool IsFishPondFull()
        {
            return ReadAdcLevel() > FishPondFull;
        }
    }
}

