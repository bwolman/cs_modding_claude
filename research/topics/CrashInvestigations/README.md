# Crash Investigations

> **Status**: Active
> **Date started**: 2026-08-13
> **Last updated**: 2026-09-05

## Scope

Documented native and managed Cities: Skylines II process crashes, with sources, stacks, and what can and cannot be attributed to a mod.

Site: `site/crashes/`

## Cases

| Date | Id | Verdict | Page |
|------|-----|---------|------|
| 2026-08-13 | `80f60f31-02a5-4f7d-af86-888772f15379` | Not a mod native stack. Fault in Cohtml layout → `VCRUNTIME140`. Root cause inside Cohtml / hardware not determined. | [2026-08-13-cohtml-layout-execute-work.md](2026-08-13-cohtml-layout-execute-work.md) |
| 2026-08-13 | `76c70cc8-8542-4f88-8562-507e466f7c79` | Not a mod native stack. `0xC0000005` read of `0xffffffffffffffff` in `mono-2.0-bdwgc.dll` during/after asset unload. Empty managed stack. | [2026-08-13-mono-gc-access-violation.md](2026-08-13-mono-gc-access-violation.md) |
| 2026-08-14 | `114e1625-d749-4591-a211-e4af5b020daf` | Not a mod native stack. `0xC0000005` read at `0x70` in game `lib_burst_generated.dll+0xa69d`. Empty managed stack. | [2026-08-14-burst-generated-access-violation.md](2026-08-14-burst-generated-access-violation.md) |
| 2026-08-15 | `9bc2166b-359f-447c-aacc-85fa6d508b79` | Not a mod stack. Backtrace ANR `0x0517A7ED`. No `Native Crash Reporting` in `Player.log`. Hung work not identified. | [2026-08-15-backtrace-anr.md](2026-08-15-backtrace-anr.md) |
| 2026-08-16 | `ce956c0b-f996-4f42-9250-d4425340143f` | Not a mod stack. Cohtml `WT_Layout`. `0xC0000005` DEP; RIP unmapped on the stack. Same managed path as 2026-08-13 Cohtml case. | [2026-08-16-cohtml-layout-dep.md](2026-08-16-cohtml-layout-dep.md) |
| 2026-08-16 | `ad178da9-d794-4742-9d3e-11ba1496396d` | Repeat of `ce956c0b`. Same `RIP-RSP=0xf78`, same Cohtml offsets. ~4 min after reloading that autosave. GPU only 112 MB. | [2026-08-16-cohtml-layout-dep-2.md](2026-08-16-cohtml-layout-dep-2.md) |
| 2026-08-16 | `44b20996-0643-416b-bcba-6a92f73b264e` | Not a mod stack. `0xC0000005` read of `0xb5003286b3` in game `lib_burst_generated.dll+0x8a01c9`. Empty managed stack. Not the Cohtml `RIP-RSP=0xf78` DEP. | [2026-08-16-burst-generated-access-violation.md](2026-08-16-burst-generated-access-violation.md) |
| 2026-08-17 | managed UI error (no dump) | BTV `GetStopResolvedName` `GetPrefab` on obsolete stop-owner building (More Transport OLD 81040 missing; playset has NEW 150791). Process did not die. | [2026-08-17-transit-ui-argumentoutofrange.md](2026-08-17-transit-ui-argumentoutofrange.md) |
| 2026-08-21 | `41f2ba03-37d9-410f-83a5-267e1d1f77f2` | Not a mod stack. Cohtml `WT_Layout`. `0xC0000005` DEP; RIP unmapped zeros on the stack. `RIP-RSP=0x12a8` (not Aug 16 `0xf78`). | [2026-08-21-cohtml-layout-dep.md](2026-08-21-cohtml-layout-dep.md) |
| 2026-08-21 | `569f8e3e-b4da-41c7-80d7-862e3a638f7a` | Not a mod stack. Cohtml `WT_Layout` → `VCRUNTIME140+0xdf8b` (`0xC000001D`). Same RIP as 2026-08-13. ~2 min after load. | [2026-08-21-cohtml-layout-execute-work.md](2026-08-21-cohtml-layout-execute-work.md) |
| 2026-08-29 | `db5da537-93e4-4743-a4a8-a8761c4dcebc` | Not a mod stack. Cohtml `WT_Layout` → `VCRUNTIME140+0xdf8b` (`0xC000001D`). Same RIP as 08-13 / 08-21. Gilbert County 52 / autosave `29-August-20-08-43`. ~8 min after load. | [2026-08-29-cohtml-layout-execute-work.md](2026-08-29-cohtml-layout-execute-work.md) |
| 2026-08-29 | `4f46a920-4a21-4315-839b-86596b60f2aa` | Not a mod stack. `--disableCodeModding`. Empty managed stack. Game `lib_burst_generated.dll+0xc142` DEP (`0xC0000005` `info0=8`). ~22 min after load. | [2026-08-29-burst-generated-dep.md](2026-08-29-burst-generated-dep.md) |
| 2026-09-04 | `13872449-e667-47c3-917c-079cd3a0bdba` | Not a mod stack. Cohtml `WT_Layout`. `0xC000001D` at `cohtml.WindowsDesktop.dll+0x404bb9` (not `VCRUNTIME140+0xdf8b`). Gilbert County 56→57. ~58 min after load. | [2026-09-04-cohtml-layout-execute-work.md](2026-09-04-cohtml-layout-execute-work.md) |
| 2026-09-04 | `9455b60c-4398-4cc7-a805-9db4b007577c` | Not a mod stack. Cohtml `WT_Resources` (`b__8_0`). `v8.dll+0xdacdfa` read of `0x2f00000086` (`0xC0000005`). ~21 min after reloading last autosave. | [2026-09-04-cohtml-resources-execute-work.md](2026-09-04-cohtml-resources-execute-work.md) |
| 2026-09-04 | `3d3320c4-f393-4357-8b04-87197ddd0914` | Not a mod stack. Cohtml `WT_Layout` DEP; RIP on stack; **`RIP-RSP=0xf78`** (same smash as 2026-08-16). ~54 min after reload. | [2026-09-04-cohtml-layout-dep.md](2026-09-04-cohtml-layout-dep.md) |
| 2026-09-04 | none (boot ANRs only) | Not a mod stack. Main-thread `UIUpdateSystem` → `GCHandle.GetTarget` native. No dump of this death. ~58 min idle after reload. | [2026-09-04-uiupdate-gchandle.md](2026-09-04-uiupdate-gchandle.md) |
| 2026-09-05 | `7b0a8cf8-85fe-42d4-bece-ecbc7ee3e673` | Not a mod stack. Cohtml `WT_Layout` DEP; RIP on stack; **`RIP-RSP=0x1458`** (not `0xf78` / `0x12a8`). ~11 min after reload. | [2026-09-05-cohtml-layout-dep.md](2026-09-05-cohtml-layout-dep.md) |

## Standard evidence locations

| Item | Path |
|------|------|
| Player log (current) | `{UserData}/Player.log` |
| Player log (previous session) | `{UserData}/Player-prev.log` |
| Mod / subsystem logs | `{UserData}/Logs/` |
| Crashpad dumps | `{UserData}/.cache/backtrace/crashpad/reports/*.dmp` |
| Local copies (gitignored) | `research/topics/CrashInvestigations/evidence/<YYYY-MM-DD>-<dump-prefix>/` |
| Game install | Steam `Cities Skylines II/` |
| Managed assemblies | `{Game}/Cities2_Data/Managed/` |
| Native plugins | `{Game}/Cities2_Data/Plugins/x86_64/` |

`Player.log` is the **current** process. A relaunch rotates the crashed session to `Player-prev.log` and truncates `Logs/*.log`. Copy dump + both Player logs + `Logs/` **before** the next launch.

This install:

- UserData: `/Volumes/Users/micro/AppData/locallow/Colossal Order/Cities Skylines II`
- Game: `/Volumes/steamapps/common/Cities Skylines II`

## Classification

| Signal | Meaning |
|--------|---------|
| `Native Crash Reporting` + `Got a UNKNOWN while executing native code` | Native fault. Managed stack (if present) is the pinvoke boundary, not the faulting instruction. |
| `Managed Stacktrace` present but empty (no `at` frames) | Native worker: Mono GC, game Burst, or Unity job. Not a missing log. |
| RIP in `mono-2.0-bdwgc.dll` | Mono Boehm GC. Repeating nearby offsets are a mark/sweep walk, not extra modules. |
| RIP in `Cities2_Data/Plugins/x86_64/lib_burst_generated.dll` | Game AOT Burst jobs. Not a mod. Mod Burst is `BurstRuntime/<hash>/<Mod>_win_x86_64.dll`. |
| `ANRException: Blocked thread detected` in dump strings | Watchdog / hung thread. Exception often raised from `BacktraceCrashpadWindows.dll`, not the hung code. |
| Managed `Exception` / `NullReferenceException` only | C# fault. Mod assemblies can appear in the managed stack. |
| Dump exception `0xC0000005` | Access violation. Record `ExceptionInformation[0]` (0=read, 1=write, 8=DEP) and `[1]` (address). Address `< 0x1000` (e.g. `0x70`) is a null-base field read, not a mapped module. `info0=8` + RIP within ~one page of RSP = execute-stack (DEP). Observed `RIP-RSP` deltas: `0xf78` (2026-08-16 / 2026-09-04), `0x12a8` (2026-08-21), `0x1458` (2026-09-05). Same delta = same smash; different delta = same class, not the same gadget. |
| Dump exception `0xC000001D` | Illegal instruction. |
| Dump exception `0x0517A7ED` | Non-NT custom code used by Backtrace ANR dumps on this install. |

## Attribution rules

- A mod is the crashing **code** only if its module (managed type or Burst/native DLL) is on the faulting stack.
- A handled managed error on a different thread (e.g. `DefaultResourceHandler.RespondWithFailure`) is not the crash site.
- A mod used earlier in the session is not attribution. Record the last time it was active vs crash time.
- Absence of a mod from the native stack does not prove a mod did not *provoke* a vanilla native/GC bug (heap already corrupt, object the GC then followed).
- Game/runtime, not mods: `cohtml*.dll`, `VCRUNTIME140.dll`, `UnityPlayer.dll`, `mono-2.0-bdwgc.dll`, `lib_burst_generated.dll`, `ntdll.dll`, `BacktraceCrashpadWindows.dll`.
- Without native source/symbols and without dump code pages at RIP, the faulting function cannot be named.

## Community matches (`Library_ExecuteWork__SWIG_1`)

| Source | Date | Stack | Notes |
|--------|------|-------|-------|
| [Steam #4339860975800843114](https://steamcommunity.com/app/949230/discussions/0/4339860975800843114/) | 2024-06 | Same `ExecuteWork` pinvoke; **`TaskScheduler:<ScheduleTask>b__8_0`** (thread-pool `WT_Resources`, not our `b__5_0` layout thread) | Reporter: no mods. 5–10 min then silent close. |
| [PDX 1721186](https://forum.paradoxplaza.com/forum/threads/crash-to-desktop-without-warning.1721186/) | 2024-12 | `Library_ExecuteWork__SWIG_1` | In Review. Snippet also mentions Skyve thumbs path (not our stacks). |
| [PDX 1923478](https://forum.paradoxplaza.com/forum/threads/landbirn-map-post-thred.1923478/) | 2026-05 | `ExecuteWork` / `UIManager:ExecuteWork` | Map/asset thread. Not confirmed same smash. |

Does **not** prove our `RIP-RSP=0xf78` DEP-on-stack is vanilla. Does prove `ExecuteWork` native death is a known CS2 class, including at least one no-mod report (different work type).

`cohtml.WindowsDesktop.dll` is **not** on the Mac game mount. Offsets `+0x4050aa` / `+0x40a03f` / `+0x40629a` cannot be named from this machine.
