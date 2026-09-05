# 2026-08-21 — Cohtml layout DEP (RIP-RSP `0x12a8`)

> **Status**: Closed — attribution complete; Cohtml-internal cause not named
> **Date**: 2026-08-21
> **Dump**: `41f2ba03-37d9-410f-83a5-267e1d1f77f2.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-21-41f2ba03/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml layout thread (`TaskScheduler` `b__5_0` / `WT_Layout`). Native `0xC0000005` DEP (`info0=8`). RIP unmapped, in the stack address range, 32 bytes of zeros. |
| Exact root cause | Not determined. Same managed path as [2026-08-13 Cohtml layout](2026-08-13-cohtml-layout-execute-work.md) and [2026-08-16 DEP](2026-08-16-cohtml-layout-dep.md). DEP-on-stack again, but `RIP-RSP=0x12a8` not `0xf78`. |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Unity 2022.3.71f1; Cohtml 1.64.0.7 |
| OS / CPU / GPU | Windows 11; i9-14900KF; RTX 5080 D3D11 |
| Boot | MoreDispatch `OnLoad` 18:47:14. First sim ticks 18:51:11. |
| Save loaded | Not in `Player-prev.log`. `Logs/SceneFlow.log` / `UI.log` / `FileSystem.log` are the 19:34 relaunch. |
| Last autosave | `21-August-19-30-51` (mtime Mac 22:30) |
| Crash | Last MoreDispatch tick 19:32:49. Dump mtime Mac 22:32. |
| Session length | ~45 min (boot 18:47 → death ~19:32) |

## Same-day dumps (not this crash)

Launch ANRs, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, `0x0517A7ED`. Do not merge with this native `0xC0000005`.

Crashed-session boot (thread `0x3950`):

| Dump | mtime Mac |
|------|-----------|
| `57f47227-6e50-48d6-84ba-c2559beabc67` | 21:47 |
| `8ca96ab7-e219-407f-9f99-19f822e0beb9` | 21:50 |
| `ca69ba56-5ff0-4351-8b32-2a2860333a5d` | 21:51 |

Post-crash relaunch (thread `0x3470`):

| Dump | mtime Mac |
|------|-----------|
| `59d33b87-00cf-462d-b950-65203860a9e5` | 22:35 |
| `1b013ff0-ab1b-4c05-b46b-3426f6c656b3` | 22:36 |

## Match to prior Cohtml layout deaths

| Field | `ce956c0b` / `ad178da9` (2026-08-16) | `41f2ba03` (this) |
|-------|--------------------------------------|-------------------|
| Managed stack | `TaskScheduler` `b__5_0` → `Library_ExecuteWork__SWIG_1` | identical (same IL offsets) |
| Exception | `0xC0000005` DEP (`info0=8`) | identical |
| RIP | unmapped, stack range | unmapped, stack range; **32 bytes zeros at RIP** |
| RIP-RSP | **`0xf78`** (3960 bytes) | **`0x12a8`** (4776 bytes) |
| Page relation | adjacent 4 KiB pages | adjacent 4 KiB pages |
| First stack qword | `cohtml.WindowsDesktop.dll+0x4050aa` | identical `+0x4050aa` (`0x7ffb55f050aa`) |
| Shared later offsets | `+0x40a03f`, `+0x4061ec`, `+0x40b7f7`, `+0x406a6d`, `+0x40ba69`, `+0x254600`, `+0x403994` | present |
| `+0x40629a` | both Aug 16 dumps | **absent**; `+0x4062cc` instead |
| GPU buffer | 80→320 MB / 80→112 MB | 80→304 MB |
| `infoviews.zoneInfos` | yes / no | **absent** |
| `TravelPurpose` / extra-renter | mixed | **absent** |

`RIP-RSP=0x12a8` is not the Aug 16 smash gadget. Same class: layout thread, DEP execute-stack, Cohtml-only native callers near RSP.

## Immediately before death (`Player-prev.log`)

No timestamps on these lines. Order: GPU 80→128 MB, unused-asset unload, `FirstPersonCamera loaded!`, VehicleController missing property pack, SchoolOrganizer education-level warns, GPU 144→176 MB, more unloads, GPU 192 MB, **JobTempAlloc >4-frame leak**, GPU 208 MB, CSS `syntax error` near `5.000000e-02`, GPU 224→304 MB, three unused-asset unloads, `Native Crash Reporting`.

No main-thread `View_Advance` frames. No `TravelPurpose`. No extra-renter. No `infoviews.zoneInfos`.

404 `coui://uil/Standard/Alarm.svg` (Id 332) is `DefaultResourceHandler` main-thread coroutine. Coincident. Same URL on prior Cohtml cases.

GPU growth is coincident (not required: `ad178da9` died at 112 MB).

## Managed stack (dying thread)

```
at cohtml.Net.cohtmlNativePINVOKE:Library_ExecuteWork__SWIG_1
at cohtml.Net.Library:ExecuteWork
at Colossal.UI.UIManager:ExecuteWork
at Colossal.UI.TaskScheduler:<.ctor>b__5_0
```

`b__5_0` = dedicated `WT_Layout` thread. Same IL offsets as 2026-08-13 / 2026-08-16.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x4c78` |
| Code | `0xC0000005` (`ACCESS_VIOLATION`) |
| AV type | DEP / execute (`info0=8`) |
| AV address / RIP | `0xd7d1d0ea50` (unmapped as a module) |
| RSP | `0xd7d1d0d7a8` |
| RIP vs RSP | `0x12a8` (4776 bytes); RIP page `0xd7d1d0e000` off `0xa50`; RSP page `0xd7d1d0d000` off `0x7a8`. Adjacent 4 KiB pages. |
| Bytes at RIP | 32 zeros (MemoryList includes this page) |
| RSP+0x0 | `0x7ffb55f050aa` = `cohtml.WindowsDesktop.dll+0x4050aa` |
| RSP+0x68 | `0xd7d1d0ea50` (RIP stored on the stack) |

Executing zeros on the stack is blocked by NX/DEP. Corrupted return / function pointer, not a named Cohtml export.

`cohtml.WindowsDesktop.dll` is not on the Mac game mount. Offsets cannot be named from this machine.

## Native stack (qword scan from RSP, 10328 bytes)

Collapsed unique `cohtml.WindowsDesktop.dll` offsets, in first-seen order:

`+0x4050aa` (×8), `+0x40512b`, `+0x4051a7` (×5), `+0x394ef6`, `+0x404f0b`, `+0x4057ad`, `+0x40a03f`, `+0x4062cc`, `+0x40ba69`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d`, `+0x3609e2`, `+0x36762a`, `+0x47db9d`, `+0x254dd3`, `+0x25462a`, `+0x412228`, `+0x412d45`, `+0x4162b0`

Also on that scan (thread start / CRT, not RIP): `ntdll.dll`, `MSVCP140.dll`, `combase.dll`, `KERNELBASE.dll`, `KERNEL32.DLL`, `mono-2.0-bdwgc.dll`.

No mod `BurstRuntime`. No `lib_burst_generated.dll`. RIP not in `VCRUNTIME140`.

55 BurstRuntime DLLs are **loaded** in the process (including `BetterTransitView_win_x86_64.dll`, `MoveIt_win_x86_64.dll`, `Platter_win_x86_64.dll`). None appear on the faulting stack scan.

## MoreDispatchMod

No `HandleToggleTool` this session. No errors. Last tool `Object Tool` at 19:31:53 (`isDispatchTool=False`). Last lines 19:32:49 FireAccident/FireMedical ticks.

## Coincident load warnings (`Player-prev.log`)

Unknown prefab IDs include `ContentPrefab:Mod:81040`, `ContentPrefab:Mod:80942` (same missing OLD packs as the 2026-08-17 BTV `GetPrefab` case), `W7OldCemetery`, `JoesIndustrial*`. Load-time. Not on the layout native stack.

## Evidence gaps

Relaunch at 19:34 rotated `Player.log` → `Player-prev.log` and truncated `Logs/SceneFlow.log`, `UI.log`, `Modding.log`, `FileSystem.log`. Those copies are the next boot. MoreDispatch + RealisticTrips still contain the crashed session.

## Open

- What overwrote the layout-thread return address (`RIP-RSP=0x12a8` vs Aug 16 `0xf78`).
- Whether 2026-08-13 (`0xC000001D` / `VCRUNTIME140`), Aug 16 `0xf78` DEP, and this `0x12a8` DEP are the same Cohtml bug with different smash sites.
- Isolation (UI mods off, same autosave) not run.
