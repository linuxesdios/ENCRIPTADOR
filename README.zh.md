<div align="center">

# 🔒 Encriptador

**在 Windows、Linux 和 Android 上使用 AES-256-GCM 加密和解密你的文件与文件夹。**

🇨🇳 中文 | 🇪🇸 [Español](README.md) | 🇬🇧 [English](README.en.md) | 🇷🇺 [Русский](README.ru.md)

[![MIT 许可证](https://img.shields.io/badge/许可证-MIT-blue.svg)](LICENSE)
![Windows](https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white)
![Linux](https://img.shields.io/badge/Linux-FCC624?logo=linux&logoColor=black)
![Android](https://img.shields.io/badge/Android-3DDC84?logo=android&logoColor=white)

</div>

---

## ✨ 功能特点

- **强加密**:经过身份验证的 AES-256-GCM,分块处理(能提前检测到密码错误或文件被篡改,而不用等到流的末尾)。
- **强健的密钥派生**:PBKDF2-HMAC-SHA256,60 万次迭代。
- **可选的安全删除**:删除前用随机数据覆盖原始文件。
- **可加密单个文件、多个文件或整个文件夹**(含子文件夹),打包为一个 `.enc` 容器。
- **内置浏览器**:无需解密全部内容,即可浏览并提取加密文件夹中的单个文件。
- 密码强度指示器、密码确认、显示/隐藏密码切换、按文件显示的真实进度。
- **系统集成**:右键点击 →「使用 Encriptador 加密」,双击 / 「打开方式」直接处理 `.enc` 文件(Windows、Linux/MATE 和 Android)。
- **语言选择器**(Español / English / Русский / 中文),三个平台均支持,首次运行时自动检测系统语言。
- 三个平台使用相同的深色界面和相同的 `.enc` 文件格式。

## 📦 下载

### 方式一 —— 预编译版本(推荐)

本仓库的 **[Releases](../../releases/latest)** 页面为每个版本发布了三个平台的现成安装包:

| 平台 | 下载文件 | 用法 |
|---|---|---|
| 🪟 Windows | `Encriptador-windows-Setup.exe` | 运行以安装应用(添加开始菜单项 + 右键菜单集成,无需管理员权限)。也提供 `Encriptador-windows-x64.exe`,一个无需安装的便携版 `.exe`。 |
| 🐧 Linux | `Encriptador-linux-x64.tar.gz` | 解压后运行 `chmod +x Encriptador instalar.sh desinstalar.sh && ./instalar.sh` |
| 🤖 Android | `Encriptador-android.apk` | 复制到手机上打开(首次需允许"未知来源"),或使用 `adb install -r Encriptador-android.apk` |

这些二进制文件由 [`.github/workflows/release.yml`](.github/workflows/release.yml) 在每次打版本标签时自动构建并发布(`git tag vX.Y.Z && git push origin vX.Y.Z`)。

### 方式二 —— 从源码编译

每个平台都可以在各自的文件夹中通过一条命令/双击完成编译:

| 平台 | 文件夹 | 编译方法 | 产物 |
|---|---|---|---|
| 🪟 Windows | [`windows/`](windows/) | 双击 `compilar_windows.bat` | `compilado\Encriptador.exe` + 安装程序 `Encriptador_Setup.exe` |
| 🐧 Linux | [`linux/`](linux/) | 双击 `compilar_linux.bat` | 生成 `compilado_linux/` 文件夹,可直接复制并运行 `./instalar.sh` |
| 🤖 Android | [`android/`](android/) | 双击 `compilar_android.bat` | `compilado_android/Encriptador.apk` |

> 完整源代码就在本仓库中,按平台分文件夹组织——无需另外下载任何东西。

---

## 🪟 Windows

仅编译时需要安装 [.NET SDK](https://dotnet.microsoft.com/download)(生成的 `.exe` 是自包含的,目标电脑上不需要安装 .NET)。

```bat
cd windows
compilar_windows.bat
```

这会生成 `compilado\Encriptador.exe`;如果你安装了 [Inno Setup](https://jrsoftware.org/isdl.php),还会生成 `salida_instalador\Encriptador_Setup.exe`——一个按用户安装的安装程序(无需管理员权限),会自动注册「使用 Encriptador 加密」右键菜单以及 `.enc` 文件关联。

## 🐧 Linux

基于 .NET 之上的 [Avalonia UI](https://avaloniaui.net/) 构建。仅编译时需要 .NET SDK;生成的二进制文件是自包含的。

```bat
cd linux
compilar_linux.bat
```

将整个 `compilado_linux/` 文件夹复制到你的 Linux 机器上,然后:

```bash
chmod +x Encriptador instalar.sh desinstalar.sh
./instalar.sh
```

安装脚本会注册 `.desktop` 文件、`.enc` 的 MIME 关联,并在 MATE/Caja 环境下同时添加一个"Scripts"脚本入口和一个尽力而为的 Caja Actions 右键菜单项。

## 🤖 Android

原生 Kotlin/Jetpack Compose 应用,与桌面版本完全独立(格式相同的 `.enc`,不共享任何代码)。

```bat
cd android
compilar_android.bat
```

通过 USB 连接手机安装(需开启 USB 调试):

```bash
adb install -r compilado_android\Encriptador.apk
```

或者直接把 `.apk` 复制到手机上打开(首次需要允许"安装未知来源的应用")。

---

## 🔐 `.enc` 文件格式

三个平台共享同一种二进制格式,因此在 Windows 上加密的文件可以在 Linux 或 Android 上解密,反之亦然:

- 以 64 KB 为单位分块加密,每块都有自己的 nonce 和身份验证标签(AES-256-GCM)。
- 一个容器可以代表单个文件、多个文件,或包含子文件夹的整个文件夹。
- 密钥通过 PBKDF2-HMAC-SHA256(60 万次迭代)从密码派生,每个文件使用随机盐值。

## 📄 许可证

本项目基于 [MIT](LICENSE) 许可证发布。

---

<div align="center">
<sub>由 Pablo Martín Fernández 开发</sub>
</div>
