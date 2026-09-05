# 2026-08-29 — Cohtml layout `ExecuteWork` (`VCRUNTIME140` illegal instruction)

> **Status**: Closed — attribution complete; Cohtml-internal cause not named
> **Date**: 2026-08-29
> **Dump**: `db5da537-93e4-4743-a4a8-a8761c4dcebc.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-29-db5da537/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml layout thread (`TaskScheduler` `b__5_0` / `WT_Layout`). RIP in `VCRUNTIME140.dll+0xdf8b`. Exception `0xC000001D` (`ILLEGAL_INSTRUCTION`). |
| Exact root cause | Not determined. Same class as [2026-08-13](2026-08-13-cohtml-layout-execute-work.md) and [2026-08-21 ExecuteWork](2026-08-21-cohtml-layout-execute-work.md): same RIP module+offset, same managed path. |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Unity 2022.3.71f1; Cohtml 1.64.0.7 |
| OS / CPU / GPU | Windows 11; i9-14900KF; RTX 5080 D3D11 |
| Boot | SceneFlow command line 20:11:29. MoreDispatch `OnLoad` 20:12:45. |
| Save loaded | `29-August-20-08-43` (20:14:24). Loading completed 20:14:52. Metadata `133e58d1dda49117348402ab9edf445c`. |
| Manual save this session | `Gilbert County 52` (mtime Mac 23:17 ≈ 20:17) |
| Last autosave | `29-August-20-19-49` (mtime Mac 23:19) |
| Crash | Last MoreDispatch tick 20:22:13. Water Features still writing 20:22:14. Dump mtime Mac 23:22. |
| Session length | ~11 min boot; ~8 min after load |

Crashed session is live `Player.log` (banner at line 483). Copied before rotate.

## Same-day dumps (not this crash)

Launch ANRs this boot, thread `0x53f8`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, `0x0517A7ED`:

| Dump | mtime Mac |
|------|-----------|
| `b0b12997-2c54-4042-b10c-2580a9547c0f` | 23:12 |
| `774b09b2-fb3b-4d3c-a463-f8cac947ecbd` | 23:13 |
| `fe2d3236-ac39-4b16-b886-fa54cc8a5b03` | 23:14 |

## Prior session (`Player-prev.log`) — same managed path, dump rotated

`Player-prev.log` also ends in `Native Crash Reporting` + `WT_Layout` `ExecuteWork` (`b__5_0`, same IL offsets). Crashpad reports folder only retained the three relaunch ANRs + this dump; the prior native dump is gone.

That session: 583 `Removed extra renter` lines, GPU last 288 MB, then ExecuteWork. This session: **0** extra-renter lines, GPU 208 MB. Extra-renter burst is not required.

## Match to prior `VCRUNTIME140+0xdf8b` deaths

| Field | `80f60f31` (08-13) | `569f8e3e` (08-21) | `db5da537` (this) |
|-------|-------------------|--------------------|-------------------|
| Managed stack | `b__5_0` → `ExecuteWork` | identical | identical |
| Exception | `0xC000001D` | identical | identical |
| RIP | `VCRUNTIME140+0xdf8b` | identical | **identical** |
| In-dump RIP page | absent | absent | absent |
| First stack offsets | `+0x404d76`, `+0x4050aa`, `+0x404fba` | same | **same** (`RSP+0` = `+0x404d76`) |
| `+0x40629a` / `+0x40ba69` / `+0x4061ec` / `+0x254600` | present | present | present |
| `RenoirCore+0xb4e4` | not listed on 08-13 first scan | present | present |
| Time after load | hours | ~2 min | ~8 min |
| Save | Gilbert County 42 | `21-August-19-59-26` | `29-August-20-08-43` / Gilbert County 52 |

Not the DEP-on-stack class (`info0=8`, unmapped RIP, `RIP-RSP` `0xf78` / `0x12a8`).

## Immediately before death (`Player.log`)

GPU 80→112 MB, unused-asset unload, `FirstPersonCamera loaded!`, VehicleController missing property pack, SchoolOrganizer education-level warns, GPU 128→192 MB, unused-asset unloads, GPU 208 MB, `Native Crash Reporting`.

No `View_Advance`. No `TravelPurpose`. No extra-renter. No `JobTempAlloc`. No `infoviews.zoneInfos`.

404 `coui://uil/Standard/Alarm.svg` (Id 182) at 20:14:42: `DefaultResourceHandler` main-thread coroutine. Coincident.

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
| Thread | `0x1790` |
| Code | `0xC000001D` (`ILLEGAL_INSTRUCTION`) |
| RIP | `0x7ffe2926df8b` = `VCRUNTIME140.dll+0xdf8b` |
| RSP | `0xe4d718d8b8` |
| RIP vs RSP | not adjacent; RIP mapped in CRT, not stack |
| Bytes at RIP in dump | not present |
| RSP+0x0 | `0x7ffd7aa44d76` = `cohtml.WindowsDesktop.dll+0x404d76` |

On-disk bytes at this RVA were recorded on 2026-08-13 (`xor eax,eax; … ret` under `strstr`). `VCRUNTIME140.dll` / `cohtml.WindowsDesktop.dll` are not on the Mac game mount this pass.

## Native stack (qword scan from RSP, 10056 bytes)

Collapsed unique `cohtml.WindowsDesktop.dll` offsets, first-seen:

`+0x404d76`, `+0x4050aa` (×2), `+0x404fba`, `+0x4057ad`, `+0x40a03f`, `+0x40629a`, `+0x0`, `+0x40ba69`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d`, `+0x47db9d`, `+0x254dd3`, `+0x25462a`, `+0x412228`, `+0xf4f5f`, `+0x412619`, `+0x15270a`, `+0x4147d5`

Also: `RenoirCore.WindowsDesktop.dll+0xb4e4`, `cohtml_Unity3DPlugin.dll+0x2ed79`, then `ntdll` / `MSVCP140` / `mono-2.0-bdwgc` / `combase` (thread start / CRT).

No mod `BurstRuntime`. No `lib_burst_generated.dll`. RIP not unmapped.

## MoreDispatchMod / Water Features

No `HandleToggleTool`. No MoreDispatch errors. Last tool `Terrain Tool` at 20:22:09 (`isDispatchTool=False`). RoadBuilderTool used 20:19:07. Last FireAccident/FireMedical ticks 20:22:13.

`Mods_Yenyang_Water_Features.log` still logging `[ChangeWaterSystemValues] OnCreate changed evaporation rate back to 0.0001` at 20:22:14. Ticking until death ≠ stack hit. Water Features DLL not on the qword scan.

## Save instability (facts only)

Gilbert County line: 08-13 case was Gilbert County 42. This boot loaded autosave `29-August-20-08-43`, wrote `Gilbert County 52`, then died on the same `WT_Layout` / `VCRUNTIME140+0xdf8b` site ~8 min later. Immediate previous session died on the same managed stack (dump rotated).

## Isolation (`--disableCodeModding`, same city)

Boot 20:28:53. SceneFlow `disableCodeModding: true`. `Modding.log`: `Modding is disabled` at 20:30:01. Prefab library still 20753 (asset packs on). No BurstRuntime in the boot dump. No code-mod UI modules in `UI.log` (vanilla Cohtml 1.64.0.7 only).

Loaded `29-August-20-19-49` at 20:30:57. `Loading completed` 20:31:18. No `Native Crash Reporting` in this session `Player.log` at copy (~20:33, log still growing).

Unknown prefabs this load (code mods off): 45 `RoadBuilderPrefab`, 24 `ParcelPrefab`, `FakePrefab:Traffic.FakePrefab`, plus the already-missing `W7OldCemetery` / `W7MiniBusStation` / `Mod:81040` / `Mod:80942`. Load still completed.

Dump `63146559-def9-4fb7-b2ba-583ada7fb13d` (mtime Mac 23:31) is a launch ANR `0x0517A7ED`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`. Not this ExecuteWork class. Evidence: `research/topics/CrashInvestigations/evidence/2026-08-29-63146559/` (gitignored).

Isolation session then died ~22 min after load on a **different** class: [game Burst DEP](2026-08-29-burst-generated-dep.md) (`lib_burst_generated.dll+0xc142`, empty managed stack, dump `4f46a920`). No `ExecuteWork`. The `VCRUNTIME140+0xdf8b` smash did not recur past the prior 2–8 min window with code mods off (one run).

## Open

- Why `VCRUNTIME140+0xdf8b` executed an illegal instruction (same unnamed Cohtml caller chain as 08-13 and 08-21).
- Whether this save uniquely provokes layout-thread smash vs city-size / UI load in general.
- Isolation: city loads with code mods off. `VCRUNTIME140+0xdf8b` did not recur in ~22 min; process died in game Burst instead ([4f46a920](2026-08-29-burst-generated-dep.md)).
