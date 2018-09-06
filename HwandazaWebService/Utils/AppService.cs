using Newtonsoft.Json;
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
    public class AppService
    {
        AppServiceConnection _appServiceConnection;

        public async void SetAppServiceConnectionAsync()
        {
            // Initialize the AppServiceConnection
            _appServiceConnection = new AppServiceConnection
            {
                PackageFamilyName = "HwandazaWebService_7c1xvdqapnqy0",
                AppServiceName = "HwandazaAppCommunicationService"
            };

            // Send a initialize request 
            var res = await _appServiceConnection.OpenAsync();
            if (res != AppServiceConnectionStatus.Success)
            {
                throw new Exception("Failed to connect to the AppService");
            }
        }

        private bool GetBoolFromResponse(AppServiceResponse serviceResponse)
        {
            if (serviceResponse != null)
            {
                if (serviceResponse.Status == AppServiceResponseStatus.Success)
                {
                    return (bool)serviceResponse.Message["Result"];
                }
            }

            return false;
        }

        private async Task<AppServiceResponse> RequestAppServiceAsync(HwandazaCommand command)
        {
            if (_appServiceConnection == null)
            {
                SetAppServiceConnectionAsync();
            }
            AppServiceResponse response = null;

            try
            {
                var hwandazaMessage = new ValueSet { { "HwandazaCommand", JsonConvert.SerializeObject(command) } };
#pragma warning disable CS4014
                response = await _appServiceConnection.SendMessageAsync(hwandazaMessage);
#pragma warning restore CS4014
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return response;
        }

        public bool SystemsHeartBeatIsRunning()
        {
            var command = new HwandazaCommand()
            {
                Command = "SystemsHeartBeatIsRunning",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool FishPondPumpModuleIsRunning()
        {
            var command = new HwandazaCommand()
            {
                Command = "FishPondPumpModuleIsRunning",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool LawnIrrigatorModuleIsRunning()
        {
            var command = new HwandazaCommand()
            {
                Command = "LawnIrrigatorModuleIsRunning",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool MainWaterPumpModuleIsRunning()
        {
            var command = new HwandazaCommand()
            {
                Command = "MainWaterPumpModuleIsRunning",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        private float getFloatFromResponse(AppServiceResponse serviceResponse)
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

        public float MainWaterPumpModuleAdcVoltage()
        {
            var command = new HwandazaCommand()
            {
                Command = "MainWaterPumpModuleAdcVoltage",
            };

            var response = RequestAppServiceAsync(command).Result;
            return getFloatFromResponse(response);
        }

        public float IrrigatorModuleAdcVoltage()
        {
            var command = new HwandazaCommand()
            {
                Command = "IrrigatorModuleAdcVoltage",
            };

            var response = RequestAppServiceAsync(command).Result;
            return getFloatFromResponse(response);
        }

        public float FishPondPumpModuleAdcVoltage()
        {
            var command = new HwandazaCommand()
            {
                Command = "FishPondPumpModuleAdcVoltage",
            };

            var response = RequestAppServiceAsync(command).Result;
            return getFloatFromResponse(response);
        }

        public void ButtonFishPondPump()
        {
            var command = new HwandazaCommand()
            {
                Command = "ButtonFishPondPump",
            };

            var response = RequestAppServiceAsync(command).Result;
        }

        public void ButtonLawnIrrigator()
        {
            var command = new HwandazaCommand()
            {
                Command = "ButtonLawnIrrigator",
            };

            var response = RequestAppServiceAsync(command).Result;
        }

        public void ButtonWaterPump()
        {
            var command = new HwandazaCommand()
            {
                Command = "ButtonWaterPump",
            };

            var response = RequestAppServiceAsync(command).Result;
        }

        public void ButtonLights(IList<string> lights)
        {
            var command = new HwandazaCommand()
            {
                Command = "ToggleLights",
                Lights = lights,
            };

            var response = RequestAppServiceAsync(command).Result;
        }

        public bool RandomLightsModuleLightsStatusIsOnM1()
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnM1",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool RandomLightsModuleLightsStatusIsOnM2()
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnM2",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool RandomLightsModuleLightsStatusIsOnL3()
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL3",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool RandomLightsModuleLightsStatusIsOnL4()
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL4",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool RandomLightsModuleLightsStatusIsOnL5()
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL5",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public bool RandomLightsModuleLightsStatusIsOnL6()
        {
            var command = new HwandazaCommand()
            {
                Command = "RandomLightsModuleLightsStatusIsOnL6",
            };

            var response = RequestAppServiceAsync(command).Result;
            return GetBoolFromResponse(response);
        }

        public HwandazaAutomation GetStatus()
        {
            HwandazaAutomation status;
            var command = new HwandazaCommand()
            {
                Command = "Status",
            };

            var serviceResponse = RequestAppServiceAsync(command).Result;
            status = serviceResponse.Message["Result"] as HwandazaAutomation;
            return status;
        }
    }
}
