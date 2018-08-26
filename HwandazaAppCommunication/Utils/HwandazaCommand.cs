using System.Collections.Generic;

namespace HwandazaAppCommunication.Utils
{
    public sealed class HwandazaCommand
    {
        public string Command { get; set; }
        public string Module { get; set; }
        public IList<string> Lights { get; set; }
        public string SqlRowGuidId { get; set; }
    }
}
