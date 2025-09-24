-- Создаем тестовые объекты напрямую в PostgreSQL
-- Для проверки фильтрации Price == 2000

-- Убеждаемся что схема существует
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM _schemes WHERE _name = 'ProductTestProps') THEN
        RAISE EXCEPTION 'Схема ProductTestProps не найдена! Запустите сначала C# тесты.';
    END IF;
END $$;

-- Вставляем тестовый объект с Price=2000 напрямую
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
    INSERT INTO _objects (_id_scheme, _name, _note, _is_deleted, _date_created, _date_modified)
    SELECT scheme_id, 'Test Expensive Laptop', 'Direct SQL insert', false, NOW(), NOW()
    FROM scheme_info
    RETURNING _id
)
-- Вставляем значение Price=2000
INSERT INTO _values (_id_object, _id_structure, _Double)
SELECT no._id, ps.struct_id, 2000.0
FROM new_object no, price_structure ps;

-- Проверяем что объект создался
SELECT o._id, o._name, v._Double as price
FROM _objects o
JOIN _values v ON o._id = v._id_object
JOIN _structures s ON v._id_structure = s._id
WHERE o._id_scheme = (SELECT _id FROM _schemes WHERE _name = 'ProductTestProps')
AND s._name = 'Price';
