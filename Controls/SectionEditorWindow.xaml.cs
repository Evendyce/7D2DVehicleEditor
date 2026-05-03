using System.Collections.ObjectModel;
using System.Windows;

namespace SevenDaysVehicleEditor.Controls;

public partial class SectionEditorWindow : Window
{
    public SectionEditorWindow(string header, string description, IEnumerable<EditorFieldViewModel> fields)
    {
        InitializeComponent();
        DataContext = new SectionEditorWindowViewModel(header, description, fields);
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

public sealed class SectionEditorWindowViewModel(string header, string description, IEnumerable<EditorFieldViewModel> fields)
{
    public string Header { get; } = header;
    public string Description { get; } = description;
    public ObservableCollection<EditorFieldViewModel> Fields { get; } = [.. fields];
}
