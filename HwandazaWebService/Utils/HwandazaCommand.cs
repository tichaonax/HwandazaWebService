﻿using System.Collections.Generic;

namespace HwandazaWebService.Utils
{
    public sealed class HwandazaCommand
    {
        public string Command { get; set; }
        public string Module { get; set; }
        public IList<string> Lights { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }
}
