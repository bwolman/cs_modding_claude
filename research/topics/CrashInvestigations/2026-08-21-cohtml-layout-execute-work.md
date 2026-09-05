# 2026-08-21 — Cohtml layout `ExecuteWork` (`VCRUNTIME140` illegal instruction)

> **Status**: Closed — attribution complete; Cohtml-internal cause not named
> **Date**: 2026-08-21
> **Dump**: `569f8e3e-b4da-41c7-80d7-862e3a638f7a.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-21-569f8e3e/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml layout thread (`TaskScheduler` `b__5_0` / `WT_Layout`). RIP in `VCRUNTIME140.dll+0xdf8b`. Exception `0xC000001D` (`ILLEGAL_INSTRUCTION`). |
| Exact root cause | Not determined. Same class as [2026-08-13 Cohtml layout](2026-08-13-cohtml-layout-execute-work.md): same RIP module+offset, same managed path. Not the DEP-on-stack class (`RIP-RSP` `0xf78` / `0x12a8`). |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Unity 2022.3.71f1; Cohtml 1.64.0.7 |
| OS / CPU / GPU | Windows 11; i9-14900KF; RTX 5080 D3D11 |
| Boot | SceneFlow 20:08:44. MoreDispatch `OnLoad` 20:09:45. |
| Save loaded | `21-August-19-59-26` (20:11:07). Loading completed 20:11:39. Metadata `329481aa5f03eaffe9c4382f56da9220`. |
| Crash | Last MoreDispatch tick 20:13:53. Water Features log still writing at 20:13:53. Dump mtime Mac 23:13. |
| Session length | ~5 min (~2 min after load completed) |

Crashed session is live `Player.log` (banner at line 470). Not rotated yet when copied.

## Same-day dumps (not this crash)

Launch ANRs this boot, thread `0x3f2c`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, `0x0517A7ED`:

| Dump | mtime Mac |
|------|-----------|
| `f4b9e2d0-4127-4d09-b918-c563a4db517f` | 23:09 |
| `a6894516-b4f2-4bb6-9c3e-9431582af188` | 23:10 |
| `6f2cb66a-8a92-46f2-b4bc-c4e7c08da91c` | 23:11 |

Prior session (`Player-prev.log`, empty managed stack — different class, do not merge):

| Dump | mtime Mac | Exception | RIP |
|------|-----------|-----------|-----|
| `8170a449-46d3-48ad-9b9a-d03fe8620bfd` | 23:01 | `0xC0000005` read (`info0=0`) of `0x51e8b28` | `UnityPlayer.dll+0x54f6ad` |

`ecb1910c` 22:39 is a prior-boot ANR (`0x3470`).

## Match to `80f60f31` (2026-08-13)

| Field | `80f60f31` (2026-08-13) | `569f8e3e` (this) |
|-------|------------------------|-------------------|
| Managed stack | `b__5_0` → `Library_ExecuteWork__SWIG_1` | identical (same IL offsets) |
| Exception | `0xC000001D` | identical |
| RIP module | `VCRUNTIME140.dll+0xdf8b` | **identical** |
| In-dump code page at RIP | absent | absent |
| First stack offsets | `+0x404d76`, `+0x405716`, `+0x4050aa`, `+0x404fba` | **identical** |
| `+0x40629a` / `+0x40ba69` / `+0x4061ec` / `+0x254600` | present | present |
| Session | ~hours, Gilbert County 42 | **~2 min after load** of `21-August-19-59-26` |

Same smash site as 2026-08-13, not Aug 16/21 DEP (`info0=8`, unmapped RIP on stack).

## Immediately before death (`Player.log`)

GPU 80→96 MB, unused-asset unload, `FirstPersonCamera loaded!`, VehicleController missing property pack, SchoolOrganizer education-level warns, GPU 112→192 MB, `Native Crash Reporting`.

No `View_Advance`. No `TravelPurpose`. No extra-renter. No `JobTempAlloc`. No `infoviews.zoneInfos`.

404 `coui://uil/Standard/Alarm.svg` (Id 182) at 20:11:26: `DefaultResourceHandler` main-thread coroutine. Coincident.

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
| Thread | `0x3120` |
| Code | `0xC000001D` (`ILLEGAL_INSTRUCTION`) |
| RIP | `0x7ffbe930df8b` = `VCRUNTIME140.dll+0xdf8b` |
| RSP | `0x2042d1dc18` |
| RIP vs RSP | not adjacent; RIP mapped in CRT, not stack |
| Bytes at RIP in dump | not present |
| RSP+0x0 | `0x7ffb56104d76` = `cohtml.WindowsDesktop.dll+0x404d76` |

On-disk bytes at this RVA were recorded on 2026-08-13 (`xor eax,eax; … ret` under `strstr`). `VCRUNTIME140.dll` / `cohtml.WindowsDesktop.dll` are not on the Mac game mount this pass.

## Native stack (qword scan from RSP, 9192 bytes)

Collapsed unique `cohtml.WindowsDesktop.dll` offsets, first-seen:

`+0x404d76`, `+0x405716`, `+0x4050aa` (×2), `+0x404fba`, `+0x405759`, `+0x40629a`, `+0x40ba69`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d`, `+0x412228`, `+0x412619`, `+0x15270a`, `+0x4147d5`

Also: `RenoirCore.WindowsDesktop.dll+0xb4e4`, `cohtml_Unity3DPlugin.dll+0x2e200`, then `ntdll` / `MSVCP140` / `mono-2.0-bdwgc` / `combase` / `KERNELBASE` (thread start / CRT).

No mod `BurstRuntime`. No `lib_burst_generated.dll`. RIP not unmapped (unlike DEP cases).

## MoreDispatchMod / Water Features

No `HandleToggleTool`. No MoreDispatch errors. Last tool `Yenyang's Water Tool` at 20:13:24 (`isDispatchTool=False`). Last FireAccident/FireMedical ticks 20:13:53.

`Mods_Yenyang_Water_Features.log` still logging `[ChangeWaterSystemValues] OnCreate changed evaporation rate back to 0.0001` at 20:13:53. Ticking until death ≠ stack hit. `Water_Features_win_x86_64.dll` is not on the qword scan.

## Open

- Why `VCRUNTIME140+0xdf8b` executed an illegal instruction (same unnamed Cohtml caller chain as 2026-08-13).
- Relation to DEP-on-stack deaths the same day (`41f2ba03`, `RIP-RSP=0x12a8`) and Aug 16 (`0xf78`).
- Isolation (UI mods off, same save) not run.
- Prior session `8170a449` (`UnityPlayer+0x54f6ad` read AV, empty managed stack) is a separate case, not written here.
