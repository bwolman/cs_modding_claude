# Research: Garbage Collection System

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 accumulates garbage at buildings, dispatches garbage trucks, collects garbage along routes, and processes it at landfills and incinerators.

**Why**: To understand the full garbage lifecycle for mods that might adjust accumulation rates, modify collection behavior, tweak processing speed, or add custom garbage facility types.

**Boundaries**: This research covers the garbage accumulation, collection dispatch, truck AI, and facility processing systems. It does not cover the UI/infoview system or the detailed prefab initialization pipeline.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | GarbageAccumulationSystem, GarbageCollectorDispatchSystem, GarbageTruckAISystem, GarbageFacilityAISystem, GarbageTransferDispatchSystem, GarbageCollectionRequest, GarbageTransferRequest, GarbagePathfindSetup |
| Game.dll | Game.Buildings | GarbageProducer, GarbageFacility, GarbageProducerFlags, GarbageFacilityFlags |
| Game.dll | Game.Vehicles | GarbageTruck, GarbageTruckFlags |
| Game.dll | Game.Prefabs | GarbageParameterData, GarbageFacilityData, GarbageTruckData, GarbagePoweredData |

## Architecture Overview

The garbage system follows a four-stage pipeline:

1. **Accumulation** -- `GarbageAccumulationSystem` runs 16 times per game day. Each update, every building with a `GarbageProducer` component gains garbage based on its `ConsumptionData.m_GarbageAccumulation` rate, modified by citizen education levels, building level, district modifiers, and city modifiers.

2. **Request Creation** -- When a building's garbage exceeds `GarbageParameterData.m_RequestGarbageLimit`, the accumulation system creates a `GarbageCollectionRequest` entity. When garbage exceeds `m_WarningGarbageLimit`, a notification icon appears.

3. **Dispatch** -- `GarbageCollectorDispatchSystem` matches collection requests to available garbage trucks via pathfinding. It uses the standard service dispatch pattern: pathfind from a `GarbageCollector` source to the request target, then dispatch the vehicle.

4. **Collection & Processing** -- `GarbageTruckAISystem` drives trucks along routes, collecting garbage from buildings along sidewalks. When full (or disabled), trucks return to their facility. `GarbageFacilityAISystem` processes stored garbage at a rate determined by `GarbageFacilityData.m_ProcessingSpeed` and building efficiency.

## Key Components

### GarbageProducer (Game.Buildings)
Attached to every building that produces garbage.

| Field | Type | Description |
|-------|------|-------------|
| m_CollectionRequest | Entity | Reference to active collection request entity |
| m_Garbage | int | Current accumulated garbage amount |
| m_Flags | GarbageProducerFlags | Warning state flags |
| m_DispatchIndex | byte | Dispatch tracking index |

### GarbageCollectionRequest (Game.Simulation)
Created when a building needs garbage pickup.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | The building requesting collection |
| m_Priority | int | Priority (set to current garbage amount) |
| m_Flags | GarbageCollectionRequestFlags | IndustrialWaste flag for industrial zones |
| m_DispatchIndex | byte | Incremented on each dispatch attempt |

### GarbageFacility (Game.Buildings)
Runtime component on landfills and incinerators.

| Field | Type | Description |
|-------|------|-------------|
| m_GarbageDeliverRequest | Entity | Active transfer request (deliver to other facility) |
| m_GarbageReceiveRequest | Entity | Active transfer request (receive from other facility) |
| m_TargetRequest | Entity | Active reverse collection request |
| m_Flags | GarbageFacilityFlags | Availability flags (trucks, space, industrial, full) |
| m_AcceptGarbagePriority | float | Priority for accepting incoming garbage |
| m_DeliverGarbagePriority | float | Priority for sending garbage to other facilities |
| m_ProcessingRate | int | Current processing rate (calculated each tick) |

### GarbageTruck (Game.Vehicles)
Vehicle component for garbage collection trucks.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetRequest | Entity | Active reverse request from truck seeking work |
| m_State | GarbageTruckFlags | State flags (Returning, Unloading, Disabled, etc.) |
| m_RequestCount | int | Number of queued service dispatch requests |
| m_Garbage | int | Current garbage load |
| m_EstimatedGarbage | int | Estimated garbage from pre-planned route |
| m_PathElementTime | float | Average time per path element for route planning |

### GarbageParameterData (Game.Prefabs)
Global singleton with garbage system tuning parameters.

| Field | Type | Description |
|-------|------|-------------|
| m_CollectionGarbageLimit | int | Minimum garbage for a truck to collect from a building |
| m_RequestGarbageLimit | int | Garbage threshold to create a collection request |
| m_WarningGarbageLimit | int | Garbage threshold for the "piling up" warning icon |
| m_MaxGarbageAccumulation | int | Hard cap on garbage per building |
| m_BuildingLevelBalance | float | How much building level reduces garbage rate |
| m_EducationBalance | float | How much citizen education reduces garbage rate |
| m_HomelessGarbageProduce | int | Extra garbage per homeless citizen |

### GarbageFacilityData (Game.Prefabs)
Prefab data for garbage processing facilities.

| Field | Type | Description |
|-------|------|-------------|
| m_GarbageCapacity | int | Maximum garbage the facility can store |
| m_VehicleCapacity | int | Number of garbage trucks |
| m_TransportCapacity | int | Number of delivery trucks for inter-facility transfer |
| m_ProcessingSpeed | int | Base processing speed (units per 1024 ticks) |
| m_IndustrialWasteOnly | bool | Whether facility only handles industrial waste |
| m_LongTermStorage | bool | Whether facility is a landfill (shows "full" warning) |

## Garbage Accumulation Formula

The accumulation rate per building per update tick is:

```
baseRate = ConsumptionData.m_GarbageAccumulation
adjustedRate = max(0, baseRate - (buildingLevel * BuildingLevelBalance + avgEducation * EducationBalance)) * citizenCount
adjustedRate += HomelessGarbageProduce * homelessCount
```

Then district modifiers (`DistrictModifierType.GarbageProduction`) and city modifiers (`CityModifierType.IndustrialGarbage` for non-office industrial) are applied. The final value is divided by `kUpdatesPerDay` (16) and rounded randomly.

Garbage is capped at `m_MaxGarbageAccumulation`.

## Efficiency Penalty

When garbage exceeds the warning limit, building efficiency decreases linearly:

```
factor = (garbage - WarningGarbageLimit) / (MaxGarbageAccumulation - WarningGarbageLimit)
efficiency = 1.0 - GarbagePenalty * saturate(factor)
```

Where `GarbagePenalty` comes from `BuildingEfficiencyParameterData.m_GarbagePenalty`.

## Collection Dispatch Pattern

The dispatch system follows the standard CS2 service dispatch pattern:

1. A `GarbageCollectionRequest` entity is created with `ServiceRequest` and `RequestGroup(32)` components.
2. `GarbageCollectorDispatchSystem` processes requests via `ServiceRequest` cooldown ticking.
3. Pathfinding runs from `SetupTargetType.GarbageCollector` (source) to `SetupTargetType.CurrentLocation` (target building).
4. On successful pathfind, the dispatch system adds the request to the facility's `ServiceDispatch` buffer via `VehicleDispatch`.
5. `GarbageFacilityAISystem` spawns (or activates a parked) garbage truck with the dispatched path.

### Reverse Requests (Facility/Truck Seeking Work)

Both facilities and trucks can create **reverse** collection requests (with `ServiceRequestFlags.Reversed`). This allows idle trucks and under-utilized facilities to proactively seek out buildings needing collection, rather than waiting for buildings to request service.

## Truck Collection Behavior

The `GarbageTruckAISystem` is the most complex system. Key behaviors:

1. **Route-based collection**: Trucks collect garbage from all buildings connected to roads along their path, not just the target building. They find sidewalks adjacent to their driving lane and collect from all `ConnectedBuilding` entities along each road segment.

2. **Waypoint stops**: Path elements with `CarLaneFlags.Waypoint` indicate stops where the truck should slow down and collect. The truck checks `CheckGarbagePresence()` to verify buildings actually have garbage before stopping.

3. **Capacity management**: When `m_Garbage >= GarbageTruckData.m_GarbageCapacity` or the truck is disabled, it returns to the depot. The `EstimatedFull` flag prevents accepting new dispatches when the estimated load would exceed capacity.

4. **Industrial waste**: Trucks with `GarbageTruckFlags.IndustrialWasteOnly` only collect from buildings in industrial zones.

5. **Unloading**: At the facility, garbage is unloaded at `GarbageTruckData.m_UnloadRate` per tick. The garbage is added to the facility's `Resource.Garbage` resource buffer.

## Facility Processing

`GarbageFacilityAISystem` processes garbage at facilities:

1. **Processing rate**: `processingSpeed / 1024` units per tick, scaled by building efficiency and a garbage amount factor: `0.1 + garbageAmount * 1.8 / garbageCapacity`.

2. **Resource production**: Facilities can produce resources (e.g., electricity from incineration) via `ResourceProductionData` buffers on the prefab.

3. **Landfill storage areas**: Facilities with `SubArea` storage areas distribute garbage between internal storage and area storage, keeping reserves for processing and incoming loads.

4. **Inter-facility transfers**: Facilities create `GarbageTransferRequest` entities to send or receive garbage via delivery trucks. This allows garbage to flow from landfills to incinerators.

5. **Full notification**: Landfills (`m_LongTermStorage = true`) show a "facility full" notification when total garbage reaches capacity.

## Data Flow

```
Buildings with GarbageProducer
     |
     v
GarbageAccumulationSystem (16x/day)
     |-- Adds garbage per tick based on consumption, education, modifiers
     |-- Caps at m_MaxGarbageAccumulation
     |-- Creates GarbageCollectionRequest when garbage > m_RequestGarbageLimit
     |-- Adds warning icon when garbage > m_WarningGarbageLimit
     |-- Updates building efficiency factor
     |
     v
GarbageCollectionRequest entities (ServiceRequest + RequestGroup(32))
     |
     v
GarbageCollectorDispatchSystem (every 16 frames)
     |-- Validates requests (target still has enough garbage)
     |-- Pathfinds: GarbageCollector source -> target building
     |-- Dispatches to facility ServiceDispatch buffer
     |
     v
GarbageFacilityAISystem (every 256 frames)
     |-- Processes stored garbage (ResourceProduction)
     |-- Spawns/activates garbage trucks for dispatched requests
     |-- Creates GarbageTransferRequests for inter-facility transfer
     |-- Manages vehicle fleet (enable/disable based on capacity)
     |-- Shows "full" notification for landfills
     |
     v
GarbageTruckAISystem (every 16 frames)
     |-- Drives route, collecting from buildings along sidewalks
     |-- Collects when building garbage > m_CollectionGarbageLimit
     |-- Returns to depot when full or disabled
     |-- Unloads garbage at facility (adds to Resource.Garbage)
     |-- Requests new targets when idle
     |
     v
Facility receives garbage -> processes -> produces resources
```

## Modding Implications

### Adjusting Garbage Rates
- Modify `ConsumptionData.m_GarbageAccumulation` on building prefabs to change per-building rates
- Use district modifiers (`DistrictModifierType.GarbageProduction`) for area-wide adjustments
- Use city modifiers (`CityModifierType.IndustrialGarbage`) for industrial-specific adjustments
- Harmony-patch `GarbageAccumulationSystem.GetGarbageAccumulation()` for custom formulas

### Custom Collection Behavior
- The truck AI collects from all buildings along its route, not just targeted ones
- Collection threshold is `GarbageParameterData.m_CollectionGarbageLimit`
- Industrial waste separation is flag-based (`GarbageCollectionRequestFlags.IndustrialWaste`)

### Facility Customization
- `GarbageFacilityData` is combinable via `ICombineData` -- upgrades add to capacity
- `m_LongTermStorage` distinguishes landfills (storage) from incinerators (processing)
- Processing speed scales with both efficiency and current fill level

### Key Thresholds (from GarbageParameterData)
- `m_CollectionGarbageLimit`: Minimum garbage for truck to bother collecting
- `m_RequestGarbageLimit`: When building creates a service request
- `m_WarningGarbageLimit`: When warning icon appears and efficiency starts dropping
- `m_MaxGarbageAccumulation`: Hard cap, garbage stops accumulating

## Open Questions

- Exact default values for `GarbageParameterData` fields are set in prefab data (not hardcoded in the system) and would need to be read at runtime or extracted from game prefab assets.
- The `GarbageTransferDispatchSystem` handles inter-facility garbage transfers but was not fully traced in this research.
- The `GarbagePoweredData` prefab component suggests some facilities can be powered by garbage (waste-to-energy), but the exact integration was not fully traced.

## Decompiled Sources

All snippets saved to `snippets/` directory:

- `GarbageProducer.cs` -- Building garbage component
- `GarbageCollectionRequest.cs` -- Service request component
- `GarbageFacility.cs` -- Facility runtime component
- `GarbageTruck.cs` -- Vehicle component
- `GarbageParameterData.cs` -- Global tuning parameters
- `GarbageFacilityData.cs` -- Facility prefab data

Full decompiled systems referenced:
- `Game.Simulation.GarbageAccumulationSystem` -- 683 lines
- `Game.Simulation.GarbageCollectorDispatchSystem` -- 583 lines
- `Game.Simulation.GarbageTruckAISystem` -- 1500 lines
- `Game.Simulation.GarbageFacilityAISystem` -- 1289 lines
