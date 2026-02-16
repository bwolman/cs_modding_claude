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

## Open Questions

- [x] Is `activeTool` safe to set from TriggerBinding callbacks? — Yes, fully safe
- [x] Does `RequireFullUpdate()` behave differently outside OnUpdate? — Different branch but same outcome
- [x] Does `ToolUpdate()` rely on any flag to detect tool changes? — No, it checks `activeTool != m_LastTool` directly
- [x] Are EventToolChanged subscribers phase-dependent? — No, they are UI-focused

## Sources

- Decompiled from: Game.dll → `Game.Tools.ToolSystem`, Colossal.UI.Binding.dll → `TriggerBinding`, `RawTriggerBindingBase`
- Related research: `research/topics/ToolRaycast/` (ToolSystem architecture), `research/topics/ModUIButtons/` (TriggerBinding usage)
- Game version: Current as of 2026-02-15
