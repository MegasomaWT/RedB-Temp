-- Тест фильтрации Price == 2000 в PostgreSQL
-- Проверяем работает ли $eq оператор для Longitude типа

-- 1. Проверяем схему ProductTestProps
SELECT _id, _name FROM _schemes WHERE _name = 'ProductTestProps';

-- 2. Проверяем объекты в схеме (должны быть созданы заново)
SELECT _id, _name FROM _objects WHERE _id_scheme = 1000002 ORDER BY _id;

-- 3. Проверяем структуру поля Price 
SELECT s._name, t._name as type_name, s._id_type 
FROM _structures s 
JOIN _schemes sc ON s._id_scheme = sc._id 
JOIN _types t ON s._id_type = t._id 
WHERE sc._name = 'ProductTestProps' AND s._name = 'Price';

-- 4. ГЛАВНЫЙ ТЕСТ: Прямой вызов search_objects_with_facets с фильтром Price=$eq:2000
SELECT search_objects_with_facets(
    1000002,                           -- scheme_id для ProductTestProps  
    '{"Price": {"$eq": 2000}}'::jsonb, -- фильтр Price == 2000
    10,                                -- limit
    0,                                 -- offset
    '[]'::jsonb,                       -- order_by (пустой)
    10                                 -- max_recursion_depth
);

-- 5. ТЕСТ ДИАПАЗОНА: Price BETWEEN 1999-2001 (должен работать)
SELECT search_objects_with_facets(
    1000002,                                    -- scheme_id
    '{"Price": {"$gte": 1999, "$lte": 2001}}'::jsonb, -- диапазон 1999-2001
    10, 0, '[]'::jsonb, 10
);

-- 6. Проверяем все объекты без фильтра
SELECT search_objects_with_facets(
    1000002,                           -- scheme_id
    '{}'::jsonb,                       -- пустой фильтр (все объекты)
    10, 0, '[]'::jsonb, 10
);
