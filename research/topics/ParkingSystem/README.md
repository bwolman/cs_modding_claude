# Research: Parking System

> **Status**: Complete
> **Date started**: 2026-02-17
> **Last updated**: 2026-02-17
> **GitHub Issue**: #24

## Scope

**What we're investigating**: How vehicles find, occupy, and pay for parking in CS2. This covers street parking (ParkingLane on road edges), garage parking (GarageLane inside buildings), dedicated parking facilities, the parking search algorithm, parking fee calculation, capacity tracking, and district parking policies.

**Why**: To understand the parking simulation so mods can modify parking capacity, adjust or eliminate parking fees, disable street parking in districts, create custom parking search behavior, or build parking management overlays.

**Boundaries**: Vehicle navigation and lane-level driving is covered in [TrafficFlowLaneControl](../TrafficFlowLaneControl/README.md). The pathfinding cost model (including `PathMethod.Parking`) is covered in [Pathfinding](../Pathfinding/README.md). Vehicle spawning, trip initiation, and the `PersonalCarAISystem` driving pipeline are covered in [VehicleSpawning](../VehicleSpawning/README.md). This research focuses specifically on the parking subsystem: data structures, capacity calculation, fee propagation, and the search/park/pay lifecycle.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Net | ParkingLane, ParkingLaneFlags, GarageLane |
| Game.dll | Game.Buildings | ParkingFacility, ParkingFacilityFlags, CarParkingFacility, BicycleParkingFacility |
| Game.dll | Game.Vehicles | ParkedCar, FixParkingLocation, PersonalCar |
| Game.dll | Game.Prefabs | ParkingLaneData, ParkingFacilityData, ParkingSpaceData, ParkingFacility (managed), ParkingLane (managed), ParkingSpace (managed), ParkingFacilityMode |
| Game.dll | Game.Routes | ParkingSpace, CarParking, BicycleParking |
| Game.dll | Game.Pathfind | ParkingLaneDataSystem |
| Game.dll | Game.Simulation | ParkingFacilityAISystem, PersonalCarAISystem (parking logic) |
| Game.dll | Game.Vehicles | FixParkingLocationSystem, ParkedVehiclesSystem |
| Game.dll | Game.City | PlayerResource (Parking fee resource) |

## Architecture Overview

CS2's parking system operates in three layers:

1. **Lane layer**: `ParkingLane` (street parking) and `GarageLane` (building garages) are ECS components on lane entities. They track free space, fees, comfort, and access restrictions.

2. **Building layer**: `ParkingFacility` (Game.Buildings) is attached to buildings that provide parking. It tracks the runtime comfort factor and active state. The prefab `ParkingFacilityData` defines base capacity and comfort.

3. **Vehicle layer**: `ParkedCar` marks a vehicle as parked on a specific lane at a specific curve position. `FixParkingLocation` is a tag that triggers position resolution for non-standard parking targets.

The `ParkingLaneDataSystem` is the central coordinator. It runs whenever parking lanes are updated and recalculates free space, fees (from district or building modifiers), comfort factors, and access restrictions. The pathfinder reads these values when evaluating `PathMethod.Parking` edges.

### Two Parking Types

| Type | Component | Location | Capacity Model | Fee Source |
|------|-----------|----------|----------------|------------|
| Street parking | `ParkingLane` | Road edge lanes | Free space along curve (continuous or slotted) | District `DistrictModifier` with `PaidParking` option |
| Garage parking | `GarageLane` | Building interior | `m_VehicleCount` / `m_VehicleCapacity` (integer) | Building `BuildingModifier` with `PaidParking` option |

## Component Map

### `ParkingLane` (Game.Net)

Runtime component on lane entities that serve as on-street parking spaces.

| Field | Type | Description |
|-------|------|-------------|
| m_AccessRestriction | Entity | Entity defining access restrictions (building with restricted parking flags) |
| m_AdditionalStartNode | PathNode | Additional pathfind start node for improved lane connectivity |
| m_Flags | ParkingLaneFlags | Parking lane behavior flags (see enum below) |
| m_FreeSpace | float | Available parking space on the lane (in game units, 0 = full) |
| m_ParkingFee | ushort | Parking fee encoded as ushort (from district/city modifiers) |
| m_ComfortFactor | ushort | Comfort multiplier encoded as ushort (0-65535 maps to 0.0-1.0) |
| m_TaxiAvailability | ushort | Taxi availability metric for this lane |
| m_TaxiFee | ushort | Taxi starting fee for pickups at this lane |

*Source: `Game.dll` -> `Game.Net.ParkingLane`*

### `ParkingLaneFlags` (Game.Net)

| Flag | Value | Description |
|------|-------|-------------|
| Invert | 0x1 | Lane direction is inverted |
| StartingLane | 0x2 | First segment of a parking area |
| EndingLane | 0x4 | Last segment of a parking area |
| AdditionalStart | 0x8 | Has additional start node |
| ParkingInverted | 0x10 | Parking direction is inverted |
| LeftSide | 0x20 | Parking is on the left side of the road |
| RightSide | 0x40 | Parking is on the right side of the road |
| TaxiAvailabilityUpdated | 0x80 | Taxi availability was recently updated |
| TaxiAvailabilityChanged | 0x100 | Taxi availability changed since last check |
| VirtualLane | 0x200 | Not a physical lane (abstract parking) |
| FindConnections | 0x400 | Needs connection resolution |
| ParkingLeft | 0x800 | Vehicles park facing left |
| ParkingRight | 0x1000 | Vehicles park facing right |
| ParkingDisabled | 0x2000 | Parking is disabled on this lane |
| AllowEnter | 0x4000 | Vehicles can enter from this lane |
| AllowExit | 0x8000 | Vehicles can exit from this lane |
| SpecialVehicles | 0x10000 | Reserved for special vehicles |
| SecondaryStart | 0x20000 | Secondary start node |

### `GarageLane` (Game.Net)

Runtime component on lane entities that represent parking garage access points.

| Field | Type | Description |
|-------|------|-------------|
| m_ParkingFee | ushort | Fee charged for parking in the garage |
| m_ComfortFactor | ushort | Comfort multiplier (0-65535 maps to 0.0-1.0) |
| m_VehicleCount | ushort | Current number of vehicles parked |
| m_VehicleCapacity | ushort | Maximum vehicle capacity |

*Source: `Game.dll` -> `Game.Net.GarageLane`*

### `ParkingFacility` (Game.Buildings)

Runtime component on parking facility building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_ComfortFactor | float | Current comfort factor (base * efficiency) |
| m_Flags | ParkingFacilityFlags | Facility state flags |

*Source: `Game.dll` -> `Game.Buildings.ParkingFacility`*

### `ParkingFacilityFlags` (Game.Buildings)

| Flag | Value | Description |
|------|-------|-------------|
| ParkingSpacesActive | 0x1 | Facility is operational (efficiency > 0) |

### `CarParkingFacility` (Game.Buildings)

Empty marker component. Added to buildings that provide car parking. Used for entity queries.

### `BicycleParkingFacility` (Game.Buildings)

Empty marker component. Added to buildings that provide bicycle parking. Used for entity queries.

### `ParkedCar` (Game.Vehicles)

Marks a vehicle as parked in a specific lane.

| Field | Type | Description |
|-------|------|-------------|
| m_Lane | Entity | The parking lane entity (ParkingLane or GarageLane) |
| m_CurvePosition | float | Position along the lane curve (0.0-1.0) |

*Source: `Game.dll` -> `Game.Vehicles.ParkedCar`*

### `FixParkingLocation` (Game.Vehicles)

Tag component (zero-size). Added to vehicles undergoing a parking transition with a non-standard location. Consumed by `FixParkingLocationSystem` to resolve the parking position.

*Source: `Game.dll` -> `Game.Vehicles.FixParkingLocation`*

### `ParkingLaneData` (Game.Prefabs)

Prefab data defining physical dimensions of parking lane slots.

| Field | Type | Description |
|-------|------|-------------|
| m_SlotSize | float2 | Width and length of each parking slot |
| m_SlotAngle | float | Angle of parking slots (0 = parallel, 90 = perpendicular) |
| m_SlotInterval | float | Distance between parking slot centers along the lane (0 = continuous) |
| m_MaxCarLength | float | Maximum vehicle length that can fit in a slot |
| m_RoadTypes | RoadTypes | Which road types this parking lane supports (Car, Bicycle) |

When `m_SlotInterval` is 0, the lane uses **continuous free-space** calculation. When non-zero, it uses **slotted** calculation with discrete parking positions.

*Source: `Game.dll` -> `Game.Prefabs.ParkingLaneData`*

### `ParkingFacilityData` (Game.Prefabs)

Prefab data for parking facility buildings.

| Field | Type | Description |
|-------|------|-------------|
| m_RoadTypes | RoadTypes | Which vehicle types can park (Car, Bicycle) |
| m_ComfortFactor | float | Base comfort factor (0.0-1.0, higher = more desirable) |
| m_GarageMarkerCapacity | int | Number of parking spaces per garage marker |

Implements `ICombineData<ParkingFacilityData>` -- when a building has upgrades with parking facilities, capacities and comfort factors are combined (added together).

*Source: `Game.dll` -> `Game.Prefabs.ParkingFacilityData`*

### `ParkingSpaceData` (Game.Prefabs)

Empty marker component on parking space prefab entities.

### Route Components

| Component | Namespace | Description |
|-----------|-----------|-------------|
| `ParkingSpace` | Game.Routes | Empty marker on route parking space objects |
| `CarParking` | Game.Routes | Empty marker identifying car parking route stops |
| `BicycleParking` | Game.Routes | Empty marker identifying bicycle parking route stops |

### `PlayerResource.Parking` (Game.City)

The `PlayerResource` enum value `Parking` (index 12) is the service fee resource for parking revenue. When a vehicle parks, the fee is collected as a `ServiceFeeSystem.FeeEvent` with this resource type.

## System Map

### `ParkingLaneDataSystem` (Game.Pathfind)

The central parking data coordinator. Recalculates parking lane state whenever lanes are updated.

- **Base class**: GameSystemBase
- **Update phase**: Simulation (reactive -- runs when lanes have `Updated` or `PathfindUpdated` tags)
- **Queries**: Entities with (`Updated` OR `PathfindUpdated`) AND (`ParkingLane` OR `GarageLane` OR `SpawnLocation`), excluding `Deleted`/`Temp`
- **Key job**: `UpdateLaneDataJob` (IJobChunk, Burst-compiled)
- **Key behavior**:
  1. **Street parking** (ParkingLane entities):
     - Reads `ParkingLaneData` from prefab for slot dimensions
     - Calls `CalculateFreeSpace()` which counts occupied slots via `LaneObject` buffer and `ParkedCar` lookups
     - Two modes: **slotted** (when `m_SlotInterval != 0`) uses `NetUtils.GetParkingSlotCount()` and iterates discrete slots, **continuous** measures gaps between parked vehicles
     - Checks for blocked ranges from adjacent `CarLane` blockage data
     - Calls `GetParkingStats()` to resolve fees, comfort, and access restrictions
     - Writes `m_FreeSpace`, `m_ParkingFee`, `m_ComfortFactor`, `m_TaxiFee`, `m_AccessRestriction`, and flag updates
  2. **Garage parking** (GarageLane entities):
     - Calls `CountVehicles()` using `SearchSystem` spatial quad-tree to count parked cars in the garage bounds
     - Expands search bounds using `ActivityLocationElement` with `ActivityType.GarageSpot` mask
     - Calls `GetParkingStats()` for capacity, fees, and comfort
     - Writes `m_VehicleCount`, `m_VehicleCapacity`, `m_ParkingFee`, `m_ComfortFactor`
  3. **Fee calculation** (`GetParkingStats()`):
     - Walks up the owner hierarchy to find the root building
     - For **building parking**: reads `BuildingModifier` buffer, checks `BuildingOption.PaidParking`, applies `BuildingModifierType.ParkingFee`
     - For **street parking**: reads district from `BorderDistrict` (left/right side) or `CurrentDistrict`, checks `DistrictOption.PaidParking`, applies `DistrictModifierType.ParkingFee`
     - **Taxi fee**: reads `CityOption.PaidTaxiStart` and `CityModifierType.TaxiStartingFee` from city entity
  4. **Capacity fallback** (buildings without explicit `ParkingFacilityData`):
     - Default: 2 spaces per garage marker, 0.8 comfort
     - If building has `BuildingPropertyData`: capacity = `max(1, RoundToInt(m_SpaceMultiplier))`
     - If building has `WorkplaceData`: capacity = `max(2, m_MaxWorkers / 20)`

### `ParkingFacilityAISystem` (Game.Simulation)

Manages parking facility building state based on efficiency.

- **Base class**: GameSystemBase
- **Update phase**: Simulation (UpdateFrame-based, index 12)
- **Queries**: Buildings with `ParkingFacility` + `PrefabRef` + `Efficiency`
- **Key job**: `ParkingFacilityTickJob` (IJobChunk, Burst-compiled)
- **Key behavior**:
  1. Reads `ParkingFacilityData` from prefab, combines with installed upgrade data
  2. Computes effective comfort: `saturate(prefabComfortFactor * efficiency)`
  3. Sets/clears `ParkingSpacesActive` flag based on `efficiency > 0`
  4. When comfort or active state changes, walks `SubLane`/`SubNet`/`SubObject` hierarchy and adds `PathfindUpdated` to all parking/garage lanes to trigger `ParkingLaneDataSystem` recalculation

### `FixParkingLocationSystem` (Game.Vehicles)

Resolves parking positions for vehicles with the `FixParkingLocation` tag.

- **Base class**: GameSystemBase
- **Key jobs**: `CollectParkedCarsJob`, `FixParkingLocationJob`
- **Key behavior**: Spatial search for available parking spots, position resolution for non-standard parking locations (building lots, cargo docks)

### `ParkedVehiclesSystem` (Game.Vehicles)

Manages parked vehicle lifecycle: spawning service vehicles from buildings, collecting deleted vehicles, handling duplicate vehicle cleanup.

- **Base class**: GameSystemBase
- **Key jobs**: `FindParkingLocationsJob`, `CollectDeletedVehiclesJob`, `DuplicateVehiclesJob`, plus per-service spawn jobs (police, fire, healthcare, transport, post, maintenance, garbage)

### `PersonalCarAISystem` Parking Logic (Game.Simulation)

The `PersonalCarTickJob` inside `PersonalCarAISystem` handles the vehicle-side parking search. Key methods:

- **`CheckParkingSpace()`**: Validates the current target parking spot. Scans ahead up to 40000 path nodes. For each `PathElement`, checks `ParkingLane` free space or `GarageLane` capacity.
- **`StartDisembarking()`**: When parking, reads fee from `ParkingLane.m_ParkingFee` or `GarageLane.m_ParkingFee`, creates `MoneyTransfer` and `ServiceFeeSystem.FeeEvent` with `PlayerResource.Parking`.
- **MovingToParked transition**: Removes 11 movement components, adds `ParkedCar` + `Stopped` + `Updated`.

*Cross-reference: [VehicleSpawning](../VehicleSpawning/README.md) for the full PersonalCarAISystem documentation.*

## Data Flow

### Parking Data Update Pipeline

```
TRIGGER: Lane created/modified (Updated or PathfindUpdated tag)
    |
    v
ParkingLaneDataSystem.UpdateLaneDataJob
    |
    +-- STREET PARKING (ParkingLane entities)
    |   1. Read ParkingLaneData from prefab (slot size, interval, max car length)
    |   2. Sort LaneObject buffer by curve position
    |   3. CalculateFreeSpace():
    |      - Slotted mode: count occupied slots via ParkedCar lookup
    |      - Continuous mode: measure gaps between parked vehicles
    |      - Account for blocked ranges from adjacent CarLane blockage
    |   4. GetParkingStats():
    |      - Walk owner hierarchy to root building
    |      - Read district/building parking fee modifiers
    |      - Compute comfort factor from ParkingFacilityData
    |      - Determine access restrictions from BuildingFlags
    |   5. Write: m_FreeSpace, m_ParkingFee, m_ComfortFactor, m_TaxiFee, flags
    |
    +-- GARAGE PARKING (GarageLane entities)
    |   1. CountVehicles() via SearchSystem quad-tree spatial query
    |   2. Expand bounds using ActivityLocationElement (GarageSpot)
    |   3. GetParkingStats() for capacity, fee, comfort
    |   4. Write: m_VehicleCount, m_VehicleCapacity, m_ParkingFee, m_ComfortFactor
    |
    v
PATHFINDER reads ParkingLane/GarageLane data
    Uses m_FreeSpace, m_ComfortFactor, m_ParkingFee
    to weight PathMethod.Parking edges in route cost
```

### Vehicle Parking Lifecycle

```
VEHICLE APPROACHING DESTINATION
    PersonalCarAISystem.PersonalCarTickJob
        |
        v
    CheckParkingSpace()
        Scans ahead in PathElement buffer (up to 40000 nodes)
        For each parking target:
          ParkingLane -> VehicleUtils.FindFreeParkingSpace()
          GarageLane -> check m_VehicleCount < m_VehicleCapacity
        If no parking found:
          Set PathFlags.Obsolete -> triggers re-pathfinding
        |
        v
    PARKING SPOT FOUND
        Vehicle drives to target lane + curve position
        |
        v
    StartDisembarking()
        1. Read fee: ParkingLane.m_ParkingFee or GarageLane.m_ParkingFee
        2. Create MoneyTransfer (household -> city)
        3. Create ServiceFeeSystem.FeeEvent(PlayerResource.Parking, fee)
        4. Passengers get out
        |
        v
    MovingToParked TRANSITION
        Remove: Moving, TransformFrame, InterpolatedTransform, Swaying,
                CarNavigation, CarNavigationLane, CarCurrentLane,
                PathOwner, Target, Blocker, PathElement
        Add:    ParkedCar(lane, curvePosition), Stopped, Updated
        |
        v
    PARKED
        ParkedCar.m_Lane = parking lane entity
        ParkedCar.m_CurvePosition = position on lane
        ParkingLaneDataSystem recalculates free space on next update
        |
        v
    DEPARTURE (when citizen needs to travel again)
        TripSource added -> VehicleSpawnSystem processes
        ParkedCar removed, movement components re-added
```

### Parking Fee Flow

```
DISTRICT LEVEL
    District entity with DistrictOption.PaidParking enabled
        |
        v
    DistrictModifier buffer: DistrictModifierType.ParkingFee
        |
        v
    ParkingLaneDataSystem.GetDistrictParkingFee()
        Reads modifier value, clamps to ushort (0-65535)
        Writes to ParkingLane.m_ParkingFee
        |
        v
    Pathfinder uses fee as cost (money channel)
    Vehicle pays fee on parking via MoneyTransfer

BUILDING LEVEL
    Building entity with BuildingOption.PaidParking enabled
        |
        v
    BuildingModifier buffer: BuildingModifierType.ParkingFee
        |
        v
    ParkingLaneDataSystem.GetBuildingParkingFee()
        Reads modifier value, clamps to ushort (0-65535)
        Writes to GarageLane.m_ParkingFee
        |
        v
    Same fee collection path as district level

TAXI LEVEL
    City entity with CityOption.PaidTaxiStart enabled
        |
        v
    CityModifier buffer: CityModifierType.TaxiStartingFee
        |
        v
    ParkingLaneDataSystem writes to ParkingLane.m_TaxiFee
```

## Prefab & Configuration

| Value | Source | Default | Location |
|-------|--------|---------|----------|
| Slot size | ParkingLaneData.m_SlotSize | Varies by road prefab | Game.Prefabs.ParkingLaneData |
| Slot angle | ParkingLaneData.m_SlotAngle | 0 (parallel) | Game.Prefabs.ParkingLaneData |
| Slot interval | ParkingLaneData.m_SlotInterval | Varies (0 = continuous) | Game.Prefabs.ParkingLaneData |
| Max car length | ParkingLaneData.m_MaxCarLength | Varies by road prefab | Game.Prefabs.ParkingLaneData |
| Garage marker capacity | ParkingFacilityData.m_GarageMarkerCapacity | Varies per building | Game.Prefabs.ParkingFacilityData |
| Comfort factor (facility) | ParkingFacilityData.m_ComfortFactor | 0.5 | Game.Prefabs.ParkingFacility managed |
| Default garage capacity (no facility) | Hardcoded fallback | 2 per marker | ParkingLaneDataSystem.GetParkingStats |
| Default comfort (no facility) | Hardcoded fallback | 0.8 | ParkingLaneDataSystem.GetParkingStats |
| Residential garage capacity | BuildingPropertyData.m_SpaceMultiplier | max(1, round) | ParkingLaneDataSystem fallback |
| Workplace garage capacity | WorkplaceData.m_MaxWorkers / 20 | max(2, workers/20) | ParkingLaneDataSystem fallback |
| ParkingFacilityAI update frame | UpdateFrameData | 12 | ParkingFacility.Initialize |
| Parking reroute scan distance | Hardcoded | 40000 path nodes | PersonalCarAISystem.CheckParkingSpace |
| Parking pathfind cost | PathfindCarData.ParkingCost | Prefab-defined | Game.Prefabs.PathfindCarData |

## Harmony Patch Points

### Candidate 1: `ParkingLaneDataSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Read or modify parking lane data after the system recalculates free space, fees, and comfort. Implement custom parking pricing, override comfort factors, or create parking availability overlays.
- **Risk level**: Low -- reactive system, only runs when lanes are updated
- **Side effects**: Modifying ParkingLane/GarageLane data affects pathfinder edge costs

### Candidate 2: `ParkingFacilityAISystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Override parking facility comfort or active state after the AI tick. Implement time-of-day pricing, dynamic capacity limits, or facility-specific policies.
- **Risk level**: Low -- runs on UpdateFrame cycle
- **Side effects**: Must add `PathfindUpdated` to affected lanes if modifying comfort

### Candidate 3: `PersonalCarAISystem.OnUpdate` (parking search)

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix or Transpiler on inner job
- **What it enables**: Override parking search behavior, change reroute scan distance, modify parking validation logic
- **Risk level**: High -- core vehicle AI, Burst-compiled job cannot be patched directly
- **Side effects**: Performance-critical; the system runs every frame for all personal cars

### Candidate 4: Direct ECS manipulation (no patch needed)

- **What it enables**: Read `ParkingLane.m_FreeSpace` to monitor availability. Write `ParkingLane.m_ParkingFee` to set custom fees. Read `GarageLane.m_VehicleCount/m_VehicleCapacity` for occupancy. Toggle `ParkingLaneFlags.ParkingDisabled` to disable street parking. Add `PathfindUpdated` after modifications to propagate changes.
- **Risk level**: Low for reads; Medium for writes (must add PathfindUpdated tag)
- **Side effects**: None for reads; writes require triggering pathfind recalculation

### Candidate 5: `PathUtils.GetParkingSpecification` or similar

- **What it enables**: Modify how parking costs are computed in the pathfind graph
- **Risk level**: Medium -- affects route selection for all vehicles seeking parking
- **Note**: The exact method name needs further tracing in PathUtils

## Mod Blueprint

### Systems to create

1. **ParkingMonitorSystem** (`GameSystemBase`) -- reads ParkingLane/GarageLane data to expose parking statistics to UI (total capacity, occupancy rates, revenue)
2. **CustomParkingPolicySystem** (`GameSystemBase`) -- overrides parking fees, comfort factors, or disabled flags based on custom rules (time of day, district, building type)
3. **ParkingOverlaySystem** (`GameSystemBase`) -- generates overlay data for a custom info view showing parking availability as a heatmap

### Components to add

- `CustomParkingPolicy` (IComponentData) -- stores mod-specific parking overrides per lane or building
- `ParkingMonitored` (IComponentData) -- tag for lanes being tracked by the monitoring system

### Patches needed

- **Postfix on `ParkingLaneDataSystem.OnUpdate`** to apply custom fee overrides after vanilla calculation
- **Or** no patches if using a custom system that runs after ParkingLaneDataSystem and directly modifies ParkingLane/GarageLane components

### Settings

- Global parking fee multiplier
- Per-district parking fee overrides
- Street parking enable/disable toggle
- Garage capacity multiplier
- Comfort factor override
- Parking search radius adjustment

### Mod Blueprint: Parking Behavior Modification

A parking behavior mod modifies how vehicles search for, choose, and interact with parking spaces. This is one of the most requested mod categories by the CS2 community. Based on analysis of the [RealisticParking](https://github.com/MasatoTakedai/RealisticParking) mod, which demonstrates three orthogonal parking features.

#### Systems to Create

1. **`NewPersonalCarAISystem`** (replaces `PersonalCarAISystem`) -- contains a modified `PersonalCarTickJob` that:
   - Adds `CarQueued`/`CarParked` tag components for demand tracking when vehicles park or fail to park
   - Implements configurable reroute distance via `GetRerouteLimit()` (vanilla hardcodes 40000 path nodes; realistic value is ~5)
2. **`NewParkingLaneDataSystem`** (replaces `ParkingLaneDataSystem`) -- contains a modified `UpdateLaneDataJob` that:
   - Calculates custom free space factoring in demand (`CalculateCustomFreeSpace`)
   - Applies custom garage capacity based on `BuildingPropertyData.CountProperties()` and `WorkProvider.m_MaxWorkers` instead of `ParkingFacilityData.m_GarageMarkerCapacity`
   - Writes `GarageCount` component with demand-adjusted counts
3. **`ParkingDemandSystem`** (new, `SystemUpdatePhase.Modification5`) -- processes demand tracking:
   - Reads `CarQueued`/`CarParked` tags from vehicles that just parked or failed
   - Updates `ParkingDemand` component (demand value + cooldown timer) on parking lane entities
   - Manages demand decay over time
   - Triggers `PathfindUpdated` on affected lanes to propagate changes
4. **`GarageLanesModifiedSystem`** (new, `SystemUpdatePhase.ModificationEnd`) -- updates pathfind graph:
   - Runs after demand system updates
   - Iterates garage lane entities with modified demand
   - Updates pathfind graph edges to reflect demand-adjusted garage availability
   - Applies demand to `PathSpecification` for pathfind cost calculation

#### Components to Create

| Component | Type | Purpose |
|-----------|------|---------|
| `ParkingDemand` | IComponentData | Per-lane demand value (`m_Demand`) + cooldown timer (`m_Cooldown`) |
| `GarageCount` | IComponentData | Demand-adjusted garage vehicle count for pathfind graph |
| `CarQueued` | Tag (zero-size) | Marks vehicles that failed to find parking (demand increase signal) |
| `CarParked` | Tag (zero-size) | Marks vehicles that successfully parked (demand tracking signal) |

#### Harmony Patches Needed

- **`LanesModifiedSystem.OnCreate`** -- postfix to exclude `GarageLane` from the vanilla `m_UpdatedLanesQuery`, preventing duplicate processing of garage lanes (since the custom `GarageLanesModifiedSystem` handles them instead)

#### Key Game Components

| Component | Namespace | Role |
|-----------|-----------|------|
| `PersonalCarAISystem` | Game.Simulation | Vanilla system to replace for reroute distance and demand tracking |
| `ParkingLaneDataSystem` | Game.Pathfind | Vanilla system to replace for custom capacity and free space |
| `LanesModifiedSystem` | Game.Pathfind | Patched to exclude garage lanes from vanilla processing |
| `ParkingLane` | Game.Net | Street parking state (free space, fees, comfort) |
| `GarageLane` | Game.Net | Garage parking state (vehicle count, capacity) |
| `ParkingFacilityData` | Game.Prefabs | `m_GarageMarkerCapacity` -- overridden by custom formula |
| `BuildingPropertyData` | Game.Prefabs | `CountProperties()` used in custom garage capacity formula |
| `WorkProvider` | Game.Companies | `m_MaxWorkers` used in workplace parking capacity formula |
| `PathfindQueueSystem` | Game.Pathfind | `Enqueue(UpdateAction)` for pathfind graph updates |
| `PathfindUpdated` | Game.Common | Tag to signal pathfind graph recalculation |
| `SearchSystem` | Game.Objects | Spatial queries for counting parked vehicles |
| `ServiceFeeSystem` | Game.Simulation | Parking fee collection via `FeeEvent` |
| `MoneyTransfer` | Game.Simulation | Household-to-city money transfer on parking |

#### Architecture

```
Mod.cs (IMod entry point)
  |
  +-- NewPersonalCarAISystem (replaces PersonalCarAISystem)
  |     +-- PersonalCarTickJob (copied + modified)
  |           - AddCarQueuedComponent/AddCarParkedComponent (demand tracking)
  |           - GetRerouteLimit (configurable reroute distance)
  |
  +-- NewParkingLaneDataSystem (replaces ParkingLaneDataSystem)
  |     +-- UpdateLaneDataJob (copied + modified)
  |           - CalculateCustomFreeSpace (demand-adjusted)
  |           - ApplyCustomGarageCapacity (parking minimums)
  |           - SetGarageCountComponent (demand tracking)
  |
  +-- ParkingDemandSystem (new, Modification5)
  |     +-- UpdateParkingDemandJob
  |           - Processes CarQueued/CarParked tags
  |           - Manages demand cooldown
  |           - Triggers PathfindUpdated
  |
  +-- GarageLanesModifiedSystem (new, ModificationEnd)
  |     +-- UpdateTollEdgeJob
  |           - Updates pathfind graph edges for garages
  |           - Applies demand to pathfind specification
  |
  +-- LanesModifiedSystem_Patch (Harmony postfix on OnCreate)
        - Excludes GarageLane from vanilla query
        - Prevents duplicate processing
```

#### Implementation Notes

- The `PersonalCarAISystem` replacement requires copying the entire `PersonalCarTickJob` because it is Burst-compiled and cannot be selectively patched
- The reroute distance reduction (from 40000 to ~5 path nodes) is the single highest-impact change for realistic parking search behavior
- Demand decay prevents permanent congestion artifacts when traffic patterns change
- The `GarageLanesModifiedSystem` must run at `ModificationEnd` to ensure demand data is finalized before pathfind graph updates

## Examples

### Example 1: Read Parking Availability on a Road

Query all parking lanes on a road segment and log their free space and fees.

```csharp
/// <summary>
/// Reads parking availability for all parking lanes on a road entity.
/// </summary>
public void CheckRoadParking(EntityManager em, Entity road)
{
    if (!em.HasBuffer<SubLane>(road)) return;

    DynamicBuffer<SubLane> subLanes = em.GetBuffer<SubLane>(road);
    int totalSlots = 0;
    int occupiedSlots = 0;

    for (int i = 0; i < subLanes.Length; i++)
    {
        Entity lane = subLanes[i].m_SubLane;
        if (em.HasComponent<ParkingLane>(lane))
        {
            ParkingLane parking = em.GetComponentData<ParkingLane>(lane);
            bool isDisabled = (parking.m_Flags
                & ParkingLaneFlags.ParkingDisabled) != 0;
            bool isVirtual = (parking.m_Flags
                & ParkingLaneFlags.VirtualLane) != 0;

            if (!isVirtual)
            {
                totalSlots++;
                if (parking.m_FreeSpace <= 0f)
                    occupiedSlots++;
            }

            float fee = (float)parking.m_ParkingFee;
            float comfort = (float)parking.m_ComfortFactor / 65535f;
            string side = (parking.m_Flags & ParkingLaneFlags.RightSide) != 0
                ? "right" : "left";

            Log.Info($"  Parking lane {lane}: side={side}, "
                + $"free={parking.m_FreeSpace:F1}, fee={fee}, "
                + $"comfort={comfort:F2}, "
                + $"disabled={isDisabled}");
        }
    }
    Log.Info($"Road {road}: {occupiedSlots}/{totalSlots} lanes occupied");
}
```

### Example 2: Disable Street Parking on a Road

Set the `ParkingDisabled` flag on all parking lanes of a road and trigger pathfind recalculation.

```csharp
/// <summary>
/// Disables street parking on all parking lanes of a road segment.
/// Adds PathfindUpdated so the pathfinder picks up the change.
/// </summary>
public void DisableStreetParking(EntityManager em, Entity road)
{
    if (!em.HasBuffer<SubLane>(road)) return;

    DynamicBuffer<SubLane> subLanes = em.GetBuffer<SubLane>(road);
    int modified = 0;

    for (int i = 0; i < subLanes.Length; i++)
    {
        Entity lane = subLanes[i].m_SubLane;
        if (em.HasComponent<ParkingLane>(lane))
        {
            ParkingLane parking = em.GetComponentData<ParkingLane>(lane);
            if ((parking.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
                continue;

            parking.m_Flags |= ParkingLaneFlags.ParkingDisabled;
            em.SetComponentData(lane, parking);

            // Signal pathfind graph to recalculate this lane's edge
            if (!em.HasComponent<PathfindUpdated>(lane))
                em.AddComponent<PathfindUpdated>(lane);

            modified++;
        }
    }
    Log.Info($"Disabled parking on {modified} lanes of road {road}");
}
```

### Example 3: Modify Parking Fees via District Modifiers

Read and adjust parking fees by modifying the district modifier buffer, then let `ParkingLaneDataSystem` propagate the change naturally.

```csharp
/// <summary>
/// Sets the parking fee for a district by writing to its modifier buffer.
/// The district must have PaidParking option enabled.
/// </summary>
public void SetDistrictParkingFee(EntityManager em, Entity district,
    float newFee)
{
    // Ensure the district has PaidParking enabled
    if (!em.HasComponent<District>(district)) return;

    District districtData = em.GetComponentData<District>(district);
    // DistrictOption.PaidParking must be set on the district

    if (!em.HasBuffer<DistrictModifier>(district)) return;

    DynamicBuffer<DistrictModifier> modifiers =
        em.GetBuffer<DistrictModifier>(district);

    // Find and update the ParkingFee modifier
    bool found = false;
    for (int i = 0; i < modifiers.Length; i++)
    {
        DistrictModifier mod = modifiers[i];
        if (mod.m_Type == DistrictModifierType.ParkingFee)
        {
            mod.m_Delta = newFee;
            modifiers[i] = mod;
            found = true;
            break;
        }
    }

    if (!found)
    {
        // Add new modifier if not present
        modifiers.Add(new DistrictModifier
        {
            m_Type = DistrictModifierType.ParkingFee,
            m_Delta = newFee
        });
    }

    Log.Info($"Set parking fee to {newFee} for district {district}");
}
```

### Example 4: Monitor Garage Occupancy

Query all buildings with parking facilities to calculate city-wide garage occupancy.

```csharp
/// <summary>
/// Calculates city-wide parking garage occupancy statistics.
/// </summary>
public partial class GarageOccupancySystem : GameSystemBase
{
    private EntityQuery _facilityQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _facilityQuery = GetEntityQuery(
            ComponentType.ReadOnly<Game.Buildings.ParkingFacility>(),
            ComponentType.ReadOnly<SubLane>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _facilityQuery.ToEntityArray(Allocator.Temp);
        int totalCapacity = 0;
        int totalOccupied = 0;
        int facilitiesActive = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            var facility = EntityManager
                .GetComponentData<Game.Buildings.ParkingFacility>(entities[i]);
            bool active = (facility.m_Flags
                & ParkingFacilityFlags.ParkingSpacesActive) != 0;

            if (!active) continue;
            facilitiesActive++;

            DynamicBuffer<SubLane> subLanes =
                EntityManager.GetBuffer<SubLane>(entities[i]);

            for (int j = 0; j < subLanes.Length; j++)
            {
                Entity lane = subLanes[j].m_SubLane;
                if (EntityManager.HasComponent<GarageLane>(lane))
                {
                    GarageLane garage =
                        EntityManager.GetComponentData<GarageLane>(lane);
                    totalCapacity += garage.m_VehicleCapacity;
                    totalOccupied += garage.m_VehicleCount;
                }
            }
        }

        if (facilitiesActive > 0)
        {
            float occupancy = (totalCapacity > 0)
                ? (float)totalOccupied / totalCapacity * 100f : 0f;
            Log.Info($"Garages: {facilitiesActive} active, "
                + $"{totalOccupied}/{totalCapacity} occupied "
                + $"({occupancy:F1}%)");
        }
        entities.Dispose();
    }
}
```

### Example 5: Adjust Parking Facility Comfort Factor

Modify the comfort factor on a parking facility's prefab data to make it more or less desirable for pathfinding.

```csharp
/// <summary>
/// Adjusts the comfort factor of a parking facility building.
/// Higher comfort makes vehicles prefer this facility during pathfinding.
/// </summary>
public void AdjustFacilityComfort(EntityManager em,
    Entity facilityBuilding, float newComfort)
{
    if (!em.HasComponent<Game.Buildings.ParkingFacility>(facilityBuilding))
        return;

    // Modify the runtime comfort factor on the building
    var facility = em
        .GetComponentData<Game.Buildings.ParkingFacility>(facilityBuilding);
    facility.m_ComfortFactor = math.saturate(newComfort);
    em.SetComponentData(facilityBuilding, facility);

    // Propagate to all child parking/garage lanes
    if (em.HasBuffer<SubLane>(facilityBuilding))
    {
        DynamicBuffer<SubLane> subLanes =
            em.GetBuffer<SubLane>(facilityBuilding);
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity lane = subLanes[i].m_SubLane;
            if (em.HasComponent<ParkingLane>(lane)
                || em.HasComponent<GarageLane>(lane))
            {
                if (!em.HasComponent<PathfindUpdated>(lane))
                    em.AddComponent<PathfindUpdated>(lane);
            }
        }
    }

    Log.Info($"Set comfort to {newComfort:F2} on facility "
        + $"{facilityBuilding}");
}
```

## Open Questions

- [ ] **Parking search radius**: The vanilla scan distance of 40000 path nodes in `CheckParkingSpace` seems very large. The RealisticParking mod reduces this to 5 nodes. The exact distance-to-game-units conversion for this value is not clear.
- [ ] **Bicycle parking details**: `BicycleParkingFacility` and `BicycleParking` route components exist but the bicycle parking search algorithm was not fully traced. It likely follows a similar pattern to car parking.
- [ ] **SpecialParking PathMethod**: The `PathMethod.SpecialParking` (0x1000) flag exists but its exact use case (which vehicle types, which scenarios) needs further tracing.
- [ ] **ParkingFacilityMode interaction**: The `ParkingFacilityMode` class can modify comfort factors per mode (e.g., different comfort for day/night). How this interacts with district policies at runtime is not fully documented.
- [ ] **Taxi availability calculation**: The `m_TaxiAvailability` field on `ParkingLane` and the `TaxiAvailabilityUpdated`/`TaxiAvailabilityChanged` flags suggest a taxi dispatch optimization system that was not fully traced.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key types decompiled: ParkingLane (Game.Net), ParkingLaneFlags, GarageLane, ParkingFacility (Game.Buildings), ParkingFacilityFlags, CarParkingFacility, BicycleParkingFacility, ParkedCar (Game.Vehicles), FixParkingLocation, ParkingLaneData (Game.Prefabs), ParkingFacilityData, ParkingSpaceData, ParkingFacility (Game.Prefabs managed), ParkingLane (Game.Prefabs managed), ParkingSpace (Game.Prefabs managed), ParkingFacilityMode, ParkingLaneDataSystem (Game.Pathfind), ParkingFacilityAISystem, PlayerResource, ParkingSpace/CarParking/BicycleParking (Game.Routes)
- All decompiled snippets saved in `snippets/` directory
- Cross-references: [TrafficFlowLaneControl](../TrafficFlowLaneControl/README.md), [VehicleSpawning](../VehicleSpawning/README.md), [Pathfinding](../Pathfinding/README.md)
