using System;

namespace HwandazaWebService.Utils
{
    public sealed class ModuleStatus
    {
        public string StatusText { get; set; }
        public float AdcVoltage { get; set; }
        public bool IsRunning { get; set; }
        public DateTime LastUpdate { get; set; }
        public LightsStatus LightsStatus { get; set; }
    }

    public sealed class M1
    {
        public bool IsOn { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public sealed class M2
    {
        public bool IsOn { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public sealed class L3
    {
        public bool IsOn { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public sealed class L4
    {
        public bool IsOn { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public sealed class L5
    {
        public bool IsOn { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public sealed class L6
    {
        public bool IsOn { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public sealed class LightsStatus
    {   
        public M1 M1 { get; set; }
        public M2 M2 { get; set; }
        public L3 L3 { get; set; }
        public L4 L4 { get; set; }
        public L5 L5 { get; set; }
        public L6 L6 { get; set; }
    }
}
