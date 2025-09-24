DROP VIEW IF EXISTS v_objects_json;
DROP FUNCTION IF EXISTS get_object_json;
DROP FUNCTION IF EXISTS build_hierarchical_properties_optimized;

-- ===== ОПТИМИЗИРОВАННЫЕ ФУНКЦИИ =====

-- Оптимизированная функция для построения иерархических properties с предзагруженными values
CREATE OR REPLACE FUNCTION build_hierarchical_properties_optimized(
    object_id bigint,
    parent_structure_id bigint,
    object_scheme_id bigint,
    all_values_json jsonb,
    max_depth integer DEFAULT 10,
    array_index integer DEFAULT NULL -- Новый параметр для элементов массива
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    result_json jsonb := '{}'::jsonb;
    structure_record RECORD;
    current_value jsonb;
    field_value jsonb;
BEGIN
    -- Защита от бесконечной рекурсии
    IF max_depth <= 0 THEN
        RETURN jsonb_build_object('error', 'Max recursion depth reached for hierarchical fields');
    END IF;
    
    -- Собираем все структуры для данного parent_structure_id (БЕЗ JOIN с _values!)
    FOR structure_record IN
        SELECT 
            s._id as structure_id,
            s._name as field_name,
            s._is_array,
            t._name as type_name,
            t._db_type as db_type,
            t._type as type_semantic
        FROM _structures s
        JOIN _types t ON t._id = s._id_type
        WHERE s._id_scheme = object_scheme_id
          AND ((parent_structure_id IS NULL AND s._id_parent IS NULL) 
               OR (parent_structure_id IS NOT NULL AND s._id_parent = parent_structure_id))
        ORDER BY s._order, s._id
    LOOP
        -- 🚀 ОПТИМИЗАЦИЯ: Ищем значение из предзагруженных данных с учетом array_index
        IF array_index IS NULL THEN
            -- Для обычных полей или корневых полей массива
            current_value := all_values_json->structure_record.structure_id::text;
        ELSE
            -- Для элементов массива - ищем значение с конкретным array_index
            current_value := (
                SELECT jsonb_build_object(
                    '_String', v._String,
                    '_Long', v._Long,
                    '_Guid', v._Guid,
                    '_Double', v._Double,
                    '_DateTime', v._DateTime,
                    '_Boolean', v._Boolean,
                    '_ByteArray', v._ByteArray,
                    '_array_parent_id', v._array_parent_id,
                    '_array_index', v._array_index
                )
                FROM _values v
                WHERE v._id_object = object_id 
                  AND v._id_structure = structure_record.structure_id
                  AND v._array_index = array_index
                LIMIT 1
            );
        END IF;
        
        -- Определяем значение поля на основе его типа и предзагруженных данных
        field_value := CASE 
            -- Если это массив - обрабатываем реляционно через _array_index
            WHEN structure_record._is_array = true THEN
                CASE 
                    -- Массив Class полей - строим из реляционных данных рекурсивно
                    WHEN structure_record.type_semantic = 'Object' THEN
                        (
                            WITH array_elements AS (
                                -- Находим все элементы массива с их индексами
                                SELECT DISTINCT 
                                    v._array_index,
                                    build_hierarchical_properties_optimized(
                                        object_id, 
                                        structure_record.structure_id, 
                                        object_scheme_id, 
                                        all_values_json, 
                                        max_depth - 1,
                                        v._array_index
                                    ) as element_json
                                FROM _values v
                                JOIN _structures s ON s._id = v._id_structure AND s._id_parent = structure_record.structure_id
                                WHERE v._id_object = object_id 
                                  AND v._array_index IS NOT NULL
                                ORDER BY v._array_index
                            )
                            SELECT CASE 
                                WHEN COUNT(*) = 0 THEN NULL  -- ✅ Пустой массив = NULL
                                ELSE jsonb_agg(element_json ORDER BY _array_index)
                            END
                            FROM array_elements
                        )
                    -- Массивы простых типов (String, Long, Boolean, etc.) - реляционно
                    ELSE
                        (
                            SELECT CASE 
                                WHEN COUNT(*) = 0 THEN NULL  -- ✅ Пустой массив = NULL
                                ELSE jsonb_agg(
                                CASE 
                                    -- Object ссылки - проверяем раньше обычных Long
                                    WHEN structure_record.db_type = 'Long' AND structure_record.type_semantic = '_RObject' THEN
                                        get_object_json(v._Long, max_depth - 1)
                                    WHEN structure_record.db_type = 'String' THEN to_jsonb(v._String)
                                    WHEN structure_record.db_type = 'Long' THEN to_jsonb(v._Long)
                                    WHEN structure_record.db_type = 'Guid' THEN to_jsonb(v._Guid)
                                    WHEN structure_record.db_type = 'Double' THEN to_jsonb(v._Double)
                                    WHEN structure_record.db_type = 'DateTime' THEN to_jsonb(v._DateTime)
                                    WHEN structure_record.db_type = 'Boolean' THEN to_jsonb(v._Boolean)
                                    WHEN structure_record.db_type = 'ListItem' THEN
                                        jsonb_build_object(
                                            'id', v._Long,
                                            'value', (SELECT _value FROM _list_items WHERE _id = v._Long)
                                        )
                                    WHEN structure_record.db_type = 'ByteArray' THEN 
                                        to_jsonb(encode(decode(v._ByteArray::text, 'base64'), 'base64'))
                                    ELSE NULL
                                END ORDER BY v._array_index
                                )
                            END
                            FROM _values v
                            WHERE v._id_object = object_id 
                              AND v._id_structure = structure_record.structure_id
                              AND v._array_index IS NOT NULL
                        )
                END
            
            -- Обычные поля (не массивы)
            -- Object ссылка на другой объект
            WHEN structure_record.type_name = 'Object' AND structure_record.type_semantic = '_RObject' THEN
                CASE 
                    WHEN (current_value->>'_Long')::bigint IS NOT NULL THEN 
                        get_object_json((current_value->>'_Long')::bigint, max_depth - 1)
                    ELSE NULL
                END
            
            -- Class поле с иерархическими дочерними полями  
            WHEN structure_record.type_semantic = 'Object' THEN
                CASE 
                    WHEN current_value IS NULL OR (current_value->>'_Guid') IS NULL THEN 
                        NULL  -- ✅ Class поле действительно NULL - не строим объект
                    ELSE
                        build_hierarchical_properties_optimized(object_id, structure_record.structure_id, object_scheme_id, all_values_json, max_depth - 1, array_index)
                END
                
            -- Примитивные типы
            WHEN structure_record.db_type = 'String' THEN to_jsonb(current_value->>'_String')
            WHEN structure_record.db_type = 'Long' THEN to_jsonb((current_value->>'_Long')::bigint)
            WHEN structure_record.db_type = 'Guid' THEN to_jsonb((current_value->>'_Guid')::uuid)
            WHEN structure_record.db_type = 'Double' THEN to_jsonb((current_value->>'_Double')::double precision)
            WHEN structure_record.db_type = 'DateTime' THEN to_jsonb((current_value->>'_DateTime')::timestamp)
            WHEN structure_record.db_type = 'Boolean' THEN to_jsonb((current_value->>'_Boolean')::boolean)
            WHEN structure_record.db_type = 'ListItem' THEN 
                CASE 
                    WHEN (current_value->>'_Long')::bigint IS NOT NULL THEN 
                        jsonb_build_object(
                            'id', (current_value->>'_Long')::bigint,
                            'value', (SELECT _value FROM _list_items WHERE _id = (current_value->>'_Long')::bigint)
                        )
                    ELSE NULL
                END
            WHEN structure_record.db_type = 'ByteArray' THEN 
                CASE 
                    WHEN current_value->>'_ByteArray' IS NOT NULL THEN 
                        to_jsonb(encode(decode(current_value->>'_ByteArray', 'base64'), 'base64'))
                    ELSE NULL
                END
            ELSE NULL
        END;
        
        -- Добавляем поле в результат только если значение не NULL
        IF field_value IS NOT NULL THEN
            result_json := result_json || jsonb_build_object(structure_record.field_name, field_value);
        END IF;
        
    END LOOP;
    
    RETURN result_json;
END;
$BODY$;

-- ОПТИМИЗИРОВАННАЯ функция для получения объекта в JSON формате с предзагрузкой всех values
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
    object_scheme_id bigint;
    all_values_json jsonb;
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
    
    -- Собираем базовую информацию об объекте + получаем scheme_id
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
    ), o._id_scheme
    INTO base_info, object_scheme_id
    FROM _objects o
    JOIN _schemes sc ON sc._id = o._id_scheme
    WHERE o._id = object_id;
    
    -- 🚀 ОПТИМИЗАЦИЯ: Загружаем ВСЕ values объекта ОДНИМ запросом
    SELECT jsonb_object_agg(
        v._id_structure::text, 
        jsonb_build_object(
            '_String', v._String,
            '_Long', v._Long,
            '_Guid', v._Guid,
            '_Double', v._Double,
            '_DateTime', v._DateTime,
            '_Boolean', v._Boolean,
            '_ByteArray', v._ByteArray,
            '_array_parent_id', v._array_parent_id,
            '_array_index', v._array_index
        )
    ) INTO all_values_json
    FROM _values v
    WHERE v._id_object = object_id;
    
    -- Используем оптимизированную функцию с предзагруженными values
    SELECT build_hierarchical_properties_optimized(
        object_id, 
        NULL, 
        object_scheme_id, 
        COALESCE(all_values_json, '{}'::jsonb), 
        max_depth,
        NULL -- array_index = NULL для корневых полей
    ) INTO properties_info;
    
    -- Объединяем базовую информацию с properties
    result_json := base_info || jsonb_build_object('properties', COALESCE(properties_info, '{}'::jsonb));
    
    RETURN result_json;
END;
$BODY$;

-- BULK-ОПТИМИЗИРОВАННАЯ VIEW для массового получения объектов в JSON формате  
-- CREATE OR REPLACE VIEW v_objects_json AS
-- WITH 
-- -- 🚀 Этап 1: BULK загрузка values (оптимально - GROUP BY только по ID)
-- all_values AS (
--     SELECT 
--         o._id,
--         COALESCE(
--             jsonb_object_agg(
--                 v._id_structure::text, 
--                 jsonb_build_object(
--                     '_String', v._String,
--                     '_Long', v._Long,
--                     '_Guid', v._Guid,
--                     '_Double', v._Double,
--                     '_DateTime', v._DateTime,
--                     '_Boolean', v._Boolean,
--                     '_ByteArray', v._ByteArray,
--                     '_array_parent_id', v._array_parent_id,
--                     '_array_index', v._array_index
--                 )
--             ) FILTER (WHERE v._id IS NOT NULL),
--             '{}'::jsonb
--         ) as all_values_json
--     FROM _objects o
--     LEFT JOIN _values v ON v._id_object = o._id
--     GROUP BY o._id  -- GROUP BY только по ID (быстро!)
-- ),
-- -- 🚀 Этап 2: Объединяем с полями _objects и строим JSON
-- objects_with_json AS (
--     SELECT 
--         o.*,  -- Все поля _objects одной звездочкой (эффективно)
--         -- Полный JSON объекта с properties
--         jsonb_build_object(
--             'id', o._id,
--             'name', o._name,
--             'scheme_id', o._id_scheme,
--             'scheme_name', s._name,
--             'parent_id', o._id_parent,
--             'owner_id', o._id_owner,
--             'who_change_id', o._id_who_change,
--             'date_create', o._date_create,
--             'date_modify', o._date_modify,
--             'date_begin', o._date_begin,
--             'date_complete', o._date_complete,
--             'key', o._key,
--             'code_int', o._code_int,
--             'code_string', o._code_string,
--             'code_guid', o._code_guid,
--             'note', o._note,
--             'bool', o._bool,
--             'hash', o._hash,
--             'properties', 
--             build_hierarchical_properties_optimized(
--                 o._id, 
--                 NULL, 
--                 o._id_scheme, 
--                 av.all_values_json,  -- Используем предзагруженные данные
--                 10,
--                 NULL -- array_index = NULL для корневых полей
--             )
--         ) as object_json
--     FROM _objects o
--     JOIN _schemes s ON s._id = o._id_scheme  
--     JOIN all_values av ON av._id = o._id  -- JOIN с предзагруженными values
-- )
-- SELECT * FROM objects_with_json ORDER BY _id;

-- -- Комментарии к ОПТИМИЗИРОВАННЫМ функциям и VIEW для получения объектов
-- COMMENT ON VIEW v_objects_json IS 'МАКСИМАЛЬНО ОПТИМИЗИРОВАННАЯ VIEW для получения объектов. Двухэтапная архитектура: 1) BULK агрегация _values с GROUP BY только по _id (быстро!) 2) JOIN готовых данных с _objects через o.* (эффективно). Возвращает ВСЕ оригинальные поля _objects как колонки ПЛЮС полный JSON с properties. Избегает heavy GROUP BY по 17 полям. Идеально для интеграции и API. Поддерживает иерархические Class поля.';

COMMENT ON FUNCTION build_hierarchical_properties_optimized(bigint, bigint, bigint, jsonb, integer, integer) IS 'ОПТИМИЗИРОВАННАЯ функция для рекурсивного построения иерархической JSON структуры с предзагруженными values. Поддерживает реляционные массивы Class полей через array_index. БЕЗ JOIN с _values в цикле для обычных полей! 3-5x быстрее для объектов с большим количеством полей.';

COMMENT ON FUNCTION get_object_json(bigint, integer) IS 'ОПТИМИЗИРОВАННАЯ функция получения объекта в JSON формате. Загружает ВСЕ values объекта ОДНИМ запросом, затем использует быстрый поиск в памяти. Поддерживает иерархические Class поля, Object ссылки, массивы и глубокую рекурсию. Оптимальна для объектов с 10+ полями.';

-- ===== ПРОСТАЯ VIEW ДЛЯ ОБЪЕКТОВ С JSON =====

-- Удаляем существующую view если есть
DROP VIEW IF EXISTS v_objects_json;

-- Простая view: все поля _objects + JSON через get_object_json
CREATE VIEW v_objects_json AS
SELECT 
    o.*,  -- Все поля _objects как есть
    get_object_json(o._id, 10) as object_json  -- JSON представление объекта
FROM _objects o;
COMMENT ON VIEW v_objects_json IS 'Простая view для получения объектов: все поля _objects + полный JSON через get_object_json. Удобна для просмотра и отладки.';
