# Research: AccidentSite Rendering Overhead

> **Status**: Complete
> **Date**: 2026-02-22
> **Issue resolved**: #343

## Summary

Adding `AccidentSite` to building entities causes a native crash in Unity's Burst rendering jobs
after sustained presence (~37–57 seconds for 20 buildings). There is no reliably safe concurrent
limit for buildings with AccidentSite sustained in a small radius.

**Resolution**: Use Path D2 (direct ServiceDispatch injection without AccidentSite) for all
mod-created police dispatch to buildings.

---

## Three Rendering Cost Paths from AccidentSite

### 1. ECS Archetype Change → New Batch Group (one-time, per unique archetype)

Adding `AccidentSite` changes the entity's archetype (new component). The entity moves to a new
ECS chunk. `BatchManagerSystem.AllocateBuffersJob` detects the new chunk and allocates a GPU
instance buffer:

```csharp
// In BatchManagerSystem.AllocateBuffersJob.Execute()
m_NativeBatchInstances.AllocateInstanceBuffers(groupIndex, 16_777_216u, m_NativeBatchGroups);
```

`16_777_216u` is the max-instance-count capacity for the new batch group's GPU buffer. This is
a one-time cost per new archetype, but the batch group remains active as long as any entity
holds the archetype.

### 2. Culling Buffer Allocation (every culling pass — 3–5× per frame)

`BatchManagerSystem.AllocateCullingJob` runs on every culling pass (main camera + shadow
cascades). It allocates `Allocator.TempJob` buffers:

```csharp
// Size grows with total instances × LODs across ALL active batch groups
reference.visibleInstances = (int*)UnsafeUtility.Malloc(sizeof(int) * num2, ..., Allocator.TempJob);
reference.drawCommands     = (BatchDrawCommand*)UnsafeUtility.Malloc(..., Allocator.TempJob);
reference.drawRanges       = (BatchDrawRange*)UnsafeUtility.Malloc(..., Allocator.TempJob);
```

These are passed to Unity as `BatchCullingOutput`. Unity's rendering backend is responsible for
freeing them after GPU consumption. With more active batch instances (from AccidentSite entities),
these buffers are larger every frame.

### 3. Notification Icon (every frame — negligible)

`NotificationIconBufferSystem` processes entities with `Icon` component. AccidentSite causes an
Icon to be added to the building. Cost is O(1) per icon per frame — not the crash source.

---

## Crash Mechanism

`AddAccidentSiteSystem` is **not** the crash source. It uses a single `IJob` with `Allocator.Temp`
(auto-freed), merging all additions in one pass with no per-entity TempJob allocations.

The crash chain:

1. 20 buildings get AccidentSite → each moves to a new archetype chunk
2. `AllocateBuffersJob` creates N new active batch groups (N = distinct building prefab types)
3. New batch groups increase `activeGroupCount`
4. `AllocateCullingJob` allocates `TempJob` buffers sized by **total instances × LODs across
   ALL active groups** — larger now due to the new groups
5. Unity's GPU rendering backend must free these TempJob buffers after consuming them
6. GPU back-pressure from the sudden 16 MB buffer spike causes TempJob buffers to outlive
   their 4-frame budget
7. Unity reports: `JobTempAlloc has allocations that are more than maximum lifespan of 4 frames old`
8. Native crash in Burst rendering job (`UploadInstances`) — likely accessing memory before
   the previous TempJob allocations are fully freed

**Crash timeline** (observed):
- Addition: clean (AddAccidentSiteSystem handles in one IJob pass)
- Frames 1–N: culling TempJob buffers grow each culling pass
- ~37–57 seconds sustained: TempJob budget exhausted; native crash

---

## AccidentSiteFlags Cost Difference

`CrimeScene` vs `TrafficAccident` flags control which sub-meshes render (different crime tape
variants, different icon prefabs). Different sub-meshes may use different materials → different
batch groups. But the per-entity structural cost is identical: one archetype change, one batch
group registration, one notification icon.

No evidence that `CrimeScene | CrimeDetected` is significantly more expensive than `TrafficAccident`.

---

## Safe Concurrent Limit

**There is no reliably safe limit for sustained AccidentSite on buildings.**

The crash depends on total scene batch load (all active batch groups, not just AccidentSite ones).
A player with a dense city could crash even at 1–2 sustained AccidentSites if existing culling
buffers are near capacity.

Empirical data:
| Config | Crash time |
|---|---|
| 279 buildings (Area Crime naive) | ~37s |
| 20 buildings (hard cap) | ~46s |
| 20 buildings (batched, rate-limited) | ~57s |
| 1 building | No crash (limited testing) |

---

## Systems Decompiled

| Type | File |
|---|---|
| `Game.Events.AddAccidentSiteSystem` | `/tmp/AddAccidentSiteSystem.cs` |
| `Game.Rendering.BatchManagerSystem` | `/tmp/BatchManagerSystem.cs` |
| `Game.Rendering.BatchUploadSystem` | `/tmp/BatchUploadSystem.cs` |
| `Game.Rendering.NotificationIconBufferSystem` | `/tmp/NotificationIconBufferSystem.cs` |

(Not committed — decompiled output in /tmp)

---

## Conclusion

**Use Path D2 (direct ServiceDispatch injection without AccidentSite) for all building dispatch.**

Path D2 eliminates all three rendering cost paths:
- No AccidentSite = no archetype change
- No new batch group registration
- No culling buffer growth
- No notification icon (unless explicitly desired)

The only rendering effect with Path D2 is the police car's emergency lights (via `EffectsUpdated`
on the car entity), which is the intended visual behavior.
