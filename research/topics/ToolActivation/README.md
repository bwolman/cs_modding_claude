# Research: Tool Activation from Non-Update Contexts

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: Whether setting `ToolSystem.activeTool` from outside the normal `ToolUpdate` phase (e.g., from a `TriggerBinding` callback or other non-ECS context) is safe, or whether it can cause race conditions or be silently ignored.

**Why**: Mods that activate custom tools from UI buttons (TriggerBinding callbacks) or other event handlers need to know if tool switching must be deferred to `OnUpdate()`.

**Boundaries**: Out of scope — tool *behavior* after activation, input binding setup, and tool UI rendering.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Tools | ToolSystem, ToolBaseSystem |
| Colossal.UI.Binding.dll | Colossal.UI.Binding | TriggerBinding, RawTriggerBindingBase |

## Analysis

### The `activeTool` Setter

```csharp
// ToolSystem — activeTool property
public ToolBaseSystem activeTool
{
    set
    {
        if (value != m_ActiveTool)
        {
            m_ActiveTool = value;              // (1) writes field immediately
            RequireFullUpdate();                // (2) sets update flag
            EventToolChanged?.Invoke(value);    // (3) fires delegates synchronously
        }
    }
}
```

Three things happen synchronously when you set `activeTool`:

1. **`m_ActiveTool = value`** — the backing field is overwritten immediately on the calling thread
2. **`RequireFullUpdate()`** — sets an update flag (see branching analysis below)
3. **`EventToolChanged?.Invoke(value)`** — fires all subscribers synchronously in the caller's context

### The `RequireFullUpdate()` Branching

```csharp
public void RequireFullUpdate()
{
    if (m_IsUpdating)
    {
        m_FullUpdateRequired = true;    // deferred: flushed at end of OnUpdate
    }
    else
    {
        fullUpdateRequired = true;      // immediate: written to public property
    }
}
```

`m_IsUpdating` is only `true` during `ToolSystem.OnUpdate()`:

```csharp
protected override void OnUpdate()
{
    m_ToolActionBarrier.blocked = ...;
    m_IsUpdating = true;
    m_UpdateSystem.Update(SystemUpdatePhase.PreTool);
    ToolUpdate();
    m_UpdateSystem.Update(SystemUpdatePhase.PostTool);
    fullUpdateRequired = m_FullUpdateRequired;   // flush deferred flag
    m_FullUpdateRequired = false;
    m_IsUpdating = false;
}
```

When called from a TriggerBinding callback (which runs outside `OnUpdate`), `m_IsUpdating` is `false`, so `RequireFullUpdate()` takes the `else` branch and writes `fullUpdateRequired = true` directly. The result is identical to the deferred path — either way, `fullUpdateRequired` is `true` before the next `ToolUpdate()` runs.

### How `ToolUpdate()` Detects Tool Changes

```csharp
private void ToolUpdate()
{
    ...
    if (activeTool != m_LastTool)         // checks EVERY frame
    {
        if (m_LastTool != null)
        {
            m_LastTool.Enabled = false;    // disables old tool's ECS system
            m_LastTool.Update();           // final update for cleanup
        }
        m_LastTool = activeTool;           // commits the change
    }
    ...
    if (m_LastTool != null)
    {
        m_LastTool.Enabled = true;         // enables new tool
    }
    m_UpdateSystem.Update(SystemUpdatePhase.ToolUpdate);   // runs tool's OnUpdate
    ...
}
```

`ToolUpdate()` does **not** use `fullUpdateRequired` to detect tool changes. It directly compares `activeTool != m_LastTool` every frame. Since the setter already wrote `m_ActiveTool` synchronously, `ToolUpdate()` will see the new value on its next invocation regardless of when the setter was called.

### TriggerBinding Execution Context

TriggerBinding callbacks are invoked by the Coherent (cohtml) UI engine via `view.RegisterForEvent()`:

```csharp
// RawTriggerBindingBase
public override void Attach(View attachView)
{
    ...
    m_Handle = attachView.RegisterForEvent(base.path, new Action(BaseCallback));
}

// TriggerBinding
private void Callback()
{
    try { m_Callback(); }
    catch (Exception exception) { ... }
}
```

The cohtml engine processes UI events on the **Unity main thread** during its own update tick, which occurs outside the ECS `ToolSystem.OnUpdate()` phase. This means:

- `m_IsUpdating` is `false` → `RequireFullUpdate()` takes the direct path
- No concurrent thread access → no race conditions (CS2 is single-threaded)
- Full `EntityManager` access is available (not inside a Burst job)

### Vanilla Precedent

`ToolSystem.ActivatePrefabTool()` sets `activeTool` directly and is called from various non-`ToolUpdate` contexts (input callbacks, UI events):

```csharp
public bool ActivatePrefabTool(PrefabBase prefab)
{
    ...
    activeTool = tool;   // same setter, called from outside ToolUpdate
    return true;
}
```

This confirms Colossal Order considers setting `activeTool` from outside `ToolUpdate` to be the normal usage pattern.

### `toolID` Spoofing for Custom Sub-Tools

Custom tools can return another tool's `toolID` to appear as that tool to the game's UI system. This is useful when a mod provides alternative behavior for an existing tool category. The game's UI checks `activeTool.toolID` to decide which panels, overlays, and toolbar states to show.

```csharp
// BetterBulldozer pattern: custom tool impersonates the vanilla bulldozer
public class SubElementBulldozerTool : ToolBaseSystem
{
    private BulldozeToolSystem m_BulldozeToolSystem;

    // Return the vanilla bulldozer's toolID so the UI shows the bulldozer
    // panel, toolbar highlight, etc. as if the vanilla tool were active.
    public override string toolID => m_BulldozeToolSystem.toolID;

    // ...custom raycast and behavior logic...
}
```

This pattern is used by BetterBulldozer to provide sub-element bulldozing and vehicle/citizen removal while keeping the bulldozer UI active.

### `ToolSystem.tools` — Mutable Tool Registration List

`ToolSystem.tools` is a `List<ToolBaseSystem>` that is **mutable at runtime**. The game populates it with vanilla tools, and mods can add their own tools to it. The list's iteration order matters for `ActivatePrefabTool()`, which iterates `tools` and calls `TrySetPrefab()` on each — the first tool that returns `true` wins.

```csharp
// ToolSystem field
public List<ToolBaseSystem> tools;

// ActivatePrefabTool iterates the list
public bool ActivatePrefabTool(PrefabBase prefab)
{
    foreach (ToolBaseSystem tool in tools)
    {
        if (tool.TrySetPrefab(prefab))
        {
            activeTool = tool;
            return true;
        }
    }
    return false;
}
```

Mods that want their tool to handle certain prefabs (before vanilla tools) should insert at the beginning of the list. Mods that add fallback behavior can append to the end.

### `EventPrefabChanged` and `activePrefab`

`ToolSystem` exposes `activePrefab` (read-only, returns `activeTool.GetPrefab()`) and fires `EventPrefabChanged` when the prefab changes. Mods can subscribe to this event to react to prefab selection:

```csharp
// ToolSystem events
public event Action<ToolBaseSystem> EventToolChanged;
public event Action<PrefabBase> EventPrefabChanged;

// activePrefab property
public PrefabBase activePrefab => activeTool?.GetPrefab();
```

`EventPrefabChanged` fires during `ToolUpdate()` when the current tool's `GetPrefab()` returns a different value than last frame. Both `EventToolChanged` and `EventPrefabChanged` subscribers may do more than UI updates — Anarchy's systems check `activeTool.toolID` and `activePrefab` in these handlers to conditionally enable/disable features.

## Verdict

**Setting `ToolSystem.activeTool` from a TriggerBinding callback is safe. No deferral pattern is needed.**

| Concern | Risk | Why |
|---------|------|-----|
| Thread safety of `m_ActiveTool` write | None | CS2 is single-threaded. TriggerBinding callbacks fire on the Unity main thread. |
| `RequireFullUpdate()` with `m_IsUpdating = false` | None | Takes the `else` branch, writes directly. Same net result as the deferred path. |
| `ToolUpdate()` missing the change | None | Checks `activeTool != m_LastTool` every frame; the change is visible immediately. |
| `EventToolChanged` subscribers running outside ToolUpdate | Low | Subscribers are typically UI systems doing panel updates. No ECS phase dependency. |
| ECS structural changes from subscribers | Conditional | Safe as long as no subscriber performs structural changes while a job is scheduled. Same constraint applies everywhere in Unity ECS. |

### What happens step by step

1. TriggerBinding callback fires on the Unity main thread (cohtml UI event processing)
2. `m_ActiveTool = newTool` — field written immediately
3. `RequireFullUpdate()` — `m_IsUpdating` is `false` → `fullUpdateRequired = true` set directly
4. `EventToolChanged?.Invoke(newTool)` — UI subscribers update synchronously
5. On the next ECS frame, `ToolSystem.OnUpdate()` → `ToolUpdate()` sees `activeTool != m_LastTool`
6. Old tool is disabled (`Enabled = false`, final `Update()`), new tool is enabled (`Enabled = true`)
7. `SystemUpdatePhase.ToolUpdate` runs the new tool's `OnUpdate()`

### Recommendation for mods

Set `activeTool` directly from TriggerBinding callbacks:

```csharp
// In UISystemBase.OnCreate():
AddBinding(new TriggerBinding(kGroup, "ActivateMyTool", () =>
{
    var toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
    toolSystem.activeTool = World.GetOrCreateSystemManaged<MyCustomToolSystem>();
}));
```

No need for flag-based deferral. This matches vanilla patterns.

## Examples

### Example 1: Activate a Custom Tool from a UI Button

The most common pattern -- a TriggerBinding callback that switches the active tool when the user clicks a button in your mod's UI panel.

```csharp
using Game.Tools;
using Game.UI;
using Colossal.UI.Binding;

/// <summary>
/// UI system that exposes a trigger to activate a custom tool.
/// The TriggerBinding callback fires on the Unity main thread (via cohtml),
/// outside ToolSystem.OnUpdate(). Setting activeTool here is safe because:
///   - m_ActiveTool is written immediately (no deferred queue)
///   - ToolUpdate() detects the change via activeTool != m_LastTool on the next frame
///   - This matches the vanilla ActivatePrefabTool() pattern
/// </summary>
public partial class MyToolUISystem : UISystemBase
{
    private const string kGroup = "myMod";

    private ToolSystem _toolSystem;
    private MyCustomToolSystem _myTool;

    protected override void OnCreate()
    {
        base.OnCreate();

        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
        _myTool = World.GetOrCreateSystemManaged<MyCustomToolSystem>();

        // When the UI fires "ActivateMyTool", set the active tool directly.
        // No flag-based deferral is needed.
        AddBinding(new TriggerBinding(kGroup, "ActivateMyTool", () =>
        {
            _toolSystem.activeTool = _myTool;
        }));
    }
}
```

### Example 2: Toggle Between a Custom Tool and the Default Tool

A toggle pattern that switches to your tool if it is not active, or back to the default tool if it is already active.

```csharp
using Game.Tools;
using Game.UI;
using Colossal.UI.Binding;

public partial class ToggleToolUISystem : UISystemBase
{
    private const string kGroup = "myMod";

    private ToolSystem _toolSystem;
    private MyCustomToolSystem _myTool;
    private DefaultToolSystem _defaultTool;

    protected override void OnCreate()
    {
        base.OnCreate();

        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
        _myTool = World.GetOrCreateSystemManaged<MyCustomToolSystem>();
        _defaultTool = World.GetOrCreateSystemManaged<DefaultToolSystem>();

        AddBinding(new TriggerBinding(kGroup, "ToggleMyTool", () =>
        {
            // activeTool getter returns m_ActiveTool, so this comparison
            // is always up to date even if another callback changed it
            // earlier in the same frame.
            if (_toolSystem.activeTool == _myTool)
            {
                // Switch back to the default tool (same as pressing Escape in-game)
                _toolSystem.activeTool = _defaultTool;
            }
            else
            {
                _toolSystem.activeTool = _myTool;
            }
        }));
    }
}
```

### Example 3: Listen for Tool Changes via EventToolChanged

Subscribe to `EventToolChanged` to update your mod's UI state whenever any tool becomes active -- including changes made by other mods or by the base game.

```csharp
using Game.Tools;
using Game.UI;
using Colossal.UI.Binding;

public partial class ToolStateUISystem : UISystemBase
{
    private const string kGroup = "myMod";

    private ToolSystem _toolSystem;
    private MyCustomToolSystem _myTool;
    private ValueBinding<bool> _isMyToolActiveBinding;

    protected override void OnCreate()
    {
        base.OnCreate();

        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
        _myTool = World.GetOrCreateSystemManaged<MyCustomToolSystem>();

        // Expose a boolean to the UI so it can highlight the tool button
        _isMyToolActiveBinding = new ValueBinding<bool>(kGroup, "IsMyToolActive", false);
        AddBinding(_isMyToolActiveBinding);

        // EventToolChanged fires synchronously inside the activeTool setter,
        // on the same thread that set the tool. This is safe for UI updates.
        _toolSystem.EventToolChanged += OnToolChanged;
    }

    private void OnToolChanged(ToolBaseSystem newTool)
    {
        // newTool is the tool that was just assigned to activeTool.
        // Update the binding so the UI reflects the current state.
        _isMyToolActiveBinding.Update(newTool == _myTool);
    }

    protected override void OnDestroy()
    {
        _toolSystem.EventToolChanged -= OnToolChanged;
        base.OnDestroy();
    }
}
```

### Example 4: Activate a Tool with a Prefab (Vanilla Pattern)

Use `ActivatePrefabTool()` when you want the game to find the right tool for a given prefab. This is how the vanilla UI activates tools for roads, buildings, etc.

```csharp
using Game.Prefabs;
using Game.Tools;

/// <summary>
/// Demonstrates the vanilla ActivatePrefabTool() pattern.
/// Internally, this iterates over all registered ToolBaseSystem instances,
/// calls TrySetPrefab() on each, and sets activeTool to the first one that
/// accepts the prefab. If none accept it, activeTool is set to DefaultToolSystem.
/// </summary>
public void ActivateToolForPrefab(ToolSystem toolSystem, PrefabBase prefab)
{
    // Returns true if a tool accepted the prefab, false if it fell back to default.
    // The activeTool setter fires synchronously inside this method -- by the time
    // it returns, m_ActiveTool is already updated and EventToolChanged has fired.
    bool activated = toolSystem.ActivatePrefabTool(prefab);

    if (!activated)
    {
        Mod.Log.Info($"No tool accepted prefab '{prefab.name}', fell back to default tool.");
    }
}
```

### Example 5: Guard Pattern for ActivatePrefabTool (FindIt Pattern)

When activating a prefab from mod UI, always check `activePrefab` first to avoid redundant tool switches. Use a flag to distinguish mod-initiated prefab changes from user-initiated ones:

```csharp
private bool _settingPrefab;

internal void TryActivatePrefabTool(PrefabBase prefab)
{
    // Guard: don't re-activate if already selected
    if (prefab != null && _toolSystem.activePrefab != prefab)
    {
        _settingPrefab = true;
        _toolSystem.ActivatePrefabTool(prefab);
        _settingPrefab = false;
    }
}

// In EventPrefabChanged handler:
private void OnPrefabChanged(PrefabBase prefab)
{
    if (_settingPrefab) return; // We initiated this, ignore
    // User changed prefab via vanilla UI -- update mod state
}
```

Key patterns:
- `activePrefab` returns `activeTool?.GetPrefab()` -- use it for comparison before calling `ActivatePrefabTool`
- `ActivatePrefabTool` returns `false` if no tool accepted the prefab -- caller may need fallback behavior
- The `_settingPrefab` flag pattern lets the mod distinguish its own prefab changes from user actions

### Example 6: Reactivating a Custom Tool on Prefab Change

Subscribe to `ToolSystem.EventPrefabChanged` to reactivate a custom tool when the user selects a different prefab via the toolbar. Use a deferred flag rather than setting `activeTool` directly inside the event handler to avoid re-entrant tool switching during the event dispatch:

```csharp
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

public partial class CustomPlacementToolSystem : ToolBaseSystem
{
    public override string toolID => "Custom Placement Tool";

    private ToolSystem _toolSystem;
    private bool _reactivateNextFrame;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

        // Subscribe to prefab changes — fires when user picks a different
        // prefab in the toolbar while any tool is active.
        _toolSystem.EventPrefabChanged += OnPrefabChanged;
    }

    private void OnPrefabChanged(PrefabBase newPrefab)
    {
        // Only react if our tool was active but lost focus due to prefab change
        // (e.g., ActivatePrefabTool switched to ObjectToolSystem).
        if (_toolSystem.activeTool != this && newPrefab != null)
        {
            // Don't set activeTool here — we're inside the event dispatch.
            // Setting activeTool would fire EventToolChanged while
            // EventPrefabChanged is still propagating, which can confuse
            // other subscribers. Instead, defer to next frame.
            _reactivateNextFrame = true;
        }
    }

    [Preserve]
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (_reactivateNextFrame)
        {
            _reactivateNextFrame = false;
            // Now safe to switch — we're in ToolUpdate phase.
            _toolSystem.activeTool = this;
        }

        // ... normal tool logic ...
        return inputDeps;
    }

    public override PrefabBase GetPrefab() => _toolSystem.activePrefab;
    public override bool TrySetPrefab(PrefabBase prefab) => false;

    [Preserve]
    protected override void OnDestroy()
    {
        _toolSystem.EventPrefabChanged -= OnPrefabChanged;
        base.OnDestroy();
    }
}
```

**Key pattern**: Setting `activeTool` inside `EventPrefabChanged` is technically safe (single-threaded), but causes re-entrant event dispatch -- `EventToolChanged` fires while `EventPrefabChanged` subscribers are still running. The deferred flag avoids this by waiting until the next `OnUpdate`. This pattern is used by mods that replace vanilla placement tools but need to stay active when the user browses different prefabs.

### Example 7: ToolbarUISystem.Apply Postfix for Theme Tracking

Patch `ToolbarUISystem.Apply` to detect toolbar theme/category changes for multi-select support:

```csharp
[HarmonyPatch(typeof(Game.UI.InGame.ToolbarUISystem), "Apply")]
public static class ToolbarApplyPatch
{
    // Apply signature: void Apply(List<Entity> themes, List<Entity> packs,
    //     Entity assetMenuEntity, Entity assetCategoryEntity,
    //     Entity assetEntity, bool updateTool = false)
    static void Postfix(List<Entity> themes, Entity assetEntity)
    {
        // Track theme changes
        if (themes.Count != _lastThemeCount)
        {
            _lastThemeCount = themes.Count;
            UpdateSelectionSet = true; // flag for deferred UI update
        }
        // assetEntity is the currently selected asset in the toolbar
    }
}
```

## ToolSystem.EventToolChanged

Subscribe to `ToolSystem.EventToolChanged` for systems that should only run when a specific tool is active. This is more efficient than checking tool state every frame:

```csharp
protected override void OnCreate()
{
    base.OnCreate();
    Enabled = false;  // Start disabled

    m_ToolSystem.EventToolChanged += (ToolBaseSystem tool) =>
        Enabled = tool == m_ObjectToolSystem
               || (tool.toolID != null && tool.toolID == "Line Tool");
}
```

The system's `Enabled` property is toggled by the event, so `OnUpdate` is never called when irrelevant tools are active.

## EntityCommandBuffer Barrier Selection

When a system needs to add/remove components during `OnUpdate`, use the barrier that matches the system's update phase:

| Barrier | When to Use |
|---------|-------------|
| `EndFrameBarrier` | Simulation-phase systems (GameSimulation, ModificationEnd) |
| `ToolOutputBarrier` | Tool-phase systems (ToolUpdate) |

```csharp
// In a tool system (ToolUpdate phase):
private ToolOutputBarrier _barrier;
protected override void OnCreate()
{
    _barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
}
protected override JobHandle OnUpdate(JobHandle inputDeps)
{
    var buffer = _barrier.CreateCommandBuffer();
    buffer.AddComponent<Highlighted>(entity);
    buffer.AddComponent<BatchesUpdated>(entity);
    // ...
}
```

For parallel jobs, use `CreateCommandBuffer().AsParallelWriter()` and call `barrier.AddJobHandleForProducer(jobHandle)`.

## ModificationBarrier Pattern

Barrier systems synchronize `EntityCommandBuffer` playback at update phase boundaries. Different barriers correspond to different phases:

| Barrier | Phase | Use When |
|---------|-------|----------|
| `ModificationBarrier4B` | Modification4B | Systems modifying entities during Modification4 |
| `ModificationBarrier5` | Modification5 | Systems running in Modification5 |
| `ToolOutputBarrier` | After tool phases | Tool systems creating/modifying entities |
| `EndFrameBarrier` | End of frame | General-purpose deferred commands |

**Pattern**:
```csharp
private ModificationBarrier4B m_Barrier;

protected override void OnCreate()
{
    base.OnCreate();
    m_Barrier = World.GetOrCreateSystemManaged<ModificationBarrier4B>();
}

protected override void OnUpdate()
{
    var ecb = m_Barrier.CreateCommandBuffer().AsParallelWriter();
    // Schedule job using ecb...
    m_Barrier.AddJobHandleForProducer(Dependency);
}
```

**Key rule**: Use the barrier matching your system's update phase. If your system runs in Modification4, use `ModificationBarrier4B` (or `ModificationBarrier5` if you need commands to execute after Modification4B). For tool systems, use `ToolOutputBarrier`.

## OverlayRenderSystem for Custom Overlays

Register a system at `SystemUpdatePhase.Rendering` and use `OverlayRenderSystem.GetBuffer()` for drawing:

```csharp
public partial class CustomOverlaySystem : GameSystemBase
{
    private OverlayRenderSystem m_OverlayRenderSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
    }

    protected override void OnUpdate()
    {
        var buffer = m_OverlayRenderSystem.GetBuffer(out var dependencies);
        dependencies.Complete();

        // Draw shapes
        buffer.DrawCircle(color, outlineColor, 0f, styleFlags,
            new float2(0f, 1f), position, radius);
        buffer.DrawLine(color, outlineColor, 0f, styleFlags,
            new Line3.Segment(start, end), width, 1f);
        buffer.DrawDashedLine(color, outlineColor, 0f, styleFlags,
            line, width, dashLength, gapLength);

        m_OverlayRenderSystem.AddBufferWriter(Dependency);
    }
}
```

**Key details**: Use `RenderingSettingsData` singleton for game-consistent colors. For performance, use `[BurstCompile]` IJob with `Allocator.TempJob`. Register at `SystemUpdatePhase.Rendering`.

## Custom ToolBaseSystem (Picker/Eyedropper)

Complete pattern for a custom `ToolBaseSystem` with raycast configuration:

```csharp
public partial class PickerToolSystem : ToolBaseSystem
{
    public override string toolID => "Picker Tool";

    private ToolOutputBarrier m_ToolOutputBarrier;
    private ProxyAction m_ApplyAction;
    private Entity m_HighlightedEntity;

    public override void InitializeRaycast()
    {
        base.InitializeRaycast();
        m_ToolRaycastSystem.collisionMask = CollisionMask.Overground
            | CollisionMask.OnGround | CollisionMask.Underground;
        m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects | TypeMask.Net;
        m_ToolRaycastSystem.netLayerMask = Layer.All;
        m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
    }

    protected override void OnUpdate()
    {
        // Get raycast hit
        if (GetRaycastResult(out Entity hit, out RaycastHit _))
        {
            // Traverse Owner hierarchy to get root entity
            while (EntityManager.TryGetComponent<Owner>(hit, out var owner))
                hit = owner.m_Owner;

            // Highlight via EntityCommandBuffer
            if (hit != m_HighlightedEntity)
            {
                var buffer = m_ToolOutputBarrier.CreateCommandBuffer();
                if (m_HighlightedEntity != Entity.Null)
                {
                    buffer.RemoveComponent<Highlighted>(m_HighlightedEntity);
                    buffer.AddComponent<BatchesUpdated>(m_HighlightedEntity);
                }
                buffer.AddComponent<Highlighted>(hit);
                buffer.AddComponent<BatchesUpdated>(hit);
                m_HighlightedEntity = hit;
            }
        }
    }
}
```

**Key elements**: Custom `toolID` string, `InitializeRaycast()` override for configuring collision/type/layer masks, `Owner` traversal for root entity, `Highlighted + BatchesUpdated` for visual feedback, `ToolOutputBarrier` for deferred commands, `ProxyAction` for custom input binding.

## Open Questions

- [x] Is `activeTool` safe to set from TriggerBinding callbacks? — Yes, fully safe
- [x] Does `RequireFullUpdate()` behave differently outside OnUpdate? — Different branch but same outcome
- [x] Does `ToolUpdate()` rely on any flag to detect tool changes? — No, it checks `activeTool != m_LastTool` directly
- [x] Are EventToolChanged subscribers phase-dependent? — No, they are UI-focused

## Sources

- Decompiled from: Game.dll → `Game.Tools.ToolSystem`, Colossal.UI.Binding.dll → `TriggerBinding`, `RawTriggerBindingBase`
- Related research: `research/topics/ToolRaycast/` (ToolSystem architecture), `research/topics/ModUIButtons/` (TriggerBinding usage)
- Game version: Current as of 2026-02-15
