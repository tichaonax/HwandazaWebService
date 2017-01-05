using System;
using System.Collections.Generic;
using Windows.Devices.Gpio;
using Windows.System;
using Windows.System.Threading;
using Windows.UI.Xaml;
using HwandazaWebService.Utils;

namespace HwandazaWebService.Modules
{
    internal class SystemsHeartBeat: IModule
    {
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

            _isRunning = false;
            _gpioPinValueSystemsHeartBeatLed = GpioPinValue.Low;
            _gpioPinSystemsHeartBeatLed.Write(GpioPinValue.Low);
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
            //ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(5.0));
            Application.Current.Exit();
           // Windows.ApplicationModel.Core.CoreApplication.Exit();
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
                _shutdownCount = 5;
            }
        }


        public Status ModuleStatus()
        {
            return new Status();
        }

        public float ReadAdcLevel()
        {
            return 0.0f;
        }
    }
}
