namespace HwandazaAppCommunication.Utils
{
    public sealed class ModuleStatus
    {
        public string StatusText { get; set; }
        public float AdcVoltage { get; set; }
        public bool IsRunning { get; set; }
        public LightsStatus LightsStatus { get; set; }
    }

    public sealed class LightsStatus
    {
        public bool IsOnM1 { get; set; }
        public bool IsOnM2 { get; set; }
        public bool IsOnL3 { get; set; }
        public bool IsOnL4 { get; set; }
        public bool IsOnL5 { get; set; }
        public bool IsOnL6 { get; set; }
    }
}
