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

namespace HwandazaAppCommunication
{
    public sealed class HwandazaAppService : IBackgroundTask
    {
        private BackgroundTaskDeferral _backgroundTaskDeferral;
        private AppServiceConnection _appServiceConnection;

        private AppServiceConnection _requestAppServiceConnection;


        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Associate a cancellation handler with the background task. 
            taskInstance.Canceled += OnCanceled;
            _backgroundTaskDeferral = taskInstance.GetDeferral();
   
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
            if (_requestAppServiceConnection == null)
            {
                // Initialize the AppServiceConnection
                _requestAppServiceConnection = new AppServiceConnection
                {
                    PackageFamilyName = "HwandazaWebService_7c1xvdqapnqy0",
                    AppServiceName = "HwandazaWebService",
                };

                // Send a initialize request 
                var res = await _requestAppServiceConnection.OpenAsync();
                if (res != AppServiceConnectionStatus.Success)
                {
                    throw new Exception("Failed to connect to the AppService");
                }
            }

            //Debug.WriteLine("Message Received:");
            var messageDefferal = args.GetDeferral();
            var message = args.Request.Message;
            HwandazaCommand command;
            try
            {
                var hwandazaCommand = message["HwandazaCommand"] as string;

                command = JsonConvert.DeserializeObject<HwandazaCommand>(hwandazaCommand);

                //we have the comman now process it
                var response = RequestAppServiceAsync(command);
                
                var status = JsonConvert.SerializeObject(null);

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

        private async Task<AppServiceResponse> RequestAppServiceAsync(HwandazaCommand command)
        {
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
    }
}
