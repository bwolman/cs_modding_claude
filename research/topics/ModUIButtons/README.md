# Research: Mod UI Buttons & Bindings

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-16

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

RoadBuilder's `GenericUIWriter<T>` / `GenericUIReader<T>` use reflection to auto-serialize any object by iterating public properties and fields. This avoids implementing `IJsonWritable` on every type but has runtime reflection cost. See `snippets/RoadBuilder_ExtendedUISystemBase.cs`.

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

- **`ValueBindingHelper<T>`** — wraps a `ValueBinding` with dirty-checking. Defers updates until `ForceUpdate()` is called during `OnUpdate()`, batching changes per frame.
- **`GenericUIWriter<T>` / `GenericUIReader<T>`** — reflection-based serialization that auto-serializes any object by iterating public properties and fields, avoiding the need to implement `IJsonWritable` on every type.

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

### Accessing Vanilla Internal Components

The `VanillaComponentResolver` pattern (from Klyte45) accesses game-internal React components not exposed through `cs2/ui`:

```typescript
const registryIndex = {
    Section: ['game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx', 'Section'],
    ToolButton: ['game-ui/game/components/tool-options/tool-button/tool-button.tsx', 'ToolButton'],
};

// Usage: VanillaComponentResolver.instance.Section
```

### Additional CSS Variables

| Variable | Description |
|----------|-------------|
| `--panelBlur` | Frosted glass backdrop blur for panels |
| `--panelColorDark` | Darker panel background variant |
| `--accentColorDark-focused` | Dark accent for focused/active states |
| `--menuText1Normal` | Standard menu text color |
| `--normalTextColorLocked` | Greyed-out text for disabled controls |

## Open Questions

- [ ] Whether `CallBinding` results are serialized automatically by cohtml or require manual `IJsonWritable`
- [ ] Full list of available `moduleRegistry` injection slots beyond the documented ones
- [ ] Whether `cs2/api` exposes a `call()` function for `CallBinding` (not observed in community mods)
- [ ] How the game resolves the mod UI build output path at load time (convention vs. explicit config)
- [ ] Thread safety of `ValueBinding.Update()` when called from background systems

### Answered Questions

- **What does `moduleRegistry.extend()` wrapper receive as props?** For `selectedInfoSectionComponents`, the wrapper receives the component list (an object mapping system-qualified names to React components) and adds/modifies entries. For component wrapping (like `RightMenu`), the wrapper receives the original component as a prop.

## Sources

- Decompiled from: `Colossal.UI.Binding.dll`, `Game.dll` (Cities: Skylines II)
- Reference mod: [RoadBuilder-CSII](https://github.com/JadHajjar/RoadBuilder-CSII) by JadHajjar
- Reference mod: [InfoLoom](https://github.com/bruceyboy24804/InfoLoom) — toolbar button, multi-level flyout menu, draggable panels, visibility-gated updates, ExtendedUISystemBase
- Game version tested: Current Steam release
