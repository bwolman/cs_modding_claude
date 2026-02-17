# Research: Localization System

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

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

## Open Questions

- [ ] How does the UI template engine resolve `ILocElement` arguments in format strings?
- [ ] How are indexed entries (e.g., `SubServices.NAME:0`) used in practice?
- [ ] How does `UILocalizationManager` bridge between `LocalizationManager` and the Cohtml UI layer?
- [ ] What is the complete set of locale IDs supported by the base game?

## Sources

- Decompiled from: Colossal.Localization.dll, Game.dll (Cities: Skylines II)
- Key types: LocalizationManager (~428 lines), LocalizationDictionary (~122 lines), MemorySource (~34 lines), LocalizedString (~100+ lines), IDictionarySource
