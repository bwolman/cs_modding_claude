# 2026-09-04 — Cohtml layout `ExecuteWork` (`cohtml+0x404bb9` illegal instruction)

> **Status**: Closed — attribution complete; Cohtml-internal cause not named
> **Date**: 2026-09-04
> **Dump**: `13872449-e667-47c3-917c-079cd3a0bdba.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-09-04-13872449/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml layout thread (`TaskScheduler` `b__5_0` / `WT_Layout`). RIP in `cohtml.WindowsDesktop.dll+0x404bb9`. Exception `0xC000001D` (`ILLEGAL_INSTRUCTION`). |
| Exact root cause | Not determined. Same managed path as [2026-08-13](2026-08-13-cohtml-layout-execute-work.md) / [08-21](2026-08-21-cohtml-layout-execute-work.md) / [08-29](2026-08-29-cohtml-layout-execute-work.md) ExecuteWork. RIP is **in Cohtml**, not `VCRUNTIME140+0xdf8b`. Offset `+0x404bb9` is 445 bytes before the usual stack gadget `+0x404d76`. |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Unity 2022.3.71f1 |
| Flag | `disableCodeModding: false`. Playset Current (5892750). |
| Boot | SceneFlow 12:28:17. |
| First city | `Cumberland County 4` 12:34:08–16:00. Manual `Cumberland County 5` 15:59:57. |
| Gilbert load | `Gilbert County 56` 16:00:06. Loading completed 16:00:33. Format tag `NewWaterSources`. |
| Named save | `Gilbert County 57` 16:42:02 (284 729 483 bytes, completed). |
| Last autosave | `04-September-16-55-29` 16:55:32 (285 747 967 bytes, completed). |
| Crash | MoreDispatch last tick 16:58:17. Dump mtime Mac 19:58. ~58 min after Gilbert load. |

Crashed session is live `Player.log`. Copied before rotate.

## Same-day dumps (not this crash)

Launch / load ANRs, thread `0x4f94`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, `0x0517A7ED`:

| Dump | mtime Mac | Notes |
|------|-----------|--------|
| `e3252d16-de45-42b3-a13c-dbad33efd7ed` | 15:29 | Boot. 0 BurstRuntime. |
| `ceb56779-0f9c-4d12-86f5-89ed9895ccca` | 15:31 | Boot. 55 BurstRuntime. |
| `86ef7061-3412-40c1-96f6-aba6607ed43c` | 19:00 | Gilbert load window. |
| `284fa20b-6ade-4816-9aaf-040b33ae22eb` | 19:00 | Same. |

## Match to prior ExecuteWork deaths

| Field | 08-13 / 08-21 / 08-29 | This `13872449` |
|-------|----------------------|-----------------|
| Managed stack | `b__5_0` → `Library_ExecuteWork__SWIG_1` | identical (same IL offsets) |
| Exception | `0xC000001D` | identical |
| RIP | `VCRUNTIME140+0xdf8b` | **`cohtml.WindowsDesktop.dll+0x404bb9`** |
| In-dump RIP page | absent | absent |
| First Cohtml stack offsets | `+0x4050aa`, `+0x404fba`, `+0x40a03f`, `+0x40629a` | same |
| `RenoirCore+0xb4e4` | often present | present |
| Time after Gilbert load | 2–8 min | **~58 min** (autosaves succeeded) |

Not DEP-on-stack (`info0=8`, unmapped RIP). RIP mapped in Cohtml.

## Immediately before death (`Player.log`)

GPU 80→256 MB. `JobTempAlloc` >4-frame leak at 256 MB. `TravelPurpose 9` then `10`. Unused-asset unloads. Then `Native Crash Reporting`.

No `View_Advance` immediately above the banner. Extra-renter count this log: 1.

404s: `coui://uil/Standard/Alarm.svg` at Gilbert load; later Police/Fire/Healthcare/Crime/Car.svg during Cumberland (~15:54). `DefaultResourceHandler` main thread. Coincident.

## Managed stack (dying thread)

```
at cohtml.Net.cohtmlNativePINVOKE:Library_ExecuteWork__SWIG_1
at cohtml.Net.Library:ExecuteWork
at Colossal.UI.UIManager:ExecuteWork
at Colossal.UI.TaskScheduler:<.ctor>b__5_0
```

`b__5_0` = dedicated `WT_Layout` thread.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x20bc` |
| Code | `0xC000001D` (`ILLEGAL_INSTRUCTION`) |
| RIP | `0x7ffe301e4bb9` = `cohtml.WindowsDesktop.dll+0x404bb9` |
| RSP | `0xc652f4d5e0` |
| RIP vs RSP | not adjacent; RIP mapped in Cohtml |
| Bytes at RIP in dump | not present |
| RSP+0x0 | `0xc652f4d690` (stack, RSP+`0xb0`) |

`cohtml.WindowsDesktop.dll` is not on the Mac game mount. Offset cannot be named from this machine.

## Native stack (qword scan from RSP, 10240 bytes)

Collapsed unique `cohtml.WindowsDesktop.dll` offsets, first-seen:

`+0x4050aa` (×2), `+0x404fba`, `+0x4057ad`, `+0x405101`, `+0x40a03f`, `+0x40629a`, `+0x0`, `+0x40ba69`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d`, `+0x412228`, `+0x3ac622`, `+0x412619`, `+0x15270a`, `+0x4147d5`, …

Also: `RenoirCore.WindowsDesktop.dll+0xb4e4`, `MSVCP140.dll`, later `mono-2.0-bdwgc.dll` (thread start). No mod `BurstRuntime` on the scan. No `lib_burst_generated`. RIP not in `VCRUNTIME140`.

55 BurstRuntime DLLs loaded in-process; none on the faulting stack scan.

## MoreDispatchMod

`HandleToggleTool` ×6 this boot, last **15:54:17** (still Cumberland, ~6 min before city switch). Crash ~16:58 on Gilbert. Last tool Zone Tool 16:57:59 (`isDispatchTool=False`). Last ticks 16:58:17. Ticking until death ≠ stack hit.

## Coincident at Gilbert load (16:00)

`VehicleController.PropertyPack.LoadFromFile` `FileNotFoundException` (`ModsData\VehicleController\packs\property\.json`) during `VehiclePropertySystem.Deserialize` / `SerializerSystem.OnUpdate` / `LoadGameSystem`. Handled. Load completed. Autosaves after that succeeded for ~55 min. Not the dying thread.

Unknown prefabs: `W7MiniBusStation`, `W7OldCemetery`, `Mod:81040`, `Mod:80942`. Same as prior Gilbert loads.

## Open

- Why Cohtml executed an illegal instruction at `+0x404bb9` vs prior `VCRUNTIME140+0xdf8b` (same managed path).
- Isolation with code mods off did not hit this class in ~22 min ([08-29 Burst DEP](2026-08-29-burst-generated-dep.md)). This boot had code/UI mods on for ~58 min on Gilbert then died here.
- Intact named save: `Gilbert County 57`. Last autosave `04-September-16-55-29`.
