namespace HwandazaAppCommunication.Utils
{    
    public sealed class WaterPump
    {
        public int power { get; set; }
        public double adcFloatValue { get; set; }
    }

    public sealed class Irrigator
    {
        public int power { get; set; }
        public double adcFloatValue { get; set; }
    }

    public sealed class FishPond
    {
        public int power { get; set; }
        public double adcFloatValue { get; set; }
    }

    public sealed class Modules
    {
        public WaterPump WaterPump { get; set; }
        public Irrigator Irrigator { get; set; }
        public FishPond FishPond { get; set; }
    }

    public sealed class Lights
    {
        public int M1 { get; set; }
        public int M2 { get; set; }
        public int L3 { get; set; }
        public int L4 { get; set; }
        public int L5 { get; set; }
        public int L6 { get; set; }
    }

    public sealed class Status
    {
        public Modules modules { get; set; }
        public Lights lights { get; set; }
    }

    public sealed class HwandazaAutomation
    {
        public int statusId { get; set; }
        public string statusDate { get; set; }
        public Status status { get; set; }
    }

}
