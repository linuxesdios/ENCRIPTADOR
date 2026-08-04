namespace Encriptador.Services;

public static partial class Loc
{
    private static readonly Dictionary<string, string> Es = new()
    {
        // ===== common =====
        ["common.mostrar"] = "Mostrar",
        ["common.ocultar"] = "Ocultar",
        ["common.encriptar"] = "Encriptar",
        ["common.desencriptar"] = "Desencriptar",
        ["common.contrasena"] = "CONTRASEÑA",
        ["common.repetirContrasena"] = "REPETIR CONTRASEÑA",
        ["common.chk.borradoSeguro"] = "Borrado seguro (sobrescribir antes de borrar)",
        ["common.chk.conservarOriginal.encriptar"] = "Conservar el archivo original (no borrarlo al terminar)",
        ["common.chk.conservarOriginal.desencriptar"] = "Conservar el archivo encriptado .enc (no borrarlo al terminar)",
        ["common.estado.passwordRequerida"] = "Ingresá una contraseña.",
        ["common.estado.passwordsNoCoinciden"] = "Las contraseñas no coinciden.",
        ["common.estado.procesando"] = "Procesando...",
        ["common.estado.progreso"] = "{0}%",
        ["common.estado.progresoConEta"] = "{0}% · quedan {1}",
        ["common.estado.archivoDesencriptado"] = "Archivo desencriptado correctamente.",
        ["common.estado.archivoEncriptado"] = "Archivo encriptado correctamente.",
        ["common.estado.carpetaEncriptada"] = "Carpeta encriptada en un solo archivo ({0} archivos incluidos).",
        ["common.estado.variosEncriptados"] = "{0} archivos encriptados en un solo paquete.",
        ["common.estado.carpetaRestaurada"] = "Carpeta restaurada: {0} de {1} archivos.",
        ["common.estado.operacionCancelada"] = "Operación cancelada. No se modificó nada.",
        ["common.estado.verificandoPassword"] = "Verificando contraseña...",
        ["common.estado.noPudoDesencriptar"] = "No se pudo desencriptar: contraseña incorrecta o archivo dañado.",
        ["common.estado.error"] = "Error: {0}",
        ["common.footer.credito"] = "Realizado por {0}",
        ["common.contextmenu.encriptarCon"] = "Encriptar con Encriptador",
        ["common.contextmenu.progIdNombre"] = "Archivo encriptado (Encriptador)",

        // ===== strength =====
        ["strength.muyDebil"] = "Muy débil",
        ["strength.debil"] = "Débil",
        ["strength.media"] = "Media",
        ["strength.fuerte"] = "Fuerte",
        ["strength.muyFuerte"] = "Muy fuerte",

        // ===== crypto =====
        ["crypto.archivoDanado"] = "Contraseña incorrecta o archivo dañado.",
        ["crypto.rutaNoExiste"] = "La ruta no existe.",
        ["crypto.tipoEsCarpeta"] = "Este .enc contiene una carpeta: abrilo con el explorador en vez de desencriptarlo directo.",
        ["crypto.tipoEsArchivo"] = "Este .enc es un archivo individual: desencriptalo directo en vez de explorarlo.",
        ["crypto.archivoNoManifiesto"] = "El archivo no está en el manifiesto.",
        ["crypto.seleccionarArchivoEnc"] = "Para desencriptar seleccioná el archivo .enc, no la carpeta.",

        // ===== main (MainWindow) =====
        ["main.tagline"] = "Cifrado AES-256-GCM de archivos",
        ["main.archivo.etiqueta"] = "ARCHIVO",
        ["main.btn.elegirCarpeta"] = "Carpeta",
        ["main.archivo.placeholder"] = "Arrastrá archivo(s) o una carpeta aquí, o hacé clic para elegir",
        ["main.archivo.carpeta"] = "Carpeta: {0}",
        ["main.archivo.variosSeleccionados"] = "{0} archivos seleccionados",
        ["main.dialog.abrir.titulo"] = "Seleccionar archivo(s)",
        ["main.dialog.abrir.filtroEtiqueta"] = "Todos los archivos (*.*)",
        ["main.dialog.guardar.titulo"] = "Guardar como",
        ["main.dialog.guardar.filtroEtiqueta"] = "Archivo encriptado (*.enc)",
        ["main.dialog.guardar.nombreVarios"] = "{0} archivos.enc",
        ["main.error.carpetaUnaPorVez"] = "Para carpetas, elegí una por vez (no se puede combinar con otros archivos).",
        ["main.error.soloUnArchivoEnc"] = "Para desencriptar, elegí un solo archivo .enc.",
        ["main.error.noEsCarpeta"] = "Para desencriptar, elegí un archivo .enc (no una carpeta).",
        ["main.error.seleccionInvalida"] = "Seleccioná un archivo o carpeta válido.",
        ["main.tooltip.acerca"] = "Acerca de",

        // ===== quick (QuickWindow) =====
        ["quick.archivo.archivo"] = "Archivo: {0}",

        // ===== explorer (ExplorerWindow) =====
        ["explorer.titulo"] = "Contenido encriptado",
        ["explorer.instrucciones"] = "Doble clic en un archivo para abrirlo. No se guarda nada en destino hasta que extraigas los seleccionados.",
        ["explorer.btn.seleccionarTodo"] = "Seleccionar todo",
        ["explorer.btn.deseleccionarTodo"] = "Deseleccionar todo",
        ["explorer.btn.cancelar"] = "Cancelar",
        ["explorer.btn.extraerTodo"] = "Extraer todo ({0})",
        ["explorer.btn.extraerSeleccionados"] = "Extraer seleccionados ({0})",
        ["explorer.subtitulo_uno"] = "1 archivo",
        ["explorer.subtitulo_otros"] = "{0} archivos",
        ["explorer.tooltip.dobleClic"] = "Doble clic para abrir",
        ["explorer.estado.abriendo"] = "   (abriendo...)",
        ["explorer.error.noSePudoAbrir"] = "No se pudo abrir el archivo: {0}",

        // ===== about (AboutWindow) =====
        ["about.desarrollador"] = "Desarrollador de la aplicación",
        ["about.tagline"] = "Cifrado y descifrado de archivos con AES-256",
        ["about.version"] = "Versión 1.0",
        ["about.cerrar"] = "Cerrar",
    };
}
