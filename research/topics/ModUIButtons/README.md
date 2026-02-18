# Research: Mod UI Buttons & Bindings

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2 mods add custom buttons to the game UI (toolbar, panels) and communicate between C# systems and the TypeScript/React frontend via the Colossal UI binding framework.

**Why**: To add toolbar buttons, custom panels, and bidirectional C#-to-JS communication to mods. This completes our understanding of mod UI integration, building on Topic 9 (ModHotkeyInput) and Topic 10 (ModOptionsUI).

**Boundaries**: Not covering the vanilla Options UI system (see ModOptionsUI) or the Coherent/cohtml rendering engine internals. Focus is on the binding API and module registration pattern.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Colossal.UI.Binding.dll | Colossal.UI.Binding | All binding types: ValueBinding, TriggerBinding, CallBinding, serialization interfaces |
| Game.dll | Game.UI | `UISystemBase` — base class for mod UI systems |
| Game.dll | Game.SceneFlow | `GameManager` — hosts `userInterface.bindings` registry |
| Colossal.UI.dll | Colossal.UI | `UIManager` — manages cohtml views and `coui://` resource hosts |

## Architecture — Two-Sided Bridge

The CS2 UI is a two-layer system bridged by Coherent (cohtml). C# systems push data to and receive events from a TypeScript/React frontend.

```
C# UISystemBase subclass
    |-- AddBinding(ValueBinding<T>)            → pushes data to JS on change
    |-- AddUpdateBinding(GetterValueBinding<T>) → polls getter each frame, pushes if changed
    |-- AddBinding(TriggerBinding)              → receives JS trigger events
    |-- AddBinding(CallBinding<TResult>)        → receives JS calls, returns value
    |
    v  Coherent UI (cohtml) native bridge
    |  - BindingBase.Attach(View) registers with cohtml View
    |  - Events serialized via IJsonWriter / IJsonReader
    |  - Path format: "{group}.{name}" (e.g. "RoadBuilder.GetRoadName")
    |
TypeScript/React frontend
    |-- moduleRegistry.append(slot, Component)  → injects into game UI
    |-- bindValue(group, name, default)          → creates observable binding$
    |-- useValue(binding$)                       → React hook subscribing to C# values
    |-- trigger(group, name, ...args)            → fires C# trigger binding
```

### Binding Path Convention

All bindings use a two-part path: `"{group}.{name}"`. By convention, mods use their mod ID as the group (from `mod.json`). This ensures no collisions with vanilla or other mods.

```csharp
// C# side — group = mod ID, name = binding key
new ValueBinding<bool>("RoadBuilder", "IsPaused", false);
```

```typescript
// TypeScript side — same group and name
const isPaused$ = bindValue<boolean>("RoadBuilder", "IsPaused", false);
```

## Binding Type Map

### C# → JS (Value Bindings)

| Type | Base Class | Interface | Usage |
|------|-----------|-----------|-------|
| `ValueBinding<T>` | `RawEventBindingBase` | — | Push value with `.Update(newValue)`; only sends if value changed (uses `EqualityComparer<T>`) |
| `GetterValueBinding<T>` | `RawEventBindingBase` | `IUpdateBinding` | Auto-polled getter every frame via `UISystemBase.OnUpdate()`; only sends if value changed |
| `RawValueBinding` | `RawEventBindingBase` | `IUpdateBinding` | Untyped — takes `Action<IJsonWriter>` delegate, called every frame |

### JS → C# (Trigger Bindings)

| Type | Base Class | Usage |
|------|-----------|-------|
| `TriggerBinding` | `BindingBase` | No-arg callback — `Action` |
| `TriggerBinding<T>` | `RawTriggerBindingBase` | 1-arg callback — `Action<T>` |
| `TriggerBinding<T1,T2>` | `RawTriggerBindingBase` | 2-arg callback — `Action<T1,T2>` |
| `TriggerBinding<T1,T2,T3>` | `RawTriggerBindingBase` | 3-arg callback — `Action<T1,T2,T3>` |
| `TriggerBinding<T1,T2,T3,T4>` | `RawTriggerBindingBase` | 4-arg callback — `Action<T1,T2,T3,T4>` |

### JS → C# → JS (Call Bindings)

| Type | Base Class | Usage |
|------|-----------|-------|
| `CallBinding<TResult>` | `RawCallBindingBase<TResult>` | No-arg call, returns `TResult` |
| `CallBinding<T1,TResult>` | `RawCallBindingBase<TResult>` | 1-arg call, returns `TResult` |
| Up to `CallBinding<T1..T5,TResult>` | `RawCallBindingBase<TResult>` | Up to 5-arg call with return value |

### Inheritance Hierarchy

```
IBinding                          ← Attach/Detach to cohtml View
├── BindingBase (abstract)        ← group, name, path properties
│   ├── EventBindingBase          ← subscribe/unsubscribe observer tracking
│   │   └── RawEventBindingBase   ← owns JsonWriter for sending events
│   │       ├── ValueBinding<T>
│   │       ├── GetterValueBinding<T>  (also IUpdateBinding)
│   │       └── RawValueBinding        (also IUpdateBinding)
│   ├── RawTriggerBindingBase     ← owns JsonReader for receiving events
│   │   ├── TriggerBinding<T>
│   │   ├── TriggerBinding<T1,T2>
│   │   ├── TriggerBinding<T1,T2,T3>
│   │   ├── TriggerBinding<T1,T2,T3,T4>
│   │   └── RawTriggerBinding     ← raw Action<IJsonReader> callback
│   ├── TriggerBinding            ← no-arg (directly on BindingBase)
│   └── RawCallBindingBase<TResult>  ← owns JsonReader, uses View.BindCall
│       ├── CallBinding<TResult>
│       ├── CallBinding<T1,TResult>
│       └── ... (up to 5 args)

IUpdateBinding : IBinding         ← bool Update() called each frame
```

## UISystemBase (Game.UI)

Base class for all mod UI systems. Extends `GameSystemBase` (ECS system).

### Key Members

| Member | Type | Description |
|--------|------|-------------|
| `m_Bindings` | `List<IBinding>` | All registered bindings |
| `m_UpdateBindings` | `List<IUpdateBinding>` | Bindings polled each frame |
| `gameMode` | `virtual GameMode` | Controls which modes the system is active in (default: `GameMode.All`) |

### Lifecycle

```
OnCreate()
    |-- Initializes m_Bindings and m_UpdateBindings lists
    |-- Mod subclass creates and registers bindings here via AddBinding/AddUpdateBinding
    |
OnGamePreload(purpose, mode)
    |-- Sets Enabled = (gameMode & mode) != 0
    |-- System auto-disables in modes it doesn't support
    |
OnUpdate()  [called every frame when Enabled]
    |-- Iterates m_UpdateBindings and calls .Update() on each
    |-- GetterValueBinding/RawValueBinding poll their data sources here
    |
OnDestroy()
    |-- Removes all bindings from GameManager.instance.userInterface.bindings
    |-- Bindings auto-cleaned
```

### Registration Methods

```csharp
// Register a binding (ValueBinding, TriggerBinding, etc.)
protected void AddBinding(IBinding binding);

// Register + add to frame-update poll list (GetterValueBinding, RawValueBinding)
protected void AddUpdateBinding(IUpdateBinding binding);
```

Both methods register the binding globally via `GameManager.instance.userInterface.bindings.AddBinding(binding)`.

*Source: `Game.dll` → `Game.UI.UISystemBase`*

## TypeScript Module System

### Packages

| Package | Key Exports | Description |
|---------|------------|-------------|
| `cs2/modding` | `ModRegistrar` type | Type for the default export of `index.tsx` |
| `cs2/api` | `bindValue(group, name, default)`, `useValue(binding$)`, `trigger(group, name, ...args)` | Binding subscription and trigger functions |
| `cs2/ui` | `Button`, `Tooltip`, `Panel`, etc. | Game-style React components |
| `cs2/bindings` | `tool` and other vanilla binding observables | Access to vanilla game state |

### Module Registration (index.tsx)

Every mod with UI exports a `ModRegistrar` function as the default export from `index.tsx`:

```typescript
import { ModRegistrar } from "cs2/modding";

const register: ModRegistrar = (moduleRegistry) => {
    // Inject components into game UI slots
    moduleRegistry.append("GameTopLeft", MyToolbarButton);
    moduleRegistry.append("Game", MyGamePanel);
    moduleRegistry.append("Editor", MyEditorPanel);

    // Extend/wrap vanilla components
    moduleRegistry.extend(
        "game-ui/game/components/right-menu/right-menu.tsx",
        "RightMenu",
        WrapperComponent
    );
};

export default register;
```

### Injection Slots (moduleRegistry)

| Slot Name | Location | Method | Usage |
|-----------|----------|--------|-------|
| `"GameTopLeft"` | Top-left toolbar area | `append` | Toolbar buttons alongside vanilla tools |
| `"Game"` | Game view root | `append` | Full panels/overlays during gameplay |
| `"Editor"` | Editor view root | `append` | Panels during map/asset editor |
| `"GameBottomRight"` | Bottom-right area (near chirper) | `append` | Floating buttons/indicators |
| Vanilla component paths | (see examples below) | `extend` | Wrap/replace vanilla components |

- `append(slot, Component)` — adds a React component to an injection slot
- `extend(path, exportName, WrapperComponent)` — wraps a vanilla component with a higher-order component. The wrapper receives the original component as a prop and can render it with modifications.

**Common `extend()` targets** (from Anarchy mod):

| Path | Export Name | Purpose |
|------|-------------|---------|
| `"game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx"` | `'MouseToolOptions'` | Add custom sections to tool options panel |
| `"game-ui/game/components/tool-options/gamepad-tool-options/gamepad-tool-options.tsx"` | `'GamepadToolOptions'` | Gamepad variant of tool options |
| `"game-ui/game/components/tool-options/tool-options-panel.tsx"` | `'useToolOptionsVisible'` | Control tool options panel visibility |
| `"game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx"` | `'selectedInfoSectionComponents'` | Add sections to entity info panel |

### TypeScript Binding Usage

```typescript
import { bindValue, trigger, useValue } from "cs2/api";
import mod from "mod.json";  // { id: "MyMod", ... }

// Create observable binding (matches C# ValueBinding)
const isActive$ = bindValue<boolean>(mod.id, "IsActive", false);

// React hook — subscribes to C# value updates
function MyComponent() {
    const isActive = useValue(isActive$);
    return <div>{isActive ? "Active" : "Inactive"}</div>;
}

// Trigger — calls C# TriggerBinding (no-arg)
const toggleTool = trigger.bind(null, mod.id, "ToggleTool");

// Trigger with args — calls C# TriggerBinding<T>
const setName = (name: string) => trigger(mod.id, "SetName", name);

// Trigger with multiple args
const optionClicked = (id: number, value: number) =>
    trigger(mod.id, "OptionClicked", id, value);
```

## Serialization

### Built-in Writers (C# → JS)

Registered in `ValueWriters` static constructor:
- Primitives: `bool`, `int`, `uint`, `float`, `double` — direct `IJsonWriter.Write()` calls
- `string` — via `StringWriter` (handles null)
- `long`, `ulong` — via `LongWriter`/`ULongWriter`
- Unity math types — via `MathematicsWriters` / `UnityWriters`

### Built-in Readers (JS → C#)

Registered in `ValueReaders` static constructor:
- Primitives: `bool`, `int`, `uint`, `long`, `ulong`, `float`, `double`
- `string` — via `StringReader` (nullable)

### Auto-resolved Types

`ValueWriters.Create<T>()` and `ValueReaders.Create<T>()` auto-resolve in this order:

1. Check registered writers/readers dictionary
2. If type implements `IJsonWritable` / `IJsonReadable` → use `ValueWriter<T>` / `ValueReader<T>`
3. If array → use `ArrayWriter<T>` / `ArrayReader<T>`
4. If `IList<T>` → use `ListWriter<T>` / `ListReader<T>`
5. If `IDictionary<K,V>` → use `DictionaryWriter<K,V>` / `DictionaryReader<K,V>`
6. Otherwise → throws `ArgumentException`

### Custom Type Serialization

**Option A**: Implement `IJsonWritable` and/or `IJsonReadable`:

```csharp
public struct MyData : IJsonWritable, IJsonReadable
{
    public int Id;
    public string Name;

    public void Write(IJsonWriter writer)
    {
        writer.TypeBegin("MyData");
        writer.PropertyName("Id");
        writer.Write(Id);
        writer.PropertyName("Name");
        writer.Write(Name);
        writer.TypeEnd();
    }

    public void Read(IJsonReader reader)
    {
        reader.ReadMapBegin();
        if (reader.ReadProperty("Id")) reader.Read(out int id);
        if (reader.ReadProperty("Name")) reader.Read(out string name);
        reader.ReadMapEnd();
        Id = id;
        Name = name;
    }
}
```

**Option B**: Register a custom writer/reader:

```csharp
ValueWriters.Register<MyType>((IJsonWriter writer, MyType value) => {
    writer.Write(value.ToString());
});
```

### Enum Serialization

Enums are serialized as integers via `EnumWriter<T>`:
```csharp
writer.Write((int)(object)value);  // Cast enum to int
```

### Reflection-Based Generic Serialization (Community Pattern)

`GenericUIWriter<T>` and `GenericUIReader<T>` (from RoadBuilder / Yenyang's `ExtendedUISystemBase`) use reflection to auto-serialize any C# object for UI bindings. Instead of implementing `IJsonWritable` on every data type, these classes iterate public properties and fields at runtime.

**GenericUIWriter\<T\>** implements `IWriter<T>` and serializes objects to JSON:

```csharp
/// <summary>
/// Reflection-based writer that serializes any object's public properties
/// and fields to JSON for ValueBinding consumption by the React UI.
/// </summary>
public class GenericUIWriter<T> : IWriter<T>
{
    public void Write(IJsonWriter writer, T value)
    {
        if (value == null) { writer.WriteNull(); return; }

        writer.TypeBegin(typeof(T).Name);

        // Iterate public properties
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;
            writer.PropertyName(prop.Name);
            WriteValue(writer, prop.GetValue(value), prop.PropertyType);
        }

        // Iterate public fields
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            writer.PropertyName(field.Name);
            WriteValue(writer, field.GetValue(value), field.FieldType);
        }

        writer.TypeEnd();
    }

    // WriteValue dispatches to the appropriate IJsonWriter.Write overload
    // based on runtime type (string, int, float, bool, nested object, etc.)
}
```

**GenericUIReader\<T\>** implements `IReader<T>` and deserializes JSON back to C# objects using the same reflection approach.

**ValueBindingHelper\<T\>** wraps a `ValueBinding<T>` with dirty-tracking to defer updates:

```csharp
/// <summary>
/// Wraps a ValueBinding with dirty-checking. Call Value setter to mark dirty,
/// then ForceUpdate() during OnUpdate() to batch-send changes per frame.
/// </summary>
public class ValueBindingHelper<T>
{
    public ValueBinding<T> Binding { get; }
    private T _pendingValue;
    private bool _dirty;

    public ValueBindingHelper(ValueBinding<T> binding)
    {
        Binding = binding;
        _pendingValue = binding.value;
    }

    /// <summary>
    /// Set a new value. Does not immediately push to JS — marks as dirty.
    /// </summary>
    public T Value
    {
        get => _pendingValue;
        set { _pendingValue = value; _dirty = true; }
    }

    /// <summary>
    /// Call during OnUpdate() to push the value to JS if dirty.
    /// </summary>
    public void ForceUpdate()
    {
        if (_dirty)
        {
            Binding.Update(_pendingValue);
            _dirty = false;
        }
    }
}
```

**Usage with ExtendedUISystemBase:**

```csharp
// CreateBinding<T> combines GenericUIWriter with ValueBindingHelper
// to provide auto-serialization + dirty-tracking in one call.
public ValueBindingHelper<T> CreateBinding<T>(string key, T initialValue)
{
    var helper = new ValueBindingHelper<T>(
        new ValueBinding<T>(Mod.modName, key, initialValue, new GenericUIWriter<T>()));
    AddBinding(helper.Binding);
    _updateCallbacks.Add(helper.ForceUpdate);
    return helper;
}

// In OnUpdate(), all helpers are force-updated:
protected override void OnUpdate()
{
    foreach (var callback in _updateCallbacks)
        callback();
    base.OnUpdate();
}
```

**Trade-offs:**
- Avoids boilerplate `IJsonWritable` implementations on every data type
- Runtime reflection cost per serialization — acceptable for infrequent UI updates, avoid for per-frame high-volume data
- Type names in JSON output may differ from what hand-written serialization would produce
- Does not handle circular references

See `snippets/RoadBuilder_ExtendedUISystemBase.cs` for the full implementation.

### Note: UI Serialization vs ECS Save/Load Serialization

CS2 has **two separate serialization systems** — don't confuse them:

| System | Namespace | Interfaces | Purpose |
|--------|-----------|------------|---------|
| **UI Binding** | `Colossal.UI.Binding` | `IJsonWritable`, `IJsonReadable` | C# ↔ JS data transfer for UI bindings |
| **ECS Persistence** | `Colossal.Serialization.Entities` | `ISerializable` (with `IWriter`/`IReader`) | Save/load game state for ECS components |

The UI binding interfaces (documented above) use `IJsonWriter.Write()` / `IJsonReader.Read()` to serialize data to/from the cohtml frontend. The ECS persistence interfaces use generic `Serialize<TWriter>(TWriter writer) where TWriter : IWriter` for binary save/load. Recolor uses ECS persistence on its custom `AssignedPalette` and `Swatch` components to persist color data across save/load cycles. ECS persistence is automatic for components implementing `ISerializable` — the game's serialization system handles them during save/load.

## Build Setup

### mod.json

Every mod with UI must include a `mod.json` declaring the module:

```json
{
    "id": "MyModName",
    "author": "AuthorName",
    "version": "1.0.0",
    "dependencies": []
}
```

The `id` field is used as the binding group name and module identifier.

### TypeScript Build Pipeline

- **Toolchain**: webpack + TypeScript + SCSS
- **Entry point**: `UI/src/index.tsx` → exports `ModRegistrar` as default
- **Output**: `UI/build/index.js` (bundled)
- **Key dependencies**: `react`, `react-dom`, `classnames`
- **Dev dependencies**: `webpack`, `typescript`, `ts-loader`, `sass-loader`, `css-loader`
- **Commands**: `npm run build` (webpack), `npm run dev` (webpack --watch)
- **Scaffolding**: `npx create-csii-ui-mod` for initial setup

### Auto-Generating TypeScript Types with Reinforced.Typings

The [Reinforced.Typings](https://github.com/nicknisi/Reinforced.Typings) NuGet package auto-generates TypeScript type definitions (`.d.ts` files) from C# types. This keeps the TypeScript side in sync with C# data models passed via ValueBinding, eliminating manual type definition maintenance.

**Add to .csproj:**

```xml
<PackageReference Include="Reinforced.Typings" Version="1.6.5" PrivateAssets="true" />
```

The `PrivateAssets="true"` attribute prevents the package from being included in the mod's published output — it is a build-time-only tool.

**Configuration (ReinforcedTypings.xml or fluent API):**

```csharp
using Reinforced.Typings.Fluent;

public static class TypeScriptExportConfig
{
    public static void Configure(ConfigurationBuilder builder)
    {
        // Export C# types as TypeScript interfaces
        builder.ExportAsInterface<ModPanelData>()
            .WithPublicProperties();

        builder.ExportAsInterface<MySettingsData>()
            .WithPublicProperties()
            .WithPublicFields();

        // Export enums as TypeScript enums
        builder.ExportAsEnum<ProgressState>();
    }
}
```

**Generated output** (placed in `UI/src/domain/` by convention):

```typescript
// Auto-generated from C# ModPanelData
export interface ModPanelData {
    title: string;
    progress: number;
    isComplete: boolean;
}
```

This pairs naturally with `GenericUIWriter<T>` — the writer serializes the C# object, and the generated TypeScript interface describes the shape the React code receives.

### Directory Structure

```
ModName/
├── UI/
│   ├── mod.json              ← Module declaration (id, author, version)
│   ├── package.json          ← npm dependencies
│   ├── tsconfig.json         ← TypeScript config
│   ├── webpack.config.js     ← Webpack bundler config
│   ├── types/                ← Custom type declarations for cs2/* packages
│   ├── src/
│   │   ├── index.tsx         ← ModRegistrar entry point
│   │   ├── mods/
│   │   │   ├── bindings.ts   ← All bindValue/trigger declarations
│   │   │   ├── Components/   ← React components
│   │   │   └── ...
│   │   └── domain/           ← TypeScript type definitions matching C# types
│   └── build/                ← Webpack output (not committed)
```

### Game Loading

The game loads mod UI modules from the mod's published directory. The `mod.json` `id` is used to identify the module. The game calls the `ModRegistrar` function, passing the `moduleRegistry` for component injection.

### Custom Icon Hosting via coui:// Protocol

CS2's UI uses the `coui://` protocol to load resources (images, icons, stylesheets) from registered host locations. The game registers its own hosts (e.g., `coui://GameUI/` for vanilla UI resources). Mods can register custom hosts via `UIManager.AddHostLocation()` to serve their own icons and assets to the TypeScript/React frontend.

#### `UIManager` (Colossal.UI)

The `UIManager` class manages cohtml views and resource host registrations. It is accessed via the `defaultUISystem` singleton property.

| Member | Type | Description |
|--------|------|-------------|
| `defaultUISystem` | `UIManager` (static) | Singleton accessor for the default UI system |
| `AddHostLocation(name, path, readOnly)` | `void` | Register a `coui://` resource host mapping a hostname to a directory |
| `RemoveHostLocation(name)` | `void` | Unregister a previously registered `coui://` host |
| `view` | `UIView` | The active cohtml `View` wrapper |

**Access patterns:**
- Via `UIManager.defaultUISystem` (simplest): `UIManager.defaultUISystem.AddHostLocation(...)`
- Via `GameManager`: `GameManager.instance.userInterface.view.uiSystem.AddHostLocation(...)`

Both paths reach the same `UIManager` instance. Use `defaultUISystem` for brevity; use the `GameManager` path when you already have a reference to the game manager.

**Registration in C# (during OnLoad or OnCreate):**

```csharp
using Game.SceneFlow;

public void OnLoad(UpdateSystem updateSystem)
{
    // Register a custom coui:// host that maps to a directory on disk.
    // After this, coui://mymod/icons/tool.svg resolves to
    // {modDirectory}/Resources/icons/tool.svg
    string modDirectory = GetModDirectory(); // via TryGetExecutableAsset or SearchFilter

    // Option A: via defaultUISystem singleton
    UIManager.defaultUISystem.AddHostLocation(
        "mymod",                                          // host name
        Path.Combine(modDirectory, "Resources"),          // directory on disk
        false);                                           // not read-only

    // Option B: via GameManager (equivalent)
    GameManager.instance.userInterface.view.uiSystem.AddHostLocation(
        "mymod",
        Path.Combine(modDirectory, "Resources"),
        false);
}

public void OnDispose()
{
    // Clean up host registration when the mod is unloaded.
    UIManager.defaultUISystem.RemoveHostLocation("mymod");
}
```

**Usage in TypeScript:**

```typescript
// Reference the icon via coui:// protocol in JSX
<img src="coui://mymod/icons/tool.svg" />

// Or in CSS mask-image for themed icons
<img style={{ maskImage: "url(coui://mymod/icons/tool.svg)" }} />

// Or via the Button src prop
<Button variant="floating" src="coui://mymod/icons/tool.svg" />
```

**Key details:**
- The host name in `AddHostLocation()` becomes the hostname in the `coui://` URL. E.g., host `"mymod"` serves resources at `coui://mymod/...`.
- The directory path maps to the root of that host. Subdirectories are accessible via the URL path.
- The vanilla game uses hosts like `GameUI` (for `coui://GameUI/...`) and `uil` (for the standard UI library). Avoid using these names.
- Call `RemoveHostLocation(name)` during `OnDispose()` to clean up when the mod is unloaded. Failing to remove hosts leaves orphaned entries in the cohtml resource handler.
- Icons served this way work with the CSS mask-image pattern for theme-aware coloring: set the `<img>` background-color to the desired theme variable and use the SVG as a mask.

*Source: `Colossal.UI.dll` -> `Colossal.UI.UIManager`*

## Mod Blueprint — Adding a Toolbar Button

### C# Side

**1. Create a UISystemBase subclass:**

```csharp
using Colossal.UI.Binding;
using Game.UI;

public partial class MyModUISystem : UISystemBase
{
    private ValueBinding<bool> _isActive;

    protected override void OnCreate()
    {
        base.OnCreate();

        // Push state to JS
        _isActive = new ValueBinding<bool>("MyMod", "IsActive", false);
        AddBinding(_isActive);

        // Receive JS events
        AddBinding(new TriggerBinding("MyMod", "ToggleTool", OnToggleTool));
        AddBinding(new TriggerBinding<string>("MyMod", "SetName", OnSetName));
    }

    private void OnToggleTool()
    {
        _isActive.Update(!_isActive.value);
    }

    private void OnSetName(string name)
    {
        // Handle name change
    }
}
```

**2. Register system in Mod.OnLoad():**

```csharp
public void OnLoad(UpdateSystem updateSystem)
{
    updateSystem.UpdateAt<MyModUISystem>(SystemUpdatePhase.UIUpdate);
}
```

### TypeScript Side

**1. index.tsx — Module registration:**

```typescript
import { ModRegistrar } from "cs2/modding";
import MyToolbarButton from "mods/Components/MyToolbarButton";
import MyPanel from "mods/MyPanel";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopLeft", MyToolbarButton);
    moduleRegistry.append("Game", MyPanel);
};

export default register;
```

**2. bindings.ts — Binding declarations:**

```typescript
import { bindValue, trigger } from "cs2/api";
import mod from "mod.json";

export const isActive$ = bindValue<boolean>(mod.id, "IsActive", false);
export const toggleTool = trigger.bind(null, mod.id, "ToggleTool");
export const setName = (name: string) => trigger(mod.id, "SetName", name);
```

**3. MyToolbarButton.tsx — Button component:**

```typescript
import { Button, Tooltip } from "cs2/ui";
import { useValue } from "cs2/api";
import { isActive$, toggleTool } from "mods/bindings";
import styles from "./MyToolbarButton.module.scss";

export default () => {
    const isActive = useValue(isActive$);
    return (
        <Tooltip tooltip="My Mod">
            <Button
                variant="floating"
                className={isActive ? styles.selected : styles.toggle}
                onSelect={toggleTool}
            >
                <img style={{ maskImage: `url(${icon})` }} />
            </Button>
        </Tooltip>
    );
};
```

**4. MyToolbarButton.module.scss — Styling:**

```scss
.toggle {
    background-color: var(--accentColorNormal);
    &:hover { background-color: var(--accentColorNormal-hover); }
    &:active { background-color: var(--accentColorNormal-pressed); }
    &.selected { background-color: white; }
    img {
        width: 100%; height: 100%;
        background-color: white;
        mask-size: contain;
        mask-position: 50% 50%;
    }
}
```

### Key CSS Variables (Game Theme)

| Variable | Description |
|----------|-------------|
| `--accentColorNormal` | Primary accent color |
| `--accentColorNormal-hover` | Hover state |
| `--accentColorNormal-pressed` | Pressed/active state |
| `--panelColorNormal` | Panel background |
| `--textColorNormal` | Default text color |

## Data Flow

### C# → JS (Value Push)

```
C# code calls ValueBinding<T>.Update(newValue)
    |-- EqualityComparer<T> checks if value changed
    |-- If changed: TriggerUpdate()
        |-- jsonWriter.BeginEvent("{group}.{name}.update", 1)
        |-- IWriter<T>.Write(jsonWriter, value)
        |-- jsonWriter.EndEvent()
        |-- cohtml native bridge delivers event to JS
            |-- JS binding$ observable fires
            |-- useValue() hook re-renders React component
```

### C# → JS (Getter Poll)

```
UISystemBase.OnUpdate() [every frame]
    |-- Iterates m_UpdateBindings
    |-- GetterValueBinding<T>.Update()
        |-- Calls m_Getter() to get current value
        |-- EqualityComparer<T> checks if changed from last value
        |-- If changed: TriggerUpdateImpl()
            |-- Same event path as ValueBinding
```

### JS → C# (Trigger)

```
TypeScript calls trigger(group, name, arg1, arg2, ...)
    |-- cohtml fires event "{group}.{name}"
    |-- RawTriggerBindingBase.BaseCallback()
        |-- If active: calls abstract Callback()
            |-- TriggerBinding<T1,T2>.Callback()
                |-- IReader<T1>.Read(jsonReader, out value1)
                |-- IReader<T2>.Read(jsonReader, out value2)
                |-- m_Callback(value1, value2)
        |-- If inactive: skips all args via SkipValue()
```

### JS → C# → JS (Call)

```
TypeScript calls a bound function
    |-- cohtml calls View.BindCall handler
    |-- RawCallBindingBase<TResult>.Callback()
        |-- Reads args via IReader<T>.Read()
        |-- Calls m_Callback(args...) → returns TResult
        |-- Return value automatically serialized back to JS by cohtml
```

## Observer Pattern

Value bindings use a subscribe/unsubscribe observer pattern managed by `EventBindingBase`:

1. When JS calls `useValue(binding$)`, the `{path}.subscribe` event fires
2. `EventBindingBase.OnSubscribe()` increments `observerCount` and pushes current value
3. When React component unmounts, `{path}.unsubscribe` fires
4. `EventBindingBase.OnUnsubscribe()` decrements `observerCount`
5. `active` property returns `observerCount > 0`
6. Value bindings skip sending events when `active` is false (no observers)

This ensures no wasted serialization when no JS component is listening.

## Low-Level cohtml Communication (View.RegisterForEvent / engine.trigger)

The ValueBinding/TriggerBinding framework (documented above) is the recommended approach for C#-to-JS communication. However, the underlying cohtml engine exposes a lower-level event API that some mods use directly. This is the same mechanism the binding framework is built on.

### C# Side: View.RegisterForEvent

Register a handler on the cohtml `View` to receive events fired from JavaScript:

```csharp
using cohtml.Net;

// Get the cohtml View from the UIManager
View view = UIManager.defaultUISystem.view.View;

// Register for a named event — returns a BoundEventHandle for lifecycle management
BoundEventHandle handle = view.RegisterForEvent("MyMod.OnButtonClicked", new Action<string>((data) =>
{
    Mod.Log.Info($"Button clicked with data: {data}");
}));

// To fire an event from C# to JavaScript:
view.TriggerEvent("MyMod.DataReady", someJsonString);
```

### JavaScript Side: engine.trigger / engine.on

In the cohtml JavaScript environment, `engine` is a global object:

```javascript
// Fire an event from JS to C# (calls the RegisterForEvent handler)
engine.trigger("MyMod.OnButtonClicked", "some-data");

// Listen for events from C# (from View.TriggerEvent)
engine.on("MyMod.DataReady", function(data) {
    console.log("Received from C#:", data);
});
```

### BoundEventHandle Lifecycle

`BoundEventHandle` tracks the registration and must be disposed to avoid leaks:

```csharp
private BoundEventHandle _eventHandle;

protected override void OnCreate()
{
    base.OnCreate();
    View view = UIManager.defaultUISystem.view.View;
    _eventHandle = view.RegisterForEvent("MyMod.MyEvent", new Action(OnMyEvent));
}

protected override void OnDestroy()
{
    // Unregister the event handler to prevent memory leaks
    _eventHandle?.Dispose();
    base.OnDestroy();
}
```

**When to use this pattern:**
- When working without the TypeScript build pipeline (no `cs2/api` available)
- When injecting raw HTML/JS via `View.ExecuteScript()` (see below)
- When interfacing with cohtml features not exposed by the binding framework

**When to prefer ValueBinding/TriggerBinding instead:**
- When using the standard TypeScript/React build pipeline
- When you want automatic dirty-checking and observer management
- For all new mods — the binding framework handles serialization and lifecycle automatically

## Raw DOM Injection via View.ExecuteScript()

For mods that need UI without a TypeScript build pipeline, `View.ExecuteScript()` allows injecting raw HTML, CSS, and JavaScript directly into the game's cohtml DOM. This is a lightweight alternative when a full webpack/TypeScript setup is not warranted.

### How It Works

```csharp
View view = UIManager.defaultUISystem.view.View;

// Inject CSS
view.ExecuteScript(@"
    var style = document.createElement('style');
    style.textContent = `
        .my-mod-panel {
            position: absolute;
            top: 100px;
            left: 100px;
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 12px;
            border-radius: 4px;
            z-index: 1000;
        }
    `;
    document.head.appendChild(style);
");

// Inject HTML into an existing game container
view.ExecuteScript(@"
    var panel = document.createElement('div');
    panel.id = 'my-mod-panel';
    panel.className = 'my-mod-panel';
    panel.innerHTML = '<h3>My Mod</h3><button id=""my-mod-btn"">Click Me</button>';
    document.body.appendChild(panel);
");

// Wire up events back to C# via engine.trigger
view.ExecuteScript(@"
    document.getElementById('my-mod-btn').addEventListener('click', function() {
        engine.trigger('MyMod.ButtonClicked');
    });
");
```

### Targeting Game CSS Selectors

The game's React UI renders into predictable DOM structures. Mods can target these with CSS selectors to inject adjacent to game elements:

```csharp
// Find a game UI container and inject next to it
view.ExecuteScript(@"
    var toolbar = document.querySelector('.toolbar_toolbar');
    if (toolbar) {
        var btn = document.createElement('div');
        btn.className = 'my-mod-toolbar-button';
        btn.onclick = function() { engine.trigger('MyMod.ToolbarClicked'); };
        toolbar.appendChild(btn);
    }
");
```

### Embedding Resources in the Assembly

Store HTML/CSS/JS as embedded resources in the C# assembly and load them at runtime:

```csharp
// In .csproj: <EmbeddedResource Include="Resources\panel.html" />

private string LoadEmbeddedResource(string resourceName)
{
    var assembly = Assembly.GetExecutingAssembly();
    string fullName = $"{assembly.GetName().Name}.Resources.{resourceName}";
    using var stream = assembly.GetManifestResourceStream(fullName);
    if (stream == null) return null;
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

// Inject the embedded HTML
string panelHtml = LoadEmbeddedResource("panel.html");
view.ExecuteScript($"document.body.insertAdjacentHTML('beforeend', `{panelHtml}`);");
```

### Cleanup on Dispose

Remove injected DOM elements when the mod is unloaded:

```csharp
protected override void OnDestroy()
{
    View view = UIManager.defaultUISystem.view.View;
    view.ExecuteScript("var el = document.getElementById('my-mod-panel'); if (el) el.remove();");
    base.OnDestroy();
}
```

**Trade-offs:**
- No TypeScript build pipeline required — all UI is plain strings in C#
- No React component lifecycle, no `useValue` hooks, no scoped SCSS
- Fragile — game CSS class names may change between updates
- No dirty-checking or observer pattern — must manually manage data synchronization via `engine.trigger` / `RegisterForEvent`
- Best suited for simple overlays, debug panels, or mods that need minimal UI

## TooltipSystemBase (Game.UI.Tooltip)

`TooltipSystemBase` is the base class for systems that display custom tooltips over game objects (tools, entities, terrain). It integrates with the game's tooltip rendering pipeline at `SystemUpdatePhase.UITooltip`.

### Key Types

| Type | Namespace | Description |
|------|-----------|-------------|
| `TooltipSystemBase` | `Game.UI.Tooltip` | Abstract base class for tooltip systems; extends `UISystemBase` |
| `TooltipGroup` | `Game.UI.Tooltip` | Groups tooltip widgets into a positioned tooltip bubble |
| `IntTooltip` | `Game.UI.Tooltip` | Built-in widget for displaying an integer value with a label and icon |
| `StringTooltip` | `Game.UI.Tooltip` | Built-in widget for displaying a localized string |
| `FloatTooltip` | `Game.UI.Tooltip` | Built-in widget for displaying a float value |

### Lifecycle

Tooltip systems run at `SystemUpdatePhase.UITooltip`, which executes after simulation and UI updates. During `OnUpdate()`, the system checks whether its tooltip should be visible (e.g., based on active tool, hovered entity) and populates tooltip groups with widgets.

```
OnCreate()
    |-- Cache references to other systems (ToolSystem, RaycastSystem, etc.)
    |
OnUpdate()  [called every frame at UITooltip phase]
    |-- Check if tooltip should be shown (tool active, entity hovered, etc.)
    |-- If showing:
    |   |-- Create or reuse TooltipGroup
    |   |-- Set position via WorldToTooltipPos(worldPosition)
    |   |-- Add tooltip widgets (IntTooltip, StringTooltip, etc.)
    |   |-- Call AddGroup(tooltipGroup)
    |
OnDestroy()
    |-- Cleanup
```

### WorldToTooltipPos

Converts a world-space position (e.g., from a raycast hit) to tooltip screen coordinates:

```csharp
// In a TooltipSystemBase subclass:
float3 worldPos = raycastHit.m_Position;
TooltipGroup group = new TooltipGroup();
group.position = WorldToTooltipPos(worldPos);
group.path = "MyMod.ToolTooltip";
```

### Example: Custom Tool Tooltip

```csharp
using Game.UI.Tooltip;
using Unity.Mathematics;

/// <summary>
/// Displays a tooltip with custom data when the mod's tool is active.
/// Register at SystemUpdatePhase.UITooltip in Mod.OnLoad().
/// </summary>
public partial class MyToolTooltipSystem : TooltipSystemBase
{
    private ToolSystem _toolSystem;
    private IntTooltip _countTooltip;
    private StringTooltip _labelTooltip;

    protected override void OnCreate()
    {
        base.OnCreate();
        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
        _countTooltip = new IntTooltip
        {
            path = "MyMod.Count",
            icon = "coui://mymod/icons/counter.svg",
            label = LocalizedString.Id("MyMod.CountLabel")
        };
        _labelTooltip = new StringTooltip
        {
            path = "MyMod.Label"
        };
    }

    protected override void OnUpdate()
    {
        // Only show tooltip when our tool is active
        if (_toolSystem.activeTool != World.GetOrCreateSystemManaged<MyCustomToolSystem>())
            return;

        // Position the tooltip at the cursor's world position
        _countTooltip.value = GetCurrentCount();
        _labelTooltip.value = LocalizedString.Id("MyMod.CurrentLabel");

        TooltipGroup group = new TooltipGroup();
        group.position = WorldToTooltipPos(_toolSystem.raycastPoint);
        group.path = "MyMod.ToolTooltip";
        group.children.Add(_countTooltip);
        group.children.Add(_labelTooltip);

        AddGroup(group);
    }

    private int GetCurrentCount() => 42; // Placeholder
}

// Registration in Mod.OnLoad():
// updateSystem.UpdateAt<MyToolTooltipSystem>(SystemUpdatePhase.UITooltip);
```

## NotificationSystem and MessageDialog (Game.UI)

CS2 provides built-in systems for displaying notification banners and modal dialogs to the player.

### NotificationSystem.Push / Pop

`NotificationSystem` manages in-game notification banners (the pop-up messages that appear at the top of the screen).

```csharp
using Game.UI;

// Get the notification system
var notificationSystem = World.GetOrCreateSystemManaged<NotificationSystem>();

// Push a notification banner
notificationSystem.Push(
    "my-mod-notification",                           // unique identifier
    title: LocalizedString.Id("MyMod.NotifTitle"),   // notification title
    text: LocalizedString.Id("MyMod.NotifText"),     // notification body
    progressState: ProgressState.Complete,            // icon/style variant
    progress: 100                                    // progress percentage (0-100)
);

// Remove the notification when done
notificationSystem.Pop("my-mod-notification");
```

### ProgressState Enum

Controls the visual style of the notification banner:

| Value | Description |
|-------|-------------|
| `ProgressState.None` | No progress indicator |
| `ProgressState.Indeterminate` | Spinning/loading indicator |
| `ProgressState.Progressing` | Shows progress bar at specified percentage |
| `ProgressState.Complete` | Checkmark/success indicator |
| `ProgressState.Failed` | Error/failure indicator |
| `ProgressState.Warning` | Warning indicator |

### MessageDialog

`MessageDialog` displays a modal dialog box that blocks interaction until dismissed. It uses an `onClicked` callback pattern to handle button presses:

```csharp
using Game.UI;
using Game.PSI;

// Show a confirmation dialog with OK/Cancel buttons
MessageDialog.Show(
    LocalizedString.Id("MyMod.ConfirmTitle"),          // dialog title
    LocalizedString.Id("MyMod.ConfirmMessage"),        // dialog body text
    LocalizedString.Id("Common.OK"),                   // primary button label
    LocalizedString.Id("Common.CANCEL"),               // secondary button label
    onClicked: (int buttonIndex) =>
    {
        // buttonIndex 0 = primary button (OK), 1 = secondary button (Cancel)
        if (buttonIndex == 0)
        {
            Mod.Log.Info("User confirmed action");
            ExecuteConfirmedAction();
        }
    }
);

// Show a simple info dialog (single OK button)
MessageDialog.Show(
    LocalizedString.Id("MyMod.InfoTitle"),
    LocalizedString.Id("MyMod.InfoMessage"),
    LocalizedString.Id("Common.OK"),
    onClicked: (_) => { /* dismissed */ }
);
```

**Key details:**
- The `onClicked` callback receives a zero-based button index matching the order buttons were provided.
- `MessageDialog` is modal -- it pauses game input until the player responds.
- Use `NotificationSystem` for non-blocking status updates and `MessageDialog` for actions requiring confirmation.

## Harmony Patch Points

### Candidate 1: `UISystemBase.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Inject custom update logic after all bindings are polled
- **Risk level**: Low — standard frame-update hook
- **Side effects**: Runs every frame; keep patches lightweight

### Candidate 2: `GameManager.userInterface.bindings.AddBinding`

- **Signature**: `void AddBinding(IBinding binding)` (on `IBindingRegistry`)
- **Patch type**: Postfix
- **What it enables**: Intercept or log all binding registrations
- **Risk level**: Medium — affects global binding registry
- **Side effects**: Could interfere with vanilla systems if not careful

Note: In practice, mods rarely need to patch the binding system. The `UISystemBase` API provides sufficient extension points via subclassing and `AddBinding`.

## Examples

### Example 1: Basic UI System with Value and Trigger Bindings

A minimal `UISystemBase` subclass that pushes a boolean state to the JS frontend and receives toggle events back. This is the foundation of every mod UI system.

```csharp
using Colossal.UI.Binding;
using Game.UI;

/// <summary>
/// Minimal mod UI system demonstrating ValueBinding (C# -> JS)
/// and TriggerBinding (JS -> C#).
/// </summary>
public partial class PanelToggleUISystem : UISystemBase
{
    private ValueBinding<bool> _isPanelOpen;
    private ValueBinding<string> _statusText;

    protected override void OnCreate()
    {
        base.OnCreate();

        // ValueBinding pushes state to JS whenever Update() is called.
        // The first arg is the binding group (use your mod ID), the second
        // is the binding name, and the third is the initial value.
        _isPanelOpen = new ValueBinding<bool>("MyMod", "IsPanelOpen", false);
        _statusText = new ValueBinding<string>("MyMod", "StatusText", "Ready");
        AddBinding(_isPanelOpen);
        AddBinding(_statusText);

        // TriggerBinding receives events from JS. The no-arg version
        // takes an Action callback. Typed versions (TriggerBinding<T>)
        // deserialize arguments automatically.
        AddBinding(new TriggerBinding("MyMod", "TogglePanel", OnTogglePanel));
        AddBinding(new TriggerBinding<string>("MyMod", "SetStatus", OnSetStatus));
    }

    private void OnTogglePanel()
    {
        // Update() only sends to JS if the value actually changed
        // (uses EqualityComparer<T> internally).
        _isPanelOpen.Update(!_isPanelOpen.value);
        _statusText.Update(_isPanelOpen.value ? "Panel is open" : "Panel is closed");
    }

    private void OnSetStatus(string newStatus)
    {
        _statusText.Update(newStatus);
    }
}
```

### Example 2: Getter-Based Polling Binding

Use `GetterValueBinding<T>` when you want the UI to reflect a value that changes externally (e.g., from other ECS systems). The binding polls the getter every frame and only pushes to JS when the value changes.

```csharp
using Colossal.UI.Binding;
using Game.UI;

/// <summary>
/// Demonstrates GetterValueBinding for auto-polled values.
/// The getter runs every frame; the binding only sends updates
/// when the return value differs from the previous frame.
/// </summary>
public partial class StatsUISystem : UISystemBase
{
    private int _entityCount;

    protected override void OnCreate()
    {
        base.OnCreate();

        // GetterValueBinding takes a Func<T> that is called every frame.
        // Both AddBinding and AddUpdateBinding work for GetterValueBinding.
        // AddUpdateBinding includes it in the per-frame poll loop (OnUpdate),
        // while AddBinding registers it without automatic polling — the
        // getter is still invoked when the UI requests the value.
        // Most mods (including yenyang's) use AddBinding successfully.
        AddBinding(new GetterValueBinding<int>(
            "MyMod", "EntityCount", () => _entityCount));

        // You can also poll computed values or query other systems.
        AddBinding(new GetterValueBinding<bool>(
            "MyMod", "HasEntities", () => _entityCount > 0));
    }

    protected override void OnUpdate()
    {
        // Update game state before bindings are polled.
        _entityCount = ComputeEntityCount();

        // base.OnUpdate() iterates all IUpdateBinding instances
        // and calls Update() on each, which triggers the getters.
        base.OnUpdate();
    }

    private int ComputeEntityCount()
    {
        // Placeholder — in a real mod, query ECS data here.
        return 42;
    }
}
```

### Example 3: Multi-Arg Trigger Binding

Trigger bindings support up to 4 typed arguments. Each argument is deserialized from JSON automatically using `ValueReaders`.

```csharp
using Colossal.UI.Binding;
using Game.UI;

/// <summary>
/// Demonstrates multi-argument TriggerBindings. The JS side passes
/// arguments via trigger(group, name, arg1, arg2, ...) and they
/// are deserialized to the corresponding C# types.
/// </summary>
public partial class ConfigUISystem : UISystemBase
{
    private ValueBinding<int> _selectedOption;

    protected override void OnCreate()
    {
        base.OnCreate();

        _selectedOption = new ValueBinding<int>("MyMod", "SelectedOption", -1);
        AddBinding(_selectedOption);

        // 2-arg trigger: receives (string category, int optionId)
        AddBinding(new TriggerBinding<string, int>(
            "MyMod", "SelectOption", OnSelectOption));

        // 3-arg trigger: receives (int x, int y, int z)
        AddBinding(new TriggerBinding<int, int, int>(
            "MyMod", "SetPosition", OnSetPosition));
    }

    private void OnSelectOption(string category, int optionId)
    {
        Mod.Log.Info($"Option selected: {category} #{optionId}");
        _selectedOption.Update(optionId);
    }

    private void OnSetPosition(int x, int y, int z)
    {
        Mod.Log.Info($"Position set to ({x}, {y}, {z})");
    }
}
```

### Example 4: Custom Serializable Type with IJsonWritable

When pushing complex objects from C# to JS, implement `IJsonWritable` on your data type. The binding framework auto-detects the interface via `ValueWriters.Create<T>()`.

```csharp
using Colossal.UI.Binding;
using Game.UI;

/// <summary>
/// A custom data type that serializes itself to JSON for the UI.
/// Implement IJsonWritable for C# -> JS, IJsonReadable for JS -> C#.
/// </summary>
public struct ModPanelData : IJsonWritable
{
    public string Title;
    public int Progress;
    public bool IsComplete;

    public void Write(IJsonWriter writer)
    {
        // TypeBegin writes a __Type discriminator property.
        // Use it when JS needs to identify the object type.
        writer.TypeBegin("ModPanelData");
        writer.PropertyName("title");
        writer.Write(Title);
        writer.PropertyName("progress");
        writer.Write(Progress);
        writer.PropertyName("isComplete");
        writer.Write(IsComplete);
        writer.TypeEnd();
    }
}

/// <summary>
/// UI system that pushes a custom struct to the frontend.
/// </summary>
public partial class PanelDataUISystem : UISystemBase
{
    private ValueBinding<ModPanelData> _panelData;

    protected override void OnCreate()
    {
        base.OnCreate();

        // ValueWriters.Create<ModPanelData>() detects IJsonWritable
        // and uses it automatically — no custom IWriter<T> needed.
        _panelData = new ValueBinding<ModPanelData>(
            "MyMod", "PanelData",
            new ModPanelData { Title = "Loading...", Progress = 0, IsComplete = false });
        AddBinding(_panelData);
    }

    public void SetProgress(int progress)
    {
        _panelData.Update(new ModPanelData
        {
            Title = "Working...",
            Progress = progress,
            IsComplete = progress >= 100
        });
    }
}
```

### Example 5: Restricting a UI System to Specific Game Modes

Override the `gameMode` property to control when your UI system is active. The system auto-disables itself during mode transitions via `OnGamePreload`.

```csharp
using Colossal.UI.Binding;
using Game.UI;

/// <summary>
/// UI system that only runs during gameplay (not in the editor or main menu).
/// The gameMode property is checked on every mode transition via OnGamePreload.
/// </summary>
public partial class GameplayOnlyUISystem : UISystemBase
{
    // Only active during gameplay — the system is disabled (and bindings
    // stop polling) in the editor, main menu, and other modes.
    public override GameMode gameMode => GameMode.Game;

    protected override void OnCreate()
    {
        base.OnCreate();

        AddUpdateBinding(new GetterValueBinding<float>(
            "MyMod", "GameTime", () => UnityEngine.Time.time));

        AddBinding(new TriggerBinding("MyMod", "PauseAction", OnPauseAction));
    }

    private void OnPauseAction()
    {
        Mod.Log.Info("Pause action triggered during gameplay");
    }
}
```

### Example 6: Registering the UI System in Mod.OnLoad

All UI systems must be registered with the update system during `IMod.OnLoad`. Use `SystemUpdatePhase.UIUpdate` so bindings are polled at the correct point in the frame.

```csharp
using Game;
using Game.Modding;

/// <summary>
/// Mod entry point. Registers all UI systems with the update loop.
/// </summary>
public class Mod : IMod
{
    internal static Colossal.Logging.ILog Log { get; }
        = Colossal.Logging.LogManager.GetLogger(nameof(Mod)).SetShowsErrorsInUI(true);

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info("Mod loaded");

        // Register UI systems at the UIUpdate phase.
        // This ensures OnUpdate() runs after game simulation
        // but before the frame is rendered.
        updateSystem.UpdateAt<PanelToggleUISystem>(SystemUpdatePhase.UIUpdate);
        updateSystem.UpdateAt<StatsUISystem>(SystemUpdatePhase.UIUpdate);
    }

    public void OnDispose()
    {
        Log.Info("Mod disposed");
        // UISystemBase.OnDestroy() auto-removes bindings from the registry.
    }
}
```

### Example 7: TypeScript Bindings and React Component (Full Round-Trip)

The TypeScript side mirrors the C# bindings. Each `bindValue` call creates an observable that subscribes to a C# `ValueBinding`, and `trigger` calls invoke C# `TriggerBinding` callbacks.

```typescript
// --- bindings.ts ---
// Centralize all binding declarations in one file.
// Components import from here rather than calling bindValue/trigger directly.

import { bindValue, trigger } from "cs2/api";
import mod from "mod.json"; // { id: "MyMod" }

// Value bindings (C# -> JS): the group and name must match the C# side exactly.
// The third argument is the default value used before C# pushes data.
export const isPanelOpen$ = bindValue<boolean>(mod.id, "IsPanelOpen", false);
export const statusText$ = bindValue<string>(mod.id, "StatusText", "Ready");
export const entityCount$ = bindValue<number>(mod.id, "EntityCount", 0);

// No-arg trigger: use .bind() to create a clean callable function.
export const togglePanel = trigger.bind(null, mod.id, "TogglePanel");

// Typed trigger: wrap in an arrow function to pass arguments.
export const setStatus = (status: string) => trigger(mod.id, "SetStatus", status);

// Multi-arg trigger: all args are serialized and sent to C#.
export const selectOption = (category: string, optionId: number) =>
    trigger(mod.id, "SelectOption", category, optionId);
```

```typescript
// --- MyPanel.tsx ---
// React component that reads C# state and triggers C# callbacks.

import { useValue } from "cs2/api";
import { Button, Panel } from "cs2/ui";
import { isPanelOpen$, statusText$, entityCount$, togglePanel, setStatus } from "mods/bindings";

export const MyPanel = () => {
    // useValue subscribes to the C# ValueBinding observable.
    // The component re-renders automatically when C# calls binding.Update().
    const isOpen = useValue(isPanelOpen$);
    const status = useValue(statusText$);
    const count = useValue(entityCount$);

    if (!isOpen) return null;

    return (
        <Panel>
            <h2>My Mod Panel</h2>
            <p>Status: {status}</p>
            <p>Entities: {count}</p>
            <Button onSelect={() => setStatus("User clicked!")}>
                Update Status
            </Button>
            <Button onSelect={togglePanel}>
                Close Panel
            </Button>
        </Panel>
    );
};
```

```typescript
// --- index.tsx ---
// Entry point: register components into the game UI via moduleRegistry.

import { ModRegistrar } from "cs2/modding";
import { MyPanel } from "mods/MyPanel";
import { MyToolbarButton } from "mods/MyToolbarButton";

const register: ModRegistrar = (moduleRegistry) => {
    // "GameTopLeft" slot places the button in the top-left toolbar
    // alongside vanilla tool buttons (roads, zones, etc.).
    moduleRegistry.append("GameTopLeft", MyToolbarButton);

    // "Game" slot injects a component at the gameplay view root.
    // The component controls its own visibility (returns null when hidden).
    moduleRegistry.append("Game", MyPanel);
};

export default register;
```

## Community Patterns (from InfoLoom)

The following patterns were discovered by analyzing [InfoLoom](https://github.com/bruceyboy24804/InfoLoom), a large data-display mod with a toolbar button, multi-level flyout menu, and draggable panels.

### Same Binding Key for ValueBinding and TriggerBinding

A `ValueBinding` and `TriggerBinding` can share the same group+name key. They're registered in different internal registries and don't collide. This simplifies toggle state management:

```csharp
// C# — same key "InfoLoomMenuOpen" for both
_panelVisibleBinding = new ValueBinding<bool>(ModID, "InfoLoomMenuOpen", false);
AddBinding(_panelVisibleBinding);
AddBinding(new TriggerBinding<bool>(ModID, "InfoLoomMenuOpen", SetVisibility));

private void SetVisibility(bool open) => _panelVisibleBinding.Update(open);
```

```typescript
// TypeScript — same key for both subscribe and trigger
export const menuOpen$ = bindValue<boolean>(mod.id, "InfoLoomMenuOpen", false);
export const setMenuOpen = (open: boolean) => trigger(mod.id, "InfoLoomMenuOpen", open);
```

### Visibility-Gated Data Updates

When a mod has multiple data panels, only fetch and push ECS data when the panel is actually visible. This avoids unnecessary queries and serialization every frame:

```csharp
protected override void OnUpdate()
{
    base.OnUpdate();

    // Only query ECS and update bindings when the user has this panel open
    if (_demographicsPanelVisible.value)
    {
        var data = QueryDemographicsData();
        _demographicsBinding.Update(data);
    }
    if (_workforcesPanelVisible.value)
    {
        var data = QueryWorkforcesData();
        _workforcesBinding.Update(data);
    }
}
```

### Custom Flyout Menu (Dropdown from Toolbar Button)

InfoLoom implements a multi-level menu as a conditionally-rendered absolutely-positioned `<div>` below the toolbar button. This is the standard pattern — CS2 has no native dropdown widget for toolbar menus.

```typescript
// InfoLoomMenu.tsx — button + conditional flyout
<Tooltip tooltip="Info Loom">
    <Button variant="floating" src={icon} selected={isOpen}
            onSelect={() => setMenuOpen(!isOpen)} />
</Tooltip>
{isOpen && (
    <div className={styles.panel}>
        <header className={styles.header}>Info Loom</header>
        <div className={styles.buttonRow}>
            {sections.map(name => (
                <Button key={name} variant="flat"
                    onSelect={() => toggleSection(name)}>
                    {name}
                </Button>
            ))}
        </div>
    </div>
)}
```

> **Note on `onSelect` vs `onClick`:** The `cs2/ui` `Button` component extends
> `React.ButtonHTMLAttributes`, so both `onSelect` (CS2-specific) and `onClick`
> (standard HTML) are valid props. Always prefer `onSelect` — it integrates with
> the game's input system (gamepad support, UI sounds). InfoLoom's actual source
> uses `onClick` on flyout sub-buttons, which works but bypasses CS2 input handling.

Key SCSS for the flyout panel:

```scss
.panel {
    position: absolute;
    top: 40rem;               // below the toolbar button
    width: 200rem;
    background-color: var(--panelColorNormal);
    backdrop-filter: var(--panelBlur);
    border-radius: 4rem;
    z-index: 10;
    animation: scale-up-center 0.25s ease;
}
```

### Independent Sub-Menu Sections

Sub-menu sections (Residential, Commercial, Industrial) persist independently of their parent menu. They're rendered unconditionally at the component root so they stay visible when the main menu closes:

```typescript
// Always render sub-menus regardless of main menu state
// Each manages its own visibility binding
<ResidentialMenuButton />
<IndustrialMenuButton />
<CommercialMenuButton />
```

### Button `src` Prop for Icons

The `cs2/ui` `Button` component supports a `src` prop for icon rendering, as an alternative to the mask-image CSS approach:

```typescript
import icon from 'images/Statistics.svg';

<Button variant="floating" src={icon} selected={isOpen} onSelect={onToggle} />
```

### Draggable Panel with Initial Position

Individual data panels use the `Panel` component from `cs2/ui` with `draggable` and `initialPosition`:

```typescript
import { Panel, DraggablePanelProps } from "cs2/ui";

const MyPanel = ({ onClose }: DraggablePanelProps) => (
    <Panel draggable onClose={onClose}
           initialPosition={{ x: 0.16, y: 0.15 }}
           header={<span>Panel Title</span>}>
        {/* panel content */}
    </Panel>
);
```

The parent injects `onClose` via `React.cloneElement`:

```typescript
{React.cloneElement(section.component, {
    onClose: () => toggleSection(name),
})}
```

### ExtendedUISystemBase with ValueBindingHelper

InfoLoom (and other Yenyang-derived mods) use `ExtendedUISystemBase`, which wraps `UISystemBase` with:

- **`ValueBindingHelper<T>`** — wraps a `ValueBinding` with dirty-checking. Defers updates until `ForceUpdate()` is called during `OnUpdate()`, batching changes per frame. See [Reflection-Based Generic Serialization](#reflection-based-generic-serialization-community-pattern) for the full `ValueBindingHelper<T>` implementation.
- **`GenericUIWriter<T>` / `GenericUIReader<T>`** — reflection-based serialization that auto-serializes any object by iterating public properties and fields, avoiding the need to implement `IJsonWritable` on every type. See [Reflection-Based Generic Serialization](#reflection-based-generic-serialization-community-pattern) for the full pattern.

```csharp
public ValueBindingHelper<T> CreateBinding<T>(string key, T initialValue)
{
    var helper = new ValueBindingHelper<T>(
        new(Mod.modName, key, initialValue, new GenericUIWriter<T?>()));
    AddBinding(helper.Binding);
    _updateCallbacks.Add(helper.ForceUpdate);
    return helper;
}
```

### Extending the Selected Info Panel

InfoLoom uses `moduleRegistry.extend()` to inject custom sections into the vanilla entity info panel:

```typescript
moduleRegistry.extend(
    'game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx',
    'selectedInfoSectionComponents',
    ILCitizenInfoSection
);
```

The info section component receives the component list as a parameter and adds itself using its system-qualified name:

```typescript
export const ILCitizenInfoSection = (componentList: any): any => {
    componentList['InfoLoomTwo.Systems.Sections.ILCitizenSection'] = (props) => {
        return (
            <InfoSectionFoldout header="Citizen Info" initialExpanded={true}>
                <PanelSectionRow left="Health" right={props.Health} />
            </InfoSectionFoldout>
        );
    };
    return componentList;
};
```

### Removing/Replacing Vanilla UI Components via extend()

`moduleRegistry.extend()` can also be used to conditionally remove or replace vanilla components entirely:

```typescript
moduleRegistry.extend(
    "game-ui/game/components/some-vanilla-component.tsx",
    "SomeExport",
    (OriginalComponent) => {
        // Return null to remove, or conditionally render
        return (props) => {
            if (shouldHide) return null;
            return <OriginalComponent {...props} />;
        };
    }
);
```

FindIt uses this pattern with `RemoveVanillaAssetMenu.tsx` and `RemoveVanillaRightToolbar.tsx` to conditionally hide vanilla UI elements when its own UI is active.

### Accessing Vanilla Internal Components (VanillaComponentResolver)

The `VanillaComponentResolver` pattern (originally by Klyte45 and yenyang) is a singleton class that wraps `ModuleRegistry` to lazily resolve and cache vanilla game UI components by their registry paths. Mod TSX code cannot directly import vanilla components — they must be resolved from the registry at runtime.

```typescript
class VanillaComponentResolver {
    private registryData: ModuleRegistryData;
    private cachedComponents: Map<string, any> = new Map();

    static setRegistry(moduleRegistry: ModuleRegistryData) {
        VanillaComponentResolver.instance.registryData = moduleRegistry;
    }
}

const registryIndex = {
    Section: ['game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx', 'Section'],
    ToolButton: ['game-ui/game/components/tool-options/tool-button/tool-button.tsx', 'ToolButton'],
    InfoSection: ['game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx', 'InfoSection'],
    InfoRow: ['game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx', 'InfoRow'],
    descriptionTooltipTheme: ['game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss', 'default'],
    FOCUS_DISABLED: ['game-ui/common/focus/focus-key.ts', 'FOCUS_DISABLED'],
};

// Initialization in mod's register function:
const register: ModRegistrar = (moduleRegistry) => {
    VanillaComponentResolver.setRegistry(moduleRegistry);
};

// Usage: VanillaComponentResolver.instance.Section
```

**Discovering registry paths**: In the UI developer tools at `http://localhost:9444/`, go to Sources → Index.js, pretty print, and search for the TSX or SCSS file paths.

### Async Search Pattern with CancellationToken

For expensive UI operations like search/filter that shouldn't block the game's UI thread, use debounced async execution with `CancellationToken`:

```csharp
private CancellationTokenSource searchTokenSource = new();

private async Task DelayedSearch()
{
    // Cancel any previous pending search
    searchTokenSource.Cancel();
    searchTokenSource = new();
    var token = searchTokenSource.Token;

    // Debounce: wait 250ms before executing
    await Task.Delay(250);

    if (token.IsCancellationRequested)
        return;

    // Execute search on background thread
    ProcessSearch(token);

    // Signal OnUpdate to apply results on main thread
    if (!token.IsCancellationRequested)
        filterCompleted = true;
}
```

Key techniques:
1. **Debouncing**: 250ms delay cancels rapid repeated searches (e.g., typing in search box)
2. **CancellationToken**: Threads through entire filter pipeline; each step checks for cancellation
3. **Main thread signaling**: Results are applied in `OnUpdate()` via a boolean flag, not directly from the async task

### Additional CSS Variables

| Variable | Description |
|----------|-------------|
| `--panelBlur` | Frosted glass backdrop blur for panels |
| `--panelColorDark` | Darker panel background variant |
| `--accentColorDark-focused` | Dark accent for focused/active states |
| `--menuText1Normal` | Standard menu text color |
| `--normalTextColorLocked` | Greyed-out text for disabled controls |

## npm-Based UI Build Pipeline

Complex mods use a TypeScript/React build pipeline in a `UI/` directory with `package.json`, `webpack.config.js`, and `tsconfig.json`. The build is triggered as a post-build MSBuild target:

```xml
<!-- In .csproj -->
<Target Name="BuildUI" AfterTargets="AfterBuild">
    <Exec Command="npm run build" WorkingDirectory="$(ProjectDir)UI" />
</Target>
```

**Key TS APIs for C#↔UI communication:**
- `bindValue<T>(group, key)` — read a C# `GetterValueBinding` value
- `useValue<T>(binding)` — React hook for reactive binding values
- `trigger(group, key, ...args)` — invoke a C# `TriggerBinding`
- `moduleRegistry.extend(path, export, Component)` — inject into vanilla UI

**Styling**: Use SCSS modules (`*.module.scss`) for scoped styling, referencing game CSS variables (`--panelColorNormal`, `--accentColorNormal`, etc.).

## Info Panel Section Injection

Three techniques for injecting into the building info panel:

### 1. Subscribe to `selectedInfo.middleSections$`

```typescript
import { selectedInfo } from "cs2/bindings";
selectedInfo.middleSections$.subscribe(sections => {
    // Inject custom section types
});
```

### 2. Extend `selectedInfoSectionComponents`

```typescript
moduleRegistry.extend(
    "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
    'selectedInfoSectionComponents',
    MyInfoComponent
);
```

### 3. C# Side: Provide Data via Bindings

```csharp
// In a UISystemBase subclass:
AddUpdateBinding(new GetterValueBinding<bool>("MyMod", "IsLocked",
    () => EntityManager.HasComponent<LevelLocked>(selectedEntity)));

AddBinding(new TriggerBinding<Entity>("MyMod", "ToggleLock",
    entity => ToggleLockComponent(entity)));
```

**Key TS APIs**: `selectedInfo.selectedEntity$`, `selectedInfo.middleSections$`, `InfoSection` / `InfoRow` vanilla components for consistent styling.

## ToolbarUISystem Integration

### m_LastSelectedAssets (Reflection)

`ToolbarUISystem` has a private `m_LastSelectedAssets` field (`Dictionary<Entity, Entity>`) tracking the last selected prefab per toolbar group. When mods replace prefab entities (e.g., RoadBuilder regenerating a road prefab), stale references in this dictionary can cause toolbar issues:

```csharp
var toolbarUI = World.GetOrCreateSystemManaged<ToolbarUISystem>();
var lastSelected = typeof(ToolbarUISystem)
    .GetField("m_LastSelectedAssets", BindingFlags.NonPublic | BindingFlags.Instance)
    .GetValue(toolbarUI) as Dictionary<Entity, Entity>;
// Remove stale entries when replacing prefab entities
lastSelected.Remove(oldGroupEntity);
```

### UIGroupElement Manipulation

To add/remove prefabs from toolbar groups, modify the `UIGroupElement` buffer on group entities:

**Known UI group names**: `"RoadsSmallRoads"`, `"RoadsMediumRoads"`, `"RoadsLargeRoads"`, `"RoadsHighways"`, `"TransportationTrain"`, `"TransportationTram"`, `"TransportationSubway"`, `"TransportationRoad"`, `"Pathways"`.

## Open Questions

- [ ] Whether `CallBinding` results are serialized automatically by cohtml or require manual `IJsonWritable`
- [ ] Full list of available `moduleRegistry` injection slots beyond the documented ones
- [ ] Whether `cs2/api` exposes a `call()` function for `CallBinding` (not observed in community mods)
- [ ] How the game resolves the mod UI build output path at load time (convention vs. explicit config)
- [ ] Thread safety of `ValueBinding.Update()` when called from background systems

### Answered Questions

- **What does `moduleRegistry.extend()` wrapper receive as props?** For `selectedInfoSectionComponents`, the wrapper receives the component list (an object mapping system-qualified names to React components) and adds/modifies entries. For component wrapping (like `RightMenu`), the wrapper receives the original component as a prop.

## Sources

- Decompiled from: `Colossal.UI.Binding.dll`, `Colossal.UI.dll`, `Game.dll` (Cities: Skylines II)
- Reference mod: [RoadBuilder-CSII](https://github.com/JadHajjar/RoadBuilder-CSII) by JadHajjar
- Reference mod: [InfoLoom](https://github.com/bruceyboy24804/InfoLoom) — toolbar button, multi-level flyout menu, draggable panels, visibility-gated updates, ExtendedUISystemBase
- NuGet package: [Reinforced.Typings](https://www.nuget.org/packages/Reinforced.Typings/) — C# to TypeScript type generation
- Game version tested: Current Steam release
