# Research: ProxyAction Enabled State Lifecycle & Input Blocking

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: When and how the game automatically disables mod-registered `ProxyAction` instances, what mechanisms control input blocking, and whether a mod hotkey can work globally (outside the tool context).

**Why**: Mods that register keyboard shortcuts via `ModSettings` / `ProxyAction` need to understand when those shortcuts will silently stop working. `WasPressedThisFrame()` returns `false` without any error in multiple game contexts.

**Boundaries**: Out of scope — UI rendering of keybinding widgets, gamepad-specific composite handling, and the rebinding UI flow itself.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Input | `InputManager`, `ProxyAction`, `ProxyActionMap`, `InputConflictResolution`, `InputBarrier`, `InputActivator`, `MaskFloatProcessor`, `BaseMaskProcessor<T>` |
| Game.dll | Game.Tools | `ToolSystem` (creates `m_ToolActionBarrier` on "Tool" map) |
| Game.dll | Game.Settings | `ModSetting` (calls `RegisterKeyBindings()` to create mod action map) |

## Architecture: Two Enforcement Paths

The game disables mod actions through two independent mechanisms that operate simultaneously.

### Path 1: InputConflictResolution (enable/disable via ApplyState)

`Game.Input.InputConflictResolution` is the central authority that enables/disables the underlying Unity `InputAction`. It runs `ResolveConflicts()` every frame via `InputManager.m_ConflictResolution.Update()`.

It classifies every action into three priority buckets:

```csharp
// InputConflictResolution.RefreshActions():
if (!action.isBuiltIn)
    m_ModActions.Add(new State(action));        // lowest priority
else if (action.isSystemAction)
    m_SystemActions.Add(new State(action));     // highest priority (Camera, Tool, Editor)
else
    m_UIActions.Add(new State(action));         // middle priority (menu/nav actions)
```

Each frame, `ResolveConflicts()` resolves in priority order — system > UI > mod. If a mod action shares a key binding with an enabled system or UI action, the mod action gets `m_HasConflict = true`:

```csharp
// State.enabled combines pre-resolved state with conflict detection:
public bool enabled => m_Action.preResolvedEnable && !m_HasConflict;
```

The result calls `ApplyState()`, which calls `m_SourceAction.Enable()` or `m_SourceAction.Disable()` on the underlying Unity `InputAction`.

### Path 2: MaskFloatProcessor (zeroes out values at binding level)

A `MaskFloatProcessor` is attached to every composite binding by `InitializeMasks()`. It operates at the Unity Input System level, independently of Path 1:

```csharp
public override TValue Process(TValue value, InputControl control)
{
    if ((m_Action.mask & m_Mask) != InputManager.DeviceType.None)
        return value;       // allowed
    return default(TValue); // zeroed out — silently blocked
}
```

This is a secondary safety net: even if the action somehow remains "enabled", masked device types produce zero values.

## The Global Mask Mechanism

`InputManager.mask` is recalculated every frame and pushed to **all** `ProxyActionMap` instances, including mod maps:

```csharp
// InputManager.GetMaskForControlScheme():
ControlScheme.KeyboardAndMouse => (!overlayActive) ? (hasInputFieldFocus ? 2 : 3) : 0,
//                                                    Mouse only (2)  Kb+Mouse (3)   Nothing (0)
ControlScheme.Gamepad => (!overlayActive) ? 4 : 0,
//                                           Gamepad (4)    Nothing (0)

// InputManager.mask setter pushes to ALL maps:
foreach (var (_, proxyActionMap) in m_Maps)
    proxyActionMap.mask = value;  // includes mod maps
```

When `mask = 0`, all actions in all maps get `preResolvedMask = None` → `preResolvedEnable = false` → action disabled.

## Contexts That Block Mod Keyboard Actions

### 1. Text Field Focus (any UI text input)

**Trigger**: `InputManager.instance.hasInputFieldFocus = true` (set by `OnTextInputTypeChanged()` in UI renderers)

**Effect**: Global mask becomes `DeviceType.Mouse (2)` only. Keyboard composites (value 1) are ANDed to zero. All mod keyboard bindings get `preResolvedMask = None` → disabled. `WasPressedThisFrame()` silently returns `false`.

Note: `ToolBaseSystem.actionsEnabled` also explicitly checks this:
```csharp
private protected bool actionsEnabled => m_ActionsEnabled && !InputManager.instance.hasInputFieldFocus;
```

### 2. Loading Screen

**Trigger**: `LoadingScreen.Execute()` calls `InputManager.instance.CreateOverlayBarrier("LoadingScreen")`

**Effect**: `CreateOverlayBarrier` creates an `InputBarrier` covering all maps (except "Engagement" and "Splash screen") with `blocked = true`:

```csharp
public InputBarrier CreateOverlayBarrier(string barrierName)
{
    var maps = m_Maps.Values
        .Where(m => m.name != "Engagement" && m.name != "Splash screen")
        .ToArray();
    return new InputBarrier(barrierName, maps, DeviceType.All, blocked: true);
}
```

`ProxyActionMap.UpdateState()` checks barriers:
```csharp
bool flag = m_Barriers.All(b => !b.blocked);
m_Enabled = flag;  // becomes false when any barrier is blocked
```

All mod actions in all maps → disabled.

### 3. Splash/Engagement/Piracy Screens

Same `CreateOverlayBarrier` mechanism as loading screens. All mod maps are blocked.

### 4. Platform Overlay (Steam overlay, controller pairing)

**Trigger**: `InputManager.OnOverlayStateChanged(_, active)` sets `m_OverlayActive = true`

**Effect**: `GetMaskForControlScheme()` returns `0` for all control schemes. Global mask = 0 → all mod actions disabled.

### 5. Key Rebinding Dialog

**Trigger**: `InputRebindingUISystem.Start()` sets `InputManager.instance.blockedControlTypes = DeviceType.Keyboard`

**Effect**: `blockedControlTypes` is subtracted from the global mask. While rebinding a keyboard key, all keyboard input is blocked globally — including mod keyboard actions.

### 6. Conflict with System Action (same key binding)

**Trigger**: `InputConflictResolution.ResolveConflicts()` detects binding conflict

**Effect**: If a mod action uses the same key as an enabled system action (Tool, Camera, Editor maps), the mod action gets `m_HasConflict = true` and `WasPressedThisFrame()` returns `false`.

## What Does NOT Block Mod Actions

| Context | Why it doesn't affect mod maps |
|---------|-------------------------------|
| Tool context changes (active tool, default tool) | `ToolSystem.m_ToolActionBarrier` only blocks the "Tool" named map, not mod maps |
| Camera mode changes | Camera barriers only affect the "Camera" map |
| Photo mode | Only photo mode map actions change |

**This is the key finding**: Mod maps are completely separate from the "Tool" map. `ToolSystem`'s `m_ToolActionBarrier` does NOT affect mod actions:

```csharp
// ToolSystem — only blocks the "Tool" map:
m_ToolActionBarrier = InputManager.instance.CreateMapBarrier("Tool", "ToolSystem");
```

## The `shouldBeEnabled` Requirement

**Mod actions require `shouldBeEnabled = true` to function at all.**

In `ProxyAction.UpdateState()`, if `m_Activators` is empty, `deviceType` stays `None` and the action is never enabled:

```csharp
InputManager.DeviceType deviceType = InputManager.DeviceType.None;
foreach (InputActivator activator in m_Activators)
{
    if (activator.enabled)
        deviceType |= activator.mask & m_PreResolvedMask;
}
// If m_Activators is empty → deviceType = None → action never enabled
```

`shouldBeEnabled = true` creates the required `m_DefaultActivator`:

```csharp
public bool shouldBeEnabled
{
    set
    {
        if (isBuiltIn) throw new Exception("Built-in actions can not be enabled directly");
        if (m_DefaultActivator != null)
            m_DefaultActivator.enabled = value;
        else if (value)
            m_DefaultActivator = new InputActivator(
                ignoreIsBuiltIn: false, "Default (" + name + ")",
                this, DeviceType.All, enabled: true);
    }
}
```

**`RegisterKeyBindings()` does NOT call `shouldBeEnabled = true`** — it only calls `InputManager.AddActions()`. Mods must explicitly set `action.shouldBeEnabled = true` in `OnCreate()`, or the action never fires.

## InputBarrier Lifecycle

Barriers are `IDisposable`. `CreateOverlayBarrier` returns a barrier that is `blocked = true` from construction. Use `using` for scope-limited blocking:

```csharp
using (InputManager.instance.CreateOverlayBarrier("MyScreen"))
{
    // all non-system maps blocked for this scope
}
```

For permanent barriers (e.g., `ToolSystem.m_ToolActionBarrier`), keep a reference, toggle `blocked`, and call `Dispose()` in `OnDestroy()`. The map's `UpdateState()` is triggered automatically when `barrier.blocked` changes.

## Verdict

**Mod keyboard shortcuts work globally during normal gameplay but are silently blocked in 6 specific contexts.**

### Summary Table

| Context | Mechanism | Affects mod actions? |
|---------|-----------|---------------------|
| Text field focused (UI input) | Global mask = Mouse only | **YES** — keyboard bindings silently disabled |
| Loading screen | `CreateOverlayBarrier` blocks all maps | **YES** |
| Splash/Engagement screens | Same as loading screen | **YES** |
| Platform overlay (Steam, etc.) | Global mask = 0 | **YES** — all mod actions disabled |
| Key rebinding dialog | `blockedControlTypes = Keyboard` | **YES** |
| Mod action conflicts with system action | `InputConflictResolution` marks `m_HasConflict` | **YES** — silently |
| `shouldBeEnabled` never set | No activator → action never enabled | **YES** — action never fires |
| Tool map blocked (ToolSystem barrier) | Only blocks "Tool" named map | **NO** — mod map unaffected |
| Photo mode | Only photo mode map changes | **NO** |
| Camera mode changes | Only camera map changes | **NO** |

### Recommendation for Mods

For a globally-active mod hotkey:

```csharp
public partial class MyHotkeySystem : GameSystemBase
{
    private ProxyAction _action;

    protected override void OnCreate()
    {
        base.OnCreate();
        _action = InputManager.instance.FindAction(Mod.Settings.id, "TriggerAction");

        // REQUIRED: Without this, the action never fires.
        _action.shouldBeEnabled = true;
    }

    protected override void OnUpdate()
    {
        if (_action != null && _action.WasPressedThisFrame())
        {
            DoThing();
        }
    }
}
```

- Call `shouldBeEnabled = true` — **required**, without it the action never fires
- Use a unique key combination that doesn't conflict with system actions (Camera, Tool, Editor maps)
- Accept that the key will be blocked during loading screens, splash screens, platform overlays, and text field focus — these are correct behaviors
- The key **is not** blocked by tool context changes, camera mode, or photo mode

## Open Questions

- [x] Does the game disable mod ProxyActions? — Yes, silently, in 6 contexts
- [x] Is `shouldBeEnabled = true` required? — Yes, without it the action never fires
- [x] Does `RegisterKeyBindings()` auto-enable? — No, it only calls `AddActions()`
- [x] Are mod maps affected by ToolSystem barriers? — No, only the "Tool" map is affected
- [x] What is the InputBarrier lifecycle? — IDisposable; `blocked = true` from construction; use `using` for scoped blocking

## Sources

- Decompiled from: Game.dll — `Game.Input.InputManager`, `Game.Input.ProxyAction`, `Game.Input.ProxyActionMap`, `Game.Input.InputConflictResolution`, `Game.Input.InputBarrier`, `Game.Input.InputActivator`, `Game.Input.BaseMaskProcessor<T>`, `Game.Tools.ToolSystem`, `Game.Settings.ModSetting`
- Related research: `research/topics/ToolActivation/` (ToolSystem lifecycle), `research/topics/ToolRaycast/` (ToolSystem architecture)
- Game version: Current as of 2026-02-15
