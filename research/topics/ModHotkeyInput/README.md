# Research: Mod Hotkey Input

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 handles keyboard input and how mods register rebindable hotkeys.

**Why**: To add keybindings to any mod we build -- triggering custom logic from keyboard shortcuts, with player-rebindable keys in the Options UI.

**Boundaries**: Not covering gamepad bindings in depth (documented but not primary focus). Not covering UI input fields or text entry. Not covering the low-level Unity Input System directly -- mods should only use the CO wrapper.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Input | `InputManager`, `ProxyAction`, `ProxyActionMap`, `ProxyBinding`, `ProxyComposite`, `ProxyModifier`, `ActionType`, `BindingKeyboard`, `ActionComponent`, `Mode`, `RebindOptions`, `ModifierOptions` |
| Game.dll | Game.Settings | `SettingsUIKeyboardActionAttribute`, `SettingsUIKeyboardBindingAttribute`, `SettingsUIInputActionAttribute`, `SettingsUIKeybindingAttribute`, `SettingsUIMouseBindingAttribute`, `SettingsUIGamepadBindingAttribute`, `SettingsUIBindingMimicAttribute` |
| Game.dll | Game.Modding | `ModSetting` (keybinding registration lifecycle) |
| Unity.InputSystem.dll | UnityEngine.InputSystem | Underlying Unity Input System (not used directly by mods) |

## Architecture

CS2 wraps Unity's Input System behind `Game.Input.InputManager` (singleton). Mods never touch Unity `InputAction` directly.

```
ModSetting subclass
    |-- [SettingsUIKeyboardAction] attrs on class (declare actions)
    |-- ProxyBinding properties with [SettingsUIKeyboardBinding] attrs (declare default keys)
    |
    v
Constructor: InitializeKeyBindings()
    |-- Reads reflection on ProxyBinding properties
    |-- Generates ProxyBinding values from attributes
    |
    v
Mod.OnLoad():
    |-- RegisterInOptionsUI() -> adds settings page to Options
    |-- LoadSettings() -> loads saved rebindings from disk
    |-- RegisterKeyBindings() -> calls InputManager.AddActions()
    |       |-- Creates ProxyActionMap + ProxyAction in InputManager
    |       |-- Creates ProxyBinding.Watcher for auto-sync on rebind
    |
    v
ECS System OnUpdate():
    |-- InputManager.instance.FindAction(mapName, actionName) -> ProxyAction
    |-- action.WasPressedThisFrame() / IsPressed() / onInteraction event
```

## Type Map

### `InputManager` (Game.Input)

Singleton class. Owns all action maps and resolves conflicts.

**Key members**:

| Member | Type | Description |
|--------|------|-------------|
| `instance` | static InputManager | Singleton accessor |
| `FindAction(mapName, actionName)` | ProxyAction | Looks up an action by map and action name |
| `TryFindAction(mapName, actionName, out action)` | bool | Safe lookup returning success |
| `FindActionMap(name)` | ProxyActionMap | Looks up an action map by name |
| `AddActions(ProxyAction.Info[])` | void | Registers new actions (called by `ModSetting.RegisterKeyBindings()`) |
| `DeferUpdating()` | IDisposable | Defers binding resolution until disposed |
| `SetBinding(binding, out result)` | void | Sets/rebinds a binding |
| `GetBindings(pathType, options)` | IEnumerable\<ProxyBinding\> | Gets all bindings |
| `CreateMapBarrier(map, name)` | InputBarrier | Creates barrier to block all actions in a map |
| `CreateActionBarrier(map, action, name)` | InputBarrier | Creates barrier to block a specific action |

**Built-in map name constants**:

| Constant | Value |
|----------|-------|
| `kSplashScreenMap` | "Splash screen" |
| `kNavigationMap` | "Navigation" |
| `kMenuMap` | "Menu" |
| `kCameraMap` | "Camera" |
| `kToolMap` | "Tool" |
| `kShortcutsMap` | "Shortcuts" |
| `kPhotoModeMap` | "Photo mode" |
| `kEditorMap` | "Editor" |
| `kDebugMap` | "Debug" |
| `kEngagementMap` | "Engagement" |

**Built-in modifier path constants**:

| Constant | Value |
|----------|-------|
| `kShiftName` | `<Keyboard>/shift` |
| `kCtrlName` | `<Keyboard>/ctrl` |
| `kAltName` | `<Keyboard>/alt` |
| `kLeftStick` | `<Gamepad>/leftStickPress` |
| `kRightStick` | `<Gamepad>/rightStickPress` |

*Source: `Game.dll` -> `Game.Input.InputManager`*

### `ProxyAction` (Game.Input)

Wraps a Unity `InputAction`. Provides polling methods and event subscription.

**Key members**:

| Member | Type | Description |
|--------|------|-------------|
| `name` | string | Action name |
| `mapName` | string | Parent map name |
| `enabled` | bool | Whether action is currently enabled |
| `isKeyboardAction` | bool | Has keyboard composite |
| `isMouseAction` | bool | Has mouse composite |
| `isGamepadAction` | bool | Has gamepad composite |
| `composites` | IReadOnlyDictionary\<DeviceType, ProxyComposite\> | Per-device composites |
| `bindings` | IEnumerable\<ProxyBinding\> | All bindings across composites |
| `shouldBeEnabled` | bool | Set to true/false to enable/disable (non-built-in actions only) |

**Polling methods** (call from `OnUpdate()`):

| Method | Returns | Description |
|--------|---------|-------------|
| `IsPressed()` | bool | True while key is held down |
| `WasPressedThisFrame()` | bool | True on the frame the key was pressed |
| `WasReleasedThisFrame()` | bool | True on the frame the key was released |
| `WasPerformedThisFrame()` | bool | True when action was fully performed this frame |
| `IsInProgress()` | bool | True while action is in progress |
| `ReadValue<T>()` | T | Read current value (for Axis/Vector2 actions) |
| `GetMagnitude()` | float | Get current magnitude |

**Event subscription**:

```csharp
event Action<ProxyAction, InputActionPhase> onInteraction;
// Phases: Started, Performed, Canceled
```

Subscribing to `onInteraction` automatically hooks Unity's `started`, `performed`, and `canceled` callbacks. When no subscribers remain, callbacks are unhooked.

**Other methods**:

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateBarrier(name, mask)` | InputBarrier | Block this action from processing input |
| `CreateActivator(name, mask)` | InputActivator | Enable this action for specific devices |

**Nested types**:
- `ProxyAction.Info` -- Blueprint struct: `m_Name`, `m_Map`, `m_Type`, `m_Composites`

*Source: `Game.dll` -> `Game.Input.ProxyAction`*

### `ProxyBinding` (Game.Input)

Struct representing one binding: which map, action, device, key path, and modifiers.

**Key fields**:

| Field | Type | Description |
|-------|------|-------------|
| `m_MapName` | string | Action map name |
| `m_ActionName` | string | Action name within the map |
| `m_Component` | ActionComponent | Which component (Press, Negative, Positive, etc.) |
| `m_Name` | string | Binding name within the composite |
| `m_Device` | InputManager.DeviceType | Device type (Keyboard, Mouse, Gamepad) |
| `m_Path` | string | Unity input path (e.g. `<Keyboard>/f5`) |
| `m_Modifiers` | ProxyModifier[] | Modifier keys (shift, ctrl, alt) |

**Key properties**:

| Property | Type | Description |
|----------|------|-------------|
| `path` | string | Get/set the key path |
| `modifiers` | IReadOnlyList\<ProxyModifier\> | Get/set modifier keys |
| `isSet` | bool | True if path is non-empty |
| `isBuiltIn` | bool | True if this is a game-original binding |
| `isRebindable` | bool | True if player can rebind |
| `hasConflicts` | ConflictType | Whether this binding conflicts with others |
| `conflicts` | IList\<ProxyBinding\> | List of conflicting bindings |
| `action` | ProxyAction | Resolved action this binding belongs to |
| `title` | string | `mapName/actionName/name` |

**Nested types**:
- `ProxyBinding.Watcher` -- Watches for rebinding changes, auto-syncs property values
- `ProxyBinding.Comparer` -- Configurable equality comparer
- `ProxyBinding.ConflictType` -- None, WithBuiltIn, WithNotBuiltIn, All

*Source: `Game.dll` -> `Game.Input.ProxyBinding`*

### `ProxyModifier` (Game.Input)

Struct representing a modifier key attached to a binding.

| Field | Type | Description |
|-------|------|-------------|
| `m_Component` | ActionComponent | Component this modifier applies to |
| `m_Name` | string | Modifier name (e.g. "modifier") |
| `m_Path` | string | Unity input path (e.g. `<Keyboard>/shift`) |

*Source: `Game.dll` -> `Game.Input.ProxyModifier`*

### `ModSetting` (Game.Modding)

Base class for mod settings. Handles keybinding registration lifecycle.

**Key members**:

| Member | Type | Description |
|--------|------|-------------|
| `id` | string | Unique ID: `{AssemblyName}.{Namespace}.{ModType}` |
| `name` | string | Settings class name |
| `keyBindingRegistered` | bool | Whether RegisterKeyBindings has been called |

**Key methods**:

| Method | Description |
|--------|-------------|
| `RegisterInOptionsUI()` | Registers settings page in Options screen |
| `UnregisterInOptionsUI()` | Removes settings page |
| `RegisterKeyBindings()` | Reads all `ProxyBinding` properties, builds `ProxyAction.Info[]`, calls `InputManager.AddActions()`, creates watchers |
| `InitializeKeyBindings()` | Called in constructor. Reads binding attributes via reflection, sets initial `ProxyBinding` values |

**Registration flow in `RegisterKeyBindings()`**:
1. Collects all `ProxyBinding` properties via reflection
2. Reads `[SettingsUIInputActionAttribute]` from class for action configuration
3. Groups bindings by action name, builds `ProxyAction.Info` with composites
4. Calls `InputManager.instance.AddActions(actionsToAdd)` to register
5. Creates `ProxyBinding.Watcher` for each property (auto-syncs on player rebind)
6. Sets `keyBindingRegistered = true`

*Source: `Game.dll` -> `Game.Modding.ModSetting`*

## Settings Attributes

### `SettingsUIKeyboardActionAttribute` (Game.Settings)

Applied to the **class** (AllowMultiple = true). Declares an input action.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SettingsUIKeyboardActionAttribute : SettingsUIInputActionAttribute
```

**Constructor parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | required | Action name (must match binding's `actionName`) |
| `type` | ActionType | Button | Button, Axis, or Vector2 |
| `rebindOptions` | RebindOptions | All | What the player can rebind (Key, Modifiers, or All) |
| `modifierOptions` | ModifierOptions | Allow | Whether modifiers are allowed |
| `canBeEmpty` | bool | true | Whether the binding can be unset |
| `developerOnly` | bool | false | Hide from non-developer users |
| `mode` | Mode | DigitalNormalized | Input processing mode |
| `usages` | string[] | null | Custom usage tags |
| `interactions` | string[] | null | Unity Input System interactions |
| `processors` | string[] | null | Unity Input System processors |

### `SettingsUIKeyboardBindingAttribute` (Game.Settings)

Applied to **ProxyBinding properties**. Sets the default key for a binding.

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class SettingsUIKeyboardBindingAttribute : SettingsUIKeybindingAttribute
```

**Constructor overloads**:

```csharp
// Button with default key + modifiers
SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, string actionName = null,
    bool alt = false, bool ctrl = false, bool shift = false)

// Button with no default key
SettingsUIKeyboardBindingAttribute(string actionName = null)

// Axis component
SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, AxisComponent component,
    string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)

// Vector2 component
SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, Vector2Component component,
    string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
```

**Key behavior**: The `control` property converts `BindingKeyboard` enum to Unity path strings (e.g. `BindingKeyboard.F5` -> `<Keyboard>/f5`). The `modifierControls` property yields modifier paths for shift/ctrl/alt.

### `SettingsUIBindingMimicAttribute` (Game.Settings)

Applied to **ProxyBinding properties**. Mirrors a built-in game binding so the mod's key automatically updates when the player rebinds the original.

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class SettingsUIBindingMimicAttribute : Attribute
{
    public readonly string map;    // Built-in map name
    public readonly string action; // Built-in action name
}
```

## Key Enums

### `ActionType` (Game.Input)

```csharp
public enum ActionType { Button, Axis, Vector2 }
```

### `BindingKeyboard` (Game.Input)

106 values covering all standard keyboard keys:
- Letters: `A`-`Z` (15-40)
- Digits: `Digit0`-`Digit9` (41-50)
- Function: `F1`-`F12` (94-105)
- Navigation: `LeftArrow`, `RightArrow`, `UpArrow`, `DownArrow`, `Home`, `End`, `PageUp`, `PageDown`
- Editing: `Space`, `Enter`, `Tab`, `Backspace`, `Delete`, `Escape`
- Numpad: `Numpad0`-`Numpad9`, `NumpadEnter`, `NumpadDivide`, `NumpadMultiply`, `NumpadPlus`, `NumpadMinus`, `NumpadPeriod`, `NumpadEquals`
- Symbols: `Backquote`, `Quote`, `Semicolon`, `Comma`, `Period`, `Slash`, `Backslash`, `LeftBracket`, `RightBracket`, `Minus`, `Equals`
- OEM: `OEM1`-`OEM5` (106-110)
- Special: `None` (0)

### `ActionComponent` (Game.Input)

```csharp
public enum ActionComponent { None, Press, Negative, Positive, Down, Up, Left, Right }
```

### `InputManager.DeviceType` (Game.Input)

```csharp
[Flags]
public enum DeviceType { None = 0, Keyboard = 1, Mouse = 2, Gamepad = 4, All = 7 }
```

### `RebindOptions` (Game.Input)

```csharp
[Flags]
public enum RebindOptions { None = 0, Key = 1, Modifiers = 2, All = 3 }
```

### `ModifierOptions` (Game.Input)

```csharp
public enum ModifierOptions { Disallow, Allow, Ignore }
```

### `Mode` (Game.Input)

```csharp
public enum Mode { DigitalNormalized, Digital, Analog }
```

## Data Flow

```
[Mod loads]
    |
    v
ModSetting constructor:
    |-- InitializeKeyBindings()
    |   |-- For each ProxyBinding property:
    |   |   |-- Reads [SettingsUIKeyboardBinding] attribute
    |   |   |-- Generates ProxyBinding with default path + modifiers
    |   |   |-- Sets property value
    |
    v
Mod.OnLoad():
    |-- RegisterInOptionsUI() -> Options page appears in game
    |-- LoadSettings() -> loads saved rebindings from disk (overrides defaults)
    |-- RegisterKeyBindings()
    |   |-- Reads [SettingsUIKeyboardAction] from class attrs
    |   |-- Groups ProxyBinding properties by actionName
    |   |-- Builds ProxyAction.Info[] with composites
    |   |-- Calls InputManager.instance.AddActions()
    |   |   |-- Creates/gets ProxyActionMap for the mod's ID
    |   |   |-- Creates ProxyAction for each action
    |   |   |-- Creates ProxyComposite per device
    |   |   |-- Applies bindings to composites
    |   |-- Creates ProxyBinding.Watcher for each property
    |   |   |-- Auto-syncs property value when player rebinds
    |
    v
[Game running]
    |
    v
ECS System.OnCreate():
    |-- m_Action = InputManager.instance.FindAction(settings.id, "ActionName")
    |-- optionally: m_Action.shouldBeEnabled = true
    |
    v
ECS System.OnUpdate() -- every frame:
    |-- if (m_Action.WasPressedThisFrame()) { ... }
    |
    v
[Player rebinds in Options UI]
    |-- InputRebindingUISystem handles capture
    |-- InputManager.SetBinding() updates path
    |-- ProxyBinding.Watcher fires -> property value auto-updates
    |-- Settings saved to disk via AssetDatabase
```

## Mod Blueprint

### Complete Example: Adding a Hotkey to a Mod

#### Step 1: Settings Class

```csharp
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

[FileLocation(nameof(MyMod))]
[SettingsUIKeyboardAction("TriggerAction", ActionType.Button)]
public class MyModSettings : ModSetting
{
    public MyModSettings(IMod mod) : base(mod) { }

    [SettingsUIKeyboardBinding(BindingKeyboard.F5, "TriggerAction", shift: true)]
    public ProxyBinding TriggerActionBinding { get; set; }

    public override void SetDefaults() { }
}
```

This declares a "TriggerAction" action with default binding `Shift+F5`.

#### Step 2: Registration in Mod.cs

```csharp
public class Mod : IMod
{
    internal static MyModSettings Settings { get; private set; }

    public void OnLoad(UpdateSystem updateSystem)
    {
        Settings = new MyModSettings(this);
        Settings.RegisterInOptionsUI();
        AssetDatabase.global.LoadSettings(nameof(MyMod), Settings, new MyModSettings(this));
        Settings.RegisterKeyBindings();

        // Register your system
        updateSystem.UpdateAt<MyHotkeySystem>(SystemUpdatePhase.MainLoop);
    }

    public void OnDispose() { }
}
```

**Order matters**: `RegisterInOptionsUI()` -> `LoadSettings()` -> `RegisterKeyBindings()`.

#### Step 3: Reading the Hotkey in a System

```csharp
using Game.Input;
using Unity.Entities;

public partial class MyHotkeySystem : GameSystemBase
{
    private ProxyAction _triggerAction;

    protected override void OnCreate()
    {
        base.OnCreate();
        _triggerAction = InputManager.instance.FindAction(
            Mod.Settings.id, "TriggerAction");
    }

    protected override void OnUpdate()
    {
        if (_triggerAction != null && _triggerAction.WasPressedThisFrame())
        {
            // Your logic here
            Log.Info("Hotkey pressed!");
        }
    }
}
```

#### Step 4: Multiple Bindings (Optional)

```csharp
[FileLocation(nameof(MyMod))]
[SettingsUIKeyboardAction("TogglePanel")]
[SettingsUIKeyboardAction("DoSomething")]
public class MyModSettings : ModSetting
{
    public MyModSettings(IMod mod) : base(mod) { }

    [SettingsUIKeyboardBinding(BindingKeyboard.F5, "TogglePanel", shift: true)]
    public ProxyBinding TogglePanelBinding { get; set; }

    [SettingsUIKeyboardBinding(BindingKeyboard.F6, "DoSomething", ctrl: true)]
    public ProxyBinding DoSomethingBinding { get; set; }

    public override void SetDefaults() { }
}
```

#### Step 5: Event-Driven Alternative

```csharp
// Instead of polling in OnUpdate, subscribe to events:
_triggerAction.onInteraction += (action, phase) =>
{
    if (phase == UnityEngine.InputSystem.InputActionPhase.Performed)
    {
        // Your logic here
    }
};
```

### Key Points

- **Map name**: The `ModSetting.id` property is used as the action map name (format: `{AssemblyName}.{Namespace}.{ModType}`)
- **Action name**: The `actionName` parameter in `[SettingsUIKeyboardBinding]` links the binding to its `[SettingsUIKeyboardAction]`
- **If `actionName` is null**: defaults to the property name
- **Rebinding**: Comes free with `RegisterInOptionsUI()` -- player sees the binding in Options and can rebind
- **Persistence**: Rebindings are saved/loaded via `AssetDatabase` automatically
- **Barriers**: Use `action.CreateBarrier()` to temporarily block input (e.g., when a text field has focus)
- **Activators**: Use `action.CreateActivator()` to enable/disable per device type
- **`shouldBeEnabled`**: Must be set to `true` for non-built-in actions to receive input (but this is handled automatically by the framework for mod actions)

## Open Questions

- [ ] How does `shouldBeEnabled` interact with the automatic enabling done by `RegisterKeyBindings()`? Is it necessary to explicitly set it for mod actions?
- [ ] What happens if two mods register the same action name in different maps? Conflict resolution behavior for cross-mod conflicts.
- [ ] How does the `Usages` system work for filtering when actions are active? (e.g., only during gameplay vs. in menus)
- [ ] Can mod actions be registered for mouse buttons using `SettingsUIMouseBindingAttribute`? How do the `BindingMouse` enum values map to buttons?
- [ ] What is the `InputBarrier` lifecycle? When should mods create and dispose barriers?
- [ ] How does `SettingsUIBindingMimicAttribute` interact with mod-registered bindings -- can a mod mirror a built-in shortcut and extend it?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
- Reference: [Mod Key Binding - CS2 Wiki](https://cs2.paradoxwikis.com/Mod_Key_Binding)
