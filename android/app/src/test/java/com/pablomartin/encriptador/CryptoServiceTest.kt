package com.pablomartin.encriptador

import org.junit.Assert.assertArrayEquals
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import kotlin.random.Random

class CryptoServiceTest {

    @Test
    fun `archivo chico ida y vuelta`() {
        val original = "hola mundo".toByteArray()
        val encSalida = ByteArrayOutputStream()
        CryptoService.encriptarArchivo(ByteArrayInputStream(original), original.size.toLong(), encSalida, "clave1")

        val planoSalida = ByteArrayOutputStream()
        CryptoService.desencriptarArchivo(ByteArrayInputStream(encSalida.toByteArray()), planoSalida, "clave1")

        assertArrayEquals(original, planoSalida.toByteArray())
    }

    @Test
    fun `archivo grande varios chunks ida y vuelta`() {
        val original = Random.nextBytes(300_000)
        val encSalida = ByteArrayOutputStream()
        CryptoService.encriptarArchivo(ByteArrayInputStream(original), original.size.toLong(), encSalida, "clave2")

        val planoSalida = ByteArrayOutputStream()
        CryptoService.desencriptarArchivo(ByteArrayInputStream(encSalida.toByteArray()), planoSalida, "clave2")

        assertArrayEquals(original, planoSalida.toByteArray())
    }

    @Test
    fun `contrasena incorrecta rechazada`() {
        val original = "secreto".toByteArray()
        val encSalida = ByteArrayOutputStream()
        CryptoService.encriptarArchivo(ByteArrayInputStream(original), original.size.toLong(), encSalida, "clave3")

        try {
            CryptoService.desencriptarArchivo(ByteArrayInputStream(encSalida.toByteArray()), ByteArrayOutputStream(), "clave_mala")
            fail("no debería haber podido desencriptar")
        } catch (e: ArchivoDanadoException) {
            // esperado
        }
    }

    @Test
    fun `archivo manipulado es detectado`() {
        val original = Random.nextBytes(150_000)
        val encSalida = ByteArrayOutputStream()
        CryptoService.encriptarArchivo(ByteArrayInputStream(original), original.size.toLong(), encSalida, "clave4")

        val bytes = encSalida.toByteArray()
        bytes[bytes.size / 2] = (bytes[bytes.size / 2].toInt() xor 0xFF).toByte()

        try {
            CryptoService.desencriptarArchivo(ByteArrayInputStream(bytes), ByteArrayOutputStream(), "clave4")
            fail("no debería aceptar un archivo manipulado")
        } catch (e: ArchivoDanadoException) {
            // esperado: la autenticación GCM detectó la manipulación
        }
    }

    @Test
    fun `carpeta con varios archivos ida y vuelta`() {
        val contenidoA = "contenido a".toByteArray()
        val contenidoB = Random.nextBytes(80_000)
        val fuentes = listOf(
            CryptoService.FuenteArchivo("leeme.txt", contenidoA.size.toLong()) { ByteArrayInputStream(contenidoA) },
            CryptoService.FuenteArchivo("fotos/2024/grande.bin", contenidoB.size.toLong()) { ByteArrayInputStream(contenidoB) },
        )

        val encSalida = ByteArrayOutputStream()
        val incluidos = CryptoService.encriptarCarpeta(fuentes, encSalida, "clave5")
        assertEquals(2, incluidos)

        val encBytes = encSalida.toByteArray()
        assertTrue(CryptoService.esContenedorDeCarpeta(ByteArrayInputStream(encBytes)))

        val sesion = CryptoService.abrirCarpeta(ByteArrayInputStream(encBytes), "clave5")
        assertEquals(2, sesion.entradas.size)

        val resultados = mutableMapOf<String, ByteArray>()
        CryptoService.extraerTodo(
            reabrirEntrada = { ByteArrayInputStream(encBytes) },
            sesion = sesion,
            crearSalida = { entrada ->
                object : ByteArrayOutputStream() {
                    override fun close() {
                        resultados[entrada.relativa] = toByteArray()
                    }
                }
            },
        )

        assertArrayEquals(contenidoA, resultados["leeme.txt"])
        assertArrayEquals(contenidoB, resultados["fotos/2024/grande.bin"])
    }

    @Test
    fun `carpeta con contrasena incorrecta rechazada al abrir`() {
        val fuentes = listOf(
            CryptoService.FuenteArchivo("a.txt", 5) { ByteArrayInputStream("hola!".toByteArray()) },
        )
        val encSalida = ByteArrayOutputStream()
        CryptoService.encriptarCarpeta(fuentes, encSalida, "clave6")

        try {
            CryptoService.abrirCarpeta(ByteArrayInputStream(encSalida.toByteArray()), "clave_mala")
            fail("no debería haber podido abrir")
        } catch (e: ArchivoDanadoException) {
            // esperado
        }
    }
}
