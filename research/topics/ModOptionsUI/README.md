# Research: Mod Options UI System

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2's Options UI system works for mods -- registering settings pages, rendering widgets, keybinding rebinding UI, and localization.

**Why**: To add user-configurable settings and rebindable hotkeys to mods via the in-game Options screen.

**Boundaries**: Not covering the Cohtml rendering layer or custom UI beyond the Options page. Keybinding registration (the `Game.Input` side) is documented in the companion topic `ModHotkeyInput`.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Settings | `Setting` base class, all `SettingsUI*` attributes |
| Game.dll | Game.Modding | `ModSetting` (mod-specific subclass) |
| Game.dll | Game.UI.Menu | `AutomaticSettings` (reflection engine), `OptionsUISystem` (UI rendering), `InputRebindingUISystem` (keybinding capture) |

## Architecture

The Options UI is **reflection-driven**: your `ModSetting` subclass is scanned via reflection at registration time. C# property types + attributes determine which UI widget is created.

```
ModSetting subclass
    |-- Properties: bool, int, float, string, enum, ProxyBinding
    |-- Attributes: [SettingsUISection], [SettingsUISlider], [SettingsUIHidden], etc.
    |
    v
RegisterInOptionsUI()
    |-- Setting.RegisterInOptionsUI(id, addPrefix=true)
    |   |-- OptionsUISystem.RegisterSetting(instance, id, addPrefix)
    |       |-- setting.GetPageData(id, addPrefix)
    |           |-- AutomaticSettings.FillSettingsPage(this, id, addPrefix)
    |               |-- Reads class-level attrs: TabOrder, GroupOrder, ShowGroupName
    |               |-- Iterates all public instance properties
    |               |   |-- Skips [SettingsUIHidden], [SettingsUIDeveloper]
    |               |   |-- Determines WidgetType from property type + attrs
    |               |   |-- Reads [SettingsUISection] for tab/group placement
    |               |   |-- Creates SettingItemData per property
    |               |-- Returns SettingPageData
    |       |-- pageData.BuildPage() -> Page with Sections and Options
    |       |-- page.builtIn = false (for mods)
    |
    v
[Options screen opens]
    |-- OptionsUISystem renders pages
    |-- Built-in pages first, then mod pages by registration order
    |-- Each page has Sections (tabs) with Options (widgets)
    |
    v
[User changes a setting]
    |-- Widget calls property setter on the ModSetting instance
    |-- For keybindings: InputRebindingUISystem captures new key
    |-- Apply() fires onSettingsApplied event
    |-- ApplyAndSave() persists to disk via AssetDatabase
```

## Widget Type Determination

The `AutomaticSettings.GetWidgetType()` method maps C# property types to UI widgets:

| Property Type | Attribute(s) | Widget |
|---------------|-------------|--------|
| `bool` (read+write) | none | Toggle |
| `bool` (read+write) | `[SettingsUIButton]` | Button |
| `bool` (read+write) | `[SettingsUIButton]` + `[SettingsUIConfirmation]` | Button with confirmation dialog |
| `bool` (write-only) | none | Button |
| `int` | `[SettingsUISlider]` | Int slider |
| `int` | `[SettingsUIDropdown]` | Int dropdown |
| `float` | `[SettingsUISlider]` | Float slider |
| `string` (read+write) | `[SettingsUITextInput]` | Text input field |
| `string` (read+write) | `[SettingsUIDropdown]` | String dropdown |
| `string` (read+write) | `[SettingsUIDirectoryPicker]` | Directory picker |
| `string` (read-only) | `[SettingsUIMultilineText]` | Multiline text block |
| `string` (read-only) | none | String display |
| `enum` (any) | none | Enum dropdown |
| `enum` (any) | `[SettingsUIDropdown]` | Custom enum dropdown |
| `ProxyBinding` | none | **Keybinding rebind widget** |
| `IJsonWritable+IJsonReadable` | `[SettingsUIDropdown]` | Custom dropdown |

**Key insight**: `ProxyBinding` properties trigger the keybinding widget automatically -- no extra attribute needed beyond `[SettingsUIKeyboardBinding]` for the default key.

## Attribute Reference

### Layout Attributes

#### `[SettingsUITabOrder(params string[] tabs)]`

**Target**: Class | **Purpose**: Declares and orders tabs

```csharp
[SettingsUITabOrder("General", "Keybindings", "Advanced")]
public class MySettings : ModSetting { ... }
```

Also supports dynamic form: `[SettingsUITabOrder(typeof(MySettings), "GetTabOrder")]` where method returns `string[]`.

| Field | Type | Description |
|-------|------|-------------|
| `tabs` | ReadOnlyCollection\<string\> | Tab names in display order |
| `checkType` | Type | Optional: type containing dynamic getter |
| `checkMethod` | string | Optional: method name returning string[] |

#### `[SettingsUIGroupOrder(params string[] groups)]`

**Target**: Class | **Purpose**: Declares and orders groups within tabs

```csharp
[SettingsUIGroupOrder("Behavior", "Display", "Advanced")]
```

Same structure as TabOrder. Also supports dynamic form.

#### `[SettingsUISection(string tab, string group)]`

**Target**: Class or Property (AllowMultiple) | **Purpose**: Places property into a tab+group

```csharp
[SettingsUISection("General", "Behavior")]
public bool MyToggle { get; set; }
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `tab` | string | "General" | Tab name |
| `simpleGroup` | string | "" | Group in simple view |
| `advancedGroup` | string | same as simple | Group in advanced view |

Overloads:
- `(string group)` -- default "General" tab
- `(string tab, string group)` -- same group for simple and advanced
- `(string tab, string simpleGroup, string advancedGroup)` -- separate groups

#### `[SettingsUIShowGroupName]` / `[SettingsUIShowGroupName(params string[] groups)]`

**Target**: Class/Struct | **Purpose**: Makes group header labels visible

Without this attribute, groups organize properties but no header is rendered.

| Field | Type | Description |
|-------|------|-------------|
| `showAll` | bool | True when using parameterless constructor |
| `groups` | ReadOnlyCollection\<string\> | Specific groups to show (null = all) |

#### `[SettingsUIButtonGroup(string name)]`

**Target**: Property | **Purpose**: Groups multiple button properties into a horizontal row

```csharp
[SettingsUIButton]
[SettingsUIButtonGroup("ResetGroup")]
public bool ResetSettings { set { ... } }

[SettingsUIButton]
[SettingsUIButtonGroup("ResetGroup")]
public bool ResetKeybindings { set { ... } }
```

### Visibility Attributes

#### `[SettingsUIHidden]`

**Target**: Class, Struct, Property, Field | **Purpose**: Completely excludes from Options UI

#### `[SettingsUIAdvanced]`

**Target**: Class, Struct, Property | **Purpose**: Only shown in advanced mode

#### `[SettingsUIDeveloper]`

**Target**: Class, Struct, Property | **Purpose**: Only shown in developer mode

#### `[SettingsUISearchHidden]`

**Target**: Class, Struct, Property, Field | **Purpose**: Visible in page but hidden from search

#### `[SettingsUIHideByCondition(Type checkType, string checkMethod, bool invert = false)]`

**Target**: Class or Property | **Purpose**: Dynamically hides based on runtime condition

```csharp
[SettingsUIHideByCondition(typeof(MySettings), nameof(IsFeatureDisabled))]
public int FeatureIntensity { get; set; }

public bool IsFeatureDisabled() => !EnableFeature;
```

The method must be accessible on the settings class instance.

#### `[SettingsUIDisableByCondition(Type checkType, string checkMethod, bool invert = false)]`

**Target**: Class or Property | **Purpose**: Dynamically grays out (disabled but visible)

Same signature as HideByCondition.

### Widget Configuration Attributes

#### `[SettingsUIButton]`

**Target**: Class, Struct, Property | **Purpose**: Makes `bool` property render as a button

For write-only `bool` properties, button behavior is automatic.

#### `[SettingsUIConfirmation(string overrideId = null, string overrideValue = null)]`

**Target**: Property (with `[SettingsUIButton]`) | **Purpose**: Shows confirmation dialog before applying

Localization key: `"Options.WARNING[{path}]"` (or override).

#### `[SettingsUISlider]`

**Target**: Property (int or float) | **Purpose**: Renders as a slider

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `min` | float | 0 | Minimum value |
| `max` | float | 100 | Maximum value |
| `step` | float | 1 | Step increment |
| `unit` | string | "integer" | Unit label ("percentage", "angle", etc.) |
| `scalarMultiplier` | float | 1 | Display multiplier |
| `scaleDragVolume` | bool | false | Scale drag sensitivity |
| `updateOnDragEnd` | bool | false | Only apply when drag ends |

```csharp
[SettingsUISlider(min = 0, max = 100, step = 5, unit = "percentage")]
public int Volume { get; set; } = 50;
```

#### `[SettingsUIDropdown(Type itemsGetterType, string itemsGetterMethod)]`

**Target**: Property (int, string, enum, custom) | **Purpose**: Renders as dropdown

The getter method must return `DropdownItem<T>[]` where T matches the property type.

```csharp
[SettingsUIDropdown(typeof(MySettings), nameof(GetStyleOptions))]
public string SelectedStyle { get; set; }

public DropdownItem<string>[] GetStyleOptions()
{
    return new[]
    {
        new DropdownItem<string> { value = "default", displayName = "Default" },
        new DropdownItem<string> { value = "compact", displayName = "Compact" },
    };
}
```

#### `[SettingsUITextInput]`

**Target**: Property (string, read+write) | **Purpose**: Free-form text input

#### `[SettingsUIMultilineText(string icon = null)]`

**Target**: Property (string, read-only) | **Purpose**: Multiline text block with optional icon

#### `[SettingsUIDirectoryPicker]`

**Target**: Property (string, read+write) | **Purpose**: Directory picker button

### Metadata Attributes

#### `[SettingsUIDisplayName(string overrideId = null, string overrideValue = null)]`

**Target**: Enum, Property | **Purpose**: Override display name localization

Also supports dynamic form: `[SettingsUIDisplayName(typeof(MySettings), "GetDisplayName")]` returning `LocalizedString`.

#### `[SettingsUIDescription(string overrideId = null, string overrideValue = null)]`

**Target**: Enum, Property | **Purpose**: Override tooltip/description localization

#### `[SettingsUIWarning(Type checkType, string checkMethod)]`

**Target**: Property | **Purpose**: Shows warning indicator on a specific option

#### `[SettingsUIPageWarning(Type checkType, string checkMethod)]`

**Target**: Class | **Purpose**: Warning indicator on the entire page

#### `[SettingsUITabWarning(string tab, Type checkType, string checkMethod)]`

**Target**: Class (AllowMultiple) | **Purpose**: Warning indicator on a specific tab

#### `[SettingsUISetter(Type setterType, string setterMethod)]`

**Target**: Property | **Purpose**: Custom callback before the property setter

#### `[SettingsUIValueVersion(Type versionGetterType, string versionGetterMethod)]`

**Target**: Property | **Purpose**: Forces widget to re-read value when version changes

#### `[SettingsUICustomFormat]`

**Target**: Property | **Purpose**: Custom number formatting

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `fractionDigits` | int | 0 | Decimal places |
| `separateThousands` | bool | true | Thousand separators |
| `maxValueWithFraction` | float | 100 | Above this, fractions are hidden |
| `signed` | bool | false | Show +/- sign |

## Keybinding Rebinding UI

### `InputRebindingUISystem` (Game.UI.Menu)

Handles the in-game keybinding capture flow. Mods get this for free when they register `ProxyBinding` properties.

**Rebinding flow**:
1. Player clicks a keybinding widget in Options
2. `InputRebindingUISystem.Start(binding, onSetBinding)` is called
3. System blocks all keyboard/mouse/gamepad input
4. Starts Unity `RebindingOperation` filtering to the binding's device
5. For keyboard: modifier keys (shift, ctrl, alt, caps, windows, meta, numlock) are excluded from main capture -- they're captured by a separate `m_ModifierOperation`
6. On key press: checks for conflicts with existing bindings
7. **No conflict**: immediately applies new binding via `InputManager.SetBinding()`
8. **Conflict detected**: UI shows resolution options
   - "Swap": both bindings exchange keys (`CompleteAndSwapConflicts()`)
   - "Unset": conflicting binding is cleared (`CompleteAndUnsetConflicts()`)
9. `ProxyBinding.Watcher` fires -> property value auto-updates on the `ModSetting` instance
10. Settings are persisted via `AssetDatabase`

**Modifier capture**: A parallel `m_ModifierOperation` runs alongside the main key capture. It only accepts `<Keyboard>/shift`, `<Keyboard>/ctrl`, `<Keyboard>/alt`. Only runs if `binding.allowModifiers && binding.isModifiersRebindable`.

## Localization

### Key Format

For a mod with id `"MyAssembly.MyNamespace.MyMod"` and settings class `"MySettings"`:

| Element | Localization Key |
|---------|-----------------|
| Page title | `Options.SECTION[MyAssembly.MyNamespace.MyMod]` |
| Tab label | `Options.TAB[MyAssembly.MyNamespace.MyMod.General]` |
| Group label | `Options.GROUP[MyAssembly.MyNamespace.MyMod.Behavior]` |
| Option label | `Options.OPTION[MyAssembly.MyNamespace.MyMod.MySettings.PropertyName]` |
| Option tooltip | `Options.OPTION_DESCRIPTION[MyAssembly.MyNamespace.MyMod.MySettings.PropertyName]` |
| Option warning | `Options.WARNING[MyAssembly.MyNamespace.MyMod.MySettings.PropertyName]` |
| Number format | `Options.FORMAT[MyAssembly.MyNamespace.MyMod.MySettings.PropertyName]` |
| Enum value | `Options.MyAssembly.MyNamespace.MyMod.ENUMTYPENAME[EnumValue]` |
| Keybinding label | `Options.OPTION[MyAssembly.MyNamespace.MyMod/ActionName/binding]` |
| Keybinding hint | `Common.ACTION[MyAssembly.MyNamespace.MyMod/ActionName]` |
| Input map label | `Options.INPUT_MAP[MyAssembly.MyNamespace.MyMod]` |

### Loading Localization

```csharp
var sources = new Dictionary<string, string>
{
    { "Options.SECTION[MyAssembly.MyNamespace.MyMod]", "My Mod" },
    { "Options.TAB[MyAssembly.MyNamespace.MyMod.General]", "General" },
    { "Options.OPTION[MyAssembly.MyNamespace.MyMod.MySettings.EnableFeature]", "Enable Feature" },
    { "Options.OPTION_DESCRIPTION[MyAssembly.MyNamespace.MyMod.MySettings.EnableFeature]", "Turns the feature on or off." },
    // Keybinding
    { "Options.OPTION[MyAssembly.MyNamespace.MyMod/ToggleWindow/binding]", "Toggle Window" },
    { "Options.INPUT_MAP[MyAssembly.MyNamespace.MyMod]", "My Mod" },
};

GameManager.instance.localizationManager.AddSource("en-US", new MemorySource(sources));
```

### Helper Methods on `ModSetting`

| Method | Returns | Example Output |
|--------|---------|----------------|
| `GetSettingsLocaleID()` | string | `"Options.SECTION[{id}]"` |
| `GetOptionLabelLocaleID("Prop")` | string | `"Options.OPTION[{id}.{name}.Prop]"` |
| `GetOptionDescLocaleID("Prop")` | string | `"Options.OPTION_DESCRIPTION[{id}.{name}.Prop]"` |
| `GetOptionTabLocaleID("Tab")` | string | `"Options.TAB[{id}.Tab]"` |
| `GetOptionGroupLocaleID("Group")` | string | `"Options.GROUP[{id}.Group]"` |
| `GetEnumValueLocaleID(MyEnum.Foo)` | string | `"Options.{id}.MYENUM[Foo]"` |
| `GetBindingKeyLocaleID("Action")` | string | `"Options.OPTION[{id}/Action/binding]"` |
| `GetBindingMapLocaleID()` | string | `"Options.INPUT_MAP[{id}]"` |

## Mod Blueprint

### Complete Example: Settings Page with Tabs, Toggles, Slider, and Keybinding

```csharp
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

[FileLocation(nameof(MyMod))]
[SettingsUITabOrder("General", "Keybindings")]
[SettingsUIGroupOrder("Behavior", "Display")]
[SettingsUIShowGroupName]
[SettingsUIKeyboardAction("ToggleWindow", ActionType.Button)]
public class MyModSettings : ModSetting
{
    public MyModSettings(IMod mod) : base(mod) { }

    // --- General > Behavior ---
    [SettingsUISection("General", "Behavior")]
    public bool EnableFeature { get; set; } = true;

    [SettingsUISection("General", "Behavior")]
    [SettingsUISlider(min = 0, max = 100, step = 5, unit = "percentage")]
    [SettingsUIDisableByCondition(typeof(MyModSettings), nameof(IsFeatureDisabled))]
    public int Intensity { get; set; } = 50;

    public bool IsFeatureDisabled() => !EnableFeature;

    // --- General > Display ---
    [SettingsUISection("General", "Display")]
    public MyDisplayMode DisplayMode { get; set; } = MyDisplayMode.Normal;

    // --- Keybindings ---
    [SettingsUISection("Keybindings", "Keybindings")]
    [SettingsUIKeyboardBinding(BindingKeyboard.F5, "ToggleWindow", shift: true)]
    public ProxyBinding ToggleWindowBinding { get; set; }

    // --- Reset button (hidden group, no header) ---
    [SettingsUISection("General", "Reset")]
    [SettingsUIButton]
    [SettingsUIConfirmation]
    public bool ResetSettings
    {
        set { SetDefaults(); ApplyAndSave(); }
    }

    public override void SetDefaults()
    {
        EnableFeature = true;
        Intensity = 50;
        DisplayMode = MyDisplayMode.Normal;
    }
}

public enum MyDisplayMode { Normal, Compact, Detailed }
```

### Registration in Mod.cs

```csharp
public class Mod : IMod
{
    internal static MyModSettings Settings { get; private set; }

    public void OnLoad(UpdateSystem updateSystem)
    {
        // 1. Create settings (constructor calls InitializeKeyBindings)
        Settings = new MyModSettings(this);

        // 2. Register in Options UI (page appears)
        Settings.RegisterInOptionsUI();

        // 3. Load saved settings (overrides defaults)
        AssetDatabase.global.LoadSettings(nameof(MyMod), Settings, new MyModSettings(this));

        // 4. Register keybindings with InputManager
        Settings.RegisterKeyBindings();

        // 5. Load localization
        LoadLocalization();

        // 6. Register systems
        updateSystem.UpdateAt<MyHotkeySystem>(SystemUpdatePhase.MainLoop);
    }

    private void LoadLocalization()
    {
        var id = Settings.id;
        var name = Settings.name;
        var sources = new Dictionary<string, string>
        {
            { $"Options.SECTION[{id}]", "My Mod" },
            { $"Options.TAB[{id}.General]", "General" },
            { $"Options.TAB[{id}.Keybindings]", "Keybindings" },
            { $"Options.GROUP[{id}.Behavior]", "Behavior" },
            { $"Options.GROUP[{id}.Display]", "Display" },
            { $"Options.OPTION[{id}.{name}.EnableFeature]", "Enable Feature" },
            { $"Options.OPTION_DESCRIPTION[{id}.{name}.EnableFeature]", "Turns the feature on or off." },
            { $"Options.OPTION[{id}.{name}.Intensity]", "Intensity" },
            { $"Options.OPTION_DESCRIPTION[{id}.{name}.Intensity]", "Controls the feature intensity." },
            { $"Options.OPTION[{id}.{name}.DisplayMode]", "Display Mode" },
            { $"Options.OPTION[{id}.{name}.ResetSettings]", "Reset to Defaults" },
            { $"Options.WARNING[{id}.{name}.ResetSettings]", "Reset all settings to their default values?" },
            { $"Options.{id}.MYDISPLAYMODE[Normal]", "Normal" },
            { $"Options.{id}.MYDISPLAYMODE[Compact]", "Compact" },
            { $"Options.{id}.MYDISPLAYMODE[Detailed]", "Detailed" },
            { $"Options.OPTION[{id}/ToggleWindow/binding]", "Toggle Window" },
            { $"Options.INPUT_MAP[{id}]", "My Mod" },
        };
        GameManager.instance.localizationManager.AddSource("en-US", new MemorySource(sources));
    }

    public void OnDispose() { }
}
```

### Key Points

- **Registration order matters**: `RegisterInOptionsUI()` -> `LoadSettings()` -> `RegisterKeyBindings()`
- **ProxyBinding type triggers keybinding widget** -- no extra widget attribute needed
- **`[SettingsUISection]`** controls tab+group placement; without it, property goes to "General" tab
- **`[SettingsUIShowGroupName]`** is needed to show group headers -- groups still organize without it but no label is shown
- **Page ordering**: built-in pages first, then mod pages by registration order
- **Localization** uses auto-generated keys based on `{id}.{ClassName}.{PropertyName}` pattern
- **Settings are auto-saved** when the player changes them via the UI (no manual save needed)
- **Keybinding rebinding is fully automatic** -- the `InputRebindingUISystem` handles capture, conflict detection, and persistence

## Open Questions

- [ ] How does `Apply()` vs `ApplyAndSave()` differ in practice for mod settings? Is `Apply()` called automatically when the user changes a value in the UI?
- [ ] What is the exact behavior of `[SettingsUIValueVersion]` -- when does the version getter get called to trigger a widget refresh?
- [ ] Can mods add localization for multiple languages, and how does the fallback work when a key is missing?
- [ ] How does `[SettingsUIPlatform]` interact with CS2's platform support? What platforms are available?
- [ ] Is there a way to listen for setting changes from another mod's settings (cross-mod communication)?
- [ ] What is `DropdownItem<T>` -- where is it defined and what fields does it have besides `value` and `displayName`?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
- Companion topic: `research/topics/ModHotkeyInput/` (keybinding registration pipeline)
