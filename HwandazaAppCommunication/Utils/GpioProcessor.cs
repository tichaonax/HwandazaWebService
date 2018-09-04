using System;
using System.Collections.Generic;
using System.Diagnostics;
using HwandazaAppCommunication.RaspiModules;

namespace HwandazaAppCommunication.Utils
{
    public sealed class GpioProcessor
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

            public const string CommandOn = "on";
            public const string CommandOff = "off";
            public const string CommandOperations = "operations";
            public const string CommandStatus = "status";
        }

        private readonly MainWaterPump _mainWaterPump;
        private readonly FishPondPump _fishPondPump;
        private readonly LawnIrrigator _lawnIrrigator;
        private readonly RandomLights _randomLights;
        private readonly SystemsHeartBeat _systemsHeartBeat;

        public GpioProcessor(
            MainWaterPump mainWaterPump, 
            FishPondPump fishPondPump, 
            LawnIrrigator lawnIrrigator, 
            RandomLights randomLights,
            SystemsHeartBeat systemsHeartBeat)
        {
            _mainWaterPump = mainWaterPump;
            _fishPondPump = fishPondPump;
            _lawnIrrigator = lawnIrrigator;
            _randomLights = randomLights;
            _systemsHeartBeat = systemsHeartBeat;
        }

        private dynamic CommandOff(HwandazaCommand command)
        {
            switch (command.Module.ToLower())
            {
                case Const.MainWaterPump:
                    _mainWaterPump.Stop();
                    break;
                case Const.FishPondPump:
                    _fishPondPump.Stop();
                    break;
                case Const.LawnIrrigator:
                    _lawnIrrigator.Stop();
                    break;
                case Const.RandomLights:
                    _randomLights.TurnOffLights(command.Lights);
                    break;
            }

            return GetSystemStatus();
        }

        private dynamic CommandOn(HwandazaCommand command)
        {
            switch (command.Module.ToLower())
            {
                case Const.MainWaterPump:
                    _mainWaterPump.ManualOverideSwitch();
                    _mainWaterPump.Run();
                    break;
                case Const.FishPondPump:
                    _fishPondPump.ManualOverideSwitch();
                    _fishPondPump.Run();
                    break;
                case Const.LawnIrrigator:
                    _lawnIrrigator.ManualOverideSwitch();
                    _lawnIrrigator.Run();
                    break;
                case Const.RandomLights:
                    _randomLights.TurnOnLights(command.Lights);
                    break;
            }

            return GetSystemStatus();
        }

        private dynamic CommandOperations(HwandazaCommand command)
        {
            throw new NotImplementedException();
        }

        private dynamic ActOnCommand(HwandazaCommand command)
        {
            switch (command.Command.ToLower())
            {
                case Const.CommandOn:
                    return CommandOn(command);

                case Const.CommandOff:
                    return CommandOff(command);

                case Const.CommandOperations:
                    return CommandOperations(command);

                case Const.CommandStatus:
                    return GetSystemStatus();

                case "systemsheartbeatisrunning":
                    return _systemsHeartBeat.IsRunning();

                case "fishpondpumpmoduleisrunning":
                    return _fishPondPump.IsRunning();

                case "lawnirrigatormoduleisrunning":
                    return _lawnIrrigator.IsRunning();

                case "mainwaterpumpmoduleisrunning":
                    return _mainWaterPump.IsRunning();

                case "randomlightsmoduleLightsstatusisonm1":
                    return _randomLights.ModuleStatus().LightsStatus.IsOnM1;

                case "randomLightsmodulelightsstatusisonm2":
                    return _randomLights.ModuleStatus().LightsStatus.IsOnM2;

                case "randomlightsmodulelightsstatusisonl3":
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL3;

                case "randomlightsmodulelightsstatusisonl4":
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL4;

                case "randomlightsmodulelightsstatusisonl5":
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL5;

                case "randomlightsmodulelightsstatusisonl6":
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL6;

                case "togglelights":
                    _randomLights.ToggleLights(command.Lights);
                    return GetSystemStatus();

                case "buttonwaterpump":
                    _mainWaterPump.ButtonPressed();
                    return GetSystemStatus();

                case "buttonfishpondpump":
                    _fishPondPump.ButtonPressed();
                    return GetSystemStatus();

                case "buttonlawnirrigator":
                    _lawnIrrigator.ButtonPressed();
                    return GetSystemStatus();

                case "mainWaterpumpmoduleadcvoltage":
                   return _mainWaterPump.ModuleStatus().AdcVoltage;

                case "irrigatormoduleadcvoltage":
                    return _lawnIrrigator.ModuleStatus().AdcVoltage;

                case "fishpondpumpmoduleadcvoltage":
                    return _fishPondPump.ModuleStatus().AdcVoltage;

            }

            return new AutomationError()
            {
                Error = "Command not recognized",
                Command = command
            };
        }

        public dynamic ProcessHwandazaCommand(HwandazaCommand command)
        {
            return ActOnCommand(command);
        }

        private HwandazaAutomation GetSystemStatus()
        {
            // interrogate the raspberry pi and get all the information

            var waterPump = _mainWaterPump.ModuleStatus();

            var fishPond = _fishPondPump.ModuleStatus();

            var lawnIrrigator = _lawnIrrigator.ModuleStatus();

            var lights = _randomLights.ModuleStatus().LightsStatus;

            return new HwandazaAutomation()
            {
                statusId = 0,
                statusDate = DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"),
                status = new Status()
                {
                    modules = new Modules()
                    {
                        WaterPump = new WaterPump()
                        {
                            power = waterPump.IsRunning ? 1 : 0,
                            adcFloatValue = waterPump.AdcVoltage,
                        },
                        FishPond = new FishPond()
                        {
                            power = fishPond.IsRunning ? 1 : 0,
                            adcFloatValue = fishPond.AdcVoltage,
                        },
                        Irrigator = new Irrigator()
                        {
                            power = lawnIrrigator.IsRunning ? 1 : 0,
                            adcFloatValue = lawnIrrigator.AdcVoltage,
                        }
                    },
                    lights = new Lights()
                    {
                        L3 = lights.IsOnL3 ? 1 : 0,
                        L4 = lights.IsOnL4 ? 1 : 0,
                        L5 = lights.IsOnL5 ? 1 : 0,
                        L6 = lights.IsOnL6 ? 1 : 0,
                        M1 = lights.IsOnM1 ? 1 : 0,
                        M2 = lights.IsOnM2 ? 1 : 0
                    }
                }
            };
        }
    }
}
