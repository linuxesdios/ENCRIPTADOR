package com.pablomartin.encriptador

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import java.util.Locale

enum class Idioma { ES, EN, RU, ZH }

/**
 * Textos traducidos de la app (ES/EN/RU/ZH). `idiomaActual` es un
 * mutableStateOf de Compose: cualquier composable que llame a [t] queda
 * suscripto automáticamente y se recompone solo al cambiar el idioma, sin
 * necesitar un "reaplicar" manual como en las versiones de escritorio.
 * Pluralización simplificada a 2 formas (singular/plural), también en ruso.
 */
object Localization {
    var idiomaActual by mutableStateOf(Idioma.ES)
        private set

    fun inicializar(idioma: Idioma) {
        idiomaActual = idioma
    }

    fun cambiarIdioma(nuevo: Idioma) {
        idiomaActual = nuevo
        Preferencias.guardarIdioma(nuevo)
    }

    /** Busca [clave] en el idioma actual; si falta, cae a español y, si tampoco está, devuelve [[clave]] (nunca tira excepción). */
    fun t(clave: String, vararg args: Any): String {
        val texto = mapa(idiomaActual)[clave] ?: LocalizationEs.mapa[clave] ?: return "[[$clave]]"
        return if (args.isEmpty()) texto else String.format(Locale.getDefault(), texto, *args)
    }

    /** Simplificación deliberada: 2 formas (singular/plural), también para ruso. */
    fun plural(n: Int, claveBase: String, vararg args: Any): String =
        t(if (n == 1) "${claveBase}_uno" else "${claveBase}_otros", *args)

    private fun mapa(idioma: Idioma): Map<String, String> = when (idioma) {
        Idioma.EN -> LocalizationEn.mapa
        Idioma.RU -> LocalizationRu.mapa
        Idioma.ZH -> LocalizationZh.mapa
        Idioma.ES -> LocalizationEs.mapa
    }

    /** Detecta el idioma del sistema (en/ru/zh mapean directo, cualquier otro -incluido es- cae a español). */
    fun detectarDesdeSistema(): Idioma = when (Locale.getDefault().language.lowercase()) {
        "en" -> Idioma.EN
        "ru" -> Idioma.RU
        "zh" -> Idioma.ZH
        else -> Idioma.ES
    }
}
