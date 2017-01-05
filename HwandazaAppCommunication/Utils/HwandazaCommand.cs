using System.Collections.Generic;

namespace HwandazaAppCommunication.Utils
{
    internal class HwandazaCommand
    {
        public string Method { get; set; }
        public string Command { get; set; }
        public string Module { get; set; }
        public List<string> Lights { get; set; }
        public string SqlRowGuidId { get; set; }
    }
}
