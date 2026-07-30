namespace Encriptador.Services;

public static partial class Loc
{
    private static readonly Dictionary<string, string> En = new()
    {
        // ===== common =====
        ["common.mostrar"] = "Show",
        ["common.ocultar"] = "Hide",
        ["common.encriptar"] = "Encrypt",
        ["common.desencriptar"] = "Decrypt",
        ["common.contrasena"] = "PASSWORD",
        ["common.repetirContrasena"] = "CONFIRM PASSWORD",
        ["common.chk.borradoSeguro"] = "Secure delete (overwrite before deleting)",
        ["common.chk.conservarOriginal.encriptar"] = "Keep the original file (don't delete it when finished)",
        ["common.chk.conservarOriginal.desencriptar"] = "Keep the encrypted .enc file (don't delete it when finished)",
        ["common.estado.passwordRequerida"] = "Enter a password.",
        ["common.estado.passwordsNoCoinciden"] = "Passwords don't match.",
        ["common.estado.procesando"] = "Processing...",
        ["common.estado.procesandoArchivo"] = "Processing file {0} of {1}...",
        ["common.estado.archivoDesencriptado"] = "File decrypted successfully.",
        ["common.estado.archivoEncriptado"] = "File encrypted successfully.",
        ["common.estado.carpetaEncriptada"] = "Folder encrypted into a single file ({0} files included).",
        ["common.estado.variosEncriptados"] = "{0} files encrypted into a single package.",
        ["common.estado.carpetaRestaurada"] = "Folder restored: {0} of {1} files.",
        ["common.estado.operacionCancelada"] = "Operation cancelled. Nothing was changed.",
        ["common.estado.verificandoPassword"] = "Verifying password...",
        ["common.estado.noPudoDesencriptar"] = "Couldn't decrypt: wrong password or corrupted file.",
        ["common.estado.error"] = "Error: {0}",
        ["common.footer.credito"] = "Made by {0}",
        ["common.contextmenu.encriptarCon"] = "Encrypt with Encriptador",
        ["common.contextmenu.progIdNombre"] = "Encrypted file (Encriptador)",

        // ===== strength =====
        ["strength.muyDebil"] = "Very weak",
        ["strength.debil"] = "Weak",
        ["strength.media"] = "Medium",
        ["strength.fuerte"] = "Strong",
        ["strength.muyFuerte"] = "Very strong",

        // ===== crypto =====
        ["crypto.archivoDanado"] = "Wrong password or corrupted file.",
        ["crypto.rutaNoExiste"] = "The path doesn't exist.",
        ["crypto.tipoEsCarpeta"] = "This .enc contains a folder: open it with the explorer instead of decrypting it directly.",
        ["crypto.tipoEsArchivo"] = "This .enc is a single file: decrypt it directly instead of exploring it.",
        ["crypto.archivoNoManifiesto"] = "The file isn't in the manifest.",
        ["crypto.seleccionarArchivoEnc"] = "To decrypt, select the .enc file, not the folder.",

        // ===== main (MainWindow) =====
        ["main.tagline"] = "AES-256-GCM file encryption",
        ["main.archivo.etiqueta"] = "FILE",
        ["main.archivo.placeholder"] = "Drag file(s) or a folder here, or click to choose",
        ["main.archivo.carpeta"] = "Folder: {0}",
        ["main.archivo.variosSeleccionados"] = "{0} files selected",
        ["main.dialog.abrir.titulo"] = "Select file(s)",
        ["main.dialog.abrir.filtroEtiqueta"] = "All files (*.*)",
        ["main.dialog.guardar.titulo"] = "Save as",
        ["main.dialog.guardar.filtroEtiqueta"] = "Encrypted file (*.enc)",
        ["main.dialog.guardar.nombreVarios"] = "{0} files.enc",
        ["main.error.carpetaUnaPorVez"] = "For folders, choose one at a time (can't be combined with other files).",
        ["main.error.soloUnArchivoEnc"] = "To decrypt, choose a single .enc file.",
        ["main.error.noEsCarpeta"] = "To decrypt, choose an .enc file (not a folder).",
        ["main.error.seleccionInvalida"] = "Select a valid file or folder.",
        ["main.tooltip.acerca"] = "About",

        // ===== quick (QuickWindow) =====
        ["quick.archivo.archivo"] = "File: {0}",

        // ===== explorer (ExplorerWindow) =====
        ["explorer.titulo"] = "Encrypted content",
        ["explorer.instrucciones"] = "Double-click a file to open it. Nothing is saved to disk until you extract the selected files.",
        ["explorer.btn.seleccionarTodo"] = "Select all",
        ["explorer.btn.deseleccionarTodo"] = "Deselect all",
        ["explorer.btn.cancelar"] = "Cancel",
        ["explorer.btn.extraerTodo"] = "Extract all ({0})",
        ["explorer.btn.extraerSeleccionados"] = "Extract selected ({0})",
        ["explorer.subtitulo_uno"] = "1 file",
        ["explorer.subtitulo_otros"] = "{0} files",
        ["explorer.tooltip.dobleClic"] = "Double-click to open",
        ["explorer.estado.abriendo"] = "   (opening...)",
        ["explorer.estado.errorAlAbrir"] = "   (couldn't open: {0})",
        ["explorer.error.noSePudoAbrir"] = "Couldn't open the file: {0}",

        // ===== about (AboutWindow) =====
        ["about.desarrollador"] = "App developer",
        ["about.tagline"] = "AES-256-GCM file encryption and decryption",
        ["about.version"] = "Version 1.0 · Avalonia UI",
        ["about.cerrar"] = "Close",
    };
}
