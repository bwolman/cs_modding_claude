# Research: Emergency Vehicle Dispatch

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-22

## Scope

**What we're investigating**: How emergency vehicles (police cars, fire engines, ambulances, hearses) are dispatched in CS2 -- from the triggering event through request creation, queuing, pathfinding, and vehicle assignment.

**Why**: To understand whether dispatch can be triggered independently of events, whether service types can respond to cross-domain events (e.g., fire engines to medical calls, police to every accident), and to identify modding entry points for custom dispatch behavior.

**Boundaries**: Out of scope -- vehicle AI after dispatch (how they drive, fight fires, arrest criminals), building-level service coverage calculations, and the patrol system's route generation.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | ServiceRequest, ServiceDispatch, Dispatched, HandleRequest, RequestGroup, all dispatch systems, all request types |
| Game.dll | Game.Events | AccidentSite, AccidentSiteFlags, OnFire, InvolvedInAccident, ImpactSystem, IgniteSystem |
| Game.dll | Game.Citizens | HealthProblem, HealthProblemFlags |
| Game.dll | Game.Buildings | RescueTarget, FireStation, Hospital, DeathcareFacility, PoliceStation |
| Game.dll | Game.Vehicles | PoliceCar, FireEngine, Ambulance, Hearse |
| Game.dll | Game.Prefabs | PolicePurpose, FireRescueRequestType, HealthcareRequestType |
| Game.dll | Game.Pathfind | PathInformation, PathElement, SetupQueueItem |

## Namespace Disambiguation: `Game.Common.Event` vs `Game.Events.Event`

CS2 has two different empty marker structs both named `Event` in different namespaces. Several components in this research (e.g., `AccidentSite.m_Event`, `OnFire.m_Event`, `HealthProblem.m_Event`) store an `Entity` reference to an event entity. Understanding which `Event` type that entity carries is critical for mods that create or query event entities.

| Type | Namespace | Purpose | Lifetime |
|------|-----------|---------|----------|
| `Game.Common.Event` | `Game.Common` | Tag component on short-lived command/notification entities (Impact, AddAccidentSite, Ignite, AddHealthProblem) | Created and destroyed within 1-2 frames |
| `Game.Events.Event` | `Game.Events` | Tag component on persistent accident/disaster event entities (TrafficAccident, etc.) with duration, flags, and target tracking | Persists for the full duration of the accident or disaster |

**The `m_Event` fields on `AccidentSite`, `OnFire`, `HealthProblem`, and `InvolvedInAccident` point to persistent `Game.Events.Event` entities** (the long-lived TrafficAccident event entity), not the short-lived `Game.Common.Event` command entities.

**Ambiguity warning:** If your mod imports both `using Game.Common;` and `using Game.Events;`, the bare name `Event` becomes ambiguous and will cause a compiler error. Always use fully-qualified names when both namespaces are in scope:

```csharp
// BAD: ambiguous if both namespaces are imported
using Game.Common;
using Game.Events;

// ...
ComponentType.ReadWrite<Event>()  // Compiler error: 'Event' is ambiguous

// GOOD: use fully-qualified names
ComponentType.ReadWrite<Game.Events.Event>()     // persistent event entity
ComponentType.ReadWrite<Game.Common.Event>()      // short-lived command entity
```

Using the wrong `Event` type is a silent failure at runtime -- the entity will be created but will not match the queries that downstream systems use, so the entire pipeline silently does nothing.

For full details on the TrafficAccident event entity archetype and how these two `Event` types are used in the accident pipeline, see [`research/topics/EventEntityArchetype/README.md`](../EventEntityArchetype/README.md).

## Component Map

### `ServiceRequest` (Game.Simulation)

Base component present on **every** service request entity. Provides the common retry/cooldown/flag infrastructure.

| Field | Type | Description |
|-------|------|-------------|
| m_FailCount | byte | Number of times dispatch has failed for this request |
| m_Cooldown | byte | Ticks remaining before next dispatch attempt |
| m_Flags | ServiceRequestFlags | Reversed (find target from source), SkipCooldown |

*Source: `Game.dll` -> `Game.Simulation.ServiceRequest`*

### `ServiceRequestFlags` (Game.Simulation)

| Flag | Value | Description |
|------|-------|-------------|
| Reversed | 1 | Request searches for a target from a source (station/vehicle finds work) instead of the normal source-from-target flow |
| SkipCooldown | 2 | Skip the cooldown timer on next tick |

### `ServiceDispatch` (Game.Simulation)

Buffer element on service buildings and vehicles. Each element is a pending request assigned to that handler.

| Field | Type | Description |
|-------|------|-------------|
| m_Request | Entity | The request entity this handler is dispatched to fulfill |

*Source: `Game.dll` -> `Game.Simulation.ServiceDispatch`*

### `Dispatched` (Game.Simulation)

Added to a request entity once a handler (vehicle/building) has been assigned.

| Field | Type | Description |
|-------|------|-------------|
| m_Handler | Entity | The vehicle or building entity handling this request |

*Source: `Game.dll` -> `Game.Simulation.Dispatched`*

### `HandleRequest` (Game.Simulation)

Transient component used to signal request state changes (completion or handler assignment).

| Field | Type | Description |
|-------|------|-------------|
| m_Request | Entity | The request entity |
| m_Handler | Entity | The handler entity (or Entity.Null if completed/failed) |
| m_Completed | bool | True if the request should be destroyed (fulfilled) |
| m_PathConsumed | bool | True if path data should be cleaned up |

*Source: `Game.dll` -> `Game.Simulation.HandleRequest`*

### `RequestGroup` (Game.Simulation)

Temporary component added at request creation time. The ServiceRequestSystem randomly assigns the request to an UpdateFrame group, which controls which tick the dispatch system processes it.

| Field | Type | Description |
|-------|------|-------------|
| m_GroupCount | uint | Number of update frame groups to distribute across (typically 4 or 16) |

*Source: `Game.dll` -> `Game.Simulation.RequestGroup`*

### `PoliceEmergencyRequest` (Game.Simulation)

Request for police emergency response (accidents and crimes).

| Field | Type | Description |
|-------|------|-------------|
| m_Site | Entity | The AccidentSite entity (road segment or building) |
| m_Target | Entity | The specific target entity (vehicle, criminal) |
| m_Priority | float | Priority value (higher = more urgent, based on severity) |
| m_Purpose | PolicePurpose | Flags: Patrol, Emergency, Intelligence |

*Source: `Game.dll` -> `Game.Simulation.PoliceEmergencyRequest`*

### `PolicePurpose` (Game.Prefabs)

| Flag | Value | Description |
|------|-------|-------------|
| Patrol | 1 | Routine patrol |
| Emergency | 2 | Emergency response (accidents) |
| Intelligence | 4 | Intelligence/surveillance (monitored crimes) |

### `FireRescueRequest` (Game.Simulation)

Request for fire department response.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | The entity that is on fire or needs rescue |
| m_Priority | float | Priority value (fire intensity) |
| m_Type | FireRescueRequestType | Fire or Disaster |

*Source: `Game.dll` -> `Game.Simulation.FireRescueRequest`*

### `FireRescueRequestType` (Game.Simulation)

| Value | Description |
|-------|-------------|
| Fire | Standard fire response |
| Disaster | Disaster rescue response |

### `HealthcareRequest` (Game.Simulation)

Request for healthcare response (ambulance or hearse).

| Field | Type | Description |
|-------|------|-------------|
| m_Citizen | Entity | The citizen entity needing help |
| m_Type | HealthcareRequestType | Ambulance or Hearse |

*Source: `Game.dll` -> `Game.Simulation.HealthcareRequest`*

### `HealthcareRequestType` (Game.Simulation)

| Value | Description |
|-------|-------------|
| Ambulance | Living citizen needing medical transport |
| Hearse | Dead citizen needing body transport |

### `FireStationFlags` (Game.Buildings)

| Flag | Value | Description |
|------|-------|-------------|
| `HasAvailableFireEngines` | 1 | At least one truck slot available (efficiency-adjusted) |
| `HasFreeFireEngines` | 2 | At least one raw truck slot free (unmodified capacity) |
| `HasAvailableFireHelicopters` | 4 | At least one helicopter slot available |
| `HasFreeFireHelicopters` | 8 | At least one raw helicopter slot free |
| `DisasterResponseAvailable` | 0x10 | Disaster response capacity available |

### `FireEngineFlags` (Game.Vehicles)

| Flag | Value | Description |
|------|-------|-------------|
| `Returning` | 1 | Heading back to the fire station |
| `Extinguishing` | 2 | Stopped at fire, actively extinguishing |
| `Empty` | 4 | Water tank depleted, cannot fight fires |
| `DisasterResponse` | 8 | Assigned to disaster response capacity pool |
| `Rescueing` | 0x10 | Stopped at collapsed building, clearing debris |
| `EstimatedEmpty` | 0x20 | Predicted to run empty based on current fire intensity |
| `Disabled` | 0x40 | Over-capacity parked vehicle, stays idle |

### `FireStationData` (Game.Prefabs)

Prefab component on fire station entities. Merged with upgrades via `Combine()`.

| Field | Type | Description |
|-------|------|-------------|
| `m_FireEngineCapacity` | int | Number of fire truck slots |
| `m_FireHelicopterCapacity` | int | Number of helicopter slots |
| `m_DisasterResponseCapacity` | int | Disaster response capacity (separate pool) |
| `m_VehicleEfficiency` | float | Multiplier for vehicle extinguish rate and effectiveness |

**`Combine()` merge (additive)**:
```csharp
m_FireEngineCapacity += otherData.m_FireEngineCapacity;
m_FireHelicopterCapacity += otherData.m_FireHelicopterCapacity;
m_DisasterResponseCapacity += otherData.m_DisasterResponseCapacity;
m_VehicleEfficiency += otherData.m_VehicleEfficiency;
```

### `FireEngineData` (Game.Prefabs)

Prefab component on fire engine vehicle prefabs.

| Field | Type | Description |
|-------|------|-------------|
| `m_ExtinguishingRate` | float | Fire intensity reduction per tick while extinguishing |
| `m_ExtinguishingSpread` | float | Area-of-effect radius — nearby buildings also get hosed |
| `m_ExtinguishingCapacity` | float | Total water tank size; 0 = infinite (no tank limit) |
| `m_DestroyedClearDuration` | float | Time to clear a collapsed building's rubble |

### `FireConfigurationData` (Game.Prefabs)

Singleton prefab. Controls fire response timing and structural integrity.

| Field | Type | Description |
|-------|------|-------------|
| `m_DefaultStructuralIntegrity` | float | Base integrity for non-building entities |
| `m_BuildingStructuralIntegrity` | float | Base integrity for buildings (affects water damage rate) |
| `m_StructuralIntegrityLevel1`–`Level5` | float | Per-level integrity multipliers |
| `m_ResponseTimeRange` | Bounds1 | Min/max frames before 911 call is placed after ignition |
| `m_TelecomResponseTimeModifier` | float | Response time reduction from telecom infrastructure |
| `m_DarknessResponseTimeModifier` | float | Response time increase at night |
| `m_DeathRateOfFireAccident` | float | Citizen death probability per fire event |

### `PolicePatrolRequest` (Game.Simulation)

Request for routine police patrol.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | Target location for patrol |
| m_Priority | float | Patrol priority |
| m_DispatchIndex | byte | Index for dispatch ordering |

### `EvacuationRequest` (Game.Simulation)

Request for evacuation vehicle dispatch.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | Building/area to evacuate |
| m_Priority | float | Evacuation priority |

### `AccidentSite` (Game.Events)

Attached to road segments or buildings where an accident/crime is occurring. This is the key gatekeeper for police dispatch.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The event entity (traffic accident or crime) |
| m_PoliceRequest | Entity | Current police emergency request (or Entity.Null) |
| m_Flags | AccidentSiteFlags | State flags controlling police request creation |
| m_CreationFrame | uint | Frame when the accident site was created |
| m_SecuredFrame | uint | Frame when police secured the site |

*Source: `Game.dll` -> `Game.Events.AccidentSite`*

### `AccidentSiteFlags` (Game.Events)

| Flag | Value | Description |
|------|-------|-------------|
| StageAccident | 0x01 | Site is staging additional accident impacts |
| Secured | 0x02 | Police have secured the site |
| CrimeScene | 0x04 | This is a crime scene (vs traffic accident) |
| TrafficAccident | 0x08 | This is a traffic accident |
| CrimeFinished | 0x10 | The crime has concluded |
| CrimeDetected | 0x20 | The crime has been detected by citizens/monitoring |
| CrimeMonitored | 0x40 | CCTV or similar is monitoring the crime |
| RequirePolice | 0x80 | Police are needed (set each tick by AccidentSiteSystem) |
| MovingVehicles | 0x100 | Involved vehicles are still moving |

### `OnFire` (Game.Events)

Attached to entities (buildings, trees, vehicles) that are currently burning. Triggers fire rescue request creation.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The fire event entity |
| m_RescueRequest | Entity | Current fire rescue request (or Entity.Null) |
| m_Intensity | float | Current fire intensity (used as priority) |
| m_RequestFrame | uint | Frame at which the request should be created |

### `HealthProblem` (Game.Citizens)

Attached to citizens with health issues. Triggers healthcare request creation.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The event that caused the health problem |
| m_HealthcareRequest | Entity | Current healthcare request (or Entity.Null) |
| m_Flags | HealthProblemFlags | Sick, Dead, Injured, RequireTransport, InDanger, Trapped, NoHealthcare |
| m_Timer | byte | Timer for health problem progression |

### `InvolvedInAccident` (Game.Events)

Attached to vehicles/entities involved in a traffic accident.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The accident event entity |
| m_Severity | float | Severity of involvement (used for police priority) |
| m_InvolvedFrame | uint | Frame when entity became involved |

## System Map

### `ServiceRequestSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Purpose**: Central request lifecycle manager. Handles two jobs:
  1. **UpdateRequestGroupJob**: Takes new requests with `RequestGroup` component, randomly assigns them to an `UpdateFrame` group, then removes `RequestGroup`.
  2. **HandleRequestJob**: Processes `HandleRequest` events -- marks requests as `Dispatched`, destroys completed requests, resets failed requests.
- **Queries**:
  - RequestGroup entities (new requests needing frame assignment)
  - HandleRequest entities (request state changes)
- **Reads**: HandleRequest, RequestGroup
- **Writes**: Dispatched, ServiceRequest

### `AccidentSiteSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames
- **Purpose**: Manages accident sites and creates police emergency requests. This is the ONLY system that creates `PoliceEmergencyRequest` entities for accidents.
- **Key logic in AccidentSiteJob**:
  1. Iterates over all entities with `AccidentSite` component
  2. Checks `TargetElement` buffer on the event to find involved entities
  3. Tracks max severity from `InvolvedInAccident` components (`num2` = max severity)
  4. Identifies the highest-severity non-moving entity (`entity2`)
  5. For crime scenes: manages detection delay (alarm delay from `CrimeData`)
  6. **Unconditionally clears `RequirePolice` flag** at the start of each entity's evaluation
  7. **Conditionally re-sets `RequirePolice`** only if conditions are met and a valid target exists
  8. Calls `RequestPoliceIfNeeded()` which creates a `PoliceEmergencyRequest` entity if one does not already exist
- **Decompiled RequirePolice logic** (from AccidentSiteJob):
  ```csharp
  // Step 1: UNCONDITIONAL CLEAR
  accidentSite.m_Flags &= ~AccidentSiteFlags.RequirePolice;

  // Step 2: CONDITIONAL RE-SET based on severity and crime scene status
  if (num2 > 0f || (accidentSite.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene)) == AccidentSiteFlags.CrimeScene)
  {
      if (num2 > 0f || (accidentSite.m_Flags & AccidentSiteFlags.CrimeDetected) != 0)
      {
          if (entity2 != Entity.Null)
          {
              accidentSite.m_Flags |= AccidentSiteFlags.RequirePolice;
              RequestPoliceIfNeeded(...);
          }
      }
  }
  ```
  Where `num2` is the maximum severity from `InvolvedInAccident` targets, and `entity2` is the highest-severity non-moving entity.
- **Request archetype**: `ServiceRequest + PoliceEmergencyRequest + RequestGroup(4)`
- **Critical insight for modding**: `RequirePolice` follows a **clear-then-evaluate** pattern -- it is unconditionally stripped via bitmask clear (`&= ~RequirePolice`) before evaluation, then conditionally re-added via bitmask set (`|= RequirePolice`) only if the conditions above are met. This means any value a mod sets on `RequirePolice` before `AccidentSiteSystem` runs will be wiped. A mod system that needs `RequirePolice` to persist must use `[UpdateAfter(typeof(AccidentSiteSystem))]` to run after the clear-and-evaluate cycle, not `[UpdateBefore]`.
- **Synthetic AccidentSite removal (in-game tested)**: If a mod creates an `AccidentSite` component on an entity that lacks valid `InvolvedInAccident` entities in the event's `TargetElement` buffer, `AccidentSiteSystem` will find zero severity (`num2 == 0`) and no valid target (`entity2 == Entity.Null`). It will **not** re-set `RequirePolice`. For crime scenes, the `CrimeDetected` flag must also be set. Without these conditions met, `AccidentSiteSystem` strips `RequirePolice` on its next tick (~64 frames / ~1 second), causing `PoliceEmergencyDispatchSystem.ValidateSite()` to destroy any associated police request. Synthetic AccidentSites become inert within ~1 second unless they are backed by a valid event with proper `InvolvedInAccident` entities or are properly configured crime scenes with `CrimeScene | CrimeDetected` flags and a valid event entity referencing a prefab with `CrimeData`.
- **SecureAccidentSite completes instantly without AccidentSite (in-game tested)**: If a police car arrives at a target entity that does not have an `AccidentSite` component, the `SecureAccidentSite` action returns `true` immediately — the car leaves as soon as it arrives. The police car's AI checks for `AccidentSite` on the target to determine whether securing work needs to be done; without it, the car considers the job done instantly.

### `PoliceEmergencyDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Purpose**: Processes `PoliceEmergencyRequest` entities and dispatches police vehicles.
- **Queries**: Entities with `PoliceEmergencyRequest` + `UpdateFrame`
- **Key logic**:
  1. **ValidateSite()**: Checks that the `AccidentSite` still has `RequirePolice` flag set and is not already `Secured`. If the site no longer requires police, the request is destroyed.
  2. **FindVehicleSource()**: Pathfinds from `SetupTargetType.PolicePatrol` origin (police stations in target's district) to `SetupTargetType.AccidentLocation` destination. **WARNING (in-game tested): `AccidentLocation` pathfinding is road-only.** If the target entity is a building (not a road segment), `PoliceCarAISystem` produces straight-line paths, causing police cars to **float over buildings**. For dispatching police to buildings or arbitrary locations, use the ServiceDispatch buffer injection approach described in the Mod Blueprint section below.
  3. **Reversed requests**: Police stations/cars with available capacity create reversed requests to find work.
  4. **DispatchVehicle()**: Adds `Dispatched` component to request, adds `ServiceDispatch` element to the handler's buffer.
- **PolicePurpose filtering**: The `FindVehicleSource()` method passes `m_Purpose` as `m_Value` on the origin `SetupQueueTarget`. The pathfinder uses this to match police stations/cars whose `m_PurposeMask` includes the requested purpose.

### `FireRescueDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Purpose**: Processes `FireRescueRequest` entities and dispatches fire engines/helicopters using a two-sided request model.
- **Two request directions**:
  - **Non-reversed** (fire needing help): created by `FireSimulationSystem` when a building ignites. The dispatcher searches for the nearest available engine.
  - **Reversed** (station/engine advertising availability): created by `FireStationAISystem` and `FireEngineAISystem` when capacity is available. The dispatcher searches for a fire to send the vehicle to.
- **Key logic for non-reversed requests (fire needing help)**:
  1. **ValidateTarget()**: Checks target still has `OnFire` or `RescueTarget`. Claims ownership via `OnFire.m_RescueRequest = entity`. Destroys request if target no longer burning.
  2. **FindVehicleSource()**: Determines fire's district (via `CurrentDistrict` or spatial quad-tree lookup). Pathfinds with `SetupTargetType.FireEngine` origin (road+flying+offroad), `SetupTargetType.CurrentLocation` destination at the fire. Max speed ~1000 km/h — pathfinder scores by travel time only. Fire has 30m tolerance radius (`m_Value2 = 30f`).
  3. **DispatchVehicle()**: When pathfind returns, enqueues a `VehicleDispatch`. `DispatchVehiclesJob` adds `ServiceDispatch` element to the winning station's or engine's buffer.
- **Key logic for reversed requests (station advertising)**:
  1. **ValidateReversed()**: Validates source still has `HasAvailableFireEngines` or `HasAvailableFireHelicopters`. Also works for en-route fire engines: validates not `Empty/EstimatedEmpty/Disabled` and `m_RequestCount <= 1`.
  2. **FindVehicleTarget()**: Pathfinds from the station/engine's current location to `SetupTargetType.FireRescueRequest` destination — the pathfinder finds a matching fire request.
  3. On success: enqueues `VehicleDispatch`, station's `ServiceDispatch` buffer gets the fire request added.
- **District lookup**: Uses `AreaSearchSystem` quad-tree to find which `District` entity contains the fire's position if it doesn't already have `CurrentDistrict`.
- **Pathfind parameters**: `m_IgnoredRules = ForbidCombustionEngines | ForbidTransitTraffic | ForbidHeavyTraffic | ForbidPrivateTraffic | ForbidSlowTraffic | AvoidBicycles`

### `HealthcareDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Purpose**: Processes `HealthcareRequest` entities and dispatches ambulances or hearses.
- **Key logic**:
  1. **ValidateTarget()**: Checks citizen still has `HealthProblem` with `RequireTransport` flag.
  2. **FindVehicleSource()**: Branches on `HealthcareRequestType`:
     - `Ambulance`: Pathfinds from `SetupTargetType.Ambulance` (hospitals) with road + flying + boarding methods
     - `Hearse`: Pathfinds from `SetupTargetType.Hearse` (deathcare facilities) with road + boarding methods
  3. **ValidateReversed()**: Also branches on type -- checks `Hospital`/`Ambulance` for ambulance requests, `DeathcareFacility`/`Hearse` for hearse requests.

### `FireSimulationSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Purpose**: Manages active fires. Creates `FireRescueRequest` when an entity has `OnFire` and the request frame has been reached.
- **Request creation**: `new FireRescueRequest(entity, onFire.m_Intensity, FireRescueRequestType.Fire)` with `RequestGroup(4)`
- **Request archetype**: `ServiceRequest + FireRescueRequest + RequestGroup`

### `FireStationAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 256 frames (offset 112)
- **Purpose**: Manages fire station state and deploys vehicles when dispatches arrive in the station's `ServiceDispatch` buffer.
- **Key logic in `Tick()`**:
  1. **Capacity calculation**: Computes `vehicleCapacity = GetVehicleCapacity(min(efficiency, immediateEfficiency), m_FireEngineCapacity)`. Efficiency is from the `Efficiency` buffer (service coverage zone). Counts all `OwnedVehicle` entities to determine how many are deployed vs. parked.
  2. **Upgrade merging**: `UpgradeUtils.CombineStats(ref data, installedUpgrades, ...)` merges all `InstalledUpgrade` entities' `FireStationData.Combine()` results — additively sums all capacity and efficiency values.
  3. **SpawnVehicle()**: Called for each pending `ServiceDispatch`. Validates:
     - `availableVehicles > 0` (or `freeVehicles > 0` if request target is the station itself)
     - Disaster requests also require `disasterResponseAvailable > 0`
     - If pathfinder pre-selected a specific parked vehicle (via `PathInformation.m_Origin`), deploys that exact entity; otherwise calls `FireEngineSelectData.CreateVehicle()` to spawn from the city configuration's vehicle prefab pool
     - Sets `Car.Flags = Emergency | StayOnRoad | UsePublicTransportLanes`
     - No `DisasterResponse` flag unless `FireRescueRequestType.Disaster`
  4. **Vehicle flag sync**: For parked engines, syncs `Disabled` (over-capacity) and `DisasterResponse` flags via `FireStationActionJob`.
  5. **FireStationFlags update**: Sets `HasAvailableFireEngines`, `HasFreeFireEngines`, `HasAvailableFireHelicopters`, `HasFreeFireHelicopters`, `DisasterResponseAvailable` based on remaining counts.
  6. **Available vs Free distinction**:
     - `availableVehicles = GetVehicleCapacity(min(efficiency, immediateEfficiency), capacity)` — efficiency-adjusted count
     - `freeVehicles = capacity` — raw configured capacity
     - Dispatches from other districts use `HasAvailableFireEngines` (efficiency-reduced). Dispatches to the station itself (self-fire) use `HasFreeFireEngines` (raw capacity).
  7. **RequestTargetIfNeeded()**: If any vehicles available, creates a reversed `FireRescueRequest(station_entity, availableEngines + availableHelicopters, Fire)` — the station advertises itself to the dispatcher.
- **Helicopter detection**: `m_HelicopterData.HasComponent(vehicle)` distinguishes helicopter vehicles from truck vehicles. Separate counters for each type.
- **`CheckPathType()`**: Inspects first `PathElement` to determine if a parked vehicle should be deployed as car or helicopter route type.
- **`FireStationData.Combine()`** (upgrade merge):
  ```csharp
  m_FireEngineCapacity += otherData.m_FireEngineCapacity;
  m_FireHelicopterCapacity += otherData.m_FireHelicopterCapacity;
  m_DisasterResponseCapacity += otherData.m_DisasterResponseCapacity;
  m_VehicleEfficiency += otherData.m_VehicleEfficiency;
  ```

### `FireEngineAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames (offset 4)
- **Purpose**: Controls deployed fire engine behavior — driving to fires, extinguishing, multi-stop chaining, and returning home.
- **State machine (via `FireEngineFlags`)**:

| Flag | Meaning |
|------|---------|
| `Returning` | Heading back to station |
| `Extinguishing` | Stopped at fire, actively fighting |
| `Rescueing` | Stopped at collapsed building, clearing debris |
| `Empty` | Water tank depleted, cannot fight |
| `EstimatedEmpty` | Predicted empty based on current fire intensity |
| `Disabled` | Over-capacity at station, stays parked |
| `DisasterResponse` | Assigned to disaster response pool |

- **Key logic in `Tick()`**:
  1. **Path end reached**: If at destination and not Extinguishing/Rescueing, calls `BeginExtinguishing()`. If fire is out or can't extinguish, calls `CheckServiceDispatches()` for chained fires, then `ReturnToDepot()`.
  2. **`BeginExtinguishing()`**: Checks target — if `OnFire` → `Extinguishing`; if `RescueTarget` → `Rescueing`. Stops the vehicle.
  3. **`TryExtinguishFire()`**: Area-of-effect extinguishing. Uses `ObjectExtinguishIterator` to find all burning/destroyed entities within `m_ExtinguishingSpread` radius. Per tick rate: `4/15 * m_ExtinguishingRate * m_Efficiency`. Water damage: `rate * 10 / structuralIntegrity`. Tank depletion: `m_ExtinguishingAmount -= rate * 4/15`. Sets `Empty` when `m_ExtinguishingAmount == 0`.
  4. **`CheckServiceDispatches()`**: Selects highest-priority queued fire from `ServiceDispatch` buffer. Priority is `FireRescueRequest.m_Priority`. Checks that fire is on same road endpoint as current path to avoid detours.
  5. **`SelectNextDispatch()`**: Pops next dispatch, validates fire still burning. If path is pre-computed in `PathElement` buffer, appends it to current path. Otherwise sets new `Target`.
  6. **`ReturnToDepot()`**: Clears all requests, sets `Returning` flag, sets target to `owner.m_Owner` (the fire station entity).
  7. **`ParkCar()`**: On arrival home — resets state to 0, restores `m_ExtinguishingAmount = m_ExtinguishingCapacity` (tank refilled), sets `Disabled` if `HasFreeFireEngines` is absent, sets `DisasterResponse` if station has it available.
  8. **`RequestTargetIfNeeded()`**: Every ~64 frames, if `m_RequestCount <= 1` and not `EstimatedEmpty`, creates reversed `FireRescueRequest(vehicleEntity, 1f, Fire)` — engine advertises itself for additional calls.
  9. **Emergency pathfinding (to fire)**: `PathfindWeights(1f, 0f, 0f, 0f)` — pure time, no comfort/safety tradeoffs. Ignores combustion engine zones, heavy traffic bans. Destination has 30m tolerance (`m_Value2 = 30f`).
  10. **Return pathfinding**: `PathfindWeights(1f, 1f, 1f, 1f)` — balanced normal routing.
  11. **Close-enough shortcut**: If blocked by traffic and within 30m of target, `EndNavigation()` — stops and considers path complete.

### `FirePathfindSetup.SetupFireEnginesJob` (Game.Simulation)

- **Purpose**: For each pending fire rescue pathfind request, identifies which fire stations and engines are eligible candidates and submits them to the pathfinder.
- **For stations** (`FireStation` chunk):
  - If request target is the station itself (self-fire): uses `HasFreeFireEngines`/`HasFreeFireHelicopters` (raw capacity)
  - If request district matches station's service district: uses `HasAvailableFireEngines`/`HasAvailableFireHelicopters`
  - Disaster requests additionally require `DisasterResponseAvailable`
  - Outside connections allowed only if city has `ImportOutsideServices` option enabled
  - Cost: `pathfindWeights.time * 10f` (10-second base penalty per station candidate)
- **For in-flight engines** (`FireEngine` chunk):
  - Skip if `Empty` or `EstimatedEmpty`
  - Skip if not in same service district as the fire
  - Skip if `Disabled` (unless the fire is at the engine's own station)
  - Disaster requests require `DisasterResponse` flag on the engine
  - Cost for chaining: remaining path time + sum of pre-cached `PathInformation.m_Duration` for each queued stop

### `FirePathfindSetup.FireRescueRequestsJob` (Game.Simulation)

- **Purpose**: For reversed requests (station/engine advertising, looking for a fire), finds eligible `FireRescueRequest` entities to respond to.
- **District filter**: Fire's district must be in the source's service district
- **Disaster filter**: `DisasterResponse` type requires station `DisasterResponseAvailable` or engine `DisasterResponse` flag
- **Multiple candidate points per fire**: Adds the fire's road-network-adjacent positions (within 30m radius via `NetTree` search) as path targets — this is how fire engines navigate to the correct side of the road

### `HealthProblemSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Purpose**: Manages citizen health problems. Creates `HealthcareRequest` when a citizen has `HealthProblem` with `RequireTransport` flag.
- **Request creation logic**:
  1. Determines type: `Hearse` if citizen is dead, `Ambulance` otherwise
  2. For hearses: only creates request if citizen is in a building with `HasInsideRoom` flag
  3. Creates entity: `new HealthcareRequest(entity, type)` with `RequestGroup(16)`
- **Request archetype**: `ServiceRequest + HealthcareRequest + RequestGroup`

## Data Flow

```
[Event Occurs]
    |
    +---> Fire ignites (IgniteSystem adds OnFire to entity)
    |         |
    |         v
    |     FireSimulationSystem: if m_RequestFrame <= currentFrame
    |         |                 && no existing request
    |         v
    |     Creates FireRescueRequest entity
    |         (ServiceRequest + FireRescueRequest + RequestGroup(4))
    |         priority = onFire.m_Intensity
    |
    +---> Accident occurs (ImpactSystem -> AddAccidentSiteSystem)
    |         |
    |         v
    |     AccidentSiteSystem (every 64 frames):
    |         |  1. Clears RequirePolice unconditionally (&= ~RequirePolice)
    |         |  2. Checks InvolvedInAccident severity + valid target
    |         |  3. Re-sets RequirePolice only if conditions met (|= RequirePolice)
    |         v
    |     Creates PoliceEmergencyRequest entity
    |         (ServiceRequest + PoliceEmergencyRequest + RequestGroup(4))
    |
    +---> Citizen sick/injured/dead (AddHealthProblem event)
              |
              v
          HealthProblemSystem: checks RequireTransport flag
              |
              v
          Creates HealthcareRequest entity
              (ServiceRequest + HealthcareRequest + RequestGroup(16))

[ServiceRequestSystem]
    |
    v
UpdateRequestGroupJob: removes RequestGroup, adds UpdateFrame(random 0..N)
    |
    v
[Dispatch System] (runs every 16 frames, processes requests in matching UpdateFrame)
    |
    +---> PoliceEmergencyDispatchSystem
    +---> FireRescueDispatchSystem
    +---> HealthcareDispatchSystem
    |
    v
FindVehicleSource(): enqueues pathfind request
    |
    v
[PathfindSetupSystem -> Pathfinding]
    |
    v
PathInformation component added to request with origin/destination
    |
    v
[Next dispatch tick]
    |
    v
DispatchVehicle():
    +---> Adds Dispatched component to request (points to handler)
    +---> Adds ServiceDispatch element to handler's buffer (points to request)
    |
    v
[Vehicle AI System] (PoliceCarAISystem, FireEngineAISystem, AmbulanceAISystem, HearseAISystem)
    |
    v
Vehicle drives to target, performs service
    |
    v
[HandleRequest created with m_Completed = true]
    |
    v
ServiceRequestSystem.HandleRequestJob: destroys the request entity
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Police request group size | Hardcoded 4 | AccidentSiteSystem.RequestPoliceIfNeeded() |
| Fire request group size | Hardcoded 4 | FireSimulationSystem |
| Healthcare request group size | Hardcoded 16 | HealthProblemSystem |
| Crime alarm delay | CrimeData.m_AlarmDelay (Bounds1) | Event prefab, modified by CityModifierType.CrimeResponseTime |
| Crime duration | CrimeData.m_CrimeDuration (Bounds1) | Event prefab |
| Accident staging timeout | 3600 frames (~60 seconds) | Hardcoded in AccidentSiteSystem |
| Police pathfind max speed | 111.111115f (~400 km/h) | PoliceEmergencyDispatchSystem.FindVehicleSource() |
| Fire pathfind max speed | 277.77777f (~1000 km/h) | FireRescueDispatchSystem.FindVehicleSource() |
| Fire engine emergency pathfind weights | (1,0,0,0) — pure time | FireEngineAISystem.FindNewPath() outbound |
| Fire engine return pathfind weights | (1,1,1,1) — balanced | FireEngineAISystem.FindNewPath() returning |
| Fire engine close-enough threshold | 30m | FireEngineAISystem.IsCloseEnough() |
| Fire engine extinguish tick rate | 4/15 * ExtinguishingRate * Efficiency | FireEngineAISystem.TryExtinguishFire() |
| Fire engine water damage cap | 50% (0.5 Damaged.m_Damage.z) | FireEngineAISystem.FireExtinguishingJob |
| Fire station update interval | 256 frames | FireStationAISystem.GetUpdateInterval() |
| Fire engine update interval | 16 frames | FireEngineAISystem.GetUpdateInterval() |
| Fire station capacity base penalty (pathfind) | time_weight * 10f | FirePathfindSetup.SetupFireEnginesJob |
| Disaster response request priority | 10f | CollapsedBuildingSystem.RequestRescueIfNeeded() |
| Ambulance pathfind max speed | 277.77777f (~1000 km/h) | HealthcareDispatchSystem.FindVehicleSource() |
| Hearse pathfind max speed | 111.111115f (~400 km/h) | HealthcareDispatchSystem.FindVehicleSource() |
| Police purpose for accidents | PolicePurpose.Emergency | AccidentSiteSystem |
| Police purpose for monitored crimes | PolicePurpose.Intelligence | AccidentSiteSystem |
| Crime scene notification prefab | PoliceConfigurationData.m_CrimeSceneNotificationPrefab | Singleton |

## Harmony Patch Points

### Candidate 1: `AccidentSiteSystem.AccidentSiteJob.RequestPoliceIfNeeded`

- **Signature**: `void RequestPoliceIfNeeded(int jobIndex, Entity entity, ref AccidentSite accidentSite, Entity target, float severity)`
- **Patch type**: Not directly patchable (Burst-compiled job struct method)
- **What it enables**: Would control when/if police requests are created for accident sites
- **Risk level**: High (Burst-compiled, cannot use Harmony)
- **Alternative**: Patch AccidentSiteSystem.OnUpdate() or create a system that runs after AccidentSiteSystem and creates additional requests

### Candidate 2: `AccidentSiteSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: A Postfix could inspect newly-created police request entities and create additional requests (e.g., force police to every accident even without severity). A Prefix could modify the AccidentSite components before the job runs.
- **Risk level**: Medium (scheduled job, need to complete dependency)
- **Side effects**: Must wait for job completion in postfix to read results

### Candidate 3: Custom system after AccidentSiteSystem

- **Signature**: N/A (new system)
- **Patch type**: N/A (ECS system, not a patch)
- **What it enables**: Query all AccidentSite entities each tick and create police/fire/healthcare requests as desired. This is the safest approach for cross-service dispatch.
- **Risk level**: Low
- **Side effects**: Additional requests may overwhelm available vehicles

### Candidate 4: `PoliceEmergencyDispatchSystem.PoliceDispatchJob.ValidateSite`

- **Signature**: `bool ValidateSite(Entity entity, Entity site)`
- **Patch type**: Not directly patchable (Burst-compiled)
- **What it enables**: Currently checks `(AccidentSiteFlags.Secured | AccidentSiteFlags.RequirePolice) != RequirePolice` -- controls whether a request stays alive
- **Alternative**: Ensure RequirePolice flag is always set on the AccidentSite component via a custom system

### Candidate 5: Custom system creating requests directly

- **Signature**: N/A
- **Patch type**: N/A (ECS system)
- **What it enables**: Create any request type (PoliceEmergencyRequest, FireRescueRequest, HealthcareRequest) for any target entity. The dispatch systems process requests based on their component types, not on what originally triggered them.
- **Risk level**: Low
- **Side effects**: Must ensure the target entity has the expected validation component (AccidentSite for police, OnFire/RescueTarget for fire, HealthProblem with RequireTransport for healthcare). **Important**: HealthProblem must be added via the `AddHealthProblem` event pipeline, not directly -- see the [Mod Blueprint warning](#how-to-dispatch-an-emergency-vehicle-programmatically-no-event-needed).

## Modding Warning: UpdateBefore vs UpdateAfter for RequirePolice

`AccidentSiteSystem` uses a **clear-then-evaluate** pattern for the `RequirePolice` flag. On every tick (every 64 frames), for every `AccidentSite` entity, the system:

1. **Unconditionally clears** `RequirePolice`: `accidentSite.m_Flags &= ~AccidentSiteFlags.RequirePolice;`
2. **Conditionally re-sets** `RequirePolice` only if severity > 0 with a valid non-moving target, or if it is a detected unsecured crime scene with a valid target: `accidentSite.m_Flags |= AccidentSiteFlags.RequirePolice;`

This means:

- **`[UpdateBefore(typeof(AccidentSiteSystem))]` is WRONG** for systems that need `RequirePolice` to persist. Any value you set will be unconditionally wiped by the `&= ~RequirePolice` clear at the top of the system's evaluation loop.
- **`[UpdateAfter(typeof(AccidentSiteSystem))]` is CORRECT**. Your system runs after the clear-and-evaluate cycle, so you can safely set `RequirePolice` and it will persist until the next `AccidentSiteSystem` tick (64 frames later). The `PoliceEmergencyDispatchSystem` will see your flag during the intervening dispatch ticks.

The same pattern applies to `MovingVehicles`, which is also unconditionally cleared then conditionally re-set each tick.

## Mod Blueprint

### Key Findings

**1. Dispatch is triggered by events, but requests are independent entities.**

Each dispatch system processes request *entities* -- it does not care *why* the request was created. The request types are simple data structs. A mod can create request entities directly without any underlying event, as long as the validation components exist on the target.

**2. Police dispatch via PoliceEmergencyRequest has strict requirements (in-game tested).**

The `PoliceEmergencyDispatchSystem.ValidateSite()` checks that the site entity has `AccidentSite` with `RequirePolice` set and `Secured` not set. However, creating an `AccidentSite` on an arbitrary entity **does not work reliably** — `AccidentSiteSystem` removes `RequirePolice` within ~1 second if the event lacks valid `InvolvedInAccident` entities. Additionally, `SetupTargetType.AccidentLocation` pathfinding is **road-only** — targeting a building entity causes police cars to float over buildings instead of following roads. For arbitrary police dispatch (especially to buildings), use the ServiceDispatch buffer injection approach (see finding #6 below).

**3. Fire dispatch requires OnFire or RescueTarget on the target.**

The `FireRescueDispatchSystem.ValidateTarget()` checks for `OnFire` or `RescueTarget`. Without one of these, the request gets destroyed.

**4. Healthcare dispatch requires HealthProblem with RequireTransport.**

The `HealthcareDispatchSystem.ValidateTarget()` checks for `HealthProblem` with `RequireTransport` flag.

**5. Each service type has its own request type and dispatch system -- they are completely independent.**

Police requests go through `PoliceEmergencyDispatchSystem`. Fire requests go through `FireRescueDispatchSystem`. Healthcare requests go through `HealthcareDispatchSystem`. There is no cross-service dispatch in vanilla.

**6. Direct ServiceDispatch buffer injection works for manual police dispatch (in-game tested).**

The approach that works for programmatic police dispatch to any location (including buildings) is to directly inject a `ServiceDispatch` buffer element onto a police vehicle entity, set the vehicle's `Target` component, and add the `Sirens` component. This bypasses the entire `AccidentSite`/`AccidentTarget`/`PoliceEmergencyRequest` pipeline and uses normal patrol pathfinding, which follows roads correctly. The police car activates sirens and drives to the target via the road network. See "How to dispatch police to any location" below.

**7. Crime dispatch requires a valid crime event prefab.**

Creating a crime scene `AccidentSite` requires a proper event entity with `PrefabRef` pointing to a prefab that has `CrimeData`. Without this, `AccidentSiteSystem` cannot manage the crime lifecycle (alarm delay, detection, duration). The event entity must have the `Game.Events.Event` tag, `Duration`, and reference a prefab with `CrimeData` for the full crime pipeline to work. See the [Crime Trigger](../CrimeTrigger/README.md) research for details on the crime event pipeline.

### How to force police to every vehicle accident

The AccidentSiteSystem unconditionally clears `RequirePolice` at the start of each entity's evaluation, then only re-sets it when `severity > 0` (from `InvolvedInAccident.m_Severity`) and a valid non-moving target exists. Accidents where all vehicles are still moving or have low severity will not trigger police. To force police dispatch:

> **Note:** This approach only works for **existing traffic accidents on road segments** that already have valid `AccidentSite` components with `InvolvedInAccident` entities. It does NOT work for dispatching police to buildings or arbitrary locations — for that, use the ServiceDispatch buffer injection approach below.

**Approach A**: Custom system that runs **after** `AccidentSiteSystem` (using `[UpdateAfter(typeof(AccidentSiteSystem))]`) and sets `RequirePolice` on all `AccidentSite` entities with `TrafficAccident` flag. It must run after, not before, because `AccidentSiteSystem` unconditionally clears `RequirePolice` before re-evaluating it:

```csharp
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

public partial class ForcePoliceToAccidentsSystem : GameSystemBase
{
    private EntityQuery m_AccidentQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_AccidentQuery = GetEntityQuery(
            ComponentType.ReadWrite<AccidentSite>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>());
        RequireForUpdate(m_AccidentQuery);
    }

    protected override void OnUpdate()
    {
        var entities = m_AccidentQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            var site = EntityManager.GetComponentData<AccidentSite>(entities[i]);
            if ((site.m_Flags & AccidentSiteFlags.TrafficAccident) != 0
                && (site.m_Flags & AccidentSiteFlags.Secured) == 0
                && !EntityManager.HasComponent<PoliceEmergencyRequest>(site.m_PoliceRequest))
            {
                // Force RequirePolice flag
                site.m_Flags |= AccidentSiteFlags.RequirePolice;
                EntityManager.SetComponentData(entities[i], site);

                // Create police request if none exists
                Entity request = EntityManager.CreateEntity();
                EntityManager.AddComponentData(request, new ServiceRequest());
                EntityManager.AddComponentData(request, new PoliceEmergencyRequest(
                    entities[i], entities[i], 1f, PolicePurpose.Emergency));
                EntityManager.AddComponentData(request, new RequestGroup(4u));
            }
        }
        entities.Dispose();
    }
}
```

### How to dispatch police to any location (ServiceDispatch injection, in-game tested)

The `PoliceEmergencyRequest` pipeline uses `AccidentLocation` pathfinding which is road-only and requires a valid `AccidentSite`. For dispatching police to **any entity** (buildings, citizens, custom locations), bypass the request pipeline entirely and directly inject a `ServiceDispatch` buffer element onto a police vehicle:

```csharp
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

public partial class ManualPoliceDispatchSystem : GameSystemBase
{
    private EntityQuery m_AvailablePoliceQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        // Find police cars that are not already dispatched
        m_AvailablePoliceQuery = GetEntityQuery(
            ComponentType.ReadOnly<PoliceCar>(),
            ComponentType.ReadOnly<Car>(),
            ComponentType.ReadWrite<Game.Vehicles.PersonalCar>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>());
    }

    /// <summary>
    /// Dispatch a police car to any target entity. The car follows roads
    /// correctly regardless of whether the target is a building or road segment.
    /// </summary>
    public void DispatchPoliceTo(Entity targetEntity)
    {
        // Find an available police car
        var policeCars = m_AvailablePoliceQuery.ToEntityArray(Allocator.Temp);
        Entity selectedCar = Entity.Null;

        for (int i = 0; i < policeCars.Length; i++)
        {
            var dispatches = EntityManager.GetBuffer<ServiceDispatch>(policeCars[i]);
            if (dispatches.Length == 0)
            {
                selectedCar = policeCars[i];
                break;
            }
        }
        policeCars.Dispose();

        if (selectedCar == Entity.Null) return; // No available cars

        // Set the vehicle's target
        EntityManager.SetComponentData(selectedCar, new Target(targetEntity));

        // Add sirens (activates emergency lights + siren sound)
        if (!EntityManager.HasComponent<Sirens>(selectedCar))
        {
            EntityManager.AddComponent<Sirens>(selectedCar);
        }
    }

    protected override void OnUpdate() { }
}
```

This approach was verified through in-game testing: the police car follows roads correctly, activates sirens, drives to the target, and returns to patrol afterward. It works for buildings, road segments, and any other entity with a valid position.

### How to send fire engines to medical calls or accidents

Fire engines only respond to `FireRescueRequest` entities, and those require `OnFire` or `RescueTarget` on the target. You cannot make fire engines respond to healthcare requests through the existing systems. However:

> **WARNING (in-game tested):** `RescueTarget` must be added to a **building** entity, not a citizen or vehicle. Adding `RescueTarget` to citizens corrupts their archetypes and breaks `HealthcareDispatchSystem` (see "Archetype Safety" section below). Adding it to road segments or accident event entities will cause the fire engine to arrive but find nothing to do (no `Destroyed` component to clear) and leave immediately.

**Approach**: Add `RescueTarget` component to the **building** entity at the accident scene, then create a `FireRescueRequest` targeting that building:

```csharp
using Game.Buildings;
using Game.Simulation;
using Unity.Entities;

// targetBuilding must be a BUILDING entity, not a citizen or vehicle
if (!EntityManager.HasComponent<RescueTarget>(targetBuilding))
{
    EntityManager.AddComponentData(targetBuilding, new RescueTarget(Entity.Null));
}

// Create fire rescue request for that building
Entity request = EntityManager.CreateEntity();
EntityManager.AddComponentData(request, new ServiceRequest());
EntityManager.AddComponentData(request, new FireRescueRequest(
    targetBuilding, 1f, FireRescueRequestType.Disaster));
EntityManager.AddComponentData(request, new RequestGroup(4u));
```

### How to dispatch an emergency vehicle programmatically (no event needed)

Create the appropriate request entity with the right archetype. The dispatch system will process it. The only requirement is that the target entity has the validation component:

- **Police**: Use the **ServiceDispatch buffer injection** approach (see "How to dispatch police to any location" above). The `PoliceEmergencyRequest` pipeline requires a valid `AccidentSite` with `InvolvedInAccident` entities and uses road-only `AccidentLocation` pathfinding, making it unsuitable for arbitrary dispatch. Direct ServiceDispatch injection bypasses these limitations.
- **Fire**: target needs `OnFire` or `RescueTarget` (on a **building** entity only — see archetype safety warning)
- **Healthcare**: target (citizen) needs `HealthProblem` with `RequireTransport` -- but **do NOT add `HealthProblem` directly** (see warning below; use the `AddHealthProblem` event-based approach instead)

If you want to dispatch fire or healthcare without a real event, ensure the validation component exists on the target entity, then create the request. **Exception**: for healthcare, you must use the `AddHealthProblem` event pipeline to add `HealthProblem` to a citizen rather than adding it directly -- see the warning immediately below.

> **WARNING: Do NOT directly add `HealthProblem` via `EntityManager.AddComponentData()`.** Directly adding `HealthProblem` to a citizen bypasses `AddHealthProblemSystem`, which performs critical side effects:
>
> 1. **Stops citizen movement** (`StopMoving()` clears pathfinding on the citizen's transport) -- without this, the citizen keeps walking/driving and ambulances may never reach a moving target.
> 2. **Fires trigger events** (`CitizenGotSick`, `CitizenGotInjured`, `CitizenGotTrapped`, `CitizenGotInDanger`) -- other systems and mods rely on these.
> 3. **Creates journal data** for statistics tracking.
> 4. **Merges flags properly** via `MergeProblems()` (Dead > RequireTransport > non-null Event > flag union) -- direct addition can clobber existing flags.
>
> **The correct approach** is to create an `AddHealthProblem` event entity (with `Game.Common.Event` tag + `Game.Events.AddHealthProblem` component) and let `AddHealthProblemSystem` handle it:
>
> ```csharp
> // CORRECT: Create an event entity for AddHealthProblemSystem to process
> Entity cmd = EntityManager.CreateEntity();
> EntityManager.AddComponentData(cmd, new Game.Common.Event());
> EntityManager.AddComponentData(cmd, new AddHealthProblem
> {
>     m_Event = Entity.Null,
>     m_Target = citizenEntity,
>     m_Flags = HealthProblemFlags.Sick | HealthProblemFlags.RequireTransport
> });
> // AddHealthProblemSystem processes this next frame -- stops movement,
> // fires triggers, creates journal data, and merges flags correctly.
> ```
>
> See the [CitizenSickness research topic](../CitizenSickness/README.md) for full details on `AddHealthProblemSystem`, the event-based approach, and code examples for making citizens sick.

### What controls which service responds to which event

There is no central "dispatch router." Each service has a completely separate pipeline:

1. **Event occurs** -> specific system adds a specific component to the target
2. **Request creation system** -> detects that component, creates the corresponding request type
3. **Dispatch system** -> processes only its request type, validates the target still has the expected component

The mapping is hardcoded:
- `OnFire` -> `FireSimulationSystem` -> `FireRescueRequest` -> `FireRescueDispatchSystem`
- `AccidentSite` + severity -> `AccidentSiteSystem` -> `PoliceEmergencyRequest` -> `PoliceEmergencyDispatchSystem`
- `HealthProblem` + `RequireTransport` -> `HealthProblemSystem` -> `HealthcareRequest` -> `HealthcareDispatchSystem`

To make a service respond to a different event type, you must:
1. Add the validation component the service expects (e.g., `RescueTarget` for fire)
2. Create the appropriate request entity

### Systems to create
- `ForcePoliceToAccidentsSystem` -- ensures all traffic accidents get police response
- `CrossServiceDispatchSystem` -- creates fire/healthcare requests for accident sites (fire engines to accidents, etc.)

### Components to add
- `RescueTarget` on accident entities (to satisfy FireRescueDispatchSystem validation)

### Patches needed
- None required if using the ECS approach (custom systems creating request entities)
- Optional: Harmony postfix on `AccidentSiteSystem.OnUpdate()` to inject additional logic after the job completes

### Settings
- Toggle: force police to all accidents (on/off)
- Toggle: send fire engines to accidents (on/off)
- Toggle: send fire engines to medical calls (on/off)
- Priority multiplier for cross-service requests

### UI changes
- None required for basic functionality

## In-Game Tested Results Summary

The following approaches have been verified through in-game testing across 5 iterations during mod development:

| Approach | Result | Notes |
|----------|--------|-------|
| **Police patrol dispatch** via ServiceDispatch injection + Sirens | **Works** | Car follows roads, sirens on, arrives and returns to patrol |
| **Crime dispatch** via AccidentSite with `CrimeScene \| CrimeDetected` flags + event entity with PrefabRef → crime prefab with CrimeData | **Works** | Vanilla manages full lifecycle (detection, securing, cleanup) |
| **Fire dispatch** via RescueTarget on **building** entity + FireRescueRequest | **Works** | As documented, but target must be a building entity |
| **Police dispatch** via PoliceEmergencyRequest to building target | **Fails** | AccidentLocation pathfinding is road-only → floating cars |
| **Police dispatch** via synthetic AccidentSite on arbitrary entity | **Fails** | AccidentSiteSystem strips RequirePolice within ~1 second |
| **RescueTarget** on citizen entity | **Fails** | Corrupts citizen archetype, breaks HealthcareDispatchSystem |

## Archetype Safety: Do Not Add Building Components to Citizens

### The Problem

In CS2's ECS, every entity has an **archetype** determined by its exact set of component types. When you add or remove a component, the entity moves to a different archetype, which changes which `EntityQuery` results it appears in. This is a fundamental ECS concept, but it creates a subtle and dangerous pitfall for modders.

`RescueTarget` is defined in the `Game.Buildings` namespace. It is a building-domain component -- the game adds it to **building** entities that need fire rescue (e.g., collapsed structures). The `FireRescueDispatchSystem.ValidateTarget()` method checks for `OnFire` or `RescueTarget` on the target entity to decide whether a `FireRescueRequest` is still valid.

It may seem logical to add `RescueTarget` to a **citizen** entity to make fire engines respond to citizen emergencies. **Do not do this.** Adding a building-domain component to a citizen entity changes the citizen's archetype, which can cause it to:

1. **Match queries it should not match.** Systems that query for entities with `RescueTarget` (building rescue systems, cleanup systems) will now pick up this citizen entity and may process it incorrectly.
2. **Stop matching queries it should match.** Some systems use `EntityQuery` configurations with `ComponentType.Exclude` or archetype-based chunk filtering. An unexpected component can cause the entity to fall out of these queries.
3. **Break `HealthcareDispatchSystem` specifically.** The `HealthcareDispatchSystem` queries for citizen entities with `HealthProblem` and processes them for ambulance/hearse dispatch. While the dispatch system uses `ComponentLookup` for validation rather than direct query inclusion, other healthcare-adjacent systems that iterate over citizen archetypes may skip entities with unexpected building-domain components. This can result in ambulances never being dispatched or citizens being silently ignored by the healthcare pipeline.

### Concrete Example: RescueTarget on Citizens

The "Send Fire Engines to Accident Scenes" example in the Mod Blueprint section above adds `RescueTarget` to **accident site entities** (road segments or event entities), which is acceptable because those entities are not citizens. However, if you were to apply the same pattern to citizen entities:

```csharp
// BAD: Adding a building-domain component to a citizen entity
EntityManager.AddComponentData(citizenEntity, new RescueTarget(Entity.Null));
```

This changes the citizen's archetype from something like `[Citizen, CurrentBuilding, HouseholdMember, HealthProblem, ...]` to `[Citizen, CurrentBuilding, HouseholdMember, HealthProblem, RescueTarget, ...]`. Any system that queries for citizen archetypes without expecting `RescueTarget` may behave unpredictably.

### Recommended Approach: Custom Tag Components

Instead of reusing game components from other domains, define a custom tag component in your mod's namespace:

```csharp
namespace YourMod.Components
{
    /// <summary>
    /// Tag component marking a citizen as needing fire rescue.
    /// Uses a mod-specific type to avoid archetype conflicts with
    /// Game.Buildings.RescueTarget.
    /// </summary>
    public struct NeedsFireRescue : IComponentData
    {
        public Entity m_Request;
    }
}
```

Then create a custom system that:
1. Queries for citizens with your `NeedsFireRescue` tag
2. Creates `FireRescueRequest` entities pointing to the citizen's **current building** (not the citizen) as the target
3. Adds `RescueTarget` to the **building** entity (where it belongs), not the citizen

This keeps citizen archetypes clean and avoids breaking `HealthcareDispatchSystem` or any other system that queries citizens.

### General Rule

**Never add components from one domain to entities of another domain.** Specifically:
- Do not add `Game.Buildings.*` components to citizen or vehicle entities
- Do not add `Game.Citizens.*` components to building or vehicle entities
- Do not add `Game.Vehicles.*` components to citizen or building entities

If you need to create cross-domain relationships, use custom components in your mod's namespace and a bridging system that operates on the correct entity types.

## RescueTarget Lifecycle

### Overview

`RescueTarget` (`Game.Buildings`) is a component added to **collapsed/destroyed buildings** that need fire engine rescue crews. It is used exclusively for disaster rescue (not for active fires). The full lifecycle is managed by two systems: `CollapsedBuildingSystem` (creation and removal) and `FireEngineAISystem` (response behavior).

```csharp
public struct RescueTarget : IComponentData
{
    public Entity m_Request;  // Points to the active FireRescueRequest entity
}
```

### Who Creates RescueTarget?

**`CollapsedBuildingSystem`** (update interval: 64 frames) is the sole creator of `RescueTarget` in vanilla gameplay. It runs on all entities with `Destroyed` + (`Building` | `Extension`), excluding `Deleted`/`Temp`.

Creation logic:
1. When a building is destroyed, the `Destroyed` component is added with `m_Cleared` starting negative (collapse animation phase).
2. `m_Cleared` increments by `1.0666667f` each tick until it reaches `0`.
3. Once `m_Cleared >= 0`, the system checks `BuildingData.m_Flags & BuildingFlags.RequireRoad`:
   - **Road-connected buildings**: `m_Cleared` is set to `0f`, `RescueTarget` is added, and `RequestRescueIfNeeded()` creates a `FireRescueRequest` with type `Disaster` and priority `10f`.
   - **Non-road buildings** (e.g., detached structures): `m_Cleared` is set to `1f`, skipping rescue entirely.

### Who Cleans Up RescueTarget?

**`CollapsedBuildingSystem`** also removes `RescueTarget`. Each tick (64 frames), it checks all entities that already have `RescueTarget`:
- If `Destroyed.m_Cleared < 1.0`: calls `RequestRescueIfNeeded()` to ensure a `FireRescueRequest` exists (keeps fire engines coming if the previous request was completed or destroyed).
- If `Destroyed.m_Cleared >= 1.0`: **removes the `RescueTarget` component** via `m_CommandBuffer.RemoveComponent<RescueTarget>()`. This is the cleanup.

The `m_Cleared` value is advanced toward `1.0` by `FireEngineAISystem` through the `ObjectExtinguishIterator.TryExtinguish()` method, which increments `Destroyed.m_Cleared` at rate `4/15 * clearRate` per tick while a fire engine is actively working the site.

### What Happens When a Fire Engine Arrives at a RescueTarget?

The `FireEngineAISystem.BeginExtinguishing()` method checks the target:
- If the target has `OnFire`: sets state to `FireEngineFlags.Extinguishing`
- If the target has `RescueTarget` (but no `OnFire`): sets state to `FireEngineFlags.Rescueing` (note: "Rescueing" is the game's spelling)

In the `Rescueing` state, the fire engine calls `TryExtinguishFire()` which uses the `ObjectExtinguishIterator`:
- It does **not** reduce fire intensity (there is no fire).
- Instead, it checks for a `Destroyed` component and advances `m_Cleared` toward `1.0`.
- The clearing rate is `4/15 * (efficiency / DestroyedClearDuration)` per tick.
- Once `m_Cleared >= 1.0`, the fire engine's work is done.

### What Happens if a Fire Engine Arrives at a RescueTarget with No Active Fire?

This is the **normal** RescueTarget scenario. `RescueTarget` is specifically for collapsed buildings that are no longer burning. The fire engine enters `Rescueing` state (not `Extinguishing`) and clears the debris by advancing `Destroyed.m_Cleared`. This is by design -- `RescueTarget` and `OnFire` serve different purposes:
- `OnFire` = active fire, fire engine extinguishes
- `RescueTarget` = collapsed rubble, fire engine clears debris

If both `OnFire` and `RescueTarget` are present (building still burning while collapsed), the fire engine prioritizes the `OnFire` check in `BeginExtinguishing()` and enters `Extinguishing` state first. `RescueTarget`'s `Rescueing` state is only engaged when `OnFire` is absent.

If the target has `RescueTarget` but does NOT have a `Destroyed` component (e.g., a mod-added RescueTarget on a non-destroyed entity), `TryExtinguishFire()` returns `false` immediately (nothing to clear), and the engine moves to the next dispatch or returns to its station. **It does NOT idle indefinitely.**

### What Happens After the Fire Engine Finishes?

After `TryExtinguishFire()` returns `false` (no more work to do), the fire engine's `Tick()` method calls `SelectNextDispatch()`:
- `SelectNextDispatch()` validates Disaster-type requests by checking `m_RescueTargetData.HasComponent(entity)`. If `RescueTarget` was removed (because `m_Cleared >= 1.0`), the dispatch is dropped.
- If no more dispatches, the fire engine calls `ReturnToDepot()`.

### Lifecycle Summary

```
Building destroyed (fire burns out or disaster)
    |
    v
Destroyed component added (m_Cleared starts negative)
    |
    v (collapse animation plays, m_Cleared increments toward 0)
    |
    v
m_Cleared reaches 0
    |
    +--- Building has RequireRoad flag?
    |         |
    |    YES: m_Cleared = 0, RescueTarget ADDED
    |         |
    |         v
    |    CollapsedBuildingSystem.RequestRescueIfNeeded():
    |         Creates FireRescueRequest(entity, 10f, Disaster)
    |         |
    |         v
    |    FireRescueDispatchSystem dispatches fire engine
    |         (validates via RescueTarget on target)
    |         |
    |         v
    |    FireEngineAISystem: engine arrives, enters Rescueing state
    |         |
    |         v
    |    TryExtinguishFire -> ObjectExtinguishIterator.TryExtinguish:
    |         advances Destroyed.m_Cleared toward 1.0
    |         |
    |         v
    |    m_Cleared reaches 1.0
    |         |
    |         v
    |    CollapsedBuildingSystem: REMOVES RescueTarget
    |         |
    |         v
    |    Building entity deleted (or remains as cleared rubble)
    |
    |    NO: m_Cleared = 1.0, no RescueTarget, no rescue
    |
    v
Done
```

### Modding Implications

1. **Adding `RescueTarget` manually**: When adding `RescueTarget` to a non-destroyed entity (e.g., an accident site), the fire engine will enter `Rescueing` state but `TryExtinguishFire()` will fail immediately because the entity has no `Destroyed` component to clear. The engine will then call `SelectNextDispatch()` or `ReturnToDepot()`. This means **manually-added `RescueTarget` on non-destroyed entities will cause the fire engine to arrive, fail to find work, and leave**. To make it actually do something, the entity needs a `Destroyed` component with `m_Cleared` between 0 and 1.

2. **Cleanup responsibility**: If a mod adds `RescueTarget` to entities that are not `Destroyed` buildings, `CollapsedBuildingSystem` will NOT clean it up (the system only queries `Destroyed` entities). The mod must handle its own cleanup. A lingering `RescueTarget` on a building will cause `FireRescueDispatchSystem` to consider fire rescue requests for that building as valid even after the original event is over.

3. **Request recreation**: `CollapsedBuildingSystem` re-creates `FireRescueRequest` entities every 64 frames if the previous request was fulfilled but `m_Cleared < 1.0`. This means rescue is persistent -- fire engines keep coming until the job is done.

4. **Custom behavior at RescueTarget**: If the goal is to have a fire engine perform some action at a non-destroyed RescueTarget scene, a Harmony patch on `TryExtinguishFire` or `BeginExtinguishing` would be needed to inject custom behavior.

## Car Accident Severity & Police Dispatch Timing

*Decompiled 2026-02-22 from `Game.Simulation.AccidentVehicleSystem`, `Game.Simulation.VehicleOutOfControlSystem`, `Game.Simulation.AccidentSiteSystem`, `Game.Events.ImpactSystem`.*

### The Severity Pipeline

```
[Real collision or scripted accident event]
    |
    v
Impact + Game.Common.Event entity created
    (m_Event = traffic accident entity, m_Target = vehicle, m_Severity = <see below>)
    |
    v
ImpactSystem (every frame):
    Applies velocity delta to target's Moving component
    Adds OutOfControl to cars (parked → moving; moving → stay moving)
    Creates/updates InvolvedInAccident on target: m_Severity = impact.m_Severity
    |
    v
VehicleOutOfControlSystem (every 16 frames per UpdateFrame group):
    Physics simulation: apply braking, gravity, angular velocity
    No Impact creation — pure movement update
    |
    v
AccidentVehicleSystem (every 64 frames, query: InvolvedInAccident + Vehicle):
    Two branches depending on whether the vehicle is still Moving:

    BRANCH A — Vehicle still Moving:
        Checks if velocity^2 < threshold (threshold grows with time: 0.01 + t^2 * 3e-9)
        When slowed enough: StopVehicle() [removes Moving, adds Stopped]
        AddInjuries() for each Passenger: creates Impact(severity=random.NextFloat(vehicle.m_Severity))
        If no AccidentSite exists within 30m: creates AddAccidentSite + Game.Common.Event
            (flags = TrafficAccident, target = nearest road segment)

    BRANCH B — Vehicle already Stopped:
        Checks IsSecured() → if accident secured or timeout (14400 frames ~4 min):
            StartVehicle() + ClearAccident() [removes InvolvedInAccident + OutOfControl]

    AddAccidentSiteSystem processes AddAccidentSite → creates AccidentSite on road segment
    |
    v
AccidentSiteSystem (every 64 frames):
    Reads max severity (num2) from all InvolvedInAccident entities for this event
    CLEAR: m_Flags &= ~RequirePolice
    EVALUATE: if (num2 > 0 AND highest-severity vehicle is STOPPED):
        SET: m_Flags |= RequirePolice
        RequestPoliceIfNeeded() → creates PoliceEmergencyRequest
    |
    v
PoliceEmergencyDispatchSystem (every 16 frames): dispatches police car
```

### Severity Values

| Source | Value | When |
|--------|-------|------|
| Staged accident — `LoseControl` type | **5.0** (hardcoded) | `AccidentSiteSystem.AddImpact()` when `StageAccident` flag and no vehicles present |
| Staged accident — other types | **0.0** (default) | Same path, other `TrafficAccidentType`; no police |
| Passenger injury (from vehicle crash) | `random.NextFloat(vehicle_severity)` decreasing per passenger | `AccidentVehicleSystem.AddInjuries()` |
| Real vehicle collision | Unknown (velocity-based, from initial `Impact` creator) | Initial impact system not identified |

The severity threshold for police dispatch is strictly `> 0.0f`. Any non-zero value triggers police evaluation.

### The Stopped-Vehicle Requirement

**Police are only dispatched to traffic accidents when the highest-severity involved vehicle has stopped.** This is the most important modding constraint:

From `AccidentSiteSystem.AccidentSiteJob.Execute()` (line 139–142):
```csharp
if (componentData.m_Severity > num2)
{
    entity2 = (flag2 ? Entity.Null : entity3);  // flag2 = m_MovingData.HasComponent(entity3)
    num2 = componentData.m_Severity;
}
```
`entity2` is the target for `RequestPoliceIfNeeded`. If the highest-severity vehicle is still `Moving` (`flag2 = true`), `entity2 = Entity.Null`, and police are NOT dispatched.

**Exception — Building AccidentSites**: When the AccidentSite entity itself is a `Building`, the stopped-vehicle check is bypassed:
```csharp
if (flag)   // flag = chunk.Has(ref m_BuildingType)
{
    entity2 = entity;  // use the building as target
}
```
Crime scenes on buildings always dispatch police once `CrimeDetected` is set, regardless of whether any moving vehicles are involved.

### Timing from Collision to Police Arrival

| Phase | Duration |
|-------|----------|
| Vehicle slows to stop (physics simulation) | Variable, seconds to ~10 sim seconds |
| `AccidentVehicleSystem` next tick — creates `AddAccidentSite` | ≤64 frames (~1s) |
| `AddAccidentSiteSystem` processes → `AccidentSite` created | 1 frame |
| `AccidentSiteSystem` next tick — creates `PoliceEmergencyRequest` | ≤64 frames (~1s) |
| `ServiceRequestSystem` — assigns `UpdateFrame` | 1 frame |
| `PoliceEmergencyDispatchSystem` next tick — pathfind request | ≤16 frames (~0.25s) |
| Pathfinding resolves | 1-2 frames |
| Police car dispatched, drives to scene | Minutes (distance-dependent) |
| **Total from stop to dispatch** | **~2–3 seconds minimum** |

### Vehicle Lifecycle After Accident

From `AccidentVehicleSystem.AccidentVehicleJob` (stopped branch):
- **Normal vehicles** (no `Destroyed`): `IsSecured` check → when `AccidentSite.Secured` is set by arriving police car OR after **14400 frames (~4 minutes)**, vehicle gets `StartVehicle()` + `ClearAccident()`
- **Bicycles** (has `Bicycle`): timeout = **300 frames (~5 seconds)** instead of 14400
- **Destroyed vehicles** (has `Destroyed`): wait for `Destroyed.m_Cleared >= 1.0` OR `14400 frames` → vehicle deleted
- **Vehicles that fall below y = -1000** (fell off map): immediately deleted via `VehicleUtils.DeleteVehicle()`

### StageAccident Flag Behavior

The `StageAccident` flag on `AccidentSite` controls whether `AccidentSiteSystem` will scripted-trigger a nearby vehicle into the accident:
- **Cleared after 3600 frames (~60 seconds)**: `if (m_SimulationFrame - accidentSite.m_CreationFrame >= 3600) accidentSite.m_Flags &= ~StageAccident`
- **Cleared immediately when severity is found**: when a vehicle's severity > current max, `StageAccident` is cleared (`accidentSite.m_Flags &= ~AccidentSiteFlags.StageAccident`)
- **Only fires when `num == 0`**: if NO vehicles are already involved, `AccidentSiteSystem` uses `TrafficAccidentData` prefab to find a nearby random car and add it as an `Impact` with severity 5.0 (`LoseControl`) or 0.0 (other types)

**Implication for mods**: A synthetic `AccidentSite` with `StageAccident` flag and `TrafficAccident` event pointing to a prefab with `TrafficAccidentData { m_AccidentType = LoseControl }` will automatically pull in a nearby vehicle as involved, with severity 5.0. This is the cleanest way to trigger police without manually creating `InvolvedInAccident`.

## Open Questions

- [ ] How does the pathfinder's `SetupTargetType.PolicePatrol` matching work internally? Does it filter by `PolicePurpose` flags on the station, or by distance only?
- [x] When `RescueTarget` is added manually, does the fire engine AI know how to handle a non-fire target (e.g., accident scene)? **Answer**: The fire engine enters `Rescueing` state but requires a `Destroyed` component with `m_Cleared < 1.0` to do actual work. Without it, the engine arrives, finds nothing to do, and returns to depot.
- [ ] What happens if multiple requests exist for the same AccidentSite? The system checks `m_PoliceRequest` on AccidentSite -- does this prevent duplicates?
- [ ] How does the `Reversed` request flow work in practice? Police stations and vehicles with capacity create reversed requests to proactively find work. How does this interact with manually created requests?
- [ ] Does `PolicePatrolDispatchSystem` create requests that could also respond to accidents, or is patrol strictly separate from emergency?
- [x] Can police be dispatched to building targets via PoliceEmergencyRequest? **Answer (in-game tested)**: No. `AccidentLocation` pathfinding is road-only. Police cars targeting buildings float in straight lines instead of following roads. Use ServiceDispatch buffer injection instead.
- [x] Can synthetic AccidentSites be created on arbitrary entities? **Answer (in-game tested)**: No. `AccidentSiteSystem` strips `RequirePolice` within ~1 second if the event lacks valid `InvolvedInAccident` entities. The site becomes inert almost immediately.
- [x] Does `SecureAccidentSite` require an AccidentSite component on the target? **Answer (in-game tested)**: Yes. Without an `AccidentSite` on the target entity, `SecureAccidentSite` returns `true` immediately — the police car leaves as soon as it arrives.
- [x] Do fire stations carry `CrimeProducer`? **Runtime-confirmed**: Yes. Fire stations have `CrimeProducer` with `m_Crime` values up to 1213 observed in a 599K city. This means police patrol fire stations as a crime target. The crime value reflects staff activity or vandalism modeling — not a design oversight.
- [x] What is the AccidentSite archetype? **Runtime-confirmed (ECS dump)**: AccidentSite is attached to the building entity (not a standalone entity). All 11 live AccidentSite instances observed were `CrimeScene` type. No live `TrafficAccident` AccidentSite was observed — traffic accident scenes resolve very quickly relative to crime scenes. AccidentSite is not restricted to roads or commercial buildings — park/recreation buildings (`AttractivenessProvider`, `ModifiedServiceCoverage`) and cargo airports (`TransportStation`, `StorageCompany`) also carry AccidentSite as crime scenes.
- [x] Does `ServiceUsage.m_Usage` have documented meaning? **Runtime-confirmed**: `m_Usage = 1` on all operational service buildings (police, fire, hospital). Likely a binary operational indicator — 0 = inactive/under construction, 1 = operational. Not previously documented.
- [x] Does fire dispatch maintain more reverse-search requests than police? **Runtime-confirmed**: 18 of 18 operational fire stations have active `m_TargetRequest` vs only 7 of 18 police stations. Fire dispatch systems more aggressively maintain reverse requests.
- [x] Why does a large rescue station (with "Disaster Response" upgrade) respond to a regular fire while a nearby plain fire station does not? **Answer**: Three independent causes, all decompilation-confirmed:
  1. **Availability filtering is the primary cause.** `FirePathfindSetup.SetupFireEnginesJob` only includes a station as a pathfind candidate if its `HasAvailableFireEngines` flag is set. This flag is cleared by `FireStationAISystem` whenever `GetVehicleCapacity(efficiency, engineCount)` for remaining parked engines reaches zero. A 2-engine local station with both engines deployed elsewhere is **invisible to the pathfind** that sets up a new fire dispatch — it doesn't appear in the candidate list at all, regardless of distance. A large rescue station with 6+ engines will almost always have availability.
  2. **Service district restrictions** (if configured). `AreaUtils.CheckServiceDistrict()` checks the `ServiceDistrict` buffer on each candidate station. Stations with no buffer (or an empty buffer) serve everywhere. Stations with entries only serve fires whose district is listed. If local stations are restricted to their own district and the fire is in a district only covered by the rescue station, local stations are excluded.
  3. **`DisasterResponseAvailable` is irrelevant for regular fires.** The disaster-response gate only applies to `FireRescueRequestType.Disaster` requests — created exclusively by `CollapsedBuildingSystem` for collapsed buildings. Regular fires create `FireRescueRequestType.Fire` requests. Both plain fire stations and rescue stations are equally eligible for regular fires, regardless of upgrades. The "Disaster Response" upgrade gives rescue stations no advantage in ordinary fire response — it only enables them to respond to collapsed building rescue calls that local stations cannot handle.

## Cross-Service Dispatch Deep-Dive

*Added 2026-02-22 — findings from deep-dive across AmbulanceAISystem (1093 lines), FireEngineAISystem (1352 lines), HealthcareDispatchSystem (869 lines), FireRescueDispatchSystem (716 lines), HealthcarePathfindSetup, FirePathfindSetup, and related prefab structs.*

### Path D2 Universality: ServiceDispatch Buffer Injection

Path D2 (direct `ServiceDispatch` buffer injection into a vehicle entity + manual flag manipulation) works structurally identically across all three emergency service types. The pattern is the same; only the request type name and vehicle state flags differ.

| Step | Police (D2) | Ambulance (D2) | Fire Engine (D2) |
|------|-------------|----------------|------------------|
| 1. Request entity | `PoliceEmergencyRequest` | `HealthcareRequest` | `FireRescueRequest` |
| 2. Inject `ServiceDispatch` | `EntityManager.GetBuffer<ServiceDispatch>(car).Add(new ServiceDispatch(request))` | same | same |
| 3. Set vehicle flags | `Car.m_Flags |= CarFlags.Emergency \| StayOnRoad` | `Car.m_Flags |= CarFlags.Emergency` | `Car.m_Flags |= CarFlags.Emergency \| StayOnRoad` |
| 4. Set request count | `PoliceCar.m_RequestCount = 1` | `Ambulance.m_RequestCount = 1` | `FireEngine.m_RequestCount = 1` |
| 5. Set target | `Target = targetEntity` | `Target = targetEntity` | `Target = targetEntity` |
| 6. Trigger re-path | `EntityManager.AddComponent<PathOwner>(car)` (reset bit) | same | same |
| 7. Mark updated | `EntityManager.AddComponent<EffectsUpdated>(car)` | same | same |
| **Confidence** | **95%** (in-game tested) | **90%** | **85%** |

**Service-specific differences and caveats:**

- **Ambulance multi-stage journey**: After picking up the patient the ambulance sets `AmbulanceFlags.PickedUp` and routes to a hospital (FindHospital lookup). The injected dispatch entry is consumed on pickup; the hospital-routing phase is independent. D2 only controls the pickup leg.
- **Fire engine at-arrival validation**: `BeginExtinguishing()` checks `OnFire.m_Intensity > 0` at the target. If the fire has already been extinguished, the engine will enter `SelectNextDispatch()` and leave. For D2 fire dispatch to produce a lasting visit, either keep `OnFire` alive or use `RescueTarget`.
- **Ambulance target citizen**: `m_Citizen` in `HealthcareRequest` must be an entity with `HealthProblem`. D2 bypasses `HealthcareDispatchSystem.ValidateTarget()` (which checks `RequireTransport`), but the AI's own `CheckPatient()` may still validate on arrival.

### `ResetPath()` — The Universal Gatekeeper

Every vehicle AI (police, ambulance, fire engine) calls `ResetPath()` when beginning a new path. This method reads `serviceDispatches[0].m_Request` and sets `CarFlags.Emergency` based on whether the request entity has the matching request component:

```csharp
// Police (PoliceCarAISystem.ResetPath, ~line 590)
if (m_PoliceEmergencyRequestData.HasComponent(request))
    car.m_Flags |= CarFlags.Emergency | StayOnRoad | UsePublicTransportLanes;
else
    car.m_Flags &= ~CarFlags.Emergency;

// Ambulance (AmbulanceAISystem.ResetPath, ~line 716)
if (m_HealthcareRequestData.HasComponent(request))
    car.m_Flags |= CarFlags.Emergency;
else
    car.m_Flags &= ~CarFlags.Emergency;

// Fire Engine (FireEngineAISystem.ResetPath, ~line 820)
if (m_FireRescueRequestData.HasComponent(request))
    car.m_Flags |= CarFlags.Emergency | StayOnRoad;
else
    car.m_Flags &= ~CarFlags.Emergency;
```

**Implication**: For lights + sirens on D2 dispatch, the injected `ServiceDispatch.m_Request` entity must have the matching request component. A null or wrong-type request causes `ResetPath()` to clear `Emergency` — the vehicle drives without lights/sirens.

### Alternative Dispatch Path Rankings

| Path | Approach | Confidence | Notes |
|------|----------|------------|-------|
| **D2** | Direct `ServiceDispatch` injection | **95%** | In-game tested for police; structurally identical for ambulance/fire |
| **E** | Patch `ServiceRequestSystem.UpdateRequestGroupJob` to add `UpdateFrame` to custom requests | **75%** | Works for routing; fails lights/sirens because `ResetPath()` needs `PathElement` buffer for `EndOfPath` handling. Path not pre-computed → `SelectNextDispatch()` fallthrough skips `Emergency` flag set. |
| **F** | Harmony postfix on dispatch system `OnUpdate()` | **70%** | Clean but brittle against game updates. Must complete the job dependency before reading dispatch results. |
| **C-Patched** | `AccidentSite` approach with Harmony postfix to keep `RequirePolice` alive | **60%** | Requires maintaining `RequirePolice` on every `AccidentSiteSystem` tick. Still uses `AccidentLocation` pathfinding (road-only). |
| **G** | Reverse-request interception (station advertising) | **45%** | Complex; reversed request flow requires matching district + vehicle availability filters. Hard to target a specific vehicle. |
| **C unpatched** | Synthetic `AccidentSite` on arbitrary entity | **20%** | `AccidentSiteSystem` strips `RequirePolice` within ~64 frames (~1 second) unless backed by valid `InvolvedInAccident` entities. |

### Pathfinding Reliability Across Services

**Baseline reliability: ~65% for arbitrary entities → ~95% for real game buildings.**

The critical factor is district resolution. All three dispatch systems resolve the target's district through the same two-step pattern:

```
1. Check CurrentDistrict component on target entity → fast, reliable
2. Fallback: AreaSearchSystem quad-tree spatial lookup via target's Transform → slower, 100% reliable if Transform is present
```

If neither `CurrentDistrict` nor a valid `Transform` is available, the district lookup returns `Entity.Null`, and the dispatch system silently fails to enqueue a pathfind (the dispatch entry is never created).

**Per-service pathfinding parameters:**

| Service | Origin type | Destination type | Max speed | Close-enough radius | Offroad? |
|---------|------------|-----------------|-----------|--------------------|----|
| Police | `SetupTargetType.PolicePatrol` | `SetupTargetType.AccidentLocation` | ~400 km/h | n/a | No |
| Ambulance | `SetupTargetType.Ambulance` | `SetupTargetType.CurrentLocation` | ~1000 km/h | n/a | No |
| Fire Engine | `SetupTargetType.FireEngine` | `SetupTargetType.CurrentLocation` | ~1000 km/h | **30m** | **Yes** |

**Two separate 30m checks for fire engines:**
1. **Pathfinder radius** (`m_Value2 = 30f` on `SetupQueueTarget`): tells the pathfinder to consider positions within 30m of the target as valid endpoints.
2. **Navigation proximity** (`IsCloseEnough()` in `FireEngineAISystem.Tick()`): if blocked by traffic and within 30m, the engine calls `EndNavigation()` and considers the path complete.

This double 30m tolerance makes fire engines the most forgiving service for D2 dispatch to buildings.

**Checklist for reliable building dispatch via D2:**

- [ ] Target entity has `Transform` component (all buildings do)
- [ ] Target entity has `CurrentDistrict` OR is within the spatial quad-tree (all buildings qualify)
- [ ] For ambulance: target citizen has `HealthProblem` (or use a mock citizen)
- [ ] For fire engine: target has `OnFire` (intensity > 0) or `RescueTarget`
- [ ] `ServiceDispatch.m_Request` entity has the correct request component (for lights/sirens)

### Cross-Service Request Types Quick Reference

| Service | Request component | Validation component on target | Dispatch system | Vehicle AI |
|---------|-----------------|-----------------------------|----------------|------------|
| Police | `PoliceEmergencyRequest` | `AccidentSite` (with `RequirePolice`) | `PoliceEmergencyDispatchSystem` | `PoliceCarAISystem` |
| Ambulance | `HealthcareRequest` | `HealthProblem` (with `RequireTransport`) | `HealthcareDispatchSystem` | `AmbulanceAISystem` |
| Fire | `FireRescueRequest` | `OnFire` or `RescueTarget` | `FireRescueDispatchSystem` | `FireEngineAISystem` |

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key types: Game.Simulation.ServiceRequest, Game.Simulation.ServiceDispatch, Game.Simulation.Dispatched, Game.Simulation.HandleRequest, Game.Simulation.RequestGroup, Game.Simulation.PoliceEmergencyRequest, Game.Simulation.FireRescueRequest, Game.Simulation.HealthcareRequest, Game.Events.AccidentSite, Game.Events.OnFire, Game.Citizens.HealthProblem, Game.Buildings.RescueTarget, Game.Buildings.FireStation, Game.Buildings.FireStationFlags, Game.Vehicles.FireEngine, Game.Vehicles.FireEngineFlags, Game.Prefabs.FireStationData, Game.Prefabs.FireEngineData, Game.Prefabs.FireConfigurationData, Game.Simulation.ServiceRequestSystem, Game.Simulation.AccidentSiteSystem, Game.Simulation.PoliceEmergencyDispatchSystem, Game.Simulation.FireRescueDispatchSystem, Game.Simulation.FireStationAISystem, Game.Simulation.FireEngineAISystem, Game.Simulation.FirePathfindSetup, Game.Simulation.FireSimulationSystem, Game.Simulation.HealthcareDispatchSystem, Game.Simulation.HealthProblemSystem, Game.Simulation.CollapsedBuildingSystem, Game.Simulation.SimulationUtils, Game.Areas.AreaUtils, Game.Areas.ServiceDistrictSystem, Game.Simulation.PathfindSetupSystem
- Decompiled snippets saved to: `research/topics/EmergencyDispatch/snippets/`
