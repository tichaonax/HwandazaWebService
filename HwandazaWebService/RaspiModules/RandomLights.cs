using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using HwandazaWebService.Utils;

namespace HwandazaWebService.RaspiModules
{
    public sealed class RandomLights : IModule
    {
        private const int RandomLightsM1PowerPin = 25; //turn lights inside the house
        private const int RandomLightsM2PowerPin = 12; //turn lights on outside the house
        private const int RandomLightsL3PowerPin = 16; //turn on lights in the main garage
        private const int RandomLightsL4PowerPin = 20; //turn on light in the pump house
        private const int RandomLightsL5PowerPin = 3; //turn on street lights
        private const int RandomLightsL6PowerPin = 2; //turn on street lights
        private const int RandomLightsLedPin = 26; //on when any of the lights are on
        private const int RandomLightsManualOverideSwitchPin = 19;
        private readonly GpioController _gpio;
        private ThreadPoolTimer _randomTimerModule;
        private GpioPin _gpioPinRandomLightsM1Power;
        private GpioPin _gpioPinRandomLightsM2Power;
        private GpioPin _gpioPinRandomLightsL3Power;
        private GpioPin _gpioPinRandomLightsL4Power;
        private GpioPin _gpioPinRandomLightsL5Power;
        private GpioPin _gpioPinRandomLightsL6Power;
        private GpioPin _gpioPinRandomLightsLed;
        private GpioPin _randomLightsManualOverideSwitch;
        private static readonly object SpinLock = new object();

        private bool _isManualOverideSwitch;
        private bool _isRunning;

        private readonly List<GpioPin> _gpioList;
        private readonly List<int> _delayTimeList;
        private GpioPinValue _gpioPinValueRandomLightsLed;

        private readonly LightsStatus _lightsStatus;
        private readonly ModuleStatus _status;

        public RandomLights()
        {

            _lightsStatus = new LightsStatus()
            {
                IsOnL3 = false,
                IsOnL4 = false,
                IsOnL5 = false,
                IsOnL6 = false,
                IsOnM1 = false,
                IsOnM2 = false
            };

            _status = new ModuleStatus()
            {
                AdcVoltage = 0.0f,
                IsRunning = false,
                LightsStatus = _lightsStatus,
                StatusText = Const.Stopped
            };

            _gpioList = new List<GpioPin>();
            _gpio = GpioController.GetDefault();
            _isRunning = false;
            _isManualOverideSwitch = false;
            _delayTimeList = new List<int>()
                             {
                                 Const.TwoMinutes,
                                 Const.HalfMinuteMs,
                                 Const.FourMinutes,
                                 Const.FortySecondsDelayMs,
                                 Const.OneMinuteDelayMs,
                                 Const.ThreeMinutes,
                                 Const.TwentyecondsDelayMs,
                                 Const.FiveMinutesDelayMs,
                                 Const.TenSecondsDelayMs,
                            };
        }

        public void InitializeGPIO()
        {
            //Indicator LED when any light are on
            _gpioPinRandomLightsLed = _gpio.OpenPin(RandomLightsLedPin);
            _gpioPinRandomLightsLed.Write(GpioPinValue.Low);
            _gpioPinRandomLightsLed.SetDriveMode(GpioPinDriveMode.Output);

            //Power pin to turn M1 lights on and off
            _gpioPinRandomLightsM1Power = _gpio.OpenPin(RandomLightsM1PowerPin);
            _gpioPinRandomLightsM1Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsM1Power.SetDriveMode(GpioPinDriveMode.Output);
            _gpioList.Add(_gpioPinRandomLightsM1Power);

            //Power pin to turn M2 lights on and off
            _gpioPinRandomLightsM2Power = _gpio.OpenPin(RandomLightsM2PowerPin);
            _gpioPinRandomLightsM2Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsM2Power.SetDriveMode(GpioPinDriveMode.Output);
            _gpioList.Add(_gpioPinRandomLightsM2Power);

            //Power pin to turn L3 lights on and off
            _gpioPinRandomLightsL3Power = _gpio.OpenPin(RandomLightsL3PowerPin);
            _gpioPinRandomLightsL3Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL3Power.SetDriveMode(GpioPinDriveMode.Output);
            _gpioList.Add(_gpioPinRandomLightsL3Power);

            //Power pin to turn L4 lights on and off
            _gpioPinRandomLightsL4Power = _gpio.OpenPin(RandomLightsL4PowerPin);
            _gpioPinRandomLightsL4Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL4Power.SetDriveMode(GpioPinDriveMode.Output);
            _gpioList.Add(_gpioPinRandomLightsL4Power);

            //Power pin to turn L5 lights on and off
            _gpioPinRandomLightsL5Power = _gpio.OpenPin(RandomLightsL5PowerPin);
            _gpioPinRandomLightsL5Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL5Power.SetDriveMode(GpioPinDriveMode.Output);
            _gpioList.Add(_gpioPinRandomLightsL5Power);

            //Power pin to turn L6 lights on and off
            _gpioPinRandomLightsL6Power = _gpio.OpenPin(RandomLightsL6PowerPin);
            _gpioPinRandomLightsL6Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL6Power.SetDriveMode(GpioPinDriveMode.Output);
            _gpioList.Add(_gpioPinRandomLightsL6Power);


            //Pin to manually turn all lights on and off
            _randomLightsManualOverideSwitch = _gpio.OpenPin(RandomLightsManualOverideSwitchPin);
            _randomLightsManualOverideSwitch.SetDriveMode(
                _randomLightsManualOverideSwitch.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                    ? GpioPinDriveMode.InputPullUp
                    : GpioPinDriveMode.Input);

            _randomLightsManualOverideSwitch.DebounceTimeout = TimeSpan.FromMilliseconds(Const.FiftyMsDelayMs);
            //Register for the ValueChanged event so our RandomLights.ButtonPressed 
            // function is called when the button is pressed
            _randomLightsManualOverideSwitch.ValueChanged += ButtonPressed;

            _randomTimerModule = ThreadPoolTimer.CreatePeriodicTimer(ModuleTimerControl,
                TimeSpan.FromMilliseconds(Const.QuarterSecondDelayMs));
            Schedule();
        }

        public void ModuleTimerControl(ThreadPoolTimer timer)
        {
            if (_isRunning)
            {
                //As long as the lights are on remain on
                _gpioPinValueRandomLightsLed = GpioPinValue.High;
                _gpioPinRandomLightsLed.Write(GpioPinValue.High);
            }
            else
            {
                //keep making sure the lights off
                _gpioPinValueRandomLightsLed = GpioPinValue.Low;
                _gpioPinRandomLightsLed.Write(GpioPinValue.Low);
            }
        }

        public void ButtonPressed(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
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
                LightsOn();
            }
            else
            {
                LightsOff();
            }
        }

        private async void LightsOn()
        {
            _gpioPinRandomLightsM1Power.Write(GpioPinValue.Low);
            _gpioPinRandomLightsM2Power.Write(GpioPinValue.Low);
            _gpioPinRandomLightsL3Power.Write(GpioPinValue.Low);
            _gpioPinRandomLightsL4Power.Write(GpioPinValue.Low);
            _gpioPinRandomLightsL5Power.Write(GpioPinValue.Low);
            _gpioPinRandomLightsL6Power.Write(GpioPinValue.Low);
            SetLightStatus(true);
            
            lock (SpinLock)
            {
                _isRunning = AnyLightsOn();
            }
            _gpioPinValueRandomLightsLed = GpioPinValue.High;
            _gpioPinRandomLightsLed.Write(GpioPinValue.High);
            //switch off all the lights after 10 minutes
            await Task.Delay(Convert.ToInt32(Const.TenMinutesDelayMs));
            LightsOff();
        }

        private void LightsOff()
        {
            _isManualOverideSwitch = false;
            _gpioPinRandomLightsM1Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsM2Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL3Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL4Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL5Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL6Power.Write(GpioPinValue.High);
            SetLightStatus(false);

            lock (SpinLock)
            {
                _isRunning = AnyLightsOn();
            }
            _gpioPinValueRandomLightsLed = GpioPinValue.Low;
            _gpioPinRandomLightsLed.Write(GpioPinValue.Low);
        }

        private void SetLightStatus(bool status)
        {
            switch (status)
            {
                case true:
                    _lightsStatus.IsOnM1 = true;
                    _lightsStatus.IsOnM2 = true;
                    _lightsStatus.IsOnL3 = true;
                    _lightsStatus.IsOnL4 = true;
                    _lightsStatus.IsOnL5 = true;
                    _lightsStatus.IsOnL6 = true;
                    break;
                default:
                    _lightsStatus.IsOnM1 = false;
                    _lightsStatus.IsOnM2 = false;
                    _lightsStatus.IsOnL3 = false;
                    _lightsStatus.IsOnL4 = false;
                    _lightsStatus.IsOnL5 = false;
                    _lightsStatus.IsOnL6 = false;
                    break;
            }
        }

        public async void Run()
        {
            var r = new Random();

            //delay start of turn of random lights by 10 seconds to 5 minutes
            var randDelayStartTime = _delayTimeList[r.Next(_delayTimeList.Count)];
            await Task.Delay(Convert.ToInt32(randDelayStartTime));

            //turn on random lights the lights for random minutes from list 10 seconds to 5 minutes

            var randGpio = _gpioList[r.Next(_gpioList.Count)];
            randGpio.Write(GpioPinValue.Low);
            _isRunning = true;
            _gpioPinValueRandomLightsLed = GpioPinValue.High;
            //set the status of the light that has been turned on
            SetLightStatus(randGpio, true);
            var randDelayTime = _delayTimeList[r.Next(_delayTimeList.Count)];
            await Task.Delay(Convert.ToInt32(randDelayTime));
            Stop();
            //set the status of the light that has been turned off
            SetLightStatus(randGpio, false);
        }

        private void SetLightStatus(GpioPin pin,bool status)
        {
            switch (pin.PinNumber)
            {
                case RandomLightsM1PowerPin:
                    _lightsStatus.IsOnM1 = status;
                    break;
                case RandomLightsM2PowerPin:
                    _lightsStatus.IsOnM2 = status;
                    break;
                case RandomLightsL3PowerPin:
                    _lightsStatus.IsOnL3 = status;
                    break;
                case RandomLightsL4PowerPin:
                    _lightsStatus.IsOnL4 = status;
                    break;
                case RandomLightsL5PowerPin:
                    _lightsStatus.IsOnL5 = status;
                    break;
                case RandomLightsL6PowerPin:
                    _lightsStatus.IsOnL6 = status;
                    break;
            }
        }


        public void Stop()
        {
            //just turn off all the lights
            _gpioPinRandomLightsM1Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsM2Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL3Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL4Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL5Power.Write(GpioPinValue.High);
            _gpioPinRandomLightsL6Power.Write(GpioPinValue.High);

            lock (SpinLock)
            {
                _isRunning = false;
            }
            _gpioPinValueRandomLightsLed = GpioPinValue.Low;
            _gpioPinRandomLightsLed.Write(GpioPinValue.Low);
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

        public ModuleStatus ModuleStatus()
        {
            _status.IsRunning = _isRunning;
            _status.StatusText = _isRunning ? Const.Running : Const.Stopped;
            _status.LightsStatus = _lightsStatus;
            return _status;
        }

        public float ReadAdcLevel()
        {
            return 0.0f;
        }

        private void Schedule()
        {
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 0, 45, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 1, 45, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 2, 45, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 3, 45, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 4, 45, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 18, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 18, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 19, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 19, 30, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 20, 0, 0), this);
            //Scheduler.InitTimeBasedTimer(new TimeSpan(0, 20, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 21, 45, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 22, 45, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 0, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 15, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 30, 0), this);
            Scheduler.InitTimeBasedTimer(new TimeSpan(0, 23, 45, 0), this);
        }

        public void TurnOnLights(IList<string> lights)
        {
            if (lights.Any())
            {
                foreach (var light in lights)
                {
                    switch (light.ToLower())
                    {
                        case Const.Lights.M1:
                            _gpioPinRandomLightsM1Power.Write(GpioPinValue.Low);
                            _lightsStatus.IsOnM1 = true;
                            break;
                        case Const.Lights.M2:
                            _gpioPinRandomLightsM2Power.Write(GpioPinValue.Low);
                            _lightsStatus.IsOnM2 = true;
                            break;
                        case Const.Lights.L3:
                            _gpioPinRandomLightsL3Power.Write(GpioPinValue.Low);
                            _lightsStatus.IsOnL3 = true;
                            break;
                        case Const.Lights.L4:
                            _gpioPinRandomLightsL4Power.Write(GpioPinValue.Low);
                            _lightsStatus.IsOnL4 = true;
                            break;
                        case Const.Lights.L5:
                            _gpioPinRandomLightsL5Power.Write(GpioPinValue.Low);
                            _lightsStatus.IsOnL5 = true;
                            break;
                        case Const.Lights.L6:
                            _gpioPinRandomLightsL6Power.Write(GpioPinValue.Low);
                            _lightsStatus.IsOnL6 = true;
                            break;
                    }
                }

    
                lock (SpinLock)
                {
                    _isRunning = AnyLightsOn();
                }
                _gpioPinValueRandomLightsLed = GpioPinValue.High;
                _gpioPinRandomLightsLed.Write(GpioPinValue.High);
            }
            else
            {
                //turn on all the lights

                LightsOn();
            }
        }


        private bool AnyLightsOn()
        {
            var isOn = _lightsStatus.IsOnL3 || _lightsStatus.IsOnL4 || _lightsStatus.IsOnL5 
                || _lightsStatus.IsOnL6 || _lightsStatus.IsOnM1 || _lightsStatus.IsOnM2;
            return isOn;
        }

        public void TurnOffLights(IList<string> lights)
        {
            if (lights.Any())
            {
                foreach (var light in lights)
                {
                    switch (light.ToLower())
                    {
                        case Const.Lights.M1:
                            _gpioPinRandomLightsM1Power.Write(GpioPinValue.High);
                            _lightsStatus.IsOnM1 = false;
                            break;
                        case Const.Lights.M2:
                            _gpioPinRandomLightsM2Power.Write(GpioPinValue.High);
                            _lightsStatus.IsOnM2 = false;
                            break;
                        case Const.Lights.L3:
                            _gpioPinRandomLightsL3Power.Write(GpioPinValue.High);
                            _lightsStatus.IsOnL3 = false;
                            break;
                        case Const.Lights.L4:
                            _gpioPinRandomLightsL4Power.Write(GpioPinValue.High);
                            _lightsStatus.IsOnL4 = false;
                            break;
                        case Const.Lights.L5:
                            _gpioPinRandomLightsL5Power.Write(GpioPinValue.High);
                            _lightsStatus.IsOnL5 = false;
                            break;
                        case Const.Lights.L6:
                            _gpioPinRandomLightsL6Power.Write(GpioPinValue.High);
                            _lightsStatus.IsOnL6 = false;
                            break;
                    }
                }
            
                lock (SpinLock)
                {
                    _isRunning = AnyLightsOn();
                }
                _gpioPinValueRandomLightsLed = GpioPinValue.High;
                _gpioPinRandomLightsLed.Write(GpioPinValue.Low);
            }
            else
            {
                //turn off all the lights
                LightsOff();
            }
        }

        public void ToggleLights(IList<string> lights)
        {
            if (lights.Any())
            {
                foreach (var light in lights)
                {
                    switch (light.ToLower())
                    {
                        case Const.Lights.M1:
                            ToggleLight(_lightsStatus.IsOnM1, Const.Lights.M1);
                            break;
                        case Const.Lights.M2:
                            ToggleLight(_lightsStatus.IsOnM2, Const.Lights.M2);
                            break;
                        case Const.Lights.L3:
                            ToggleLight(_lightsStatus.IsOnL3, Const.Lights.L3);
                            break;
                        case Const.Lights.L4:
                            ToggleLight(_lightsStatus.IsOnL4, Const.Lights.L4);
                            break;
                        case Const.Lights.L5:
                            ToggleLight(_lightsStatus.IsOnL5, Const.Lights.L5);
                            break;
                        case Const.Lights.L6:
                            ToggleLight(_lightsStatus.IsOnL6, Const.Lights.L6);
                            break;
                    }
                }
            }
        }

        private void ToggleLight(bool status, string light)
        {
            if (!status)
            {
                _isManualOverideSwitch = true;
                TurnOnLights(new List<string>() {light});
            }
            else
            {
                TurnOffLights(new List<string>() {light});
            }
        }
    }
}
