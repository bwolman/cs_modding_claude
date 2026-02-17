# Research: Disaster Simulation (Non-Fire)

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 simulates non-fire disasters -- tornado/storm damage, flood/tsunami water damage, building collapse, lightning strikes, and the evacuation pipeline that responds to these events.

**Why**: To understand disaster damage mechanics so modders can create custom disaster scenarios, adjust damage rates, modify evacuation behavior, or build challenge/scenario mods that use the disaster pipeline.

**Boundaries**: Fire ignition and fire spread are covered in Fire Ignition research. Weather spawning conditions (temperature, rain thresholds) are covered in Weather & Climate research. This focuses on the damage pipeline from weather phenomenon to building destruction and citizen evacuation.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Events | WeatherPhenomenon, Flood, Flooded, FacingWeather, InDanger, DangerLevel, Duration, DangerFlags, Endanger, Submerge, FaceWeather, LightningStrike, WaterLevelChange, Destruction |
| Game.dll | Game.Objects | Damaged, Damage, DamageSystem |
| Game.dll | Game.Simulation | WeatherPhenomenonSystem, WaterDamageSystem, WeatherDamageSystem, FloodCheckSystem, CollapsedBuildingSystem, DamagedVehicleSystem, CitizenEvacuateSystem, EvacuationDispatchSystem, FloodCounterData, EvacuationRequest |
| Game.dll | Game.Prefabs | WeatherPhenomenonData, DisasterConfigurationData, DestructibleObjectData, DestructionData, FloodData, WaterLevelChangeData, DisasterFacilityData, EarlyDisasterWarningEventData |
| Game.dll | Game.Buildings | DisasterFacility, EarlyDisasterWarningSystem (component), EmergencyShelter |
| Game.dll | Game.Vehicles | EvacuatingTransport |

## Key Concepts

### Damage Channel System

CS2 uses a float3 vector (`Damaged.m_Damage`) to track three independent damage channels:

| Channel | Index | Source | Applied by |
|---------|-------|--------|------------|
| Weather (tornado/storm) | x | WeatherDamageSystem | `damaged.m_Damage.x += value` |
| Fire | y | FireSpreadSystem | `damaged.m_Damage.y += value` |
| Water (flood/tsunami) | z | WaterDamageSystem | `damaged.m_Damage.z += value` |

When `ObjectUtils.GetTotalDamage(damaged) == 1f`, the object is destroyed. Total damage is the sum of all three channels, capped at 1.0.

### Structural Integrity

Each object has a `DestructibleObjectData.m_StructuralIntegrity` value from its prefab. Damage per tick is divided by structural integrity -- higher integrity means the object takes longer to destroy. Objects with structural integrity >= 100,000,000 are effectively indestructible (weather damage is zeroed out).

### Two Disaster Pipelines

The game has two distinct disaster damage pipelines:

1. **Weather Phenomenon Pipeline** (tornados, storms): Uses `WeatherPhenomenon` event entities that move across the map. Buildings in the hotspot radius get `FacingWeather` added, then `WeatherDamageSystem` applies damage to `Damaged.m_Damage.x`.

2. **Flood/Tsunami Pipeline**: Uses water surface simulation. Buildings below water get `Flooded` added by `FloodCheckSystem`, then `WaterDamageSystem` applies damage to `Damaged.m_Damage.z`.

## Component Map

### `WeatherPhenomenon` (Game.Events)

The core runtime state of a weather disaster event (tornado, storm).

| Field | Type | Description |
|-------|------|-------------|
| m_PhenomenonPosition | float3 | Center of the overall weather system |
| m_HotspotPosition | float3 | Center of the damage hotspot (eye of storm) |
| m_HotspotVelocity | float3 | Movement velocity of the hotspot |
| m_PhenomenonRadius | float | Outer radius of the phenomenon |
| m_HotspotRadius | float | Radius of the damage-dealing hotspot |
| m_Intensity | float | Current intensity (0-1), ramps up/down over duration |
| m_LightningTimer | float | Countdown to next lightning strike |

*Source: `Game.dll` -> `Game.Events.WeatherPhenomenon`*

### `Damaged` (Game.Objects)

Tracks accumulated damage on any object (building, vehicle, etc.).

| Field | Type | Description |
|-------|------|-------------|
| m_Damage | float3 | Three damage channels: x=weather, y=fire, z=water |

*Source: `Game.dll` -> `Game.Objects.Damaged`*

### `Damage` (Game.Objects)

Transient event component requesting damage be applied to an object.

| Field | Type | Description |
|-------|------|-------------|
| m_Object | Entity | Target entity to damage |
| m_Delta | float3 | Damage amount per channel |

*Source: `Game.dll` -> `Game.Objects.Damage`*

### `Flooded` (Game.Events)

Added to objects submerged by flood water.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The flood/tsunami event causing the flooding |
| m_Depth | float | Current water depth above the object |

*Source: `Game.dll` -> `Game.Events.Flooded`*

### `FacingWeather` (Game.Events)

Added to buildings within a weather phenomenon's hotspot radius.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The weather phenomenon event |
| m_Severity | float | Calculated damage severity based on distance to hotspot |

*Source: `Game.dll` -> `Game.Events.FacingWeather`*

### `InDanger` (Game.Events)

Added to buildings in the danger zone of an approaching disaster. Triggers evacuation.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The disaster event |
| m_EvacuationRequest | Entity | Associated evacuation request entity |
| m_Flags | DangerFlags | What action citizens should take |
| m_EndFrame | uint | Simulation frame when danger expires |

*Source: `Game.dll` -> `Game.Events.InDanger`*

### `DangerFlags` (Game.Events)

Flags controlling evacuation behavior.

| Flag | Value | Description |
|------|-------|-------------|
| StayIndoors | 1 | Citizens should remain in their building |
| Evacuate | 2 | Citizens must leave the building |
| UseTransport | 4 | Evacuation vehicles should be dispatched |
| WaitingCitizens | 8 | Citizens are waiting for transport |

*Source: `Game.dll` -> `Game.Events.DangerFlags`*

### `WaterLevelChange` (Game.Events)

Tracks an active tsunami or flood event that changes water levels.

| Field | Type | Description |
|-------|------|-------------|
| m_Intensity | float | Current wave intensity |
| m_MaxIntensity | float | Peak intensity |
| m_DangerHeight | float | Water height that triggers danger |
| m_Direction | float2 | Direction the wave travels |

*Source: `Game.dll` -> `Game.Events.WaterLevelChange`*

### `Duration` (Game.Events)

Time bounds for any disaster event.

| Field | Type | Description |
|-------|------|-------------|
| m_StartFrame | uint | Simulation frame the event begins |
| m_EndFrame | uint | Simulation frame the event ends |

*Source: `Game.dll` -> `Game.Events.Duration`*

### `DangerLevel` (Game.Events)

Current danger level of a disaster event.

| Field | Type | Description |
|-------|------|-------------|
| m_DangerLevel | float | 0 = no danger, higher = more severe |

*Source: `Game.dll` -> `Game.Events.DangerLevel`*

### `LightningStrike` (Game.Events)

Transient data for a lightning strike (not an ECS component, used as queue element).

| Field | Type | Description |
|-------|------|-------------|
| m_HitEntity | Entity | The entity struck by lightning |
| m_Position | float3 | World position of the strike |

*Source: `Game.dll` -> `Game.Events.LightningStrike`*

### `FloodCounterData` (Game.Simulation)

Tracks flood duration for statistics.

| Field | Type | Description |
|-------|------|-------------|
| m_FloodCounter | float | Accumulated flood time |

*Source: `Game.dll` -> `Game.Simulation.FloodCounterData`*

### `EvacuationRequest` (Game.Simulation)

Service request for evacuating a building.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | Building to evacuate |
| m_Priority | float | Request priority |

*Source: `Game.dll` -> `Game.Simulation.EvacuationRequest`*

### `EvacuatingTransport` (Game.Vehicles)

Marker component on vehicles performing evacuation duty.

*Source: `Game.dll` -> `Game.Vehicles.EvacuatingTransport`*

## System Map

### `WeatherPhenomenonSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Entities with `WeatherPhenomenon`, excluding `Deleted`/`Temp`
- **Reads**: Duration, PrefabRef, WeatherPhenomenonData, Wind data, terrain/water height, static object search tree, net search tree
- **Writes**: WeatherPhenomenon (position, velocity, intensity, lightning timer), DangerLevel
- **Key responsibilities**:
  1. **Movement**: Moves the phenomenon position with wind, moves hotspot with instability-driven velocity
  2. **Intensity ramping**: Increases intensity by 0.2/sec after start, decreases after end
  3. **Building damage**: Calls `FindAffectedObjects()` to create `FaceWeather` events for buildings in hotspot radius
  4. **Lightning**: Decrements lightning timer, fires `LightningStrike()` which searches the static object quad tree for the tallest building/tree, can start fires via `Ignite` events
  5. **Traffic accidents**: Calls `FindAffectedEdges()` to create `Impact` events on vehicles in the hotspot
  6. **Danger warnings**: Calls `FindEndangeredObjects()` to create `Endanger` events for buildings in the projected path (based on wind direction + CityModifier warning time)
  7. **Cleanup**: Marks the event `Deleted` when intensity drops to 0 after end frame

### `WeatherDamageSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames
- **Queries**: Entities with `FacingWeather`, excluding `Deleted`/`Temp`
- **Reads**: PrefabRef, Building, Transform, Destroyed, WeatherPhenomenon, WeatherPhenomenonData, CityModifiers
- **Writes**: FacingWeather.m_Severity, Damaged.m_Damage.x
- **Key logic**:
  - Recalculates severity from `EventUtils.GetSeverity()` (linear falloff from hotspot center)
  - Divides severity by structural integrity
  - Applies `CityModifierType.DisasterDamageRate` for buildings
  - Caps per-tick damage at 0.5
  - When total damage reaches 1.0, creates `Destroy` event and shows `WeatherDestroyedNotificationPrefab`
  - Removes `FacingWeather` when severity drops to 0 (object leaves hotspot)

### `FloodCheckSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Buildings without `Flooded`, excluding `Placeholder`/`Deleted`/`Temp`
- **Reads**: Transform, PrefabRef, WaterSurfaceData, TerrainHeightData, ObjectGeometryData, PlaceableObjectData
- **Writes**: Creates `Submerge` events
- **Key logic**:
  - Samples water depth at each building's position
  - If water depth > 0.5m above terrain, building is considered flooded
  - Skips objects with `CanSubmerge` geometry flag or `Floating|Swaying` placement flags
  - Creates `Submerge` event to add `Flooded` component
  - Finds the associated `WaterLevelChange` event (tsunami wave) by matching position

### `WaterDamageSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames
- **Queries**: Entities with `Flooded`, excluding `Deleted`/`Temp`
- **Reads**: PrefabRef, Building, Transform, Destroyed, WaterSurfaceData, TerrainHeightData, DisasterConfigurationData, ObjectGeometryData, CityModifiers
- **Writes**: Flooded.m_Depth, Damaged.m_Damage.z
- **Key logic**:
  - Recalculates flood depth from water surface data each tick
  - Damage rate: `min(FloodDamageRate, depth * FloodDamageRate / max(0.5, objectHeight))`
  - Divides by structural integrity, applies `CityModifierType.DisasterDamageRate`
  - Caps per-tick damage at 0.5
  - When total damage reaches 1.0, creates `Destroy` event
  - Removes `Flooded` when water depth drops to 0

### `DamageSystem` (Game.Objects)

- **Base class**: GameSystemBase
- **Queries**: Entities with `Event` + `Damage`
- **Key logic**:
  - Aggregates all `Damage` events per object into a `NativeParallelHashMap`
  - Applies accumulated damage deltas to each object's `Damaged` component
  - Adds `Damaged` component if not present, removes it if all channels reach 0
  - For vehicles, also manages `MaintenanceConsumer` component

### `CollapsedBuildingSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames
- **Queries**: Entities with `Destroyed` + `Building` or `Extension`, excluding `Deleted`/`Temp`
- **Key logic**:
  - For buildings requiring road access: creates `FireRescueRequest` with type `Disaster` for rescue operations
  - Manages the `RescueTarget` component lifecycle
  - Progresses `Destroyed.m_Cleared` from negative (collapsing animation) through 0 to 1.0 (fully cleared)
  - Deletes buildings with `Overridable` geometry or area-owned buildings once cleared

### `DamagedVehicleSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 512 frames
- **Queries**: Entities with `Damaged` + `Stopped` + `Car`, excluding `Deleted`/`Temp`
- **Key logic**:
  - Creates `MaintenanceRequest` service requests for damaged stopped vehicles
  - Removes `MaintenanceConsumer` when vehicle is fully cleared

### `CitizenEvacuateSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Citizens with `CurrentBuilding`, excluding `HealthProblem`/`Deleted`/`Temp`
- **Reads**: CurrentBuilding, InDanger, Dispatched, PublicTransport, ServiceDispatch
- **Writes**: TripNeeded buffer, InDanger.m_Flags (WaitingCitizens)
- **Key logic**:
  - Checks each citizen's building for `InDanger` component
  - If `Evacuate` + `UseTransport`: waits for boarding vehicle, then sends citizen to vehicle
  - If `Evacuate` without `UseTransport`: sends citizen directly to emergency shelter
  - Sets trip purpose to `Purpose.EmergencyShelter`

### `EvacuationDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Entities with `EvacuationRequest` + `UpdateFrame`
- **Key logic**:
  - Pathfinds from emergency shelters to endangered buildings
  - Dispatches evacuation vehicles via `ServiceDispatch` buffer
  - Uses road pathfinding with `SetupTargetType.EvacuationTransport`
  - Validates that target building still has `InDanger` with `Evacuate|UseTransport` flags

## Data Flow

```
WEATHER PHENOMENON PIPELINE (Tornado/Storm)
  WeatherPhenomenonSystem creates event entity
      |
      v
  Event moves with wind, hotspot oscillates with instability
      |
      +---> FindEndangeredObjects() creates Endanger events
      |         -> Buildings get InDanger component
      |         -> CitizenEvacuateSystem sends citizens to shelters
      |         -> EvacuationDispatchSystem dispatches vehicles
      |
      +---> FindAffectedObjects() creates FaceWeather events
      |         -> Buildings get FacingWeather component
      |         -> WeatherDamageSystem applies damage to Damaged.m_Damage.x
      |              -> If total damage >= 1.0: Destroy event created
      |              -> CollapsedBuildingSystem handles rubble
      |
      +---> LightningStrike() finds tallest building/tree
      |         -> May create Ignite event (starts fire)
      |
      +---> FindAffectedEdges() finds vehicles on roads
                -> Creates Impact events (vehicles lose control)

FLOOD/TSUNAMI PIPELINE
  WaterLevelChangeSystem raises water levels
      |
      v
  FloodCheckSystem checks buildings vs water surface
      |
      v
  Creates Submerge events -> Buildings get Flooded component
      |
      v
  WaterDamageSystem applies damage to Damaged.m_Damage.z
      |
      v
  If total damage >= 1.0: Destroy event created
      -> CollapsedBuildingSystem handles rubble
      -> FireRescueRequest(Disaster) for rescue teams

DAMAGE APPLICATION (shared)
  DamageSystem aggregates all Damage events
      -> Applies delta to Damaged.m_Damage float3
      -> Adds/removes Damaged component as needed
```

## Prefab & Configuration

### `WeatherPhenomenonData` (Game.Prefabs)

Configures a weather phenomenon event type (tornado, thunderstorm, etc.).

| Value | Type | Description |
|-------|------|-------------|
| m_OccurenceProbability | float | Base probability of occurring |
| m_HotspotInstability | float | How erratically the hotspot moves |
| m_DamageSeverity | float | Base damage per tick at hotspot center |
| m_DangerLevel | float | Danger level reported to UI |
| m_PhenomenonRadius | Bounds1 | Min/max outer radius |
| m_HotspotRadius | Bounds1 | Min/max damage hotspot radius |
| m_LightningInterval | Bounds1 | Min/max seconds between lightning strikes |
| m_Duration | Bounds1 | Min/max duration in seconds |
| m_OccurenceTemperature | Bounds1 | Temperature range for occurrence |
| m_OccurenceRain | Bounds1 | Rain level range for occurrence |
| m_OccurenceCloudiness | Bounds1 | Cloudiness range for occurrence |
| m_DangerFlags | DangerFlags | What type of danger response is triggered |

### `DisasterConfigurationData` (Game.Prefabs)

Global disaster configuration singleton.

| Value | Type | Description |
|-------|------|-------------|
| m_FloodDamageRate | float | Base flood damage rate per tick |
| m_WeatherDamageNotificationPrefab | Entity | Notification icon for weather damage |
| m_WeatherDestroyedNotificationPrefab | Entity | Notification icon for weather destruction |
| m_WaterDamageNotificationPrefab | Entity | Notification icon for flood damage |
| m_WaterDestroyedNotificationPrefab | Entity | Notification icon for flood destruction |
| m_DestroyedNotificationPrefab | Entity | Generic destruction notification |
| m_EmergencyShelterDangerLevelExitProbability | AnimationCurve1 | Probability citizens leave shelter based on danger level |
| m_InoperableEmergencyShelterExitProbability | float | Exit probability when shelter is inoperable |

### `DestructibleObjectData` (Game.Prefabs)

Per-object durability settings.

| Value | Type | Description |
|-------|------|-------------|
| m_FireHazard | float | Fire susceptibility (0 = cannot catch fire from lightning) |
| m_StructuralIntegrity | float | Damage resistance (higher = harder to destroy) |

### `WaterLevelChangeData` (Game.Prefabs)

Configures tsunami/flood water level behavior.

| Value | Type | Description |
|-------|------|-------------|
| m_TargetType | WaterLevelTargetType | None, River, Sea, or All |
| m_ChangeType | WaterLevelChangeType | None, Sine (tsunami wave), or RainControlled |
| m_EscalationDelay | float | Delay before water level starts changing |
| m_DangerFlags | DangerFlags | Danger response type |
| m_DangerLevel | float | Danger level for this event |

### City Modifiers

| Modifier | Effect |
|----------|--------|
| `CityModifierType.DisasterDamageRate` | Multiplies damage rate for buildings (Early Disaster Warning buildings can reduce this) |
| `CityModifierType.DisasterWarningTime` | Extends the warning time before a disaster arrives |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.WeatherDamageSystem+WeatherDamageJob.Execute`

- **Signature**: `void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Transpiler (Burst-compiled, cannot use Prefix/Postfix)
- **What it enables**: Modify weather damage rates, make certain buildings immune, change damage channels
- **Risk level**: High (Burst-compiled job, transpiler only)
- **Side effects**: Incorrect patching could crash the simulation

### Candidate 2: `Game.Events.EventUtils.GetSeverity`

- **Signature**: `static float GetSeverity(float3 position, WeatherPhenomenon weatherPhenomenon, WeatherPhenomenonData weatherPhenomenonData)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override severity calculation (e.g., based on distance, building type, or custom rules)
- **Risk level**: Medium (static utility method, called from multiple systems)
- **Side effects**: Affects all severity calculations globally

### Candidate 3: `Game.Simulation.FloodCheckSystem+FloodCheckJob.IsFlooded`

- **Signature**: `private bool IsFlooded(float3 position, out float depth)`
- **Patch type**: Transpiler (Burst-compiled)
- **What it enables**: Change the flood threshold (default 0.5m), make objects flood-resistant
- **Risk level**: Medium

### Candidate 4: `Game.Simulation.CollapsedBuildingSystem+CollapsedBuildingJob.Execute`

- **Signature**: `void Execute(in ArchetypeChunk chunk, ...)`
- **Patch type**: Transpiler (Burst-compiled)
- **What it enables**: Modify collapsed building behavior, change rescue request logic
- **Risk level**: Medium

## Mod Blueprint

- **Systems to create**:
  - `CustomDisasterMonitorSystem` -- track active disasters, count damaged/destroyed buildings
  - `DisasterModifierSystem` -- apply custom damage modifiers based on mod settings
- **Components to add**:
  - `DisasterResistant` (IComponentData) -- marker for buildings with custom resistance
- **Patches needed**:
  - `EventUtils.GetSeverity` (Postfix) -- apply custom resistance multipliers
  - `WaterDamageSystem` (Transpiler) -- modify flood damage threshold
- **Settings**:
  - `DamageMultiplier` -- global damage rate multiplier
  - `FloodThreshold` -- minimum water depth for flooding
  - `LightningFireChance` -- probability of lightning starting fires
- **UI changes**:
  - Info panel showing active disaster stats (buildings damaged, citizens evacuated)

## Examples

### Example 1: Monitor Active Disasters

Query all active weather phenomena and flood events to track disaster state.

```csharp
using Game;
using Game.Events;
using Game.Simulation;
using Unity.Entities;
using Colossal.Logging;

public class DisasterMonitorSystem : GameSystemBase
{
    private static readonly ILog Log = LogManager.GetLogger(nameof(DisasterMonitorSystem));

    private EntityQuery _weatherQuery;
    private EntityQuery _floodedQuery;
    private EntityQuery _damagedQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _weatherQuery = GetEntityQuery(
            ComponentType.ReadOnly<WeatherPhenomenon>(),
            ComponentType.ReadOnly<Duration>(),
            ComponentType.Exclude<Deleted>());
        _floodedQuery = GetEntityQuery(
            ComponentType.ReadOnly<Flooded>(),
            ComponentType.Exclude<Deleted>());
        _damagedQuery = GetEntityQuery(
            ComponentType.ReadOnly<Damaged>(),
            ComponentType.Exclude<Deleted>());
    }

    protected override void OnUpdate()
    {
        int activeStorms = _weatherQuery.CalculateEntityCount();
        int floodedBuildings = _floodedQuery.CalculateEntityCount();
        int damagedObjects = _damagedQuery.CalculateEntityCount();

        if (activeStorms > 0 || floodedBuildings > 0)
        {
            Log.Info($"Active storms: {activeStorms}, " +
                     $"Flooded: {floodedBuildings}, " +
                     $"Damaged: {damagedObjects}");
        }
    }
}
```

### Example 2: Modify Damage Severity via Harmony

Patch `EventUtils.GetSeverity` to apply a custom damage multiplier.

```csharp
using HarmonyLib;
using Game.Events;
using Game.Prefabs;
using Unity.Mathematics;

[HarmonyPatch(typeof(EventUtils), nameof(EventUtils.GetSeverity))]
public static class SeverityPatch
{
    public static float DamageMultiplier { get; set; } = 0.5f; // Half damage

    static void Postfix(ref float __result)
    {
        __result *= DamageMultiplier;
    }
}
```

### Example 3: Query Buildings in Danger Zones

Find all buildings currently under evacuation orders.

```csharp
using Game;
using Game.Buildings;
using Game.Events;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

public class EvacuationStatusSystem : GameSystemBase
{
    private EntityQuery _inDangerQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _inDangerQuery = GetEntityQuery(
            ComponentType.ReadOnly<Building>(),
            ComponentType.ReadOnly<InDanger>(),
            ComponentType.Exclude<Deleted>());
    }

    protected override void OnUpdate()
    {
        var entities = _inDangerQuery.ToEntityArray(Allocator.Temp);
        var dangers = _inDangerQuery.ToComponentDataArray<InDanger>(Allocator.Temp);

        int evacuating = 0;
        int stayingIndoors = 0;
        for (int i = 0; i < dangers.Length; i++)
        {
            if ((dangers[i].m_Flags & DangerFlags.Evacuate) != 0)
                evacuating++;
            else if ((dangers[i].m_Flags & DangerFlags.StayIndoors) != 0)
                stayingIndoors++;
        }

        entities.Dispose();
        dangers.Dispose();
    }
}
```

### Example 4: Read Damage State from Buildings

Inspect the three damage channels on a specific building entity.

```csharp
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

public static class DamageUtils
{
    public static (float weather, float fire, float water) GetDamageChannels(
        EntityManager em, Entity building)
    {
        if (!em.HasComponent<Damaged>(building))
            return (0f, 0f, 0f);

        Damaged damaged = em.GetComponentData<Damaged>(building);
        return (damaged.m_Damage.x, damaged.m_Damage.y, damaged.m_Damage.z);
    }

    public static float GetTotalDamage(EntityManager em, Entity building)
    {
        if (!em.HasComponent<Damaged>(building))
            return 0f;

        Damaged damaged = em.GetComponentData<Damaged>(building);
        return math.csum(math.min(damaged.m_Damage, 1f));
    }

    public static bool IsDestroyed(EntityManager em, Entity building)
    {
        return em.HasComponent<Destroyed>(building);
    }
}
```

### Example 5: Create a Custom Flood Event

Programmatically flood a building by adding the Flooded component.

```csharp
using Game;
using Game.Events;
using Game.Simulation;
using Unity.Entities;

public class CustomFloodSystem : GameSystemBase
{
    private EndFrameBarrier _barrier;
    private EntityArchetype _submergeArchetype;

    protected override void OnCreate()
    {
        base.OnCreate();
        _barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        _submergeArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<Game.Common.Event>(),
            ComponentType.ReadWrite<Submerge>());
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Flood a target building with the given water depth.
    /// Call from another system's OnUpdate.
    /// </summary>
    public void FloodBuilding(Entity target, float depth)
    {
        var cmd = _barrier.CreateCommandBuffer();
        Entity floodEvent = cmd.CreateEntity(_submergeArchetype);
        cmd.SetComponent(floodEvent, new Submerge
        {
            m_Event = Entity.Null,
            m_Target = target,
            m_Depth = depth
        });
    }
}
```

## Open Questions

- [ ] **Tornado path prediction**: The `FindEndangeredObjects` method projects the danger zone along wind direction with `CityModifierType.DisasterWarningTime`. The exact interaction between wind changes and path prediction needs in-game testing.
- [ ] **Structural integrity values**: Specific structural integrity values for each building type (residential, commercial, industrial) are set in prefab data. A comprehensive list would require scanning all building prefabs.
- [ ] **WaterLevelChangeSystem details**: The tsunami wave propagation system (`WaterLevelChangeSystem`) controls how water levels rise and fall. Its full implementation was not decompiled due to scope -- it modifies the water simulation directly.
- [ ] **Emergency shelter capacity**: How `EmergencyShelter` building capacity affects evacuation flow (overflow behavior) needs testing.
- [ ] **Disaster event creation**: The exact code path that creates `WeatherPhenomenon` event entities (likely in the weather system responding to conditions) was not traced.

## Sources

- Decompiled from: Game.dll -- Game.Events.WeatherPhenomenon, Game.Events.Flooded, Game.Events.FacingWeather, Game.Events.InDanger, Game.Events.DangerFlags, Game.Events.WaterLevelChange, Game.Events.Duration, Game.Events.DangerLevel, Game.Events.EventUtils, Game.Events.Submerge, Game.Events.FaceWeather, Game.Events.Endanger, Game.Events.LightningStrike
- Decompiled from: Game.dll -- Game.Objects.Damaged, Game.Objects.Damage, Game.Objects.DamageSystem
- Decompiled from: Game.dll -- Game.Simulation.WeatherPhenomenonSystem, Game.Simulation.WaterDamageSystem, Game.Simulation.WeatherDamageSystem, Game.Simulation.FloodCheckSystem, Game.Simulation.CollapsedBuildingSystem, Game.Simulation.DamagedVehicleSystem, Game.Simulation.CitizenEvacuateSystem, Game.Simulation.EvacuationDispatchSystem
- Decompiled from: Game.dll -- Game.Prefabs.WeatherPhenomenonData, Game.Prefabs.DisasterConfigurationData, Game.Prefabs.DestructibleObjectData, Game.Prefabs.WaterLevelChangeData, Game.Prefabs.FloodData, Game.Prefabs.DestructionData
