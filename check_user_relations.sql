-- SQL запрос для проверки связей пользователя с объектами
-- Заменить 1080010 на нужный ID пользователя

WITH user_info AS (
    SELECT 1080010 as user_id  -- Замените на нужный ID пользователя
)

-- 1. Информация о пользователе
SELECT 'USER INFO' as section, 
       u._id as id, 
       u._login as login, 
       u._name as name,
       u._enabled as enabled,
       u._date_dismiss as date_dismiss
FROM _r_user u, user_info ui 
WHERE u._id = ui.user_id

UNION ALL

-- 2. Объекты где пользователь владелец (owner_id)
SELECT 'OWNED OBJECTS' as section,
       o._id::text as id,
       o._name as info,
       o._scheme_id::text as details,
       'owner' as type,
       o._date_create::text as date_info
FROM _r_object o, user_info ui
WHERE o._owner_id = ui.user_id

UNION ALL

-- 3. Объекты где пользователь последний изменявший (who_change_id)  
SELECT 'MODIFIED OBJECTS' as section,
       o._id::text as id,
       o._name as info,
       o._scheme_id::text as details,
       'modifier' as type,
       o._date_modify::text as date_info
FROM _r_object o, user_info ui
WHERE o._who_change_id = ui.user_id

UNION ALL

-- 4. Разрешения пользователя
SELECT 'USER PERMISSIONS' as section,
       p._id::text as id,
       CONCAT('Object: ', p._id_object) as info,
       CONCAT('Actions: ', p._can_select::text, p._can_insert::text, p._can_update::text, p._can_delete::text) as details,
       'permission' as type,
       '' as date_info
FROM _r_permission p, user_info ui
WHERE p._id_user = ui.user_id

UNION ALL

-- 5. Роли пользователя
SELECT 'USER ROLES' as section,
       ur._id_role::text as id,
       r._name as info,
       r._alias as details,
       'role' as type,
       '' as date_info  
FROM _r_users_role ur 
JOIN _r_role r ON ur._id_role = r._id, user_info ui
WHERE ur._id_user = ui.user_id

UNION ALL

-- 6. Подсчет связей
SELECT 'SUMMARY' as section,
       'Total Objects (owner)' as id,
       COUNT(*)::text as info,
       '' as details,
       'count' as type,
       '' as date_info
FROM _r_object o, user_info ui  
WHERE o._owner_id = ui.user_id

UNION ALL

SELECT 'SUMMARY' as section,
       'Total Objects (modifier)' as id, 
       COUNT(*)::text as info,
       '' as details,
       'count' as type,
       '' as date_info
FROM _r_object o, user_info ui
WHERE o._who_change_id = ui.user_id

UNION ALL

SELECT 'SUMMARY' as section,
       'Total Permissions' as id,
       COUNT(*)::text as info, 
       '' as details,
       'count' as type,
       '' as date_info
FROM _r_permission p, user_info ui
WHERE p._id_user = ui.user_id

UNION ALL

SELECT 'SUMMARY' as section,
       'Total Roles' as id,
       COUNT(*)::text as info,
       '' as details, 
       'count' as type,
       '' as date_info
FROM _r_users_role ur, user_info ui
WHERE ur._id_user = ui.user_id

ORDER BY section, type, id;


-- Альтернативный упрощенный запрос:
-- SELECT 
--     'Objects as Owner' as relation_type,
--     COUNT(*) as count
-- FROM _r_object 
-- WHERE _owner_id = 1080010  -- ID пользователя
-- 
-- UNION ALL
-- 
-- SELECT 
--     'Objects as Modifier' as relation_type,
--     COUNT(*) as count  
-- FROM _r_object
-- WHERE _who_change_id = 1080010  -- ID пользователя
--
-- UNION ALL
--
-- SELECT
--     'User Permissions' as relation_type,
--     COUNT(*) as count
-- FROM _r_permission
-- WHERE _id_user = 1080010  -- ID пользователя
--
-- UNION ALL  
--
-- SELECT
--     'User Roles' as relation_type,
--     COUNT(*) as count
-- FROM _r_users_role
-- WHERE _id_user = 1080010;  -- ID пользователя
