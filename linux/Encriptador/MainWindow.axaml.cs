using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Encriptador.Services;

namespace Encriptador;

/// <summary>
/// Ventana principal: permite elegir uno o varios archivos, o una carpeta
/// (por examinar o arrastrando) y cifrarlos/descifrarlos con AES-256-GCM
/// usando CryptoService.
/// </summary>
public partial class MainWindow : Window
{
    private readonly List<string> _rutasSeleccionadas = new();
    private bool _mostrandoPassword;

    public MainWindow()
    {
        InitializeComponent();
        AplicarIdioma();
        ActualizarModoUI();
    }

    // ===================== Idioma =====================

    private void AplicarIdioma()
    {
        TxtTagline.Text = Loc.T("main.tagline");
        LblArchivo.Text = Loc.T("main.archivo.etiqueta");
        BtnElegirCarpeta.Content = "\U0001F4C1  " + Loc.T("main.btn.elegirCarpeta");
        ActualizarTextoArchivo();
        LblContrasena.Text = Loc.T("common.contrasena");
        LblRepetir.Text = Loc.T("common.repetirContrasena");
        BtnMostrarPassword.Content = Loc.T(_mostrandoPassword ? "common.ocultar" : "common.mostrar");
        ChkBorradoSeguro.Content = Loc.T("common.chk.borradoSeguro");
        BtnEncriptar.Content = "\U0001F512  " + Loc.T("common.encriptar");
        BtnDesencriptar.Content = "\U0001F511  " + Loc.T("common.desencriptar");
        TxtCredito.Text = Loc.T("common.footer.credito", "Pablo Martín Fernández");
        ToolTip.SetTip(BtnAcerca, Loc.T("main.tooltip.acerca"));

        var actual = Loc.IdiomaActual;
        foreach (var boton in new[] { BtnIdiomaEs, BtnIdiomaEn, BtnIdiomaRu, BtnIdiomaZh })
            boton.Background = Brushes.Transparent;
        var activo = actual switch
        {
            Idioma.En => BtnIdiomaEn,
            Idioma.Ru => BtnIdiomaRu,
            Idioma.Zh => BtnIdiomaZh,
            _ => BtnIdiomaEs,
        };
        activo.Background = Recurso<IBrush>("BrushAcento1Solido");

        ActualizarModoUI();
    }

    private void BtnIdioma_Click(object? sender, RoutedEventArgs e)
    {
        var nuevo = sender switch
        {
            var s when s == BtnIdiomaEn => Idioma.En,
            var s when s == BtnIdiomaRu => Idioma.Ru,
            var s when s == BtnIdiomaZh => Idioma.Zh,
            _ => Idioma.Es,
        };

        Loc.CambiarIdioma(nuevo);
        AplicarIdioma();
    }

    // ===================== Selección de archivo(s)/carpeta =====================

    private async void ZonaArchivo_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var opciones = new FilePickerOpenOptions
        {
            Title = Loc.T("main.dialog.abrir.titulo"),
            AllowMultiple = true
        };

        var archivos = await StorageProvider.OpenFilePickerAsync(opciones);
        var rutas = archivos
            .Select(a => a.TryGetLocalPath())
            .Where(p => p is not null)
            .Select(p => p!)
            .ToArray();

        if (rutas.Length > 0)
            SeleccionarRutas(rutas);
    }

    private async void BtnElegirCarpeta_Click(object? sender, RoutedEventArgs e)
    {
        var opciones = new FolderPickerOpenOptions
        {
            Title = Loc.T("main.dialog.abrir.titulo")
        };

        var carpetas = await StorageProvider.OpenFolderPickerAsync(opciones);
        var ruta = carpetas.FirstOrDefault()?.TryGetLocalPath();
        if (ruta is not null)
            SeleccionarRutas(new[] { ruta });
    }

    private void ZonaArchivo_PointerEntered(object? sender, PointerEventArgs e) =>
        ZonaArchivo.BorderBrush = Recurso<IBrush>("BrushAcento1Solido");

    private void ZonaArchivo_PointerExited(object? sender, PointerEventArgs e) =>
        ZonaArchivo.BorderBrush = Recurso<IBrush>("BrushBorde");

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void Window_Drop(object? sender, DragEventArgs e)
    {
        var archivos = e.DataTransfer.TryGetFiles();
        if (archivos is null)
            return;

        var rutas = archivos
            .Select(i => i.TryGetLocalPath())
            .Where(p => p is not null)
            .Select(p => p!)
            .ToArray();

        if (rutas.Length > 0)
            SeleccionarRutas(rutas);
    }

    private void SeleccionarRutas(string[] rutas)
    {
        var validas = rutas.Where(r => File.Exists(r) || Directory.Exists(r)).ToArray();
        if (validas.Length == 0)
            return;

        if (validas.Length > 1 && validas.Any(Directory.Exists))
        {
            MostrarEstado("⚠️", Loc.T("main.error.carpetaUnaPorVez"), EstadoTipo.Error);
            return;
        }

        _rutasSeleccionadas.Clear();
        _rutasSeleccionadas.AddRange(validas);

        ActualizarTextoArchivo();
        ActualizarModoUI();
        LimpiarEstado();
    }

    private void ActualizarTextoArchivo()
    {
        if (_rutasSeleccionadas.Count == 0)
        {
            TxtNombreArchivo.Text = Loc.T("main.archivo.placeholder");
            IconoArchivo.Text = "\U0001F4C1";
        }
        else if (_rutasSeleccionadas.Count == 1)
        {
            var ruta = _rutasSeleccionadas[0];
            var esCarpeta = Directory.Exists(ruta);
            TxtNombreArchivo.Text = esCarpeta ? Loc.T("main.archivo.carpeta", Path.GetFileName(ruta)) : Path.GetFileName(ruta);
            IconoArchivo.Text = esCarpeta ? "\U0001F4C1" : "\U0001F4C4";
        }
        else
        {
            TxtNombreArchivo.Text = Loc.T("main.archivo.variosSeleccionados", _rutasSeleccionadas.Count);
            IconoArchivo.Text = "\U0001F4E6";
        }
    }

    private bool EsModoEncriptar()
    {
        if (_rutasSeleccionadas.Count != 1)
            return true;

        var ruta = _rutasSeleccionadas[0];
        return !(File.Exists(ruta) && CryptoService.EsArchivoEncriptado(ruta));
    }

    private void ActualizarModoUI()
    {
        var modoEncriptar = EsModoEncriptar();
        PanelModoEncriptar.IsVisible = modoEncriptar;
        ChkConservarOriginal.Content = Loc.T(modoEncriptar
            ? "common.chk.conservarOriginal.encriptar"
            : "common.chk.conservarOriginal.desencriptar");
    }

    // ===================== Contraseña: mostrar/ocultar y fuerza =====================

    private string ObtenerPassword() => TxtPassword.Text ?? string.Empty;

    private string ObtenerConfirmar() => TxtConfirmar.Text ?? string.Empty;

    private void BtnMostrarPassword_Click(object? sender, RoutedEventArgs e)
    {
        _mostrandoPassword = !_mostrandoPassword;
        var caracter = _mostrandoPassword ? default(char) : '●';
        TxtPassword.PasswordChar = caracter;
        TxtConfirmar.PasswordChar = caracter;
        BtnMostrarPassword.Content = Loc.T(_mostrandoPassword ? "common.ocultar" : "common.mostrar");
    }

    private void TxtPassword_Changed(object? sender, TextChangedEventArgs e) => ActualizarFuerza();

    private void ActualizarFuerza()
    {
        var password = ObtenerPassword();
        var (nivel, _) = PasswordStrengthHelper.Evaluar(password);

        var encendidos = string.IsNullOrEmpty(password) ? 0 : nivel switch
        {
            PasswordStrengthHelper.Nivel.MuyDebil => 1,
            PasswordStrengthHelper.Nivel.Debil => 2,
            PasswordStrengthHelper.Nivel.Media => 3,
            _ => 4,
        };

        var color = nivel switch
        {
            PasswordStrengthHelper.Nivel.MuyDebil or PasswordStrengthHelper.Nivel.Debil => Recurso<IBrush>("BrushError"),
            PasswordStrengthHelper.Nivel.Media => new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23)),
            _ => Recurso<IBrush>("BrushExito"),
        };
        var apagado = Recurso<IBrush>("BrushBorde");

        var segmentos = new[] { Segmento1, Segmento2, Segmento3, Segmento4 };
        for (var i = 0; i < segmentos.Length; i++)
            segmentos[i].Background = i < encendidos ? color : apagado;

        TxtFuerza.Text = string.IsNullOrEmpty(password) ? string.Empty : PasswordStrengthHelper.Texto(nivel);
    }

    private void Password_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (EsModoEncriptar())
            BtnEncriptar_Click(sender, e);
        else
            BtnDesencriptar_Click(sender, e);
    }

    private void ChkConservarOriginal_Changed(object? sender, RoutedEventArgs e)
    {
        var conservar = ChkConservarOriginal.IsChecked == true;
        ChkBorradoSeguro.IsEnabled = !conservar;
        if (conservar)
            ChkBorradoSeguro.IsChecked = false;
    }

    private async void BtnAcerca_Click(object? sender, RoutedEventArgs e)
    {
        await new AboutWindow().ShowDialog(this);
    }

    // ===================== Encriptar =====================

    private async void BtnEncriptar_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidarSeleccion())
            return;

        var password = ObtenerPassword();
        if (string.IsNullOrEmpty(password))
        {
            MostrarEstado("⚠️", Loc.T("common.estado.passwordRequerida"), EstadoTipo.Error);
            return;
        }

        if (password != ObtenerConfirmar())
        {
            MostrarEstado("⚠️", Loc.T("common.estado.passwordsNoCoinciden"), EstadoTipo.Error);
            return;
        }

        var conservarOriginal = ChkConservarOriginal.IsChecked == true;
        var borradoSeguro = ChkBorradoSeguro.IsChecked == true;

        if (_rutasSeleccionadas.Count > 1)
        {
            var carpetaInicial = await StorageProvider.TryGetFolderFromPathAsync(Path.GetDirectoryName(_rutasSeleccionadas[0])!);
            var opciones = new FilePickerSaveOptions
            {
                Title = Loc.T("main.dialog.guardar.titulo"),
                SuggestedFileName = Loc.T("main.dialog.guardar.nombreVarios", _rutasSeleccionadas.Count),
                DefaultExtension = "enc",
                SuggestedStartLocation = carpetaInicial,
                FileTypeChoices = new[] { new FilePickerFileType(Loc.T("main.dialog.guardar.filtroEtiqueta")) { Patterns = new[] { "*.enc" } } }
            };

            var archivoDestino = await StorageProvider.SaveFilePickerAsync(opciones);
            var destino = archivoDestino?.TryGetLocalPath();
            if (destino is null)
                return;

            var progreso = CrearProgreso();
            await EjecutarOperacionAsync(
                () => CryptoService.EncriptarVarios(_rutasSeleccionadas, destino, password, conservarOriginal, borradoSeguro, progreso),
                n => Loc.T("common.estado.variosEncriptados", n));
            return;
        }

        var ruta = _rutasSeleccionadas[0];
        var esCarpeta = Directory.Exists(ruta);
        var progresoUno = CrearProgreso();

        await EjecutarOperacionAsync(
            () => CryptoService.Encriptar(ruta, password, conservarOriginal, borradoSeguro, progresoUno),
            r => esCarpeta
                ? Loc.T("common.estado.carpetaEncriptada", r.ArchivosIncluidos)
                : Loc.T("common.estado.archivoEncriptado"));
    }

    // ===================== Desencriptar =====================

    private async void BtnDesencriptar_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidarSeleccion())
            return;

        if (_rutasSeleccionadas.Count > 1)
        {
            MostrarEstado("⚠️", Loc.T("main.error.soloUnArchivoEnc"), EstadoTipo.Error);
            return;
        }

        var ruta = _rutasSeleccionadas[0];
        var password = ObtenerPassword();
        if (string.IsNullOrEmpty(password))
        {
            MostrarEstado("⚠️", Loc.T("common.estado.passwordRequerida"), EstadoTipo.Error);
            return;
        }

        if (Directory.Exists(ruta))
        {
            MostrarEstado("⚠️", Loc.T("main.error.noEsCarpeta"), EstadoTipo.Error);
            return;
        }

        var conservarOriginal = ChkConservarOriginal.IsChecked == true;

        if (CryptoService.EsContenedorDeCarpeta(ruta))
        {
            await ExplorarYExtraerCarpeta(ruta, password, conservarOriginal);
            return;
        }

        var progreso = CrearProgreso();
        await EjecutarOperacionAsync(
            () => CryptoService.Desencriptar(ruta, password, conservarOriginal, progreso),
            _ => Loc.T("common.estado.archivoDesencriptado"));
    }

    /// <summary>
    /// Valida la contraseña y abre el explorador del contenido de la carpeta
    /// encriptada. No se descifra ningún archivo hasta que el usuario abre
    /// uno puntual (doble clic, dentro del explorador) o confirma una
    /// extracción de los seleccionados.
    /// </summary>
    private async Task ExplorarYExtraerCarpeta(string ruta, string password, bool conservarOriginal)
    {
        SetControlesHabilitados(false);
        BarraProgreso.IsIndeterminate = true;
        MostrarEstado("⏳", Loc.T("common.estado.verificandoPassword"), EstadoTipo.Info);

        try
        {
            var sesion = await Task.Run(() => CryptoService.AbrirCarpeta(ruta, password));

            var explorador = new ExplorerWindow(Path.GetFileName(ruta), sesion);
            await explorador.ShowDialog(this);

            if (explorador.Confirmado)
            {
                var seleccionados = explorador.Seleccionados;
                var destinoDir = Path.GetDirectoryName(ruta) ?? string.Empty;

                var progreso = CrearProgreso();
                await Task.Run(() => sesion.ExtraerVarios(seleccionados, destinoDir, progreso));

                if (!conservarOriginal)
                    File.Delete(ruta);

                MostrarEstado("✅", Loc.T("common.estado.carpetaRestaurada", seleccionados.Count, sesion.Archivos.Count), EstadoTipo.Exito);
            }
            else
            {
                MostrarEstado("ℹ️", Loc.T("common.estado.operacionCancelada"), EstadoTipo.Info);
            }
        }
        catch (CryptographicException)
        {
            MostrarEstado("❌", Loc.T("common.estado.noPudoDesencriptar"), EstadoTipo.Error);
        }
        catch (Exception ex)
        {
            MostrarEstado("❌", Loc.T("common.estado.error", ex.Message), EstadoTipo.Error);
        }
        finally
        {
            BarraProgreso.IsIndeterminate = false;
            SetControlesHabilitados(true);
        }
    }

    // ===================== Helpers comunes =====================

    private bool ValidarSeleccion()
    {
        if (_rutasSeleccionadas.Count == 0 || !_rutasSeleccionadas.All(r => File.Exists(r) || Directory.Exists(r)))
        {
            MostrarEstado("⚠️", Loc.T("main.error.seleccionInvalida"), EstadoTipo.Error);
            return false;
        }
        return true;
    }

    private IProgress<(long BytesHechos, long BytesTotal)> CrearProgreso()
    {
        var cronometro = Stopwatch.StartNew();
        return new Progress<(long BytesHechos, long BytesTotal)>(p =>
        {
            if (p.BytesTotal <= 0)
                return;

            BarraProgreso.IsIndeterminate = false;
            BarraProgreso.Minimum = 0;
            BarraProgreso.Maximum = p.BytesTotal;
            BarraProgreso.Value = p.BytesHechos;

            var porcentaje = (int)(p.BytesHechos * 100.0 / p.BytesTotal);
            var elapsed = cronometro.Elapsed.TotalSeconds;
            if (elapsed > 0.3 && p.BytesHechos > 0)
            {
                var throughput = p.BytesHechos / elapsed;
                var etaSegundos = (p.BytesTotal - p.BytesHechos) / throughput;
                MostrarEstado("⏳", Loc.T("common.estado.progresoConEta", porcentaje, FormatearTiempo(etaSegundos)), EstadoTipo.Info);
            }
            else
            {
                MostrarEstado("⏳", Loc.T("common.estado.progreso", porcentaje), EstadoTipo.Info);
            }
        });
    }

    private static string FormatearTiempo(double segundos)
    {
        var total = Math.Max(0, (int)Math.Round(segundos));
        if (total < 60)
            return $"{total}s";
        return $"{total / 60}m {total % 60}s";
    }

    private async Task EjecutarOperacionAsync<T>(Func<T> operacion, Func<T, string> mensajeExito)
    {
        SetControlesHabilitados(false);
        BarraProgreso.IsIndeterminate = true;
        MostrarEstado("⏳", Loc.T("common.estado.procesando"), EstadoTipo.Info);

        try
        {
            var resultado = await Task.Run(operacion);
            MostrarEstado("✅", mensajeExito(resultado), EstadoTipo.Exito);
        }
        catch (CryptographicException)
        {
            MostrarEstado("❌", Loc.T("common.estado.noPudoDesencriptar"), EstadoTipo.Error);
        }
        catch (InvalidOperationException ex)
        {
            MostrarEstado("⚠️", ex.Message, EstadoTipo.Error);
        }
        catch (Exception ex)
        {
            MostrarEstado("❌", Loc.T("common.estado.error", ex.Message), EstadoTipo.Error);
        }
        finally
        {
            BarraProgreso.IsIndeterminate = false;
            SetControlesHabilitados(true);
        }
    }

    private enum EstadoTipo { Info, Exito, Error }

    private void MostrarEstado(string icono, string texto, EstadoTipo tipo)
    {
        IconoEstado.Text = icono;
        TxtEstado.Text = texto;
        TxtEstado.Foreground = tipo switch
        {
            EstadoTipo.Exito => Recurso<IBrush>("BrushExito"),
            EstadoTipo.Error => Recurso<IBrush>("BrushError"),
            _ => Recurso<IBrush>("BrushTextoSecundario"),
        };
    }

    private void LimpiarEstado()
    {
        IconoEstado.Text = string.Empty;
        TxtEstado.Text = string.Empty;
    }

    private void SetControlesHabilitados(bool habilitados)
    {
        ZonaArchivo.IsEnabled = habilitados;
        BtnEncriptar.IsEnabled = habilitados;
        BtnDesencriptar.IsEnabled = habilitados;
        TxtPassword.IsEnabled = habilitados;
        TxtConfirmar.IsEnabled = habilitados;
        BtnMostrarPassword.IsEnabled = habilitados;
        ChkConservarOriginal.IsEnabled = habilitados;
        ChkBorradoSeguro.IsEnabled = habilitados && ChkConservarOriginal.IsChecked != true;
    }

    private static T Recurso<T>(string clave) where T : class =>
        (T)Application.Current!.Resources[clave]!;
}
