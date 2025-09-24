using redb.Core.DBModels;
using System.Text;
using Newtonsoft.Json;
using redb.Core;

namespace redb.WebApp.DataModels
{
    public class DeleteObjectItemView
    {
        public required string Id { get; set; }
        public string? ParentId { get; set; }

        public DateTime DateCreate { get; set; }

        public DateTime DateModify { get; set; }

        public DateTime? DateBegin { get; set; }

        public DateTime? DateComplete { get; set; }

        public long? KeyValue { get; set; }

        public long? CodeInt { get; set; }

        public string? CodeString { get; set; }

        public Guid? CodeGuid { get; set; }

        public string? Name { get; set; }

        public string? Note { get; set; }

        public byte[]? Hash { get; set; }

        public required long Scheme { get; set; }

        public required long User { get; set; }

        public DateTime DateDelete { get; set; }

        public required List<PropertyItem> Properties { get; set; }

        public static implicit operator DeleteObjectItemView?(_RDeletedObject? rdobj) => rdobj == null ? null : new()
        {
            Id = rdobj.Id.ToString(),
            CodeGuid = rdobj.CodeGuid,
            CodeString = rdobj.CodeString,
            CodeInt = rdobj.CodeInt,
            DateBegin = rdobj.DateBegin,
            DateComplete = rdobj.DateComplete,
            DateCreate = rdobj.DateCreate,
            DateModify = rdobj.DateModify,
            Hash = rdobj.Hash?.ToByteArray(),
            ParentId = rdobj.IdParent.ToString(),
            KeyValue = rdobj.Key,
            Name = rdobj.Name,
            Note = rdobj.Note,
            Scheme = rdobj.IdScheme,
            User = rdobj.IdOwner,
            DateDelete = rdobj.DateDelete,
            Properties = (JsonConvert.DeserializeObject<List<ValueView>>(rdobj.Values ?? "[]") ?? new())
               .Select(o => new PropertyItem
               {
                   Id = o._id.ToString(),
                   Name = o._name,
                   Value = ((Func<string?>)(() => o.GetType()
                        .GetProperty($"_{o._db_type}" ?? throw new NotImplementedException())?
                        .GetValue(o)?.ToString())).Invoke()
               }).ToList()
        };
    }
}
