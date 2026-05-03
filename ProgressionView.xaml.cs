using System.Windows;
using System.Windows.Controls;

namespace SevenDaysVehicleEditor;

public partial class ProgressionView : UserControl
{
    public ProgressionView()
    {
        InitializeComponent();
    }

    private void BrowseProgressionFileClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.BrowseProgressionFileClick(sender, e);
        }
    }

    private void ReloadProgressionFileClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.ReloadProgressionFileClick(sender, e);
        }
    }

    private void SaveProgressionFileClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.SaveProgressionFileClick(sender, e);
        }
    }

    private void CreateProgressionBackupClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.CreateProgressionBackupClick(sender, e);
        }
    }

    private void RestoreProgressionBackupClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.RestoreProgressionBackupClick(sender, e);
        }
    }
}
