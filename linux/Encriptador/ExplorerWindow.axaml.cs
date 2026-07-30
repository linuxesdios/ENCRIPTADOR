using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
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

        Title = Loc.T("explorer.titulo");
        TxtTitulo.Text = nombreOrigen;
        TxtInstrucciones.Text = Loc.T("explorer.instrucciones");
        BtnSeleccionarTodo.Content = Loc.T("explorer.btn.seleccionarTodo");
        BtnDeseleccionarTodo.Content = Loc.T("explorer.btn.deseleccionarTodo");
        BtnCancelarAccion.Content = Loc.T("explorer.btn.cancelar");

        var archivos = _sesion.Archivos;
        TxtSubtitulo.Text = Loc.Plural(archivos.Count, "explorer.subtitulo", archivos.Count);

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

    private IEnumerable<Control> ConstruirNodosCarpeta(NodoCarpeta nodo, int profundidad)
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

    private Control ConstruirFilaCarpeta(NodoCarpeta carpeta, int profundidad, StackPanel panelHijos)
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
            Foreground = Recurso<IBrush>("BrushTextoSecundario"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        toggle.Tapped += (_, _) =>
        {
            expandido = !expandido;
            panelHijos.IsVisible = expandido;
            toggle.Text = expandido ? "▾" : "▸";
        };
        Grid.SetColumn(toggle, 0);

        var casillaCarpeta = new CheckBox
        {
            IsChecked = true,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        casillaCarpeta.Classes.Add("moderno");
        Grid.SetColumn(casillaCarpeta, 1);
        casillaCarpeta.IsCheckedChanged += (_, _) => MarcarDescendientes(panelHijos, casillaCarpeta.IsChecked == true);

        var etiqueta = new TextBlock
        {
            Text = $"\U0001F4C1  {carpeta.Nombre}",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = Recurso<IBrush>("BrushTextoPrincipal"),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(etiqueta, 2);

        fila.Children.Add(toggle);
        fila.Children.Add(casillaCarpeta);
        fila.Children.Add(etiqueta);
        return fila;
    }

    private static void MarcarDescendientes(ILogical contenedor, bool marcado)
    {
        foreach (var hijo in contenedor.GetLogicalChildren())
        {
            if (hijo is CheckBox casilla)
                casilla.IsChecked = marcado;
            MarcarDescendientes(hijo, marcado);
        }
    }

    private Control ConstruirFilaArchivo(string relativa, long longitud, int profundidad)
    {
        var nombre = ObtenerNombre(relativa);

        var fila = new Grid { Margin = new Thickness(profundidad * 18 + 18, 3, 0, 3) };
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var casilla = new CheckBox
        {
            IsChecked = true,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        casilla.Classes.Add("moderno");
        Grid.SetColumn(casilla, 0);
        _entradas.Add((relativa, casilla));
        casilla.IsCheckedChanged += (_, _) => ActualizarContador();

        var etiqueta = new TextBlock
        {
            Text = $"\U0001F4C4  {nombre}   ·   {FormatearTamano(longitud)}",
            FontSize = 12.5,
            Foreground = Recurso<IBrush>("BrushTextoSecundario"),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ToolTip.SetTip(etiqueta, Loc.T("explorer.tooltip.dobleClic"));
        Grid.SetColumn(etiqueta, 1);
        etiqueta.DoubleTapped += async (_, _) => await AbrirArchivo(relativa, etiqueta);

        fila.Children.Add(casilla);
        fila.Children.Add(etiqueta);
        return fila;
    }

    private async Task AbrirArchivo(string relativa, TextBlock etiqueta)
    {
        var textoOriginal = etiqueta.Text;
        try
        {
            etiqueta.Text = textoOriginal + Loc.T("explorer.estado.abriendo");

            var carpetaTemp = Path.Combine(Path.GetTempPath(), "Encriptador_preview_" + Guid.NewGuid().ToString("N"));
            var destino = Path.Combine(carpetaTemp, ObtenerNombre(relativa));

            await Task.Run(() => _sesion.ExtraerArchivo(relativa, destino));
            _temporalesAbiertos.Add(carpetaTemp);

            Process.Start(new ProcessStartInfo(destino) { UseShellExecute = true });
            etiqueta.Text = textoOriginal;
        }
        catch (Exception ex)
        {
            etiqueta.Text = textoOriginal + Loc.T("explorer.estado.errorAlAbrir", ex.Message);
            await Task.Delay(2500);
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
            ? Loc.T("explorer.btn.extraerTodo", seleccionados)
            : Loc.T("explorer.btn.extraerSeleccionados", seleccionados);
        BtnExtraer.IsEnabled = seleccionados > 0;
    }

    private void BtnSeleccionarTodo_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var (_, casilla) in _entradas)
            casilla.IsChecked = true;
    }

    private void BtnDeseleccionarTodo_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var (_, casilla) in _entradas)
            casilla.IsChecked = false;
    }

    private void BtnExtraer_Click(object? sender, RoutedEventArgs e)
    {
        Seleccionados = _entradas.Where(x => x.Casilla.IsChecked == true).Select(x => x.Relativa).ToList();
        Confirmado = true;
        Close();
    }

    private void BtnCancelar_Click(object? sender, RoutedEventArgs e)
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

    private static T Recurso<T>(string clave) where T : class =>
        (T)Application.Current!.Resources[clave]!;
}
