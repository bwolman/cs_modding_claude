# Research: Vehicle Spawning & Despawning

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How private cars appear in CS2, how they find routes, park, and get recycled. The vehicle lifecycle from creation through pathfinding, driving, parking, and deletion. Vehicle spawning from outside connections.

**Why**: To understand the vehicle lifecycle so modders can create custom vehicle behaviors, modify spawning rates, track vehicle counts, or build traffic management tools.

**Boundaries**: Public transit vehicles are covered in Public Transit research. Emergency vehicle dispatch is covered in Emergency Dispatch. Traffic accident mechanics are covered in Vehicle Accidents. This focuses on the personal car lifecycle and the generic vehicle spawn pipeline.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Vehicles | Car, PersonalCar, ParkedCar, OwnedVehicle, Vehicle, CarFlags, PersonalCarFlags, SizeClass, EnergyTypes, EvacuatingTransport |
| Game.dll | Game.Simulation | VehicleSpawnSystem, PersonalCarAISystem, PersonalCarOwnerSystem, CountVehicleDataSystem |
| Game.dll | Game.Objects | TripSource |
| Game.dll | Game.Prefabs | CarData, PersonalCarData, PersonalCarSelectData, VehicleData |
| Game.dll | Game.Net | GarageLane, ParkingLane |

## Key Concepts

### Vehicle Entity Lifecycle

A personal car in CS2 goes through these states:

1. **Owned (parked)**: Created when a household gets a car. Has `OwnedVehicle` buffer on household, `ParkedCar` on the vehicle, and `PersonalCar` component.
2. **Awaiting spawn**: Citizen needs transport, vehicle gets `TripSource` component. `VehicleSpawnSystem` manages the spawn queue.
3. **Spawned (driving)**: `TripSource` removed, vehicle is on the road. `PersonalCarAISystem` handles tick-by-tick driving, pathfinding, boarding/disembarking passengers.
4. **Parking**: Vehicle reaches destination, searches for parking lane or garage, transitions to `ParkedCar`.
5. **Deleted**: `PersonalCarOwnerSystem` checks ownership -- if the household no longer owns the vehicle, it's deleted via `VehicleUtils.DeleteVehicle`.

### TripSource Timer

The `TripSource` component has a `m_Timer` field that counts down (decremented by 16 each update). When it reaches 0 or below, `VehicleSpawnSystem` adds the vehicle to the spawn queue. This creates a delay before vehicles enter the road.

### Personal Car Flags

| Flag | Value | Meaning |
|------|-------|---------|
| Transporting | 1 | Carrying passengers |
| Boarding | 2 | Passengers are getting in |
| Disembarking | 4 | Passengers are getting out |
| DummyTraffic | 8 | Simulated traffic from outside connections |
| HomeTarget | 16 | Heading home |

### Car Flags

| Flag | Value | Meaning |
|------|-------|---------|
| Emergency | 1 | Emergency vehicle mode |
| StayOnRoad | 2 | Cannot leave road |
| AnyLaneTarget | 4 | Can target any lane |
| Warning | 8 | Warning lights active |
| Queueing | 16 | In traffic queue |
| UsePublicTransportLanes | 32 | Allowed in bus lanes |
| PreferPublicTransportLanes | 64 | Prefers bus lanes |
| Sign | 128 | Has sign display |
| Interior | 256 | In garage/interior |
| Working | 512 | Working vehicle mode |
| CannotReverse | 4096 | Cannot reverse |

## Component Map

### `PersonalCar` (Game.Vehicles)

Identifies a personal car and its keeper.

| Field | Type | Description |
|-------|------|-------------|
| m_Keeper | Entity | The citizen who is driving/responsible for the car |
| m_State | PersonalCarFlags | Current state flags |

*Source: `Game.dll` -> `Game.Vehicles.PersonalCar`*

### `Car` (Game.Vehicles)

Generic car component shared by all car types.

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | CarFlags | Behavioral flags |

*Source: `Game.dll` -> `Game.Vehicles.Car`*

### `ParkedCar` (Game.Vehicles)

Marks a vehicle as parked in a specific lane.

| Field | Type | Description |
|-------|------|-------------|
| m_Lane | Entity | The parking lane entity |
| m_CurvePosition | float | Position along the lane curve (0-1) |

*Source: `Game.dll` -> `Game.Vehicles.ParkedCar`*

### `OwnedVehicle` (Game.Vehicles)

Buffer element on a household listing its owned vehicles.

| Field | Type | Description |
|-------|------|-------------|
| m_Vehicle | Entity | The owned vehicle entity |

*Source: `Game.dll` -> `Game.Vehicles.OwnedVehicle`*

### `Vehicle` (Game.Vehicles)

Empty marker component. All vehicles have this.

*Source: `Game.dll` -> `Game.Vehicles.Vehicle`*

### `TripSource` (Game.Objects)

Added to vehicles waiting to spawn onto the road.

| Field | Type | Description |
|-------|------|-------------|
| m_Source | Entity | The spawn source (building, parking spot, outside connection) |
| m_Timer | int | Countdown timer; vehicle spawns when <= 0 |

*Source: `Game.dll` -> `Game.Objects.TripSource`*

## Prefab & Configuration

### `CarData` (Game.Prefabs)

Physics and classification data for a car prefab.

| Value | Type | Description |
|-------|------|-------------|
| m_SizeClass | SizeClass | Small, Medium, Large, or Undefined |
| m_EnergyType | EnergyTypes | Fuel, Electricity, FuelAndElectricity, or None |
| m_MaxSpeed | float | Maximum speed |
| m_Acceleration | float | Acceleration rate |
| m_Braking | float | Braking rate |
| m_PivotOffset | float | Pivot point offset |
| m_Turning | float2 | Turning parameters |

### `PersonalCarData` (Game.Prefabs)

Personal car-specific prefab data.

| Value | Type | Description |
|-------|------|-------------|
| m_PassengerCapacity | int | Maximum number of passengers |
| m_BaggageCapacity | int | Baggage/cargo capacity |
| m_CostToDrive | int | Economic cost per trip |
| m_Probability | int | Selection probability weight |

### Vehicle Size Classes

| Value | Name | Description |
|-------|------|-------------|
| 0 | Small | Compact cars, motorcycles |
| 1 | Medium | Standard sedans, SUVs |
| 2 | Large | Trucks, buses |
| 3 | Undefined | Unclassified |

### Energy Types

| Value | Name | Description |
|-------|------|-------------|
| 0 | None | No energy source |
| 1 | Fuel | Combustion engine |
| 2 | Electricity | Electric vehicle |
| 3 | FuelAndElectricity | Hybrid |

## System Map

### `VehicleSpawnSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Entities with `TripSource` + `Vehicle`, excluding `Deleted`/`Temp`
- **Key responsibilities**:
  1. `GroupSpawnSourcesJob`: Collects all vehicles with expired spawn timers (timer <= 0), groups them by spawn source, sorts by priority
  2. `TrySpawnVehiclesJob`: For each spawn group, checks if there's space at the spawn point (no blocking parked trains), removes `TripSource` to release the vehicle onto the road
- **Key behavior**: Decrements `TripSource.m_Timer` by 16 each update. When timer expires, vehicle enters spawn queue.

### `PersonalCarAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: Every frame (no explicit interval)
- **Queries**: Personal car entities
- **Key responsibilities**:
  1. Tick-by-tick driving simulation via `PersonalCarTickJob`
  2. Path following and lane navigation
  3. Passenger boarding and disembarking
  4. Parking search when arriving at destination
  5. Route recalculation when path fails
  6. Money transfers (toll fees, parking fees) via `MoneyTransfer` queue
- **Inner class**: `Actions` system processes deferred money transfers

### `PersonalCarOwnerSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 1024 frames
- **Queries**: PersonalCar entities, excluding `CarTrailer`/`Deleted`/`Temp`/`OutOfControl`/`Destroyed`
- **Key responsibilities**:
  1. Validates that each personal car is still listed in its household's `OwnedVehicle` buffer
  2. Validates that each bicycle still has a matching `BicycleOwner` on its keeper
  3. Deletes orphaned vehicles via `VehicleUtils.DeleteVehicle`
- **Deletion trigger**: If the household no longer has the vehicle in its `OwnedVehicle` buffer, the vehicle is deleted

### `CountVehicleDataSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Key responsibilities**:
  1. Counts vehicles by type for statistics
  2. Counts parked vehicles per parking facility
  3. Sends vehicle count triggers for achievements

## Data Flow

```
VEHICLE CREATION
  Household spawns (immigration or game start)
      |
      v
  OwnedVehicle buffer gets vehicle entity
  Vehicle created with PersonalCar + ParkedCar
  (parked at home building's garage/parking lane)
      |
      v
TRIP INITIATION
  Citizen needs to travel (work, shop, leisure)
      |
      v
  TripSource added to parked vehicle
  TripSource.m_Timer set (delay before spawn)
      |
      v
  VehicleSpawnSystem decrements timer each 16 frames
  When timer <= 0, vehicle enters spawn queue
      |
      v
  VehicleSpawnSystem removes TripSource
  Vehicle appears on road network
      |
      v
DRIVING
  PersonalCarAISystem ticks each frame:
    - Follows path through CarNavigationLane buffer
    - Handles lane changes, intersections, traffic
    - Boards/disembarks passengers at stops
    - Pays tolls/fees via MoneyTransfer queue
      |
      v
PARKING
  Vehicle reaches destination:
    - Searches for parking lane or garage
    - ParkedCar component added with lane + position
    - Passengers disembark
      |
      v
DELETION / RECYCLING
  PersonalCarOwnerSystem checks every 1024 frames:
    - Is vehicle still in household's OwnedVehicle buffer?
    - If not: VehicleUtils.DeleteVehicle
  Also deleted when:
    - Household moves away
    - Building demolished
    - Vehicle destroyed (disaster/accident)
```

## Harmony Patch Points

### Candidate 1: `Game.Simulation.VehicleSpawnSystem+GroupSpawnSourcesJob.Execute`

- **Signature**: `void Execute()`
- **Patch type**: Transpiler (Burst-compiled)
- **What it enables**: Modify spawn priority sorting, change timer decrement rate, filter which vehicles can spawn
- **Risk level**: High (core spawn pipeline)

### Candidate 2: `Game.Simulation.PersonalCarOwnerSystem+PersonalCarOwnerJob.Execute`

- **Signature**: `void Execute(in ArchetypeChunk chunk, ...)`
- **Patch type**: Transpiler (Burst-compiled)
- **What it enables**: Prevent vehicle deletion, modify ownership validation
- **Risk level**: Medium

### Candidate 3: `Game.Vehicles.VehicleUtils.DeleteVehicle`

- **Signature**: `static void DeleteVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, Entity vehicle, DynamicBuffer<LayoutElement> layout)`
- **Patch type**: Prefix (if not Burst-compiled) or Transpiler
- **What it enables**: Intercept vehicle deletion, log vehicle lifecycle events
- **Risk level**: Medium

## Mod Blueprint

- **Systems to create**:
  - `VehicleCounterSystem` -- track vehicle counts by type/state
  - `CustomSpawnRateSystem` -- modify spawn timing based on mod settings
- **Components to add**:
  - `TrackedVehicle` (IComponentData) -- marker for vehicles being monitored
- **Patches needed**:
  - `VehicleSpawnSystem` (Transpiler) -- modify spawn timer decrement rate
  - `VehicleUtils.DeleteVehicle` (Prefix) -- log deletions
- **Settings**:
  - `SpawnRateMultiplier` -- scale spawn timer speed
  - `MaxVehicles` -- cap total vehicle count
- **UI changes**:
  - Vehicle statistics panel showing counts by type and state

## Examples

### Example 1: Count Vehicles by State

Query all personal cars and categorize by their current state.

```csharp
using Game;
using Game.Vehicles;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;
using Colossal.Logging;

public class VehicleCounterSystem : GameSystemBase
{
    private static readonly ILog Log = LogManager.GetLogger(nameof(VehicleCounterSystem));

    private EntityQuery _parkedQuery;
    private EntityQuery _drivingQuery;
    private EntityQuery _awaitingSpawnQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _parkedQuery = GetEntityQuery(
            ComponentType.ReadOnly<PersonalCar>(),
            ComponentType.ReadOnly<ParkedCar>(),
            ComponentType.Exclude<Deleted>());
        _drivingQuery = GetEntityQuery(
            ComponentType.ReadOnly<PersonalCar>(),
            ComponentType.ReadOnly<Car>(),
            ComponentType.Exclude<ParkedCar>(),
            ComponentType.Exclude<TripSource>(),
            ComponentType.Exclude<Deleted>());
        _awaitingSpawnQuery = GetEntityQuery(
            ComponentType.ReadOnly<PersonalCar>(),
            ComponentType.ReadOnly<TripSource>(),
            ComponentType.Exclude<Deleted>());
    }

    protected override void OnUpdate()
    {
        int parked = _parkedQuery.CalculateEntityCount();
        int driving = _drivingQuery.CalculateEntityCount();
        int awaiting = _awaitingSpawnQuery.CalculateEntityCount();

        Log.Info($"Vehicles -- Parked: {parked}, Driving: {driving}, " +
                 $"Awaiting spawn: {awaiting}");
    }
}
```

### Example 2: Find a Household's Vehicles

Look up the OwnedVehicle buffer on a household to find its cars.

```csharp
using Game.Vehicles;
using Unity.Entities;
using System.Collections.Generic;

public static class HouseholdVehicleUtils
{
    public static List<Entity> GetHouseholdVehicles(
        EntityManager em, Entity household)
    {
        var vehicles = new List<Entity>();

        if (!em.HasBuffer<OwnedVehicle>(household))
            return vehicles;

        var buffer = em.GetBuffer<OwnedVehicle>(household);
        for (int i = 0; i < buffer.Length; i++)
        {
            vehicles.Add(buffer[i].m_Vehicle);
        }
        return vehicles;
    }

    public static bool IsVehicleParked(EntityManager em, Entity vehicle)
    {
        return em.HasComponent<ParkedCar>(vehicle);
    }

    public static bool IsVehicleDriving(EntityManager em, Entity vehicle)
    {
        return em.HasComponent<Car>(vehicle)
            && !em.HasComponent<ParkedCar>(vehicle)
            && !em.HasComponent<TripSource>(vehicle);
    }
}
```

### Example 3: Query Vehicles by Energy Type

Find all electric personal cars currently on the road.

```csharp
using Game;
using Game.Vehicles;
using Game.Prefabs;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;

public class ElectricVehicleCounter : GameSystemBase
{
    private EntityQuery _personalCarQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _personalCarQuery = GetEntityQuery(
            ComponentType.ReadOnly<PersonalCar>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<ParkedCar>(),
            ComponentType.Exclude<TripSource>(),
            ComponentType.Exclude<Deleted>());
    }

    protected override void OnUpdate()
    {
        var entities = _personalCarQuery.ToEntityArray(Allocator.Temp);
        var prefabRefs = _personalCarQuery.ToComponentDataArray<PrefabRef>(Allocator.Temp);

        int electricCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (EntityManager.TryGetComponentData<CarData>(
                prefabRefs[i].m_Prefab, out var carData))
            {
                if ((carData.m_EnergyType & EnergyTypes.Electricity) != 0)
                    electricCount++;
            }
        }

        entities.Dispose();
        prefabRefs.Dispose();
    }
}
```

### Example 4: Track Vehicle Spawn Events

Monitor when vehicles enter the road by watching for TripSource removal.

```csharp
using Game;
using Game.Vehicles;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;
using Colossal.Logging;

public class SpawnTrackerSystem : GameSystemBase
{
    private static readonly ILog Log = LogManager.GetLogger(nameof(SpawnTrackerSystem));

    private EntityQuery _waitingQuery;
    private int _previousWaitingCount;

    protected override void OnCreate()
    {
        base.OnCreate();
        _waitingQuery = GetEntityQuery(
            ComponentType.ReadOnly<PersonalCar>(),
            ComponentType.ReadOnly<TripSource>(),
            ComponentType.ReadOnly<Vehicle>(),
            ComponentType.Exclude<Deleted>());
    }

    protected override void OnUpdate()
    {
        int currentWaiting = _waitingQuery.CalculateEntityCount();
        int spawned = _previousWaitingCount - currentWaiting;

        if (spawned > 0)
        {
            Log.Info($"Vehicles spawned this tick: {spawned}");
        }

        _previousWaitingCount = currentWaiting;
    }
}
```

### Example 5: Inspect Parked Car Locations

Find where vehicles are parked by reading the ParkedCar lane reference.

```csharp
using Game;
using Game.Vehicles;
using Game.Net;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public class ParkingInspectorSystem : GameSystemBase
{
    private EntityQuery _parkedQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _parkedQuery = GetEntityQuery(
            ComponentType.ReadOnly<PersonalCar>(),
            ComponentType.ReadOnly<ParkedCar>(),
            ComponentType.ReadOnly<Game.Objects.Transform>(),
            ComponentType.Exclude<Deleted>());
    }

    protected override void OnUpdate()
    {
        var parkedCars = _parkedQuery.ToComponentDataArray<ParkedCar>(Allocator.Temp);
        var transforms = _parkedQuery.ToComponentDataArray<Game.Objects.Transform>(Allocator.Temp);

        for (int i = 0; i < parkedCars.Length; i++)
        {
            ParkedCar parked = parkedCars[i];
            float3 position = transforms[i].m_Position;

            bool isInGarage = EntityManager.HasComponent<GarageLane>(parked.m_Lane);
            bool isOnStreet = EntityManager.HasComponent<ParkingLane>(parked.m_Lane);
            // Use position and lane type for analysis
        }

        parkedCars.Dispose();
        transforms.Dispose();
    }
}
```

### Building Parking Detection (Prefab Inspection)

To detect whether a building prefab has parking, use a recursive prefab inspection pattern (from FindIt):

```csharp
private bool HasParking(PrefabBase prefab)
{
    // Method 1: Check SpawnLocation connection type
    if (prefab.TryGet<SpawnLocation>(out var spawnLocation)
        && spawnLocation.m_ConnectionType == RouteConnectionType.Parking)
        return true;

    // Method 2: Check ObjectSubLanes for ParkingLane prefabs
    if (prefab.TryGet<ObjectSubLanes>(out var subLanes) && subLanes.m_SubLanes is not null)
    {
        foreach (var lane in subLanes.m_SubLanes)
        {
            if (lane.m_LanePrefab.Has<ParkingLane>())
                return true;
        }
    }

    // Method 3: Recurse into sub-objects
    if (prefab.TryGet<ObjectSubObjects>(out var subObjects) && subObjects.m_SubObjects is not null)
    {
        foreach (var obj in subObjects.m_SubObjects)
        {
            if (obj.m_Object is not null && HasParking(obj.m_Object))
                return true;
        }
    }
    return false;
}
```

Three detection methods:
1. **`SpawnLocation`** prefab component — if `m_ConnectionType == RouteConnectionType.Parking`, the building has a direct parking spawn point
2. **`ObjectSubLanes`** — iterate `m_SubLanes` array, check if any lane prefab has a `ParkingLane` component
3. **`ObjectSubObjects`** — recurse into sub-objects (e.g., parking structures attached to buildings)

## Open Questions

- [ ] **Outside connection spawning**: Vehicles from outside connections use `PersonalCarFlags.DummyTraffic`. The exact mechanism for spawning traffic from outside connections (arrival rates, vehicle selection) was not fully traced.
- [x] **Parking search algorithm**: ~~Previously unclear.~~ Now understood from the RealisticParking mod and further decompilation. `PersonalCarAISystem` uses a two-phase parking search: (1) **Direct search** -- scans `ParkingLane` and `GarageLane` entities near the destination within a configurable radius (default ~100m game units), checking `ParkedCar` occupancy counts against lane capacity. (2) **Fallback search** -- if no spot is found, the vehicle re-pathfinds with `PathMethod.Parking` or `PathMethod.SpecialParking` to find the nearest available parking, expanding the search area. If all parking fails, the vehicle circles (path becomes obsolete and is recalculated). The RealisticParking mod hooks into this by patching the parking cost in `PathfindCarData.ParkingCost` and adjusting the search radius via `PathfindParameters.m_ParkingSize`, demonstrating that the parking system is fully moddable through cost tuning and parameter adjustment.
- [ ] **Vehicle model selection**: `PersonalCarSelectData` controls which car prefab is chosen for a household based on wealth and other factors. The selection algorithm was not fully decompiled.
- [ ] **ParkedVehiclesSystem**: This system handles spawning service vehicles (police, fire, garbage, etc.) from their depots. It was identified but not fully decompiled in this research scope.
- [ ] **Bicycle lifecycle**: Bicycles share the `PersonalCar` component but have a separate ownership model via `BicycleOwner`. The full bicycle lifecycle needs separate investigation.

## Sources

- Decompiled from: Game.dll -- Game.Vehicles.PersonalCar, Game.Vehicles.Car, Game.Vehicles.ParkedCar, Game.Vehicles.OwnedVehicle, Game.Vehicles.Vehicle, Game.Vehicles.CarFlags, Game.Vehicles.PersonalCarFlags, Game.Vehicles.SizeClass, Game.Vehicles.EnergyTypes
- Decompiled from: Game.dll -- Game.Simulation.VehicleSpawnSystem, Game.Simulation.PersonalCarAISystem, Game.Simulation.PersonalCarOwnerSystem
- Decompiled from: Game.dll -- Game.Objects.TripSource
- Decompiled from: Game.dll -- Game.Prefabs.CarData, Game.Prefabs.PersonalCarData
