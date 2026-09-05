# 2026-08-17 — BetterTransitView `TransitUISystem` ArgumentOutOfRangeException

> **Status**: Closed — root cause and fix identified
> **Date**: 2026-08-17
> **Kind**: Managed UI update error (process did not die)

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-17-transit-ui-aore/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | Yes. `BetterTransitView.Systems.TransitUISystem` (`BetterTransitView`, Version=1.5.0.0). |
| Crash site | Not a process crash. Main-thread `UIUpdate`. Throw: `List<T>.get_Item` ← `Game.Prefabs.PrefabSystem.GetPrefab<T>(PrefabData)` ← `GetPrefab<T>(PrefabRef)` ← `BetterTransitView.Systems.TransitUISystem.GetStopResolvedName` (`TransitUISystem.cs:1137`, IL `0x0019c`). |
| Exact root cause | Opening the BTV panel called `GetPrefab` on a stop-owner `Building` whose `PrefabData.m_Index` is negative (obsolete / missing pack this boot). Vanilla `GetPrefab` has no bounds check. Not a BTV load-order bug. |

`Game.dll` has no type named `TransitUISystem`. SceneFlow's `UIUpdate->TransitUISystem` is the BetterTransitView ECS system, not vanilla `Game.UI`.

The process stayed up. `Game.UpdateSystem` logged CRITICAL and continued. No `Native Crash Reporting` banner.

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404] |
| Unity | 2022.3.71f1 |
| OS / CPU / GPU | Windows 11 64-bit (10.0.26200); i9-14900KF; RTX 5080 D3D11 driver 32.0.15.7688 |
| Launch | `Cities2.exe -developerMode` |
| Boot | 01:20:22 (`SceneFlow`). MainMenu 01:23:38 |
| Save loaded | Gilbert County 47 (01:25:54). Loading completed 01:26:28 |
| Last autosave | `17-August-02-06-24` (completed 02:06:28) |
| Manual save | Gilbert County 48 completed 02:06:54 |
| First error | 02:06:58,187 (`SceneFlow`) |
| Last error | 02:07:10,697 |
| Count | **123** identical CRITICALs (`SceneFlow` and `Player.log`) |
| Shutdown | Mods `OnDispose` 02:07:10. GameManager destroyed 02:07:23. Unity allocator teardown at end of `Player.log` |
| `Player.log` | 6761 lines. No `Native Crash Reporting` |
| `Player-prev.log` | No `Native Crash Reporting`. No `TransitUISystem` |

## First exception (`SceneFlow` 02:06:58,187 / `Player.log` line 545)

```
[SceneFlow] [CRITICAL]  System update error during UIUpdate->TransitUISystem:
ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection.
Parameter name: index
  at System.Collections.Generic.List`1[T].get_Item (System.Int32 index) [0x00009] in <ec44db4194e74e18bbf336ba559460da>:0
  at Game.Prefabs.PrefabSystem.GetPrefab[T] (Game.Prefabs.PrefabData prefabData) [0x00000] in <36b776ac50074e0681b3242b2abfd5b6>:0
  at Game.Prefabs.PrefabSystem.GetPrefab[T] (Game.Prefabs.PrefabRef refData) [0x00015] in <36b776ac50074e0681b3242b2abfd5b6>:0
  at BetterTransitView.Systems.TransitUISystem.GetStopResolvedName (Unity.Entities.Entity wp, Unity.Entities.Entity stopTarget, Game.UI.NameSystem nameSystem, System.Int32 stopIndexCounter) [0x0019c] in C:\Users\Jeremy\source\repos\BetterTransitView\src\Systems\TransitUISystem.cs:1137
  at BetterTransitView.Systems.TransitUISystem.UpdateTransitLinesData () [0x00501] in C:\Users\Jeremy\source\repos\BetterTransitView\src\Systems\TransitUISystem.cs:701
  at BetterTransitView.Systems.TransitUISystem.ActivateTransitMode (System.String mode) [0x00275] in C:\Users\Jeremy\source\repos\BetterTransitView\src\Systems\TransitUISystem.cs:930
  at BetterTransitView.Systems.TransitUISystem.OnUpdate () [0x000c8] in C:\Users\Jeremy\source\repos\BetterTransitView\src\Systems\TransitUISystem.cs:382
  at Unity.Entities.SystemBase.Update () [0x0004e] in <8f03b65544074c2c8e0de9e31800fa72>:0
  at Game.UpdateSystem.Update (Game.SystemUpdatePhase phase) [0x00051] in <36b776ac50074e0681b3242b2abfd5b6>:0
Game.UpdateSystem:Update(SystemUpdatePhase)
Game.UI.UIUpdateSystem:OnUpdate()
Unity.Entities.SystemBase:Update()
Game.UpdateSystem:Update(SystemUpdatePhase)
Game.SceneFlow.GameManager:UpdateWorld()
Game.SceneFlow.GameManager:Update()
```

Assemblies: `mscorlib` (`ec44db41…`), `Game.dll` (`36b776ac…`), `BetterTransitView` (PDB path on Jeremy's machine), `Unity.Entities` (`8f03b655…`).

`Player.log` also prints the logging path (`UnityLogger.CriticalFormat` ← `UpdateSystem.Update`) and native `UnityPlayer` / `mono-2.0-bdwgc` / `KERNEL32` / `ntdll` frames for the log call. That is not a native fault.

All 123 repeats use the same type, method, source line, and IL offset.

## BetterTransitView (loaded)

| Field | Value |
|-------|-------|
| Assembly | `BetterTransitView, Version=1.5.0.0` |
| PDX cache | `.cache/Mods/pdx_mods/139595_12/` |
| DLL | `BetterTransitView.dll` |
| Burst | `BetterTransitView_win_x86_64.dll` (not on this stack) |
| Load | 01:22:07,346 |
| UI module | `BetterTransitView` 1.5, author `bash@bashsoftware.net` |
| Dispose | 02:07:10,935 |

Decompiled with `ilspycmd -t BetterTransitView.Systems.TransitUISystem` from that DLL.

`ActivateTransitMode` is only called from `OnUpdate` when `m_ModeChangeRequested` is set: hotkey toggle (`Mod.m_ToggleAction`) or UI trigger `BetterTransitView.toggleTransitCustom`. That path always calls `UpdateTransitLinesData()`.

`GetStopResolvedName` (IL `0x0019c` matches the first `GetPrefab` in this method): if the stop/waypoint has `Owner` and the owner is not a `Route`, has `Building` + `PrefabRef`, it does:

```csharp
PrefabRef componentData = EntityManager.GetComponentData<PrefabRef>(owner);
PrefabBase prefab = orCreateSystemManaged.GetPrefab<PrefabBase>(componentData);
```

Same unguarded `GetPrefab<PrefabBase>(PrefabRef)` later for `Attached.m_Parent` and for the stop entity itself. Those are later IL offsets.

## Vanilla `PrefabSystem` (`Game.dll`)

```csharp
public T GetPrefab<T>(PrefabData prefabData) where T : PrefabBase
{
    return m_Prefabs[prefabData.m_Index] as T;
}

public T GetPrefab<T>(PrefabRef refData) where T : PrefabBase
{
    return GetPrefab<T>(EntityManager.GetComponentData<PrefabData>(refData.m_Prefab));
}

public bool TryGetPrefab<T>(PrefabData prefabData, out T prefab) where T : PrefabBase
{
    if (prefabData.m_Index >= 0)
    {
        prefab = m_Prefabs[prefabData.m_Index] as T;
        return true;
    }
    prefab = null;
    return false;
}
```

`GetPrefab` has no range check. `TryGetPrefab` only rejects `m_Index < 0`; an index `>= m_Prefabs.Count` still throws.

## Immediately before first error (`Player.log`)

No timestamps on these lines. Order above the first CRITICAL:

```
IsTransitioningOut
Unloading unused Assets … Loaded Objects now: 366858
[SceneFlow] [CRITICAL] UIUpdate->TransitUISystem
```

`IsTransitioningIn` / unused-asset unload appear earlier in the same block. Coincident with save UI teardown after Gilbert County 48 (02:06:54). Not a native crash prelude: no `Native Crash Reporting` follows.

## Same-folder dumps (not this error)

Both are Backtrace ANRs. No `ArgumentOutOfRange`. No `TransitUISystem` string. No `Native Crash` string. Same class as prior launch ANRs. Do not merge with this managed exception.

| Dump | mtime (Mac) | Thread | Code | RIP |
|------|-------------|--------|------|-----|
| `7c10b3fc-dda3-410d-8f75-8c2676fa965d` | 04:23 | `0x4ab0` | `0x0517A7ED` | `BacktraceCrashpadWindows.dll+0x2dcd` |
| `2dd558fc-2796-4747-907b-c339bcc71226` | 04:26 | `0x4ab0` | `0x0517A7ED` | `BacktraceCrashpadWindows.dll+0x2dcd` |

Dump strings: `ANRException` / `Blocked thread`. `BetterTransitView` appears as a mapped module name only.

Mac mtime 04:23–04:26 ≈ game boot 01:20 (this machine's usual +3 h offset). The UI errors start 02:06:58 game.

## MoreDispatchMod

| Check | Result |
|-------|--------|
| Log | Copied. `OnLoad` 01:22:05. `OnDispose` 02:07:10,711 |
| Errors / exceptions | None |
| `HandleToggleTool` | **0** this session |
| `OnToolChanged` | 29 times, all `isDispatchTool=False`. Last 01:58:54 (`Default Tool`) — ~8 min before the first CRITICAL |
| At error | Last tick 02:06:56 FireAccident/FireMedical. Not on the exception stack |

## Other mod logs (same timestamps)

| Log | Errors |
|-----|--------|
| `Mods_K45_XTM.log` | None |
| `Traffic.Mod.log` | None |
| `ExtraLib.log` | None |
| `MoveIt.log` | None |

None of these types appear on the exception stack.

## Coincident UI (`UI.log`)

| Time | Fact |
|------|------|
| 02:06:50 | `New render target set 680x383` then `1920x1080` (save dialog / window) |
| 02:06:54–02:06:56 | `loading button` / `[BetterTransitView] Button rendering...` |
| 02:06:58,201 | `[BetterTransitView] Button rendering...` (14 ms after first CRITICAL) |
| Last line | 02:07:07,432 `[BetterTransitView] Button rendering...` |

No 404 / host-map lines at the error. Button-render INFO continues while `SceneFlow` is logging the same exception.

## Load-time unknown prefabs (`SceneFlow` 01:26:01)

```
Unknown prefab ID: BuildingPrefab:W7MiniBusStation
Unknown prefab ID: ContentPrefab:Mod:81040
Unknown prefab ID: ContentPrefab:Mod:80942
Unknown prefab ID: BuildingPrefab:W7OldCemetery
Unknown prefab ID: StaticObjectPrefab:W7EU_BusStop02
Unknown prefab ID: CarTrailerPrefab:Abschlepphaenger
```

`W7EU_BusStop02` / `W7MiniBusStation` are transit-related missing prefabs at load (~41 min before the exception). Not shown to be the `PrefabRef` passed into `GetPrefab`. Not on the exception stack.

## Root cause

`GetStopResolvedName` (shipped 1.5.0.0, source [kurupted/CS2TransitView](https://github.com/kurupted/CS2TransitView) `TransitUISystem.cs:1137`) does this after `HasComponent<Building>` + `HasComponent<PrefabRef>` only — no `TryGetPrefab`:

```csharp
var pRef = EntityManager.GetComponentData<PrefabRef>(owner);
var ownerPrefab = prefabSystem.GetPrefab<PrefabBase>(pRef);
```

Vanilla `PrefabSystem.GetPrefab` is `return m_Prefabs[prefabData.m_Index] as T` with no range check. On save load, `ResolvePrefabsSystem` remaps every prefab the save names but this boot did not register: `m_Index = -1 - n`, then `AddObsoleteID` (the `Unknown prefab ID:` lines). `TryGetPrefab` rejects `m_Index < 0`. `GetPrefab` indexes `m_Prefabs[-1]` and throws.

This boot’s unknown IDs include transit assets from **More Transport (OLD)** PDX **81040** (`ContentPrefab:Mod:81040`, `BuildingPrefab:W7MiniBusStation`, `StaticObjectPrefab:W7EU_BusStop02`, shared GUID `28dd14c3…`). The playset has **More Transport (NEW)** `150791` instead. NEW does not register those old IDs. BTV has no `MissedDependencies` on that pack; it is not a load-order failure.

The first CRITICAL is `ActivateTransitMode` → `UpdateTransitLinesData` (source only ever opens mode `"custom"`). That runs when the panel is toggled (hotkey or `toggleTransitCustom`). Same missing W7 IDs appear on 2026-08-16 sessions that did **not** throw — those logs show BTV button-render only, not this `ActivateTransitMode` stack.

The owner entity is not logged. The only unknown **BuildingPrefab**s this boot are `W7MiniBusStation` (transit) and `W7OldCemetery` (from **More Deathcare (OLD)** `80942`, also replaced by NEW `150346`). Line 1137 is the owner-`Building` path; `W7MiniBusStation` is the matching transit building.

## Fix

**Code (smallest, correct):** in BTV `GetStopResolvedName`, replace every `GetPrefab<PrefabBase>(pRef)` (source 1137, 1175, 1215) with `TryGetPrefab`. On false, skip to the next strategy; the method already falls back to `"Bus Stop #n"`. Same change for `GetPrefab<TransportLinePrefab>` in `UpdateTransitLinesData` (592 / 720 / 823) so a missing line prefab cannot throw later. Upstream `main` still uses `GetPrefab` (no fix as of this check).

`TryGetPrefab` is enough here: obsolete indices are negative. A try/catch around the name resolver is a belt-and-suspenders, not required if `TryGetPrefab` is used.

**Playset (workaround, no BTV rebuild):** enable **More Transport (OLD)** PDX [81040](https://mods.paradoxplaza.com/mods/81040/Windows) so `W7MiniBusStation` / `W7EU_BusStop02` resolve. NEW `150791` does not substitute those IDs. Optionally also OLD **More Deathcare** `80942` for `W7OldCemetery`. Replacing the placed mini-bus stations with NEW-pack equivalents also works. Do not treat this as a BTV-vs-XTM load-order issue.

## Open

- The throwing owner entity is not in the log; `W7MiniBusStation` is inferred from the unknown-BuildingPrefab list + the 1137 owner-Building path.
- Whether OLD+NEW More Transport together emit `Duplicate prefab ID` is not tested.
