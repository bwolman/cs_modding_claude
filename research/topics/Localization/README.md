# Research: Localization System

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2's localization pipeline loads, stores, and resolves translated strings. How mods add custom localized text using `IDictionarySource` and `MemorySource`.

**Why**: Every mod displaying text to users needs localization. The system is referenced in ModOptionsUI and Mod UI research but deserves standalone documentation.

**Boundaries**: Out of scope -- font rendering, text layout, UI widget internals.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Colossal.Localization.dll | Colossal.Localization | LocalizationManager, LocalizationDictionary, MemorySource, IDictionarySource, LocalizationEntry, CSVFileSource |
| Game.dll | Game.UI.Localization | LocalizedString, LocalizationBindings, UILocalizationManager, CachedLocalizedStringBuilder |
| Colossal.IO.AssetDatabase.dll | Colossal.IO.AssetDatabase | LocaleAsset |

## Component Map

### `LocalizationManager` (Colossal.Localization)

Central manager for all localization data. Maintains dictionaries per locale and resolves string lookups.

| Field / Property | Type | Description |
|------------------|------|-------------|
| `activeDictionary` | LocalizationDictionary | Currently active locale dictionary |
| `activeLocaleId` | string | ID of active locale (e.g., "en-US") |
| `fallbackLocaleId` | string | Fallback locale (usually "en-US") |
| `m_LocaleInfos` | Dictionary\<string, LocaleInfo\> | Per-locale source lists |
| `m_UserSources` | List\<(string, IDictionarySource)\> | User/mod-added sources |

**Key Methods**:
- `AddSource(localeId, source)` -- add a dictionary source for a locale
- `RemoveSource(localeId, source)` -- remove a dictionary source
- `SetActiveLocale(localeId)` -- switch the active language
- `GetSupportedLocales()` -- list all registered locale IDs
- `ReloadActiveLocale()` -- clear and reload current locale from sources

**Events**:
- `onActiveDictionaryChanged` -- fired when the active dictionary is rebuilt
- `onSupportedLocalesChanged` -- fired when available locales change

*Source: `Colossal.Localization.dll` -> `Colossal.Localization.LocalizationManager`*

### `LocalizationDictionary` (Colossal.Localization)

String lookup dictionary for a single locale.

| Field / Property | Type | Description |
|------------------|------|-------------|
| `localeID` | string | Locale identifier (e.g., "en-US") |
| `entryCount` | int | Number of entries |
| `entryIDs` | IEnumerable\<string\> | All registered string IDs |
| `entries` | IEnumerable\<KVP\<string, string\>\> | All ID-value pairs |
| `indexCounts` | Dictionary\<string, int\> | Indexed entry count tracking |

**Key Methods**:
- `TryGetValue(entryID, out value)` -- lookup a string by ID
- `ContainsID(entryID)` -- check if an ID exists
- `Add(entryID, value, fallback)` -- add or replace an entry
- `MergeFrom(other, fallback)` -- merge entries from another dictionary

*Source: `Colossal.Localization.dll` -> `Colossal.Localization.LocalizationDictionary`*

### `IDictionarySource` (Colossal.Localization)

Interface for providing localization entries to the system.

```csharp
public interface IDictionarySource
{
    IEnumerable<KeyValuePair<string, string>> ReadEntries(
        IList<IDictionaryEntryError> errors,
        Dictionary<string, int> indexCounts);
    void Unload();
}
```

### `MemorySource` (Colossal.Localization)

Simple in-memory implementation of `IDictionarySource`. Wraps a `Dictionary<string, string>`.

*Source: `Colossal.Localization.dll` -> `Colossal.Localization.MemorySource`*

### Custom `IDictionarySource` Implementation (Alternative to MemorySource)

Instead of passing a pre-built dictionary to `MemorySource`, mods can implement `IDictionarySource` directly. This enables dynamic key generation at read time — useful when localization keys depend on the mod's settings structure or runtime state.

The pattern holds a reference to the mod's `Setting` instance and uses helper methods like `GetSettingsLocaleID()` to generate keys matching the settings UI convention:

```csharp
using Colossal.Localization;

/// <summary>
/// Custom dictionary source that generates localization entries dynamically
/// from the mod's settings instance. Keys are generated using the settings
/// locale ID convention so they match the Options UI.
/// </summary>
public class MyModLocaleSource : IDictionarySource
{
    private readonly Setting _setting;

    public MyModLocaleSource(Setting setting)
    {
        _setting = setting;
    }

    public IEnumerable<KeyValuePair<string, string>> ReadEntries(
        IList<IDictionaryEntryError> errors,
        Dictionary<string, int> indexCounts)
    {
        var entries = new List<KeyValuePair<string, string>>();

        // Generate settings UI keys dynamically from the Setting instance.
        // GetSettingsLocaleID() returns the conventional key format:
        // "Options.SECTION[ModName.ModName.Mod]:GroupName"
        entries.Add(new(
            _setting.GetSettingsLocaleID(),
            "My Mod Settings"));

        // Per-option display names and descriptions
        entries.Add(new(
            _setting.GetOptionLabelLocaleID(nameof(Setting.EnableFeature)),
            "Enable Feature"));
        entries.Add(new(
            _setting.GetOptionDescLocaleID(nameof(Setting.EnableFeature)),
            "Toggle the main feature on or off"));

        // Per-tab and per-group labels
        entries.Add(new(
            _setting.GetOptionTabLocaleID(Setting.kGeneralTab),
            "General"));
        entries.Add(new(
            _setting.GetOptionGroupLocaleID(Setting.kMainGroup),
            "Main Settings"));

        // Enum value display names
        foreach (var value in Enum.GetValues(typeof(MyEnum)))
        {
            entries.Add(new(
                _setting.GetEnumValueLocaleID((MyEnum)value),
                value.ToString()));
        }

        return entries;
    }

    public void Unload() { }
}

// Registration in Mod.OnLoad():
// var locManager = GameManager.instance.localizationManager;
// locManager.AddSource("en-US", new MyModLocaleSource(settings));
```

**When to use `IDictionarySource` vs `MemorySource`:**
- Use `MemorySource` when you have a static, pre-known set of key-value pairs (e.g., loaded from a file)
- Use a custom `IDictionarySource` when keys are generated dynamically from the mod's settings structure, or when the entry set depends on runtime state
- Both approaches register the same way via `LocalizationManager.AddSource()`

### `LocalizedString` (Game.UI.Localization)

Readonly struct used in UI bindings to reference localized text. Supports string IDs, literal values, and parameterized substitution.

| Property | Type | Description |
|----------|------|-------------|
| `id` | string | Localization key (null for literal values) |
| `value` | string | Literal or fallback value (null for ID-only lookups) |
| `args` | IReadOnlyDictionary\<string, ILocElement\> | Substitution arguments |

**Factory Methods**:
- `LocalizedString.Id(id)` -- lookup by ID
- `LocalizedString.Value(value)` -- literal text
- `LocalizedString.IdWithFallback(id, value)` -- ID with fallback
- `LocalizedString.IdHash<T>(id, hash)` -- ID with enum hash suffix

*Source: `Game.dll` -> `Game.UI.Localization.LocalizedString`*

## Data Flow

```
[Locale assets loaded from AssetDatabase]
        |
[LocalizationManager.LoadAvailableLocales()]
  - Finds all LocaleAsset objects
  - Calls AddLocale() and AddSourceInternal() for each
        |
[Mod calls AddSource("en-US", new MemorySource(dict))]
  - Source added to m_UserSources list
  - If locale matches active: entries loaded into activeDictionary
  - If locale matches fallback: entries loaded into fallback, then merged
        |
[UI requests a string]
  - activeDictionary.TryGetValue(entryID, out value)
  - If not found in active locale: fallback entries used
  - LocalizedString sent to UI with id/value/args
```

## String ID Convention

CS2 uses a hierarchical dot-separated ID format:

```
// Prefab names (square brackets, NOT colon)
Assets.NAME[PrefabName]
Assets.DESCRIPTION[PrefabName]

// Settings
Options.SECTION[ModName.ModName.Mod]:Setting.DisplayName
Options.SECTION[ModName.ModName.Mod]:Setting.Description

// UI elements
Menu.TITLE[SomeTool]
ToolOptions.TOOLTIP[SomeOption]

// Indexed entries (with colon + index)
SubServices.NAME:0
SubServices.NAME:1
```

## Harmony Patch Points

### Candidate 1: `LocalizationManager.AddSource`

- **Signature**: `void AddSource(string localeId, IDictionarySource source)`
- **Patch type**: Postfix
- **What it enables**: Monitor when sources are added, inject additional entries
- **Risk level**: Low

### Candidate 2: `LocalizationDictionary.TryGetValue`

- **Signature**: `bool TryGetValue(string entryID, out string value)`
- **Patch type**: Postfix
- **What it enables**: Override specific string lookups dynamically
- **Risk level**: High (called very frequently)

## Mod Blueprint

- **To add localized strings**: Create a `MemorySource` with a `Dictionary<string, string>` and call `LocalizationManager.AddSource("en-US", source)` for each supported locale
- **To support multiple languages**: Create separate `MemorySource` instances per locale, each with the same keys but translated values
- **Access**: Get the manager via `GameManager.instance.localizationManager`

## Examples

### Example 1: Adding Mod Strings

```csharp
public void AddLocalization(LocalizationManager manager)
{
    var strings = new Dictionary<string, string>
    {
        { "MyMod.Title", "My Custom Mod" },
        { "MyMod.Description", "This mod adds cool features" },
        { "MyMod.Setting.Enabled", "Enable Feature" },
        { "MyMod.Setting.Enabled.Desc", "Toggle the main feature on or off" }
    };

    manager.AddSource("en-US", new MemorySource(strings));
}
```

### Example 2: Supporting Multiple Languages

```csharp
public void AddMultiLanguageStrings(LocalizationManager manager)
{
    manager.AddSource("en-US", new MemorySource(new Dictionary<string, string>
    {
        { "MyMod.Title", "My Custom Mod" },
        { "MyMod.Greeting", "Hello, Mayor!" }
    }));

    manager.AddSource("de-DE", new MemorySource(new Dictionary<string, string>
    {
        { "MyMod.Title", "Mein benutzerdefinierter Mod" },
        { "MyMod.Greeting", "Hallo, Buergermeister!" }
    }));
}
```

### Example 3: Looking Up a String

```csharp
var dictionary = GameManager.instance.localizationManager.activeDictionary;
if (dictionary.TryGetValue("MyMod.Title", out string title))
{
    // Use title
}
```

### Example 4: Using LocalizedString in UI Bindings

```csharp
// ID-only (resolved by the UI)
LocalizedString label = LocalizedString.Id("MyMod.Title");

// With substitution parameters
LocalizedString greeting = LocalizedString.Id("MyMod.Welcome",
    ("PLAYER_NAME", LocalizedString.Value("Mayor")));

// Literal value (no localization lookup)
LocalizedString literal = LocalizedString.Value("Fixed text");

// ID with fallback
LocalizedString safe = LocalizedString.IdWithFallback(
    "MyMod.MaybeExists", "Default Text");
```

### Example 5: Embedded JSON Localization with Assembly Resources

Load localization from JSON files embedded as assembly resources. This pattern supports multiple languages with graceful fallback.

```csharp
public void LoadEmbeddedLocalization()
{
    var assembly = Assembly.GetExecutingAssembly();
    var supportedLocales = GameManager.instance.localizationManager.GetSupportedLocales();

    foreach (var locale in supportedLocales)
    {
        // Convention: AssemblyName.l10n.localeID.json
        string resourceName = $"{assembly.GetName().Name}.l10n.{locale}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) continue; // Locale not supported by this mod

        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();

        // Parse JSON into Dictionary<string, string>
        // Using Colossal.Json: var dict = JSON.Load(json) as Dictionary<string, string>;
        // Or System.Text.Json: var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        var source = new MemorySource(dict);
        GameManager.instance.localizationManager.AddSource(locale, source);
    }
}
```

**Resource naming convention**: `{AssemblyName}.l10n.{localeID}.json` (e.g., `TreeController.l10n.en-US.json`, `TreeController.l10n.de-DE.json`). The JSON file is a flat object mapping string IDs to translated text.

**Graceful fallback**: If a locale's JSON file doesn't exist as an embedded resource, the `GetManifestResourceStream` call returns null and the locale is skipped. The game falls back to the default locale (en-US) for missing translations.

### Example 6: CSV-Based Multi-Locale Localization Loading

Load localization from a tab-separated embedded resource file with columns for each locale. This pattern scales well for mods supporting many languages — all translations live in a single file.

**CSV file format** (`Resources/l10n.csv`, tab-separated, embedded resource):

```
KEY	en-US	de-DE	fr-FR	zh-HANS
MyMod.Title	My Custom Mod	Mein Mod	Mon Mod	我的模组
MyMod.Greeting	Hello, Mayor!	Hallo, Buergermeister!	Bonjour, Maire!	你好，市长！
MyMod.Setting.Enabled	Enable Feature	Feature aktivieren	Activer la fonctionnalite	启用功能
```

**Loader implementation:**

```csharp
using System.IO;
using System.Reflection;
using Colossal.Localization;

/// <summary>
/// Loads localization from a tab-separated embedded resource file.
/// Each column after the key column corresponds to a locale ID.
/// Creates a MemorySource per supported locale found in the CSV header.
/// </summary>
public static class CsvLocalizationLoader
{
    public static void Load(LocalizationManager locManager)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"{assembly.GetName().Name}.Resources.l10n.csv";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Mod.Log.Warn("Localization CSV not found as embedded resource");
            return;
        }

        using var reader = new StreamReader(stream);

        // Parse header row to get locale column indices
        string headerLine = reader.ReadLine();
        if (headerLine == null) return;
        string[] headers = headerLine.Split('\t');

        // Get the game's supported locales to know which columns to load
        var supportedLocales = new HashSet<string>(locManager.GetSupportedLocales());

        // Map column index -> locale ID (skip column 0 which is the key)
        var localeColumns = new Dictionary<int, string>();
        for (int i = 1; i < headers.Length; i++)
        {
            string localeId = headers[i].Trim();
            if (supportedLocales.Contains(localeId))
                localeColumns[i] = localeId;
        }

        // Build dictionaries per locale
        var dictionaries = new Dictionary<string, Dictionary<string, string>>();
        foreach (var localeId in localeColumns.Values)
            dictionaries[localeId] = new Dictionary<string, string>();

        // Parse data rows
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] columns = line.Split('\t');
            if (columns.Length < 2) continue;

            string key = columns[0].Trim();
            foreach (var (colIndex, localeId) in localeColumns)
            {
                if (colIndex < columns.Length && !string.IsNullOrEmpty(columns[colIndex]))
                    dictionaries[localeId][key] = columns[colIndex];
            }
        }

        // Register a MemorySource per locale
        foreach (var (localeId, dict) in dictionaries)
        {
            if (dict.Count > 0)
            {
                locManager.AddSource(localeId, new MemorySource(dict));
                Mod.Log.Info($"Loaded {dict.Count} localization entries for {localeId}");
            }
        }
    }
}

// Call in Mod.OnLoad():
// CsvLocalizationLoader.Load(GameManager.instance.localizationManager);
```

**Embedding the CSV in .csproj:**

```xml
<ItemGroup>
    <EmbeddedResource Include="Resources\l10n.csv" />
</ItemGroup>
```

**Loading embedded resources**: `Assembly.GetManifestResourceStream()` expects the full resource name in the format `{AssemblyName}.{FolderPath}.{FileName}` where folder separators are replaced with dots. For example, `Resources/l10n.csv` in assembly `MyMod` becomes `MyMod.Resources.l10n.csv`.

### JavaScript-Side Translation Resolution (engine.translate)

When using the low-level cohtml communication pattern (without the React/TypeScript build pipeline), JavaScript code can resolve localization keys via `engine.translate()`:

```javascript
// Resolve a localization key to its translated string
var translatedText = engine.translate("MyMod.Title");

// Use in dynamically injected DOM elements
var label = document.createElement('span');
label.textContent = engine.translate("MyMod.Greeting");
```

`engine.translate()` queries the same `LocalizationDictionary` used by the C# side. It returns the translated string for the active locale, falling back to the fallback locale if the key is not found. This is the cohtml-level equivalent of `LocalizedString.Id()` on the C# side.

**Note**: When using the standard TypeScript/React build pipeline, localization is typically handled via `LocalizedString` bindings pushed from C# rather than calling `engine.translate()` directly. The `engine.translate()` approach is primarily useful for raw DOM injection patterns (see ModUIButtons research).

## Open Questions

- [ ] How does the UI template engine resolve `ILocElement` arguments in format strings?
- [ ] How are indexed entries (e.g., `SubServices.NAME:0`) used in practice?
- [ ] How does `UILocalizationManager` bridge between `LocalizationManager` and the Cohtml UI layer?
- [ ] What is the complete set of locale IDs supported by the base game?

## Sources

- Decompiled from: Colossal.Localization.dll, Game.dll (Cities: Skylines II)
- Key types: LocalizationManager (~428 lines), LocalizationDictionary (~122 lines), MemorySource (~34 lines), LocalizedString (~100+ lines), IDictionarySource
