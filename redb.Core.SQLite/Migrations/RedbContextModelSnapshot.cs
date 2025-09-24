using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace redb.Core.SQLite.Migrations
{
    [DbContext(typeof(RedbContext))]
    partial class RedbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            RedbBuildModel(modelBuilder);
        }

        public static void RedbBuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("redb.Core.Models._RDeletedObject", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<Guid?>("CodeGuid")
                        .HasColumnType("uuid")
                        .HasColumnName("_code_guid");

                    b.Property<long?>("CodeInt")
                        .HasColumnType("bigint")
                        .HasColumnName("_code_int");

                    b.Property<string>("CodeString")
                        .HasColumnType("varchar (250)")
                        .HasColumnName("_code_string");

                    b.Property<DateTime?>("DateBegin")
                        .HasColumnType("datetime")
                        .HasColumnName("_date_begin");

                    b.Property<DateTime?>("DateComplete")
                        .HasColumnType("datetime")
                        .HasColumnName("_date_complete");

                    b.Property<DateTime>("DateCreate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("_date_create")
                        .HasDefaultValueSql("julianday(CURRENT_TIMESTAMP)");

                    b.Property<DateTime>("DateDelete")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("_date_delete")
                        .HasDefaultValueSql("julianday(CURRENT_TIMESTAMP)");

                    b.Property<DateTime>("DateModify")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("_date_modify")
                        .HasDefaultValueSql("julianday(CURRENT_TIMESTAMP)");

                    b.Property<byte[]>("Hash")
                        .HasColumnType("bytea")
                        .HasColumnName("_hash");

                    b.Property<long>("IdOwner")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_owner");

                    b.Property<long?>("IdParent")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_parent");

                    b.Property<long>("IdScheme")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_scheme");

                    b.Property<long>("IdWhoChange")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_who_change");

                    b.Property<long?>("Key")
                        .HasColumnType("bigint")
                        .HasColumnName("_key");

                    b.Property<string>("Name")
                        .HasColumnType("varchar (250)")
                        .HasColumnName("_name");

                    b.Property<string>("Note")
                        .HasColumnType("varchar (1000)")
                        .HasColumnName("_note");

                    b.Property<byte[]>("Values")
                        .HasColumnType("bytea")
                        .HasColumnName("_values");

                    b.HasKey("Id");

                    b.ToTable("_deleted_objects", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RDependency", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<long?>("IdScheme1")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_scheme_1");

                    b.Property<long>("IdScheme2")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_scheme_2");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "IdScheme1", "IdScheme2" }, "IX__dependencies__id_scheme_1__id_scheme_2")
                        .IsUnique();

                    b.HasIndex(new[] { "IdScheme1" }, "IX__dependencies__schemes_1");

                    b.HasIndex(new[] { "IdScheme2" }, "IX__dependencies__schemes_2");

                    b.ToTable("_dependencies", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RFunction", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("_body");

                    b.Property<long>("IdScheme")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_scheme");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasColumnType("varchar(50)")
                        .HasColumnName("_language");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(1000)")
                        .HasColumnName("_name");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "IdScheme", "Name" }, "IX__functions__id_scheme__name")
                        .IsUnique();

                    b.HasIndex(new[] { "IdScheme" }, "IX__functions__schemes");

                    b.ToTable("_functions", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models.GlobalIdentity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("_id");

                    b.HasKey("Id");

                    b.ToTable("_global_identity", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RLink", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<long>("Id1")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_1");

                    b.Property<long>("Id2")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_2");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "Id1", "Id2" }, "IX__links__id_1__id_2")
                        .IsUnique();

                    b.ToTable("_links", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RList", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<string>("Alias")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_alias");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_name");

                    b.HasKey("Id");

                    b.ToTable("_lists", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RListItem", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<long>("IdList")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_list");

                    b.Property<long?>("IdObject")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_object");

                    b.Property<string>("_RValue")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_value");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "IdList" }, "IX__list_items__id_list");

                    b.HasIndex(new[] { "IdObject" }, "IX__list_items__objects");

                    b.ToTable("_list_items", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RObject", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<Guid?>("CodeGuid")
                        .HasColumnType("uuid")
                        .HasColumnName("_code_guid");

                    b.Property<long?>("CodeInt")
                        .HasColumnType("bigint")
                        .HasColumnName("_code_int");

                    b.Property<string>("CodeString")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_code_string");

                    b.Property<DateTime?>("DateBegin")
                        .HasColumnType("datetime")
                        .HasColumnName("_date_begin");

                    b.Property<DateTime?>("DateComplete")
                        .HasColumnType("datetime")
                        .HasColumnName("_date_complete");

                    b.Property<DateTime>("DateCreate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("_date_create")
                        .HasDefaultValueSql("julianday(CURRENT_TIMESTAMP)");

                    b.Property<DateTime>("DateModify")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("_date_modify")
                        .HasDefaultValueSql("julianday(CURRENT_TIMESTAMP)");

                    b.Property<byte[]>("Hash")
                        .HasColumnType("BLOB")
                        .HasColumnName("_hash");

                    b.Property<long>("IdOwner")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_owner");

                    b.Property<long?>("IdParent")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_parent");

                    b.Property<long>("IdScheme")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_scheme");

                    b.Property<long>("IdWhoChange")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_who_change");

                    b.Property<long?>("Key")
                        .HasColumnType("bigint")
                        .HasColumnName("_key");

                    b.Property<string>("Name")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_name");

                    b.Property<string>("Note")
                        .HasColumnType("varchar(1000)")
                        .HasColumnName("_note");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "CodeGuid" }, "IX__objects__code_guid");

                    b.HasIndex(new[] { "CodeInt" }, "IX__objects__code_int");

                    b.HasIndex(new[] { "CodeString" }, "IX__objects__code_string");

                    b.HasIndex(new[] { "DateCreate" }, "IX__objects__date_create");

                    b.HasIndex(new[] { "DateModify" }, "IX__objects__date_modify");

                    b.HasIndex(new[] { "Hash" }, "IX__objects__hash");

                    b.HasIndex(new[] { "Name" }, "IX__objects__name");

                    b.HasIndex(new[] { "IdParent" }, "IX__objects__objects");

                    b.HasIndex(new[] { "IdScheme" }, "IX__objects__schemes");

                    b.HasIndex(new[] { "IdOwner" }, "IX__objects__users1");

                    b.HasIndex(new[] { "IdWhoChange" }, "IX__objects__users2");

                    b.ToTable("_objects", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RPermission", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<bool?>("Delete")
                        .HasColumnType("boolean")
                        .HasColumnName("_delete");

                    b.Property<long>("IdRef")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_ref");

                    b.Property<long?>("IdRole")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_role");

                    b.Property<long?>("IdUser")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_user");

                    b.Property<bool?>("Insert")
                        .HasColumnType("boolean")
                        .HasColumnName("_insert");

                    b.Property<bool?>("Select")
                        .HasColumnType("boolean")
                        .HasColumnName("_select");

                    b.Property<bool?>("Update")
                        .HasColumnType("boolean")
                        .HasColumnName("_update");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "IdRole", "IdUser", "IdRef", "Select", "Insert", "Update", "Delete" }, "IX__permissions__id_role__id_user__id_ref__select__insert__update__delete")
                        .IsUnique();

                    b.HasIndex(new[] { "IdRole" }, "IX__permissions__roles");

                    b.HasIndex(new[] { "IdUser" }, "IX__permissions__users");

                    b.ToTable("_permissions", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RRole", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_name");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "Name" }, "IX__roles__name")
                        .IsUnique();

                    b.ToTable("_roles", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RScheme", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<string>("Alias")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_alias");

                    b.Property<long?>("IdParent")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_parent");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_name");

                    b.Property<string>("NameSpace")
                        .HasColumnType("varchar(1000)")
                        .HasColumnName("_name_space");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "Name" }, "IX__schemes__name")
                        .IsUnique();

                    b.HasIndex(new[] { "IdParent" }, "IX__schemes__schemes");

                    b.ToTable("_schemes", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RStructure", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<string>("Alias")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_alias");

                    b.Property<bool?>("AllowNotNull")
                        .HasColumnType("boolean")
                        .HasColumnName("_allow_not_null");

                    b.Property<string>("DefaultEditor")
                        .HasColumnType("TEXT")
                        .HasColumnName("_default_editor");

                    b.Property<byte[]>("DefaultValue")
                        .HasColumnType("bytea")
                        .HasColumnName("_default_value");

                    b.Property<long?>("IdList")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_list");

                    b.Property<long?>("IdOverride")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_override");

                    b.Property<long?>("IdParent")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_parent");

                    b.Property<long>("IdScheme")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_scheme");

                    b.Property<long>("IdType")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_type");

                    b.Property<bool?>("IsArray")
                        .HasColumnType("boolean")
                        .HasColumnName("_is_array");

                    b.Property<bool?>("IsCompress")
                        .HasColumnType("boolean")
                        .HasColumnName("_is_compress");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_name");

                    b.Property<long?>("Order")
                        .HasColumnType("bigint")
                        .HasColumnName("_order");

                    b.Property<bool?>("Readonly")
                        .HasColumnType("boolean")
                        .HasColumnName("_readonly");

                    b.Property<bool?>("StoreNull")
                        .HasColumnType("boolean")
                        .HasColumnName("_store_null");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "IdScheme", "Name" }, "IX__structures__id_scheme__name")
                        .IsUnique();

                    b.HasIndex(new[] { "IdList" }, "IX__structures__lists");

                    b.HasIndex(new[] { "IdScheme" }, "IX__structures__schemes");

                    b.HasIndex(new[] { "IdParent" }, "IX__structures__structures");

                    b.HasIndex(new[] { "IdType" }, "IX__structures__types");

                    b.ToTable("_structures", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RType", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<string>("DbType")
                        .IsRequired()
                        .HasColumnType("varchar (250)")
                        .HasColumnName("_db_type");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar (250)")
                        .HasColumnName("_name");

                    b.Property<string>("Type1")
                        .IsRequired()
                        .HasColumnType("varchar (250)")
                        .HasColumnName("_type");

                    b.HasKey("Id");

                    b.ToTable("_types", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RUser", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<DateTime?>("DateDismiss")
                        .HasColumnType("datetime")
                        .HasColumnName("_date_dismiss");

                    b.Property<DateTime>("DateRegister")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("_date_register")
                        .HasDefaultValueSql("julianday(CURRENT_TIMESTAMP)");

                    b.Property<string>("Email")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_email");

                    b.Property<bool>("Enabled")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasColumnName("_enabled")
                        .HasDefaultValueSql("true");

                    b.Property<string>("Login")
                        .IsRequired()
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_login");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_name");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("_password");

                    b.Property<string>("Phone")
                        .HasColumnType("varchar(250)")
                        .HasColumnName("_phone");

                    b.HasKey("Id");

                    b.ToTable("_users", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RUsersRole", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<long>("IdRole")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_role");

                    b.Property<long>("IdUser")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_user");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "IdRole", "IdUser" }, "IX__users_roles__id_role__id_user")
                        .IsUnique();

                    b.HasIndex(new[] { "IdRole" }, "IX__users_roles__roles");

                    b.HasIndex(new[] { "IdUser" }, "IX__users_roles__users");

                    b.ToTable("_users_roles", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RValue", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint")
                        .HasColumnName("_id");

                    b.Property<bool?>("Boolean")
                        .HasColumnType("boolean")
                        .HasColumnName("_Boolean");

                    b.Property<byte[]>("ByteArray")
                        .HasColumnType("bytea")
                        .HasColumnName("_ByteArray");

                    b.Property<DateTime?>("DateTime")
                        .HasColumnType("datetime")
                        .HasColumnName("_DateTime");

                    b.Property<double?>("Double")
                        .HasColumnType("float")
                        .HasColumnName("_Double");

                    b.Property<Guid?>("Guid")
                        .HasColumnType("uuid")
                        .HasColumnName("_Guid");

                    b.Property<long>("IdObject")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_object");

                    b.Property<long>("IdStructure")
                        .HasColumnType("bigint")
                        .HasColumnName("_id_structure");

                    b.Property<long?>("Long")
                        .HasColumnType("bigint")
                        .HasColumnName("_Long");

                    b.Property<string>("String")
                        .HasColumnType("varchar(850)")
                        .HasColumnName("_String");

                    b.Property<string>("Text")
                        .HasColumnType("TEXT")
                        .HasColumnName("_Text");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "Boolean" }, "IX__values__Boolean");

                    b.HasIndex(new[] { "DateTime" }, "IX__values__DateTime");

                    b.HasIndex(new[] { "Double" }, "IX__values__Double");

                    b.HasIndex(new[] { "Guid" }, "IX__values__Guid");

                    b.HasIndex(new[] { "Long" }, "IX__values__Long");

                    b.HasIndex(new[] { "String" }, "IX__values__String");

                    b.HasIndex(new[] { "IdStructure", "IdObject" }, "IX__values__id_structure__id_object")
                        .IsUnique();

                    b.HasIndex(new[] { "IdObject" }, "IX__values__objects");

                    b.HasIndex(new[] { "IdStructure" }, "IX__values__structures");

                    b.ToTable("_values", (string)null);
                });

            modelBuilder.Entity("redb.Core.Models._RDependency", b =>
                {
                    b.HasOne("redb.Core.Models._RScheme", "Scheme1Navigation")
                        .WithMany("DependencyScheme1Navigations")
                        .HasForeignKey("IdScheme1");

                    b.HasOne("redb.Core.Models._RScheme", "Scheme2Navigation")
                        .WithMany("DependencyScheme2Navigations")
                        .HasForeignKey("IdScheme2")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Scheme1Navigation");

                    b.Navigation("Scheme2Navigation");
                });

            modelBuilder.Entity("redb.Core.Models._RFunction", b =>
                {
                    b.HasOne("redb.Core.Models._RScheme", "SchemeNavigation")
                        .WithMany("Functions")
                        .HasForeignKey("IdScheme")
                        .IsRequired();

                    b.Navigation("SchemeNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RListItem", b =>
                {
                    b.HasOne("redb.Core.Models._RList", "ListNavigation")
                        .WithMany("ListItems")
                        .HasForeignKey("IdList")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("redb.Core.Models._RObject", "ObjectNavigation")
                        .WithMany("ListItems")
                        .HasForeignKey("IdObject");

                    b.Navigation("ListNavigation");

                    b.Navigation("ObjectNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RObject", b =>
                {
                    b.HasOne("redb.Core.Models._RUser", "OwnerNavigation")
                        .WithMany("ObjectOwnerNavigations")
                        .HasForeignKey("IdOwner")
                        .IsRequired();

                    b.HasOne("redb.Core.Models._RObject", "ParentNavigation")
                        .WithMany("InverseParentNavigation")
                        .HasForeignKey("IdParent")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("redb.Core.Models._RScheme", "SchemeNavigation")
                        .WithMany("Objects")
                        .HasForeignKey("IdScheme")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("redb.Core.Models._RUser", "WhoChangeNavigation")
                        .WithMany("ObjectWhoChangeNavigations")
                        .HasForeignKey("IdWhoChange")
                        .IsRequired();

                    b.Navigation("OwnerNavigation");

                    b.Navigation("ParentNavigation");

                    b.Navigation("SchemeNavigation");

                    b.Navigation("WhoChangeNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RPermission", b =>
                {
                    b.HasOne("redb.Core.Models._RRole", "RoleNavigation")
                        .WithMany("Permissions")
                        .HasForeignKey("IdRole")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("redb.Core.Models._RUser", "UserNavigation")
                        .WithMany("Permissions")
                        .HasForeignKey("IdUser")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("RoleNavigation");

                    b.Navigation("UserNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RScheme", b =>
                {
                    b.HasOne("redb.Core.Models._RScheme", "ParentNavigation")
                        .WithMany("InverseParentNavigation")
                        .HasForeignKey("IdParent");

                    b.Navigation("ParentNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RStructure", b =>
                {
                    b.HasOne("redb.Core.Models._RList", "ListNavigation")
                        .WithMany("Structures")
                        .HasForeignKey("IdList");

                    b.HasOne("redb.Core.Models._RStructure", "ParentNavigation")
                        .WithMany("InverseParentNavigation")
                        .HasForeignKey("IdParent");

                    b.HasOne("redb.Core.Models._RScheme", "SchemeNavigation")
                        .WithMany("Structures")
                        .HasForeignKey("IdScheme")
                        .IsRequired();

                    b.HasOne("redb.Core.Models._RType", "TypeNavigation")
                        .WithMany("Structures")
                        .HasForeignKey("IdType")
                        .IsRequired();

                    b.Navigation("ListNavigation");

                    b.Navigation("ParentNavigation");

                    b.Navigation("SchemeNavigation");

                    b.Navigation("TypeNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RUsersRole", b =>
                {
                    b.HasOne("redb.Core.Models._RRole", "RoleNavigation")
                        .WithMany("UsersRoles")
                        .HasForeignKey("IdRole")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK__users_roles__roles");

                    b.HasOne("redb.Core.Models._RUser", "UserNavigation")
                        .WithMany("UsersRoles")
                        .HasForeignKey("IdUser")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK__users_roles__users");

                    b.Navigation("RoleNavigation");

                    b.Navigation("UserNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RValue", b =>
                {
                    b.HasOne("redb.Core.Models._RObject", "ObjectNavigation")
                        .WithMany("Values")
                        .HasForeignKey("IdObject")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("redb.Core.Models._RStructure", "StructureNavigation")
                        .WithMany("Values")
                        .HasForeignKey("IdStructure")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ObjectNavigation");

                    b.Navigation("StructureNavigation");
                });

            modelBuilder.Entity("redb.Core.Models._RList", b =>
                {
                    b.Navigation("ListItems");

                    b.Navigation("Structures");
                });

            modelBuilder.Entity("redb.Core.Models._RObject", b =>
                {
                    b.Navigation("InverseParentNavigation");

                    b.Navigation("ListItems");

                    b.Navigation("Values");
                });

            modelBuilder.Entity("redb.Core.Models._RRole", b =>
                {
                    b.Navigation("Permissions");

                    b.Navigation("UsersRoles");
                });

            modelBuilder.Entity("redb.Core.Models._RScheme", b =>
                {
                    b.Navigation("DependencyScheme1Navigations");

                    b.Navigation("DependencyScheme2Navigations");

                    b.Navigation("Functions");

                    b.Navigation("InverseParentNavigation");

                    b.Navigation("Objects");

                    b.Navigation("Structures");
                });

            modelBuilder.Entity("redb.Core.Models._RStructure", b =>
                {
                    b.Navigation("InverseParentNavigation");

                    b.Navigation("Values");
                });

            modelBuilder.Entity("redb.Core.Models._RType", b =>
                {
                    b.Navigation("Structures");
                });

            modelBuilder.Entity("redb.Core.Models._RUser", b =>
                {
                    b.Navigation("ObjectOwnerNavigations");

                    b.Navigation("ObjectWhoChangeNavigations");

                    b.Navigation("Permissions");
                });
#pragma warning restore 612, 618
        }
    }
}
