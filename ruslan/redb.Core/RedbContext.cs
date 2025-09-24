using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using redb.Core.DBModels;

namespace redb.Core;

public abstract class RedbContext : DbContext
{
    public RedbContext()
    {
    }

    public RedbContext(DbContextOptions options)
        : base(options)
    {
    }

    public abstract long GetNextKey();

    public abstract List<long> GetKeysBatch(int count);

    public abstract Task<long> GetNextKeyAsync();

    public abstract Task<List<long>> GetKeysBatchAsync(int count);

    public virtual DbSet<DBModels._RDependency> Dependencies { get; set; }

    public virtual DbSet<DBModels._RDeletedObject> DeletedObjects { get; set; }

    public virtual DbSet<DBModels._RFunction> Functions { get; set; }

    public virtual DbSet<DBModels._RLink> Links { get; set; }

    public virtual DbSet<DBModels._RList> Lists { get; set; }

    public virtual DbSet<DBModels._RListItem> ListItems { get; set; }

    public virtual DbSet<DBModels._RObject> Objects { get; set; }

    public virtual DbSet<DBModels._RPermission> Permissions { get; set; }

    public virtual DbSet<DBModels._RRole> Roles { get; set; }

    public virtual DbSet<DBModels._RScheme> Schemes { get; set; }

    public virtual DbSet<DBModels._RStructure> Structures { get; set; }

    public virtual DbSet<DBModels._RType> Types { get; set; }

    public virtual DbSet<DBModels._RUser> Users { get; set; }

    public virtual DbSet<DBModels._RUsersRole> UsersRoles { get; set; }

    public virtual DbSet<DBModels._RValue> Values { get; set; }
}
