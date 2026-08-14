# Crash Investigations

> **Status**: Active
> **Date started**: 2026-08-13
> **Last updated**: 2026-08-13

## Scope

Documented native and managed Cities: Skylines II process crashes, with sources, stacks, and what can and cannot be attributed to a mod.

Site: `site/crashes/`

## Cases

| Date | Id | Verdict | Page |
|------|-----|---------|------|
| 2026-08-13 | `80f60f31-02a5-4f7d-af86-888772f15379` | Not a mod native stack. Fault in Cohtml layout → `VCRUNTIME140`. Root cause inside Cohtml / hardware not determined. | [2026-08-13-cohtml-layout-execute-work.md](2026-08-13-cohtml-layout-execute-work.md) |

## Standard evidence locations

| Item | Path |
|------|------|
| Player log (current) | `{UserData}/Player.log` |
| Player log (previous session) | `{UserData}/Player-prev.log` |
| Mod / subsystem logs | `{UserData}/Logs/` |
| Crashpad dumps | `{UserData}/.cache/backtrace/crashpad/reports/*.dmp` |
| Game install | Steam `Cities Skylines II/` |
| Managed assemblies | `{Game}/Cities2_Data/Managed/` |
| Native plugins | `{Game}/Cities2_Data/Plugins/x86_64/` |

This install:

- UserData: `/Volumes/Users/micro/AppData/locallow/Colossal Order/Cities Skylines II`
- Game: `/Volumes/steamapps/common/Cities Skylines II`

## Classification

| Signal | Meaning |
|--------|---------|
| `Native Crash Reporting` + `Got a UNKNOWN while executing native code` in `Player.log` | Native fault. Managed stack (if present) is the pinvoke boundary, not the faulting instruction. |
| `ANRException: Blocked thread detected` in dump strings | Watchdog / hung thread. Exception often raised from `BacktraceCrashpadWindows.dll`, not the hung code. |
| Managed `Exception` / `NullReferenceException` only | C# fault. Mod assemblies can appear in the managed stack. |
| Dump exception `0xC0000005` | Access violation. |
| Dump exception `0xC000001D` | Illegal instruction. |
| Dump exception `0x0517A7ED` | Non-NT custom code used by Backtrace ANR dumps on this install. |

## Attribution rules

- A mod is the crashing **code** only if its module (managed type or Burst/native DLL) is on the faulting stack.
- A handled managed error on a different thread (e.g. `DefaultResourceHandler.RespondWithFailure`) is not the crash site.
- Absence of a mod from the native stack does not prove a mod did not *provoke* a vanilla native bug.
- Cohtml (`cohtml.WindowsDesktop.dll`, `cohtml.Net`) and `VCRUNTIME140.dll` are game/runtime, not mods.
- Without Cohtml native source/symbols and without dump code pages at RIP, the instruction inside Cohtml cannot be named.
