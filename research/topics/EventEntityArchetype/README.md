# Research: TrafficAccident Event Entity Archetype

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: The complete entity archetype for a TrafficAccident event entity — which components must be present at creation time for all downstream systems to process it correctly.

**Why**: Mods that programmatically create accident events need to know the exact archetype. Missing components cause silent failures — downstream systems guard with `HasBuffer`/`HasComponent` checks and skip entities that don't match.

**Boundaries**: Out of scope — how collisions are detected, vehicle physics, UI notifications, and save/load serialization of event entities.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Events | `Event` (persistent marker), `TrafficAccident` (type marker), `TargetElement` (buffer) |
| Game.dll | Game.Common | `Event` (short-lived command marker), `PrefabRef`, `Created`, `Updated` |
| Game.dll | Game.Prefabs | `EventPrefab`, `TrafficAccident` (ComponentBase), `EventData` |
| Game.dll | Game.Simulation | `ImpactSystem`, `AccidentVehicleSystem`, `AccidentSiteSystem`, `AddAccidentSiteSystem`, `AccidentCreatureSystem` |

## Critical Clarification: Two Different "Event" Types

The codebase has two distinct empty marker structs named `Event` in different namespaces:

| Type | Purpose | Lifetime |
|------|---------|----------|
| `Game.Common.Event` | Short-lived command/notification entities (Impact, AddAccidentSite, Ignite, Damage) | Created and destroyed within a few frames |
| `Game.Events.Event` | Persistent accident event entities (the TrafficAccident event that survives for the accident's duration) | Persists until the accident resolves |

Every system that creates temporary command entities (`ObjectCollisionSystem`, `AccidentVehicleSystem`, `AccidentSiteSystem`, `AccidentCreatureSystem`) uses `Game.Common.Event`.

`ImpactSystem` and `AddAccidentSiteSystem` query for `Game.Common.Event` to find these temporary commands.

The **TrafficAccident event entity** uses `Game.Events.Event` (not `Game.Common.Event`), as established by `EventPrefab.GetArchetypeComponents()`.

## Analysis

### How the TrafficAccident Event Entity is Created

The entity is NOT created by any of the downstream processing systems. It is created by an event spawning system using an `EntityArchetype` stored inside the **prefab entity** of a `TrafficAccident` prefab asset.

The archetype is assembled at prefab load time by `EventPrefab.RefreshArchetype()`, which:

1. Collects all `ComponentBase` children of the `EventPrefab` and calls `GetArchetypeComponents()` on each
2. Adds `Created` and `Updated` unconditionally
3. Stores the resulting `EntityArchetype` in `EventData.m_Archetype` on the prefab entity

For a `TrafficAccident` prefab, the chain of `GetArchetypeComponents()` calls is:

| Class | Adds |
|-------|------|
| `PrefabBase.GetArchetypeComponents()` | `PrefabRef` |
| `EventPrefab.GetArchetypeComponents()` | `Game.Events.Event` |
| `Game.Prefabs.TrafficAccident.GetArchetypeComponents()` | `Game.Events.TrafficAccident` (empty marker), `TargetElement` (DynamicBuffer) |
| `EventPrefab.RefreshArchetype()` unconditional | `Created`, `Updated` |

### Complete Minimum Archetype

```csharp
EntityArchetype trafficAccidentArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.PrefabRef>(),         // systems read PrefabRef to get TrafficAccidentData
    ComponentType.ReadWrite<Game.Events.Event>(),             // base event marker
    ComponentType.ReadWrite<Game.Events.TrafficAccident>(),   // traffic accident type marker
    ComponentType.ReadWrite<TargetElement>(),                  // buffer: downstream systems check HasBuffer
    ComponentType.ReadWrite<Game.Common.Created>(),           // ECS lifecycle
    ComponentType.ReadWrite<Game.Common.Updated>()            // ECS lifecycle
);
```

### TargetElement Buffer: Must Exist at Creation

**The `TargetElement` buffer MUST be present on the entity archetype at creation time.** All four downstream systems guard their operations with `HasBuffer` checks — if the buffer is absent, those code paths are skipped entirely:

| System | Guard Check | Consequence if Missing |
|--------|------------|----------------------|
| `ImpactSystem` | `m_TargetElements.HasBuffer(involvedInAccident.m_Event)` | Target entity gets `InvolvedInAccident` but is never tracked in the event's target list — breaks all downstream systems |
| `AccidentVehicleSystem` | `m_TargetElements.HasBuffer(involvedInAccident.m_Event)` | Skips entire stopped-vehicle branch: no `AddAccidentSite` command, no notifications, no injuries |
| `AccidentSiteSystem` | `m_TargetElements.HasBuffer(accidentSite.m_Event)` | Cannot count participants, cannot detect chain-reaction eligibility, cannot determine police dispatch need |
| `AddAccidentSiteSystem` | `m_TargetElements.HasBuffer(accidentSite.m_Event)` | Site added to road edge but event entity never knows about it — breaks `AccidentSiteSystem` |

### PrefabRef Requirements

`PrefabRef` must point to a real `TrafficAccident` prefab entity that has:

| Component on Prefab Entity | Used By | For |
|---------------------------|---------|-----|
| `TrafficAccidentData` | `AccidentSiteSystem` | Chain-reaction behavior, severity thresholds |
| `FireData` | `AccidentVehicleSystem` | Fire probability on involved vehicles |
| `CrimeData` | `AccidentSiteSystem` | Crime timer for the accident site |
| `EventData` | Event spawning system | `m_Archetype` (the archetype itself), `m_ConcurrentLimit` |

If `PrefabRef` is missing or points to a non-existent prefab, systems silently skip fire and chain-reaction logic but don't crash.

### Components Each Downstream System Reads from the Event Entity

#### `ImpactSystem` (processes `Impact` command entities)

| Access | Component/Buffer | Entity |
|--------|-----------------|--------|
| `HasBuffer` | `TargetElement` | accident event entity (`impact.m_Event`) |
| `TryAddUniqueValue` | `TargetElement` | accident event entity |

#### `AccidentVehicleSystem` (processes vehicles with `InvolvedInAccident`)

| Access | Component/Buffer | Entity |
|--------|-----------------|--------|
| `HasBuffer` + read | `TargetElement` | accident event entity |
| `HasComponent` + read | `PrefabRef` | accident event entity |
| lookup via `PrefabRef` | `FireData` | prefab entity |

#### `AccidentSiteSystem` (processes road edges with `AccidentSite`)

| Access | Component/Buffer | Entity |
|--------|-----------------|--------|
| `HasBuffer` + read | `TargetElement` | accident event entity |
| `HasComponent` + read | `PrefabRef` | accident event entity |
| lookup via `PrefabRef` | `TrafficAccidentData` | prefab entity |
| lookup via `PrefabRef` | `CrimeData` | prefab entity |

#### `AddAccidentSiteSystem` (processes `AddAccidentSite` command entities)

| Access | Component/Buffer | Entity |
|--------|-----------------|--------|
| `HasBuffer` + `TryAddUniqueValue` | `TargetElement` | accident event entity |

### Temporary Command Entity Archetypes

All use `Game.Common.Event` (not `Game.Events.Event`):

```csharp
// ObjectCollisionSystem, AccidentVehicleSystem, AccidentSiteSystem:
m_EventImpactArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<Impact>()
);

// AccidentVehicleSystem:
m_AddAccidentSiteArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<AddAccidentSite>()
);

m_EventIgniteArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<Ignite>()
);

// AccidentCreatureSystem:
m_AddProblemArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<AddHealthProblem>()
);

// AccidentSiteSystem:
m_PoliceRequestArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<ServiceRequest>(),
    ComponentType.ReadWrite<PoliceEmergencyRequest>(),
    ComponentType.ReadWrite<RequestGroup>()
);
```

## Verdict

**The complete TrafficAccident event entity archetype requires 6 components, and all must be present at creation time.**

| Component | Required | Why |
|-----------|----------|-----|
| `PrefabRef` | Yes | All downstream systems look up prefab data (TrafficAccidentData, FireData, CrimeData) |
| `Game.Events.Event` | Yes | Base event marker — semantic contract for persistent event entities |
| `Game.Events.TrafficAccident` | Yes | Type marker — queries filtering for TrafficAccident type need this |
| `TargetElement` (buffer) | **Critical** | All 4 downstream systems guard with `HasBuffer`. Without it, the entire accident pipeline silently fails |
| `Created` | Yes | ECS lifecycle — change-detection queries may skip the entity without it |
| `Updated` | Yes | ECS lifecycle — same as Created |

### What a Mod Would Miss If Creating the Event Entity Programmatically

| Missing Component | Consequence |
|------------------|-------------|
| `TargetElement` buffer | **Fatal** — all downstream systems skip entity. No vehicle tracking, no accident sites, no police dispatch, no chain reactions |
| `PrefabRef` | Systems silently skip fire and chain reactions but don't crash |
| `Game.Events.TrafficAccident` marker | Entity won't match queries filtering for TrafficAccident type |
| `Game.Events.Event` marker | Future systems filtering on Event component won't match |
| `Created` / `Updated` | Systems using change-detection queries may skip the entity |

### Recommendation for Mods

**Option A (Recommended): Use the Impact event entity approach.** Create an `Impact` event entity with `Game.Common.Event` + `Impact`, setting `m_Event` to point to an existing accident event entity that was spawned by the game's own event system.

**Option B: Create a full accident event entity:**

```csharp
// At system OnCreate:
m_TrafficAccidentEventArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<PrefabRef>(),
    ComponentType.ReadWrite<Game.Events.Event>(),
    ComponentType.ReadWrite<Game.Events.TrafficAccident>(),
    ComponentType.ReadWrite<TargetElement>(),
    ComponentType.ReadWrite<Created>(),
    ComponentType.ReadWrite<Updated>()
);

// At runtime:
var eventEntity = commandBuffer.CreateEntity(m_TrafficAccidentEventArchetype);
commandBuffer.SetComponent(eventEntity, new PrefabRef { m_Prefab = trafficAccidentPrefabEntity });

// Then create an Impact entity referencing it:
var impactEntity = commandBuffer.CreateEntity(m_EventImpactArchetype);
commandBuffer.SetComponent(impactEntity, new Impact {
    m_Event = eventEntity,
    m_Target = targetVehicle,
    m_VelocityDelta = lateralForce,
    m_AngularVelocityDelta = spinForce,
    m_Severity = 5f
});
```

The `PrefabRef` must point to a real `TrafficAccident` prefab entity that has `TrafficAccidentData` and `EventData` components, otherwise `AccidentSiteSystem` will fail to do chain-reaction calculations and fire probability lookup will return defaults.

## Examples

### Example 1: Understanding the Two Event Namespaces

The most common source of confusion is that CS2 has two different `Event` structs. This example shows the distinction and when to use each.

```csharp
using Game.Common;   // Event = short-lived command marker (consumed in 1-2 frames)
using Game.Events;   // Event = persistent event marker (lives for duration of accident)

// WRONG: Using Game.Common.Event for a persistent accident event entity.
// Downstream systems query for Game.Events.Event — this entity won't match.
EntityArchetype wrongArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),            // <-- wrong namespace
    ComponentType.ReadWrite<Game.Events.TrafficAccident>(),
    ComponentType.ReadWrite<TargetElement>()
);

// CORRECT: Use Game.Events.Event for persistent event entities.
EntityArchetype correctArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Events.Event>(),            // <-- persistent marker
    ComponentType.ReadWrite<Game.Events.TrafficAccident>(),
    ComponentType.ReadWrite<TargetElement>(),
    ComponentType.ReadWrite<PrefabRef>(),
    ComponentType.ReadWrite<Created>(),
    ComponentType.ReadWrite<Updated>()
);

// Use Game.Common.Event only for temporary command entities like Impact:
EntityArchetype impactArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),            // <-- short-lived command
    ComponentType.ReadWrite<Impact>()
);
```

### Example 2: Creating a Full TrafficAccident Event Entity from a Mod System

This shows how a custom `GameSystemBase` would set up archetypes during `OnCreate` and spawn a complete TrafficAccident event entity at runtime.

```csharp
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;

public partial class MyAccidentSpawnerSystem : GameSystemBase
{
    private EntityArchetype _trafficAccidentArchetype;
    private EntityArchetype _impactArchetype;
    private EntityQuery _prefabQuery;
    private EndFrameBarrier _endFrameBarrier;

    protected override void OnCreate()
    {
        base.OnCreate();

        _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

        // The 6-component archetype required for a persistent TrafficAccident event entity.
        // All 6 must be present at creation time — downstream systems guard with HasBuffer
        // and HasComponent checks, and silently skip entities missing any of these.
        _trafficAccidentArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<PrefabRef>(),                    // links to prefab data
            ComponentType.ReadWrite<Game.Events.Event>(),            // persistent event marker
            ComponentType.ReadWrite<Game.Events.TrafficAccident>(),  // type marker
            ComponentType.ReadWrite<TargetElement>(),                // buffer for involved entities
            ComponentType.ReadWrite<Created>(),                      // ECS lifecycle
            ComponentType.ReadWrite<Updated>()                       // ECS lifecycle
        );

        // Temporary Impact command entity archetype — consumed by ImpactSystem within 1-2 frames.
        _impactArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<Game.Common.Event>(),
            ComponentType.ReadWrite<Impact>()
        );

        // Query to find the TrafficAccident prefab entity at runtime.
        // The prefab entity holds TrafficAccidentData, FireData, CrimeData, and EventData.
        _prefabQuery = GetEntityQuery(
            ComponentType.ReadOnly<TrafficAccidentData>(),
            ComponentType.ReadOnly<EventData>()
        );
    }

    protected override void OnUpdate()
    {
        // Find the game's TrafficAccident prefab entity.
        // PrefabRef on the event entity MUST point to this — it carries
        // TrafficAccidentData, FireData, and CrimeData that downstream systems read.
        if (_prefabQuery.IsEmptyIgnoreFilter)
            return;

        Entity prefabEntity = _prefabQuery.GetSingletonEntity();

        var commandBuffer = _endFrameBarrier.CreateCommandBuffer();

        // Step 1: Create the persistent event entity with the full archetype.
        // The TargetElement DynamicBuffer is automatically created (empty) because
        // it's part of the archetype — no need to AddBuffer separately.
        Entity eventEntity = commandBuffer.CreateEntity(_trafficAccidentArchetype);
        commandBuffer.SetComponent(eventEntity, new PrefabRef { m_Prefab = prefabEntity });

        // Step 2: Create an Impact command entity referencing our event entity.
        // ImpactSystem will pick this up, add InvolvedInAccident to the target,
        // and append the target to the event's TargetElement buffer.
        Entity impactEntity = commandBuffer.CreateEntity(_impactArchetype);
        commandBuffer.SetComponent(impactEntity, new Impact
        {
            m_Event = eventEntity,        // links this impact to our event
            m_Target = targetVehicle,     // the vehicle entity involved
            m_VelocityDelta = 5f,
            m_AngularVelocityDelta = 1f,
            m_Severity = 5f
        });
    }
}
```

### Example 3: How the Archetype Is Built at Prefab Load Time

The game assembles event entity archetypes automatically via `EventPrefab.RefreshArchetype()`. This example traces the call chain to show where each component comes from. Understanding this is useful when researching other event types beyond TrafficAccident.

```csharp
// EventPrefab.RefreshArchetype() collects components from all ComponentBase children:
//
// Call chain for a TrafficAccident prefab:
//
//   PrefabBase.GetArchetypeComponents()
//       -> adds: PrefabRef
//
//   EventPrefab.GetArchetypeComponents()
//       -> calls base (PrefabBase) first
//       -> adds: Game.Events.Event
//
//   Game.Prefabs.TrafficAccident.GetArchetypeComponents()    (ComponentBase child)
//       -> adds: Game.Events.TrafficAccident
//       -> adds: TargetElement (DynamicBuffer)
//
//   RefreshArchetype() then unconditionally adds:
//       -> Created
//       -> Updated
//
// Result stored in EventData.m_Archetype on the prefab entity:
//   { PrefabRef, Game.Events.Event, Game.Events.TrafficAccident, TargetElement, Created, Updated }

// To research a DIFFERENT event type (e.g., a fire event), decompile its ComponentBase
// and look at its GetArchetypeComponents() override. The pattern is always the same:
//   EventPrefab base components + type-specific ComponentBase additions + Created/Updated
```

### Example 4: Creating Temporary Command Entities (the Command-Entity Pattern)

Downstream systems communicate through short-lived command entities tagged with `Game.Common.Event`. Each command entity carries a payload struct and is consumed within 1-2 frames by a dedicated processing system. This pattern is how the accident pipeline chains its stages together.

```csharp
// The command-entity pattern: create a short-lived entity with Game.Common.Event
// and a payload component. A dedicated system queries for that payload component
// and processes/destroys the entity.

// --- Impact command (consumed by ImpactSystem) ---
// Signals that a vehicle was involved in a collision.
// ImpactSystem reads Impact.m_Event, looks up the event entity's TargetElement buffer,
// and adds the target to it via TryAddUniqueValue.
EntityArchetype impactArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<Impact>()
);
Entity impact = commandBuffer.CreateEntity(impactArchetype);
commandBuffer.SetComponent(impact, new Impact
{
    m_Event = accidentEventEntity,  // the persistent Game.Events.Event entity
    m_Target = vehicleEntity,
    m_Severity = 5f
});

// --- AddAccidentSite command (consumed by AddAccidentSiteSystem) ---
// Requests creation of an accident site marker on a road edge.
// AddAccidentSiteSystem reads the command, creates the site, then appends
// the site entity to the event's TargetElement buffer.
EntityArchetype addSiteArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<AddAccidentSite>()
);
Entity addSite = commandBuffer.CreateEntity(addSiteArchetype);
commandBuffer.SetComponent(addSite, new AddAccidentSite
{
    m_Event = accidentEventEntity,
    m_Target = buildingEntity,
    m_Flags = AccidentSiteFlags.TrafficAccident
});

// --- Ignite command (consumed by a fire system) ---
// Requests fire ignition on a vehicle as a chain-reaction effect.
// AccidentVehicleSystem creates these when fire probability check passes.
EntityArchetype igniteArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<Ignite>()
);
Entity ignite = commandBuffer.CreateEntity(igniteArchetype);
commandBuffer.SetComponent(ignite, new Ignite
{
    m_Target = vehicleEntity
});
```

### Example 5: Why TargetElement Must Be in the Archetype (Not Added Later)

This example demonstrates the critical timing issue with `TargetElement`. Adding it after entity creation causes a race condition with downstream systems.

```csharp
// WRONG: Creating the entity without TargetElement and adding it later.
// ImpactSystem runs in the same frame and checks HasBuffer BEFORE your
// AddBuffer command is played back. The buffer check fails, and the
// target entity never gets tracked.
EntityArchetype incompleteArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<PrefabRef>(),
    ComponentType.ReadWrite<Game.Events.Event>(),
    ComponentType.ReadWrite<Game.Events.TrafficAccident>(),
    ComponentType.ReadWrite<Created>(),
    ComponentType.ReadWrite<Updated>()
    // Missing: TargetElement
);

Entity eventEntity = commandBuffer.CreateEntity(incompleteArchetype);
commandBuffer.AddBuffer<TargetElement>(eventEntity);  // Too late — race condition

// All four downstream systems perform this guard check:
//   if (m_TargetElements.HasBuffer(involvedInAccident.m_Event))
//
// If HasBuffer returns false (because the buffer hasn't been added yet):
//   - ImpactSystem: target entity gets InvolvedInAccident but is never tracked
//   - AccidentVehicleSystem: no AddAccidentSite command, no notifications, no injuries
//   - AccidentSiteSystem: cannot count participants, no police dispatch
//   - AddAccidentSiteSystem: site added to road but event doesn't know about it

// CORRECT: Include TargetElement in the archetype so the buffer exists immediately.
EntityArchetype completeArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<PrefabRef>(),
    ComponentType.ReadWrite<Game.Events.Event>(),
    ComponentType.ReadWrite<Game.Events.TrafficAccident>(),
    ComponentType.ReadWrite<TargetElement>(),    // <-- in archetype, exists from frame 0
    ComponentType.ReadWrite<Created>(),
    ComponentType.ReadWrite<Updated>()
);
```

## Open Questions

- [x] What is the complete archetype for a TrafficAccident event entity? — 6 components (see above)
- [x] Must `TargetElement` buffer exist at creation? — Yes, all downstream systems guard with `HasBuffer`
- [x] Are `Game.Common.Event` and `Game.Events.Event` the same type? — No, different structs in different namespaces
- [x] What does `PrefabRef` need to point to? — A real TrafficAccident prefab entity with TrafficAccidentData, FireData, CrimeData, EventData

## Sources

- Decompiled from: Game.dll — `Game.Prefabs.EventPrefab`, `Game.Prefabs.TrafficAccident`, `Game.Simulation.ImpactSystem`, `Game.Simulation.AccidentVehicleSystem`, `Game.Simulation.AccidentSiteSystem`, `Game.Simulation.AddAccidentSiteSystem`, `Game.Simulation.AccidentCreatureSystem`, `Game.Simulation.ObjectCollisionSystem`
- Related research: `research/topics/ToolRaycast/` (ECS patterns), `research/topics/ToolActivation/` (system lifecycle)
- Game version: Current as of 2026-02-15
