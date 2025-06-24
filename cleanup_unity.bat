@echo off
echo Unity Project Cleanup Script
echo.

echo Deleting Library folder...
if exist "c:\Users\crack.crackdesk\test\Library" (
    rmdir /s /q "c:\Users\crack.crackdesk\test\Library"
    echo Library folder deleted.
) else (
    echo Library folder not found.
)

echo.
echo Deleting Temp folder...
if exist "c:\Users\crack.crackdesk\test\Temp" (
    rmdir /s /q "c:\Users\crack.crackdesk\test\Temp"
    echo Temp folder deleted.
) else (
    echo Temp folder not found.
)

echo.
echo Cleanup complete. You can now start Unity again.
echo Unity will recreate these folders automatically.
pause
