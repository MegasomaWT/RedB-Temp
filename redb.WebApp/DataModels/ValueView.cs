using redb.Core.DBModels;

namespace redb.WebApp.DataModels
{
    public class ValueView
    {
        public long _id { get; set; }
        public long _id_structure { get; set; }
        public long _id_object { get; set; }
        public string? _String { get; set; }
        public long? _Long { get; set; }
        public Guid? _Guid { get; set; }
        public double? _Double { get; set; }
        public DateTime? _DateTime { get; set; }
        public bool? _Boolean { get; set; }
        public byte[]? _ByteArray { get; set; }
        public string? _Text { get; set; }
        public required string _name {  get; set; }
        public required string _db_type {  get; set; }
    }
}
