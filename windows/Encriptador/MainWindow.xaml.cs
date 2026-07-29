using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Encriptador.Services;
using Microsoft.Win32;

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
        ActualizarModoUI();
    }

    // ===================== Selección de archivo(s)/carpeta =====================

    private void ZonaArchivo_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar archivo(s)",
            Filter = "Todos los archivos (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
            SeleccionarRutas(dialog.FileNames);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } rutas)
        {
            SeleccionarRutas(rutas);
        }
    }

    private void SeleccionarRutas(string[] rutas)
    {
        var validas = rutas.Where(r => File.Exists(r) || Directory.Exists(r)).ToArray();
        if (validas.Length == 0)
            return;

        if (validas.Length > 1 && validas.Any(Directory.Exists))
        {
            MostrarEstado("⚠️", "Para carpetas, elegí una por vez (no se puede combinar con otros archivos).", EstadoTipo.Error);
            return;
        }

        _rutasSeleccionadas.Clear();
        _rutasSeleccionadas.AddRange(validas);

        if (validas.Length == 1)
        {
            var ruta = validas[0];
            var esCarpeta = Directory.Exists(ruta);
            TxtNombreArchivo.Text = esCarpeta ? $"Carpeta: {Path.GetFileName(ruta)}" : Path.GetFileName(ruta);
            IconoArchivo.Text = esCarpeta ? "\U0001F4C1" : "\U0001F4C4";
        }
        else
        {
            TxtNombreArchivo.Text = $"{validas.Length} archivos seleccionados";
            IconoArchivo.Text = "\U0001F4E6";
        }

        ActualizarModoUI();
        LimpiarEstado();
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
        PanelModoEncriptar.Visibility = modoEncriptar ? Visibility.Visible : Visibility.Collapsed;
        ChkConservarOriginal.Content = modoEncriptar
            ? "Conservar el archivo original (no borrarlo al terminar)"
            : "Conservar el archivo encriptado .enc (no borrarlo al terminar)";
    }

    // ===================== Contraseña: mostrar/ocultar y fuerza =====================

    private string ObtenerPassword() => _mostrandoPassword ? TxtPasswordVisible.Text : TxtPassword.Password;

    private string ObtenerConfirmar() => _mostrandoPassword ? TxtConfirmarVisible.Text : TxtConfirmar.Password;

    private void BtnMostrarPassword_Click(object sender, RoutedEventArgs e)
    {
        _mostrandoPassword = !_mostrandoPassword;

        if (_mostrandoPassword)
        {
            TxtPasswordVisible.Text = TxtPassword.Password;
            TxtConfirmarVisible.Text = TxtConfirmar.Password;
        }
        else
        {
            TxtPassword.Password = TxtPasswordVisible.Text;
            TxtConfirmar.Password = TxtConfirmarVisible.Text;
        }

        TxtPassword.Visibility = _mostrandoPassword ? Visibility.Collapsed : Visibility.Visible;
        TxtPasswordVisible.Visibility = _mostrandoPassword ? Visibility.Visible : Visibility.Collapsed;
        TxtConfirmar.Visibility = _mostrandoPassword ? Visibility.Collapsed : Visibility.Visible;
        TxtConfirmarVisible.Visibility = _mostrandoPassword ? Visibility.Visible : Visibility.Collapsed;

        BtnMostrarPassword.Content = _mostrandoPassword ? "Ocultar" : "Mostrar";
    }

    private void TxtPassword_Changed(object sender, RoutedEventArgs e) => ActualizarFuerza();

    private void TxtPasswordVisible_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => ActualizarFuerza();

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
            PasswordStrengthHelper.Nivel.MuyDebil or PasswordStrengthHelper.Nivel.Debil => (Brush)FindResource("BrushError"),
            PasswordStrengthHelper.Nivel.Media => new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23)),
            _ => (Brush)FindResource("BrushExito"),
        };
        var apagado = (Brush)FindResource("BrushBorde");

        var segmentos = new[] { Segmento1, Segmento2, Segmento3, Segmento4 };
        for (var i = 0; i < segmentos.Length; i++)
            segmentos[i].Background = i < encendidos ? color : apagado;

        TxtFuerza.Text = string.IsNullOrEmpty(password) ? string.Empty : PasswordStrengthHelper.Texto(nivel);
    }

    private void Password_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (EsModoEncriptar())
            BtnEncriptar_Click(sender, e);
        else
            BtnDesencriptar_Click(sender, e);
    }

    private void ChkConservarOriginal_Changed(object sender, RoutedEventArgs e)
    {
        var conservar = ChkConservarOriginal.IsChecked == true;
        ChkBorradoSeguro.IsEnabled = !conservar;
        if (conservar)
            ChkBorradoSeguro.IsChecked = false;
    }

    private void BtnAcerca_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    // ===================== Encriptar =====================

    private async void BtnEncriptar_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidarSeleccion())
            return;

        var password = ObtenerPassword();
        if (string.IsNullOrEmpty(password))
        {
            MostrarEstado("⚠️", "Ingresá una contraseña.", EstadoTipo.Error);
            return;
        }

        if (password != ObtenerConfirmar())
        {
            MostrarEstado("⚠️", "Las contraseñas no coinciden.", EstadoTipo.Error);
            return;
        }

        var conservarOriginal = ChkConservarOriginal.IsChecked == true;
        var borradoSeguro = ChkBorradoSeguro.IsChecked == true;

        if (_rutasSeleccionadas.Count > 1)
        {
            var dialogoGuardar = new SaveFileDialog
            {
                Title = "Guardar como",
                Filter = "Archivo encriptado (*.enc)|*.enc",
                FileName = $"{_rutasSeleccionadas.Count} archivos.enc",
                InitialDirectory = Path.GetDirectoryName(_rutasSeleccionadas[0])
            };
            if (dialogoGuardar.ShowDialog(this) != true)
                return;

            var destino = dialogoGuardar.FileName;
            var progreso = CrearProgreso();
            await EjecutarOperacionAsync(
                () => CryptoService.EncriptarVarios(_rutasSeleccionadas, destino, password, conservarOriginal, borradoSeguro, progreso),
                n => $"{n} archivos encriptados en un solo paquete.");
            return;
        }

        var ruta = _rutasSeleccionadas[0];
        var esCarpeta = Directory.Exists(ruta);
        var progresoUno = esCarpeta ? CrearProgreso() : null;

        await EjecutarOperacionAsync(
            () => CryptoService.Encriptar(ruta, password, conservarOriginal, borradoSeguro, progresoUno),
            r => esCarpeta
                ? $"Carpeta encriptada en un solo archivo ({r.ArchivosIncluidos} archivos incluidos)."
                : "Archivo encriptado correctamente.");
    }

    // ===================== Desencriptar =====================

    private async void BtnDesencriptar_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidarSeleccion())
            return;

        if (_rutasSeleccionadas.Count > 1)
        {
            MostrarEstado("⚠️", "Para desencriptar, elegí un solo archivo .enc.", EstadoTipo.Error);
            return;
        }

        var ruta = _rutasSeleccionadas[0];
        var password = ObtenerPassword();
        if (string.IsNullOrEmpty(password))
        {
            MostrarEstado("⚠️", "Ingresá una contraseña.", EstadoTipo.Error);
            return;
        }

        if (Directory.Exists(ruta))
        {
            MostrarEstado("⚠️", "Para desencriptar, elegí un archivo .enc (no una carpeta).", EstadoTipo.Error);
            return;
        }

        var conservarOriginal = ChkConservarOriginal.IsChecked == true;

        if (CryptoService.EsContenedorDeCarpeta(ruta))
        {
            await ExplorarYExtraerCarpeta(ruta, password, conservarOriginal);
            return;
        }

        await EjecutarOperacionAsync(
            () => CryptoService.Desencriptar(ruta, password, conservarOriginal),
            _ => "Archivo desencriptado correctamente.");
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
        MostrarEstado("⏳", "Verificando contraseña...", EstadoTipo.Info);

        try
        {
            var sesion = await Task.Run(() => CryptoService.AbrirCarpeta(ruta, password));

            var explorador = new ExplorerWindow(Path.GetFileName(ruta), sesion) { Owner = this };
            explorador.ShowDialog();

            if (explorador.Confirmado)
            {
                var seleccionados = explorador.Seleccionados;
                var destinoDir = Path.GetDirectoryName(ruta) ?? string.Empty;

                MostrarEstado("⏳", "Extrayendo...", EstadoTipo.Info);
                var progreso = CrearProgreso();
                await Task.Run(() => sesion.ExtraerVarios(seleccionados, destinoDir, progreso));

                if (!conservarOriginal)
                    File.Delete(ruta);

                MostrarEstado("✅", $"Carpeta restaurada: {seleccionados.Count} de {sesion.Archivos.Count} archivos.", EstadoTipo.Exito);
            }
            else
            {
                MostrarEstado("ℹ️", "Operación cancelada. No se modificó nada.", EstadoTipo.Info);
            }
        }
        catch (CryptographicException)
        {
            MostrarEstado("❌", "No se pudo desencriptar: contraseña incorrecta o archivo dañado.", EstadoTipo.Error);
        }
        catch (Exception ex)
        {
            MostrarEstado("❌", $"Error: {ex.Message}", EstadoTipo.Error);
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
            MostrarEstado("⚠️", "Seleccioná un archivo o carpeta válido.", EstadoTipo.Error);
            return false;
        }
        return true;
    }

    private IProgress<(int Actual, int Total)> CrearProgreso() =>
        new Progress<(int Actual, int Total)>(p =>
        {
            BarraProgreso.IsIndeterminate = false;
            BarraProgreso.Minimum = 0;
            BarraProgreso.Maximum = p.Total;
            BarraProgreso.Value = p.Actual;
            MostrarEstado("⏳", $"Procesando archivo {p.Actual} de {p.Total}...", EstadoTipo.Info);
        });

    private async Task EjecutarOperacionAsync<T>(Func<T> operacion, Func<T, string> mensajeExito)
    {
        SetControlesHabilitados(false);
        BarraProgreso.IsIndeterminate = true;
        MostrarEstado("⏳", "Procesando...", EstadoTipo.Info);

        try
        {
            var resultado = await Task.Run(operacion);
            MostrarEstado("✅", mensajeExito(resultado), EstadoTipo.Exito);
        }
        catch (CryptographicException)
        {
            MostrarEstado("❌", "No se pudo desencriptar: contraseña incorrecta o archivo dañado.", EstadoTipo.Error);
        }
        catch (InvalidOperationException ex)
        {
            MostrarEstado("⚠️", ex.Message, EstadoTipo.Error);
        }
        catch (Exception ex)
        {
            MostrarEstado("❌", $"Error: {ex.Message}", EstadoTipo.Error);
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
            EstadoTipo.Exito => (Brush)FindResource("BrushExito"),
            EstadoTipo.Error => (Brush)FindResource("BrushError"),
            _ => (Brush)FindResource("BrushTextoSecundario"),
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
        TxtPasswordVisible.IsEnabled = habilitados;
        TxtConfirmar.IsEnabled = habilitados;
        TxtConfirmarVisible.IsEnabled = habilitados;
        BtnMostrarPassword.IsEnabled = habilitados;
        ChkConservarOriginal.IsEnabled = habilitados;
        ChkBorradoSeguro.IsEnabled = habilitados && ChkConservarOriginal.IsChecked != true;
    }
}
