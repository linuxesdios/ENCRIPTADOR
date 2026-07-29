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
    var progreso by remember { mutableStateOf<Pair<Int, Int>?>(null) }
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
            val nombre = DocumentFile.fromTreeUri(context, uri)?.name ?: "carpeta"
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
        if (password.isEmpty()) { mostrar("Ingresá una contraseña.", TipoEstado.ERROR); return }
        if (password != confirmar) { mostrar("Las contraseñas no coinciden.", TipoEstado.ERROR); return }
        if (sel is Seleccion.Ninguna) { mostrar("Seleccioná archivo(s) o una carpeta.", TipoEstado.ERROR); return }

        scope.launch {
            procesando = true
            progreso = null
            mostrar("Elegí dónde guardar el .enc...", TipoEstado.INFO)
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
                                        CryptoService.encriptarArchivo(entrada, longitud, salida, password)
                                    }
                                }
                                if (!conservarOriginal) borrarUri(context, uri)
                            }
                            mostrar("Archivo encriptado correctamente.", TipoEstado.EXITO)
                        } else {
                            val destinoUri = pedirDestino("${sel.uris.size} archivos.enc") ?: run { procesando = false; return@launch }
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
                            mostrar("${sel.uris.size} archivos encriptados en un solo paquete.", TipoEstado.EXITO)
                        }
                    }

                    is Seleccion.Carpeta -> {
                        val raiz = DocumentFile.fromTreeUri(context, sel.treeUri)
                            ?: throw IllegalStateException("No se pudo abrir la carpeta.")
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
                        mostrar("Carpeta encriptada en un solo archivo.", TipoEstado.EXITO)
                    }

                    Seleccion.Ninguna -> Unit
                }
            } catch (ex: Exception) {
                mostrar("Error: ${ex.message}", TipoEstado.ERROR)
            } finally {
                procesando = false
                progreso = null
            }
        }
    }

    fun ejecutarDesencriptar() {
        val sel = seleccion
        if (sel !is Seleccion.Archivos || sel.uris.size != 1) {
            mostrar("Seleccioná un solo archivo .enc.", TipoEstado.ERROR); return
        }
        if (password.isEmpty()) { mostrar("Ingresá una contraseña.", TipoEstado.ERROR); return }

        val uri = sel.uris[0]
        scope.launch {
            procesando = true
            mostrar("Verificando contraseña...", TipoEstado.INFO)
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
                                CryptoService.desencriptarArchivo(entrada, salida, password)
                            }
                        }
                        if (!conservarOriginal) borrarUri(context, uri)
                    }
                    mostrar("Archivo desencriptado correctamente.", TipoEstado.EXITO)
                } else {
                    val sesion = withContext(Dispatchers.IO) {
                        resolver.openInputStream(uri)!!.use { CryptoService.abrirCarpeta(it, password) }
                    }
                    sesionPendiente = sesion
                    uriPendiente = uri
                    mostrar("Carpeta con ${sesion.entradas.size} archivo(s). Elegí dónde extraerla.", TipoEstado.INFO)
                }
            } catch (ex: GeneralSecurityException) {
                mostrar("Contraseña incorrecta o archivo dañado.", TipoEstado.ERROR)
            } catch (ex: Exception) {
                mostrar("Error: ${ex.message}", TipoEstado.ERROR)
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
                mostrar("Extrayendo...", TipoEstado.INFO)
                try {
                    val raizDestino = DocumentFile.fromTreeUri(context, destinoTreeUri)
                        ?: throw IllegalStateException("No se pudo abrir el destino.")
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
                    mostrar("Carpeta restaurada: ${sesion.entradas.size} archivo(s).", TipoEstado.EXITO)
                } catch (ex: Exception) {
                    mostrar("Error al extraer: ${ex.message}", TipoEstado.ERROR)
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
            Column {
                Text("Encriptador", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = ColorTextoPrincipal)
                Text("Cifrado AES-256-GCM de archivos", fontSize = 11.sp, color = ColorTextoSecundario)
            }
        }

        Card(
            colors = CardDefaults.cardColors(containerColor = ColorFondoCard),
            border = BorderStroke(1.dp, ColorBorde),
            shape = RoundedCornerShape(16.dp),
        ) {
            Column(Modifier.padding(18.dp)) {

                Etiqueta("ARCHIVO O CARPETA")
                Row(Modifier.padding(top = 6.dp, bottom = 4.dp)) {
                    BotonSecundario("📄 Archivo(s)", Modifier.weight(1f)) {
                        lanzadorArchivos.launch(arrayOf("*/*"))
                    }
                    Spacer(Modifier.width(8.dp))
                    BotonSecundario("📁 Carpeta", Modifier.weight(1f)) {
                        lanzadorCarpeta.launch(null)
                    }
                }
                Text(
                    text = when (val sel = seleccion) {
                        is Seleccion.Ninguna -> "Nada seleccionado"
                        is Seleccion.Archivos -> if (sel.uris.size == 1) sel.nombres[0] else "${sel.uris.size} archivos seleccionados"
                        is Seleccion.Carpeta -> "Carpeta: ${sel.nombre}"
                    },
                    fontSize = 12.5.sp,
                    color = ColorTextoSecundario,
                    modifier = Modifier.padding(top = 4.dp, bottom = 14.dp),
                )

                Row(verticalAlignment = Alignment.CenterVertically) {
                    Etiqueta("CONTRASEÑA", Modifier.weight(1f))
                    BotonSecundario(if (mostrarPassword) "Ocultar" else "Mostrar", chico = true) {
                        mostrarPassword = !mostrarPassword
                    }
                }
                Spacer(Modifier.height(6.dp))
                CampoPassword(password, { password = it }, mostrarPassword)

                if (esModoEncriptar) {
                    Etiqueta("REPETIR CONTRASEÑA", Modifier.padding(top = 14.dp, bottom = 6.dp))
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
                        if (esModoEncriptar) "Conservar el original (no borrarlo al terminar)" else "Conservar el .enc (no borrarlo al terminar)",
                        fontSize = 12.sp, color = ColorTextoSecundario,
                    )
                }

                Spacer(Modifier.height(16.dp))
                Row {
                    BotonPrimario("🔒 Encriptar", Modifier.weight(1f), BrushAcento(), habilitado = !procesando) { ejecutarEncriptar() }
                    Spacer(Modifier.width(10.dp))
                    BotonPrimario("🔑 Desencriptar", Modifier.weight(1f), BrushExito(), habilitado = !procesando) { ejecutarDesencriptar() }
                }

                if (sesionPendiente != null) {
                    Spacer(Modifier.height(12.dp))
                    BotonPrimario("📂 Elegir carpeta destino y extraer", Modifier.fillMaxWidth(), BrushAcento(), habilitado = !procesando) {
                        lanzadorDestinoCarpeta.launch(null)
                    }
                }

                if (procesando) {
                    Spacer(Modifier.height(14.dp))
                    val p = progreso
                    if (p != null) {
                        LinearProgressIndicator(
                            progress = { p.first.toFloat() / p.second.toFloat() },
                            modifier = Modifier.fillMaxWidth(),
                            color = ColorAcento1, trackColor = ColorFondoCampo,
                        )
                        Text("Archivo ${p.first} de ${p.second}", fontSize = 11.sp, color = ColorTextoSecundario, modifier = Modifier.padding(top = 6.dp))
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
            "Realizado por Pablo Martín Fernández",
            fontSize = 10.5.sp, color = ColorTextoSecundario,
            modifier = Modifier.padding(start = 4.dp),
        )
    }
}

// ===================== Componentes reutilizables =====================

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
            if (idx >= 0) return c.getString(idx) ?: "archivo"
        }
    }
    return DocumentFile.fromSingleUri(context, uri)?.name ?: "archivo"
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
            ?: throw IllegalStateException("No se pudo crear la carpeta '${partes[i]}'.")
    }
    val nombreArchivo = partes.last()
    return actual.createFile("application/octet-stream", nombreArchivo)
        ?: throw IllegalStateException("No se pudo crear el archivo '$nombreArchivo'.")
}
