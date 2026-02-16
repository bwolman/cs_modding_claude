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

## Open Questions

- [x] What is the complete archetype for a TrafficAccident event entity? — 6 components (see above)
- [x] Must `TargetElement` buffer exist at creation? — Yes, all downstream systems guard with `HasBuffer`
- [x] Are `Game.Common.Event` and `Game.Events.Event` the same type? — No, different structs in different namespaces
- [x] What does `PrefabRef` need to point to? — A real TrafficAccident prefab entity with TrafficAccidentData, FireData, CrimeData, EventData

## Sources

- Decompiled from: Game.dll — `Game.Prefabs.EventPrefab`, `Game.Prefabs.TrafficAccident`, `Game.Simulation.ImpactSystem`, `Game.Simulation.AccidentVehicleSystem`, `Game.Simulation.AccidentSiteSystem`, `Game.Simulation.AddAccidentSiteSystem`, `Game.Simulation.AccidentCreatureSystem`, `Game.Simulation.ObjectCollisionSystem`
- Related research: `research/topics/ToolRaycast/` (ECS patterns), `research/topics/ToolActivation/` (system lifecycle)
- Game version: Current as of 2026-02-15
