<div align="center">

# 🔒 Encriptador

**Шифруйте и расшифровывайте файлы и папки с помощью AES-256-GCM на Windows, Linux и Android.**

🇷🇺 Русский | 🇪🇸 [Español](README.md) | 🇬🇧 [English](README.en.md) | 🇨🇳 [中文](README.zh.md)

[![Лицензия MIT](https://img.shields.io/badge/лицензия-MIT-blue.svg)](LICENSE)
![Windows](https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white)
![Linux](https://img.shields.io/badge/Linux-FCC624?logo=linux&logoColor=black)
![Android](https://img.shields.io/badge/Android-3DDC84?logo=android&logoColor=white)

</div>

---

## ✨ Возможности

- **Надёжное шифрование**: аутентифицированный AES-256-GCM, по блокам (неверный пароль или повреждение файла обнаруживаются сразу, а не только в конце потока).
- **Стойкая деривация ключа**: PBKDF2-HMAC-SHA256, 600 000 итераций.
- **Безопасное удаление (опционально)**: перезаписывает исходный файл случайными данными перед удалением.
- **Шифрует отдельные файлы, несколько файлов сразу или целые папки** (с подпапками) в один контейнер `.enc`.
- **Встроенный проводник**: просмотр и извлечение отдельных файлов из зашифрованной папки без расшифровки всего содержимого.
- Индикатор надёжности пароля, подтверждение пароля, показать/скрыть, реальный прогресс по каждому файлу.
- **Интеграция с системой**: правый клик → «Зашифровать с помощью Encriptador», двойной клик / «Открыть с помощью» для файлов `.enc` (Windows, Linux/MATE и Android).
- **Выбор языка** (Español / English / Русский / 中文) на всех трёх платформах, с автоматическим определением языка системы при первом запуске.
- Одинаковый тёмный интерфейс и один и тот же формат файла `.enc` на всех трёх платформах.

## 📦 Скачать

### Вариант 1 — Готовая версия (рекомендуется)

В разделе **[Releases](../../releases/latest)** этого репозитория для каждой версии публикуются готовые к использованию сборки для всех трёх платформ:

| Платформа | Файл для скачивания | Использование |
|---|---|---|
| 🪟 Windows | `Encriptador-windows-Setup.exe` | Запустите, чтобы установить приложение (ярлык в меню «Пуск» + интеграция с контекстным меню, без прав администратора). Также доступен `Encriptador-windows-x64.exe` — портативный `.exe` без установщика. |
| 🐧 Linux | `Encriptador-linux-x64.tar.gz` | Распакуйте и выполните `chmod +x Encriptador instalar.sh desinstalar.sh && ./instalar.sh` |
| 🤖 Android | `Encriptador-android.apk` | Скопируйте на телефон и откройте (разрешив «неизвестные источники» при первом запуске), либо `adb install -r Encriptador-android.apk` |

Эти бинарники собираются и публикуются автоматически ([`.github/workflows/release.yml`](.github/workflows/release.yml)) при каждой публикации тега версии (`git tag vX.Y.Z && git push origin vX.Y.Z`).

### Вариант 2 — Сборка из исходного кода

Каждая платформа собирается из своей папки одной командой / двойным щелчком:

| Платформа | Папка | Как собрать | Результат |
|---|---|---|---|
| 🪟 Windows | [`windows/`](windows/) | двойной клик по `compilar_windows.bat` | `compilado\Encriptador.exe` + установщик `Encriptador_Setup.exe` |
| 🐧 Linux | [`linux/`](linux/) | двойной клик по `compilar_linux.bat` | папка `compilado_linux/`, готовая к копированию и запуску `./instalar.sh` |
| 🤖 Android | [`android/`](android/) | двойной клик по `compilar_android.bat` | `compilado_android/Encriptador.apk` |

> Полный исходный код находится прямо в этом репозитории, организованный по папкам для каждой платформы — больше ничего скачивать не нужно.

---

## 🪟 Windows

Требует [.NET SDK](https://dotnet.microsoft.com/download), установленный только для сборки (получившийся `.exe` самодостаточен и не требует .NET на целевом ПК).

```bat
cd windows
compilar_windows.bat
```

Это создаёт `compilado\Encriptador.exe`, а если у вас установлен [Inno Setup](https://jrsoftware.org/isdl.php) — ещё и `salida_instalador\Encriptador_Setup.exe`: установщик для текущего пользователя (без прав администратора), который автоматически регистрирует пункт контекстного меню «Зашифровать с помощью Encriptador» и ассоциацию файлов `.enc`.

## 🐧 Linux

Собрано на [Avalonia UI](https://avaloniaui.net/) поверх .NET. Требует .NET SDK только для сборки; итоговый бинарник самодостаточен.

```bat
cd linux
compilar_linux.bat
```

Скопируйте всю папку `compilado_linux/` на вашу Linux-машину и там выполните:

```bash
chmod +x Encriptador instalar.sh desinstalar.sh
./instalar.sh
```

Установщик регистрирует `.desktop`-файл, MIME-ассоциацию для `.enc`, а в окружениях MATE/Caja — как пункт в «Scripts», так и best-effort пункт в контекстном меню Caja Actions.

## 🤖 Android

Нативное приложение на Kotlin/Jetpack Compose, полностью независимое от версий для ПК (тот же формат `.enc`, без общего кода).

```bat
cd android
compilar_android.bat
```

Установка на телефон, подключённый по USB (с включённой отладкой по USB):

```bash
adb install -r compilado_android\Encriptador.apk
```

Либо скопируйте `.apk` на телефон и откройте его напрямую (разрешив «установку приложений из неизвестных источников» при первом запуске).

---

## 🔐 Формат файла `.enc`

Все три платформы используют один и тот же бинарный формат, поэтому файл, зашифрованный в Windows, можно расшифровать в Linux или Android, и наоборот:

- Шифрование блоками по 64 КБ, каждый со своим nonce и тегом аутентификации (AES-256-GCM).
- Контейнер может представлять собой один файл, несколько файлов или целую папку с подпапками.
- Ключ выводится из пароля с помощью PBKDF2-HMAC-SHA256 (600 000 итераций) и случайной соли для каждого файла.

## 📄 Лицензия

Этот проект распространяется по лицензии [MIT](LICENSE).

---

<div align="center">
<sub>Разработано Pablo Martín Fernández</sub>
</div>
