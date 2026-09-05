---
name: cs2-crash-investigate
description: >
  Investigate a Cities: Skylines II process crash from Player.log, mod logs, and
  Crashpad minidumps. Classify native vs managed vs ANR, map RIP to a module,
  decide whether a mod is on the faulting stack, and write a facts-only case
  under research/topics/CrashInvestigations/ and site/crashes/.
  Use when the user says "investigate this crash", "game crashed", "look at the
  dump", "Player.log crash", "Crashpad", "/cs2-crash-investigate", or wants a
  new crash-investigation page.
---

# CS2 crash investigation

Do not narrate. Record facts. Stop when attribution is decided or evidence is exhausted.

Classification, attribution rules, and default paths: `research/topics/CrashInvestigations/README.md`.
Writeup shape: existing case in that folder and `site/crashes/`.

## Inputs

Ask only if a path is missing. Defaults for this machine are in the README.

Need, **in this copy order** (relaunch rotates `Player.log` → `Player-prev.log` and truncates `Logs/`):

1. Newest matching `*.dmp`
2. `{UserData}/Player.log` **and** `Player-prev.log`
3. `{UserData}/Logs/` (at least `UI.log`, `SceneFlow.log`, and any large/recent mod log)
4. Destination: `research/topics/CrashInvestigations/evidence/<YYYY-MM-DD>-<dump-prefix>/` (gitignored)

If `Player.log` has no `Native Crash Reporting` block, the crashed session is `Player-prev.log`.

## Procedure

1. **Copy first.** Then pick the session: `Native Crash Reporting` in the session log, or the newest dump if that banner is absent. No banner + dump code `0x0517A7ED` is an ANR, still a case. List other same-day dumps; do not merge ANR with a native `0xC0000005`/`0xC000001D`.
2. **Managed boundary.** Copy `Managed Stacktrace`. Empty (header only, no `at` frames) is a result — GC, game Burst, or Unity job thread — not a missing log. If frames exist, name the thread (`TaskScheduler` ctor lambda = Cohtml `WT_Layout`; thread-pool = `WT_Resources`; main-thread coroutine ≠ layout).
3. **Dump exception.** Thread id, code, CONTEXT RIP (prefer over ExceptionAddress), module+offset. For `0xC0000005`, also `ExceptionInformation[0]` (0=read, 1=write, 8=DEP) and `[1]` (address). Address `< 0x1000` is a null-base field read. `info0=8` and RIP within ~4 KiB of RSP is execute-stack (DEP); RIP will be unmapped as a module. Same `RIP-RSP` delta across dumps (e.g. `0xf78`) is the same smash, not a new class.
4. **Native callers.** Qword-scan RSP if MemoryList has the stack. Record unique module+offset; collapse repeating pairs. Classify each module with the README (game `lib_burst_generated.dll` vs mod `BurstRuntime/…/<Mod>_win_x86_64.dll`). Huge RVAs far from RIP are often data pointers, not callers.
5. **On-disk vs RIP.** Bytes at that RVA if the DLL is readable on the mount. Missing file or missing code page is a fact. Do not invent job names from nearby `Player.log` lines.
6. **Decompile** only managed wrappers that actually appear on the stack (`ilspycmd -t`, sequential). Skip if the managed stack is empty. Do not decompile Cohtml, Mono, or Burst native.
7. **Mod logs.** Same timestamps. Ticking until death ≠ managed exception. Last open/toggle time vs crash time; earlier use is not a stack hit.
8. **Coincident errors.** Resource 404 / `Invalid host locations map` / `Cannot abort request` are `DefaultResourceHandler` main-thread paths unless the native stack says otherwise. Lines immediately above `Native Crash Reporting` (`GPU instance buffer size changing`, unused-asset unload, `IsTransitioning*`, `TravelPurpose`) are coincident until the RIP module matches that system.

## Verdict (required)

Fill this table before writing files:

| Question | Allowed answers |
|----------|-----------------|
| Mod module on faulting stack? | Yes (name it) / No / Unknown (why) |
| Crash site (module + thread) | fact |
| Exact root cause | fact or "not determined" + what is missing |

A mod is the crashing *code* only if its managed type or Burst/native DLL is on the faulting stack. Game/runtime list is in the README.

Stop when: no mod on the stack **and** remaining cause needs Cohtml/Unity/Mono/Burst native source, dump code pages, a mods-off repro, or a hardware repro you cannot run.

## Writeup

Facts only. No story.

1. `research/topics/CrashInvestigations/<YYYY-MM-DD>-<short-id>.md`
2. Matching `site/crashes/<same>.html` (tables, `<pre>` stacks, scope verdict table)
3. Add the row to `research/topics/CrashInvestigations/README.md` and `site/crashes/index.html`
4. Add the sidebar link on both crash HTML pages

Do not invent stacks, modules, or exception codes. If a binary/page is missing, write that.
