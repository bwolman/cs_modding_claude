# 2026-08-16 — `lib_burst_generated` access violation (`+0x8a01c9`)

> **Status**: Closed — attribution complete; Burst job not named
> **Date**: 2026-08-16
> **Dump**: `44b20996-0643-416b-bcba-6a92f73b264e.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-16-44b20996/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Thread `0xf08`. RIP `lib_burst_generated.dll+0x8a01c9`. Callers in that DLL and `UnityPlayer.dll`. |
| Exact root cause | Not determined. No Burst symbols. `lib_burst_generated.dll` not on the Mac game mount. Dump has no code page at RIP. Managed stack empty. |

`lib_burst_generated.dll` is the game's AOT Burst output (`Cities2_Data/Plugins/x86_64/`), not a mod `BurstRuntime/<hash>/<Mod>_win_x86_64.dll`.

Not the same smash as [ce956c0b](2026-08-16-cohtml-layout-dep.md) / [ad178da9](2026-08-16-cohtml-layout-dep-2.md): this is a read AV (`info0=0`), RIP mapped in game Burst, `RIP-RSP` not `0xf78`.

Same *family* as [2026-08-14 Burst AV](2026-08-14-burst-generated-access-violation.md) (`114e1625`, `+0xa69d` read of `0x70`). Different RIP offset and AV address — not the same instruction.

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404] |
| Unity | 2022.3.71f1 |
| OS / CPU / GPU | Windows 11 64-bit (10.0.26200); i9-14900KF; RTX 5080 D3D11 driver 32.0.15.7688 |
| Launch | `Cities2.exe -developerMode` |
| Boot | 15:35:23 (`SceneFlow`) |
| Save loaded | `16-August-15-21-05` (15:39:10). Loading completed 15:39:39. Metadata `dad38b90866d2f9f24dba2d47b7def33` |
| Last autosave | `16-August-20-37-46` (completed 20:37:49) |
| Crash (game log clock) | at/after 20:38:23 (last `UI.log` / `MoreDispatchMod` line) |
| Dump mtime | 2026-08-16 23:38 (Mac view) |
| Session length | ~5 h |
| `Player.log` | Copied (9911 lines). Has `Native Crash Reporting` at line 9901. |

## Same-session dumps (not this crash)

Launch ANRs, thread `0x15e8`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, code `0x0517A7ED`. Dump strings include `ANRException` / `Blocked thread detected`. Do not merge with the `0xC0000005`.

| Dump | mtime (Mac) | Notes |
|------|-------------|-------|
| `3a660f5f-efa9-4ba6-a42e-c9a11a370ab4` | 18:36 | Launch (~15:36 game) |
| `60df0133-202a-4339-b879-ddc8d18a4904` | 18:36 | Launch (~15:36 game) |
| `9c5cb13e-dadc-4913-802e-b3d454e65464` | 18:39 | During save load (~15:39 game) |

## `Player-prev.log` (previous session, not this crash)

Has its own `Native Crash Reporting` and managed `TaskScheduler` `b__5_0` → `Library_ExecuteWork__SWIG_1` (`WT_Layout`). That is the Cohtml layout class. No dump in this evidence folder matches that session. The save loaded this boot (`16-August-15-21-05`) is from that prior session.

## Immediately before death (`Player.log`)

No timestamps on these lines:

```
Unloading … unused Assets … Loaded Objects now: 369329 … 369397.
GPU instance buffer size changing to: 320 MB
Worker 1920552 had incorrect TravelPurpose 10!
Native Crash Reporting
```

Instance buffer this session: 80 → 304 MB early, then **320 MB** immediately above the banner. Coincident until the RIP module is shown to be that path.

Above the unload block, `Player.log` reprints a main-thread `DefaultResourceHandler` stack for `Invalid host locations map` `coui://customassets/medclbyMrSandcrfat_AssetPack/…png` (Id 5765). Same Id/URL as `UI.log` **20:27:21** (~11 min earlier). Coincident. Not the dying thread.

## Crash block

```
Native Crash Reporting
Got a UNKNOWN while executing native code.

Managed Stacktrace:
=================================================================
```

Managed stack is empty (header only, no `at` frames). Native worker: game Burst / Unity job. Not a missing log.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0xf08` |
| Code | `0xC0000005` (`ACCESS_VIOLATION`) |
| AV type | read (`info0=0`) |
| AV address | `0xb5003286b3` (not a mapped module; not a null-base `< 0x1000` field) |
| RIP | `0x7fff8efe01c9` = `lib_burst_generated.dll+0x8a01c9` (base `0x7fff8e740000`, size `0x3916000`) |
| RSP | `0xf3beefef40` |
| RBP | `0xf3beefefc0` |
| RCX | `0xb5003286b7` (`info1+4`) |
| RIP-RSP | not `0xf78`; RIP and RSP not adjacent pages |
| In-dump code page at RIP | Not present |
| On-disk DLL via Mac mount | `{Game}/Cities2_Data/Plugins/x86_64/` not present |

`cohtml.WindowsDesktop.dll` is mapped in this process. It is **not** on the qword scan of the faulting stack.

## Native stack (qword scan from RSP, 4288 bytes)

Unique modules: `lib_burst_generated.dll`, `UnityPlayer.dll`, `KERNELBASE.dll`, `KERNEL32.DLL`, `ntdll.dll`.

Code-near RIP (`lib_burst_generated`): `+0x8a01c9` (RIP), `+0x8a0870`, `+0x89f956`.

Large RVAs in the same DLL (`+0x35c0840`, `+0x35bfb40`, `+0x35bac50`, `+0x35bfb57`, `+0x35bac67`) are far from RIP; treat as data pointers, not callers.

First `UnityPlayer` hits: `+0x58ef0`, `+0x58e20`. Later: `+0x6fd854`, `+0x578116`, `+0x578432`, `+0x573852`, `+0x5789ec`, `+0x5760e4`, `+0x573dc2`, `+0x57471b`, `+0x5747a9`, `+0x6b35bc`, plus two large Unity RVAs (`+0x190e3d3`, `+0x1c3b9e0`, `+0x19b9bd0`).

No mod `BurstRuntime/…/<Mod>_win_x86_64.dll`. No `cohtml*`. No `RenoirCore`. No `mono-2.0-bdwgc.dll`. No `VCRUNTIME140` at RIP. No `MoreDispatchMod` native module (none is mapped).

Mod BurstRuntime DLLs **are** mapped in the process (NodeController, MoveIt, Anarchy, Time2Work, Traffic, Platter, etc.). None appear on the faulting scan.

## MoreDispatchMod

| Check | Result |
|-------|--------|
| Log | Copied. `OnLoad` 15:36:22. Last line 20:38:23 |
| Errors / exceptions | None |
| `HandleToggleTool` | 37 times this session. Last 19:22:01 (deactivate after taxi reroute). ~76 min before death |
| At crash | `OnToolChanged → Object Tool, isDispatchTool=False` (20:38:23). Dispatch tool not active |

Earlier use is not a stack hit.

## Coincident UI (`UI.log`)

| Time | Fact |
|------|------|
| 15:39:28 | 404 `coui://uil/Standard/Alarm.svg` (Id 180). `DefaultResourceHandler` main-thread coroutine |
| 20:21:30–20:27:21 | Repeated `Invalid host locations map` `coui://customassets/medclbyMrSandcrfat_AssetPack/…png`. Last Id 5765 |
| 20:38:23 | Last line: `loading button` / `[BetterTransitView] Button rendering...` |

`infoviews.zoneInfos` does not appear. No extra-renter lines. `TravelPurpose 10` is vanilla `WorkProviderSystem.RemoveWorker`; also seen on other crash classes.

## Compare to prior cases

| Field | `114e1625` (2026-08-14) | `44b20996` (this) | `ce956c0b` / `ad178da9` |
|-------|-------------------------|-------------------|-------------------------|
| Managed stack | empty | empty | `b__5_0` → `ExecuteWork` |
| Code | `0xC0000005` read | `0xC0000005` read | `0xC0000005` DEP (`info0=8`) |
| RIP module | `lib_burst_generated+0xa69d` | `lib_burst_generated+0x8a01c9` | unmapped (stack) |
| AV address | `0x70` (null-base) | `0xb5003286b3` | RIP itself |
| RIP-RSP | n/a (mapped RIP) | n/a (mapped RIP) | **`0xf78`** |

## Open

- Burst function at `+0x8a01c9`: no symbols; DLL not on this mount; no dump code page.
- Which job read `0xb5003286b3` — not determined.
- Whether this and `114e1625` (`+0xa69d`) are the same job family — not shown.
