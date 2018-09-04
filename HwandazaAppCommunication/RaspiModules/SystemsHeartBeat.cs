using System;
using System.Collections.Generic;
using Windows.Devices.Gpio;
using Windows.System;
using Windows.System.Threading;
using Windows.UI.Xaml;
using HwandazaAppCommunication.Utils;

namespace HwandazaAppCommunication.RaspiModules
{
    public sealed class SystemsHeartBeat: IModule
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

        /*
        System Hearbeat, continues blinking blue light indicating system is running normally.
        Calls the Stop() function in every module to terminate running processes and make sure no peripheral device is left running
        after a system shutdom response.
    */
        private const int SystemsHeartBeatLedPin = 27;//blue led blinks every second to show app in good state
        private const int SystemsHeartBeatManualOverideSwitchPin = 22; //push button used to shut the system down

        private readonly GpioController _gpio;
        private ThreadPoolTimer _poolTimerModule;
        private GpioPin _gpioPinSystemsHeartBeatLed;
        private GpioPin _systemsHeartBeatManualOverideSwitch;

        private int _shutdownCount = 3;
        private ThreadPoolTimer _poolTimerShutdown;
        private bool _isRunning;
        private GpioPinValue _gpioPinValueSystemsHeartBeatLed;
        private readonly IList<IModule> _modules;   //List of modules that need to be gracefully terminated on systemsgutdown

        private static readonly object SpinLock = new object();

        public SystemsHeartBeat(IList<IModule> modules)
        {
            _isRunning = false;
            _gpio = GpioController.GetDefault();
            _modules = modules;
            _gpioPinValueSystemsHeartBeatLed = GpioPinValue.Low;
        }

        public void InitializeGPIO()
        {
            //Indicator LED when system is running
            _gpioPinSystemsHeartBeatLed = _gpio.OpenPin(SystemsHeartBeatLedPin);
            _gpioPinSystemsHeartBeatLed.Write(GpioPinValue.Low);
            _gpioPinSystemsHeartBeatLed.SetDriveMode(GpioPinDriveMode.Output);
            
            //Pin to manually shutdown the raspberry pi system
            _systemsHeartBeatManualOverideSwitch = _gpio.OpenPin(SystemsHeartBeatManualOverideSwitchPin);
            _systemsHeartBeatManualOverideSwitch.SetDriveMode(_systemsHeartBeatManualOverideSwitch.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                ? GpioPinDriveMode.InputPullUp
                : GpioPinDriveMode.Input);

            _systemsHeartBeatManualOverideSwitch.DebounceTimeout = TimeSpan.FromMilliseconds(100);
            //Register for the ValueChanged event so our SystemsHeartBeat.ButtonPressed 
            // function is called when the button is pressed
            _systemsHeartBeatManualOverideSwitch.ValueChanged += ButtonPressed;

            //Timer for how fast the system heart beat LED blinks
            _poolTimerModule = ThreadPoolTimer.CreatePeriodicTimer(ModuleTimerControl, TimeSpan.FromMilliseconds(Const.OneSecondDelayMs));

            //Timer to check when the user clicks the shutdown button 3 times in a row with in 5 seconds
            _poolTimerShutdown = ThreadPoolTimer.CreatePeriodicTimer(CountDownToShutDownTimerControl, TimeSpan.FromMilliseconds(Const.ThreeSecondsDelayMs));

            _isRunning = true;
        }

        public void ModuleTimerControl(ThreadPoolTimer timer)
        {
            if (_isRunning)
            {
                _gpioPinValueSystemsHeartBeatLed = (_gpioPinValueSystemsHeartBeatLed == GpioPinValue.High)
                    ? GpioPinValue.Low
                    : GpioPinValue.High;
                _gpioPinSystemsHeartBeatLed.Write(_gpioPinValueSystemsHeartBeatLed);
            }
            else
            {
                _gpioPinValueSystemsHeartBeatLed = GpioPinValue.Low;
                _gpioPinSystemsHeartBeatLed.Write(GpioPinValue.Low);
            }
        }

        public void ButtonPressed(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            ButtonPressed();
        }

        public void Run()
        {
        }

        public void Stop()
        {
            foreach (var module in _modules)
            {
                module.Stop();
            }

            //you do not want to disable the systen heartbeat LED
            //_isRunning = false;
            //_gpioPinValueSystemsHeartBeatLed = GpioPinValue.Low;
            //_gpioPinSystemsHeartBeatLed.Write(GpioPinValue.Low);
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
        }

        public void ButtonPressed()
        {
            CountDownToShutDown();
            //turn off everything and then shutdown the raspberry pi OS
            if (_shutdownCount > 0) return;
            Stop();
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(5.0));
            //Application.Current.Exit();
            //Windows.ApplicationModel.Core.CoreApplication.Exit();
        }

        private void CountDownToShutDown()
        {
            lock (SpinLock)
            {
                _shutdownCount = _shutdownCount - 1;
            }
        }

        public void CountDownToShutDownTimerControl(ThreadPoolTimer timer)
        {
            lock (SpinLock)
            {
                _shutdownCount = 3;
            }
        }


        public ModuleStatus ModuleStatus()
        {
            return new ModuleStatus();
        }

        public float ReadAdcLevel()
        {
            return 0.0f;
        }
    }
}
