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

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is System.Windows.Controls.ScrollViewer scroller)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }
}
