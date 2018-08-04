using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HwandazaWebService.Modules;

namespace HwandazaWebService.Utils
{
    internal static class IoTimerControl
    {
        private const float ReferenceVoltage = 3.30F;
        public static List<IModule> IoTimerControls;
        public static bool GpioInitialized;

        private static string _path;
        private static readonly SQLite.Net.SQLiteConnection _sqLiteConnection;

        static IoTimerControl()
        {
            // sql - lite database
            _path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "db.sqlite");
            _sqLiteConnection = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), _path);
            _sqLiteConnection.CreateTable<HwandazaStatus>();
            _sqLiteConnection.CreateTable<HwandazaCommandStatus>();
            _sqLiteConnection.DeleteAll<HwandazaStatus>();
            _sqLiteConnection.DeleteAll<HwandazaCommandStatus>();

            Initialize();
        }

        public static SQLite.Net.SQLiteConnection SqLiteConnection()
        {
            return _sqLiteConnection;
        }

        private static void InitialiseSqlLite(IModule mainWaterPump, IModule fishPondPump, IModule lawnIrrigator, IModule randomLights)
        {
            var status = _sqLiteConnection.Table<HwandazaStatus>().Where(x => x.RowGuidId == Const.StatusRowGuidId).FirstOrDefault();

            var insert = (status == null);

            if (insert)
            {
                status = new HwandazaStatus {RowGuidId = Const.StatusRowGuidId };
            }

            var waterPump = mainWaterPump.ModuleStatus();
            status.MainWaterPump = waterPump.IsRunning ? 1 : 0;
            status.MainWaterPumpAdcFloatValue = waterPump.AdcVoltage;

            var fishPond = fishPondPump.ModuleStatus();
            status.FishPondPump = fishPond.IsRunning ? 1 : 0;
            status.FishPondPumpAdcFloatValue = fishPond.AdcVoltage;

            var lawn = lawnIrrigator.ModuleStatus();
            status.LawnIrrigator = lawn.IsRunning ? 1 : 0;
            status.LawnIrrigatorAdcFloatValue = lawn.AdcVoltage;

            var lights = randomLights.ModuleStatus().LightsStatus;

            status.M1 = lights.IsOnM1 ? 1 : 0;
            status.M2 = lights.IsOnM2 ? 1 : 0;
            status.L3 = lights.IsOnL3 ? 1 : 0;
            status.L3 = lights.IsOnL4 ? 1 : 0;
            status.L5 = lights.IsOnL5 ? 1 : 0;
            status.L6 = lights.IsOnL6 ? 1 : 0;

            if (insert)
            {
                _sqLiteConnection.Insert(status);
                Debug.WriteLine("IoTimerControls: New Record");
            }
            else
            {
                _sqLiteConnection.Update(status);
                Debug.WriteLine("IoTimerControls: Update Record");
            }
        }

        public static void Initialize()
        {
            if (GpioInitialized) return;
            
            //Inittialize the MCP ADC contoller before passing to modules that need it
            var mcpAdcController = new Mcp3008AdcCtrl(ReferenceVoltage);
           var mainWaterPump = new MainWaterPump(mcpAdcController);
            var fishPondPump = new FishPondPump(mcpAdcController);
            var lawnIrrigator = new LawnIrrigator(mcpAdcController);
            var randomLights = new RandomLights();
            mcpAdcController.InitSPI();

            var modules = new List<IModule>()
                          {
                              mainWaterPump,
                              fishPondPump,
                              lawnIrrigator,
                              randomLights
                          };

            //Add  SystemsHeartBeat module this last as it needs a list of all the modules
            IoTimerControls = new List<IModule> {new SystemsHeartBeat(modules)};

            IoTimerControls.AddRange(modules);
            //call the GPIO initializers for each module
            foreach (var iotimer in IoTimerControls)
            {
                iotimer.InitializeGPIO();
            }
            GpioInitialized = true;
            InitialiseSqlLite(mainWaterPump,fishPondPump,lawnIrrigator, randomLights);
        }

        public static void SuspendOperations(bool intitialize = false)
        {
            foreach (var iotimer in IoTimerControls)
            {
                iotimer.Stop();
            }

            if (intitialize)
            {
                Windows.ApplicationModel.Core.CoreApplication.Exit();
            }

            GpioInitialized = intitialize;
        }
    }
}