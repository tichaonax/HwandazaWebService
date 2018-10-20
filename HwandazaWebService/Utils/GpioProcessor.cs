﻿using System;
using System.Collections.Generic;
using HwandazaWebService.RaspiModules;
using Newtonsoft.Json;

namespace HwandazaWebService.Utils
{
    public sealed class GpioProcessor
    {
        private static MainWaterPump _mainWaterPump;
        private static FishPondPump _fishPondPump;
        private static LawnIrrigator _lawnIrrigator;
        private static RandomLights _randomLights;
        private static SystemsHeartBeat _systemsHeartBeat;

        private static dynamic _imageList;
        private static dynamic _songList;
        private static dynamic _videoList;

        private static Random _rnd = new Random();
        private static MediaLibrary _mediaLibrary = new MediaLibrary();

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
            LoadMediaLibrary();
        }
       
        private static void LoadMediaLibrary()
        {
            _songList = _mediaLibrary.GetMediaSongs();
            _imageList = _mediaLibrary.GetMediaImges();
            _videoList = _mediaLibrary.GetMediaVideos();
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

        private static async System.Threading.Tasks.Task<dynamic> ActOnCommandAsync(HwandazaCommand request)
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

                case Const.CommandSongs:
                    return Shuffle(_songList);

                case Const.CommandVideos:
                    return Shuffle(_videoList);

                case Const.CommandPictures:
                    return Shuffle(_imageList);

                case Const.SystemsHeartbeatIsRunning:
                    return _systemsHeartBeat.IsRunning();

                case Const.FishpondpumpModuleIsRunning:
                    return _fishPondPump.IsRunning();

                case Const.LawnirrigatorModuleIsRunning:
                    return _lawnIrrigator.IsRunning();

                case Const.MainwaterPumpModuleIsRunning:
                    return _mainWaterPump.IsRunning();

                case Const.RandomLightsModuleLightsStatusIsOnM1:
                    return _randomLights.ModuleStatus().LightsStatus.IsOnM1;

                case Const.RandomLightsModuleLightsStatusIsOnM2:
                    return _randomLights.ModuleStatus().LightsStatus.IsOnM2;

                case Const.RandomLightsModuleLightsStatusIsOnL3:
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL3;

                case Const.RandomlightsModuleLightsStatusIsOnL4:
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL4;

                case Const.RandomLightsModuleLightsStatusIsOnL5:
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL5;

                case Const.RandomLightsModuleLightsStatusIsOnL6:
                    return _randomLights.ModuleStatus().LightsStatus.IsOnL6;

                case Const.ToggleLights:
                    _randomLights.ToggleLights(request.Lights);
                    return GetSystemStatus();

                case Const.ButtonWaterpump:
                    _mainWaterPump.ButtonPressed();
                    return GetSystemStatus();

                case Const.FishPondPump:
                    _fishPondPump.ButtonPressed();
                    return GetSystemStatus();

                case Const.ButtonLawnIrrigator:
                    _lawnIrrigator.ButtonPressed();
                    return GetSystemStatus();

                case Const.MainWaterpumpModuleAdcVoltage:
                    return _mainWaterPump.ModuleStatus().AdcVoltage;

                case Const.LawnIrrigatorModuleAdcVoltage:
                    return _lawnIrrigator.ModuleStatus().AdcVoltage;

                case Const.FishpondPumpModuleAdcVoltage:
                    return _fishPondPump.ModuleStatus().AdcVoltage;

                case Const.SetSystemDate:
                case Const.SetSystemTime:
                    UpdateSyatemDateTimeAsync(request);
                    return GetSystemDateTime();

                case Const.GetSupporterdCommandList:
                    return GetSupportedCommands();
            }

            return new AutomationError()
            {
                error = "Command not recognized",
                request = request
            };
        }

        private static List<MediaFile> Shuffle(dynamic dynamicArray)
        {
            List<MediaFile> list = dynamicArray;
            var cloneList = list.GetRange(0, list.Count);
            var randomList = new List<MediaFile>();
            int rndIndex = 0;

            while (cloneList.Count > 0)
            {
                rndIndex = _rnd.Next(0, cloneList.Count);
                randomList.Add(cloneList[rndIndex]);
                cloneList.RemoveAt(rndIndex);
            }

            return cloneList;
        }

        public static dynamic ProcessHwandazaCommand(HwandazaCommand request)
        {
            return ActOnCommandAsync(request);
        }

        private static void UpdateSyatemDateTimeAsync(HwandazaCommand request)
        {
            var serializedRequest = JsonConvert.SerializeObject(request);
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["HwandazaCommand"] = serializedRequest;
            Windows.Storage.ApplicationData.Current.SignalDataChanged();
        }

        private static dynamic GetSystemDateTime()
        {
            return GetSystemStatus();
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
                systemUpTime = _systemsHeartBeat.GetSystemUpTime(),
                statusDate = DateTime.Now.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                status = new Status()
                {
                    modules = new Modules()
                    {
                        waterPump = new WaterPump()
                        {
                            power = waterPump.IsRunning ? 1 : 0,
                            adcFloatValue = waterPump.AdcVoltage,
                            lastUpdate = waterPump.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        },

                        fishPond = new FishPond()
                        {
                            power = fishPond.IsRunning ? 1 : 0,
                            adcFloatValue = fishPond.AdcVoltage,
                            lastUpdate = fishPond.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        },

                        irrigator = new Irrigator()
                        {
                            power = lawnIrrigator.IsRunning ? 1 : 0,
                            adcFloatValue = lawnIrrigator.AdcVoltage,
                            lastUpdate = lawnIrrigator.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        }
                    },

                    lights = new Lights()
                    {
                        l3 = lights.IsOnL3 ? 1 : 0,
                        l4 = lights.IsOnL4 ? 1 : 0,
                        l5 = lights.IsOnL5 ? 1 : 0,
                        l6 = lights.IsOnL6 ? 1 : 0,
                        m1 = lights.IsOnM1 ? 1 : 0,
                        m2 = lights.IsOnM2 ? 1 : 0
                    }
                }
            };
        }

        private static dynamic GetSupportedCommands()
        {
            throw new NotImplementedException();
        }
    }
}
