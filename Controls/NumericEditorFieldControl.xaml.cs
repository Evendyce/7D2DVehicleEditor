using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace SevenDaysVehicleEditor.Controls;

public partial class NumericEditorFieldControl : UserControl
{
    private static readonly Regex NumericInputRegex = new(@"^[0-9\.\,\-]+$");

    public NumericEditorFieldControl()
    {
        InitializeComponent();
    }

    private void NumericInputPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !NumericInputRegex.IsMatch(e.Text);
    }
}
