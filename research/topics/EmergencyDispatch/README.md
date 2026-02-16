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
  3. Tracks max severity from `InvolvedInAccident` components
  4. For crime scenes: manages detection delay (alarm delay from `CrimeData`)
  5. **Clears and re-evaluates `RequirePolice` flag each tick**
  6. Sets `RequirePolice` if severity > 0 or if crime is detected
  7. Calls `RequestPoliceIfNeeded()` which creates a `PoliceEmergencyRequest` entity if one does not already exist
- **Request archetype**: `ServiceRequest + PoliceEmergencyRequest + RequestGroup(4)`
- **Critical insight for modding**: The system only creates a police request if `severity > 0` (from `InvolvedInAccident`) AND a valid non-moving target exists, OR if it is a detected crime scene. The `RequirePolice` flag is recalculated from scratch each tick.

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
    |     AccidentSiteSystem: checks InvolvedInAccident severity
    |         |               recalculates RequirePolice flag
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

The AccidentSiteSystem only sets `RequirePolice` when `severity > 0` (from `InvolvedInAccident.m_Severity`). Accidents where all vehicles are still moving or have low severity may not trigger police. To force police dispatch:

**Approach A**: Custom system that runs after `AccidentSiteSystem` and sets `RequirePolice` on all `AccidentSite` entities with `TrafficAccident` flag:

```csharp
public partial class ForcePoliceToAccidentsSystem : GameSystemBase
{
    private EntityQuery m_AccidentQuery;
    private EntityArchetype m_PoliceRequestArchetype;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_AccidentQuery = GetEntityQuery(
            ComponentType.ReadWrite<AccidentSite>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>());
        m_PoliceRequestArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<ServiceRequest>(),
            ComponentType.ReadWrite<PoliceEmergencyRequest>(),
            ComponentType.ReadWrite<RequestGroup>());
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
                Entity request = EntityManager.CreateEntity(m_PoliceRequestArchetype);
                EntityManager.SetComponentData(request, new PoliceEmergencyRequest(
                    entities[i], entities[i], 1f, PolicePurpose.Emergency));
                EntityManager.SetComponentData(request, new RequestGroup(4u));
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
// Add RescueTarget to accident entity so FireRescueDispatchSystem accepts it
if (!EntityManager.HasComponent<RescueTarget>(accidentEntity))
{
    EntityManager.AddComponentData(accidentEntity, new RescueTarget(Entity.Null));
}

// Create fire rescue request for that entity
Entity request = EntityManager.CreateEntity(m_FireRescueRequestArchetype);
EntityManager.SetComponentData(request, new FireRescueRequest(
    accidentEntity, 1f, FireRescueRequestType.Disaster));
EntityManager.SetComponentData(request, new RequestGroup(4u));
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

## Open Questions

- [ ] How does the pathfinder's `SetupTargetType.PolicePatrol` matching work internally? Does it filter by `PolicePurpose` flags on the station, or by distance only?
- [ ] When `RescueTarget` is added manually, does the fire engine AI know how to handle a non-fire target (e.g., accident scene)? The vehicle AI after dispatch needs investigation.
- [ ] What happens if multiple requests exist for the same AccidentSite? The system checks `m_PoliceRequest` on AccidentSite -- does this prevent duplicates?
- [ ] How does the `Reversed` request flow work in practice? Police stations and vehicles with capacity create reversed requests to proactively find work. How does this interact with manually created requests?
- [ ] Does `PolicePatrolDispatchSystem` create requests that could also respond to accidents, or is patrol strictly separate from emergency?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key types: Game.Simulation.ServiceRequest, Game.Simulation.ServiceDispatch, Game.Simulation.Dispatched, Game.Simulation.HandleRequest, Game.Simulation.RequestGroup, Game.Simulation.PoliceEmergencyRequest, Game.Simulation.FireRescueRequest, Game.Simulation.HealthcareRequest, Game.Events.AccidentSite, Game.Events.OnFire, Game.Citizens.HealthProblem, Game.Simulation.ServiceRequestSystem, Game.Simulation.AccidentSiteSystem, Game.Simulation.PoliceEmergencyDispatchSystem, Game.Simulation.FireRescueDispatchSystem, Game.Simulation.HealthcareDispatchSystem, Game.Simulation.FireSimulationSystem, Game.Simulation.HealthProblemSystem
- Decompiled snippets saved to: `research/topics/EmergencyDispatch/snippets/`
