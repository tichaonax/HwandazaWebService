using SQLite.Net.Attributes;

namespace HwandazaWebService.Utils
{
    public class HwandazaCommandStatus
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string RowGuidId { get; set; }
    }
}