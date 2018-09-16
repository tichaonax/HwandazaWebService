using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using HwandazaWebService.Utils;

namespace HwandazaWebService.RaspiModules
{
    public sealed class LawnIrrigator : IModule
    {
        private readonly Mcp3008AdcCtrl _mcpAdcController;
        private const int LawnIrrigatorPowerPin = 24;
        private const int LawnIrrigatorLedPin = 4;
        private const int LawnIrrigatorManualOverideSwitchPin = 17;
        private readonly GpioController _gpio;
        private ThreadPoolTimer _poolTimerModule;
        private ThreadPoolTimer _poolTimerLawnMoisture;
        private GpioPin _gpioPinLawnIrrigatorPower;
        private GpioPin _gpioPinLawnIrrigatorLed;
        private GpioPin _lawnIrrigatorManualOverideSwitch;
        private bool _isManualOverideSwitch;
        private static readonly object SpinLock = new object();

        private bool _isRunning;
        private DateTime _lastUpdate ;
        private GpioPinValue _gpioPinValueLawnIrrigatorLed = GpioPinValue.Low;

        private const float LawnMoistureLow = 3.0F;
        private const float LawnMoistureHigh = 4.0F;

        public LawnIrrigator(Mcp3008AdcCtrl mcpAdcController)
        {
            _mcpAdcController = mcpAdcController;
            _gpio = GpioController.GetDefault();
            _isRunning = false;
            _isManualOverideSwitch = false;
            _lastUpdate = DateTime.Now;
        }
        
        public void ButtonPressed(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            //Trun on the lawn irrigator. It should run no more than 30 minutes but can stop anytime 
            //the pond is full or when the button is pressed again to stop
            // toggle the state of the LED every time the button is pressed
            switch (e.Edge)
            {
                case GpioPinEdge.FallingEdge:
                    ButtonPressed();
                    break;
            }
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
        public async void Run()
        {
            //water lawn only the mosture is low and not over the weekend unless manual override
            if ((IsLawnMoistureLow() && ShouldRunToday()) || _isManualOverideSwitch)
            {
                _gpioPinLawnIrrigatorPower.Write(GpioPinValue.Low);
              
                lock (SpinLock)
                {
                    _isRunning = true;
                    _lastUpdate = DateTime.Now;
                }
                _gpioPinValueLawnIrrigatorLed = GpioPinValue.High;
                //run pump for30 minutes
                await Task.Delay(Convert.ToInt32(Const.ThirtyMinutesDelayMs));
            }
            Stop();
        }

        public void Stop()
        {
            _isManualOverideSwitch = false;
            _gpioPinLawnIrrigatorPower.Write(GpioPinValue.High);

            lock (SpinLock)
            {
                _isRunning = false;
                _lastUpdate = DateTime.Now;
            }
            _gpioPinValueLawnIrrigatorLed = GpioPinValue.Low;
            _gpioPinLawnIrrigatorLed.Write(GpioPinValue.Low);
        }

        public void Schedule()
        {
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 7, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 17, 0, 0), this);
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public bool ShouldRunToday()
        {
            //no watering over the weekend
            var today = DateTime.Today.DayOfWeek;
            return !((today == DayOfWeek.Sunday) || (today == DayOfWeek.Saturday));
        }

        public IModule Module()
        {
            return this;
        }

        public void ManualOverideSwitch()
        {
            _isManualOverideSwitch = true;
        }

        public ModuleStatus ModuleStatus()
        {
            return new ModuleStatus()
            {
                AdcVoltage = ReadAdcLevel(),
                IsRunning = _isRunning,
                StatusText = _isRunning ? Const.Running : Const.Stopped,
                LastUpdate = _lastUpdate,
        };
        }

        public void InitializeGPIO()
        {
            //Indicator LED when pump is running
            _gpioPinLawnIrrigatorLed = _gpio.OpenPin(LawnIrrigatorLedPin);
            _gpioPinLawnIrrigatorLed.Write(GpioPinValue.Low);
            _gpioPinLawnIrrigatorLed.SetDriveMode(GpioPinDriveMode.Output);

            //Power pin to turn pump on and off
            _gpioPinLawnIrrigatorPower = _gpio.OpenPin(LawnIrrigatorPowerPin);
            _gpioPinLawnIrrigatorPower.Write(GpioPinValue.High);
            _gpioPinLawnIrrigatorPower.SetDriveMode(GpioPinDriveMode.Output);

            _lawnIrrigatorManualOverideSwitch = _gpio.OpenPin(LawnIrrigatorManualOverideSwitchPin);

            _lawnIrrigatorManualOverideSwitch.SetDriveMode(
                _lawnIrrigatorManualOverideSwitch.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                    ? GpioPinDriveMode.InputPullUp
                    : GpioPinDriveMode.Input);

            _lawnIrrigatorManualOverideSwitch.DebounceTimeout = TimeSpan.FromMilliseconds(Const.FiftyMsDelayMs);
            //Register for the ValueChanged event so our LawnIrrigator.ButtonPressed 
            // function is called when the button is pressed
            _lawnIrrigatorManualOverideSwitch.ValueChanged += ButtonPressed;

            //Timer for how fast the water pump LED blinks
            _poolTimerModule = ThreadPoolTimer.CreatePeriodicTimer(ModuleTimerControl, TimeSpan.FromMilliseconds(Const.HalfSecondDelayMs));

            //Timer to periodically check the status of the moisture level every 10 minutes
            _poolTimerLawnMoisture = ThreadPoolTimer.CreatePeriodicTimer(LawnMoistureLevelTimerControl, TimeSpan.FromMilliseconds(Const.TenMinutesDelayMs));
            Schedule();
        }

        public void ModuleTimerControl(ThreadPoolTimer timer)
        {
            if (_isRunning)
            {
                _gpioPinValueLawnIrrigatorLed = (_gpioPinValueLawnIrrigatorLed == GpioPinValue.High)
                    ? GpioPinValue.Low
                    : GpioPinValue.High;
                _gpioPinLawnIrrigatorLed.Write(_gpioPinValueLawnIrrigatorLed);
            }
            else
            {
                //keep making sure the pump is off
                _gpioPinValueLawnIrrigatorLed = GpioPinValue.Low;
                _gpioPinLawnIrrigatorLed.Write(GpioPinValue.Low);
                _gpioPinLawnIrrigatorPower.Write(GpioPinValue.High);
            }
        }

        private void LawnMoistureLevelTimerControl(ThreadPoolTimer timer)
        {
            if (IsLawnMoistureHigh() && !_isManualOverideSwitch)
            {
                Stop();
            }
        }

        public float ReadAdcLevel()
        {
            if (_mcpAdcController.IsSpiInitialized())
            {
                return _mcpAdcController.LawnIrrigatorAdcFloatValue;
            }
            return (LawnMoistureLow - 0.1F);
        }

        public bool IsLawnMoistureLow()
        {
            return ReadAdcLevel() < LawnMoistureLow;
        }

        public bool IsLawnMoistureHigh()
        {
            return ReadAdcLevel() > LawnMoistureHigh;
        }
    }
}
