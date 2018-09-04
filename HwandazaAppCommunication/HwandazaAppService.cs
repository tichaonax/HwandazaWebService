using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using HwandazaAppCommunication.Utils;
using Newtonsoft.Json;
using HwandazaAppCommunication.RaspiModules;

namespace HwandazaAppCommunication
{
    public sealed class HwandazaAppService : IBackgroundTask
    {
        private BackgroundTaskDeferral _backgroundTaskDeferral;
        private AppServiceConnection _appServiceConnection;

        private MainWaterPump _mainWaterPump;
        private FishPondPump _fishPondPump;
        private LawnIrrigator _lawnIrrigator;
        private RandomLights _randomLights;
        private SystemsHeartBeat _systemsHeartBeat;
        private GpioProcessor _gpioProcessor;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Associate a cancellation handler with the background task. 
            taskInstance.Canceled += OnCanceled;
            _backgroundTaskDeferral = taskInstance.GetDeferral();
            
            IoTimerControl.Initialize();

            //get hanlde to the various GPIO modules
            var modules = IoTimerControl.getIoTimerControls();
            _mainWaterPump = (MainWaterPump)modules.First(r => r.Module() is MainWaterPump);
            _lawnIrrigator = (LawnIrrigator)modules.First(r => r.Module() is LawnIrrigator);
            _fishPondPump = (FishPondPump)modules.First(r => r.Module() is FishPondPump);
            _randomLights = (RandomLights)modules.First(r => r.Module() is RandomLights);
            _systemsHeartBeat = (SystemsHeartBeat)modules.First(r => r.Module() is SystemsHeartBeat);
            _gpioProcessor = new GpioProcessor(_mainWaterPump, _fishPondPump, _lawnIrrigator, _randomLights, _systemsHeartBeat);
           // _systemsHeartBeat = (SystemsHeartBeat)modules.First(r => r.Module() is SystemsHeartBeat);

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null &&
                appService.Name == "HwandazaAppCommunicationService")
            {
                _appServiceConnection = appService.AppServiceConnection;
                _appServiceConnection.RequestReceived += OnRequestReceived;
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //Debug.WriteLine("Message Received:");
            var messageDefferal = args.GetDeferral();
            var message = args.Request.Message;
            HwandazaCommand command;
            try
            {
                var hwandazaCommand = message["HwandazaCommand"] as string;

                command = JsonConvert.DeserializeObject<HwandazaCommand>(hwandazaCommand);

                //we have the comman now process it
                var response = _gpioProcessor.ProcessHwandazaCommand(command);

                var status = JsonConvert.SerializeObject(response);

                var returnMessage = new ValueSet
                                    {
                                        {"Result", status},
                                        {"Status", "OK"}
                                    };

                await args.Request.SendResponseAsync(returnMessage);
                Debug.WriteLine("HwandazaAppService Command execution completed: " + status);
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                Debug.WriteLine("HwandazaAppService OnRequestReceived Eror:" + error);
            }
            messageDefferal.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //Clean up and get ready to exit
            if (_backgroundTaskDeferral != null)
            {
                _backgroundTaskDeferral.Complete();
            }
        }
    }
}
