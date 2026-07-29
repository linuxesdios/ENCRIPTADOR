; Script de Inno Setup para el instalador de Encriptador.
; Compilar con: ISCC.exe instalador.iss
; (el .bat compilar_windows.bat ya hace esto automáticamente)

#define MyAppName "Encriptador"
#define MyAppVersion "1.0"
#define MyAppPublisher "Pablo Martín Fernández"
#define MyAppExeName "Encriptador.exe"

[Setup]
AppId={{B6C1A9F4-8E2D-4B7A-9C3E-7F1A2D5E6B90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Instalación por usuario: no requiere permisos de administrador.
PrivilegesRequired=lowest
OutputDir=salida_instalador
OutputBaseFilename=Encriptador_Setup
SetupIconFile=Encriptador\Resources\icono.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "compilado\Encriptador.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Activa el menú contextual (clic derecho / abrir con) apenas termina la instalación, sin abrir ninguna ventana.
Filename: "{app}\{#MyAppExeName}"; Parameters: "--registrar"; Flags: waituntilterminated runhidden
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Quita las entradas del menú contextual (clic derecho / abrir con) antes de borrar los archivos.
Filename: "{app}\{#MyAppExeName}"; Parameters: "--desregistrar"; Flags: waituntilterminated runhidden; RunOnceId: "DesregistrarMenuContextual"
