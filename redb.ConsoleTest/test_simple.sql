SELECT 'TEST 1: Count all objects' as test;
SELECT (search_objects_with_facets(1000002, '{}', 5, 0, null, true))->>'total_count';

SELECT 'TEST 2: Stock gt 100' as test;  
SELECT (search_objects_with_facets(1000002, '{"Stock": {"$gt": "100"}}', 5, 0, null, true))->>'total_count';

SELECT 'TEST 3: TestValue eq 2' as test;
SELECT (search_objects_with_facets(1000002, '{"TestValue": {"$eq": "2"}}', 5, 0, null, true))->>'total_count';
