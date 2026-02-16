# Research: Fire Ignition

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How fires are triggered on buildings and trees in CS2 — specifically whether building fires and forest fires use separate event types or a unified system, and how hazard probability is calculated for each.

**Why**: To build mods that programmatically ignite buildings or trees, modify fire probability, or intercept/prevent fires.

**Boundaries**: Not covering fire rescue dispatch logic, fire station pathfinding, or fire visual effects. Fire *spread* mechanics are documented as they share the same system, but the primary focus is ignition.

**Key finding**: Building fires and forest fires are **NOT separate event types**. They share one unified system (`FireHazardSystem` → `IgniteSystem` → `FireSimulationSystem`) with different hazard calculation paths in `EventHelpers.FireHazardData.GetFireHazard()`.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Events | `Ignite`, `OnFire`, `Fire` (tag), `TargetElement`, `IgniteSystem` |
| Game.dll | Game.Simulation | `FireSimulationSystem`, `FireHazardSystem`, `EventHelpers` (contains `FireHazardData`, `StructuralIntegrityData`) |
| Game.dll | Game.Prefabs | `Fire` (ComponentBase), `FireData`, `FireConfigurationData`, `FireConfigurationPrefab`, `DestructibleObjectData` |

## Component Map

### `Ignite` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The fire event entity this ignition belongs to |
| m_Target | Entity | The entity to ignite (building or tree) |
| m_Intensity | float | Starting fire intensity |
| m_RequestFrame | uint | Simulation frame when rescue should be requested |

Temporary event component. Created as a standalone entity with `Event` tag, processed by `IgniteSystem` within one frame, then destroyed. This is the primary way to start a fire on an entity.

*Source: `Game.dll` → `Game.Events.Ignite`*

### `OnFire` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The fire event entity |
| m_RescueRequest | Entity | Reference to the active FireRescueRequest entity |
| m_Intensity | float | Current fire intensity (0 = extinguished, up to 100) |
| m_RequestFrame | uint | Frame when fire rescue should be requested (0 = not yet initialized) |

Placed on burning entities (buildings, trees) by `IgniteSystem`. Persisted — survives save/load via `ISerializable`. Intensity escalates over time via `FireSimulationSystem`.

*Source: `Game.dll` → `Game.Events.OnFire`*

### `Fire` (Game.Events) — Tag Component

| Field | Type | Description |
|-------|------|-------------|
| *(empty)* | — | Marker struct, no fields |

Empty marker component (`IEmptySerializable`). Tags fire event entities (the event itself, not the burning target). Added to the event entity archetype by `Game.Prefabs.Fire.GetArchetypeComponents()`.

*Source: `Game.dll` → `Game.Events.Fire`*

### `TargetElement` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Entity | Entity | Reference to a target entity involved in this event |

Buffer element (`IBufferElementData`) on fire event entities. Lists all entities currently burning as part of this fire event. Same pattern used by `TrafficAccident` events.

*Source: `Game.dll` → `Game.Events.TargetElement`*

### `FireData` (Game.Prefabs)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_RandomTargetType | EventTargetType | — | Which entity type this fire prefab targets (`Building` or `WildTree`) |
| m_StartProbability | float | 0.01 | Probability multiplier for spontaneous ignition |
| m_StartIntensity | float | 1.0 | Initial fire intensity when ignited |
| m_EscalationRate | float | 1/60 (~0.0167) | Rate of intensity increase per tick |
| m_SpreadProbability | float | 1.0 | Probability multiplier for spreading to nearby objects |
| m_SpreadRange | float | 20.0 | Maximum distance (meters) fire can spread |

Per-prefab configuration for fire events. Set by `Game.Prefabs.Fire` (ComponentBase) during initialization.

*Source: `Game.dll` → `Game.Prefabs.FireData`*

### `FireConfigurationData` (Game.Prefabs)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_FireNotificationPrefab | Entity | — | Notification icon for active fires |
| m_BurnedDownNotificationPrefab | Entity | — | Notification icon for burned-down buildings |
| m_DefaultStructuralIntegrity | float | 3,000 | HP for non-building objects (trees, props) |
| m_BuildingStructuralIntegrity | float | 15,000 | HP for buildings without level data |
| m_StructuralIntegrityLevel1 | float | 12,000 | HP for level 1 buildings |
| m_StructuralIntegrityLevel2 | float | 13,000 | HP for level 2 buildings |
| m_StructuralIntegrityLevel3 | float | 14,000 | HP for level 3 buildings |
| m_StructuralIntegrityLevel4 | float | 15,000 | HP for level 4 buildings |
| m_StructuralIntegrityLevel5 | float | 16,000 | HP for level 5 buildings |
| m_ResponseTimeRange | Bounds1 | [3, 30] | Min/max seconds before fire rescue is requested |
| m_TelecomResponseTimeModifier | float | -0.15 | Response time modifier with telecom coverage (negative = faster) |
| m_DarknessResponseTimeModifier | float | 0.1 | Response time modifier at night (positive = slower) |
| m_DeathRateOfFireAccident | float | 0.01 | Death probability per fire accident |

Global singleton. Set by `FireConfigurationPrefab` during `LateInitialize`. The prefab also contains two `AnimationCurve` fields (`m_TemperatureForestFireHazard`, `m_NoRainForestFireHazard`) that are evaluated at runtime but not stored in the ECS component.

*Source: `Game.dll` → `Game.Prefabs.FireConfigurationData`*

## System Map

### `FireHazardSystem` (Game.Simulation)

- **Base class**: GameSystemBase (also ISerializable — persists `noRainDays`)
- **Update interval**: 4096 frames (~68 seconds at 60fps)
- **Queries**:
  - Flammable: `(Building OR Tree)`, excluding `FireStation, Placeholder, OnFire, Deleted, Overridden, Temp`
  - Fire prefabs: `EventData + FireData`, excluding `Locked`
- **Reads**: Building, Tree, Owner, CurrentDistrict, Damaged, UnderConstruction, Transform, PrefabRef, EventData, FireData, PlaceableObjectData, DestructibleObjectData, SpawnableBuildingData, ZonePropertiesData, ServiceCoverage, DistrictModifier
- **Writes**: Creates fire event entities via EntityCommandBuffer
- **Key methods**:
  - `OnUpdate()` — Tracks `noRainDays` (resets to 0 when raining, increments by 1/64 per update otherwise). Updates `FireHazardData` with current temperature and rain data. Schedules `FireHazardJob`.
  - `FireHazardJob.Execute()` — Only processes 1-in-64 chunks per update (random skip). Two paths:
    - **Building path**: Skips owned buildings (unless floating). Skips under-construction. Calls `FireHazardData.GetFireHazard(building)`. Tries `TryStartFire` with `EventTargetType.Building`.
    - **Tree path**: Only wild trees (no Owner). Requires `naturalDisasters` city setting. Calls `FireHazardData.GetFireHazard(tree)`. Tries `TryStartFire` with `EventTargetType.WildTree`.
  - `TryStartFire()` — Matches fire prefab by `m_RandomTargetType`. Final probability: `fireHazard * fireData.m_StartProbability`. Roll: `random.NextFloat(10000) < probability`. Creates fire event entity.

### `IgniteSystem` (Game.Events)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every frame)
- **Queries**:
  - EntityQuery requiring: `[Ignite, Event]`
- **Reads**: Ignite, PrefabRef, Building, InstalledUpgrade, OnFire (existing)
- **Writes**: OnFire (adds/updates), TargetElement (adds target to event buffer), BatchesUpdated, AddEventJournalData
- **Key methods**:
  - `IgniteFireJob.Execute()` — Collects all `Ignite` events into a `NativeParallelHashMap<Entity, OnFire>` keyed by target entity (deduplicating — keeps highest intensity). For each unique target:
    - **Already on fire**: Updates intensity if higher, preserves existing `m_RescueRequest` and earliest `m_RequestFrame`. Adds target to new event's `TargetElement` buffer if event changed.
    - **Not on fire**: Adds `OnFire` component + `BatchesUpdated`. Adds target to event's `TargetElement` buffer.
    - **Buildings**: Creates journal data for damage tracking. Marks non-building upgrades as `BatchesUpdated`.
  - Uses `ModificationBarrier4` for deferred structural changes.

### `FireSimulationSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames (~1 second at 60fps)
- **Queries**:
  - Fire query: `[OnFire]`, excluding `[Deleted, Temp]`
- **Reads**: PrefabRef, Building, CurrentDistrict, Transform, Tree, Destroyed, InstalledUpgrade, FireRescueRequest, FireData, DistrictModifier, Renter, FireConfigurationData, TelecomCoverage, LocalEffects
- **Writes**: OnFire (intensity), Damaged (structural damage), Efficiency (fire factor), creates FireRescueRequest/Damage/Destroy/Ignite events, notification icons
- **Key methods**:
  - `FireSimulationJob.Execute()` — Two responsibilities per burning entity:
    1. **Damage escalation**: If structurally destroyed (damage.y >= 1.0), intensity decays at 2x escalation rate. Otherwise, intensity increases by `escalationRate * 1.0667`. Structural damage per tick = `min(0.5, intensity * dt / structuralIntegrity)`. When total damage = 1.0: creates Destroy event. When intensity = 0: removes OnFire.
    2. **Rescue request**: Calculates delay via `InitializeRequestFrame()` (base 3-30s, modified by darkness +10%, telecom -15%, district/local modifiers). Creates `FireRescueRequest` when frame threshold reached.
  - `FireSpreadCheckJob.Execute()` — For each burning entity, rolls spread check: `intensity * sqrt(spreadProbability * 0.01) * dt`. If passed, searches `ObjectSearchTree` for nearby buildings/trees within `spreadRange`. Per-target spread probability: `fireHazard * (range - distance) * probability / (100 * range)`. Creates `Ignite` event for successful spreads.

## Data Flow

```
[Spontaneous Ignition — FireHazardSystem (every 4096 frames)]
    │
    │  For buildings: FireHazardData.GetFireHazard(building)
    │    → base hazard from DestructibleObjectData.m_FireHazard (or 100)
    │    → * (1 - (level-1) * 0.03) level penalty
    │    → * ZonePropertiesData.m_FireHazardMultiplier
    │    → * max(0.01, 1 - serviceCoverage * 0.01) fire station coverage
    │    → * district BuildingFireHazard modifier
    │    → * damageFactor: (1 - structural - weather damage)^4
    │
    │  For trees: FireHazardData.GetFireHazard(tree)
    │    → base hazard from DestructibleObjectData.m_FireHazard (or 100)
    │    → * m_ForestFireHazardFactor (temperature curve × rain curve)
    │    → * ForestFireHazard local effect modifier
    │    → * damageFactor: (1 - structural - weather damage)^4
    │    → Requires: naturalDisasters enabled, no Owner (wild tree)
    │
    │  TryStartFire: probability = hazard * FireData.m_StartProbability
    │  Roll: random.NextFloat(10000) < probability
    │
    ▼
[Fire Event Entity created]
    │  Archetype: PrefabRef + Game.Events.Fire (tag) + TargetElement buffer
    │  TargetElement contains the target entity
    │
    ▼
[IgniteSystem (every frame)] processes Ignite events
    │  Deduplicates by target (keeps highest intensity)
    │  Adds OnFire component to target entity
    │  Adds target to event's TargetElement buffer
    │  Creates journal data for buildings
    │
    ▼
[OnFire component on target entity]
    │  m_Intensity starts at FireData.m_StartIntensity (default 1.0)
    │  m_RequestFrame calculated from response time + modifiers
    │
    ▼
[FireSimulationSystem (every 64 frames)]
    │
    ├── Damage Escalation:
    │   intensity += escalationRate * 1.0667 (capped at 100)
    │   structuralDamage += min(0.5, intensity * 1.0667 / structuralIntegrity)
    │   When damage.y >= 1.0 → intensity decays at 2x rate
    │   When totalDamage == 1.0 → Destroy event + burned-down notification
    │   When intensity == 0 → remove OnFire + fire notification
    │   Building efficiency set to 0 while burning
    │
    ├── Rescue Request:
    │   Delay: random(3s, 30s) * (1 + 0.1*darkness) * (1 - 0.15*telecom)
    │   + district/local modifiers
    │   When frame reached → FireRescueRequest + fire notification icon
    │
    └── Fire Spread:
        Roll: intensity * sqrt(spreadProb * 0.01) * dt
        Search ObjectSearchTree for nearby buildings/trees
        Per-target: fireHazard * (range - dist) * prob / (100 * range)
        Creates Ignite events → feeds back to IgniteSystem
```

## Prefab & Configuration

| Value | Default | Source | Location |
|-------|---------|--------|----------|
| Start probability | 0.01 | Fire (ComponentBase) | Game.Prefabs.Fire.m_StartProbability |
| Start intensity | 1.0 | Fire (ComponentBase) | Game.Prefabs.Fire.m_StartIntensity |
| Escalation rate | 1/60 | Fire (ComponentBase) | Game.Prefabs.Fire.m_EscalationRate |
| Spread probability | 1.0 | Fire (ComponentBase) | Game.Prefabs.Fire.m_SpreadProbability |
| Spread range | 20m | Fire (ComponentBase) | Game.Prefabs.Fire.m_SpreadRange |
| Random target type | varies | Fire (ComponentBase) | Game.Prefabs.Fire.m_RandomTargetType |
| Default structural integrity | 3,000 | FireConfigurationPrefab | m_DefaultStructuralIntegrity |
| Building structural integrity | 15,000 | FireConfigurationPrefab | m_BuildingStructuralIntegrity |
| Level 1-5 structural integrity | 12,000-16,000 | FireConfigurationPrefab | m_StructuralIntegrityLevel1-5 |
| Response time range | 3-30s | FireConfigurationPrefab | m_ResponseTimeRange |
| Telecom response modifier | -0.15 | FireConfigurationPrefab | m_TelecomResponseTimeModifier |
| Darkness response modifier | 0.1 | FireConfigurationPrefab | m_DarknessResponseTimeModifier |
| Death rate | 0.01 | FireConfigurationPrefab | m_DeathRateOfFireAccident |
| Temperature fire hazard | AnimationCurve | FireConfigurationPrefab | m_TemperatureForestFireHazard |
| No-rain fire hazard | AnimationCurve | FireConfigurationPrefab | m_NoRainForestFireHazard |
| Base fire hazard | per-prefab | DestructibleObjectData | m_FireHazard |
| Zone fire multiplier | per-zone | ZonePropertiesData | m_FireHazardMultiplier |
| Building level penalty | 3% per level | Hardcoded | `1 - (level-1) * 0.03` in GetFireHazard |

### Key Constants (Hardcoded)

| Constant | Value | Where Used |
|----------|-------|------------|
| dt (tick duration) | 1.0666667 (64/60) | FireSimulationSystem |
| Max intensity | 100 | FireSimulationSystem |
| Max damage per tick | 0.5 | FireSimulationSystem |
| Destroyed decay rate | 2x escalation | FireSimulationSystem |
| Building level hazard reduction | 3% per level | EventHelpers.FireHazardData |
| Min service coverage factor | 0.01 | EventHelpers.FireHazardData |
| Default fire hazard (no prefab) | 100 | EventHelpers.FireHazardData |
| FireHazardSystem random skip | 1-in-64 chunks | FireHazardSystem |
| FireHazardSystem update interval | 4096 frames | FireHazardSystem |
| Spontaneous ignition roll divisor | 10,000 | FireHazardSystem.TryStartFire |
| Spread request frame offset | +64 frames | FireSimulationSystem.FireSpreadCheckJob |
| Request frame offset | -32 - 128 frames | FireSimulationSystem.InitializeRequestFrame |
| Rescue request group | 4 | FireSimulationSystem |
| Efficiency factor while burning | 0 | FireSimulationSystem |
| noRainDays increment | 1/64 per update | FireHazardSystem |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.FireHazardSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to block spontaneous fires) or Postfix (to modify hazard data)
- **What it enables**: Prevent all spontaneous fires, or modify the `noRainDays` / temperature hazard factor before the job runs. A Prefix returning `false` would completely disable spontaneous ignition.
- **Risk level**: Low — only affects spontaneous fire generation, not existing fires
- **Side effects**: Blocking would prevent all spontaneous fires (both building and forest)

### Candidate 2: `Game.Events.IgniteSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to intercept all ignitions) or Postfix
- **What it enables**: Intercept and filter all fire ignition attempts — from spontaneous fires, fire spread, or programmatic triggers. Could selectively prevent fires on specific entity types.
- **Risk level**: Medium — central to all fire ignition, blocking prevents all fires from starting
- **Side effects**: Would also block fire spread and programmatic ignition, not just spontaneous

### Candidate 3: `Game.Simulation.FireSimulationSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify fire behavior after ignition — escalation rate, spread mechanics, rescue timing. A Prefix could modify the `FireConfigurationData` singleton before the job reads it.
- **Risk level**: Medium — affects all active fires
- **Side effects**: Changes apply to all fires simultaneously

### Candidate 4: `EventHelpers.FireHazardData.GetFireHazard` (Building overload)

- **Signature**: `public bool GetFireHazard(PrefabRef prefabRef, Building building, CurrentDistrict currentDistrict, Damaged damaged, UnderConstruction underConstruction, out float fireHazard, out float riskFactor)`
- **Patch type**: Postfix (to modify computed hazard)
- **What it enables**: Per-building fire hazard modification. Could increase/decrease hazard for specific building types, zones, or districts.
- **Risk level**: Low — only modifies hazard calculation, doesn't break system flow
- **Side effects**: Called from both FireHazardSystem (spontaneous) and FireSimulationSystem (spread), so changes affect both paths
- **Note**: This is a struct method — Harmony can patch it but behavior may vary. Test carefully.

### Candidate 5: `EventHelpers.FireHazardData.GetFireHazard` (Tree overload)

- **Signature**: `public bool GetFireHazard(PrefabRef prefabRef, Tree tree, Transform transform, Damaged damaged, out float fireHazard, out float riskFactor)`
- **Patch type**: Postfix (to modify computed hazard)
- **What it enables**: Per-tree fire hazard modification. Could suppress forest fires in certain areas or increase them.
- **Risk level**: Low
- **Side effects**: Same as building overload — affects both spontaneous and spread paths
- **Note**: Struct method caveat applies.

### Alternative: ECS System Approach (No Harmony)

A custom `GameSystemBase` can:
1. Create `Ignite` event entities programmatically to start fires
2. Query for `OnFire` entities and modify/remove the component
3. Query fire event entities (`Fire` tag + `TargetElement`) to track active fires
4. Run before `IgniteSystem` to filter/modify `Ignite` events
5. Modify `FireConfigurationData` singleton values at runtime

This is safer than Harmony patching and compatible with Burst-compiled jobs.

## Mod Blueprint

### To programmatically ignite a building:

```csharp
// In a custom GameSystemBase.OnUpdate():
public void IgniteBuilding(Entity targetBuilding, Entity fireEventEntity)
{
    // Create the Ignite event entity
    var igniteArchetype = EntityManager.CreateArchetype(
        ComponentType.ReadWrite<Game.Common.Event>(),
        ComponentType.ReadWrite<Ignite>()
    );

    Entity igniteEntity = EntityManager.CreateEntity(igniteArchetype);
    EntityManager.SetComponentData(igniteEntity, new Ignite
    {
        m_Event = fireEventEntity,   // The fire event entity (has Fire tag + TargetElement)
        m_Target = targetBuilding,
        m_Intensity = 1f,            // FireData.m_StartIntensity default
        m_RequestFrame = 0           // 0 = IgniteSystem will calculate from response time
    });
    // IgniteSystem processes this next frame → adds OnFire to the building
}
```

### To programmatically ignite a tree:

```csharp
// Same approach — trees and buildings use the exact same Ignite mechanism
public void IgniteTree(Entity targetTree, Entity fireEventEntity)
{
    var igniteArchetype = EntityManager.CreateArchetype(
        ComponentType.ReadWrite<Game.Common.Event>(),
        ComponentType.ReadWrite<Ignite>()
    );

    Entity igniteEntity = EntityManager.CreateEntity(igniteArchetype);
    EntityManager.SetComponentData(igniteEntity, new Ignite
    {
        m_Event = fireEventEntity,
        m_Target = targetTree,
        m_Intensity = 1f,
        m_RequestFrame = 0
    });
}
```

### To create a fire event entity (needed by ignition):

```csharp
// Fire event entities need a prefab. Find the fire event prefab:
private EntityQuery m_FirePrefabQuery;

protected override void OnCreate()
{
    base.OnCreate();
    m_FirePrefabQuery = GetEntityQuery(
        ComponentType.ReadOnly<EventData>(),
        ComponentType.ReadOnly<FireData>()
    );
}

public Entity CreateFireEvent(EventTargetType targetType)
{
    // Find fire prefab matching target type
    var chunks = m_FirePrefabQuery.ToArchetypeChunkArray(Allocator.Temp);
    var fireDataHandle = GetComponentTypeHandle<FireData>(true);
    var eventDataHandle = GetComponentTypeHandle<EventData>(true);
    var entityHandle = GetEntityTypeHandle();

    foreach (var chunk in chunks)
    {
        var fireDatas = chunk.GetNativeArray(ref fireDataHandle);
        var eventDatas = chunk.GetNativeArray(ref eventDataHandle);
        var entities = chunk.GetNativeArray(entityHandle);

        for (int i = 0; i < fireDatas.Length; i++)
        {
            if (fireDatas[i].m_RandomTargetType == targetType)
            {
                // Create event entity from prefab archetype
                Entity eventEntity = EntityManager.CreateEntity(eventDatas[i].m_Archetype);
                EntityManager.SetComponentData(eventEntity, new PrefabRef(entities[i]));
                EntityManager.GetBuffer<TargetElement>(eventEntity); // Initialize empty buffer
                chunks.Dispose();
                return eventEntity;
            }
        }
    }
    chunks.Dispose();
    return Entity.Null;
}
```

### Systems to create:
- **FireTriggerSystem** — Custom system to programmatically create `Ignite` events (e.g., player-triggered fires, scripted events)
- **FireModifierSystem** — Custom system running before `FireHazardSystem` to modify hazard factors or `FireConfigurationData`

### Components to add:
- **FireImmune** — Optional marker to prevent entities from catching fire (filter in a system that removes `Ignite` events targeting immune entities)
- **FireProne** — Optional marker to increase fire probability for specific entities

### Patches needed:
- For basic ignition: **none** (ECS approach is sufficient)
- For modifying spontaneous fire probability: Postfix on `FireHazardSystem.OnUpdate()` to modify `noRainDays` or temperature
- For per-entity hazard control: Postfix on `EventHelpers.FireHazardData.GetFireHazard()` overloads

### Settings:
- Fire probability multiplier (default: 1.0)
- Fire intensity multiplier (default: 1.0)
- Spread probability multiplier (default: 1.0)
- Spread range multiplier (default: 1.0)
- Structural integrity multiplier (default: 1.0)
- Forest fire enabled (default: true)

## Examples

### Example 1: Ignite a Building

Creates an `Ignite` event entity that `IgniteSystem` will process on the next frame, adding the `OnFire` component to the target building. Requires a fire event entity (see Example 3 for how to create one).

```csharp
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Unity.Entities;

/// <summary>
/// Demonstrates how to programmatically ignite a building entity.
/// Call from within a custom GameSystemBase.OnUpdate().
/// </summary>
public partial class BuildingIgnitionExample : GameSystemBase
{
    private EntityQuery m_FirePrefabQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        // Query for fire event prefabs — these define fire behavior parameters
        m_FirePrefabQuery = GetEntityQuery(
            ComponentType.ReadOnly<EventData>(),
            ComponentType.ReadOnly<FireData>()
        );
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Ignites a building. The Ignite event is a temporary entity processed
    /// by IgniteSystem within one frame, then destroyed.
    /// </summary>
    public void IgniteBuilding(Entity targetBuilding)
    {
        // Step 1: Create the fire event entity from the Building fire prefab.
        // This entity carries the Fire tag and TargetElement buffer.
        Entity fireEvent = CreateFireEvent(EventTargetType.Building);
        if (fireEvent == Entity.Null)
            return;

        // Step 2: Create the Ignite event entity.
        // Must have both Ignite data and the Event tag for IgniteSystem to find it.
        Entity igniteEntity = EntityManager.CreateEntity(
            ComponentType.ReadWrite<Event>(),
            ComponentType.ReadWrite<Ignite>()
        );

        // Step 3: Set ignition parameters.
        // m_Intensity = 1f matches the default FireData.m_StartIntensity.
        // m_RequestFrame = 0 tells IgniteSystem to calculate rescue timing automatically.
        EntityManager.SetComponentData(igniteEntity, new Ignite
        {
            m_Event = fireEvent,
            m_Target = targetBuilding,
            m_Intensity = 1f,
            m_RequestFrame = 0
        });

        // IgniteSystem processes this next frame:
        //   1. Adds OnFire component to targetBuilding
        //   2. Adds targetBuilding to fireEvent's TargetElement buffer
        //   3. Creates journal data for damage tracking
        //   4. Destroys this Ignite entity
    }

    /// <summary>
    /// Finds the fire event prefab matching the given target type and creates
    /// an event entity from its archetype.
    /// </summary>
    private Entity CreateFireEvent(EventTargetType targetType)
    {
        var chunks = m_FirePrefabQuery.ToArchetypeChunkArray(Unity.Collections.Allocator.Temp);
        var fireDataHandle = GetComponentTypeHandle<FireData>(true);
        var eventDataHandle = GetComponentTypeHandle<EventData>(true);
        var entityHandle = GetEntityTypeHandle();

        foreach (var chunk in chunks)
        {
            var fireDatas = chunk.GetNativeArray(ref fireDataHandle);
            var eventDatas = chunk.GetNativeArray(ref eventDataHandle);
            var entities = chunk.GetNativeArray(entityHandle);

            for (int i = 0; i < fireDatas.Length; i++)
            {
                if (fireDatas[i].m_RandomTargetType == targetType)
                {
                    // Create event entity using the prefab's archetype.
                    // This gives us Fire tag + TargetElement buffer automatically.
                    Entity eventEntity = EntityManager.CreateEntity(eventDatas[i].m_Archetype);
                    EntityManager.SetComponentData(eventEntity, new PrefabRef(entities[i]));
                    chunks.Dispose();
                    return eventEntity;
                }
            }
        }

        chunks.Dispose();
        return Entity.Null;
    }
}
```

### Example 2: Ignite a Tree

Trees use the exact same `Ignite` mechanism as buildings. The only difference is using `EventTargetType.WildTree` when looking up the fire event prefab. Note that forest fires normally require the `naturalDisasters` city setting to be enabled, but programmatic ignition bypasses that check entirely.

```csharp
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Unity.Entities;

/// <summary>
/// Demonstrates how to programmatically ignite a wild tree.
/// Trees and buildings share the same Ignite → OnFire pipeline.
/// </summary>
public partial class TreeIgnitionExample : GameSystemBase
{
    private EntityQuery m_FirePrefabQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_FirePrefabQuery = GetEntityQuery(
            ComponentType.ReadOnly<EventData>(),
            ComponentType.ReadOnly<FireData>()
        );
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Ignites a wild tree. Uses EventTargetType.WildTree to find
    /// the correct fire prefab (which may have different escalation/spread values).
    /// </summary>
    public void IgniteTree(Entity targetTree)
    {
        // Use WildTree target type — the game has separate fire prefabs
        // for buildings and trees with potentially different parameters
        Entity fireEvent = CreateFireEvent(EventTargetType.WildTree);
        if (fireEvent == Entity.Null)
            return;

        Entity igniteEntity = EntityManager.CreateEntity(
            ComponentType.ReadWrite<Event>(),
            ComponentType.ReadWrite<Ignite>()
        );

        EntityManager.SetComponentData(igniteEntity, new Ignite
        {
            m_Event = fireEvent,
            m_Target = targetTree,
            m_Intensity = 1f,
            m_RequestFrame = 0
        });

        // Trees have lower structural integrity (default 3,000 vs 12,000-16,000
        // for buildings), so they burn down faster.
        // FireSimulationSystem handles all the escalation and destruction.
    }

    private Entity CreateFireEvent(EventTargetType targetType)
    {
        // Same pattern as building example — find prefab by target type
        var chunks = m_FirePrefabQuery.ToArchetypeChunkArray(Unity.Collections.Allocator.Temp);
        var fireDataHandle = GetComponentTypeHandle<FireData>(true);
        var eventDataHandle = GetComponentTypeHandle<EventData>(true);
        var entityHandle = GetEntityTypeHandle();

        foreach (var chunk in chunks)
        {
            var fireDatas = chunk.GetNativeArray(ref fireDataHandle);
            var eventDatas = chunk.GetNativeArray(ref eventDataHandle);
            var entities = chunk.GetNativeArray(entityHandle);

            for (int i = 0; i < fireDatas.Length; i++)
            {
                if (fireDatas[i].m_RandomTargetType == targetType)
                {
                    Entity eventEntity = EntityManager.CreateEntity(eventDatas[i].m_Archetype);
                    EntityManager.SetComponentData(eventEntity, new PrefabRef(entities[i]));
                    chunks.Dispose();
                    return eventEntity;
                }
            }
        }

        chunks.Dispose();
        return Entity.Null;
    }
}
```

### Example 3: Check if an Entity is On Fire

Query for entities with the `OnFire` component to find all burning entities, or check a specific entity directly. The `OnFire` component contains the current fire intensity and event reference.

```csharp
using Game.Events;
using Game.Buildings;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

/// <summary>
/// Demonstrates how to query for burning entities and read their fire state.
/// </summary>
public partial class FireCheckExample : GameSystemBase
{
    private EntityQuery m_BurningQuery;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        // Query all entities currently on fire (excluding deleted/temp)
        m_BurningQuery = GetEntityQuery(
            ComponentType.ReadOnly<OnFire>(),
            ComponentType.Exclude<Game.Common.Deleted>(),
            ComponentType.Exclude<Game.Tools.Temp>()
        );
    }

    [Preserve]
    protected override void OnUpdate()
    {
        // --- Check a specific entity ---
        // Use HasComponent to test if a known entity is burning
        Entity someBuilding = Entity.Null; // your target entity
        if (EntityManager.HasComponent<OnFire>(someBuilding))
        {
            OnFire onFire = EntityManager.GetComponentData<OnFire>(someBuilding);

            // m_Intensity ranges from 0 (extinguished) to 100 (maximum)
            float intensity = onFire.m_Intensity;

            // m_Event references the fire event entity (has Fire tag + TargetElement buffer)
            Entity fireEvent = onFire.m_Event;

            // m_RescueRequest is Entity.Null until a FireRescueRequest has been created
            bool rescueRequested = onFire.m_RescueRequest != Entity.Null;

            Log.Info($"Entity is burning! Intensity: {intensity}, Rescue requested: {rescueRequested}");
        }

        // --- Query all burning entities ---
        var burningEntities = m_BurningQuery.ToEntityArray(Allocator.Temp);
        var onFireComponents = m_BurningQuery.ToComponentDataArray<OnFire>(Allocator.Temp);

        for (int i = 0; i < burningEntities.Length; i++)
        {
            Entity entity = burningEntities[i];
            OnFire fire = onFireComponents[i];

            // Distinguish buildings from trees
            bool isBuilding = EntityManager.HasComponent<Building>(entity);
            bool isTree = EntityManager.HasComponent<Tree>(entity);

            string type = isBuilding ? "Building" : isTree ? "Tree" : "Other";
            Log.Info($"{type} {entity.Index} on fire — intensity: {fire.m_Intensity:F1}");
        }

        burningEntities.Dispose();
        onFireComponents.Dispose();
    }
}
```

### Example 4: How Fire Hazard and Probability are Calculated

This example shows the logic `FireHazardSystem` uses to determine whether a spontaneous fire starts. The actual calculation happens inside `EventHelpers.FireHazardData.GetFireHazard()` and `FireHazardSystem.TryStartFire()`. Below is a standalone illustration of the math.

```csharp
using Game.Buildings;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Illustrates how fire hazard probability is computed for buildings and trees.
/// This is NOT runnable code — it reconstructs the logic from FireHazardData.GetFireHazard()
/// and FireHazardSystem.TryStartFire() to explain the math.
/// </summary>
public static class FireHazardCalculationReference
{
    /// <summary>
    /// Building fire hazard calculation.
    /// Mirrors EventHelpers.FireHazardData.GetFireHazard(building overload).
    /// </summary>
    public static float CalculateBuildingFireHazard(
        float prefabFireHazard,   // DestructibleObjectData.m_FireHazard, or 100 if not set
        int buildingLevel,        // SpawnableBuildingData.m_Level (1-5)
        float zoneFireMultiplier, // ZonePropertiesData.m_FireHazardMultiplier
        float serviceCoverage,    // Fire station coverage at building's road edge (0-100)
        float districtModifier,   // District BuildingFireHazard modifier (additive)
        float structuralDamage,   // Damaged.m_Damage.y (structural damage, 0-1)
        float weatherDamage)      // Damaged.m_Damage.z (weather damage, 0-1)
    {
        float hazard = prefabFireHazard;

        // Higher level buildings have slightly lower hazard (3% reduction per level above 1)
        hazard *= 1f - (buildingLevel - 1) * 0.03f;

        // Zone type multiplier (e.g., industrial zones have higher fire hazard)
        hazard *= zoneFireMultiplier;

        // Fire station coverage reduces hazard — but never below 1% of base
        // Full coverage (100) reduces hazard by 99%
        hazard *= math.max(0.01f, 1f - serviceCoverage * 0.01f);

        // District policy modifier (applied additively via AreaUtils.ApplyModifier)
        // This is a simplified representation — actual uses AreaUtils.ApplyModifier
        hazard *= (1f + districtModifier);

        // Existing damage reduces fire hazard (already-damaged buildings are less flammable)
        // Factor = (1 - totalDamage)^4 — drops off sharply
        float damageFactor = math.max(0f, 1f - (structuralDamage + weatherDamage));
        damageFactor = damageFactor * damageFactor * damageFactor * damageFactor; // ^4
        hazard *= damageFactor;

        return hazard;
    }

    /// <summary>
    /// Tree fire hazard calculation.
    /// Mirrors EventHelpers.FireHazardData.GetFireHazard(tree overload).
    /// </summary>
    public static float CalculateTreeFireHazard(
        float prefabFireHazard,       // DestructibleObjectData.m_FireHazard, or 100 if not set
        float forestFireHazardFactor,  // Temperature curve * no-rain curve (from FireConfigurationPrefab)
        float localEffectModifier,     // ForestFireHazard local effect at tree position
        float structuralDamage,
        float weatherDamage)
    {
        float hazard = prefabFireHazard;

        // Climate factor: product of temperature curve and no-rain-days curve
        // Both curves are AnimationCurves defined in the FireConfigurationPrefab asset.
        // Hot + dry = high factor, cold + wet = near zero.
        hazard *= forestFireHazardFactor;

        // Local effect modifier (spatial map — areas near certain buildings or map features
        // can increase or decrease forest fire hazard)
        // Simplified — actual uses LocalEffectSystem.ReadData.ApplyModifier
        hazard *= (1f + localEffectModifier);

        // Same damage-based reduction as buildings
        float damageFactor = math.max(0f, 1f - (structuralDamage + weatherDamage));
        damageFactor = damageFactor * damageFactor * damageFactor * damageFactor;
        hazard *= damageFactor;

        return hazard;
    }

    /// <summary>
    /// Final ignition roll — performed by FireHazardSystem.TryStartFire().
    /// This is how the game decides whether a spontaneous fire actually starts.
    /// </summary>
    public static bool WouldFireStart(
        float fireHazard,        // Output of GetFireHazard() above
        float startProbability,  // FireData.m_StartProbability (default 0.01)
        float randomRoll)        // random.NextFloat(10000)
    {
        // Final probability combines the environmental hazard with the prefab's base probability
        float probability = fireHazard * startProbability;

        // The roll is out of 10,000 — so a probability of 1.0 means a 0.01% chance per check.
        // With default startProbability (0.01) and a hazard of 100: probability = 1.0
        // Combined with 1-in-64 chunk skip and 4096-frame interval, actual fires are rare.
        return randomRoll < probability;
    }
}
```

## Open Questions

- [ ] What are the exact shapes of the `m_TemperatureForestFireHazard` and `m_NoRainForestFireHazard` AnimationCurves? These are defined in the prefab asset, not in code. Would need to extract from game data files.
- [ ] What `EventTargetType` values exist besides `Building` and `WildTree`? The enum is likely in `Game.Prefabs` but wasn't fully traced.
- [ ] How does `LocalModifierType.ForestFireHazard` get applied spatially? The `LocalEffectSystem` internals were not fully traced — it likely uses a cell map.
- [ ] What is the exact archetype created by `EventData.m_Archetype` for fire events? It includes `Fire` tag + `TargetElement` buffer (from `GetArchetypeComponents`), but the base `EventPrefab` may add additional components.
- [ ] How does the game handle fire on vehicles? `AccidentVehicleSystem` creates `Ignite` events for crashed vehicles using `damage.x * fireData.m_StartProbability`, but the fire event entity source for vehicle fires wasn't traced.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
