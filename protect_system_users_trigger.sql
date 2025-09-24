-- 🔐 ТРИГГЕР ДЛЯ ЗАЩИТЫ СИСТЕМНЫХ ПОЛЬЗОВАТЕЛЕЙ

-- Запрещает удаление и переименование пользователей с ID 0 (sys) и 1
-- Остальные операции (смена пароля, email, телефона, статуса) разрешены

-- Функция триггера
CREATE OR REPLACE FUNCTION protect_system_users()
RETURNS TRIGGER AS $$
BEGIN
    -- Проверяем операцию DELETE
    IF TG_OP = 'DELETE' THEN
        -- Запрещаем удаление системных пользователей
        IF OLD._id IN (0, 1) THEN
            RAISE EXCEPTION 'Нельзя удалить системного пользователя с ID %', OLD._id;
        END IF;
        RETURN OLD;
    END IF;
    
    -- Проверяем операцию UPDATE
    IF TG_OP = 'UPDATE' THEN
        -- Запрещаем изменение ID (на всякий случай)
        IF OLD._id != NEW._id THEN
            RAISE EXCEPTION 'Нельзя изменить ID пользователя';
        END IF;
        
        -- Для системных пользователей запрещаем изменение логина и имени
        IF OLD._id IN (0, 1) THEN
            -- Запрещаем изменение логина
            IF OLD._login != NEW._login THEN
                RAISE EXCEPTION 'Нельзя изменить логин системного пользователя с ID %', OLD._id;
            END IF;
            
            -- Запрещаем изменение имени
            IF OLD._name != NEW._name THEN
                RAISE EXCEPTION 'Нельзя изменить имя системного пользователя с ID %', OLD._id;
            END IF;
        END IF;
        
        RETURN NEW;
    END IF;
    
    -- Для INSERT ничего не проверяем
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Создание триггера на таблице _users
DROP TRIGGER IF EXISTS tr_protect_system_users ON _users;
CREATE TRIGGER tr_protect_system_users
    BEFORE UPDATE OR DELETE ON _users
    FOR EACH ROW
    EXECUTE FUNCTION protect_system_users();

-- Комментарии к триггеру
COMMENT ON FUNCTION protect_system_users() IS 'Защищает системных пользователей (ID 0, 1) от удаления и переименования';
COMMENT ON TRIGGER tr_protect_system_users ON _users IS 'Триггер защиты системных пользователей от удаления и переименования логина/имени';

-- Тестовые запросы для проверки триггера:
/*
-- ✅ Это должно работать (смена пароля sys пользователя):
UPDATE _users SET _password = 'new_hash' WHERE _id = 0;

-- ✅ Это должно работать (смена email sys пользователя):
UPDATE _users SET _email = 'sys@newdomain.com' WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (смена логина sys пользователя):
UPDATE _users SET _login = 'system' WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (смена имени sys пользователя):
UPDATE _users SET _name = 'System Administrator' WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (удаление sys пользователя):
DELETE FROM _users WHERE _id = 0;

-- ❌ Это должно вызвать ошибку (удаление пользователя с ID 1):
DELETE FROM _users WHERE _id = 1;

-- ✅ Это должно работать (обычный пользователь):
UPDATE _users SET _login = 'new_login', _name = 'New Name' WHERE _id = 100;
DELETE FROM _users WHERE _id = 100;
*/
