using System;
using System.Collections.Generic;
using HwandazaWebService.RaspiModules;

namespace HwandazaWebService.Utils
{
    public sealed class GpioProcessor
    {
        private static MainWaterPump _mainWaterPump;
        private static FishPondPump _fishPondPump;
        private static LawnIrrigator _lawnIrrigator;
        private static RandomLights _randomLights;
        private static SystemsHeartBeat _systemsHeartBeat;

        public static void Initialize(
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

        public static void ButtonWaterPump()
        {
            _mainWaterPump.ButtonPressed();
        }

        public static void ButtonFishPondPump()
        {
            _fishPondPump.ButtonPressed();
        }

        public static void ButtonLawnIrrigator()
        {
            _lawnIrrigator.ButtonPressed();
        }

        public static void ButtonLights(List<string> lights)
        {
            _randomLights.ToggleLights(lights);
        }

        private static dynamic ActOnCommandOff(HwandazaCommand request)
        {
            switch (request.Module.ToLower())
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
                    _randomLights.TurnOffLights(request.Lights);
                    break;
            }

            return GetSystemStatus();
        }

        private static dynamic ActOnCommandOn(HwandazaCommand request)
        {
            switch (request.Module.ToLower())
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
                    _randomLights.TurnOnLights(request.Lights);
                    break;
            }

            return GetSystemStatus();
        }

        private static dynamic ActOnCommandOperations(HwandazaCommand request)
        {
            throw new NotImplementedException();
        }

        private static dynamic ActOnCommand(HwandazaCommand request)
        {
            switch (request.Command.ToLower())
            {
                case Const.CommandOn:
                    return ActOnCommandOn(request);

                case Const.CommandOff:
                    return ActOnCommandOff(request);

                case Const.CommandOperations:
                    return ActOnCommandOperations(request);

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
                    _randomLights.ToggleLights(request.Lights);
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
                Request = request
            };
        }

        public static dynamic ProcessHwandazaCommand(HwandazaCommand request)
        {
            return ActOnCommand(request);
        }

        private static HwandazaAutomation GetSystemStatus()
        {
            // interrogate the raspberry pi and get all the information

            var waterPump = _mainWaterPump.ModuleStatus();

            var fishPond = _fishPondPump.ModuleStatus();

            var lawnIrrigator = _lawnIrrigator.ModuleStatus();

            var lights = _randomLights.ModuleStatus().LightsStatus;

            return new HwandazaAutomation()
            {
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
