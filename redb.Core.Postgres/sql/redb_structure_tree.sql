-- ====================================================================================================
-- ФУНКЦИИ ДЛЯ РАБОТЫ С ДЕРЕВОМ СТРУКТУР СХЕМЫ
-- ====================================================================================================
-- Поддерживает иерархическую навигацию по структурам: parent → children → descendants
-- Решает проблемы плоского поиска структур в SaveAsync
-- ====================================================================================================

-- ОСНОВНАЯ ФУНКЦИЯ: Построение дерева структур схемы (ПРОСТОЙ ПОДХОД)
-- ✅ ПРОСТАЯ И ПОНЯТНАЯ ЛОГИКА: получаем текущий слой → для каждой структуры получаем детей рекурсивно
CREATE OR REPLACE FUNCTION get_scheme_structure_tree(
    scheme_id bigint,
    parent_id bigint DEFAULT NULL,
    max_depth integer DEFAULT 10
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    result jsonb := '[]'::jsonb;
    structure_record RECORD;
    children_json jsonb;
BEGIN
    -- Защита от бесконечной рекурсии
    IF max_depth <= 0 THEN
        RETURN jsonb_build_array(jsonb_build_object('error', 'Max recursion depth reached'));
    END IF;
    
    -- Проверяем существование схемы
    IF NOT EXISTS(SELECT 1 FROM _schemes WHERE _id = scheme_id) THEN
        RETURN jsonb_build_array(jsonb_build_object('error', 'Scheme not found'));
    END IF;
    
    -- ✅ ПРОСТАЯ ЛОГИКА: Получаем структуры ТЕКУЩЕГО УРОВНЯ
    FOR structure_record IN
        SELECT 
            s._id,
            s._name,
            s._order,
            s._is_array,
            s._store_null,
            s._allow_not_null,
            t._name as type_name,
            t._db_type as db_type,
            t._type as type_semantic
        FROM _structures s
        JOIN _types t ON t._id = s._id_type
        WHERE s._id_scheme = scheme_id
          AND ((parent_id IS NULL AND s._id_parent IS NULL) 
               OR (parent_id IS NOT NULL AND s._id_parent = parent_id))
        ORDER BY s._order, s._id
    LOOP
        -- ✅ ПРОВЕРЯЕМ ЕСТЬ ЛИ ДЕТИ у данной структуры
        IF EXISTS(SELECT 1 FROM _structures 
                 WHERE _id_scheme = scheme_id 
                   AND _id_parent = structure_record._id) THEN
            -- 🔄 РЕКУРСИВНО получаем детей (простой вызов функции!)
            children_json := get_scheme_structure_tree(scheme_id, structure_record._id, max_depth - 1);
        ELSE
            -- Нет детей - пустой массив
            children_json := '[]'::jsonb;
        END IF;
        
        -- ✅ ДОБАВЛЯЕМ СТРУКТУРУ В РЕЗУЛЬТАТ (простое конструирование)
        result := result || jsonb_build_array(
            jsonb_build_object(
                'structure_id', structure_record._id,
                'name', structure_record._name,
                'order', structure_record._order,
                'is_array', structure_record._is_array,
                'store_null', structure_record._store_null,
                'allow_not_null', structure_record._allow_not_null,
                'type_name', structure_record.type_name,
                'db_type', structure_record.db_type,
                'type_semantic', structure_record.type_semantic,
                'children', children_json  -- ✅ Рекурсивно полученные дети
            )
        );
    END LOOP;
    
    RETURN result;
END;
$BODY$;

-- ВСПОМОГАТЕЛЬНАЯ ФУНКЦИЯ: Получение только прямых дочерних структур  
CREATE OR REPLACE FUNCTION get_structure_children(
    scheme_id bigint,
    parent_id bigint
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 50
VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
    RETURN (
        SELECT COALESCE(jsonb_agg(
            jsonb_build_object(
                'structure_id', s._id,
                'name', s._name,
                'order', s._order,
                'is_array', s._is_array,
                'type_name', t._name,
                'db_type', t._db_type,
                'type_semantic', t._type
            ) ORDER BY s._order, s._id
        ), '[]'::jsonb)
        FROM _structures s
        JOIN _types t ON t._id = s._id_type
        WHERE s._id_scheme = scheme_id
          AND s._id_parent = parent_id
    );
END;
$BODY$;

-- ДИАГНОСТИЧЕСКАЯ ФУНКЦИЯ: Валидация дерева структур на избыточность
CREATE OR REPLACE FUNCTION validate_structure_tree(
    scheme_id bigint
) RETURNS jsonb
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    validation_result jsonb;
    excessive_structures jsonb;
    orphaned_structures jsonb;
    circular_references jsonb;
BEGIN
    -- 1. Поиск избыточных структур (структуры без связей с values)
    SELECT jsonb_agg(
        jsonb_build_object(
            'structure_id', s._id,
            'name', s._name,
            'parent_name', parent_s._name,
            'issue', 'No values found - possibly excessive structure'
        )
    ) INTO excessive_structures
    FROM _structures s
    LEFT JOIN _structures parent_s ON parent_s._id = s._id_parent
    LEFT JOIN _values v ON v._id_structure = s._id
    WHERE s._id_scheme = scheme_id
      AND v._id IS NULL  -- Нет values для этой структуры
      AND s._id_parent IS NOT NULL; -- Только дочерние структуры
    
    -- 2. Поиск потерянных структур (parent не существует)
    SELECT jsonb_agg(
        jsonb_build_object(
            'structure_id', s._id,
            'name', s._name,
            'parent_id', s._id_parent,
            'issue', 'Parent structure does not exist'
        )
    ) INTO orphaned_structures
    FROM _structures s
    WHERE s._id_scheme = scheme_id
      AND s._id_parent IS NOT NULL
      AND NOT EXISTS(SELECT 1 FROM _structures parent_s WHERE parent_s._id = s._id_parent);
    
    -- 3. Простая проверка на циклические ссылки (структура ссылается на себя через цепочку)
    WITH RECURSIVE cycle_check AS (
        SELECT _id, _id_parent, ARRAY[_id] as path, false as has_cycle
        FROM _structures WHERE _id_scheme = scheme_id AND _id_parent IS NOT NULL
        
        UNION ALL
        
        SELECT s._id, s._id_parent, cc.path || s._id, s._id = ANY(cc.path)
        FROM _structures s
        JOIN cycle_check cc ON cc._id_parent = s._id
        WHERE NOT cc.has_cycle AND array_length(cc.path, 1) < 50
    )
    SELECT jsonb_agg(
        jsonb_build_object(
            'structure_id', _id,
            'path', path,
            'issue', 'Circular reference detected'
        )
    ) INTO circular_references
    FROM cycle_check 
    WHERE has_cycle;
    
    -- Формирование итогового отчета
    validation_result := jsonb_build_object(
        'scheme_id', scheme_id,
        'validation_date', NOW(),
        'excessive_structures', COALESCE(excessive_structures, '[]'::jsonb),
        'orphaned_structures', COALESCE(orphaned_structures, '[]'::jsonb), 
        'circular_references', COALESCE(circular_references, '[]'::jsonb),
        'total_structures', (SELECT COUNT(*) FROM _structures WHERE _id_scheme = scheme_id),
        'is_valid', (excessive_structures IS NULL AND orphaned_structures IS NULL AND circular_references IS NULL)
    );
    
    RETURN validation_result;
END;
$BODY$;

-- ФУНКЦИЯ: Получение всех потомков структуры (плоский список)
CREATE OR REPLACE FUNCTION get_structure_descendants(
    scheme_id bigint,
    parent_id bigint
) RETURNS jsonb
LANGUAGE 'plpgsql'  
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
    RETURN (
        WITH RECURSIVE descendants AS (
            -- Прямые дочерние структуры
            SELECT _id, _name, _id_parent, 0 as level
            FROM _structures 
            WHERE _id_scheme = scheme_id AND _id_parent = parent_id
            
            UNION ALL
            
            -- Рекурсивно все потомки
            SELECT s._id, s._name, s._id_parent, d.level + 1
            FROM _structures s
            JOIN descendants d ON d._id = s._id_parent
            WHERE s._id_scheme = scheme_id AND d.level < 10
        )
        SELECT COALESCE(jsonb_agg(
            jsonb_build_object(
                'structure_id', _id,
                'name', _name, 
                'parent_id', _id_parent,
                'level', level
            ) ORDER BY level, _id
        ), '[]'::jsonb)
        FROM descendants
    );
END;
$BODY$;

-- Комментарии к функциям дерева структур
COMMENT ON FUNCTION get_scheme_structure_tree(bigint, bigint, integer) IS 'Построение полного дерева структур схемы с иерархией. Поддерживает ограничение глубины рекурсии. Используется PostgresSchemeSyncProvider для корректного обхода структур в SaveAsync.';

COMMENT ON FUNCTION get_structure_children(bigint, bigint) IS 'Получение только прямых дочерних структур без рекурсии. Быстрая функция для простых случаев навигации по дереву.';

COMMENT ON FUNCTION validate_structure_tree(bigint) IS 'Диагностика дерева структур: поиск избыточных структур, потерянных ссылок, циклических зависимостей. Помогает выявить проблемы как с Address.Details.Tags1.';

COMMENT ON FUNCTION get_structure_descendants(bigint, bigint) IS 'Получение всех потомков структуры в плоском формате с указанием уровня вложенности. Полезно для анализа глубоких иерархий.';
