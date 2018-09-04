using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using HwandazaAppCommunication.RaspiModules;
using Newtonsoft.Json;
using Path = System.IO.Path;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using HwandazaAppCommunication.Utils;
using HwandazaWebService.Utils;
using Windows.ApplicationModel.AppService;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HwandazaWebService
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
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
            public const string Running = "Running";
            public const string Stopped = "Stopped";

            public const string MainWaterPump = "mainwaterpump";
            public const string FishPondPump = "fishpondpump";
            public const string RandomLights = "randomlights";
            public const string LawnIrrigator = "lawnirrigator";
            public const string Operations = "operations";

            public const string CommandOn = "ON";
            public const string CommandOff = "OFF";
            public const string CommandOperations = "OPERATIONS";
            public const string CommandStatus = "STATUS";

            public class Lights
            {
                public const string M1 = "m1";
                public const string M2 = "m2";
                public const string L3 = "l3";
                public const string L4 = "l4";
                public const string L5 = "l5";
                public const string L6 = "l6";
            }
        }
     
        private readonly SolidColorBrush _redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private readonly SolidColorBrush _ledOffBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private readonly SolidColorBrush _deepblueBrush = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
        private readonly SolidColorBrush _greenBrush = new SolidColorBrush(Windows.UI.Colors.SpringGreen);
        private readonly SolidColorBrush _goldBrush = new SolidColorBrush(Windows.UI.Colors.Gold);
        private readonly SolidColorBrush _ghostWhiteBrush = new SolidColorBrush(Windows.UI.Colors.GhostWhite);
        private SolidColorBrush _currentSystemHeartBeatBrush;
        private SolidColorBrush _currentPumpStatusBrush;
        private SolidColorBrush _currentPondStatusBrush;
        private SolidColorBrush _currentLawnStatusBrush;
        private SolidColorBrush _currentM1StatusBrush;
        private SolidColorBrush _currentM2StatusBrush;
        private SolidColorBrush _currentL3StatusBrush;
        private SolidColorBrush _currentL4StatusBrush;
        private SolidColorBrush _currentL5StatusBrush;
        private ThreadPoolTimer _poolTimerUiUpdate;
        private ThreadPoolTimer _poolTimerHeartBeat;
        private ThreadPoolTimer _imageTimerHeartBeat;


        private bool _bDateChangedByUser = false;
        private bool _bTimeChangedByUser = false;

        private AppServiceConnection _appServiceConnection;

        private const string BackgroundImageFolder = @"Assets\Album";

        private static readonly List<string> BackgroundImageList = new List<string>();
        static Random _rnd = new Random();

        public MainPage()
        {
            this.InitializeComponent();

            _appServiceConnection = AppService.GetAppServiceConnectionAsync().Result;

            LoadBackGroundImages(BackgroundImageFolder);

            InitializeCalender();

            _currentSystemHeartBeatBrush = _ledOffBrush;
 
            //setup timer to update the UI
            _poolTimerUiUpdate = ThreadPoolTimer.CreatePeriodicTimer(HwandazaUiUpdate, TimeSpan.FromMilliseconds(Const.HalfSecondDelayMs));

            _poolTimerHeartBeat = ThreadPoolTimer.CreatePeriodicTimer(SystemHeartBeatControl, TimeSpan.FromMilliseconds(Const.OneSecondDelayMs));

            _imageTimerHeartBeat = ThreadPoolTimer.CreatePeriodicTimer(ImageHeartBeatControlAsync, period: TimeSpan.FromMilliseconds(Const.FiveSecondsDelayMs));
        }

        private void SystemHeartBeatControl(ThreadPoolTimer timer)
        {
            if (AppService.SystemsHeartBeatIsRunning(_appServiceConnection))
            {
                _currentSystemHeartBeatBrush = _currentSystemHeartBeatBrush == _ledOffBrush ? _deepblueBrush : _ledOffBrush;
                /* UI updates must be invoked on the UI thread */
                var task = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //  var dt = DateTime.Now;
                    HeartBeatLED.Fill = _currentSystemHeartBeatBrush;        /* Display the value on screen                      */
                });
            }
            else
            {
                _currentSystemHeartBeatBrush = _ledOffBrush;
                var task = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    HeartBeatLED.Fill = _currentSystemHeartBeatBrush;        /* Display the value on screen                      */
                });
            }

            var updateDate = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (!(_bDateChangedByUser || _bTimeChangedByUser))
                {
                    HwandaTimePicker.Time = new TimeSpan(DateTime.Now.Ticks);
                    CalendarDatePickerControl.Date = DateTime.Now;
                }
            });
        }

        private void ImageHeartBeatControlAsync(ThreadPoolTimer timer)
        {
            var task = Dispatcher.RunAsync(
                   CoreDispatcherPriority.Normal, async () =>
                   {
                       string uri = string.Format("ms-appx:///{0}", GetNextBackGroundImage());
                       if (uri != null)
                       {
                           StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
                           BitmapImage image = new BitmapImage();
                          
                           IRandomAccessStream ram = await file.OpenAsync(FileAccessMode.Read);
                           await image.SetSourceAsync(ram);

                           HwandaGrid.Background = new ImageBrush() { ImageSource = image, Stretch = Stretch.UniformToFill, Opacity = 0.75};
                       }
                   });
        }

        
        private void FishPondPump_OnClick(object sender, RoutedEventArgs e)
        {
            AppService.ButtonFishPondPump(_appServiceConnection);
        }

        private void LawnIrrigator_OnClick(object sender, RoutedEventArgs e)
        {
            AppService.ButtonLawnIrrigator(_appServiceConnection);
        }

        private void WaterPunp_OnClick(object sender, RoutedEventArgs e)
        {
            AppService.ButtonWaterPump(_appServiceConnection);
        }
        
        private void HwandazaUiUpdate(ThreadPoolTimer timer)
        {
            SystemStatus();
        }
        
        private void ButtonBase_OnClick_M1(object sender, RoutedEventArgs e)
        {
            AppService.ButtonLights(_appServiceConnection, new List<string>() { Const.Lights.M1});
        }

        private void ButtonBase_OnClick_M2(object sender, RoutedEventArgs e)
        {
            AppService.ButtonLights(_appServiceConnection, new List<string>() { Const.Lights.M2 });
        }

        private void ButtonBase_OnClick_L3(object sender, RoutedEventArgs e)
        {
            AppService.ButtonLights(_appServiceConnection, new List<string>() { Const.Lights.L3 });
        }

        private void ButtonBase_OnClick_L4(object sender, RoutedEventArgs e)
        {
            AppService.ButtonLights(_appServiceConnection, new List<string>() { Const.Lights.L4 });
        }

        private void ButtonBase_OnClick_L5(object sender, RoutedEventArgs e)
        {
            AppService.ButtonLights(_appServiceConnection, new List<string>() { Const.Lights.L5 });
        }

        private void ButtonBase_OnClick_L6(object sender, RoutedEventArgs e)
        {
            AppService.ButtonLights(_appServiceConnection, new List<string>() { Const.Lights.L6 });
        }

        private void SystemStatus()  
        {
            //blink the status lights for each operation
            _currentPumpStatusBrush = _currentPumpStatusBrush == _ledOffBrush ? _redBrush : _ledOffBrush;
            _currentPondStatusBrush = _currentPondStatusBrush == _ledOffBrush ? _goldBrush : _ledOffBrush;
            _currentLawnStatusBrush = _currentLawnStatusBrush == _ledOffBrush ? _greenBrush : _ledOffBrush;
           
            if (AppService.FishPondPumpModuleIsRunning(_appServiceConnection))
            {
                /* UI updates must be invoked on the UI thread */
                var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                                                   {
                                                       PondLED.Fill = _currentPondStatusBrush;
                                                       /* Display the value on screen                      */
                                                   });
            }
            else
            {
                var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                                                   {
                                                       PondLED.Fill = _ledOffBrush;
                                                       /* Display the value on screen                      */
                                                   });
            }

            if (AppService.LawnIrrigatorModuleIsRunning(_appServiceConnection))
            {
                /* UI updates must be invoked on the UI thread */
                var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                                                   {
                                                       LawnLED.Fill = _currentLawnStatusBrush;
                                                       /* Display the value on screen                      */
                                                   });
            }
            else
            {
                var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                                                   {
                                                       LawnLED.Fill = _ledOffBrush;
                                                       /* Display the value on screen                      */
                                                   });
            }
            if (AppService.MainWaterPumpModuleIsRunning(_appServiceConnection))
            {
                /* UI updates must be invoked on the UI thread */
                var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                                                   {
                                                       PumpLED.Fill = _currentPumpStatusBrush;
                                                       /* Display the value on screen                      */
                                                   });
            }
            else
            {
                var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                                                   {
                                                       PumpLED.Fill = _ledOffBrush;
                                                       /* Display the value on screen                      */
                                                   });
            }

            SetLightStatus(Const.Lights.M1);
            SetLightStatus(Const.Lights.M2);
            SetLightStatus(Const.Lights.L3);
            SetLightStatus(Const.Lights.L4);
            SetLightStatus(Const.Lights.L5);
            SetLightStatus(Const.Lights.L6);
            UpdateButtonADCValues();
        }

        private void UpdateButtonADCValues()
        {
            /* UI updates must be invoked on the UI thread */
            var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                    {
                        WaterPumpADC.Text = string.Format("ADC: {0:0.00}V", AppService.MainWaterPumpModuleAdcVoltage(_appServiceConnection));
                        PondPumpADC.Text = string.Format("ADC: {0:0.00}V", AppService.FishPondPumpModuleAdcVoltage(_appServiceConnection));
                        LawnIrrigatorADC.Text = string.Format("ADC: {0:0.00}V", AppService.IrrigatorModuleAdcVoltage(_appServiceConnection));
                        /* Display the value on screen                      */
                    });
        }
        private void SetLightStatus(string light)
        {
            switch (light)
            {
                case Const.Lights.M1:
                   SetLedColor(M1LED, AppService.RandomLightsModuleLightsStatusIsOnM1(_appServiceConnection));
                    break;
                case Const.Lights.M2:
                    SetLedColor(M2LED, AppService.RandomLightsModuleLightsStatusIsOnM2(_appServiceConnection));
                    break;
                case Const.Lights.L3:
                    SetLedColor(L3LED, AppService.RandomLightsModuleLightsStatusIsOnL3(_appServiceConnection));
                    break;
                case Const.Lights.L4:
                    SetLedColor(L4LED, AppService.RandomLightsModuleLightsStatusIsOnL4(_appServiceConnection));
                    break;
                case Const.Lights.L5:
                    SetLedColor(L5LED, AppService.RandomLightsModuleLightsStatusIsOnL5(_appServiceConnection));
                    break;
                case Const.Lights.L6:
                    SetLedColor(L6LED, AppService.RandomLightsModuleLightsStatusIsOnL6(_appServiceConnection));
                    break;
            }
        }

        private void SetLedColor(Ellipse led, bool status)
        {
            /* UI updates must be invoked on the UI thread */
            var task = Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () =>
                    {
                        led.Fill = status ? _ghostWhiteBrush : _ledOffBrush;
                        /* Display the value on screen                      */
                    });
        }

        private void TimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (_bTimeChangedByUser) {
                var currentDate = DateTime.Now.ToUniversalTime();

                var newDateTime = new DateTime(currentDate.Year,
                                               currentDate.Month,
                                               currentDate.Day,
                                               e.NewTime.Hours,
                                               e.NewTime.Minutes,
                                               e.NewTime.Seconds);

                DateTimeSettings.SetSystemDateTime(newDateTime);


                // reset the timers only when the time changes
                IoTimerControl.SuspendOperations(true);
                IoTimerControl.Initialize();
                _bTimeChangedByUser = false;
                //If the app is set to auto start the following restarts the app
                //Windows.ApplicationModel.Core.CoreApplication.Exit();
            }
        }

        private void CalendarDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_bDateChangedByUser)
            {

                DateTimeOffset date = args.NewDate.Value;
                var currentDate = DateTime.Now;
                var newDateTime = new DateTime(date.UtcDateTime.Year,
                                               date.UtcDateTime.Month,
                                               date.UtcDateTime.Day,
                                               currentDate.Hour,
                                               currentDate.Minute,
                                               currentDate.Second);

                DateTimeSettings.SetSystemDateTime(newDateTime);
                _bDateChangedByUser = false;
            }
        }

        private void InitializeCalender()
        {
            _bDateChangedByUser = false;
            CalendarDatePickerControl.Date = DateTime.Now;
        }

        private void CalendarDatePickerControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _bDateChangedByUser = true;
        }

        private void HwandaTimePicker_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _bTimeChangedByUser = true;
        }


        private void LoadBackGroundImages(string path)
        {
            ProcessDirectory(path);
        }

        private static void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                AddBackGroundImage(fileName);
            
            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        private static void AddBackGroundImage(string path)
        {
            BackgroundImageList.Add(path);
        }

        private static string GetNextBackGroundImage()
        {
            int r = _rnd.Next(BackgroundImageList.Count);
            return BackgroundImageList[r];
        }

    }
}
