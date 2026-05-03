using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace SevenDaysVehicleEditor;

public sealed class VehicleConfigDocument
{
    private readonly XDocument _document;

    private VehicleConfigDocument(string filePath, XDocument document, IReadOnlyList<VehicleConfig> vehicles)
    {
        FilePath = filePath;
        _document = document;
        Vehicles = vehicles;
    }

    public string FilePath { get; }

    public IReadOnlyList<VehicleConfig> Vehicles { get; }

    public static VehicleConfigDocument Load(string path)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var vehicles = document
            .Descendants("vehicle")
            .Select(BuildVehicle)
            .ToList();

        return new VehicleConfigDocument(path, document, vehicles);
    }

    public void Save(string path) => _document.Save(path, SaveOptions.DisableFormatting);

    private static VehicleConfig BuildVehicle(XElement vehicleElement)
    {
        var name = vehicleElement.Attribute("name")?.Value ?? "Unnamed Vehicle";
        var properties = new List<EditableProperty>();

        foreach (var propertyElement in vehicleElement.Elements("property"))
        {
            if (propertyElement.Attribute("name") is not null)
            {
                properties.Add(CreateProperty(name, string.Empty, propertyElement));
            }

            var section = propertyElement.Attribute("class")?.Value;
            if (string.IsNullOrWhiteSpace(section))
            {
                continue;
            }

            foreach (var nestedElement in propertyElement.Elements("property"))
            {
                if (nestedElement.Attribute("name") is null)
                {
                    continue;
                }

                properties.Add(CreateProperty(name, section, nestedElement));
            }
        }

        return new VehicleConfig(name, properties);
    }

    private static EditableProperty CreateProperty(string vehicleName, string section, XElement element)
    {
        var propertyName = element.Attribute("name")?.Value ?? string.Empty;
        return new EditableProperty(
            vehicleName,
            section,
            propertyName,
            propertyName,
            string.Empty,
            element.Attribute("value")?.Value ?? string.Empty,
            element);
    }
}

public sealed class VehicleConfig(string name, IEnumerable<EditableProperty> properties)
{
    public string Name { get; } = name;
    public ObservableCollection<EditableProperty> AllProperties { get; } = [.. properties];

    public EditableProperty? FindProperty(string propertyName, string? section) =>
        AllProperties.FirstOrDefault(property =>
            string.Equals(property.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(property.Section, section ?? string.Empty, StringComparison.OrdinalIgnoreCase));
}

public sealed class ModFolderConfig
{
    public ModFolderConfig(string name, string folderPath, VehicleConfigDocument document)
    {
        Name = name;
        FolderPath = folderPath;
        Document = document;
        Vehicles = [.. document.Vehicles.Select(vehicle => new ModVehicleNode(vehicle.Name, vehicle, document, name))];
    }

    public string Name { get; }
    public string FolderPath { get; }
    public VehicleConfigDocument Document { get; }
    public ObservableCollection<ModVehicleNode> Vehicles { get; }
    public string VehicleCountLabel => $"{Vehicles.Count} vehicle(s)";
}

public sealed class ModVehicleNode
{
    public ModVehicleNode(string name, VehicleConfig vehicle, VehicleConfigDocument document, string modName)
    {
        Name = name;
        Vehicle = vehicle;
        Document = document;
        ModName = modName;
    }

    public string Name { get; }
    public VehicleConfig Vehicle { get; }
    public VehicleConfigDocument Document { get; }
    public string ModName { get; }
}

public sealed class EditableProperty : INotifyPropertyChanged
{
    private readonly XObject _target;
    private string _displayName;
    private string _description;
    private string _value;

    public EditableProperty(
        string vehicleName,
        string section,
        string propertyName,
        string displayName,
        string description,
        string value,
        XObject target)
    {
        VehicleName = vehicleName;
        Section = section;
        PropertyName = propertyName;
        _displayName = displayName;
        _description = description;
        _value = value;
        _target = target;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string VehicleName { get; }
    public string Section { get; }
    public string PropertyName { get; }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName == value)
            {
                return;
            }

            _displayName = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value)
            {
                return;
            }

            _description = value;
            OnPropertyChanged();
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            switch (_target)
            {
                case XElement element:
                    element.SetAttributeValue("value", value);
                    break;
                case XAttribute attribute:
                    attribute.Value = value;
                    break;
            }
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
