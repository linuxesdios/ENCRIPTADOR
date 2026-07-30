using System.IO;
using System.Text.Json;

namespace Encriptador.Services;

/// <summary>
/// Persiste preferencias simples de la app (por ahora, solo el idioma) en
/// un JSON chico bajo %AppData%\Encriptador\settings.json (en Linux,
/// ApplicationData resuelve automáticamente a ~/.config).
/// </summary>
internal static class SettingsService
{
    private sealed class Settings
    {
        public string? Idioma { get; set; }
    }

    private static string RutaArchivo =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Encriptador", "settings.json");

    public static string? LeerIdioma()
    {
        try
        {
            if (!File.Exists(RutaArchivo))
                return null;

            var json = File.ReadAllText(RutaArchivo);
            return JsonSerializer.Deserialize<Settings>(json)?.Idioma;
        }
        catch
        {
            return null;
        }
    }

    public static void GuardarIdioma(string idioma)
    {
        try
        {
            var carpeta = Path.GetDirectoryName(RutaArchivo)!;
            Directory.CreateDirectory(carpeta);
            File.WriteAllText(RutaArchivo, JsonSerializer.Serialize(new Settings { Idioma = idioma }));
        }
        catch
        {
            // No crítico: si no se puede persistir, la app sigue funcionando con el idioma en memoria.
        }
    }
}
