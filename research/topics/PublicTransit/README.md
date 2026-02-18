# Research: Public Transit Systems

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2 manages public transit lines -- line creation, stop management, vehicle dispatch, passenger boarding, and the ECS entity structure that connects routes, waypoints, segments, and vehicles.

**Why**: To understand the data model and system pipeline for public transit, enabling mods that modify line behavior, vehicle scheduling, boarding logic, or create custom transit types.

**Boundaries**: This research covers the Route/TransportLine ECS architecture, vehicle dispatch, and boarding. It does not cover pathfinding internals, rendering, or the UI layer in depth. Cargo transport is included where it shares infrastructure with passenger transport.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Routes | Route, TransportLine, TransportStop, Waypoint, Segment, RouteWaypoint, RouteSegment, RouteVehicle, Connected, CurrentRoute, VehicleTiming, BoardingVehicle, WaitingPassengers, TaxiStand, RouteInfo, RouteNumber, Color, VehicleModel, DispatchedRequest, RouteModifier, RouteModifierType, RouteOption, RouteFlags, StopFlags, TransportLineFlags, RouteUtils |
| Game.dll | Game.Simulation | TransportLineSystem, TransportStopSystem, TransportVehicleDispatchSystem, TransportBoardingHelpers, TransportDepotAISystem, TransportStationAISystem, TransportCarAISystem, TransportTrainAISystem, TransportAircraftAISystem, TransportWatercraftAISystem, TransportUsageTrackSystem |
| Game.dll | Game.Policies | RouteModifierInitializeSystem |
| Game.dll | Game.Vehicles | PublicTransport, PublicTransportFlags, CargoTransport, CargoTransportFlags, PassengerTransport, EvacuatingTransport, PrisonerTransport |
| Game.dll | Game.Prefabs | TransportLinePrefab, TransportLineData, TransportType, TransportStop (prefab), TransportStation, TransportDepot, TransportStopMarker, TransportPathfind, PublicTransport (prefab), CargoTransport (prefab), PublicTransportVehicleData, PublicTransportPurpose, RouteConnectionType, RouteModifierData, RouteOptionData, UITransportConfigurationPrefab, UITransportConfigurationData |
| Game.dll | Game.Buildings | TransportStation (runtime), TransportStationFlags, TransportDepotFlags, CargoTransportStation |
| Game.dll | Game.UI.InGame | VehicleCountSection |
| Game.dll | Game.Tools | RouteToolSystem, GenerateRoutesSystem, ApplyRoutesSystem |

## Architecture Overview

Public transit in CS2 is built on a **Route entity hierarchy**. A transport line is an ECS entity with `Route` + `TransportLine` components, linked to child **Waypoint** and **Segment** entities via `RouteWaypoint` and `RouteSegment` buffer elements.

### Entity Hierarchy

```
Route Entity (the "line")
  Components: Route, TransportLine, RouteNumber, Color, PrefabRef
  Buffers: RouteWaypoint[], RouteSegment[], RouteVehicle[], VehicleModel[],
           DispatchedRequest[], RouteModifier[], Policy[]
  |
  +-- Waypoint Entities (one per stop/node on the line)
  |     Components: Waypoint (index), Connected (-> TransportStop),
  |                 VehicleTiming, BoardingVehicle, WaitingPassengers,
  |                 AccessLane, RouteLane
  |
  +-- Segment Entities (one per segment between waypoints)
  |     Components: Segment (index), RouteInfo, PathInformation
  |     Buffers: PathElement[], PathTargets[]
  |
  +-- Vehicle Entities (assigned to line via RouteVehicle buffer)
        Components: PublicTransport (or CargoTransport), CurrentRoute,
                    PrefabRef, Odometer, etc.
```

### Route Options

Routes support scheduling and configuration via `RouteOption` flags stored in `Route.m_OptionMask`:

| Option | Effect |
|--------|--------|
| Day | Line active during daytime only (normalizedTime >= 0.25 and < 11/12) |
| Night | Line active during nighttime only |
| Inactive | Line completely disabled |
| PaidTicket | Passengers pay a ticket price (set via RouteModifier) |

### Transport Types

The `TransportType` enum defines all transit modes:

| Value | Type | Notes |
|-------|------|-------|
| 0 | Bus | Road-based, uses RouteConnectionType.Road |
| 1 | Train | Track-based |
| 2 | Taxi | Special dispatching |
| 3 | Tram | Road track hybrid |
| 4 | Ship | Watercraft |
| 5 | Post | Mail delivery |
| 6 | Helicopter | Air-based |
| 7 | Airplane | Air-based |
| 8 | Subway | Underground track |
| 9 | Rocket | Special |
| 10 | Work | Worker routes |
| 11 | Ferry | Water-based |
| 12 | Bicycle | Bike routes |
| 13 | Car | Private car routes |

## Component Map

### `Route` (Game.Routes)

The base component for any route entity.

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | RouteFlags | Flags: Complete = 1 |
| m_OptionMask | uint | Bitmask of RouteOption values (Day, Night, Inactive, PaidTicket) |

### `TransportLine` (Game.Routes)

Added to Route entities that represent transport lines.

| Field | Type | Description |
|-------|------|-------------|
| m_VehicleRequest | Entity | Current pending vehicle request entity |
| m_VehicleInterval | float | Calculated interval between vehicles (seconds) |
| m_UnbunchingFactor | float | Factor controlling vehicle spacing smoothing |
| m_Flags | TransportLineFlags | RequireVehicles, NotEnoughVehicles |
| m_TicketPrice | ushort | Current ticket price (if PaidTicket option enabled) |

### `TransportStop` (Game.Routes)

Attached to stop entities (connected to waypoints via `Connected`).

| Field | Type | Description |
|-------|------|-------------|
| m_AccessRestriction | Entity | Access restriction entity |
| m_ComfortFactor | float | Stop comfort (0-1), affects passenger satisfaction |
| m_LoadingFactor | float | Loading speed multiplier (>= 0) |
| m_Flags | StopFlags | Active = 1, AllowEnter = 2 |

### `PublicTransport` (Game.Vehicles)

Runtime state for a public transport vehicle.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetRequest | Entity | Current dispatch request being handled |
| m_State | PublicTransportFlags | State flags (see below) |
| m_DepartureFrame | uint | Simulation frame when vehicle should depart |
| m_RequestCount | int | Number of pending requests |
| m_PathElementTime | float | Time along current path element |
| m_MaxBoardingDistance | float | Max distance from stop at which boarding occurred |
| m_MinWaitingDistance | float | Min distance to next waiting passenger |

### `VehicleTiming` (Game.Routes)

Attached to waypoint entities that have a connected stop.

| Field | Type | Description |
|-------|------|-------------|
| m_LastDepartureFrame | uint | Frame when last vehicle departed |
| m_AverageTravelTime | float | Smoothed average travel time to this stop |

### `BoardingVehicle` (Game.Routes)

Tracks which vehicle is currently boarding at a stop.

| Field | Type | Description |
|-------|------|-------------|
| m_Vehicle | Entity | Vehicle currently boarding (or Entity.Null) |
| m_Testing | Entity | Vehicle currently testing the stop |

### `WaitingPassengers` (Game.Routes)

Attached to waypoint entities at transit stops. Tracks passenger accumulation and average wait time.

| Field | Type | Description |
|-------|------|-------------|
| m_AverageWaitingTime | ushort | Average waiting time at this stop in seconds |
| m_Count | int | Number of passengers currently waiting at this stop |

*Source: `Game.dll` -> `Game.Routes.WaitingPassengers`*

**Usage**: Use `m_Count` to measure stop utilization and calculate crowding signals. A high `m_Count` relative to vehicle capacity indicates the stop is underserved. Combine with `m_AverageWaitingTime` to detect chronic overcrowding vs. transient peaks.

### `TaxiStand` (Game.Routes)

Attached to taxi stand entities. Defines the base fare for taxis dispatched from this stand.

| Field | Type | Description |
|-------|------|-------------|
| m_StartingFee | ushort | Base fare charged when a taxi departs from this stand |

*Source: `Game.dll` -> `Game.Routes.TaxiStand`*

**Usage**: Modify `m_StartingFee` to implement dynamic fare pricing. Cache the baseline fee before applying adjustments, then scale based on `WaitingPassengers.m_Count` at the connected stop to implement surge pricing.

### `RouteInfo` (Game.Routes)

Per-segment travel information.

| Field | Type | Description |
|-------|------|-------------|
| m_Duration | float | Travel duration for this segment |
| m_Distance | float | Travel distance for this segment |
| m_Flags | RouteInfoFlags | InactiveDay, InactiveNight |

### PublicTransportFlags

| Flag | Value | Description |
|------|-------|-------------|
| Returning | 0x1 | Vehicle returning to depot |
| EnRoute | 0x2 | Vehicle traveling along route |
| Boarding | 0x4 | Vehicle at stop, loading passengers |
| Arriving | 0x8 | Vehicle approaching a stop |
| Launched | 0x10 | Vehicle has been launched (aircraft) |
| Evacuating | 0x20 | Emergency evacuation mode |
| PrisonerTransport | 0x40 | Transporting prisoners |
| RequiresMaintenance | 0x80 | Vehicle needs maintenance |
| Refueling | 0x100 | Vehicle refueling at stop |
| AbandonRoute | 0x200 | Vehicle flagged to leave route |
| RouteSource | 0x400 | Vehicle is at route source |
| Testing | 0x800 | Vehicle testing a stop |
| RequireStop | 0x1000 | Vehicle requires a stop |
| DummyTraffic | 0x2000 | Non-real traffic vehicle |
| StopLeft | 0x4000 | Stop is on left side |
| StopRight | 0x8000 | Stop is on right side |
| Disabled | 0x10000 | Vehicle disabled |
| Full | 0x20000 | Vehicle at capacity |

### `PublicTransportVehicleData` (Game.Prefabs)

Prefab component on vehicle prefabs defining transport type and capacity.

| Field | Type | Description |
|-------|------|-------------|
| m_TransportType | TransportType | The transport mode this vehicle serves (Bus, Train, etc.) |
| m_PassengerCapacity | int | Maximum number of passengers this vehicle can carry |

*Source: `Game.dll` -> `Game.Prefabs.PublicTransportVehicleData`*

**Vehicle resolution chain**: To get vehicle capacity from a route, follow: `RouteVehicle` buffer -> vehicle entity -> `PrefabRef` -> `PublicTransportVehicleData`.

```csharp
DynamicBuffer<RouteVehicle> vehicles = em.GetBuffer<RouteVehicle>(routeEntity);
for (int i = 0; i < vehicles.Length; i++)
{
    Entity vehicleEntity = vehicles[i].m_Vehicle;
    PrefabRef prefabRef = em.GetComponentData<PrefabRef>(vehicleEntity);
    PublicTransportVehicleData vtd = em.GetComponentData<PublicTransportVehicleData>(prefabRef.m_Prefab);
    Log.Info($"Vehicle {vehicleEntity}: type={vtd.m_TransportType}, capacity={vtd.m_PassengerCapacity}");
}
```

### `TransportLineData` (Game.Prefabs)

Prefab component on transport line prefabs defining default scheduling and transport configuration.

| Field | Type | Description |
|-------|------|-------------|
| m_DefaultVehicleInterval | float | Default interval between vehicle dispatches (seconds) |
| m_TransportType | TransportType | Transport mode for this line type |
| m_StopDuration | float | Default stop dwell time (seconds) |
| m_DefaultUnbunchingFactor | float | Default unbunching factor for spacing vehicles |
| m_PassengerTransport | bool | True if this line type carries passengers |
| m_CargoTransport | bool | True if this line type carries cargo |

*Source: `Game.dll` -> `Game.Prefabs.TransportLineData`*

### `UITransportConfigurationPrefab` (Game.Prefabs)

Singleton prefab component that holds entity references to transport UI configuration data, including the ticket price policy.

| Field | Type | Description |
|-------|------|-------------|
| m_TicketPricePolicy | Entity | Reference to the ticket price policy prefab entity |

*Source: `Game.dll` -> `Game.Prefabs.UITransportConfigurationPrefab`*

### `UITransportConfigurationData` (Game.Prefabs)

Singleton prefab component providing runtime transport configuration data for the UI.

*Source: `Game.dll` -> `Game.Prefabs.UITransportConfigurationData`*

**Singleton access pattern**: Use `PrefabSystem.GetSingletonPrefab<T>(EntityQuery)` to retrieve the singleton prefab entity, then read its components:

```csharp
EntityQuery configQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
Entity configEntity = _prefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(configQuery);
UITransportConfigurationPrefab config = em.GetComponentData<UITransportConfigurationPrefab>(configEntity);
Entity ticketPricePolicy = config.m_TicketPricePolicy;
```

### `RouteModifier` (Game.Routes) [Buffer]

Per-route modifier buffer, analogous to `DistrictModifier` for districts. Indexed by `RouteModifierType` enum value.

| Field | Type | Description |
|-------|------|-------------|
| m_Delta | float2 | x = absolute delta, y = relative delta. Index in buffer corresponds to RouteModifierType enum value |

*Source: `Game.dll` -> `Game.Routes.RouteModifier`*

### `RouteModifierData` (Game.Prefabs) [Buffer]

Prefab buffer defining which simulation values a route policy modifies and how.

| Field | Type | Description |
|-------|------|-------------|
| m_Type | RouteModifierType | Which route simulation value to modify |
| m_Mode | ModifierValueMode | Relative, Absolute, or InverseRelative |
| m_Range | Bounds1 | Min/max effect range (interpolated by slider position) |

*Source: `Game.dll` -> `Game.Prefabs.RouteModifierData`*

### `RouteOptionData` (Game.Prefabs)

Prefab component defining which route options a policy activates.

| Field | Type | Description |
|-------|------|-------------|
| m_OptionMask | uint | Bitmask of RouteOption values this policy sets |

*Source: `Game.dll` -> `Game.Prefabs.RouteOptionData`*

## System Map

### `TransportLineSystem` (Game.Simulation)

The core system managing transport lines. Runs every 256 frames.

- **Query**: Route + TransportLine + RouteWaypoint + PrefabRef, excluding Temp/Deleted
- **Key behavior per line**:
  1. Apply route modifiers (VehicleInterval, TicketPrice)
  2. Determine if line is active (Day/Night/Inactive options, building activity check)
  3. Refresh segment durations from PathInformation and VehicleTiming
  4. Calculate target vehicle count: `round(lineDuration / vehicleInterval)`, minimum 1
  5. Calculate actual vehicle interval: `lineDuration / vehicleCount`
  6. Check current vehicles -- abandon those with wrong model, cancel abandon if more needed
  7. If not enough vehicles, create `TransportVehicleRequest` entities
  8. Show notification if vehicles cannot be dispatched
  9. Track max transport speed (passenger and cargo separately)

### `TransportStopSystem` (Game.Simulation)

Updates stop properties every 256 frames.

- **Query**: TransportStop entities, excluding Temp/Deleted
- **Key behavior per stop**:
  1. Read TransportStopData from prefab (base comfort/loading factors)
  2. If stop belongs to a TransportStation building, apply station bonuses
  3. Update comfort factor: `saturate(base + (1-base) * stationComfort)`
  4. Update loading factor: `max(0, base * stationLoading)`
  5. Check station active status (TransportStationFlags.TransportStopsActive)
  6. Trigger PathfindUpdated on connected routes if values changed

### `TransportVehicleDispatchSystem` (Game.Simulation)

Dispatches vehicles from depots to transport lines.

- **Processes** `TransportVehicleRequest` entities
- **Flow**:
  1. Request created by TransportLineSystem when line needs more vehicles
  2. System finds a suitable depot (TransportDepot with available vehicles)
  3. Pathfinds from depot to first stop on the line
  4. On success, dispatches vehicle from depot
  5. Vehicle gets CurrentRoute component pointing to the line
  6. Vehicle added to line's RouteVehicle buffer

### `TransportBoardingHelpers` (Game.Simulation)

Static helper managing the boarding process at stops.

- **BeginBoarding**: Sets BoardingVehicle on stop, calculates departure frame, starts cargo loading/unloading
- **EndBoarding**: Clears BoardingVehicle, clears Boarding flag on vehicle, finishes cargo operations
- **BeginTesting/EndTesting**: Vehicle checks if a stop is reachable without committing to board
- **Departure frame** calculated from: vehicle interval, unbunching factor, stop duration, last departure frame

### Vehicle AI Systems

Per-vehicle-type AI systems handle movement along routes:

| System | Vehicle Type |
|--------|-------------|
| TransportCarAISystem | Bus, Taxi, Car |
| TransportTrainAISystem | Train, Tram, Subway |
| TransportAircraftAISystem | Airplane, Helicopter |
| TransportWatercraftAISystem | Ship, Ferry |

These systems handle:
- Following PathElement sequences along the route
- Approaching and stopping at waypoints
- Triggering boarding via TransportBoardingHelpers
- Departing when departure frame is reached
- Returning to depot when AbandonRoute is set

### `RouteModifierInitializeSystem` (Game.Policies)

The route-level equivalent of `DistrictModifierInitializeSystem`. Initializes route modifiers when route entities are created.

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**:
  - Created Route entities (with Policy buffer, excluding Temp)
- **Reads**: Policy buffer, PolicySliderData, RouteOptionData, RouteModifierData
- **Writes**: Route.m_OptionMask, RouteModifier buffer
- **Key methods**:
  - `RefreshRouteOptions()` -- Iterates active policies, OR's each policy's RouteOptionData.m_OptionMask into Route.m_OptionMask
  - `RefreshRouteModifiers()` -- Clears modifier buffer, iterates active policies, interpolates slider value to modifier delta, calls AddModifier

**Inner struct `RouteModifierRefreshData`**:

| Field | Type | Description |
|-------|------|-------------|
| m_PolicySliderData | ComponentLookup\<PolicySliderData\> | Lookup for policy slider configuration |
| m_RouteOptionData | ComponentLookup\<RouteOptionData\> | Lookup for route option masks |
| m_RouteModifierData | BufferLookup\<RouteModifierData\> | Lookup for route modifier definitions |

### `VehicleCountSection` (Game.UI.InGame)

UI system that displays and manages the vehicle count control for transit lines.

- **Base class**: UISystemBase
- **Update phase**: UI
- **Key methods**:
  - `Visible(Entity)` -- Returns true if the entity is a transit line that supports vehicle count adjustment
  - `OnUpdate()` -- Refreshes the UI state for the currently selected transit line
  - `OnWriteProperties(IJsonWriter)` -- Serializes vehicle count data for the UI panel
  - `OnSetVehicleCount(Entity, int)` -- Callback from UI when player adjusts vehicle count; modifies `TransportLine.m_VehicleInterval` accordingly
- **Internal fields**: Tracks the currently displayed route entity, current vehicle count, and min/max vehicle count bounds

### `TransportLineSystem.CalculateVehicleCount` (Game.Simulation)

Public static method for computing target vehicle count from line parameters.

- **Signature**: `static int CalculateVehicleCount(float vehicleInterval, float lineDuration)`
- **Behavior**: Returns `max(1, round(lineDuration / vehicleInterval))`
- **Usage**: Call directly to predict vehicle count for a given interval and line duration without running the full system update

### `RouteUtils.ApplyModifier` (Game.Routes)

Public static method for applying a route modifier to a float value.

- **Signature**: `static void ApplyModifier(ref float value, DynamicBuffer<RouteModifier> modifiers, RouteModifierType type)`
- **Behavior**: Reads the `RouteModifier` at the index matching `type`, applies absolute delta (`value += m_Delta.x`) then relative delta (`value *= (1 + m_Delta.y)`)
- **Usage**: Call to apply route-level policy modifiers to any simulation value (e.g. vehicle interval, ticket price)

## Data Flow

### Line Update Cycle (every 256 frames)

```
TransportLineSystem.OnUpdate()
    |
    +-- TransportLineTickJob (per line, parallel)
    |   |
    |   +-- Apply RouteModifiers (VehicleInterval, TicketPrice)
    |   +-- Check Day/Night/Inactive options
    |   +-- Check if any active buildings on line
    |   +-- RefreshLineSegments:
    |   |     For each segment: read PathInformation.m_Duration
    |   |     For each stop: read VehicleTiming.m_AverageTravelTime
    |   |     Compute total lineDuration
    |   +-- CalculateVehicleCount(vehicleInterval, lineDuration)
    |   +-- CheckVehicles: count total/continuing, remove stale
    |   +-- AbandonVehicles or CancelAbandon to match target count
    |   +-- RequestNewVehicleIfNeeded -> creates TransportVehicleRequest
    |
    +-- VehicleActionJob (sequential)
        +-- Process abandon/cancel-abandon actions on vehicles
```

### Vehicle Dispatch Flow

```
TransportVehicleRequest (entity)
    |
    v
TransportVehicleDispatchSystem
    |-- Find depot with available vehicles
    |-- Pathfind depot -> first line stop
    |-- DispatchVehicle: assign vehicle to line
    |
    v
Vehicle Entity
    Components: PublicTransport (State = EnRoute), CurrentRoute (-> line)
    Added to: line's RouteVehicle buffer
```

### Boarding Flow

```
Vehicle arrives at waypoint
    |
    v
Vehicle AI System (e.g. TransportCarAISystem)
    |-- BeginTesting(vehicle, route, stop, waypoint)
    |   Check if stop is reachable
    |-- EndTesting
    |
    |-- BeginBoarding(vehicle, route, stop, waypoint, currentStation, nextStation, refuel)
    |   |-- Set BoardingVehicle.m_Vehicle = vehicle on stop
    |   |-- Calculate departure frame (interval + unbunching)
    |   |-- Set PublicTransportFlags.Boarding
    |   |-- Unload cargo at current station
    |   |-- Start loading cargo for next station
    |
    |   [Passengers board/alight during boarding window]
    |   [Departure frame reached]
    |
    |-- EndBoarding(vehicle, route, stop, waypoint, ...)
        |-- Clear BoardingVehicle.m_Vehicle
        |-- Clear Boarding flag
        |-- Final cargo loading
        |-- Vehicle continues to next waypoint
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Transport type | TransportLinePrefab.m_TransportType | Game.Prefabs.TransportLinePrefab |
| Default vehicle interval | TransportLinePrefab.m_DefaultVehicleInterval | Default: 15 seconds |
| Default unbunching factor | TransportLinePrefab.m_DefaultUnbunchingFactor | Default: 0.75 |
| Stop duration | TransportLinePrefab.m_StopDuration | Default: 1 second |
| Connection type (access) | TransportLinePrefab.m_AccessConnectionType | Default: Pedestrian |
| Connection type (route) | TransportLinePrefab.m_RouteConnectionType | Default: Road |
| Is passenger transport | TransportLinePrefab.m_PassengerTransport | Default: true |
| Is cargo transport | TransportLinePrefab.m_CargoTransport | Default: false |
| Stop comfort factor | TransportStopData.m_ComfortFactor | Per-stop prefab |
| Stop loading factor | TransportStopData.m_LoadingFactor | Per-stop prefab |
| Station comfort bonus | TransportStation.m_ComfortFactor | Per-building runtime |
| Station loading bonus | TransportStation.m_LoadingFactor | Per-building runtime |

## Harmony Patch Points

### Candidate 1: `TransportLineSystem.OnUpdate`

- **Patch type**: Prefix or Postfix
- **What it enables**: Modify line behavior before/after the tick job runs
- **Risk level**: Medium -- job is already scheduled in the method
- **Side effects**: Must complete jobs before modifying data

### Candidate 2: `TransportBoardingHelpers.TransportBoardingJob.Execute`

- **Patch type**: Not patchable (BurstCompile job struct)
- **Note**: Burst-compiled jobs cannot be Harmony-patched

### Candidate 3: `TransportStopSystem.OnUpdate`

- **Patch type**: Prefix or Postfix
- **What it enables**: Modify stop comfort/loading factors
- **Risk level**: Low

### Candidate 4: Custom GameSystemBase

- **Patch type**: N/A -- no patch needed
- **What it enables**: Create a system that reads/writes Route, TransportLine, TransportStop components directly
- **Risk level**: Low -- read component data and modify as needed
- **Note**: Safest approach for most transit mods

### Candidate 5: `TransportLineSystem.OnUpdate` Postfix for Per-Mode Duration

- **Patch type**: Postfix
- **What it enables**: Modify `RouteInfo.m_Duration` on segments after the system recalculates them, allowing per-transit-mode travel time adjustments
- **Risk level**: Medium -- must complete jobs before reading/writing segment data
- **Trigger pattern**: The system sets `PathfindUpdated` on route entities when segment durations change. A Postfix can detect this to apply modifications only when the system has refreshed data.

```csharp
[HarmonyPatch(typeof(TransportLineSystem), "OnUpdate")]
public static class TransportLineSystemOnUpdatePostfix
{
    public static void Postfix(TransportLineSystem __instance)
    {
        // Complete any scheduled jobs before accessing entity data
        __instance.CompleteDependency();

        EntityQuery routeQuery = __instance.GetEntityQuery(
            ComponentType.ReadOnly<Route>(),
            ComponentType.ReadOnly<TransportLine>(),
            ComponentType.ReadOnly<RouteSegment>(),
            ComponentType.ReadOnly<PathfindUpdated>()
        );

        NativeArray<Entity> routes = routeQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < routes.Length; i++)
        {
            Entity route = routes[i];
            DynamicBuffer<RouteSegment> segments =
                __instance.EntityManager.GetBuffer<RouteSegment>(route);

            for (int s = 0; s < segments.Length; s++)
            {
                Entity segEntity = segments[s].m_Segment;
                RouteInfo info = __instance.EntityManager.GetComponentData<RouteInfo>(segEntity);

                // Example: scale duration by 0.8 for this transit mode
                info.m_Duration *= 0.8f;
                __instance.EntityManager.SetComponentData(segEntity, info);
            }
        }
        routes.Dispose();
    }
}
```

## Mod Blueprint: Transport Vehicle Count Adjuster

**Description**: Mods that modify the maximum number of vehicles assignable to a transport line per transport type (Bus, Train, Tram, Ship, Airplane, Subway). This pattern involves replacing core route policy systems and patching the vehicle count UI to support higher limits.

**Reference mod**: [TransportPolicyAdjuster](https://github.com/SonnyX/CS2-TransportPolicyAdjuster)

### Systems to Create

1. **Custom ModifiedSystem** (`GameSystemBase`, registered at `SystemUpdatePhase.Modification4`) -- replaces `Game.Policies.ModifiedSystem` entirely. Replicates the route modifier refresh pipeline with custom max vehicle count logic per transport type.

### Components to Create

- None required. This pattern reuses existing game components: `TransportLineData`, `PolicySliderData`, `RouteModifierData`, `RouteOptionData`, `Route`, `RouteModifier`, `RouteVehicle`, `RouteWaypoint`, `RouteSegment`, `VehicleTiming`, `PathInformation`, `Policy`, `Modify`.

### Harmony Patches Needed

1. **Prefix on `Game.Policies.ModifiedSystem.OnUpdate`** -- disables the vanilla system (return false)
2. **Prefix on `Game.Policies.RouteModifierInitializeSystem.OnUpdate`** -- disables the vanilla initializer (return false)
3. **Postfix on `Game.UI.InGame.VehicleCountSection.OnUpdate`** -- adjusts max vehicle count bounds displayed in the UI
4. **Postfix on `Game.UI.InGame.VehicleCountSection.OnWriteProperties`** -- serializes the custom max vehicle count to the UI
5. **Prefix on `Game.UI.InGame.VehicleCountSection.OnSetVehicleCount`** -- intercepts vehicle count changes from the UI and applies custom limits
6. **Reverse patch on `Game.UI.InGame.VehicleCountSection.Visible`** -- preserves original visibility logic while modifying other behavior

### Key Game Components

| Component | Namespace | Role |
|-----------|-----------|------|
| `TransportLineData` | Game.Prefabs | Default vehicle interval and transport type per line prefab |
| `PolicySliderData` | Game.Prefabs | Slider range and step size for policy adjustments |
| `RouteModifierData` | Game.Prefabs | Defines how a policy modifies route simulation values |
| `RouteOptionData` | Game.Prefabs | Bitmask of route options a policy activates |
| `Route` | Game.Routes | Base route entity with option mask |
| `RouteModifier` | Game.Routes | Per-route modifier buffer (vehicle interval, ticket price) |
| `VehicleCountSection` | Game.UI.InGame | UI system controlling the vehicle count slider |
| `TransportLineSystem` | Game.Simulation | Core system that uses `CalculateVehicleCount(vehicleInterval, lineDuration)` |
| `RouteUtils` | Game.Routes | `ApplyModifier` static method for applying route-level policy modifiers |

### Settings Pattern

- Per-transport-type max vehicle count sliders (Bus, Train, Tram, Ship, Airplane, Subway)
- Uses `TransportType` enum to map settings to transport modes
- `IDictionarySource` for localization (not `MemorySource`)
- Burst-compiled native libraries shipped for performance parity with vanilla systems

### Compatibility Concerns

Incompatible with any mod that also patches `ModifiedSystem`, `RouteModifierInitializeSystem`, or `VehicleCountSection`. Only one mod can replace these systems at a time.

## Open Questions

- [x] How are transport lines structured as ECS entities? Route + TransportLine components, with child Waypoint and Segment entities
- [x] How does vehicle count get determined? `round(lineDuration / vehicleInterval)`, minimum 1
- [x] How does boarding work? Via TransportBoardingHelpers queue system with begin/end boarding items
- [x] What controls Day/Night scheduling? RouteOption.Day/Night flags checked against TimeSystem.normalizedTime
- [ ] How do passengers choose which line to ride? Likely via pathfinding -- passengers pathfind through the transit network. The PathfindPrefab on the TransportLinePrefab configures this.
- [ ] How does unbunching work precisely? The unbunching factor affects departure frame calculation in RouteUtils.CalculateDepartureFrame. Needs further decompilation.
- [ ] What triggers a vehicle to return to depot vs continue on route? The AbandonRoute flag is set when vehicle count exceeds target, or when vehicle model changes. Exact depot-return logic is in the vehicle AI systems.
- [ ] How are ticket prices applied to the economy? The TicketPrice is set on TransportLine and used via RouteModifier, but the revenue flow needs tracing through the economy system.

## Sources

- Decompiled from: Game.dll (Game.Routes, Game.Simulation, Game.Vehicles, Game.Prefabs, Game.UI.InGame namespaces)
- Key types: TransportLineSystem, TransportStopSystem, TransportVehicleDispatchSystem, TransportBoardingHelpers, RouteModifierInitializeSystem, VehicleCountSection, TransportLinePrefab, TransportLineData, Route, TransportLine, TransportStop, PublicTransport, PublicTransportFlags, PublicTransportVehicleData, TransportType, VehicleTiming, BoardingVehicle, WaitingPassengers, TaxiStand, RouteWaypoint, RouteSegment, RouteVehicle, RouteInfo, RouteModifier, RouteModifierData, RouteOptionData, RouteUtils, UITransportConfigurationPrefab, UITransportConfigurationData, Connected, CurrentRoute
- All decompiled snippets saved in `snippets/` directory
