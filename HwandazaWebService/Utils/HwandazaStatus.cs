using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;

namespace HwandazaWebService.Utils
{
    public class HwandazaStatus
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string RowGuidId { get; set; }
        public int MainWaterPump { get; set; }
        public int LawnIrrigator { get; set; }
        public int FishPondPump { get; set; }
        public int M1 { get; set; }
        public int M2 { get; set; }
        public int L3 { get; set; }
        public int L4 { get; set; }
        public int L5 { get; set; }
        public int L6 { get; set; }
        public double MainWaterPumpAdcFloatValue { get; set; }
        public float LawnIrrigatorAdcFloatValue { get; set; }
        public float FishPondPumpAdcFloatValue { get; set; }
    }
}
