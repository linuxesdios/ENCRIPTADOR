namespace Encriptador.Services;

public static partial class Loc
{
    private static readonly Dictionary<string, string> Zh = new()
    {
        // ===== common =====
        ["common.mostrar"] = "显示",
        ["common.ocultar"] = "隐藏",
        ["common.encriptar"] = "加密",
        ["common.desencriptar"] = "解密",
        ["common.contrasena"] = "密码",
        ["common.repetirContrasena"] = "确认密码",
        ["common.chk.borradoSeguro"] = "安全删除(删除前用随机数据覆盖)",
        ["common.chk.conservarOriginal.encriptar"] = "保留原始文件(完成后不删除)",
        ["common.chk.conservarOriginal.desencriptar"] = "保留加密的 .enc 文件(完成后不删除)",
        ["common.estado.passwordRequerida"] = "请输入密码。",
        ["common.estado.passwordsNoCoinciden"] = "两次输入的密码不一致。",
        ["common.estado.procesando"] = "正在处理…",
        ["common.estado.procesandoArchivo"] = "正在处理文件 {0}/{1}…",
        ["common.estado.archivoDesencriptado"] = "文件解密成功。",
        ["common.estado.archivoEncriptado"] = "文件加密成功。",
        ["common.estado.carpetaEncriptada"] = "文件夹已加密为单个文件(包含 {0} 个文件)。",
        ["common.estado.variosEncriptados"] = "已将 {0} 个文件加密为一个压缩包。",
        ["common.estado.carpetaRestaurada"] = "文件夹已还原:{0}/{1} 个文件。",
        ["common.estado.operacionCancelada"] = "操作已取消,未做任何更改。",
        ["common.estado.verificandoPassword"] = "正在验证密码…",
        ["common.estado.noPudoDesencriptar"] = "无法解密:密码错误或文件已损坏。",
        ["common.estado.error"] = "错误:{0}",
        ["common.footer.credito"] = "作者:{0}",
        ["common.contextmenu.encriptarCon"] = "使用 Encriptador 加密",
        ["common.contextmenu.progIdNombre"] = "加密文件 (Encriptador)",

        // ===== strength =====
        ["strength.muyDebil"] = "非常弱",
        ["strength.debil"] = "弱",
        ["strength.media"] = "中等",
        ["strength.fuerte"] = "强",
        ["strength.muyFuerte"] = "非常强",

        // ===== crypto =====
        ["crypto.archivoDanado"] = "密码错误或文件已损坏。",
        ["crypto.rutaNoExiste"] = "路径不存在。",
        ["crypto.tipoEsCarpeta"] = "此 .enc 文件包含一个文件夹:请使用浏览器打开,而不要直接解密。",
        ["crypto.tipoEsArchivo"] = "此 .enc 是单个文件:请直接解密,而不要用浏览器打开。",
        ["crypto.archivoNoManifiesto"] = "清单中没有该文件。",
        ["crypto.seleccionarArchivoEnc"] = "解密时请选择 .enc 文件,而不是文件夹。",

        // ===== main (MainWindow) =====
        ["main.tagline"] = "AES-256-GCM 文件加密",
        ["main.archivo.etiqueta"] = "文件",
        ["main.archivo.placeholder"] = "将文件或文件夹拖到此处,或点击选择",
        ["main.archivo.carpeta"] = "文件夹:{0}",
        ["main.archivo.variosSeleccionados"] = "已选择 {0} 个文件",
        ["main.dialog.abrir.titulo"] = "选择文件",
        ["main.dialog.abrir.filtroEtiqueta"] = "所有文件 (*.*)",
        ["main.dialog.guardar.titulo"] = "另存为",
        ["main.dialog.guardar.filtroEtiqueta"] = "加密文件 (*.enc)",
        ["main.dialog.guardar.nombreVarios"] = "{0} 个文件.enc",
        ["main.error.carpetaUnaPorVez"] = "文件夹请一次选择一个(不能与其他文件混合)。",
        ["main.error.soloUnArchivoEnc"] = "解密时请选择一个 .enc 文件。",
        ["main.error.noEsCarpeta"] = "解密时请选择 .enc 文件(而不是文件夹)。",
        ["main.error.seleccionInvalida"] = "请选择有效的文件或文件夹。",
        ["main.tooltip.acerca"] = "关于",

        // ===== quick (QuickWindow) =====
        ["quick.archivo.archivo"] = "文件:{0}",

        // ===== explorer (ExplorerWindow) =====
        ["explorer.titulo"] = "加密内容",
        ["explorer.instrucciones"] = "双击文件即可打开。在您提取所选文件之前,不会保存任何内容。",
        ["explorer.btn.seleccionarTodo"] = "全选",
        ["explorer.btn.deseleccionarTodo"] = "取消全选",
        ["explorer.btn.cancelar"] = "取消",
        ["explorer.btn.extraerTodo"] = "提取全部 ({0})",
        ["explorer.btn.extraerSeleccionados"] = "提取所选 ({0})",
        ["explorer.subtitulo_uno"] = "1 个文件",
        ["explorer.subtitulo_otros"] = "{0} 个文件",
        ["explorer.tooltip.dobleClic"] = "双击打开",
        ["explorer.estado.abriendo"] = "   (正在打开…)",
        ["explorer.estado.errorAlAbrir"] = "   (无法打开:{0})",
        ["explorer.error.noSePudoAbrir"] = "无法打开文件:{0}",

        // ===== about (AboutWindow) =====
        ["about.desarrollador"] = "应用开发者",
        ["about.tagline"] = "使用 AES-256-GCM 加密和解密文件",
        ["about.version"] = "版本 1.0 · Avalonia UI",
        ["about.cerrar"] = "关闭",
    };
}
