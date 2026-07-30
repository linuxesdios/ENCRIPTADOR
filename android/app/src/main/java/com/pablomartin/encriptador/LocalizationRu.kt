package com.pablomartin.encriptador

internal object LocalizationRu {
    val mapa: Map<String, String> = mapOf(
        // ===== common =====
        "common.mostrar" to "Показать",
        "common.ocultar" to "Скрыть",
        "common.contrasena" to "ПАРОЛЬ",
        "common.repetirContrasena" to "ПОВТОРИТЕ ПАРОЛЬ",
        "common.footer.credito" to "Разработано: %s",
        "common.estado.passwordRequerida" to "Введите пароль.",
        "common.estado.passwordsNoCoinciden" to "Пароли не совпадают.",
        "common.estado.archivoEncriptado" to "Файл успешно зашифрован.",
        "common.estado.archivoDesencriptado" to "Файл успешно расшифрован.",
        "common.estado.verificandoPassword" to "Проверка пароля...",
        "common.estado.error" to "Ошибка: %s",

        // ===== strength =====
        "strength.muyDebil" to "Очень слабый",
        "strength.debil" to "Слабый",
        "strength.media" to "Средний",
        "strength.fuerte" to "Сильный",
        "strength.muyFuerte" to "Очень сильный",

        // ===== crypto =====
        "crypto.archivoDanado" to "Неверный пароль или повреждённый файл.",
        "crypto.tipoEsCarpeta" to "Этот .enc содержит папку.",
        "crypto.tipoEsArchivo" to "Этот .enc — отдельный файл.",

        // ===== pantalla (MainActivity) =====
        "pantalla.tagline" to "Шифрование файлов AES-256-GCM",
        "pantalla.archivoOCarpeta" to "ФАЙЛ ИЛИ ПАПКА",
        "pantalla.btnArchivos" to "📄 Файл(ы)",
        "pantalla.btnCarpeta" to "📁 Папка",
        "pantalla.nadaSeleccionado" to "Ничего не выбрано",
        "pantalla.variosSeleccionados" to "Выбрано файлов: %d",
        "pantalla.carpetaSeleccionada" to "Папка: %s",
        "pantalla.conservarOriginal" to "Сохранить исходный файл (не удалять по завершении)",
        "pantalla.conservarEnc" to "Сохранить файл .enc (не удалять по завершении)",
        "pantalla.btnEncriptar" to "🔒 Зашифровать",
        "pantalla.btnDesencriptar" to "🔑 Расшифровать",
        "pantalla.btnElegirCarpetaDestino" to "📂 Выбрать папку назначения и извлечь",
        "pantalla.progresoArchivo" to "Файл %1\$d из %2\$d",
        "pantalla.elegirDondeGuardar" to "Выберите, куда сохранить .enc...",
        "pantalla.sinSeleccion" to "Выберите файл(ы) или папку.",
        "pantalla.variosArchivosEnc" to "%d файлов.enc",
        "pantalla.variosEncriptados" to "Файлов зашифровано в один пакет: %d.",
        "pantalla.noSePudoAbrirCarpeta" to "Не удалось открыть папку.",
        "pantalla.carpetaEncriptadaUnArchivo" to "Папка зашифрована в один файл.",
        "pantalla.soloUnArchivoEnc" to "Выберите один файл .enc.",
        "pantalla.carpetaConArchivos" to "Папка содержит файлов: %d. Выберите, куда извлечь.",
        "pantalla.extrayendo" to "Извлечение...",
        "pantalla.noSePudoAbrirDestino" to "Не удалось открыть место назначения.",
        "pantalla.carpetaRestaurada" to "Папка восстановлена: %d файл(ов).",
        "pantalla.errorAlExtraer" to "Ошибка при извлечении: %s",
        "pantalla.noSePudoCrearCarpeta" to "Не удалось создать папку «%s».",
        "pantalla.noSePudoCrearArchivo" to "Не удалось создать файл «%s».",
        "pantalla.nombreFallbackArchivo" to "файл",
        "pantalla.nombreFallbackCarpeta" to "папка",
    )
}
