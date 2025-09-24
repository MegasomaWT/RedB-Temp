@echo off
echo Running Stage17 test...
dotnet run --project . -- --stages 17 > test_result.txt 2>&1
echo Test completed. Check test_result.txt for results.
findstr /C:"исправленным кодом" /C:"Price == 2000" /C:"объектов" test_result.txt
