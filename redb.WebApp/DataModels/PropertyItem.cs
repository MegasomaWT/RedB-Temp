namespace redb.WebApp.DataModels
{
    public class PropertyItem
    {
        public required string Id {  get; set; }
        public required string Name { get; set; }
        public string? Value { get; set; }
    }
}
