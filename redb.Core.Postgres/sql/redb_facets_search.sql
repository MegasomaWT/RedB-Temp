-- ===== REDB FACETS & SEARCH MODULE =====
-- Модуль для фасетного поиска и фильтрации объектов
-- Архитектура: Модульная система от Руслана + наши реляционные массивы + Class поля
-- Включает: LINQ операторы, логические операторы, Class поля, иерархический поиск

-- ===== ОЧИСТКА СУЩЕСТВУЮЩИХ ФУНКЦИЙ =====
DROP FUNCTION IF EXISTS _format_json_array_for_in CASCADE;
DROP FUNCTION IF EXISTS _parse_field_path CASCADE;
DROP FUNCTION IF EXISTS _find_structure_info CASCADE;
DROP FUNCTION IF EXISTS _build_inner_condition CASCADE;
DROP FUNCTION IF EXISTS _build_exists_condition CASCADE;
DROP FUNCTION IF EXISTS _build_and_condition CASCADE;
DROP FUNCTION IF EXISTS _build_or_condition CASCADE;
DROP FUNCTION IF EXISTS _build_not_condition CASCADE;
DROP FUNCTION IF EXISTS _build_single_facet_condition CASCADE;
DROP FUNCTION IF EXISTS _build_facet_field_path CASCADE;
DROP FUNCTION IF EXISTS get_facets CASCADE;
-- DROP FUNCTION IF EXISTS build_advanced_facet_conditions CASCADE; -- ✅ УДАЛЕНА В ВАРИАНТЕ C
-- DROP FUNCTION IF EXISTS build_base_facet_conditions CASCADE; -- ✅ УДАЛЕНА! МЕРТВЫЙ КОД!
DROP FUNCTION IF EXISTS build_order_conditions CASCADE;
DROP FUNCTION IF EXISTS build_has_ancestor_condition CASCADE;
DROP FUNCTION IF EXISTS build_has_descendant_condition CASCADE;
DROP FUNCTION IF EXISTS build_level_condition CASCADE;
DROP FUNCTION IF EXISTS build_hierarchical_conditions CASCADE;
DROP FUNCTION IF EXISTS execute_objects_query CASCADE;
DROP FUNCTION IF EXISTS search_objects_with_facets CASCADE;
DROP FUNCTION IF EXISTS search_tree_objects_with_facets CASCADE;

-- ===== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ =====

-- Функция для форматирования JSON массива для оператора IN
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
                -- ✅ ИСПРАВЛЕНИЕ: Извлекаем чистую строку БЕЗ JSON кавычек, потом квотируем
                element_text := quote_literal(json_element #>> '{}');
            WHEN 'number' THEN
                element_text := json_element::text;
            WHEN 'boolean' THEN
                element_text := CASE WHEN (json_element)::boolean THEN 'true' ELSE 'false' END;
            ELSE
                -- ✅ ИСПРАВЛЕНИЕ: И здесь тоже для других типов
                element_text := quote_literal(json_element #>> '{}');
        END CASE;
        
        in_values := in_values || element_text;
    END LOOP;
    
    RETURN in_values;
END;
$BODY$;

COMMENT ON FUNCTION _format_json_array_for_in(jsonb) IS 'Преобразует JSONB массив в строку значений для SQL IN clause. Поддерживает string, number, boolean типы. Используется в операторах $in.';

-- Функция парсинга пути поля для Class полей и массивов
CREATE OR REPLACE FUNCTION _parse_field_path(
    field_path text
) RETURNS TABLE (
    root_field text,
    nested_field text, 
    is_array boolean,
    is_nested boolean
)
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
BEGIN
    -- Определяем является ли поле массивом (содержит [])
    is_array := field_path LIKE '%[]%';
    
    -- Определяем является ли поле вложенным (содержит точку)
    is_nested := field_path LIKE '%.%';
    
    IF is_nested THEN
        IF is_array THEN
            -- Случай: "Contacts[].Email" -> root="Contacts", nested="Email", is_array=true
            root_field := split_part(replace(field_path, '[]', ''), '.', 1);
            nested_field := split_part(replace(field_path, '[]', ''), '.', 2);
        ELSE
            -- Случай: "Contact.Name" -> root="Contact", nested="Name", is_array=false  
            root_field := split_part(field_path, '.', 1);
            nested_field := split_part(field_path, '.', 2);
        END IF;
    ELSE
        IF is_array THEN
            -- Случай: "Tags[]" -> root="Tags", nested=NULL, is_array=true
            root_field := replace(field_path, '[]', '');
            nested_field := NULL;
        ELSE
            -- Случай: "Name" -> root="Name", nested=NULL, is_array=false
            root_field := field_path;
            nested_field := NULL;
        END IF;
    END IF;
    
    RETURN QUERY SELECT root_field, nested_field, is_array, is_nested;
END;
$BODY$;

COMMENT ON FUNCTION _parse_field_path(text) IS 'Парсит путь поля для поддержки Class полей и массивов. Поддерживает: "Name", "Contact.Name", "Tags[]", "Contacts[].Email". Возвращает компоненты пути для дальнейшей обработки.';

-- Функция поиска информации о структурах для Class полей
CREATE OR REPLACE FUNCTION _find_structure_info(
    scheme_id bigint,
    root_field text,
    nested_field text DEFAULT NULL
) RETURNS TABLE (
    root_structure_id bigint,
    nested_structure_id bigint,
    root_type_info jsonb,
    nested_type_info jsonb
)
LANGUAGE 'plpgsql'
COST 50
VOLATILE NOT LEAKPROOF  
AS $BODY$
DECLARE
    scheme_def jsonb;
BEGIN
    -- Получаем определение схемы используя существующую функцию
    SELECT get_scheme_definition(scheme_id) INTO scheme_def;
    
    -- Ищем корневую структуру
    SELECT 
        (struct->>'_id')::bigint,
        jsonb_build_object(
            'type_name', struct->>'_type_name',
            'db_type', struct->>'_type_db_type', 
            'type_semantic', struct->>'_type_dotnet_type',
            'is_array', (struct->>'_is_array')::boolean
        )
    INTO root_structure_id, root_type_info
    FROM jsonb_array_elements(scheme_def->'structures') AS struct
    WHERE struct->>'_name' = root_field
      AND struct->>'_id_parent' IS NULL;
    
    -- Если есть вложенное поле, ищем его структуру
    IF nested_field IS NOT NULL AND root_structure_id IS NOT NULL THEN
        SELECT 
            (struct->>'_id')::bigint,
            jsonb_build_object(
                'type_name', struct->>'_type_name',
                'db_type', struct->>'_type_db_type',
                'type_semantic', struct->>'_type_dotnet_type', 
                'is_array', (struct->>'_is_array')::boolean
            )
        INTO nested_structure_id, nested_type_info
        FROM jsonb_array_elements(scheme_def->'structures') AS struct
        WHERE struct->>'_name' = nested_field
          AND (struct->>'_id_parent')::bigint = root_structure_id;
    ELSE
        nested_structure_id := NULL;
        nested_type_info := NULL;
    END IF;
    
    RETURN QUERY SELECT root_structure_id, nested_structure_id, root_type_info, nested_type_info;
END;
$BODY$;

COMMENT ON FUNCTION _find_structure_info(bigint, text, text) IS 'Находит информацию о структурах для Class полей используя get_scheme_definition. Возвращает ID структур и метаданные типов для корневого и вложенного полей.';

-- ===== ЯДРО СИСТЕМЫ: LINQ ОПЕРАТОРЫ =====

-- Функция построения внутренних условий с поддержкой всех LINQ операторов
CREATE OR REPLACE FUNCTION _build_inner_condition(
    operator_name text,
    operator_value text,
    type_info jsonb  -- Информация о типе из _find_structure_info
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    op_symbol text;
    pattern text;
    in_values_list text;
    db_type text := type_info->>'db_type';
    is_array boolean := (type_info->>'is_array')::boolean;
BEGIN
    -- Числовые и DateTime операторы
    IF operator_name IN ('$gt', '$lt', '$gte', '$lte') THEN
        CASE operator_name
            WHEN '$gt' THEN op_symbol := '>';
            WHEN '$lt' THEN op_symbol := '<';
            WHEN '$gte' THEN op_symbol := '>=';
            WHEN '$lte' THEN op_symbol := '<=';
        END CASE;
        
        -- Определяем тип данных по формату значения
        IF operator_value ~ '^\d{4}-\d{2}-\d{2}' THEN
            -- DateTime формат (YYYY-MM-DD...)
            RETURN format('ft._db_type = ''DateTime'' AND fv._DateTime %s %L::timestamp',
                op_symbol, operator_value);
        ELSE
            -- Числовые типы
            RETURN format('((ft._db_type = ''Long'' AND fv._Long %s %L) OR (ft._db_type = ''Double'' AND fv._Double %s %L))',
                op_symbol, operator_value::bigint, op_symbol, operator_value::double precision);
        END IF;
    
    -- Строковые операторы (чувствительные к регистру)
    ELSIF operator_name IN ('$startsWith', '$endsWith', '$contains') THEN
        CASE operator_name
            WHEN '$startsWith' THEN pattern := operator_value || '%';
            WHEN '$endsWith' THEN pattern := '%' || operator_value;
            WHEN '$contains' THEN pattern := '%' || operator_value || '%';
        END CASE;
        
        RETURN format('ft._db_type = ''String'' AND fv._String LIKE %L', pattern);
    
    -- ✅ ИСПРАВЛЕНИЕ: Строковые операторы (регистронезависимые)
    ELSIF operator_name IN ('$startsWithIgnoreCase', '$endsWithIgnoreCase', '$containsIgnoreCase') THEN
        CASE operator_name
            WHEN '$startsWithIgnoreCase' THEN pattern := operator_value || '%';
            WHEN '$endsWithIgnoreCase' THEN pattern := '%' || operator_value;
            WHEN '$containsIgnoreCase' THEN pattern := '%' || operator_value || '%';
        END CASE;
        
        RETURN format('ft._db_type = ''String'' AND fv._String ILIKE %L', pattern);
    
    -- Оператор IN
    ELSIF operator_name = '$in' THEN
        in_values_list := _format_json_array_for_in(operator_value::jsonb);
        -- 🚀 ПРАВИЛЬНО: Используем type_info для определения типа поля (из структур)
        IF db_type = 'String' THEN
            RETURN format('ft._db_type = ''String'' AND fv._String IN (%s)', in_values_list);
        ELSIF db_type = 'Long' THEN
            RETURN format('ft._db_type = ''Long'' AND fv._Long IN (%s)', in_values_list);
        ELSIF db_type = 'Double' THEN
            RETURN format('ft._db_type = ''Double'' AND fv._Double IN (%s)', in_values_list);
        ELSIF db_type = 'Boolean' THEN
            RETURN format('ft._db_type = ''Boolean'' AND fv._Boolean IN (%s)', in_values_list);
        ELSIF db_type = 'DateTime' THEN
            RETURN format('ft._db_type = ''DateTime'' AND fv._DateTime IN (%s)', in_values_list);
        ELSE
            -- Fallback: пробуем все типы (как было раньше, но с правильной типизацией)
            RETURN format('((ft._db_type = ''String'' AND fv._String IN (%s)) OR (ft._db_type = ''Long'' AND fv._Long IN (%s)) OR (ft._db_type = ''Double'' AND fv._Double IN (%s)) OR (ft._db_type = ''Boolean'' AND fv._Boolean IN (%s)) OR (ft._db_type = ''DateTime'' AND fv._DateTime IN (%s)))',
                in_values_list, in_values_list, in_values_list, in_values_list, in_values_list);
        END IF;
    
    -- Оператор NOT EQUAL - требует специальной обработки
    ELSIF operator_name = '$ne' THEN
        -- Для $ne null это особый случай - ищем существующие записи (в EAV null = нет записи)
        IF operator_value IS NULL OR operator_value = 'null' OR operator_value = '' THEN
            -- $ne null означает "поле существует" (в EAV модели null значения не сохраняются)  
            -- Это будет обработано через обычный EXISTS, а не NOT EXISTS
            RETURN 'TRUE';  -- Любая существующая запись означает "не null"
        ELSE
            -- $ne конкретное значение - строим позитивное условие для отрицания через NOT EXISTS
            IF operator_value ~ '^\d{4}-\d{2}-\d{2}' THEN
                RETURN format('ft._db_type = ''DateTime'' AND fv._DateTime = %L::timestamp', operator_value);
            ELSIF operator_value ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN
                RETURN format('ft._db_type = ''Guid'' AND fv._Guid = %L::uuid', operator_value);
            ELSIF operator_value ~ '^-?\d+(\.\d+)?$' THEN
                RETURN format('ft._db_type = ''Long'' AND fv._Long = %L::bigint', operator_value);
            ELSIF operator_value IN ('true', 'false') THEN
                RETURN format('ft._db_type = ''Boolean'' AND fv._Boolean = %L::boolean', operator_value);
            ELSE
                RETURN format('ft._db_type = ''String'' AND fv._String = %L', operator_value);
            END IF;
        END IF;
    
    -- Оператор явного равенства
    ELSIF operator_name = '$eq' THEN
        -- Явный оператор равенства - определяем тип по формату значения (аналогично простому равенству)
        IF operator_value ~ '^\d{4}-\d{2}-\d{2}' THEN
            -- DateTime формат (YYYY-MM-DD...)
            RETURN format('ft._db_type = ''DateTime'' AND fv._DateTime = %L::timestamp', operator_value);
        ELSIF operator_value ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN
            -- GUID формат (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
            RETURN format('ft._db_type = ''Guid'' AND fv._Guid = %L::uuid', operator_value);
        ELSIF operator_value ~ '^-?\d+(\.\d+)?$' THEN
            -- ✅ ИСПРАВЛЕНИЕ: Числовое значение - УМНАЯ конверсия типов
            IF operator_value ~ '^-?\d+$' THEN
                -- Целое число - проверяем Long и Double
                RETURN format('((ft._db_type = ''Long'' AND fv._Long = %L::bigint) OR (ft._db_type = ''Double'' AND fv._Double = %L::double precision))', 
                    operator_value, operator_value);
            ELSE
                -- Десятичное число - ТОЛЬКО Double (bigint не поддерживает "2000.0")
                RETURN format('(ft._db_type = ''Double'' AND fv._Double = %L::double precision)', 
                    operator_value);
            END IF;
        ELSIF operator_value IN ('true', 'false') THEN
            -- Boolean значение
            RETURN format('ft._db_type = ''Boolean'' AND fv._Boolean = %L::boolean', operator_value);
        ELSE
            -- Строковое значение (по умолчанию)
            RETURN format('ft._db_type = ''String'' AND fv._String = %L', operator_value);
        END IF;
    
    -- 🚀 РАСШИРЕННЫЕ ОПЕРАТОРЫ РЕЛЯЦИОННЫХ МАССИВОВ
    ELSIF operator_name = '$arrayContains' THEN
        -- Ищем значение в реляционном массиве с УМНОЙ типизацией (избегаем ошибок приведения)
        IF operator_value ~ '^-?\d+$' THEN
            -- Числовое значение
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av 
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure  
              AND av._array_index IS NOT NULL
                  AND ft._db_type = ''Long'' AND ft._type != ''_RObject'' 
                  AND av._Long = %L::bigint
            )', operator_value);
        ELSIF operator_value IN ('true', 'false') THEN
            -- Boolean значение
            RETURN format('fs._is_array = true AND EXISTS(
                SELECT 1 FROM _values av 
                WHERE av._id_object = fv._id_object
                  AND av._id_structure = fv._id_structure  
                  AND av._array_index IS NOT NULL
                  AND ft._db_type = ''Boolean''
                  AND av._Boolean = %L::boolean
            )', operator_value);
        ELSIF operator_value ~ '^\d{4}-\d{2}-\d{2}' THEN
            -- DateTime значение
            RETURN format('fs._is_array = true AND EXISTS(
                SELECT 1 FROM _values av 
                WHERE av._id_object = fv._id_object
                  AND av._id_structure = fv._id_structure  
                  AND av._array_index IS NOT NULL
                  AND ft._db_type = ''DateTime''
                  AND av._DateTime = %L::timestamp
            )', operator_value);
        ELSE
            -- Строковое значение (по умолчанию)
            RETURN format('fs._is_array = true AND EXISTS(
                SELECT 1 FROM _values av 
                WHERE av._id_object = fv._id_object
                  AND av._id_structure = fv._id_structure  
                  AND av._array_index IS NOT NULL
                  AND ft._db_type = ''String''
                  AND av._String = %L
            )', operator_value);
        END IF;
    
    -- Оператор проверки непустого массива  
    ELSIF operator_name = '$arrayAny' THEN
        -- Проверяем что реляционный массив не пустой
        RETURN 'fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        )';
    
    -- Оператор проверки пустого массива
    ELSIF operator_name = '$arrayEmpty' THEN
        -- Проверяем что реляционный массив пустой
        RETURN 'fs._is_array = true AND NOT EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure  
              AND av._array_index IS NOT NULL
        )';
    
    -- 📊 ОПЕРАТОРЫ ПОДСЧЕТА ЭЛЕМЕНТОВ МАССИВА
    ELSIF operator_name = '$arrayCount' THEN
        RETURN format('fs._is_array = true AND (
            SELECT COUNT(*) FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        ) = %L::int', operator_value::int);
    
    ELSIF operator_name = '$arrayCountGt' THEN
        RETURN format('fs._is_array = true AND (
            SELECT COUNT(*) FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        ) > %L::int', operator_value::int);
    
    ELSIF operator_name = '$arrayCountGte' THEN
        RETURN format('fs._is_array = true AND (
            SELECT COUNT(*) FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        ) >= %L::int', operator_value::int);
    
    ELSIF operator_name = '$arrayCountLt' THEN
        RETURN format('fs._is_array = true AND (
            SELECT COUNT(*) FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        ) < %L::int', operator_value::int);
    
    ELSIF operator_name = '$arrayCountLte' THEN
        RETURN format('fs._is_array = true AND (
            SELECT COUNT(*) FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        ) <= %L::int', operator_value::int);
    
    -- 🎯 НОВЫЕ ОПЕРАТОРЫ ДЛЯ РЕЛЯЦИОННЫХ МАССИВОВ
    ELSIF operator_name = '$arrayAt' THEN
        -- Получить элемент массива по индексу
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index = %L::int
        )', operator_value::int);
    
    ELSIF operator_name = '$arrayFirst' THEN
        -- Проверить первый элемент массива
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index = 0
              AND (
                (ft._db_type = ''String'' AND av._String = %L) OR
                (ft._db_type = ''Long'' AND ft._type != ''_RObject'' AND av._Long = %L::bigint) OR
                (ft._db_type = ''Double'' AND av._Double = %L::double precision) OR
                (ft._db_type = ''Boolean'' AND av._Boolean = %L::boolean) OR
                (ft._db_type = ''Guid'' AND ft._type != ''Object'' AND av._Guid = %L::uuid) OR
                (ft._db_type = ''DateTime'' AND av._DateTime = %L::timestamp)
              )
        )', operator_value, operator_value, operator_value, operator_value, operator_value, operator_value);
    
    ELSIF operator_name = '$arrayLast' THEN
        -- Проверить последний элемент массива
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index = (
                SELECT MAX(av2._array_index) FROM _values av2
                WHERE av2._id_object = fv._id_object
                  AND av2._id_structure = fv._id_structure
                  AND av2._array_index IS NOT NULL
              )
              AND (
                (ft._db_type = ''String'' AND av._String = %L) OR
                (ft._db_type = ''Long'' AND ft._type != ''_RObject'' AND av._Long = %L::bigint) OR
                (ft._db_type = ''Double'' AND av._Double = %L::double precision) OR
                (ft._db_type = ''Boolean'' AND av._Boolean = %L::boolean) OR
                (ft._db_type = ''Guid'' AND ft._type != ''Object'' AND av._Guid = %L::uuid) OR
                (ft._db_type = ''DateTime'' AND av._DateTime = %L::timestamp)
              )
        )', operator_value, operator_value, operator_value, operator_value, operator_value, operator_value);
    
    -- 🔍 ОПЕРАТОРЫ ПОИСКА В МАССИВАХ
    ELSIF operator_name = '$arrayStartsWith' THEN
        -- Ищем строковые значения в массиве, которые начинаются с префикса
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
              AND ft._db_type = ''String''
              AND av._String LIKE %L
        )', operator_value || '%');
    
    ELSIF operator_name = '$arrayEndsWith' THEN
        -- Ищем строковые значения в массиве, которые заканчиваются суффиксом
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
              AND ft._db_type = ''String''
              AND av._String LIKE %L
        )', '%' || operator_value);
    
    ELSIF operator_name = '$arrayMatches' THEN
        -- Поиск по регулярному выражению в строковых элементах массива
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
              AND ft._db_type = ''String''
              AND av._String ~ %L
        )', operator_value);
    
    -- 📈 ОПЕРАТОРЫ АГРЕГАЦИИ МАССИВОВ
    ELSIF operator_name = '$arraySum' THEN
        -- Сумма числовых элементов массива
        RETURN format('fs._is_array = true AND (
            SELECT COALESCE(SUM(
                CASE 
                    WHEN ft._db_type = ''Long'' THEN av._Long
                    WHEN ft._db_type = ''Double'' THEN av._Double
                    ELSE 0
                END
            ), 0) FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        ) = %L::numeric', operator_value::numeric);
    
    ELSIF operator_name = '$arrayAvg' THEN
        -- Среднее арифметическое числовых элементов массива
        RETURN format('fs._is_array = true AND (
            SELECT AVG(
                CASE 
                    WHEN ft._db_type = ''Long'' THEN av._Long::numeric
                    WHEN ft._db_type = ''Double'' THEN av._Double
                    ELSE NULL
                END
            ) FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
        ) = %L::numeric', operator_value::numeric);
    
    ELSIF operator_name = '$arrayMin' THEN
        -- Минимальное значение в массиве
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
              AND (
                (ft._db_type = ''Long'' AND av._Long = (
                    SELECT MIN(av2._Long) FROM _values av2
                    WHERE av2._id_object = av._id_object
                      AND av2._id_structure = av._id_structure
                      AND av2._array_index IS NOT NULL
                      AND av2._Long IS NOT NULL
                )) OR
                (ft._db_type = ''Double'' AND av._Double = (
                    SELECT MIN(av2._Double) FROM _values av2
                    WHERE av2._id_object = av._id_object
                      AND av2._id_structure = av._id_structure
                      AND av2._array_index IS NOT NULL
                      AND av2._Double IS NOT NULL
                )) OR
                (ft._db_type = ''DateTime'' AND av._DateTime = (
                    SELECT MIN(av2._DateTime) FROM _values av2
                    WHERE av2._id_object = av._id_object
                      AND av2._id_structure = av._id_structure
                      AND av2._array_index IS NOT NULL
                      AND av2._DateTime IS NOT NULL
                ))
              )
              AND (
                (ft._db_type = ''Long'' AND av._Long = %L::bigint) OR
                (ft._db_type = ''Double'' AND av._Double = %L::double precision) OR
                (ft._db_type = ''DateTime'' AND av._DateTime = %L::timestamp)
              )
        )', operator_value, operator_value, operator_value);
    
    ELSIF operator_name = '$arrayMax' THEN
        -- Максимальное значение в массиве  
        RETURN format('fs._is_array = true AND EXISTS(
            SELECT 1 FROM _values av
            WHERE av._id_object = fv._id_object
              AND av._id_structure = fv._id_structure
              AND av._array_index IS NOT NULL
              AND (
                (ft._db_type = ''Long'' AND av._Long = (
                    SELECT MAX(av2._Long) FROM _values av2
                    WHERE av2._id_object = av._id_object
                      AND av2._id_structure = av._id_structure
                      AND av2._array_index IS NOT NULL
                      AND av2._Long IS NOT NULL
                )) OR
                (ft._db_type = ''Double'' AND av._Double = (
                    SELECT MAX(av2._Double) FROM _values av2
                    WHERE av2._id_object = av._id_object
                      AND av2._id_structure = av._id_structure
                      AND av2._array_index IS NOT NULL
                      AND av2._Double IS NOT NULL
                )) OR
                (ft._db_type = ''DateTime'' AND av._DateTime = (
                    SELECT MAX(av2._DateTime) FROM _values av2
                    WHERE av2._id_object = av._id_object
                      AND av2._id_structure = av._id_structure
                      AND av2._array_index IS NOT NULL
                      AND av2._DateTime IS NOT NULL
                ))
              )
              AND (
                (ft._db_type = ''Long'' AND av._Long = %L::bigint) OR
                (ft._db_type = ''Double'' AND av._Double = %L::double precision) OR
                (ft._db_type = ''DateTime'' AND av._DateTime = %L::timestamp)
              )
        )', operator_value, operator_value, operator_value);
    
    ELSE
        -- Простое равенство - определяем тип по формату значения
        IF operator_value ~ '^\d{4}-\d{2}-\d{2}' THEN
            -- DateTime формат (YYYY-MM-DD)
            RETURN format('ft._db_type = ''DateTime'' AND fv._DateTime = %L::timestamp', operator_value);
        ELSIF operator_value ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN
            -- GUID формат (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
            RETURN format('ft._db_type = ''Guid'' AND fv._Guid = %L::uuid', operator_value);
        ELSIF operator_value ~ '^-?\d+(\.\d+)?$' THEN
            -- ✅ ИСПРАВЛЕНИЕ: Числовое значение - УМНАЯ конверсия типов
            IF operator_value ~ '^-?\d+$' THEN
                -- Целое число - проверяем Long и Double
                RETURN format('((ft._db_type = ''Long'' AND fv._Long = %L::bigint) OR (ft._db_type = ''Double'' AND fv._Double = %L::double precision))', 
                    operator_value, operator_value);
            ELSE
                -- Десятичное число - ТОЛЬКО Double (bigint не поддерживает "2000.0")
                RETURN format('(ft._db_type = ''Double'' AND fv._Double = %L::double precision)', 
                    operator_value);
            END IF;
        ELSIF operator_value IN ('true', 'false') THEN
            -- Boolean значение
            RETURN format('ft._db_type = ''Boolean'' AND fv._Boolean = %L::boolean', operator_value);
        ELSE
            -- Строковое значение (по умолчанию)
            RETURN format('ft._db_type = ''String'' AND fv._String = %L', operator_value);
        END IF;
    END IF;
END;
$BODY$;

COMMENT ON FUNCTION _build_inner_condition(text, text, jsonb) IS '🚀 РАСШИРЕННОЕ ядро системы LINQ операторов. Поддерживает 25+ операторов: 
📊 Числовые: $gt, $gte, $lt, $lte, $ne, $in
📝 Строковые: $contains, $startsWith, $endsWith  
🔢 Массивы (базовые): $arrayContains, $arrayAny, $arrayEmpty, $arrayCount*
🎯 Массивы (позиция): $arrayAt, $arrayFirst, $arrayLast
🔍 Массивы (поиск): $arrayStartsWith, $arrayEndsWith, $arrayMatches
📈 Массивы (агрегация): $arraySum, $arrayAvg, $arrayMin, $arrayMax
Все операторы адаптированы под реляционные массивы через _array_index. Поддерживает различение _RObject vs Object типов. Автоопределение типов по формату значений.';

-- ===== УНИВЕРСАЛЬНЫЕ ОБЕРТКИ =====

-- Универсальная функция построения EXISTS/NOT EXISTS условий с полной поддержкой Class полей
CREATE OR REPLACE FUNCTION _build_exists_condition(
    field_path text,
    condition_sql text,
    use_not_exists boolean DEFAULT false,
    scheme_id bigint DEFAULT NULL,
    table_alias text DEFAULT 'o'
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    parsed_path RECORD;
    structure_info RECORD;
    exists_query text;
    field_condition text;
    nested_join text := '';
    nested_condition text := '';
BEGIN
    -- Парсим путь поля
    SELECT * INTO parsed_path FROM _parse_field_path(field_path);
    
    -- 📦 CLASS ПОЛЯ (Contact.Name синтаксис)
    IF parsed_path.is_nested AND scheme_id IS NOT NULL THEN
        SELECT * INTO structure_info 
        FROM _find_structure_info(scheme_id, parsed_path.root_field, parsed_path.nested_field);
        
        -- Проверяем что найдены обе структуры
        IF structure_info.root_structure_id IS NULL THEN
            RAISE EXCEPTION 'Не найдена корневая структура для поля: %', parsed_path.root_field;
        END IF;
        
        IF structure_info.nested_structure_id IS NULL THEN
            RAISE EXCEPTION 'Не найдена вложенная структура % в поле %', parsed_path.nested_field, parsed_path.root_field;
        END IF;
        
        -- 🔍 ОБРАБОТКА CLASS МАССИВОВ (Contact[].Name)
        IF parsed_path.is_array THEN
            -- ✅ ИСПРАВЛЕНИЕ: Для Class массивов добавляем JOIN для типа вложенного поля
            nested_join := format('
                JOIN _values nv ON nv._id_object = fv._id_object
                  AND nv._id_structure = %s
                  AND nv._array_parent_id = fv._id  -- связь с родительским массивом Class элементов  
                  AND nv._array_index IS NOT NULL
                JOIN _types nt ON nt._id = (SELECT _id_type FROM _structures WHERE _id = %s)', -- тип вложенного поля
                structure_info.nested_structure_id,
                structure_info.nested_structure_id);
            
            -- ✅ ИСПРАВЛЕНИЕ: Заменяем типы для массивов тоже!
            nested_condition := replace(replace(condition_sql, 'fv.', 'nv.'), 'ft.', 'nt.');
            field_condition := format(
                'fs._id = %s AND fs._is_array = true AND fv._array_index IS NOT NULL AND %s', 
                structure_info.root_structure_id, 
                nested_condition
            );
        
        -- 🔍 ОБЫЧНЫЕ CLASS ПОЛЯ (Contact.Name)  
        ELSE
            -- ✅ ИСПРАВЛЕНИЕ: Для вложенных Class полей добавляем JOIN для типа вложенного поля
            nested_join := format('
                JOIN _values nv ON nv._id_object = fv._id_object
                  AND nv._id_structure = %s
                  AND nv._array_index IS NULL
                JOIN _types nt ON nt._id = (SELECT _id_type FROM _structures WHERE _id = %s)', -- тип вложенного поля
                structure_info.nested_structure_id,
                structure_info.nested_structure_id);
            
            -- ✅ ИСПРАВЛЕНИЕ: Условие работает с вложенным полем - заменяем типы тоже!
            nested_condition := replace(replace(condition_sql, 'fv.', 'nv.'), 'ft.', 'nt.');
            field_condition := format(
                'fs._id = %s AND fs._is_array = false AND fv._array_index IS NULL AND %s', 
                structure_info.root_structure_id, 
                nested_condition
            );
        END IF;
    
    -- 📋 ОБЫЧНЫЕ ПОЛЯ И МАССИВЫ (Name, Tags[])
    ELSE
        IF parsed_path.is_array THEN
            -- Обычные массивы (Tags[])
            field_condition := format('fs._name = %L AND fs._is_array = true AND %s', 
                                    parsed_path.root_field, 
                                    condition_sql);
        ELSE
            -- Обычные поля (Name)
            field_condition := format('fs._name = %L AND fs._is_array = false AND fv._array_index IS NULL AND %s', 
                                    parsed_path.root_field, 
                                    condition_sql);
        END IF;
        nested_join := '';
    END IF;
    
    -- 🚀 СТРОИМ ФИНАЛЬНЫЙ EXISTS ЗАПРОС
    exists_query := format('
        %s EXISTS (
            SELECT 1 FROM _values fv 
            JOIN _structures fs ON fs._id = fv._id_structure 
            JOIN _types ft ON ft._id = fs._id_type
            %s
            WHERE fv._id_object = %s._id 
              AND %s
        )',
        CASE WHEN use_not_exists THEN 'NOT' ELSE '' END,
        nested_join,
        table_alias,
        field_condition
    );
    
    RETURN ' AND ' || exists_query;
END;
$BODY$;

COMMENT ON FUNCTION _build_exists_condition(text, text, boolean, bigint, text) IS '🚀 РАСШИРЕННАЯ универсальная обертка для построения EXISTS/NOT EXISTS условий с полной поддержкой Class архитектуры:
📝 Обычные поля: Name, Title  
📋 Обычные массивы: Tags[], Categories[]
📦 Class поля: Contact.Name, Address.City (через _structures._id_parent)
🔗 Class массивы: Contacts[].Email, Addresses[].Street (комбинация _array_index + _id_parent)
Автоматически определяет тип поля, строит правильные JOIN для вложенных структур, проверяет наличие структур в схеме.';

-- ===== ЛОГИЧЕСКИЕ ОПЕРАТОРЫ =====

-- Функция построения AND условий (рекурсивная)
CREATE OR REPLACE FUNCTION _build_and_condition(
    and_array jsonb,
    scheme_id bigint,
    table_alias text DEFAULT 'o',
    max_depth integer DEFAULT 10
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    conditions text := '';
    condition_item jsonb;
    single_condition text;
    i integer;
BEGIN
    -- Проверка глубины рекурсии
    IF max_depth <= 0 THEN
        RAISE EXCEPTION 'Достигнута максимальная глубина рекурсии для $and оператора';
    END IF;
    
    -- Проверяем что это массив
    IF jsonb_typeof(and_array) != 'array' OR jsonb_array_length(and_array) = 0 THEN
        RETURN '';
    END IF;
    
    -- Обрабатываем каждый элемент массива
    FOR i IN 0..jsonb_array_length(and_array) - 1 LOOP
        condition_item := and_array->i;
        
        -- Рекурсивно обрабатываем каждый элемент
        single_condition := _build_single_facet_condition(condition_item, scheme_id, table_alias, max_depth - 1);
        
        IF single_condition != '' AND single_condition != ' AND TRUE' THEN
            -- Убираем лишний ' AND ' из начала каждого условия
            single_condition := ltrim(single_condition, ' AND ');
            
            IF conditions != '' THEN
                conditions := conditions || ' AND ';
            END IF;
            conditions := conditions || single_condition;
        END IF;
    END LOOP;
    
    IF conditions != '' THEN
        RETURN ' AND (' || conditions || ')';
    ELSE
        RETURN '';
    END IF;
END;
$BODY$;

-- Функция построения OR условий (рекурсивная)  
CREATE OR REPLACE FUNCTION _build_or_condition(
    or_array jsonb,
    scheme_id bigint,
    table_alias text DEFAULT 'o',
    max_depth integer DEFAULT 10
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    conditions text := '';
    condition_item jsonb;
    single_condition text;
    or_parts text[] := '{}';
    i integer;
    final_condition text;
BEGIN
    -- Проверка глубины рекурсии
    IF max_depth <= 0 THEN
        RAISE EXCEPTION 'Достигнута максимальная глубина рекурсии для $or оператора';
    END IF;
    
    -- Проверяем что это массив
    IF jsonb_typeof(or_array) != 'array' OR jsonb_array_length(or_array) = 0 THEN
        RETURN '';
    END IF;
    
    -- Обрабатываем каждый элемент массива
    FOR i IN 0..jsonb_array_length(or_array) - 1 LOOP
        condition_item := or_array->i;
        
        -- Рекурсивно обрабатываем каждый элемент (убираем префикс ' AND ')
        single_condition := _build_single_facet_condition(condition_item, scheme_id, table_alias, max_depth - 1);
        
        IF single_condition != '' AND single_condition != ' AND TRUE' THEN
            -- Убираем ' AND ' из начала каждого условия для OR
            single_condition := ltrim(single_condition, ' AND ');
            or_parts := array_append(or_parts, single_condition);
        END IF;
    END LOOP;
    
    -- Объединяем через OR
    IF array_length(or_parts, 1) > 0 THEN
        final_condition := array_to_string(or_parts, ' OR ');
        RETURN ' AND (' || final_condition || ')';
    END IF;
    
    RETURN '';
END;
$BODY$;

-- Функция построения NOT условий (рекурсивная)
CREATE OR REPLACE FUNCTION _build_not_condition(
    not_object jsonb,
    scheme_id bigint,
    table_alias text DEFAULT 'o',
    max_depth integer DEFAULT 10
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE
AS $BODY$
DECLARE
    inner_condition text;
BEGIN
    -- Проверка глубины рекурсии
    IF max_depth <= 0 THEN
        RAISE EXCEPTION 'Достигнута максимальная глубина рекурсии для $not оператора';
    END IF;
    
    -- Рекурсивно обрабатываем внутреннее условие
    inner_condition := _build_single_facet_condition(not_object, scheme_id, table_alias, max_depth - 1);
    
    IF inner_condition != '' AND inner_condition != 'TRUE' THEN
        -- Превращаем EXISTS в NOT EXISTS и наоборот
        IF inner_condition LIKE '%EXISTS (%' THEN
            inner_condition := replace(inner_condition, 'EXISTS (', 'NOT EXISTS (');
            RETURN ' AND ' || inner_condition;
        ELSIF inner_condition LIKE '%NOT EXISTS (%' THEN  
            inner_condition := replace(inner_condition, 'NOT EXISTS (', 'EXISTS (');
            RETURN ' AND ' || inner_condition;
        ELSE
            -- Для сложных условий оборачиваем в NOT
            RETURN ' AND NOT (' || inner_condition || ')';
        END IF;
    END IF;
    
    RETURN '';
END;
$BODY$;

-- Универсальная функция обработки одиночного фасетного условия (рекурсивная)
CREATE OR REPLACE FUNCTION _build_single_facet_condition(
    facet_condition jsonb,
    scheme_id bigint,
    table_alias text DEFAULT 'o',
    max_depth integer DEFAULT 10
) RETURNS text
LANGUAGE 'plpgsql'
IMMUTABLE  
AS $BODY$
DECLARE
    condition_key text;
    condition_value jsonb;
    field_path text;
    parsed_path RECORD;
    structure_info RECORD;
    operator_name text;
    operator_value text;
    inner_condition_sql text;
    all_conditions text := '';
    single_condition text;
BEGIN
    -- Проверка глубины рекурсии
    IF max_depth <= 0 THEN
        RETURN 'TRUE';  -- Безопасное завершение рекурсии
    END IF;
    
    -- Проверяем тип входных данных
    IF jsonb_typeof(facet_condition) != 'object' THEN
        RETURN '';
    END IF;
    
    -- Обрабатываем каждую пару ключ-значение
    FOR condition_key, condition_value IN SELECT * FROM jsonb_each(facet_condition) LOOP
        -- Логические операторы
        IF condition_key = '$and' THEN
            RETURN _build_and_condition(condition_value, scheme_id, table_alias, max_depth - 1);
        ELSIF condition_key = '$or' THEN
            RETURN _build_or_condition(condition_value, scheme_id, table_alias, max_depth - 1);
        ELSIF condition_key = '$not' THEN
            RETURN _build_not_condition(condition_value, scheme_id, table_alias, max_depth - 1);
        
        -- Иерархические операторы (обрабатываются отдельно)
        ELSIF condition_key IN ('$hasAncestor', '$hasDescendant', '$level', '$isRoot', '$isLeaf') THEN
            CONTINUE; -- Пропускаем, они обрабатываются в build_hierarchical_conditions
        
        -- Операторы для полей 
        ELSE
            -- Парсим путь поля
            field_path := condition_key;
            SELECT * INTO parsed_path FROM _parse_field_path(field_path);
            
            -- Получаем информацию о структуре для всех полей
            SELECT * INTO structure_info 
            FROM _find_structure_info(scheme_id, parsed_path.root_field, parsed_path.nested_field);
            
            -- Обрабатываем значение поля
            IF jsonb_typeof(condition_value) = 'object' THEN
                -- Сложное условие с операторами типа {"$gt": 100, "$lt": 200}
                FOR operator_name, operator_value IN SELECT key, value FROM jsonb_each_text(condition_value) LOOP
                    inner_condition_sql := _build_inner_condition(
                        operator_name, 
                        operator_value, 
                        CASE 
                            WHEN parsed_path.is_nested THEN structure_info.nested_type_info
                            ELSE structure_info.root_type_info
                        END
                    );
                    
                    single_condition := _build_exists_condition(field_path, inner_condition_sql, false, scheme_id, table_alias);
                    
                    -- Накапливаем условия через AND
                    IF all_conditions != '' THEN
                        all_conditions := all_conditions || ' AND ';
                    END IF;
                    all_conditions := all_conditions || ltrim(single_condition, ' AND ');
                END LOOP;
            
            ELSIF jsonb_typeof(condition_value) = 'array' THEN
                -- Массив значений - обрабатываем как $in
                inner_condition_sql := _build_inner_condition(
                    '$in', 
                    condition_value::text,
                    CASE 
                        WHEN parsed_path.is_nested THEN structure_info.nested_type_info
                        ELSE structure_info.root_type_info
                    END
                );
                
                single_condition := _build_exists_condition(field_path, inner_condition_sql, false, scheme_id, table_alias);
                
                -- Накапливаем условия через AND
                IF all_conditions != '' THEN
                    all_conditions := all_conditions || ' AND ';
                END IF;
                all_conditions := all_conditions || ltrim(single_condition, ' AND ');
            
            ELSE
                -- Простое значение - равенство
                inner_condition_sql := _build_inner_condition(
                    '=', 
                    -- Убираем лишние кавычки из строковых значений
                    CASE 
                        WHEN jsonb_typeof(condition_value) = 'string' THEN condition_value #>> '{}'
                        ELSE condition_value::text 
                    END,
                    CASE 
                        WHEN parsed_path.is_nested THEN structure_info.nested_type_info
                        ELSE structure_info.root_type_info
                    END
                );
                
                single_condition := _build_exists_condition(field_path, inner_condition_sql, false, scheme_id, table_alias);
                
                -- Накапливаем условия через AND
                IF all_conditions != '' THEN
                    all_conditions := all_conditions || ' AND ';
                END IF;
                all_conditions := all_conditions || ltrim(single_condition, ' AND ');
            END IF;
        END IF;
    END LOOP;
    
    -- Возвращаем все накопленные условия
    IF all_conditions != '' THEN
        RETURN ' AND (' || all_conditions || ')';
    END IF;
    RETURN '';
END;
$BODY$;

-- Комментарии к логическим операторам
COMMENT ON FUNCTION _build_and_condition(jsonb, bigint, text, integer) IS 'Рекурсивный построитель AND условий. Поддерживает вложенные логические операторы и Class поля. Ограничение рекурсии: 10 уровней.';
COMMENT ON FUNCTION _build_or_condition(jsonb, bigint, text, integer) IS 'Рекурсивный построитель OR условий. Объединяет условия через OR с правильной обработкой скобок. Ограничение рекурсии: 10 уровней.';
COMMENT ON FUNCTION _build_not_condition(jsonb, bigint, text, integer) IS 'Рекурсивный построитель NOT условий. Инвертирует EXISTS в NOT EXISTS и обрабатывает сложные условия. Ограничение рекурсии: 10 уровней.';
COMMENT ON FUNCTION _build_single_facet_condition(jsonb, bigint, text, integer) IS 'Универсальная рекурсивная функция обработки фасетных условий. Поддерживает логические операторы ($and, $or, $not), LINQ операторы, Class поля и массивы. ИСПРАВЛЕНО: Теперь корректно обрабатывает множественные поля в JSON через накопление условий, а не преждевременный RETURN.';

-- ===== РАСШИРЕННАЯ ФУНКЦИЯ ФАСЕТОВ С CLASS ПОЛЯМИ =====

-- Рекурсивная функция для построения фасетного пути поля (например: "Contact.Name", "Contacts[].Email")  
CREATE OR REPLACE FUNCTION _build_facet_field_path(
    structure_id bigint,
    scheme_id bigint,
    current_path text DEFAULT '',
    max_depth integer DEFAULT 10
) RETURNS text
LANGUAGE 'plpgsql'
COST 50
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    structure_record RECORD;
    parent_path text;
BEGIN
    -- Проверка глубины рекурсии
    IF max_depth <= 0 THEN
        RETURN current_path;
    END IF;
    
    -- Получаем информацию о текущей структуре
    SELECT s._name, s._id_parent, s._is_array
    INTO structure_record
    FROM _structures s 
    WHERE s._id = structure_id AND s._id_scheme = scheme_id;
    
    -- Если структура не найдена, возвращаем текущий путь
    IF NOT FOUND THEN
        RETURN current_path;
    END IF;
    
    -- Формируем имя поля с учетом массивов
    current_path := structure_record._name || 
                   CASE WHEN structure_record._is_array THEN '[]' ELSE '' END ||
                   CASE WHEN current_path != '' THEN '.' || current_path ELSE '' END;
    
    -- Если есть родитель, рекурсивно строим путь
    IF structure_record._id_parent IS NOT NULL THEN
        RETURN _build_facet_field_path(structure_record._id_parent, scheme_id, current_path, max_depth - 1);
    END IF;
    
    -- Возвращаем построенный путь
    RETURN current_path;
END;
$BODY$;

-- Функция для построения расширенных фасетов с Class полями
CREATE OR REPLACE FUNCTION get_facets(scheme_id bigint)
RETURNS jsonb 
LANGUAGE 'plpgsql'
COST 150
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    result_facets jsonb := '{}'::jsonb;
    all_facets jsonb;
    class_facets jsonb;
BEGIN
    -- 🚀 ШАГ 1: Получаем все базовые фасеты (корневые поля и простые массивы)
    SELECT jsonb_object_agg(s._name, COALESCE(f.facet_values, '[]'::jsonb))
    INTO all_facets
    FROM _structures s
    LEFT JOIN (
        SELECT 
            v._id_structure, 
            jsonb_agg(DISTINCT 
                CASE 
                    -- Массивы
                    WHEN st._is_array = true THEN
                        (
                            SELECT COALESCE(jsonb_agg(
                                CASE 
                                    -- Простые типы массивов
                                    WHEN t._db_type = 'String' THEN to_jsonb(av._String)
                                    WHEN t._db_type = 'Long' AND t._type != '_RObject' THEN to_jsonb(av._Long)
                                    WHEN t._db_type = 'Guid' AND t._type != 'Object' THEN to_jsonb(av._Guid)
                                    WHEN t._db_type = 'Double' THEN to_jsonb(av._Double)
                                    WHEN t._db_type = 'DateTime' THEN to_jsonb(av._DateTime)
                                    WHEN t._db_type = 'Boolean' THEN to_jsonb(av._Boolean)
                                    
                                    -- _RObject массивы
                                    WHEN t._db_type = 'Long' AND t._type = '_RObject' THEN 
                                        jsonb_build_object(
                                            'id', av._Long,
                                            'name', (SELECT _name FROM _objects WHERE _id = av._Long),
                                            'scheme', (SELECT sc._name FROM _objects o2 JOIN _schemes sc ON o2._id_scheme = sc._id WHERE o2._id = av._Long)
                                        )
                                    
                                    WHEN t._db_type = 'ListItem' THEN
                                        jsonb_build_object(
                                            'id', av._Long,
                                            'value', (SELECT _value FROM _list_items WHERE _id = av._Long)
                                        )
                                    WHEN t._db_type = 'ByteArray' THEN 
                                        to_jsonb(encode(av._ByteArray, 'base64'))
                                    ELSE to_jsonb(av._String)
                                END ORDER BY av._array_index
                            ), '[]'::jsonb)
                            FROM _values av 
                            WHERE av._id_object = v._id_object 
                              AND av._id_structure = v._id_structure 
                              AND av._array_index IS NOT NULL
                        )
                    
                    -- Обычные поля
                    WHEN t._db_type = 'String' THEN to_jsonb(v._String)
                    WHEN t._db_type = 'Long' AND t._type != '_RObject' THEN to_jsonb(v._Long)
                    WHEN t._db_type = 'Guid' AND t._type != 'Object' THEN to_jsonb(v._Guid)
                    WHEN t._db_type = 'Double' THEN to_jsonb(v._Double)
                    WHEN t._db_type = 'DateTime' THEN to_jsonb(v._DateTime)
                    WHEN t._db_type = 'Boolean' THEN to_jsonb(v._Boolean)
                    
                    -- _RObject поля
                    WHEN t._db_type = 'Long' AND t._type = '_RObject' THEN 
                        CASE 
                            WHEN v._Long IS NOT NULL THEN 
                                jsonb_build_object(
                                    'id', v._Long,
                                    'name', (SELECT _name FROM _objects WHERE _id = v._Long),
                                    'scheme', (SELECT sc._name FROM _objects o2 JOIN _schemes sc ON o2._id_scheme = sc._id WHERE o2._id = v._Long)
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
                    -- Фильтрация массивов
                    WHEN st._is_array = true THEN 
                        EXISTS(SELECT 1 FROM _values av2 WHERE av2._id_object = v._id_object AND av2._id_structure = v._id_structure AND av2._array_index IS NOT NULL)
                    -- Фильтрация обычных полей
                    WHEN t._db_type = 'String' THEN v._String IS NOT NULL
                    WHEN t._db_type = 'Long' AND t._type != '_RObject' THEN v._Long IS NOT NULL
                    WHEN t._db_type = 'Guid' AND t._type != 'Object' THEN v._Guid IS NOT NULL
                    WHEN t._db_type = 'Double' THEN v._Double IS NOT NULL
                    WHEN t._db_type = 'DateTime' THEN v._DateTime IS NOT NULL
                    WHEN t._db_type = 'Boolean' THEN v._Boolean IS NOT NULL
                    WHEN t._db_type = 'Long' AND t._type = '_RObject' THEN v._Long IS NOT NULL
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
          AND o._id NOT IN (SELECT _id FROM _deleted_objects)
          AND st._id_parent IS NULL  -- 🔑 Только корневые поля на этом этапе
          AND NOT (t._db_type = 'Guid' AND t._type = 'Object') -- 🔑 Исключаем Class поля, их обработаем отдельно
        GROUP BY v._id_structure
    ) f ON f._id_structure = s._id
    WHERE s._id_scheme = scheme_id 
      AND s._id_parent IS NULL;  -- 🔑 Только корневые структуры
    
    -- 🚀 ШАГ 2: Добавляем развернутые Class поля (Contact.Name, Contact[].Email)
    SELECT jsonb_object_agg(
        field_path,
        COALESCE(field_values, '[]'::jsonb)
    ) INTO class_facets
    FROM (
        SELECT 
            _build_facet_field_path(nested_s._id, scheme_id) as field_path,
            jsonb_agg(DISTINCT
                CASE 
                    WHEN nested_s._is_array = true THEN
                        (
                            SELECT COALESCE(jsonb_agg(
                                CASE 
                                    WHEN nested_t._db_type = 'String' THEN to_jsonb(nested_v._String)
                                    WHEN nested_t._db_type = 'Long' AND nested_t._type != '_RObject' THEN to_jsonb(nested_v._Long)
                                    WHEN nested_t._db_type = 'Double' THEN to_jsonb(nested_v._Double)
                                    WHEN nested_t._db_type = 'Boolean' THEN to_jsonb(nested_v._Boolean)
                                    WHEN nested_t._db_type = 'DateTime' THEN to_jsonb(nested_v._DateTime)
                                    WHEN nested_t._db_type = 'Guid' AND nested_t._type != 'Object' THEN to_jsonb(nested_v._Guid)
                                    ELSE to_jsonb(nested_v._String)
                                END ORDER BY nested_v._array_index
                            ), '[]'::jsonb)
                            FROM _values nested_v
                            WHERE nested_v._id_object = o._id 
                              AND nested_v._id_structure = nested_s._id
                              AND nested_v._array_index IS NOT NULL
                        )
                    ELSE
                        CASE 
                            WHEN nested_t._db_type = 'String' THEN to_jsonb(nested_v._String)
                            WHEN nested_t._db_type = 'Long' AND nested_t._type != '_RObject' THEN to_jsonb(nested_v._Long)
                            WHEN nested_t._db_type = 'Double' THEN to_jsonb(nested_v._Double)
                            WHEN nested_t._db_type = 'Boolean' THEN to_jsonb(nested_v._Boolean)
                            WHEN nested_t._db_type = 'DateTime' THEN to_jsonb(nested_v._DateTime)
                            WHEN nested_t._db_type = 'Guid' AND nested_t._type != 'Object' THEN to_jsonb(nested_v._Guid)
                            ELSE to_jsonb(nested_v._String)
                        END
                END
            ) FILTER (WHERE nested_v._id IS NOT NULL) as field_values
        FROM _objects o
        JOIN _values root_v ON root_v._id_object = o._id AND root_v._array_index IS NULL
        JOIN _structures root_s ON root_s._id = root_v._id_structure AND root_s._id_parent IS NULL
        JOIN _types root_t ON root_t._id = root_s._id_type AND root_t._db_type = 'Guid' AND root_t._type = 'Object'  -- 🔑 Только Class поля
        JOIN _structures nested_s ON nested_s._id_parent = root_s._id  -- 🔑 Вложенные структуры
        JOIN _types nested_t ON nested_t._id = nested_s._id_type
        LEFT JOIN _values nested_v ON nested_v._id_object = o._id AND nested_v._id_structure = nested_s._id
        WHERE o._id_scheme = scheme_id
          AND o._id NOT IN (SELECT _id FROM _deleted_objects)
        GROUP BY nested_s._id
        HAVING COUNT(nested_v._id) > 0  -- 🔑 Только поля с реальными значениями
    ) class_fields
    WHERE field_path IS NOT NULL AND field_path != '';
    
    -- 🚀 ШАГ 3: Объединяем базовые и Class фасеты
    result_facets := COALESCE(all_facets, '{}'::jsonb) || COALESCE(class_facets, '{}'::jsonb);
    
    RETURN result_facets;
END;
$BODY$;

-- Комментарии к расширенной функции фасетов
COMMENT ON FUNCTION _build_facet_field_path(bigint, bigint, text, integer) IS 'Рекурсивная функция построения путей для Class полей в фасетах. Создает пути типа "Contact.Name", "Contacts[].Email", "Address.City" из иерархии _structures._id_parent. Поддерживает массивы и многоуровневую вложенность.';

COMMENT ON FUNCTION get_facets(bigint) IS '🚀 РАСШИРЕННАЯ функция построения фасетов с полной поддержкой Class архитектуры:
📋 Базовые фасеты: Name, Status, Tags[] (корневые поля и простые массивы)
🔗 _RObject фасеты: {id, name, scheme} для ссылок на объекты
📦 Class фасеты: Contact.Name, Address.City (развернутые из _structures._id_parent)  
🔗 Class массивы: Contacts[].Email, Products[].Price (комбинация массивов + вложенность)
Двухэтапная обработка: сначала базовые фасеты, затем развертка Class полей. Исключает удаленные объекты.';

-- ===== НОВАЯ МОДУЛЬНАЯ АРХИТЕКТУРА =====

-- ===== ФИНАЛЬНАЯ АРХИТЕКТУРА: АБСОЛЮТНАЯ ЧИСТОТА =====
-- ✅ build_advanced_facet_conditions() - УДАЛЕНА
-- ✅ build_base_facet_conditions() - УДАЛЕНА 
-- ✅ use_advanced_facets - УДАЛЕН
-- 🚀 ОСТАЕТСЯ: ТОЛЬКО _build_single_facet_condition() как ЕДИНАЯ ТОЧКА ВХОДА
-- 💎 ИДЕАЛЬНАЯ ЧИСТОТА БЕЗ ЕДИНОЙ ЛИШНЕЙ СТРОКИ!

-- Функция 1: Построение условий сортировки
CREATE OR REPLACE FUNCTION build_order_conditions(
    order_by jsonb,
    table_alias text DEFAULT 'o'
) RETURNS text
LANGUAGE 'plpgsql'
COST 50
IMMUTABLE
AS $BODY$
DECLARE
    order_conditions text := format('ORDER BY %s._id', table_alias);
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
                    WHERE v._id_object = %s._id AND s._name = %L
                      AND v._array_index IS NULL  -- исключаем элементы массивов
                    LIMIT 1
                ) %s NULLS LAST', table_alias, field_name, direction);
                
                -- Добавляем запятую, если уже есть условия
                IF order_conditions != '' THEN
                    order_conditions := order_conditions || ', ';
                END IF;
                order_conditions := order_conditions || order_clause;
            END IF;
        END LOOP;
        
        -- Формируем финальный ORDER BY
        IF order_conditions != '' THEN
            order_conditions := 'ORDER BY ' || order_conditions || format(', %s._id', table_alias);
        ELSE
            order_conditions := format('ORDER BY %s._id', table_alias);
        END IF;
    END IF;
    
    RETURN order_conditions;
END;
$BODY$;

-- Комментарий к функции сортировки
COMMENT ON FUNCTION build_order_conditions(jsonb, text) IS 'Строит ORDER BY условия на основе order_by параметра. Поддерживает сортировку по полям из _values с правильной обработкой типов данных. Исключает элементы массивов (array_index IS NULL). Использует padding для корректной сортировки чисел как строк.';

-- Функция 2: Построение иерархических условий
CREATE OR REPLACE FUNCTION build_has_ancestor_condition(
    ancestor_id bigint,
    table_alias text DEFAULT 'o'
) RETURNS text
LANGUAGE 'plpgsql'
COST 50
IMMUTABLE
AS $BODY$
BEGIN
    RETURN format(
        ' AND EXISTS (
            WITH RECURSIVE ancestors AS (
                SELECT %s._id_parent as parent_id, 1 as level
                FROM _objects dummy WHERE dummy._id = %s._id
                UNION ALL
                SELECT o._id_parent, ancestors.level + 1
                FROM _objects o
                JOIN ancestors ON o._id = ancestors.parent_id
                WHERE ancestors.level < 50
            )
            SELECT 1 FROM ancestors WHERE parent_id = %s
        )', 
        table_alias, table_alias, ancestor_id
    );
END;
$BODY$;

CREATE OR REPLACE FUNCTION build_has_descendant_condition(
    descendant_id bigint,
    table_alias text DEFAULT 'o'  
) RETURNS text
LANGUAGE 'plpgsql'
COST 50
IMMUTABLE
AS $BODY$
BEGIN
    RETURN format(
        ' AND EXISTS (
            WITH RECURSIVE descendants AS (
                SELECT _id, _id_parent, 1 as level
                FROM _objects WHERE _id = %s
                UNION ALL
                SELECT o._id, o._id_parent, descendants.level + 1
                FROM _objects o
                JOIN descendants ON o._id_parent = descendants._id
                WHERE descendants.level < 50
            )
            SELECT 1 FROM descendants WHERE _id_parent = %s._id
        )', 
        descendant_id, table_alias
    );
END;
$BODY$;

CREATE OR REPLACE FUNCTION build_level_condition(
    target_level integer,
    table_alias text DEFAULT 'o'
) RETURNS text
LANGUAGE 'plpgsql'
COST 50
IMMUTABLE
AS $BODY$
BEGIN
    RETURN format(
        ' AND (
            WITH RECURSIVE level_calc AS (
                SELECT %s._id, %s._id_parent, 0 as level
                WHERE %s._id_parent IS NULL
                UNION ALL
                SELECT o._id, o._id_parent, level_calc.level + 1
                FROM _objects o
                JOIN level_calc ON o._id_parent = level_calc._id
                WHERE level_calc.level < 50
            )
            SELECT level FROM level_calc WHERE _id = %s._id
        ) = %s', 
        table_alias, table_alias, table_alias, table_alias, target_level
    );
END;
$BODY$;

-- ✅ НОВАЯ ФУНКЦИЯ: Поддержка операторов сравнения для уровней
CREATE OR REPLACE FUNCTION build_level_condition_with_operators(
    level_operators jsonb,
    table_alias text DEFAULT 'o'
) RETURNS text
LANGUAGE 'plpgsql'
COST 50
IMMUTABLE
AS $BODY$
DECLARE
    operator_name text;
    operator_value text;
    level_condition text := '';
    op_symbol text;
BEGIN
    -- Обрабатываем каждый оператор в JSON объекте
    FOR operator_name, operator_value IN SELECT key, value FROM jsonb_each_text(level_operators) LOOP
        
        -- Определяем SQL оператор
        CASE operator_name
            WHEN '$gt' THEN op_symbol := '>';
            WHEN '$gte' THEN op_symbol := '>=';
            WHEN '$lt' THEN op_symbol := '<';
            WHEN '$lte' THEN op_symbol := '<=';
            WHEN '$eq' THEN op_symbol := '=';
            WHEN '$ne' THEN op_symbol := '!=';
            ELSE 
                CONTINUE; -- Пропускаем неизвестные операторы
        END CASE;
        
        -- Формируем условие для текущего оператора
        IF level_condition != '' THEN
            level_condition := level_condition || ' AND ';
        END IF;
        
        level_condition := level_condition || format(
            '(
                WITH RECURSIVE level_calc AS (
                    SELECT %s._id, %s._id_parent, 0 as level
                    WHERE %s._id_parent IS NULL
                    UNION ALL
                    SELECT o._id, o._id_parent, level_calc.level + 1
                    FROM _objects o
                    JOIN level_calc ON o._id_parent = level_calc._id
                    WHERE level_calc.level < 50
                )
                SELECT level FROM level_calc WHERE _id = %s._id
            ) %s %s',
            table_alias, table_alias, table_alias, table_alias, op_symbol, operator_value
        );
    END LOOP;
    
    -- Возвращаем полное условие с AND префиксом
    IF level_condition != '' THEN
        RETURN ' AND (' || level_condition || ')';
    END IF;
    
    RETURN '';
END;
$BODY$;

-- Функция объединения иерархических условий
CREATE OR REPLACE FUNCTION build_hierarchical_conditions(
    facet_filters jsonb,
    table_alias text DEFAULT 'o'
) RETURNS text
LANGUAGE 'plpgsql'
COST 50
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    where_conditions text := '';
    ancestor_id bigint;
    descendant_id bigint;
    target_level integer;
BEGIN
    IF facet_filters IS NOT NULL AND jsonb_typeof(facet_filters) = 'object' THEN
        -- $hasAncestor
        IF facet_filters ? '$hasAncestor' THEN
            ancestor_id := (facet_filters->>'$hasAncestor')::bigint;
            where_conditions := where_conditions || build_has_ancestor_condition(ancestor_id, table_alias);
        END IF;
        
        -- $hasDescendant
        IF facet_filters ? '$hasDescendant' THEN
            descendant_id := (facet_filters->>'$hasDescendant')::bigint;
            where_conditions := where_conditions || build_has_descendant_condition(descendant_id, table_alias);
        END IF;
        
        -- $level: Поддержка операторов сравнения {"$gt": 2}, {"$eq": 3} и т.д.
        IF facet_filters ? '$level' THEN
            -- ✅ ИСПРАВЛЕНИЕ: Обработка JSON операторов для $level
            IF jsonb_typeof(facet_filters->'$level') = 'object' THEN
                -- Сложное условие с операторами типа {"$gt": 2}, {"$lt": 5}
                where_conditions := where_conditions || build_level_condition_with_operators(facet_filters->'$level', table_alias);
            ELSE
                -- Простое значение - точное равенство
                target_level := (facet_filters->>'$level')::integer;
                where_conditions := where_conditions || build_level_condition(target_level, table_alias);
            END IF;
        END IF;
        
        -- $isRoot
        IF facet_filters ? '$isRoot' AND (facet_filters->>'$isRoot')::boolean THEN
            where_conditions := where_conditions || format(' AND %s._id_parent IS NULL', table_alias);
        END IF;
        
        -- $isLeaf  
        IF facet_filters ? '$isLeaf' AND (facet_filters->>'$isLeaf')::boolean THEN
            where_conditions := where_conditions || format(
                ' AND NOT EXISTS (SELECT 1 FROM _objects child WHERE child._id_parent = %s._id)', 
                table_alias
            );
        END IF;
    END IF;
    
    RETURN where_conditions;
END;
$BODY$;

-- Комментарий к иерархическим условиям
COMMENT ON FUNCTION build_hierarchical_conditions(jsonb, text) IS 'Строит WHERE условия для иерархических фильтров: $hasAncestor, $hasDescendant, $level, $isRoot, $isLeaf. Использует рекурсивные CTE для эффективного поиска в иерархии объектов. Ограничение глубины рекурсии: 50 уровней.';

-- Функция 3: Выполнение запроса и возврат результата
CREATE OR REPLACE FUNCTION execute_objects_query(
    scheme_id bigint,
    base_conditions text,
    hierarchical_conditions text,
    order_conditions text,
    limit_count integer DEFAULT NULL,  -- ✅ ИСПРАВЛЕНИЕ: убираем DEFAULT 100
    offset_count integer DEFAULT 0
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 200
VOLATILE NOT LEAKPROOF  
AS $BODY$
DECLARE
    query_text text;
    count_query_text text;
    objects_result jsonb;
    total_count integer;
    final_where text;
BEGIN
    -- Объединяем все условия
    final_where := format('WHERE o._id_scheme = %s%s%s', 
                         scheme_id, 
                         COALESCE(base_conditions, ''),
                         COALESCE(hierarchical_conditions, ''));
    
    -- ✅ ИСПРАВЛЕНИЕ: Строим основной запрос с обработкой NULL limit
    query_text := format('
        SELECT jsonb_agg(get_object_json(sub._id, 10))
        FROM (
            SELECT o._id
            FROM _objects o
            %s
            %s
            %s
        ) sub',
        final_where,
        order_conditions,
        CASE 
            WHEN limit_count IS NULL OR limit_count >= 2000000000 THEN ''  -- ✅ БЕЗ LIMIT если не указан или очень большой
            ELSE format('LIMIT %s OFFSET %s', limit_count, offset_count)
        END
    );
    
    -- Строим запрос подсчета
    count_query_text := format('
        SELECT COUNT(*)
        FROM _objects o  
        %s',
        final_where
    );
    
    -- Выполняем запросы
    EXECUTE query_text INTO objects_result;
    EXECUTE count_query_text INTO total_count;
    
    -- Формируем результат
    RETURN jsonb_build_object(
        'objects', COALESCE(objects_result, '[]'::jsonb),
        'total_count', total_count,
        'limit', limit_count,
        'offset', offset_count,
        'facets', get_facets(scheme_id)
    );
END;
$BODY$;

-- Комментарий к функции выполнения запроса
COMMENT ON FUNCTION execute_objects_query(bigint, text, text, text, integer, integer) IS 'Выполняет поиск объектов с построенными условиями и возвращает стандартизированный результат с объектами, метаданными и фасетами. Использует get_object_json для полного JSON представления каждого найденного объекта.';

-- Основная функция фасетного поиска объектов с чистейшей архитектурой
CREATE OR REPLACE FUNCTION search_objects_with_facets(
    scheme_id bigint,
    facet_filters jsonb DEFAULT NULL,
    limit_count integer DEFAULT NULL,  -- ✅ ИСПРАВЛЕНИЕ: убираем DEFAULT 100
    offset_count integer DEFAULT 0,
    order_by jsonb DEFAULT NULL,
    max_recursion_depth integer DEFAULT 10
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 200
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    base_conditions text;
    hierarchical_conditions text;
    order_conditions text;
BEGIN
    -- 🚀 ФИНАЛЬНАЯ ЧИСТОТА: ТОЛЬКО _build_single_facet_condition() - БЕЗ МЕРТВОГО КОДА!
    base_conditions := _build_single_facet_condition(facet_filters, scheme_id, 'o', max_recursion_depth);
    
    -- Строим иерархические и сортировочные условия (без изменений)
    hierarchical_conditions := build_hierarchical_conditions(facet_filters, 'o');
    order_conditions := build_order_conditions(order_by, 'o');
    
    -- Выполняем поиск
    RETURN execute_objects_query(
        scheme_id,
        base_conditions,
        hierarchical_conditions,
        order_conditions,
        limit_count,
        offset_count
    );
END;
$BODY$;

-- Комментарий к основной функции поиска с новыми возможностями
COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, jsonb, integer) IS '🚀 ФИНАЛЬНАЯ ЧИСТОТА: Абсолютно чистая архитектура БЕЗ МЕРТВОГО КОДА! Прямой вызов _build_single_facet_condition() как ЕДИНСТВЕННОЙ точки входа. БЕЗ legacy функций, БЕЗ use_advanced_facets, БЕЗ мертвых веток! Поддерживает логические операторы ($and, $or, $not), 25+ LINQ операторов ($gt, $contains, $arrayContains и др.), Class поля (Contact.Name), Class массивы (Contacts[].Email). 🆕 max_recursion_depth для сложных запросов (DEFAULT 10).';

-- Функция для поиска в иерархии (дети объекта) с ПОДДЕРЖКОЙ НОВОЙ LINQ ПАРАДИГМЫ
CREATE OR REPLACE FUNCTION search_tree_objects_with_facets(
    scheme_id bigint,
    parent_id bigint,
    facet_filters jsonb DEFAULT NULL,
    limit_count integer DEFAULT NULL,  -- ✅ ИСПРАВЛЕНИЕ: убираем DEFAULT 100
    offset_count integer DEFAULT 0,
    order_by jsonb DEFAULT NULL,
    max_depth integer DEFAULT 10,
    max_recursion_depth integer DEFAULT 10
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 300
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    query_text text;
    count_query_text text;
    objects_result jsonb;
    total_count integer;
    base_conditions text;
    order_conditions text;
BEGIN
    -- 🚀 ВАРИАНТ C: ЧИСТЕЙШАЯ АРХИТЕКТУРА - прямой вызов универсальной системы
    base_conditions := _build_single_facet_condition(facet_filters, scheme_id, 'd', max_recursion_depth);
    order_conditions := build_order_conditions(order_by, 'd');
    
    -- Если max_depth = 1, ищем только прямых детей
    IF max_depth = 1 THEN
        query_text := format('
            SELECT jsonb_agg(get_object_json(sub._id, 10))
            FROM (
                SELECT d._id
                FROM _objects d
                WHERE d._id_scheme = %s 
                  AND d._id_parent = %s%s
                %s
                %s
            ) sub',
            scheme_id,
            parent_id,
            COALESCE(base_conditions, ''),
            order_conditions,
            CASE 
                WHEN limit_count IS NOT NULL THEN format('LIMIT %s OFFSET %s', limit_count, offset_count)
                ELSE ''  -- ✅ БЕЗ LIMIT если не указан явно
            END
        );
        
        count_query_text := format('
            SELECT COUNT(*)
            FROM _objects d
            WHERE d._id_scheme = %s 
              AND d._id_parent = %s%s',
            scheme_id,
            parent_id,
            COALESCE(base_conditions, '')
        );
    ELSE
        -- Рекурсивный поиск потомков
        query_text := format('
            WITH RECURSIVE descendants AS (
                SELECT %s::bigint as _id, 0::bigint as depth
                UNION ALL
                SELECT o._id, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT jsonb_agg(get_object_json(sub._id, 10))
            FROM (
                SELECT d._id
                FROM descendants dt
                JOIN _objects d ON dt._id = d._id
                WHERE dt.depth > 0 
                  AND d._id_scheme = %s%s
                %s
                %s
            ) sub',
            parent_id,
            max_depth,
            scheme_id,
            COALESCE(base_conditions, ''),
            order_conditions,
            CASE 
                WHEN limit_count IS NOT NULL THEN format('LIMIT %s OFFSET %s', limit_count, offset_count)
                ELSE ''  -- ✅ БЕЗ LIMIT если не указан явно
            END
        );
        
        count_query_text := format('
            WITH RECURSIVE descendants AS (
                SELECT %s::bigint as _id, 0::bigint as depth
                UNION ALL
                SELECT o._id, d.depth + 1
                FROM _objects o
                JOIN descendants d ON o._id_parent = d._id
                WHERE d.depth < %s
            )
            SELECT COUNT(*)
            FROM descendants dt
            JOIN _objects d ON dt._id = d._id
            WHERE dt.depth > 0 
              AND d._id_scheme = %s%s',
            parent_id,
            max_depth,
            scheme_id,
            COALESCE(base_conditions, '')
        );
    END IF;
    
    -- Выполняем запросы
    EXECUTE query_text INTO objects_result;
    EXECUTE count_query_text INTO total_count;
    
    -- Формируем результат
    RETURN jsonb_build_object(
        'objects', COALESCE(objects_result, '[]'::jsonb),
        'total_count', total_count,
        'limit', limit_count,
        'offset', offset_count,
        'parent_id', parent_id,
        'max_depth', max_depth,
        'facets', get_facets(scheme_id)
    );
END;
$BODY$;

-- Комментарий к функции поиска в дереве
COMMENT ON FUNCTION search_tree_objects_with_facets(bigint, bigint, jsonb, integer, integer, jsonb, integer, integer) IS '🚀 ВАРИАНТ C + API: ЧИСТЕЙШАЯ АРХИТЕКТУРА с настраиваемой рекурсией! Прямой вызов _build_single_facet_condition() для древовидных запросов. БЕЗ build_advanced_facet_conditions() - МАКСИМАЛЬНАЯ ЧИСТОТА! Поддерживает:
📊 Логические операторы: $and, $or, $not
🔍 LINQ операторы: $gt, $contains, $arrayContains, $arrayAny и др.
📦 Class поля: Contact.Name, Address.City
🔗 Class массивы: Contacts[].Email, Products[].Price  
🌳 Иерархические условия: поиск прямых детей (max_depth=1) и рекурсивный поиск потомков
🆕 max_recursion_depth для сложных запросов (DEFAULT 10). ЕДИНАЯ точка входа!';

-- ===== ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ НОВОЙ АРХИТЕКТУРЫ =====
/*
-- 🚀 ОБНОВЛЕННЫЕ ВОЗМОЖНОСТИ С ОПТИМАЛЬНОЙ EAV СЕМАНТИКОЙ:

-- 🎯 НОВАЯ NULL СЕМАНТИКА:
-- = null теперь ищет ОТСУТСТВУЮЩИЕ поля (НЕ записи с NULL значениями)
SELECT search_objects_with_facets(
    9001, 
    '{"OptionalField": null}'::jsonb  -- найдет объекты БЕЗ этого поля в _values
);

-- $ne null теперь ищет поля с РЕАЛЬНЫМИ не-NULL значениями  
SELECT search_objects_with_facets(
    9001,
    '{"Name": {"$ne": null}}'::jsonb  -- найдет объекты где Name действительно заполнено
);

-- 🎯 НОВЫЙ ОПЕРАТОР $exists:
-- Явный контроль существования полей
SELECT search_objects_with_facets(
    9001,
    '{
        "RequiredField": {"$exists": true},    -- поле ДОЛЖНО существовать
        "OptionalField": {"$exists": false}    -- поле НЕ должно существовать
    }'::jsonb
);

-- 🚀 НОВЫЕ ВОЗМОЖНОСТИ:

-- 1. Логические операторы:
SELECT search_objects_with_facets(
    1002, 
    '{
        "$and": [
            {"Status": "Active"}, 
            {"$or": [{"Priority": "High"}, {"Urgent": true}]}
        ]
    }'::jsonb,
    10, 0, NULL
);

-- 2. LINQ операторы:
SELECT search_objects_with_facets(
    1002,
    '{
        "Price": {"$gt": "100", "$lt": "500"},
        "Title": {"$contains": "analytics"},
        "CreatedDate": {"$gte": "2024-01-01"}
    }'::jsonb,
    10, 0, NULL
);

-- 3. Базовые операторы массивов:
SELECT search_objects_with_facets(
    1002,
    '{
        "Tags[]": {"$arrayContains": "important"},
        "Scores[]": {"$arrayCountGt": 3},
        "Categories[]": {"$arrayAny": true},
        "Items[]": {"$arrayEmpty": false}
    }'::jsonb,
    10, 0, NULL
);

-- 4. Позиционные операторы массивов:
SELECT search_objects_with_facets(
    1002,
    '{
        "Tags[]": {"$arrayFirst": "urgent"},
        "Scores[]": {"$arrayLast": "100"},
        "Items[]": {"$arrayAt": "2"}
    }'::jsonb,
    10, 0, NULL
);

-- 5. Поисковые операторы массивов:  
SELECT search_objects_with_facets(
    1002,
    '{
        "Tags[]": {"$arrayStartsWith": "test_"},
        "Names[]": {"$arrayEndsWith": "_prod"},
        "Descriptions[]": {"$arrayMatches": ".*error.*"}
    }'::jsonb,
    10, 0, NULL
);

-- 6. Агрегационные операторы массивов:
SELECT search_objects_with_facets(
    1002,
    '{
        "Scores[]": {"$arraySum": "300"},
        "Ratings[]": {"$arrayAvg": "4.5"},
        "Prices[]": {"$arrayMin": "10.50"},
        "Quantities[]": {"$arrayMax": "1000"}
    }'::jsonb,
    10, 0, NULL
);

-- 7. NOT условия:
SELECT search_objects_with_facets(
    1002,
    '{
        "$not": {"Status": "Deleted"},
        "Title": {"$ne": null}
    }'::jsonb,
    10, 0, NULL
);

-- 8. Class поля - полная поддержка:
SELECT search_objects_with_facets(
    1002,
    '{
        "Contact.Name": "John Doe",
        "Address.City": "Moscow",
        "Contact.Phone": {"$startsWith": "+7"},
        "Address.PostalCode": {"$in": ["101000", "102000"]},
        "$not": {"Contact.Email": {"$endsWith": "@test.com"}}
    }'::jsonb,
    10, 0, NULL
);

-- 9. Class массивы с вложенными полями:
SELECT search_objects_with_facets(
    1002,
    '{
        "Contacts[].Name": "Jane Smith",
        "Addresses[].Country": "Russia", 
        "Products[].Price": {"$gt": "100"},
        "Tags[].Category": {"$contains": "business"},
        "$or": [
            {"Contacts[].Email": {"$endsWith": "@company.com"}},
            {"Addresses[].City": {"$in": ["Moscow", "SPb"]}}
        ]
    }'::jsonb,
    10, 0, NULL
);

-- 10. 🎯 НАСТРОЙКА РЕКУРСИИ - кастомная глубина:
SELECT search_objects_with_facets(
    1002, 
    '{"$and": [{"Tags[]": {"$arrayContains": "complex"}}, {"$or": [{"Age": {"$gt": "25"}}, {"Stock": {"$gt": "100"}}]}]}'::jsonb,
    10, 0,
    '[{"field": "Date", "direction": "DESC"}]'::jsonb,
    20  -- max_recursion_depth = 20 для сложных запросов
);

-- 📊 ИЕРАРХИЧЕСКИЕ условия:
SELECT search_objects_with_facets(
    1002,
    '{"$isRoot": true, "Status": ["Active"]}'::jsonb
);

-- 🌳 ПОИСК В ДЕРЕВЕ:
SELECT search_tree_objects_with_facets(
    1002, 1021,  -- scheme_id, parent_id
    '{"Status": ["Active"]}'::jsonb,
    10, 0, NULL, 1  -- прямые дети
);

-- Рекурсивный поиск потомков:
SELECT search_tree_objects_with_facets(
    1002, 1021,  -- scheme_id, parent_id  
    NULL, 20, 0, NULL, 5  -- до 10 уровней вглубь
);

-- 📈 ПОЛУЧЕНИЕ ФАСЕТОВ для UI:
SELECT get_facets(1002);

-- ⚡ СЛОЖНЫЙ ПРИМЕР - комбинация всех возможностей:
SELECT search_objects_with_facets(
    1002,
    '{
        "$and": [
            {"Status": {"$ne": "Deleted"}},
            {"$or": [
                {"Priority": {"$in": ["High", "Critical"]}},
                {"Tags[]": {"$arrayContains": "urgent"}}
            ]},
            {"CreatedDate": {"$gte": "2024-01-01"}},
            {"Price": {"$gt": "0"}},
            {"$not": {"Archive": true}}
        ],
        "$isRoot": false
    }'::jsonb,
    20, 0,
    '[{"field": "CreatedDate", "direction": "DESC"}]'::jsonb,
    15  -- max_recursion_depth = 15 для экстремально сложных запросов
);
*/
