using System.Collections.Generic;
using HwandazaAppCommunication.RaspiModules;

namespace HwandazaAppCommunication.Utils
{
    public static class IoTimerControl
    {
        private const float ReferenceVoltage = 3.30F;
        private static IList<IModule> IoTimerControls;
        private static bool GpioInitialized;

        static IoTimerControl()
        {
            Initialize();
        }

        public static IList<IModule> getIoTimerControls()
        {
            return IoTimerControls;
        } 

        public static bool getGpioInitialized()
        {
            return GpioInitialized;
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

            foreach (IModule module in modules)
            {
                IoTimerControls.Add(module);
            }

            //IoTimerControls.AddRange(modules);
            //call the GPIO initializers for each module
            foreach (var iotimer in IoTimerControls)
            {
                iotimer.InitializeGPIO();
            }
            GpioInitialized = true;
        }

        public static void SuspendOperations(bool intitialize)
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