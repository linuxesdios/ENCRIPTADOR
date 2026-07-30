using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Encriptador.Services;

namespace Encriptador;

/// <summary>
/// Ventana liviana usada desde el menú contextual de Windows: recibe una
/// ruta (archivo o carpeta), decide si corresponde encriptar o desencriptar,
/// pide la contraseña y ejecuta la operación.
/// </summary>
public partial class QuickWindow : Window
{
    private readonly string _ruta;
    private readonly bool _esDesencriptar;
    private readonly bool _esCarpeta;
    private bool _mostrandoPassword;

    public QuickWindow(string ruta)
    {
        InitializeComponent();

        _ruta = ruta;
        _esDesencriptar = File.Exists(ruta) && CryptoService.EsArchivoEncriptado(ruta);
        _esCarpeta = Directory.Exists(ruta);

        Icono.Text = _esDesencriptar ? "\U0001F511" : "\U0001F512";
        PanelModoEncriptar.Visibility = _esDesencriptar ? Visibility.Collapsed : Visibility.Visible;
        AplicarIdioma();

        Loaded += (_, _) => TxtPassword.Focus();
    }

    // ===================== Idioma =====================

    private void AplicarIdioma()
    {
        var nombreArchivo = Path.GetFileName(_ruta.TrimEnd('\\'));
        TxtNombre.Text = Loc.T(_esCarpeta ? "main.archivo.carpeta" : "quick.archivo.archivo", nombreArchivo);
        TxtTitulo.Text = Loc.T(_esDesencriptar ? "common.desencriptar" : "common.encriptar");
        BtnAccion.Content = Loc.T(_esDesencriptar ? "common.desencriptar" : "common.encriptar");
        LblContrasena.Text = Loc.T("common.contrasena");
        LblRepetir.Text = Loc.T("common.repetirContrasena");
        BtnMostrarPassword.Content = Loc.T(_mostrandoPassword ? "common.ocultar" : "common.mostrar");
        ChkConservarOriginal.Content = Loc.T(_esDesencriptar
            ? "common.chk.conservarOriginal.desencriptar"
            : "common.chk.conservarOriginal.encriptar");
        ChkBorradoSeguro.Content = Loc.T("common.chk.borradoSeguro");

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
        activo.Background = (Brush)FindResource("BrushAcento1Solido");
    }

    private void BtnIdioma_Click(object sender, RoutedEventArgs e)
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

        BtnMostrarPassword.Content = Loc.T(_mostrandoPassword ? "common.ocultar" : "common.mostrar");
    }

    private void TxtPassword_Changed(object sender, RoutedEventArgs e) => ActualizarFuerza();

    private void TxtPasswordVisible_Changed(object sender, TextChangedEventArgs e) => ActualizarFuerza();

    private void ActualizarFuerza()
    {
        if (_esDesencriptar)
            return;

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

    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            BtnAccion_Click(sender, e);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();

    private async void BtnAccion_Click(object sender, RoutedEventArgs e)
    {
        var password = ObtenerPassword();
        if (string.IsNullOrEmpty(password))
        {
            MostrarEstado("⚠️", Loc.T("common.estado.passwordRequerida"), Estado.Error);
            return;
        }

        if (!_esDesencriptar && password != ObtenerConfirmar())
        {
            MostrarEstado("⚠️", Loc.T("common.estado.passwordsNoCoinciden"), Estado.Error);
            return;
        }

        SetControlesHabilitados(false);
        MostrarEstado("⏳", Loc.T("common.estado.procesando"), Estado.Info);

        try
        {
            var conservarOriginal = ChkConservarOriginal.IsChecked == true;

            string texto;
            var cancelado = false;

            if (_esDesencriptar && CryptoService.EsContenedorDeCarpeta(_ruta))
            {
                var sesion = await Task.Run(() => CryptoService.AbrirCarpeta(_ruta, password));

                var explorador = new ExplorerWindow(Path.GetFileName(_ruta), sesion) { Owner = this };
                explorador.ShowDialog();

                if (explorador.Confirmado)
                {
                    var seleccionados = explorador.Seleccionados;
                    var destinoDir = Path.GetDirectoryName(_ruta) ?? string.Empty;

                    var progreso = new Progress<(int Actual, int Total)>(p =>
                        MostrarEstado("⏳", Loc.T("common.estado.procesandoArchivo", p.Actual, p.Total), Estado.Info));
                    await Task.Run(() => sesion.ExtraerVarios(seleccionados, destinoDir, progreso));

                    if (!conservarOriginal)
                        File.Delete(_ruta);

                    texto = Loc.T("common.estado.carpetaRestaurada", seleccionados.Count, sesion.Archivos.Count);
                }
                else
                {
                    texto = Loc.T("common.estado.operacionCancelada");
                    cancelado = true;
                }
            }
            else if (_esDesencriptar)
            {
                await Task.Run(() => CryptoService.Desencriptar(_ruta, password, conservarOriginal));
                texto = Loc.T("common.estado.archivoDesencriptado");
            }
            else
            {
                var borradoSeguro = ChkBorradoSeguro.IsChecked == true;
                var (procesados, _) = await Task.Run(() => CryptoService.Encriptar(_ruta, password, conservarOriginal, borradoSeguro));
                texto = _esCarpeta
                    ? Loc.T("common.estado.carpetaEncriptada", procesados)
                    : Loc.T("common.estado.archivoEncriptado");
            }

            MostrarEstado(cancelado ? "ℹ️" : "✅", texto, cancelado ? Estado.Info : Estado.Exito);
            await Task.Delay(1100);
            Close();
        }
        catch (CryptographicException)
        {
            MostrarEstado("❌", Loc.T("common.estado.noPudoDesencriptar"), Estado.Error);
            SetControlesHabilitados(true);
            TxtPassword.Focus();
        }
        catch (InvalidOperationException ex)
        {
            MostrarEstado("⚠️", ex.Message, Estado.Error);
            SetControlesHabilitados(true);
        }
        catch (Exception ex)
        {
            MostrarEstado("❌", Loc.T("common.estado.error", ex.Message), Estado.Error);
            SetControlesHabilitados(true);
        }
    }

    private enum Estado { Info, Exito, Error }

    private void MostrarEstado(string icono, string texto, Estado estado)
    {
        IconoEstado.Text = icono;
        TxtEstado.Text = texto;
        TxtEstado.Foreground = estado switch
        {
            Estado.Exito => (Brush)FindResource("BrushExito"),
            Estado.Error => (Brush)FindResource("BrushError"),
            _ => (Brush)FindResource("BrushTextoSecundario"),
        };
    }

    private void SetControlesHabilitados(bool habilitados)
    {
        TxtPassword.IsEnabled = habilitados;
        TxtPasswordVisible.IsEnabled = habilitados;
        TxtConfirmar.IsEnabled = habilitados;
        TxtConfirmarVisible.IsEnabled = habilitados;
        BtnMostrarPassword.IsEnabled = habilitados;
        BtnAccion.IsEnabled = habilitados;
        ChkConservarOriginal.IsEnabled = habilitados;
        ChkBorradoSeguro.IsEnabled = habilitados;
    }
}
