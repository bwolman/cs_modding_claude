# 2026-08-13 — Mono GC access violation

> **Status**: Closed — attribution complete; Mono-internal root cause not determined
> **Date**: 2026-08-13
> **Dump**: `76c70cc8-8542-4f88-8562-507e466f7c79.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-13-76c70cc8/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Native thread `0xf74`. RIP `mono-2.0-bdwgc.dll+0x27a5e8`. Entire scanned stack is that DLL. |
| Exact root cause | Not determined. No Mono symbols. Dump has no code page at RIP. Managed stack empty. |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404] |
| Unity | 2022.3.71f1 |
| OS | Windows 11 64-bit (10.0.26200) |
| CPU | Intel Core i9-14900KF (32 logical) |
| GPU | NVIDIA GeForce RTX 5080, driver 32.0.15.7688, D3D11 |
| RAM | 63.840 GB |
| Crash (game log clock) | ~20:24 |
| Dump mtime | 2026-08-13 23:24 (Mac view of the Windows volume) |
| Session log (after relaunch) | `{UserData}/Player-prev.log` (1590 lines, 126197 bytes). Copied. |
| `UI.log` / `SceneFlow.log` from this session | Overwritten by the next launch. Not copied in time. |
| `MoreDispatchMod.log` | Copied. Last line 20:24:41. |

## Same-session dumps (not this crash)

Launch ANRs, thread `0x1ef8`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, code `0x0517A7ED`:

| Dump | mtime |
|------|-------|
| `aff1cca7-8619-4ac7-846f-08ed3e870a43` | 22:07 |
| `253abb37-7e05-4287-867f-9c5606154a2e` | 22:08 |
| `d05dcc84-5fa1-4690-b48e-1805eae6c29a` | 22:09 |
| `f2e1c341-9da6-4c2d-93fc-849db2116ded` | 22:14 |

## `Player-prev.log` immediately before death

- `Unloading 1 Unused Serialized files` / unused assets (Loaded Objects ~364151)
- `IsTransitioningIn` / `IsTransitioningOut`
- `Worker … had incorrect TravelPurpose 10!` (also earlier)
- `[UI] [ERROR] ResourceHandler: Invalid host locations map.` `coui://customassets/W7RoundaboutAssetPack/W7Roundabouts/W7Roundabouts.png` — `DefaultResourceHandler` main-thread coroutine (same path as the earlier case)

## Crash block

```
Native Crash Reporting
Got a UNKNOWN while executing native code.

Managed Stacktrace:
=================================================================
```

Managed stack is empty. No C# frames.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0xf74` |
| Code | `0xC0000005` (`ACCESS_VIOLATION`) |
| AV type | read (`info0=0`) |
| AV address | `0xffffffffffffffff` |
| RIP | `0x7ffef5aca5e8` |
| RIP module | `MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll` + `0x27a5e8` |
| RSP | `0x3bfb6ea040` |
| In-dump code page at RIP | Not present |
| On-disk `mono-2.0-bdwgc.dll` via the mounted game path | Not readable at investigation time |

`bdwgc` = Boehm–Demers–Weiser GC used by this Mono runtime.

## Native stack (qword scan, 400 qwords)

Only module: `mono-2.0-bdwgc.dll`.

Repeating pair (GC recursion / mark walk):

- `+0x27b2b4` / `+0x27b2d6`
- `+0x27b5c9`

No `cohtml*.dll`, no `VCRUNTIME140.dll`, no Burst runtime, no mod DLL.

## MoreDispatchMod

| Check | Result |
|-------|--------|
| Errors / exceptions | None |
| `HandleToggleTool` | Five times: 19:34:10, 19:34:30, 19:38:21, 19:46:21, 19:46:31 |
| Last 4 minutes | `OnToolChanged` for Selection / Default / Bulldoze only. `isDispatchTool=False` |
| Last log line | 20:24:41 `FireAccident` / `FireMedical` info ticks, then process stop |

Tool was used ~38 minutes before the crash. Not on the native stack.

## Open

- Mono function at `+0x27a5e8` / repeating `+0x27b2b4`/`+0x27b5c9`: no public symbols on this install.
- Whether the AV on `0xffffffffffffffff` is a collected object, a GC invariant break, or hardware.
- `IsTransitioningIn`/`Out` + unused-asset unload immediately before the fault: coincident, not proven causal.
