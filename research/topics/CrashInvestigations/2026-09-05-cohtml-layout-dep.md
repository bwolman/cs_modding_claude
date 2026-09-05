# 2026-09-05 — Cohtml layout DEP (`RIP-RSP=0x1458`)

> **Status**: Closed — same class as prior layout DEP; not the `0xf78` / `0x12a8` gadget
> **Date**: 2026-09-05
> **Dump**: `7b0a8cf8-85fe-42d4-bece-ecbc7ee3e673.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-09-05-7b0a8cf8/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml `WT_Layout` (`b__5_0`). `0xC0000005` DEP (`info0=8`). RIP unmapped on the stack. |
| Exact root cause | Same class as [2026-08-16](2026-08-16-cohtml-layout-dep.md) / [2026-08-21](2026-08-21-cohtml-layout-dep.md) / [2026-09-04](2026-09-04-cohtml-layout-dep.md) DEP. **`RIP-RSP=0x1458`** — not `0xf78`, not `0x12a8`. |

Not yesterday’s `UIUpdate`/`GCHandle` main-thread death. Not `VCRUNTIME140` / `cohtml+0x404bb9` illegal-instruction. Not `WT_Resources`/`v8`.

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1 (419.d6c6) [6216.19404]; Cohtml 1.64.0.7. `disableCodeModding: false`. `-developerMode`. |
| Boot | SceneFlow 10:43. Boot completed 10:46:07. MainMenu 10:46:10. |
| Save loaded | `04-September-19-36-14` 10:55:40 (last autosave from the [UIUpdate GCHandle](2026-09-04-uiupdate-gchandle.md) crash). Loading completed 10:56:14. Format tags include `NewWaterSources`. |
| Last autosave | `05-September-11-06-10` completed 11:06:13. |
| Crash | MoreDispatch last tick 11:07:35. Dump mtime Mac 14:07. ~11 min after load. |

Crashed session is live `Player.log`. Copied before rotate.

## Same-boot dumps (not this crash)

ANRs, thread `0x51d4`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`, `0x0517A7ED`, identical RSP `0xd4e56cede0`:

| Dump | mtime Mac | BurstRuntime |
|------|-----------|--------------|
| `305ed515-a9e4-4d38-9af7-4eef68d6c8e8` | 13:42 | 0 |
| `0a443954-0d5d-44e4-9823-f910aa26b57a` | 13:44 | 0 |
| `dd711b8d-0283-4571-b617-ec734e813344` | 13:46 | 55 |
| `4dfd7355-3bdd-431d-b0fb-da0641035790` | 13:55 | 55 |

Do not merge with this native `0xC0000005`.

## Match to prior layout DEP

| Field | Aug 16 / Sep 4 `0xf78` | Aug 21 `0x12a8` | This |
|-------|------------------------|-----------------|------|
| Managed stack | `b__5_0` → `ExecuteWork` | identical | identical |
| Exception | DEP `info0=8` | identical | identical |
| RIP | unmapped on stack | unmapped; 32 zeros | unmapped; 32 bytes of stack data, not zeros |
| RIP-RSP | **`0xf78`** | **`0x12a8`** | **`0x1458`** (5208 bytes) |
| Pages | adjacent 4 KiB | adjacent | adjacent (`…cd000` / `…ce000`) |
| RSP+0x0 | `cohtml+0x4050aa` | identical | identical (`0x7fffed1f50aa`) |
| Later offsets | `+0x40a03f`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d` | present | present |
| `+0x40629a` | yes | **absent**; `+0x4062cc` | **`+0x4062cc`** (same as Aug 21) |
| `RenoirCore+0xb4e4` | yes (Sep 4) | — | **absent** on this scan |

Same class: layout thread, execute-stack, Cohtml callers at RSP. Different smash offset.

## Immediately before death

GPU 80→304 MB. Extra-renter ×10. `TravelPurpose 10` ×1. Unused-asset unloads. Then `Native Crash Reporting`. Coincident.

`UI.log` last 11:07:34: Object Tool button load; `-inkscape-font-specification`; BetterTransitView button. Ticking until death ≠ stack hit.

## Managed stack

```
at cohtml.Net.cohtmlNativePINVOKE:Library_ExecuteWork__SWIG_1
at cohtml.Net.Library:ExecuteWork
at Colossal.UI.UIManager:ExecuteWork
at Colossal.UI.TaskScheduler:<.ctor>b__5_0
```

`b__5_0` = dedicated `WT_Layout` thread.

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x2834` |
| Code | `0xC0000005` DEP (`info0=8`) |
| RIP / AV | `0xd4e52ce830` (unmapped) |
| RSP | `0xd4e52cd3d8` |
| RIP-RSP | `0x1458` |
| RSP+0x0 | `cohtml.WindowsDesktop.dll+0x4050aa` |
| Bytes at RIP | present in dump (stack data, not a mapped module) |

55 BurstRuntime loaded; none on the qword scan. No `lib_burst_generated`. No `VCRUNTIME140`. No `v8.dll` on the scan.

`cohtml.WindowsDesktop.dll` is not on the Mac game mount.

## MoreDispatchMod

No `HandleToggleTool`. Last tool Object Tool 11:07:34 (Net Tool 11:07:27). Last ticks 11:07:35. Not on the layout native stack.

## Open

Unnamed Cohtml smash. Third observed `RIP-RSP` (`0xf78`, `0x12a8`, `0x1458`). Intact autosave `05-September-11-06-10`.
