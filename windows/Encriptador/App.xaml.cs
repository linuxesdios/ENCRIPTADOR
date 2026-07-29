using System.IO;
using System.Windows;
using Encriptador.Services;

namespace Encriptador;

/// <summary>
/// Punto de entrada: sin argumentos abre la ventana principal completa.
/// Con un argumento (ruta de archivo o carpeta, pasada por el menú
/// contextual de Windows) abre la ventana rápida en modo encriptar o
/// desencriptar según corresponda.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--desregistrar", StringComparer.OrdinalIgnoreCase))
        {
            // Usado por el desinstalador para quitar las entradas del menú contextual antes de borrar los archivos.
            try { ContextMenuService.Desregistrar(); } catch { /* no crítico */ }
            Shutdown();
            return;
        }

        if (e.Args.Contains("--registrar", StringComparer.OrdinalIgnoreCase))
        {
            // Usado por el instalador para activar el menú contextual apenas termina de instalar, sin abrir ninguna ventana.
            try { ContextMenuService.Registrar(); } catch { /* no crítico */ }
            Shutdown();
            return;
        }

        var ruta = e.Args.FirstOrDefault();

        if (ruta is not null && (File.Exists(ruta) || Directory.Exists(ruta)))
        {
            new QuickWindow(ruta).Show();
            return;
        }

        RegistrarMenuContextualSiHaceFalta();
        new MainWindow().Show();
    }

    private static void RegistrarMenuContextualSiHaceFalta()
    {
        try
        {
            if (!ContextMenuService.EstaRegistrado())
                ContextMenuService.Registrar();
        }
        catch
        {
            // La integración con el menú contextual es una comodidad, no algo crítico:
            // si falla (p. ej. políticas de registro restringidas), la app sigue funcionando igual.
        }
    }
}
