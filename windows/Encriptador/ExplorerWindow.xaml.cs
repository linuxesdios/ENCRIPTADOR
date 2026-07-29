using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Encriptador.Services;

namespace Encriptador;

/// <summary>
/// Explorador tipo WinRAR: muestra el árbol de una carpeta encriptada sin
/// descifrar ningún contenido todavía (solo se validó la contraseña y se
/// leyó el manifiesto). Doble clic en un archivo lo descifra a un temporal
/// y lo abre con la app asociada; "Extraer seleccionados" descifra
/// únicamente los elegidos hacia el destino final.
/// </summary>
public partial class ExplorerWindow : Window
{
    public bool Confirmado { get; private set; }
    public List<string> Seleccionados { get; private set; } = new();

    private readonly CryptoService.SesionCarpeta _sesion;
    private readonly List<(string Relativa, CheckBox Casilla)> _entradas = new();
    private readonly List<string> _temporalesAbiertos = new();

    private sealed class NodoCarpeta
    {
        public string Nombre = "";
        public readonly SortedDictionary<string, NodoCarpeta> Subcarpetas = new(StringComparer.OrdinalIgnoreCase);
        public readonly List<(string Relativa, long Longitud)> Archivos = new();
    }

    public ExplorerWindow(string nombreOrigen, CryptoService.SesionCarpeta sesion)
    {
        InitializeComponent();
        _sesion = sesion;

        TxtTitulo.Text = nombreOrigen;
        var archivos = _sesion.Archivos;
        TxtSubtitulo.Text = archivos.Count == 1 ? "1 archivo" : $"{archivos.Count} archivos";

        var raiz = ConstruirArbol(archivos);
        foreach (var control in ConstruirNodosCarpeta(raiz, 0))
            ListaArchivos.Children.Add(control);

        ActualizarContador();
        Closed += (_, _) => LimpiarTemporales();
    }

    private static NodoCarpeta ConstruirArbol(IReadOnlyList<(string RutaRelativa, long Longitud)> archivos)
    {
        var raiz = new NodoCarpeta();
        foreach (var (relativa, longitud) in archivos)
        {
            var partes = relativa.Split('/');
            var actual = raiz;
            for (var i = 0; i < partes.Length - 1; i++)
            {
                if (!actual.Subcarpetas.TryGetValue(partes[i], out var siguiente))
                {
                    siguiente = new NodoCarpeta { Nombre = partes[i] };
                    actual.Subcarpetas[partes[i]] = siguiente;
                }
                actual = siguiente;
            }
            actual.Archivos.Add((relativa, longitud));
        }
        return raiz;
    }

    private IEnumerable<UIElement> ConstruirNodosCarpeta(NodoCarpeta nodo, int profundidad)
    {
        foreach (var subcarpeta in nodo.Subcarpetas.Values)
        {
            var panelHijos = new StackPanel();
            foreach (var hijo in ConstruirNodosCarpeta(subcarpeta, profundidad + 1))
                panelHijos.Children.Add(hijo);

            yield return ConstruirFilaCarpeta(subcarpeta, profundidad, panelHijos);
            yield return panelHijos;
        }

        foreach (var (relativa, longitud) in nodo.Archivos.OrderBy(a => a.Relativa, StringComparer.OrdinalIgnoreCase))
            yield return ConstruirFilaArchivo(relativa, longitud, profundidad);
    }

    private UIElement ConstruirFilaCarpeta(NodoCarpeta carpeta, int profundidad, StackPanel panelHijos)
    {
        var fila = new Grid { Margin = new Thickness(profundidad * 18, 4, 0, 4) };
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var expandido = true;
        var toggle = new TextBlock
        {
            Text = "▾",
            FontSize = 11,
            Width = 18,
            TextAlignment = TextAlignment.Center,
            Foreground = (Brush)FindResource("BrushTextoSecundario"),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand
        };
        toggle.MouseLeftButtonUp += (_, _) =>
        {
            expandido = !expandido;
            panelHijos.Visibility = expandido ? Visibility.Visible : Visibility.Collapsed;
            toggle.Text = expandido ? "▾" : "▸";
        };
        Grid.SetColumn(toggle, 0);

        var casillaCarpeta = new CheckBox
        {
            Style = (Style)FindResource("CheckBoxModerno"),
            IsChecked = true,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(casillaCarpeta, 1);
        casillaCarpeta.Checked += (_, _) => MarcarDescendientes(panelHijos, true);
        casillaCarpeta.Unchecked += (_, _) => MarcarDescendientes(panelHijos, false);

        var etiqueta = new TextBlock
        {
            Text = $"\U0001F4C1  {carpeta.Nombre}",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextoPrincipal"),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(etiqueta, 2);

        fila.Children.Add(toggle);
        fila.Children.Add(casillaCarpeta);
        fila.Children.Add(etiqueta);
        return fila;
    }

    private static void MarcarDescendientes(DependencyObject contenedor, bool marcado)
    {
        foreach (var hijo in LogicalTreeHelper.GetChildren(contenedor))
        {
            if (hijo is CheckBox casilla)
                casilla.IsChecked = marcado;
            if (hijo is DependencyObject dependencyHijo)
                MarcarDescendientes(dependencyHijo, marcado);
        }
    }

    private UIElement ConstruirFilaArchivo(string relativa, long longitud, int profundidad)
    {
        var nombre = ObtenerNombre(relativa);

        var fila = new Grid { Margin = new Thickness(profundidad * 18 + 18, 3, 0, 3) };
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var casilla = new CheckBox
        {
            Style = (Style)FindResource("CheckBoxModerno"),
            IsChecked = true,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(casilla, 0);
        _entradas.Add((relativa, casilla));
        casilla.Checked += (_, _) => ActualizarContador();
        casilla.Unchecked += (_, _) => ActualizarContador();

        var etiqueta = new TextBlock
        {
            Text = $"\U0001F4C4  {nombre}   ·   {FormatearTamano(longitud)}",
            FontSize = 12.5,
            Foreground = (Brush)FindResource("BrushTextoSecundario"),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand,
            ToolTip = "Doble clic para abrir"
        };
        Grid.SetColumn(etiqueta, 1);
        etiqueta.MouseLeftButtonDown += async (_, e) =>
        {
            if (e.ClickCount == 2)
                await AbrirArchivo(relativa, etiqueta);
        };

        fila.Children.Add(casilla);
        fila.Children.Add(etiqueta);
        return fila;
    }

    private async Task AbrirArchivo(string relativa, TextBlock etiqueta)
    {
        var textoOriginal = etiqueta.Text;
        try
        {
            etiqueta.Text = textoOriginal + "   (abriendo...)";

            var carpetaTemp = Path.Combine(Path.GetTempPath(), "Encriptador_preview_" + Guid.NewGuid().ToString("N"));
            var destino = Path.Combine(carpetaTemp, ObtenerNombre(relativa));

            await Task.Run(() => _sesion.ExtraerArchivo(relativa, destino));
            _temporalesAbiertos.Add(carpetaTemp);

            Process.Start(new ProcessStartInfo(destino) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"No se pudo abrir el archivo: {ex.Message}", "Encriptador",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            etiqueta.Text = textoOriginal;
        }
    }

    private void LimpiarTemporales()
    {
        foreach (var carpeta in _temporalesAbiertos)
            CryptoService.LimpiarCarpetaTemporalSegura(carpeta);
    }

    private static string ObtenerNombre(string relativa) =>
        relativa.Contains('/') ? relativa[(relativa.LastIndexOf('/') + 1)..] : relativa;

    private void ActualizarContador()
    {
        var seleccionados = _entradas.Count(e => e.Casilla.IsChecked == true);
        BtnExtraer.Content = seleccionados == _entradas.Count
            ? $"Extraer todo ({seleccionados})"
            : $"Extraer seleccionados ({seleccionados})";
        BtnExtraer.IsEnabled = seleccionados > 0;
    }

    private void BtnSeleccionarTodo_Click(object sender, RoutedEventArgs e)
    {
        foreach (var (_, casilla) in _entradas)
            casilla.IsChecked = true;
    }

    private void BtnDeseleccionarTodo_Click(object sender, RoutedEventArgs e)
    {
        foreach (var (_, casilla) in _entradas)
            casilla.IsChecked = false;
    }

    private void BtnExtraer_Click(object sender, RoutedEventArgs e)
    {
        Seleccionados = _entradas.Where(x => x.Casilla.IsChecked == true).Select(x => x.Relativa).ToList();
        Confirmado = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        Confirmado = false;
        Close();
    }

    private static string FormatearTamano(long bytes)
    {
        string[] unidades = { "B", "KB", "MB", "GB", "TB" };
        double tamano = bytes;
        var i = 0;
        while (tamano >= 1024 && i < unidades.Length - 1)
        {
            tamano /= 1024;
            i++;
        }
        return $"{tamano:0.#} {unidades[i]}";
    }
}
