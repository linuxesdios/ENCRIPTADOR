package com.pablomartin.encriptador

import java.io.EOFException
import java.io.InputStream
import java.io.OutputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.security.GeneralSecurityException
import java.security.SecureRandom
import javax.crypto.Cipher
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.PBEKeySpec
import javax.crypto.spec.SecretKeySpec

/** Contraseña incorrecta o archivo .enc dañado/manipulado (falló la verificación GCM). */
class ArchivoDanadoException(mensaje: String = Localization.t("crypto.archivoDanado")) :
    GeneralSecurityException(mensaje)

/** El .enc es de carpeta pero se pidió tratarlo como archivo individual, o viceversa. */
class TipoIncorrectoException(mensaje: String) : IllegalStateException(mensaje)

data class EntradaManifiesto(val relativa: String, val longitud: Long, val offset: Long)

/**
 * Cifra/descifra con AES-256-GCM por bloques de 64 KB (mismo esquema que la versión
 * de escritorio: cada bloque lleva su propio nonce y tag de autenticación, así que
 * una contraseña incorrecta o un archivo manipulado se detectan de inmediato). El
 * formato de archivo es compatible byte a byte con la versión Windows/Linux, aunque
 * el código de esta app es una implementación independiente, sin compartir nada.
 *
 * Un archivo individual: [salt 16][tipo=1][longitud int64][datos cifrados por bloques]
 * Una carpeta: [salt 16][tipo=2][longitud manifiesto int64][manifiesto cifrado][datos de cada archivo, uno atrás del otro]
 */
object CryptoService {
    private const val SALT_SIZE = 16
    private const val KEY_SIZE_BYTES = 32
    private const val PBKDF2_ITERATIONS = 600_000
    private const val TIPO_ARCHIVO = 1
    private const val TIPO_CARPETA = 2
    private const val MAX_ENTRADAS = 5_000_000
    private const val MAX_LONGITUD_RUTA = 32_000
    private const val MAX_LONGITUD_MANIFIESTO = 200_000_000L
    private const val TAMANO_CHUNK = 64 * 1024
    private const val NONCE_SIZE = 12
    private const val TAG_SIZE = 16

    const val EXTENSION = ".enc"

    fun esArchivoEncriptado(nombre: String) = nombre.endsWith(EXTENSION, ignoreCase = true)

    // ===================== Archivo individual =====================

    fun encriptarArchivo(entrada: InputStream, longitud: Long, salida: OutputStream, password: String) {
        val salt = ByteArray(SALT_SIZE).also { SecureRandom().nextBytes(it) }
        val clave = derivarClave(password, salt)
        salida.write(salt)
        salida.write(TIPO_ARCHIVO)
        escribirLong(salida, longitud)
        cifrarStream(entrada, salida, clave, longitud)
    }

    /** Lee el tipo de un .enc (archivo o carpeta) sin necesitar la contraseña: va sin cifrar. */
    fun esContenedorDeCarpeta(entrada: InputStream): Boolean {
        val salt = ByteArray(SALT_SIZE)
        leerCompleto(entrada, salt)
        return entrada.read() == TIPO_CARPETA
    }

    fun desencriptarArchivo(entrada: InputStream, salida: OutputStream, password: String) {
        val salt = ByteArray(SALT_SIZE)
        leerCompleto(entrada, salt)
        val tipo = entrada.read()
        if (tipo != TIPO_ARCHIVO) throw TipoIncorrectoException(Localization.t("crypto.tipoEsCarpeta"))

        val clave = derivarClave(password, salt)
        val longitud = leerLong(entrada)
        if (longitud < 0) throw ArchivoDanadoException()
        descifrarStream(entrada, salida, clave, longitud)
    }

    // ===================== Carpeta =====================

    /** Cada entrada de origen: ruta relativa (con '/') + su tamaño + una forma de abrirla para leer. */
    class FuenteArchivo(val relativa: String, val longitud: Long, val abrir: () -> InputStream)

    fun encriptarCarpeta(archivos: List<FuenteArchivo>, salida: OutputStream, password: String, progreso: ((Int, Int) -> Unit)? = null): Int {
        val salt = ByteArray(SALT_SIZE).also { SecureRandom().nextBytes(it) }
        val clave = derivarClave(password, salt)

        var offsetAcumulado = 0L
        val entradas = archivos.map { f ->
            val e = EntradaManifiesto(f.relativa, f.longitud, offsetAcumulado)
            offsetAcumulado += longitudCifrada(f.longitud)
            e
        }

        salida.write(salt)
        salida.write(TIPO_CARPETA)

        val manifiestoPlano = serializarManifiesto(entradas)
        escribirLong(salida, manifiestoPlano.size.toLong())
        cifrarStream(manifiestoPlano.inputStream(), salida, clave, manifiestoPlano.size.toLong())

        archivos.forEachIndexed { i, f ->
            f.abrir().use { cifrarStream(it, salida, clave, f.longitud) }
            progreso?.invoke(i + 1, archivos.size)
        }

        return archivos.size
    }

    class SesionCarpeta internal constructor(
        val entradas: List<EntradaManifiesto>,
        internal val clave: ByteArray,
    )

    /** Valida la contraseña y descifra solo el manifiesto (nombres/tamaños), sin tocar contenido de archivos. */
    fun abrirCarpeta(entrada: InputStream, password: String): SesionCarpeta {
        val salt = ByteArray(SALT_SIZE)
        leerCompleto(entrada, salt)
        val tipo = entrada.read()
        if (tipo != TIPO_CARPETA) throw TipoIncorrectoException(Localization.t("crypto.tipoEsArchivo"))

        val clave = derivarClave(password, salt)
        val manifiestoLen = leerLong(entrada)
        if (manifiestoLen < 0 || manifiestoLen > MAX_LONGITUD_MANIFIESTO) throw ArchivoDanadoException()

        val bufferSalida = java.io.ByteArrayOutputStream()
        descifrarStream(entrada, bufferSalida, clave, manifiestoLen)
        val entradas = deserializarManifiesto(bufferSalida.toByteArray())
        return SesionCarpeta(entradas, clave)
    }

    /**
     * Descifra todo el contenido de la sesión, en orden, invocando [crearSalida] por cada
     * entrada para obtener dónde escribirla. [entrada] debe volver a abrirse desde el
     * principio del archivo .enc (se vuelve a posicionar internamente).
     */
    fun extraerTodo(
        reabrirEntrada: () -> InputStream,
        sesion: SesionCarpeta,
        crearSalida: (EntradaManifiesto) -> OutputStream,
        progreso: ((Int, Int) -> Unit)? = null,
    ) {
        reabrirEntrada().use { entrada ->
            val inicioDatos = SALT_SIZE + 1 + 8 + longitudCifrada(manifiestoPlanoLongitud(sesion))
            saltar(entrada, inicioDatos)

            sesion.entradas.forEachIndexed { i, e ->
                crearSalida(e).use { salida -> descifrarStream(entrada, salida, sesion.clave, e.longitud) }
                progreso?.invoke(i + 1, sesion.entradas.size)
            }
        }
    }

    private fun manifiestoPlanoLongitud(sesion: SesionCarpeta): Long {
        // Se recalcula serializando de nuevo el manifiesto: es chico (solo nombres/tamaños), no los datos.
        return serializarManifiesto(sesion.entradas).size.toLong()
    }

    // ===================== Cifrado/descifrado por bloques (AES-256-GCM) =====================

    private fun cifrarStream(entrada: InputStream, salida: OutputStream, clave: ByteArray, longitudTotal: Long) {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        val secretKey = SecretKeySpec(clave, "AES")
        val buffer = ByteArray(TAMANO_CHUNK)
        val nonce = ByteArray(NONCE_SIZE)
        val rng = SecureRandom()
        var restante = longitudTotal
        val chunks = numeroDeChunks(longitudTotal)

        repeat(chunks) {
            val tam = minOf(TAMANO_CHUNK.toLong(), restante).toInt()
            leerCompleto(entrada, buffer, tam)

            rng.nextBytes(nonce)
            cipher.init(Cipher.ENCRYPT_MODE, secretKey, GCMParameterSpec(TAG_SIZE * 8, nonce))
            val cifradoConTag = cipher.doFinal(buffer, 0, tam) // ciphertext + tag (16B) al final

            salida.write(nonce)
            salida.write(cifradoConTag, tam, TAG_SIZE) // tag
            salida.write(cifradoConTag, 0, tam) // ciphertext

            restante -= tam
        }
    }

    private fun descifrarStream(entrada: InputStream, salida: OutputStream, clave: ByteArray, longitudTotal: Long) {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        val secretKey = SecretKeySpec(clave, "AES")
        val nonce = ByteArray(NONCE_SIZE)
        val tag = ByteArray(TAG_SIZE)
        var restante = longitudTotal
        val chunks = numeroDeChunks(longitudTotal)

        repeat(chunks) {
            val tam = minOf(TAMANO_CHUNK.toLong(), restante).toInt()
            leerCompleto(entrada, nonce)
            leerCompleto(entrada, tag)
            val cifrado = ByteArray(tam)
            leerCompleto(entrada, cifrado, tam)

            val cifradoConTag = cifrado + tag
            try {
                cipher.init(Cipher.DECRYPT_MODE, secretKey, GCMParameterSpec(TAG_SIZE * 8, nonce))
                val plano = cipher.doFinal(cifradoConTag)
                salida.write(plano)
            } catch (ex: GeneralSecurityException) {
                throw ArchivoDanadoException()
            }

            restante -= tam
        }
    }

    private fun numeroDeChunks(longitud: Long): Int =
        if (longitud == 0L) 1 else ((longitud + TAMANO_CHUNK - 1) / TAMANO_CHUNK).toInt()

    private fun longitudCifrada(longitudOriginal: Long): Long =
        longitudOriginal + numeroDeChunks(longitudOriginal).toLong() * (NONCE_SIZE + TAG_SIZE)

    // ===================== Manifiesto =====================

    private fun serializarManifiesto(entradas: List<EntradaManifiesto>): ByteArray {
        val buf = java.io.ByteArrayOutputStream()
        escribirInt(buf, entradas.size)
        for (e in entradas) {
            val pathBytes = e.relativa.toByteArray(Charsets.UTF_8)
            escribirInt(buf, pathBytes.size)
            buf.write(pathBytes)
            escribirLong(buf, e.longitud)
            escribirLong(buf, e.offset)
        }
        return buf.toByteArray()
    }

    private fun deserializarManifiesto(datos: ByteArray): List<EntradaManifiesto> {
        val stream = datos.inputStream()
        val cantidad = leerInt(stream)
        if (cantidad < 0 || cantidad > MAX_ENTRADAS) throw ArchivoDanadoException()

        val lista = ArrayList<EntradaManifiesto>(cantidad)
        repeat(cantidad) {
            val pathLen = leerInt(stream)
            if (pathLen <= 0 || pathLen > MAX_LONGITUD_RUTA) throw ArchivoDanadoException()
            val pathBytes = ByteArray(pathLen)
            leerCompleto(stream, pathBytes)
            val relativa = String(pathBytes, Charsets.UTF_8)

            val longitud = leerLong(stream)
            val offset = leerLong(stream)
            if (longitud < 0 || offset < 0) throw ArchivoDanadoException()

            lista.add(EntradaManifiesto(relativa, longitud, offset))
        }
        return lista
    }

    // ===================== Helpers de bajo nivel =====================

    private fun escribirInt(stream: OutputStream, valor: Int) {
        val bb = ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(valor)
        stream.write(bb.array())
    }

    private fun escribirLong(stream: OutputStream, valor: Long) {
        val bb = ByteBuffer.allocate(8).order(ByteOrder.LITTLE_ENDIAN).putLong(valor)
        stream.write(bb.array())
    }

    private fun leerInt(stream: InputStream): Int {
        val buffer = ByteArray(4)
        leerCompleto(stream, buffer)
        return ByteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN).int
    }

    private fun leerLong(stream: InputStream): Long {
        val buffer = ByteArray(8)
        leerCompleto(stream, buffer)
        return ByteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN).long
    }

    private fun leerCompleto(stream: InputStream, buffer: ByteArray, cantidad: Int = buffer.size) {
        var leido = 0
        while (leido < cantidad) {
            val n = stream.read(buffer, leido, cantidad - leido)
            if (n <= 0) throw ArchivoDanadoException()
            leido += n
        }
    }

    private fun saltar(stream: InputStream, cantidad: Long) {
        var restante = cantidad
        while (restante > 0) {
            val saltado = stream.skip(restante)
            if (saltado <= 0) {
                if (stream.read() == -1) throw EOFException()
                restante -= 1
            } else {
                restante -= saltado
            }
        }
    }

    private fun derivarClave(password: String, salt: ByteArray): ByteArray {
        val spec = PBEKeySpec(password.toCharArray(), salt, PBKDF2_ITERATIONS, KEY_SIZE_BYTES * 8)
        val factory = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256")
        return factory.generateSecret(spec).encoded
    }
}
