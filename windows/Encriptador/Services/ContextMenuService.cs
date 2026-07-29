using Microsoft.Win32;

namespace Encriptador.Services;

/// <summary>
/// Registra/quita la integración con el menú contextual de Windows (clic
/// derecho), similar a WinRAR: "Encriptar con Encriptador" sobre cualquier
/// archivo o carpeta, y asociación de la extensión .enc para poder
/// desencriptar con "Abrir con" o doble clic. Todo bajo
/// HKEY_CURRENT_USER, por lo que no requiere permisos de administrador.
/// </summary>
public static class ContextMenuService
{
    private const string VerboEncriptar = "EncriptadorEncriptar";
    private const string ProgId = "Encriptador.ArchivoEncriptado";

    private static string RutaEjecutable => Environment.ProcessPath
        ?? throw new InvalidOperationException("No se pudo determinar la ruta del ejecutable.");

    public static bool EstaRegistrado()
    {
        using var clave = Registry.CurrentUser.OpenSubKey($@"Software\Classes\*\shell\{VerboEncriptar}");
        return clave is not null;
    }

    public static void Registrar()
    {
        var exe = RutaEjecutable;
        var comando = $"\"{exe}\" \"%1\"";

        // Clic derecho sobre cualquier archivo -> "Encriptar con Encriptador"
        RegistrarVerbo($@"Software\Classes\*\shell\{VerboEncriptar}", "Encriptar con Encriptador", exe, comando);

        // Clic derecho sobre una carpeta -> "Encriptar con Encriptador"
        RegistrarVerbo($@"Software\Classes\Directory\shell\{VerboEncriptar}", "Encriptar con Encriptador", exe, comando);

        // Asociación de archivos .enc para "Abrir con" / doble clic -> desencriptar
        using (var progId = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
        {
            progId.SetValue(string.Empty, "Archivo encriptado (Encriptador)");
            using var icono = progId.CreateSubKey("DefaultIcon");
            icono.SetValue(string.Empty, $"{exe},0");
            using var comandoClave = progId.CreateSubKey(@"shell\open\command");
            comandoClave.SetValue(string.Empty, comando);
        }

        using (var extension = Registry.CurrentUser.CreateSubKey($@"Software\Classes\.enc"))
        {
            extension.SetValue(string.Empty, ProgId);
            using var openWith = extension.CreateSubKey("OpenWithProgids");
            openWith.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
        }

        NotificarExplorador();
    }

    public static void Desregistrar()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\*\shell\{VerboEncriptar}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\Directory\shell\{VerboEncriptar}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", throwOnMissingSubKey: false);

        using (var extension = Registry.CurrentUser.OpenSubKey($@"Software\Classes\.enc", writable: true))
        {
            extension?.DeleteSubKeyTree("OpenWithProgids", throwOnMissingSubKey: false);
            if (extension?.GetValue(string.Empty) as string == ProgId)
                extension.DeleteValue(string.Empty, throwOnMissingValue: false);
        }

        // Si la clave .enc quedó vacía (sin valores ni subclaves), la borramos por completo.
        using (var extension = Registry.CurrentUser.OpenSubKey($@"Software\Classes\.enc"))
        {
            if (extension is not null && extension.ValueCount == 0 && extension.SubKeyCount == 0)
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\.enc", throwOnMissingSubKey: false);
        }

        NotificarExplorador();
    }

    private static void RegistrarVerbo(string rutaClave, string texto, string exe, string comando)
    {
        using var clave = Registry.CurrentUser.CreateSubKey(rutaClave);
        clave.SetValue(string.Empty, texto);
        clave.SetValue("Icon", $"{exe},0");
        using var comandoClave = clave.CreateSubKey("command");
        comandoClave.SetValue(string.Empty, comando);
    }

    private static void NotificarExplorador()
    {
        NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }

    private static class NativeMethods
    {
        public const int SHCNE_ASSOCCHANGED = 0x08000000;
        public const int SHCNF_IDLIST = 0x0000;

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
