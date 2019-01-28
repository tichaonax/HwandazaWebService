using System;
using System.Collections.Generic;
using HwandazaWebService.RaspiModules;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HwandazaWebService.Utils
{
    public sealed class GpioProcessor
    {
        private static MainWaterPump _mainWaterPump;
        private static FishPondPump _fishPondPump;
        private static LawnIrrigator _lawnIrrigator;
        private static RandomLights _randomLights;
        private static SystemsHeartBeat _systemsHeartBeat;

        private static Task<List<MediaFile>> _imageList;
        private static Task<List<MediaFile>> _songList;
        private static Task<List<MediaFile>> _videoList;

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
            LoadMediaLibraryAsync();
        }

        private static void LoadMediaLibraryAsync()
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

        private static async Task<dynamic> ActOnCommandAsync(HwandazaCommand request)
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

                case Const.CommandNamedSongs:
                    return GetNamedSongs(request);

                case Const.CommandFolderSongs:
                    return GetFolderSongs(request);

                case Const.CommandRootFolders:
                    return GetRootFolders(request);

                case Const.CommandSongs:
                    var songs = ShuffleSongsWithImages(_songList);
                    if (songs.Count > 10)
                    {
                        int rIntSong = _rnd.Next(0, (songs.Count - 10));
                        return PackageResult(songs.GetRange(rIntSong, 10), songs.Count);
                    }

                    return PackageResult(songs, songs.Count);

                case Const.CommandVideos:
                    var videos = Shuffle(_videoList);
                    if (videos.Count > 200)
                    {
                        int rIntVideo = _rnd.Next(0, (videos.Count - 200));
                        return PackageResult(videos.GetRange(rIntVideo, 200), videos.Count);
                    }

                    return PackageResult(videos, videos.Count);

                case Const.CommandPictures:
                    var images = Shuffle(_imageList);
                    if (images.Count > 200)
                    {
                        int rIntPictures = _rnd.Next(0, (images.Count - 200));
                        return PackageResult(images.GetRange(rIntPictures, 200), images.Count);
                    }

                    return PackageResult(images, images.Count);

                case Const.CommandCoverPicture:
                    var coverpics = Shuffle(_imageList);
                    return coverpics[_rnd.Next(0, (coverpics.Count - 1))];

                case Const.SystemsHeartbeatIsRunning:
                    return new SystemsHeartbeat() { IsRunning = _systemsHeartBeat.IsRunning() };

                case Const.FishpondpumpModuleIsRunning:
                    return _fishPondPump.IsRunning();

                case Const.LawnirrigatorModuleIsRunning:
                    return _lawnIrrigator.IsRunning();

                case Const.MainwaterPumpModuleIsRunning:
                    return _mainWaterPump.IsRunning();

                case Const.RandomLightsModuleLightsStatusIsOnM1:
                    return _randomLights.ModuleStatus().LightsStatus.M1.IsOn;

                case Const.RandomLightsModuleLightsStatusIsOnM2:
                    return _randomLights.ModuleStatus().LightsStatus.M2.IsOn;

                case Const.RandomLightsModuleLightsStatusIsOnL3:
                    return _randomLights.ModuleStatus().LightsStatus.L3.IsOn;

                case Const.RandomlightsModuleLightsStatusIsOnL4:
                    return _randomLights.ModuleStatus().LightsStatus.L4.IsOn;

                case Const.RandomLightsModuleLightsStatusIsOnL5:
                    return _randomLights.ModuleStatus().LightsStatus.L5.IsOn;

                case Const.RandomLightsModuleLightsStatusIsOnL6:
                    return _randomLights.ModuleStatus().LightsStatus.L6.IsOn;

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

        private static dynamic PackageResult(List<MediaFile> list, int totalCount)
        {
            return new HwandaResponse()
            {
                statusDate = DateTime.Now.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                recordCount = list.Count,
                totalAvailable = totalCount,
                result = list,
            };
        }

        private static List<MediaFile> Shuffle(Task<List<MediaFile>> task)
        {
            List<MediaFile> list = task.Result;
            var cloneList = list.GetRange(0, list.Count);
            var randomList = new List<MediaFile>();
            int rndIndex = 0;

            while (cloneList.Count > 0)
            {
                rndIndex = _rnd.Next(0, cloneList.Count);
                randomList.Add(cloneList[rndIndex]);
                cloneList.RemoveAt(rndIndex);
            }

            list = null;
            cloneList = null;
            return randomList;
        }

        private static List<MediaFile> ShuffleSongsWithImages(Task<List<MediaFile>> task)
        {
            List<MediaFile> list = task.Result;
            var cloneList = list.GetRange(0, list.Count);
            var randomList = new List<MediaFile>();
            int rndIndex = 0;

            while (cloneList.Count > 0)
            {
                rndIndex = _rnd.Next(0, cloneList.Count);
                cloneList[rndIndex].Cover = GetRandomImageFromPictures();
                randomList.Add(cloneList[rndIndex]);
                cloneList.RemoveAt(rndIndex);
            }

            list = null;
            cloneList = null;
            return randomList;
        }

        public static string GetRandomImageFromPictures()
        {
            if (_imageList.IsCompleted)
            {
                var rndIndex = _rnd.Next(0, _imageList.Result.Count);
                return _imageList.Result[rndIndex].Url;
            }
            else
            {
                return null;
            }
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

        private static dynamic GetNamedSongs(HwandazaCommand request)
        {
            var songs = Shuffle(_songList);

            var list = new List<MediaFile>();

            foreach (MediaFile song in songs)
            {

                if (Uri.UnescapeDataString(song.Url.ToLower()).Contains(request.Module.ToLower()))
                {
                    song.Cover = GetRandomImageFromPictures();
                    list.Add(song);
                }
            }

            songs = null;
            return list;
        }

        private static dynamic GetFolderSongs(HwandazaCommand request)
        {
            var songs = Shuffle(_songList);

            var list = new List<MediaFile>();

            foreach (MediaFile song in songs)
            {
                if (Uri.UnescapeDataString(song.Url.ToLower()).StartsWith(request.Module.ToLower()))
                {
                    song.Cover = GetRandomImageFromPictures();
                    list.Add(song);
                }
            }

            songs = null;
            return PackageResult(list, list.Count);
        }

        private static dynamic GetRootFolders(HwandazaCommand request)
        {
            var list = new List<string>();

            foreach (MediaFile song in _songList.Result)
            {
                var newFolder = Uri.UnescapeDataString(song.Url.Split(new string[] { "/" }, StringSplitOptions.None)[0]);
                if (!list.Contains(newFolder))
                {
                    list.Add(newFolder);
                }
            }

            return list;
        }

        private static HwandazaAutomation GetSystemStatus()
        {
            // interrogate the raspberry pi and get all the information

            var waterPump = _mainWaterPump.ModuleStatus();

            var fishPond = _fishPondPump.ModuleStatus();

            var lawnIrrigator = _lawnIrrigator.ModuleStatus();

            var lights = _randomLights.ModuleStatus().LightsStatus;

            var isRunning = _systemsHeartBeat.IsRunning();

            return new HwandazaAutomation()
            {
                isRunning = isRunning,
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
                        l3 = new l3()
                        {
                            lastUpdate = lights.L3.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                            power = lights.L3.IsOn ? 1 : 0,
                        },

                        l4 = new l4()
                        {
                            power = lights.L4.IsOn ? 1 : 0,
                            lastUpdate = lights.L4.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        },

                        l5 = new l5()
                        {
                            power = lights.L5.IsOn ? 1 : 0,
                            lastUpdate = lights.L5.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        },

                        l6 = new l6()
                        {
                            power = lights.L6.IsOn ? 1 : 0,
                            lastUpdate = lights.L6.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        },

                        m1 = new m1()
                        {
                            power = lights.M1.IsOn ? 1 : 0,
                            lastUpdate = lights.M1.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        },

                        m2 = new m2()
                        {
                            power = lights.M2.IsOn ? 1 : 0,
                            lastUpdate = lights.M2.LastUpdate.ToString("yyyy'-'MM'-'dd' 'hh':'mm':'ss tt"),
                        },
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
