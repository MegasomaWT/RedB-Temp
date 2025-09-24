namespace redb.WebApp.DataModels
{
    public class DeletedObjectView
    {
        public required string Id { get; set; }
        public string? Name { get; set; }
        public string? ParentId { get; set; }
    }
}
