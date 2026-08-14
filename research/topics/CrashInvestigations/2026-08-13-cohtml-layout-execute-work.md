# 2026-08-13 — Cohtml layout `ExecuteWork`

> **Status**: Closed — attribution complete; Cohtml-internal / hardware root cause not determined
> **Date**: 2026-08-13
> **Dump**: `80f60f31-02a5-4f7d-af86-888772f15379.dmp`

## Verdict

| Question | Answer |
|----------|--------|
| Did a mod native/Burst module crash? | No. |
| Did MoreDispatchMod's UI or native code crash? | No. Panel never opened. No Burst DLL. Log has no errors. |
| Did missing `coui://customassets/` thumbnails crash the process? | No. Handled on the Unity main-thread coroutine path. |
| Where did the process die? | Cohtml dedicated layout thread, inside `cohtml.WindowsDesktop.dll`, RIP in `VCRUNTIME140.dll+0xdf8b`. |
| Exact Cohtml function / cause? | Not determined. No Cohtml source/symbols. Dump has no code pages at RIP. |

## Session

| Field | Value |
|-------|-------|
| Game | Cities: Skylines II 1.6.0f1 (419.d6c6) [6216.19404] |
| Unity | 2022.3.71f1 |
| OS | Windows 11 64-bit (10.0.26200) |
| CPU | Intel Core i9-14900KF (32 logical) |
| GPU | NVIDIA GeForce RTX 5080, driver 32.0.15.7688, D3D11 |
| RAM | 63.840 GB |
| Save | Gilbert County 42 (manual save 18:25:38) |
| Last autosave | 13-August-18-28-21 (18:28:24) |
| Crash (game log clock) | ~18:30 |
| Crash (dump mtime) | 2026-08-13 21:30 (Mac view of the Windows volume) |
| `Player.log` | `{UserData}/Player.log` (8733 lines) |
| Dump | `{UserData}/.cache/backtrace/crashpad/reports/80f60f31-02a5-4f7d-af86-888772f15379.dmp` (51,075,168 bytes) |

## Same-night dumps (not this crash)

| Dump | mtime | Exception | RIP module | Notes |
|------|-------|-----------|------------|-------|
| `2c8c84ee-9f03-4646-958f-9784a82ae9f9` | 19:47 | `0x0517A7ED` | `BacktraceCrashpadWindows.dll+0x2dcd` | `ANRException: Blocked thread detected`. Launch-time. |
| `b41e9532-044e-47c7-a5c2-085e95325f63` | 19:48 | `0x0517A7ED` | same class | ANR. Dump contains `MoreDispatchMod.mjs` as mapped UI, not as RIP. |
| `9580b268-ea75-429d-98e7-c13862996c33` | 19:49 | `0x0517A7ED` | same class | ANR. |
| `97e8388c-df04-4e7b-8747-2b0b4f250480` | 19:53 | `0x0517A7ED` | same class | ANR. |

`Player-prev.log` has no `Native Crash Reporting` block. Ends in Unity allocator stats.

## `Player.log` crash block

```
Native Crash Reporting
Got a UNKNOWN while executing native code. This usually indicates
a fatal error in the mono runtime or one of the native libraries
used by your application.

Managed Stacktrace:
  at <unknown> <0xffffffff>
  at cohtml.Net.cohtmlNativePINVOKE:Library_ExecuteWork__SWIG_1
  at cohtml.Net.Library:ExecuteWork
  at Colossal.UI.UIManager:ExecuteWork
  at Colossal.UI.TaskScheduler:<.ctor>b__5_0
  at System.Threading.ThreadHelper:ThreadStart_Context
  ...
```

`TaskScheduler` constructor lambda (`b__5_0`) is the dedicated layout thread:

```
while (m_IsLayoutThreadRunning)
{
    UIManager.instance.ExecuteWork(WorkType.WT_Layout);
    m_LayoutReset.WaitOne();
}
```

`UIManager.ExecuteWork` calls `m_Library.ExecuteWork(type, WorkExecutionMode.WEM_UntilQueueEmpty)`.

`Library.ExecuteWork(WorkType, WorkExecutionMode)` is `cohtmlNativePINVOKE.Library_ExecuteWork__SWIG_1`.

`WT_Resources` uses the thread pool, not this lambda. This crash is `WT_Layout`.

## Dump exception (this case)

| Field | Value |
|-------|-------|
| Thread | `0x355c` |
| Code | `0xC000001D` (`ILLEGAL_INSTRUCTION`) |
| RIP | `0x7fff71e5df8b` |
| RIP module | `Cities2_Data/Plugins/x86_64/VCRUNTIME140.dll` + `0xdf8b` |
| RSP | `0x4d6ed8daa8` |
| On-disk bytes at RVA `0xdf8b` | `33 c0 48 8b 5c 24 08 48 8b 7c 24 10 c3` (`xor eax,eax; mov rbx,[rsp+8]; mov rdi,[rsp+10h]; ret`) |
| In-dump code page at RIP | Not present (minidump MemoryList does not include that page) |

Nearest export at or before `0xdf8b`: `strstr` (`0xddd0`). `wcschr` is at `0xdfa0`.

## Native stack (qword scan of dumped stack memory)

Resolved return addresses under RIP, in order of increasing RSP offset. All in `cohtml.WindowsDesktop.dll` (base `0x7fff26b80000`):

| RSP offset | Address | Module offset |
|------------|---------|---------------|
| +0x000 | `0x7fff26f84d76` | +`0x404d76` |
| +0x030 | `0x7fff26f85716` | +`0x405716` |
| +0x060 | `0x7fff26f850aa` | +`0x4050aa` |
| +0x090 | `0x7fff26f84fba` | +`0x404fba` |
| +0x0b0 | `0x7fff26f85716` | +`0x405716` |
| +0x0e0 | `0x7fff26f850aa` | +`0x4050aa` |
| +0x110 | `0x7fff26f8a03f` | +`0x40a03f` |
| +0x130 | `0x7fff26f8629a` | +`0x40629a` |
| +0x190 | `0x7fff26f8ba69` | +`0x40ba69` |
| +0x1b0 | `0x7fff26f861ec` | +`0x4061ec` |
| +0x1e0 | `0x7fff26dd4600` | +`0x254600` |

No Burst runtime DLL, no `MoreDispatchMod`, no ExtraAssetsImporter, no FindIt, no `lib_burst_generated` on this scan.

## Coincident log lines (not the crashing thread)

`UI.log` through 18:30:40:

- Repeated `loading button`
- `ResourceHandler: Invalid host locations map.` for
  - `coui://customassets/W7RoundaboutAssetPack/W7Roundabouts/W7Roundabouts.png`
  - `coui://customassets/W7MoreDeathcareAssetPack/W7OldCemetery/W7OldCemetery.png`
- Four `Cannot abort request with ID` errors at 17:35:03–17:35:04 (55 minutes before the crash)

`DefaultResourceHandler.RequestResourceAsync` (`Colossal.UI.dll`):

- Host `customassets` missing from `m_HostLocationsMap` → `requestData.Error = "Invalid host locations map."` → `CheckForFailedRequest` → `RespondWithFailure`
- Runs as a Unity coroutine on the main thread (`StartCoroutine`)
- `OnAbortResourceRequest`: if id not in `m_PendingRequests`, logs `Cannot abort request with ID: {0}. It's doesn't exist.`

## MoreDispatchMod

| Check | Result |
|-------|--------|
| Log | `{UserData}/Logs/MoreDispatchMod.log` (1,087,838 bytes), last line 18:30:47 |
| Errors / exceptions | None |
| `HandleToggleTool` | Never logged this session |
| Last `ManualDispatchUI` activity | `OnToolChanged` for vanilla tools (Net / Object / Zone / Default). `isDispatchTool=False` |
| UI | `moduleRegistry.append("GameTopLeft", DispatchToolButton)`. Bool `ValueBinding`s + `TriggerBinding`s. No Burst plugin in dump module list |
| At crash | `FireAccident` / `FireMedical` / `HeliBlock` info ticks, then process stop |

## Assemblies decompiled for this case

| Type | Assembly | Role |
|------|----------|------|
| `Colossal.UI.UIManager` | `Colossal.UI.dll` | `ExecuteWork` → native library |
| `Colossal.UI.TaskScheduler` | `Colossal.UI.dll` | Layout thread = crash managed frame `b__5_0` |
| `Colossal.UI.DefaultResourceHandler` | `Colossal.UI.dll` | Host map + abort; main-thread resource path |
| `cohtml.Net.Library` | `cohtml.Net.dll` | SWIG `Library_ExecuteWork__SWIG_1` |

## Open

- Cohtml function at `cohtml.WindowsDesktop.dll+0x404d76` (and callers listed above): no public symbols.
- Whether RIP `ILLEGAL_INSTRUCTION` on a `xor/ret` region of `VCRUNTIME140` is a bad dump/context, in-memory patch, or hardware (14900KF).
- Whether some other UI (FindIt / asset menus / vanilla toolbar) provoked the Cohtml abort without appearing on the native stack.
