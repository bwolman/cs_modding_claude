# 2026-08-14 — `lib_burst_generated` access violation

> **Status**: Closed — attribution complete; Burst job not named
> **Date**: 2026-08-14
> **Dump**: `114e1625-d749-4591-a211-e4af5b020daf.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-14-114e1625/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Thread `0x4550`. RIP `lib_burst_generated.dll+0xa69d`. Callers in that DLL and `UnityPlayer.dll`. |
| Exact root cause | Not determined. No Burst symbols. `lib_burst_generated.dll` not readable on the Mac game mount. Dump has no code page at RIP. Managed stack empty. |

`lib_burst_generated.dll` is the game's AOT Burst output (`Cities2_Data/Plugins/x86_64/`), not a mod `BurstRuntime/<hash>/<Mod>_win_x86_64.dll`.

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404] |
| Unity | 2022.3.71f1 |
| OS / CPU / GPU | Windows 11 64-bit; i9-14900KF; RTX 5080 D3D11 |
| Launch | `Cities2.exe -developerMode` |
| Save loaded | `13-August-20-19-41` (20:59:05) |
| Last autosave | `13-August-21-04-34` (21:04:37) |
| Crash (game log clock) | ~21:08 |
| Dump mtime | 2026-08-14 00:08 (Mac view) |
| Session log | `{UserData}/Player.log` at copy time (378 lines). Copied. |
| `UI.log` / `SceneFlow.log` | Copied (not overwritten). |

## Same-session dumps (not this crash)

Launch ANRs, thread `0x5124`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, code `0x0517A7ED`:

| Dump | mtime |
|------|-------|
| `8fb8af87-1093-4347-b7ae-223d312b1248` | 23:57 |
| `ad8cd635-4e12-43d8-aea9-ede9cdb856d9` | 23:58 |
| `51067424-a6dc-4896-a799-c1d354397ac8` | 23:59 |

## Immediately before death (`Player.log`)

```
GPU instance buffer size changing to: 80 MB
...
GPU instance buffer size changing to: 208 MB
Worker 1246103 had incorrect TravelPurpose 10!
Unloading 1 Unused Serialized files ...
Unloading 43 unused Assets ... Loaded Objects now: 359678.
GPU instance buffer size changing to: 224 MB
GPU instance buffer size changing to: 240 MB
Native Crash Reporting
```

Buffer grew 80 → 240 MB this session.

## Crash block

```
Native Crash Reporting
Got a UNKNOWN while executing native code.

Managed Stacktrace:
=================================================================
```

Managed stack is empty.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x4550` |
| Code | `0xC0000005` (`ACCESS_VIOLATION`) |
| AV type | read (`info0=0`) |
| AV address | `0x70` |
| RIP | `0x7ff862c9a69d` = `lib_burst_generated.dll+0xa69d` |
| RSP | `0x645fdfee40` |
| In-dump code page at RIP | Not present |
| On-disk DLL via Mac mount | Not present / not readable |

`0x70` is a null-base struct field read (`*(null + 0x70)`).

## Native stack (qword scan)

Unique modules: `lib_burst_generated.dll`, `UnityPlayer.dll`, `KERNELBASE.dll`.

Recorded `lib_burst_generated` offsets: `+0xa69d` (RIP), `+0x2685d0`, `+0x26b72f`, `+0x267476`, `+0xb54e6b`, plus two large RVAs (`+0x35bf980`, `+0x35f2450`) likely data, not code.

No mod BurstRuntime DLL. No `cohtml*`. No `mono-2.0-bdwgc.dll`.

## MoreDispatchMod

| Check | Result |
|-------|--------|
| `HandleToggleTool` | Never this session |
| Errors | None |
| Last lines | 21:08:14 `FireAccident`/`FireMedical` info ticks, then process stop |

## Coincident UI (`UI.log`)

| Time | Error |
|------|-------|
| 20:59:24 | 404 `coui://uil/Standard/Alarm.svg` |
| 21:00:52, 21:01:55 | `Invalid host locations map` `coui://customassets/W7RoundaboutAssetPack/W7Roundabouts/W7Roundabouts.png` |

Main-thread `DefaultResourceHandler`. Not on the Burst stack.

## Open

- Burst function at `+0xa69d`: no symbols; DLL not on this mount.
- Whether the GPU instance-buffer growth is the same path as `BatchManagerSystem` / culling jobs (`research/topics/AccidentSiteRendering/`) is not proven from this dump.
- AV at `0x70`: which job and which null object — not determined.
