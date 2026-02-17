# Research: Police Dispatch with Lights and Sirens

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How police vehicles are dispatched to specific locations with emergency lights and sirens activated, and how a mod can trigger this programmatically.

**Why**: To build a mod that can programmatically dispatch police cars with lights and sirens to any world location -- useful for scripted events, custom emergency scenarios, or enhanced police behavior mods.

**Boundaries**: Out of scope: police helicopter AI (`PoliceAircraftAISystem`), prisoner transport (`PrisonerTransportDispatchSystem`), crime accumulation internals (`CrimeAccumulationSystem`), and patrol route optimization.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | PoliceEmergencyDispatchSystem, PolicePatrolDispatchSystem, PoliceCarAISystem, PoliceStationAISystem, PoliceEmergencyRequest, PolicePatrolRequest, ServiceRequest, ServiceDispatch, Dispatched |
| Game.dll | Game.Vehicles | PoliceCar (component), PoliceCarFlags, Car, CarFlags, CarCurrentLane |
| Game.dll | Game.Buildings | PoliceStation (component), PoliceStationFlags, CrimeProducer |
| Game.dll | Game.Events | AccidentSite, AccidentSiteFlags, InvolvedInAccident |
| Game.dll | Game.Prefabs | PoliceCarData, PoliceStationData, PoliceConfigurationData, PolicePurpose, PoliceCar (prefab) |
| Game.dll | Game.Common | Target, EffectsUpdated |
| Game.dll | Game.Pathfind | PathOwner, PathInformation, PathElement |

## Component Map

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

*Source: `Game.dll` -> `Game.Vehicles.PoliceCar`*

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

### `CarFlags` (Game.Vehicles) -- Key Flags

| Flag | Value | Description |
|------|-------|-------------|
| **Emergency** | **0x01** | **ENABLES LIGHTS AND SIRENS. Also grants lane rule exemptions.** |
| StayOnRoad | 0x02 | Prevents the car from using parking/garage lanes |
| AnyLaneTarget | 0x04 | Can target any lane position (used during patrol) |
| Warning | 0x08 | Warning lights (not full emergency) |
| UsePublicTransportLanes | 0x20 | Can use bus/tram lanes |

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

## System Map

### `PoliceEmergencyDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames)
- **Queries**: Entities with `PoliceEmergencyRequest` + `UpdateFrame`
- **Reads**: AccidentSite, PoliceStation, PoliceCar, ParkedCar, Transform, CurrentDistrict
- **Writes**: AccidentSite.m_PoliceRequest, PoliceStation.m_TargetRequest, PoliceCar.m_TargetRequest, ServiceDispatch buffer
- **Key methods**:
  - `ValidateSite()` -- checks AccidentSite has RequirePolice set and is not Secured
  - `FindVehicleSource()` -- enqueues pathfind from available stations/cars to target
  - `DispatchVehicle()` -- adds Dispatched component and ServiceDispatch buffer entry
  - `ValidateReversed()` -- for reverse requests (station/car seeking work), checks availability

### `PolicePatrolDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames)
- **Queries**: Entities with `PolicePatrolRequest` + `UpdateFrame`
- **Reads**: CrimeProducer, PoliceStation, PoliceCar, PoliceConfigurationData
- **Writes**: CrimeProducer.m_PatrolRequest, PoliceStation.m_TargetRequest, PoliceCar.m_TargetRequest
- **Key methods**:
  - `ValidateTarget()` -- checks CrimeProducer.m_Crime >= tolerance threshold
  - `FindVehicleSource()` -- enqueues pathfind including Flying method for helicopters
  - `FindVehicleTarget()` -- reverse: finds patrol targets for idle cars/stations

### `PoliceCarAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames, offset 5)
- **Queries**: Entities with `CarCurrentLane` + `Owner` + `PrefabRef` + `PathOwner` + `PoliceCar` + `Car` + `Target`, excluding Deleted/Temp/TripSource/OutOfControl
- **Reads**: PoliceCarData, PolicePatrolRequest, PoliceEmergencyRequest, AccidentSite, CrimeProducer, many pathfinding/lane components
- **Writes**: PoliceCar (state flags), Car (Emergency flag), Target, PathOwner, CarCurrentLane, ServiceDispatch buffer, AccidentSite (Secured flag), CrimeProducer (crime reduction)
- **Key methods**:
  - `Tick()` -- main per-vehicle update loop
  - `SelectNextDispatch()` -- **THE KEY METHOD**: picks next service request, sets `CarFlags.Emergency` for emergency requests, clears it for patrol
  - `ResetPath()` -- after pathfinding completes, sets `CarFlags.Emergency` for emergency requests
  - `SecureAccidentSite()` -- at target, sets AccidentSite.Secured flag
  - `ReturnToStation()` -- clears AccidentTarget, sets Returning flag
  - `RequestTargetIfNeeded()` -- proactively creates reverse requests to seek new work

### `PoliceStationAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**: Entities with `PoliceStation` + `PrefabRef`, various vehicle lookups
- **Reads**: OwnedVehicle buffer, PoliceCar states, PoliceCarData, Efficiency
- **Writes**: PoliceStation.m_Flags (HasAvailablePatrolCars, HasAvailablePoliceHelicopters)
- **Key methods**:
  - `PoliceStationTickJob` -- counts available vehicles, sets station flags, manages shift schedules, handles dispatching/recalling vehicles

## Data Flow

```
TRIGGER: Crime event or accident occurs
    |
    v
AccidentSiteSystem (every 64 frames)
    Evaluates AccidentSite entities
    Sets RequirePolice flag if severity > 0 or unsecured crime detected
    Calls RequestPoliceIfNeeded() to create request
    |
    v
REQUEST CREATION
    Creates entity with:
      - ServiceRequest (base)
      - PoliceEmergencyRequest (site, target, priority, purpose)
      - RequestGroup(4)
    |
    v
ServiceRequestSystem
    Sees RequestGroup, assigns random UpdateFrame index
    Removes RequestGroup
    |
    v
PoliceEmergencyDispatchSystem (every 16 frames)
    1. ValidateSite(): AccidentSite has RequirePolice & not Secured?
    2. FindVehicleSource(): enqueue pathfind to find nearest available car
       - Origin: SetupTargetType.PolicePatrol (finds stations/cars)
       - Destination: SetupTargetType.AccidentLocation
       - Uses district-based location for priority matching
    ~~ pathfinding runs asynchronously ~~
    3. DispatchVehicle(): PathInformation.m_Origin found
       - Adds Dispatched(handler) to request
       - Adds ServiceDispatch(request) to handler's buffer
    |
    v
PoliceCarAISystem.SelectNextDispatch()
    Reads ServiceDispatch buffer
    If request is PoliceEmergencyRequest:
      - Sets PoliceCarFlags.AccidentTarget
      - Sets CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes
      - Clears CarFlags.AnyLaneTarget
      - Adds EffectsUpdated tag (triggers siren/light rendering)
    If request is PolicePatrolRequest:
      - Clears CarFlags.Emergency
      - Sets CarFlags.StayOnRoad | CarFlags.AnyLaneTarget | CarFlags.UsePublicTransportLanes
    Sets Target.m_Target = site entity
    |
    v
CAR NAVIGATION (CarNavigationSystem)
    CarFlags.Emergency causes:
      - Other vehicles yield (move out of the way)
      - Can use any lane including oncoming traffic
      - Ignores traffic signals
      - Emergency lights and siren visual/audio effects activated
    |
    v
ARRIVAL
    PoliceCarAISystem detects VehicleUtils.PathEndReached()
    OR IsCloseEnough() (within 30m of accident site)
    |
    v
ON SCENE
    SecureAccidentSite():
      - Sets PoliceCarFlags.AtTarget
      - Stops vehicle
      - Sets AccidentSite.m_Flags |= Secured (m_SecuredFrame = current frame)
    |
    v
COMPLETION
    When AccidentSite is secured:
      - Creates HandleRequest(completed: true)
      - ServiceRequestSystem destroys request entity
      - Car calls SelectNextDispatch() or ReturnToStation()
      - ReturnToStation() clears AccidentTarget, Emergency flags
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Criminal capacity | PoliceCarData.m_CriminalCapacity | Prefab: Game.Prefabs.PoliceCar.m_CriminalCapacity = 2 |
| Crime reduction rate | PoliceCarData.m_CrimeReductionRate | Prefab: Game.Prefabs.PoliceCar.m_CrimeReductionRate = 10000 |
| Shift duration | PoliceCarData.m_ShiftDuration | Prefab: Game.Prefabs.PoliceCar.m_ShiftDuration = 1.0 (multiplied by 262144) |
| Purpose mask | PoliceCarData.m_PurposeMask | Prefab: Patrol + Emergency (default) |
| Patrol car capacity | PoliceStationData.m_PatrolCarCapacity | Prefab: Game.Prefabs.PoliceStation.m_PatrolCarCapacity = 10 |
| Helicopter capacity | PoliceStationData.m_PoliceHelicopterCapacity | Prefab: Game.Prefabs.PoliceStation.m_PoliceHelicopterCapacity = 0 |
| Jail capacity | PoliceStationData.m_JailCapacity | Prefab: Game.Prefabs.PoliceStation.m_JailCapacity = 15 |
| Crime tolerance threshold | PoliceConfigurationData.m_CrimeAccumulationTolerance | Singleton prefab |
| Dispatch update interval | Hardcoded: 16 frames | All dispatch systems |
| Pathfind max speed | Hardcoded: 111.111115f (400 km/h) | Emergency dispatch pathfinding |
| Close enough distance | Hardcoded: 30f | PoliceCarAISystem.IsCloseEnough() |

## Harmony Patch Points

### Candidate 1: `PoliceCarAISystem.SelectNextDispatch` (via PoliceCarTickJob)

- **Signature**: `private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, ...)`
- **Patch type**: Transpiler (Burst-compiled job -- cannot directly Harmony patch)
- **What it enables**: Modify which flags are set when dispatching (e.g., always set Emergency)
- **Risk level**: High -- Burst-compiled, not directly patchable
- **Side effects**: Part of a job struct, requires transpiler or alternative approach

### Candidate 2: Direct ECS manipulation (Recommended)

- **Approach**: Custom GameSystemBase that modifies Car.m_Flags and PoliceCar.m_State after PoliceCarAISystem runs
- **Patch type**: No Harmony needed -- pure ECS system
- **What it enables**: Set CarFlags.Emergency on any police car, set Target, trigger pathfinding
- **Risk level**: Low
- **Side effects**: Must add EffectsUpdated tag to trigger rendering update

### Candidate 3: Create emergency request entities directly

- **Approach**: Create PoliceEmergencyRequest entities pointing to a synthetic AccidentSite
- **Patch type**: No Harmony needed -- entity creation
- **What it enables**: Full dispatch pipeline including pathfinding and vehicle selection
- **Risk level**: Low-Medium (requires maintaining AccidentSite lifecycle)
- **Side effects**: AccidentSiteSystem may clear RequirePolice if system ordering is wrong

## Mod Blueprint

- **Systems to create**:
  1. `PoliceDispatchCommandSystem` -- creates dispatch requests or directly manipulates car entities
  2. Optionally: `SyntheticAccidentSiteSystem` -- manages fake AccidentSite entities for dispatch targets
- **Components to add**: None required (uses existing game components). Optionally a custom tag component to track mod-dispatched cars.
- **Patches needed**: None for the basic approach. Harmony prefix on `AccidentSiteSystem` only if you need to prevent RequirePolice clearing on synthetic sites.
- **Settings**: Target location (float3), dispatch purpose (emergency vs patrol), whether to activate lights/sirens
- **UI changes**: Optional button or hotkey to trigger dispatch

## The Key Insight: CarFlags.Emergency

The `CarFlags.Emergency` flag on the `Car` component is the **single flag** that controls:

1. **Emergency lights and sirens** (visual and audio effects via the rendering pipeline)
2. **Traffic yielding** (other vehicles move out of the way)
3. **Lane rule exemptions** (can use any lane, ignore traffic signals)
4. **Public transport lane access** (combined with UsePublicTransportLanes flag)

When `PoliceCarAISystem.SelectNextDispatch()` processes an emergency request, it sets:
```csharp
car.m_Flags &= ~CarFlags.AnyLaneTarget;
car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
```

When processing a patrol request, it clears Emergency:
```csharp
car.m_Flags &= ~CarFlags.Emergency;
car.m_Flags |= CarFlags.StayOnRoad | CarFlags.AnyLaneTarget | CarFlags.UsePublicTransportLanes;
```

After any flag change, `EffectsUpdated` is added to trigger the rendering system:
```csharp
m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
```

## Examples

### Example 1: Directly Set Emergency Lights on a Police Car

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

### Example 2: Dispatch Police to a Specific Building via Emergency Request

> **Warning**: This request-based approach is **unreliable for mod-created requests**. While the
> code compiles and creates valid-looking entities, the `PoliceEmergencyDispatchSystem` pipeline
> often silently fails to match and dispatch vehicles for synthetically created requests. The
> pathfinding and reverse-dispatch matching assumes requests originate from real game events, and
> mod-created requests frequently get stuck without ever dispatching a car.
>
> **Use [Example 3](#example-3-force-a-police-car-to-drive-to-world-coordinates-with-sirens)
> (direct car manipulation) as the preferred approach** for reliably sending police cars to
> specific locations.

Create a full emergency dispatch request targeting a specific building. This requires an AccidentSite component on the target for validation.

```csharp
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;

public partial class DispatchPoliceToLocationSystem : GameSystemBase
{
    /// <summary>Tracks the dummy event entity so it can be cleaned up.</summary>
    private Entity m_DummyEventEntity;

    protected override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnUpdate() { }

    protected override void OnDestroy()
    {
        // Clean up the dummy event entity if it exists
        if (m_DummyEventEntity != Entity.Null
            && EntityManager.Exists(m_DummyEventEntity))
        {
            EntityManager.DestroyEntity(m_DummyEventEntity);
        }
        base.OnDestroy();
    }

    /// <summary>
    /// Dispatch police to a target entity. The target must have an AccidentSite
    /// component with RequirePolice set, or the dispatch system will reject it.
    /// </summary>
    public void DispatchTo(Entity targetEntity)
    {
        // Create a dummy Event entity so AccidentSiteSystem does not
        // immediately remove the AccidentSite component.
        // Entity.Null in m_Event causes AccidentSiteSystem to treat the
        // site as invalid and remove it.
        if (m_DummyEventEntity == Entity.Null
            || !EntityManager.Exists(m_DummyEventEntity))
        {
            m_DummyEventEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData<Game.Common.Event>(
                m_DummyEventEntity, default);
        }

        // Ensure target has AccidentSite with RequirePolice
        if (!EntityManager.HasComponent<AccidentSite>(targetEntity))
        {
            EntityManager.AddComponentData(targetEntity, new AccidentSite
            {
                m_Flags = AccidentSiteFlags.RequirePolice | AccidentSiteFlags.CrimeScene
                        | AccidentSiteFlags.CrimeDetected,
                m_Event = m_DummyEventEntity,
                m_PoliceRequest = Entity.Null
            });
        }
        else
        {
            AccidentSite site = EntityManager.GetComponentData<AccidentSite>(targetEntity);
            site.m_Flags |= AccidentSiteFlags.RequirePolice;
            if (site.m_Event == Entity.Null)
            {
                site.m_Event = m_DummyEventEntity;
            }
            EntityManager.SetComponentData(targetEntity, site);
        }

        // Create emergency request entity with individual AddComponentData
        // calls (CreateArchetype is not available on netstandard2.1)
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
}
```

### Example 3: Force a Police Car to Drive to World Coordinates with Sirens

Directly set a police car's target and pathfinding to reach specific coordinates with emergency lights active.

```csharp
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

public partial class ForcePoliceToLocationSystem : GameSystemBase
{
    private PathfindSetupSystem m_PathfindSetupSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PathfindSetupSystem = World.GetOrCreateSystemManaged<PathfindSetupSystem>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Send a specific police car to world coordinates with lights and sirens.
    /// The targetEntity should be an entity near or at the destination (e.g., a road segment).
    /// </summary>
    public void SendPoliceCarTo(Entity policeCarEntity, Entity targetEntity)
    {
        // 1. Set emergency flags
        Car car = EntityManager.GetComponentData<Car>(policeCarEntity);
        car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
        car.m_Flags &= ~CarFlags.AnyLaneTarget;
        EntityManager.SetComponentData(policeCarEntity, car);

        // 2. Set AccidentTarget state
        PoliceCar policeCar = EntityManager.GetComponentData<PoliceCar>(policeCarEntity);
        policeCar.m_State |= PoliceCarFlags.AccidentTarget;
        policeCar.m_State &= ~(PoliceCarFlags.Returning | PoliceCarFlags.AtTarget
                               | PoliceCarFlags.Cancelled);
        EntityManager.SetComponentData(policeCarEntity, policeCar);

        // 3. Set navigation target
        Target target = new Target(targetEntity);
        EntityManager.SetComponentData(policeCarEntity, target);

        // 4. Request new path
        PathOwner pathOwner = EntityManager.GetComponentData<PathOwner>(policeCarEntity);
        pathOwner.m_State |= PathFlags.Updated;
        EntityManager.SetComponentData(policeCarEntity, pathOwner);

        // 5. Trigger rendering update for lights/sirens
        if (!EntityManager.HasComponent<EffectsUpdated>(policeCarEntity))
        {
            EntityManager.AddComponent<EffectsUpdated>(policeCarEntity);
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

## Open Questions

- [x] How are lights and sirens activated? -- Via `CarFlags.Emergency` flag on the `Car` component, combined with `EffectsUpdated` tag for rendering
- [x] How does dispatch select which car to send? -- Pathfinding: finds nearest available car/station via `SetupTargetType.PolicePatrol` origin query
- [ ] Can a mod create a standalone dispatch target without AccidentSite? -- The PoliceEmergencyDispatchSystem.ValidateSite() requires AccidentSite with RequirePolice. A custom dispatch system could bypass this validation.
- [ ] How does the rendering pipeline convert CarFlags.Emergency to actual siren audio and flashing lights? -- The EffectsUpdated tag triggers an effect recalculation, but the specific rendering system that reads CarFlags.Emergency and enables the siren mesh/audio is deep in the rendering pipeline and not fully traced.
- [ ] What is the exact behavior when a police car with Emergency flag encounters traffic? Does CarNavigationSystem explicitly check for Emergency to enable lane-switching and signal-ignoring, or is this handled at the pathfinding level?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II) using ilspycmd v9.1
- Types decompiled: Game.Simulation.PoliceEmergencyDispatchSystem, Game.Simulation.PolicePatrolDispatchSystem, Game.Simulation.PoliceCarAISystem, Game.Simulation.PoliceStationAISystem, Game.Vehicles.PoliceCar, Game.Vehicles.Car, Game.Vehicles.PoliceCarFlags, Game.Vehicles.CarFlags, Game.Buildings.PoliceStation, Game.Buildings.PoliceStationFlags, Game.Simulation.PoliceEmergencyRequest, Game.Simulation.PolicePatrolRequest, Game.Prefabs.PoliceCarData, Game.Prefabs.PoliceStationData, Game.Prefabs.PoliceConfigurationData, Game.Prefabs.PolicePurpose, Game.Prefabs.PoliceCar, Game.Prefabs.PoliceStation, Game.Common.Target, Game.Common.EffectsUpdated, Game.Simulation.ServiceRequest, Game.Simulation.ServiceDispatch, Game.Simulation.Dispatched
- Related research: [Emergency Dispatch](../EmergencyDispatch/) (general dispatch framework), [Crime Trigger](../CrimeTrigger/) (what triggers police)
