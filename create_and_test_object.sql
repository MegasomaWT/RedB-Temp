-- Создаем тестовый объект с правильной структурой и анализируем через get_object_json

-- 1. Получаем ID схемы ProductTestProps
SELECT _id as scheme_id, _name FROM _schemes WHERE _name = 'ProductTestProps';

-- 2. Создаем объект с правильными полями
WITH scheme_info AS (
    SELECT _id as scheme_id FROM _schemes WHERE _name = 'ProductTestProps'
),
price_structure AS (
    SELECT s._id as struct_id 
    FROM _structures s 
    JOIN scheme_info si ON s._id_scheme = si.scheme_id 
    WHERE s._name = 'Price'
),
new_object AS (
    INSERT INTO _objects (_id_scheme, _id_owner, _id_who_change, _name, _note)
    SELECT scheme_id, 0, 0, 'Test Expensive Laptop SQL', 'Direct SQL Price=2000'
    FROM scheme_info
    RETURNING _id
)
-- Вставляем значение Price=2000.0
INSERT INTO _values (_id_object, _id_structure, _Double)
SELECT no._id, ps.struct_id, 2000.0
FROM new_object no, price_structure ps
RETURNING _id_object as created_object_id;

-- 3. Получаем последний созданный объект
SELECT _id, _name FROM _objects 
WHERE _id_scheme = (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps')
ORDER BY _id DESC LIMIT 1;

-- 4. ГЛАВНОЕ: Используем get_object_json для анализа созданного объекта
WITH last_object AS (
    SELECT _id FROM _objects 
    WHERE _id_scheme = (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps')
    ORDER BY _id DESC LIMIT 1
)
SELECT get_object_json(_id, 10) as object_json
FROM last_object;

-- 5. Проверяем raw значения в таблице _values
SELECT o._id, o._name, s._name as field_name, v._Double, v._String, v._Long
FROM _objects o
JOIN _values v ON o._id = v._id_object
JOIN _structures s ON v._id_structure = s._id
WHERE o._id_scheme = (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps')
ORDER BY o._id DESC;
