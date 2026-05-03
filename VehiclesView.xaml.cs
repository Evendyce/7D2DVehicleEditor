using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SevenDaysVehicleEditor;

public partial class VehiclesView : UserControl
{
    public ICommand OpenPerformanceEditorCommand { get; }
    public ICommand OpenHandlingEditorCommand { get; }
    public ICommand OpenFuelEditorCommand { get; }
    public ICommand OpenExtrasEditorCommand { get; }

    public VehiclesView()
    {
        OpenPerformanceEditorCommand = new RelayCommand(OpenPerformanceEditor);
        OpenHandlingEditorCommand = new RelayCommand(OpenHandlingEditor);
        OpenFuelEditorCommand = new RelayCommand(OpenFuelEditor);
        OpenExtrasEditorCommand = new RelayCommand(OpenExtrasEditor);
        InitializeComponent();
    }

    private void OpenPerformanceEditor()
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenPerformanceSectionEditor();
        }
    }

    private void OpenHandlingEditor()
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenHandlingSectionEditor();
        }
    }

    private void OpenFuelEditor()
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenFuelSectionEditor();
        }
    }

    private void OpenExtrasEditor()
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenExtrasSectionEditor();
        }
    }

    private void BrowseFileClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.BrowseFileClick(sender, e);
        }
    }

    private void ReloadFileClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.ReloadFileClick(sender, e);
        }
    }

    private void SaveFileClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.SaveFileClick(sender, e);
        }
    }

    private void CreateBackupClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.CreateBackupClick(sender, e);
        }
    }

    private void RestoreBackupClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.RestoreBackupClick(sender, e);
        }
    }

    private void ApplyPerformanceMultiplierClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.ApplyPerformanceMultiplierClick(sender, e);
        }
    }

    private void ModTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.ModTreeSelectionChanged(sender, e);
        }
    }

    private void ModTreePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.ModTreePreviewMouseLeftButtonDown(sender, e);
        }
    }
}
