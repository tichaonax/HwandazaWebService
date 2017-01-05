using SQLite.Net.Attributes;

namespace HwandazaAppCommunication.Utils
{
    public sealed class HwandazaCommandStatus
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string RowGuidId { get; set; }
    }
}