namespace redb.WebApp.DataModels
{
    public class ObjectView
    {
        public required string Id { get; set; }
        public string? Name { get; set; }
        public string? ParentId { get; set; }
    }
}
