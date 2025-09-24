# План рефакторинга SQL для устранения дублирования кода

## 📊 Анализ проблемы

### Текущее состояние:
- **Старый файл**: 1,915 строк
- **Новый файл**: 3,101 строк  
- **Рост**: 1,186 строк (62% увеличение!)
- **Основная причина**: Массовое дублирование кода для поддержки операторов

### Выявленное дублирование:

1. **Оператор `$in`** - повторяется 5 раз (~40 строк × 5 = 200 строк):
   - В `_build_single_condition`
   - В `_build_and_condition`  
   - В `_build_or_condition`
   - В `_build_not_condition`
   - В `_build_facet_conditions`

2. **Операторы сравнения** - повторяются 4+ раза:
   - `$gt`, `$lt`, `$gte`, `$lte` (~20 строк × 4 оператора × 4 места = 320 строк)
   - `$startsWith`, `$endsWith`, `$contains` (~15 строк × 3 оператора × 4 места = 180 строк)

3. **CASE для типов JSONB** - повторяется 5 раз (~15 строк × 5 = 75 строк)

**Итого дублированного кода: ~775 строк**

## 🎯 Цель рефакторинга

Сократить размер файла на 500-600 строк путем выноса дублированного кода в переиспользуемые функции, сохранив всю функциональность.

## 📋 План рефакторинга

### 1. Создать вспомогательную функцию для форматирования JSON массива

```sql
CREATE OR REPLACE FUNCTION _format_json_array_for_in(
    json_array jsonb
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
    FOR json_element IN SELECT value FROM jsonb_array_elements(json_array) LOOP
        IF NOT first_item THEN
            in_values := in_values || ', ';
        END IF;
        first_item := false;
        
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
```

### 2. Упростить все места использования $in

Вместо 40 строк кода будет:
```sql
WHEN '$in' THEN
    condition := format(
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
        _format_json_array_for_in(operator_value::jsonb),
        _format_json_array_for_in(operator_value::jsonb),
        _format_json_array_for_in(operator_value::jsonb),
        _format_json_array_for_in(operator_value::jsonb)
    );
```

### 3. Создать макро-функцию для числовых операторов

```sql
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
```

### 4. Создать макро-функцию для строковых операторов

```sql
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
```

### 5. Рефакторинг _build_single_condition

```sql
CREATE OR REPLACE FUNCTION _build_single_condition(
    field_name text,
    operator_name text,
    operator_value text
) RETURNS text
LANGUAGE 'plpgsql'
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
            _format_json_array_for_in(operator_value::jsonb),
            _format_json_array_for_in(operator_value::jsonb),
            _format_json_array_for_in(operator_value::jsonb),
            _format_json_array_for_in(operator_value::jsonb)
        );
    
    ELSE
        RAISE EXCEPTION 'Неподдерживаемый оператор: %', operator_name;
    END IF;
END;
$BODY$;
```

### 6. Упрощение остальных функций

В `_build_and_condition`, `_build_or_condition`, `_build_not_condition` и `_build_facet_conditions`:

Заменить весь дублированный код на:
```sql
-- Вместо 10-15 строк на каждый оператор
condition := condition || _build_single_condition(field_name, operator_key, operator_value);
```

## 📈 Ожидаемые результаты

1. **Сокращение кода**: с 3,101 до ~2,500 строк (экономия ~600 строк)
2. **Упрощение поддержки**: изменения в одном месте применяются везде
3. **Лучшая читаемость**: логика разделена на понятные функции
4. **Легкость добавления новых операторов**: достаточно добавить в одну функцию

## 🔧 Порядок внедрения

1. Создать вспомогательные функции (без изменения существующих)
2. Протестировать вспомогательные функции отдельно
3. Заменить код в `_build_single_condition`
4. Протестировать
5. Заменить код в остальных функциях по очереди
6. Финальное тестирование всей системы

## ⚠️ Риски и их минимизация

1. **Риск**: Изменение производительности
   - **Решение**: Пометить функции как IMMUTABLE для оптимизации

2. **Риск**: Потеря функциональности при рефакторинге
   - **Решение**: Поэтапное внедрение с тестированием после каждого шага

3. **Риск**: Проблемы с порядком создания функций
   - **Решение**: Создавать функции в правильном порядке (сначала вспомогательные)

## ✅ Критерии успеха

1. Все тесты Stage37 проходят успешно
2. Размер файла сокращен минимум на 500 строк
3. Нет дублирования логики обработки операторов
4. Код легко расширяется для новых операторов
