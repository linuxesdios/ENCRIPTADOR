using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Encriptador;

/// <summary>
/// Sin argumentos abre la ventana principal completa. Con un argumento
/// (ruta de archivo o carpeta, para cuando en el futuro se integre con el
/// menú contextual del explorador de archivos del escritorio Linux que se
/// use) abre la ventana rápida en modo encriptar o desencriptar.
/// </summary>
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var ruta = desktop.Args?.FirstOrDefault();

            desktop.MainWindow = ruta is not null && (File.Exists(ruta) || Directory.Exists(ruta))
                ? new QuickWindow(ruta)
                : new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
