-- ТЕСТ: есть ли Tags1 и Scores1 в схеме MixedTestProps
SELECT 'Структуры массивов в MixedTestProps:' as test_name;
SELECT s._name, s._is_array, t._name as type_name, t._db_type
FROM _structures s 
JOIN _types t ON t._id = s._id_type
JOIN _schemes sc ON sc._id = s._id_scheme 
WHERE sc._name = 'MixedTestProps' 
  AND s._name IN ('Tags1', 'Scores1', 'Tags2', 'Scores2')
ORDER BY s._name;

-- ТЕСТ: есть ли данные в массивах
SELECT 'Данные в массивах:' as test_name;
SELECT o._name, s._name as field, v._array_index, v._String, v._Long
FROM _values v 
JOIN _structures s ON s._id = v._id_structure 
JOIN _objects o ON o._id = v._id_object 
JOIN _schemes sc ON sc._id = o._id_scheme
WHERE sc._name = 'MixedTestProps'
  AND s._name IN ('Tags1', 'Scores1')
  AND v._array_index IS NOT NULL
ORDER BY o._name, s._name, v._array_index;
