# План модульного рефакторинга search_objects_with_facets

## Цель
Рефакторинг существующей функции `search_objects_with_facets` в модульную архитектуру без изменения функциональности. **Полностью сохранить текущую логику и поведение**, но разделить на переиспользуемые модули.

## Проблема текущей архитектуры
- **Монолитная функция**: ~430 строк с множественными ветвлениями
- **Сложные условные блоки**: `IF max_depth = 1 THEN ... ELSE IF distinct_mode THEN ... END IF`
- **Дублирование логики**: одинаковые паттерны для запроса и подсчета
- **Сложность поддержки**: изменения затрагивают большие блоки кода

## Архитектурное решение
**Модульная архитектура через вспомогательные функции:**
- ✅ **Выделить общую логику** в отдельные приватные функции (`_build_*`)
- ✅ **Создать специализированные перегрузки** с чистой уникальной логикой
- ✅ **PostgreSQL overloading** для автоматического выбора функции
- ✅ **Полное сохранение поведения** - никаких функциональных изменений
- ✅ **Пошаговый подход** - сначала модули, потом проверяем, потом batch

## Задачи

### Рекомендуемый порядок выполнения (снизу вверх):

### 1. Создание вспомогательных модулей
- [ ] В `redb.Core.Postgres/sql/redbPostgre.sql` создать приватные функции:

  - **Модуль фасетной фильтрации:**
    ```sql
    -- Построение WHERE условий на основе facet_filters
    CREATE OR REPLACE FUNCTION _build_facet_conditions(
        facet_filters jsonb
    ) RETURNS text
    LANGUAGE 'plpgsql' AS $$
    DECLARE
        filter_key text;
        filter_values jsonb;
        where_conditions text := '';
    BEGIN
        -- Перенести логику строк 1074-1220 из существующей функции
        IF facet_filters IS NOT NULL AND jsonb_typeof(facet_filters) = 'object' THEN
            FOR filter_key, filter_values IN SELECT * FROM jsonb_each(facet_filters) LOOP
                -- Вся существующая логика обработки фильтров
            END LOOP;
        END IF;
        
        RETURN where_conditions;
    END;
    $$;
    ```

  - **Модуль сортировки:**
    ```sql
    -- Построение ORDER BY условий
    CREATE OR REPLACE FUNCTION _build_order_conditions(
        order_by jsonb,
        use_recursive_alias boolean DEFAULT false
    ) RETURNS text
    LANGUAGE 'plpgsql' AS $$
    DECLARE
        order_conditions text := 'ORDER BY o._id';
    BEGIN
        -- Перенести логику строк 1221-1275 из существующей функции
        -- Если use_recursive_alias = true, то replace('o.', 'd.')
        
        IF use_recursive_alias THEN
            order_conditions := replace(order_conditions, 'o.', 'd.');
        END IF;
        
        RETURN order_conditions;
    END;
    $$;
    ```

  - **Модуль форматирования результата:**
    ```sql
    -- Построение финального JSON результата
    CREATE OR REPLACE FUNCTION _build_search_result(
        objects_result jsonb,
        total_count integer,
        limit_count integer,
        offset_count integer,
        scheme_id bigint
    ) RETURNS jsonb
    LANGUAGE 'plpgsql' AS $$
    BEGIN
        RETURN jsonb_build_object(
            'objects', COALESCE(objects_result, '[]'::jsonb),
            'total_count', total_count,
            'limit', limit_count,
            'offset', offset_count,
            'facets', get_facets(scheme_id)
        );
    END;
    $$;
    ```

### 2. Создание специализированных основных функций  
- [ ] Создать **базовую функцию** (без parent_id):
  ```sql
  CREATE OR REPLACE FUNCTION search_objects_with_facets(
      scheme_id bigint,
      facet_filters jsonb DEFAULT NULL,
      limit_count integer DEFAULT 100,
      offset_count integer DEFAULT 0,
      distinct_mode boolean DEFAULT false,
      order_by jsonb DEFAULT NULL
  ) RETURNS jsonb
  LANGUAGE 'plpgsql' AS $$
  DECLARE
      objects_result jsonb;
      total_count integer;
      where_conditions text := _build_facet_conditions(facet_filters);
      order_conditions text := _build_order_conditions(order_by, false);
      query_text text;
  BEGIN
      -- ТОЛЬКО уникальная логика простого поиска (без parent_id)
      -- Строить запрос типа: SELECT ... FROM _objects o WHERE o._id_scheme = scheme_id {where_conditions}
      
      EXECUTE query_text INTO objects_result;
      
      -- Аналогично для подсчета
      EXECUTE count_query INTO total_count;
      
      RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
  END;
  $$;
  ```

- [ ] Создать **функцию для детей** (с parent_id, max_depth = 1):
  ```sql
  CREATE OR REPLACE FUNCTION search_objects_with_facets(
      scheme_id bigint,
      facet_filters jsonb DEFAULT NULL,
      limit_count integer DEFAULT 100,
      offset_count integer DEFAULT 0,
      distinct_mode boolean DEFAULT false,
      order_by jsonb DEFAULT NULL,
      parent_id bigint
  ) RETURNS jsonb
  LANGUAGE 'plpgsql' AS $$
  DECLARE
      objects_result jsonb;
      total_count integer;
      where_conditions text := _build_facet_conditions(facet_filters);
      order_conditions text := _build_order_conditions(order_by, false);
      query_text text;
  BEGIN
      -- ТОЛЬКО уникальная логика поиска детей
      -- Добавить к where_conditions: format(' AND o._id_parent = %s', parent_id)
      
      -- Простой запрос (не рекурсивный, так как max_depth = 1)
      
      RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
  END;
  $$;
  ```

- [ ] Создать **функцию для потомков** (с parent_id, max_depth > 1):
  ```sql
  CREATE OR REPLACE FUNCTION search_objects_with_facets(
      scheme_id bigint,
      facet_filters jsonb DEFAULT NULL,
      limit_count integer DEFAULT 100,
      offset_count integer DEFAULT 0,
      distinct_mode boolean DEFAULT false,
      order_by jsonb DEFAULT NULL,
      parent_id bigint,
      max_depth integer
  ) RETURNS jsonb
  LANGUAGE 'plpgsql' AS $$
  DECLARE
      objects_result jsonb;
      total_count integer;
      where_conditions text := _build_facet_conditions(facet_filters);
      order_conditions text := _build_order_conditions(order_by, true); -- рекурсивный алиас
      query_text text;
  BEGIN
      -- ТОЛЬКО уникальная логика рекурсивного поиска
      -- WITH RECURSIVE descendants AS (...) логика
      
      IF distinct_mode THEN
          -- Рекурсивный DISTINCT запрос
      ELSE
          -- Рекурсивный обычный запрос  
      END IF;
      
      -- Аналогично для подсчета
      
      RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
  END;
  $$;
  ```

### 3. Удаление старой функции
- [ ] **Осторожно!** Удалить существующую монолитную функцию только после полного тестирования:
  ```sql
  -- DROP FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint, integer);
  ```

### 4. Обновление комментариев
- [ ] Добавить комментарии к новым функциям:
  ```sql
  COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb) IS 
  'Базовый фасетный поиск объектов указанной схемы без ограничений по иерархии';
  
  COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint) IS 
  'Фасетный поиск прямых дочерних объектов указанного родителя';
  
  COMMENT ON FUNCTION search_objects_with_facets(bigint, jsonb, integer, integer, boolean, jsonb, bigint, integer) IS 
  'Фасетный поиск всех потомков указанного родителя до заданной глубины с рекурсивным обходом';
  ```

### 5. Тестирование совместимости
- [ ] **Критично**: Проверить что **ВСЕ** существующие вызовы работают идентично:
  
  - **Базовые запросы** (QueryAsync):
    ```sql
    SELECT search_objects_with_facets(1, null, 10, 0, false, null);
    ```
  
  - **Запросы детей** (QueryChildrenAsync):
    ```sql
    SELECT search_objects_with_facets(1, null, 10, 0, false, null, 123);
    ```
  
  - **Запросы потомков** (QueryDescendantsAsync):
    ```sql
    SELECT search_objects_with_facets(1, null, 10, 0, false, null, 123, 3);
    ```
  
  - **С фильтрами**:
    ```sql
    SELECT search_objects_with_facets(1, '{"field":"value"}', 10, 0, false, null);
    SELECT search_objects_with_facets(1, '{"field":"value"}', 10, 0, false, null, 123);
    ```
  
  - **С сортировкой и DISTINCT**:
    ```sql
    SELECT search_objects_with_facets(1, null, 10, 0, true, '{"field":"asc"}');
    ```

### 6. Тестирование C# интеграции
- [ ] Запустить существующие C# тесты и убедиться что результаты **идентичны**:
  ```csharp
  // Должны работать без изменений:
  var test1 = await (await redb.QueryAsync<ValidationTestProps>()).ToListAsync();
  var test2 = await (await redb.QueryChildrenAsync<ValidationTestProps>(prod1)).ToListAsync();
  var test3 = await (await redb.QueryDescendantsAsync<AnalyticsMetricsProps>(prod1)).ToListAsync();
  ```

## Ключевые принципы рефакторинга

### 🎯 **Полное сохранение поведения:**
- **Никаких функциональных изменений** - только структурные
- **Идентичные результаты** для всех существующих вызовов
- **Та же производительность** или лучше

### 🧩 **Модульность:**
- **Приватные функции** начинаются с `_build_*`
- **Четкие интерфейсы** между модулями
- **Единственная ответственность** каждого модуля

### 🔄 **PostgreSQL overloading:**
- **Автоматический выбор** функции по количеству параметров
- **Чистые сигнатуры** без взаимоисключающих параметров
- **Обратная совместимость** - существующий C# код работает без изменений

### ⚡ **Производительность:**
- **Никаких лишних вызовов** - модули вызываются только при необходимости
- **Оптимизированные запросы** - каждая функция делает минимум работы
- **Переиспользование** кода без дублирования

## Примечания
- **Осторожность**: Это критический рефакторинг основной функции поиска
- **Тестирование**: Обязательное сравнение результатов до/после рефакторинга
- **Откат**: Сохранить резервную копию старой функции до полного тестирования
- **Поэтапность**: Сначала создать новые функции, протестировать, потом удалить старую
- **Подготовка к batch**: После успешного рефакторинга будет легко добавить parent_ids[] версии

## Результат
После выполнения получим:
- **🧩 Модульную архитектуру** с переиспользуемыми компонентами
- **🎯 Специализированные функции** для каждого типа поиска  
- **🚀 Готовность к расширению** - легко добавить batch операции
- **🧹 Чистый код** - каждая функция делает одну вещь хорошо
- **📈 Улучшенную поддерживаемость** - изменения локализованы в модулях

### 💡 **Следующий этап (после проверки):**
- Создание batch версий с `parent_ids bigint[]` параметром  
- Использование тех же модулей `_build_*` для batch функций
- Минимальные изменения благодаря модульной архитектуре
