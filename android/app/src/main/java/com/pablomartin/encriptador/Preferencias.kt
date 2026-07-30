package com.pablomartin.encriptador

import android.content.Context

private const val PREFS = "encriptador_prefs"
private const val CLAVE_IDIOMA = "idioma"

/** Persiste el idioma elegido en SharedPreferences (sin dependencias nuevas). */
object Preferencias {
    private lateinit var prefs: android.content.SharedPreferences

    fun inicializar(context: Context) {
        prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
    }

    /** Devuelve el idioma guardado o, si es la primera vez, lo detecta del sistema y lo persiste. */
    fun cargarIdioma(): Idioma {
        val guardado = prefs.getString(CLAVE_IDIOMA, null)
        if (guardado != null) {
            return try { Idioma.valueOf(guardado) } catch (_: IllegalArgumentException) { Idioma.ES }
        }

        val detectado = Localization.detectarDesdeSistema()
        guardarIdioma(detectado)
        return detectado
    }

    fun guardarIdioma(idioma: Idioma) {
        if (!::prefs.isInitialized) return
        prefs.edit().putString(CLAVE_IDIOMA, idioma.name).apply()
    }
}
