namespace HwandazaWebService.Utils
{    
    public sealed class WaterPump
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
        public double adcFloatValue { get; set; }
    }

    public sealed class Irrigator
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
        public double adcFloatValue { get; set; }
    }

    public sealed class FishPond
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
        public double adcFloatValue { get; set; }
    }

    public sealed class Modules
    {
        public WaterPump waterPump { get; set; }
        public Irrigator irrigator { get; set; }
        public FishPond fishPond { get; set; }
    }

    public sealed class Lights
    {
        public int m1 { get; set; }
        public int m2 { get; set; }
        public int l3 { get; set; }
        public int l4 { get; set; }
        public int l5 { get; set; }
        public int l6 { get; set; }
    }

    public sealed class Status
    {
        public Modules modules { get; set; }
        public Lights lights { get; set; }
    }

    public sealed class HwandazaAutomation
    {
        public string systemUpTime { get; set; }
        public string statusDate { get; set; }
        public Status status { get; set; }
        public bool isRunning { get; set; }
    }

    public sealed class AutomationError
    {
        public string error { get; set; }
        public HwandazaCommand request { get; set; }
    }
}
