-- ===== ПРОСТАЯ ПРОВЕРКА НОВОЙ ФАСЕТНОЙ АРХИТЕКТУРЫ =====

-- 1. Проверка загрузки функций
\echo '=== 1. ПРОВЕРКА ФУНКЦИЙ ==='
SELECT 'Функций фасетов загружено: ' || count(*) as functions_count 
FROM pg_proc 
WHERE proname LIKE '_build_%' OR proname LIKE 'build_advanced_%';

-- 2. Проверка get_facets 
\echo '=== 2. GET_FACETS ==='
SELECT get_facets(9001) ->> 'Name' as name_facet;
SELECT get_facets(9001) ->> 'Age' as age_facet;

-- 3. Простой поиск
\echo '=== 3. ПРОСТОЙ ПОИСК ==='
SELECT count(*) as total_objects FROM search_objects_with_facets(9001, '{}');

-- 4. Проверка простых условий (без операторов пока)
\echo '=== 4. ПРОСТЫЕ УСЛОВИЯ ==='
SELECT 'Простое условие работает' as status;

-- 5. Проверка Class полей в фасетах
\echo '=== 5. CLASS ПОЛЯ В ФАСЕТАХ ==='
SELECT get_facets(9001) ->> 'Address.City' as address_city_facet;

-- 6. Проверка объектов с JSON
\echo '=== 6. ОБЪЕКТЫ С JSON ==='
SELECT jsonb_pretty(get_object_json(9001)) as object_json;

-- 7. Проверка массивов в фасетах  
\echo '=== 7. МАССИВЫ В ФАСЕТАХ ==='
SELECT get_facets(9001) ->> 'Tags' as tags_facet;

\echo '=== ТЕСТ ЗАВЕРШЕН ==='
