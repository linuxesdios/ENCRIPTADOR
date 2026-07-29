@echo off
setlocal

rem =========================================================================
rem  compilar_android.bat
rem  Compila la app Android (Kotlin, independiente del resto del proyecto)
rem  y deja el APK listo para instalar en:
rem      compilado_android\Encriptador.apk
rem
rem  Para instalarlo en tu telefono:
rem    1) Activa "Opciones de desarrollador" -> "Depuracion USB" en el telefono
rem       (Ajustes -> Acerca del telefono -> tocar 7 veces "Numero de compilacion")
rem    2) Conectalo por USB y aceptá el permiso de depuracion que aparece en pantalla
rem    3) Corre: adb install -r compilado_android\Encriptador.apk
rem  O simplemente copia el .apk al telefono y abrilo (hay que permitir
rem  "instalar apps de origenes desconocidos" la primera vez).
rem =========================================================================

set "PROYECTO=%~dp0"
set "JAVA_HOME=C:\Program Files\Android\Android Studio\jbr"
set "PATH=%JAVA_HOME%\bin;%PATH%"

if not exist "%JAVA_HOME%\bin\java.exe" (
    echo No se encontro el JDK de Android Studio en:
    echo   %JAVA_HOME%
    echo Instala Android Studio, o edita esta linea con la ruta de tu JDK.
    exit /b 1
)

echo ============================================
echo  Compilando APK de Android (debug)...
echo ============================================
cd /d "%PROYECTO%"
call "%PROYECTO%gradlew.bat" assembleDebug

if errorlevel 1 (
    echo.
    echo La compilacion fallo.
    exit /b 1
)

if not exist "%~dp0compilado_android" mkdir "%~dp0compilado_android"
copy /Y "%PROYECTO%app\build\outputs\apk\debug\app-debug.apk" "%~dp0compilado_android\Encriptador.apk" >nul

echo.
echo ============================================
echo  Listo: %~dp0compilado_android\Encriptador.apk
echo  Instalar por USB:  adb install -r compilado_android\Encriptador.apk
echo  O copialo al telefono y abrilo directamente.
echo ============================================
endlocal
