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
        private string _path;
        private SQLite.Net.SQLiteConnection _sqLiteConnection;
        private const string StatusRowGuidId = "D5A87081-1B16-4876-8B0A-02275EAB9007";

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Associate a cancellation handler with the background task. 
            taskInstance.Canceled += OnCanceled;
            _backgroundTaskDeferral = taskInstance.GetDeferral();

            //sql-lite database
            _path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "db1.sqlite");
            _sqLiteConnection = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), _path);
            _sqLiteConnection.CreateTable<HwandazaStatus>();
            _sqLiteConnection.CreateTable<HwandazaCommandStatus>();

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
                command.SqlRowGuidId = Guid.NewGuid().ToString();

                _sqLiteConnection.Insert(new HwandazaCommandStatus()
                {
                    RowGuidId = command.SqlRowGuidId
                });
                
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["HwandazaCommand"] = hwandazaCommand;
                Windows.Storage.ApplicationData.Current.SignalDataChanged();
                await Task.Delay(500);
                //poll to see if the commans has been completed
                //cancel command after 1000 times ????
                bool bNotDone = false;
                var count = 0;
                while (bNotDone)
                {
                    var doneStatus = _sqLiteConnection
                        .Table<HwandazaCommandStatus>()
                        .FirstOrDefault(x => x.RowGuidId == command.SqlRowGuidId);

                    if (doneStatus == null)
                    {
                        bNotDone = false;
                    }
                    else
                    {
                        //Debug.WriteLine("Waiting for command execution : SqlRowGuidId = " + doneStatus.RowGuidId);
                        await Task.Delay(500);
                        count++;
                    }
                }

                var status = JsonConvert.SerializeObject(GetSystemStatus());
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

        private HwandazaStatus GetSystemStatus()
        {
            var status = _sqLiteConnection
                .Table<HwandazaStatus>()
                .FirstOrDefault(x => x.RowGuidId == StatusRowGuidId);
            return status;
        }

        private void DeleteCompletedTask(string sqlRowId)
        {
            var delete =
                _sqLiteConnection.Table<HwandazaCommandStatus>().FirstOrDefault(m => m.RowGuidId == sqlRowId);

            if (delete != null)
            {
                _sqLiteConnection.Delete(delete);
            }
        }
    }
}
