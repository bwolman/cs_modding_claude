# 2026-09-04 — Cohtml layout DEP (`RIP-RSP=0xf78`)

> **Status**: Closed — same class as `ce956c0b` / `ad178da9`
> **Date**: 2026-09-04
> **Dump**: `3d3320c4-f393-4357-8b04-87197ddd0914.dmp`

Local copies (not in git): `research/topics/CrashInvestigations/evidence/2026-09-04-3d3320c4/`

## Verdict

| Question | Answer |
|----------|--------|
| Mod module on faulting stack? | No. |
| Crash site | Cohtml `WT_Layout` (`b__5_0`). `0xC0000005` DEP (`info0=8`). RIP unmapped on the stack. |
| Exact root cause | Same smash as [2026-08-16 DEP](2026-08-16-cohtml-layout-dep.md): **`RIP-RSP=0xf78`**. Not today’s `VCRUNTIME140`/`cohtml+0x404bb9` illegal-instruction, not `WT_Resources`/`v8`. |

## Session

| Field | Value |
|-------|-------|
| Game | 1.6.0f1; Cohtml 1.64.0.7. `disableCodeModding: false`. |
| Boot | 17:34:22. |
| Save loaded | `04-September-17-32-38` 17:37:42 (last autosave from the [WT_Resources crash](2026-09-04-cohtml-resources-execute-work.md)). Loading completed 17:38:15. |
| Last autosave | `04-September-18-32-12` completed 18:32:15 (291 313 732 bytes). |
| Crash | MoreDispatch last tick 18:32:26. Dump mtime Mac 21:32. ~54 min after load. |

## Same-boot dumps (not this crash)

ANRs, thread `0x2fa8`, RIP `BacktraceCrashpadWindows.dll+0x2dcd`: `72c936ef` 20:35, `72c90dc2` 20:37.

Do not merge with `13872449` (layout `0xC000001D` in `cohtml+0x404bb9`) or `9455b60c` (`WT_Resources` `v8` read AV).

## Match to `ce956c0b` / `ad178da9`

| Field | Aug 16 DEP | This |
|-------|------------|------|
| Managed stack | `b__5_0` → `ExecuteWork` | identical |
| Exception | DEP `info0=8` | identical |
| RIP-RSP | **`0xf78`** | **identical** |
| First stack qword | `cohtml+0x4050aa` | `0x7ffe537650aa` = **`+0x4050aa`** |
| Later offsets | `+0x40a03f`, `+0x40629a`, `+0x40ba69`, `+0x4061ec`, `+0x254600`, `+0x40b7f7`, `+0x403994`, `+0x406a6d` | identical |
| `RenoirCore+0xb4e4` | yes | yes |
| Bytes at RIP | (not always zeros) | 32 bytes of stack data, not a mapped module |

Not `RIP-RSP=0x12a8` ([41f2ba03](2026-08-21-cohtml-layout-dep.md)).

## Immediately before death

GPU 80→304 MB. `TravelPurpose 10` ×3. `stroke-dasharray` / `index.js:1:40289` also present (106 `index.js` lines) — that spam also ran on the resources-thread death; here RIP is execute-stack, not `v8`. Extra-renter 0.

UI 404s last 18:29:15 (same second as last `HandleToggleTool`). Main-thread `DefaultResourceHandler`. Coincident.

`UI.log` 18:32:12 render target 680×383 then 1920×1080 (autosave preview?). BetterTransitView button 18:32:15. Not on the native scan.

## Managed stack

```
at cohtml.Net.cohtmlNativePINVOKE:Library_ExecuteWork__SWIG_1
at cohtml.Net.Library:ExecuteWork
at Colossal.UI.UIManager:ExecuteWork
at Colossal.UI.TaskScheduler:<.ctor>b__5_0
```

## Dump exception

| Field | Value |
|-------|-------|
| Thread | `0x4e14` |
| Code | `0xC0000005` DEP (`info0=8`) |
| RIP / AV | `0x27cc4fecd0` (unmapped) |
| RSP | `0x27cc4fdd58` |
| RIP-RSP | `0xf78` (3960 bytes), adjacent 4 KiB pages |
| RSP+0x0 | `cohtml.WindowsDesktop.dll+0x4050aa` |

55 BurstRuntime loaded; none on the qword scan.

## MoreDispatchMod

`HandleToggleTool` ×3, last 18:29:15 (~3 min before death). Last tool Object Tool 18:31:51. Not on the layout native stack.

## Open

Same unnamed Cohtml smash as Aug 16. Third Cohtml native class today on Gilbert reloads: layout ILLEGAL_INSTRUCTION, resources `v8` read AV, layout DEP `0xf78`. Intact autosave `04-September-18-32-12`.
