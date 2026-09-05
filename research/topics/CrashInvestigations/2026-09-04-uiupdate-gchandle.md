# 2026-09-04 — UIUpdate `GCHandle.GetTarget` native crash

> **Status**: Closed — attribution complete; native RIP not in a dump
> **Date**: 2026-09-04
> **Dump**: none for this death. Session copy: `research/topics/CrashInvestigations/evidence/2026-09-04-7b6af8a1/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. Frames are mscorlib `GCHandle`, Unity.Entities `ComponentSystemBase`/`SystemBase`, `Game.UpdateSystem`, `Game.UI.UIUpdateSystem`, `Game.SceneFlow.GameManager`. |
| Crash site | Main thread. Native icall `GCHandle.GetTarget` while `SystemBase.Update` of some `SystemUpdatePhase.UIUpdate` system read `ComponentSystemBase.World` (`m_StatePtr->m_World.Target`). |
| Exact root cause | Not determined. No Crashpad dump of this native crash. Child system type is not on the managed stack. Handle looked allocated (`IsAllocated` would have thrown managed and been caught). |

Not Cohtml `ExecuteWork` (`b__5_0` / `b__8_0`). Not empty-stack Burst/GC.

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Cohtml 1.64.0.7. `disableCodeModding: false`. `-developerMode`. |
| Boot | SceneFlow 18:34:58. Boot completed 18:37:13. MainMenu 18:37:16. |
| Save loaded | `04-September-18-32-12` 18:37:46 (last autosave from the [layout DEP](2026-09-04-cohtml-layout-dep.md)). Loading completed 18:38:16. Format tags include `NewWaterSources`. |
| Last autosave | `04-September-19-36-14` completed 19:36:17. |
| Crash | MoreDispatch last tick 19:36:48. `Player.log` mtime Mac 22:36. ~58 min after load. |

Crashed session is live `Player.log`. Copied before rotate.

## Same-boot dumps (not this crash)

All `0x0517A7ED`, thread `0x5070`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, **identical RSP** `0xbb6cddee20`. ANR strings present. No `Native Crash Reporting` / `GCHandle` in dump strings.

| Dump | mtime Mac | BurstRuntime | Notes |
|------|-----------|--------------|-------|
| `2ca5d50a-83f5-4bff-b088-f09baccaa008` | 21:36 | 0 | Boot (ECS init ~18:36). |
| `153c2bfa-e11f-404b-8e53-9db9a577ad43` | 21:37 | 55 | Boot. |
| `7b6af8a1-2a69-4c37-b10e-c2cfada1253c` | 21:37 | 55 | Larger file; same ANR as the other two. |

Do not merge with this `Native Crash Reporting` block, or with earlier-today Cohtml dumps (`13872449`, `9455b60c`, `3d3320c4`).

## Not the Cohtml classes

| Field | Layout / resources `ExecuteWork` | This |
|-------|----------------------------------|------|
| Thread | Cohtml `WT_Layout` (`b__5_0`) or `WT_Resources` (`b__8_0`) | Main (`GameManager.Update`) |
| Managed top | `Library_ExecuteWork__SWIG_1` | `GCHandle.GetTarget` |
| Dump | native `0xC000001D` / DEP / `v8` read | **none** |

## Immediately before death (`Player.log`)

Unused-asset unloads (Loaded Objects ~370621). `TravelPurpose 10` × several. Then `Native Crash Reporting`.

GPU 80→**320** MB (this session). Coincident.

`[UI] EventsController.TRIGGER:toggleCollapseTool` / `toggleFireTool` “2 arguments, expected 1” + `index.js:1:3011` (two lines). Main-thread JS. Coincident.

404 `coui://uil/Standard/Police.svg` (Id 1060). `DefaultResourceHandler`. Coincident.

No `stroke-dasharray` spam this log. No `System update error during UIUpdate`.

`UI.log` last 19:36:17: autosave preview 680×383 then 1920×1080; BetterTransitView button. Ticking until death ≠ stack hit.

## Managed stack (dying thread)

```
at <unknown> <0xffffffff>
at System.Runtime.InteropServices.GCHandle:GetTarget <0x00069>
at System.Runtime.InteropServices.GCHandle:get_Target <0x0003a>
at Unity.Entities.ComponentSystemBase:get_World <0x0003a>
at Unity.Entities.SystemBase:Update <0x000da>
at Game.UpdateSystem:Update <0x00183>
at Game.UI.UIUpdateSystem:OnUpdate <0x0002a>
at Unity.Entities.SystemBase:Update <0x001ca>
at Game.UpdateSystem:Update <0x00183>
at Game.SceneFlow.GameManager:UpdateWorld <0x00062>
at Game.SceneFlow.GameManager:Update <0x000f2>
at System.Object:runtime_invoke_void__this__ <0x00087>
```

`Got a UNKNOWN while executing native code.`

## Decompiled path (`lib/Game.dll`, `lib/Unity.Entities.dll`, `lib/mscorlib.dll`)

`UIUpdateSystem.OnUpdate` is one call: `m_UpdateSystem.Update(SystemUpdatePhase.UIUpdate)` (IL `0x0002a`).

`UpdateSystem.Update(phase)` walks `m_Updates` for that phase and calls `systemData.m_System.Update()`. `catch (Exception)` logs `System update error during {phase}->{type}` — a native AV is not that path. No such line in this `Player.log`.

`SystemBase.Update` (IL `0x000da` on the inner frame): after `CheckedState()` / `Enabled` / `ShouldRunSystem()`, reads `base.World.Unmanaged`.

`ComponentSystemBase.World` (IL `0x0003a`): `m_StatePtr != null` then `(World)m_StatePtr->m_World.Target`. `SystemState.m_World` is a `GCHandle`.

`GCHandle.get_Target`: if `!IsAllocated` throw `InvalidOperationException`; else `GetTarget(handle)` (`[MethodImpl(InternalCall)]`). Stack is inside `GetTarget` (`0x00069`), so the handle looked allocated.

The child system’s managed type never appears. Any `UIUpdate` registrant (vanilla or mod) could have been current.

## MoreDispatchMod

Last `HandleToggleTool` 19:17:01 (dispatch on, then Default 19:17:04). Last tool Default 19:24:44 (after Selection Tool 19:24:38). Last ticks 19:36:48. ~12 min Default Tool with autosaves still running. Not on the managed stack.

## Open

- RIP module: no dump of this native crash (Crashpad only has the three boot ANRs).
- Which `UIUpdate` system was `m_Updates[i]` when `get_World` ran.
- Why `m_StatePtr->m_World` was allocated but `GetTarget` died (corrupt handle / collected `World` / Mono GC table).
- Intact autosave: `04-September-19-36-14`.
