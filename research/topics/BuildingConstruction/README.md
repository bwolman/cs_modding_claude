# Research: Building Construction & Upgrade Pipeline

> **Status**: Complete
> **Date started**: 2026-02-17
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: The full lifecycle of buildings in CS2 -- from initial placement/spawning through construction progress, completion, service upgrades, level-ups, abandonment, condemnation, destruction, and demolition. This covers both zoned (spawnable) buildings and placed (service) buildings.

**Why**: Understanding the construction pipeline is essential for mods that trigger building construction, skip construction animations, prevent abandonment, force demolition, add custom upgrade logic, or intercept the building replacement pipeline during level-ups.

**Boundaries**: Zone block creation and vacant lot detection are covered in `research/topics/Zoning/`. Land value, rent, and the condition-based leveling system are in `research/topics/LandValueProperty/`. This document focuses on the construction state machine, the upgrade/extension system, and the demolition pipeline.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Objects | UnderConstruction, Placeholder, Attached, Destroy, DestroySystem, PlaceholderSystem |
| Game.dll | Game.Buildings | Building, BuildingFlags, BuildingCondition, Extension, ExtensionFlags, InstalledUpgrade, ServiceUpgrade, Abandoned, Condemned, ServiceUpgradeSystem, ServiceUpgradeReferencesSystem |
| Game.dll | Game.Common | Destroyed, Owner, Created, Updated, Deleted |
| Game.dll | Game.Simulation | BuildingConstructionSystem, CondemnedBuildingSystem, DestroyAbandonedSystem, BuildingUpkeepSystem |
| Game.dll | Game.Prefabs | BuildingData, BuildingExtensionData, SpawnableBuildingData, ServiceUpgradeData, ServiceUpgradeBuilding, BuildingUpgradeElement, PlaceholderBuildingData, ConsumptionData, BuildingInitializeSystem, UpgradeUtils |
| Game.dll | Game.Tools | UpgradeToolSystem, UpgradeDeletedSystem |

## Component Map

### `UnderConstruction` (Game.Objects)

The central construction state component. Added to a building entity when construction begins (either from initial spawn or from a level-up). Removed by `BuildingConstructionSystem` when construction completes.

| Field | Type | Description |
|-------|------|-------------|
| m_NewPrefab | Entity | The prefab entity this building will become when construction finishes. If Entity.Null, defaults to the building's current PrefabRef. |
| m_Progress | byte | Construction progress from 0 to 255. Values below 100 mean still under construction. At 100+ the system triggers completion. Old saves without this field default to 255 (instant complete). |
| m_Speed | byte | Random speed factor (39-89) controlling how fast construction progresses. Assigned on first tick if zero. Old saves default to 50. |

Formerly serialized as `Game.Buildings.SetLevel`. The progress formula uses bit-shifting: `progress += ((frame >> 6) + speed) * speed >> 7` per tick, so higher speed values cause faster construction.

*Source: `Game.dll` -> `Game.Objects.UnderConstruction`*

### `Building` (Game.Buildings)

The core building component present on all building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_RoadEdge | Entity | The road edge entity this building connects to |
| m_CurvePosition | float | Position along the road edge curve (0.0-1.0) |
| m_OptionMask | uint | Bitmask for building options/variants |
| m_Flags | BuildingFlags | Runtime state flags |

*Source: `Game.dll` -> `Game.Buildings.Building`*

### `BuildingFlags` (Game.Buildings)

Runtime state flags on building entities.

| Flag | Value | Description |
|------|-------|-------------|
| None | 0 | Default state |
| HighRentWarning | 1 | Over 70% of renters overpaying |
| StreetLightsOff | 2 | Street lights disabled |
| LowEfficiency | 4 | Building operating below efficiency threshold |
| Illuminated | 8 | Building is illuminated |

*Source: `Game.dll` -> `Game.Buildings.BuildingFlags`*

### `BuildingFlags` (Game.Prefabs)

Prefab-level flags defining building connectivity and access requirements.

| Flag | Value | Description |
|------|-------|-------------|
| RequireRoad | 0x1 | Building must be adjacent to a road |
| NoRoadConnection | 0x2 | No road connection needed |
| LeftAccess | 0x4 | Has left-side access |
| RightAccess | 0x8 | Has right-side access |
| BackAccess | 0x10 | Has back access |
| RestrictedPedestrian | 0x20 | Restricted pedestrian access |
| RestrictedCar | 0x40 | Restricted car access |
| ColorizeLot | 0x80 | Lot surface is colorized |
| HasLowVoltageNode | 0x100 | Has low-voltage electricity node |
| HasWaterNode | 0x200 | Has water pipe node |
| HasSewageNode | 0x400 | Has sewage pipe node |
| HasInsideRoom | 0x800 | Has interior room |
| RestrictedParking | 0x1000 | Restricted parking access |
| RestrictedTrack | 0x2000 | Restricted track access |
| CanBeOnRoad | 0x4000 | Can be placed on a road |
| CanBeOnRoadArea | 0x8000 | Can be placed on a road area |
| RequireAccess | 0x10000 | Requires access point |
| CanBeRoadSide | 0x20000 | Can be placed roadside |
| HasResourceNode | 0x40000 | Has resource extraction node |

*Source: `Game.dll` -> `Game.Prefabs.BuildingFlags`*

### `BuildingData` (Game.Prefabs)

Per-building prefab configuration defining lot size and physical properties.

| Field | Type | Description |
|-------|------|-------------|
| m_LotSize | int2 | Lot dimensions in zone cells (width x depth) |
| m_Flags | BuildingFlags | Prefab-level building flags (access, connectivity) |

*Source: `Game.dll` -> `Game.Prefabs.BuildingData`*

### `BuildingExtensionData` (Game.Prefabs)

Prefab component for service upgrade buildings that extend a main building.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | Local-space position offset relative to the parent building |
| m_LotSize | int2 | Lot dimensions of the extension in zone cells |
| m_External | bool | Whether the extension is placed external to the main building |
| m_HasUndergroundElements | bool | Whether the extension has underground components |

*Source: `Game.dll` -> `Game.Prefabs.BuildingExtensionData`*

### `SpawnableBuildingData` (Game.Prefabs)

Links zoned buildings to their zone type and tracks the building level.

| Field | Type | Description |
|-------|------|-------------|
| m_ZonePrefab | Entity | Reference to the zone prefab entity this building belongs to |
| m_Level | byte | Building level (1-5). ZoneSpawnSystem only selects level 1 for new spawns. |

*Source: `Game.dll` -> `Game.Prefabs.SpawnableBuildingData`*

### `ConsumptionData` (Game.Prefabs)

Defines a building's utility consumption rates. Implements `ICombineData` so upgrade extensions can add their consumption to the main building.

| Field | Type | Description |
|-------|------|-------------|
| m_Upkeep | int | Monetary upkeep cost per day |
| m_ElectricityConsumption | float | Electricity consumption rate |
| m_WaterConsumption | float | Water consumption rate |
| m_GarbageAccumulation | float | Garbage generation rate |
| m_TelecomNeed | float | Telecom/internet need level |

The `AddArchetypeComponents` method auto-adds `ElectricityConsumer`, `WaterConsumer`, `GarbageProducer`, and `TelecomConsumer` components when the respective consumption values are > 0.

*Source: `Game.dll` -> `Game.Prefabs.ConsumptionData`*

### `Abandoned` (Game.Buildings)

Added to buildings by `BuildingUpkeepSystem.LeveldownJob` when condition drops below the abandon threshold.

| Field | Type | Description |
|-------|------|-------------|
| m_AbandonmentTime | uint | Simulation frame when abandonment occurred |

The abandonment time is compared against `BuildingConfigurationData.m_AbandonedDestroyDelay` by `DestroyAbandonedSystem` to determine when to collapse the building.

*Source: `Game.dll` -> `Game.Buildings.Abandoned`*

### `Condemned` (Game.Buildings)

Tag component (no fields) added by `ZoneCheckSystem` when a building's zone type no longer matches its underlying zone block cells. Triggers the demolition pipeline via `CondemnedBuildingSystem`.

*Source: `Game.dll` -> `Game.Buildings.Condemned`*

### `Destroyed` (Game.Common)

Added to buildings after destruction events (fire, collapse, disaster).

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The event entity that caused the destruction |
| m_Cleared | float | Cleanup progress (0.0 to 1.0) |

*Source: `Game.dll` -> `Game.Common.Destroyed`*

### `Placeholder` (Game.Objects)

Empty tag component marking entities that are placeholder objects for future sub-object placement. Used in the building extension/upgrade pipeline to mark positions where service upgrades can be installed.

*Source: `Game.dll` -> `Game.Objects.Placeholder`*

### `Attached` (Game.Objects)

Links a child object (upgrade, extension) to its parent building.

| Field | Type | Description |
|-------|------|-------------|
| m_Parent | Entity | The parent building entity |
| m_OldParent | Entity | Previous parent (used during reparenting) |
| m_CurvePosition | float | Position along parent's curve |

*Source: `Game.dll` -> `Game.Objects.Attached`*

### `Extension` (Game.Buildings)

Marks a building entity as a building extension.

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | ExtensionFlags | Extension state flags |

`ExtensionFlags` values: `Disabled` (1), `HasUnderground` (2).

*Source: `Game.dll` -> `Game.Buildings.Extension`*

### `InstalledUpgrade` (Game.Buildings)

Buffer element on building entities listing all installed service upgrades.

| Field | Type | Description |
|-------|------|-------------|
| m_Upgrade | Entity | The upgrade entity installed on this building |
| m_OptionMask | uint | Option bitmask for this upgrade variant |

Has implicit conversion to Entity. Buffer capacity is 0 (heap-allocated).

*Source: `Game.dll` -> `Game.Buildings.InstalledUpgrade`*

### `ServiceUpgrade` (Game.Buildings)

Tag component marking an entity as a service upgrade (fire station upgrade, police HQ upgrade, etc.). Used by `ServiceUpgradeSystem` to track upgrade creation and deletion.

*Source: `Game.dll` -> `Game.Buildings.ServiceUpgrade`*

### `ServiceUpgradeData` (Game.Prefabs)

Prefab data for service upgrade buildings.

| Field | Type | Description |
|-------|------|-------------|
| m_UpgradeCost | uint | Cost to install this upgrade |
| m_XPReward | int | XP gained when installing |
| m_MaxPlacementOffset | int | Maximum placement offset from parent building |
| m_MaxPlacementDistance | float | Maximum distance from parent building |
| m_ForbidMultiple | bool | Whether only one instance can be installed |

*Source: `Game.dll` -> `Game.Prefabs.ServiceUpgradeData`*

### `ServiceUpgradeBuilding` (Game.Prefabs)

Buffer element on service upgrade prefabs listing which buildings they can be installed on.

| Field | Type | Description |
|-------|------|-------------|
| m_Building | Entity | A building prefab entity this upgrade is compatible with |

*Source: `Game.dll` -> `Game.Prefabs.ServiceUpgradeBuilding`*

### `BuildingUpgradeElement` (Game.Prefabs)

Buffer element on building prefabs listing available upgrades.

| Field | Type | Description |
|-------|------|-------------|
| m_Upgrade | Entity | An upgrade prefab entity available for this building |

*Source: `Game.dll` -> `Game.Prefabs.BuildingUpgradeElement`*

### `PlaceholderBuildingData` (Game.Prefabs)

Defines what zone type a placeholder building represents. Used by `ZoneCheckSystem.ValidateAttachedParent` to verify zone compatibility.

| Field | Type | Description |
|-------|------|-------------|
| m_ZonePrefab | Entity | Zone prefab this placeholder represents |
| m_Type | BuildingType | Building classification type |

*Source: `Game.dll` -> `Game.Prefabs.PlaceholderBuildingData`*

### `Destroy` (Game.Objects)

Event component used to request object destruction via the `DestroySystem` pipeline.

| Field | Type | Description |
|-------|------|-------------|
| m_Object | Entity | The object to destroy |
| m_Event | Entity | The event causing the destruction (Entity.Null for manual) |

*Source: `Game.dll` -> `Game.Objects.Destroy`*

## System Map

### `BuildingConstructionSystem` (Game.Simulation)

The primary system managing construction progress and completion.

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update interval**: 64 frames
- **Query**: `[UnderConstruction, Building]`, excluding `[Destroyed, Deleted, Temp]`
- **Dependencies**: SimulationSystem, TerrainSystem, ZoneSpawnSystem, CityConfigurationSystem, EndFrameBarrier

**Key logic** (`BuildingConstructionJob`):
1. For each building with `UnderConstruction` where `m_Progress < 100`:
   - If `m_Speed == 0`: assigns random speed (39-89)
   - If `m_DebugFastSpawn`: sets progress to 100 (instant)
   - If `m_Progress == 0`: increments to 1 and calls `UpdateCranes()` (initial crane placement)
   - Otherwise: advances progress using `((frame >> 6) + speed) * speed >> 7` formula
   - Randomly (1/10 chance) repositions construction cranes within building bounds
2. When `m_Progress >= 100` (construction complete):
   - If `m_NewPrefab == Entity.Null`, defaults to current `PrefabRef.m_Prefab`
   - Calls `UpdatePrefab()` which:
     - Sets `PrefabRef` to the new prefab
     - Adds `Updated` tag to trigger re-rendering
     - Resets `MeshBatch` entries (invalidates render cache)
     - Deletes old sub-areas, creates new sub-areas from new prefab
     - Handles sub-net reconnection (preserves connected external edges)
     - Creates new sub-nets from new prefab
   - Removes `UnderConstruction` component
   - Records previous prefab in `TerrainSystem.BuildingUpgradeWriter` (for terrain updates)

### `CondemnedBuildingSystem` (Game.Simulation)

Processes condemned buildings for demolition.

- **Base class**: GameSystemBase
- **Update interval**: 64 frames (with frame group filter, 16 groups)
- **Query**: `[Condemned, Building, UpdateFrame]`, excluding `[Destroyed, Deleted, Temp]`
- **Key logic** (`CondemnedBuildingJob`):
  - For each condemned building, rolls a 1-in-4 random chance each tick
  - If the roll succeeds: adds `Deleted` component (destroys the building)
  - This means condemned buildings have a ~25% chance per tick of being demolished
  - With 64-frame update interval and 16 groups, each building is checked every 1024 frames
  - Expected time to demolition: ~4 checks on average (4096 frames total)

### `DestroyAbandonedSystem` (Game.Simulation)

Converts long-abandoned buildings into destroyed (collapsed) buildings.

- **Base class**: GameSystemBase
- **Update interval**: 4096 frames
- **Query**: `[Building, Abandoned]`, excluding `[Destroyed]`
- **Key logic** (`DestroyAbandonedJob`):
  - Checks `Abandoned.m_AbandonmentTime + BuildingConfigurationData.m_AbandonedDestroyDelay <= currentFrame`
  - When the delay expires:
    - Creates a `Damage` event entity
    - Creates a `Destroy` event entity targeting the building
    - Removes problem/fatal-problem icons
    - Adds `m_AbandonedCollapsedNotification` icon at FatalProblem priority
  - The building is not directly destroyed -- the `DestroySystem` processes the `Destroy` event and adds the `Destroyed` component

### `BuildingConstructionSystem` Construction Speed

The construction progress formula in detail:

```
// Per tick (every 64 frames):
if (m_Speed == 0) m_Speed = random(39, 89);
if (m_Progress == 0) m_Progress = 1; // first tick: init cranes

// Subsequent ticks:
uint n = (simulationFrame >> 6) + speed;
uint delta = ((n + 1) * speed >> 7) - (n * speed >> 7);
m_Progress = min(255, m_Progress + delta);
```

With speed=50 (default for old saves): delta averages ~19 per tick. At 64 frames/tick, construction takes roughly 5-6 ticks (~320-384 frames) to reach 100.

With speed=89 (max): delta averages ~62 per tick. Construction completes in ~2 ticks (~128 frames).

With speed=39 (min): delta averages ~12 per tick. Construction takes ~8-9 ticks (~512-576 frames).

### `ServiceUpgradeSystem` (Game.Buildings)

Manages service upgrade installation and removal.

- **Base class**: GameSystemBase
- **Query**: `[ServiceUpgrade, Object]` with `[Created | Deleted]`, excluding `[Temp]`; also `[InstalledUpgrade, Deleted]` excluding `[Temp]`
- **Key behavior**: When a `ServiceUpgrade` entity is created or deleted, updates the parent building's `InstalledUpgrade` buffer. Uses `UpgradeUtils.CombineStats()` to aggregate upgrade effects onto the building.

### `BuildingInitializeSystem` (Game.Prefabs)

Initializes building prefab entities during the prefab loading phase.

- **Base class**: GameSystemBase
- **Key behavior**: Sets up `BuildingData.m_Flags` based on sub-nets (detecting water, sewage, electricity nodes), calculates lot sizes, and resolves placeholder building requirements. Runs `FindConnectionRequirementsJob` to determine which utility connections each building prefab needs.

### `UpgradeToolSystem` (Game.Tools)

The tool system for placing service upgrades on buildings.

- **Base class**: ObjectToolBaseSystem
- **Key behavior**: When the player selects a service upgrade and clicks on a building, creates a `CreationDefinition` entity with the upgrade prefab and owner. The `EndFrameBarrier` processes the creation, which triggers `ServiceUpgradeSystem` to update the parent's `InstalledUpgrade` buffer.

## Data Flow

```
=== ZONED BUILDING SPAWNING ===

[Zone Demand Threshold Met]
    |
    v
ZoneSpawnSystem.EvaluateSpawnAreas (every 16 frames)
    |-- Finds VacantLot on Block entities
    |-- Selects building prefab (level 1, matching zone type)
    |-- Creates CreationDefinition entity
    |-- EndFrameBarrier processes creation
    |
    v
[Building Entity Created]
    |-- Building component (m_RoadEdge, m_CurvePosition)
    |-- UnderConstruction { m_NewPrefab=Entity.Null, m_Progress=0, m_Speed=0 }
    |-- PrefabRef -> building prefab
    |-- Zone cells marked CellFlags.Occupied
    |
    v
BuildingConstructionSystem (every 64 frames)
    |-- Tick 1: m_Speed = random(39,89), m_Progress = 1, spawn cranes
    |-- Tick 2-N: m_Progress += speed-based delta
    |-- Cranes reposition randomly (1/10 chance per tick)
    |
    v
[m_Progress >= 100] -- Construction Complete
    |-- UpdatePrefab(): sets PrefabRef to m_NewPrefab
    |-- Deletes old sub-areas, creates new ones
    |-- Reconnects sub-nets
    |-- Removes UnderConstruction component
    |-- Adds Updated tag (triggers re-render)
    |
    v
[Functional Building]
    |-- PropertyOnMarket added (vacant units)
    |-- Renters move in
    |-- BuildingCondition tracking begins


=== LEVEL-UP (from LandValueProperty research) ===

[BuildingCondition >= levelingCost]
    |
    v
BuildingUpkeepSystem.LevelupJob
    |-- Selects higher-level prefab (matching zone, lot size)
    |-- Adds UnderConstruction { m_NewPrefab=higherLevelPrefab, m_Progress=255 }
    |   (m_Progress=255 = instant completion for level-ups)
    |
    v
BuildingConstructionSystem
    |-- m_Progress >= 100 -> immediate UpdatePrefab()
    |-- Swaps to higher-level building prefab
    |-- Removes UnderConstruction


=== SERVICE UPGRADE INSTALLATION ===

[Player selects upgrade in UpgradeToolSystem]
    |
    v
UpgradeToolSystem creates CreationDefinition entity
    |-- m_Prefab = upgrade prefab
    |-- m_Owner = target building entity
    |
    v
EndFrameBarrier processes creation
    |-- Creates upgrade entity with:
    |   ServiceUpgrade component
    |   Attached { m_Parent = building }
    |   Owner { m_Owner = building }
    |
    v
ServiceUpgradeSystem detects Created + ServiceUpgrade
    |-- Adds InstalledUpgrade { m_Upgrade } to parent building's buffer
    |-- UpgradeUtils.CombineStats() aggregates consumption/effects
    |-- ConsumptionData.Combine() adds upgrade's utility needs
    |
    v
[Building has enhanced capabilities]


=== ABANDONMENT PIPELINE ===

[BuildingCondition <= -abandonCost]
    |
    v
BuildingUpkeepSystem.LeveldownJob
    |-- Removes utility consumers (electricity, water, garbage, mail, telecom)
    |-- Doubles CrimeProducer.m_Crime
    |-- Clears HighRentWarning flag and icon
    |-- Evicts all renters (reverse iterate Renter buffer)
    |-- Adds Abandoned { m_AbandonmentTime = currentFrame }
    |-- Adds m_AbandonedNotification icon
    |
    v
[Abandoned Building -- waiting]
    |
    v
DestroyAbandonedSystem (every 4096 frames)
    |-- Checks: m_AbandonmentTime + m_AbandonedDestroyDelay <= currentFrame
    |-- When expired:
    |   Creates Damage event entity
    |   Creates Destroy event entity
    |   Adds m_AbandonedCollapsedNotification icon
    |
    v
DestroySystem processes Destroy event
    |-- Adds Destroyed component
    |-- Building becomes a ruin (rubble mesh)


=== CONDEMNATION PIPELINE ===

[Zone type changed under existing building]
    |
    v
ZoneCheckSystem.CheckBuildingZonesJob
    |-- Validates building cells against zone block cells
    |-- If mismatch: adds Condemned tag + notification icon
    |
    v
CondemnedBuildingSystem (every 64 frames, 16 groups)
    |-- 25% random chance per check to demolish
    |-- If selected: adds Deleted component
    |
    v
[Building removed from world]


=== MANUAL DEMOLITION ===

[Player uses bulldoze tool on building]
    |
    v
Adds Deleted component directly
    |-- Entity cleanup systems process deletion
    |-- Zone cells unmarked (CellFlags.Occupied cleared)
    |-- Renters evicted
```

## Prefab & Configuration

| Value | Source | Default | Description |
|-------|--------|---------|-------------|
| Lot size | BuildingData.m_LotSize | Varies by prefab | Width x depth in zone cells |
| Prefab flags | BuildingData.m_Flags | Varies | Access/connectivity requirements |
| Building level | SpawnableBuildingData.m_Level | 1 (for new spawns) | Current building level (1-5) |
| Zone prefab | SpawnableBuildingData.m_ZonePrefab | Varies | Zone type this building belongs to |
| Upkeep cost | ConsumptionData.m_Upkeep | Varies by prefab | Daily monetary upkeep |
| Electricity need | ConsumptionData.m_ElectricityConsumption | Varies | Electricity consumption rate |
| Water need | ConsumptionData.m_WaterConsumption | Varies | Water consumption rate |
| Garbage rate | ConsumptionData.m_GarbageAccumulation | Varies | Garbage generation rate |
| Upgrade cost | ServiceUpgradeData.m_UpgradeCost | Varies | Cost to install service upgrade |
| Upgrade XP | ServiceUpgradeData.m_XPReward | Varies | XP awarded on installation |
| Forbid multiple | ServiceUpgradeData.m_ForbidMultiple | false | Only one instance allowed |
| Extension position | BuildingExtensionData.m_Position | Varies | Local-space offset from parent |
| Extension external | BuildingExtensionData.m_External | Varies | Placed outside main building |
| Abandoned destroy delay | BuildingConfigurationData.m_AbandonedDestroyDelay | (from prefab) | Frames before abandoned building collapses |
| Construction update interval | Hardcoded | 64 frames | BuildingConstructionSystem tick rate |
| Condemned check interval | Hardcoded | 1024 frames (64 * 16 groups) | Per-building condemnation check |
| Condemned demolish chance | Hardcoded | 25% (1 in 4) | Chance per check to demolish condemned building |
| Destroy abandoned interval | Hardcoded | 4096 frames | DestroyAbandonedSystem tick rate |
| Construction speed range | Hardcoded | 39-89 | Random speed assigned to new constructions |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.BuildingConstructionSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept construction completion to modify which prefab the building becomes, skip construction entirely, or add custom post-construction logic.
- **Risk level**: Low -- only affects buildings with UnderConstruction component
- **Side effects**: Skipping the system freezes all construction progress

### Candidate 2: `Game.Simulation.CondemnedBuildingSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (skip)
- **What it enables**: Prevent condemned buildings from being demolished. A prefix returning false disables the entire demolition pipeline for condemned buildings.
- **Risk level**: Low -- only affects condemned buildings
- **Side effects**: Condemned buildings persist indefinitely until the patch is removed

### Candidate 3: `Game.Simulation.DestroyAbandonedSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (skip) or Postfix
- **What it enables**: Prevent abandoned buildings from collapsing into ruins. A prefix returning false keeps abandoned buildings standing indefinitely.
- **Risk level**: Low -- only affects abandoned buildings
- **Side effects**: Abandoned buildings never collapse but remain non-functional

### Candidate 4: `Game.Buildings.ZoneCheckSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (skip)
- **What it enables**: Prevent buildings from being condemned when zone types change. Useful for mods that want buildings to persist through zone changes.
- **Risk level**: Medium -- affects zone-building compatibility checks
- **Side effects**: Buildings may visually mismatch their zone type

### Candidate 5: `Game.Simulation.BuildingUpkeepSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (skip) or system replacement
- **What it enables**: Override the entire leveling/abandonment pipeline. Community mods (RealisticWorkplacesAndHouseholds, UrbanInequality) fully replace this system.
- **Risk level**: High -- affects all building condition, leveling, and abandonment
- **Side effects**: Disabling prevents all level-ups and abandonments

### Alternative: ECS System Approach (No Harmony)

A custom `GameSystemBase` can:
1. Add `UnderConstruction` to trigger construction on any building
2. Set `UnderConstruction.m_Progress = 100` to skip the animation
3. Remove `Abandoned` / `Condemned` / `Destroyed` to restore buildings
4. Add/remove `InstalledUpgrade` buffer entries to manage upgrades
5. Create `Destroy` event entities to trigger demolition

## Examples

### Example 1: Trigger Construction on an Existing Building

Start the construction animation on a building, optionally changing its prefab.

```csharp
public partial class TriggerConstructionSystem : GameSystemBase
{
    protected override void OnCreate() { base.OnCreate(); }
    protected override void OnUpdate() { }

    /// <summary>
    /// Start construction on a building entity. If newPrefab is Entity.Null,
    /// the building keeps its current prefab (just replays the animation).
    /// </summary>
    public void StartConstruction(Entity buildingEntity, Entity newPrefab = default)
    {
        if (!EntityManager.HasComponent<Building>(buildingEntity))
            return;

        // Remove existing construction if any
        if (EntityManager.HasComponent<UnderConstruction>(buildingEntity))
            EntityManager.RemoveComponent<UnderConstruction>(buildingEntity);

        // Add UnderConstruction with progress=0 for full animation
        EntityManager.AddComponentData(buildingEntity, new UnderConstruction
        {
            m_NewPrefab = newPrefab,
            m_Progress = 0,
            m_Speed = 0  // System assigns random speed (39-89)
        });
    }
}
```

### Example 2: Complete Construction Instantly

Skip the construction animation and immediately finish construction.

```csharp
public void CompleteConstructionInstantly(EntityManager em, Entity buildingEntity)
{
    if (!em.HasComponent<Building>(buildingEntity))
        return;

    if (em.HasComponent<UnderConstruction>(buildingEntity))
    {
        // Setting m_Progress >= 100 triggers completion on the next
        // BuildingConstructionSystem tick (every 64 frames)
        var uc = em.GetComponentData<UnderConstruction>(buildingEntity);
        uc.m_Progress = 100;
        em.SetComponentData(buildingEntity, uc);
    }
}
```

Alternatively, to upgrade a building to a new prefab instantly:

```csharp
public void UpgradeBuildingInstantly(EntityManager em, Entity building, Entity newPrefab)
{
    // m_Progress=255 is what BuildingUpkeepSystem.LevelupJob uses
    // for level-up construction -- it completes on the very next tick.
    em.AddComponentData(building, new UnderConstruction
    {
        m_NewPrefab = newPrefab,
        m_Progress = 255,
        m_Speed = 50
    });
}
```

### Example 3: Prevent Building Abandonment

Remove abandonment and restore a building to functional state.

```csharp
public void RestoreAbandonedBuilding(EntityManager em, Entity buildingEntity)
{
    if (!em.HasComponent<Abandoned>(buildingEntity))
        return;

    // 1. Remove abandoned status
    em.RemoveComponent<Abandoned>(buildingEntity);

    // 2. Reset building condition to neutral
    if (em.HasComponent<BuildingCondition>(buildingEntity))
    {
        em.SetComponentData(buildingEntity, new BuildingCondition { m_Condition = 0 });
    }

    // 3. Re-list on property market
    if (!em.HasComponent<PropertyToBeOnMarket>(buildingEntity))
    {
        em.AddComponent<PropertyToBeOnMarket>(buildingEntity);
    }

    // 4. Restore utility consumers (removed during LeveldownJob)
    if (!em.HasComponent<ElectricityConsumer>(buildingEntity))
        em.AddComponentData(buildingEntity, default(ElectricityConsumer));
    if (!em.HasComponent<WaterConsumer>(buildingEntity))
        em.AddComponentData(buildingEntity, default(WaterConsumer));
    if (!em.HasComponent<GarbageProducer>(buildingEntity))
        em.AddComponentData(buildingEntity, default(GarbageProducer));
    if (!em.HasComponent<MailProducer>(buildingEntity))
        em.AddComponentData(buildingEntity, default(MailProducer));

    // 5. Halve the doubled crime producer
    if (em.HasComponent<CrimeProducer>(buildingEntity))
    {
        var crime = em.GetComponentData<CrimeProducer>(buildingEntity);
        crime.m_Crime *= 0.5f;
        em.SetComponentData(buildingEntity, crime);
    }
}
```

### Example 4: Force Demolish a Building

Programmatically demolish any building entity.

```csharp
public void DemolishBuilding(EntityManager em, Entity buildingEntity)
{
    if (!em.HasComponent<Building>(buildingEntity))
        return;

    // Option A: Instant deletion (like bulldoze tool)
    em.AddComponent<Deleted>(buildingEntity);

    // Option B: Via the Destroy event pipeline (triggers collapse animation)
    // Entity destroyEvent = em.CreateEntity();
    // em.AddComponentData(destroyEvent, new Game.Events.Event());
    // em.AddComponentData(destroyEvent, new Destroy(buildingEntity, Entity.Null));
}
```

### Example 5: Spawn a Building at a Specific Location

Create a new building entity at a world position using the `CreationDefinition` pipeline.

```csharp
public partial class BuildingSpawnerSystem : GameSystemBase
{
    private EndFrameBarrier _endFrameBarrier;

    protected override void OnCreate()
    {
        base.OnCreate();
        _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Spawn a building at the given world position with specified rotation.
    /// The buildingPrefab must be a valid building prefab entity.
    /// </summary>
    public void SpawnBuilding(Entity buildingPrefab, float3 worldPosition, quaternion rotation)
    {
        var ecb = _endFrameBarrier.CreateCommandBuffer();

        Entity definition = ecb.CreateEntity();
        ecb.AddComponent(definition, new CreationDefinition
        {
            m_Prefab = buildingPrefab,
            m_Flags = CreationFlags.Permanent
        });
        ecb.AddComponent(definition, new ObjectDefinition
        {
            m_Position = worldPosition,
            m_Rotation = rotation,
            m_Probability = 100
        });
        ecb.AddComponent(definition, default(Updated));
    }
}
```

## Advanced Patterns

### Service Upgrade Aggregation via ICombineData

The game uses the `ICombineData<T>` interface and `UpgradeUtils.CombineStats()` to aggregate service upgrade effects onto the parent building. When a system needs the effective consumption data for a building (including all upgrades), it:

1. Reads the base `ConsumptionData` from the building's prefab
2. Iterates the building's `InstalledUpgrade` buffer
3. For each upgrade: resolves `PrefabRef -> ConsumptionData` and calls `data.Combine(upgradeData)`
4. `ConsumptionData.Combine()` adds electricity, water, garbage, and upkeep; takes max of telecom need

This pattern applies to all `ICombineData` components, not just `ConsumptionData`. Mods that add custom upgrade effects should implement `ICombineData` and register with the combine pipeline.

### Construction Speed Modding

To control construction speed without patching, a custom system running after `BuildingConstructionSystem` can:

1. Query `[UnderConstruction, Building]`
2. Read `UnderConstruction.m_Speed`
3. Override `m_Speed` to a fixed value (e.g., 89 for fastest, 39 for slowest)
4. Or set `m_Progress = 100` to force instant completion

The `m_DebugFastSpawn` flag from `ZoneSpawnSystem.debugFastSpawn` also provides instant completion but is a global setting.

### Condemned Building Protection

To protect buildings from condemnation when zones change:

1. Create a custom system running after `ZoneCheckSystem`
2. Query for `[Condemned, Building]` with a custom tag component
3. Remove the `Condemned` component from protected buildings each tick

This is safer than patching `ZoneCheckSystem.OnUpdate` because it only affects mod-tagged buildings.

## Open Questions

- [ ] **PlaceholderSystem job logic**: The `PlaceholderSystem.PlaceholderJob` handles placeholder resolution during building construction, but the full resolution logic (matching placeholder requirements to available sub-objects) was not fully traced.
- [ ] **BuildingInitializeSystem connection requirements**: The `FindConnectionRequirementsJob` determines which utility nodes a building needs based on its sub-nets, but the exact flag-setting logic for `HasWaterNode`, `HasSewageNode`, etc. was not fully traced.
- [ ] **Upgrade placement validation**: The exact spatial validation logic in `UpgradeToolSystem` for determining whether an upgrade can be placed at a given position relative to the parent building was not fully traced. `ServiceUpgradeData.m_MaxPlacementDistance` and `m_MaxPlacementOffset` constrain placement.
- [ ] **BuildingConfigurationData.m_AbandonedDestroyDelay**: The exact default value of this field (the number of frames before an abandoned building collapses) was not traced from the prefab data.
- [x] **Construction progress formula**: Fully traced. Uses bit-shifted multiplication: `delta = ((frame >> 6 + speed) * speed >> 7) - ((frame >> 6 + speed - 1) * speed >> 7)`. Speed range 39-89 gives completion in ~128-576 frames.
- [x] **Level-up construction**: Level-ups set `m_Progress = 255` for instant completion. The `BuildingConstructionSystem` swaps the prefab and removes `UnderConstruction` on the very next tick.
- [x] **Condemnation demolition**: `CondemnedBuildingSystem` uses a 25% random chance per check (every 1024 frames per building) to add `Deleted`, meaning condemned buildings survive approximately 4 checks on average.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Components: Game.Objects.UnderConstruction, Game.Objects.Placeholder, Game.Objects.Attached, Game.Objects.Destroy, Game.Buildings.Building, Game.Buildings.BuildingFlags, Game.Buildings.Extension, Game.Buildings.ExtensionFlags, Game.Buildings.InstalledUpgrade, Game.Buildings.ServiceUpgrade, Game.Buildings.Abandoned, Game.Buildings.Condemned, Game.Common.Destroyed, Game.Prefabs.BuildingData, Game.Prefabs.BuildingExtensionData, Game.Prefabs.SpawnableBuildingData, Game.Prefabs.ConsumptionData, Game.Prefabs.ServiceUpgradeData, Game.Prefabs.ServiceUpgradeBuilding, Game.Prefabs.BuildingUpgradeElement, Game.Prefabs.PlaceholderBuildingData
- Systems: Game.Simulation.BuildingConstructionSystem, Game.Simulation.CondemnedBuildingSystem, Game.Simulation.DestroyAbandonedSystem, Game.Buildings.ServiceUpgradeSystem, Game.Prefabs.BuildingInitializeSystem, Game.Tools.UpgradeToolSystem
- Utility: Game.Prefabs.UpgradeUtils
- Related research: Zoning (zone block spawning), LandValueProperty (condition/leveling)
