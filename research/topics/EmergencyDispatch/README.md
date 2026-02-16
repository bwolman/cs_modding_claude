# Research: Emergency Vehicle Dispatch

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

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

### `PoliceEmergencyDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Purpose**: Processes `PoliceEmergencyRequest` entities and dispatches police vehicles.
- **Queries**: Entities with `PoliceEmergencyRequest` + `UpdateFrame`
- **Key logic**:
  1. **ValidateSite()**: Checks that the `AccidentSite` still has `RequirePolice` flag set and is not already `Secured`. If the site no longer requires police, the request is destroyed.
  2. **FindVehicleSource()**: Pathfinds from `SetupTargetType.PolicePatrol` origin (police stations in target's district) to `SetupTargetType.AccidentLocation` destination.
  3. **Reversed requests**: Police stations/cars with available capacity create reversed requests to find work.
  4. **DispatchVehicle()**: Adds `Dispatched` component to request, adds `ServiceDispatch` element to the handler's buffer.
- **PolicePurpose filtering**: The `FindVehicleSource()` method passes `m_Purpose` as `m_Value` on the origin `SetupQueueTarget`. The pathfinder uses this to match police stations/cars whose `m_PurposeMask` includes the requested purpose.

### `FireRescueDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Purpose**: Processes `FireRescueRequest` entities and dispatches fire engines/helicopters.
- **Key logic**:
  1. **ValidateTarget()**: Checks that the target still has `OnFire` component OR `RescueTarget` component. If neither exists, request is destroyed.
  2. **FindVehicleSource()**: Pathfinds from `SetupTargetType.FireEngine` origin to the fire location. Supports road, flying (helicopter), and offroad paths.
  3. No filtering by request type in dispatch -- fire engines respond to both Fire and Disaster types.

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
- **Side effects**: Must ensure the target entity has the expected validation component (AccidentSite for police, OnFire/RescueTarget for fire, HealthProblem with RequireTransport for healthcare)

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

**2. Police dispatch requires AccidentSite with RequirePolice flag.**

The `PoliceEmergencyDispatchSystem.ValidateSite()` checks that the site entity has `AccidentSite` with `RequirePolice` set and `Secured` not set. If you want police to respond to something, you need an `AccidentSite` on the target (or bypass this by creating an AccidentSite component on any entity).

**3. Fire dispatch requires OnFire or RescueTarget on the target.**

The `FireRescueDispatchSystem.ValidateTarget()` checks for `OnFire` or `RescueTarget`. Without one of these, the request gets destroyed.

**4. Healthcare dispatch requires HealthProblem with RequireTransport.**

The `HealthcareDispatchSystem.ValidateTarget()` checks for `HealthProblem` with `RequireTransport` flag.

**5. Each service type has its own request type and dispatch system -- they are completely independent.**

Police requests go through `PoliceEmergencyDispatchSystem`. Fire requests go through `FireRescueDispatchSystem`. Healthcare requests go through `HealthcareDispatchSystem`. There is no cross-service dispatch in vanilla.

### How to force police to every vehicle accident

The AccidentSiteSystem unconditionally clears `RequirePolice` at the start of each entity's evaluation, then only re-sets it when `severity > 0` (from `InvolvedInAccident.m_Severity`) and a valid non-moving target exists. Accidents where all vehicles are still moving or have low severity will not trigger police. To force police dispatch:

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

### How to send fire engines to medical calls or accidents

Fire engines only respond to `FireRescueRequest` entities, and those require `OnFire` or `RescueTarget` on the target. You cannot make fire engines respond to healthcare requests through the existing systems. However:

**Approach**: Add `RescueTarget` component to accident site entities, then create a `FireRescueRequest`. The `FireRescueDispatchSystem` will validate the target via `RescueTarget` and dispatch a fire engine:

```csharp
using Game.Buildings;
using Game.Simulation;
using Unity.Entities;

// Add RescueTarget to accident entity so FireRescueDispatchSystem accepts it
if (!EntityManager.HasComponent<RescueTarget>(accidentEntity))
{
    EntityManager.AddComponentData(accidentEntity, new RescueTarget(Entity.Null));
}

// Create fire rescue request for that entity
Entity request = EntityManager.CreateEntity();
EntityManager.AddComponentData(request, new ServiceRequest());
EntityManager.AddComponentData(request, new FireRescueRequest(
    accidentEntity, 1f, FireRescueRequestType.Disaster));
EntityManager.AddComponentData(request, new RequestGroup(4u));
```

### How to dispatch an emergency vehicle programmatically (no event needed)

Create the appropriate request entity with the right archetype. The dispatch system will process it. The only requirement is that the target entity has the validation component:

- Police: target needs `AccidentSite` with `RequirePolice` flag
- Fire: target needs `OnFire` or `RescueTarget`
- Healthcare: target (citizen) needs `HealthProblem` with `RequireTransport`

If you want to dispatch without a real event, add the validation component to a target entity, then create the request.

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

## Open Questions

- [ ] How does the pathfinder's `SetupTargetType.PolicePatrol` matching work internally? Does it filter by `PolicePurpose` flags on the station, or by distance only?
- [x] When `RescueTarget` is added manually, does the fire engine AI know how to handle a non-fire target (e.g., accident scene)? **Answer**: The fire engine enters `Rescueing` state but requires a `Destroyed` component with `m_Cleared < 1.0` to do actual work. Without it, the engine arrives, finds nothing to do, and returns to depot.
- [ ] What happens if multiple requests exist for the same AccidentSite? The system checks `m_PoliceRequest` on AccidentSite -- does this prevent duplicates?
- [ ] How does the `Reversed` request flow work in practice? Police stations and vehicles with capacity create reversed requests to proactively find work. How does this interact with manually created requests?
- [ ] Does `PolicePatrolDispatchSystem` create requests that could also respond to accidents, or is patrol strictly separate from emergency?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key types: Game.Simulation.ServiceRequest, Game.Simulation.ServiceDispatch, Game.Simulation.Dispatched, Game.Simulation.HandleRequest, Game.Simulation.RequestGroup, Game.Simulation.PoliceEmergencyRequest, Game.Simulation.FireRescueRequest, Game.Simulation.HealthcareRequest, Game.Events.AccidentSite, Game.Events.OnFire, Game.Citizens.HealthProblem, Game.Buildings.RescueTarget, Game.Simulation.ServiceRequestSystem, Game.Simulation.AccidentSiteSystem, Game.Simulation.PoliceEmergencyDispatchSystem, Game.Simulation.FireRescueDispatchSystem, Game.Simulation.HealthcareDispatchSystem, Game.Simulation.FireSimulationSystem, Game.Simulation.HealthProblemSystem, Game.Simulation.CollapsedBuildingSystem, Game.Simulation.FireEngineAISystem
- Decompiled snippets saved to: `research/topics/EmergencyDispatch/snippets/`
