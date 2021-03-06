﻿using Windows.Devices.Gpio;
using Windows.System.Threading;
using HwandazaWebService.Utils;

namespace HwandazaWebService.RaspiModules
{
    public interface IModule
    {
        void InitializeGPIO();  //use this to do all GPIO initialization required
        void ModuleTimerControl(ThreadPoolTimer timer);
        void ButtonPressed(GpioPin sender, GpioPinValueChangedEventArgs e); //Add logic to determine what happens when the button is pressed
        void Run();
        void Stop();
        bool IsRunning();
        bool ShouldRunToday();
        IModule Module();
        ModuleStatus ModuleStatus();
        float ReadAdcLevel();
        void ManualOverideSwitch();
        void ButtonPressed();
    }
}
