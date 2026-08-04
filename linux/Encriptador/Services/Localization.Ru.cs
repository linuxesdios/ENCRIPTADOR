namespace Encriptador.Services;

public static partial class Loc
{
    private static readonly Dictionary<string, string> Ru = new()
    {
        // ===== common =====
        ["common.mostrar"] = "Показать",
        ["common.ocultar"] = "Скрыть",
        ["common.encriptar"] = "Зашифровать",
        ["common.desencriptar"] = "Расшифровать",
        ["common.contrasena"] = "ПАРОЛЬ",
        ["common.repetirContrasena"] = "ПОВТОРИТЕ ПАРОЛЬ",
        ["common.chk.borradoSeguro"] = "Безопасное удаление (перезаписать перед удалением)",
        ["common.chk.conservarOriginal.encriptar"] = "Сохранить исходный файл (не удалять по завершении)",
        ["common.chk.conservarOriginal.desencriptar"] = "Сохранить зашифрованный файл .enc (не удалять по завершении)",
        ["common.estado.passwordRequerida"] = "Введите пароль.",
        ["common.estado.passwordsNoCoinciden"] = "Пароли не совпадают.",
        ["common.estado.procesando"] = "Обработка...",
        ["common.estado.progreso"] = "{0}%",
        ["common.estado.progresoConEta"] = "{0}% · осталось {1}",
        ["common.estado.archivoDesencriptado"] = "Файл успешно расшифрован.",
        ["common.estado.archivoEncriptado"] = "Файл успешно зашифрован.",
        ["common.estado.carpetaEncriptada"] = "Папка зашифрована в один файл (включено файлов: {0}).",
        ["common.estado.variosEncriptados"] = "Файлов зашифровано в один пакет: {0}.",
        ["common.estado.carpetaRestaurada"] = "Папка восстановлена: {0} из {1} файлов.",
        ["common.estado.operacionCancelada"] = "Операция отменена. Ничего не изменено.",
        ["common.estado.verificandoPassword"] = "Проверка пароля...",
        ["common.estado.noPudoDesencriptar"] = "Не удалось расшифровать: неверный пароль или повреждённый файл.",
        ["common.estado.error"] = "Ошибка: {0}",
        ["common.footer.credito"] = "Разработано: {0}",
        ["common.contextmenu.encriptarCon"] = "Зашифровать с помощью Encriptador",
        ["common.contextmenu.progIdNombre"] = "Зашифрованный файл (Encriptador)",

        // ===== strength =====
        ["strength.muyDebil"] = "Очень слабый",
        ["strength.debil"] = "Слабый",
        ["strength.media"] = "Средний",
        ["strength.fuerte"] = "Сильный",
        ["strength.muyFuerte"] = "Очень сильный",

        // ===== crypto =====
        ["crypto.archivoDanado"] = "Неверный пароль или повреждённый файл.",
        ["crypto.rutaNoExiste"] = "Путь не существует.",
        ["crypto.tipoEsCarpeta"] = "Этот .enc содержит папку: откройте его в проводнике вместо прямой расшифровки.",
        ["crypto.tipoEsArchivo"] = "Этот .enc — отдельный файл: расшифруйте его напрямую вместо просмотра.",
        ["crypto.archivoNoManifiesto"] = "Файл отсутствует в манифесте.",
        ["crypto.seleccionarArchivoEnc"] = "Для расшифровки выберите файл .enc, а не папку.",

        // ===== main (MainWindow) =====
        ["main.tagline"] = "Шифрование файлов AES-256-GCM",
        ["main.archivo.etiqueta"] = "ФАЙЛ",
        ["main.btn.elegirCarpeta"] = "Папка",
        ["main.archivo.placeholder"] = "Перетащите файл(ы) или папку сюда, или нажмите, чтобы выбрать",
        ["main.archivo.carpeta"] = "Папка: {0}",
        ["main.archivo.variosSeleccionados"] = "Выбрано файлов: {0}",
        ["main.dialog.abrir.titulo"] = "Выбрать файл(ы)",
        ["main.dialog.abrir.filtroEtiqueta"] = "Все файлы (*.*)",
        ["main.dialog.guardar.titulo"] = "Сохранить как",
        ["main.dialog.guardar.filtroEtiqueta"] = "Зашифрованный файл (*.enc)",
        ["main.dialog.guardar.nombreVarios"] = "{0} файлов.enc",
        ["main.error.carpetaUnaPorVez"] = "Для папок выбирайте по одной за раз (нельзя сочетать с другими файлами).",
        ["main.error.soloUnArchivoEnc"] = "Для расшифровки выберите один файл .enc.",
        ["main.error.noEsCarpeta"] = "Для расшифровки выберите файл .enc (не папку).",
        ["main.error.seleccionInvalida"] = "Выберите допустимый файл или папку.",
        ["main.tooltip.acerca"] = "О программе",

        // ===== quick (QuickWindow) =====
        ["quick.archivo.archivo"] = "Файл: {0}",

        // ===== explorer (ExplorerWindow) =====
        ["explorer.titulo"] = "Зашифрованное содержимое",
        ["explorer.instrucciones"] = "Дважды щёлкните файл, чтобы открыть его. Ничего не сохраняется, пока вы не извлечёте выбранные файлы.",
        ["explorer.btn.seleccionarTodo"] = "Выбрать всё",
        ["explorer.btn.deseleccionarTodo"] = "Снять выделение",
        ["explorer.btn.cancelar"] = "Отмена",
        ["explorer.btn.extraerTodo"] = "Извлечь всё ({0})",
        ["explorer.btn.extraerSeleccionados"] = "Извлечь выбранные ({0})",
        ["explorer.subtitulo_uno"] = "1 файл",
        ["explorer.subtitulo_otros"] = "Файлов: {0}",
        ["explorer.tooltip.dobleClic"] = "Двойной клик, чтобы открыть",
        ["explorer.estado.abriendo"] = "   (открывается...)",
        ["explorer.estado.errorAlAbrir"] = "   (не удалось открыть: {0})",
        ["explorer.error.noSePudoAbrir"] = "Не удалось открыть файл: {0}",

        // ===== about (AboutWindow) =====
        ["about.desarrollador"] = "Разработчик приложения",
        ["about.tagline"] = "Шифрование и расшифровка файлов с помощью AES-256-GCM",
        ["about.version"] = "Версия 1.0 · Avalonia UI",
        ["about.cerrar"] = "Закрыть",
    };
}
