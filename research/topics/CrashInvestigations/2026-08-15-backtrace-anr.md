# 2026-08-15 — Backtrace ANR (blocked thread)

> **Status**: Closed — attribution complete; hung code not identified
> **Date**: 2026-08-15
> **Dump**: `9bc2166b-359f-447c-aacc-85fa6d508b79.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-15-9bc2166b/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Not a native exception. Dump code `0x0517A7ED` (Backtrace ANR). RIP `BacktraceCrashpadWindows.dll+0x2dcd` (watchdog raise). Scanned stack: that DLL, `mono-2.0-bdwgc.dll`, `ntdll.dll`. |
| Exact root cause | Not determined. ANR RIP is the reporter, not the blocked work. No `Native Crash Reporting` block in `Player.log`. |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404] |
| OS / CPU / GPU | Windows 11; i9-14900KF; RTX 5080 D3D11 |
| Save loaded | Gilbert County 45 (14:34:47) |
| Last autosave | 15-August-14-45-14 (14:45:17) |
| Last `Player.log` lines | ~14:45–14:47 (GPU buffer 288 MB, `TravelPurpose 10`) |
| Dump mtime | 2026-08-15 17:47 (Mac view) |
| Session log | `{UserData}/Player.log` at copy (425 lines). No `Native Crash Reporting`. |

## Same-day dumps (not this in-game ANR)

All `0x0517A7ED`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`:

| Dump | mtime (Mac) | Thread | Notes |
|------|-------------|--------|-------|
| `3e9014d9-751e-4893-bcdd-28bc63970f15` | 17:21 | `0x20dc` | Previous session (`Player-prev.log` is allocator teardown). |
| `5d2ac3d9-8a64-4da1-83a4-74ca02f2d89a` | 17:31 | `0x3c38` | This boot (MainMenu ~14:31:56). |
| `0c85b1e1-a119-4b28-abbf-613656279648` | 17:34 | `0x3c38` | Save load Gilbert County 45. |
| `9bc2166b-359f-447c-aacc-85fa6d508b79` | 17:47 | `0x3c38` | **This case.** In-game, ~12 min after load. |

Dump strings: `ANRException: Blocked thread detected.`

## Last `Player.log` lines

```
GPU instance buffer size changing to: 288 MB
Worker 761451 had incorrect TravelPurpose 10!
src\common\pipes.cpp (900) : fatal stalled cross-thread pipe.
src\common\pipes.cpp (900) : Fatal assert; application exiting
_ExitOnFatalAssert
```

No `Native Crash Reporting`. Buffer this session: 80 → 288 MB. Also `IsTransitioningIn`/`Out` and unused-asset unloads earlier. Coincident.

`src\common\pipes.cpp` is Steam IPC. Stalled-pipe + `_ExitOnFatalAssert` after an ANR is the Steam client giving up on a hung process, not a second crash class.

Player observation: the game window/process looked closed while Steam still showed it running for a while. Fits this sequence (hang → UI gone or frozen → Steam pipe still up → `pipes.cpp` fatal → Steam marks the app exited).

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x3c38` (same as the two launch ANRs this boot) |
| Code | `0x0517A7ED` (Backtrace ANR, not NT status) |
| RIP | `BacktraceCrashpadWindows.dll+0x2dcd` |
| RSP | `0x698aa8f000` |
| Code page at RIP | Not in dump |

## Native stack (qword scan)

| Module | Role |
|--------|------|
| `BacktraceCrashpadWindows.dll` | Watchdog / raise (`+0x2dcd`, `+0x1b708`) |
| `mono-2.0-bdwgc.dll` | `+0x88110`, `+0x8aecc`, `+0x29504e` |
| `ntdll.dll` | `+0x1cca4` |

No mod BurstRuntime. No `lib_burst_generated.dll`. No `cohtml*`.

Mono frames on an ANR stack are not a GC AV (that was `0xC0000005` at `+0x27a5e8` on 2026-08-13).

## MoreDispatchMod

No `HandleToggleTool` this session. No errors. Last lines 14:47:35 FireAccident/FireMedical ticks.

## Coincident UI

| Time | Error |
|------|-------|
| 14:35:05 | 404 `coui://uil/Standard/Alarm.svg` |
| 14:38:59 | `Invalid host locations map` `W7MoreParksAssetPack/W7BaseballField.png` |
| 14:42:22, 14:44:46 | `Invalid host locations map` `W7RoundaboutAssetPack/W7Roundabouts.png` |

Main-thread `DefaultResourceHandler`. Not the ANR RIP.

## Open

- Which thread was actually blocked, and on what. Backtrace RIP is the detector.
- Whether thread `0x3c38` ANRs at boot and at 14:47 are the same hang class.
- GPU instance buffer at 288 MB vs Burst AV case (240 MB): not the same exception class.
