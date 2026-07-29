@echo off
setlocal

rem =========================================================================
rem  compilar_linux.bat
rem  Compila la version Avalonia UI de Encriptador para Linux (self-contained:
rem  no requiere tener .NET instalado en la maquina destino) en:
rem      compilado_linux\Encriptador  (instalar.sh y desinstalar.sh ya viven
rem                                    ahi mismo, dotnet publish no los toca)
rem =========================================================================

set "PROYECTO=%~dp0Encriptador\Encriptador.csproj"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo No se encontro el SDK de .NET en el PATH.
    echo Instalalo desde https://dotnet.microsoft.com/download
    exit /b 1
)

echo ============================================
echo  Compilando para Linux (linux-x64)...
echo ============================================
dotnet publish "%PROYECTO%" -c Release -r linux-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%~dp0compilado_linux"

if errorlevel 1 (
    echo.
    echo La compilacion para Linux fallo.
    exit /b 1
)

echo.
echo Copiando icono (instalar.sh y desinstalar.sh ya viven en compilado_linux)...
copy /Y "%~dp0Encriptador\Assets\icono.png" "%~dp0compilado_linux\icono.png" >nul

echo.
echo ============================================
echo  Listo: %~dp0compilado_linux\
echo  Copia esa carpeta entera a tu maquina Linux y ahi corre:
echo    chmod +x Encriptador instalar.sh desinstalar.sh
echo    ./instalar.sh
echo ============================================
endlocal
