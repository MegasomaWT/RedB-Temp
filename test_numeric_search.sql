-- Детальная проверка поиска числовых значений в REDB
-- Анализируем почему $eq не работает, а $gte/$lte работают

-- 1. Проверяем как хранятся числовые значения в _values для Price=2000
SELECT 
    o._id as object_id,
    o._name as object_name,
    s._name as field_name,
    ft._name as type_name,
    ft._db_type as db_type,
    v._Long as long_value,
    v._Double as double_value,
    v._String as string_value
FROM _objects o
JOIN _values v ON o._id = v._id_object
JOIN _structures s ON v._id_structure = s._id
JOIN _types ft ON s._id_type = ft._id
WHERE o._id_scheme = (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps')
  AND s._name = 'Price'
  AND o._name LIKE '%Expensive%'
LIMIT 5;

-- 2. ТЕСТИРУЕМ SQL функцию search_objects_with_facets с разными операторами

-- 2a. Тест $eq оператора (НЕ РАБОТАЕТ)
SELECT 'TEST $eq: Price = 2000' as test_name,
       (search_objects_with_facets(
           (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps'),
           '{"Price": {"$eq": "2000"}}'::jsonb,
           10, 0, '[]'::jsonb, 10
       ))->>'total_count' as found_count;

-- 2b. Тест $eq с явным double (НЕ РАБОТАЕТ?)  
SELECT 'TEST $eq: Price = 2000.0' as test_name,
       (search_objects_with_facets(
           (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps'),
           '{"Price": {"$eq": "2000.0"}}'::jsonb,
           10, 0, '[]'::jsonb, 10
       ))->>'total_count' as found_count;

-- 2c. Тест $gte оператора (РАБОТАЕТ)
SELECT 'TEST $gte: Price >= 2000' as test_name,
       (search_objects_with_facets(
           (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps'),
           '{"Price": {"$gte": "2000"}}'::jsonb,
           10, 0, '[]'::jsonb, 10
       ))->>'total_count' as found_count;

-- 2d. Тест диапазона (РАБОТАЕТ)
SELECT 'TEST range: Price 2000-2000' as test_name,
       (search_objects_with_facets(
           (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps'),
           '{"Price": {"$gte": "2000", "$lte": "2000"}}'::jsonb,
           10, 0, '[]'::jsonb, 10
       ))->>'total_count' as found_count;

-- 3. ПРЯМОЙ ТЕСТ ФУНКЦИИ _build_inner_condition
-- Эмулируем как функция определяет тип для разных значений

-- 3a. Проверяем regex для определения типа
SELECT 
    '2000' ~ '^-?\d+$' as is_integer_regex,
    '2000.0' ~ '^-?\d+$' as is_double_regex,
    '2000.5' ~ '^-?\d+$' as is_float_regex;

-- 4. Проверяем структуру поля Price
SELECT 
    s._name as field_name,
    ft._name as type_name,
    ft._db_type as db_type,
    s._id as structure_id,
    ft._id as type_id
FROM _structures s
JOIN _types ft ON s._id_type = ft._id
WHERE s._id_scheme = (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps')
  AND s._name = 'Price';
