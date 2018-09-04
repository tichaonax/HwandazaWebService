using System;
using System.Threading.Tasks;
using HwandazaAppCommunication.RaspiModules;

namespace HwandazaAppCommunication.Utils
{
    internal class Scheduler
    {
        /*
        Schedules 24 hour jobs an then executes the module.Run() method
        and then reschedules for the next day to run at the same time again

        */

        public static async void InitTimeBasedTimer(TimeSpan timeToRun, IModule module)
        { 
            //Create a recurring schedule to run module method Run()
            var interval = timeToRun.Subtract(DateTime.Now.TimeOfDay);
            var intervalMillis = interval.TotalMilliseconds;
            if (interval.TotalMilliseconds < 0)
            {
                intervalMillis = (new TimeSpan(24, 0, 0)).Add(interval).TotalMilliseconds;
            }
            //Wait for the duration before starting the job
            await Task.Delay(Convert.ToInt32(intervalMillis));
            TimeBasedCallback(timeToRun, module);
        }

        public static async void TimeBasedCallback(TimeSpan timeToRun, IModule module)
        {
            //This method will continously loop executing module.Run() method
            while (true)
            {
                module.Run();
                var interval = timeToRun.Subtract(DateTime.Now.TimeOfDay);
                var intervalMillis = interval.TotalMilliseconds;
                if (intervalMillis < 0)
                {
                    intervalMillis = (new TimeSpan(24, 0, 0)).Add(interval).TotalMilliseconds;
                }

                await Task.Delay(Convert.ToInt32(intervalMillis));
            }
        }
    }
}
