using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace SevenDaysVehicleEditor.Controls;

public partial class BooleanEditorFieldControl : UserControl
{
    public BooleanEditorFieldControl()
    {
        InitializeComponent();
    }
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public static InverseBooleanConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolValue ? !boolValue : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolValue ? !boolValue : false;
}
