package com.pablomartin.encriptador

internal object LocalizationEs {
    val mapa: Map<String, String> = mapOf(
        // ===== common =====
        "common.mostrar" to "Mostrar",
        "common.ocultar" to "Ocultar",
        "common.contrasena" to "CONTRASEÑA",
        "common.repetirContrasena" to "REPETIR CONTRASEÑA",
        "common.footer.credito" to "Realizado por %s",
        "common.estado.passwordRequerida" to "Ingresá una contraseña.",
        "common.estado.passwordsNoCoinciden" to "Las contraseñas no coinciden.",
        "common.estado.archivoEncriptado" to "Archivo encriptado correctamente.",
        "common.estado.archivoDesencriptado" to "Archivo desencriptado correctamente.",
        "common.estado.verificandoPassword" to "Verificando contraseña...",
        "common.estado.error" to "Error: %s",

        // ===== strength =====
        "strength.muyDebil" to "Muy débil",
        "strength.debil" to "Débil",
        "strength.media" to "Media",
        "strength.fuerte" to "Fuerte",
        "strength.muyFuerte" to "Muy fuerte",

        // ===== crypto =====
        "crypto.archivoDanado" to "Contraseña incorrecta o archivo dañado.",
        "crypto.tipoEsCarpeta" to "Este .enc contiene una carpeta.",
        "crypto.tipoEsArchivo" to "Este .enc es un archivo individual.",

        // ===== pantalla (MainActivity) =====
        "pantalla.tagline" to "Cifrado AES-256-GCM de archivos",
        "pantalla.archivoOCarpeta" to "ARCHIVO O CARPETA",
        "pantalla.btnArchivos" to "📄 Archivo(s)",
        "pantalla.btnCarpeta" to "📁 Carpeta",
        "pantalla.nadaSeleccionado" to "Nada seleccionado",
        "pantalla.variosSeleccionados" to "%d archivos seleccionados",
        "pantalla.carpetaSeleccionada" to "Carpeta: %s",
        "pantalla.conservarOriginal" to "Conservar el original (no borrarlo al terminar)",
        "pantalla.conservarEnc" to "Conservar el .enc (no borrarlo al terminar)",
        "pantalla.btnEncriptar" to "🔒 Encriptar",
        "pantalla.btnDesencriptar" to "🔑 Desencriptar",
        "pantalla.btnElegirCarpetaDestino" to "📂 Elegir carpeta destino y extraer",
        "pantalla.progresoArchivo" to "Archivo %1\$d de %2\$d",
        "pantalla.elegirDondeGuardar" to "Elegí dónde guardar el .enc...",
        "pantalla.sinSeleccion" to "Seleccioná archivo(s) o una carpeta.",
        "pantalla.variosArchivosEnc" to "%d archivos.enc",
        "pantalla.variosEncriptados" to "%d archivos encriptados en un solo paquete.",
        "pantalla.noSePudoAbrirCarpeta" to "No se pudo abrir la carpeta.",
        "pantalla.carpetaEncriptadaUnArchivo" to "Carpeta encriptada en un solo archivo.",
        "pantalla.soloUnArchivoEnc" to "Seleccioná un solo archivo .enc.",
        "pantalla.carpetaConArchivos" to "Carpeta con %d archivo(s). Elegí dónde extraerla.",
        "pantalla.extrayendo" to "Extrayendo...",
        "pantalla.noSePudoAbrirDestino" to "No se pudo abrir el destino.",
        "pantalla.carpetaRestaurada" to "Carpeta restaurada: %d archivo(s).",
        "pantalla.errorAlExtraer" to "Error al extraer: %s",
        "pantalla.noSePudoCrearCarpeta" to "No se pudo crear la carpeta '%s'.",
        "pantalla.noSePudoCrearArchivo" to "No se pudo crear el archivo '%s'.",
        "pantalla.nombreFallbackArchivo" to "archivo",
        "pantalla.nombreFallbackCarpeta" to "carpeta",
    )
}
