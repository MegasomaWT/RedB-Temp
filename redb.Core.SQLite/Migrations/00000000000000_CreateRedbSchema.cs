using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace redb.Core.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class CreateRedbSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "_deleted_objects",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_parent = table.Column<long>(type: "bigint", nullable: true),
                    _id_scheme = table.Column<long>(type: "bigint", nullable: false),
                    _id_owner = table.Column<long>(type: "bigint", nullable: false),
                    _id_who_change = table.Column<long>(type: "bigint", nullable: false),
                    _date_create = table.Column<double>(type: "datetime", nullable: false, defaultValueSql: "julianday(CURRENT_TIMESTAMP)"),
                    _date_modify = table.Column<double>(type: "datetime", nullable: false, defaultValueSql: "julianday(CURRENT_TIMESTAMP)"),
                    _date_begin = table.Column<DateTime>(type: "datetime", nullable: true),
                    _date_complete = table.Column<DateTime>(type: "datetime", nullable: true),
                    _key = table.Column<long>(type: "bigint", nullable: true),
                    _code_int = table.Column<long>(type: "bigint", nullable: true),
                    _code_string = table.Column<string>(type: "varchar (250)", nullable: true),
                    _code_guid = table.Column<Guid>(type: "uuid", nullable: true),
                    _name = table.Column<string>(type: "varchar (250)", nullable: true),
                    _note = table.Column<string>(type: "varchar (1000)", nullable: true),
                    _hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    _date_delete = table.Column<double>(type: "datetime", nullable: false, defaultValueSql: "julianday(CURRENT_TIMESTAMP)"),
                    _values = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__deleted_objects", x => x._id);
                });

            migrationBuilder.CreateTable(
                name: "_global_identity",
                columns: table => new
                {
                    _id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__global_identity", x => x._id);
                });

            migrationBuilder.CreateTable(
                name: "_links",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_1 = table.Column<long>(type: "bigint", nullable: false),
                    _id_2 = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__links", x => x._id);
                });

            migrationBuilder.CreateTable(
                name: "_lists",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _name = table.Column<string>(type: "varchar(250)", nullable: false),
                    _alias = table.Column<string>(type: "varchar(250)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__lists", x => x._id);
                });

            migrationBuilder.CreateTable(
                name: "_roles",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _name = table.Column<string>(type: "varchar(250)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__roles", x => x._id);
                });

            migrationBuilder.CreateTable(
                name: "_schemes",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_parent = table.Column<long>(type: "bigint", nullable: true),
                    _name = table.Column<string>(type: "varchar(250)", nullable: false),
                    _alias = table.Column<string>(type: "varchar(250)", nullable: true),
                    _name_space = table.Column<string>(type: "varchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__schemes", x => x._id);
                    table.ForeignKey(
                        name: "FK__schemes__schemes__id_parent",
                        column: x => x._id_parent,
                        principalTable: "_schemes",
                        principalColumn: "_id");
                });

            migrationBuilder.CreateTable(
                name: "_types",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _name = table.Column<string>(type: "varchar (250)", nullable: false),
                    _db_type = table.Column<string>(type: "varchar (250)", nullable: false),
                    _type = table.Column<string>(type: "varchar (250)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__types", x => x._id);
                });

            migrationBuilder.CreateTable(
                name: "_users",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _login = table.Column<string>(type: "varchar(250)", nullable: false),
                    _password = table.Column<string>(type: "TEXT", nullable: false),
                    _name = table.Column<string>(type: "varchar(250)", nullable: false),
                    _phone = table.Column<string>(type: "varchar(250)", nullable: true),
                    _email = table.Column<string>(type: "varchar(250)", nullable: true),
                    _date_register = table.Column<double>(type: "datetime", nullable: false, defaultValueSql: "julianday(CURRENT_TIMESTAMP)"),
                    _date_dismiss = table.Column<DateTime>(type: "datetime", nullable: true),
                    _enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "true")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__users", x => x._id);
                });

            migrationBuilder.CreateTable(
                name: "_users_roles",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_role = table.Column<long>(type: "bigint", nullable: false),
                    _id_user = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__users_roles", x => x._id);
                    table.ForeignKey(
                        name: "FK__users_roles__roles__id_role",
                        column: x => x._id_role,
                        principalTable: "_roles",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__users_roles__roles__id_user",
                        column: x => x._id_user,
                        principalTable: "_roles",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "_dependencies",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_scheme_1 = table.Column<long>(type: "bigint", nullable: true),
                    _id_scheme_2 = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__dependencies", x => x._id);
                    table.ForeignKey(
                        name: "FK__dependencies__schemes__id_scheme_1",
                        column: x => x._id_scheme_1,
                        principalTable: "_schemes",
                        principalColumn: "_id");
                    table.ForeignKey(
                        name: "FK__dependencies__schemes__id_scheme_2",
                        column: x => x._id_scheme_2,
                        principalTable: "_schemes",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "_functions",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_scheme = table.Column<long>(type: "bigint", nullable: false),
                    _language = table.Column<string>(type: "varchar(50)", nullable: false),
                    _name = table.Column<string>(type: "varchar(1000)", nullable: false),
                    _body = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__functions", x => x._id);
                    table.ForeignKey(
                        name: "FK__functions__schemes__id_scheme",
                        column: x => x._id_scheme,
                        principalTable: "_schemes",
                        principalColumn: "_id");
                });

            migrationBuilder.CreateTable(
                name: "_structures",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_parent = table.Column<long>(type: "bigint", nullable: true),
                    _id_scheme = table.Column<long>(type: "bigint", nullable: false),
                    _id_override = table.Column<long>(type: "bigint", nullable: true),
                    _id_type = table.Column<long>(type: "bigint", nullable: false),
                    _id_list = table.Column<long>(type: "bigint", nullable: true),
                    _name = table.Column<string>(type: "varchar(250)", nullable: false),
                    _alias = table.Column<string>(type: "varchar(250)", nullable: true),
                    _order = table.Column<long>(type: "bigint", nullable: true),
                    _readonly = table.Column<bool>(type: "boolean", nullable: true),
                    _allow_not_null = table.Column<bool>(type: "boolean", nullable: true),
                    _is_array = table.Column<bool>(type: "boolean", nullable: true),
                    _is_compress = table.Column<bool>(type: "boolean", nullable: true),
                    _store_null = table.Column<bool>(type: "boolean", nullable: true),
                    _default_value = table.Column<byte[]>(type: "bytea", nullable: true),
                    _default_editor = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__structures", x => x._id);
                    table.ForeignKey(
                        name: "FK__structures__lists__id_list",
                        column: x => x._id_list,
                        principalTable: "_lists",
                        principalColumn: "_id");
                    table.ForeignKey(
                        name: "FK__structures__schemes__id_scheme",
                        column: x => x._id_scheme,
                        principalTable: "_schemes",
                        principalColumn: "_id");
                    table.ForeignKey(
                        name: "FK__structures__structures__id_parent",
                        column: x => x._id_parent,
                        principalTable: "_structures",
                        principalColumn: "_id");
                    table.ForeignKey(
                        name: "FK__structures__types__id_type",
                        column: x => x._id_type,
                        principalTable: "_types",
                        principalColumn: "_id");
                });

            migrationBuilder.CreateTable(
                name: "_objects",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_parent = table.Column<long>(type: "bigint", nullable: true),
                    _id_scheme = table.Column<long>(type: "bigint", nullable: false),
                    _id_owner = table.Column<long>(type: "bigint", nullable: false),
                    _id_who_change = table.Column<long>(type: "bigint", nullable: false),
                    _date_create = table.Column<double>(type: "datetime", nullable: false, defaultValueSql: "julianday(CURRENT_TIMESTAMP)"),
                    _date_modify = table.Column<double>(type: "datetime", nullable: false, defaultValueSql: "julianday(CURRENT_TIMESTAMP)"),
                    _date_begin = table.Column<DateTime>(type: "datetime", nullable: true),
                    _date_complete = table.Column<DateTime>(type: "datetime", nullable: true),
                    _key = table.Column<long>(type: "bigint", nullable: true),
                    _code_int = table.Column<long>(type: "bigint", nullable: true),
                    _code_string = table.Column<string>(type: "varchar(250)", nullable: true),
                    _code_guid = table.Column<Guid>(type: "uuid", nullable: true),
                    _name = table.Column<string>(type: "varchar(250)", nullable: true),
                    _note = table.Column<string>(type: "varchar(1000)", nullable: true),
                    _hash = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__objects", x => x._id);
                    table.ForeignKey(
                        name: "FK__objects__objects__id_parent",
                        column: x => x._id_parent,
                        principalTable: "_objects",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__objects__schemes__id_scheme",
                        column: x => x._id_scheme,
                        principalTable: "_schemes",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__objects__users__id_owner",
                        column: x => x._id_owner,
                        principalTable: "_users",
                        principalColumn: "_id");
                    table.ForeignKey(
                        name: "FK__objects__users__id_who_change",
                        column: x => x._id_who_change,
                        principalTable: "_users",
                        principalColumn: "_id");
                });

            migrationBuilder.CreateTable(
                name: "_permissions",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_role = table.Column<long>(type: "bigint", nullable: true),
                    _id_user = table.Column<long>(type: "bigint", nullable: true),
                    _id_ref = table.Column<long>(type: "bigint", nullable: false),
                    _select = table.Column<bool>(type: "boolean", nullable: true),
                    _insert = table.Column<bool>(type: "boolean", nullable: true),
                    _update = table.Column<bool>(type: "boolean", nullable: true),
                    _delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__permissions", x => x._id);
                    table.ForeignKey(
                        name: "FK__permissions__roles__id_role",
                        column: x => x._id_role,
                        principalTable: "_roles",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__permissions__users__id_user",
                        column: x => x._id_user,
                        principalTable: "_users",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "_list_items",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_list = table.Column<long>(type: "bigint", nullable: false),
                    _value = table.Column<string>(type: "varchar(250)", nullable: true),
                    _id_object = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__list_items", x => x._id);
                    table.ForeignKey(
                        name: "FK__list_items__lists__id_list",
                        column: x => x._id_list,
                        principalTable: "_lists",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__list_items__objects__id_object",
                        column: x => x._id_object,
                        principalTable: "_objects",
                        principalColumn: "_id");
                });

            migrationBuilder.CreateTable(
                name: "_values",
                columns: table => new
                {
                    _id = table.Column<long>(type: "bigint", nullable: false),
                    _id_structure = table.Column<long>(type: "bigint", nullable: false),
                    _id_object = table.Column<long>(type: "bigint", nullable: false),
                    _String = table.Column<string>(type: "varchar(850)", nullable: true),
                    _Long = table.Column<long>(type: "bigint", nullable: true),
                    _Guid = table.Column<Guid>(type: "uuid", nullable: true),
                    _Double = table.Column<double>(type: "float", nullable: true),
                    _DateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    _Boolean = table.Column<bool>(type: "boolean", nullable: true),
                    _ByteArray = table.Column<byte[]>(type: "bytea", nullable: true),
                    _Text = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__values", x => x._id);
                    table.ForeignKey(
                        name: "FK__values__objects__id_object",
                        column: x => x._id_object,
                        principalTable: "_objects",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__values__structures__id_structure",
                        column: x => x._id_structure,
                        principalTable: "_structures",
                        principalColumn: "_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX__dependencies__id_scheme_1__id_scheme_2",
                table: "_dependencies",
                columns: new[] { "_id_scheme_1", "_id_scheme_2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__dependencies__schemes_1",
                table: "_dependencies",
                column: "_id_scheme_1");

            migrationBuilder.CreateIndex(
                name: "IX__dependencies__schemes_2",
                table: "_dependencies",
                column: "_id_scheme_2");

            migrationBuilder.CreateIndex(
                name: "IX__functions__id_scheme__name",
                table: "_functions",
                columns: new[] { "_id_scheme", "_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__functions__schemes",
                table: "_functions",
                column: "_id_scheme");

            migrationBuilder.CreateIndex(
                name: "IX__links__id_1__id_2",
                table: "_links",
                columns: new[] { "_id_1", "_id_2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__list_items__id_list",
                table: "_list_items",
                column: "_id_list");

            migrationBuilder.CreateIndex(
                name: "IX__list_items__objects",
                table: "_list_items",
                column: "_id_object");

            migrationBuilder.CreateIndex(
                name: "IX__objects__code_guid",
                table: "_objects",
                column: "_code_guid");

            migrationBuilder.CreateIndex(
                name: "IX__objects__code_int",
                table: "_objects",
                column: "_code_int");

            migrationBuilder.CreateIndex(
                name: "IX__objects__code_string",
                table: "_objects",
                column: "_code_string");

            migrationBuilder.CreateIndex(
                name: "IX__objects__date_create",
                table: "_objects",
                column: "_date_create");

            migrationBuilder.CreateIndex(
                name: "IX__objects__date_modify",
                table: "_objects",
                column: "_date_modify");

            migrationBuilder.CreateIndex(
                name: "IX__objects__hash",
                table: "_objects",
                column: "_hash");

            migrationBuilder.CreateIndex(
                name: "IX__objects__name",
                table: "_objects",
                column: "_name");

            migrationBuilder.CreateIndex(
                name: "IX__objects__objects",
                table: "_objects",
                column: "_id_parent");

            migrationBuilder.CreateIndex(
                name: "IX__objects__schemes",
                table: "_objects",
                column: "_id_scheme");

            migrationBuilder.CreateIndex(
                name: "IX__objects__users1",
                table: "_objects",
                column: "_id_owner");

            migrationBuilder.CreateIndex(
                name: "IX__objects__users2",
                table: "_objects",
                column: "_id_who_change");

            migrationBuilder.CreateIndex(
                name: "IX__permissions__id_role__id_user__id_ref__select__insert__update__delete",
                table: "_permissions",
                columns: new[] { "_id_role", "_id_user", "_id_ref", "_select", "_insert", "_update", "_delete" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__permissions__roles",
                table: "_permissions",
                column: "_id_role");

            migrationBuilder.CreateIndex(
                name: "IX__permissions__users",
                table: "_permissions",
                column: "_id_user");

            migrationBuilder.CreateIndex(
                name: "IX__roles__name",
                table: "_roles",
                column: "_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__schemes__name",
                table: "_schemes",
                column: "_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__schemes__schemes",
                table: "_schemes",
                column: "_id_parent");

            migrationBuilder.CreateIndex(
                name: "IX__structures__id_scheme__name",
                table: "_structures",
                columns: new[] { "_id_scheme", "_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__structures__lists",
                table: "_structures",
                column: "_id_list");

            migrationBuilder.CreateIndex(
                name: "IX__structures__schemes",
                table: "_structures",
                column: "_id_scheme");

            migrationBuilder.CreateIndex(
                name: "IX__structures__structures",
                table: "_structures",
                column: "_id_parent");

            migrationBuilder.CreateIndex(
                name: "IX__structures__types",
                table: "_structures",
                column: "_id_type");

            migrationBuilder.CreateIndex(
                name: "IX__users_roles__id_role__id_user",
                table: "_users_roles",
                columns: new[] { "_id_role", "_id_user" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__users_roles__roles",
                table: "_users_roles",
                column: "_id_role");

            migrationBuilder.CreateIndex(
                name: "IX__users_roles__users",
                table: "_users_roles",
                column: "_id_user");

            migrationBuilder.CreateIndex(
                name: "IX__values__Boolean",
                table: "_values",
                column: "_Boolean");

            migrationBuilder.CreateIndex(
                name: "IX__values__DateTime",
                table: "_values",
                column: "_DateTime");

            migrationBuilder.CreateIndex(
                name: "IX__values__Double",
                table: "_values",
                column: "_Double");

            migrationBuilder.CreateIndex(
                name: "IX__values__Guid",
                table: "_values",
                column: "_Guid");

            migrationBuilder.CreateIndex(
                name: "IX__values__id_structure__id_object",
                table: "_values",
                columns: new[] { "_id_structure", "_id_object" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX__values__Long",
                table: "_values",
                column: "_Long");

            migrationBuilder.CreateIndex(
                name: "IX__values__objects",
                table: "_values",
                column: "_id_object");

            migrationBuilder.CreateIndex(
                name: "IX__values__String",
                table: "_values",
                column: "_String");

            migrationBuilder.CreateIndex(
                name: "IX__values__structures",
                table: "_values",
                column: "_id_structure");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_deleted_objects");

            migrationBuilder.DropTable(
                name: "_dependencies");

            migrationBuilder.DropTable(
                name: "_functions");

            migrationBuilder.DropTable(
                name: "_global_identity");

            migrationBuilder.DropTable(
                name: "_links");

            migrationBuilder.DropTable(
                name: "_list_items");

            migrationBuilder.DropTable(
                name: "_permissions");

            migrationBuilder.DropTable(
                name: "_users_roles");

            migrationBuilder.DropTable(
                name: "_values");

            migrationBuilder.DropTable(
                name: "_roles");

            migrationBuilder.DropTable(
                name: "_objects");

            migrationBuilder.DropTable(
                name: "_structures");

            migrationBuilder.DropTable(
                name: "_users");

            migrationBuilder.DropTable(
                name: "_lists");

            migrationBuilder.DropTable(
                name: "_schemes");

            migrationBuilder.DropTable(
                name: "_types");
        }
    }
}
