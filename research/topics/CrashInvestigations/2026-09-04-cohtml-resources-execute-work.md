# 2026-09-04 — Cohtml resources `ExecuteWork` (`v8.dll` read AV)

> **Status**: Closed — attribution complete; V8/Cohtml-internal cause not named
> **Date**: 2026-09-04
> **Dump**: `9455b60c-4398-4cc7-a805-9db4b007577c.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-09-04-9455b60c/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml **resources** thread-pool worker (`TaskScheduler` `b__8_0` / `WT_Resources`). RIP in `v8.dll+0xdacdfa`. Exception `0xC0000005` **read** (`info0=0`) of `0x2f00000086`. |
| Exact root cause | Not determined. Same managed pinvoke as layout deaths (`Library_ExecuteWork__SWIG_1`) but **not** `b__5_0` / `WT_Layout`. Matches the Steam no-mod report class (`ScheduleTask` `b__8_0`). |

`v8.dll` is Cohtml’s JS engine (game/runtime).

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Cohtml 1.64.0.7 |
| Flag | `disableCodeModding: false`. Playset Current (5892750). |
| Boot | SceneFlow 17:02:08. |
| Save loaded | `04-September-16-55-29` (17:11:11). Loading completed 17:11:42. Last autosave from the [16:58 layout crash](2026-09-04-cohtml-layout-execute-work.md). |
| Named save | `Gilbert County 57` 17:12:24 (285 875 397 bytes). |
| Last autosave | `04-September-17-32-38` completed 17:32:42 (282 937 859 bytes). |
| Crash | MoreDispatch last tick 17:33:08. Dump mtime Mac 20:33. ~21 min after load. |

Crashed session is live `Player.log`. Copied before rotate.

## Same-boot dumps (not this crash)

ANRs, thread `0x47e0`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, `0x0517A7ED`:

| Dump | mtime Mac |
|------|-----------|
| `522ceb30-7278-464c-ad96-4b108adea051` | 20:03 (boot, 0 BurstRuntime) |
| `8895c12b-6040-4756-a7ee-de3932228a5e` | 20:11 (55 BurstRuntime) |

Do not merge with earlier-today layout dump `13872449`.

## Not the layout ExecuteWork class

| Field | Layout cases (`b__5_0`) | This (`b__8_0`) |
|-------|-------------------------|-----------------|
| Thread | Dedicated `WT_Layout` | Thread-pool `WT_Resources` |
| Exception | `0xC000001D` or DEP `info0=8` | **read AV `info0=0`** |
| RIP | `VCRUNTIME140+0xdf8b` or `cohtml+0x404bb9` or stack | **`v8.dll+0xdacdfa`** |
| Community | — | [Steam #4339860975800843114](https://steamcommunity.com/app/949230/discussions/0/4339860975800843114/) (reporter: no mods) |

## Immediately before death (`Player.log`)

GPU 80→288 MB. Hundreds of `[UI] Unable to parse declaration: stroke-dasharray - none` / `While executing JS assetdb://gameui/index.js:1:40289` (240 `index.js` lines this log). Unused-asset unloads. Then `Native Crash Reporting`.

Those JS/CSS lines are Cohtml UI. They are **not** a named native RIP. Not proven as the smash; they are the same engine (`v8`) that died.

`UI.log` last lines 17:33:05–09: BetterTransitView button render / `New div appended`. Ticking until death ≠ stack hit. No BTV type on the native scan.

404 `coui://uil/Standard/Alarm.svg` (Id 194) at load. Coincident.

## Managed stack (dying thread)

```
at cohtml.Net.cohtmlNativePINVOKE:Library_ExecuteWork__SWIG_1
at cohtml.Net.Library:ExecuteWork
at Colossal.UI.UIManager:ExecuteWork
at Colossal.UI.TaskScheduler:<ScheduleTask>b__8_0
at System.Threading.QueueUserWorkItemCallback:WaitCallback_Context
…
at System.Threading.ThreadPoolWorkQueue:Dispatch
```

`b__8_0` = thread-pool `WT_Resources` (`ThreadPool.QueueUserWorkItem`). Not `b__5_0`.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0xc30` |
| Code | `0xC0000005` (`ACCESS_VIOLATION`) |
| AV type | read (`info0=0`) |
| AV address | `0x2f00000086` (not a mapped module; not a `< 0x1000` null-base field) |
| RIP | `0x7ffe4738cdfa` = `v8.dll+0xdacdfa` |
| RSP | `0x8ccd85d660` |
| Bytes at RIP in dump | not present |

`v8.dll` / `cohtml.WindowsDesktop.dll` are not on the Mac game mount.

## Native stack (qword scan from RSP, 10240 bytes)

Dominated by `v8.dll` (many offsets, including data-range RVAs `+0x11e61c0` etc.). Also `ucrtbase.dll`, `ntdll.dll`, `MSVCP140.dll`.

`cohtml.WindowsDesktop.dll`: `+0x5a2c33`, `+0x477429`, `+0x3d4aeb` (later on the scan than V8).

`mono-2.0-bdwgc.dll` (thread-pool start). `CoreUIComponents.dll+0x0` (one hit). `xinput1_3.dll+0x3` (likely data).

No mod `BurstRuntime` on the scan. No `lib_burst_generated`. RIP not in `VCRUNTIME140`. 55 BurstRuntime DLLs loaded in-process.

## MoreDispatchMod

No `HandleToggleTool`. Last tool Default Tool 17:31:59. Last ticks 17:33:08.

## Open

- V8 function at `+0xdacdfa` and why it read `0x2f00000086`.
- Whether `stroke-dasharray` / `index.js:1:40289` spam is a trigger or noise (same engine).
- Isolation with UI mods off not run for this `WT_Resources` class (Steam report claims no mods).
- Intact saves: `Gilbert County 57` (17:12), `04-September-17-32-38`.
