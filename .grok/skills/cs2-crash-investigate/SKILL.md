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

Need:

1. `{UserData}/Player.log` and `Player-prev.log`
2. `{UserData}/Logs/`
3. `{UserData}/.cache/backtrace/crashpad/reports/*.dmp` (newest matching the session)
4. `{Game}/Cities2_Data/Managed/` and `Plugins/x86_64/`

## Procedure

1. **Pick the session.** Newest `Player.log` `Native Crash Reporting` block, or the dump the user names. List other same-day dumps; do not merge ANR (`0x0517A7ED`, `ANRException`) with a native crash.
2. **Managed boundary.** Copy the `Managed Stacktrace` from `Player.log`. Identify the thread (e.g. `TaskScheduler` ctor lambda = Cohtml `WT_Layout`; thread-pool = `WT_Resources`; main-thread coroutine = not the layout thread).
3. **Dump exception.** Parse minidump Exception stream: thread id, code, RIP. Resolve RIP to a loaded module + offset. Prefer CONTEXT RIP if it differs from ExceptionAddress.
4. **Native callers.** If MemoryList contains the stack, qword-scan from RSP and resolve each pointer to a module. Record only resolved return addresses.
5. **On-disk vs RIP.** If RIP is in a game/plugin DLL you can read, dump bytes at that RVA. If they are a normal epilogue/`ret` and the exception is `0xC000001D`, say so. If the dump has no code page at RIP, say so.
6. **Decompile only the managed wrappers on the stack** (`ilspycmd -t`, sequential, not parallel on the same DLL). Do not decompile Cohtml native.
7. **Mod logs.** Same timestamps. A log that keeps ticking until process death is not a managed exception. A UI that never logged its open/toggle path was not in use.
8. **Coincident errors.** Resource 404 / `Invalid host locations map` / `Cannot abort request` are `DefaultResourceHandler` main-thread paths unless the native stack says otherwise.

## Verdict (required)

Fill this table before writing files:

| Question | Allowed answers |
|----------|-----------------|
| Mod module on faulting stack? | Yes (name it) / No / Unknown (why) |
| Crash site (module + thread) | fact |
| Exact root cause | fact or "not determined" + what is missing |

A mod is the crashing *code* only if its managed type or Burst/native DLL is on the faulting stack. Game/runtime: `cohtml*.dll`, `VCRUNTIME140.dll`, `UnityPlayer.dll`, `ntdll.dll`, `BacktraceCrashpadWindows.dll`.

Stop when: no mod on the stack **and** remaining cause needs Cohtml/Unity native source, dump code pages, or a hardware repro you cannot run.

## Writeup

Facts only. No story.

1. `research/topics/CrashInvestigations/<YYYY-MM-DD>-<short-id>.md`
2. Matching `site/crashes/<same>.html` (tables, `<pre>` stacks, scope verdict table)
3. Add the row to `research/topics/CrashInvestigations/README.md` and `site/crashes/index.html`
4. Add the sidebar link on both crash HTML pages

Do not invent stacks, modules, or exception codes. If a binary/page is missing, write that.
