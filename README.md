<div align="center">

# 🔒 Encriptador

**Cifra y descifra tus archivos y carpetas con AES-256-GCM, en Windows, Linux y Android.**

[![Licencia MIT](https://img.shields.io/badge/licencia-MIT-blue.svg)](LICENSE)
![Windows](https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white)
![Linux](https://img.shields.io/badge/Linux-FCC624?logo=linux&logoColor=black)
![Android](https://img.shields.io/badge/Android-3DDC84?logo=android&logoColor=white)

</div>

---

## ✨ Características

- **Cifrado fuerte**: AES-256-GCM autenticado por bloques (detecta contraseñas incorrectas y manipulación del archivo de forma temprana).
- **Derivación de clave robusta**: PBKDF2-HMAC-SHA256 con 600.000 iteraciones.
- **Borrado seguro opcional**: sobrescribe el archivo original con datos aleatorios antes de borrarlo.
- **Cifra archivos sueltos, varios a la vez, o carpetas enteras** (con subcarpetas) en un único contenedor `.enc`.
- **Explorador integrado**: navega y extrae archivos individuales de una carpeta cifrada sin descifrar todo el contenido.
- Medidor de fortaleza de contraseña, confirmación de contraseña, mostrar/ocultar, progreso real por archivo.
- **Integración con el sistema**: clic derecho → "Encriptar con Encriptador", y doble clic / "Abrir con" sobre archivos `.enc` (Windows, Linux/MATE y Android).
- Misma interfaz oscura y el mismo formato de archivo `.enc` en las tres plataformas.

## 📦 Descargar

### Opción 1 — Versión ya compilada (recomendado)

En la sección **[Releases](../../releases/latest)** de este repositorio se publica, para cada versión, el instalable listo para usar de las tres plataformas:

| Plataforma | Archivo a descargar | Uso |
|---|---|---|
| 🪟 Windows | `Encriptador-windows-Setup.exe` | Ejecutalo e instala la app (menú Inicio + integración con el clic derecho, sin necesitar admin). También está `Encriptador-windows-x64.exe` (el `.exe` suelto, portable, sin instalador). |
| 🐧 Linux | `Encriptador-linux-x64.tar.gz` | Descomprimilo y corré `chmod +x Encriptador instalar.sh desinstalar.sh && ./instalar.sh` |
| 🤖 Android | `Encriptador-android.apk` | Copialo al teléfono y abrilo (permitiendo "orígenes desconocidos" la primera vez), o `adb install -r Encriptador-android.apk` |

Estos binarios se generan y publican automáticamente ([`.github/workflows/release.yml`](.github/workflows/release.yml)) cada vez que se etiqueta una nueva versión (`git tag vX.Y.Z && git push origin vX.Y.Z`).

### Opción 2 — Compilar desde el código fuente

Cada plataforma se compila desde su propia carpeta con un solo comando/doble clic:

| Plataforma | Carpeta | Cómo compilar | Resultado |
|---|---|---|---|
| 🪟 Windows | [`windows/`](windows/) | doble clic en `compilar_windows.bat` | `compilado\Encriptador.exe` + instalador `Encriptador_Setup.exe` |
| 🐧 Linux | [`linux/`](linux/) | doble clic en `compilar_linux.bat` | carpeta `compilado_linux/` lista para copiar y ejecutar `./instalar.sh` |
| 🤖 Android | [`android/`](android/) | doble clic en `compilar_android.bat` | `compilado_android/Encriptador.apk` |

> El código fuente completo está en este mismo repositorio, organizado por carpeta según la plataforma — no hace falta descargar nada aparte.

---

## 🪟 Windows

Requiere el [SDK de .NET](https://dotnet.microsoft.com/download) instalado solo para compilar (el `.exe` generado es autocontenido y no lo necesita en la PC destino).

```bat
cd windows
compilar_windows.bat
```

Esto genera `compilado\Encriptador.exe` y, si tenés [Inno Setup](https://jrsoftware.org/isdl.php) instalado, también `salida_instalador\Encriptador_Setup.exe`: un instalador por usuario (sin necesitar admin) que registra automáticamente el menú contextual "Encriptar con Encriptador" y la asociación de archivos `.enc`.

## 🐧 Linux

Compilado con [Avalonia UI](https://avaloniaui.net/) sobre .NET. Requiere el SDK de .NET solo para compilar; el binario resultante es autocontenido.

```bat
cd linux
compilar_linux.bat
```

Copiá la carpeta `compilado_linux/` completa a tu máquina Linux y ahí:

```bash
chmod +x Encriptador instalar.sh desinstalar.sh
./instalar.sh
```

El instalador registra el `.desktop`, la asociación MIME para `.enc` y, en entornos MATE/Caja, tanto un script de "Scripts" como una entrada best-effort en el menú contextual de Caja Actions.

## 🤖 Android

App nativa en Kotlin/Jetpack Compose, totalmente independiente de las versiones de escritorio (mismo formato `.enc`, sin compartir código).

```bat
cd android
compilar_android.bat
```

Instalación en el teléfono conectado por USB (con depuración USB activada):

```bash
adb install -r compilado_android\Encriptador.apk
```

O copiá el `.apk` al teléfono y abrilo directamente (permitiendo "instalar apps de orígenes desconocidos" la primera vez).

---

## 🔐 Formato del archivo `.enc`

Las tres plataformas comparten un mismo formato binario, así que un archivo cifrado en Windows se puede descifrar en Linux o Android y viceversa:

- Cifrado por bloques de 64 KB, cada uno con su propio nonce y tag de autenticación (AES-256-GCM).
- Un contenedor puede representar un archivo suelto, varios archivos, o una carpeta completa con subcarpetas.
- La clave se deriva de la contraseña con PBKDF2-HMAC-SHA256 (600.000 iteraciones) y una sal aleatoria por archivo.

## 📄 Licencia

Este proyecto se distribuye bajo la licencia [MIT](LICENSE).

---

<div align="center">
<sub>Desarrollado por Pablo Martín Fernández</sub>
</div>
