# 2026-08-30 — Autosave write failure (`SerializerSystem` NRE → sharing violation)

> **Status**: Closed — pipeline named; ECS null inside `HighestEntityIndex` not named
> **Date**: 2026-08-30

Local copies (gitignored): `research/topics/CrashInvestigations/evidence/2026-08-30-save-write-fail/`
Save backups (gitignored): `research/topics/CrashInvestigations/evidence/saves-backup-gilbert/`

## Verdict

| Question | Answer |
|----------|--------|
| Did disk fill up? | No. Quota ~1.003 TB free. |
| Was Gilbert County 52 overwritten? | No. `Gilbert County 52.cok` still 290 797 989 bytes; zip contains Texture + SaveGameData + Metadata; `testzip()` clean. |
| What failed? | First autosave after loading Gilbert County 52. Serialize NRE, then zip step hit a locked temp file. |
| Exact ECS object that was null | Not determined. `EntityManager.HighestEntityIndex` IL `0x00053` (Unity.Entities). |

The on-screen “cannot write save” is the **second** exception. Root of this session: serialize already aborted.

## Session

| Field | Value |
|-------|-------|
| Boot | SceneFlow 15:50:17. Code mods **on**. |
| First city | New game `Cumberland Gap` 16:01:03. Manual save `Cumberland County` 17:21:02 (43 029 195 bytes, complete). Autosaves 16:06–17:16 all `Saving completed`. |
| Switch | 17:21:08 load `Gilbert County 52`. Loading completed 17:21:35. |
| Fail | 17:26:30 autosave `30-August-17-26-30` (~5 min after load). |
| Exit | 17:27:27 `GameManager destroyed`. Second NRE in `EntityComponentStore.Dispose` / `TerminateGame`. |

## Failed file on disk

`30-August-17-26-30.cok` is **32 953 bytes**. Zip contains only:

- `30-August-17-26-30.SaveGameMetadata`
- `30-August-17-26-30.SaveGameMetadata.cid`

No `.SaveGameData`. Metadata `cityName` is still Gilbert County (pop 416 295 vs 414 970 in `Gilbert County 52`). `autoSave: true`. `saveGameData` hash `1959d50f…` was never packed.

Do **not** load this file. `UserState.coc` `lastSaveGameMetadata` is `7c7317ac6c0c8117d4fc75267a2163a5` (mtime 20:27). Continue-last-save may point at the stub.

## Exception 1 — serialize (cause)

```
System update error during Serialize->SerializerSystem:
NullReferenceException
  at Unity.Entities.EntityManager.HighestEntityIndex ()
  at Colossal.Serialization.Entities.EntitySerializer`1.CreateEntityTable (...)
  at Colossal.Serialization.Entities.EntitySerializer`1.Serialize[...] (...)
  at Game.Serialization.SerializerSystem.OnUpdate ()
  at Game.Serialization.SaveGameSystem.OnUpdate ()
```

`Game.Serialization.SerializerSystem.OnUpdate` (`lib/Game.dll`): on `SystemUpdatePhase.Serialize` constructs `EntitySerializer<WriteBuffer>` and calls `Serialize<BinaryWriter, FormatTags>(context, m_Query, BufferFormat.CompressedZStd, PrefabData)`.

Unity Entities `HighestEntityIndex` walks every archetype chunk and reads entity indexes from `chunk.Buffer`. NRE there means a chunk/archetype pointer was already null — ECS store corrupt or disposed — not a path/permissions error.

## Exception 2 — zip (the popup)

```
IOException: \\?\C:\Users\micro\AppData\Local\Temp\Colossal Order\Cities Skylines II\273ac9a7646d46d2a07b17d09b3563a5\30-August-17-26-30.SaveGameData
The process cannot access the file because it is being used by another process.
  at FileSystemDataSource.GetReadStream
  at ZipPackageWriter.Add
  at PackageAsset.Save
  at GameManager.Save
  at AutoSaveSystem.AutoSave
```

After NRE, the write pipeline still tried to package the temp `.SaveGameData`. The handle was still open **in this process**. Not another app. Disk quota was logged 17:26:31 as 1.003 TB free.

## Exception 3 — exit

`TerminateGame` → `DestroyWorld` → `ChunkDataUtility.DeallocateBuffers` NRE. Matches “cannot quit, must kill process”: world teardown after a broken serialize.

## Intact Gilbert County copies

| File | Size | Zip | Notes |
|------|------|-----|-------|
| `Gilbert County 52.cok` | 290 797 989 | Texture + SaveGameData + Metadata, `testzip` OK | Last named save (Aug 29 23:17). Load this. |
| `Gilbert County 51.cok` | 290 725 254 | (not retested this pass) | Aug 19. |
| `30-August-17-26-30.cok` | 32 953 | metadata only | **Dead. Do not load.** |

Local copies of 52 and 51: `evidence/saves-backup-gilbert/` (gitignored).

## Open

- Which chunk/archetype `HighestEntityIndex` dereferenced (needs Unity.Entities + dump of the serialize thread; no native Crashpad dump for this — process did not take the Cohtml/Burst path).
- Whether same-process switch Cumberland → Gilbert County 52 left EntityManager invalid. Not proven. Cumberland autosaves in this process had succeeded for 80 minutes.
- Whether a code mod’s serializable component produced a bad chunk. Not on the managed stack (stack is vanilla serialize).
