using Microsoft.EntityFrameworkCore;
using redb.Core.DBModels;
using redb.Core.Models.Permissions;
using System.Collections.Generic;
using System.Data;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace redb.Core.Postgres;
public partial class RedbContext(DbContextOptions<RedbContext> options) : Core.RedbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<_RDeletedObject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__deleted_objects");

            entity.ToTable("_deleted_objects");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.CodeGuid).HasColumnName("_code_guid");
            entity.Property(e => e.CodeInt).HasColumnName("_code_int");
            entity.Property(e => e.CodeString)
                .HasMaxLength(250)
                .HasColumnName("_code_string");
            entity.Property(e => e.DateBegin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_begin");
            entity.Property(e => e.DateComplete)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_complete");
            entity.Property(e => e.DateCreate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_create");
            entity.Property(e => e.DateDelete)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_delete");
            entity.Property(e => e.DateModify)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_modify");
            entity.Property(e => e.Hash).HasColumnName("_hash");
            entity.Property(e => e.IdOwner).HasColumnName("_id_owner");
            entity.Property(e => e.IdParent).HasColumnName("_id_parent");
            entity.Property(e => e.IdScheme).HasColumnName("_id_scheme");
            entity.Property(e => e.IdWhoChange).HasColumnName("_id_who_change");
            entity.Property(e => e.Key).HasColumnName("_key");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("_note");
            entity.Property(e => e.Bool).HasColumnName("_bool");
            entity.Property(e => e.Values)
                .HasColumnType("text")
                .HasColumnName("_values");
        });

        modelBuilder.Entity<_RDependency>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__dependencies");

            entity.ToTable("_dependencies");

            entity.HasIndex(e => e.IdScheme1, "IX__dependencies__schemes_1").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdScheme2, "IX__dependencies__schemes_2").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.IdScheme1, e.IdScheme2 }, "ix__dependencies").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.IdScheme1).HasColumnName("_id_scheme_1");
            entity.Property(e => e.IdScheme2).HasColumnName("_id_scheme_2");

            entity.HasOne(d => d.Scheme1Navigation).WithMany(p => p.DependencyScheme1Navigations)
                .HasForeignKey(d => d.IdScheme1)
                .HasConstraintName("fk__dependencies__schemes_1");

            entity.HasOne(d => d.Scheme2Navigation).WithMany(p => p.DependencyScheme2Navigations)
                .HasForeignKey(d => d.IdScheme2)
                .HasConstraintName("fk__dependencies__schemes_2");
        });

        modelBuilder.Entity<_RFunction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__functions");

            entity.ToTable("_functions");

            entity.HasIndex(e => e.IdScheme, "IX__functions__schemes").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.IdScheme, e.Name }, "ix__functions_scheme_name").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Body).HasColumnName("_body");
            entity.Property(e => e.IdScheme).HasColumnName("_id_scheme");
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .HasColumnName("_language");
            entity.Property(e => e.Name)
                .HasMaxLength(1000)
                .HasColumnName("_name");

            entity.HasOne(d => d.SchemeNavigation).WithMany(p => p.Functions)
                .HasForeignKey(d => d.IdScheme)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk__functions__schemes");
        });

        modelBuilder.Entity<_RLink>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__links");

            entity.ToTable("_links");

            entity.HasIndex(e => new { e.Id1, e.Id2 }, "ix__links").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Id1).HasColumnName("_id_1");
            entity.Property(e => e.Id2).HasColumnName("_id_2");
        });

        modelBuilder.Entity<_RList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__lists");

            entity.ToTable("_lists");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Alias)
                .HasMaxLength(250)
                .HasColumnName("_alias");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
        });

        modelBuilder.Entity<_RListItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__list_items");

            entity.ToTable("_list_items");

            entity.HasIndex(e => e.IdList, "IX__list_items__id_list").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdObject, "IX__list_items__objects").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.IdList).HasColumnName("_id_list");
            entity.Property(e => e.IdObject).HasColumnName("_id_object");
            entity.Property(e => e.Value)
                .HasMaxLength(250)
                .HasColumnName("_value");

            entity.HasOne(d => d.ListNavigation).WithMany(p => p.ListItems)
                .HasForeignKey(d => d.IdList)
                .HasConstraintName("fk__list_items__id_list");

            entity.HasOne(d => d.ObjectNavigation).WithMany(p => p.ListItems)
                .HasForeignKey(d => d.IdObject)
                .HasConstraintName("fk__list_items__objects");
        });

        modelBuilder.Entity<_RObject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__objects");

            entity.ToTable("_objects");

            entity.HasIndex(e => e.CodeGuid, "IX__objects__code_guid").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CodeInt, "IX__objects__code_int").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CodeString, "IX__objects__code_string").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.DateCreate, "IX__objects__date_create").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.DateModify, "IX__objects__date_modify").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Hash, "IX__objects__hash").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Name, "IX__objects__name").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdParent, "IX__objects__objects").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdScheme, "IX__objects__schemes").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdOwner, "IX__objects__users1").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdWhoChange, "IX__objects__users2").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Bool).HasColumnName("_bool");
            entity.Property(e => e.CodeGuid).HasColumnName("_code_guid");
            entity.Property(e => e.CodeInt).HasColumnName("_code_int");
            entity.Property(e => e.CodeString)
                .HasMaxLength(250)
                .HasColumnName("_code_string");
            entity.Property(e => e.DateBegin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_begin");
            entity.Property(e => e.DateComplete)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_complete");
            entity.Property(e => e.DateCreate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_create");
            entity.Property(e => e.DateModify)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_modify");
            entity.Property(e => e.Hash).HasColumnName("_hash");
            entity.Property(e => e.IdOwner).HasColumnName("_id_owner");
            entity.Property(e => e.IdParent).HasColumnName("_id_parent");
            entity.Property(e => e.IdScheme).HasColumnName("_id_scheme");
            entity.Property(e => e.IdWhoChange).HasColumnName("_id_who_change");
            entity.Property(e => e.Key).HasColumnName("_key");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("_note");

            entity.HasOne(d => d.OwnerNavigation).WithMany(p => p.ObjectOwnerNavigations)
                .HasForeignKey(d => d.IdOwner)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk__objects__users1");

            entity.HasOne(d => d.ParentNavigation).WithMany(p => p.InverseParentNavigation)
                .HasForeignKey(d => d.IdParent)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk__objects__objects");

            entity.HasOne(d => d.SchemeNavigation).WithMany(p => p.Objects)
                .HasForeignKey(d => d.IdScheme)
                .HasConstraintName("fk__objects__schemes");

            entity.HasOne(d => d.WhoChangeNavigation).WithMany(p => p.ObjectWhoChangeNavigations)
                .HasForeignKey(d => d.IdWhoChange)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk__objects__users2");
        });

        modelBuilder.Entity<_RPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__object_permissions");

            entity.ToTable("_permissions");

            entity.HasIndex(e => e.IdRole, "IX__permissions__roles").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdUser, "IX__permissions__users").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.IdRole, e.IdUser, e.IdRef, e.Select, e.Insert, e.Update, e.Delete }, "ix__permissions").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Delete).HasColumnName("_delete");
            entity.Property(e => e.IdRef).HasColumnName("_id_ref");
            entity.Property(e => e.IdRole).HasColumnName("_id_role");
            entity.Property(e => e.IdUser).HasColumnName("_id_user");
            entity.Property(e => e.Insert).HasColumnName("_insert");
            entity.Property(e => e.Select).HasColumnName("_select");
            entity.Property(e => e.Update).HasColumnName("_update");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.Permissions)
                .HasForeignKey(d => d.IdRole)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk__permissions__roles");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.Permissions)
                .HasForeignKey(d => d.IdUser)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk__permissions__users");
        });

        modelBuilder.Entity<_RRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__roles");

            entity.ToTable("_roles");

            entity.HasIndex(e => e.Name, "ix__roles").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
        });

        modelBuilder.Entity<_RScheme>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__schemes");

            entity.ToTable("_schemes");

            entity.HasIndex(e => e.IdParent, "IX__schemes__schemes").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Name, "ix__schemes").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Alias)
                .HasMaxLength(250)
                .HasColumnName("_alias");
            entity.Property(e => e.IdParent).HasColumnName("_id_parent");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
            entity.Property(e => e.NameSpace)
                .HasMaxLength(1000)
                .HasColumnName("_name_space");

            entity.HasOne(d => d.ParentNavigation).WithMany(p => p.InverseParentNavigation)
                .HasForeignKey(d => d.IdParent)
                .HasConstraintName("fk__schemes__schemes");
        });

        modelBuilder.Entity<_RStructure>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__structure");

            entity.ToTable("_structures");

            entity.HasIndex(e => e.IdList, "IX__structures__lists").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdScheme, "IX__structures__schemes").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdParent, "IX__structures__structures").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdType, "IX__structures__types").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.IdScheme, e.Name }, "ix__structures").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.Alias)
                .HasMaxLength(250)
                .HasColumnName("_alias");
            entity.Property(e => e.AllowNotNull).HasColumnName("_allow_not_null");
            entity.Property(e => e.DefaultEditor).HasColumnName("_default_editor");
            entity.Property(e => e.DefaultValue).HasColumnName("_default_value");
            entity.Property(e => e.IdList).HasColumnName("_id_list");
            entity.Property(e => e.IdOverride).HasColumnName("_id_override");
            entity.Property(e => e.IdParent).HasColumnName("_id_parent");
            entity.Property(e => e.IdScheme).HasColumnName("_id_scheme");
            entity.Property(e => e.IdType).HasColumnName("_id_type");
            entity.Property(e => e.IsArray).HasColumnName("_is_array");
            entity.Property(e => e.IsCompress).HasColumnName("_is_compress");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
            entity.Property(e => e.Order).HasColumnName("_order");
            entity.Property(e => e.Readonly).HasColumnName("_readonly");
            entity.Property(e => e.StoreNull).HasColumnName("_store_null");

            entity.HasOne(d => d.ListNavigation).WithMany(p => p.Structures)
                .HasForeignKey(d => d.IdList)
                .HasConstraintName("fk__structures__lists");

            entity.HasOne(d => d.ParentNavigation).WithMany(p => p.InverseParentNavigation)
                .HasForeignKey(d => d.IdParent)
                .HasConstraintName("fk__structures__structures");

            entity.HasOne(d => d.SchemeNavigation).WithMany(p => p.Structures)
                .HasForeignKey(d => d.IdScheme)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk__structures__schemes");

            entity.HasOne(d => d.TypeNavigation).WithMany(p => p.Structures)
                .HasForeignKey(d => d.IdType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk__structures__types");
        });

        modelBuilder.Entity<_RType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__types");

            entity.ToTable("_types");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.DbType)
                .HasMaxLength(250)
                .HasColumnName("_db_type");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
            entity.Property(e => e.Type1)
                .HasMaxLength(250)
                .HasColumnName("_type");
        });

        modelBuilder.Entity<_RUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__users");

            entity.ToTable("_users");

            entity.HasIndex(e => e.Name, "_users__name_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.CodeGuid).HasColumnName("_code_guid");
            entity.Property(e => e.CodeInt).HasColumnName("_code_int");
            entity.Property(e => e.CodeString)
                .HasMaxLength(250)
                .HasColumnName("_code_string");
            entity.Property(e => e.DateDismiss)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_dismiss");
            entity.Property(e => e.DateRegister)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_date_register");
            entity.Property(e => e.Email)
                .HasMaxLength(250)
                .HasColumnName("_email");
            entity.Property(e => e.Enabled)
                .HasDefaultValue(true)
                .HasColumnName("_enabled");
            entity.Property(e => e.Hash).HasColumnName("_hash");
            entity.Property(e => e.Key).HasColumnName("_key");
            entity.Property(e => e.Login)
                .HasMaxLength(250)
                .HasColumnName("_login");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("_name");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("_note");
            entity.Property(e => e.Password).HasColumnName("_password");
            entity.Property(e => e.Phone)
                .HasMaxLength(250)
                .HasColumnName("_phone");

        });

        modelBuilder.Entity<_RUsersRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__users_roles");

            entity.ToTable("_users_roles");

            entity.HasIndex(e => e.IdRole, "IX__users_roles__roles").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdUser, "IX__users_roles__users").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.IdRole, e.IdUser }, "ix__users_roles").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.IdRole).HasColumnName("_id_role");
            entity.Property(e => e.IdUser).HasColumnName("_id_user");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.UsersRoles)
                .HasForeignKey(d => d.IdRole)
                .HasConstraintName("fk__users_roles__roles");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.UsersRoles)
                .HasForeignKey(d => d.IdUser)
                .HasConstraintName("fk__users_roles__users");
        });

        modelBuilder.Entity<_RObjectsJson>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_objects_json");

            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.ObjectJson)
                .HasColumnType("jsonb")
                .HasColumnName("object_json");
        });

        // Маппинг VIEW v_user_permissions
        modelBuilder.Entity<VUserPermission>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_user_permissions");

            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.PermissionType).HasColumnName("permission_type");
            entity.Property(e => e.IdRole).HasColumnName("_id_role");
            entity.Property(e => e.CanSelect).HasColumnName("can_select");
            entity.Property(e => e.CanInsert).HasColumnName("can_insert");
            entity.Property(e => e.CanUpdate).HasColumnName("can_update");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
        });

        modelBuilder.Entity<_RValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk__values");

            entity.ToTable("_values", tb => tb.HasComment("Таблица хранения значений полей объектов. Поддерживает реляционные массивы всех типов (простых и Class полей) через _array_parent_id и _array_index"));

            entity.HasIndex(e => e.Boolean, "IX__values__Boolean").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.DateTime, "IX__values__DateTime").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Double, "IX__values__Double").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Guid, "IX__values__Guid").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Long, "IX__values__Long").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.String, "IX__values__String").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.ArrayIndex, "IX__values__array_index").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.ArrayParentId, "IX__values__array_parent_id").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.ArrayParentId, e.ArrayIndex }, "IX__values__array_parent_index").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdObject, "IX__values__objects").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.IdStructure, "IX__values__structures").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.IdStructure, e.IdObject }, "UIX__values__structure_object")
                .IsUnique()
                .HasFilter("(_array_index IS NULL)");

            entity.HasIndex(e => new { e.IdStructure, e.IdObject, e.ArrayIndex }, "UIX__values__structure_object_array_index")
                .IsUnique()
                .HasFilter("(_array_index IS NOT NULL)");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("_id");
            entity.Property(e => e.ArrayIndex)
                .HasComment("Позиция элемента в массиве (0,1,2...). NULL для обычных (не-массивных) полей. Используется для всех типов массивов: простых типов и Class полей")
                .HasColumnName("_array_index");
            entity.Property(e => e.ArrayParentId)
                .HasComment("ID родительского элемента для элементов массива. NULL для обычных (не-массивных) полей и корневых элементов массива")
                .HasColumnName("_array_parent_id");
            entity.Property(e => e.Boolean).HasColumnName("_boolean");
            entity.Property(e => e.ByteArray).HasColumnName("_bytearray");
            entity.Property(e => e.DateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("_datetime");
            entity.Property(e => e.Double).HasColumnName("_double");
            entity.Property(e => e.Guid).HasColumnName("_guid");
            entity.Property(e => e.IdObject).HasColumnName("_id_object");
            entity.Property(e => e.IdStructure).HasColumnName("_id_structure");
            entity.Property(e => e.Long).HasColumnName("_long");
            entity.Property(e => e.String).HasColumnName("_string");

            entity.HasOne(d => d.ArrayParent).WithMany(p => p.InverseArrayParent)
                .HasForeignKey(d => d.ArrayParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk__values__array_parent");

            entity.HasOne(d => d.ObjectNavigation).WithMany(p => p.Values)
                .HasForeignKey(d => d.IdObject)
                .HasConstraintName("fk__values__objects");

            entity.HasOne(d => d.StructureNavigation).WithMany(p => p.Values)
                .HasForeignKey(d => d.IdStructure)
                .HasConstraintName("fk__values__structures");
        });

        modelBuilder.HasSequence("global_identity").HasMin(1000000L);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
