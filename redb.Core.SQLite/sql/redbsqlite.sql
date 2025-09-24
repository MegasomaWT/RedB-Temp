DROP TABLE IF EXISTS  _global_identity;
DROP TABLE IF EXISTS _deleted_objects;
DROP TABLE IF EXISTS _dependencies;
DROP TABLE IF EXISTS _values;
DROP TABLE IF EXISTS _permissions;
DROP TABLE IF EXISTS _structures;
DROP TABLE IF EXISTS _users_roles;
DROP TABLE IF EXISTS _roles;
DROP TABLE IF EXISTS _list_items;
DROP TABLE IF EXISTS _lists;
DROP TABLE IF EXISTS _objects;
DROP TABLE IF EXISTS _links;
DROP TABLE IF EXISTS _users;
DROP TABLE IF EXISTS _functions;
DROP TABLE IF EXISTS _schemes;
DROP TABLE IF EXISTS _types;

CREATE TABLE _types(
	_id bigint NOT NULL,
	_name varchar(250) NOT NULL,
	_db_type varchar(250) NULL,
	_type varchar(250) NULL,
    CONSTRAINT PK__types PRIMARY KEY (_id)
) WITHOUT ROWID;

CREATE TABLE _links(
	_id bigint NOT NULL,
	_id_1 bigint NOT NULL,
	_id_2 bigint NOT NULL,
    CONSTRAINT PK__links PRIMARY KEY (_id),
    CONSTRAINT IX__links UNIQUE (_id_1, _id_2),
    CONSTRAINT CK__links CHECK  (_id_1<>_id_2)
) WITHOUT ROWID;

CREATE TABLE _lists(
	_id bigint NOT NULL,
	_name varchar(250) NOT NULL,
	_alias varchar(250) NULL,
    CONSTRAINT PK__lists PRIMARY KEY (_id)
) WITHOUT ROWID;


CREATE TABLE _roles(
	_id bigint NOT NULL,
	_name varchar(250) NOT NULL,
    CONSTRAINT PK__roles PRIMARY KEY (_id),
    CONSTRAINT IX__roles UNIQUE (_name)
) WITHOUT ROWID;


CREATE TABLE _users(
	_id bigint NOT NULL,
	_login varchar(250) NOT NULL,
	_password TEXT NOT NULL,
	_name varchar(250) NOT NULL,
	_phone varchar(250) NULL,
	_email varchar(250) NULL,
	_date_register datetime DEFAULT (julianday(CURRENT_TIMESTAMP)) NOT NULL,
	_date_dismiss datetime NULL,
	_enabled boolean DEFAULT true NOT NULL,
    CONSTRAINT PK__users PRIMARY KEY (_id)
) WITHOUT ROWID;


CREATE TABLE _users_roles(
	_id bigint NOT NULL,
	_id_role bigint NOT NULL,
    _id_user bigint NOT NULL,
    CONSTRAINT PK__users_roles PRIMARY KEY (_id),
    CONSTRAINT IX__users_roles UNIQUE (_id_role, _id_user),
    CONSTRAINT FK__users_roles__roles FOREIGN KEY (_id_role) REFERENCES _roles (_id) ON DELETE CASCADE,
    CONSTRAINT FK__users_roles__users FOREIGN KEY (_id_user) REFERENCES _users (_id) ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE _schemes(
	_id bigint NOT NULL,
	_id_parent bigint NULL,
	_name varchar(250) NOT NULL,
	_alias varchar(250) NULL,
	_name_space varchar(1000) NULL,
    CONSTRAINT PK__schemes PRIMARY KEY (_id),
	CONSTRAINT IX__schemes UNIQUE (_name),
    CONSTRAINT FK__schemes__schemes FOREIGN KEY (_id_parent) REFERENCES _schemes (_id) 
) WITHOUT ROWID;


CREATE TABLE _structures(
	_id bigint NOT NULL,
	_id_parent bigint NULL,
	_id_scheme bigint NOT NULL,
	_id_override bigint NULL,
	_id_type bigint NOT NULL,
	_id_list bigint NULL,
	_name varchar(250) NOT NULL,
	_alias varchar(250) NULL,
	_order bigint NULL,
	_readonly boolean NULL,
	_allow_not_null boolean NULL,
	_is_array boolean NULL,
	_is_compress boolean NULL,
	_store_null boolean NULL,
	_default_value bytea NULL,
	_default_editor text NULL,
    CONSTRAINT PK__structure PRIMARY KEY (_id),
	CONSTRAINT IX__structures UNIQUE (_id_scheme,_name),
    CONSTRAINT FK__structures__structures FOREIGN KEY (_id_parent) REFERENCES _structures (_id),
    CONSTRAINT FK__structures__schemes FOREIGN KEY (_id_scheme) REFERENCES _schemes (_id),
    CONSTRAINT FK__structures__types FOREIGN KEY (_id_type) REFERENCES _types (_id),
    CONSTRAINT FK__structures__lists FOREIGN KEY (_id_list) REFERENCES _lists (_id)
) WITHOUT ROWID;


CREATE TABLE _dependencies(
	_id bigint NOT NULL,
	_id_scheme_1 bigint,
	_id_scheme_2 bigint NOT NULL,
    CONSTRAINT PK__dependencies PRIMARY KEY (_id),
	CONSTRAINT IX__dependencies UNIQUE (_id_scheme_1,_id_scheme_2),
    CONSTRAINT FK__dependencies__schemes_1 FOREIGN KEY (_id_scheme_1) REFERENCES _schemes (_id), 
    CONSTRAINT FK__dependencies__schemes_2 FOREIGN KEY (_id_scheme_2) REFERENCES _schemes (_id)  ON DELETE CASCADE
) WITHOUT ROWID;


CREATE TABLE _objects(
	_id bigint NOT NULL,
	_id_parent bigint NULL,
	_id_scheme bigint NOT NULL,
	_id_owner bigint NOT NULL,
	_id_who_change bigint NOT NULL,
	_date_create datetime DEFAULT (julianday(CURRENT_TIMESTAMP)) NOT NULL,
	_date_modify datetime DEFAULT (julianday(CURRENT_TIMESTAMP)) NOT NULL,
	_date_begin datetime NULL,
	_date_complete datetime NULL,
	_key bigint NULL,
	_code_int bigint NULL,
	_code_string varchar(250) NULL,
	_code_guid uuid NULL,
	_name varchar(250) NULL,
	_note varchar(1000) NULL,
	_hash blob NULL,
    CONSTRAINT PK__objects PRIMARY KEY (_id),
	CONSTRAINT CK__hash CHECK (length(_hash)<=32),
    CONSTRAINT FK__objects__objects FOREIGN KEY (_id_parent) REFERENCES _objects (_id) ON DELETE CASCADE,
    CONSTRAINT FK__objects__schemes FOREIGN KEY (_id_scheme) REFERENCES _schemes (_id) ON DELETE CASCADE, 
    CONSTRAINT FK__objects__users1 FOREIGN KEY (_id_owner) REFERENCES _users (_id),
	CONSTRAINT FK__objects__users2 FOREIGN KEY (_id_who_change) REFERENCES _users (_id)    
) WITHOUT ROWID;

CREATE TABLE _deleted_objects(
	_id bigint NOT NULL,
	_id_parent bigint NULL,
	_id_scheme bigint NOT NULL,
	_id_owner bigint NOT NULL,
	_id_who_change bigint NOT NULL,
	_date_create datetime DEFAULT (julianday(CURRENT_TIMESTAMP)) NOT NULL,
	_date_modify datetime DEFAULT (julianday(CURRENT_TIMESTAMP)) NOT NULL,
	_date_begin datetime NULL,
	_date_complete datetime NULL,
	_key bigint NULL,
	_code_int bigint NULL,
	_code_string varchar(250) NULL,
	_code_guid uuid NULL,
	_name varchar(250) NULL,
	_note varchar(1000) NULL,
	_hash bytea NULL,
	_date_delete datetime DEFAULT (julianday(CURRENT_TIMESTAMP)) NOT NULL,
	_values bytea NULL,
    CONSTRAINT PK__deleted_objects PRIMARY KEY (_id)
) WITHOUT ROWID;

CREATE TABLE _list_items(
	_id bigint NOT NULL,
	_id_list bigint NOT NULL,
	_value varchar(250) NULL,
	_id_object bigint NULL,
    CONSTRAINT PK__list_items PRIMARY KEY (_id),
    CONSTRAINT FK__list_items__id_list FOREIGN KEY (_id_list) REFERENCES _lists (_id) ON DELETE CASCADE,
    CONSTRAINT FK__list_items__objects FOREIGN KEY (_id_object) REFERENCES _objects (_id)
) WITHOUT ROWID;

CREATE TABLE _values(
	_id bigint NOT NULL,
	_id_structure bigint NOT NULL,
	_id_object bigint NOT NULL,
	_String varchar(850) NULL,
	_Long bigint NULL,
	_Guid uuid NULL,
	_Double float NULL,
	_DateTime datetime NULL,
	_Boolean boolean NULL,
	_ByteArray bytea NULL,
	_Text text NULL,
    CONSTRAINT PK__values PRIMARY KEY (_id),
    CONSTRAINT FK__values__objects FOREIGN KEY (_id_object) REFERENCES _objects (_id) ON DELETE CASCADE,
    CONSTRAINT FK__values__structures FOREIGN KEY (_id_structure) REFERENCES _structures (_id) ON DELETE CASCADE,
	CONSTRAINT IX__values_SO UNIQUE (_id_structure,_id_object)
) WITHOUT ROWID;

CREATE TABLE _permissions(
	_id bigint NOT NULL,
	_id_role bigint NULL,
	_id_user bigint NULL,
	_id_ref bigint NOT NULL,
	_select boolean NULL,
	_insert boolean NULL,
	_update boolean NULL,
	_delete boolean NULL,
    CONSTRAINT PK__object_permissions PRIMARY KEY (_id),
    CONSTRAINT CK__permissions_users_roles CHECK  (_id_role IS NOT NULL AND _id_user IS NULL OR _id_role IS NULL AND _id_user IS NOT NULL),
    CONSTRAINT IX__permissions UNIQUE (_id_role, _id_user, _id_ref, _select, _insert, _update, _delete),
    CONSTRAINT FK__permissions__roles FOREIGN KEY (_id_role) REFERENCES _roles (_id) ON DELETE CASCADE,
    CONSTRAINT FK__permissions__users FOREIGN KEY (_id_user) REFERENCES _users (_id) ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE _functions
(
    _id bigint NOT NULL,
    _id_scheme bigint NOT NULL,
	_language varchar(50) NOT NULL,
    _name varchar(1000) NOT NULL,
    _body text NOT NULL,
    CONSTRAINT PK__functions PRIMARY KEY (_id),
	CONSTRAINT IX__functions_scheme_name UNIQUE (_id_scheme, _name),
    CONSTRAINT FK__functions__schemes FOREIGN KEY (_id_scheme) REFERENCES _schemes (_id)
) WITHOUT ROWID;

CREATE TABLE _global_identity
(
    _id integer PRIMARY KEY AUTOINCREMENT
);


CREATE INDEX "IX__functions__schemes" ON _functions (_id_scheme);
CREATE INDEX "IX__permissions__roles" ON _permissions (_id_role);
CREATE INDEX "IX__permissions__users" ON _permissions (_id_user);
CREATE INDEX "IX__values__objects" ON _values (_id_object);
CREATE INDEX "IX__values__structures" ON _values (_id_structure);
CREATE INDEX "IX__values__String" ON _values (_String);
CREATE INDEX "IX__values__Long" ON _values (_Long);
CREATE INDEX "IX__values__Guid" ON _values (_Guid);
CREATE INDEX "IX__values__Double" ON _values (_Double);
CREATE INDEX "IX__values__DateTime" ON _values (_DateTime);
CREATE INDEX "IX__values__Boolean" ON _values (_Boolean);
CREATE INDEX "IX__list_items__id_list" ON _list_items (_id_list);
CREATE INDEX "IX__list_items__objects" ON _list_items (_id_object);
CREATE INDEX "IX__objects__objects" ON _objects (_id_parent);
CREATE INDEX "IX__objects__schemes" ON _objects (_id_scheme);
CREATE INDEX "IX__objects__users1" ON _objects (_id_owner);
CREATE INDEX "IX__objects__users2" ON _objects (_id_who_change);
CREATE INDEX "IX__objects__date_create" ON _objects (_date_create);
CREATE INDEX "IX__objects__date_modify" ON _objects (_date_modify);
CREATE INDEX "IX__objects__code_int" ON _objects (_code_int);
CREATE INDEX "IX__objects__code_string" ON _objects (_code_string);
CREATE INDEX "IX__objects__name" ON _objects (_name);
CREATE INDEX "IX__objects__code_guid" ON _objects (_code_guid);
CREATE INDEX "IX__objects__hash" ON _objects (_hash);
CREATE INDEX "IX__dependencies__schemes_1" ON _dependencies (_id_scheme_1); 
CREATE INDEX "IX__dependencies__schemes_2" ON _dependencies (_id_scheme_2);
CREATE INDEX "IX__structures__structures" ON _structures (_id_parent);
CREATE INDEX "IX__structures__schemes" ON _structures (_id_scheme);
CREATE INDEX "IX__structures__types" ON _structures (_id_type);
CREATE INDEX "IX__structures__lists" ON _structures (_id_list);
CREATE INDEX "IX__schemes__schemes" ON _schemes (_id_parent);
CREATE INDEX "IX__users_roles__roles" ON _users_roles (_id_role);
CREATE INDEX "IX__users_roles__users" ON _users_roles (_id_user);

CREATE TRIGGER TR__objects__deleted_objects
BEFORE DELETE ON _objects
FOR EACH ROW
BEGIN
  insert into _deleted_objects        
  select _o.*,julianday(CURRENT_TIMESTAMP) _date_delete, _v._value  from _objects as '_o'
  left join (select _id_object, cast(json_group_array( 
        json_patch('{}',json_object(
        '_id', _jv._id, 
        '_id_structure', _id_structure,
        '_id_object', _id_object,
        '_String',_String,
        '_Long',_Long,
        '_Guid',_Guid,
        '_Double',_Double,
        '_DateTime',_DateTime,
        '_Boolean',_Boolean,
        '_ByteArray',_ByteArray,
        '_Text',_Text,
        '_name',_s._name,
        '_db_type', _t._db_type
        ))) as blob)_value FROM _values as '_jv'
	join _structures _s on _s._id = _jv._id_structure
	join _types _t on _t._id = _s._id_type
        group by _id_object) as '_v' on _v._id_object=_o._id
         where _o._id = old._id
END;


INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775709, 'Boolean', 'Boolean', 'boolean');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775708, 'DateTime', 'DateTime', 'DateTime');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775707, 'Double', 'Double', 'double');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775706, 'ListItem', 'Long', '_RListItem');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775705, 'Guid', 'Guid', 'Guid');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775704, 'Long', 'Long', 'long');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775703, 'Object', 'Long', '_RObject');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775702, 'Text', 'Text', 'string');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775701, 'ByteArray', 'ByteArray', 'byte[]');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775700, 'String', 'String', 'string');


INSERT INTO _users (_id, _login, _password, _name, _phone, _email, _date_register, _date_dismiss, _enabled) VALUES (-9223372036854775800, 'admin', '', 'admin', NULL, NULL, julianday('2023-12-26T01:14:34.410'), NULL, true);

-- for webApi dashboard
INSERT into _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (-9223372036854769999, NULL, 'redb.WebApp', 'redB Dashboard', NULL);
INSERT into _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (-9223372036854769998, -9223372036854769999, 'redb.WebApp.Sidebar.Group', 'sidebar Group', NULL);
INSERT into _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (-9223372036854769997, -9223372036854769998, 'redb.WebApp.Sidebar.Item', 'sidebar Item', NULL);
INSERT into _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (-9223372036854769996, NULL, -9223372036854769997, NULL, -9223372036854775700, NULL, 'name', 'redb.WebApp.Sidebar.Item.name', NULL, 1, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (-9223372036854769995, NULL, -9223372036854769997, NULL, -9223372036854775702, NULL, 'path', 'redb.WebApp.Sidebar.Item.path', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769994, NULL, -9223372036854769999, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T14:48:18.847'), julianday('2024-02-06T14:48:18.847'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769944, -9223372036854769994, -9223372036854769998, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T19:44:34.133'), julianday('2024-02-06T19:44:34.133'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769993, -9223372036854769944, -9223372036854769998, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T14:50:53.753'), julianday('2024-02-06T14:50:53.753'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769945, -9223372036854769944, -9223372036854769998, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T19:42:54.410'), julianday('2024-02-06T19:42:54.410'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769992, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T14:57:30.190'), julianday('2024-02-06T14:57:30.190'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Dependencies', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769991, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Lists', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769990, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Objects', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769989, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Permissions', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769988, -9223372036854769945, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings.Roles', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769987, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Schemes', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769986, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Structures', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769985, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Types', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769984, -9223372036854769945, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings.Users', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769983, -9223372036854769945, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings.Users_roles', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769982, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Values', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769981, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Deleted_objects', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769980, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Functions', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769979, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Links', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769978, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, julianday('2024-02-06T15:20:32.563'), julianday('2024-02-06T15:20:32.563'), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.List_items', NULL, NULL);
INSERT into _dependencies (_id, _id_scheme_1, _id_scheme_2) VALUES (-9223372036854769947, -9223372036854769999, -9223372036854769998);
INSERT into _dependencies (_id, _id_scheme_1, _id_scheme_2) VALUES (-9223372036854769946, -9223372036854769998, -9223372036854769997);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769977, -9223372036854769996, -9223372036854769992, 'Dependencies', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769976, -9223372036854769995, -9223372036854769992, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'M 4 19 h 6 v -2 H 4 v 2 Z M 20 5 H 4 v 2 h 16 V 5 Z m -3 6 H 4 v 2 h 13.25 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 H 15 v -2 l -3 3 l 3 3 v -2 h 2 c 2.21 0 4 -1.79 4 -4 s -1.79 -4 -4 -4 Z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769975, -9223372036854769996, -9223372036854769981, 'Deleted objects', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769974, -9223372036854769995, -9223372036854769981, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 0 0 c -0.5626 0.6404 -0.491 1.6212 0.1116 2.2238 l 6.841 6.8358 l -6.8724 6.8674 c -0.6184 0.6186 -0.6184 1.6216 0 2.2402 l 0.0226 0.0226 c 0.6186 0.6186 1.6214 0.6186 2.2402 -0 l 6.8732 -6.8682 l 6.8732 6.868 c 0.6186 0.6186 1.6216 0.6186 2.2402 -0 l 0.0226 -0.0226 c 0.6186 -0.6186 0.6186 -1.6214 -0 -2.2402 l -6.8726 -6.8674 l 6.841 -6.8358 c 0.6026 -0.6026 0.674 -1.5834 0.1116 -2.2238 c -0.6162 -0.7014 -1.6858 -0.7274 -2.3356 -0.0776 l -6.8806 6.8758 l -6.8808 -6.8754 c -0.6498 -0.65 -1.7192 -0.624 -2.3354 0.0776 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769973, -9223372036854769996, -9223372036854769980, 'Functions', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769972, -9223372036854769995, -9223372036854769980, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 11.344 5.71 c 0 -0.73 0.074 -1.122 1.199 -1.122 h 1.502 v -2.717 h -2.404 c -2.886 0 -3.903 1.36 -3.903 3.646 v 1.765 h -1.8 v 2.718 h 1.8 v 8.128 h 3.601 v -8.128 h 2.403 l 0.32 -2.718 h -2.724 l 0.006 -1.572 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769971, -9223372036854769996, -9223372036854769979, 'Links', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769970, -9223372036854769995, -9223372036854769979, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 16.469 8.924 l -2.414 2.413 c -0.156 0.156 -0.408 0.156 -0.564 0 c -0.156 -0.155 -0.156 -0.408 0 -0.563 l 2.414 -2.414 c 1.175 -1.175 1.175 -3.087 0 -4.262 c -0.57 -0.569 -1.326 -0.883 -2.132 -0.883 s -1.562 0.313 -2.132 0.883 l -2.414 2.413 c -1.175 1.175 -1.175 3.087 0 4.263 c 0.288 0.288 0.624 0.511 0.997 0.662 c 0.204 0.083 0.303 0.315 0.22 0.52 c -0.171 0.422 -0.643 0.17 -0.52 0.22 c -0.473 -0.191 -0.898 -0.474 -1.262 -0.838 c -1.487 -1.485 -1.487 -3.904 0 -5.391 l 2.414 -2.413 c 0.72 -0.72 1.678 -1.117 2.696 -1.117 s 1.976 0.396 2.696 1.117 c 1.487 1.486 1.487 3.904 0.001 5.39 m -6.393 -1.099 c -0.205 -0.083 -0.437 0.016 -0.52 0.22 c -0.083 0.205 0.016 0.437 0.22 0.52 c 0.374 0.151 0.709 0.374 0.997 0.662 c 1.176 1.176 1.176 3.088 0 4.263 l -2.414 2.413 c -0.569 0.569 -1.326 0.883 -2.131 0.883 s -1.562 -0.313 -2.132 -0.883 c -1.175 -1.175 -1.175 -3.087 0 -4.262 l 2.414 -2.414 c 0.156 -0.155 0.156 -0.408 0 -0.564 c -0.156 -0.156 -0.408 -0.156 -0.564 0 l -2.414 2.414 c -1.487 1.485 -1.487 3.904 0 5.391 c 0.72 0.72 1.678 1.116 2.696 1.116 s 1.976 -0.396 2.696 -1.116 l 2.414 -2.413 c 1.487 -1.486 1.487 -3.905 0 -5.392 c -0.364 -0.365 -0.788 -0.646 -1.262 -0.838');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769969, -9223372036854769996, -9223372036854769978, 'List items', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769968, -9223372036854769995, -9223372036854769978, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 2.25 12.584 c -0.713 0 -1.292 0.578 -1.292 1.291 s 0.579 1.291 1.292 1.291 c 0.713 0 1.292 -0.578 1.292 -1.291 s -0.579 -1.291 -1.292 -1.291 z m 0 1.723 c -0.238 0 -0.43 -0.193 -0.43 -0.432 s 0.192 -0.432 0.43 -0.432 c 0.238 0 0.431 0.193 0.431 0.432 s -0.193 0.432 -0.431 0.432 z m 3.444 -7.752 h 12.916 c 0.237 0 0.431 -0.191 0.431 -0.43 s -0.193 -0.431 -0.431 -0.431 h -12.916 c -0.238 0 -0.43 0.192 -0.43 0.431 s 0.193 0.43 0.43 0.43 z m -3.444 2.153 c -0.713 0 -1.292 0.578 -1.292 1.291 c 0 0.715 0.579 1.292 1.292 1.292 c 0.713 0 1.292 -0.577 1.292 -1.292 c 0 -0.712 -0.579 -1.291 -1.292 -1.291 z m 0 1.722 c -0.238 0 -0.43 -0.192 -0.43 -0.431 c 0 -0.237 0.192 -0.43 0.43 -0.43 c 0.238 0 0.431 0.192 0.431 0.43 c 0 0.239 -0.193 0.431 -0.431 0.431 z m 16.36 -0.86 h -12.916 c -0.238 0 -0.43 0.192 -0.43 0.43 c 0 0.238 0.192 0.431 0.43 0.431 h 12.916 c 0.237 0 0.431 -0.192 0.431 -0.431 c 0 -0.238 -0.193 -0.43 -0.431 -0.43 z m 0 3.873 h -12.916 c -0.238 0 -0.43 0.193 -0.43 0.432 s 0.192 0.432 0.43 0.432 h 12.916 c 0.237 0 0.431 -0.193 0.431 -0.432 s -0.193 -0.432 -0.431 -0.432 z m -16.36 -8.61 c -0.713 0 -1.292 0.578 -1.292 1.292 c 0 0.713 0.579 1.291 1.292 1.291 c 0.713 0 1.292 -0.578 1.292 -1.291 c 0 -0.713 -0.579 -1.292 -1.292 -1.292 z m 0 1.722 c -0.238 0 -0.43 -0.191 -0.43 -0.43 s 0.192 -0.431 0.43 -0.431 c 0.238 0 0.431 0.192 0.431 0.431 s -0.193 0.43 -0.431 0.43 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769967, -9223372036854769996, -9223372036854769991, 'Lists', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769966, -9223372036854769995, -9223372036854769991, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 14 2 h -8 c -1.1 0 -1.99 0.9 -1.99 2 l -0.01 16 c 0 1.1 0.89 2 1.99 2 h 12.01 c 1.1 0 2 -0.9 2 -2 v -12 l -6 -6 z m 2 16 h -8 v -2 h 8 v 2 z m 0 -4 h -8 v -2 h 8 v 2 z m -3 -5 v -5.5 l 5.5 5.5 h -5.5 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769965, -9223372036854769996, -9223372036854769990, 'Objects', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769964, -9223372036854769995, -9223372036854769990, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'M 4 8 h 4 V 4 H 4 v 4 Z m 6 12 h 4 v -4 h -4 v 4 Z m -6 0 h 4 v -4 H 4 v 4 Z m 0 -6 h 4 v -4 H 4 v 4 Z m 6 0 h 4 v -4 h -4 v 4 Z m 6 -10 v 4 h 4 V 4 h -4 Z m -6 4 h 4 V 4 h -4 v 4 Z m 6 6 h 4 v -4 h -4 v 4 Z m 0 6 h 4 v -4 h -4 v 4 Z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769963, -9223372036854769996, -9223372036854769989, 'Permissions', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769962, -9223372036854769995, -9223372036854769989, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 12.443 9.672 c 0.203 -0.496 0.329 -1.052 0.329 -1.652 c 0 -1.969 -1.241 -3.565 -2.772 -3.565 s -2.772 1.596 -2.772 3.565 c 0 0.599 0.126 1.156 0.33 1.652 c -1.379 0.555 -2.31 1.553 -2.31 2.704 c 0 1.75 2.128 3.169 4.753 3.169 c 2.624 0 4.753 -1.419 4.753 -3.169 c -0.001 -1.151 -0.933 -2.149 -2.311 -2.704 z m -2.443 -4.425 c 1.094 0 1.98 1.242 1.98 2.773 c 0 1.531 -0.887 2.772 -1.98 2.772 s -1.98 -1.241 -1.98 -2.772 c 0 -1.531 0.886 -2.773 1.98 -2.773 z m 0 9.506 c -2.187 0 -3.96 -1.063 -3.96 -2.377 c 0 -0.854 0.757 -1.596 1.885 -2.015 c 0.508 0.745 1.245 1.224 2.076 1.224 s 1.567 -0.479 2.076 -1.224 c 1.127 0.418 1.885 1.162 1.885 2.015 c -0.001 1.313 -1.774 2.377 -3.962 2.377 z m 0 -13.862 c -5.031 0 -9.109 4.079 -9.109 9.109 c 0 5.031 4.079 9.109 9.109 9.109 c 5.031 0 9.109 -4.078 9.109 -9.109 c 0 -5.031 -4.078 -9.109 -9.109 -9.109 z m 0 17.426 c -4.593 0 -8.317 -3.725 -8.317 -8.317 c 0 -4.593 3.724 -8.317 8.317 -8.317 c 4.593 0 8.317 3.724 8.317 8.317 c 0 4.593 -3.724 8.317 -8.317 8.317 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769961, -9223372036854769996, -9223372036854769988, 'Roles', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769960, -9223372036854769995, -9223372036854769988, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 12 2 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 s -2 -0.9 -2 -2 s 0.9 -2 2 -2 z m 9 7 h -6 v 13 h -2 v -6 h -2 v 6 h -2 v -13 h -6 v -2 h 18 v 2 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769959, -9223372036854769996, -9223372036854769987, 'Schemes', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769958, -9223372036854769995, -9223372036854769987, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 18.378 1.062 h -14.523 c -0.309 0 -0.559 0.25 -0.559 0.559 c 0 0.309 0.25 0.559 0.559 0.559 h 13.964 v 13.964 c 0 0.309 0.25 0.559 0.559 0.559 c 0.31 0 0.56 -0.25 0.56 -0.559 v -14.523 c 0 -0.309 -0.25 -0.559 -0.56 -0.559 z m -2.234 2.234 h -14.523 c -0.309 0 -0.559 0.25 -0.559 0.559 v 14.523 c 0 0.31 0.25 0.56 0.559 0.56 h 14.523 c 0.309 0 0.559 -0.25 0.559 -0.56 v -14.523 c -0.001 -0.309 -0.251 -0.559 -0.559 -0.559 z m -0.558 13.966 c 0 0.31 -0.25 0.558 -0.56 0.558 h -12.288 c -0.309 0 -0.559 -0.248 -0.559 -0.558 v -12.29 c 0 -0.309 0.25 -0.559 0.559 -0.559 h 12.289 c 0.31 0 0.56 0.25 0.56 0.559 v 12.29 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769957, -9223372036854769996, -9223372036854769986, 'Structures', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769956, -9223372036854769995, -9223372036854769986, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 5.029 1.734 h 10.935 c 0.317 0 0.575 -0.257 0.575 -0.575 s -0.258 -0.576 -0.575 -0.576 h -10.935 c -0.318 0 -0.576 0.258 -0.576 0.576 s 0.258 0.575 0.576 0.575 z m -4.029 3.454 v 13.812 h 18.417 v -13.812 h -18.417 z m 17.266 12.661 h -16.115 v -11.511 h 16.115 v 11.511 z m -15.539 -13.813 h 14.963 c 0.317 0 0.575 -0.257 0.575 -0.576 c 0 -0.318 -0.258 -0.575 -0.575 -0.575 h -14.963 c -0.318 0 -0.576 0.257 -0.576 0.575 c 0 0.319 0.258 0.576 0.576 0.576 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769955, -9223372036854769996, -9223372036854769985, 'Types', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769954, -9223372036854769995, -9223372036854769985, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 14.68 12.621 c -0.9 0 -1.702 0.43 -2.216 1.09 l -4.549 -2.637 c 0.284 -0.691 0.284 -1.457 0 -2.146 l 4.549 -2.638 c 0.514 0.661 1.315 1.09 2.216 1.09 c 1.549 0 2.809 -1.26 2.809 -2.808 c 0 -1.548 -1.26 -2.809 -2.809 -2.809 c -1.548 0 -2.808 1.26 -2.808 2.809 c 0 0.38 0.076 0.741 0.214 1.073 l -4.55 2.638 c -0.515 -0.661 -1.316 -1.09 -2.217 -1.09 c -1.548 0 -2.808 1.26 -2.808 2.809 s 1.26 2.808 2.808 2.808 c 0.9 0 1.702 -0.43 2.217 -1.09 l 4.55 2.637 c -0.138 0.332 -0.214 0.693 -0.214 1.074 c 0 1.549 1.26 2.809 2.808 2.809 c 1.549 0 2.809 -1.26 2.809 -2.809 s -1.26 -2.81 -2.809 -2.81 m 0 -10.109 c 1.136 0 2.06 0.923 2.06 2.06 s -0.925 2.058 -2.06 2.058 s -2.059 -0.923 -2.059 -2.059 s 0.923 -2.059 2.059 -2.059 m -9.361 9.549 c -1.136 0 -2.06 -0.924 -2.06 -2.06 s 0.923 -2.059 2.06 -2.059 c 1.135 0 2.06 0.923 2.06 2.059 s -0.925 2.06 -2.06 2.06 m 9.361 5.427 c -1.136 0 -2.059 -0.922 -2.059 -2.059 s 0.923 -2.061 2.059 -2.061 s 2.06 0.924 2.06 2.061 s -0.925 2.059 -2.06 2.059 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769953, -9223372036854769996, -9223372036854769984, 'Users', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769952, -9223372036854769995, -9223372036854769984, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 16 11 c 1.66 0 2.99 -1.34 2.99 -3 s -1.33 -3 -2.99 -3 c -1.66 0 -3 1.34 -3 3 s 1.34 3 3 3 z m -8 0 c 1.66 0 2.99 -1.34 2.99 -3 s -1.33 -3 -2.99 -3 c -1.66 0 -3 1.34 -3 3 s 1.34 3 3 3 z m 0 2 c -2.33 0 -7 1.17 -7 3.5 v 2.5 h 14 v -2.5 c 0 -2.33 -4.67 -3.5 -7 -3.5 z m 8 0 c -0.29 0 -0.62 0.02 -0.97 0.05 c 1.16 0.84 1.97 1.97 1.97 3.45 v 2.5 h 6 v -2.5 c 0 -2.33 -4.67 -3.5 -7 -3.5 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769951, -9223372036854769996, -9223372036854769983, 'Users roles', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769950, -9223372036854769995, -9223372036854769983, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 12 2 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 s -2 -0.9 -2 -2 s 0.9 -2 2 -2 z m 9 7 h -6 v 13 h -2 v -6 h -2 v 6 h -2 v -13 h -6 v -2 h 18 v 2 z m -3 3 q 1 10 3 0 z m -12 0 q 2 2 -3 0 z m -2 10 q 4 -8 0 -7 z');
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769949, -9223372036854769996, -9223372036854769982, 'Values', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Text) VALUES (-9223372036854769948, -9223372036854769995, -9223372036854769982, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'm 15.396 2.292 h -10.792 c -0.212 0 -0.385 0.174 -0.385 0.386 v 14.646 c 0 0.212 0.173 0.385 0.385 0.385 h 10.792 c 0.211 0 0.385 -0.173 0.385 -0.385 v -14.647 c 0 -0.212 -0.174 -0.385 -0.385 -0.385 m -0.386 14.646 h -10.02 v -2.698 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.519 1.156 -1.156 s -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.321 -1.089 0.771 h -1.609 v -3.083 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.518 1.156 -1.156 c 0 -0.638 -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.322 -1.089 0.771 h -1.609 v -3.086 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.519 1.156 -1.156 c 0 -0.638 -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.322 -1.089 0.771 h -1.609 v -2.699 h 10.02 v 13.876 z m -7.708 -3.084 c 0 -0.212 0.173 -0.386 0.385 -0.386 s 0.385 0.174 0.385 0.386 s -0.173 0.385 -0.385 0.385 s -0.385 -0.173 -0.385 -0.385 m 0 -3.854 c 0 -0.212 0.173 -0.385 0.385 -0.385 s 0.386 0.173 0.386 0.385 s -0.173 0.385 -0.385 0.385 s -0.386 -0.173 -0.386 -0.385 m 0 -3.854 c 0 -0.212 0.173 -0.386 0.385 -0.386 s 0.385 0.174 0.385 0.386 s -0.173 0.385 -0.384 0.385 s -0.386 -0.173 -0.386 -0.385');

INSERT into _functions (_id, _id_scheme, _language, _name, _body) VALUES (-9223372036854769943, -9223372036854769997, 'js', 'Objects', '
$(() => {

    $("#ObjectList").dxTreeList({
        scrolling: {
            useNative: true,
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "onHover", // or "onScroll" | "always" | "never"
            mode: "standard" // or "virtual"
        },
        dataSource: new DevExpress.data.CustomStore({
            load: function () {
                var d = $.Deferred();
                return $.getJSON("/Cnt/CRObjects/GetAllObjects")
                    .done(function (result) {
                        d.resolve(result);
                    })
                    .fail(function () {
                        throw "Data loading error";
                    });
            }
        }),
        sorting: {
            mode: "multiple",
        },
        selection: {
            mode: "single",
        },
        columns: [{
            dataField: "id",
            caption: "id",
            fixed: true,
            cellTemplate: function (container, options) {
                let refProperties = $("<a>", { class: "propertiesToggle", text: options.data.id, id: options.data.id, type: "CRObjects" });
                refProperties.one("click", handler1);
                container.append(refProperties);
            }
        },
        {
            dataField: "name",
            caption: "Name"
        },
        {
            dataField: "parentId",
            caption: "Parent"
        }]
    });
});
');


INSERT into _functions (_id, _id_scheme, _language, _name, _body) VALUES (-9223372036854769939, -9223372036854769997, 'js', 'Deleted_objects', '
$(() => {

    $("#DeletedObjectsList").dxTreeList({
        scrolling: {
            useNative: true,
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "onHover", // or "onScroll" | "always" | "never"
            mode: "standard" // or "virtual"
        },
        dataSource: new DevExpress.data.CustomStore({
            load: function () {
                var d = $.Deferred();
                return $.getJSON("/Cnt/CRDeletedObjects/GetAllObjects")
                    .done(function (result) {
                        d.resolve(result);
                    })
                    .fail(function () {
                        throw "Data loading error";
                    });
            }
        }),
        sorting: {
            mode: "multiple",
        },
        selection: {
            mode: "single",
        },
        columns: [{
            dataField: "id",
            caption: "id",
            fixed: true,
            cellTemplate: function (container, options) {
                let refProperties = $("<a>", { class: "propertiesToggle", text: options.data.id, id: options.data.id, type: "CRDeletedObjects" });
                refProperties.one("click", handler1);
                container.append(refProperties);
            }
        },
        {
            dataField: "name",
            caption: "Name"
        },
        {
            dataField: "parentId",
            caption: "Parent"
        }]
    });
});');
