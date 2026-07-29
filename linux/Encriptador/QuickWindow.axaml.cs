using System.IO;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Encriptador.Services;

namespace Encriptador;

/// <summary>
/// Ventana liviana pensada para invocarse con una ruta como argumento (por
/// ejemplo, desde una futura integración con el explorador de archivos del
/// escritorio Linux que se use): recibe una ruta (archivo o carpeta),
/// decide si corresponde encriptar o desencriptar, pide la contraseña y
/// ejecuta la operación.
/// </summary>
public partial class QuickWindow : Window
{
    private readonly string _ruta;
    private readonly bool _esDesencriptar;
    private readonly bool _esCarpeta;

    public QuickWindow(string ruta)
    {
        InitializeComponent();

        _ruta = ruta;
        _esDesencriptar = File.Exists(ruta) && CryptoService.EsArchivoEncriptado(ruta);
        _esCarpeta = Directory.Exists(ruta);

        TxtNombre.Text = (_esCarpeta ? "Carpeta: " : "Archivo: ") + Path.GetFileName(ruta.TrimEnd('/', '\\'));
        Icono.Text = _esDesencriptar ? "\U0001F511" : "\U0001F512";
        TxtTitulo.Text = _esDesencriptar ? "Desencriptar" : "Encriptar";
        BtnAccion.Content = _esDesencriptar ? "Desencriptar" : "Encriptar";
        ChkConservarOriginal.Content = _esDesencriptar
            ? "Conservar el archivo encriptado (.enc)"
            : "Conservar el archivo original";
        PanelModoEncriptar.IsVisible = !_esDesencriptar;

        Opened += (_, _) => TxtPassword.Focus();
    }

    private string ObtenerPassword() => TxtPassword.Text ?? string.Empty;

    private string ObtenerConfirmar() => TxtConfirmar.Text ?? string.Empty;

    private bool _mostrandoPassword;

    private void BtnMostrarPassword_Click(object? sender, RoutedEventArgs e)
    {
        _mostrandoPassword = !_mostrandoPassword;
        var caracter = _mostrandoPassword ? default(char) : '●';
        TxtPassword.PasswordChar = caracter;
        TxtConfirmar.PasswordChar = caracter;
        BtnMostrarPassword.Content = _mostrandoPassword ? "Ocultar" : "Mostrar";
    }

    private void TxtPassword_Changed(object? sender, TextChangedEventArgs e) => ActualizarFuerza();

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

    private void TxtPassword_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            BtnAccion_Click(sender, e);
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void BtnCerrar_Click(object? sender, RoutedEventArgs e) => Close();

    private async void BtnAccion_Click(object? sender, RoutedEventArgs e)
    {
        var password = ObtenerPassword();
        if (string.IsNullOrEmpty(password))
        {
            MostrarEstado("⚠️", "Ingresá una contraseña.", Estado.Error);
            return;
        }

        if (!_esDesencriptar && password != ObtenerConfirmar())
        {
            MostrarEstado("⚠️", "Las contraseñas no coinciden.", Estado.Error);
            return;
        }

        SetControlesHabilitados(false);
        MostrarEstado("⏳", "Procesando...", Estado.Info);

        try
        {
            var conservarOriginal = ChkConservarOriginal.IsChecked == true;

            string texto;
            var cancelado = false;

            if (_esDesencriptar && CryptoService.EsContenedorDeCarpeta(_ruta))
            {
                var sesion = await Task.Run(() => CryptoService.AbrirCarpeta(_ruta, password));

                var explorador = new ExplorerWindow(Path.GetFileName(_ruta), sesion);
                await explorador.ShowDialog(this);

                if (explorador.Confirmado)
                {
                    var seleccionados = explorador.Seleccionados;
                    var destinoDir = Path.GetDirectoryName(_ruta) ?? string.Empty;

                    var progreso = new Progress<(int Actual, int Total)>(p =>
                        MostrarEstado("⏳", $"Extrayendo {p.Actual} de {p.Total}...", Estado.Info));
                    await Task.Run(() => sesion.ExtraerVarios(seleccionados, destinoDir, progreso));

                    if (!conservarOriginal)
                        File.Delete(_ruta);

                    texto = $"Carpeta restaurada: {seleccionados.Count} de {sesion.Archivos.Count} archivos.";
                }
                else
                {
                    texto = "Operación cancelada.";
                    cancelado = true;
                }
            }
            else if (_esDesencriptar)
            {
                await Task.Run(() => CryptoService.Desencriptar(_ruta, password, conservarOriginal));
                texto = "Archivo desencriptado.";
            }
            else
            {
                var borradoSeguro = ChkBorradoSeguro.IsChecked == true;
                var (procesados, _) = await Task.Run(() => CryptoService.Encriptar(_ruta, password, conservarOriginal, borradoSeguro));
                texto = _esCarpeta
                    ? $"Carpeta encriptada en un solo archivo ({procesados} archivos)."
                    : "Archivo encriptado.";
            }

            MostrarEstado(cancelado ? "ℹ️" : "✅", texto, cancelado ? Estado.Info : Estado.Exito);
            await Task.Delay(1100);
            Close();
        }
        catch (CryptographicException)
        {
            MostrarEstado("❌", "Contraseña incorrecta o archivo dañado.", Estado.Error);
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
            MostrarEstado("❌", $"Error: {ex.Message}", Estado.Error);
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
            Estado.Exito => Recurso<IBrush>("BrushExito"),
            Estado.Error => Recurso<IBrush>("BrushError"),
            _ => Recurso<IBrush>("BrushTextoSecundario"),
        };
    }

    private void SetControlesHabilitados(bool habilitados)
    {
        TxtPassword.IsEnabled = habilitados;
        TxtConfirmar.IsEnabled = habilitados;
        BtnMostrarPassword.IsEnabled = habilitados;
        BtnAccion.IsEnabled = habilitados;
        ChkConservarOriginal.IsEnabled = habilitados;
        ChkBorradoSeguro.IsEnabled = habilitados;
    }

    private static T Recurso<T>(string clave) where T : class =>
        (T)Application.Current!.Resources[clave]!;
}
