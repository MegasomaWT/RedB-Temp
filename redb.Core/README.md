# REDB - Анализ структуры базы данных

## Обзор системы

**REDB** (Relational Entity Database) - это универсальная система управления данными, построенная на основе паттерна **EAV (Entity-Attribute-Value)** с расширениями для создания гибких, масштабируемых приложений с динамической структурой данных.

## Архитектурный паттерн

### Основной паттерн: Enhanced EAV (Entity-Attribute-Value)

**EAV** - это паттерн проектирования базы данных, который позволяет хранить данные в виде сущностей (Entity), атрибутов (Attribute) и значений (Value). REDB расширяет классический EAV следующими возможностями:

- **Типизированные схемы** - система типов для валидации данных
- **Иерархические структуры** - поддержка вложенных объектов и схем  
- **Система безопасности** - гранулярные права доступа
- **Версионирование** - отслеживание изменений и удалений
- **Метаданные** - расширенная информация о структуре данных

## Структура базы данных

### Основные таблицы

#### 1. Система типов и метаданных
```sql
_types        -- Типы данных (String, Long, DateTime, Boolean, etc.)
_schemes      -- Схемы объектов (типы сущностей)
_structures   -- Структуры полей (метаданные атрибутов)
```

#### 2. Хранение данных  
```sql
_objects      -- Основные сущности (Entity)
_values       -- Значения полей (Value)
_links        -- Связи между объектами
```

#### 3. Система безопасности
```sql
_users        -- Пользователи системы
_roles        -- Роли пользователей
_users_roles  -- Связь пользователей и ролей
_permissions  -- Права доступа к объектам
```

#### 4. Вспомогательные таблицы
```sql
_lists           -- Справочники
_list_items      -- Элементы справочников
_dependencies    -- Зависимости между схемами
_functions       -- Пользовательские функции
_deleted_objects -- Архив удаленных объектов
```

## Детальный анализ таблиц

### _objects - Основная таблица сущностей
```sql
_id              -- Уникальный идентификатор
_id_parent       -- Иерархическая связь (self-reference)
_id_scheme       -- Тип объекта (ссылка на _schemes)
_id_owner        -- Владелец объекта
_id_who_change   -- Кто последний изменял
_date_create     -- Дата создания
_date_modify     -- Дата последнего изменения
_date_begin      -- Дата начала действия
_date_complete   -- Дата завершения
_key             -- Дополнительный ключ
_code_int        -- Числовой код
_code_string     -- Строковый код
_code_guid       -- GUID код
_name            -- Наименование
_note            -- Примечание
_bool            -- Логическое поле для быстрого доступа
_hash            -- GUID хеш для быстрого сравнения
```

**Особенности:**
- Поддержка иерархии через `_id_parent`
- Множественные идентификаторы для гибкости
- Аудит изменений (кто, когда)
- Временные рамки существования объекта

### _values - Таблица значений (EAV Core)
```sql
_id              -- Уникальный идентификатор
_id_structure    -- Ссылка на структуру поля
_id_object       -- Ссылка на объект
_String          -- Строковое значение (до 850 символов)
_Long            -- Числовое значение
_Guid            -- GUID значение
_Double          -- Число с плавающей точкой
_DateTime        -- Дата и время
_Boolean         -- Логическое значение
_ByteArray       -- Бинарные данные
_Text            -- Длинный текст
_Array           -- JSON массив (если _is_array = true в структуре)
```

**Особенности:**
- Все типы данных в одной таблице
- Уникальное ограничение на пару (структура, объект)
- Поддержка NULL значений
- **Поддержка массивов** - если в `_structures._is_array = true`, значение хранится в поле `_Array` как JSON
  - **Примитивы**: `["str1", "str2"]`, `[1, 2, 3]`, `[true, false]`
  - **Объекты**: `[101, 102, 103]` (только ID, полные объекты извлекаются рекурсивно)

### _structures - Метаданные полей
```sql
_id              -- Уникальный идентификатор
_id_parent       -- Родительская структура (для вложенных)
_id_scheme       -- Схема, к которой относится
_id_override     -- Переопределение структуры
_id_type         -- Тип данных
_id_list         -- Связанный справочник
_name            -- Имя поля
_alias           -- Псевдоним
_order           -- Порядок отображения
_readonly        -- Только для чтения
_allow_not_null  -- Обязательность поля
_is_array        -- Массив значений
_is_compress     -- Сжатие данных
_store_null      -- Сохранять NULL
_default_value   -- Значение по умолчанию
_default_editor  -- Редактор по умолчанию
```

## Технологические решения

### 1. Универсальность схемы
- **Динамическое создание типов** - новые сущности без изменения схемы БД
- **Гибкая валидация** - правила на уровне метаданных
- **Расширяемость** - добавление новых типов данных

### 2. Производительность
- **Индексы по типам данных** - быстрый поиск по значениям
- **Партиционирование по схемам** - возможно для больших объемов
- **Денормализация** - дублирование часто используемых данных

### 3. Целостность данных
- **Каскадное удаление** - автоматическая очистка связанных данных
- **Триггеры аудита** - автоматическое сохранение удаленных объектов
- **Констрейнты** - проверка бизнес-правил на уровне БД
- **Валидация имен полей и схем** - триггеры проверки корректности названий согласно правилам C#

### 4. Функции поиска и извлечения данных
- **JSON извлечение объектов** - `get_object_json()` с рекурсией и метаданными
- **Фасетный поиск** - `get_facets()` для построения динамических фильтров
- **Поиск с фильтрацией** - `search_objects_with_facets()` с пагинацией
- **Полнотекстовый поиск** - интеграция с PostgreSQL FTS
- **Типизированная обработка** - поддержка всех типов EAV включая Object и ListItem

## Практики использования

### 1. Создание новых типов объектов

```sql
-- 1. Создать схему
INSERT INTO _schemes (_id, _name, _alias) 
VALUES (nextval('global_identity'), 'Employee', 'Сотрудник');

-- 2. Определить структуру полей
INSERT INTO _structures (_id, _id_scheme, _id_type, _name, _alias, _order) 
VALUES 
  (nextval('global_identity'), [scheme_id], [string_type_id], 'first_name', 'Имя', 1),
  (nextval('global_identity'), [scheme_id], [string_type_id], 'last_name', 'Фамилия', 2),
  (nextval('global_identity'), [scheme_id], [datetime_type_id], 'birth_date', 'Дата рождения', 3);

-- 3. Создать объект
INSERT INTO _objects (_id, _id_scheme, _id_owner, _id_who_change, _name) 
VALUES (nextval('global_identity'), [scheme_id], [user_id], [user_id], 'Иванов Иван');

-- 4. Заполнить значения
INSERT INTO _values (_id, _id_structure, _id_object, _String) 
VALUES 
  (nextval('global_identity'), [first_name_structure_id], [object_id], 'Иван'),
  (nextval('global_identity'), [last_name_structure_id], [object_id], 'Иванов');
```

### 2. Поиск данных

```sql
-- Поиск объектов с определенными значениями
SELECT DISTINCT o._id, o._name 
FROM _objects o
JOIN _values v ON v._id_object = o._id
JOIN _structures s ON s._id = v._id_structure
WHERE s._name = 'first_name' AND v._String = 'Иван';

-- Поиск с множественными условиями
SELECT o._id, o._name
FROM _objects o
WHERE o._id IN (
    SELECT v1._id_object 
    FROM _values v1 
    JOIN _structures s1 ON s1._id = v1._id_structure
    WHERE s1._name = 'first_name' AND v1._String = 'Иван'
    INTERSECT
    SELECT v2._id_object 
    FROM _values v2 
    JOIN _structures s2 ON s2._id = v2._id_structure
    WHERE s2._name = 'last_name' AND v2._String = 'Иванов'
);
```

### 3. Извлечение JSON объектов

Система предоставляет мощную функцию для извлечения объектов в JSON формате с поддержкой рекурсии:

```sql
-- Получить объект (базовые поля в корне + properties)
SELECT get_object_json(object_id);

-- Ограничить глубину рекурсии для связанных объектов  
SELECT get_object_json(object_id, max_depth := 3);
```

**Пример результата:**
```json
{
  "id": 1001,
  "name": "Петров Иван",
  "scheme_id": 100,
  "scheme_name": "Employee",
  "parent_id": null,
  "owner_id": 1,
  "who_change_id": 1,
  "date_create": "2024-01-15T10:30:00",
  "date_modify": "2024-01-20T14:25:00",
  "date_begin": null,
  "date_complete": null,
  "key": null,
  "code_int": null,
  "code_string": "EMP001",
  "code_guid": null,
  "note": "Ведущий разработчик",
  "bool": true,
  "hash": "550e8400-e29b-41d4-a716-446655440000",
  "properties": {
    "firstName": "Иван",
    "lastName": "Петров", 
    "department": "IT",
    "salary": 120000,
    "isActive": true,
    "manager": {
      "id": 1002,
      "name": "Сидорова Анна",
      "scheme_id": 100,
      "scheme_name": "Employee",
      "properties": {
        "firstName": "Анна",
        "lastName": "Сидорова",
        "department": "HR"
      }
    },
    "skills": [
      {"id": 1, "value": "C#"},
      {"id": 2, "value": "PostgreSQL"}
    ]
  }
}
```

#### Особенности функции:
- **Единая структура JSON** - базовые поля объекта в корне, свойства в разделе properties
- **Рекурсивная обработка** связанных объектов (тип Object) 
- **Поддержка массивов** - если `_is_array = true`, обрабатывает данные из поля `_Array`
  - **Массивы примитивов** - `["значение1", "значение2"]`
  - **Массивы объектов** - `[1, 2, 3]` (массив ID) с рекурсивной обработкой каждого объекта через `get_object_json()`
- **Умная типизация** - автоматическое определение типа поля
- **Поддержка всех типов** EAV (String, Long, DateTime, Boolean, etc.)
- **Обработка справочников** (ListItem) с получением значений
- **Base64 кодирование** бинарных данных
- **Защита от зацикливания** с ограничением глубины
- **Полная информация** - все базовые поля + схема + свойства в одном JSON

## Стратегии поиска

### 1. Индексирование

```sql
-- Индексы по значениям для быстрого поиска
CREATE INDEX idx_values_string ON _values (_String) WHERE _String IS NOT NULL;
CREATE INDEX idx_values_long ON _values (_Long) WHERE _Long IS NOT NULL;
CREATE INDEX idx_values_datetime ON _values (_DateTime) WHERE _DateTime IS NOT NULL;

-- Составные индексы для оптимизации запросов
CREATE INDEX idx_values_structure_string ON _values (_id_structure, _String);
CREATE INDEX idx_values_object_structure ON _values (_id_object, _id_structure);

-- Индекс для иерархических запросов
CREATE INDEX idx_objects_parent ON _objects (_id_parent);
CREATE INDEX idx_objects_scheme ON _objects (_id_scheme);
```

### 2. Полнотекстовый поиск

```sql
-- Создание составного текстового индекса
CREATE INDEX idx_fulltext_search ON _values 
USING gin(to_tsvector('russian', coalesce(_String, '') || ' ' || coalesce(_Text, '')));

-- Функция полнотекстового поиска
CREATE OR REPLACE FUNCTION search_objects(search_text text)
RETURNS TABLE(object_id bigint, relevance real) AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT v._id_object, ts_rank(
        to_tsvector('russian', coalesce(v._String, '') || ' ' || coalesce(v._Text, '')),
        to_tsquery('russian', search_text)
    ) as relevance
    FROM _values v
    WHERE to_tsvector('russian', coalesce(v._String, '') || ' ' || coalesce(v._Text, ''))
          @@ to_tsquery('russian', search_text)
    ORDER BY relevance DESC;
END;
$$ LANGUAGE plpgsql;
```

### 3. Фасетный поиск

Система включает мощные функции для фасетного поиска и фильтрации данных:

#### Функция получения фасетов
```sql
-- Получить все уникальные значения полей для схемы
SELECT get_facets(scheme_id);
```

**Пример результата:**
```json
{
  "department": ["IT", "HR", "Финансы"],
  "city": ["Москва", "СПб", "Екатеринбург"],
  "salary": [50000, 75000, 100000, 120000],
  "is_active": [true, false],
  "manager": [
    {"id": 123, "name": "Иванов И.И."},
    {"id": 456, "name": "Петров П.П."}
  ]
}
```

#### Функция фасетного поиска с фильтрацией
```sql
-- Поиск сотрудников с фильтрами
SELECT search_objects_with_facets(
    scheme_id := 100,
    facet_filters := '{
        "department": ["IT", "HR"],
        "city": ["Москва"],
        "salary": [100000, 120000]
    }'::jsonb,
    limit_count := 50,
    offset_count := 0
);
```

**Пример результата:**
```json
{
  "objects": [
    {
      "id": 1001,
      "name": "Иванов Иван",
      "scheme_id": 100,
      "scheme_name": "Employee",
      "parent_id": null,
      "owner_id": 1,
      "who_change_id": 1,
      "date_create": "2024-01-15T10:30:00Z",
      "date_modify": "2024-01-20T14:20:00Z",
      "date_begin": null,
      "date_complete": null,
      "key": null,
      "code_int": 12345,
      "code_string": "EMP001",
      "code_guid": null,
      "note": "Ведущий разработчик",
      "bool": true,
      "hash": "550e8400-e29b-41d4-a716-446655440000",
      "properties": {
        "department": "IT",
        "city": "Москва", 
        "salary": 120000,
        "is_active": true
      }
    }
  ],
  "total_count": 25,
  "limit": 50,
  "offset": 0,
  "facets": {
    "department": ["IT", "HR"],
    "city": ["Москва", "СПб"],
    "salary": [100000, 120000]
  }
}
```

#### Поддерживаемые типы данных в фасетах:
- **String, Long, Double, Boolean, DateTime** - простые значения
- **Object** - связанные объекты `{"id": 123, "name": "Имя объекта"}`
- **ListItem** - элементы справочников `{"id": 456, "value": "Значение"}`
- **ByteArray** - бинарные данные в Base64
- **Text** - длинные текстовые поля

#### Особенности реализации:
- **Автоматическое исключение** удаленных объектов
- **Поддержка пагинации** (limit/offset)
- **Динамическая фильтрация** по любым полям
- **Оптимизированные запросы** с JOIN и индексами
- **Типизированная обработка** всех типов данных EAV

## Сравнительная таблица подходов к БД

| Критерий | Traditional RDBMS | NoSQL Document | EAV (REDB) | Graph DB | Object DB |
|----------|------------------|----------------|-------------|-----------|-----------|
| **Схема данных** | Жесткая, статичная | Гибкая, динамичная | Гибкая с метаданными | Гибкая | Объектно-ориентированная |
| **Производительность чтения** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Производительность записи** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| **Масштабируемость** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Целостность данных** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Сложность запросов** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| **Изменение схемы** | ⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Поддержка связей** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Аналитика** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐ |
| **Простота разработки** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ | ⭐⭐⭐ |

### Преимущества REDB EAV подхода

**✅ Плюсы:**
- Универсальность структуры данных
- Быстрое прототипирование
- Гибкость бизнес-логики
- Централизованная система безопасности
- Встроенный аудит изменений
- Поддержка сложных иерархий
- Возможность создания универсальных UI

**❌ Минусы:**
- Сложность написания запросов
- Потенциальные проблемы с производительностью
- Сложность отладки
- Требует глубокого понимания архитектуры
- Сложность миграции данных

## Сценарии использования

### Идеально подходит для:
- **CMS и портальные решения** - множество типов контента
- **CRM системы** - разнообразные типы клиентов и взаимодействий
- **Конфигураторы продуктов** - сложные иерархии параметров
- **Системы документооборота** - разные типы документов
- **IoT платформы** - различные типы датчиков и данных
- **Административные системы** - множество справочников

### Не рекомендуется для:
- **Высоконагруженные OLTP** - много простых транзакций
- **Аналитические системы** - требуют оптимизированную структуру
- **Системы реального времени** - критична скорость отклика
- **Простые приложения** - избыточная сложность

## Валидация имен полей и схем

В системе REDB реализованы триггеры валидации для обеспечения корректности именования согласно правилам C#:

### 1. Валидация полей (`_structures`)
Триггер `validate_structure_name()` проверяет корректность именования полей в таблице `_structures`.

### Правила валидации:

#### 1. Запрещенные системные имена
Имена полей не могут совпадать с системными полями таблицы `_objects`:
```
_id, _id_parent, _id_scheme, _id_owner, _id_who_change,
_date_create, _date_modify, _date_begin, _date_complete,
_key, _code_int, _code_string, _code_guid, _name, _note, _bool, _hash
```

#### 2. Правила именования C#
- **Начало имени**: только буква (a-z, A-Z) или символ подчеркивания (_)
- **Символы**: только латинские буквы, цифры и символы подчеркивания
- **Запрещены**: пробелы, точки, дефисы, спецсимволы (@, #, $, % и т.д.)
- **Длина**: максимум 64 символа

#### 3. Зарезервированные слова C#
Имена полей не могут быть зарезервированными словами C#:
```
abstract, as, base, bool, break, byte, case, catch, char, checked,
class, const, continue, decimal, default, delegate, do, double, else,
enum, event, explicit, extern, false, finally, fixed, float, for,
foreach, goto, if, implicit, in, int, interface, internal, is, lock,
long, namespace, new, null, object, operator, out, override, params,
private, protected, public, readonly, ref, return, sbyte, sealed,
short, sizeof, stackalloc, static, string, struct, switch, this,
throw, true, try, typeof, uint, ulong, unchecked, unsafe, ushort,
using, virtual, void, volatile, while
```

### Примеры использования:

**✅ Корректные имена:**
```sql
firstName, lastName, phoneNumber, _address, user_Age, Account123
```

**❌ Некорректные имена:**
```sql
-- Системные поля
INSERT INTO _structures (..., _name, ...) VALUES (..., '_id', ...);  -- ОШИБКА

-- Имена с недопустимыми символами
INSERT INTO _structures (..., _name, ...) VALUES (..., 'first.Name', ...);  -- ОШИБКА
INSERT INTO _structures (..., _name, ...) VALUES (..., 'first-name', ...);  -- ОШИБКА
INSERT INTO _structures (..., _name, ...) VALUES (..., 'first name', ...);  -- ОШИБКА

-- Имена, начинающиеся с цифры
INSERT INTO _structures (..., _name, ...) VALUES (..., '1stName', ...);  -- ОШИБКА

-- Зарезервированные слова
INSERT INTO _structures (..., _name, ...) VALUES (..., 'class', ...);  -- ОШИБКА
INSERT INTO _structures (..., _name, ...) VALUES (..., 'public', ...);  -- ОШИБКА
```

### Тестирование триггера полей:
Для проверки работы триггера выполните:
```sql
\i redb.Core.Postgres/sql/test_structure_validation.sql
```

### 2. Валидация схем (`_schemes`)
Триггер `validate_scheme_name()` проверяет корректность именования схем в таблице `_schemes`, которые могут использоваться как имена классов C#.

### Правила валидации схем:

#### 1. Правила именования классов C#
- **Начало имени**: только буква (a-z, A-Z) или символ подчеркивания (_)
- **Символы**: латинские буквы, цифры, символы подчеркивания и точки (для namespace)
- **Запрещены**: пробелы, дефисы, спецсимволы (@, #, $, % и т.д.)
- **Длина**: максимум 128 символов

#### 2. Поддержка Namespace
- **Разделитель**: точка (.) для создания иерархии namespace
- **Валидация частей**: каждая часть между точками проверяется как отдельный идентификатор
- **Ограничения**: 
  - Нельзя начинать или заканчивать точкой
  - Нельзя использовать две точки подряд (..)
  - Каждая часть должна быть валидным идентификатором

#### 3. Зарезервированные слова C#
Имена схем (включая части namespace) не могут быть зарезервированными словами C#:
```
abstract, as, base, bool, break, byte, case, catch, char, checked,
class, const, continue, decimal, default, delegate, do, double, else,
enum, event, explicit, extern, false, finally, fixed, float, for,
foreach, goto, if, implicit, in, int, interface, internal, is, lock,
long, namespace, new, null, object, operator, out, override, params,
private, protected, public, readonly, ref, return, sbyte, sealed,
short, sizeof, stackalloc, static, string, struct, switch, this,
throw, true, try, typeof, uint, ulong, unchecked, unsafe, ushort,
using, virtual, void, volatile, while
```

### Примеры использования схем:

**✅ Корректные имена схем:**
```sql
-- Простые имена классов
Employee, UserAccount, _BaseEntity, Product123, Order_Item

-- С использованием namespace
MyCompany.Domain.User, System.Collections.Generic, _Internal._Hidden._Entity
```

**❌ Некорректные имена схем:**
```sql
-- Имена с недопустимыми символами
INSERT INTO _schemes (..., _name, ...) VALUES (..., 'User-Account', ...);  -- ОШИБКА
INSERT INTO _schemes (..., _name, ...) VALUES (..., 'User Account', ...);  -- ОШИБКА

-- Проблемы с namespace
INSERT INTO _schemes (..., _name, ...) VALUES (..., '.User', ...);         -- ОШИБКА (начинается с точки)
INSERT INTO _schemes (..., _name, ...) VALUES (..., 'User.', ...);         -- ОШИБКА (заканчивается точкой)
INSERT INTO _schemes (..., _name, ...) VALUES (..., 'Domain..User', ...);  -- ОШИБКА (две точки)

-- Зарезервированные слова
INSERT INTO _schemes (..., _name, ...) VALUES (..., 'class', ...);         -- ОШИБКА
INSERT INTO _schemes (..., _name, ...) VALUES (..., 'Domain.class.User', ...); -- ОШИБКА
```

### Тестирование триггера схем:
Для проверки работы триггера выполните:
```sql
\i redb.Core.Postgres/sql/test_scheme_validation.sql
```

### Практические сценарии:

**Генерация C# классов из схем:**
```sql
-- Схема будет сгенерирована как класс Employee
INSERT INTO _schemes (_id, _name, _alias) VALUES (..., 'Employee', 'Сотрудник');

-- Схема будет сгенерирована как MyCompany.Domain.User
INSERT INTO _schemes (_id, _name, _alias) VALUES (..., 'MyCompany.Domain.User', 'Пользователь');
```

**Результат в C#:**
```csharp
// Простой класс
public class Employee { ... }

// Класс с namespace
namespace MyCompany.Domain 
{
    public class User { ... }
}
```

### Реальные примеры namespace схем

Система поддерживает сложные namespace структуры. Пример с реальными данными:

#### 1. Создание схем с namespace

```sql
-- Схемы TrueSight.Models в системе
INSERT INTO _schemes (_id, _id_parent, _name, _alias, _name_space) VALUES 
(1001, NULL, 'TrueSight.Models.AnalyticsMetrics', 'Метрики аналитики', 'TrueSight.Models'),
(1002, NULL, 'TrueSight.Models.AnalyticsRecord', 'Запись аналитики', 'TrueSight.Models');
```

#### 2. Структуры для AnalyticsMetrics

```sql
-- Поля схемы 1001 (AnalyticsMetrics)
INSERT INTO _structures (_id, _id_scheme, _id_type, _name, _alias, _order, _allow_not_null) VALUES 
(1003, 1001, -9223372036854775704, 'AdvertId', 'ID рекламной кампании', 1, true),
(1004, 1001, -9223372036854775704, 'Baskets', 'Корзины', 2, NULL),
(1005, 1001, -9223372036854775704, 'Base', 'База', 3, NULL),
(1006, 1001, -9223372036854775704, 'Association', 'Ассоц.', 4, NULL),
(1007, 1001, -9223372036854775707, 'Costs', 'Затраты', 5, NULL),
(1008, 1001, -9223372036854775704, 'Rate', 'Ставка', 6, NULL),
(1009, 1001, -9223372036854775700, 'Note', 'Примечание', 7, NULL);
```

#### 3. Структуры для AnalyticsRecord

```sql  
-- Поля схемы 1002 (AnalyticsRecord)
INSERT INTO _structures (_id, _id_scheme, _id_type, _name, _alias, _order, _allow_not_null) VALUES 
(1010, 1002, -9223372036854775708, 'Date', 'Дата', 1, true),
(1011, 1002, -9223372036854775700, 'Article', 'Артикул', 2, true),
(1012, 1002, -9223372036854775704, 'Orders', 'Заказы', 3, NULL),
(1013, 1002, -9223372036854775704, 'Stock', 'Остатки', 4, NULL),
(1014, 1002, -9223372036854775704, 'TotalCart', 'Общая корзина', 5, NULL),
(1015, 1002, -9223372036854775704, 'Price', 'Цена', 6, NULL),
(1016, 1002, -9223372036854775703, 'AutoMetrics', 'Метрики авто', 7, true),    -- Object ссылка
(1017, 1002, -9223372036854775703, 'AuctionMetrics', 'Метрики аукциона', 8, true), -- Object ссылка  
(1018, 1002, -9223372036854775700, 'Tag', 'Тег (группировка)', 9, NULL);
```

#### 4. Примеры данных

```sql
-- Создание объектов с данными
INSERT INTO _objects (_id, _id_scheme, _id_owner, _name) VALUES 
(1019, 1001, 0, 'AutoMetrics Example'),     -- AnalyticsMetrics объект
(1020, 1001, 0, 'AuctionMetrics Example'),  -- AnalyticsMetrics объект  
(1021, 1002, 0, 'AnalyticsRecord Example'); -- AnalyticsRecord объект

-- Значения для AnalyticsRecord (1021)
INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _DateTime) VALUES 
(1034, 1010, 1021, NULL, NULL, '2025-07-15T00:00:00'::TIMESTAMP), -- Date
(1035, 1011, 1021, 'пт 5х260', NULL, NULL),                       -- Article  
(1036, 1012, 1021, NULL, 0, NULL),                                -- Orders
(1037, 1013, 1021, NULL, 151, NULL),                              -- Stock
(1038, 1014, 1021, NULL, 2, NULL),                                -- TotalCart
(1039, 1016, 1021, NULL, 1019, NULL),                             -- AutoMetrics (Object ID)
(1040, 1017, 1021, NULL, 1020, NULL),                             -- AuctionMetrics (Object ID)
(1041, 1018, 1021, 'пт', NULL, NULL);                             -- Tag
```

#### 5. Получение JSON объекта с namespace

```sql
-- Извлечение полного объекта AnalyticsRecord с вложенными объектами
SELECT get_object_json(1021, 3);
```

**Результат JSON:**
```json
{
  "id": 1021,
  "name": "AnalyticsRecord Example", 
  "scheme_id": 1002,
  "scheme_name": "TrueSight.Models.AnalyticsRecord",
  "namespace": "TrueSight.Models",
  "date_create": "2024-01-15T10:30:00Z",
  "properties": {
    "Date": "2025-07-15T00:00:00Z",
    "Article": "пт 5х260",
    "Orders": 0,
    "Stock": 151, 
    "TotalCart": 2,
    "AutoMetrics": {
      "id": 1019,
      "name": "AutoMetrics Example",
      "scheme_name": "TrueSight.Models.AnalyticsMetrics", 
      "properties": {
        "AdvertId": 0,
        "Baskets": 0,
        "Base": 0,
        "Association": 0,
        "Costs": 0.0,
        "Rate": 0
      }
    },
    "AuctionMetrics": {
      "id": 1020,
      "name": "AuctionMetrics Example",
      "scheme_name": "TrueSight.Models.AnalyticsMetrics",
      "properties": {
        "AdvertId": 0,
        "Baskets": 0,
        "Base": 0,
        "Association": 0,
        "Costs": 0.0,
        "Rate": 0
      }
    },
    "Tag": "пт"
  }
}
```

#### 6. Генерация C# классов из namespace схем

```csharp
// Сгенерированные классы из схем TrueSight.Models
namespace TrueSight.Models
{
    public class AnalyticsMetrics
    {
        public long AdvertId { get; set; }        // обязательное поле
        public long? Baskets { get; set; }
        public long? Base { get; set; }
        public long? Association { get; set; }
        public double? Costs { get; set; }
        public long? Rate { get; set; }
        public string Note { get; set; }          // может быть null
    }

    public class AnalyticsRecord  
    {
        public DateTime Date { get; set; }        // обязательное поле
        public string Article { get; set; }      // обязательное поле
        public long? Orders { get; set; }
        public long? Stock { get; set; }
        public long? TotalCart { get; set; }
        public long? Price { get; set; }
        public AnalyticsMetrics AutoMetrics { get; set; }    // обязательная ссылка
        public AnalyticsMetrics AuctionMetrics { get; set; } // обязательная ссылка
        public string Tag { get; set; }
    }
}
```

#### 7. Фасетный поиск по namespace схемам

```sql
-- Получить фасеты для AnalyticsRecord
SELECT get_facets(1002);

-- Поиск записей по дате и артикулу  
SELECT search_objects_with_facets(
    1002,
    '{
        "Date": ["2025-07-15"],
        "Article": ["пт 5х260"],
        "Tag": ["пт"]
    }'::jsonb
);
```

Такая структура позволяет создавать сложные доменные модели с четкой организацией namespace и автоматической генерацией соответствующих C# классов.

## Система разрешений доступа

В REDB реализована гибкая система контроля доступа к объектам на основе пользователей и ролей с поддержкой иерархического наследования разрешений.

### Архитектура разрешений

```sql
_permissions    -- Разрешения на объекты (пользователь ИЛИ роль)
_users         -- Пользователи системы  
_roles         -- Роли пользователей
_users_roles   -- Связка пользователей с ролями
```

### Принципы работы

#### 1. Структура разрешений
```sql
-- Таблица разрешений (только пользователь ИЛИ роль)
_permissions(
    _id_user bigint NULL,      -- Прямое пользовательское разрешение
    _id_role bigint NULL,      -- Ролевое разрешение
    _id_ref bigint NOT NULL,   -- Объект, к которому применяется разрешение
    _select boolean,           -- Право чтения
    _insert boolean,           -- Право создания дочерних
    _update boolean,           -- Право редактирования
    _delete boolean            -- Право удаления
)
```

#### 2. Иерархическое наследование
- Если у объекта НЕТ прямого разрешения → ищем у родителя
- Если у родителя НЕТ → идем выше по дереву до корня
- Первое найденное разрешение применяется ко всем потомкам

#### 3. Приоритет разрешений
1. **Пользовательские разрешения** (высший приоритет) - `_id_user`
2. **Ролевые разрешения** (обычный приоритет) - `_id_role`

### VIEW для работы с разрешениями

Система предоставляет готовый VIEW `v_user_permissions` для эффективной работы с разрешениями:

```sql
-- VIEW автоматически:
-- 1. Ищет ближайшее разрешение для каждого объекта вверх по дереву
-- 2. Применяет приоритеты (пользователь > роль)
-- 3. Развертывает ролевые разрешения через users_roles
-- 4. Возвращает эффективные права для каждой пары (объект, пользователь)

SELECT * FROM v_user_permissions;
-- Результат:
-- object_id | user_id | permission_type | can_select | can_insert | can_update | can_delete
```

### Примеры использования

#### 1. Базовая проверка доступа

```sql
-- Получить все объекты доступные пользователю для чтения
SELECT o.*
FROM _objects o
JOIN v_user_permissions vup ON vup.object_id = o._id
WHERE vup.user_id = 12345 
  AND vup.can_select = true;
```

#### 2. Фильтрация по схеме с правами

```sql
-- Доступные документы конкретной схемы
SELECT 
    o._id,
    o._name,
    o._code_string,
    vup.permission_type,
    CASE 
        WHEN vup.can_update THEN 'Редактирование'
        WHEN vup.can_select THEN 'Только чтение'
        ELSE 'Нет доступа'
    END as access_level
FROM _objects o
JOIN v_user_permissions vup ON vup.object_id = o._id
WHERE vup.user_id = 12345 
  AND vup.can_select = true
  AND o._id_scheme = 100
ORDER BY o._date_modify DESC;
```

#### 3. Проверка конкретных прав

```sql
-- Объекты которые можно редактировать
SELECT o._id, o._name
FROM _objects o
JOIN v_user_permissions vup ON vup.object_id = o._id
WHERE vup.user_id = 12345 
  AND vup.can_update = true
  AND o._id_scheme = 100;

-- Объекты с правом создания дочерних
SELECT o._id, o._name
FROM _objects o
JOIN v_user_permissions vup ON vup.object_id = o._id
WHERE vup.user_id = 12345 
  AND vup.can_insert = true;
```

#### 4. Иерархический просмотр с правами

```sql
-- Дерево объектов с указанием уровня доступа
WITH RECURSIVE object_tree AS (
    -- Корневые объекты
    SELECT 
        o._id,
        o._name,
        o._id_parent,
        vup.can_select,
        vup.can_update,
        vup.permission_type,
        0 as level
    FROM _objects o
    JOIN v_user_permissions vup ON vup.object_id = o._id
    WHERE o._id_parent IS NULL
      AND vup.user_id = 12345
      AND vup.can_select = true
      AND o._id_scheme = 100
    
    UNION ALL
    
    -- Дочерние объекты
    SELECT 
        o._id,
        o._name,
        o._id_parent,
        vup.can_select,
        vup.can_update,
        vup.permission_type,
        ot.level + 1
    FROM _objects o
    JOIN object_tree ot ON o._id_parent = ot._id
    JOIN v_user_permissions vup ON vup.object_id = o._id
    WHERE vup.user_id = 12345
      AND vup.can_select = true
)
SELECT 
    LPAD('', level * 2, ' ') || _name as tree_name,
    CASE 
        WHEN can_update THEN '✏️ Редактирование'
        ELSE '👁️ Чтение' 
    END as access,
    permission_type
FROM object_tree 
ORDER BY level, _name;
```

#### 5. Статистика разрешений

```sql
-- Сколько объектов доступно пользователю по схемам
SELECT 
    s._name as scheme_name,
    COUNT(*) as total_accessible,
    COUNT(*) FILTER (WHERE vup.can_select) as can_read,
    COUNT(*) FILTER (WHERE vup.can_insert) as can_create_children,
    COUNT(*) FILTER (WHERE vup.can_update) as can_edit,
    COUNT(*) FILTER (WHERE vup.can_delete) as can_delete,
    COUNT(*) FILTER (WHERE vup.permission_type = 'user') as personal_rights,
    COUNT(*) FILTER (WHERE vup.permission_type = 'role') as role_rights
FROM _objects o
JOIN v_user_permissions vup ON vup.object_id = o._id
JOIN _schemes s ON s._id = o._id_scheme
WHERE vup.user_id = 12345
  AND vup.can_select = true
GROUP BY s._id, s._name
ORDER BY total_accessible DESC;
```

#### 6. Проверка прав для конкретного объекта

Для случаев когда нужно проверить права конкретного пользователя на конкретный объект, используйте функцию `get_user_permissions_for_object`:

```sql
-- Проверить какие права у пользователя на конкретный объект
SELECT * FROM get_user_permissions_for_object(1001, 12345);

-- Результат:
-- object_id | user_id | permission_source_id | permission_type | can_select | can_insert | can_update | can_delete
-- 1001      | 12345   | 1000                | role           | true       | false      | true       | false

-- Быстрая проверка - может ли пользователь читать объект
SELECT EXISTS(
    SELECT 1 FROM get_user_permissions_for_object(1001, 12345) 
    WHERE can_select = true
) as can_read;

-- Быстрая проверка - может ли пользователь редактировать объект  
SELECT EXISTS(
    SELECT 1 FROM get_user_permissions_for_object(1001, 12345)
    WHERE can_update = true  
) as can_edit;

-- Получить источник разрешения (откуда наследуется)
SELECT 
    CASE 
        WHEN permission_type = 'user' THEN 'Личное разрешение'
        ELSE 'Через роль: ' || r._name
    END as permission_source,
    CASE 
        WHEN permission_source_id = object_id THEN 'Прямое'
        ELSE 'Наследуется от объекта ' || permission_source_id
    END as inheritance_type
FROM get_user_permissions_for_object(1001, 12345) p
LEFT JOIN _roles r ON r._id = p._id_role;
```

#### 7. Массовая проверка прав

```sql
-- Проверить права пользователя на несколько объектов за раз
WITH target_objects AS (
    SELECT unnest(ARRAY[1001, 1002, 1003, 1004]) as object_id
)
SELECT 
    to.object_id,
    o._name,
    COALESCE(p.can_select, false) as can_read,
    COALESCE(p.can_update, false) as can_edit,
    COALESCE(p.permission_type, 'no_access') as access_type
FROM target_objects to
JOIN _objects o ON o._id = to.object_id
LEFT JOIN get_user_permissions_for_object(to.object_id, 12345) p ON true
ORDER BY to.object_id;
```

### Настройка разрешений

#### 1. Создание ролевых разрешений

```sql
-- Дать роли "Manager" права на чтение и создание в корневой папке проектов
INSERT INTO _permissions (_id, _id_role, _id_ref, _select, _insert, _update, _delete)
VALUES (
    nextval('global_identity'),
    (SELECT _id FROM _roles WHERE _name = 'Manager'),
    1001,  -- ID корневой папки проектов
    true,  -- чтение
    true,  -- создание дочерних
    false, -- редактирование
    false  -- удаление
);
```

#### 2. Персональные исключения

```sql
-- Дать конкретному пользователю дополнительные права на секретный проект
INSERT INTO _permissions (_id, _id_user, _id_ref, _select, _insert, _update, _delete)
VALUES (
    nextval('global_identity'),
    12345,  -- ID пользователя John
    2001,   -- ID секретного проекта  
    true,   -- чтение (переопределяет ролевой запрет)
    false,  -- создание
    true,   -- редактирование (дополнительное право)
    false   -- удаление
);
```

#### 3. Запрет доступа к ветке

```sql
-- Запретить роли "Employee" доступ к финансовым документам
INSERT INTO _permissions (_id, _id_role, _id_ref, _select, _insert, _update, _delete)
VALUES (
    nextval('global_identity'),
    (SELECT _id FROM _roles WHERE _name = 'Employee'),
    3001,  -- ID папки "Финансы"
    false, -- запрет чтения (распространяется на всех потомков)
    false,
    false,
    false
);
```

### Интеграция с EF Core

```csharp
// Модель для VIEW
public class VUserPermission
{
    public long ObjectId { get; set; }
    public long UserId { get; set; }
    public long PermissionId { get; set; }
    public string PermissionType { get; set; }
    public long? IdRole { get; set; }
    public bool CanSelect { get; set; }
    public bool CanInsert { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}

// Использование в сервисе
public async Task<List<RObject>> GetAccessibleObjects(long userId, long schemeId)
{
    return await _context.Objects
        .Join(_context.VUserPermissions.Where(p => p.UserId == userId && p.CanSelect),
              o => o.Id,
              p => p.ObjectId,
              (o, p) => o)
        .Where(o => o.IdScheme == schemeId)
        .ToListAsync();
}

// Проверка прав на редактирование
public async Task<bool> CanUserEditObject(long userId, long objectId)
{
    return await _context.VUserPermissions
        .AnyAsync(p => p.UserId == userId && p.ObjectId == objectId && p.CanUpdate);
}

// Получение детальной информации о правах на конкретный объект
public async Task<UserPermissionDetail> GetUserPermissionDetails(long userId, long objectId)
{
    var result = await _context.Database.SqlQueryRaw<UserPermissionResult>(
        "SELECT * FROM get_user_permissions_for_object({0}, {1})", 
        objectId, userId
    ).FirstOrDefaultAsync();
    
    if (result == null)
        return new UserPermissionDetail { HasAccess = false };
        
    return new UserPermissionDetail
    {
        HasAccess = true,
        CanSelect = result.CanSelect,
        CanInsert = result.CanInsert, 
        CanUpdate = result.CanUpdate,
        CanDelete = result.CanDelete,
        PermissionType = result.PermissionType,
        PermissionSourceId = result.PermissionSourceId,
        IsInherited = result.PermissionSourceId != objectId
    };
}

// Модели для результата функции
public class UserPermissionResult
{
    public long ObjectId { get; set; }
    public long UserId { get; set; }
    public long PermissionSourceId { get; set; }
    public string PermissionType { get; set; }
    public long? IdRole { get; set; }
    public bool CanSelect { get; set; }
    public bool CanInsert { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}

public class UserPermissionDetail
{
    public bool HasAccess { get; set; }
    public bool CanSelect { get; set; }
    public bool CanInsert { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public string PermissionType { get; set; }
    public long PermissionSourceId { get; set; }
    public bool IsInherited { get; set; }
}
```

### Автоматическая оптимизация performance permissions

Система REDB включает **триггер автоматического создания permissions** для узловых объектов, который радикально ускоряет поиск разрешений в глубоких иерархиях.

#### Принцип работы триггера `auto_create_node_permissions()`

```sql
-- Триггер срабатывает при INSERT в _objects
-- Если новый объект имеет _id_parent, проверяется:
-- 1. Есть ли у родителя permission? → Если ДА - ничего не делаем
-- 2. Если НЕТ - ищем ближайший permission вверх по дереву
-- 3. Создаем для родителя копию найденного permission
```

#### 🚀 Радикальное ускорение поиска

| Сценарий | Без триггера | С триггером | Ускорение |
|----------|--------------|-------------|-----------|
| Дерево 3 уровня | 3 итерации CTE | max 2 итерации | **2x** |
| Дерево 10 уровней | 10 итераций CTE | max 2 итерации | **5x** |
| Дерево 50 уровней | 50 итераций CTE | max 2 итерации | **25x** |

```sql
-- Результат: практически ВСЕГДА максимум 2 итерации
WITH RECURSIVE permission_search AS (
    -- Итерация 1: проверяем сам объект
    SELECT object_id, current_id, level=0
    
    UNION ALL
    
    -- Итерация 2: проверяем родителя (у него ГАРАНТИРОВАННО есть permission)
    SELECT object_id, parent_id, level=1
    -- СТОП! Дальше не идем - permission найден
)
```

#### Автоматическое создание permissions

```sql
-- Пример работы триггера:
-- 1. Создаем корневую папку с permission
INSERT INTO _permissions (_id, _id_role, _id_ref, _select, _insert, _update, _delete)
VALUES (1, (SELECT _id FROM _roles WHERE _name = 'Manager'), 1000, true, true, false, false);

-- 2. Создаем дочерний объект
INSERT INTO _objects (_id, _id_parent, _id_scheme, _name)
VALUES (1001, 1000, 100, 'Подпапка');

-- 3. Триггер автоматически создает для объекта 1000 permission (если его не было)
-- 4. Теперь поиск permission для любого потомка = максимум 2 итерации!
```

#### VIEW оптимизация

С триггером VIEW `v_user_permissions` работает **в разы быстрее**:

```sql
-- Практически мгновенный поиск permissions для любой глубины дерева
SELECT * FROM v_user_permissions 
WHERE user_id = 12345 AND object_id IN (очень_глубокие_объекты);

-- Результат: O(1-2) вместо O(глубина_дерева)
```

### Практические сценарии

#### Корпоративная система документооборота
```sql
-- Настройка прав:
-- 1. Все сотрудники могут читать общие документы
INSERT INTO _permissions VALUES (role:'Employee', object:1000, select:true, insert:false, update:false, delete:false);

-- 2. Менеджеры могут создавать документы в своих отделах  
INSERT INTO _permissions VALUES (role:'Manager', object:2000, select:true, insert:true, update:true, delete:false);

-- 3. Директор имеет полные права
INSERT INTO _permissions VALUES (user:director_id, object:1, select:true, insert:true, update:true, delete:true);

-- 4. Секретные документы только для топ-менеджмента
INSERT INTO _permissions VALUES (role:'TopManager', object:9000, select:true, insert:true, update:true, delete:false);
INSERT INTO _permissions VALUES (role:'Employee', object:9000, select:false, insert:false, update:false, delete:false);
```

### Преимущества системы

✅ **Гибкость** - легко настраивать права на любом уровне иерархии  
✅ **Производительность** - VIEW оптимизирован для быстрых JOIN'ов  
✅ **Наследование** - автоматическое применение прав к потомкам  
✅ **Приоритеты** - персональные права важнее ролевых  
✅ **Безопасность** - явный запрет нельзя обойти наследованием  
✅ **EF Core совместимость** - стандартные LINQ запросы  

Эта система обеспечивает мощный и гибкий контроль доступа для любых корпоративных приложений! 🔐

## Рекомендации по оптимизации

### 1. Денормализация для критичных запросов
```sql
-- Материализованные представления для часто используемых данных
CREATE MATERIALIZED VIEW mv_employee_summary AS
SELECT 
    o._id,
    o._name,
    MAX(CASE WHEN s._name = 'first_name' THEN v._String END) as first_name,
    MAX(CASE WHEN s._name = 'last_name' THEN v._String END) as last_name,
    MAX(CASE WHEN s._name = 'birth_date' THEN v._DateTime END) as birth_date
FROM _objects o
JOIN _values v ON v._id_object = o._id
JOIN _structures s ON s._id = v._id_structure
JOIN _schemes sc ON sc._id = o._id_scheme
WHERE sc._name = 'Employee'
GROUP BY o._id, o._name;
```

### 2. Партиционирование больших таблиц
```sql
-- Партиционирование _values по схемам
CREATE TABLE _values_partition_scheme_1 
PARTITION OF _values 
FOR VALUES IN (SELECT _id FROM _schemes WHERE _name = 'Employee');
```

### 3. Кэширование метаданных
- Кэширование структур схем в приложении
- Использование Redis для часто запрашиваемых конфигураций
- Построение индексов для специфичных запросов

## Заключение

REDB представляет собой продвинутую реализацию EAV паттерна, обеспечивающую баланс между гибкостью и производительностью. Система особенно эффективна в сценариях, требующих частых изменений структуры данных и создания универсальных пользовательских интерфейсов.

Ключевые принципы успешного использования:
1. **Тщательное планирование схем** - продумывание структуры заранее
2. **Оптимизация критичных запросов** - создание индексов и представлений  
3. **Использование метаданных** - максимальное извлечение пользы из гибкости
4. **Мониторинг производительности** - контроль сложных запросов
5. **Документирование схем** - поддержание понятности системы

Эта архитектура позволяет создавать масштабируемые, гибкие приложения с минимальными затратами на изменение структуры данных и максимальной переиспользуемостью компонентов.

### Современные расширения REDB

Система включает передовые возможности:

#### 🚀 **Автоматическая оптимизация performance**
- **Триггер `auto_create_node_permissions()`** обеспечивает радикальное ускорение поиска разрешений (до 25x для глубоких иерархий)
- **VIEW `v_user_permissions`** работает с константной скоростью O(1-2) вместо O(глубина)
- **Иерархическое наследование** permissions с приоритетами пользователь > роль

#### 🏗️ **Полноценные namespace схемы**
- **Поддержка C# namespace** в именах схем (`TrueSight.Models.AnalyticsRecord`)
- **Автоматическая генерация** соответствующих C# классов из метаданных
- **Валидация имен** полей и схем согласно правилам C#
- **Рекурсивное извлечение** связанных объектов в JSON

#### 📊 **Расширенные возможности EAV**
- **Типизированные массивы** в поле `_Array` (примитивы и объекты)
- **Фасетный поиск** с поддержкой всех типов данных  
- **Система разрешений** с VIEW для эффективных JOIN'ов в EF Core
- **Полная интеграция** с Entity Framework Core

## Реализованные функции

Система REDB включает следующие готовые функции PostgreSQL:

### 🔍 Поиск и фильтрация
- `get_facets(scheme_id)` - построение фасетов для UI фильтров
- `search_objects_with_facets(scheme_id, filters, limit, offset)` - фасетный поиск с пагинацией, возвращает полные объекты через get_object_json
- `search_objects(search_text)` - полнотекстовый поиск (из примеров README)
- `get_user_permissions_for_object(object_id, user_id)` - получение эффективных разрешений пользователя для конкретного объекта

### �� Извлечение данных
- `get_object_json(object_id, max_depth)` - объект в JSON с базовыми полями в корне
- `validate_structure_name()` - триггер валидации имен полей
- `validate_scheme_name()` - триггер валидации имен схем

### 🗂️ Система аудита
- `ftr__objects__deleted_objects()` - триггер сохранения удаленных объектов
- `auto_create_node_permissions()` - триггер автоматического создания permissions для узловых объектов (радикальная оптимизация поиска разрешений)

### 🎯 Практическое применение:

```sql
-- Получить фасеты для построения фильтров
SELECT get_facets(100);

-- Найти сотрудников IT отдела в Москве
SELECT search_objects_with_facets(
    100, 
    '{"department": ["IT"], "city": ["Москва"]}'::jsonb
);

-- Получить полную информацию об объекте с массивами
SELECT get_object_json(1001, 3);

-- Пример создания объекта с массивом навыков (Object[] = массив ID)
INSERT INTO _values (_id, _id_structure, _id_object, _Array) 
VALUES (
    nextval('global_identity'), 
    [id_структуры_skills], 
    1001, 
    '[101, 102, 103]'::text  -- ID объектов-навыков
);

-- Пример массива примитивов (теги)
INSERT INTO _values (_id, _id_structure, _id_object, _Array) 
VALUES (
    nextval('global_identity'), 
    [id_структуры_tags], 
    1001, 
    '["backend", "database", "api"]'::text
);

-- 🔍 **Реальный пример: TrueSight.Models Analytics**

-- Получить полный объект AnalyticsRecord с вложенными метриками
SELECT get_object_json(1021, 3);
-- Результат: объект с Date="2025-07-15", Article="пт 5х260", Stock=151
-- + вложенные AutoMetrics и AuctionMetrics объекты

-- Поиск аналитики по артикулу и дате
SELECT search_objects_with_facets(
    1002,  -- схема TrueSight.Models.AnalyticsRecord
    '{
        "Article": ["пт 5х260"],
        "Date": ["2025-07-15T00:00:00"],
        "Tag": ["пт"]
    }'::jsonb,
    10, 0
);

-- Получить фасеты для построения UI фильтров аналитики
SELECT get_facets(1002);
-- Результат: уникальные значения Article, Stock, Tag + ссылки на метрики

-- Проверка прав доступа к аналитическим данным
SELECT * FROM get_user_permissions_for_object(1021, 12345);

-- Быстрая проверка возможности редактирования документа
SELECT 
    CASE 
        WHEN EXISTS(SELECT 1 FROM get_user_permissions_for_object(2001, 12345) WHERE can_update = true)
        THEN 'Можно редактировать'
        ELSE 'Только чтение'
    END as edit_access;

-- Проверка наследования разрешений
SELECT 
    object_id,
    permission_source_id,
    CASE 
        WHEN object_id = permission_source_id THEN 'Прямое разрешение'
        ELSE 'Наследуется от объекта ' || permission_source_id
    END as inheritance_info,
    permission_type
FROM get_user_permissions_for_object(3001, 12345);

-- 📊 **Анализ связанных объектов:**

-- Найти все AnalyticsRecord с определенными AutoMetrics
WITH auto_metrics_filter AS (
    SELECT _id_object 
    FROM _values v
    JOIN _structures s ON s._id = v._id_structure
    WHERE s._name = 'AutoMetrics' 
      AND v._Long = 1019  -- ID конкретного AutoMetrics объекта
)
SELECT o.*, get_object_json(o._id, 2) as full_data
FROM _objects o
JOIN auto_metrics_filter amf ON amf._id_object = o._id
WHERE o._id_scheme = 1002;

-- Агрегированная статистика по остаткам (Stock)
SELECT 
    COUNT(*) as total_records,
    AVG(v._Long) as avg_stock,
    MIN(v._Long) as min_stock,
    MAX(v._Long) as max_stock
FROM _values v
JOIN _structures s ON s._id = v._id_structure  
JOIN _objects o ON o._id = v._id_object
WHERE s._name = 'Stock' 
  AND o._id_scheme = 1002
  AND v._Long IS NOT NULL;
```

Все функции оптимизированы для работы с большими объемами данных и поддерживают все типы EAV системы. 