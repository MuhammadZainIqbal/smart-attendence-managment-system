@echo off
REM ========================================
REM Database Reset Script for AMS
REM ========================================
REM This script completely resets the database to a fresh state

echo ========================================
echo   Database Reset Script
echo ========================================
echo.

echo Step 1: Stopping LocalDB instance...
sqllocaldb stop mssqllocaldb
timeout /t 2 /nobreak >nul
echo LocalDB stopped.
echo.

echo Step 2: Deleting LocalDB instance...
sqllocaldb delete mssqllocaldb
timeout /t 1 /nobreak >nul
echo LocalDB instance deleted.
echo.

echo Step 3: Creating fresh LocalDB instance...
sqllocaldb create mssqllocaldb
timeout /t 1 /nobreak >nul
echo Fresh LocalDB instance created.
echo.

echo Step 4: Starting LocalDB instance...
sqllocaldb start mssqllocaldb
timeout /t 2 /nobreak >nul
echo LocalDB started.
echo.

echo Step 5: Cleaning up orphaned database files...
del "%USERPROFILE%\AttendenceManagementSystem.mdf" /F /Q 2>nul
del "%USERPROFILE%\AttendenceManagementSystem_log.ldf" /F /Q 2>nul
echo Database files cleaned.
echo.

echo Step 6: Deleting migration files...
if exist ".\Migrations\" (
    del ".\Migrations\*.cs" /F /Q 2>nul
    echo Migration files deleted.
) else (
    echo No migration files found.
)
echo.

echo ========================================
echo   Database Reset Complete!
echo ========================================
echo.
echo Next Steps (Run in Package Manager Console):
echo   1. Add-Migration FreshStart
echo   2. Update-Database
echo.
echo Then start your application and test signup.
echo.
pause
