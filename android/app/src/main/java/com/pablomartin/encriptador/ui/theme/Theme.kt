package com.pablomartin.encriptador.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val EsquemaOscuro = darkColorScheme(
    primary = ColorAcento1,
    secondary = ColorExito,
    background = ColorFondo,
    surface = ColorFondoCard,
    onPrimary = Color.White,
    onSecondary = Color.White,
    onBackground = ColorTextoPrincipal,
    onSurface = ColorTextoPrincipal,
    error = ColorError,
)

@Composable
fun EncriptadorTheme(content: @Composable () -> Unit) {
    // La app siempre usa el tema oscuro (coherente con las versiones Windows/Linux),
    // independientemente del tema del sistema.
    MaterialTheme(
        colorScheme = EsquemaOscuro,
        content = content,
    )
}
