using System.Windows;
using ShortcutDock.ViewModels;
using Wpf.Ui.Controls;

namespace ShortcutDock;

/// <summary>
/// Логика взаимодействия для SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
