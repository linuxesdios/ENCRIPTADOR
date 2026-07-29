@echo off
setlocal

rem =========================================================================
rem  compilar_windows.bat
rem  1) Compila el proyecto WPF (C#/.NET) "Encriptador" y genera un .exe
rem     autocontenido (no requiere tener .NET instalado en la PC destino)
rem     dentro de la carpeta "compilado".
rem  2) Si Inno Setup esta instalado, genera ademas el instalador
rem     (Encriptador_Setup.exe) dentro de "salida_instalador".
rem =========================================================================

set "PROYECTO=%~dp0Encriptador\Encriptador.csproj"
set "DESTINO=%~dp0compilado"
set "SCRIPT_ISS=%~dp0instalador.iss"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo No se encontro el SDK de .NET en el PATH.
    echo Instalalo desde https://dotnet.microsoft.com/download
    exit /b 1
)

echo Compilando "%PROYECTO%" ...
dotnet publish "%PROYECTO%" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%DESTINO%"

if errorlevel 1 (
    echo.
    echo La compilacion fallo.
    exit /b 1
)

echo.
echo Listo. El ejecutable esta en "%DESTINO%\Encriptador.exe"

set "ISCC="
if exist "%ProgramFiles%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"

if not defined ISCC (
    echo.
    echo [Aviso] No se encontro Inno Setup instalado: se omite la generacion del instalador.
    echo Descargalo de https://jrsoftware.org/isdl.php si queres generar Encriptador_Setup.exe
    goto :fin
)

echo.
echo Generando instalador con Inno Setup...
"%ISCC%" "%SCRIPT_ISS%"

if errorlevel 1 (
    echo.
    echo La generacion del instalador fallo.
    exit /b 1
)

echo.
echo Listo. El instalador esta en "%~dp0salida_instalador\Encriptador_Setup.exe"

:fin
endlocal
