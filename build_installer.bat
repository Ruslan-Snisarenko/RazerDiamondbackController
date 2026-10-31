@echo off
setlocal
cd /d "%~dp0"
echo ========================================================
echo   Building Razer Diamondback Installer (Inno Setup)
echo ========================================================

set "ISCC=C:\Users\rusua\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"

echo Compiling application first...
call "%~dp0build.bat"

echo Compiling installer...
"%ISCC%" "%~dp0installer.iss"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] RazerDiamondbackSetup.exe created successfully!
) else (
    echo.
    echo [ERROR] Installer build failed.
)
