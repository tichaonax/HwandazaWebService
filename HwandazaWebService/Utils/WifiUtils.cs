using System;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.System;

namespace HwandazaWebService.Utils
{
    public static class WifiUtils
    {

        public static async Task CheckWifiStatusAsync()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            if (!internet)
            {
                var adapter = IsAdapterPresent();
                if (!adapter)
                {
                    try
                    {
                        const string options = "/renew";
                        var CommandLineProcesserExe = @"C:\Windows\System32\ipconfig.exe";
                        var result = await ProcessLauncher.RunToCompletionAsync(CommandLineProcesserExe, options);
                    }
                    catch(Exception ex)
                    {
                        //do nothing
                        //var msg = ex.Message;
                        //var messageDialog = new Windows.UI.Popups.MessageDialog(msg);
                        //await messageDialog.ShowAsync();
                    }
                }
            }
        }


        private static bool IsAdapterPresent()
        {
            var result = Task.Run(() => CheckIfWIFIAdapterIsPresent()).Result;
            return result;
        }
    
        private async static Task<bool> CheckIfWIFIAdapterIsPresent()
        {
            var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
            if (result.Count >= 1)
            {
                return true;
            }
            return false;
        }
    }
}
