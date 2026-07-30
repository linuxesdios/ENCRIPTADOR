using Avalonia.Controls;
using Avalonia.Interactivity;
using Encriptador.Services;

namespace Encriptador;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        Title = Loc.T("main.tooltip.acerca");
        TxtDesarrollador.Text = Loc.T("about.desarrollador");
        TxtTagline.Text = Loc.T("about.tagline");
        TxtVersion.Text = Loc.T("about.version");
        BtnCerrar.Content = Loc.T("about.cerrar");
    }

    private void BtnCerrar_Click(object? sender, RoutedEventArgs e) => Close();
}
