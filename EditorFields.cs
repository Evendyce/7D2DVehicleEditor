using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SevenDaysVehicleEditor;

public abstract class EditorFieldViewModel(string key, string label, string description = "", bool isReadOnly = false) : INotifyPropertyChanged
{
    private string _label = label;
    private string _description = description;
    private bool _isReadOnly = isReadOnly;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key { get; } = key;

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ScalarEditorFieldViewModel(
    string key,
    string label,
    string value = "",
    string description = "",
    bool isReadOnly = false,
    string placeholder = "") : EditorFieldViewModel(key, label, description, isReadOnly)
{
    private string _value = value;
    private string _placeholder = placeholder;

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public string Placeholder
    {
        get => _placeholder;
        set => SetProperty(ref _placeholder, value);
    }
}

public sealed class NumericEditorFieldViewModel : EditorFieldViewModel
{
    private decimal _value;
    private string _valueText;
    private decimal _step;
    private int _decimalPlaces;

    public NumericEditorFieldViewModel(
        string key,
        string label,
        decimal value = 0,
        string description = "",
        bool isReadOnly = false,
        decimal step = 1,
        int decimalPlaces = 0) : base(key, label, description, isReadOnly)
    {
        _value = value;
        _step = step <= 0 ? 1 : step;
        _decimalPlaces = Math.Max(0, decimalPlaces);
        _valueText = FormatValue(_value, _decimalPlaces);
        IncrementCommand = new RelayCommand(Increment, () => !IsReadOnly);
        DecrementCommand = new RelayCommand(Decrement, () => !IsReadOnly);
    }

    public decimal Value
    {
        get => _value;
        set
        {
            value = Normalize(value);
            if (!SetProperty(ref _value, value))
            {
                return;
            }

            var formatted = FormatValue(_value, DecimalPlaces);
            if (_valueText != formatted)
            {
                _valueText = formatted;
                OnPropertyChanged(nameof(ValueText));
            }
        }
    }

    public string ValueText
    {
        get => _valueText;
        set
        {
            if (!SetProperty(ref _valueText, value))
            {
                return;
            }

            if (TryParse(value, out var parsed))
            {
                Value = parsed;
            }
        }
    }

    public decimal Step
    {
        get => _step;
        set => SetProperty(ref _step, value <= 0 ? 1 : value);
    }

    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            var normalized = Math.Max(0, value);
            if (!SetProperty(ref _decimalPlaces, normalized))
            {
                return;
            }

            var formatted = FormatValue(_value, _decimalPlaces);
            if (_valueText != formatted)
            {
                _valueText = formatted;
                OnPropertyChanged(nameof(ValueText));
            }
        }
    }

    public RelayCommand IncrementCommand { get; }

    public RelayCommand DecrementCommand { get; }

    public void Increment() => Value += Step;

    public void Decrement() => Value -= Step;

    public void RefreshText() => ValueText = FormatValue(Value, DecimalPlaces);

    private decimal Normalize(decimal value) =>
        decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    private static string FormatValue(decimal value, int decimalPlaces)
    {
        return decimalPlaces <= 0
            ? decimal.Round(value, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)
            : value.ToString($"0.{new string('0', decimalPlaces)}", CultureInfo.InvariantCulture);
    }

    private bool TryParse(string text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }
}

public sealed class BooleanEditorFieldViewModel(
    string key,
    string label,
    bool value = false,
    string description = "",
    bool isReadOnly = false) : EditorFieldViewModel(key, label, description, isReadOnly)
{
    private bool _value = value;

    public bool Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public sealed class ReadOnlyInfoFieldViewModel(
    string key,
    string label,
    string value = "",
    string description = "") : EditorFieldViewModel(key, label, description, isReadOnly: true)
{
    private string _value = value;

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public sealed class FourValueEditorFieldViewModel : EditorFieldViewModel
{
    private bool _isSynchronizing;
    private string _value;
    private string _firstLabel;
    private string _secondLabel;
    private string _thirdLabel;
    private string _fourthLabel;
    private string _firstValue;
    private string _secondValue;
    private string _thirdValue;
    private string _fourthValue;

    public FourValueEditorFieldViewModel(
        string key,
        string label,
        string value = "",
        string description = "",
        bool isReadOnly = false,
        string firstLabel = "Value 1",
        string secondLabel = "Value 2",
        string thirdLabel = "Value 3",
        string fourthLabel = "Value 4") : base(key, label, description, isReadOnly)
    {
        _value = value;
        _firstLabel = firstLabel;
        _secondLabel = secondLabel;
        _thirdLabel = thirdLabel;
        _fourthLabel = fourthLabel;
        _firstValue = string.Empty;
        _secondValue = string.Empty;
        _thirdValue = string.Empty;
        _fourthValue = string.Empty;

        ApplyRawValue(value);
    }

    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                ApplyRawValue(value);
            }
        }
    }

    public string FirstLabel
    {
        get => _firstLabel;
        set => SetProperty(ref _firstLabel, value);
    }

    public string SecondLabel
    {
        get => _secondLabel;
        set => SetProperty(ref _secondLabel, value);
    }

    public string ThirdLabel
    {
        get => _thirdLabel;
        set => SetProperty(ref _thirdLabel, value);
    }

    public string FourthLabel
    {
        get => _fourthLabel;
        set => SetProperty(ref _fourthLabel, value);
    }

    public string FirstValue
    {
        get => _firstValue;
        set
        {
            if (SetProperty(ref _firstValue, value))
            {
                SynchronizeCompositeValue();
            }
        }
    }

    public string SecondValue
    {
        get => _secondValue;
        set
        {
            if (SetProperty(ref _secondValue, value))
            {
                SynchronizeCompositeValue();
            }
        }
    }

    public string ThirdValue
    {
        get => _thirdValue;
        set
        {
            if (SetProperty(ref _thirdValue, value))
            {
                SynchronizeCompositeValue();
            }
        }
    }

    public string FourthValue
    {
        get => _fourthValue;
        set
        {
            if (SetProperty(ref _fourthValue, value))
            {
                SynchronizeCompositeValue();
            }
        }
    }

    public IReadOnlyList<string> Parts =>
        [_firstValue, _secondValue, _thirdValue, _fourthValue];

    private void ApplyRawValue(string rawValue)
    {
        if (_isSynchronizing)
        {
            return;
        }

        _isSynchronizing = true;

        try
        {
            var parts = rawValue
                .Split(',', StringSplitOptions.TrimEntries)
                .ToList();

            while (parts.Count < 4)
            {
                parts.Add(string.Empty);
            }

            _firstValue = parts[0];
            _secondValue = parts[1];
            _thirdValue = parts[2];
            _fourthValue = parts[3];

            OnPropertyChanged(nameof(FirstValue));
            OnPropertyChanged(nameof(SecondValue));
            OnPropertyChanged(nameof(ThirdValue));
            OnPropertyChanged(nameof(FourthValue));
            OnPropertyChanged(nameof(Parts));
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private void SynchronizeCompositeValue()
    {
        if (_isSynchronizing)
        {
            return;
        }

        _isSynchronizing = true;

        try
        {
            var normalized = string.Join(", ", new[] { _firstValue, _secondValue, _thirdValue, _fourthValue });
            if (_value != normalized)
            {
                _value = normalized;
                OnPropertyChanged(nameof(Value));
            }

            OnPropertyChanged(nameof(Parts));
        }
        finally
        {
            _isSynchronizing = false;
        }
    }
}

public sealed class NumericPartViewModel : INotifyPropertyChanged
{
    private readonly Action _valueChanged;
    private string _label;
    private decimal _value;
    private string _valueText;
    private decimal _step;
    private int _decimalPlaces;
    private bool _isReadOnly;

    public NumericPartViewModel(
        string label,
        decimal value,
        decimal step,
        int decimalPlaces,
        bool isReadOnly,
        Action valueChanged)
    {
        _label = label;
        _value = value;
        _step = step <= 0 ? 1 : step;
        _decimalPlaces = Math.Max(0, decimalPlaces);
        _isReadOnly = isReadOnly;
        _valueChanged = valueChanged;
        _valueText = FormatValue(_value, _decimalPlaces);
        IncrementCommand = new RelayCommand(Increment, () => !IsReadOnly);
        DecrementCommand = new RelayCommand(Decrement, () => !IsReadOnly);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public decimal Value
    {
        get => _value;
        set
        {
            value = Normalize(value);
            if (!SetProperty(ref _value, value))
            {
                return;
            }

            var formatted = FormatValue(_value, DecimalPlaces);
            if (_valueText != formatted)
            {
                _valueText = formatted;
                OnPropertyChanged(nameof(ValueText));
            }

            _valueChanged();
        }
    }

    public string ValueText
    {
        get => _valueText;
        set
        {
            if (!SetProperty(ref _valueText, value))
            {
                return;
            }

            if (TryParse(value, out var parsed))
            {
                Value = parsed;
            }
        }
    }

    public decimal Step
    {
        get => _step;
        set => SetProperty(ref _step, value <= 0 ? 1 : value);
    }

    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            var normalized = Math.Max(0, value);
            if (!SetProperty(ref _decimalPlaces, normalized))
            {
                return;
            }

            var formatted = FormatValue(_value, _decimalPlaces);
            if (_valueText != formatted)
            {
                _valueText = formatted;
                OnPropertyChanged(nameof(ValueText));
            }
        }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (!SetProperty(ref _isReadOnly, value))
            {
                return;
            }

            IncrementCommand.RaiseCanExecuteChanged();
            DecrementCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand IncrementCommand { get; }

    public RelayCommand DecrementCommand { get; }

    public void Increment() => Value += Step;

    public void Decrement() => Value -= Step;

    public void RefreshText()
    {
        var formatted = FormatValue(_value, _decimalPlaces);
        if (_valueText != formatted)
        {
            _valueText = formatted;
            OnPropertyChanged(nameof(ValueText));
        }
    }

    private decimal Normalize(decimal value) =>
        decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    private static string FormatValue(decimal value, int decimalPlaces)
    {
        return decimalPlaces <= 0
            ? decimal.Round(value, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)
            : value.ToString($"0.{new string('0', decimalPlaces)}", CultureInfo.InvariantCulture);
    }

    private bool TryParse(string text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class MultiNumericEditorFieldViewModel : EditorFieldViewModel
{
    private readonly ObservableCollection<NumericPartViewModel> _parts = [];
    private bool _isSynchronizing;
    private string _value;

    public MultiNumericEditorFieldViewModel(
        string key,
        string label,
        string value,
        IReadOnlyList<string> labels,
        string description = "",
        bool isReadOnly = false,
        decimal step = 1,
        int decimalPlaces = 0) : base(key, label, description, isReadOnly)
    {
        _value = value;
        Step = step <= 0 ? 1 : step;
        DecimalPlaces = Math.Max(0, decimalPlaces);

        var rawParts = value
            .Split(',', StringSplitOptions.TrimEntries)
            .ToList();

        for (var index = 0; index < labels.Count; index++)
        {
            var parsed = 0m;
            if (index < rawParts.Count)
            {
                TryParse(rawParts[index], out parsed);
            }

            _parts.Add(new NumericPartViewModel(
                labels[index],
                parsed,
                Step,
                DecimalPlaces,
                isReadOnly,
                SynchronizeCompositeValue));
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (!SetProperty(ref _value, value))
            {
                return;
            }

            ApplyRawValue(value);
        }
    }

    public decimal Step { get; }

    public int DecimalPlaces { get; }

    public ObservableCollection<NumericPartViewModel> Parts => _parts;

    private void ApplyRawValue(string rawValue)
    {
        if (_isSynchronizing)
        {
            return;
        }

        _isSynchronizing = true;

        try
        {
            var rawParts = rawValue.Split(',', StringSplitOptions.TrimEntries);
            for (var index = 0; index < _parts.Count; index++)
            {
                if (index < rawParts.Length && TryParse(rawParts[index], out var parsed))
                {
                    _parts[index].Value = parsed;
                }
            }
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private void SynchronizeCompositeValue()
    {
        if (_isSynchronizing)
        {
            return;
        }

        _isSynchronizing = true;

        try
        {
            var normalized = string.Join(", ", _parts.Select(part =>
                part.DecimalPlaces <= 0
                    ? decimal.Round(part.Value, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)
                    : part.Value.ToString($"0.{new string('0', part.DecimalPlaces)}", CultureInfo.InvariantCulture)));

            if (_value != normalized)
            {
                _value = normalized;
                OnPropertyChanged(nameof(Value));
            }
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private static bool TryParse(string text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }
}

public sealed class EditorSectionViewModel(string title, string description = "")
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public ObservableCollection<EditorFieldViewModel> Fields { get; } = [];
}

public sealed class EditorFormViewModel(string title = "", string description = "")
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public ObservableCollection<EditorFieldViewModel> Fields { get; } = [];
    public ObservableCollection<EditorSectionViewModel> Sections { get; } = [];
}

public sealed class DynamicEditorFormItemsSourceProxy(IEnumerable items)
{
    public IEnumerable Items { get; } = items;
}
