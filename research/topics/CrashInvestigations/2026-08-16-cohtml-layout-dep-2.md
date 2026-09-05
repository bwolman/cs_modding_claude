# 2026-08-16 — Cohtml layout DEP (repeat, short session)

> **Status**: Closed — same class as `ce956c0b`
> **Date**: 2026-08-16
> **Dump**: `ad178da9-d794-4742-9d3e-11ba1496396d.dmp`

Local copies: `research/topics/CrashInvestigations/evidence/2026-08-16-ad178da9/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml `WT_Layout`. `0xC0000005` DEP. RIP unmapped on the stack. |
| Exact root cause | Same pattern as [2026-08-16-cohtml-layout-dep.md](2026-08-16-cohtml-layout-dep.md) (`ce956c0b`, ~17 min earlier). |

## Match to `ce956c0b`

| Field | `ce956c0b` (~14:41) | `ad178da9` (~14:58) |
|-------|---------------------|---------------------|
| Managed stack | `TaskScheduler` `b__5_0` → `Library_ExecuteWork__SWIG_1` | identical (same IL offsets) |
| Exception | `0xC0000005` DEP (`info0=8`) | identical |
| Thread | `0x5314` | `0x2e90` |
| RIP | `0xeb0e5febb0` | `0x74f923e9b0` |
| RSP | `0xeb0e5fdc38` | `0x74f923da38` |
| RIP-RSP | `0xf78` (3960 bytes), adjacent 4 KiB pages | **identical `0xf78`** |
| RIP page offset | `0xebb0` | `0xe9b0` (shift `0x200`) |
| RSP page offset | `0xdc38` | `0xda38` (shift `0x200`) |
| First Cohtml offsets | `+0x4050aa`, `+0x40a03f`, `+0x40629a` | identical |
| Later Cohtml offsets | `+0x40b9c9`, `+0x4061ec`, `+0x40b7f7`, `+0x406a6d` | `+0x40ba69`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d`, `+0x47fa00` |
| Other native module on scan | `RenoirCore.WindowsDesktop.dll+0xb4e4` | none listed |
| Immediately before (`Player.log`) | main-thread `View_Advance` frames, unused-asset unload, **1** `TravelPurpose 10` | **no** `View_Advance` frames, **no** unload, **44** extra-renter, **11** `TravelPurpose 10` |
| `infoviews.zoneInfos` | yes (last 14:41:01) | **absent** |
| Session | ~3 h, GPU 80→320 MB | **~4 min after reload**, GPU 80→96→112 MB |

GPU instance-buffer growth is **not** required for this crash. `infoviews.zoneInfos` is **not** required for this crash.

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404] |
| Boot | 14:49:24 (`SceneFlow`) |
| Load | Autosave `16-August-14-41-03` (14:54:20) — metadata `a8830f70cf7b730f558e04d436212000`; loading completed 14:54:45 |
| Crash | at/after 14:58:52 (last `MoreDispatchMod` tick). Dump mtime 17:58 Mac |
| MoreDispatch | No `HandleToggleTool`. No errors. `OnToolChanged → Net Tool` at 14:56:04. |

## Last ~2 minutes (not the dying thread)

| Time | Source | Fact |
|------|--------|------|
| 14:54:37 | `UI.log` | Only ERROR this session: 404 `coui://uil/Standard/Alarm.svg` (Id 188). `DefaultResourceHandler` main-thread coroutine. Same URL on `ce956c0b` at 11:50:56 (Id 173). |
| 14:54:37 | `UI.log` | Move It `MIT04` / `MIT05` (INFO). Not errors. |
| 14:55:15 | `RealisticTrips.log` | Daily chirp posted (`entity 6639583`). |
| 14:55:37 / 14:57:34 | `RealisticTrips.log` | `[RT][PersonalCarRepair]` ticks. Last RealisticTrips line 14:57:34. |
| 14:55:54–14:56:04 | `UI.log` | Repeated `loading button` / `[BetterTransitView] Button rendering...`. **Last `UI.log` line 14:56:04**. |
| 14:56:04 | `MoreDispatchMod.log` | `OnToolChanged → Net Tool, isDispatchTool=False`. |
| 14:57:37 | `MoreDispatchMod.log` | `FireAccident` `AlreadyTagged` 1→0. |
| 14:58:52 | `MoreDispatchMod.log` | Last line. Then `Player.log` extra-renter + `TravelPurpose` + `Native Crash Reporting`. |

## Same-boot dumps (not this crash)

Launch ANRs, thread `0x5138`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`: `7453835f` / `49f03485` 17:50, `eb6dd0ac` 17:54.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x2e90` |
| RIP / AV | `0x74f923e9b0` (unmapped) |
| RSP | `0x74f923da38` |
| Native stack | only `cohtml.WindowsDesktop.dll` (`+0x4050aa`, `+0x40a03f`, `+0x40629a`, `+0x40ba69`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d`, `+0x47fa00`) |

## Immediately before death

`Player.log` after post-load asset unload, no timestamps on these lines:

- **44** `Removed extra renter from building:{index}` (44 unique indexes).
- **11** `Worker … had incorrect TravelPurpose 10!`.
- Then `Native Crash Reporting`.

No main-thread `View_Advance` frames. No unused-asset unload immediately above the banner.

## Extra-renter line

Vanilla `Game.Simulation.RentAdjustSystem.AdjustRentJob` (`research/topics/LandValueProperty/snippets/RentAdjustSystem.cs`):

```
if (renters.Length > num && m_PropertyRenters.HasComponent(renters[renters.Length - 1].m_Renter))
{
    m_CommandBuffer.RemoveComponent<PropertyRenter>(…);
    renters.RemoveAt(renters.Length - 1);
    UnityEngine.Debug.LogWarning($"Removed extra renter from building:{entity.Index}");
}
```

Same job can add/remove `m_HighRentNotification` icons. **Zero** extra-renter lines in `ce956c0b` `Player.log`. Unique to this reload of `16-August-14-41-03`. Not on the layout native stack.

`TravelPurpose 10` is vanilla `Game.Simulation.WorkProviderSystem.RemoveWorker` (`research/topics/WorkplaceLaborMarket/snippets/WorkProviderSystem.cs`). Warning only when purpose is `GoingToSchool` or `Studying`. Also appears on other crash classes. Coincident.

## Coincident UI

- 14:54:37 — 404 `coui://uil/Standard/Alarm.svg` (same as prior sessions). ~4 min before death.
- `infoviews.zoneInfos` — **does not appear** on this short session (`Player.log` + `UI.log`).

## Code-UI mods loaded this boot

From this session `UI.log` registrations (14:49:50 / 14:50:41). Not claimed on the faulting stack:

`MoreDispatchMod`, `Anarchy`, `CustomChirps`, `MoveIt`, `RoadBuilder`, `Platter`, `IndustriesExtended` (`TestFieldsUISystem`), `BetterBulldozer`, `ExtraLib`, `EventsController`, `Water_Features`, `MapTextureReplacer`, `Tree_Controller`, `XTM`, `ExtraDetailingTools`, `ExtraLandscapingTools`, `BetterTransitView` (button render).

Also logged this boot (not UI-module lines): `IndustriesExtended` `OnLoad` 1.38.1, `RealisticTrips`/`Time2Work` (`77171_83`), `FirstPersonCamera loaded!`, `SchoolOrganizer`, `ClearTheWay` (non-serializable types at load). `FileSystem.log`: `Node Controller`, `Better Transit View`, `Alpine A 110` enabled.

Evidence copy has **no** `Modding.log`. Live `{UserData}/Logs/Modding.log` starts **15:02:28** (next boot). BurstRuntime module names were not re-extracted from the minidump this pass.

## Dump extras (this pass)

Exception-thread facts unchanged. Other threads' stacks and the full module list were **not** re-parsed here (`read_file` refuses the `.dmp`; no extra native module is named on the existing qword scan). Smash gadget still unnamed (`cohtml.WindowsDesktop.dll` not on the Mac mount).

## Open

Same as `ce956c0b`. Reproduced on the next load of that autosave in ~4 minutes without large GPU-buffer growth and without `infoviews.zoneInfos`. Extra-renter burst is unique to this reload; not shown to be the smash.
