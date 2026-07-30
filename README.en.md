<div align="center">

# 🔒 Encriptador

**Encrypt and decrypt your files and folders with AES-256-GCM, on Windows, Linux and Android.**

🇬🇧 English | 🇪🇸 [Español](README.md) | 🇷🇺 [Русский](README.ru.md) | 🇨🇳 [中文](README.zh.md)

[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Windows](https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white)
![Linux](https://img.shields.io/badge/Linux-FCC624?logo=linux&logoColor=black)
![Android](https://img.shields.io/badge/Android-3DDC84?logo=android&logoColor=white)

</div>

---

## ✨ Features

- **Strong encryption**: authenticated AES-256-GCM, chunked (detects wrong passwords and file tampering early, not just at the end of the stream).
- **Robust key derivation**: PBKDF2-HMAC-SHA256 with 600,000 iterations.
- **Optional secure delete**: overwrites the original file with random data before deleting it.
- **Encrypts single files, multiple files at once, or entire folders** (with subfolders) into a single `.enc` container.
- **Built-in explorer**: browse and extract individual files from an encrypted folder without decrypting everything.
- Password strength meter, password confirmation, show/hide toggle, real per-file progress.
- **OS integration**: right-click → "Encrypt with Encriptador", and double-click / "Open with" on `.enc` files (Windows, Linux/MATE and Android).
- **Language selector** (Español / English / Русский / 中文) on all three platforms, with automatic system-language detection on first run.
- Same dark UI and the same `.enc` file format across all three platforms.

## 📦 Download

### Option 1 — Prebuilt version (recommended)

The **[Releases](../../releases/latest)** section of this repository publishes, for every version, ready-to-use installers for all three platforms:

| Platform | File to download | Usage |
|---|---|---|
| 🪟 Windows | `Encriptador-windows-Setup.exe` | Run it to install the app (Start Menu entry + right-click integration, no admin required). Also available: `Encriptador-windows-x64.exe`, a standalone portable `.exe` with no installer. |
| 🐧 Linux | `Encriptador-linux-x64.tar.gz` | Extract it and run `chmod +x Encriptador instalar.sh desinstalar.sh && ./instalar.sh` |
| 🤖 Android | `Encriptador-android.apk` | Copy it to your phone and open it (allowing "unknown sources" the first time), or `adb install -r Encriptador-android.apk` |

These binaries are built and published automatically ([`.github/workflows/release.yml`](.github/workflows/release.yml)) every time a new version is tagged (`git tag vX.Y.Z && git push origin vX.Y.Z`).

### Option 2 — Build from source

Each platform builds from its own folder with a single command / double-click:

| Platform | Folder | How to build | Output |
|---|---|---|---|
| 🪟 Windows | [`windows/`](windows/) | double-click `compilar_windows.bat` | `compilado\Encriptador.exe` + installer `Encriptador_Setup.exe` |
| 🐧 Linux | [`linux/`](linux/) | double-click `compilar_linux.bat` | a `compilado_linux/` folder ready to copy over and run `./instalar.sh` |
| 🤖 Android | [`android/`](android/) | double-click `compilar_android.bat` | `compilado_android/Encriptador.apk` |

> The full source code lives right here in this repository, organized by platform folder — nothing else to download.

---

## 🪟 Windows

Requires the [.NET SDK](https://dotnet.microsoft.com/download) installed only to build (the resulting `.exe` is self-contained and doesn't need it on the target PC).

```bat
cd windows
compilar_windows.bat
```

This produces `compilado\Encriptador.exe` and, if you have [Inno Setup](https://jrsoftware.org/isdl.php) installed, also `salida_instalador\Encriptador_Setup.exe`: a per-user installer (no admin required) that automatically registers the "Encrypt with Encriptador" context menu and the `.enc` file association.

## 🐧 Linux

Built with [Avalonia UI](https://avaloniaui.net/) on .NET. Requires the .NET SDK only to build; the resulting binary is self-contained.

```bat
cd linux
compilar_linux.bat
```

Copy the whole `compilado_linux/` folder to your Linux machine and there:

```bash
chmod +x Encriptador instalar.sh desinstalar.sh
./instalar.sh
```

The installer registers the `.desktop` entry, the MIME association for `.enc`, and, on MATE/Caja environments, both a "Scripts" entry and a best-effort Caja Actions context-menu item.

## 🤖 Android

Native Kotlin/Jetpack Compose app, fully independent from the desktop versions (same `.enc` format, no shared code).

```bat
cd android
compilar_android.bat
```

Installing on a phone connected via USB (with USB debugging enabled):

```bash
adb install -r compilado_android\Encriptador.apk
```

Or copy the `.apk` to the phone and open it directly (allowing "install apps from unknown sources" the first time).

---

## 🔐 The `.enc` file format

All three platforms share the same binary format, so a file encrypted on Windows can be decrypted on Linux or Android and vice versa:

- Encrypted in 64 KB chunks, each with its own nonce and authentication tag (AES-256-GCM).
- A container can represent a single file, several files, or an entire folder with subfolders.
- The key is derived from the password with PBKDF2-HMAC-SHA256 (600,000 iterations) and a random salt per file.

## 📄 License

This project is distributed under the [MIT](LICENSE) license.

---

<div align="center">
<sub>Developed by Pablo Martín Fernández</sub>
</div>
