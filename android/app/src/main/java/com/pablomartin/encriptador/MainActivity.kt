package com.pablomartin.encriptador

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.provider.DocumentsContract
import android.provider.OpenableColumns
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.documentfile.provider.DocumentFile
import com.pablomartin.encriptador.ui.theme.*
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.security.GeneralSecurityException

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        Preferencias.inicializar(this)
        Localization.inicializar(Preferencias.cargarIdioma())

        // "Abrir con" / doble tap sobre un .enc desde el explorador de archivos del teléfono.
        val uriInicial: Uri? = intent?.takeIf { it.action == Intent.ACTION_VIEW }?.data

        setContent {
            EncriptadorTheme {
                Surface(color = MaterialTheme.colorScheme.background) {
                    PantallaPrincipal(uriInicial = uriInicial)
                }
            }
        }
    }
}

// ===================== Modelo de selección =====================

private sealed class Seleccion {
    data object Ninguna : Seleccion()
    data class Archivos(val uris: List<Uri>, val nombres: List<String>) : Seleccion()
    data class Carpeta(val treeUri: Uri, val nombre: String) : Seleccion()
}

private fun Seleccion.esModoEncriptar(): Boolean = when (this) {
    is Seleccion.Ninguna -> true
    is Seleccion.Carpeta -> true
    is Seleccion.Archivos -> !(uris.size == 1 && CryptoService.esArchivoEncriptado(nombres[0]))
}

private enum class TipoEstado { INFO, EXITO, ERROR }

@Composable
private fun PantallaPrincipal(uriInicial: Uri?) {
    val context = LocalContext.current
    val resolver = context.contentResolver
    val scope = rememberCoroutineScope()

    var seleccion by remember { mutableStateOf<Seleccion>(Seleccion.Ninguna) }
    var password by remember { mutableStateOf("") }
    var confirmar by remember { mutableStateOf("") }
    var mostrarPassword by remember { mutableStateOf(false) }
    var conservarOriginal by remember { mutableStateOf(false) }
    var procesando by remember { mutableStateOf(false) }
    var progreso by remember { mutableStateOf<Pair<Long, Long>?>(null) }
    var inicioOperacion by remember { mutableStateOf(0L) }
    var mensaje by remember { mutableStateOf<String?>(null) }
    var tipoMensaje by remember { mutableStateOf(TipoEstado.INFO) }

    // Confirmación pendiente para extraer una carpeta encriptada (SesionCarpeta ya validada).
    var sesionPendiente by remember { mutableStateOf<CryptoService.SesionCarpeta?>(null) }
    var uriPendiente by remember { mutableStateOf<Uri?>(null) }

    fun mostrar(texto: String, tipo: TipoEstado) {
        mensaje = texto
        tipoMensaje = tipo
    }

    fun limpiarSeleccion(nueva: Seleccion) {
        seleccion = nueva
        mensaje = null
        sesionPendiente = null
        uriPendiente = null
    }

    val lanzadorArchivos = rememberLauncherForActivityResult(ActivityResultContracts.OpenMultipleDocuments()) { uris ->
        if (uris.isNotEmpty()) {
            val nombres = uris.map { nombreDeUri(context, it) }
            limpiarSeleccion(Seleccion.Archivos(uris, nombres))
        }
    }
    val lanzadorCarpeta = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocumentTree()) { uri ->
        if (uri != null) {
            val nombre = DocumentFile.fromTreeUri(context, uri)?.name ?: Localization.t("pantalla.nombreFallbackCarpeta")
            limpiarSeleccion(Seleccion.Carpeta(uri, nombre))
        }
    }

    // Puente entre el selector "Guardar como" (basado en callback) y las funciones suspend
    // que necesitan esperar el resultado antes de seguir escribiendo el .enc.
    var destinoDeferred by remember { mutableStateOf<CompletableDeferred<Uri?>?>(null) }
    val lanzadorDestino = rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("*/*")) { uri ->
        destinoDeferred?.complete(uri)
        destinoDeferred = null
    }
    suspend fun pedirDestino(nombreSugerido: String): Uri? {
        val deferred = CompletableDeferred<Uri?>()
        destinoDeferred = deferred
        lanzadorDestino.launch(nombreSugerido)
        return deferred.await()
    }

    val esModoEncriptar = seleccion.esModoEncriptar()

    // --- Manejo de "Abrir con" al iniciar ---
    LaunchedEffect(uriInicial) {
        if (uriInicial != null) {
            val nombre = nombreDeUri(context, uriInicial)
            limpiarSeleccion(Seleccion.Archivos(listOf(uriInicial), listOf(nombre)))
        }
    }

    fun ejecutarEncriptar() {
        val sel = seleccion
        if (password.isEmpty()) { mostrar(Localization.t("common.estado.passwordRequerida"), TipoEstado.ERROR); return }
        if (password != confirmar) { mostrar(Localization.t("common.estado.passwordsNoCoinciden"), TipoEstado.ERROR); return }
        if (sel is Seleccion.Ninguna) { mostrar(Localization.t("pantalla.sinSeleccion"), TipoEstado.ERROR); return }

        scope.launch {
            procesando = true
            progreso = null
            inicioOperacion = System.currentTimeMillis()
            mostrar(Localization.t("pantalla.elegirDondeGuardar"), TipoEstado.INFO)
            try {
                when (sel) {
                    is Seleccion.Archivos -> {
                        if (sel.uris.size == 1) {
                            val uri = sel.uris[0]
                            val nombre = sel.nombres[0]
                            val longitud = tamanoDeUri(context, uri)
                            val destinoUri = pedirDestino("$nombre.enc") ?: run { procesando = false; return@launch }
                            withContext(Dispatchers.IO) {
                                resolver.openInputStream(uri)!!.use { entrada ->
                                    resolver.openOutputStream(destinoUri)!!.use { salida ->
                                        CryptoService.encriptarArchivo(entrada, longitud, salida, password) { a, t -> progreso = a to t }
                                    }
                                }
                                if (!conservarOriginal) borrarUri(context, uri)
                            }
                            mostrar(Localization.t("common.estado.archivoEncriptado"), TipoEstado.EXITO)
                        } else {
                            val destinoUri = pedirDestino(Localization.t("pantalla.variosArchivosEnc", sel.uris.size)) ?: run { procesando = false; return@launch }
                            withContext(Dispatchers.IO) {
                                val fuentes = sel.uris.mapIndexed { i, uri ->
                                    CryptoService.FuenteArchivo(sel.nombres[i], tamanoDeUri(context, uri)) {
                                        resolver.openInputStream(uri)!!
                                    }
                                }
                                resolver.openOutputStream(destinoUri)!!.use { salida ->
                                    CryptoService.encriptarCarpeta(fuentes, salida, password) { a, t -> progreso = a to t }
                                }
                                if (!conservarOriginal) sel.uris.forEach { borrarUri(context, it) }
                            }
                            mostrar(Localization.t("pantalla.variosEncriptados", sel.uris.size), TipoEstado.EXITO)
                        }
                    }

                    is Seleccion.Carpeta -> {
                        val raiz = DocumentFile.fromTreeUri(context, sel.treeUri)
                            ?: throw IllegalStateException(Localization.t("pantalla.noSePudoAbrirCarpeta"))
                        val destinoUri = pedirDestino("${sel.nombre}.enc") ?: run { procesando = false; return@launch }
                        withContext(Dispatchers.IO) {
                            val archivos = ArrayList<Pair<String, DocumentFile>>()
                            listarRecursivo(raiz, "", archivos)
                            val fuentes = archivos.map { (relativa, doc) ->
                                CryptoService.FuenteArchivo(relativa, doc.length()) { resolver.openInputStream(doc.uri)!! }
                            }
                            resolver.openOutputStream(destinoUri)!!.use { salida ->
                                CryptoService.encriptarCarpeta(fuentes, salida, password) { a, t -> progreso = a to t }
                            }
                            if (!conservarOriginal) raiz.delete()
                        }
                        mostrar(Localization.t("pantalla.carpetaEncriptadaUnArchivo"), TipoEstado.EXITO)
                    }

                    Seleccion.Ninguna -> Unit
                }
            } catch (ex: Exception) {
                mostrar(Localization.t("common.estado.error", ex.message ?: ""), TipoEstado.ERROR)
            } finally {
                procesando = false
                progreso = null
            }
        }
    }

    fun ejecutarDesencriptar() {
        val sel = seleccion
        if (sel !is Seleccion.Archivos || sel.uris.size != 1) {
            mostrar(Localization.t("pantalla.soloUnArchivoEnc"), TipoEstado.ERROR); return
        }
        if (password.isEmpty()) { mostrar(Localization.t("common.estado.passwordRequerida"), TipoEstado.ERROR); return }

        val uri = sel.uris[0]
        scope.launch {
            procesando = true
            progreso = null
            inicioOperacion = System.currentTimeMillis()
            mostrar(Localization.t("common.estado.verificandoPassword"), TipoEstado.INFO)
            try {
                val esCarpeta = withContext(Dispatchers.IO) {
                    resolver.openInputStream(uri)!!.use { CryptoService.esContenedorDeCarpeta(it) }
                }

                if (!esCarpeta) {
                    val nombreDestino = sel.nombres[0].removeSuffix(CryptoService.EXTENSION)
                    val destinoUri = pedirDestino(nombreDestino) ?: run { procesando = false; mensaje = null; return@launch }
                    withContext(Dispatchers.IO) {
                        resolver.openInputStream(uri)!!.use { entrada ->
                            resolver.openOutputStream(destinoUri)!!.use { salida ->
                                CryptoService.desencriptarArchivo(entrada, salida, password) { a, t -> progreso = a to t }
                            }
                        }
                        if (!conservarOriginal) borrarUri(context, uri)
                    }
                    mostrar(Localization.t("common.estado.archivoDesencriptado"), TipoEstado.EXITO)
                } else {
                    val sesion = withContext(Dispatchers.IO) {
                        resolver.openInputStream(uri)!!.use { CryptoService.abrirCarpeta(it, password) }
                    }
                    sesionPendiente = sesion
                    uriPendiente = uri
                    mostrar(Localization.t("pantalla.carpetaConArchivos", sesion.entradas.size), TipoEstado.INFO)
                }
            } catch (ex: GeneralSecurityException) {
                mostrar(Localization.t("crypto.archivoDanado"), TipoEstado.ERROR)
            } catch (ex: Exception) {
                mostrar(Localization.t("common.estado.error", ex.message ?: ""), TipoEstado.ERROR)
            } finally {
                procesando = false
            }
        }
    }

    val lanzadorDestinoCarpeta = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocumentTree()) { destinoTreeUri ->
        val sesion = sesionPendiente
        val uriEnc = uriPendiente
        if (destinoTreeUri != null && sesion != null && uriEnc != null) {
            scope.launch {
                procesando = true
                progreso = null
                inicioOperacion = System.currentTimeMillis()
                mostrar(Localization.t("pantalla.extrayendo"), TipoEstado.INFO)
                try {
                    val raizDestino = DocumentFile.fromTreeUri(context, destinoTreeUri)
                        ?: throw IllegalStateException(Localization.t("pantalla.noSePudoAbrirDestino"))
                    withContext(Dispatchers.IO) {
                        CryptoService.extraerTodo(
                            reabrirEntrada = { resolver.openInputStream(uriEnc)!! },
                            sesion = sesion,
                            crearSalida = { entrada ->
                                val archivoDestino = crearEnArbol(raizDestino, entrada.relativa)
                                resolver.openOutputStream(archivoDestino.uri)!!
                            },
                            progreso = { a, t -> progreso = a to t },
                        )
                        if (!conservarOriginal) borrarUri(context, uriEnc)
                    }
                    mostrar(Localization.t("pantalla.carpetaRestaurada", sesion.entradas.size), TipoEstado.EXITO)
                } catch (ex: Exception) {
                    mostrar(Localization.t("pantalla.errorAlExtraer", ex.message ?: ""), TipoEstado.ERROR)
                } finally {
                    procesando = false
                    progreso = null
                    sesionPendiente = null
                    uriPendiente = null
                }
            }
        }
    }

    Column(
        Modifier
            .fillMaxSize()
            .padding(20.dp)
            .verticalScroll(rememberScrollState())
    ) {
        // ---- Encabezado ----
        Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(bottom = 18.dp)) {
            Box(
                Modifier
                    .size(40.dp)
                    .clip(RoundedCornerShape(10.dp))
                    .background(BrushAcento()),
                contentAlignment = Alignment.Center,
            ) { Text("🔒", fontSize = 18.sp) }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Text("Encriptador", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = ColorTextoPrincipal)
                Text(Localization.t("pantalla.tagline"), fontSize = 11.sp, color = ColorTextoSecundario)
            }
            SelectorIdioma()
        }

        Card(
            colors = CardDefaults.cardColors(containerColor = ColorFondoCard),
            border = BorderStroke(1.dp, ColorBorde),
            shape = RoundedCornerShape(16.dp),
        ) {
            Column(Modifier.padding(18.dp)) {

                Etiqueta(Localization.t("pantalla.archivoOCarpeta"))
                Row(Modifier.padding(top = 6.dp, bottom = 4.dp)) {
                    BotonSecundario(Localization.t("pantalla.btnArchivos"), Modifier.weight(1f)) {
                        lanzadorArchivos.launch(arrayOf("*/*"))
                    }
                    Spacer(Modifier.width(8.dp))
                    BotonSecundario(Localization.t("pantalla.btnCarpeta"), Modifier.weight(1f)) {
                        lanzadorCarpeta.launch(null)
                    }
                }
                Text(
                    text = when (val sel = seleccion) {
                        is Seleccion.Ninguna -> Localization.t("pantalla.nadaSeleccionado")
                        is Seleccion.Archivos -> if (sel.uris.size == 1) sel.nombres[0] else Localization.t("pantalla.variosSeleccionados", sel.uris.size)
                        is Seleccion.Carpeta -> Localization.t("pantalla.carpetaSeleccionada", sel.nombre)
                    },
                    fontSize = 12.5.sp,
                    color = ColorTextoSecundario,
                    modifier = Modifier.padding(top = 4.dp, bottom = 14.dp),
                )

                Row(verticalAlignment = Alignment.CenterVertically) {
                    Etiqueta(Localization.t("common.contrasena"), Modifier.weight(1f))
                    BotonSecundario(Localization.t(if (mostrarPassword) "common.ocultar" else "common.mostrar"), chico = true) {
                        mostrarPassword = !mostrarPassword
                    }
                }
                Spacer(Modifier.height(6.dp))
                CampoPassword(password, { password = it }, mostrarPassword)

                if (esModoEncriptar) {
                    Etiqueta(Localization.t("common.repetirContrasena"), Modifier.padding(top = 14.dp, bottom = 6.dp))
                    CampoPassword(confirmar, { confirmar = it }, mostrarPassword)

                    val nivel = PasswordStrength.evaluar(password)
                    val segmentos = PasswordStrength.segmentos(nivel, password.isEmpty())
                    Row(Modifier.padding(top = 10.dp), verticalAlignment = Alignment.CenterVertically) {
                        repeat(4) { i ->
                            Box(
                                Modifier
                                    .weight(1f)
                                    .height(4.dp)
                                    .padding(end = if (i < 3) 4.dp else 0.dp)
                                    .clip(RoundedCornerShape(2.dp))
                                    .background(if (i < segmentos) colorFuerza(nivel) else ColorFondoCampo)
                            )
                        }
                    }
                    if (password.isNotEmpty()) {
                        Text(PasswordStrength.texto(nivel), fontSize = 10.sp, color = ColorTextoSecundario, modifier = Modifier.padding(top = 5.dp))
                    }
                }

                Row(
                    Modifier
                        .padding(top = 14.dp)
                        .clickable { conservarOriginal = !conservarOriginal },
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Checkbox(checked = conservarOriginal, onCheckedChange = { conservarOriginal = it })
                    Text(
                        Localization.t(if (esModoEncriptar) "pantalla.conservarOriginal" else "pantalla.conservarEnc"),
                        fontSize = 12.sp, color = ColorTextoSecundario,
                    )
                }

                Spacer(Modifier.height(16.dp))
                Row {
                    BotonPrimario(Localization.t("pantalla.btnEncriptar"), Modifier.weight(1f), BrushAcento(), habilitado = !procesando) { ejecutarEncriptar() }
                    Spacer(Modifier.width(10.dp))
                    BotonPrimario(Localization.t("pantalla.btnDesencriptar"), Modifier.weight(1f), BrushExito(), habilitado = !procesando) { ejecutarDesencriptar() }
                }

                if (sesionPendiente != null) {
                    Spacer(Modifier.height(12.dp))
                    BotonPrimario(Localization.t("pantalla.btnElegirCarpetaDestino"), Modifier.fillMaxWidth(), BrushAcento(), habilitado = !procesando) {
                        lanzadorDestinoCarpeta.launch(null)
                    }
                }

                if (procesando) {
                    Spacer(Modifier.height(14.dp))
                    val p = progreso
                    if (p != null && p.second > 0L) {
                        LinearProgressIndicator(
                            progress = { p.first.toFloat() / p.second.toFloat() },
                            modifier = Modifier.fillMaxWidth(),
                            color = ColorAcento1, trackColor = ColorFondoCampo,
                        )
                        val porcentaje = (p.first * 100.0 / p.second).toInt()
                        val elapsed = (System.currentTimeMillis() - inicioOperacion) / 1000.0
                        val texto = if (elapsed > 0.3 && p.first > 0) {
                            val throughput = p.first / elapsed
                            val etaSegundos = ((p.second - p.first) / throughput).toLong()
                            Localization.t("common.estado.progresoConEta", porcentaje, formatearTiempo(etaSegundos))
                        } else {
                            Localization.t("common.estado.progreso", porcentaje)
                        }
                        Text(texto, fontSize = 11.sp, color = ColorTextoSecundario, modifier = Modifier.padding(top = 6.dp))
                    } else {
                        LinearProgressIndicator(modifier = Modifier.fillMaxWidth(), color = ColorAcento1, trackColor = ColorFondoCampo)
                    }
                }

                mensaje?.let {
                    Spacer(Modifier.height(12.dp))
                    Text(
                        it, fontSize = 12.sp,
                        color = when (tipoMensaje) {
                            TipoEstado.EXITO -> ColorExito
                            TipoEstado.ERROR -> ColorError
                            TipoEstado.INFO -> ColorTextoSecundario
                        },
                    )
                }
            }
        }

        Spacer(Modifier.height(16.dp))
        Text(
            Localization.t("common.footer.credito", "Pablo Martín Fernández"),
            fontSize = 10.5.sp, color = ColorTextoSecundario,
            modifier = Modifier.padding(start = 4.dp),
        )
    }
}

// ===================== Componentes reutilizables =====================

@Composable
private fun SelectorIdioma() {
    Row {
        listOf(Idioma.ES to "ES", Idioma.EN to "EN", Idioma.RU to "RU", Idioma.ZH to "中").forEach { (idioma, etiqueta) ->
            val activo = Localization.idiomaActual == idioma
            Box(
                Modifier
                    .padding(start = 4.dp)
                    .clip(RoundedCornerShape(6.dp))
                    .background(if (activo) ColorAcento1 else Color.Transparent)
                    .clickable { Localization.cambiarIdioma(idioma) }
                    .padding(horizontal = 7.dp, vertical = 4.dp),
            ) { Text(etiqueta, fontSize = 10.sp, color = if (activo) Color.White else ColorTextoSecundario) }
        }
    }
}

@Composable
private fun Etiqueta(texto: String, modifier: Modifier = Modifier) {
    Text(texto, fontSize = 10.5.sp, fontWeight = FontWeight.SemiBold, color = ColorTextoSecundario, modifier = modifier)
}

@Composable
private fun CampoPassword(valor: String, onValorCambia: (String) -> Unit, mostrar: Boolean) {
    OutlinedTextField(
        value = valor,
        onValueChange = onValorCambia,
        modifier = Modifier.fillMaxWidth(),
        visualTransformation = if (mostrar) VisualTransformation.None else PasswordVisualTransformation(),
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
        singleLine = true,
        shape = RoundedCornerShape(9.dp),
        colors = OutlinedTextFieldDefaults.colors(
            focusedContainerColor = ColorFondoCampo,
            unfocusedContainerColor = ColorFondoCampo,
            focusedBorderColor = ColorAcento1,
            unfocusedBorderColor = ColorBorde,
            focusedTextColor = ColorTextoPrincipal,
            unfocusedTextColor = ColorTextoPrincipal,
            cursorColor = ColorTextoPrincipal,
        ),
    )
}

@Composable
private fun BotonSecundario(texto: String, modifier: Modifier = Modifier, chico: Boolean = false, onClick: () -> Unit) {
    OutlinedButton(
        onClick = onClick,
        modifier = modifier,
        shape = RoundedCornerShape(9.dp),
        border = BorderStroke(1.dp, ColorBorde),
        colors = ButtonDefaults.outlinedButtonColors(contentColor = ColorTextoPrincipal),
        contentPadding = if (chico) PaddingValues(horizontal = 10.dp, vertical = 4.dp) else ButtonDefaults.ContentPadding,
    ) { Text(texto, fontSize = if (chico) 10.sp else 12.5.sp) }
}

@Composable
private fun BotonPrimario(texto: String, modifier: Modifier, fondo: Brush, habilitado: Boolean, onClick: () -> Unit) {
    Box(
        modifier = modifier
            .clip(RoundedCornerShape(10.dp))
            .background(if (habilitado) fondo else Brush.linearGradient(listOf(ColorBorde, ColorBorde)))
            .clickable(enabled = habilitado, onClick = onClick)
            .padding(vertical = 12.dp),
        contentAlignment = Alignment.Center,
    ) { Text(texto, color = Color.White, fontWeight = FontWeight.SemiBold, fontSize = 14.sp) }
}

@Composable
private fun BrushAcento() = Brush.linearGradient(listOf(ColorAcento1, ColorAcento2))

@Composable
private fun BrushExito() = Brush.linearGradient(listOf(ColorExito, ColorExitoOscuro))

private fun formatearTiempo(segundos: Long): String {
    val total = segundos.coerceAtLeast(0)
    if (total < 60) return "${total}s"
    return "${total / 60}m ${total % 60}s"
}

private fun colorFuerza(nivel: NivelFuerza): Color = when (nivel) {
    NivelFuerza.MUY_DEBIL, NivelFuerza.DEBIL -> ColorError
    NivelFuerza.MEDIA -> ColorAmbar
    else -> ColorExito
}

// ===================== Helpers de archivos (SAF / DocumentFile) =====================

private fun nombreDeUri(context: android.content.Context, uri: Uri): String {
    context.contentResolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)?.use { c ->
        if (c.moveToFirst()) {
            val idx = c.getColumnIndex(OpenableColumns.DISPLAY_NAME)
            if (idx >= 0) return c.getString(idx) ?: Localization.t("pantalla.nombreFallbackArchivo")
        }
    }
    return DocumentFile.fromSingleUri(context, uri)?.name ?: Localization.t("pantalla.nombreFallbackArchivo")
}

private fun tamanoDeUri(context: android.content.Context, uri: Uri): Long {
    context.contentResolver.query(uri, arrayOf(OpenableColumns.SIZE), null, null, null)?.use { c ->
        if (c.moveToFirst()) {
            val idx = c.getColumnIndex(OpenableColumns.SIZE)
            if (idx >= 0 && !c.isNull(idx)) return c.getLong(idx)
        }
    }
    return DocumentFile.fromSingleUri(context, uri)?.length() ?: 0L
}

private fun borrarUri(context: android.content.Context, uri: Uri) {
    try {
        DocumentsContract.deleteDocument(context.contentResolver, uri)
    } catch (_: Exception) {
        // No es crítico: si no se pudo borrar el original, igual el .enc ya se generó.
    }
}

private fun listarRecursivo(carpeta: DocumentFile, prefijo: String, salida: MutableList<Pair<String, DocumentFile>>) {
    for (hijo in carpeta.listFiles()) {
        val nombre = hijo.name ?: continue
        val relativa = if (prefijo.isEmpty()) nombre else "$prefijo/$nombre"
        if (hijo.isDirectory) {
            listarRecursivo(hijo, relativa, salida)
        } else {
            salida.add(relativa to hijo)
        }
    }
}

/** Crea (si hace falta) las subcarpetas de una ruta relativa dentro de [raiz] y devuelve el DocumentFile del archivo final. */
private fun crearEnArbol(raiz: DocumentFile, rutaRelativa: String): DocumentFile {
    val partes = rutaRelativa.split("/")
    var actual = raiz
    for (i in 0 until partes.size - 1) {
        actual = actual.findFile(partes[i]) ?: actual.createDirectory(partes[i])
            ?: throw IllegalStateException(Localization.t("pantalla.noSePudoCrearCarpeta", partes[i]))
    }
    val nombreArchivo = partes.last()
    return actual.createFile("application/octet-stream", nombreArchivo)
        ?: throw IllegalStateException(Localization.t("pantalla.noSePudoCrearArchivo", nombreArchivo))
}
