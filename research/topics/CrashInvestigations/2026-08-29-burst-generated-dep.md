# 2026-08-29 — `lib_burst_generated` DEP (`--disableCodeModding`)

> **Status**: Closed — attribution complete; Burst job not named
> **Date**: 2026-08-29
> **Dump**: `4f46a920-4a21-4315-839b-86596b60f2aa.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-08-29-4f46a920/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. Code modding disabled this boot. Zero `BurstRuntime` modules in the dump. |
| Crash site | Empty managed stack. RIP `lib_burst_generated.dll+0xc142`. Exception `0xC0000005` DEP (`info0=8`). Callers in that DLL and `UnityPlayer.dll`. |
| Exact root cause | Not determined. No Burst symbols. `lib_burst_generated.dll` not on the Mac game mount. Dump has no code page at RIP. |

`lib_burst_generated.dll` is the game's AOT Burst output (`Cities2_Data/Plugins/x86_64/`), not a mod `BurstRuntime/<hash>/<Mod>_win_x86_64.dll`.

Not the Cohtml `WT_Layout` / `VCRUNTIME140+0xdf8b` class ([2026-08-29 ExecuteWork](2026-08-29-cohtml-layout-execute-work.md) earlier the same day, code mods on). This isolation session ran ~22 min after load without that smash.

Same *family* as [2026-08-14 Burst AV](2026-08-14-burst-generated-access-violation.md) (`+0xa69d` read of `0x70`) and [2026-08-16 Burst AV](2026-08-16-burst-generated-access-violation.md) (`+0x8a01c9` read). Different RIP and AV type: this is DEP (`info0=8`) at `+0xc142` (6821 bytes after the 08-14 RIP).

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Unity 2022.3.71f1; Cohtml 1.64.0.7 |
| Flag | `disableCodeModding: true`. `Modding.log`: `Modding is disabled` 20:30:01. |
| Asset packs | Still on (20753 prefabs). |
| Save loaded | `29-August-20-19-49` (20:30:57). Loading completed 20:31:18. |
| Autosaves | `29-August-20-41-18`, `29-August-20-46-18`, `29-August-20-51-18` |
| Crash | Dump mtime Mac 23:53 ≈ 20:53. ~22 min after load. |
| UI.log | Vanilla Cohtml init only. No mod UI modules. |

## Same-boot dump (not this crash)

| Dump | mtime Mac | Exception | RIP |
|------|-----------|-----------|-----|
| `63146559-def9-4fb7-b2ba-583ada7fb13d` | 23:31 | `0x0517A7ED` ANR | `BacktraceCrashpadWindows.dll+0x2dcd` |

## Immediately before death (`Player.log`)

422 `Removed extra renter` lines (vanilla `RentAdjustSystem`). GPU 80→240 MB. Four `TravelPurpose 10` warnings. Unused-asset unloads. Then `Native Crash Reporting`.

No `ExecuteWork`. No `View_Advance`. No `JobTempAlloc`. Extra-renter / `TravelPurpose` are coincident (same as other crash classes).

## Managed stack (dying thread)

Empty (header only, no `at` frames). Native Burst / Unity job thread. Not a missing log.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x3668` |
| Code | `0xC0000005` (`ACCESS_VIOLATION`) |
| AV type | DEP / execute (`info0=8`) |
| ExceptionAddress / RIP | `0x7ffd5babc142` = `lib_burst_generated.dll+0xc142` (base `0x7ffd5bab0000`) |
| `ExceptionInformation[1]` | `0x7ffddbabc142` (`RIP XOR 0x80000000`) |
| RSP | `0x163c8fe8f0` |
| RSP+0x0 | `0` |
| Bytes at RIP in dump | not present |

## Native stack (qword scan from RSP, 5904 bytes)

`lib_burst_generated.dll`: `+0x15a965`, `+0x35bd200` (×6, data-range RVA), `+0x15bf90`, `+0x35c2c20`, `+0x15a1b6`, `+0x35bd217`

`UnityPlayer.dll`: `+0x6fd854`, `+0x578116`, `+0x1c9fbc0`, `+0x3566a5`, `+0x578432`, `+0x190e3d3`, `+0x578a20`, `+0x573e12`, `+0x57471b`, `+0x5747a9`, `+0x6b35bc`, `+0x1c3b9e0`, `+0x19b9bd0`

Then `KERNELBASE` / `KERNEL32` / `ntdll`. No `cohtml`. No mod Burst.

## Isolation vs earlier 08-29 Cohtml death

| Field | ExecuteWork `db5da537` (code mods on) | This `4f46a920` (code mods off) |
|-------|--------------------------------------|----------------------------------|
| `disableCodeModding` | false | **true** |
| Time after load | ~8 min | ~22 min |
| Managed stack | `b__5_0` → `ExecuteWork` | **empty** |
| RIP | `VCRUNTIME140+0xdf8b` | `lib_burst_generated+0xc142` |
| BurstRuntime in process | 55 | **0** |

The Cohtml `VCRUNTIME140+0xdf8b` smash did not recur on this isolation boot past the previous 2–8 min window. This death is game Burst. Does not name the Burst job. Does not prove asset packs are unrelated (they were still loaded).

## Open

- Burst function at `+0xc142` (needs Windows copy of `lib_burst_generated.dll` + dump code page).
- Whether 08-14 `+0xa69d` read and this `+0xc142` DEP are the same job.
- Cohtml ExecuteWork class vs code/UI mods: one isolation run, not a full repro matrix.
