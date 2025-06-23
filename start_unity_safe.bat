@echo off
echo Starting Unity with safe project settings...
cd /d "c:\Users\crack.crackdesk\test"

echo Cleaning temp files...
del /q Temp\* 2>nul
rmdir /s /q Temp 2>nul

echo Starting Unity...
"C:\Program Files\Unity\Hub\Editor\6000.0.46f1\Editor\Unity.exe" -projectPath "c:\Users\crack.crackdesk\test" -logFile "unity_log.txt"

echo Unity closed. Check unity_log.txt for any issues.
pause
