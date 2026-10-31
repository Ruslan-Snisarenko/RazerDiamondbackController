@echo off
setlocal
cd /d "%~dp0"
echo ========================================================
echo   Compiling Razer Diamondback Controller (Modern WPF)
echo ========================================================

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

set "DOTNET_DIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

echo Compiling RazerDiamondbackController.exe...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:app.ico /out:RazerDiamondbackController.exe /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:"%DOTNET_DIR%\System.Xaml.dll" /r:"%DOTNET_DIR%\WPF\WindowsBase.dll" /r:"%DOTNET_DIR%\WPF\PresentationCore.dll" /r:"%DOTNET_DIR%\WPF\PresentationFramework.dll" Program.cs MainWindow.cs RazerHardware.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] Built RazerDiamondbackController.exe successfully!
) else (
    echo [ERROR] Compilation failed.
)
