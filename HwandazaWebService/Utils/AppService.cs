using HwandazaAppCommunication.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace HwandazaWebService.Utils
{
    public static class AppService
    {
        public static async Task<AppServiceConnection> getAppServiceConnectionAsync()
        {
            AppServiceConnection appServiceConnection;

            // Initialize the AppServiceConnection
            appServiceConnection = new AppServiceConnection
            {
                PackageFamilyName = "HwandazaWebService_7c1xvdqapnqy0",
                AppServiceName = "HwandazaAppCommunicationService"
            };

            // Send a initialize request 
            var res = await appServiceConnection.OpenAsync();
            if (res != AppServiceConnectionStatus.Success)
            {
                throw new Exception("Failed to connect to the AppService");
            }

            return appServiceConnection;
        }
        
        private static bool getBoolFromResponse(AppServiceResponse serviceResponse)
        {
            if (serviceResponse != null)
            {
                if (serviceResponse.Status == AppServiceResponseStatus.Success)
                {
                    return (bool) serviceResponse.Message["Result"];
                }
            }

            return false;
        }

        private static async Task<AppServiceResponse> RequestAppServiceAsync(AppServiceConnection appService, HwandazaCommand command)
        {
            AppServiceResponse response = null;

            try
            {
                var hwandazaMessage = new ValueSet { { "HwandazaCommand", Newtonsoft.Json.JsonConvert.SerializeObject(command) } };
#pragma warning disable CS4014
                response = await appService.SendMessageAsync(hwandazaMessage);
#pragma warning restore CS4014
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return response;
        }

        public static bool SystemsHeartBeatIsRunning(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "SystemsHeartBeatIsRunning",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool FishPondPumpModuleIsRunning(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "FishPondPumpModuleIsRunning",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool LawnIrrigatorModuleIsRunning(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "LawnIrrigatorModuleIsRunning",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool MainWaterPumpModuleIsRunning(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "MainWaterPumpModuleIsRunning",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        private static float getFloatFromResponse(AppServiceResponse serviceResponse)
        {
            if (serviceResponse != null)
            {
                if (serviceResponse.Status == AppServiceResponseStatus.Success)
                {
                    return (float)serviceResponse.Message["Result"];
                }
            }

            return 0.0f; 
        }

        public static float MainWaterPumpModuleAdcVoltage(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "MainWaterPumpModuleAdcVoltage",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getFloatFromResponse(response);
        }

        public static float IrrigatorModuleAdcVoltage(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "IrrigatorModuleAdcVoltage",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getFloatFromResponse(response);
        }

        public static float FishPondPumpModuleAdcVoltage(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "FishPondPumpModuleAdcVoltage",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getFloatFromResponse(response);
        }

        public static void ButtonFishPondPump(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "ButtonFishPondPump",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
        }

        public static void ButtonLawnIrrigator(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "ButtonLawnIrrigator",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
        }

        public static void ButtonWaterPump(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "ButtonWaterPump",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
        }

        public static void ButtonLights(AppServiceConnection appService, IList<string> lights)
        {
            var command = new HwandazaCommand()
            {
                Command = "ToggleLights",
                Lights = lights,
            };

            var response = RequestAppServiceAsync(appService, command).Result;
        }

        public static bool RandomLightsModuleLightsStatusIsOnM1(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnM1",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool RandomLightsModuleLightsStatusIsOnM2(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnM2",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool RandomLightsModuleLightsStatusIsOnL3(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL3",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool RandomLightsModuleLightsStatusIsOnL4(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL4",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool RandomLightsModuleLightsStatusIsOnL5(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL5",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }

        public static bool RandomLightsModuleLightsStatusIsOnL6(AppServiceConnection appService)
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL6",
            };

            var response = RequestAppServiceAsync(appService, command).Result;
            return getBoolFromResponse(response);
        }
    }
}
