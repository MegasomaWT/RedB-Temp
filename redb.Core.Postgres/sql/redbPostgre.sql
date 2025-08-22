DROP SEQUENCE IF EXISTS global_identity;
DROP VIEW IF EXISTS v_user_permissions;
DROP VIEW IF EXISTS v_objects_json;
DROP VIEW IF EXISTS v_schemes_definition;
DROP FUNCTION IF EXISTS get_scheme_definition;
DROP FUNCTION IF EXISTS get_object_json;
-- Удаляем все существующие перегрузки search_objects_with_facets
DROP FUNCTION IF EXISTS search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb);
DROP FUNCTION IF EXISTS search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint);
DROP FUNCTION IF EXISTS search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint, integer);
DROP FUNCTION IF EXISTS get_facets;
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
DROP FUNCTION IF EXISTS ftr__objects__deleted_objects;
DROP FUNCTION IF EXISTS validate_structure_name;
DROP FUNCTION IF EXISTS validate_scheme_name;
DROP FUNCTION IF EXISTS get_user_permissions_for_object;
DROP FUNCTION IF EXISTS auto_create_node_permissions;


CREATE TABLE _types(
	_id bigint NOT NULL,
	_name varchar(250) NOT NULL,
	_db_type varchar(250) NULL,
	_type varchar(250) NULL,
    CONSTRAINT PK__types PRIMARY KEY (_id)
);

CREATE TABLE _links(
	_id bigint NOT NULL,
	_id_1 bigint NOT NULL,
	_id_2 bigint NOT NULL,
    CONSTRAINT PK__links PRIMARY KEY (_id),
    CONSTRAINT IX__links UNIQUE (_id_1, _id_2),
    CONSTRAINT CK__links CHECK  (_id_1<>_id_2)
);

CREATE TABLE _lists(
	_id bigint NOT NULL,
	_name varchar(250) NOT NULL,
	_alias varchar(250) NULL,
    CONSTRAINT PK__lists PRIMARY KEY (_id)
);


CREATE TABLE _roles(
	_id bigint NOT NULL,
	_name varchar(250) NOT NULL,
    CONSTRAINT PK__roles PRIMARY KEY (_id),
    CONSTRAINT IX__roles UNIQUE (_name)
);


CREATE TABLE _users(
	_id bigint NOT NULL,
	_login varchar(250) NOT NULL,
	_password TEXT NOT NULL,
	_name varchar(250) NOT NULL UNIQUE,
	_phone varchar(250) NULL,
	_email varchar(250) NULL,
	_date_register timestamp DEFAULT now() NOT NULL,
	_date_dismiss timestamp NULL,
	_enabled boolean DEFAULT true NOT NULL,
    CONSTRAINT PK__users PRIMARY KEY (_id)
);


CREATE TABLE _users_roles(
	_id bigint NOT NULL,
	_id_role bigint NOT NULL,
    _id_user bigint NOT NULL,
    CONSTRAINT PK__users_roles PRIMARY KEY (_id),
    CONSTRAINT IX__users_roles UNIQUE (_id_role, _id_user),
    CONSTRAINT FK__users_roles__roles FOREIGN KEY (_id_role) REFERENCES _roles (_id) ON DELETE CASCADE,
    CONSTRAINT FK__users_roles__users FOREIGN KEY (_id_user) REFERENCES _users (_id) ON DELETE CASCADE
);


CREATE TABLE _schemes(
	_id bigint NOT NULL,
	_id_parent bigint NULL,
	_name varchar(250) NOT NULL,
	_alias varchar(250) NULL,
	_name_space varchar(1000) NULL,
    CONSTRAINT PK__schemes PRIMARY KEY (_id),
	CONSTRAINT IX__schemes UNIQUE (_name),
    CONSTRAINT FK__schemes__schemes FOREIGN KEY (_id_parent) REFERENCES _schemes (_id) 
);


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
);

CREATE TABLE _dependencies(
	_id bigint NOT NULL,
	_id_scheme_1 bigint,
	_id_scheme_2 bigint NOT NULL,
    CONSTRAINT PK__dependencies PRIMARY KEY (_id),
	CONSTRAINT IX__dependencies UNIQUE (_id_scheme_1,_id_scheme_2),
    CONSTRAINT FK__dependencies__schemes_1 FOREIGN KEY (_id_scheme_1) REFERENCES _schemes (_id), 
    CONSTRAINT FK__dependencies__schemes_2 FOREIGN KEY (_id_scheme_2) REFERENCES _schemes (_id)  ON DELETE CASCADE
);

CREATE TABLE _objects(
	_id bigint NOT NULL,
	_id_parent bigint NULL,
	_id_scheme bigint NOT NULL,
	_id_owner bigint NOT NULL,
	_id_who_change bigint NOT NULL,
	_date_create timestamp DEFAULT now() NOT NULL,
	_date_modify timestamp DEFAULT now() NOT NULL,
	_date_begin timestamp NULL,
	_date_complete timestamp NULL,
	_key bigint NULL,
	_code_int bigint NULL,
	_code_string varchar(250) NULL,
	_code_guid uuid NULL,
	_name varchar(250) NULL,
	_note varchar(1000) NULL,
    _bool boolean NULL,
	_hash uuid NULL,
    CONSTRAINT PK__objects PRIMARY KEY (_id),
    CONSTRAINT FK__objects__objects FOREIGN KEY (_id_parent) REFERENCES _objects (_id) ON DELETE CASCADE,
    CONSTRAINT FK__objects__schemes FOREIGN KEY (_id_scheme) REFERENCES _schemes (_id) ON DELETE CASCADE, 
    CONSTRAINT FK__objects__users1 FOREIGN KEY (_id_owner) REFERENCES _users (_id),
	CONSTRAINT FK__objects__users2 FOREIGN KEY (_id_who_change) REFERENCES _users (_id)    
);

CREATE TABLE _deleted_objects(
	_id bigint NOT NULL,
	_id_parent bigint NULL,
	_id_scheme bigint NOT NULL,
	_id_owner bigint NOT NULL,
	_id_who_change bigint NOT NULL,
	_date_create timestamp DEFAULT now() NOT NULL,
	_date_modify timestamp DEFAULT now() NOT NULL,
	_date_begin timestamp NULL,
	_date_complete timestamp NULL,
	_key bigint NULL,
	_code_int bigint NULL,
	_code_string varchar(250) NULL,
	_code_guid uuid NULL,
	_name varchar(250) NULL,
	_note varchar(1000) NULL,
	_bool boolean NULL,
	_hash uuid NULL,
	_date_delete timestamp DEFAULT now() NOT NULL,
	_values text NULL,
    CONSTRAINT PK__deleted_objects PRIMARY KEY (_id)
);

CREATE TABLE _list_items(
	_id bigint NOT NULL,
	_id_list bigint NOT NULL,
	_value varchar(250) NULL,
	_id_object bigint NULL,
    CONSTRAINT PK__list_items PRIMARY KEY (_id),
    CONSTRAINT FK__list_items__id_list FOREIGN KEY (_id_list) REFERENCES _lists (_id) ON DELETE CASCADE,
    CONSTRAINT FK__list_items__objects FOREIGN KEY (_id_object) REFERENCES _objects (_id) ON DELETE SET NULL
);

CREATE TABLE _values(
	_id bigint NOT NULL,
	_id_structure bigint NOT NULL,
	_id_object bigint NOT NULL,
	_String text NULL,
	_Long bigint NULL,
	_Guid uuid NULL,
	_Double float NULL,
	_DateTime timestamp NULL,
	_Boolean boolean NULL,
	_ByteArray bytea NULL,
    _Array text, --if _is_array in structure then json array in this field
    CONSTRAINT PK__values PRIMARY KEY (_id),
    CONSTRAINT FK__values__objects FOREIGN KEY (_id_object) REFERENCES _objects (_id) ON DELETE CASCADE,
    CONSTRAINT FK__values__structures FOREIGN KEY (_id_structure) REFERENCES _structures (_id) ON DELETE CASCADE,
	CONSTRAINT IX__values_SO UNIQUE (_id_structure,_id_object)
); 


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
);

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
);


CREATE INDEX IF NOT EXISTS "IX__functions__schemes" ON _functions (_id_scheme) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__permissions__roles" ON _permissions (_id_role) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__permissions__users" ON _permissions (_id_user) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__permissions__ref" ON _permissions (_id_ref);
CREATE INDEX IF NOT EXISTS "IX__values__objects" ON _values (_id_object) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__values__structures" ON _values (_id_structure) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__values__String" ON _values (_String) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__values__Long" ON _values (_Long) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__values__Guid" ON _values (_Guid) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__values__Double" ON _values (_Double) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__values__DateTime" ON _values (_DateTime) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__values__Boolean" ON _values (_Boolean) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__list_items__id_list" ON _list_items (_id_list) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__list_items__objects" ON _list_items (_id_object) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__objects" ON _objects (_id_parent) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__schemes" ON _objects (_id_scheme) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__users1" ON _objects (_id_owner) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__users2" ON _objects (_id_who_change) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__date_create" ON _objects (_date_create) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__date_modify" ON _objects (_date_modify) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__code_int" ON _objects (_code_int) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__code_string" ON _objects (_code_string) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__name" ON _objects (_name) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__code_guid" ON _objects (_code_guid) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__objects__hash" ON _objects (_hash) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__dependencies__schemes_1" ON _dependencies (_id_scheme_1) WITH (deduplicate_items=True); 
CREATE INDEX IF NOT EXISTS "IX__dependencies__schemes_2" ON _dependencies (_id_scheme_2) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__structures__structures" ON _structures (_id_parent) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__structures__schemes" ON _structures (_id_scheme) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__structures__types" ON _structures (_id_type) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__structures__lists" ON _structures (_id_list) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__schemes__schemes" ON _schemes (_id_parent) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__users_roles__roles" ON _users_roles (_id_role) WITH (deduplicate_items=True);
CREATE INDEX IF NOT EXISTS "IX__users_roles__users" ON _users_roles (_id_user) WITH (deduplicate_items=True);

CREATE OR REPLACE FUNCTION public.get_scheme_definition(
    scheme_id bigint
) RETURNS json
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    result_json json;
    scheme_json json;
    structures_json json;
    scheme_exists boolean;
BEGIN
    -- Проверяем существование схемы
    SELECT EXISTS(SELECT 1 FROM _schemes WHERE _id = scheme_id) INTO scheme_exists;
    
    IF NOT scheme_exists THEN
        RETURN json_build_object('error', 'Scheme not found');
    END IF;
    
    -- Получаем информацию о схеме
    SELECT json_build_object(
        '_id', _s._id,
        '_id_parent', _s._id_parent,
        '_name', _s._name,
        '_alias', _s._alias,
        '_name_space', _s._name_space
    ) INTO scheme_json
    FROM _schemes _s
    WHERE _s._id = scheme_id;
    
    -- Получаем структуры (поля) схемы
    SELECT json_agg(
        json_build_object(
            '_id', _st._id,
            '_id_parent', _st._id_parent,
            '_name', _st._name,
            '_alias', _st._alias,
            '_order', _st._order,
            '_type_name', _t._name,
            '_type_db_type', _t._db_type,
            '_type_dotnet_type', _t._type,
            '_readonly', _st._readonly,
            '_allow_not_null', _st._allow_not_null,
            '_is_array', _st._is_array,
            '_is_compress', _st._is_compress,
            '_store_null', _st._store_null,
            '_id_list', _st._id_list,
            '_default_editor', _st._default_editor
        ) ORDER BY _st._order, _st._id
    ) INTO structures_json
    FROM _structures _st
    JOIN _types _t ON _t._id = _st._id_type
    WHERE _st._id_scheme = scheme_id;
    
    -- Формируем итоговый JSON
    result_json := json_build_object(
        'scheme', scheme_json,
        'structures', COALESCE(structures_json, '[]'::json)
    );
    
    RETURN result_json;
END;
$BODY$;

-- Представление для выборки определений всех схем
CREATE OR REPLACE VIEW v_schemes_definition AS
SELECT 
    _id as scheme_id,
    _name as scheme_name,
    _alias as scheme_alias,
    get_scheme_definition(_id) as scheme_definition
FROM _schemes
ORDER BY _id;



CREATE OR REPLACE FUNCTION public.ftr__objects__deleted_objects()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    values_json text;
BEGIN
    -- Собираем JSON всех связанных _values с метаданными структур
    SELECT COALESCE(
        json_strip_nulls(json_agg(
            json_build_object(
                '_id', _v._id,
                '_id_structure', _v._id_structure,
                '_id_object', _v._id_object,
                '_String', _v._String,
                '_Long', _v._Long,
                '_Guid', _v._Guid,
                '_Double', _v._Double,
                '_DateTime', _v._DateTime,
                '_Boolean', _v._Boolean,
                '_ByteArray', _v._ByteArray,
                '_Array', _v._Array,  -- Добавлено: критично для массивов!
                '_structure_name', _s._name,
                '_structure_type', _t._db_type,
                '_is_array', _s._is_array,
                '_store_null', _s._store_null
            )
        ))::text,
        '[]'  -- Пустой массив если нет _values
    )
    INTO values_json
    FROM _values _v 
    INNER JOIN _structures _s ON _s._id = _v._id_structure
    INNER JOIN _types _t ON _t._id = _s._id_type
    WHERE _v._id_object = OLD._id;

    -- Архивируем объект с улучшенными метаданными
    INSERT INTO _deleted_objects (
        _id, _id_parent, _id_scheme, _id_owner, _id_who_change,
        _date_create, _date_modify, _date_begin, _date_complete,
        _key, _code_int, _code_string, _code_guid,
        _name, _note, _bool, _hash,
        _date_delete, _values
    )
    VALUES (
        OLD._id, OLD._id_parent, OLD._id_scheme, OLD._id_owner, OLD._id_who_change,
        OLD._date_create, OLD._date_modify, OLD._date_begin, OLD._date_complete,
        OLD._key, OLD._code_int, OLD._code_string, OLD._code_guid,
        OLD._name, OLD._note, OLD._bool, OLD._hash,
        NOW(),  -- Точное время удаления
        values_json  -- JSON как text
    );

    RETURN OLD;
END;
$BODY$;

CREATE TRIGGER TR__objects__deleted_objects
BEFORE DELETE ON _objects 
FOR EACH ROW
EXECUTE PROCEDURE FTR__objects__deleted_objects();

CREATE SEQUENCE global_identity
 AS bigint
 START WITH 1000000
 INCREMENT BY 1
 MINVALUE 1000000
 MAXVALUE 9223372036854775807;

INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775709, 'Boolean', 'Boolean', 'boolean');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775708, 'DateTime', 'DateTime', 'DateTime');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775707, 'Double', 'Double', 'double');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775706, 'ListItem', 'Long', '_RListItem');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775705, 'Guid', 'Guid', 'Guid');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775704, 'Long', 'Long', 'long');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775703, 'Object', 'Long', '_RObject');
--INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775702, 'Text', 'Text', 'string');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775701, 'ByteArray', 'ByteArray', 'byte[]');
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775700, 'String', 'String', 'string');
-- Дополнительные простые типы для REDB
-- Эти типы можно добавить для расширения функциональности

-- 1. Дополнительные числовые типы
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775699, 'Int', 'Long', 'int');           -- int (маппится в Long)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775698, 'Short', 'Long', 'short');       -- short (маппится в Long)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775697, 'Byte', 'Long', 'byte');         -- byte (маппится в Long)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775696, 'Float', 'Double', 'float');     -- float (маппится в Double)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775695, 'Decimal', 'Double', 'decimal'); -- decimal (маппится в Double, с предупреждением о точности)

-- 2. Дополнительные строковые типы
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775694, 'Char', 'String', 'char');        -- char (маппится в String)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775693, 'Url', 'String', 'string');       -- URL (валидация через атрибуты)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775692, 'Email', 'String', 'string');     -- Email (валидация через атрибуты)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775691, 'Phone', 'String', 'string');     -- Phone (валидация через атрибуты)

-- 3. Специальные типы
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775690, 'Json', 'String', 'string');      -- JSON как строка
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775689, 'Xml', 'String', 'string');       -- XML как строка
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775688, 'Base64', 'String', 'string');    -- Base64 строки
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775687, 'Color', 'String', 'string');     -- Цвета (hex, rgb)

-- 4. Временные типы
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775686, 'DateOnly', 'DateTime', 'DateOnly');     -- .NET 6+ DateOnly
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775685, 'TimeOnly', 'String', 'TimeOnly');      -- .NET 6+ TimeOnly (как строка)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775684, 'TimeSpan', 'String', 'TimeSpan');      -- TimeSpan (как строка)

-- 5. Enum поддержка
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775683, 'Enum', 'String', 'Enum');        -- Enum (сохраняется как строка)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775682, 'EnumInt', 'Long', 'Enum');       -- Enum (сохраняется как число)

-- 6. Географические типы (для будущего расширения)
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775681, 'Latitude', 'Double', 'double');   -- Широта
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775680, 'Longitude', 'Double', 'double');  -- Долгота
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775679, 'GeoPoint', 'String', 'string');   -- Географическая точка как JSON

-- 7. Файловые типы
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775678, 'FilePath', 'String', 'string');   -- Путь к файлу
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775677, 'FileName', 'String', 'string');   -- Имя файла
INSERT INTO _types (_id, _name, _db_type, _type) VALUES (-9223372036854775676, 'MimeType', 'String', 'string');   -- MIME тип

-- Комментарии к дополнительным типам
COMMENT ON TABLE _types IS 'Таблица типов данных REDB. Поддерживает базовые типы C# и дополнительные специализированные типы с валидацией';

-- Примеры использования:
-- 1. Int, Short, Byte - все маппятся в Long для упрощения
-- 2. Float, Decimal - маппятся в Double (с предупреждением о потере точности для Decimal)
-- 3. Char - маппится в String
-- 4. Url, Email, Phone - строки с дополнительной валидацией на уровне приложения
-- 5. Json, Xml - строки для хранения структурированных данных
-- 6. DateOnly, TimeOnly, TimeSpan - специальные временные типы .NET 6+
-- 7. Enum - поддержка перечислений (как строка или число)
-- 8. Географические типы - для location-based приложений
-- 9. Файловые типы - для работы с файлами и медиа

INSERT INTO _users (_id, _login, _password, _name, _phone, _email, _date_register, _date_dismiss, _enabled) VALUES (0, 'sys', '', 'sys', NULL, NULL, CAST('2023-12-26T01:14:34.410' AS TimeStamp), NULL, true);
INSERT INTO _users (_id, _login, _password, _name, _phone, _email, _date_register, _date_dismiss, _enabled) VALUES (1, 'admin', '', 'admin', NULL, NULL, CAST('2023-12-26T01:14:34.410' AS TimeStamp), NULL, true);

-- for webApi dashboard
INSERT into _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (-9223372036854769999, NULL, 'redb.WebApp', 'redB Dashboard', NULL);
INSERT into _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (-9223372036854769998, -9223372036854769999, 'redb.WebApp.Sidebar.Group', 'sidebar Group', NULL);
INSERT into _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (-9223372036854769997, -9223372036854769998, 'redb.WebApp.Sidebar.Item', 'sidebar Item', NULL);
INSERT into _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (-9223372036854769996, NULL, -9223372036854769997, NULL, -9223372036854775700, NULL, 'name', 'redb.WebApp.Sidebar.Item.name', NULL, true, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (-9223372036854769995, NULL, -9223372036854769997, NULL, -9223372036854775700, NULL, 'path', 'redb.WebApp.Sidebar.Item.path', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769994, NULL, -9223372036854769999, 1, 1, cast('2024-02-06T14:48:18.847' as timestamp), cast('2024-02-06T14:48:18.847' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769944, -9223372036854769994, -9223372036854769998, 1, 1, cast('2024-02-06T19:44:34.133' as timestamp), cast('2024-02-06T19:44:34.133' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769993, -9223372036854769944, -9223372036854769998, 1, 1, cast('2024-02-06T14:50:53.753' as timestamp), cast('2024-02-06T14:50:53.753' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769945, -9223372036854769944, -9223372036854769998, 1, 1, cast('2024-02-06T19:42:54.410' as timestamp), cast('2024-02-06T19:42:54.410' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769992, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T14:57:30.190' as timestamp), cast('2024-02-06T14:57:30.190' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Dependencies', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769991, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Lists', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769990, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Objects', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769989, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Permissions', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769988, -9223372036854769945, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings.Roles', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769987, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Schemes', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769986, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Structures', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769985, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Types', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769984, -9223372036854769945, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings.Users', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769983, -9223372036854769945, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.Global_Settings.Users_roles', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769982, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Values', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769981, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Deleted_objects', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769980, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Functions', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769979, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.Links', NULL, NULL);
INSERT into _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) VALUES (-9223372036854769978, -9223372036854769993, -9223372036854769997, 1, 1, cast('2024-02-06T15:20:32.563' as timestamp), cast('2024-02-06T15:20:32.563' as timestamp), NULL, NULL, NULL, NULL, NULL, NULL, 'WebApp.Sidebar.General.List_items', NULL, NULL);
INSERT into _dependencies (_id, _id_scheme_1, _id_scheme_2) VALUES (-9223372036854769947, -9223372036854769999, -9223372036854769998);
INSERT into _dependencies (_id, _id_scheme_1, _id_scheme_2) VALUES (-9223372036854769946, -9223372036854769998, -9223372036854769997);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769977, -9223372036854769996, -9223372036854769992, 'Dependencies', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769976, -9223372036854769995, -9223372036854769992, 'M 4 19 h 6 v -2 H 4 v 2 Z M 20 5 H 4 v 2 h 16 V 5 Z m -3 6 H 4 v 2 h 13.25 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 H 15 v -2 l -3 3 l 3 3 v -2 h 2 c 2.21 0 4 -1.79 4 -4 s -1.79 -4 -4 -4 Z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769975, -9223372036854769996, -9223372036854769981, 'Deleted objects', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769974, -9223372036854769995, -9223372036854769981, 'm 0 0 c -0.5626 0.6404 -0.491 1.6212 0.1116 2.2238 l 6.841 6.8358 l -6.8724 6.8674 c -0.6184 0.6186 -0.6184 1.6216 0 2.2402 l 0.0226 0.0226 c 0.6186 0.6186 1.6214 0.6186 2.2402 -0 l 6.8732 -6.8682 l 6.8732 6.868 c 0.6186 0.6186 1.6216 0.6186 2.2402 -0 l 0.0226 -0.0226 c 0.6186 -0.6186 0.6186 -1.6214 -0 -2.2402 l -6.8726 -6.8674 l 6.841 -6.8358 c 0.6026 -0.6026 0.674 -1.5834 0.1116 -2.2238 c -0.6162 -0.7014 -1.6858 -0.7274 -2.3356 -0.0776 l -6.8806 6.8758 l -6.8808 -6.8754 c -0.6498 -0.65 -1.7192 -0.624 -2.3354 0.0776 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769973, -9223372036854769996, -9223372036854769980, 'Functions', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769972, -9223372036854769995, -9223372036854769980,  'm 11.344 5.71 c 0 -0.73 0.074 -1.122 1.199 -1.122 h 1.502 v -2.717 h -2.404 c -2.886 0 -3.903 1.36 -3.903 3.646 v 1.765 h -1.8 v 2.718 h 1.8 v 8.128 h 3.601 v -8.128 h 2.403 l 0.32 -2.718 h -2.724 l 0.006 -1.572 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769971, -9223372036854769996, -9223372036854769979, 'Links', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769970, -9223372036854769995, -9223372036854769979, 'm 16.469 8.924 l -2.414 2.413 c -0.156 0.156 -0.408 0.156 -0.564 0 c -0.156 -0.155 -0.156 -0.408 0 -0.563 l 2.414 -2.414 c 1.175 -1.175 1.175 -3.087 0 -4.262 c -0.57 -0.569 -1.326 -0.883 -2.132 -0.883 s -1.562 0.313 -2.132 0.883 l -2.414 2.413 c -1.175 1.175 -1.175 3.087 0 4.263 c 0.288 0.288 0.624 0.511 0.997 0.662 c 0.204 0.083 0.303 0.315 0.22 0.52 c -0.171 0.422 -0.643 0.17 -0.52 0.22 c -0.473 -0.191 -0.898 -0.474 -1.262 -0.838 c -1.487 -1.485 -1.487 -3.904 0 -5.391 l 2.414 -2.413 c 0.72 -0.72 1.678 -1.117 2.696 -1.117 s 1.976 0.396 2.696 1.117 c 1.487 1.486 1.487 3.904 0.001 5.39 m -6.393 -1.099 c -0.205 -0.083 -0.437 0.016 -0.52 0.22 c -0.083 0.205 0.016 0.437 0.22 0.52 c 0.374 0.151 0.709 0.374 0.997 0.662 c 1.176 1.176 1.176 3.088 0 4.263 l -2.414 2.413 c -0.569 0.569 -1.326 0.883 -2.131 0.883 s -1.562 -0.313 -2.132 -0.883 c -1.175 -1.175 -1.175 -3.087 0 -4.262 l 2.414 -2.414 c 0.156 -0.155 0.156 -0.408 0 -0.564 c -0.156 -0.156 -0.408 -0.156 -0.564 0 l -2.414 2.414 c -1.487 1.485 -1.487 3.904 0 5.391 c 0.72 0.72 1.678 1.116 2.696 1.116 s 1.976 -0.396 2.696 -1.116 l 2.414 -2.413 c 1.487 -1.486 1.487 -3.905 0 -5.392 c -0.364 -0.365 -0.788 -0.646 -1.262 -0.838', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769969, -9223372036854769996, -9223372036854769978, 'List items', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769968, -9223372036854769995, -9223372036854769978, 'm 2.25 12.584 c -0.713 0 -1.292 0.578 -1.292 1.291 s 0.579 1.291 1.292 1.291 c 0.713 0 1.292 -0.578 1.292 -1.291 s -0.579 -1.291 -1.292 -1.291 z m 0 1.723 c -0.238 0 -0.43 -0.193 -0.43 -0.432 s 0.192 -0.432 0.43 -0.432 c 0.238 0 0.431 0.193 0.431 0.432 s -0.193 0.432 -0.431 0.432 z m 3.444 -7.752 h 12.916 c 0.237 0 0.431 -0.191 0.431 -0.43 s -0.193 -0.431 -0.431 -0.431 h -12.916 c -0.238 0 -0.43 0.192 -0.43 0.431 s 0.193 0.43 0.43 0.43 z m -3.444 2.153 c -0.713 0 -1.292 0.578 -1.292 1.291 c 0 0.715 0.579 1.292 1.292 1.292 c 0.713 0 1.292 -0.577 1.292 -1.292 c 0 -0.712 -0.579 -1.291 -1.292 -1.291 z m 0 1.722 c -0.238 0 -0.43 -0.192 -0.43 -0.431 c 0 -0.237 0.192 -0.43 0.43 -0.43 c 0.238 0 0.431 0.192 0.431 0.43 c 0 0.239 -0.193 0.431 -0.431 0.431 z m 16.36 -0.86 h -12.916 c -0.238 0 -0.43 0.192 -0.43 0.43 c 0 0.238 0.192 0.431 0.43 0.431 h 12.916 c 0.237 0 0.431 -0.192 0.431 -0.431 c 0 -0.238 -0.193 -0.43 -0.431 -0.43 z m 0 3.873 h -12.916 c -0.238 0 -0.43 0.193 -0.43 0.432 s 0.192 0.432 0.43 0.432 h 12.916 c 0.237 0 0.431 -0.193 0.431 -0.432 s -0.193 -0.432 -0.431 -0.432 z m -16.36 -8.61 c -0.713 0 -1.292 0.578 -1.292 1.292 c 0 0.713 0.579 1.291 1.292 1.291 c 0.713 0 1.292 -0.578 1.292 -1.291 c 0 -0.713 -0.579 -1.292 -1.292 -1.292 z m 0 1.722 c -0.238 0 -0.43 -0.191 -0.43 -0.43 s 0.192 -0.431 0.43 -0.431 c 0.238 0 0.431 0.192 0.431 0.431 s -0.193 0.43 -0.431 0.43 z', NULL, NULL, NULL, NULL, NULL, NULL );
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769967, -9223372036854769996, -9223372036854769991, 'Lists', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769966, -9223372036854769995, -9223372036854769991, 'm 14 2 h -8 c -1.1 0 -1.99 0.9 -1.99 2 l -0.01 16 c 0 1.1 0.89 2 1.99 2 h 12.01 c 1.1 0 2 -0.9 2 -2 v -12 l -6 -6 z m 2 16 h -8 v -2 h 8 v 2 z m 0 -4 h -8 v -2 h 8 v 2 z m -3 -5 v -5.5 l 5.5 5.5 h -5.5 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769965, -9223372036854769996, -9223372036854769990, 'Objects', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769964, -9223372036854769995, -9223372036854769990, 'M 4 8 h 4 V 4 H 4 v 4 Z m 6 12 h 4 v -4 h -4 v 4 Z m -6 0 h 4 v -4 H 4 v 4 Z m 0 -6 h 4 v -4 H 4 v 4 Z m 6 0 h 4 v -4 h -4 v 4 Z m 6 -10 v 4 h 4 V 4 h -4 Z m -6 4 h 4 V 4 h -4 v 4 Z m 6 6 h 4 v -4 h -4 v 4 Z m 0 6 h 4 v -4 h -4 v 4 Z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769963, -9223372036854769996, -9223372036854769989, 'Permissions', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769962, -9223372036854769995, -9223372036854769989, 'm 12.443 9.672 c 0.203 -0.496 0.329 -1.052 0.329 -1.652 c 0 -1.969 -1.241 -3.565 -2.772 -3.565 s -2.772 1.596 -2.772 3.565 c 0 0.599 0.126 1.156 0.33 1.652 c -1.379 0.555 -2.31 1.553 -2.31 2.704 c 0 1.75 2.128 3.169 4.753 3.169 c 2.624 0 4.753 -1.419 4.753 -3.169 c -0.001 -1.151 -0.933 -2.149 -2.311 -2.704 z m -2.443 -4.425 c 1.094 0 1.98 1.242 1.98 2.773 c 0 1.531 -0.887 2.772 -1.98 2.772 s -1.98 -1.241 -1.98 -2.772 c 0 -1.531 0.886 -2.773 1.98 -2.773 z m 0 9.506 c -2.187 0 -3.96 -1.063 -3.96 -2.377 c 0 -0.854 0.757 -1.596 1.885 -2.015 c 0.508 0.745 1.245 1.224 2.076 1.224 s 1.567 -0.479 2.076 -1.224 c 1.127 0.418 1.885 1.162 1.885 2.015 c -0.001 1.313 -1.774 2.377 -3.962 2.377 z m 0 -13.862 c -5.031 0 -9.109 4.079 -9.109 9.109 c 0 5.031 4.079 9.109 9.109 9.109 c 5.031 0 9.109 -4.078 9.109 -9.109 c 0 -5.031 -4.078 -9.109 -9.109 -9.109 z m 0 17.426 c -4.593 0 -8.317 -3.725 -8.317 -8.317 c 0 -4.593 3.724 -8.317 8.317 -8.317 c 4.593 0 8.317 3.724 8.317 8.317 c 0 4.593 -3.724 8.317 -8.317 8.317 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769961, -9223372036854769996, -9223372036854769988, 'Roles', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769960, -9223372036854769995, -9223372036854769988, 'm 12 2 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 s -2 -0.9 -2 -2 s 0.9 -2 2 -2 z m 9 7 h -6 v 13 h -2 v -6 h -2 v 6 h -2 v -13 h -6 v -2 h 18 v 2 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769959, -9223372036854769996, -9223372036854769987, 'Schemes', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769958, -9223372036854769995, -9223372036854769987, 'm 18.378 1.062 h -14.523 c -0.309 0 -0.559 0.25 -0.559 0.559 c 0 0.309 0.25 0.559 0.559 0.559 h 13.964 v 13.964 c 0 0.309 0.25 0.559 0.559 0.559 c 0.31 0 0.56 -0.25 0.56 -0.559 v -14.523 c 0 -0.309 -0.25 -0.559 -0.56 -0.559 z m -2.234 2.234 h -14.523 c -0.309 0 -0.559 0.25 -0.559 0.559 v 14.523 c 0 0.31 0.25 0.56 0.559 0.56 h 14.523 c 0.309 0 0.559 -0.25 0.559 -0.56 v -14.523 c -0.001 -0.309 -0.251 -0.559 -0.559 -0.559 z m -0.558 13.966 c 0 0.31 -0.25 0.558 -0.56 0.558 h -12.288 c -0.309 0 -0.559 -0.248 -0.559 -0.558 v -12.29 c 0 -0.309 0.25 -0.559 0.559 -0.559 h 12.289 c 0.31 0 0.56 0.25 0.56 0.559 v 12.29 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769957, -9223372036854769996, -9223372036854769986, 'Structures', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769956, -9223372036854769995, -9223372036854769986, 'm 5.029 1.734 h 10.935 c 0.317 0 0.575 -0.257 0.575 -0.575 s -0.258 -0.576 -0.575 -0.576 h -10.935 c -0.318 0 -0.576 0.258 -0.576 0.576 s 0.258 0.575 0.576 0.575 z m -4.029 3.454 v 13.812 h 18.417 v -13.812 h -18.417 z m 17.266 12.661 h -16.115 v -11.511 h 16.115 v 11.511 z m -15.539 -13.813 h 14.963 c 0.317 0 0.575 -0.257 0.575 -0.576 c 0 -0.318 -0.258 -0.575 -0.575 -0.575 h -14.963 c -0.318 0 -0.576 0.257 -0.576 0.575 c 0 0.319 0.258 0.576 0.576 0.576 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769955, -9223372036854769996, -9223372036854769985, 'Types', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769954, -9223372036854769995, -9223372036854769985, 'm 14.68 12.621 c -0.9 0 -1.702 0.43 -2.216 1.09 l -4.549 -2.637 c 0.284 -0.691 0.284 -1.457 0 -2.146 l 4.549 -2.638 c 0.514 0.661 1.315 1.09 2.216 1.09 c 1.549 0 2.809 -1.26 2.809 -2.808 c 0 -1.548 -1.26 -2.809 -2.809 -2.809 c -1.548 0 -2.808 1.26 -2.808 2.809 c 0 0.38 0.076 0.741 0.214 1.073 l -4.55 2.638 c -0.515 -0.661 -1.316 -1.09 -2.217 -1.09 c -1.548 0 -2.808 1.26 -2.808 2.809 s 1.26 2.808 2.808 2.808 c 0.9 0 1.702 -0.43 2.217 -1.09 l 4.55 2.637 c -0.138 0.332 -0.214 0.693 -0.214 1.074 c 0 1.549 1.26 2.809 2.808 2.809 c 1.549 0 2.809 -1.26 2.809 -2.809 s -1.26 -2.81 -2.809 -2.81 m 0 -10.109 c 1.136 0 2.06 0.923 2.06 2.06 s -0.925 2.058 -2.06 2.058 s -2.059 -0.923 -2.059 -2.059 s 0.923 -2.059 2.059 -2.059 m -9.361 9.549 c -1.136 0 -2.06 -0.924 -2.06 -2.06 s 0.923 -2.059 2.06 -2.059 c 1.135 0 2.06 0.923 2.06 2.059 s -0.925 2.06 -2.06 2.06 m 9.361 5.427 c -1.136 0 -2.059 -0.922 -2.059 -2.059 s 0.923 -2.061 2.059 -2.061 s 2.06 0.924 2.06 2.061 s -0.925 2.059 -2.06 2.059 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769953, -9223372036854769996, -9223372036854769984, 'Users', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769952, -9223372036854769995, -9223372036854769984, 'm 16 11 c 1.66 0 2.99 -1.34 2.99 -3 s -1.33 -3 -2.99 -3 c -1.66 0 -3 1.34 -3 3 s 1.34 3 3 3 z m -8 0 c 1.66 0 2.99 -1.34 2.99 -3 s -1.33 -3 -2.99 -3 c -1.66 0 -3 1.34 -3 3 s 1.34 3 3 3 z m 0 2 c -2.33 0 -7 1.17 -7 3.5 v 2.5 h 14 v -2.5 c 0 -2.33 -4.67 -3.5 -7 -3.5 z m 8 0 c -0.29 0 -0.62 0.02 -0.97 0.05 c 1.16 0.84 1.97 1.97 1.97 3.45 v 2.5 h 6 v -2.5 c 0 -2.33 -4.67 -3.5 -7 -3.5 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769951, -9223372036854769996, -9223372036854769983, 'Users roles', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769950, -9223372036854769995, -9223372036854769983, 'm 12 2 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 s -2 -0.9 -2 -2 s 0.9 -2 2 -2 z m 9 7 h -6 v 13 h -2 v -6 h -2 v 6 h -2 v -13 h -6 v -2 h 18 v 2 z m -3 3 q 1 10 3 0 z m -12 0 q 2 2 -3 0 z m -2 10 q 4 -8 0 -7 z', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769949, -9223372036854769996, -9223372036854769982, 'Values', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT into _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (-9223372036854769948, -9223372036854769995, -9223372036854769982, 'm 15.396 2.292 h -10.792 c -0.212 0 -0.385 0.174 -0.385 0.386 v 14.646 c 0 0.212 0.173 0.385 0.385 0.385 h 10.792 c 0.211 0 0.385 -0.173 0.385 -0.385 v -14.647 c 0 -0.212 -0.174 -0.385 -0.385 -0.385 m -0.386 14.646 h -10.02 v -2.698 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.519 1.156 -1.156 s -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.321 -1.089 0.771 h -1.609 v -3.083 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.518 1.156 -1.156 c 0 -0.638 -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.322 -1.089 0.771 h -1.609 v -3.086 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.519 1.156 -1.156 c 0 -0.638 -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.322 -1.089 0.771 h -1.609 v -2.699 h 10.02 v 13.876 z m -7.708 -3.084 c 0 -0.212 0.173 -0.386 0.385 -0.386 s 0.385 0.174 0.385 0.386 s -0.173 0.385 -0.385 0.385 s -0.385 -0.173 -0.385 -0.385 m 0 -3.854 c 0 -0.212 0.173 -0.385 0.385 -0.385 s 0.386 0.173 0.386 0.385 s -0.173 0.385 -0.385 0.385 s -0.386 -0.173 -0.386 -0.385 m 0 -3.854 c 0 -0.212 0.173 -0.386 0.385 -0.386 s 0.385 0.174 0.385 0.386 s -0.173 0.385 -0.384 0.385 s -0.386 -0.173 -0.386 -0.385', NULL, NULL, NULL, NULL, NULL, NULL);

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

-- Триггер для валидации имен полей в _structures
CREATE OR REPLACE FUNCTION validate_structure_name()
RETURNS TRIGGER AS $$
DECLARE
    reserved_fields text[] := ARRAY[
        '_id', '_id_parent', '_id_scheme', '_id_owner', '_id_who_change',
        '_date_create', '_date_modify', '_date_begin', '_date_complete',
        '_key', '_code_int', '_code_string', '_code_guid', '_name', '_note', '_bool', '_hash'
    ];
    field_name text;
BEGIN
    -- Проверяем только при INSERT и UPDATE поля _name
    IF TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND OLD._name IS DISTINCT FROM NEW._name) THEN
        field_name := LOWER(NEW._name);
        
        -- Проверка 1: Имя не должно совпадать с зарезервированными полями _objects
        IF field_name = ANY(reserved_fields) THEN
            RAISE EXCEPTION 'Имя поля "_name" не может совпадать с системными полями объектов: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Используйте другое имя, избегайте: ' || array_to_string(reserved_fields, ', ');
        END IF;
        
        -- Проверка 2: Имя не должно начинаться с цифры
        IF NEW._name ~ '^[0-9]' THEN
            RAISE EXCEPTION 'Имя поля "_name" не может начинаться с цифры: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Имя поля должно начинаться с буквы или символа подчеркивания';
        END IF;
        
        -- Проверка 3: Имя должно соответствовать правилам именования C# (буквы, цифры, подчеркивания)
        IF NEW._name !~ '^[a-zA-Z_][a-zA-Z0-9_]*$' THEN
            RAISE EXCEPTION 'Имя поля "_name" содержит недопустимые символы: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Имя может содержать только латинские буквы, цифры и символы подчеркивания. Должно начинаться с буквы или подчеркивания';
        END IF;
        
        -- Проверка 4: Имя не должно быть пустым или содержать только пробелы
        IF LENGTH(TRIM(NEW._name)) = 0 THEN
            RAISE EXCEPTION 'Имя поля "_name" не может быть пустым'
                USING ERRCODE = '23514';
        END IF;
        
        -- Проверка 5: Максимальная длина имени (разумное ограничение)
        IF LENGTH(NEW._name) > 64 THEN
            RAISE EXCEPTION 'Имя поля "_name" слишком длинное (максимум 64 символа): %', NEW._name
                USING ERRCODE = '23514';
        END IF;
        
        -- Проверка 6: Дополнительные зарезервированные слова C#
        IF LOWER(NEW._name) = ANY(ARRAY[
            'abstract', 'as', 'bool', 'break', 'byte', 'case', 'catch', 'char', 'checked',
            'class', 'const', 'continue', 'decimal', 'default', 'delegate', 'do', 'double', 'else',
            'enum', 'event', 'explicit', 'extern', 'false', 'finally', 'fixed', 'float', 'for',
            'foreach', 'goto', 'if', 'implicit', 'in', 'int', 'interface', 'internal', 'is', 'lock',
            'long', 'namespace', 'new', 'null', 'object', 'operator', 'out', 'override', 'params',
            'private', 'protected', 'public', 'readonly', 'ref', 'return', 'sbyte', 'sealed',
            'short', 'sizeof', 'stackalloc', 'static', 'string', 'struct', 'switch', 'this',
            'throw', 'true', 'try', 'typeof', 'uint', 'ulong', 'unchecked', 'unsafe', 'ushort',
            'using', 'virtual', 'void', 'volatile', 'while'
        ]) THEN
            RAISE EXCEPTION 'Имя поля "_name" является зарезервированным словом C#: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Используйте другое имя или добавьте префикс/суффикс';
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Создание триггера
CREATE TRIGGER tr_validate_structure_name
    BEFORE INSERT OR UPDATE ON _structures
    FOR EACH ROW
    EXECUTE FUNCTION validate_structure_name();

-- Комментарии к триггеру
COMMENT ON FUNCTION validate_structure_name() IS 'Функция валидации имен полей в _structures согласно правилам именования C#';
COMMENT ON TRIGGER tr_validate_structure_name ON _structures IS 'Триггер проверяет корректность имен полей: запрещает системные имена, имена начинающиеся с цифры, спецсимволы и зарезервированные слова C#';

-- Триггер для валидации имен схем в _schemes
CREATE OR REPLACE FUNCTION validate_scheme_name()
RETURNS TRIGGER AS $$
DECLARE
    scheme_name text;
BEGIN
    -- Проверяем только при INSERT и UPDATE поля _name
    IF TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND OLD._name IS DISTINCT FROM NEW._name) THEN
        scheme_name := LOWER(NEW._name);
        
        -- Проверка 1: Имя не должно начинаться с цифры
        IF NEW._name ~ '^[0-9]' THEN
            RAISE EXCEPTION 'Имя схемы "_name" не может начинаться с цифры: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Имя схемы должно начинаться с буквы или символа подчеркивания';
        END IF;
        
        -- Проверка 2: Имя должно соответствовать правилам именования классов C# (буквы, цифры, подчеркивания, точки для namespace)
        IF NEW._name !~ '^[a-zA-Z_][a-zA-Z0-9_.]*$' THEN
            RAISE EXCEPTION 'Имя схемы "_name" содержит недопустимые символы: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Имя может содержать только латинские буквы, цифры, символы подчеркивания и точки. Должно начинаться с буквы или подчеркивания';
        END IF;
        
        -- Проверка 3: Имя не должно заканчиваться точкой
        IF NEW._name ~ '\.$' THEN
            RAISE EXCEPTION 'Имя схемы "_name" не может заканчиваться точкой: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Удалите точку в конце имени';
        END IF;
        
        -- Проверка 4: Имя не должно содержать две точки подряд
        IF NEW._name ~ '\.\.' THEN
            RAISE EXCEPTION 'Имя схемы "_name" не может содержать две точки подряд: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Используйте одну точку для разделения частей namespace';
        END IF;
        
        -- Проверка 5: Имя не должно быть пустым или содержать только пробелы
        IF LENGTH(TRIM(NEW._name)) = 0 THEN
            RAISE EXCEPTION 'Имя схемы "_name" не может быть пустым'
                USING ERRCODE = '23514';
        END IF;
        
        -- Проверка 6: Максимальная длина имени (разумное ограничение)
        IF LENGTH(NEW._name) > 128 THEN
            RAISE EXCEPTION 'Имя схемы "_name" слишком длинное (максимум 128 символов): %', NEW._name
                USING ERRCODE = '23514';
        END IF;
        
        -- Проверка 7: Зарезервированные слова C# (проверяем каждую часть после разделения по точкам)
        IF EXISTS (
            SELECT 1 FROM unnest(string_to_array(LOWER(NEW._name), '.')) AS part
            WHERE part = ANY(ARRAY[
                'abstract', 'as', 'bool', 'break', 'byte', 'case', 'catch', 'char', 'checked',
                'class', 'const', 'continue', 'decimal', 'default', 'delegate', 'do', 'double', 'else',
                'enum', 'event', 'explicit', 'extern', 'false', 'finally', 'fixed', 'float', 'for',
                'foreach', 'goto', 'if', 'implicit', 'in', 'int', 'interface', 'internal', 'is', 'lock',
                'long', 'namespace', 'new', 'null', 'object', 'operator', 'out', 'override', 'params',
                'private', 'protected', 'public', 'readonly', 'ref', 'return', 'sbyte', 'sealed',
                'short', 'sizeof', 'stackalloc', 'static', 'string', 'struct', 'switch', 'this',
                'throw', 'true', 'try', 'typeof', 'uint', 'ulong', 'unchecked', 'unsafe', 'ushort',
                'using', 'virtual', 'void', 'volatile', 'while'
            ])
        ) THEN
            RAISE EXCEPTION 'Имя схемы "_name" содержит зарезервированное слово C#: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Используйте другое имя или добавьте префикс/суффикс к проблемной части';
        END IF;
        
        -- Проверка 8: Каждая часть (разделенная точками) должна быть валидным идентификатором
        IF EXISTS (
            SELECT 1 FROM unnest(string_to_array(NEW._name, '.')) AS part
            WHERE LENGTH(TRIM(part)) = 0 OR part ~ '^[0-9]' OR part !~ '^[a-zA-Z_][a-zA-Z0-9_]*$'
        ) THEN
            RAISE EXCEPTION 'Имя схемы "_name" содержит невалидную часть namespace: %', NEW._name
                USING ERRCODE = '23514',
                      HINT = 'Каждая часть между точками должна быть валидным идентификатором C#';
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Создание триггера для схем
CREATE TRIGGER tr_validate_scheme_name
    BEFORE INSERT OR UPDATE ON _schemes
    FOR EACH ROW
    EXECUTE FUNCTION validate_scheme_name();

-- Комментарии к триггеру схем
COMMENT ON FUNCTION validate_scheme_name() IS 'Функция валидации имен схем в _schemes согласно правилам именования классов и namespace C#';
COMMENT ON TRIGGER tr_validate_scheme_name ON _schemes IS 'Триггер проверяет корректность имен схем: правила именования классов C#, поддержка namespace через точки, зарезервированные слова';

-- Функция для построения фасетов (фасетный поиск)
CREATE OR REPLACE FUNCTION get_facets(scheme_id bigint)
RETURNS jsonb 
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
    RETURN (
        SELECT jsonb_object_agg(s._name, COALESCE(facet_values, '[]'::jsonb))
        FROM _structures s
        LEFT JOIN (
            SELECT 
                v._id_structure, 
                jsonb_agg(DISTINCT 
                    CASE 
                        -- Если это массив - работаем с _Array поле
                        WHEN st._is_array = true AND v._Array IS NOT NULL THEN
                            -- Для массивов возвращаем сам массив (фасеты покажут все элементы)
                            CASE 
                                WHEN t._name = 'Object' THEN 
                                    -- Для массива объектов создаем массив {id, name} из ID
                                    (SELECT jsonb_agg(
                                        jsonb_build_object(
                                            'id', elem::text::bigint,
                                            'name', (SELECT _name FROM _objects WHERE _id = elem::text::bigint)
                                        )
                                    ) FROM jsonb_array_elements(v._Array::jsonb) AS elem
                                     WHERE jsonb_typeof(elem) = 'number')
                                ELSE 
                                    -- Для массива примитивов возвращаем как есть
                                    v._Array::jsonb
                            END
                        -- Обычные поля (не массивы)
                        WHEN t._db_type = 'String' THEN to_jsonb(v._String)
                        WHEN t._db_type = 'Long' THEN to_jsonb(v._Long)
                        WHEN t._db_type = 'Guid' THEN to_jsonb(v._Guid)
                        WHEN t._db_type = 'Double' THEN to_jsonb(v._Double)
                        WHEN t._db_type = 'DateTime' THEN to_jsonb(v._DateTime)
                        WHEN t._db_type = 'Boolean' THEN to_jsonb(v._Boolean)
                        WHEN t._db_type = 'Object' THEN 
                            CASE 
                                WHEN v._Long IS NOT NULL THEN 
                                    jsonb_build_object(
                                        'id', v._Long,
                                        'name', (SELECT _name FROM _objects WHERE _id = v._Long)
                                    )
                                ELSE NULL
                            END
                        WHEN t._db_type = 'ListItem' THEN
                            CASE 
                                WHEN v._Long IS NOT NULL THEN 
                                    jsonb_build_object(
                                        'id', v._Long,
                                        'value', (SELECT _value FROM _list_items WHERE _id = v._Long)
                                    )
                                ELSE NULL
                            END
                        WHEN t._db_type = 'ByteArray' THEN 
                            CASE 
                                WHEN v._ByteArray IS NOT NULL THEN 
                                    to_jsonb(encode(v._ByteArray, 'base64'))
                                ELSE NULL
                            END
                        ELSE to_jsonb(v._String)
                    END
                ) FILTER (WHERE 
                    CASE 
                        WHEN st._is_array = true THEN v._Array IS NOT NULL
                        WHEN t._db_type = 'String' THEN v._String IS NOT NULL
                        WHEN t._db_type = 'Long' THEN v._Long IS NOT NULL
                        WHEN t._db_type = 'Guid' THEN v._Guid IS NOT NULL
                        WHEN t._db_type = 'Double' THEN v._Double IS NOT NULL
                        WHEN t._db_type = 'DateTime' THEN v._DateTime IS NOT NULL
                        WHEN t._db_type = 'Boolean' THEN v._Boolean IS NOT NULL
                        WHEN t._db_type = 'Object' THEN v._Long IS NOT NULL
                        WHEN t._db_type = 'ListItem' THEN v._Long IS NOT NULL
                        WHEN t._db_type = 'ByteArray' THEN v._ByteArray IS NOT NULL
                        ELSE FALSE
                    END
                ) as facet_values
            FROM _values v
            JOIN _objects o ON o._id = v._id_object
            JOIN _structures st ON st._id = v._id_structure
            JOIN _types t ON t._id = st._id_type
            WHERE o._id_scheme = scheme_id
              AND o._id NOT IN (SELECT _id FROM _deleted_objects) -- Исключаем удаленные объекты
            GROUP BY v._id_structure
        ) f ON f._id_structure = s._id
        WHERE s._id_scheme = scheme_id
    );
END;
$BODY$;

-- Комментарий к функции фасетов
COMMENT ON FUNCTION get_facets(bigint) IS 'Функция построения фасетов для фасетного поиска. Возвращает все уникальные значения полей для объектов указанной схемы в формате JSON {field_name: [values]}. Поддержка массивов из поля _Array. Массивы объектов как [1,2,3] преобразуются в [{"id":1,"name":"..."}]';

-- Функция для получения объекта в JSON формате (улучшенная версия)
CREATE OR REPLACE FUNCTION get_object_json(
    object_id bigint,
    max_depth integer DEFAULT 10
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    result_json jsonb;
    object_exists boolean;
    base_info jsonb;
    properties_info jsonb;
BEGIN
    -- Проверяем глубину рекурсии
    IF max_depth <= 0 THEN
        RETURN jsonb_build_object('error', 'Max recursion depth reached');
    END IF;
    
    -- Проверяем существование объекта
    SELECT EXISTS(SELECT 1 FROM _objects WHERE _id = object_id) INTO object_exists;
    
    IF NOT object_exists THEN
        RETURN jsonb_build_object('error', 'Object not found');
    END IF;
    
    -- Собираем базовую информацию об объекте для корня JSON
    SELECT jsonb_build_object(
        'id', o._id,
        'name', o._name,
        'scheme_id', o._id_scheme,
        'scheme_name', sc._name,
        'parent_id', o._id_parent,
        'owner_id', o._id_owner,
        'who_change_id', o._id_who_change,
        'date_create', o._date_create,
        'date_modify', o._date_modify,
        'date_begin', o._date_begin,
        'date_complete', o._date_complete,
        'key', o._key,
        'code_int', o._code_int,
        'code_string', o._code_string,
        'code_guid', o._code_guid,
        'note', o._note,
        'bool', o._bool,
        'hash', o._hash
    ) INTO base_info
    FROM _objects o
    JOIN _schemes sc ON sc._id = o._id_scheme
    WHERE o._id = object_id;
    
    -- Собираем свойства объекта из _values с поддержкой рекурсии и массивов
    SELECT jsonb_object_agg(s._name, 
        CASE 
            -- Если это массив - обрабатываем поле _Array
            WHEN s._is_array = true THEN
                CASE 
                    WHEN v._Array IS NOT NULL THEN
                        CASE 
                            -- Массив объектов - рекурсивно обрабатываем каждый ID
                            WHEN t._name = 'Object' THEN
                                (
                                    SELECT jsonb_agg(get_object_json(array_element::text::bigint, max_depth - 1))
                                    FROM jsonb_array_elements(v._Array::jsonb) AS array_element
                                    WHERE array_element IS NOT NULL 
                                      AND jsonb_typeof(array_element) = 'number'
                                )
                            -- Массив примитивов - возвращаем как есть
                            ELSE v._Array::jsonb
                        END
                    ELSE '[]'::jsonb
                END
            -- Обычные поля (не массивы)
            WHEN t._name = 'Object' THEN
                CASE 
                    WHEN v._Long IS NOT NULL THEN 
                        get_object_json(v._Long, max_depth - 1)
                    ELSE NULL
                END
            WHEN t._db_type = 'String' THEN to_jsonb(v._String)
            WHEN t._db_type = 'Long' THEN to_jsonb(v._Long)
            WHEN t._db_type = 'Guid' THEN to_jsonb(v._Guid)
            WHEN t._db_type = 'Double' THEN to_jsonb(v._Double)
            WHEN t._db_type = 'DateTime' THEN to_jsonb(v._DateTime)
            WHEN t._db_type = 'Boolean' THEN to_jsonb(v._Boolean)
            WHEN t._db_type = 'ListItem' THEN 
                CASE 
                    WHEN v._Long IS NOT NULL THEN 
                        jsonb_build_object(
                            'id', v._Long,
                            'value', (SELECT _value FROM _list_items WHERE _id = v._Long)
                        )
                    ELSE NULL
                END
            WHEN t._db_type = 'ByteArray' THEN 
                CASE 
                    WHEN v._ByteArray IS NOT NULL THEN 
                        to_jsonb(encode(v._ByteArray, 'base64'))
                    ELSE NULL
                END
            ELSE NULL
        END
    ) INTO properties_info
    FROM _values v
    JOIN _structures s ON s._id = v._id_structure
    JOIN _types t ON t._id = s._id_type
    WHERE v._id_object = object_id;
    
    -- Объединяем базовую информацию с properties
    result_json := base_info || jsonb_build_object('properties', COALESCE(properties_info, '{}'::jsonb));
    
    RETURN result_json;
END;
$BODY$;

CREATE OR REPLACE VIEW v_objects_json AS
SELECT 
    _id as object_id,
    get_object_json(_id, 10) as object_json
FROM _objects
ORDER BY _id;

-- Комментарий к функции получения объекта
COMMENT ON FUNCTION get_object_json(bigint, integer) IS 'Функция получения объекта в JSON формате с базовыми полями в корне и свойствами в разделе properties. Поддержка рекурсии для связанных объектов и массивов из поля _Array. Массивы объектов хранятся как массив ID [1,2,3]';

-- ===== МОДУЛИ ДЛЯ МОДУЛЬНОЙ АРХИТЕКТУРЫ =====

-- Вспомогательная функция для форматирования JSON массива для оператора IN
-- Преобразует JSONB массив в строку значений для SQL IN clause
CREATE OR REPLACE FUNCTION _format_json_array_for_in(
    array_data jsonb
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    in_values text := '';
    json_element jsonb;
    first_item boolean := true;
    element_text text;
BEGIN
    -- Проверяем что это массив
    IF jsonb_typeof(array_data) != 'array' THEN
        RAISE EXCEPTION 'Ожидается JSON массив, получен: %', jsonb_typeof(array_data);
    END IF;
    
    -- Обрабатываем каждый элемент массива
    FOR json_element IN SELECT value FROM jsonb_array_elements(array_data) LOOP
        IF NOT first_item THEN
            in_values := in_values || ', ';
        END IF;
        first_item := false;
        
        -- Форматируем элемент в зависимости от типа
        CASE jsonb_typeof(json_element)
            WHEN 'string' THEN
                element_text := quote_literal(json_element #>> '{}');
            WHEN 'number' THEN
                element_text := json_element #>> '{}';
            WHEN 'boolean' THEN
                element_text := CASE WHEN (json_element)::boolean THEN 'true' ELSE 'false' END;
            ELSE
                element_text := quote_literal(json_element #>> '{}');
        END CASE;
        
        in_values := in_values || element_text;
    END LOOP;
    
    RETURN in_values;
END;
$BODY$;

COMMENT ON FUNCTION _format_json_array_for_in(jsonb) IS 'Преобразует JSONB массив в строку значений для SQL IN clause. Поддерживает string, number, boolean типы';

-- Вспомогательная функция для построения числовых сравнений
CREATE OR REPLACE FUNCTION _build_numeric_comparison(
    field_name text,
    operator_name text,
    operator_value text
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    op_symbol text;
BEGIN
    -- Маппинг операторов
    CASE operator_name
        WHEN '$gt' THEN op_symbol := '>';
        WHEN '$lt' THEN op_symbol := '<';
        WHEN '$gte' THEN op_symbol := '>=';
        WHEN '$lte' THEN op_symbol := '<=';
        ELSE RAISE EXCEPTION 'Неизвестный числовой оператор: %', operator_name;
    END CASE;
    
    RETURN format(
        ' AND EXISTS (
            SELECT 1 FROM _values fv 
            JOIN _structures fs ON fs._id = fv._id_structure 
            JOIN _types ft ON ft._id = fs._id_type
            WHERE fv._id_object = o._id 
              AND fs._name = %L 
              AND fs._is_array = false
              AND (
                (ft._db_type = ''Long'' AND fv._Long %s %L) OR
                (ft._db_type = ''Double'' AND fv._Double %s %L)
              )
        )',
        field_name,
        op_symbol,
        operator_value::bigint,
        op_symbol,
        operator_value::double precision
    );
END;
$BODY$;

COMMENT ON FUNCTION _build_numeric_comparison(text, text, text) IS 'Строит SQL условие для числовых операторов сравнения ($gt, $lt, $gte, $lte)';

-- Вспомогательная функция для построения строковых сравнений
CREATE OR REPLACE FUNCTION _build_string_comparison(
    field_name text,
    operator_name text,
    operator_value text
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    pattern text;
BEGIN
    -- Формирование LIKE паттерна
    CASE operator_name
        WHEN '$startsWith' THEN pattern := operator_value || '%';
        WHEN '$endsWith' THEN pattern := '%' || operator_value;
        WHEN '$contains' THEN pattern := '%' || operator_value || '%';
        ELSE RAISE EXCEPTION 'Неизвестный строковый оператор: %', operator_name;
    END CASE;
    
    RETURN format(
        ' AND EXISTS (
            SELECT 1 FROM _values fv 
            JOIN _structures fs ON fs._id = fv._id_structure 
            JOIN _types ft ON ft._id = fs._id_type
            WHERE fv._id_object = o._id 
              AND fs._name = %L 
              AND fs._is_array = false
              AND ft._db_type = ''String''
              AND fv._String LIKE %L
        )',
        field_name,
        pattern
    );
END;
$BODY$;

COMMENT ON FUNCTION _build_string_comparison(text, text, text) IS 'Строит SQL условие для строковых операторов ($startsWith, $endsWith, $contains)';

-- Вспомогательная функция для построения части условия внутри EXISTS
CREATE OR REPLACE FUNCTION _build_inner_condition(
    operator_name text,
    operator_value text
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    op_symbol text;
    pattern text;
    in_values_list text;
BEGIN
    -- Числовые операторы
    IF operator_name IN ('$gt', '$lt', '$gte', '$lte') THEN
        CASE operator_name
            WHEN '$gt' THEN op_symbol := '>';
            WHEN '$lt' THEN op_symbol := '<';
            WHEN '$gte' THEN op_symbol := '>=';
            WHEN '$lte' THEN op_symbol := '<=';
        END CASE;
        
        RETURN format(' AND ((ft._db_type = ''Long'' AND fv._Long %s %L) OR (ft._db_type = ''Double'' AND fv._Double %s %L))',
            op_symbol, operator_value::bigint, op_symbol, operator_value::double precision);
    
    -- Строковые операторы
    ELSIF operator_name IN ('$startsWith', '$endsWith', '$contains') THEN
        CASE operator_name
            WHEN '$startsWith' THEN pattern := operator_value || '%';
            WHEN '$endsWith' THEN pattern := '%' || operator_value;
            WHEN '$contains' THEN pattern := '%' || operator_value || '%';
        END CASE;
        
        RETURN format(' AND ft._db_type = ''String'' AND fv._String LIKE %L', pattern);
    
    -- Оператор IN
    ELSIF operator_name = '$in' THEN
        in_values_list := _format_json_array_for_in(operator_value::jsonb);
        RETURN format(' AND ((ft._db_type = ''String'' AND fv._String IN (%s)) OR (ft._db_type = ''Long'' AND fv._Long::text IN (%s)) OR (ft._db_type = ''Double'' AND fv._Double::text IN (%s)) OR (ft._db_type = ''Boolean'' AND fv._Boolean::text IN (%s)))',
            in_values_list, in_values_list, in_values_list, in_values_list);
    
    ELSE
        -- Простое равенство
        RETURN format(' AND ft._db_type = ''String'' AND fv._String = %L', operator_value);
    END IF;
END;
$BODY$;

COMMENT ON FUNCTION _build_inner_condition(text, text) IS 'Строит внутреннюю часть SQL условия (без EXISTS) для использования в комбинированных условиях';

-- Вспомогательная функция для построения одиночного условия
CREATE OR REPLACE FUNCTION _build_single_condition(
    field_name text,
    operator_name text,
    operator_value text
) RETURNS text
LANGUAGE 'plpgsql'
COST 100
VOLATILE
AS $BODY$
BEGIN
    -- Числовые операторы
    IF operator_name IN ('$gt', '$lt', '$gte', '$lte') THEN
        RETURN _build_numeric_comparison(field_name, operator_name, operator_value);
    
    -- Строковые операторы
    ELSIF operator_name IN ('$startsWith', '$endsWith', '$contains') THEN
        RETURN _build_string_comparison(field_name, operator_name, operator_value);
    
    -- Оператор IN
    ELSIF operator_name = '$in' THEN
        DECLARE
            in_values_list text;
        BEGIN
            -- Используем вспомогательную функцию для форматирования массива
            in_values_list := _format_json_array_for_in(operator_value::jsonb);
            
            RETURN format(
                ' AND EXISTS (
                    SELECT 1 FROM _values fv 
                    JOIN _structures fs ON fs._id = fv._id_structure 
                    JOIN _types ft ON ft._id = fs._id_type
                    WHERE fv._id_object = o._id 
                      AND fs._name = %L 
                      AND fs._is_array = false
                      AND (
                        (ft._db_type = ''String'' AND fv._String IN (%s)) OR
                        (ft._db_type = ''Long'' AND fv._Long::text IN (%s)) OR
                        (ft._db_type = ''Double'' AND fv._Double::text IN (%s)) OR
                        (ft._db_type = ''Boolean'' AND fv._Boolean::text IN (%s))
                      )
                )',
                field_name,
                in_values_list,
                in_values_list,
                in_values_list,
                in_values_list
            );
        END;
    
    ELSE
        RAISE EXCEPTION 'Неподдерживаемый оператор: %', operator_name;
    END IF;
END;
$BODY$;

-- Специальная функция для обработки логического оператора $and
CREATE OR REPLACE FUNCTION _build_and_condition(
    and_conditions jsonb
) RETURNS text
LANGUAGE 'plpgsql'
COST 100
VOLATILE
AS $BODY$
DECLARE
    result_condition text := '';
    sub_condition jsonb;
    field_name text;
    field_values jsonb;
    combined_condition text;
    operator_key text;
    operator_value text;
    i integer;
BEGIN
    -- Проходим по всем условиям в массиве $and
    FOR i IN 0..jsonb_array_length(and_conditions) - 1 LOOP
        sub_condition := and_conditions->i;
        
        -- Обрабатываем каждое поле в условии
        FOR field_name, field_values IN SELECT * FROM jsonb_each(sub_condition) LOOP
            -- Если это объект с операторами
            IF jsonb_typeof(field_values) = 'object' THEN
                -- Создаем EXISTS условие для этого поля со всеми его операторами
                combined_condition := format(' AND EXISTS (
                    SELECT 1 FROM _values fv 
                    JOIN _structures fs ON fs._id = fv._id_structure 
                    JOIN _types ft ON ft._id = fs._id_type
                    WHERE fv._id_object = o._id 
                      AND fs._name = %L 
                      AND fs._is_array = false', field_name);
                
                -- Добавляем все операторы для этого поля
                FOR operator_key, operator_value IN SELECT key, value FROM jsonb_each_text(field_values) LOOP
                    -- Используем вспомогательную функцию для генерации условия
                    combined_condition := combined_condition || _build_inner_condition(operator_key, operator_value);
                END LOOP;
                
                combined_condition := combined_condition || ')';
                result_condition := result_condition || combined_condition;
            ELSE
                -- Простое равенство для примитивных значений
                result_condition := result_condition || format(' AND EXISTS (
                    SELECT 1 FROM _values fv 
                    JOIN _structures fs ON fs._id = fv._id_structure 
                    JOIN _types ft ON ft._id = fs._id_type
                    WHERE fv._id_object = o._id 
                      AND fs._name = %L 
                      AND fs._is_array = false
                      AND ft._db_type = ''String''
                      AND fv._String = %L
                )', field_name, field_values #>> '{}');
            END IF;
        END LOOP;
    END LOOP;
    
    RETURN result_condition;
END;
$BODY$;

-- Специальная функция для обработки логического оператора $or
CREATE OR REPLACE FUNCTION _build_or_condition(
    or_conditions jsonb
) RETURNS text
LANGUAGE 'plpgsql'
COST 100
VOLATILE
AS $BODY$
DECLARE
    result_condition text := '';
    sub_condition jsonb;
    field_name text;
    field_values jsonb;
    single_condition text;
    operator_key text;
    operator_value text;
    i integer;
    or_parts text[] := '{}';
BEGIN
    -- Проходим по всем условиям в массиве $or
    FOR i IN 0..jsonb_array_length(or_conditions) - 1 LOOP
        sub_condition := or_conditions->i;
        
        -- Проверяем, есть ли вложенные логические операторы
        IF sub_condition ? '$and' THEN
            -- Рекурсивно обрабатываем вложенный $and
            single_condition := _build_and_condition(sub_condition->'$and');
            -- Убираем начальный ' AND ' и добавляем в массив OR
            IF single_condition LIKE ' AND %' THEN
                single_condition := substring(single_condition from 6);
            END IF;
            or_parts := or_parts || ('(' || single_condition || ')');
        ELSIF sub_condition ? '$or' THEN
            -- Рекурсивно обрабатываем вложенный $or  
            single_condition := _build_or_condition(sub_condition->'$or');
            -- Убираем начальный ' AND ' и скобки
            IF single_condition LIKE ' AND (%' THEN
                single_condition := substring(single_condition from 7);
                single_condition := left(single_condition, length(single_condition) - 1);
            END IF;
            or_parts := or_parts || ('(' || single_condition || ')');
        ELSIF sub_condition ? '$not' THEN
            -- Рекурсивно обрабатываем вложенный $not
            single_condition := _build_not_condition(sub_condition->'$not');
            -- Убираем начальный ' AND '
            IF single_condition LIKE ' AND %' THEN
                single_condition := substring(single_condition from 6);
            END IF;
            or_parts := or_parts || ('(' || single_condition || ')');
        ELSE
            -- Обрабатываем обычные поля
            FOR field_name, field_values IN SELECT * FROM jsonb_each(sub_condition) LOOP
                -- Если это объект с операторами
                IF jsonb_typeof(field_values) = 'object' THEN
                -- Создаем EXISTS условие для этого поля со всеми его операторами
                single_condition := format(' EXISTS (
                    SELECT 1 FROM _values fv 
                    JOIN _structures fs ON fs._id = fv._id_structure 
                    JOIN _types ft ON ft._id = fs._id_type
                    WHERE fv._id_object = o._id 
                      AND fs._name = %L 
                      AND fs._is_array = false', field_name);
                
                -- Добавляем все операторы для этого поля
                FOR operator_key, operator_value IN SELECT key, value FROM jsonb_each_text(field_values) LOOP
                    -- Используем вспомогательную функцию для генерации условия
                    single_condition := single_condition || _build_inner_condition(operator_key, operator_value);
                END LOOP;
                
                single_condition := single_condition || ')';
                or_parts := or_parts || single_condition;
                ELSE
                    -- Простое равенство для примитивных значений - обрабатываем разные типы
                IF jsonb_typeof(field_values) = 'boolean' THEN
                    single_condition := format(' EXISTS (
                        SELECT 1 FROM _values fv 
                        JOIN _structures fs ON fs._id = fv._id_structure 
                        JOIN _types ft ON ft._id = fs._id_type
                        WHERE fv._id_object = o._id 
                          AND fs._name = %L 
                          AND fs._is_array = false
                          AND ft._db_type = ''Boolean''
                          AND fv._Boolean = %L
                    )', field_name, (field_values #>> '{}')::boolean);
                ELSIF jsonb_typeof(field_values) = 'number' THEN
                    single_condition := format(' EXISTS (
                        SELECT 1 FROM _values fv 
                        JOIN _structures fs ON fs._id = fv._id_structure 
                        JOIN _types ft ON ft._id = fs._id_type
                        WHERE fv._id_object = o._id 
                          AND fs._name = %L 
                          AND fs._is_array = false
                          AND (
                            (ft._db_type = ''Long'' AND fv._Long = %L) OR
                            (ft._db_type = ''Double'' AND fv._Double = %L)
                          )
                    )', field_name, (field_values #>> '{}')::bigint, (field_values #>> '{}')::double precision);
                ELSIF jsonb_typeof(field_values) = 'string' THEN
                    single_condition := format(' EXISTS (
                        SELECT 1 FROM _values fv 
                        JOIN _structures fs ON fs._id = fv._id_structure 
                        JOIN _types ft ON ft._id = fs._id_type
                        WHERE fv._id_object = o._id 
                          AND fs._name = %L 
                          AND fs._is_array = false
                          AND ft._db_type = ''String''
                          AND fv._String = %L
                    )', field_name, field_values #>> '{}');
                ELSE
                    -- Для прочих типов, пытаемся как строку
                    single_condition := format(' EXISTS (
                        SELECT 1 FROM _values fv 
                        JOIN _structures fs ON fs._id = fv._id_structure 
                        JOIN _types ft ON ft._id = fs._id_type
                        WHERE fv._id_object = o._id 
                          AND fs._name = %L 
                          AND fs._is_array = false
                          AND ft._db_type = ''String''
                          AND fv._String = %L
                    )', field_name, field_values #>> '{}');
                END IF;
                or_parts := or_parts || single_condition;
                END IF;
            END LOOP;
        END IF;
    END LOOP;
    
    -- Объединяем все части через OR
    IF array_length(or_parts, 1) > 0 THEN
        result_condition := ' AND (' || array_to_string(or_parts, ' OR ') || ')';
    END IF;
    
    RETURN result_condition;
END;
$BODY$;

-- Специальная функция для обработки логического оператора $not
CREATE OR REPLACE FUNCTION _build_not_condition(
    not_condition jsonb
) RETURNS text
LANGUAGE 'plpgsql'
COST 100
VOLATILE
AS $BODY$
DECLARE
    result_condition text := '';
    field_name text;
    field_values jsonb;
    positive_condition text;
    operator_key text;
    operator_value text;
BEGIN
    -- Обрабатываем NOT условие (инвертируем EXISTS в NOT EXISTS)
    FOR field_name, field_values IN SELECT * FROM jsonb_each(not_condition) LOOP
        -- Если это объект с операторами
        IF jsonb_typeof(field_values) = 'object' THEN
            -- Создаем NOT EXISTS условие для этого поля
            positive_condition := format(' NOT EXISTS (
                SELECT 1 FROM _values fv 
                JOIN _structures fs ON fs._id = fv._id_structure 
                JOIN _types ft ON ft._id = fs._id_type
                WHERE fv._id_object = o._id 
                  AND fs._name = %L 
                  AND fs._is_array = false', field_name);
            
            -- Добавляем все операторы для этого поля
            FOR operator_key, operator_value IN SELECT key, value FROM jsonb_each_text(field_values) LOOP
                -- Используем вспомогательную функцию для генерации условия
                positive_condition := positive_condition || _build_inner_condition(operator_key, operator_value);
            END LOOP;
            
            positive_condition := positive_condition || ')';
            result_condition := result_condition || ' AND ' || positive_condition;
        ELSE
            -- Простое равенство для примитивных значений - инвертируем
            result_condition := result_condition || format(' AND NOT EXISTS (
                SELECT 1 FROM _values fv 
                JOIN _structures fs ON fs._id = fv._id_structure 
                JOIN _types ft ON ft._id = fs._id_type
                WHERE fv._id_object = o._id 
                  AND fs._name = %L 
                  AND fs._is_array = false
                  AND ft._db_type = ''String''
                  AND fv._String = %L
            )', field_name, field_values #>> '{}');
        END IF;
    END LOOP;
    
    RETURN result_condition;
END;
$BODY$;

-- Модуль построения фасетных условий фильтрации
CREATE OR REPLACE FUNCTION _build_facet_conditions(
    facet_filters jsonb
) RETURNS text
LANGUAGE 'plpgsql'
COST 100
VOLATILE
AS $BODY$
DECLARE
    filter_key text;
    filter_values jsonb;
    where_conditions text := '';
    operator_key text;
    operator_value text;
    i integer;
    sub_condition jsonb;
BEGIN
    -- Строим условия фильтрации на основе фасетов
    IF facet_filters IS NOT NULL AND jsonb_typeof(facet_filters) = 'object' THEN
        -- Обработка $and на верхнем уровне
        IF facet_filters ? '$and' THEN
            where_conditions := where_conditions || _build_and_condition(facet_filters->'$and');
            RETURN where_conditions;
        END IF;
        
        -- Обработка $or на верхнем уровне
        IF facet_filters ? '$or' THEN
            where_conditions := where_conditions || _build_or_condition(facet_filters->'$or');
            RETURN where_conditions;
        END IF;
        
        -- Обработка $not на верхнем уровне  
        IF facet_filters ? '$not' THEN
            where_conditions := where_conditions || _build_not_condition(facet_filters->'$not');
            RETURN where_conditions;
        END IF;
        
        FOR filter_key, filter_values IN SELECT * FROM jsonb_each(facet_filters) LOOP
            -- Обработка массивов (старый формат)
            IF jsonb_typeof(filter_values) = 'array' AND jsonb_array_length(filter_values) > 0 THEN
                where_conditions := where_conditions || format(
                    ' AND EXISTS (
                        SELECT 1 FROM _values fv 
                        JOIN _structures fs ON fs._id = fv._id_structure 
                        JOIN _types ft ON ft._id = fs._id_type
                        WHERE fv._id_object = o._id 
                          AND fs._name = %L 
                          AND (
                            -- Массивы - поиск пересечения с фильтром
                            (fs._is_array = true AND fv._Array IS NOT NULL AND 
                             (fv._Array::jsonb ?| %L::text[])) OR
                            -- Обычные поля
                            (fs._is_array = false AND (
                              (ft._db_type = ''String'' AND to_jsonb(fv._String) = ANY(%L::jsonb[])) OR
                              (ft._db_type = ''Long'' AND to_jsonb(fv._Long) = ANY(%L::jsonb[])) OR
                              (ft._db_type = ''Boolean'' AND to_jsonb(fv._Boolean) = ANY(%L::jsonb[])) OR
                              (ft._db_type = ''Double'' AND to_jsonb(fv._Double) = ANY(%L::jsonb[])) OR
                              (ft._db_type = ''DateTime'' AND to_jsonb(fv._DateTime) = ANY(%L::jsonb[]))
                            ))
                          )
                    )',
                    filter_key,
                    array(SELECT jsonb_array_elements_text(filter_values)), -- для массивов
                    array(SELECT jsonb_array_elements_text(filter_values)), -- String
                    array(SELECT jsonb_array_elements_text(filter_values)), -- Long
                    array(SELECT jsonb_array_elements_text(filter_values)), -- Boolean
                    array(SELECT jsonb_array_elements_text(filter_values)), -- Double
                    array(SELECT jsonb_array_elements_text(filter_values))  -- DateTime
                );
            -- Обработка объектов с операторами (новый LINQ формат)
            ELSIF jsonb_typeof(filter_values) = 'object' THEN
                -- Обработка логического оператора $and - специальная логика
                IF filter_values ? '$and' THEN
                    where_conditions := where_conditions || _build_and_condition(filter_values->'$and');
                ELSE
                    -- Проверяем количество операторов в объекте
                    IF (SELECT COUNT(*) FROM jsonb_each(filter_values)) = 1 THEN
                        -- Один оператор - используем вспомогательную функцию
                        FOR operator_key, operator_value IN SELECT key, value FROM jsonb_each_text(filter_values) LOOP
                            where_conditions := where_conditions || _build_single_condition(filter_key, operator_key, operator_value);
                        END LOOP;
                    ELSE
                        -- Множественные операторы на одном поле - создаем один EXISTS
                        where_conditions := where_conditions || format(' AND EXISTS (
                            SELECT 1 FROM _values fv 
                            JOIN _structures fs ON fs._id = fv._id_structure 
                            JOIN _types ft ON ft._id = fs._id_type
                            WHERE fv._id_object = o._id 
                              AND fs._name = %L 
                              AND fs._is_array = false', filter_key);
                        
                        -- Добавляем все операторы для этого поля
                        FOR operator_key, operator_value IN SELECT key, value FROM jsonb_each_text(filter_values) LOOP
                            -- Используем вспомогательную функцию для генерации условия
                            where_conditions := where_conditions || _build_inner_condition(operator_key, operator_value);
                        END LOOP;
                        
                        where_conditions := where_conditions || ')';
                    END IF;
                END IF;
            -- Обработка простых значений (равенство)
            ELSIF jsonb_typeof(filter_values) IN ('string', 'number', 'boolean') THEN
                -- Определяем тип значения и создаем соответствующее условие
                IF jsonb_typeof(filter_values) = 'boolean' THEN
                    where_conditions := where_conditions || format(
                        ' AND EXISTS (
                            SELECT 1 FROM _values fv 
                            JOIN _structures fs ON fs._id = fv._id_structure 
                            JOIN _types ft ON ft._id = fs._id_type
                            WHERE fv._id_object = o._id 
                              AND fs._name = %L 
                              AND fs._is_array = false
                              AND ft._db_type = ''Boolean''
                              AND fv._Boolean = %L
                        )',
                        filter_key,
                        (filter_values #>> '{}')::boolean
                    );
                ELSIF jsonb_typeof(filter_values) = 'number' THEN
                    where_conditions := where_conditions || format(
                        ' AND EXISTS (
                            SELECT 1 FROM _values fv 
                            JOIN _structures fs ON fs._id = fv._id_structure 
                            JOIN _types ft ON ft._id = fs._id_type
                            WHERE fv._id_object = o._id 
                              AND fs._name = %L 
                              AND fs._is_array = false
                              AND (
                                (ft._db_type = ''Long'' AND fv._Long = %L) OR
                                (ft._db_type = ''Double'' AND fv._Double = %L)
                              )
                        )',
                        filter_key,
                        (filter_values #>> '{}')::bigint,
                        (filter_values #>> '{}')::double precision
                    );
                ELSIF jsonb_typeof(filter_values) = 'string' THEN
                    where_conditions := where_conditions || format(
                        ' AND EXISTS (
                            SELECT 1 FROM _values fv 
                            JOIN _structures fs ON fs._id = fv._id_structure 
                            JOIN _types ft ON ft._id = fs._id_type
                            WHERE fv._id_object = o._id 
                              AND fs._name = %L 
                              AND fs._is_array = false
                              AND ft._db_type = ''String''
                              AND fv._String = %L
                        )',
                        filter_key,
                        filter_values #>> '{}'
                    );
                END IF;
            END IF;
        END LOOP;
    END IF;
    
    RETURN where_conditions;
END;
$BODY$;

-- Комментарий к модулю фасетной фильтрации
COMMENT ON FUNCTION _build_facet_conditions(jsonb) IS 'Приватный модуль для построения WHERE условий на основе facet_filters. Поддерживает массивы, операторы сравнения ($gt, $lt, $gte, $lte), строковые операторы ($startsWith, $endsWith, $contains), логические операторы ($and, $or, $not) и простые значения равенства. Возвращает готовую строку условий для добавления к основному запросу.';

-- Модуль построения условий сортировки
CREATE OR REPLACE FUNCTION _build_order_conditions(
    order_by jsonb,
    use_recursive_alias boolean
) RETURNS text
LANGUAGE 'plpgsql'
COST 100
VOLATILE
AS $BODY$
            DECLARE
    order_conditions text := 'ORDER BY o._id';
    order_item jsonb;
    field_name text;
    direction text;
                order_clause text;
    i integer;
            BEGIN
    -- Обрабатываем параметры сортировки
    IF order_by IS NOT NULL AND jsonb_typeof(order_by) = 'array' AND jsonb_array_length(order_by) > 0 THEN
        order_conditions := '';
        
        -- Обрабатываем каждый элемент сортировки
        FOR i IN 0..jsonb_array_length(order_by) - 1 LOOP
            order_item := order_by->i;
            field_name := order_item->>'field';
            direction := COALESCE(order_item->>'direction', 'ASC');
            
            -- Пропускаем некорректные элементы сортировки
            IF field_name IS NOT NULL AND field_name != '' THEN
                                        -- Формируем ORDER BY для поля из _values с padding для правильной сортировки чисел
                        order_clause := format('(
                            SELECT CASE 
                                WHEN v._String IS NOT NULL THEN v._String
                                WHEN v._Long IS NOT NULL THEN LPAD(v._Long::text, 20, ''0'')
                                WHEN v._Double IS NOT NULL THEN LPAD(REPLACE(v._Double::text, ''.'', ''~''), 25, ''0'')
                                WHEN v._DateTime IS NOT NULL THEN TO_CHAR(v._DateTime, ''YYYY-MM-DD HH24:MI:SS.US'')
                                WHEN v._Boolean IS NOT NULL THEN v._Boolean::text
                                ELSE NULL
                            END
                            FROM _values v 
                            JOIN _structures s ON v._id_structure = s._id 
                            WHERE v._id_object = o._id AND s._name = %L
                            LIMIT 1
                        ) %s NULLS LAST', field_name, direction);
                
                -- Добавляем запятую, если уже есть условия
                IF order_conditions != '' THEN
                    order_conditions := order_conditions || ', ';
                END IF;
                order_conditions := order_conditions || order_clause;
            END IF;
        END LOOP;
        
        -- Формируем финальный ORDER BY
        IF order_conditions != '' THEN
            order_conditions := 'ORDER BY ' || order_conditions || ', o._id';
        ELSE
            order_conditions := 'ORDER BY o._id';
        END IF;
    END IF;
    
    -- Если используется рекурсивный алиас (для descendants), заменяем 'o.' на 'd.'
    IF use_recursive_alias THEN
        order_conditions := replace(order_conditions, 'o.', 'd.');
    END IF;
    
    RETURN order_conditions;
END;
$BODY$;

-- Комментарий к модулю сортировки
COMMENT ON FUNCTION _build_order_conditions(jsonb, boolean) IS 'Приватный модуль для построения ORDER BY условий на основе order_by параметра. Поддерживает сортировку по полям из _values с правильной обработкой типов данных. Параметр use_recursive_alias определяет нужно ли заменить o._id на d._id для рекурсивных запросов.';

-- Модуль построения финального результата
CREATE OR REPLACE FUNCTION _build_search_result(
    objects_result jsonb,
    total_count integer,
    limit_count integer,
    offset_count integer,
    scheme_id bigint
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE
AS $BODY$
BEGIN
    RETURN jsonb_build_object(
        'objects', COALESCE(objects_result, '[]'::jsonb),
        'total_count', total_count,
        'limit', limit_count,
        'offset', offset_count,
        'facets', get_facets(scheme_id)
    );
END;
$BODY$;

-- Комментарий к модулю результата
COMMENT ON FUNCTION _build_search_result(jsonb, integer, integer, integer, bigint) IS 'Приватный модуль для построения финального JSON результата поиска. Формирует стандартный ответ с объектами, метаданными пагинации и фасетами для UI.';

-- ===== СПЕЦИАЛИЗИРОВАННЫЕ ФУНКЦИИ =====

-- Базовая функция фасетного поиска (без parent_id)
CREATE OR REPLACE FUNCTION search_objects_with_facets(
    scheme_id bigint,
    facet_filters jsonb DEFAULT NULL,
    limit_count integer DEFAULT 100,
    offset_count integer DEFAULT 0,
    distinct_mode boolean DEFAULT false,
    order_by jsonb DEFAULT NULL
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    objects_result jsonb;
    total_count integer;
    where_conditions text := _build_facet_conditions(facet_filters);
    order_conditions text := _build_order_conditions(order_by, false);
    query_text text;
BEGIN
    -- Формируем основной запрос для получения объектов
    IF distinct_mode THEN
        -- DISTINCT запрос с двухэтапным подходом
        IF order_by IS NOT NULL AND jsonb_typeof(order_by) = 'array' THEN
            -- DISTINCT + пользовательская сортировка
            query_text := format('
                SELECT jsonb_agg(
                    get_object_json(sorted._id, 10)
                )
                FROM (
                    SELECT distinct_sub._id
                    FROM (
                        SELECT DISTINCT ON (o._hash) o._id, o._date_modify
                        FROM _objects o
                        WHERE o._id_scheme = %s %s
                        ORDER BY o._hash, o._date_modify DESC
                    ) distinct_sub
                    JOIN _objects o ON o._id = distinct_sub._id %s
                    LIMIT %s OFFSET %s
                ) sorted',
                scheme_id,
                where_conditions,
                order_conditions,
                limit_count,
                offset_count
            );
        ELSE
            -- DISTINCT без пользовательской сортировки
            query_text := format('
                SELECT jsonb_agg(
                    get_object_json(sub._id, 10)
                )
                FROM (
                    SELECT DISTINCT ON (o._hash) o._id, o._date_modify
                    FROM _objects o
                    WHERE o._id_scheme = %s %s
                    ORDER BY o._hash, o._date_modify DESC
                    LIMIT %s OFFSET %s
                ) sub',
                scheme_id,
                where_conditions,
                limit_count,
                offset_count
            );
        END IF;
    ELSE
        -- Простой запрос без DISTINCT
        query_text := format('
            SELECT jsonb_agg(
                get_object_json(sub._id, 10)
            )
            FROM (
                SELECT o._id
                FROM _objects o
                WHERE o._id_scheme = %s %s %s
                LIMIT %s OFFSET %s
            ) sub',
            scheme_id,
            where_conditions,
            order_conditions,
            limit_count,
            offset_count
        );
    END IF;
    
    -- Выполняем запрос для получения объектов
    EXECUTE query_text INTO objects_result;
    
    -- Получаем общее количество объектов для пагинации
    query_text := format('
        SELECT COUNT(*)
        FROM _objects o
        WHERE o._id_scheme = %s %s',
        scheme_id,
        where_conditions
    );
    
    EXECUTE query_text INTO total_count;
    
    -- Возвращаем финальный результат через модуль
    RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
END;
$BODY$;

-- Комментарий к базовой функции
COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb) IS 'Базовый фасетный поиск объектов указанной схемы без ограничений по иерархии. Поддерживает фильтрацию, сортировку, пагинацию и DISTINCT режим.';

-- Функция фасетного поиска детей (с parent_id)
CREATE OR REPLACE FUNCTION search_objects_with_facets(
    scheme_id bigint,
    facet_filters jsonb,
    limit_count integer,
    offset_count integer,
    distinct_mode boolean,
    order_by jsonb,
    parent_id bigint
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    objects_result jsonb;
    total_count integer;
    where_conditions text := _build_facet_conditions(facet_filters);
    order_conditions text := _build_order_conditions(order_by, false);
    query_text text;
BEGIN
    -- Добавляем фильтрацию по родительскому объекту
    where_conditions := where_conditions || format(' AND o._id_parent = %s', parent_id);
    
    -- Формируем основной запрос для получения объектов
    IF distinct_mode THEN
        -- DISTINCT запрос с двухэтапным подходом
        IF order_by IS NOT NULL AND jsonb_typeof(order_by) = 'array' THEN
            -- DISTINCT + пользовательская сортировка
            query_text := format('
                SELECT jsonb_agg(
                    get_object_json(sorted._id, 10)
                )
                FROM (
                    SELECT distinct_sub._id
                    FROM (
                        SELECT DISTINCT ON (o._hash) o._id, o._date_modify
                        FROM _objects o
                        WHERE o._id_scheme = %s %s
                        ORDER BY o._hash, o._date_modify DESC
                    ) distinct_sub
                    JOIN _objects o ON o._id = distinct_sub._id %s
                    LIMIT %s OFFSET %s
                ) sorted',
                scheme_id,
                where_conditions,
                order_conditions,
                limit_count,
                offset_count
            );
        ELSE
            -- DISTINCT без пользовательской сортировки
            query_text := format('
                SELECT jsonb_agg(
                    get_object_json(sub._id, 10)
                )
                FROM (
                    SELECT DISTINCT ON (o._hash) o._id, o._date_modify
                    FROM _objects o
                    WHERE o._id_scheme = %s %s
                    ORDER BY o._hash, o._date_modify DESC
                    LIMIT %s OFFSET %s
                ) sub',
                scheme_id,
                where_conditions,
                limit_count,
                offset_count
            );
        END IF;
    ELSE
        -- Простой запрос без DISTINCT
        query_text := format('
            SELECT jsonb_agg(
                get_object_json(sub._id, 10)
            )
            FROM (
                SELECT o._id
                FROM _objects o
                WHERE o._id_scheme = %s %s %s
                LIMIT %s OFFSET %s
            ) sub',
            scheme_id,
            where_conditions,
            order_conditions,
            limit_count,
            offset_count
        );
    END IF;
    
    -- Выполняем запрос для получения объектов
    EXECUTE query_text INTO objects_result;
    
    -- Получаем общее количество объектов для пагинации
    query_text := format('
        SELECT COUNT(*)
        FROM _objects o
        WHERE o._id_scheme = %s %s',
        scheme_id,
        where_conditions
    );
    
    EXECUTE query_text INTO total_count;
    
    -- Возвращаем финальный результат через модуль
    RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
END;
$BODY$;

-- Комментарий к функции для детей
COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint) IS 'Фасетный поиск прямых дочерних объектов указанного родителя. Эквивалентен базовому поиску с добавлением фильтра parent_id.';

-- Функция фасетного поиска потомков (с parent_id и max_depth)
CREATE OR REPLACE FUNCTION search_objects_with_facets(
    scheme_id bigint,
    facet_filters jsonb,
    limit_count integer,
    offset_count integer,
    distinct_mode boolean,
    order_by jsonb,
    parent_id bigint,
    max_depth integer
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    objects_result jsonb;
    total_count integer;
    where_conditions text := _build_facet_conditions(facet_filters);
    order_conditions text := _build_order_conditions(order_by, true); -- рекурсивный алиас
    clean_where_conditions text;
    query_text text;
BEGIN
    -- Убираем parent_id из фасетных условий для рекурсивного запроса
    clean_where_conditions := replace(where_conditions, format(' AND o._id_parent = %s', parent_id), '');
    
    -- Формируем рекурсивный запрос для получения всех потомков
    IF distinct_mode THEN
        -- DISTINCT рекурсивный запрос
        query_text := format('
            WITH RECURSIVE descendants AS (
                -- Базовый случай: начинаем с родительского объекта (любой схемы)
                SELECT %s::bigint as _id, NULL::varchar as _hash, NULL::timestamp as _date_modify, 0::integer as depth
                WHERE %s IS NOT NULL
                
                UNION ALL
                
                -- Рекурсия: обходим ВСЕ дочерние объекты, фильтруем по схеме только в результате
                SELECT o._id, o._hash, o._date_modify, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT jsonb_agg(
                get_object_json(sorted._id, 10)
            )
            FROM (
                SELECT distinct_sub._id
                FROM (
                    SELECT DISTINCT ON (d._hash) d._id, d._date_modify, d.depth
                    FROM descendants d
                    JOIN _objects o ON d._id = o._id
                    WHERE d.depth > 0 AND o._id_scheme = %s %s
                    ORDER BY d._hash, d._date_modify DESC
                ) distinct_sub %s
                LIMIT %s OFFSET %s
            ) sorted',
            COALESCE(parent_id, 0),
            parent_id,
            max_depth,
            scheme_id,
            clean_where_conditions,
            order_conditions,
            limit_count,
            offset_count
        );
    ELSE
        -- Рекурсивный обычный запрос
        query_text := format('
            WITH RECURSIVE descendants AS (
                -- Базовый случай: начинаем с родительского объекта (любой схемы)
                SELECT %s::bigint as _id, 0::integer as depth
                WHERE %s IS NOT NULL
                
                UNION ALL
                
                -- Рекурсия: обходим ВСЕ дочерние объекты, фильтруем по схеме только в результате
                SELECT o._id, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT jsonb_agg(
                get_object_json(sub._id, 10)
            )
            FROM (
                SELECT d._id
                FROM descendants d
                JOIN _objects o ON d._id = o._id
                WHERE d.depth > 0 AND o._id_scheme = %s %s %s
                LIMIT %s OFFSET %s
            ) sub',
            COALESCE(parent_id, 0),
            parent_id,
            max_depth,
            scheme_id,
            clean_where_conditions,
            order_conditions,
            limit_count,
            offset_count
        );
    END IF;
    
    -- Выполняем запрос для получения объектов
    EXECUTE query_text INTO objects_result;
    
    -- Получаем общее количество объектов для пагинации (рекурсивный подсчет)
    IF distinct_mode THEN
        -- Подсчет с учетом DISTINCT
        query_text := format('
            WITH RECURSIVE descendants AS (
                SELECT %s::bigint as _id, NULL::varchar as _hash, 0::integer as depth
                WHERE %s IS NOT NULL
                
                UNION ALL
                
                SELECT o._id, o._hash, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT COUNT(DISTINCT o._hash)
            FROM descendants d
            JOIN _objects o ON d._id = o._id
            WHERE d.depth > 0 AND o._id_scheme = %s %s',
            COALESCE(parent_id, 0),
            parent_id,
            max_depth,
            scheme_id,
            clean_where_conditions
        );
    ELSE
        -- Обычный рекурсивный подсчет
        query_text := format('
            WITH RECURSIVE descendants AS (
                SELECT %s::bigint as _id, 0::integer as depth
                WHERE %s IS NOT NULL
                
                UNION ALL
                
                SELECT o._id, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT COUNT(*)
            FROM descendants d
            JOIN _objects o ON d._id = o._id
            WHERE d.depth > 0 AND o._id_scheme = %s %s',
            COALESCE(parent_id, 0),
            parent_id,
            max_depth,
            scheme_id,
            clean_where_conditions
        );
    END IF;
    
    EXECUTE query_text INTO total_count;
    
    -- Возвращаем финальный результат через модуль
    RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
END;
$BODY$;

-- Комментарий к функции для потомков
COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint, integer) IS 'Фасетный поиск всех потомков указанного родителя до заданной глубины с рекурсивным обходом. Использует WITH RECURSIVE CTE для эффективного поиска в иерархии.';

-- ===== BATCH ПЕРЕГРУЗКИ (ИСПОЛЬЗУЯ МОДУЛЬНУЮ АРХИТЕКТУРУ) =====

-- Batch функция фасетного поиска детей (с parent_ids bigint[])
CREATE OR REPLACE FUNCTION search_objects_with_facets(
    scheme_id bigint,
    facet_filters jsonb,
    limit_count integer,
    offset_count integer,
    distinct_mode boolean,
    order_by jsonb,
    parent_ids bigint[]
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    objects_result jsonb;
    total_count integer;
    where_conditions text := _build_facet_conditions(facet_filters);
    order_conditions text := _build_order_conditions(order_by, false);
    query_text text;
BEGIN
    -- Добавляем фильтрацию по МАССИВУ родительских объектов
    IF parent_ids IS NOT NULL AND array_length(parent_ids, 1) > 0 THEN
        where_conditions := where_conditions || format(' AND o._id_parent = ANY(%L)', parent_ids);
    END IF;
    
    -- Формируем основной запрос для получения объектов (аналогично children функции)
    IF distinct_mode THEN
        -- DISTINCT запрос с двухэтапным подходом
        IF order_by IS NOT NULL AND jsonb_typeof(order_by) = 'array' THEN
            -- DISTINCT + пользовательская сортировка
            query_text := format('
                SELECT jsonb_agg(
                    get_object_json(sorted._id, 10)
                )
                FROM (
                    SELECT distinct_sub._id
                    FROM (
                        SELECT DISTINCT ON (o._hash) o._id, o._date_modify
                        FROM _objects o
                        WHERE o._id_scheme = %s %s
                        ORDER BY o._hash, o._date_modify DESC
                    ) distinct_sub
                    JOIN _objects o ON o._id = distinct_sub._id %s
                    LIMIT %s OFFSET %s
                ) sorted',
                scheme_id,
                where_conditions,
                order_conditions,
                limit_count,
                offset_count
            );
        ELSE
            -- DISTINCT без пользовательской сортировки
            query_text := format('
                SELECT jsonb_agg(
                    get_object_json(sub._id, 10)
                )
                FROM (
                    SELECT DISTINCT ON (o._hash) o._id, o._date_modify
                    FROM _objects o
                    WHERE o._id_scheme = %s %s
                    ORDER BY o._hash, o._date_modify DESC
                    LIMIT %s OFFSET %s
                ) sub',
                scheme_id,
                where_conditions,
                limit_count,
                offset_count
            );
        END IF;
    ELSE
        -- Обычный запрос без DISTINCT
        query_text := format('
            SELECT jsonb_agg(
                get_object_json(sub._id, 10)
            )
            FROM (
                SELECT o._id
                FROM _objects o
                WHERE o._id_scheme = %s %s %s
                LIMIT %s OFFSET %s
            ) sub',
            scheme_id,
            where_conditions,
            order_conditions,
            limit_count,
            offset_count
        );
    END IF;
    
    -- Выполняем запрос для получения объектов
    EXECUTE query_text INTO objects_result;
    
    -- Получаем общее количество для пагинации
    query_text := format('
        SELECT COUNT(*)
        FROM _objects o
        WHERE o._id_scheme = %s %s',
        scheme_id,
        where_conditions
    );
    
    EXECUTE query_text INTO total_count;
    
    -- Возвращаем финальный результат через модуль
    RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
END;
$BODY$;

-- Batch функция фасетного поиска потомков (с parent_ids bigint[] и max_depth)
CREATE OR REPLACE FUNCTION search_objects_with_facets(
    scheme_id bigint,
    facet_filters jsonb,
    limit_count integer,
    offset_count integer,
    distinct_mode boolean,
    order_by jsonb,
    parent_ids bigint[],
    max_depth integer
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    objects_result jsonb;
    total_count integer;
    where_conditions text := _build_facet_conditions(facet_filters);
    order_conditions text := _build_order_conditions(order_by, true); -- рекурсивный алиас
    clean_where_conditions text;
    query_text text;
BEGIN
    -- Убираем parent_ids из фасетных условий для рекурсивного запроса  
    -- В batch запросах фасетные фильтры должны применяться только к результирующим объектам,
    -- а не к условию parent_ids (которое обрабатывается в CTE через unnest)
    clean_where_conditions := where_conditions;
    
    -- Формируем рекурсивный запрос для получения всех потомков из МАССИВА родителей
    IF distinct_mode THEN
        -- DISTINCT рекурсивный запрос
        query_text := format('
            WITH RECURSIVE descendants AS (
                -- Базовый случай: начинаем с МАССИВА родительских объектов (любой схемы)
                SELECT unnest(%L::bigint[]) as _id, NULL::varchar as _hash, NULL::timestamp as _date_modify, 0::integer as depth
                WHERE %L IS NOT NULL AND array_length(%L::bigint[], 1) > 0
                
                UNION ALL
                
                -- Рекурсия: обходим ВСЕ дочерние объекты, фильтруем по схеме только в результате
                SELECT o._id, o._hash, o._date_modify, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT jsonb_agg(
                get_object_json(sorted._id, 10)
            )
            FROM (
                SELECT distinct_sub._id
                FROM (
                    SELECT DISTINCT ON (d._hash) d._id, d._date_modify, d.depth
                    FROM descendants d
                    JOIN _objects o ON d._id = o._id
                    WHERE d.depth > 0 AND o._id_scheme = %s %s
                    ORDER BY d._hash, d._date_modify DESC
                ) distinct_sub %s
                LIMIT %s OFFSET %s
            ) sorted',
            parent_ids,
            parent_ids,
            parent_ids,
            max_depth,
            scheme_id,
            clean_where_conditions,
            order_conditions,
            limit_count,
            offset_count
        );
    ELSE
        -- Рекурсивный обычный запрос
        query_text := format('
            WITH RECURSIVE descendants AS (
                -- Базовый случай: начинаем с МАССИВА родительских объектов (любой схемы)
                SELECT unnest(%L::bigint[]) as _id, 0::integer as depth
                WHERE %L IS NOT NULL AND array_length(%L::bigint[], 1) > 0
                
                UNION ALL
                
                -- Рекурсия: обходим ВСЕ дочерние объекты, фильтруем по схеме только в результате
                SELECT o._id, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT jsonb_agg(
                get_object_json(sub._id, 10)
            )
            FROM (
                SELECT d._id
                FROM descendants d
                JOIN _objects o ON d._id = o._id
                WHERE d.depth > 0 AND o._id_scheme = %s %s %s
                LIMIT %s OFFSET %s
            ) sub',
            parent_ids,
            parent_ids,  
            parent_ids,
            max_depth,
            scheme_id,
            clean_where_conditions,
            order_conditions,
            limit_count,
            offset_count
        );
    END IF;
    
    -- Выполняем запрос для получения объектов
    EXECUTE query_text INTO objects_result;
    
    -- Получаем общее количество потомков для пагинации
    IF distinct_mode THEN
        -- Подсчет с учетом DISTINCT
        query_text := format('
            WITH RECURSIVE descendants AS (
                SELECT unnest(%L::bigint[]) as _id, NULL::varchar as _hash, 0::integer as depth
                WHERE %L IS NOT NULL AND array_length(%L::bigint[], 1) > 0
                
                UNION ALL
                
                SELECT o._id, o._hash, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT COUNT(DISTINCT o._hash)
            FROM descendants d
            JOIN _objects o ON d._id = o._id
            WHERE d.depth > 0 AND o._id_scheme = %s %s',
            parent_ids,
            parent_ids,
            parent_ids,
            max_depth,
            scheme_id,
            clean_where_conditions
        );
    ELSE
        -- Обычный рекурсивный подсчет
        query_text := format('
            WITH RECURSIVE descendants AS (
                SELECT unnest(%L::bigint[]) as _id, 0::integer as depth
                WHERE %L IS NOT NULL AND array_length(%L::bigint[], 1) > 0
                
                UNION ALL
                
                SELECT o._id, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT COUNT(*)
            FROM descendants d
            JOIN _objects o ON d._id = o._id
            WHERE d.depth > 0 AND o._id_scheme = %s %s',
            parent_ids,
            parent_ids,
            parent_ids,
            max_depth,
            scheme_id,
            clean_where_conditions
        );
    END IF;
    
    EXECUTE query_text INTO total_count;
    
    -- Возвращаем финальный результат через модуль
    RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
END;
$BODY$;

-- Комментарии к batch функциям
COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint[]) IS 'Batch фасетный поиск прямых дочерних объектов МАССИВА родителей. Один SQL запрос вместо N отдельных вызовов для максимальной производительности.';

COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint[], integer) IS 'Batch фасетный поиск всех потомков МАССИВА родителей до заданной глубины с рекурсивным обходом. Использует WITH RECURSIVE CTE с unnest() для эффективного поиска в иерархии от множества корневых объектов.';

-- Функция получения разрешений пользователя для конкретного объекта
CREATE OR REPLACE FUNCTION get_user_permissions_for_object(
    p_object_id bigint,
    p_user_id bigint DEFAULT NULL  -- теперь опциональный для использования в триггере
) RETURNS TABLE(
    object_id bigint,
    user_id bigint,
    permission_source_id bigint,
    permission_type varchar,
    _id_role bigint,
    _id_user bigint,
    can_select boolean,
    can_insert boolean,
    can_update boolean,
    can_delete boolean
) AS $$
BEGIN
    RETURN QUERY
    WITH RECURSIVE permission_search AS (
        -- Шаг 1: Начинаем с целевого объекта
        SELECT 
            p_object_id as object_id,
            p_object_id as current_search_id,
            o._id_parent,
            0 as level,
            EXISTS(SELECT 1 FROM _permissions WHERE _id_ref = p_object_id) as has_permission
        FROM _objects o
        WHERE o._id = p_object_id
        
        UNION ALL
        
        -- Шаг 2: Если НЕТ permission - идем к родителю
        SELECT 
            ps.object_id,
            o._id as current_search_id,
            o._id_parent,
            ps.level + 1,
            EXISTS(SELECT 1 FROM _permissions WHERE _id_ref = o._id) as has_permission
        FROM _objects o
        JOIN permission_search ps ON o._id = ps._id_parent
        WHERE ps.level < 50
          AND ps.has_permission = false  -- продолжаем только если НЕТ permission
    ),
    -- Берем первый найденный permission для объекта
    object_permission AS (
        SELECT DISTINCT ON (ps.object_id)
            ps.object_id,
            p._id as permission_id,
            p._id_user,
            p._id_role,
            p._select,
            p._insert,
            p._update,
            p._delete,
            ps.level,
            ps.current_search_id as permission_source_id
        FROM permission_search ps
        JOIN _permissions p ON p._id_ref = ps.current_search_id
        WHERE ps.has_permission = true
        ORDER BY ps.object_id, ps.level  -- ближайший к объекту
    ),
    -- Добавляем глобальные права как fallback (_id_ref = 0)
    global_permission AS (
        SELECT 
            p_object_id as object_id,
            p._id as permission_id,
            p._id_user,
            p._id_role,
            p._select,
            p._insert,
            p._update,
            p._delete,
            999 as level,  -- низкий приоритет
            0 as permission_source_id
        FROM _permissions p
        WHERE p._id_ref = 0
    ),
    -- Объединяем специфичные и глобальные права
    all_permissions AS (
        SELECT * FROM object_permission
        UNION ALL
        SELECT * FROM global_permission
    ),
    -- Берем первый по приоритету (специфичные > глобальные)
    final_permission AS (
        SELECT DISTINCT ON (object_id)
            *
        FROM all_permissions
        ORDER BY object_id, level  -- специфичные права имеют меньший level
    )
    -- Результат: для пользовательских permissions - прямо, для ролевых - через users_roles
    SELECT 
        fp.object_id,
        CASE 
            WHEN p_user_id IS NULL THEN NULL  -- если user_id не передан для триггера
            WHEN fp._id_user IS NOT NULL THEN fp._id_user  -- прямое пользовательское
            ELSE ur._id_user  -- через роль
        END as user_id,
        fp.permission_source_id,
        CASE 
            WHEN fp._id_user IS NOT NULL THEN 'user'::varchar
            ELSE 'role'::varchar
        END as permission_type,
        fp._id_role,
        fp._id_user,
        fp._select as can_select,
        fp._insert as can_insert,
        fp._update as can_update,
        fp._delete as can_delete
    FROM final_permission fp
    LEFT JOIN _users_roles ur ON ur._id_role = fp._id_role  -- только для ролевых permissions
    WHERE p_user_id IS NULL OR (fp._id_user = p_user_id OR ur._id_user = p_user_id);  -- если user_id NULL - все permissions, иначе фильтруем
END;
$$ LANGUAGE plpgsql;

-- Комментарий к функции получения разрешений для объекта
COMMENT ON FUNCTION get_user_permissions_for_object(bigint, bigint) IS 'Функция получения эффективных разрешений для конкретного объекта с учетом иерархического наследования и приоритетов (пользователь > роль). Если user_id = NULL, возвращает первый найденный permission без фильтрации по пользователю (для использования в триггерах)';

-- Функция триггера для автоматического создания permissions при создании узловых объектов
CREATE OR REPLACE FUNCTION auto_create_node_permissions()
RETURNS TRIGGER AS $$
DECLARE
    source_permission RECORD;
BEGIN
    -- Обрабатываем только INSERT новых объектов с родителем
    IF TG_OP = 'INSERT' AND NEW._id_parent IS NOT NULL THEN
        
        -- Проверяем есть ли уже permission у родителя
        IF EXISTS(SELECT 1 FROM _permissions WHERE _id_ref = NEW._id_parent) THEN
            RETURN NEW;  -- У родителя уже есть permission, ничего не делаем
        END IF;
        
        -- Используем модифицированную функцию БЕЗ user_id для поиска source permission
        SELECT * INTO source_permission 
        FROM get_user_permissions_for_object(NEW._id_parent, NULL) 
        LIMIT 1;
        
        -- Если нашли источник permission - создаем permission для родителя
        IF FOUND THEN
            INSERT INTO _permissions (
                _id, _id_role, _id_user, _id_ref,
                _select, _insert, _update, _delete
            ) VALUES (
                nextval('global_identity'),
                source_permission._id_role,
                source_permission._id_user,
                NEW._id_parent,  -- создаем permission для родителя
                source_permission.can_select,
                source_permission.can_insert,
                source_permission.can_update,
                source_permission.can_delete
            );
        END IF;
        
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Создание триггера на таблице _objects
CREATE TRIGGER tr_auto_create_node_permissions
    AFTER INSERT ON _objects
    FOR EACH ROW
    EXECUTE FUNCTION auto_create_node_permissions();

-- Комментарии к триггеру
COMMENT ON FUNCTION auto_create_node_permissions() IS 'Функция триггера для автоматического создания permissions у родительских объектов при появлении дочерних. Создает permission только если у родителя его еще нет, наследуя от ближайшего предка с permission';
COMMENT ON TRIGGER tr_auto_create_node_permissions ON _objects IS 'Триггер автоматически создает permission для объекта при добавлении к нему дочернего объекта, если у него еще нет собственного permission. Ускоряет поиск разрешений за счет сокращения глубины рекурсии';

CREATE VIEW v_user_permissions AS
WITH RECURSIVE permission_search AS (
    -- Шаг 1: Каждый объект ищет свой permission
    SELECT 
        o._id as object_id,
        o._id as current_search_id,
        o._id_parent,
        0 as level,
        EXISTS(SELECT 1 FROM _permissions WHERE _id_ref = o._id) as has_permission
    FROM _objects o
    
    UNION ALL
    
    -- Шаг 2: Если НЕТ permission - идем к родителю
    SELECT 
        ps.object_id,
        o._id as current_search_id,
        o._id_parent,
        ps.level + 1,
        EXISTS(SELECT 1 FROM _permissions WHERE _id_ref = o._id) as has_permission
    FROM _objects o
    JOIN permission_search ps ON o._id = ps._id_parent
    WHERE ps.level < 50
      AND ps.has_permission = false  -- продолжаем только если НЕТ permission
),
-- Берем первый найденный permission для каждого объекта
object_permissions AS (
    SELECT DISTINCT ON (ps.object_id)
        ps.object_id,
        p._id as permission_id,
        p._id_user,
        p._id_role,
        p._select,
        p._insert,
        p._update,
        p._delete,
        ps.level
    FROM permission_search ps
    JOIN _permissions p ON p._id_ref = ps.current_search_id
    WHERE ps.has_permission = true
    ORDER BY ps.object_id, ps.level  -- ближайший к объекту
),
-- 🚀 НОВОЕ: Добавляем глобальные права как виртуальные записи с object_id = 0
global_permissions AS (
    SELECT 
        0 as object_id,  -- виртуальный объект для глобальных прав
        p._id as permission_id,
        p._id_user,
        p._id_role,
        p._select,
        p._insert,
        p._update,
        p._delete,
        999 as level  -- низкий приоритет
    FROM _permissions p
    WHERE p._id_ref = 0  -- глобальные права
),
-- Объединяем специфичные и глобальные права
all_permissions AS (
    SELECT * FROM object_permissions
    UNION ALL
    SELECT * FROM global_permissions
),
-- Берем первый по приоритету (специфичные > глобальные)
final_permissions AS (
    SELECT DISTINCT ON (object_id)
        *
    FROM all_permissions
    ORDER BY object_id, level  -- специфичные права имеют меньший level
)
-- Результат: для пользовательских permissions - прямо, для ролевых - через users_roles
SELECT 
    fp.object_id,
    CASE 
        WHEN fp._id_user IS NOT NULL THEN fp._id_user  -- прямое пользовательское
        ELSE ur._id_user  -- через роль
    END as user_id,
    fp.permission_id,
    CASE 
        WHEN fp._id_user IS NOT NULL THEN 'user'::varchar
        ELSE 'role'::varchar
    END as permission_type,
    fp._id_role,
    fp._select as can_select,
    fp._insert as can_insert,
    fp._update as can_update,
    fp._delete as can_delete
FROM final_permissions fp
LEFT JOIN _users_roles ur ON ur._id_role = fp._id_role  -- только для ролевых permissions
WHERE fp._id_user IS NOT NULL OR ur._id_user IS NOT NULL;  -- есть пользователь


-- 🔐 ТРИГГЕР ДЛЯ ЗАЩИТЫ СИСТЕМНЫХ ПОЛЬЗОВАТЕЛЕЙ
-- Запрещает удаление и переименование пользователей с ID 0 (sys) и 1
-- Остальные операции (смена пароля, email, телефона, статуса) разрешены

-- Функция триггера
CREATE OR REPLACE FUNCTION protect_system_users()
RETURNS TRIGGER AS $$
BEGIN
    -- Проверяем операцию DELETE
    IF TG_OP = 'DELETE' THEN
        -- Запрещаем удаление системных пользователей
        IF OLD._id IN (0, 1) THEN
            RAISE EXCEPTION 'Нельзя удалить системного пользователя с ID %', OLD._id;
        END IF;
        RETURN OLD;
    END IF;
    
    -- Проверяем операцию UPDATE
    IF TG_OP = 'UPDATE' THEN
        -- Запрещаем изменение ID (на всякий случай)
        IF OLD._id != NEW._id THEN
            RAISE EXCEPTION 'Нельзя изменить ID пользователя';
        END IF;
        
        -- Для системных пользователей запрещаем изменение логина и имени
        IF OLD._id IN (0, 1) THEN
            -- Запрещаем изменение логина
            IF OLD._login != NEW._login THEN
                RAISE EXCEPTION 'Нельзя изменить логин системного пользователя с ID %', OLD._id;
            END IF;
            
            -- Запрещаем изменение имени
            IF OLD._name != NEW._name THEN
                RAISE EXCEPTION 'Нельзя изменить имя системного пользователя с ID %', OLD._id;
            END IF;
        END IF;
        
        RETURN NEW;
    END IF;
    
    -- Для INSERT ничего не проверяем
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Создание триггера на таблице _users
DROP TRIGGER IF EXISTS tr_protect_system_users ON _users;
CREATE TRIGGER tr_protect_system_users
    BEFORE UPDATE OR DELETE ON _users
    FOR EACH ROW
    EXECUTE FUNCTION protect_system_users();

-- Комментарии к триггеру
COMMENT ON FUNCTION protect_system_users() IS 'Защищает системных пользователей (ID 0, 1) от удаления и переименования';
COMMENT ON TRIGGER tr_protect_system_users ON _users IS 'Триггер защиты системных пользователей от удаления и переименования логина/имени';

-- Тестовые запросы для проверки триггера:
/*
-- ✅ Это должно работать (смена пароля sys пользователя):
UPDATE _users SET _password = 'new_hash' WHERE _id = 0;

-- ✅ Это должно работать (смена email sys пользователя):
UPDATE _users SET _email = 'sys@newdomain.com' WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (смена логина sys пользователя):
UPDATE _users SET _login = 'system' WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (смена имени sys пользователя):
UPDATE _users SET _name = 'System Administrator' WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (удаление sys пользователя):
DELETE FROM _users WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (удаление пользователя с ID 1):
DELETE FROM _users WHERE _id = 1;

-- ✅ Это должно работать (обычный пользователь):
UPDATE _users SET _login = 'new_login', _name = 'New Name' WHERE _id = 100;
DELETE FROM _users WHERE _id = 100;
*/


-- Схемы для TrueSight.Models
INSERT INTO _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (1001, NULL, 'TrueSight.Models.AnalyticsMetrics', 'Метрики аналитики', 'TrueSight.Models');
INSERT INTO _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES (1002, NULL, 'TrueSight.Models.AnalyticsRecord', 'Запись аналитики', 'TrueSight.Models');

-- Структуры для AnalyticsMetrics (схема 1001)
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1003, NULL, 1001, NULL, -9223372036854775704, NULL, 'AdvertId', 'ID рекламной кампании', 1, NULL, true, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1004, NULL, 1001, NULL, -9223372036854775704, NULL, 'Baskets', 'Корзины', 2, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1005, NULL, 1001, NULL, -9223372036854775704, NULL, 'Base', 'База', 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1006, NULL, 1001, NULL, -9223372036854775704, NULL, 'Association', 'Ассоц.', 4, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1007, NULL, 1001, NULL, -9223372036854775707, NULL, 'Costs', 'Затраты', 5, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1008, NULL, 1001, NULL, -9223372036854775704, NULL, 'Rate', 'Ставка', 6, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1009, NULL, 1001, NULL, -9223372036854775700, NULL, 'Note', 'Примечание', 7, NULL, NULL, NULL, NULL, true, NULL, NULL);

-- Структуры для AnalyticsRecord (схема 1002)
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1010, NULL, 1002, NULL, -9223372036854775708, NULL, 'Date', 'Дата', 1, NULL, true, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1011, NULL, 1002, NULL, -9223372036854775700, NULL, 'Article', 'Артикул', 2, NULL, true, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1012, NULL, 1002, NULL, -9223372036854775704, NULL, 'Orders', 'Заказы', 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1013, NULL, 1002, NULL, -9223372036854775704, NULL, 'Stock', 'Остатки', 4, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1014, NULL, 1002, NULL, -9223372036854775704, NULL, 'TotalCart', 'Общая корзина', 5, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1015, NULL, 1002, NULL, -9223372036854775704, NULL, 'Price', 'Цена', 6, NULL, NULL, NULL, NULL, true, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1016, NULL, 1002, NULL, -9223372036854775703, NULL, 'AutoMetrics', 'Метрики авто', 7, NULL, true, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1017, NULL, 1002, NULL, -9223372036854775703, NULL, 'AuctionMetrics', 'Метрики аукциона', 8, NULL, true, NULL, NULL, NULL, NULL, NULL);
INSERT INTO _structures (_id, _id_parent, _id_scheme, _id_override, _id_type, _id_list, _name, _alias, _order, _readonly, _allow_not_null, _is_array, _is_compress, _store_null, _default_value, _default_editor) VALUES (1018, NULL, 1002, NULL, -9223372036854775700, NULL, 'Tag', 'Тег (группировка)', 9, NULL, NULL, NULL, NULL, true, NULL, NULL);


-- Создание объектов для примера AnalyticsRecord с ID начиная с 1019

-- 1. Создаем объект AutoMetrics (ID: 1019)
INSERT INTO _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) 
VALUES (1019, NULL, 1001, 1, 1, NOW(), NOW(), NULL, NULL, NULL, NULL, NULL, NULL, 'AutoMetrics Example', NULL, NULL);

-- 2. Создаем объект AuctionMetrics (ID: 1020)
INSERT INTO _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) 
VALUES (1020, NULL, 1001, 1, 1, NOW(), NOW(), NULL, NULL, NULL, NULL, NULL, NULL, 'AuctionMetrics Example', NULL, NULL);

-- 3. Создаем главный объект AnalyticsRecord (ID: 1021)
INSERT INTO _objects (_id, _id_parent, _id_scheme, _id_owner, _id_who_change, _date_create, _date_modify, _date_begin, _date_complete, _key, _code_int, _code_string, _code_guid, _name, _note, _hash) 
VALUES (1021, NULL, 1002, 1, 1, NOW(), NOW(), NULL, NULL, NULL, NULL, NULL, NULL, 'AnalyticsRecord Example', NULL, NULL);

-- Заполняем значения для AutoMetrics (объект 1019)
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1022, 1003, 1019, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- AdvertId
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1023, 1004, 1019, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Baskets
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1024, 1005, 1019, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Base
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1025, 1006, 1019, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Association
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1026, 1007, 1019, NULL, NULL, NULL, 0, NULL, NULL, NULL); -- Costs
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1027, 1008, 1019, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Rate
-- Note = null, поэтому не создаем запись

-- Заполняем значения для AuctionMetrics (объект 1020)
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1028, 1003, 1020, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- AdvertId
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1029, 1004, 1020, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Baskets
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1030, 1005, 1020, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Base
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1031, 1006, 1020, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Association
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1032, 1007, 1020, NULL, NULL, NULL, 0, NULL, NULL, NULL); -- Costs
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1033, 1008, 1020, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Rate
-- Note = null, поэтому не создаем запись

-- Заполняем значения для AnalyticsRecord (объект 1021)
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1034, 1010, 1021, NULL, NULL, NULL, NULL, CAST('2025-07-15T00:00:00' AS TIMESTAMP), NULL, NULL); -- Date
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1035, 1011, 1021, 'пт 5х260', NULL, NULL, NULL, NULL, NULL, NULL); -- Article
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1036, 1012, 1021, NULL, 0, NULL, NULL, NULL, NULL, NULL); -- Orders
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1037, 1013, 1021, NULL, 151, NULL, NULL, NULL, NULL, NULL); -- Stock
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1038, 1014, 1021, NULL, 2, NULL, NULL, NULL, NULL, NULL); -- TotalCart
-- Price = null, поэтому не создаем запись
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1039, 1016, 1021, NULL, 1019, NULL, NULL, NULL, NULL, NULL); -- AutoMetrics (ссылка на объект 1019)
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1040, 1017, 1021, NULL, 1020, NULL, NULL, NULL, NULL, NULL); -- AuctionMetrics (ссылка на объект 1020)
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray) VALUES (1041, 1018, 1021, 'пт', NULL, NULL, NULL, NULL, NULL, NULL); -- Tag

INSERT INTO _permissions (_id, _id_user, _id_ref, _select, _insert, _update, _delete)
VALUES (
    nextval('global_identity'),  -- ID записи о правах
    0,        -- ID sys пользователя
    0,                           -- _id_ref = 0 означает "все объекты"
    true,                        -- _select = разрешить чтение
    true,                        -- _insert = разрешить создание
    true,                        -- _update = разрешить изменение
    true                         -- _delete = разрешить удаление
);
INSERT INTO _permissions (_id, _id_user, _id_ref, _select, _insert, _update, _delete)
VALUES (
    nextval('global_identity'),  -- ID записи о правах
    1,        -- ID sys пользователя
    0,                           -- _id_ref = 0 означает "все объекты"
    true,                        -- _select = разрешить чтение
    true,                        -- _insert = разрешить создание
    true,                        -- _update = разрешить изменение
    true                         -- _delete = разрешить удаление
);

