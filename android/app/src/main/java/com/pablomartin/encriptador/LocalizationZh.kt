package com.pablomartin.encriptador

internal object LocalizationZh {
    val mapa: Map<String, String> = mapOf(
        // ===== common =====
        "common.mostrar" to "显示",
        "common.ocultar" to "隐藏",
        "common.contrasena" to "密码",
        "common.repetirContrasena" to "确认密码",
        "common.footer.credito" to "作者:%s",
        "common.estado.passwordRequerida" to "请输入密码。",
        "common.estado.passwordsNoCoinciden" to "两次输入的密码不一致。",
        "common.estado.archivoEncriptado" to "文件加密成功。",
        "common.estado.archivoDesencriptado" to "文件解密成功。",
        "common.estado.verificandoPassword" to "正在验证密码…",
        "common.estado.error" to "错误:%s",

        // ===== strength =====
        "strength.muyDebil" to "非常弱",
        "strength.debil" to "弱",
        "strength.media" to "中等",
        "strength.fuerte" to "强",
        "strength.muyFuerte" to "非常强",

        // ===== crypto =====
        "crypto.archivoDanado" to "密码错误或文件已损坏。",
        "crypto.tipoEsCarpeta" to "此 .enc 文件包含一个文件夹。",
        "crypto.tipoEsArchivo" to "此 .enc 是单个文件。",

        // ===== pantalla (MainActivity) =====
        "pantalla.tagline" to "AES-256-GCM 文件加密",
        "pantalla.archivoOCarpeta" to "文件或文件夹",
        "pantalla.btnArchivos" to "📄 文件",
        "pantalla.btnCarpeta" to "📁 文件夹",
        "pantalla.nadaSeleccionado" to "未选择任何内容",
        "pantalla.variosSeleccionados" to "已选择 %d 个文件",
        "pantalla.carpetaSeleccionada" to "文件夹:%s",
        "pantalla.conservarOriginal" to "保留原始文件(完成后不删除)",
        "pantalla.conservarEnc" to "保留 .enc 文件(完成后不删除)",
        "pantalla.btnEncriptar" to "🔒 加密",
        "pantalla.btnDesencriptar" to "🔑 解密",
        "pantalla.btnElegirCarpetaDestino" to "📂 选择目标文件夹并提取",
        "common.estado.progreso" to "%1\$d%%",
        "common.estado.progresoConEta" to "%1\$d%% · 剩余 %2\$s",
        "pantalla.elegirDondeGuardar" to "选择 .enc 的保存位置…",
        "pantalla.sinSeleccion" to "请选择文件或文件夹。",
        "pantalla.variosArchivosEnc" to "%d 个文件.enc",
        "pantalla.variosEncriptados" to "已将 %d 个文件加密为一个压缩包。",
        "pantalla.noSePudoAbrirCarpeta" to "无法打开文件夹。",
        "pantalla.carpetaEncriptadaUnArchivo" to "文件夹已加密为单个文件。",
        "pantalla.soloUnArchivoEnc" to "请选择一个 .enc 文件。",
        "pantalla.carpetaConArchivos" to "文件夹包含 %d 个文件。请选择提取位置。",
        "pantalla.extrayendo" to "正在提取…",
        "pantalla.noSePudoAbrirDestino" to "无法打开目标位置。",
        "pantalla.carpetaRestaurada" to "文件夹已还原:%d 个文件。",
        "pantalla.errorAlExtraer" to "提取时出错:%s",
        "pantalla.noSePudoCrearCarpeta" to "无法创建文件夹“%s”。",
        "pantalla.noSePudoCrearArchivo" to "无法创建文件“%s”。",
        "pantalla.nombreFallbackArchivo" to "文件",
        "pantalla.nombreFallbackCarpeta" to "文件夹",
    )
}
