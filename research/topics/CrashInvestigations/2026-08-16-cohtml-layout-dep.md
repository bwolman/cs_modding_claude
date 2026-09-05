# 2026-08-16 — Cohtml layout DEP (execute unmapped / stack)

> **Status**: Closed — attribution complete; Cohtml-internal cause not named
> **Date**: 2026-08-16
> **Dump**: `ce956c0b-f996-4f42-9250-d4425340143f.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-16-ce956c0b/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml layout thread (`TaskScheduler` `b__5_0` / `WT_Layout`). Native callers in `cohtml.WindowsDesktop.dll` + `RenoirCore.WindowsDesktop.dll`. RIP unmapped, in the stack address range. Exception `0xC0000005` DEP (`info0=8`). |
| Exact root cause | Not determined. Same managed path as [2026-08-13 Cohtml layout](2026-08-13-cohtml-layout-execute-work.md); different exception (DEP vs `0xC000001D` in `VCRUNTIME140`). |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404] |
| OS / CPU / GPU | Windows 11; i9-14900KF; RTX 5080 D3D11 |
| Save loaded | `16-August-01-31-33` (11:50:37) |
| Last autosave | `16-August-14-41-03` (14:41:06) |
| Crash (game log clock) | ~14:41 |
| Dump mtime | 2026-08-16 17:41 (Mac view) |
| Session length | ~3 hours |

## Same-day dumps (not this crash)

Launch ANRs, thread `0x4f60`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, `0x0517A7ED`:

| Dump | mtime |
|------|-------|
| `e7e012f3-4bc0-4436-81d5-7c42a6319452` | 14:48 |
| `0dacea80-ab90-49f7-baeb-1280c7dcb937` | 14:49 |
| `7d68fbc2-abab-48e7-a09d-be653f32ff33` | 14:50 |

## Immediately before death (`Player.log`)

Main-thread Cohtml/`v8` frames (`View_Advance` ← `UIView.Update` ← `UIManager.Update` ← `GameManager.UpdateUI`). Then unused-asset unload, `TravelPurpose 10`, then `Native Crash Reporting`.

Those Update frames are **not** the dying thread. The managed crash stack is the layout worker.

GPU instance buffer this session: 80 → 320 MB. Last change at line 3275, well before the crash at 4191. Coincident.

## Managed stack (dying thread)

```
at cohtml.Net.cohtmlNativePINVOKE:Library_ExecuteWork__SWIG_1
at cohtml.Net.Library:ExecuteWork
at Colossal.UI.UIManager:ExecuteWork
at Colossal.UI.TaskScheduler:<.ctor>b__5_0
```

`b__5_0` = dedicated `WT_Layout` thread. Same as 2026-08-13 Cohtml case.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x5314` |
| Code | `0xC0000005` (`ACCESS_VIOLATION`) |
| AV type | DEP / execute (`info0=8`) |
| AV address / RIP | `0xeb0e5febb0` (unmapped as a module) |
| RSP | `0xeb0e5fdc38` |
| RIP vs RSP | 3960 bytes apart; adjacent 4 KiB pages (`0xeb0e5fe000` / `0xeb0e5fd000`). RIP is stack memory. |

Executing the stack is blocked by NX/DEP. Typical of a corrupted return address / function pointer, not a named Cohtml export.

## Native stack (qword scan)

`cohtml.WindowsDesktop.dll`: `+0x4050aa`, `+0x40a03f`, `+0x40629a`, `+0x40b9c9`, `+0x4061ec`, `+0x40b7f7`, `+0x406a6d`

`RenoirCore.WindowsDesktop.dll`: `+0xb4e4`

`+0x4050aa`, `+0x40a03f`, `+0x40629a` also appeared on the 2026-08-13 Cohtml layout stack.

No mod BurstRuntime. No `lib_burst_generated.dll`. No `VCRUNTIME140` at RIP (unlike 2026-08-13).

## MoreDispatchMod

No `HandleToggleTool` this session. No errors. Last lines 14:41:35 FireAccident/FireMedical ticks.

## Coincident UI

Repeated `Error in value binding 'infoviews.zoneInfos'` (last 14:41:01). Main-thread binding. Not on the layout native stack.

## Open

- What overwrote the layout-thread return address.
- Whether 2026-08-13 (`0xC000001D` / `VCRUNTIME140`) and this DEP-on-stack are the same Cohtml bug.
- Reproduced: [ad178da9](2026-08-16-cohtml-layout-dep-2.md) ~17 min later on the next load of autosave `16-August-14-41-03`. Same `RIP-RSP=0xf78`. GPU only 112 MB.
- `infoviews.zoneInfos` errors: not attributed.
