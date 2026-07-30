package com.pablomartin.encriptador

enum class NivelFuerza { MUY_DEBIL, DEBIL, MEDIA, FUERTE, MUY_FUERTE }

/** Heurística simple (longitud + variedad de caracteres) para estimar qué tan débil/fuerte es una contraseña. */
object PasswordStrength {

    fun evaluar(password: String): NivelFuerza {
        if (password.isEmpty()) return NivelFuerza.MUY_DEBIL

        var variedad = 0
        if (password.any { it.isLowerCase() }) variedad++
        if (password.any { it.isUpperCase() }) variedad++
        if (password.any { it.isDigit() }) variedad++
        if (password.any { !it.isLetterOrDigit() }) variedad++

        val puntajeLongitud = when {
            password.length < 6 -> 0
            password.length < 8 -> 1
            password.length < 12 -> 2
            password.length < 16 -> 3
            else -> 4
        }

        val puntaje = puntajeLongitud + variedad // 0..8
        return when {
            puntaje <= 1 -> NivelFuerza.MUY_DEBIL
            puntaje <= 3 -> NivelFuerza.DEBIL
            puntaje <= 5 -> NivelFuerza.MEDIA
            puntaje <= 6 -> NivelFuerza.FUERTE
            else -> NivelFuerza.MUY_FUERTE
        }
    }

    fun texto(nivel: NivelFuerza): String = when (nivel) {
        NivelFuerza.MUY_DEBIL -> Localization.t("strength.muyDebil")
        NivelFuerza.DEBIL -> Localization.t("strength.debil")
        NivelFuerza.MEDIA -> Localization.t("strength.media")
        NivelFuerza.FUERTE -> Localization.t("strength.fuerte")
        NivelFuerza.MUY_FUERTE -> Localization.t("strength.muyFuerte")
    }

    fun segmentos(nivel: NivelFuerza, vacia: Boolean): Int {
        if (vacia) return 0
        return when (nivel) {
            NivelFuerza.MUY_DEBIL -> 1
            NivelFuerza.DEBIL -> 2
            NivelFuerza.MEDIA -> 3
            else -> 4
        }
    }
}
