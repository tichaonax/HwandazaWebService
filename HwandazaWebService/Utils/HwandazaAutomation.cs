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

    public sealed class m1
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
    }

    public sealed class m2
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
    }

    public sealed class l3
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
    }

    public sealed class l4
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
    }

    public sealed class l5
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
    }

    public sealed class l6
    {
        public int power { get; set; }
        public string lastUpdate { get; set; }
    }

    public sealed class Lights
    {
        public m1 m1 { get; set; }
        public m2 m2 { get; set; }
        public l3 l3 { get; set; }
        public l4 l4 { get; set; }
        public l5 l5 { get; set; }
        public l6 l6 { get; set; }
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
