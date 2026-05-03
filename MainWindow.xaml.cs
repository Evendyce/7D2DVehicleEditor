using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Xml.Linq;
using System.Runtime.InteropServices;

namespace SevenDaysVehicleEditor;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string MotorTorquePropertyName = "motorTorque_turbo";
    private const string MaxVelocityPropertyName = "velocityMax_turbo";
    private const string BrakeTorquePropertyName = "brakeTorque";

    private static readonly IReadOnlyList<PropertyDefinition> ProgressionLevelDefinitions =
    [
        new("Level", "max_level", null, "Max Level", "The highest player level allowed by progression.xml."),
        new("Level", "exp_to_level", null, "Base XP To Level", "Base XP required to complete the current level."),
        new("Level", "experience_multiplier", null, "Experience Multiplier", "Scaling multiplier applied to XP required per level."),
        new("Level", "skill_points_per_level", null, "Skill Points Per Level", "How many skill points the player gains each level."),
        new("Level", "clamp_exp_cost_at_level", null, "Clamp XP Cost At Level", "Level where XP cost growth stops increasing.")
    ];

    private static readonly IReadOnlyList<PropertyDefinition> ProgressionAttributeDefaultDefinitions =
    [
        new("Attributes", "min_level", null, "Min Level", "Default minimum rank for attributes that do not override it."),
        new("Attributes", "max_level", null, "Max Level", "Default maximum rank for attributes that do not override it."),
        new("Attributes", "base_skill_point_cost", null, "Base Skill Point Cost", "Default skill point cost for attribute ranks."),
        new("Attributes", "cost_multiplier_per_level", null, "Cost Multiplier Per Level", "Scaling multiplier applied to attribute level costs.")
    ];

    private static readonly IReadOnlyList<PropertyDefinition> ProgressionAttributeOverrideDefinitions =
    [
        new("Attribute", "min_level", null, "Min Level", "Overrides the minimum level for this specific attribute."),
        new("Attribute", "max_level", null, "Max Level", "Overrides the maximum level for this specific attribute."),
        new("Attribute", "base_skill_point_cost", null, "Base Skill Point Cost", "Overrides the base cost for this specific attribute."),
        new("Attribute", "hidden", null, "Hidden", "Controls whether this attribute is hidden from normal progression presentation.")
    ];

    private static readonly IReadOnlyList<PropertyDefinition> ProgressionSkillDefaultDefinitions =
    [
        new("Skills", "min_level", null, "Min Level", "Top-level minimum level for the skills section."),
        new("Skills", "max_level", null, "Max Level", "Top-level maximum level for the skills section.")
    ];

    private static readonly IReadOnlyList<PropertyDefinition> ProgressionCraftingSkillDefaultDefinitions =
    [
        new("Crafting Skills", "complete_sound", null, "Complete Sound", "Sound event used when a crafting skill set completes.")
    ];

    private static readonly IReadOnlyList<PropertyDefinition> ProgressionPerkDefaultDefinitions =
    [
        new("Perks", "min_level", null, "Min Level", "Top-level minimum rank for perks that do not override it."),
        new("Perks", "max_level", null, "Max Level", "Top-level maximum rank for perks that do not override it."),
        new("Perks", "base_skill_point_cost", null, "Base Skill Point Cost", "Default base perk point cost."),
        new("Perks", "cost_multiplier_per_level", null, "Cost Multiplier Per Level", "Default perk cost multiplier per rank."),
        new("Perks", "max_level_ratio_to_parent", null, "Max Level Ratio To Parent", "Parent-to-child level ratio used by perk progression.")
    ];

    private static readonly IReadOnlyList<PropertyDefinition> SafeDefinitions =
    [
        new("Performance", MotorTorquePropertyName, null, "Motor Torque / Turbo", "Forward, reverse, turbo-forward, turbo-reverse values."),
        new("Performance", MaxVelocityPropertyName, null, "Max Velocity / Turbo", "Forward, reverse, turbo-forward, turbo-reverse speeds."),
        new("Performance", BrakeTorquePropertyName, null, "Brake Torque", "Higher values stop the vehicle harder."),
        new("Handling", "steerRate", null, "Steer Rate", "How quickly the steering responds."),
        new("Handling", "steerCenteringRate", null, "Steer Centering Rate", "How quickly steering returns to center."),
        new("Handling", "steerAngleMax", null, "Steer Angle Max", "Maximum steering angle when this property exists."),
        new("Handling", "upAngleMax", null, "Up Angle Max", "How steeply the vehicle can pitch upward."),
        new("Handling", "upForce", null, "Up Force", "General lift or recovery force."),
        new("Handling", "tiltAngleMax", null, "Tilt Angle Max", "Maximum tilt before limits kick in."),
        new("Handling", "tiltThreshold", null, "Tilt Threshold", "Tilt amount before dampening starts."),
        new("Handling", "tiltDampening", null, "Tilt Dampening", "Controls how aggressively tilt settles."),
        new("Handling", "tiltDampenThreshold", null, "Tilt Dampen Threshold", "Threshold for tilt dampening."),
        new("Handling", "tiltUpForce", null, "Tilt Up Force", "Recovery force used while tilted."),
        new("Handling", "unstickForce", null, "Unstick Force", "Helps pop the vehicle free when stuck."),
        new("Handling", "hopForce", null, "Hop Force", "Jump strength where supported."),
        new("Fuel", "fuelKmPerL", "engine", "Fuel Km Per L", "Fuel efficiency inside the engine section."),
        new("Fuel", "foodDrain", "engine", "Food Drain", "Used by pedal- or gyro-style vehicles."),
        new("Fuel", "capacity", "fuelTank", "Fuel Capacity", "Fuel tank size."),
        new("Fuel", "size", "storage", "Storage Size", "Storage slot count when a size property exists."),
        new("Extras", "cameraDistance", null, "Camera Distance", "Near and far third-person camera distance."),
        new("Extras", "cameraTurnRate", null, "Camera Turn Rate", "Camera responsiveness."),
        new("Extras", "wheelPtlScale", null, "Wheel Particle Scale", "Scale of dirt and dust effects."),
        new("Extras", "hornSound", null, "Horn Sound", "Horn sound event name."),
        new("Extras", "bright", "headlight", "Headlight Brightness", "Brightness used by headlight section.")
    ];

    private VehicleConfigDocument? _vanillaDocument;
    private ProgressionConfigDocument? _progressionDocument;
    private VehicleConfig? _selectedVanillaVehicle;
    private VehicleConfig? _currentVehicle;
    private ModFolderConfig? _selectedModFolder;
    private ModVehicleNode? _selectedModVehicleNode;
    private string _currentFilePath = string.Empty;
    private string _statusMessage = "Select a vehicles.xml file to get started.";
    private string _modsRootPath = string.Empty;
    private string _progressionFilePath = string.Empty;
    private string _progressionStatusMessage = "Load progression.xml to edit level and attribute settings.";
    private ProgressionAttributeConfig? _selectedProgressionAttribute;
    private ProgressionSkillConfig? _selectedProgressionSkill;
    private ProgressionCraftingSkillConfig? _selectedProgressionCraftingSkill;
    private ProgressionPerkConfig? _selectedProgressionPerk;
    private string _multiplierText = "1.0";
    private bool _applyToSelectedVehicle = true;
    private bool _affectMotorTorque = true;
    private bool _affectMaxVelocity = true;
    private bool _affectBrakeTorque = true;
    private int _selectedTabIndex;

    public ObservableCollection<VehicleConfig> VanillaVehicles { get; } = [];
    public ObservableCollection<ModFolderConfig> ModFolders { get; } = [];
    public ObservableCollection<EditableProperty> PerformanceProperties { get; } = [];
    public ObservableCollection<EditableProperty> HandlingProperties { get; } = [];
    public ObservableCollection<EditableProperty> FuelProperties { get; } = [];
    public ObservableCollection<EditableProperty> ExtraProperties { get; } = [];
    public ObservableCollection<EditableProperty> AdvancedProperties { get; } = [];
    public ObservableCollection<EditableProperty> ProgressionLevelProperties { get; } = [];
    public ObservableCollection<EditableProperty> ProgressionAttributeDefaultsProperties { get; } = [];
    public ObservableCollection<ProgressionAttributeConfig> ProgressionAttributes { get; } = [];
    public ObservableCollection<EditableProperty> ProgressionSkillDefaultsProperties { get; } = [];
    public ObservableCollection<ProgressionSkillConfig> ProgressionSkills { get; } = [];
    public ObservableCollection<EditableProperty> ProgressionCraftingSkillDefaultsProperties { get; } = [];
    public ObservableCollection<ProgressionCraftingSkillConfig> ProgressionCraftingSkills { get; } = [];
    public ObservableCollection<EditableProperty> ProgressionPerkDefaultsProperties { get; } = [];
    public ObservableCollection<ProgressionPerkConfig> ProgressionPerks { get; } = [];
    public ObservableCollection<EditableProperty> SelectedProgressionAttributeProperties { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        SourceInitialized += OnSourceInitialized;

        var defaultVehiclesPath = DetectDefaultVehiclesPath() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(defaultVehiclesPath))
        {
            LoadVanillaFile(defaultVehiclesPath, updateStatus: false);
        }

        var defaultProgressionPath = DetectDefaultProgressionPath() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(defaultProgressionPath))
        {
            LoadProgressionFile(defaultProgressionPath, updateStatus: false);
        }

        ScanModFolders(updateStatus: false);
        RefreshActiveContext();

        if (_vanillaDocument is not null)
        {
            StatusMessage = $"Loaded {VanillaVehicles.Count} vanilla vehicle entries and scanned {ModFolders.Count} compatible vehicle mods.";
        }
        else if (ModFolders.Count > 0)
        {
            StatusMessage = $"Scanned {ModFolders.Count} compatible vehicle mods.";
        }
    }

    public string CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            if (_currentFilePath == value)
            {
                return;
            }

            _currentFilePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasActiveFile));
            OnPropertyChanged(nameof(CanRestoreBackup));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string ProgressionFilePath
    {
        get => _progressionFilePath;
        set
        {
            if (_progressionFilePath == value)
            {
                return;
            }

            _progressionFilePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasActiveProgressionFile));
            OnPropertyChanged(nameof(CanRestoreProgressionBackup));
        }
    }

    public string ProgressionStatusMessage
    {
        get => _progressionStatusMessage;
        set
        {
            if (_progressionStatusMessage == value)
            {
                return;
            }

            _progressionStatusMessage = value;
            OnPropertyChanged();
        }
    }

    public ProgressionAttributeConfig? SelectedProgressionAttribute
    {
        get => _selectedProgressionAttribute;
        set
        {
            if (_selectedProgressionAttribute == value)
            {
                return;
            }

            _selectedProgressionAttribute = value;
            RebuildSelectedProgressionAttributeProperties();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedProgressionAttributeSummary));
        }
    }

    public ProgressionSkillConfig? SelectedProgressionSkill
    {
        get => _selectedProgressionSkill;
        set
        {
            if (_selectedProgressionSkill == value)
            {
                return;
            }

            _selectedProgressionSkill = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedProgressionSkillSummary));
        }
    }

    public ProgressionCraftingSkillConfig? SelectedProgressionCraftingSkill
    {
        get => _selectedProgressionCraftingSkill;
        set
        {
            if (_selectedProgressionCraftingSkill == value)
            {
                return;
            }

            _selectedProgressionCraftingSkill = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedProgressionCraftingSkillSummary));
        }
    }

    public ProgressionPerkConfig? SelectedProgressionPerk
    {
        get => _selectedProgressionPerk;
        set
        {
            if (_selectedProgressionPerk == value)
            {
                return;
            }

            _selectedProgressionPerk = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedProgressionPerkSummary));
        }
    }

    public string MultiplierText
    {
        get => _multiplierText;
        set
        {
            if (_multiplierText == value)
            {
                return;
            }

            _multiplierText = value;
            OnPropertyChanged();
        }
    }

    public bool ApplyToSelectedVehicle
    {
        get => _applyToSelectedVehicle;
        set
        {
            if (_applyToSelectedVehicle == value)
            {
                return;
            }

            _applyToSelectedVehicle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApplyToAllVehicles));
        }
    }

    public bool ApplyToAllVehicles
    {
        get => !_applyToSelectedVehicle;
        set
        {
            if (!value)
            {
                return;
            }

            ApplyToSelectedVehicle = false;
        }
    }

    public bool AffectMotorTorque
    {
        get => _affectMotorTorque;
        set
        {
            if (_affectMotorTorque == value)
            {
                return;
            }

            _affectMotorTorque = value;
            OnPropertyChanged();
        }
    }

    public bool AffectMaxVelocity
    {
        get => _affectMaxVelocity;
        set
        {
            if (_affectMaxVelocity == value)
            {
                return;
            }

            _affectMaxVelocity = value;
            OnPropertyChanged();
        }
    }

    public bool AffectBrakeTorque
    {
        get => _affectBrakeTorque;
        set
        {
            if (_affectBrakeTorque == value)
            {
                return;
            }

            _affectBrakeTorque = value;
            OnPropertyChanged();
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex == value)
            {
                return;
            }

            _selectedTabIndex = value;
            RefreshActiveContext();
            OnPropertyChanged();
            OnPropertyChanged(nameof(BrowseButtonText));
            OnPropertyChanged(nameof(ReloadButtonText));
            OnPropertyChanged(nameof(MultiplierHelpText));
        }
    }

    public VehicleConfig? SelectedVanillaVehicle
    {
        get => _selectedVanillaVehicle;
        set
        {
            if (_selectedVanillaVehicle == value)
            {
                return;
            }

            _selectedVanillaVehicle = value;
            if (SelectedTabIndex == 0)
            {
                SetCurrentVehicle(value);
                RefreshCurrentPath();
            }

            OnPropertyChanged();
        }
    }

    public VehicleConfig? CurrentVehicle
    {
        get => _currentVehicle;
        private set
        {
            if (_currentVehicle == value)
            {
                return;
            }

            _currentVehicle = value;
            RebuildCurrentVehicleCollections();
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentVehicleSummary));
        }
    }

    public ModVehicleNode? SelectedModVehicleNode
    {
        get => _selectedModVehicleNode;
        set
        {
            if (_selectedModVehicleNode == value)
            {
                return;
            }

            _selectedModVehicleNode = value;
            _selectedModFolder = value is null
                ? _selectedModFolder
                : ModFolders.FirstOrDefault(folder =>
                    string.Equals(folder.Name, value.ModName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(folder.Document.FilePath, value.Document.FilePath, StringComparison.OrdinalIgnoreCase));

            if (SelectedTabIndex == 1)
            {
                SetCurrentVehicle(value?.Vehicle);
                RefreshCurrentPath();
            }

            OnPropertyChanged();
        }
    }

    public string CurrentVehicleSummary =>
        CurrentVehicle is null
            ? SelectedTabIndex == 0
                ? "Load a config file and select a vehicle to edit its properties."
                : "Select a vehicle under a detected mod folder to edit its properties."
            : $"{CurrentVehicle.AllProperties.Count} editable properties loaded from the file. Safe sections focus on common tuning values; the advanced list exposes every discovered property for this vehicle.";

    public string BrowseButtonText => SelectedTabIndex == 0 ? "Browse..." : "Scan Mods";

    public string ReloadButtonText => SelectedTabIndex == 0 ? "Reload" : "Rescan";

    public bool HasActiveFile => !string.IsNullOrWhiteSpace(ActiveFilePath) && File.Exists(ActiveFilePath);

    public bool CanRestoreBackup => HasActiveFile && File.Exists(GetBackupPath(ActiveFilePath!));

    public string MultiplierHelpText =>
        SelectedTabIndex == 0
            ? "Rounds max velocity to the nearest 0.5 and the other selected settings to whole numbers."
            : "Applies to the selected mod vehicle or every discovered mod vehicle. Max velocity rounds to 0.5; the others round to whole numbers.";

    public string ModSummary
    {
        get
        {
            var modCount = ModFolders.Count;
            var vehicleCount = ModFolders.Sum(folder => folder.Vehicles.Count);
            return modCount == 0
                ? "No compatible vehicle mods found."
                : $"{modCount} compatible mod folder(s), {vehicleCount} vehicle(s) found.";
        }
    }

    public bool HasActiveProgressionFile => !string.IsNullOrWhiteSpace(_progressionDocument?.FilePath) && File.Exists(_progressionDocument.FilePath);

    public bool CanRestoreProgressionBackup => HasActiveProgressionFile && File.Exists(GetBackupPath(_progressionDocument!.FilePath));

    public string SelectedProgressionAttributeSummary =>
        SelectedProgressionAttribute is null
            ? "Select an attribute to view its top-level overrides and structural summary."
            : $"{SelectedProgressionAttribute.Summary} Top-level overrides here are the safe first-pass edits; deeper rank requirements and effect groups remain read-only for now.";

    public string SelectedProgressionSkillSummary =>
        SelectedProgressionSkill is null
            ? "Select a skill or book group to inspect its metadata."
            : $"{SelectedProgressionSkill.Summary} This section is intentionally read-only so the app can expose skill metadata without implying these entries are safe balance edits.";

    public string SelectedProgressionCraftingSkillSummary =>
        SelectedProgressionCraftingSkill is null
            ? "Select a crafting skill to inspect its structure."
            : $"{SelectedProgressionCraftingSkill.Summary} Crafting skills stay read-only because unlock tiers, display entries, and passive effects are tightly coupled.";

    public string SelectedProgressionPerkSummary =>
        SelectedProgressionPerk is null
            ? "Select a perk to inspect its progression structure."
            : $"{SelectedProgressionPerk.Summary} Perks stay read-only because their requirements, effects, and descriptions are heavily interdependent.";

    private VehicleConfigDocument? ActiveDocument =>
        SelectedTabIndex == 0 ? _vanillaDocument : SelectedModVehicleNode?.Document ?? _selectedModFolder?.Document;

    private IEnumerable<VehicleConfig> ActiveVehicleScope =>
        SelectedTabIndex == 0
            ? VanillaVehicles
            : ModFolders.SelectMany(folder => folder.Vehicles).Select(node => node.Vehicle);

    private string? ActiveFilePath =>
        SelectedTabIndex == 0
            ? _vanillaDocument?.FilePath
            : SelectedModVehicleNode?.Document.FilePath ?? _selectedModFolder?.Document.FilePath;

    internal void BrowseFileClick(object sender, RoutedEventArgs e)
    {
        if (SelectedTabIndex == 1)
        {
            ScanModFolders(updateStatus: true);
            RefreshActiveContext();
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "7 Days To Die vehicle config|vehicles.xml|XML files|*.xml|All files|*.*",
            FileName = "vehicles.xml",
            InitialDirectory = GetInitialVanillaDirectory()
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadVanillaFile(dialog.FileName, updateStatus: true);
        }
    }

    internal void BrowseProgressionFileClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "7 Days To Die progression config|progression.xml|XML files|*.xml|All files|*.*",
            FileName = "progression.xml",
            InitialDirectory = GetInitialProgressionDirectory()
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadProgressionFile(dialog.FileName, updateStatus: true);
        }
    }

    internal void ReloadFileClick(object sender, RoutedEventArgs e)
    {
        if (SelectedTabIndex == 1)
        {
            ScanModFolders(updateStatus: true);
            RefreshActiveContext();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_vanillaDocument?.FilePath))
        {
            LoadVanillaFile(_vanillaDocument.FilePath, updateStatus: true);
        }
    }

    internal void ReloadProgressionFileClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_progressionDocument?.FilePath))
        {
            LoadProgressionFile(_progressionDocument.FilePath, updateStatus: true);
        }
    }

    internal void SaveFileClick(object sender, RoutedEventArgs e)
    {
        if (ActiveDocument is null || string.IsNullOrWhiteSpace(ActiveFilePath))
        {
            MessageBox.Show(this, "Select a valid vehicle config file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            CreateBackupFile(ActiveFilePath);
            ActiveDocument.Save(ActiveFilePath);
            StatusMessage = $"Saved changes and refreshed backup: {GetBackupPath(ActiveFilePath)}";
            MessageBox.Show(this, "Changes saved successfully.", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void SaveProgressionFileClick(object sender, RoutedEventArgs e)
    {
        if (_progressionDocument is null || string.IsNullOrWhiteSpace(_progressionDocument.FilePath))
        {
            MessageBox.Show(this, "Select a valid progression.xml file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            CreateBackupFile(_progressionDocument.FilePath);
            _progressionDocument.Save(_progressionDocument.FilePath);
            ProgressionStatusMessage = $"Saved changes and refreshed backup: {GetBackupPath(_progressionDocument.FilePath)}";
            MessageBox.Show(this, "Progression changes saved successfully.", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            NotifyProgressionFileAvailabilityChanged();
        }
        catch (Exception ex)
        {
            ProgressionStatusMessage = $"Save failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CreateBackupClick(object sender, RoutedEventArgs e)
    {
        if (ActiveDocument is null || string.IsNullOrWhiteSpace(ActiveFilePath))
        {
            MessageBox.Show(this, "Select a valid vehicle config file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var backupPath = CreateBackupFile(ActiveFilePath);
            StatusMessage = $"Backup created: {backupPath}";
            MessageBox.Show(this, $"Backup created:\n{backupPath}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Backup Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CreateProgressionBackupClick(object sender, RoutedEventArgs e)
    {
        if (_progressionDocument is null || string.IsNullOrWhiteSpace(_progressionDocument.FilePath))
        {
            MessageBox.Show(this, "Select a valid progression.xml file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var backupPath = CreateBackupFile(_progressionDocument.FilePath);
            ProgressionStatusMessage = $"Backup created: {backupPath}";
            MessageBox.Show(this, $"Backup created:\n{backupPath}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
            NotifyProgressionFileAvailabilityChanged();
        }
        catch (Exception ex)
        {
            ProgressionStatusMessage = $"Backup failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Backup Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void RestoreBackupClick(object sender, RoutedEventArgs e)
    {
        if (ActiveDocument is null || string.IsNullOrWhiteSpace(ActiveFilePath))
        {
            MessageBox.Show(this, "Select a valid vehicle config file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var backupPath = GetBackupPath(ActiveFilePath);
        if (!File.Exists(backupPath))
        {
            StatusMessage = "No backup file was found next to the selected XML.";
            MessageBox.Show(this, "No backup file was found next to the selected XML.", "Restore Backup", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Restore this backup?\n\n{backupPath}\n\nThis will overwrite the current file.",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            File.Copy(backupPath, ActiveFilePath, overwrite: true);

            if (SelectedTabIndex == 0)
            {
                LoadVanillaFile(ActiveFilePath, updateStatus: false);
            }
            else
            {
                ScanModFolders(updateStatus: false);
                RestorePreviousModSelection(ActiveFilePath);
                RefreshActiveContext();
            }

            StatusMessage = $"Restored backup from {backupPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void RestoreProgressionBackupClick(object sender, RoutedEventArgs e)
    {
        if (_progressionDocument is null || string.IsNullOrWhiteSpace(_progressionDocument.FilePath))
        {
            MessageBox.Show(this, "Select a valid progression.xml file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var backupPath = GetBackupPath(_progressionDocument.FilePath);
        if (!File.Exists(backupPath))
        {
            ProgressionStatusMessage = "No backup file was found next to the selected progression.xml.";
            MessageBox.Show(this, "No backup file was found next to the selected progression.xml.", "Restore Backup", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Restore this backup?\n\n{backupPath}\n\nThis will overwrite the current progression.xml file.",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            File.Copy(backupPath, _progressionDocument.FilePath, overwrite: true);
            LoadProgressionFile(_progressionDocument.FilePath, updateStatus: false);
            ProgressionStatusMessage = $"Restored backup from {backupPath}";
        }
        catch (Exception ex)
        {
            ProgressionStatusMessage = $"Restore failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void ApplyPerformanceMultiplierClick(object sender, RoutedEventArgs e)
    {
        if (CurrentVehicle is null && ApplyToSelectedVehicle)
        {
            MessageBox.Show(this, "Select a vehicle first.", "No Vehicle Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!AffectMotorTorque && !AffectMaxVelocity && !AffectBrakeTorque)
        {
            MessageBox.Show(this, "Select at least one performance setting to affect.", "Nothing Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!double.TryParse(MultiplierText, out var multiplier))
        {
            MessageBox.Show(this, "Enter a valid multiplier, for example 1.1 or 0.85.", "Invalid Multiplier", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var targets = ApplyToSelectedVehicle
            ? CurrentVehicle is null ? [] : [CurrentVehicle]
            : ActiveVehicleScope.ToList();

        if (targets.Count == 0)
        {
            MessageBox.Show(this, "There are no vehicles available for the selected scope.", "No Vehicles", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var updatedValues = 0;
        foreach (var vehicle in targets)
        {
            if (AffectMotorTorque)
            {
                updatedValues += MultiplyProperty(vehicle, MotorTorquePropertyName, multiplier, roundToHalfStep: false);
            }

            if (AffectMaxVelocity)
            {
                updatedValues += MultiplyProperty(vehicle, MaxVelocityPropertyName, multiplier, roundToHalfStep: true);
            }

            if (AffectBrakeTorque)
            {
                updatedValues += MultiplyProperty(vehicle, BrakeTorquePropertyName, multiplier, roundToHalfStep: false);
            }
        }

        RebuildCurrentVehicleCollections();

        var scopeText = ApplyToSelectedVehicle
            ? "selected vehicle"
            : SelectedTabIndex == 0 ? "all vanilla vehicles" : "all discovered mod vehicles";
        StatusMessage = $"Applied multiplier {multiplier} to {scopeText}. Updated {updatedValues} value(s).";
    }

    internal void ModTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        switch (e.NewValue)
        {
            case ModFolderConfig folder:
                _selectedModFolder = folder;
                if (SelectedTabIndex == 1)
                {
                    SetCurrentVehicle(null);
                    RefreshCurrentPath();
                }
                break;

            case ModVehicleNode node:
                SelectedModVehicleNode = node;
                break;
        }
    }

    internal void ModTreePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        var treeViewItem = FindAncestor<TreeViewItem>(source);
        if (treeViewItem is not null)
        {
            treeViewItem.Focus();
            treeViewItem.IsSelected = true;
        }
    }

    private void LoadVanillaFile(string path, bool updateStatus)
    {
        try
        {
            _vanillaDocument = VehicleConfigDocument.Load(path);

            VanillaVehicles.Clear();
            foreach (var vehicle in _vanillaDocument.Vehicles)
            {
                VanillaVehicles.Add(vehicle);
            }

            SelectedVanillaVehicle = VanillaVehicles.FirstOrDefault();
            if (SelectedTabIndex == 0)
            {
                RefreshActiveContext();
            }

            if (updateStatus)
            {
                StatusMessage = $"Loaded {VanillaVehicles.Count} vanilla vehicle entries from {path}";
            }

            NotifyFileAvailabilityChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadProgressionFile(string path, bool updateStatus)
    {
        try
        {
            _progressionDocument = ProgressionConfigDocument.Load(path);
            ProgressionFilePath = path;

            ProgressionLevelProperties.Clear();
            foreach (var property in _progressionDocument.LevelProperties)
            {
                var definition = ProgressionLevelDefinitions.FirstOrDefault(item => item.Matches(property));
                if (definition is not null)
                {
                    property.DisplayName = definition.DisplayName;
                    property.Description = definition.Description;
                }

                ProgressionLevelProperties.Add(property);
            }

            ProgressionAttributeDefaultsProperties.Clear();
            foreach (var property in _progressionDocument.AttributeDefaults)
            {
                var definition = ProgressionAttributeDefaultDefinitions.FirstOrDefault(item => item.Matches(property));
                if (definition is not null)
                {
                    property.DisplayName = definition.DisplayName;
                    property.Description = definition.Description;
                }

                ProgressionAttributeDefaultsProperties.Add(property);
            }

            ProgressionAttributes.Clear();
            foreach (var attribute in _progressionDocument.Attributes)
            {
                foreach (var property in attribute.EditableProperties)
                {
                    var definition = ProgressionAttributeOverrideDefinitions.FirstOrDefault(item => item.Matches(property));
                    if (definition is not null)
                    {
                        property.DisplayName = definition.DisplayName;
                        property.Description = definition.Description;
                    }
                }

                ProgressionAttributes.Add(attribute);
            }

            SelectedProgressionAttribute = ProgressionAttributes.FirstOrDefault();

            ProgressionSkillDefaultsProperties.Clear();
            foreach (var property in _progressionDocument.SkillDefaults)
            {
                var definition = ProgressionSkillDefaultDefinitions.FirstOrDefault(item => item.Matches(property));
                if (definition is not null)
                {
                    property.DisplayName = definition.DisplayName;
                    property.Description = definition.Description;
                }

                ProgressionSkillDefaultsProperties.Add(property);
            }

            ProgressionSkills.Clear();
            foreach (var skill in _progressionDocument.Skills)
            {
                ProgressionSkills.Add(skill);
            }

            SelectedProgressionSkill = ProgressionSkills.FirstOrDefault();

            ProgressionCraftingSkillDefaultsProperties.Clear();
            foreach (var property in _progressionDocument.CraftingSkillDefaults)
            {
                var definition = ProgressionCraftingSkillDefaultDefinitions.FirstOrDefault(item => item.Matches(property));
                if (definition is not null)
                {
                    property.DisplayName = definition.DisplayName;
                    property.Description = definition.Description;
                }

                ProgressionCraftingSkillDefaultsProperties.Add(property);
            }

            ProgressionCraftingSkills.Clear();
            foreach (var craftingSkill in _progressionDocument.CraftingSkills)
            {
                ProgressionCraftingSkills.Add(craftingSkill);
            }

            SelectedProgressionCraftingSkill = ProgressionCraftingSkills.FirstOrDefault();

            ProgressionPerkDefaultsProperties.Clear();
            foreach (var property in _progressionDocument.PerkDefaults)
            {
                var definition = ProgressionPerkDefaultDefinitions.FirstOrDefault(item => item.Matches(property));
                if (definition is not null)
                {
                    property.DisplayName = definition.DisplayName;
                    property.Description = definition.Description;
                }

                ProgressionPerkDefaultsProperties.Add(property);
            }

            ProgressionPerks.Clear();
            foreach (var perk in _progressionDocument.Perks)
            {
                ProgressionPerks.Add(perk);
            }

            SelectedProgressionPerk = ProgressionPerks.FirstOrDefault();

            if (updateStatus)
            {
                ProgressionStatusMessage = $"Loaded progression sections from {path}";
            }

            NotifyProgressionFileAvailabilityChanged();
        }
        catch (Exception ex)
        {
            ProgressionStatusMessage = $"Load failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ScanModFolders(bool updateStatus)
    {
        var previousSelectedFile = SelectedModVehicleNode?.Document.FilePath ?? _selectedModFolder?.Document.FilePath;
        var previousVehicleName = SelectedModVehicleNode?.Vehicle.Name;

        ModFolders.Clear();
        _modsRootPath = DetectModsRootPath() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_modsRootPath) || !Directory.Exists(_modsRootPath))
        {
            _selectedModFolder = null;
            SelectedModVehicleNode = null;
            OnPropertyChanged(nameof(ModSummary));

            if (updateStatus)
            {
                StatusMessage = "No Mods folder was found under the detected game directory.";
            }

            return;
        }

        foreach (var modDirectory in Directory.GetDirectories(_modsRootPath))
        {
            var configPath = Path.Combine(modDirectory, "Config", "vehicles.xml");
            if (!File.Exists(configPath))
            {
                continue;
            }

            try
            {
                var document = VehicleConfigDocument.Load(configPath);
                var displayName = ReadModDisplayName(modDirectory) ?? Path.GetFileName(modDirectory);
                var modFolder = new ModFolderConfig(displayName, modDirectory, document);
                ModFolders.Add(modFolder);
            }
            catch
            {
                // Skip malformed mod vehicle configs rather than failing the whole scan.
            }
        }

        RestorePreviousModSelection(previousSelectedFile, previousVehicleName);
        OnPropertyChanged(nameof(ModSummary));

        if (updateStatus)
        {
            var vehicleCount = ModFolders.Sum(folder => folder.Vehicles.Count);
            StatusMessage = $"Scanned {_modsRootPath}. Found {ModFolders.Count} compatible vehicle mod(s) with {vehicleCount} vehicle(s).";
        }

        NotifyFileAvailabilityChanged();
    }

    private void RestorePreviousModSelection(string? previousSelectedFile, string? previousVehicleName = null)
    {
        if (ModFolders.Count == 0)
        {
            _selectedModFolder = null;
            SelectedModVehicleNode = null;
            if (SelectedTabIndex == 1)
            {
                SetCurrentVehicle(null);
                RefreshCurrentPath();
            }
            return;
        }

        ModVehicleNode? matchedVehicle = null;
        if (!string.IsNullOrWhiteSpace(previousSelectedFile) && !string.IsNullOrWhiteSpace(previousVehicleName))
        {
            matchedVehicle = ModFolders
                .Where(folder => string.Equals(folder.Document.FilePath, previousSelectedFile, StringComparison.OrdinalIgnoreCase))
                .SelectMany(folder => folder.Vehicles)
                .FirstOrDefault(node => string.Equals(node.Vehicle.Name, previousVehicleName, StringComparison.OrdinalIgnoreCase));
        }

        matchedVehicle ??= ModFolders.SelectMany(folder => folder.Vehicles).FirstOrDefault();

        if (matchedVehicle is not null)
        {
            _selectedModFolder = ModFolders.FirstOrDefault(folder =>
                string.Equals(folder.Name, matchedVehicle.ModName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(folder.Document.FilePath, matchedVehicle.Document.FilePath, StringComparison.OrdinalIgnoreCase));
            SelectedModVehicleNode = matchedVehicle;
        }
        else
        {
            _selectedModFolder = ModFolders.FirstOrDefault();
            SelectedModVehicleNode = null;
        }

        if (SelectedTabIndex == 1)
        {
            SetCurrentVehicle(SelectedModVehicleNode?.Vehicle);
            RefreshCurrentPath();
        }
    }

    private void RefreshActiveContext()
    {
        SetCurrentVehicle(SelectedTabIndex == 0 ? SelectedVanillaVehicle : SelectedModVehicleNode?.Vehicle);
        RefreshCurrentPath();
        OnPropertyChanged(nameof(CurrentVehicleSummary));
        OnPropertyChanged(nameof(BrowseButtonText));
        OnPropertyChanged(nameof(ReloadButtonText));
        OnPropertyChanged(nameof(MultiplierHelpText));
        OnPropertyChanged(nameof(ModSummary));
        NotifyFileAvailabilityChanged();
    }

    private void RefreshCurrentPath()
    {
        CurrentFilePath = SelectedTabIndex == 0
            ? _vanillaDocument?.FilePath ?? string.Empty
            : SelectedModVehicleNode?.Document.FilePath
                ?? _selectedModFolder?.Document.FilePath
                ?? _modsRootPath;
    }

    private void SetCurrentVehicle(VehicleConfig? vehicle) => CurrentVehicle = vehicle;

    private void RebuildCurrentVehicleCollections()
    {
        PerformanceProperties.Clear();
        HandlingProperties.Clear();
        FuelProperties.Clear();
        ExtraProperties.Clear();
        AdvancedProperties.Clear();

        if (CurrentVehicle is null)
        {
            return;
        }

        foreach (var definition in SafeDefinitions)
        {
            var property = CurrentVehicle.FindProperty(definition.PropertyName, definition.Section);
            if (property is null)
            {
                continue;
            }

            switch (definition.Category)
            {
                case "Performance":
                    PerformanceProperties.Add(property);
                    break;
                case "Handling":
                    HandlingProperties.Add(property);
                    break;
                case "Fuel":
                    FuelProperties.Add(property);
                    break;
                case "Extras":
                    ExtraProperties.Add(property);
                    break;
            }

            property.DisplayName = definition.DisplayName;
            property.Description = definition.Description;
        }

        foreach (var property in CurrentVehicle.AllProperties)
        {
            if (!SafeDefinitions.Any(definition => definition.Matches(property)))
            {
                AdvancedProperties.Add(property);
            }
        }
    }

    private static string? DetectDefaultVehiclesPath()
    {
        var gameDirectory = DetectGameDirectory();
        if (string.IsNullOrWhiteSpace(gameDirectory))
        {
            return null;
        }

        var vehiclesPath = Path.Combine(gameDirectory, "Data", "Config", "vehicles.xml");
        return File.Exists(vehiclesPath) ? vehiclesPath : null;
    }

    private static string? DetectDefaultProgressionPath()
    {
        var gameDirectory = DetectGameDirectory();
        if (string.IsNullOrWhiteSpace(gameDirectory))
        {
            return null;
        }

        var progressionPath = Path.Combine(gameDirectory, "Data", "Config", "progression.xml");
        return File.Exists(progressionPath) ? progressionPath : null;
    }

    private static string? DetectGameDirectory()
    {
        foreach (var candidate in EnumerateGameDirectoryCandidates())
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? DetectModsRootPath()
    {
        var gameDirectory = DetectGameDirectory();
        if (string.IsNullOrWhiteSpace(gameDirectory))
        {
            return null;
        }

        var modsPath = Path.Combine(gameDirectory, "Mods");
        return Directory.Exists(modsPath) ? modsPath : null;
    }

    private string GetInitialVanillaDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_vanillaDocument?.FilePath))
        {
            return Path.GetDirectoryName(_vanillaDocument.FilePath)!;
        }

        var defaultPath = DetectDefaultVehiclesPath();
        if (!string.IsNullOrWhiteSpace(defaultPath))
        {
            return Path.GetDirectoryName(defaultPath)!;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private string GetInitialProgressionDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_progressionDocument?.FilePath))
        {
            return Path.GetDirectoryName(_progressionDocument.FilePath)!;
        }

        var defaultPath = DetectDefaultProgressionPath();
        if (!string.IsNullOrWhiteSpace(defaultPath))
        {
            return Path.GetDirectoryName(defaultPath)!;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private static string GetBackupPath(string filePath) => $"{filePath}.bak";

    private void NotifyFileAvailabilityChanged()
    {
        OnPropertyChanged(nameof(HasActiveFile));
        OnPropertyChanged(nameof(CanRestoreBackup));
    }

    private void NotifyProgressionFileAvailabilityChanged()
    {
        OnPropertyChanged(nameof(HasActiveProgressionFile));
        OnPropertyChanged(nameof(CanRestoreProgressionBackup));
    }

    private void RebuildSelectedProgressionAttributeProperties()
    {
        SelectedProgressionAttributeProperties.Clear();

        if (SelectedProgressionAttribute is null)
        {
            return;
        }

        foreach (var property in SelectedProgressionAttribute.EditableProperties)
        {
            SelectedProgressionAttributeProperties.Add(property);
        }
    }

    private static string CreateBackupFile(string filePath)
    {
        var backupPath = GetBackupPath(filePath);
        File.Copy(filePath, backupPath, overwrite: true);
        return backupPath;
    }

    private static int MultiplyProperty(VehicleConfig vehicle, string propertyName, double multiplier, bool roundToHalfStep)
    {
        var property = vehicle.FindProperty(propertyName, section: null);
        if (property is null)
        {
            return 0;
        }

        var segments = property.Value
            .Split(',', StringSplitOptions.TrimEntries)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        if (segments.Length == 0)
        {
            return 0;
        }

        var updatedSegments = new List<string>(segments.Length);
        var updatedCount = 0;

        foreach (var segment in segments)
        {
            if (!double.TryParse(segment, out var number))
            {
                updatedSegments.Add(segment);
                continue;
            }

            var multiplied = number * multiplier;
            var rounded = roundToHalfStep
                ? Math.Round(multiplied * 2, MidpointRounding.AwayFromZero) / 2
                : Math.Round(multiplied, 0, MidpointRounding.AwayFromZero);

            updatedSegments.Add(FormatNumber(rounded, roundToHalfStep));
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            property.Value = string.Join(", ", updatedSegments);
        }

        return updatedCount;
    }

    private static string FormatNumber(double value, bool allowHalfStep)
    {
        if (!allowHalfStep)
        {
            return ((int)value).ToString();
        }

        return value % 1 == 0
            ? ((int)value).ToString()
            : value.ToString("0.0");
    }

    private static IEnumerable<string> EnumerateGameDirectoryCandidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var steamRoot in EnumerateSteamRoots())
        {
            foreach (var candidate in EnumerateGameDirectoriesFromSteamRoot(steamRoot))
            {
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateSteamRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in GetLikelySteamRootCandidates())
        {
            if (Directory.Exists(candidate) && seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<string> GetLikelySteamRootCandidates()
    {
        var roots = new List<string>();

        void AddIfPresent(string? basePath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return;
            }

            roots.Add(Path.Combine(basePath, relativePath));
        }

        AddIfPresent(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        AddIfPresent(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam");

        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed))
        {
            roots.Add(Path.Combine(drive.RootDirectory.FullName, "Steam"));
            roots.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam"));
            roots.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam"));
        }

        return roots;
    }

    private static IEnumerable<string> EnumerateGameDirectoriesFromSteamRoot(string steamRoot)
    {
        yield return Path.Combine(steamRoot, "steamapps", "common", "7 Days To Die");

        var libraryFoldersPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersPath))
        {
            yield break;
        }

        foreach (var libraryPath in ReadSteamLibraryPaths(libraryFoldersPath))
        {
            yield return Path.Combine(libraryPath, "steamapps", "common", "7 Days To Die");
        }
    }

    private static IEnumerable<string> ReadSteamLibraryPaths(string libraryFoldersPath)
    {
        var regex = new Regex("\"path\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);

        foreach (var line in File.ReadLines(libraryFoldersPath))
        {
            var match = regex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var rawPath = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            yield return rawPath.Replace(@"\\", @"\");
        }
    }

    private static string? ReadModDisplayName(string modDirectory)
    {
        try
        {
            var modInfoPath = Path.Combine(modDirectory, "ModInfo.xml");
            if (!File.Exists(modInfoPath))
            {
                return null;
            }

            var document = XDocument.Load(modInfoPath);
            return document.Root?
                .Elements("DisplayName")
                .Select(element => element.Attribute("value")?.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
        catch
        {
            return null;
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyWindowChromeColors();
    }

    private void ApplyWindowChromeColors()
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var captionColor = ToColorRef(0x23, 0x25, 0x1E);
            var textColor = ToColorRef(0xE4, 0xEE, 0xF2);
            var borderColor = ToColorRef(0x7B, 0x81, 0x7B);

            DwmSetWindowAttribute(handle, DwmaCaptionColor, ref captionColor, sizeof(uint));
            DwmSetWindowAttribute(handle, DwmaTextColor, ref textColor, sizeof(uint));
            DwmSetWindowAttribute(handle, DwmaBorderColor, ref borderColor, sizeof(uint));
        }
        catch
        {
            // Best-effort polish only; skip on unsupported systems.
        }
    }

    private static uint ToColorRef(byte red, byte green, byte blue) =>
        (uint)(red | (green << 8) | (blue << 16));

    private const int DwmaBorderColor = 34;
    private const int DwmaCaptionColor = 35;
    private const int DwmaTextColor = 36;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref uint pvAttribute, int cbAttribute);

    private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
            {
                return match;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class PropertyDefinition(
    string category,
    string propertyName,
    string? section,
    string displayName,
    string description)
{
    public string Category { get; } = category;
    public string PropertyName { get; } = propertyName;
    public string? Section { get; } = section;
    public string DisplayName { get; } = displayName;
    public string Description { get; } = description;

    public bool Matches(EditableProperty property) =>
        string.Equals(PropertyName, property.PropertyName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Section ?? string.Empty, property.Section, StringComparison.OrdinalIgnoreCase);
}
