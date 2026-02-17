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
| `GetAction(actionName)` | Convenience wrapper: returns `InputManager.instance.FindAction(id, actionName)`. Preferred over manual `FindAction` calls since it auto-fills the map name from `settings.id`. |

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

Applied to the **class** (AllowMultiple = true). Declares an input action. **Optional** — if omitted, `RegisterKeyBindings()` auto-creates actions from `[SettingsUIKeyboardBinding]` properties using default `ActionType.Button` settings. Explicit class-level attributes give control over `ActionType`, `usages`, `rebindOptions`, and other parameters. Yenyang's Recolor mod successfully uses keybindings without any `[SettingsUIKeyboardAction]` class attributes.

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

**Order is flexible**: All three calls (`RegisterInOptionsUI()`, `LoadSettings()`, `RegisterKeyBindings()`) must happen, but the order does not appear to be strict. Working mods use different orderings -- e.g., yenyang's mods call `RegisterKeyBindings()` first, then `RegisterInOptionsUI()`, then `LoadSettings()`. The `ProxyBinding.Watcher` mechanism handles deferred sync when saved rebindings are loaded after actions are registered.

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
        // Option A: Use ModSetting.GetAction() (preferred — auto-fills map name)
        _triggerAction = Mod.Settings.GetAction("TriggerAction");

        // Option B: Manual lookup via InputManager
        // _triggerAction = InputManager.instance.FindAction(
        //     Mod.Settings.id, "TriggerAction");
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

## Examples

### Example 1: Minimal Single-Hotkey Mod

A complete minimal setup for a mod with one rebindable hotkey (Ctrl+H) that toggles a boolean flag.

```csharp
// === Settings class ===
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

[FileLocation(nameof(ToggleOverlayMod))]
[SettingsUIKeyboardAction("ToggleOverlay", ActionType.Button)]
public class ToggleOverlaySettings : ModSetting
{
    public ToggleOverlaySettings(IMod mod) : base(mod) { }

    // Default binding: Ctrl+H
    // The actionName "ToggleOverlay" links this property to the class-level attribute above.
    [SettingsUIKeyboardBinding(BindingKeyboard.H, "ToggleOverlay", ctrl: true)]
    public ProxyBinding ToggleOverlayBinding { get; set; }

    public override void SetDefaults() { }
}
```

```csharp
// === Mod entry point ===
using Colossal.IO.AssetDatabase;
using Game.Modding;

public class ToggleOverlayMod : IMod
{
    internal static ToggleOverlaySettings Settings { get; private set; }

    public void OnLoad(UpdateSystem updateSystem)
    {
        Settings = new ToggleOverlaySettings(this);

        // Order matters: register UI first, load saved rebindings, then register with InputManager
        Settings.RegisterInOptionsUI();
        AssetDatabase.global.LoadSettings(nameof(ToggleOverlayMod), Settings, new ToggleOverlaySettings(this));
        Settings.RegisterKeyBindings();

        updateSystem.UpdateAt<ToggleOverlaySystem>(SystemUpdatePhase.MainLoop);
    }

    public void OnDispose() { }
}
```

```csharp
// === ECS System that reads the hotkey ===
using Game.Input;
using Unity.Entities;

public partial class ToggleOverlaySystem : GameSystemBase
{
    private ProxyAction _toggleAction;
    private bool _overlayVisible;

    protected override void OnCreate()
    {
        base.OnCreate();
        // Look up the action using the settings ID as the map name
        _toggleAction = InputManager.instance.FindAction(
            ToggleOverlayMod.Settings.id, "ToggleOverlay");
    }

    protected override void OnUpdate()
    {
        // WasPressedThisFrame() returns true only on the frame the key goes down
        if (_toggleAction != null && _toggleAction.WasPressedThisFrame())
        {
            _overlayVisible = !_overlayVisible;
            Log.Info($"Overlay toggled: {_overlayVisible}");
        }
    }
}
```

### Example 2: Multiple Actions with Different Modifiers

Demonstrates registering several hotkeys on one settings class. Each `[SettingsUIKeyboardAction]` declares an action, and each `ProxyBinding` property links to its action via the `actionName` parameter.

```csharp
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

[FileLocation(nameof(ZoneToolMod))]
[SettingsUIKeyboardAction("CycleZoneUp", ActionType.Button)]
[SettingsUIKeyboardAction("CycleZoneDown", ActionType.Button)]
[SettingsUIKeyboardAction("ResetZone", ActionType.Button)]
public class ZoneToolSettings : ModSetting
{
    public ZoneToolSettings(IMod mod) : base(mod) { }

    // PageUp with no modifiers
    [SettingsUIKeyboardBinding(BindingKeyboard.PageUp, "CycleZoneUp")]
    public ProxyBinding CycleZoneUpBinding { get; set; }

    // PageDown with no modifiers
    [SettingsUIKeyboardBinding(BindingKeyboard.PageDown, "CycleZoneDown")]
    public ProxyBinding CycleZoneDownBinding { get; set; }

    // Ctrl+Shift+R
    [SettingsUIKeyboardBinding(BindingKeyboard.R, "ResetZone", ctrl: true, shift: true)]
    public ProxyBinding ResetZoneBinding { get; set; }

    public override void SetDefaults() { }
}
```

```csharp
// Reading multiple actions in a system
public partial class ZoneToolHotkeySystem : GameSystemBase
{
    private ProxyAction _cycleUp;
    private ProxyAction _cycleDown;
    private ProxyAction _reset;

    protected override void OnCreate()
    {
        base.OnCreate();
        string mapName = ZoneToolMod.Settings.id;
        _cycleUp = InputManager.instance.FindAction(mapName, "CycleZoneUp");
        _cycleDown = InputManager.instance.FindAction(mapName, "CycleZoneDown");
        _reset = InputManager.instance.FindAction(mapName, "ResetZone");
    }

    protected override void OnUpdate()
    {
        if (_cycleUp.WasPressedThisFrame())
        {
            // Cycle to next zone type
        }
        else if (_cycleDown.WasPressedThisFrame())
        {
            // Cycle to previous zone type
        }

        if (_reset.WasPressedThisFrame())
        {
            // Reset to default zone
        }
    }
}
```

### Example 3: Hold-to-Activate with IsPressed()

Use `IsPressed()` for actions that should fire continuously while a key is held, rather than once on key-down.

```csharp
[FileLocation(nameof(SpeedBoostMod))]
[SettingsUIKeyboardAction("SpeedBoost", ActionType.Button)]
public class SpeedBoostSettings : ModSetting
{
    public SpeedBoostSettings(IMod mod) : base(mod) { }

    [SettingsUIKeyboardBinding(BindingKeyboard.Tab, "SpeedBoost", shift: true)]
    public ProxyBinding SpeedBoostBinding { get; set; }

    public override void SetDefaults() { }
}
```

```csharp
public partial class SpeedBoostSystem : GameSystemBase
{
    private ProxyAction _boostAction;

    protected override void OnCreate()
    {
        base.OnCreate();
        _boostAction = InputManager.instance.FindAction(
            SpeedBoostMod.Settings.id, "SpeedBoost");
    }

    protected override void OnUpdate()
    {
        // IsPressed() is true every frame while the key is held down
        if (_boostAction.IsPressed())
        {
            // Apply speed multiplier while held
        }

        // WasReleasedThisFrame() fires once when the key is let go
        if (_boostAction.WasReleasedThisFrame())
        {
            // Revert to normal speed
        }
    }
}
```

### Example 4: Event-Driven Input with onInteraction

Instead of polling in `OnUpdate()`, subscribe to the `onInteraction` event. Useful when you want to react to input outside of the ECS update loop, or when you need to distinguish between Started, Performed, and Canceled phases.

```csharp
using Game.Input;
using UnityEngine.InputSystem;

public partial class EventDrivenHotkeySystem : GameSystemBase
{
    private ProxyAction _action;

    protected override void OnCreate()
    {
        base.OnCreate();
        _action = InputManager.instance.FindAction(
            MyMod.Settings.id, "TriggerAction");

        // Subscribe to interaction events
        _action.onInteraction += OnHotkeyInteraction;
    }

    private void OnHotkeyInteraction(ProxyAction action, InputActionPhase phase)
    {
        switch (phase)
        {
            case InputActionPhase.Started:
                // Key was initially pressed down
                Log.Info("Key down");
                break;
            case InputActionPhase.Performed:
                // Action fully performed (for Button type, same as Started)
                Log.Info("Action performed");
                break;
            case InputActionPhase.Canceled:
                // Key was released
                Log.Info("Key released");
                break;
        }
    }

    protected override void OnUpdate() { }

    protected override void OnDestroy()
    {
        // Always unsubscribe to prevent leaks — when no subscribers remain,
        // ProxyAction automatically unhooks from the underlying Unity callbacks.
        if (_action != null)
            _action.onInteraction -= OnHotkeyInteraction;
        base.OnDestroy();
    }
}
```

### Example 5: Safe Action Lookup with TryFindAction

Use `TryFindAction` for defensive lookups, especially when the action may not be registered yet or the map name might be wrong.

```csharp
protected override void OnCreate()
{
    base.OnCreate();

    // TryFindAction returns false if the map or action doesn't exist
    if (InputManager.instance.TryFindAction(MyMod.Settings.id, "TriggerAction", out var action))
    {
        _triggerAction = action;
    }
    else
    {
        Log.Warn("Failed to find TriggerAction — keybinding may not be registered yet.");
    }
}
```

### Example 6: Unbound Default with Optional Keybinding

Declare an action with no default key. The player must manually bind it in Options. This is useful for optional or advanced shortcuts that should not conflict with anything by default.

```csharp
[FileLocation(nameof(AdvancedToolMod))]
[SettingsUIKeyboardAction("SecretFeature", ActionType.Button, canBeEmpty: true)]
public class AdvancedToolSettings : ModSetting
{
    public AdvancedToolSettings(IMod mod) : base(mod) { }

    // No default key — pass only the actionName
    [SettingsUIKeyboardBinding("SecretFeature")]
    public ProxyBinding SecretFeatureBinding { get; set; }

    public override void SetDefaults() { }
}
```

### Example 7: Temporarily Blocking Input with Barriers

Use `CreateBarrier` to prevent an action from processing input while a UI element has focus (e.g., a text field). Dispose the barrier to re-enable input.

```csharp
public partial class MyPanelSystem : GameSystemBase
{
    private ProxyAction _hotkeyAction;
    private InputBarrier _barrier;

    protected override void OnCreate()
    {
        base.OnCreate();
        _hotkeyAction = InputManager.instance.FindAction(
            MyMod.Settings.id, "TogglePanel");
    }

    /// <summary>
    /// Call when a text field gains focus to block the hotkey from firing.
    /// </summary>
    public void OnTextFieldFocused()
    {
        // CreateBarrier blocks the action from receiving input
        _barrier = _hotkeyAction.CreateBarrier("TextFieldFocus");
    }

    /// <summary>
    /// Call when the text field loses focus to restore hotkey input.
    /// </summary>
    public void OnTextFieldBlurred()
    {
        // Disposing the barrier re-enables the action
        _barrier?.Dispose();
        _barrier = null;
    }

    protected override void OnUpdate() { }
}
```

### Example 8: Mouse Action with Vanilla Binding Mimic

Register a mouse action and copy the vanilla "Apply" binding at runtime. This ensures the mod's tool responds to whatever mouse button the user has configured for the Apply action.

```csharp
// In Settings class: register a custom mouse action
[SettingsUIMouseAction(nameof(FindIt) + "Apply", "CustomUsage")]
public class FindItSettings : ModSetting
{
    // Hidden binding property — users don't see this in settings
    [SettingsUIMouseBinding(nameof(FindIt) + "Apply"), SettingsUIHidden]
    public ProxyBinding ApplyMimic { get; set; }
}

// In system: copy vanilla Apply binding to our custom action
protected override void OnCreate()
{
    base.OnCreate();
    _applyAction = Mod.Settings.GetAction(nameof(FindIt) + "Apply");

    // Find the vanilla Apply action
    var builtInApply = InputManager.instance.FindAction(InputManager.kToolMap, "Apply");

    // Copy its mouse binding to our action
    var mimicBinding = _applyAction.bindings
        .FirstOrDefault(b => b.device == InputManager.DeviceType.Mouse);
    var builtInBinding = builtInApply.bindings
        .FirstOrDefault(b => b.device == InputManager.DeviceType.Mouse);

    mimicBinding.path = builtInBinding.path;
    mimicBinding.modifiers = builtInBinding.modifiers;
    InputManager.instance.SetBinding(mimicBinding, out _);
}
```

Key techniques:
1. `SettingsUIMouseAction` class attribute declares a mouse action (not keyboard)
2. `SettingsUIMouseBinding` + `SettingsUIHidden` creates a hidden proxy binding
3. At runtime, copy `path` and `modifiers` from the vanilla action using `InputManager.instance.FindAction`
4. Call `InputManager.instance.SetBinding()` to apply the copied binding

## Open Questions

- [ ] How does `shouldBeEnabled` interact with the automatic enabling done by `RegisterKeyBindings()`? Is it necessary to explicitly set it for mod actions?
- [ ] What happens if two mods register the same action name in different maps? Conflict resolution behavior for cross-mod conflicts.
- [ ] How does the `Usages` system work for filtering when actions are active? (e.g., only during gameplay vs. in menus)
- [x] Can mod actions be registered for mouse buttons? **Answer**: Yes, use `SettingsUIMouseAction` class attribute + `SettingsUIMouseBinding` property attribute. The binding can be configured at runtime by copying from vanilla actions via `InputManager.instance.FindAction` and `SetBinding`.
- [ ] What is the `InputBarrier` lifecycle? When should mods create and dispose barriers?
- [x] How does binding mimic work? **Answer**: `FindAction` on the vanilla action, copy `path` and `modifiers` from its mouse binding to your custom action's binding, then call `InputManager.instance.SetBinding()`.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
- Reference: [Mod Key Binding - CS2 Wiki](https://cs2.paradoxwikis.com/Mod_Key_Binding)
