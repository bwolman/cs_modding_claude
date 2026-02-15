# Research: Mod UI Buttons & Bindings

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

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
| Vanilla component paths | e.g. `"game-ui/game/components/right-menu/right-menu.tsx"` | `extend` | Wrap/replace vanilla components |

- `append(slot, Component)` — adds a React component to an injection slot
- `extend(path, exportName, WrapperComponent)` — wraps a vanilla component with a higher-order component

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

## Open Questions

- [ ] What `moduleRegistry.extend()` wrapper components receive as props (the wrapped component? its props?)
- [ ] Whether `CallBinding` results are serialized automatically by cohtml or require manual `IJsonWritable`
- [ ] Full list of available `moduleRegistry` injection slots beyond the documented ones
- [ ] Whether `cs2/api` exposes a `call()` function for `CallBinding` (not observed in community mods)
- [ ] How the game resolves the mod UI build output path at load time (convention vs. explicit config)
- [ ] Thread safety of `ValueBinding.Update()` when called from background systems

## Sources

- Decompiled from: `Colossal.UI.Binding.dll`, `Game.dll` (Cities: Skylines II)
- Reference mod: [RoadBuilder-CSII](https://github.com/JadHajjar/RoadBuilder-CSII) by JadHajjar
- Game version tested: Current Steam release
