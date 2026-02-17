# Research: Police Dispatch with Lights and Sirens -- Deep Dive

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: Every possible way to make a police car respond to a location with emergency lights and sirens activated, including the full code path from trigger event through dispatch, vehicle flag setting, navigation with emergency privileges, and arrival. This is the definitive reference for police dispatch modding in CS2.

**Why**: To build a mod that can programmatically dispatch police cars with lights and sirens to any world location -- useful for scripted events, custom emergency scenarios, or enhanced police behavior mods.

**Boundaries**: Out of scope: police helicopter AI (`PoliceAircraftAISystem`), prisoner transport (`PrisonerTransportDispatchSystem`), crime accumulation internals (`CrimeAccumulationSystem`), and patrol route optimization.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | PoliceEmergencyDispatchSystem, PolicePatrolDispatchSystem, PoliceCarAISystem, PoliceStationAISystem, AccidentSiteSystem, ServiceRequestSystem, PoliceEmergencyRequest, PolicePatrolRequest, ServiceRequest, ServiceDispatch, Dispatched, HandleRequest, RequestGroup |
| Game.dll | Game.Vehicles | PoliceCar (component), PoliceCarFlags, Car, CarFlags, CarCurrentLane, CarLaneFlags (vehicle-side) |
| Game.dll | Game.Buildings | PoliceStation (component), PoliceStationFlags, CrimeProducer |
| Game.dll | Game.Citizens | Criminal, CriminalFlags |
| Game.dll | Game.Events | AccidentSite, AccidentSiteFlags, InvolvedInAccident |
| Game.dll | Game.Net | CarLaneFlags (network-side) |
| Game.dll | Game.Prefabs | PoliceCarData, PoliceStationData, PoliceConfigurationData, PolicePurpose, CrimeData, TrafficAccidentData |
| Game.dll | Game.Common | Target, EffectsUpdated |
| Game.dll | Game.Pathfind | PathOwner, PathInformation, PathElement, SetupQueueItem, SetupQueueTarget, SetupTargetType, PathFlags |

## Complete Component Map

### `PoliceCar` (Game.Vehicles)

Runtime state component on every active police car entity.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetRequest | Entity | Current reverse-search request entity this car is seeking work through |
| m_State | PoliceCarFlags | Bitfield of current state flags |
| m_RequestCount | int | Number of accepted service dispatches |
| m_PathElementTime | float | Average time per path element (for shift estimation) |
| m_ShiftTime | uint | Frames elapsed since shift started |
| m_EstimatedShift | uint | Estimated remaining frames for current route |
| m_PurposeMask | PolicePurpose | Which purposes this car serves (Patrol, Emergency, Intelligence) |

### `PoliceCarFlags` (Game.Vehicles)

| Flag | Value | Description |
|------|-------|-------------|
| Returning | 0x01 | Car is heading back to its station |
| ShiftEnded | 0x02 | Shift timer has expired |
| AccidentTarget | 0x04 | Currently dispatched to an accident/emergency site |
| AtTarget | 0x08 | Has arrived at the target location |
| Disembarking | 0x10 | Currently unloading passengers (criminals) |
| Cancelled | 0x20 | Current patrol assignment was cancelled (preempted by emergency) |
| Full | 0x40 | All passenger (criminal) capacity filled |
| Empty | 0x80 | No passengers currently |
| EstimatedShiftEnd | 0x100 | Estimated to exceed shift duration with current route |
| Disabled | 0x200 | Car is disabled (no available capacity at station) |

### `Car` (Game.Vehicles)

Base component on all car entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | CarFlags | Bitfield controlling driving behavior and visual effects |

### `CarFlags` (Game.Vehicles) -- Complete List

| Flag | Value | Description |
|------|-------|-------------|
| **Emergency** | **0x01** | **THE KEY FLAG. Enables lights, sirens, traffic yielding, lane exemptions.** |
| StayOnRoad | 0x02 | Prevents the car from using parking/garage lanes |
| AnyLaneTarget | 0x04 | Can target any lane position (used during patrol) |
| Warning | 0x08 | Warning lights (not full emergency -- amber lights) |
| UsePublicTransportLanes | 0x20 | Can use bus/tram lanes |

### `CarCurrentLane` (Game.Vehicles)

Current lane state for a moving car.

| Field | Type | Description |
|-------|------|-------------|
| m_Lane | Entity | The lane entity the car is currently on |
| m_ChangeLane | Entity | Lane the car is changing to (or Entity.Null) |
| m_CurvePosition | float3 | Position along the lane curve (x=start, y=current, z=end) |
| m_LaneFlags | CarLaneFlags | Vehicle-side lane flags (IsBlocked, EndOfPath, etc.) |
| m_ChangeProgress | float | Progress of lane change (0-1) |
| m_Duration | float | Duration of current path segment |
| m_Distance | float | Distance along current segment |
| m_LanePosition | float | Lateral position in lane |

### `CarLaneFlags` (Game.Vehicles) -- Key Flags

| Flag | Value | Description |
|------|-------|-------------|
| EndOfPath | 0x01 | At the end of path |
| EndReached | 0x02 | Path end physically reached |
| ParkingSpace | 0x10 | Currently in a parking space |
| IsBlocked | 0x4000 | Path is blocked (used by PoliceCarAISystem to trigger close-enough check) |
| Checked | 0x400 | Lane has been checked for crime reduction (prevent double-counting) |

### `CarLaneFlags` (Game.Net) -- Network Lane Flags

| Flag | Value | Description |
|------|-------|-------------|
| Yield | 0x400 | Lane has a yield sign |
| Stop | 0x800 | Lane has a stop sign |
| PublicOnly | 0x8000 | Lane is for public transport only |
| TrafficLights | 0x8000000 | Lane is controlled by traffic lights |
| Forbidden | 0x40000000 | Lane is forbidden for general traffic |

**Emergency vehicles ignore these restrictions.** The pathfinding for emergency dispatch uses `IgnoredRules` flags that bypass ForbidCombustionEngines, ForbidTransitTraffic, ForbidHeavyTraffic, ForbidPrivateTraffic, ForbidSlowTraffic, and AvoidBicycles. The `CarFlags.Emergency` flag at the vehicle navigation level causes the car to ignore traffic signals, use any lane (including oncoming), and other vehicles yield.

### `PoliceStation` (Game.Buildings)

Component on police station building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_PrisonerTransportRequest | Entity | Active prisoner transport request |
| m_TargetRequest | Entity | Active reverse-search request for dispatching |
| m_Flags | PoliceStationFlags | HasAvailablePatrolCars (1), HasAvailablePoliceHelicopters (2), NeedPrisonerTransport (4) |
| m_PurposeMask | PolicePurpose | Which purposes this station serves |

### `PoliceEmergencyRequest` (Game.Simulation)

Request entity component for emergency police dispatch.

| Field | Type | Description |
|-------|------|-------------|
| m_Site | Entity | The AccidentSite entity to respond to |
| m_Target | Entity | The specific target entity at the site |
| m_Priority | float | Dispatch priority (higher = more urgent) |
| m_Purpose | PolicePurpose | Purpose flags (Emergency, Intelligence) |

### `PolicePatrolRequest` (Game.Simulation)

Request entity component for patrol dispatch.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | The CrimeProducer building to patrol |
| m_Priority | float | Dispatch priority |
| m_DispatchIndex | byte | Incrementing index to track repeat dispatches |

### `AccidentSite` (Game.Events)

Attached to road segments or buildings where an accident/crime is occurring. This is the key gatekeeper for police dispatch.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The persistent event entity (TrafficAccident, crime event) -- must be a valid Game.Events.Event entity |
| m_PoliceRequest | Entity | Current police emergency request entity (or Entity.Null) |
| m_Flags | AccidentSiteFlags | State flags controlling police request creation |
| m_CreationFrame | uint | Frame when the accident site was created |
| m_SecuredFrame | uint | Frame when police secured the site |

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

### `Criminal` (Game.Citizens)

Component on citizen entities who are criminals.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime event entity |
| m_JailTime | ushort | Jail time (frames) |
| m_Flags | CriminalFlags | State flags |

### `CriminalFlags` (Game.Citizens)

| Flag | Value | Description |
|------|-------|-------------|
| Robber | 0x01 | Is a robber |
| Prisoner | 0x02 | Is a prisoner |
| Planning | 0x04 | Planning a crime |
| Preparing | 0x08 | Preparing for a crime |
| Monitored | 0x10 | Being monitored (CCTV) |
| Arrested | 0x20 | Has been arrested |
| Sentenced | 0x40 | Has been sentenced |

### `CrimeProducer` (Game.Buildings)

Component on buildings that produce crime (for patrol dispatch validation).

| Field | Type | Description |
|-------|------|-------------|
| m_PatrolRequest | Entity | Active patrol request for this building |
| m_Crime | float | Current crime level (0 = no crime) |
| m_DispatchIndex | byte | Dispatch index for ordering |

### `PolicePurpose` (Game.Prefabs)

| Flag | Value | Description |
|------|-------|-------------|
| Patrol | 1 | Routine patrol |
| Emergency | 2 | Emergency response (accidents, crime scenes) |
| Intelligence | 4 | Intelligence/surveillance (monitored crime scenes) |

### `PoliceCarData` (Game.Prefabs)

Prefab data on police car prefab entities.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_CriminalCapacity | int | 2 | Max criminals the car can carry |
| m_CrimeReductionRate | float | 10000 | Rate at which nearby crime is reduced |
| m_ShiftDuration | uint | 262144 (1.0 * 262144) | Shift duration in frames |
| m_PurposeMask | PolicePurpose | Patrol + Emergency | Which tasks the car type can perform |

### `PoliceConfigurationData` (Game.Prefabs)

Singleton controlling global police behavior.

| Field | Type | Description |
|-------|------|-------------|
| m_PoliceServicePrefab | Entity | Reference to police service prefab |
| m_TrafficAccidentNotificationPrefab | Entity | Notification prefab for accidents |
| m_CrimeSceneNotificationPrefab | Entity | Notification prefab for crimes |
| m_MaxCrimeAccumulation | float | Maximum crime level a building can reach |
| m_CrimeAccumulationTolerance | float | Crime threshold before patrol dispatch triggers |
| m_HomeCrimeEffect | int | Crime impact on home happiness |
| m_WorkplaceCrimeEffect | int | Crime impact on work happiness |

### `Target` (Game.Common)

Simple targeting component used by all vehicle AI systems.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | The entity this vehicle is navigating towards |

### `EffectsUpdated` (Game.Common)

Empty tag component. When added to an entity, triggers the rendering system to recalculate visual effects (including emergency lights and sirens) on the next frame.

### `ServiceRequest` (Game.Simulation)

Base component present on every service request entity.

| Field | Type | Description |
|-------|------|-------------|
| m_FailCount | byte | Number of times dispatch has failed for this request |
| m_Cooldown | byte | Ticks remaining before next dispatch attempt |
| m_Flags | ServiceRequestFlags | Reversed (find target from source), SkipCooldown |

### `Dispatched` (Game.Simulation)

Added to a request entity once a handler (vehicle/building) has been assigned.

| Field | Type | Description |
|-------|------|-------------|
| m_Handler | Entity | The vehicle or building entity handling this request |

## Complete System Map

### `AccidentSiteSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames
- **Queries**: Entities with `AccidentSite`, excluding `Deleted`/`Temp`
- **Key logic**:
  1. Iterates over all entities with `AccidentSite` component
  2. Checks `TargetElement` buffer on `m_Event` to find involved entities
  3. For each `InvolvedInAccident`: tracks max severity (`num2`) and highest-severity non-moving entity (`entity2`)
  4. For each `Criminal`: counts non-arrested criminals, sets `CrimeMonitored` if any criminal is monitored
  5. **Unconditionally clears `RequirePolice`** at line 227
  6. **Conditionally re-sets `RequirePolice`** if severity > 0 with valid target, OR unsecured crime scene with CrimeDetected and valid target
  7. Calls `RequestPoliceIfNeeded()` which creates `PoliceEmergencyRequest` if `m_PoliceRequest` is stale
  8. **AccidentSite removal**: When `num == 0` (no involved entities) AND not StageAccident AND (not secured crime scene OR 1024 frames since secured), removes the `AccidentSite` component entirely

- **Critical detail for mods**: The AccidentSite removal logic at line 243 removes `AccidentSite` when there are zero involved entities in the TargetElement buffer. For a mod-created AccidentSite to survive, the `m_Event` entity MUST have a `TargetElement` buffer with at least one valid `InvolvedInAccident` or `Criminal` entity, OR the `StageAccident` flag must be set and fewer than 3600 frames have passed.

### `PoliceEmergencyDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Entities with `PoliceEmergencyRequest` + `UpdateFrame`
- **Two-phase processing**:
  - **Phase 1 (next frame)**: Processes requests without `Dispatched` or `PathInformation` -- calls `FindVehicleSource()` or `FindVehicleTarget()` (for reversed)
  - **Phase 2 (current frame)**: Processes dispatched requests (validates handler still has ServiceDispatch) and pathfound requests (dispatches vehicle if origin found)

- **`ValidateSite()`**: Checks three conditions:
  1. `m_AccidentSiteData.TryGetComponent(site)` -- site entity must have AccidentSite component
  2. `(m_Flags & (Secured | RequirePolice)) == RequirePolice` -- RequirePolice must be set AND Secured must NOT be set
  3. `m_PoliceRequest == entity` OR current m_PoliceRequest is stale -- prevents duplicate requests

- **`FindVehicleSource()`**: The critical pathfinding setup:
  - Determines target district via `CurrentDistrict` or spatial query
  - Origin: `SetupTargetType.PolicePatrol` with `m_Value = (int)purpose` -- this matches police stations and on-road police cars whose `PolicePurpose` includes the requested purpose
  - Destination: `SetupTargetType.AccidentLocation` with `m_Value2 = 30f` (30m arrival radius) IF site has AccidentSite; otherwise `SetupTargetType.CurrentLocation` with `m_Entity = target`
  - Parameters: `m_MaxSpeed = 111.111115f` (400 km/h), all traffic rules ignored
  - **KEY INSIGHT**: The `m_Entity` on the origin is set to the target's district entity, NOT a specific station. The pathfinder searches for ANY police station or car in that district with matching purpose. This is why it works for game-created requests (the target entity has a real district) and can fail for mod-created requests if the target entity lacks `CurrentDistrict` or `Transform` components.

- **`FindVehicleTarget()`** (reverse path): Searches from vehicle/station to `SetupTargetType.PoliceRequest` -- finds unsatisfied emergency requests
- **`DispatchVehicle()`**: When pathfinding succeeds, resolves `PathInformation.m_Origin` (possibly a ParkedCar -> Owner), enqueues `VehicleDispatch`, adds `Dispatched` component
- **`DispatchVehiclesJob`**: Processes queued dispatches -- adds `ServiceDispatch` buffer element to the handler entity

### `PolicePatrolDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Entities with `PolicePatrolRequest` + `UpdateFrame`
- **`ValidateTarget()`**: Checks `CrimeProducer.m_Crime >= m_PoliceConfigurationData.m_CrimeAccumulationTolerance` -- patrol only dispatches to buildings with crime above threshold
- **`FindVehicleSource()`**: Uses `SetupTargetType.PolicePatrol` with `m_Value = 1` (Patrol purpose) AND includes `PathMethod.Flying` for helicopters
  - Notably uses `m_MaxSpeed = 277.77777f` (1000 km/h) and `PathfindWeights(1f, 1f, 1f, 1f)` unlike emergency which uses `(1f, 0f, 0f, 0f)` -- patrol considers comfort/cost, emergency only considers distance
- **`ValidateReversed()` for patrol cars**: Requires `(m_State & (ShiftEnded | Empty | EstimatedShiftEnd | Disabled)) == Empty` -- car must be empty AND not have shift ended AND not estimated to end shift. This is stricter than emergency reverse validation.
- **Key difference from emergency**: Patrol dispatch does NOT set `CarFlags.Emergency`. When `PoliceCarAISystem.SelectNextDispatch()` processes a patrol request, it clears Emergency and sets AnyLaneTarget instead.

### `PoliceCarAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames, offset 5
- **Queries**: Entities with `CarCurrentLane` + `Owner` + `PrefabRef` + `PathOwner` + `PoliceCar` + `Car` + `Target`, excluding Deleted/Temp/TripSource/OutOfControl
- **Main entry point**: `Tick()` -- runs for every active (non-parked) police car every 16 frames

#### `Tick()` method flow:

1. **Shift management**: Increment `m_ShiftTime`, check against `PoliceCarData.m_ShiftDuration`
2. **Crime reduction**: If NOT in emergency mode, call `TryReduceCrime()` on current lane's connected buildings
3. **Path updated**: If path was recalculated, call `ResetPath()` which sets Emergency/non-Emergency flags based on first dispatch type
4. **Target validation**: If target doesn't exist or pathfind failed:
   - If stuck or returning -> delete vehicle
   - Otherwise -> `ReturnToStation()`
5. **At destination**: If path end reached or parking reached or already AtTarget:
   - If Returning -> handle disembarking, then park or delete
   - If AccidentTarget -> `SecureAccidentSite()` (stop vehicle, set Secured flag)
   - Otherwise -> `TryReduceCrime()` on target building
   - Then `SelectNextDispatch()` or `ReturnToStation()`
6. **En route, blocked**: If AccidentTarget AND lane is blocked AND within 30m -> `EndNavigation()` (treat as arrived)
7. **Flag maintenance**: Update EstimatedShiftEnd, Full, Empty flags
8. **Shift ended check**: If not Emergency and shift ended -> `ReturnToStation()`, clear dispatches
9. **Request new targets**: If request count <= 1 and not disabled -> `RequestTargetIfNeeded()`
10. **Pathfinding**: If path needed -> `FindNewPath()`

#### `SelectNextDispatch()` -- THE KEY METHOD for lights/sirens:

```
while (requestCount > 0 && serviceDispatches.Length > 0):
    request = serviceDispatches[0].m_Request

    if request is PolicePatrolRequest:
        entity = request.m_Target (CrimeProducer building)
        policeCarFlags = 0 (no AccidentTarget)
    else if request is PoliceEmergencyRequest:
        entity = request.m_Site (AccidentSite)
        policeCarFlags = AccidentTarget

    if entity is valid (has PrefabRef):
        Clear: Returning, AccidentTarget, AtTarget, Cancelled
        Set: policeCarFlags (AccidentTarget for emergency)

        Create HandleRequest for path consumption

        if path can be appended (TryAppendPath succeeds):
            if AccidentTarget (EMERGENCY):
                car.m_Flags &= ~AnyLaneTarget
                car.m_Flags |= Emergency | StayOnRoad | UsePublicTransportLanes
            else (PATROL):
                car.m_Flags &= ~Emergency
                car.m_Flags |= StayOnRoad | AnyLaneTarget | UsePublicTransportLanes

            ClearEndOfPath(ref currentLane, navigationLanes)
            target.m_Target = entity
            AddComponent<EffectsUpdated>(vehicleEntity)  // TRIGGERS LIGHTS/SIRENS
            return true

        // *** FALLTHROUGH PATH (no PathElement or TryAppendPath fails) ***
        // NONE of the above flags are set! No Emergency, no EndOfPath cleared,
        // no EffectsUpdated added. Only VehicleUtils.SetTarget() is called.
        VehicleUtils.SetTarget(ref pathOwner, ref target, entity)
        return true
```

> **Known Pitfall**: For mod-created dispatch requests that don't have a `PathElement` buffer
> (which is common — the game's dispatch system adds PathElement during pathfinding, but mod
> requests typically skip this), `SelectNextDispatch` falls through to the bottom path. This
> causes two problems:
>
> 1. **`CarFlags.Emergency` is never set** — the car drives to the target in patrol mode (no
>    sirens, no lights, no traffic yielding). See issue: Emergency flags only set inside
>    TryAppendPath branch.
> 2. **`CarLaneFlags.EndOfPath` is never cleared** — on the very next tick,
>    `VehicleUtils.PathEndReached(currentLane)` returns true, triggering arrival logic which
>    immediately pops and discards the dispatch. The car appears to briefly activate then
>    immediately return to station. See issue: EndOfPath not cleared in fallthrough path.
>
> 3. **`m_RequestCount == 0` activation gap** — When injecting a dispatch into a car with
>    `m_RequestCount == 0` (no active dispatches), `CheckServiceDispatches()` accepts it and
>    increments `m_RequestCount` to 1, but `SelectNextDispatch()` never runs because the car
>    has neither `Returning` nor `Cancelled` set. The dispatch sits in the buffer, accepted but
>    never activated. When the car eventually reaches its current path end, `SelectNextDispatch`
>    pops index 0 as "completed" — destroying the dispatch without ever navigating to it. **Only
>    target cars with `m_RequestCount >= 1`** (active patrol), or use the hybrid approach (direct
>    flag manipulation + ServiceDispatch) to bypass `SelectNextDispatch` entirely.
>
> **Workaround**: Before injecting a `ServiceDispatch` entry, manually set the flags:
> ```csharp
> // Clear EndOfPath from previous patrol
> CarCurrentLane currentLane = EntityManager.GetComponentData<CarCurrentLane>(carEntity);
> currentLane.m_LaneFlags &= ~CarLaneFlags.EndOfPath;
> EntityManager.SetComponentData(carEntity, currentLane);
>
> // Set Emergency flags directly
> Car car = EntityManager.GetComponentData<Car>(carEntity);
> car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
> car.m_Flags &= ~CarFlags.AnyLaneTarget;
> EntityManager.SetComponentData(carEntity, car);
>
> // Trigger visual update
> EntityManager.AddComponent<EffectsUpdated>(carEntity);
> ```

#### `ResetPath()` -- also sets Emergency flag:

After pathfinding completes for the current dispatch, `ResetPath()` checks what type of request is active:
- `PoliceEmergencyRequest` -> sets `Emergency | StayOnRoad`
- `PolicePatrolRequest` -> clears `Emergency`, sets `StayOnRoad | AnyLaneTarget`
- Then adds `EffectsUpdated` to trigger visual update

#### `SecureAccidentSite()`:

1. Checks first service dispatch is a `PoliceEmergencyRequest`
2. Verifies the request's `m_Site` still has an `AccidentSite` component
3. Sets `PoliceCarFlags.AtTarget`
4. Calls `StopVehicle()` (removes Moving, adds Stopped)
5. If AccidentSite is not yet Secured: enqueues `PoliceAction.SecureAccidentSite` which sets `AccidentSiteFlags.Secured` and records `m_SecuredFrame`
6. Returns false (stay at target) while securing; returns true when done or site is already secured

#### `ReturnToStation()`:

1. Clears all dispatches
2. Sets `PoliceCarFlags.Returning`
3. Sets target to Owner (the police station entity)
4. Note: `CarFlags.Emergency` is cleared by `ResetPath()` after the return path is calculated, or by `ParkCar()` on arrival

#### `RequestTargetIfNeeded()` -- reverse dispatch creation:

When a car has few active dispatches:
- If purpose includes Patrol AND car is Empty AND not EstimatedShiftEnd: creates reverse `PolicePatrolRequest` with `RequestGroup(32)`
- Else if purpose includes Emergency/Intelligence: creates reverse `PoliceEmergencyRequest` with `RequestGroup(4)` using the car's own PurposeMask filtered to Emergency|Intelligence

#### `IsCloseEnough()`:

Used when a car is blocked en route to an AccidentTarget. Checks:
1. If target has Transform: simple 30m distance check
2. If target has AccidentSite: checks all TargetElement entries in the event's buffer, finds any InvolvedInAccident entity within 30m
This is how cars can "arrive" at an accident even if the exact path is blocked by the accident itself.

#### `FindNewPath()`:

Sets up pathfinding for the current target:
- For AccidentTarget: destination type = `AccidentLocation` with 30m radius
- For patrol/other: destination type = `CurrentLocation`
- For non-returning: `PathfindWeights(1f, 0f, 0f, 0f)` -- only distance matters
- For returning: `PathfindWeights(1f, 1f, 1f, 1f)` -- all weights matter, includes `SpecialParking`
- Ignored rules: same as dispatch system (all traffic restrictions bypassed)

### `PoliceStationAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: Simulation (every 16 frames)
- **Purpose**: Manages police station state -- counts available vehicles, sets `HasAvailablePatrolCars` / `HasAvailablePoliceHelicopters` flags, manages shift schedules, handles dispatching/recalling vehicles
- Sets `PoliceCarFlags.Disabled` on cars when station has no available capacity

## Complete Dispatch Paths

### Path A: Crime Event -> Police Car with Sirens

```
1. CrimeAccumulationSystem detects crime threshold exceeded on building
   -> Creates crime event entity (Game.Events.Event)
   -> Adds AccidentSite to building/road with CrimeScene flag
   -> Sets m_Event to crime event entity

2. AccidentSiteSystem (every 64 frames):
   -> Clears RequirePolice unconditionally
   -> Iterates TargetElement buffer on m_Event
   -> Finds Criminal entities (not arrested)
   -> If CrimeDetected flag set (after alarm delay):
      -> Sets RequirePolice
      -> RequestPoliceIfNeeded() creates PoliceEmergencyRequest
         with purpose = CrimeMonitored ? Intelligence : Emergency
         with RequestGroup(4)

3. ServiceRequestSystem:
   -> Sees RequestGroup(4), assigns random UpdateFrame (0-3)
   -> Removes RequestGroup

4. PoliceEmergencyDispatchSystem (every 16 frames):
   -> ValidateSite(): RequirePolice set, not Secured? YES
   -> FindVehicleSource(): pathfind from PolicePatrol to AccidentLocation
      - Origin type: SetupTargetType.PolicePatrol
      - Origin entity: target's district
      - Origin value: (int)PolicePurpose.Emergency (or Intelligence)
      - Destination: SetupTargetType.AccidentLocation, 30m radius
   ~~ pathfinding runs asynchronously ~~
   -> DispatchVehicle(): PathInformation.m_Origin found
      - Adds Dispatched(handler) to request
      - Adds ServiceDispatch(request) to handler's buffer

5. PoliceCarAISystem (every 16 frames, offset 5):
   -> CheckServiceDispatches(): validates new dispatch, accepts it
      - Emergency requests have PRIORITY over patrol requests
      - Existing patrol dispatches are cancelled (PoliceCarFlags.Cancelled)
   -> SelectNextDispatch():
      - Reads ServiceDispatch buffer, finds PoliceEmergencyRequest
      - Sets PoliceCarFlags.AccidentTarget
      - Sets Car.m_Flags: Emergency | StayOnRoad | UsePublicTransportLanes
      - Clears CarFlags.AnyLaneTarget
      - Sets Target.m_Target = AccidentSite entity
      - Adds EffectsUpdated tag -> SIREN + LIGHTS ACTIVATE

6. CarNavigationSystem (every frame):
   -> Reads CarFlags.Emergency
   -> Other cars yield to this vehicle
   -> Can use any lane including oncoming
   -> Ignores traffic signals
   -> Emergency lights and siren visual/audio effects rendered

7. ARRIVAL (path end reached OR within 30m of target when blocked):
   -> SecureAccidentSite():
      - Sets PoliceCarFlags.AtTarget
      - StopVehicle(): removes Moving, adds Stopped
      - Sets AccidentSite.m_Flags |= Secured, records m_SecuredFrame

8. COMPLETION:
   -> If more dispatches: SelectNextDispatch()
   -> Otherwise: ReturnToStation()
      - Clears AccidentTarget, AtTarget, Cancelled
      - Sets Returning
      - Target = Owner (station)
   -> ResetPath(): clears Emergency flag -> SIREN + LIGHTS OFF
```

### Path B: Traffic Accident -> Police Car with Sirens

```
1. ImpactSystem detects vehicle collision
   -> Creates Impact event entity (Game.Common.Event + Impact)

2. AddAccidentSiteSystem:
   -> Processes Impact events
   -> Creates TrafficAccident event entity (Game.Events.Event)
   -> Adds AccidentSite to road segment with TrafficAccident flag
   -> Adds InvolvedInAccident to involved vehicles with severity

3. AccidentSiteSystem (every 64 frames):
   -> Same flow as Path A, but triggered by InvolvedInAccident severity
   -> If any involved entity has severity > 0 AND a non-moving entity exists:
      - Sets RequirePolice
      - RequestPoliceIfNeeded() creates PoliceEmergencyRequest
        with purpose = Emergency, priority = max severity

4-8. Same as Path A steps 3-8.
```

### Path C: Manual Dispatch via Request Pipeline (Unreliable)

```
1. Mod creates PoliceEmergencyRequest entity:
   Entity request = EntityManager.CreateEntity();
   EntityManager.AddComponentData(request, new ServiceRequest());
   EntityManager.AddComponentData(request, new PoliceEmergencyRequest(
       siteEntity, targetEntity, 1f, PolicePurpose.Emergency));
   EntityManager.AddComponentData(request, new RequestGroup(4u));

2. Mod ensures siteEntity has AccidentSite with RequirePolice:
   -> m_Event must point to a valid Game.Events.Event entity
   -> m_Flags must include RequirePolice
   -> m_Flags must NOT include Secured

3. ServiceRequestSystem:
   -> Assigns UpdateFrame, removes RequestGroup

4. PoliceEmergencyDispatchSystem:
   -> ValidateSite(): checks RequirePolice, not Secured
   -> FindVehicleSource(): pathfinds from PolicePatrol in target's district

   *** HERE IS WHERE IT OFTEN FAILS ***

   Failure modes:
   a) Target entity has no CurrentDistrict AND no Transform -> district lookup
      returns Entity.Null -> origin m_Entity is null -> pathfinder finds no
      matching police station in "no district"
   b) Target entity is not on a road network -> pathfinder cannot route to it
   c) AccidentSiteSystem runs before the dispatch system and clears RequirePolice
      because the AccidentSite has no real InvolvedInAccident/Criminal entities
      in its TargetElement buffer -> ValidateSite() fails -> request destroyed
   d) AccidentSite is removed entirely because TargetElement buffer is empty
      and StageAccident is not set

5. If pathfinding DOES succeed (rare):
   -> DispatchVehicle() works normally
   -> PoliceCarAISystem processes it identically to game-created requests
   -> Car drives with Emergency flag set -> lights and sirens ON
```

**WHY THE REQUEST PIPELINE FAILS FOR MODS (DETAILED)**:

The core problem is `FindVehicleSource()` in `PoliceEmergencyDispatchSystem`. It requires:

1. **District resolution**: The method calls `m_CurrentDistrictData.HasComponent(target)` first. If the target entity does not have a `CurrentDistrict` component, it falls back to `m_TransformData.HasComponent(target)` and does a spatial district lookup. If the target has neither component, the district is `Entity.Null`, and the pathfinder's `SetupTargetType.PolicePatrol` origin with `m_Entity = Entity.Null` finds no matching stations.

2. **AccidentSite validation**: `ValidateSite()` requires `AccidentSiteFlags.RequirePolice` to be set. But `AccidentSiteSystem` runs every 64 frames and unconditionally clears this flag, then only re-sets it if there are valid `InvolvedInAccident` or detected `Criminal` entities in the event's `TargetElement` buffer. A mod-created AccidentSite with an empty `TargetElement` buffer will have `RequirePolice` stripped within 64 frames.

3. **AccidentSite survival**: Even worse, `AccidentSiteSystem` removes the entire `AccidentSite` component when `num == 0` (no involved entities) and `StageAccident` is not set. The mod's AccidentSite will be completely removed.

4. **Path routing**: Even if district resolution works, the destination is `SetupTargetType.AccidentLocation` which requires the site entity to be on or near a road network. Arbitrary entities may not satisfy this.

### Path D: Direct Vehicle Manipulation (Incomplete Without ServiceDispatch)

> **Warning**: Setting flags alone is NOT sufficient. When `PoliceCarAISystem.Tick()` detects
> that the path has been updated, it calls `ResetPath()`, which checks the first entry in the
> `ServiceDispatch` buffer to determine what flags to set. If the buffer is empty or does not
> contain a `PoliceEmergencyRequest`, `ResetPath()` clears `CarFlags.Emergency` and sets
> `CarFlags.AnyLaneTarget` (patrol mode). This means the lights and sirens turn off immediately
> after pathfinding completes. You MUST inject a `ServiceDispatch` buffer entry (see Path D2 below).

```
1. Mod finds an available police car (on patrol, not returning, not at target)

2. Mod sets emergency flags directly:
   Car car = EntityManager.GetComponentData<Car>(policeCarEntity);
   car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
   car.m_Flags &= ~CarFlags.AnyLaneTarget;
   EntityManager.SetComponentData(policeCarEntity, car);

3. Mod sets PoliceCar state:
   PoliceCar pc = EntityManager.GetComponentData<PoliceCar>(policeCarEntity);
   pc.m_State |= PoliceCarFlags.AccidentTarget;
   pc.m_State &= ~(PoliceCarFlags.Returning | PoliceCarFlags.AtTarget | PoliceCarFlags.Cancelled);
   EntityManager.SetComponentData(policeCarEntity, pc);

4. Mod sets target:
   EntityManager.SetComponentData(policeCarEntity, new Target(targetEntity));

5. Mod triggers path recalculation:
   PathOwner po = EntityManager.GetComponentData<PathOwner>(policeCarEntity);
   po.m_State |= PathFlags.Updated;
   EntityManager.SetComponentData(policeCarEntity, po);

6. Mod triggers visual update:
   EntityManager.AddComponent<EffectsUpdated>(policeCarEntity);

7. **FAILURE POINT**: PoliceCarAISystem.Tick() runs:
   -> Path was recalculated, calls ResetPath()
   -> ResetPath() reads ServiceDispatch buffer
   -> Buffer is empty or has no PoliceEmergencyRequest
   -> ResetPath() CLEARS CarFlags.Emergency, sets AnyLaneTarget
   -> Lights and sirens turn off; car reverts to patrol mode

8. This approach REQUIRES ServiceDispatch buffer injection (Path D2) to work.
```

### Path D2: Direct Vehicle Manipulation + ServiceDispatch Injection (RECOMMENDED)

The working approach for direct dispatch. Creates a `PoliceEmergencyRequest` entity and injects it into the vehicle's `ServiceDispatch` buffer so that `ResetPath()` and `SelectNextDispatch()` correctly recognize the dispatch as an emergency and maintain `CarFlags.Emergency`.

```
1. Mod creates a PoliceEmergencyRequest entity:
   Entity request = EntityManager.CreateEntity();
   EntityManager.AddComponentData(request, new ServiceRequest());
   EntityManager.AddComponentData(request, new PoliceEmergencyRequest {
       m_Site = targetEntity,
       m_Target = targetEntity,
       m_Priority = 1f,
       m_Purpose = PolicePurpose.Emergency
   });

2. Mod injects the request into the vehicle's ServiceDispatch buffer:
   DynamicBuffer<ServiceDispatch> dispatches =
       EntityManager.GetBuffer<ServiceDispatch>(policeCarEntity);
   dispatches.Clear();  // Remove existing patrol dispatches
   dispatches.Add(new ServiceDispatch { m_Request = request });

3. Mod sets emergency flags (same as Path D steps 2-6)

4. PoliceCarAISystem.Tick() runs:
   -> Path was recalculated, calls ResetPath()
   -> ResetPath() reads ServiceDispatch[0] -> finds PoliceEmergencyRequest
   -> Sets CarFlags.Emergency | StayOnRoad (LIGHTS AND SIRENS STAY ON)
   -> Adds EffectsUpdated

5. On arrival, SelectNextDispatch() also reads ServiceDispatch buffer:
   -> Finds PoliceEmergencyRequest, sets AccidentTarget
   -> Emergency flags remain active throughout the journey

6. When done, mod should clean up the request entity
```

### Path E: Fake Crime Event (ALTERNATIVE APPROACH)

```
1. Mod creates a full event entity that mimics a real crime event:

   // Create persistent event entity
   Entity eventEntity = EntityManager.CreateEntity();
   EntityManager.AddComponentData(eventEntity, new Game.Events.Event());
   EntityManager.AddBuffer<TargetElement>(eventEntity);

   // Create a dummy "criminal" entity to satisfy AccidentSiteSystem
   // (This entity needs Criminal component with matching m_Event)
   // OR use the mod entity itself as a target with InvolvedInAccident

2. Add AccidentSite to the target road/building:
   EntityManager.AddComponentData(targetEntity, new AccidentSite
   {
       m_Event = eventEntity,
       m_Flags = AccidentSiteFlags.CrimeScene | AccidentSiteFlags.CrimeDetected
               | AccidentSiteFlags.RequirePolice,
       m_PoliceRequest = Entity.Null,
       m_CreationFrame = simulationSystem.frameIndex
   });

3. Add the target to the event's TargetElement buffer:
   var buf = EntityManager.GetBuffer<TargetElement>(eventEntity);
   buf.Add(new TargetElement { m_Entity = targetEntity });

4. Add Criminal component to target (or use InvolvedInAccident):
   // Option A: Criminal on a citizen
   EntityManager.AddComponentData(citizenEntity, new Criminal(eventEntity, CriminalFlags.Robber));
   // Option B: InvolvedInAccident on a vehicle/object
   EntityManager.AddComponentData(vehicleEntity, new InvolvedInAccident
   {
       m_Event = eventEntity,
       m_Severity = 1f,
       m_InvolvedFrame = simulationSystem.frameIndex
   });

5. AccidentSiteSystem processes the site naturally:
   -> Finds Criminal/InvolvedInAccident entities in TargetElement
   -> Sets RequirePolice
   -> Creates PoliceEmergencyRequest

6. Full dispatch pipeline proceeds normally
   -> Vehicle dispatched with Emergency flag -> lights and sirens

7. CRITICAL: Mod must clean up the event entity, AccidentSite,
   Criminal/InvolvedInAccident when done. Otherwise the game
   will try to arrest the "criminal" or secure the "accident"
   indefinitely.
```

## Answers to Specific Questions

### 1. What EXACT component or flag turns on lights and sirens?

**`CarFlags.Emergency` (value 0x01) on the `Car` component** is the single control point. When set:
- The rendering pipeline reads this flag and activates emergency light meshes and siren audio
- `CarNavigationSystem` grants emergency lane privileges
- Other vehicles yield

The flag change must be accompanied by adding the `EffectsUpdated` tag component to trigger the rendering system to recalculate effects. Without `EffectsUpdated`, the flag change will take effect for navigation/yielding but the visual lights and audio may not update until some other system triggers an effect recalculation.

The `CarFlags.Warning` flag (value 0x08) activates amber/warning lights without full emergency sirens -- this is used for utility vehicles like garbage trucks.

### 2. Why does FindVehicleSource fail for mod-created requests?

Three reasons, in order of likelihood:

1. **District resolution failure**: The target entity lacks `CurrentDistrict` and `Transform` components, so the district lookup returns `Entity.Null`. The pathfinder's `SetupTargetType.PolicePatrol` origin filter uses this district to find nearby stations. With `Entity.Null` as the district, no stations match.

2. **AccidentSite cleared by AccidentSiteSystem**: The system unconditionally clears `RequirePolice` every 64 frames, then only re-sets it if valid `InvolvedInAccident` or `Criminal` entities exist in the event's `TargetElement` buffer. A mod-created AccidentSite without these will have `RequirePolice` stripped within 64 frames, causing `ValidateSite()` to fail and the request to be destroyed.

3. **AccidentSite removed entirely**: When the `TargetElement` buffer has zero matching entities (`num == 0`) and `StageAccident` is not set, `AccidentSiteSystem` removes the `AccidentSite` component from the entity entirely.

### 3. Can a mod create a COMPLETE working dispatch request?

**Yes, but it requires maintaining the full event lifecycle.** The minimum requirements:

1. A persistent `Game.Events.Event` entity with `TargetElement` buffer
2. At least one entity in the buffer with `InvolvedInAccident` (severity > 0, matching m_Event, NOT moving) or `Criminal` (matching m_Event, NOT arrested)
3. `AccidentSite` on a road segment or building with: `m_Event` pointing to the event entity, `RequirePolice` flag, NOT `Secured`
4. The AccidentSite entity must be on or near the road network (so pathfinding can reach it)
5. The AccidentSite entity should have `CurrentDistrict` or `Transform` (so the dispatch system can find the district)
6. A `PoliceEmergencyRequest` entity with: `ServiceRequest`, `m_Site` pointing to the AccidentSite entity, `RequestGroup(4)`

This is complex and fragile. **Direct vehicle manipulation (Path D) is strongly recommended instead.**

### 4. Is direct vehicle manipulation reliable?

**Only with ServiceDispatch buffer injection.** Setting `CarFlags.Emergency`, `PoliceCarFlags.AccidentTarget`, `Target`, and triggering path recalculation is NOT sufficient on its own. When `PoliceCarAISystem.Tick()` detects the path update, it calls `ResetPath()`, which reads the first `ServiceDispatch` buffer entry to determine what flags to set:

- If the first dispatch is a `PoliceEmergencyRequest`: sets `CarFlags.Emergency | StayOnRoad` (lights and sirens ON)
- If the first dispatch is a `PolicePatrolRequest` or buffer is empty: clears `CarFlags.Emergency`, sets `AnyLaneTarget` (lights and sirens OFF)

**This means direct flag manipulation without ServiceDispatch injection will fail** -- the Emergency flag is immediately cleared by `ResetPath()` after the path is recalculated.

**Required fix**: Create a `PoliceEmergencyRequest` entity and inject it into the vehicle's `ServiceDispatch` buffer before triggering path recalculation. This ensures `ResetPath()` and `SelectNextDispatch()` both recognize the dispatch as an emergency. See Path D2 above for the complete working approach.

Alternatively, use a custom "Emergency Flag Guardian" system (Example 6) running after `PoliceCarAISystem` that re-applies the Emergency flag each tick. This is simpler but less clean since it fights the AI every frame.

### 5. Is there a third approach nobody has tried?

**Yes: the fake crime event approach (Path E above).** By creating a complete event entity hierarchy with `Criminal` or `InvolvedInAccident` components that match the `AccidentSite`'s `m_Event`, the `AccidentSiteSystem` will naturally maintain the `RequirePolice` flag and the full dispatch pipeline works. This is more reliable than raw request creation but requires careful lifecycle management.

**Another untried approach**: Harmony postfix on `AccidentSiteSystem.OnUpdate()` to inject additional `RequirePolice` flags after the system's clear-then-evaluate cycle. The system schedules a Burst job, so the postfix would need to `CompleteDependency()` first, then modify AccidentSite components. This is simpler than creating fake events but has performance implications.

### 6. Difference between Emergency and Patrol dispatch

| Aspect | PoliceEmergencyDispatchSystem | PolicePatrolDispatchSystem |
|--------|-------------------------------|---------------------------|
| Request type | PoliceEmergencyRequest | PolicePatrolRequest |
| Validation | AccidentSite with RequirePolice, not Secured | CrimeProducer.m_Crime >= tolerance threshold |
| Vehicle flags | Emergency, StayOnRoad, UsePublicTransportLanes | StayOnRoad, AnyLaneTarget, UsePublicTransportLanes |
| Lights/sirens | YES | NO |
| PoliceCar state | AccidentTarget flag set | No AccidentTarget flag |
| Pathfind speed | 111.1 m/s (400 km/h) | 277.8 m/s (1000 km/h) |
| Pathfind weights | (1, 0, 0, 0) -- distance only | (1, 1, 1, 1) -- all factors |
| Pathfind methods | Road only | Road + Flying (helicopters) |
| Reverse request interval | 64 frames | 512 frames |
| Purpose filter | Emergency \| Intelligence | Patrol |
| Car availability | ShiftEnded OK, requestCount > 1 OK | Must be Empty, no EstimatedShiftEnd, requestCount <= 1 |

**Patrol dispatch CANNOT be leveraged for emergency response.** When `PoliceCarAISystem.SelectNextDispatch()` processes a `PolicePatrolRequest`, it explicitly clears `CarFlags.Emergency`. The only way to get lights and sirens is through `PoliceEmergencyRequest` or direct flag manipulation.

### 7. How does the game prevent double-dispatch?

Three mechanisms:

1. **AccidentSite.m_PoliceRequest**: Each AccidentSite tracks its current police request entity. `AccidentSiteSystem.RequestPoliceIfNeeded()` only creates a new request if `m_PoliceRequest` does not have a valid `PoliceEmergencyRequest` component. `PoliceEmergencyDispatchSystem.ValidateSite()` updates `m_PoliceRequest` to point to the current request.

2. **Dispatched component**: Once a vehicle is assigned to a request, `PoliceEmergencyDispatchSystem` adds a `Dispatched` component to the request. Subsequent ticks check `ValidateHandler()` instead of trying to pathfind again. If the handler's ServiceDispatch buffer no longer contains the request, it's considered failed and the request is reset.

3. **PoliceStation/PoliceCar.m_TargetRequest**: Each station and car tracks its reverse-search request. `ValidateReversed()` checks if `m_TargetRequest` matches the current request. If another request already owns this slot, the new request is rejected.

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Criminal capacity | PoliceCarData.m_CriminalCapacity | Prefab: 2 |
| Crime reduction rate | PoliceCarData.m_CrimeReductionRate | Prefab: 10000 |
| Shift duration | PoliceCarData.m_ShiftDuration | Prefab: 262144 frames |
| Purpose mask | PoliceCarData.m_PurposeMask | Prefab: Patrol + Emergency |
| Patrol car capacity | PoliceStationData.m_PatrolCarCapacity | Prefab: 10 |
| Helicopter capacity | PoliceStationData.m_PoliceHelicopterCapacity | Prefab: 0 |
| Jail capacity | PoliceStationData.m_JailCapacity | Prefab: 15 |
| Crime tolerance | PoliceConfigurationData.m_CrimeAccumulationTolerance | Singleton prefab |
| Emergency dispatch interval | Hardcoded: 16 frames | PoliceEmergencyDispatchSystem |
| Patrol dispatch interval | Hardcoded: 16 frames | PolicePatrolDispatchSystem |
| AccidentSite eval interval | Hardcoded: 64 frames | AccidentSiteSystem |
| PoliceCarAI interval | Hardcoded: 16 frames, offset 5 | PoliceCarAISystem |
| Emergency pathfind speed | Hardcoded: 111.111115f (400 km/h) | PoliceEmergencyDispatchSystem |
| Patrol pathfind speed | Hardcoded: 277.77777f (1000 km/h) | PolicePatrolDispatchSystem |
| Close enough distance | Hardcoded: 30f | PoliceCarAISystem.IsCloseEnough() |
| AccidentSite staging timeout | Hardcoded: 3600 frames (~60s) | AccidentSiteSystem |
| AccidentSite secured cleanup | Hardcoded: 1024 frames after secured | AccidentSiteSystem |
| Patrol reverse request group | Hardcoded: 32 | PoliceCarAISystem.RequestTargetIfNeeded() |
| Emergency reverse request group | Hardcoded: 4 | PoliceCarAISystem.RequestTargetIfNeeded() |
| Crime alarm delay | CrimeData.m_AlarmDelay (Bounds1) | Event prefab, modified by CityModifierType.CrimeResponseTime |

## Harmony Patch Points

### Candidate 1: Direct ECS manipulation (Recommended -- No Harmony needed)

- **Approach**: Custom GameSystemBase that finds available police cars and directly sets `CarFlags.Emergency`, `Target`, etc.
- **Risk level**: Low
- **Side effects**: Must add `EffectsUpdated` tag and manage `AccidentTarget` flag to prevent `PoliceCarAISystem` interference

### Candidate 2: Harmony postfix on `AccidentSiteSystem.OnUpdate()`

- **Signature**: `void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: After AccidentSiteSystem's job completes, modify AccidentSite components to force RequirePolice on all sites
- **Risk level**: Medium (must call `CompleteDependency()` to wait for job)
- **Side effects**: Performance impact from synchronizing the job pipeline

### Candidate 3: Custom system with `[UpdateAfter(typeof(AccidentSiteSystem))]`

- **Approach**: Query all AccidentSite entities, force RequirePolice flag, create PoliceEmergencyRequest entities
- **Risk level**: Low
- **Side effects**: Must ensure proper AccidentSite lifecycle (event entity, TargetElement buffer)

### Candidate 4: Re-apply Emergency flag system

- **Approach**: System running after PoliceCarAISystem that re-applies Emergency flag to mod-controlled cars
- **Risk level**: Low
- **Side effects**: Overrides PoliceCarAISystem decisions; must track which cars are mod-controlled

## Examples

### Example 1: Activate Emergency Lights on a Police Car

Force any police car entity to activate its lights and sirens by setting `CarFlags.Emergency` and tagging it for rendering update.

```csharp
using Game.Common;
using Game.Vehicles;
using Unity.Entities;

/// <summary>
/// Activates emergency lights and sirens on a specific police car entity.
/// Call from within a system's OnUpdate or from a tool system.
/// </summary>
public void ActivateEmergencyLights(EntityManager em, Entity policeCarEntity)
{
    if (!em.HasComponent<Car>(policeCarEntity)) return;
    if (!em.HasComponent<PoliceCar>(policeCarEntity)) return;

    // Set the Emergency flag on the Car component
    Car car = em.GetComponentData<Car>(policeCarEntity);
    car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
    em.SetComponentData(policeCarEntity, car);

    // Tag for rendering update so lights/sirens appear
    if (!em.HasComponent<EffectsUpdated>(policeCarEntity))
    {
        em.AddComponent<EffectsUpdated>(policeCarEntity);
    }
}
```

### Example 2: Full Direct Dispatch -- Send Police Car to Target with Sirens (RECOMMENDED)

The RECOMMENDED approach for reliably sending a police car to a specific entity with lights and sirens. **Critical**: Injects a `ServiceDispatch` buffer entry so that `ResetPath()` maintains the `CarFlags.Emergency` flag after path recalculation.

```csharp
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

public partial class ForcePoliceToLocationSystem : GameSystemBase
{
    protected override void OnCreate() { base.OnCreate(); }
    protected override void OnUpdate() { }

    /// <summary>
    /// Send a specific police car to a target entity with lights and sirens.
    /// The targetEntity should be a road segment, building, or entity on the road network.
    /// </summary>
    public void SendPoliceCarTo(Entity policeCarEntity, Entity targetEntity)
    {
        // 1. Create a PoliceEmergencyRequest entity for the ServiceDispatch buffer.
        //    ResetPath() reads ServiceDispatch[0] to decide whether to set Emergency
        //    or patrol flags. Without this, ResetPath() clears Emergency.
        Entity request = EntityManager.CreateEntity();
        EntityManager.AddComponentData(request, new ServiceRequest());
        EntityManager.AddComponentData(request, new PoliceEmergencyRequest
        {
            m_Site = targetEntity,
            m_Target = targetEntity,
            m_Priority = 1f,
            m_Purpose = PolicePurpose.Emergency
        });

        // 2. Inject the request into the vehicle's ServiceDispatch buffer
        DynamicBuffer<ServiceDispatch> dispatches =
            EntityManager.GetBuffer<ServiceDispatch>(policeCarEntity);
        dispatches.Clear();
        dispatches.Add(new ServiceDispatch { m_Request = request });

        // 3. Set emergency flags on Car component
        Car car = EntityManager.GetComponentData<Car>(policeCarEntity);
        car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
        car.m_Flags &= ~CarFlags.AnyLaneTarget;
        EntityManager.SetComponentData(policeCarEntity, car);

        // 4. Set AccidentTarget state on PoliceCar component
        PoliceCar policeCar = EntityManager.GetComponentData<PoliceCar>(policeCarEntity);
        policeCar.m_State |= PoliceCarFlags.AccidentTarget;
        policeCar.m_State &= ~(PoliceCarFlags.Returning | PoliceCarFlags.AtTarget
                               | PoliceCarFlags.Cancelled);
        EntityManager.SetComponentData(policeCarEntity, policeCar);

        // 5. Set navigation target
        EntityManager.SetComponentData(policeCarEntity, new Target(targetEntity));

        // 6. Request new path calculation
        PathOwner pathOwner = EntityManager.GetComponentData<PathOwner>(policeCarEntity);
        pathOwner.m_State |= PathFlags.Updated;
        EntityManager.SetComponentData(policeCarEntity, pathOwner);

        // 7. Trigger rendering update for lights/sirens
        if (!EntityManager.HasComponent<EffectsUpdated>(policeCarEntity))
        {
            EntityManager.AddComponent<EffectsUpdated>(policeCarEntity);
        }
    }

    /// <summary>
    /// Clean up the request entity when the dispatch is complete.
    /// Call after the car has arrived or the dispatch is cancelled.
    /// </summary>
    public void CleanUpRequest(Entity requestEntity)
    {
        if (requestEntity != Entity.Null && EntityManager.Exists(requestEntity))
        {
            EntityManager.DestroyEntity(requestEntity);
        }
    }
}
```

### Example 3: Request-Based Dispatch with Full Event Lifecycle

Creates a complete fake crime event that survives `AccidentSiteSystem` validation. More complex but uses the game's full dispatch pipeline.

> **Warning**: This approach requires careful lifecycle management. The mod must clean up
> the event entity, AccidentSite, and Criminal/InvolvedInAccident components when done.

> **Known Issue**: When the dispatched car picks up this request via `SelectNextDispatch()`,
> the request entity won't have a `PathElement` buffer (the game's dispatch system adds this
> during pathfinding). This means `SelectNextDispatch` falls through to the path where
> `CarFlags.Emergency` is NOT set and `CarLaneFlags.EndOfPath` is NOT cleared. Use the
> hybrid approach from Example 2 (direct flag manipulation + ServiceDispatch injection) for
> reliable emergency dispatch. This example is best used when you want the game's dispatch
> system to choose the vehicle rather than targeting a specific car.

```csharp
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(AccidentSiteSystem))]
public partial class FullPipelinePoliceDispatchSystem : GameSystemBase
{
    private SimulationSystem m_SimulationSystem;
    private Entity m_EventEntity;
    private Entity m_DummyCriminalEntity;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
    }

    protected override void OnUpdate() { }

    protected override void OnDestroy()
    {
        CleanUp();
        base.OnDestroy();
    }

    /// <summary>
    /// Dispatch police to a target road segment or building using the full pipeline.
    /// The target must be on the road network for pathfinding to succeed.
    /// </summary>
    public void DispatchTo(Entity targetEntity)
    {
        CleanUp();

        uint frame = m_SimulationSystem.frameIndex;

        // 1. Create persistent event entity with TargetElement buffer
        m_EventEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData<Game.Events.Event>(m_EventEntity, default);
        var buf = EntityManager.AddBuffer<TargetElement>(m_EventEntity);

        // 2. Create a dummy entity with InvolvedInAccident to satisfy
        //    AccidentSiteSystem's validation (needs severity > 0, not moving)
        m_DummyCriminalEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(m_DummyCriminalEntity, new InvolvedInAccident
        {
            m_Event = m_EventEntity,
            m_Severity = 1f,
            m_InvolvedFrame = frame
        });
        // Add PrefabRef so AccidentSiteSystem considers it valid
        // (entity2 validity check requires PrefabRef -- actually it just
        //  checks entity != Entity.Null, but PrefabRef helps other systems)

        // Add dummy to event's target list
        buf.Add(new TargetElement { m_Entity = m_DummyCriminalEntity });

        // 3. Add AccidentSite to the target entity
        if (!EntityManager.HasComponent<AccidentSite>(targetEntity))
        {
            EntityManager.AddComponentData(targetEntity, new AccidentSite
            {
                m_Event = m_EventEntity,
                m_Flags = AccidentSiteFlags.TrafficAccident
                        | AccidentSiteFlags.RequirePolice,
                m_PoliceRequest = Entity.Null,
                m_CreationFrame = frame
            });
        }
        else
        {
            AccidentSite site = EntityManager.GetComponentData<AccidentSite>(targetEntity);
            site.m_Event = m_EventEntity;
            site.m_Flags |= AccidentSiteFlags.RequirePolice;
            EntityManager.SetComponentData(targetEntity, site);
        }

        // 4. Create the police emergency request
        Entity request = EntityManager.CreateEntity();
        EntityManager.AddComponentData(request, new ServiceRequest());
        EntityManager.AddComponentData(request, new PoliceEmergencyRequest(
            targetEntity,   // site
            targetEntity,   // target
            1f,             // priority
            PolicePurpose.Emergency
        ));
        EntityManager.AddComponentData(request, new RequestGroup(4u));
    }

    /// <summary>
    /// Clean up all entities created by the dispatch.
    /// Call when the dispatch is no longer needed.
    /// </summary>
    public void CleanUp()
    {
        if (m_EventEntity != Entity.Null && EntityManager.Exists(m_EventEntity))
        {
            EntityManager.DestroyEntity(m_EventEntity);
            m_EventEntity = Entity.Null;
        }
        if (m_DummyCriminalEntity != Entity.Null && EntityManager.Exists(m_DummyCriminalEntity))
        {
            EntityManager.DestroyEntity(m_DummyCriminalEntity);
            m_DummyCriminalEntity = Entity.Null;
        }
    }
}
```

### Example 4: Find All Available Police Cars

Query for police cars that are available for dispatch (not busy, not returning, not disabled).

```csharp
using Game.Common;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

public partial class FindAvailablePoliceCarsSystem : GameSystemBase
{
    private EntityQuery m_PoliceCarQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PoliceCarQuery = GetEntityQuery(
            ComponentType.ReadOnly<PoliceCar>(),
            ComponentType.ReadOnly<Car>(),
            ComponentType.ReadOnly<Target>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate() { }

    public NativeList<Entity> GetAvailablePoliceCars(Allocator allocator)
    {
        NativeList<Entity> result = new NativeList<Entity>(allocator);
        NativeArray<Entity> entities = m_PoliceCarQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            PoliceCar pc = EntityManager.GetComponentData<PoliceCar>(entities[i]);
            // Available if: not returning, not at target, not shift ended, not disabled
            if ((pc.m_State & (PoliceCarFlags.Returning | PoliceCarFlags.AtTarget
                             | PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) == 0
                && pc.m_RequestCount <= 1)
            {
                result.Add(entities[i]);
            }
        }
        entities.Dispose();
        return result;
    }
}
```

### Example 5: Monitor Police Emergency Response

Watch for police cars responding to emergencies and log their status.

```csharp
using Game.Common;
using Game.Simulation;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

public partial class PoliceResponseMonitorSystem : GameSystemBase
{
    private EntityQuery m_EmergencyPoliceQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EmergencyPoliceQuery = GetEntityQuery(
            ComponentType.ReadOnly<PoliceCar>(),
            ComponentType.ReadOnly<Car>(),
            ComponentType.ReadOnly<Target>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<Entity> entities = m_EmergencyPoliceQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Car car = EntityManager.GetComponentData<Car>(entities[i]);
            PoliceCar pc = EntityManager.GetComponentData<PoliceCar>(entities[i]);
            Target target = EntityManager.GetComponentData<Target>(entities[i]);

            if ((car.m_Flags & CarFlags.Emergency) != 0)
            {
                bool atTarget = (pc.m_State & PoliceCarFlags.AtTarget) != 0;
                bool accidentTarget = (pc.m_State & PoliceCarFlags.AccidentTarget) != 0;
                Log.Info($"Police car {entities[i]}: Emergency active, " +
                         $"target={target.m_Target}, " +
                         $"atTarget={atTarget}, " +
                         $"accidentTarget={accidentTarget}, " +
                         $"requests={pc.m_RequestCount}");
            }
        }
        entities.Dispose();
    }
}
```

### Example 6: Persistent Emergency Flag Guardian

A system that prevents `PoliceCarAISystem` from clearing the Emergency flag on mod-controlled cars. Use this when directly manipulating cars to ensure lights stay on.

```csharp
using Game.Common;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Runs after PoliceCarAISystem to re-apply Emergency flags on mod-controlled cars.
/// Add a ModControlledEmergency tag component to cars you want to keep in emergency mode.
/// </summary>
[UpdateAfter(typeof(PoliceCarAISystem))]
public partial class EmergencyFlagGuardianSystem : GameSystemBase
{
    public struct ModControlledEmergency : IComponentData { }

    private EntityQuery m_ModCarQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_ModCarQuery = GetEntityQuery(
            ComponentType.ReadOnly<ModControlledEmergency>(),
            ComponentType.ReadWrite<Car>(),
            ComponentType.Exclude<Deleted>()
        );
        RequireForUpdate(m_ModCarQuery);
    }

    protected override void OnUpdate()
    {
        NativeArray<Entity> entities = m_ModCarQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Car car = EntityManager.GetComponentData<Car>(entities[i]);
            if ((car.m_Flags & CarFlags.Emergency) == 0)
            {
                car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad
                             | CarFlags.UsePublicTransportLanes;
                car.m_Flags &= ~CarFlags.AnyLaneTarget;
                EntityManager.SetComponentData(entities[i], car);

                if (!EntityManager.HasComponent<EffectsUpdated>(entities[i]))
                {
                    EntityManager.AddComponent<EffectsUpdated>(entities[i]);
                }
            }
        }
        entities.Dispose();
    }
}
```

### Example 7: Complete Dispatch-and-Guard System

The most robust approach: combines direct vehicle manipulation with a guardian system to keep lights on.

```csharp
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Complete system for dispatching police cars with persistent lights/sirens.
/// Combines finding available cars, setting emergency flags, and guarding
/// against PoliceCarAISystem clearing them.
/// </summary>
[UpdateAfter(typeof(PoliceCarAISystem))]
public partial class RobustPoliceDispatchSystem : GameSystemBase
{
    /// <summary>Tag component to track mod-dispatched cars.</summary>
    public struct ModDispatched : IComponentData
    {
        public Entity m_Target;
    }

    private EntityQuery m_PoliceCarQuery;
    private EntityQuery m_ModDispatchedQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PoliceCarQuery = GetEntityQuery(
            ComponentType.ReadOnly<PoliceCar>(),
            ComponentType.ReadOnly<Car>(),
            ComponentType.ReadOnly<Target>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
        m_ModDispatchedQuery = GetEntityQuery(
            ComponentType.ReadOnly<ModDispatched>(),
            ComponentType.ReadWrite<Car>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        // Guardian: re-apply Emergency on mod-dispatched cars
        NativeArray<Entity> modCars = m_ModDispatchedQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < modCars.Length; i++)
        {
            ModDispatched md = EntityManager.GetComponentData<ModDispatched>(modCars[i]);
            Target target = EntityManager.GetComponentData<Target>(modCars[i]);

            // If PoliceCarAISystem changed the target, re-set it
            if (target.m_Target != md.m_Target && md.m_Target != Entity.Null
                && EntityManager.Exists(md.m_Target))
            {
                EntityManager.SetComponentData(modCars[i], new Target(md.m_Target));
            }

            Car car = EntityManager.GetComponentData<Car>(modCars[i]);
            if ((car.m_Flags & CarFlags.Emergency) == 0)
            {
                car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad
                             | CarFlags.UsePublicTransportLanes;
                car.m_Flags &= ~CarFlags.AnyLaneTarget;
                EntityManager.SetComponentData(modCars[i], car);

                if (!EntityManager.HasComponent<EffectsUpdated>(modCars[i]))
                {
                    EntityManager.AddComponent<EffectsUpdated>(modCars[i]);
                }
            }

            // Check if car has arrived (AtTarget or close enough)
            PoliceCar pc = EntityManager.GetComponentData<PoliceCar>(modCars[i]);
            if ((pc.m_State & PoliceCarFlags.AtTarget) != 0)
            {
                // Car has arrived -- remove mod control, let normal AI take over
                EntityManager.RemoveComponent<ModDispatched>(modCars[i]);
            }
        }
        modCars.Dispose();
    }

    /// <summary>
    /// Find nearest available police car and dispatch it with sirens.
    /// Returns the dispatched car entity, or Entity.Null if none available.
    /// </summary>
    public Entity DispatchNearestTo(Entity targetEntity)
    {
        NativeArray<Entity> entities = m_PoliceCarQuery.ToEntityArray(Allocator.Temp);
        Entity bestCar = Entity.Null;

        for (int i = 0; i < entities.Length; i++)
        {
            PoliceCar pc = EntityManager.GetComponentData<PoliceCar>(entities[i]);

            // Skip cars that are busy, returning, disabled, or already mod-controlled
            if ((pc.m_State & (PoliceCarFlags.Returning | PoliceCarFlags.AtTarget
                             | PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) != 0)
                continue;
            if (pc.m_RequestCount > 1) continue;
            if (EntityManager.HasComponent<ModDispatched>(entities[i])) continue;

            // TODO: Could add distance-based selection here
            bestCar = entities[i];
            break;
        }
        entities.Dispose();

        if (bestCar == Entity.Null) return Entity.Null;

        // Set emergency flags
        Car car = EntityManager.GetComponentData<Car>(bestCar);
        car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad
                     | CarFlags.UsePublicTransportLanes;
        car.m_Flags &= ~CarFlags.AnyLaneTarget;
        EntityManager.SetComponentData(bestCar, car);

        // Set PoliceCar state
        PoliceCar policeCar = EntityManager.GetComponentData<PoliceCar>(bestCar);
        policeCar.m_State |= PoliceCarFlags.AccidentTarget;
        policeCar.m_State &= ~(PoliceCarFlags.Returning | PoliceCarFlags.AtTarget
                               | PoliceCarFlags.Cancelled);
        EntityManager.SetComponentData(bestCar, policeCar);

        // Set target
        EntityManager.SetComponentData(bestCar, new Target(targetEntity));

        // Request path
        PathOwner po = EntityManager.GetComponentData<PathOwner>(bestCar);
        po.m_State |= PathFlags.Updated;
        EntityManager.SetComponentData(bestCar, po);

        // Trigger visual update
        if (!EntityManager.HasComponent<EffectsUpdated>(bestCar))
        {
            EntityManager.AddComponent<EffectsUpdated>(bestCar);
        }

        // Tag for guardian tracking
        EntityManager.AddComponentData(bestCar, new ModDispatched
        {
            m_Target = targetEntity
        });

        return bestCar;
    }

    /// <summary>
    /// Release a mod-dispatched car back to normal AI control.
    /// </summary>
    public void ReleasePolicecar(Entity policeCarEntity)
    {
        if (EntityManager.HasComponent<ModDispatched>(policeCarEntity))
        {
            EntityManager.RemoveComponent<ModDispatched>(policeCarEntity);
        }
    }
}
```

## Open Questions

- [x] How are lights and sirens activated? -- Via `CarFlags.Emergency` flag on `Car` component + `EffectsUpdated` tag
- [x] How does dispatch select which car to send? -- Pathfinding via `SetupTargetType.PolicePatrol` origin in target's district
- [x] Why does the request pipeline fail for mods? -- District resolution failure, AccidentSite cleared/removed by AccidentSiteSystem
- [x] Can a mod create a working dispatch request? -- Yes, but requires full event lifecycle (event entity + TargetElement + InvolvedInAccident/Criminal)
- [x] What's the difference between emergency and patrol dispatch? -- Emergency sets CarFlags.Emergency (lights/sirens), patrol clears it
- [x] How does the game prevent double-dispatch? -- AccidentSite.m_PoliceRequest + Dispatched component + station/car m_TargetRequest
- [x] What traffic rules do emergency vehicles ignore? -- All: ForbidCombustionEngines, ForbidTransitTraffic, ForbidHeavyTraffic, ForbidPrivateTraffic, ForbidSlowTraffic, AvoidBicycles. Plus CarNavigationSystem grants additional lane/signal exemptions via CarFlags.Emergency.
- [x] How does IsCloseEnough work? -- Checks 30m distance to target Transform, or to any InvolvedInAccident entity in the event's TargetElement buffer
- [ ] Exact rendering pipeline path from CarFlags.Emergency to siren mesh/audio activation -- deep in the game's rendering subsystem, not accessible via Game.dll decompilation
- [ ] How exactly does CarNavigationSystem make other vehicles yield? -- Likely in CarNavigationHelpers.LaneReservation, not fully traced

## Sources

- Decompiled from: Game.dll (Cities: Skylines II) using ilspycmd v9.1
- Types decompiled (full): Game.Simulation.PoliceCarAISystem, Game.Simulation.PoliceEmergencyDispatchSystem, Game.Simulation.PolicePatrolDispatchSystem, Game.Simulation.PoliceStationAISystem, Game.Simulation.AccidentSiteSystem
- Types decompiled (components): Game.Vehicles.PoliceCar, Game.Vehicles.Car, Game.Vehicles.PoliceCarFlags, Game.Vehicles.CarFlags, Game.Vehicles.CarCurrentLane, Game.Vehicles.CarLaneFlags, Game.Net.CarLaneFlags, Game.Buildings.PoliceStation, Game.Buildings.PoliceStationFlags, Game.Buildings.CrimeProducer, Game.Citizens.Criminal, Game.Citizens.CriminalFlags, Game.Events.AccidentSite, Game.Events.AccidentSiteFlags, Game.Events.InvolvedInAccident, Game.Simulation.PoliceEmergencyRequest, Game.Simulation.PolicePatrolRequest, Game.Simulation.ServiceRequest, Game.Simulation.ServiceDispatch, Game.Simulation.Dispatched, Game.Common.Target, Game.Common.EffectsUpdated, Game.Prefabs.PoliceCarData, Game.Prefabs.PoliceStationData, Game.Prefabs.PoliceConfigurationData, Game.Prefabs.PolicePurpose
- Related research: [Emergency Dispatch](../EmergencyDispatch/) (general dispatch framework), [Crime Trigger](../CrimeTrigger/) (what triggers police)
