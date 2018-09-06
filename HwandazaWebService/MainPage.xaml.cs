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
using HwandazaWebService.Utils;
using Newtonsoft.Json;
using Path = System.IO.Path;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using HwandazaWebService.RaspiModules;
using Windows.ApplicationModel.AppService;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HwandazaWebService
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly MainWaterPump _mainWaterPump;
        private readonly FishPondPump _fishPondPump;
        private readonly LawnIrrigator _lawnIrrigator;
        private readonly RandomLights _randomLights;
        private readonly SystemsHeartBeat _systemsHeartBeat;

        //private readonly GpioProcessor GpioProcessor;
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

        private static readonly List<string> BackgroundImageList = new List<string>();
        static Random _rnd = new Random();

        public MainPage()
        {
            this.InitializeComponent();
            LoadBackGroundImages(Const.BackgroundImageFolder);
            InitializeCalender();
            _currentSystemHeartBeatBrush = _ledOffBrush;

            var modules = IoTimerControl.getIoTimerControls();
            _mainWaterPump = (MainWaterPump)modules.First(r => r.Module() is MainWaterPump);
            _lawnIrrigator = (LawnIrrigator)modules.First(r => r.Module() is LawnIrrigator);
            _fishPondPump = (FishPondPump)modules.First(r => r.Module() is FishPondPump);
            _randomLights = (RandomLights)modules.First(r => r.Module() is RandomLights);
            _systemsHeartBeat = (SystemsHeartBeat)modules.First(r => r.Module() is SystemsHeartBeat);
            GpioProcessor.Initialize(_mainWaterPump, _fishPondPump, _lawnIrrigator, _randomLights, _systemsHeartBeat);
            _systemsHeartBeat = (SystemsHeartBeat)modules.First(r => r.Module() is SystemsHeartBeat);

            //setup timer to update the UI
            _poolTimerUiUpdate = ThreadPoolTimer.CreatePeriodicTimer(HwandazaUiUpdate, TimeSpan.FromMilliseconds(Const.HalfSecondDelayMs));

            _poolTimerHeartBeat = ThreadPoolTimer.CreatePeriodicTimer(SystemHeartBeatControl, TimeSpan.FromMilliseconds(Const.OneSecondDelayMs));

            _imageTimerHeartBeat = ThreadPoolTimer.CreatePeriodicTimer(ImageHeartBeatControlAsync, period: TimeSpan.FromMilliseconds(Const.FiveSecondsDelayMs));

            ApplicationData.Current.DataChanged += async (d, a) => await HandleDataChangedEvent(d, a);
        }

        private void SystemHeartBeatControl(ThreadPoolTimer timer)
        {
            if (_systemsHeartBeat.IsRunning())
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

                           HwandaGrid.Background = new ImageBrush() { ImageSource = image, Stretch = Stretch.UniformToFill, Opacity = 0.75 };
                       }
                   });
        }

        private async Task HandleDataChangedEvent(ApplicationData data, object args)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("HwandazaCommand"))
                {
                    return;
                }

                var hwandazaPacket = localSettings.Values["HwandazaCommand"] as string;
                var command = JsonConvert.DeserializeObject<HwandazaCommand>(hwandazaPacket);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    GpioProcessor.ProcessHwandazaCommand(command);
                });
            }
            catch (Exception)
            {
                // Do nothing
            }
        }

        private void FishPondPump_OnClick(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonFishPondPump();
        }

        private void LawnIrrigator_OnClick(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonLawnIrrigator();
        }

        private void WaterPunp_OnClick(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonWaterPump();
        }

        public void ButtonLights(List<string> lights)
        {
            _randomLights.ToggleLights(lights);
        }

        private void HwandazaUiUpdate(ThreadPoolTimer timer)
        {
            SystemStatus();
        }

        private void ButtonBase_OnClick_M1(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonLights(new List<string>() { Const.Lights.M1 });
        }

        private void ButtonBase_OnClick_M2(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonLights(new List<string>() { Const.Lights.M2 });
        }

        private void ButtonBase_OnClick_L3(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonLights(new List<string>() { Const.Lights.L3 });
        }

        private void ButtonBase_OnClick_L4(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonLights(new List<string>() { Const.Lights.L4 });
        }

        private void ButtonBase_OnClick_L5(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonLights(new List<string>() { Const.Lights.L5 });
        }

        private void ButtonBase_OnClick_L6(object sender, RoutedEventArgs e)
        {
            GpioProcessor.ButtonLights(new List<string>() { Const.Lights.L6 });
        }

        private void SystemStatus()
        {
            //blink the status lights for each operation
            _currentPumpStatusBrush = _currentPumpStatusBrush == _ledOffBrush ? _redBrush : _ledOffBrush;
            _currentPondStatusBrush = _currentPondStatusBrush == _ledOffBrush ? _goldBrush : _ledOffBrush;
            _currentLawnStatusBrush = _currentLawnStatusBrush == _ledOffBrush ? _greenBrush : _ledOffBrush;

            if (_fishPondPump.ModuleStatus().IsRunning)
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

            if (_lawnIrrigator.ModuleStatus().IsRunning)
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
            if (_mainWaterPump.ModuleStatus().IsRunning)
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
                        WaterPumpADC.Text = string.Format("ADC: {0:0.00}V", _mainWaterPump.ModuleStatus().AdcVoltage);
                        PondPumpADC.Text = string.Format("ADC: {0:0.00}V", _fishPondPump.ModuleStatus().AdcVoltage);
                        LawnIrrigatorADC.Text = string.Format("ADC: {0:0.00}V", _lawnIrrigator.ModuleStatus().AdcVoltage);
                        /* Display the value on screen                      */
                    });
        }
        private void SetLightStatus(string light)
        {
            switch (light)
            {
                case Const.Lights.M1:
                    SetLedColor(M1LED, _randomLights.ModuleStatus().LightsStatus.IsOnM1);
                    break;
                case Const.Lights.M2:
                    SetLedColor(M2LED, _randomLights.ModuleStatus().LightsStatus.IsOnM2);
                    break;
                case Const.Lights.L3:
                    SetLedColor(L3LED, _randomLights.ModuleStatus().LightsStatus.IsOnL3);
                    break;
                case Const.Lights.L4:
                    SetLedColor(L4LED, _randomLights.ModuleStatus().LightsStatus.IsOnL4);
                    break;
                case Const.Lights.L5:
                    SetLedColor(L5LED, _randomLights.ModuleStatus().LightsStatus.IsOnL5);
                    break;
                case Const.Lights.L6:
                    SetLedColor(L6LED, _randomLights.ModuleStatus().LightsStatus.IsOnL6);
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
            if (_bTimeChangedByUser)
            {
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

        private HwandazaAutomation InitStatus()
        {
            return new HwandazaAutomation()
            {
                statusDate = DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"),
                status = new Status()
                {
                    modules = new Modules()
                    {
                        FishPond = new FishPond()
                        {
                            adcFloatValue = 0.0f,
                            power = 0,
                        },
                        WaterPump = new WaterPump()
                        {
                            adcFloatValue = 0.0f,
                            power = 1,
                        },
                        Irrigator = new Irrigator()
                        {
                            adcFloatValue = 0.0f,
                            power = 0,
                        }
                    },
                    lights = new Lights()
                    {
                        L3 = 0,
                        L4 = 0,
                        L5 = 0,
                        L6 = 1,
                        M1 = 0,
                        M2 = 0,
                    }
                }
            };
        }

        //private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        //{
        //    //Clean up and get ready to exit
        //    if (_backgroundTaskDeferral != null)
        //    {
        //        _backgroundTaskDeferral.Complete();
        //    }
        //}

        //public void Run(IBackgroundTaskInstance taskInstance)
        //{
        //    // Associate a cancellation handler with the background task. 
        //    taskInstance.Canceled += OnCanceled;
        //    _backgroundTaskDeferral = taskInstance.GetDeferral();

        //    var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
        //    if (appService != null &&
        //        appService.Name == "HwandazaWebService")
        //    {
        //        _appServiceConnection = appService.AppServiceConnection;
        //        _appServiceConnection.RequestReceived += OnRequestReceived;
        //    }
        //}

        //private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        //{
        //    Debug.WriteLine("Message Received:");
        //    var messageDefferal = args.GetDeferral();
        //    var message = args.Request.Message;
        //    HwandazaCommand command;
        //    try
        //    {
        //        var hwandazaCommand = message["HwandazaCommand"] as string;

        //        command = JsonConvert.DeserializeObject<HwandazaCommand>(hwandazaCommand);

        //        //we have the comman now process it
        //        var response = _gpioProcessor.ProcessHwandazaCommand(command);
                
        //        var status = JsonConvert.SerializeObject(response);

        //        var returnMessage = new ValueSet
        //                            {
        //                                {"Result", status},
        //                                {"Status", "OK"}
        //                            };

        //        await args.Request.SendResponseAsync(returnMessage);
        //        Debug.WriteLine("HwandazaAppService Command execution completed: " + status);
        //    }
        //    catch (Exception ex)
        //    {
        //        var error = ex.Message;
        //        Debug.WriteLine("HwandazaAppService OnRequestReceived Eror:" + error);
        //    }
        //    messageDefferal.Complete();
        //}
    }
}
