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
