-- Добавляем права admin пользователю на все операции со всеми объектами
INSERT INTO _permissions (_id, _id_user, _id_ref, _select, _insert, _update, _delete)
VALUES (nextval('global_identity'), -9223372036854775800, 0, true, true, true, true);
