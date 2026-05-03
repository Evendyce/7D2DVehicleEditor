using System.Xml.Linq;

namespace SevenDaysVehicleEditor;

public sealed class ProgressionConfigDocument
{
    private readonly XDocument _document;

    private ProgressionConfigDocument(
        string filePath,
        XDocument document,
        IReadOnlyList<EditableProperty> levelProperties,
        IReadOnlyList<EditableProperty> attributeDefaults,
        IReadOnlyList<ProgressionAttributeConfig> attributes,
        IReadOnlyList<EditableProperty> skillDefaults,
        IReadOnlyList<ProgressionSkillConfig> skills,
        IReadOnlyList<EditableProperty> craftingSkillDefaults,
        IReadOnlyList<ProgressionCraftingSkillConfig> craftingSkills,
        IReadOnlyList<EditableProperty> perkDefaults,
        IReadOnlyList<ProgressionPerkConfig> perks)
    {
        FilePath = filePath;
        _document = document;
        LevelProperties = levelProperties;
        AttributeDefaults = attributeDefaults;
        Attributes = attributes;
        SkillDefaults = skillDefaults;
        Skills = skills;
        CraftingSkillDefaults = craftingSkillDefaults;
        CraftingSkills = craftingSkills;
        PerkDefaults = perkDefaults;
        Perks = perks;
    }

    public string FilePath { get; }

    public IReadOnlyList<EditableProperty> LevelProperties { get; }

    public IReadOnlyList<EditableProperty> AttributeDefaults { get; }

    public IReadOnlyList<ProgressionAttributeConfig> Attributes { get; }

    public IReadOnlyList<EditableProperty> SkillDefaults { get; }

    public IReadOnlyList<ProgressionSkillConfig> Skills { get; }

    public IReadOnlyList<EditableProperty> CraftingSkillDefaults { get; }

    public IReadOnlyList<ProgressionCraftingSkillConfig> CraftingSkills { get; }

    public IReadOnlyList<EditableProperty> PerkDefaults { get; }

    public IReadOnlyList<ProgressionPerkConfig> Perks { get; }

    public static ProgressionConfigDocument Load(string path)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var levelElement = document.Root?.Element("level")
            ?? throw new InvalidOperationException("The progression.xml file does not contain a top-level <level> element.");

        var levelProperties = levelElement.Attributes()
            .Select(attribute => new EditableProperty(
                "progression.level",
                "level",
                attribute.Name.LocalName,
                attribute.Name.LocalName,
                string.Empty,
                attribute.Value,
                attribute))
            .ToList();

        var attributesElement = document.Root?.Element("attributes")
            ?? throw new InvalidOperationException("The progression.xml file does not contain a top-level <attributes> element.");

        var attributeDefaults = attributesElement.Attributes()
            .Select(attribute => new EditableProperty(
                "progression.attributes",
                "attributes",
                attribute.Name.LocalName,
                attribute.Name.LocalName,
                string.Empty,
                attribute.Value,
                attribute))
            .ToList();

        var attributes = attributesElement.Elements("attribute")
            .Select(BuildAttribute)
            .ToList();

        var skillsElement = document.Root?.Element("skills")
            ?? throw new InvalidOperationException("The progression.xml file does not contain a top-level <skills> element.");

        var skillDefaults = skillsElement.Attributes()
            .Select(attribute => new EditableProperty(
                "progression.skills",
                "skills",
                attribute.Name.LocalName,
                attribute.Name.LocalName,
                string.Empty,
                attribute.Value,
                attribute))
            .ToList();

        var skills = skillsElement.Elements()
            .Where(element => string.Equals(element.Name.LocalName, "skill", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(element.Name.LocalName, "book_group", StringComparison.OrdinalIgnoreCase))
            .Select(BuildSkill)
            .ToList();

        var craftingSkillsElement = document.Root?.Element("crafting_skills")
            ?? throw new InvalidOperationException("The progression.xml file does not contain a top-level <crafting_skills> element.");

        var craftingSkillDefaults = craftingSkillsElement.Attributes()
            .Select(attribute => new EditableProperty(
                "progression.crafting_skills",
                "crafting_skills",
                attribute.Name.LocalName,
                attribute.Name.LocalName,
                string.Empty,
                attribute.Value,
                attribute))
            .ToList();

        var craftingSkills = craftingSkillsElement.Elements("crafting_skill")
            .Select(BuildCraftingSkill)
            .ToList();

        var perksElement = document.Root?.Element("perks")
            ?? throw new InvalidOperationException("The progression.xml file does not contain a top-level <perks> element.");

        var perkDefaults = perksElement.Attributes()
            .Select(attribute => new EditableProperty(
                "progression.perks",
                "perks",
                attribute.Name.LocalName,
                attribute.Name.LocalName,
                string.Empty,
                attribute.Value,
                attribute))
            .ToList();

        var perks = perksElement.Elements("perk")
            .Select(BuildPerk)
            .ToList();

        return new ProgressionConfigDocument(
            path,
            document,
            levelProperties,
            attributeDefaults,
            attributes,
            skillDefaults,
            skills,
            craftingSkillDefaults,
            craftingSkills,
            perkDefaults,
            perks);
    }

    public void Save(string path) => _document.Save(path, SaveOptions.DisableFormatting);

    private static ProgressionAttributeConfig BuildAttribute(XElement attributeElement)
    {
        var name = attributeElement.Attribute("name")?.Value ?? "Unnamed Attribute";
        var displayName = GetAttributeDisplayName(name);
        var editableProperties = new List<EditableProperty>();

        foreach (var attributeName in new[] { "min_level", "max_level", "base_skill_point_cost", "hidden" })
        {
            var attribute = attributeElement.Attribute(attributeName);
            if (attribute is null)
            {
                continue;
            }

            editableProperties.Add(new EditableProperty(
                name,
                "attribute",
                attribute.Name.LocalName,
                attribute.Name.LocalName,
                string.Empty,
                attribute.Value,
                attribute));
        }

        var levelRequirementCount = attributeElement.Elements("level_requirements").Count();
        var effectGroupCount = attributeElement.Elements("effect_group").Count();

        return new ProgressionAttributeConfig(
            name,
            displayName,
            attributeElement.Attribute("icon")?.Value ?? string.Empty,
            attributeElement.Attribute("desc_key")?.Value ?? string.Empty,
            attributeElement.Attribute("name_key")?.Value ?? string.Empty,
            editableProperties,
            levelRequirementCount,
            effectGroupCount);
    }

    private static ProgressionSkillConfig BuildSkill(XElement skillElement)
    {
        var type = skillElement.Name.LocalName;
        var name = skillElement.Attribute("name")?.Value ?? $"Unnamed {type}";
        var displayName = GetSkillDisplayName(name);
        var parent = skillElement.Attribute("parent")?.Value ?? string.Empty;
        var nameKey = skillElement.Attribute("name_key")?.Value ?? string.Empty;
        var descriptionKey = skillElement.Attribute("desc_key")?.Value ?? string.Empty;
        var longDescriptionKey = skillElement.Attribute("long_desc_key")?.Value ?? string.Empty;
        var icon = skillElement.Attribute("icon")?.Value ?? string.Empty;
        var hidden = skillElement.Attribute("hidden")?.Value ?? string.Empty;
        var effectGroupCount = skillElement.Elements("effect_group").Count();

        return new ProgressionSkillConfig(
            type,
            name,
            displayName,
            parent,
            nameKey,
            descriptionKey,
            longDescriptionKey,
            icon,
            hidden,
            effectGroupCount);
    }

    private static ProgressionCraftingSkillConfig BuildCraftingSkill(XElement element)
    {
        var name = element.Attribute("name")?.Value ?? "Unnamed Crafting Skill";
        var displayName = GetCraftingSkillDisplayName(name);
        var displayEntryCount = element.Elements("display_entry").Count();
        var unlockEntryCount = element.Descendants("unlock_entry").Count();
        var effectGroupCount = element.Elements("effect_group").Count();
        var passiveEffectCount = element.Descendants("passive_effect").Count();

        return new ProgressionCraftingSkillConfig(
            name,
            displayName,
            element.Attribute("parent")?.Value ?? string.Empty,
            element.Attribute("max_level")?.Value ?? string.Empty,
            element.Attribute("name_key")?.Value ?? string.Empty,
            element.Attribute("desc_key")?.Value ?? string.Empty,
            element.Attribute("long_desc_key")?.Value ?? string.Empty,
            element.Attribute("icon")?.Value ?? string.Empty,
            displayEntryCount,
            unlockEntryCount,
            effectGroupCount,
            passiveEffectCount);
    }

    private static ProgressionPerkConfig BuildPerk(XElement element)
    {
        var name = element.Attribute("name")?.Value ?? "Unnamed Perk";
        var displayName = GetPerkDisplayName(name);
        var levelRequirementCount = element.Elements("level_requirements").Count();
        var effectGroupCount = element.Elements("effect_group").Count();
        var passiveEffectCount = element.Descendants("passive_effect").Count();
        var triggeredEffectCount = element.Descendants("triggered_effect").Count();
        var effectDescriptionCount = element.Descendants("effect_description").Count();

        return new ProgressionPerkConfig(
            name,
            displayName,
            element.Attribute("parent")?.Value ?? string.Empty,
            element.Attribute("max_level")?.Value ?? string.Empty,
            element.Attribute("override_cost")?.Value ?? string.Empty,
            element.Attribute("name_key")?.Value ?? string.Empty,
            element.Attribute("desc_key")?.Value ?? string.Empty,
            element.Attribute("icon")?.Value ?? string.Empty,
            levelRequirementCount,
            effectGroupCount,
            passiveEffectCount,
            triggeredEffectCount,
            effectDescriptionCount);
    }

    private static string GetAttributeDisplayName(string name) => name switch
    {
        "attPerception" => "Perception",
        "attStrength" => "Strength",
        "attFortitude" => "Fortitude",
        "attAgility" => "Agility",
        "attIntellect" => "Intellect",
        "attGeneralPerks" => "General Perks",
        "attBooks" => "Books",
        "attCrafting" => "Crafting",
        _ when name.StartsWith("att", StringComparison.OrdinalIgnoreCase) => name[3..],
        _ => name
    };

    private static string GetSkillDisplayName(string name)
    {
        if (name.StartsWith("skill", StringComparison.OrdinalIgnoreCase) && name.Length > 5)
        {
            return name[5..];
        }

        return name;
    }

    private static string GetCraftingSkillDisplayName(string name)
    {
        if (name.StartsWith("crafting", StringComparison.OrdinalIgnoreCase) && name.Length > 8)
        {
            return name[8..];
        }

        return name;
    }

    private static string GetPerkDisplayName(string name)
    {
        if (name.StartsWith("perk", StringComparison.OrdinalIgnoreCase) && name.Length > 4)
        {
            return name[4..];
        }

        return name;
    }
}

public sealed class ProgressionAttributeConfig(
    string name,
    string displayName,
    string icon,
    string descriptionKey,
    string nameKey,
    IReadOnlyList<EditableProperty> editableProperties,
    int levelRequirementCount,
    int effectGroupCount)
{
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public string Icon { get; } = icon;
    public string DescriptionKey { get; } = descriptionKey;
    public string NameKey { get; } = nameKey;
    public IReadOnlyList<EditableProperty> EditableProperties { get; } = editableProperties;
    public int LevelRequirementCount { get; } = levelRequirementCount;
    public int EffectGroupCount { get; } = effectGroupCount;
    public string Summary => $"{LevelRequirementCount} level requirement(s), {EffectGroupCount} effect group(s)";
}

public sealed class ProgressionSkillConfig(
    string type,
    string name,
    string displayName,
    string parent,
    string nameKey,
    string descriptionKey,
    string longDescriptionKey,
    string icon,
    string hidden,
    int effectGroupCount)
{
    public string Type { get; } = type;
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public string Parent { get; } = parent;
    public string NameKey { get; } = nameKey;
    public string DescriptionKey { get; } = descriptionKey;
    public string LongDescriptionKey { get; } = longDescriptionKey;
    public string Icon { get; } = icon;
    public string Hidden { get; } = hidden;
    public int EffectGroupCount { get; } = effectGroupCount;
    public string TypeLabel => string.Equals(Type, "book_group", StringComparison.OrdinalIgnoreCase) ? "Book Group" : "Skill";
    public string Summary => $"{TypeLabel} under {Parent} with {EffectGroupCount} effect group(s)";
}

public sealed class ProgressionCraftingSkillConfig(
    string name,
    string displayName,
    string parent,
    string maxLevel,
    string nameKey,
    string descriptionKey,
    string longDescriptionKey,
    string icon,
    int displayEntryCount,
    int unlockEntryCount,
    int effectGroupCount,
    int passiveEffectCount)
{
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public string Parent { get; } = parent;
    public string MaxLevel { get; } = maxLevel;
    public string NameKey { get; } = nameKey;
    public string DescriptionKey { get; } = descriptionKey;
    public string LongDescriptionKey { get; } = longDescriptionKey;
    public string Icon { get; } = icon;
    public int DisplayEntryCount { get; } = displayEntryCount;
    public int UnlockEntryCount { get; } = unlockEntryCount;
    public int EffectGroupCount { get; } = effectGroupCount;
    public int PassiveEffectCount { get; } = passiveEffectCount;
    public string Summary => $"{DisplayEntryCount} display entr{(DisplayEntryCount == 1 ? "y" : "ies")}, {UnlockEntryCount} unlock entr{(UnlockEntryCount == 1 ? "y" : "ies")}, {PassiveEffectCount} passive effect(s)";
}

public sealed class ProgressionPerkConfig(
    string name,
    string displayName,
    string parent,
    string maxLevel,
    string overrideCost,
    string nameKey,
    string descriptionKey,
    string icon,
    int levelRequirementCount,
    int effectGroupCount,
    int passiveEffectCount,
    int triggeredEffectCount,
    int effectDescriptionCount)
{
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public string Parent { get; } = parent;
    public string MaxLevel { get; } = maxLevel;
    public string OverrideCost { get; } = overrideCost;
    public string NameKey { get; } = nameKey;
    public string DescriptionKey { get; } = descriptionKey;
    public string Icon { get; } = icon;
    public int LevelRequirementCount { get; } = levelRequirementCount;
    public int EffectGroupCount { get; } = effectGroupCount;
    public int PassiveEffectCount { get; } = passiveEffectCount;
    public int TriggeredEffectCount { get; } = triggeredEffectCount;
    public int EffectDescriptionCount { get; } = effectDescriptionCount;
    public string Summary => $"{LevelRequirementCount} requirement(s), {EffectGroupCount} effect group(s), {PassiveEffectCount} passive effect(s)";
}
