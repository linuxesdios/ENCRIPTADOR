namespace Encriptador.Services;

/// <summary>Heurística simple (longitud + variedad de caracteres) para estimar qué tan débil/fuerte es una contraseña.</summary>
public static class PasswordStrengthHelper
{
    public enum Nivel { MuyDebil, Debil, Media, Fuerte, MuyFuerte }

    public static (Nivel Nivel, double Fraccion) Evaluar(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (Nivel.MuyDebil, 0);

        var variedad = 0;
        if (password.Any(char.IsLower)) variedad++;
        if (password.Any(char.IsUpper)) variedad++;
        if (password.Any(char.IsDigit)) variedad++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) variedad++;

        var puntajeLongitud = password.Length switch
        {
            < 6 => 0,
            < 8 => 1,
            < 12 => 2,
            < 16 => 3,
            _ => 4
        };

        var puntaje = puntajeLongitud + variedad; // 0..8

        var nivel = puntaje switch
        {
            <= 1 => Nivel.MuyDebil,
            <= 3 => Nivel.Debil,
            <= 5 => Nivel.Media,
            <= 6 => Nivel.Fuerte,
            _ => Nivel.MuyFuerte
        };

        return (nivel, Math.Clamp(puntaje / 8.0, 0, 1));
    }

    public static string Texto(Nivel nivel) => nivel switch
    {
        Nivel.MuyDebil => "Muy débil",
        Nivel.Debil => "Débil",
        Nivel.Media => "Media",
        Nivel.Fuerte => "Fuerte",
        _ => "Muy fuerte",
    };
}
