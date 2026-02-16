# Research: Vehicle OutOfControl & Accidents

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 handles vehicles going out of control, collisions, and accident aftermath (fire, injuries, police, cleanup).

**Why**: To build mods that trigger, modify, or respond to vehicle accident events -- controlling severity, physics, chain reactions, and emergency response.

**Boundaries**: Not covering general vehicle pathfinding, traffic AI decision-making, or non-accident vehicle damage (weather, aging). Crime systems are documented only where they intersect with AccidentSite.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Vehicles | `OutOfControl` marker component |
| Game.dll | Game.Events | `Impact`, `InvolvedInAccident`, `AccidentSite`, `TrafficAccident`, `AddAccidentSite`, `AccidentSiteFlags`, `ImpactSystem`, `AddAccidentSiteSystem` |
| Game.dll | Game.Simulation | `VehicleOutOfControlSystem`, `AccidentVehicleSystem`, `AccidentSiteSystem`, `AccidentCreatureSystem`, `ObjectCollisionSystem`, `DamagedVehicleSystem` |
| Game.dll | Game.Objects | `Moving`, `Damaged` |
| Game.dll | Game.Common | `Destroyed` |
| Game.dll | Game.Prefabs | `TrafficAccidentData`, `TrafficAccidentType` |

## Component Map

### `OutOfControl` (Game.Vehicles)

| Field | Type | Description |
|-------|------|-------------|
| *(empty)* | — | Marker struct, no fields |

Empty marker component (`IEmptySerializable`). Tags a vehicle entity as out of control -- it has left its lane and is governed by physics simulation rather than pathfinding.

*Source: `Game.dll` -> `Game.Vehicles.OutOfControl`*

### `InvolvedInAccident` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | Reference to the accident event entity |
| m_Severity | float | Severity of the impact (higher = more damage) |
| m_InvolvedFrame | uint | Simulation frame when the entity became involved |

Tags an entity (vehicle or creature) as part of an accident. The `m_InvolvedFrame` is used to calculate velocity thresholds for stopping (vehicles slow their threshold over time) and cleanup timing.

*Source: `Game.dll` -> `Game.Events.InvolvedInAccident`*

### `AccidentSite` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | Reference to the accident event entity |
| m_PoliceRequest | Entity | Reference to the police service request entity |
| m_Flags | AccidentSiteFlags | Bitfield controlling site behavior |
| m_CreationFrame | uint | Frame when the site was created |
| m_SecuredFrame | uint | Frame when police secured the site |

Marks a road edge or building as an accident location. Controls police dispatch, staged chain-reaction accidents, and cleanup timing.

*Source: `Game.dll` -> `Game.Events.AccidentSite`*

### `Impact` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | Reference to the accident event entity |
| m_Target | Entity | The entity being impacted |
| m_VelocityDelta | float3 | Linear velocity change to apply |
| m_AngularVelocityDelta | float3 | Angular velocity change to apply |
| m_Severity | float | Damage severity value |
| m_CheckStoppedEvent | bool | If true, skip if target is already stopped and in same event |

Temporary event component -- created as a standalone entity, processed by `ImpactSystem` within one frame, then destroyed. This is the primary way to inflict a collision on an entity.

*Source: `Game.dll` -> `Game.Events.Impact`*

### `Moving` (Game.Objects)

| Field | Type | Description |
|-------|------|-------------|
| m_Velocity | float3 | Linear velocity in world space (m/s) |
| m_AngularVelocity | float3 | Angular velocity (rad/s) |

Tracks an object's current velocity. Deserialization includes NaN safety checks (resets to zero if infinite).

*Source: `Game.dll` -> `Game.Objects.Moving`*

### `Damaged` (Game.Objects)

| Field | Type | Description |
|-------|------|-------------|
| m_Damage | float3 | Damage values: x=fire, y=structural, z=weather |

Three-channel damage state. The `x` (fire) component is used by `AccidentVehicleSystem` to calculate fire ignition probability.

*Source: `Game.dll` -> `Game.Objects.Damaged`*

### `Destroyed` (Game.Common)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | Reference to the destruction event |
| m_Cleared | float | Cleanup progress (0.0 to 1.0) |

Marks an object as destroyed. When `m_Cleared >= 1.0`, the vehicle is eligible for deletion by `AccidentVehicleSystem`.

*Source: `Game.dll` -> `Game.Common.Destroyed`*

### `TrafficAccident` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| *(empty)* | — | Marker struct, no fields |

Empty marker component. Tags an event entity as a traffic accident type.

*Source: `Game.dll` -> `Game.Events.TrafficAccident`*

### `AddAccidentSite` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The accident event entity |
| m_Target | Entity | The road edge/building to mark as site |
| m_Flags | AccidentSiteFlags | Flags to set on the new site |

Command component -- created as a temporary event entity, processed by `AddAccidentSiteSystem` to create or merge `AccidentSite` components.

*Source: `Game.dll` -> `Game.Events.AddAccidentSite`*

## System Map

### `ImpactSystem` (Game.Events)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every frame)
- **Queries**:
  - EntityQuery requiring: `[Impact, Event]`
- **Reads**: Impact (m_Target, m_VelocityDelta, m_AngularVelocityDelta, m_Severity, m_CheckStoppedEvent), Moving, Stopped, Vehicle, Car, CarTrailer, ParkedCar, CarCurrentLane, CarTrailerLane, PersonalCar, Taxi, Creature, PrefabRef, ParkingLane, GarageLane, CarLane
- **Writes**: Moving (adds velocity delta), InvolvedInAccident (adds/updates), OutOfControl (adds to vehicles), Stumbling (adds to creatures), Controller (detaches trailers)
- **Key methods**:
  - `AddImpactJob.Execute()` -- Iterates all Impact event entities. For each: applies velocity delta to target's Moving component, adds OutOfControl to vehicles (including parked/stopped cars which get activated first), adds Stumbling to creatures, and creates/updates InvolvedInAccident with highest severity. Also detaches trailers from their controllers.
  - `ActivateParkedCar()` -- Transitions a parked car to moving state (removes ParkedCar, adds navigation/pathfinding components)
  - `ActivateStoppedCar()` -- Transitions a stopped car back to moving (removes Stopped, adds Moving/TransformFrame/etc.)

### `VehicleOutOfControlSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every frame, processes 1/16th of entities per frame via UpdateFrame)
- **Queries**:
  - EntityQuery requiring: `[OutOfControl, UpdateFrame, Transform, Moving, TransformFrame]`, excluding: `[Deleted, Temp, TripSource]`
- **Reads**: PrefabRef, ObjectGeometryData, CarData (m_Braking for grip), TerrainHeightData, NetSearchTree (road geometry for ground collision)
- **Writes**: Moving (velocity/angular velocity), Transform (position/rotation), TransformFrame (interpolation)
- **Key methods**:
  - `VehicleOutOfControlMoveJob.Execute()` -- Physics simulation with 4 sub-steps per frame. Each step:
    1. Calculates moment of inertia from vehicle geometry
    2. Gets ground height at 8 corner positions (4 bottom + 4 top) from terrain AND road network geometry
    3. Applies gravity (10 m/s^2 downward)
    4. Applies friction using `CarData.m_Braking` as grip coefficient
    5. Applies angular drag (0.95^dt damping factor)
    6. Computes ground collision response (prevents falling through terrain/roads)
    7. Updates rotation from angular velocity, adjusts position to keep center of mass consistent
    8. Advances position by velocity * dt
  - `NetIterator` -- Searches road network quadtree to find road surface heights for collision detection

### `ObjectCollisionSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every frame)
- **Queries**:
  - EntityQuery requiring: `[OutOfControl, Transform, Moving, TransformFrame]`, excluding: `[Deleted, Temp]`
- **Reads**: Transform, Moving, TransformFrame, PrefabRef, ObjectGeometryData, Controller, LayoutElement (for multi-part vehicles), NetSearchTree + LaneObjects (for finding nearby objects)
- **Writes**: Creates Impact event entities when collisions are detected
- **Key methods**:
  - `FindCollisionsJob.Execute()` -- For each out-of-control vehicle, searches nearby objects via the net search tree and builds a list of potential collision targets. Tests swept OBB (oriented bounding box) intersections using TransformFrame interpolation.
  - `ApplyCollisionsJob.Execute()` -- Processes detected collisions chronologically. For each collision: calculates impact velocity from the difference in velocities at the collision point, creates an Impact event entity with appropriate velocity deltas and severity (based on relative speed), and updates the TransformFrame buffer to reflect the post-collision state.

### `AccidentVehicleSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 64 frames)
- **Queries**:
  - EntityQuery requiring: `[InvolvedInAccident, Vehicle]`, excluding: `[Deleted, Temp]`
- **Reads**: Transform, Moving, Damaged, Destroyed, OnFire, InvolvedInAccident, Controller, Bicycle, BlockedLane, Passenger, LayoutElement, CarLane, Road, Curve, EdgeGeometry, AccidentSite, Resident, PrefabRef, FireData, TargetElements, PoliceConfigurationData, NetSearchTree
- **Writes**: Creates AddAccidentSite commands, Ignite events, Impact events (for passenger injuries); removes Moving/adds Stopped when vehicle stops; removes InvolvedInAccident/OutOfControl when cleared
- **Key methods**:
  - `AccidentVehicleJob.Execute()` -- Two branches:
    1. **Moving vehicles**: Calculates dynamic velocity threshold: `thresh = 0.01 + (frame_delta^2 * 3e-9)^2`. When velocity drops below threshold: stops vehicle, finds nearest road edge within 30m, creates AddAccidentSite command, rolls for fire ignition (`damage.x * fireData.m_StartProbability`), creates injury Impacts for passengers, shows notification icon.
    2. **Stopped vehicles**: Checks if site is secured (police arrived or 14400 frames elapsed). If secured and undamaged: restarts vehicle and clears accident. If destroyed and cleared >= 1.0 or 14400 frames elapsed: deletes vehicle. Bicycles have shorter cleanup (300 frames).
  - `IsSecured()` -- Finds the AccidentSite for this event and checks if Secured flag is set or 14400 frames have elapsed since creation
  - `FindSuitableAccidentSite()` -- Searches NetSearchTree for nearest road edge within 30m that doesn't already have an AccidentSite

### `AccidentSiteSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 64 frames)
- **Queries**:
  - EntityQuery requiring: `[AccidentSite]`, excluding: `[Deleted, Temp]`
- **Reads**: Building, InvolvedInAccident, Criminal, PoliceEmergencyRequest, Moving, Vehicle, Car, Bicycle, PrefabRef, TrafficAccidentData, CrimeData, TargetElements, SubLanes, LaneObjects, CityModifiers
- **Writes**: AccidentSite (flags), creates PoliceEmergencyRequest, creates Impact events (for staged accidents)
- **Key methods**:
  - `AccidentSiteJob.Execute()` -- For each accident site:
    1. Clears StageAccident flag after 3600 frames (~60 seconds)
    2. Iterates all targets in the event to count involved entities and track MovingVehicles flag
    3. If no involved entities remain AND StageAccident flag is set: calls `TryFindSubject()` to find a passing car on the road's lanes, then `AddImpact()` to create a new collision -- **this is the chain-reaction mechanism**
    4. Manages RequirePolice flag and dispatches police if needed
    5. Removes AccidentSite component when all entities are cleared and either secured or 14400 frames elapsed (with extra 1024 frame buffer after SecuredFrame for crime scenes)
  - `TryFindSubject()` -- Iterates road sublanes and their LaneObjects looking for a moving, non-bicycle car not already in an accident. Uses reservoir sampling to pick a random target.
  - `AddImpact()` -- Creates an Impact event with severity 5.0, angular velocity delta of +/-2 rad/s around Y axis, and lateral velocity delta of 5.0 m/s perpendicular to the car's travel direction (randomly left or right)

### `AccidentCreatureSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 64 frames)
- **Queries**:
  - EntityQuery requiring: `[InvolvedInAccident, Creature]`, excluding: `[Deleted, Temp]`
- **Reads**: Transform, Moving, Resident, InvolvedInAccident, Stumbling, Target, CurrentVehicle, Road, Curve, EdgeGeometry, AccidentSite, Hearse, Ambulance, Bicycle, PrefabRef, TargetElements, PoliceConfigurationData, NetSearchTree
- **Writes**: Human (m_Flags: Collapsed), Creature (clears queue), creates AddAccidentSite commands, creates AddHealthProblem events; removes InvolvedInAccident/Stumbling when cleared
- **Key methods**:
  - `AccidentCreatureJob.Execute()` -- Three paths based on creature state:
    1. **Stumbling + in vehicle**: Applies injury (100% chance for bicycle, 50% for car), stops stumbling, finds/creates accident site
    2. **Stumbling + moving (pedestrian)**: Waits for velocity < 0.01, then rolls injury (50% chance). If RequireTransport injury: stops movement, sets Collapsed flag. Otherwise: stops stumbling, shows notification
    3. **Stopped (waiting for rescue)**: Checks if site is secured or hearse/ambulance is targeting them, then clears accident
  - `AddInjury()` -- Creates AddHealthProblem event. 20% chance of death (`HealthProblemFlags` value 2), 80% chance of injury (value 4). Severity decreases for subsequent passengers (`severity *= random(0.8, 0.9)`)

### `AddAccidentSiteSystem` (Game.Events)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every frame)
- **Queries**:
  - EntityQuery requiring: `[AddAccidentSite, Event]`
- **Reads**: AddAccidentSite, PrefabRef
- **Writes**: AccidentSite (adds or merges), CrimeProducer (reduces crime by 70% when crime scene), TargetElements (adds site to event targets)
- **Key methods**:
  - `AddAccidentSiteJob.Execute()` -- Collects all AddAccidentSite commands into a hashmap (deduplicating by target entity). For each unique target: either adds a new AccidentSite component or merges with existing one (combining flags, preserving the non-null event reference). Also adds the site entity to the event's TargetElement buffer.
  - `MergeAccidentSites()` -- When two accident sites target the same entity: keeps the one with a non-null event, ORs the flags together

### `DamagedVehicleSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 512 frames)
- **Queries**:
  - EntityQuery requiring: `[Damaged, Stopped, Car]`, excluding: `[Deleted, Temp]`
- **Reads**: Destroyed, MaintenanceConsumer, Damaged, MaintenanceRequest
- **Writes**: Creates MaintenanceRequest entities, adds/removes MaintenanceConsumer
- **Key methods**:
  - `DamagedVehicleJob.Execute()` -- For stopped, damaged cars: creates maintenance service requests (priority 100) if one doesn't already exist. Removes MaintenanceConsumer when Destroyed.m_Cleared >= 1.0 or when damage is zero.

## Data Flow

```
[TrafficAccident prefab triggers (via AccidentSiteSystem.TryFindSubject)
 OR ObjectCollisionSystem detects collision between moving objects]
    |
    v
[Impact event entity created]
    - Contains: m_Target (vehicle/creature), m_VelocityDelta, m_AngularVelocityDelta, m_Severity
    |
    v
[ImpactSystem] processes Impact entities (every frame):
    |-- Applies m_VelocityDelta + m_AngularVelocityDelta to target's Moving component
    |-- Adds OutOfControl component to vehicles (activates parked/stopped cars first)
    |-- Adds InvolvedInAccident component (severity, frame number)
    |-- Adds Stumbling to hit creatures
    |-- Detaches trailers from controllers
    |
    v
[VehicleOutOfControlSystem] simulates physics (every frame, 1/16th of entities):
    |-- 4 sub-steps per frame (dt = 4/15/4 seconds per sub-step)
    |-- Applies gravity: 10 m/s^2 downward
    |-- Applies friction: clamped by CarData.m_Braking * min(dt, groundPenetration/gravity)
    |-- Applies velocity damping: 0.95^dt per step
    |-- Ground collision: terrain height + road network geometry via NetSearchTree
    |-- Tests 8 corner points (4 bottom + 4 top of vehicle AABB)
    |-- Updates Transform (position/rotation) and TransformFrame (interpolation)
    |
    v
[ObjectCollisionSystem] detects collisions (every frame):
    |-- Swept OBB intersection tests using TransformFrame interpolation
    |-- Creates new Impact events for each detected collision -> feeds back to ImpactSystem
    |
    v
[AccidentVehicleSystem] manages post-crash (every 64 frames):
    |-- When velocity < dynamic threshold:
    |   |-- Removes Moving, adds Stopped
    |   |-- Finds nearest road edge within 30m -> AddAccidentSite command
    |   |-- Fire ignition: damage.x * fireData.m_StartProbability (if > 1%, roll random)
    |   |-- Creates injury Impacts for passengers (severity cascades down)
    |   |-- Shows accident notification icon
    |-- When secured (police arrived OR 14400 frames elapsed):
    |   |-- Undamaged vehicles: restart and clear accident
    |   |-- Destroyed vehicles: delete after m_Cleared >= 1.0 or 14400 frames
    |   |-- Bicycles: shorter cleanup (300 frames)
    |
    v
[AccidentCreatureSystem] manages hit pedestrians (every 64 frames):
    |-- Stumbling creatures: wait for velocity < 0.01
    |   |-- 50% injury chance (100% for bicycle collisions)
    |   |-- 20% of injuries are fatal, 80% non-fatal
    |   |-- Fatal: stop movement, set Collapsed flag
    |   |-- Non-fatal: stop stumbling, show notification
    |-- Creates AddAccidentSite commands for creature accident locations
    |-- Clears accident when site secured or hearse/ambulance arrives
    |
    v
[AddAccidentSiteSystem] processes commands (every frame):
    |-- Creates AccidentSite component on road edge entities
    |-- Merges multiple commands targeting same edge (ORs flags)
    |-- Adds site to event's TargetElement buffer
    |
    v
[AccidentSiteSystem] manages accident scenes (every 64 frames):
    |-- Requests police (RequirePolice flag, creates PoliceEmergencyRequest)
    |-- Chain reactions: StageAccident flag (active for first 3600 frames)
    |   |-- TryFindSubject: finds passing car on road sublanes
    |   |-- AddImpact: severity 5.0, lateral push 5 m/s, angular 2 rad/s
    |   |-- New victim feeds back into ImpactSystem -> cascading accidents
    |-- Tracks Secured state from police arrival
    |-- Cleanup: removes AccidentSite after all entities cleared
    |
    v
[DamagedVehicleSystem] (every 512 frames):
    |-- Stopped + Damaged + Car -> creates MaintenanceRequest (priority 100)
    |-- Handles destroyed vehicle cleanup tracking
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Accident site type | TrafficAccidentData.m_RandomSiteType | Game.Prefabs.TrafficAccidentData |
| Subject filter | TrafficAccidentData.m_SubjectType | Game.Prefabs.TrafficAccidentData |
| Accident behavior | TrafficAccidentData.m_AccidentType | Game.Prefabs.TrafficAccidentData (only `LoseControl`) |
| Occurrence probability | TrafficAccidentData.m_OccurenceProbability | Game.Prefabs.TrafficAccidentData |
| Fire start probability | FireData.m_StartProbability | Game.Prefabs.FireData |
| Fire start intensity | FireData.m_StartIntensity | Game.Prefabs.FireData |
| Notification prefab | PoliceConfigurationData.m_TrafficAccidentNotificationPrefab | Singleton query |
| Crime scene notification | PoliceConfigurationData.m_CrimeSceneNotificationPrefab | Singleton query |
| Vehicle grip/braking | CarData.m_Braking | Game.Prefabs.CarData |
| Vehicle geometry | ObjectGeometryData.m_Bounds, m_Size | Game.Prefabs.ObjectGeometryData |

### Key Constants (Hardcoded)

| Constant | Value | Where Used |
|----------|-------|------------|
| Gravity | 10 m/s^2 | VehicleOutOfControlSystem |
| Velocity damping | 0.95^dt | VehicleOutOfControlSystem |
| Physics sub-steps | 4 per frame | VehicleOutOfControlSystem |
| Ground tolerance | 4.0 m above corner | VehicleOutOfControlSystem (NetIterator) |
| Accident site search radius | 30 m | AccidentVehicleSystem, AccidentCreatureSystem |
| Cleanup timeout | 14400 frames (~4 min at 60fps) | AccidentVehicleSystem, AccidentCreatureSystem, AccidentSiteSystem |
| Bicycle cleanup timeout | 300 frames (~5 sec) | AccidentVehicleSystem |
| StageAccident duration | 3600 frames (~60 sec) | AccidentSiteSystem |
| Secured buffer (crime) | 1024 frames (~17 sec) | AccidentSiteSystem |
| Staged impact severity | 5.0 | AccidentSiteSystem |
| Staged impact angular | 2.0 rad/s (Y axis) | AccidentSiteSystem |
| Staged impact lateral | 5.0 m/s | AccidentSiteSystem |
| Injury probability (pedestrian) | 50% | AccidentCreatureSystem |
| Injury probability (cyclist) | 100% | AccidentCreatureSystem |
| Fatal injury chance | 20% | AccidentCreatureSystem |
| Passenger severity decay | 0.8-0.9x per passenger | AccidentVehicleSystem |

## Key Enums

### `AccidentSiteFlags` (Game.Events)

```csharp
[Flags]
public enum AccidentSiteFlags : uint
{
    StageAccident   = 1,    // Can trigger chain-reaction accidents on passing cars
    Secured         = 2,    // Police have arrived and secured the site
    CrimeScene      = 4,    // Site is also a crime scene
    TrafficAccident = 8,    // Site originated from a traffic accident
    CrimeFinished   = 16,   // Crime activity has ended
    CrimeDetected   = 32,   // Crime has been detected/reported
    CrimeMonitored  = 64,   // Crime is being monitored (surveillance)
    RequirePolice   = 128,  // Police dispatch is needed
    MovingVehicles  = 256   // At least one involved vehicle is still moving
}
```

### `TrafficAccidentType` (Game.Prefabs)

```csharp
public enum TrafficAccidentType
{
    LoseControl  // Only defined type
}
```

## Harmony Patch Points

### Candidate 1: `Game.Events.ImpactSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to block) or Postfix (to modify results)
- **What it enables**: Intercept all impacts before they're applied. A Prefix could filter which vehicles get OutOfControl (e.g., only allow it for certain vehicle types). A Postfix could add extra effects.
- **Risk level**: Medium -- this is a central system, blocking it entirely would prevent all collision effects
- **Side effects**: Blocking impacts would also prevent pedestrian stumbling and trailer detachment

### Candidate 2: `Game.Simulation.VehicleOutOfControlSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to replace physics) or Transpiler (to modify constants)
- **What it enables**: Modify out-of-control physics: gravity strength, friction coefficients, damping factor, number of sub-steps. A Transpiler could change the hardcoded `10f` gravity, `0.95f` damping, or `4` sub-steps.
- **Risk level**: Low -- only affects already-out-of-control vehicles
- **Side effects**: Physics changes could cause vehicles to clip through terrain if not careful

### Candidate 3: `Game.Simulation.AccidentVehicleSystem+AccidentVehicleJob.Execute()`

- **Signature**: `public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)` (BurstCompile)
- **Patch type**: Transpiler (BurstCompile jobs cannot use Prefix/Postfix easily)
- **What it enables**: Control fire ignition probability, injury severity for passengers, cleanup timing (14400 frame timeout), velocity threshold for stopping
- **Risk level**: High -- Burst-compiled job, Transpiler only. Complex internal logic.
- **Side effects**: Must be very careful with Burst compatibility

### Candidate 4: `Game.Simulation.AccidentSiteSystem+AccidentSiteJob.AddImpact()`

- **Signature**: `private void AddImpact(int jobIndex, Entity eventEntity, ref Random random, Entity target, TrafficAccidentData trafficAccidentData)` (inside BurstCompile job)
- **Patch type**: Transpiler
- **What it enables**: Modify chain-reaction severity (hardcoded 5.0), angular velocity (2.0), and lateral force direction
- **Risk level**: High -- Burst-compiled, private method inside a job struct
- **Side effects**: Changing chain-reaction severity affects cascading accident intensity

### Candidate 5: `Game.Simulation.ObjectCollisionSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to disable collision detection) or Postfix
- **What it enables**: Disable or modify collision detection entirely. Could filter which entities participate in collision checks.
- **Risk level**: Medium -- disabling would prevent all object-to-object collisions
- **Side effects**: Vehicles would phase through each other if disabled

### Alternative: ECS System Approach (No Harmony)

Instead of patching, a custom `GameSystemBase` could:
1. Query for Impact entities before ImpactSystem runs and modify/remove them
2. Query for `OutOfControl` entities and add/remove the component
3. Query for `AccidentSite` entities and modify flags
4. Create Impact event entities programmatically to trigger accidents

This is safer than Harmony patching and fully compatible with Burst-compiled jobs.

## Mod Blueprint

### To trigger a vehicle accident programmatically:

1. **Create an Impact event entity** with the required archetype (`Event` + `Impact`)
2. Set the `Impact` component:
   - `m_Target` = target vehicle entity
   - `m_VelocityDelta` = direction and magnitude of force (e.g., `new float3(5, 0, 0)` for 5 m/s lateral push)
   - `m_AngularVelocityDelta` = spin force (e.g., `new float3(0, 2, 0)` for Y-axis spin)
   - `m_Severity` = damage level (5.0 is the game's default for staged accidents)
   - `m_Event` = the parent accident event entity (or create a new one with `TrafficAccident` component)
3. `ImpactSystem` processes it next frame -> vehicle gets `OutOfControl` + `InvolvedInAccident`
4. Physics takes over via `VehicleOutOfControlSystem`

### Alternative (direct component manipulation):

Add `OutOfControl` + `Moving` (with velocity) + `InvolvedInAccident` components directly to a vehicle entity. This skips the event pipeline and may miss side effects (trailer detachment, activation of parked/stopped vehicles, event target tracking).

### Systems to create:
- **AccidentTriggerSystem** -- Custom system to programmatically create Impact events based on mod logic (e.g., random accidents, player-triggered events)
- **AccidentModifierSystem** -- Custom system running before ImpactSystem to filter/modify Impact events (e.g., severity scaling, vehicle type filtering)

### Components to add:
- **AccidentImmune** -- Optional marker component to protect certain vehicles from being affected by impacts
- **AccidentProne** -- Optional marker to increase accident probability for certain vehicles

### Patches needed:
- For basic trigger/response mods: **none** (ECS approach is sufficient)
- For physics modification: Transpiler on `VehicleOutOfControlSystem` to change gravity/friction constants
- For chain-reaction control: Transpiler on `AccidentSiteSystem` job to modify staged accident parameters

### Settings:
- Gravity multiplier (default: 1.0)
- Friction multiplier (default: 1.0)
- Fire probability multiplier (default: 1.0)
- Chain reaction enabled (default: true)
- Cleanup timeout multiplier (default: 1.0)
- Injury severity multiplier (default: 1.0)

### UI changes:
- None required for basic functionality
- Optional: accident statistics panel, manual accident trigger tool

## Examples

### Example 1: Trigger a Vehicle Accident via Impact Event

The safest way to make a vehicle lose control is to create an `Impact` event entity. The game's `ImpactSystem` will process it on the next frame, applying velocity changes and adding `OutOfControl` + `InvolvedInAccident` automatically.

```csharp
using Game.Events;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Custom system that creates Impact event entities to trigger vehicle accidents.
/// ImpactSystem will process these on the next simulation frame.
/// </summary>
public partial class AccidentTriggerSystem : GameSystemBase
{
    private EntityArchetype _impactArchetype;
    private EntityArchetype _accidentEventArchetype;
    private SimulationSystem _simulationSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

        // Impact entities need the Impact component plus the Event tag
        _impactArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<Impact>(),
            ComponentType.ReadWrite<Game.Common.Event>()
        );

        // The parent accident event needs TrafficAccident marker.
        // Note: The full archetype for persistent accident events may require
        // additional components (TargetElement, PrefabRef, Created, Updated)
        // depending on which downstream systems consume it. This minimal
        // archetype is sufficient for ImpactSystem processing.
        _accidentEventArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<TrafficAccident>(),
            ComponentType.ReadWrite<Game.Common.Event>()
        );
    }

    /// <summary>
    /// Creates an Impact event that will cause the target vehicle to go out of control.
    /// </summary>
    /// <param name="targetVehicle">The vehicle entity to impact.</param>
    /// <param name="lateralForce">Sideways push in m/s (5.0 is the game default for staged accidents).</param>
    /// <param name="angularSpin">Y-axis spin in rad/s (2.0 is the game default).</param>
    /// <param name="severity">Damage severity (5.0 is the game default for staged accidents).</param>
    public void TriggerAccident(Entity targetVehicle, float lateralForce, float angularSpin, float severity)
    {
        // Create the parent accident event entity
        Entity accidentEvent = EntityManager.CreateEntity(_accidentEventArchetype);

        // Create the Impact event entity
        Entity impactEntity = EntityManager.CreateEntity(_impactArchetype);
        EntityManager.SetComponentData(impactEntity, new Impact
        {
            m_Event = accidentEvent,
            m_Target = targetVehicle,
            // Lateral push -- perpendicular to travel direction
            m_VelocityDelta = new float3(lateralForce, 0f, 0f),
            // Spin around the vertical axis
            m_AngularVelocityDelta = new float3(0f, angularSpin, 0f),
            m_Severity = severity,
            m_CheckStoppedEvent = false
        });

        // ImpactSystem processes this next frame:
        //   1. Applies velocity delta to the vehicle's Moving component
        //   2. Adds OutOfControl marker component
        //   3. Adds InvolvedInAccident with the severity and current frame
        //   4. Detaches any trailer from the vehicle
    }

    protected override void OnUpdate()
    {
        // Trigger logic goes here (e.g., random selection, player input, etc.)
    }
}
```

### Example 2: Check if a Vehicle Is Out of Control or Involved in an Accident

Query for vehicles with the `OutOfControl` marker or `InvolvedInAccident` component to react to ongoing accidents.

```csharp
using Game.Events;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

/// <summary>
/// Monitors vehicles involved in accidents and logs their state.
/// Demonstrates how to query for OutOfControl and InvolvedInAccident components.
/// </summary>
public partial class AccidentMonitorSystem : GameSystemBase
{
    private EntityQuery _outOfControlQuery;
    private EntityQuery _involvedQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        // Query for vehicles currently in physics-driven out-of-control state
        _outOfControlQuery = GetEntityQuery(
            ComponentType.ReadOnly<OutOfControl>(),
            ComponentType.ReadOnly<Vehicle>(),
            ComponentType.ReadOnly<Moving>()
        );

        // Query for all entities involved in an accident (vehicles and creatures)
        _involvedQuery = GetEntityQuery(
            ComponentType.ReadOnly<InvolvedInAccident>(),
            ComponentType.ReadOnly<Vehicle>()
        );
    }

    protected override void OnUpdate()
    {
        // Count vehicles currently sliding around with physics
        int outOfControlCount = _outOfControlQuery.CalculateEntityCount();

        // Count all vehicles tagged as part of an accident (includes stopped ones)
        int involvedCount = _involvedQuery.CalculateEntityCount();

        // Example: iterate involved vehicles and check their severity
        var entities = _involvedQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity vehicle = entities[i];
            InvolvedInAccident accident = EntityManager.GetComponentData<InvolvedInAccident>(vehicle);

            // Check severity -- higher values mean more damage
            float severity = accident.m_Severity;

            // Check if vehicle is still moving or has stopped
            bool isStillSliding = EntityManager.HasComponent<OutOfControl>(vehicle);
            bool hasStopped = EntityManager.HasComponent<Game.Objects.Stopped>(vehicle);

            // Check if vehicle caught fire
            bool isOnFire = EntityManager.HasComponent<Game.Events.OnFire>(vehicle);

            // Check if vehicle is destroyed (wreck)
            bool isDestroyed = EntityManager.HasComponent<Game.Common.Destroyed>(vehicle);

            // Check the damage channels if Damaged component exists
            if (EntityManager.HasComponent<Damaged>(vehicle))
            {
                Damaged damaged = EntityManager.GetComponentData<Damaged>(vehicle);
                float fireDamage = damaged.m_Damage.x;       // Fire damage
                float structuralDamage = damaged.m_Damage.y;  // Structural damage
                float weatherDamage = damaged.m_Damage.z;      // Weather damage
            }
        }
        entities.Dispose();
    }
}
```

### Example 3: Prevent Specific Vehicles from Going Out of Control

Use a custom marker component and a system that runs before `ImpactSystem` to remove `Impact` events targeting protected vehicles.

```csharp
using Game.Events;
using Game.Simulation;
using Unity.Entities;

/// <summary>
/// Marker component to protect a vehicle from accident impacts.
/// Add this to any vehicle entity that should be immune to OutOfControl.
/// </summary>
public struct AccidentImmune : IComponentData
{
}

/// <summary>
/// Runs before ImpactSystem and removes Impact events targeting immune vehicles.
/// Uses UpdateBefore attribute to ensure it processes before impacts are applied.
/// </summary>
[UpdateBefore(typeof(ImpactSystem))]
public partial class AccidentFilterSystem : GameSystemBase
{
    private EntityQuery _impactQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _impactQuery = GetEntityQuery(
            ComponentType.ReadOnly<Impact>(),
            ComponentType.ReadOnly<Game.Common.Event>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _impactQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        var impacts = _impactQuery.ToComponentDataArray<Impact>(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity target = impacts[i].m_Target;

            // If the target vehicle has our AccidentImmune marker, destroy the Impact event
            // so ImpactSystem never processes it
            if (EntityManager.HasComponent<AccidentImmune>(target))
            {
                EntityManager.DestroyEntity(entities[i]);
            }
        }

        entities.Dispose();
        impacts.Dispose();
    }
}
```

### Example 4: Query Accident Sites and Check Their State

Read `AccidentSite` components on road edges to find active accident locations, check police status, and determine if chain reactions are possible.

```csharp
using Game.Events;
using Game.Simulation;
using Unity.Entities;

/// <summary>
/// Queries all active accident sites and inspects their flags and timing.
/// Useful for building accident statistics or UI overlays.
/// </summary>
public partial class AccidentSiteMonitorSystem : GameSystemBase
{
    private EntityQuery _siteQuery;
    private SimulationSystem _simulationSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
        _siteQuery = GetEntityQuery(ComponentType.ReadOnly<AccidentSite>());
    }

    protected override void OnUpdate()
    {
        uint currentFrame = _simulationSystem.frameIndex;

        var entities = _siteQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        var sites = _siteQuery.ToComponentDataArray<AccidentSite>(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            AccidentSite site = sites[i];

            // Check if chain-reaction accidents can still occur at this site
            // StageAccident flag is active for 3600 frames (~60 seconds) after creation
            bool canChainReact = (site.m_Flags & AccidentSiteFlags.StageAccident) != 0;

            // Check if police have arrived
            bool isSecured = (site.m_Flags & AccidentSiteFlags.Secured) != 0;

            // Check if police dispatch has been requested
            bool needsPolice = (site.m_Flags & AccidentSiteFlags.RequirePolice) != 0;

            // Check if any vehicles are still sliding around
            bool hasMovingVehicles = (site.m_Flags & AccidentSiteFlags.MovingVehicles) != 0;

            // Calculate age of the accident site in frames
            uint ageInFrames = currentFrame - site.m_CreationFrame;

            // Sites auto-cleanup after 14400 frames (~4 minutes at 60fps)
            // even without police if all involved entities are cleared
            bool isTimedOut = ageInFrames >= 14400;
        }

        entities.Dispose();
        sites.Dispose();
    }
}
```

### Example 5: Manually Add OutOfControl to a Vehicle (Direct Component Manipulation)

Bypasses the `Impact` event pipeline entirely. This is simpler but misses side effects like trailer detachment and proper event tracking. Useful for testing or when you need immediate control.

```csharp
using Game.Events;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Demonstrates directly adding OutOfControl and related components to a vehicle.
/// WARNING: This skips ImpactSystem, so trailers won't detach, parked/stopped
/// cars won't be activated, and event target tracking may be incomplete.
/// Prefer creating Impact events (Example 1) for production use.
/// </summary>
public partial class DirectOutOfControlSystem : GameSystemBase
{
    private SimulationSystem _simulationSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
    }

    /// <summary>
    /// Directly makes a moving vehicle go out of control.
    /// The vehicle must already have a Moving component (i.e., it is actively driving).
    /// </summary>
    public void MakeVehicleOutOfControl(Entity vehicle, float3 pushVelocity, float severity)
    {
        uint currentFrame = _simulationSystem.frameIndex;

        // Add the OutOfControl marker -- VehicleOutOfControlSystem will start
        // simulating physics for this vehicle
        if (!EntityManager.HasComponent<OutOfControl>(vehicle))
        {
            EntityManager.AddComponent<OutOfControl>(vehicle);
        }

        // Apply a velocity change to the existing Moving component
        if (EntityManager.HasComponent<Moving>(vehicle))
        {
            Moving moving = EntityManager.GetComponentData<Moving>(vehicle);
            moving.m_Velocity += pushVelocity;
            moving.m_AngularVelocity += new float3(0f, 2f, 0f); // Y-axis spin
            EntityManager.SetComponentData(vehicle, moving);
        }

        // Create a parent accident event (needed for InvolvedInAccident).
        // Note: This minimal archetype may need additional components
        // (TargetElement, PrefabRef, etc.) for full event pipeline processing.
        Entity accidentEvent = EntityManager.CreateEntity(
            ComponentType.ReadWrite<TrafficAccident>(),
            ComponentType.ReadWrite<Game.Common.Event>()
        );

        // Tag the vehicle as involved in the accident
        EntityManager.AddComponent<InvolvedInAccident>(vehicle);
        EntityManager.SetComponentData(vehicle, new InvolvedInAccident(
            accidentEvent,
            severity,
            currentFrame
        ));

        // AccidentVehicleSystem will handle the rest:
        //   - Stop the vehicle when velocity drops below threshold
        //   - Create an AccidentSite on the nearest road edge
        //   - Roll for fire ignition based on damage
        //   - Injure passengers
        //   - Eventually clean up or restart the vehicle
    }

    protected override void OnUpdate()
    {
        // Direct manipulation logic goes here
    }
}
```

## Open Questions

- [ ] How does the game initially create TrafficAccident event entities? The `AccidentSiteSystem.TryFindSubject()` handles staged chain-reactions, but the initial accident event creation (from the prefab's `m_OccurenceProbability`) likely happens in a separate event scheduling system not yet traced.
- [ ] What is the exact archetype needed for an accident event entity? We see `TrafficAccident` marker + `TargetElement` buffer, but there may be additional required components.
- [ ] How does `ObjectCollisionSystem` calculate the collision severity from relative velocities? The full `ApplyCollisionsJob` logic was partially truncated during decompilation.
- [ ] Are there any UI bindings that read accident state for the info panel or notification system beyond `IconCommandSystem`?
- [ ] What is the relationship between `PoliceConfigurationData` fields and the police response behavior? Only `m_TrafficAccidentNotificationPrefab` and `m_CrimeSceneNotificationPrefab` were observed.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
