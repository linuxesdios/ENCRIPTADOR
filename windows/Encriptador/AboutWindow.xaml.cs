using System.Windows;

namespace Encriptador;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();
}
