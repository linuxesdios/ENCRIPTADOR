using System.Globalization;

namespace Encriptador.Services;

/// <summary>
/// Textos traducidos de la app (ES/EN/RU/ZH). Diccionario clave→texto por
/// idioma (ver Loc.Es.cs / .En.cs / .Ru.cs / .Zh.cs), sin .resx:
/// no hay MVVM/DI en esta app, así que este servicio estático imita el
/// mismo estilo imperativo que ya usan CryptoService/PasswordStrengthHelper.
/// Pluralización simplificada a 2 formas (singular/plural) en los 4 idiomas.
/// </summary>
public static partial class Loc
{
    public static Idioma IdiomaActual { get; private set; } = Idioma.Es;

    public static event Action? IdiomaCambiado;

    /// <summary>Carga el idioma guardado o, si es la primera vez, lo detecta del sistema y lo persiste.</summary>
    public static void Inicializar()
    {
        var guardado = SettingsService.LeerIdioma();
        if (guardado is not null && TryParse(guardado, out var idioma))
        {
            IdiomaActual = idioma;
            return;
        }

        IdiomaActual = DetectarDesdeSistema();
        SettingsService.GuardarIdioma(Clave(IdiomaActual));
    }

    public static void CambiarIdioma(Idioma nuevo)
    {
        if (nuevo == IdiomaActual)
            return;

        IdiomaActual = nuevo;
        SettingsService.GuardarIdioma(Clave(nuevo));
        IdiomaCambiado?.Invoke();
    }

    /// <summary>Busca una clave en el idioma actual; si falta, cae a español y, si tampoco está, devuelve [[clave]] (nunca tira excepción).</summary>
    public static string T(string clave, params object[] args)
    {
        var diccionario = Diccionario(IdiomaActual);
        if (!diccionario.TryGetValue(clave, out var texto) && !Es.TryGetValue(clave, out texto))
            return $"[[{clave}]]";

        return args.Length == 0 ? texto : string.Format(texto, args);
    }

    /// <summary>Simplificación deliberada: 2 formas (singular/plural), también para ruso.</summary>
    public static string Plural(int n, string claveBase, params object[] args) =>
        T(n == 1 ? claveBase + "_uno" : claveBase + "_otros", args);

    private static Dictionary<string, string> Diccionario(Idioma idioma) => idioma switch
    {
        Idioma.En => En,
        Idioma.Ru => Ru,
        Idioma.Zh => Zh,
        _ => Es,
    };

    private static Idioma DetectarDesdeSistema()
    {
        var codigo = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return codigo switch
        {
            "en" => Idioma.En,
            "ru" => Idioma.Ru,
            "zh" => Idioma.Zh,
            _ => Idioma.Es,
        };
    }

    private static string Clave(Idioma idioma) => idioma switch
    {
        Idioma.En => "en",
        Idioma.Ru => "ru",
        Idioma.Zh => "zh",
        _ => "es",
    };

    private static bool TryParse(string clave, out Idioma idioma)
    {
        switch (clave.ToLowerInvariant())
        {
            case "en": idioma = Idioma.En; return true;
            case "ru": idioma = Idioma.Ru; return true;
            case "zh": idioma = Idioma.Zh; return true;
            case "es": idioma = Idioma.Es; return true;
            default: idioma = Idioma.Es; return false;
        }
    }
}
