package com.pablomartin.encriptador

internal object LocalizationEn {
    val mapa: Map<String, String> = mapOf(
        // ===== common =====
        "common.mostrar" to "Show",
        "common.ocultar" to "Hide",
        "common.contrasena" to "PASSWORD",
        "common.repetirContrasena" to "CONFIRM PASSWORD",
        "common.footer.credito" to "Made by %s",
        "common.estado.passwordRequerida" to "Enter a password.",
        "common.estado.passwordsNoCoinciden" to "Passwords don't match.",
        "common.estado.archivoEncriptado" to "File encrypted successfully.",
        "common.estado.archivoDesencriptado" to "File decrypted successfully.",
        "common.estado.verificandoPassword" to "Verifying password...",
        "common.estado.error" to "Error: %s",

        // ===== strength =====
        "strength.muyDebil" to "Very weak",
        "strength.debil" to "Weak",
        "strength.media" to "Medium",
        "strength.fuerte" to "Strong",
        "strength.muyFuerte" to "Very strong",

        // ===== crypto =====
        "crypto.archivoDanado" to "Wrong password or corrupted file.",
        "crypto.tipoEsCarpeta" to "This .enc contains a folder.",
        "crypto.tipoEsArchivo" to "This .enc is a single file.",

        // ===== pantalla (MainActivity) =====
        "pantalla.tagline" to "AES-256-GCM file encryption",
        "pantalla.archivoOCarpeta" to "FILE OR FOLDER",
        "pantalla.btnArchivos" to "📄 File(s)",
        "pantalla.btnCarpeta" to "📁 Folder",
        "pantalla.nadaSeleccionado" to "Nothing selected",
        "pantalla.variosSeleccionados" to "%d files selected",
        "pantalla.carpetaSeleccionada" to "Folder: %s",
        "pantalla.conservarOriginal" to "Keep the original (don't delete it when finished)",
        "pantalla.conservarEnc" to "Keep the .enc (don't delete it when finished)",
        "pantalla.btnEncriptar" to "🔒 Encrypt",
        "pantalla.btnDesencriptar" to "🔑 Decrypt",
        "pantalla.btnElegirCarpetaDestino" to "📂 Choose destination folder and extract",
        "common.estado.progreso" to "%1\$d%%",
        "common.estado.progresoConEta" to "%1\$d%% · %2\$s left",
        "pantalla.elegirDondeGuardar" to "Choose where to save the .enc...",
        "pantalla.sinSeleccion" to "Select file(s) or a folder.",
        "pantalla.variosArchivosEnc" to "%d files.enc",
        "pantalla.variosEncriptados" to "%d files encrypted into a single package.",
        "pantalla.noSePudoAbrirCarpeta" to "Couldn't open the folder.",
        "pantalla.carpetaEncriptadaUnArchivo" to "Folder encrypted into a single file.",
        "pantalla.soloUnArchivoEnc" to "Select a single .enc file.",
        "pantalla.carpetaConArchivos" to "Folder with %d file(s). Choose where to extract it.",
        "pantalla.extrayendo" to "Extracting...",
        "pantalla.noSePudoAbrirDestino" to "Couldn't open the destination.",
        "pantalla.carpetaRestaurada" to "Folder restored: %d file(s).",
        "pantalla.errorAlExtraer" to "Error while extracting: %s",
        "pantalla.noSePudoCrearCarpeta" to "Couldn't create the folder '%s'.",
        "pantalla.noSePudoCrearArchivo" to "Couldn't create the file '%s'.",
        "pantalla.nombreFallbackArchivo" to "file",
        "pantalla.nombreFallbackCarpeta" to "folder",
    )
}
