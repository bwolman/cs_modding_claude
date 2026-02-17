# Research: Traffic Flow & Lane Control

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How vehicles select lanes, merge between lanes, respond to traffic signals, detect and react to congestion, and obey traffic rules (yield, stop, right-of-way) within the CS2 road network.

**Why**: To understand the traffic simulation pipeline so mods can modify lane selection costs, adjust signal timing, detect bottlenecks, alter speed limits, or create custom traffic behavior systems.

**Boundaries**: This research covers the per-vehicle navigation and lane-level traffic control systems. Pathfinding (the A* route computation that produces path elements) is covered separately in [Pathfinding](../Pathfinding/README.md). Road network construction (creating edges, nodes, lanes) is covered in [RoadNetwork](../RoadNetwork/README.md). Vehicle spawning and despawning is covered in [VehicleSpawning](../VehicleSpawning/README.md).

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Vehicles | CarCurrentLane, CarNavigation, CarNavigationLane, CarLaneFlags (vehicle state), Blocker, BlockerType |
| Game.dll | Game.Net | CarLane (network lane data), CarLaneFlags (network lane properties), TrafficLights, LaneSignal, LaneSignalType, LaneSignalFlags, TrafficLightState, TrafficLightFlags, LaneFlow, SecondaryFlow, LaneReservation, LaneOverlap, Bottleneck |
| Game.dll | Game.Objects | TrafficLight (visual state per traffic light object) |
| Game.dll | Game.Simulation | CarNavigationSystem, CarNavigationHelpers, CarLaneSelectIterator, CarLaneSelectBuffer, TrafficLightSystem, TrafficFlowSystem, TrafficBottleneckSystem, TrafficAmbienceSystem, TrafficAmbienceCell, RandomTrafficDispatchSystem |
| Game.dll | Game.Net | TrafficLightInitializationSystem, LaneOverlapSystem, FlipTrafficHandednessSystem |
| Game.dll | Game.Prefabs | CarLaneData, TrafficConfigurationData, TrafficConfigurationPrefab, TrafficLightData, TrafficSignData |

## Architecture Overview

CS2's traffic flow operates through a layered system. The **pathfinder** produces a high-level route as a sequence of `PathElement` entries. The **CarNavigationSystem** then handles per-frame vehicle movement: choosing the optimal lane within a road segment, responding to traffic signals, detecting blockers ahead, and smoothly merging between lanes. In parallel, the **TrafficLightSystem** cycles signal groups at intersections, the **TrafficFlowSystem** tracks lane utilization over time, and the **TrafficBottleneckSystem** detects gridlocked groups of vehicles.

### Two Distinct CarLaneFlags Enums

A critical detail: there are **two separate `CarLaneFlags` enums** in different namespaces:

1. **`Game.Vehicles.CarLaneFlags`** -- per-vehicle state flags (EndOfPath, IsBlocked, TurnLeft, Queue, Roundabout, etc.)
2. **`Game.Net.CarLaneFlags`** -- per-lane network properties (Highway, Yield, Stop, TrafficLights, Roundabout, ForbidPassing, etc.)

These serve completely different purposes. The vehicle flags describe what the vehicle is doing; the network flags describe lane rules.

## Component Map

### `CarCurrentLane` (Game.Vehicles)

The vehicle's current position on the lane graph. Updated every frame by CarNavigationSystem.

| Field | Type | Description |
|-------|------|-------------|
| m_Lane | Entity | Current lane entity the vehicle occupies |
| m_ChangeLane | Entity | Target lane during a lane change (Entity.Null if not changing) |
| m_CurvePosition | float3 | Position along the lane's Bezier curve (x=start, y=current, z=end) |
| m_LaneFlags | CarLaneFlags (Vehicles) | Vehicle-level state flags |
| m_ChangeProgress | float | Lane change interpolation progress (0.0 to 1.0) |
| m_Duration | float | Time spent on current lane segment |
| m_Distance | float | Distance traveled on current lane segment |
| m_LanePosition | float | Lateral position within lane |

*Source: `Game.dll` -> `Game.Vehicles.CarCurrentLane`*

### `CarLaneFlags` (Game.Vehicles)

Per-vehicle navigation state flags.

| Flag | Value | Description |
|------|-------|-------------|
| EndOfPath | 0x1 | Vehicle has reached the end of its pathfind path |
| EndReached | 0x2 | Vehicle has physically reached the lane end |
| UpdateOptimalLane | 0x4 | Triggers lane cost recalculation |
| TransformTarget | 0x8 | Vehicle is transforming to a non-lane target |
| ParkingSpace | 0x10 | Vehicle is entering a parking space |
| EnteringRoad | 0x20 | Vehicle is entering the road from a building/lot |
| Obsolete | 0x40 | Navigation data is stale |
| Reserved | 0x80 | Lane is reserved for this vehicle |
| FixedLane | 0x100 | Vehicle cannot change lanes |
| Waypoint | 0x200 | Current target is a waypoint |
| Checked | 0x400 | Lane has been validated |
| GroupTarget | 0x800 | Target is a group of parallel lanes |
| Queue | 0x1000 | Vehicle is in a queue (e.g., traffic jam) |
| IgnoreBlocker | 0x2000 | Ignore blocking vehicles ahead |
| IsBlocked | 0x4000 | Vehicle is blocked by another vehicle or signal |
| QueueReached | 0x8000 | Vehicle has reached the back of a queue |
| Validated | 0x10000 | Lane navigation has been validated |
| Interruption | 0x20000 | Navigation interrupted |
| TurnLeft | 0x40000 | Vehicle is making a lane change to the left |
| TurnRight | 0x80000 | Vehicle is making a lane change to the right |
| PushBlockers | 0x100000 | Push blocking vehicles out of the way |
| HighBeams | 0x200000 | High beams active |
| RequestSpace | 0x400000 | Vehicle requesting space in target lane |
| FixedStart | 0x800000 | Fixed starting lane position |
| Connection | 0x1000000 | Using a connection lane |
| ResetSpeed | 0x2000000 | Reset speed for next segment |
| Area | 0x4000000 | In an area lane (e.g., parking lot) |
| Roundabout | 0x8000000 | Vehicle is on a roundabout |
| CanReverse | 0x10000000 | Vehicle can reverse direction |
| ClearedForPathfind | 0x20000000 | Cleared for pathfind recalculation |

### `CarLaneFlags` (Game.Net)

Per-lane network property flags. These define the traffic rules for each lane.

| Flag | Value | Description |
|------|-------|-------------|
| Unsafe | 0x1 | Lane has unsafe merging conditions |
| UTurnLeft | 0x2 | Lane allows U-turn to the left |
| Invert | 0x4 | Lane direction is inverted relative to the road direction |
| SideConnection | 0x8 | Lane is a side connection (driveway, parking) |
| TurnLeft | 0x10 | Lane turns left at intersection |
| TurnRight | 0x20 | Lane turns right at intersection |
| LevelCrossing | 0x40 | Lane crosses a rail level crossing |
| Twoway | 0x80 | Lane is bidirectional |
| IsSecured | 0x100 | Lane has traffic signal protection |
| Yield | 0x400 | Vehicles must yield on this lane |
| Stop | 0x800 | Vehicles must stop on this lane |
| Highway | 0x10000 | Lane is on a highway |
| Forward | 0x100000 | Lane goes straight through intersection |
| Approach | 0x200000 | Lane is an approach to an intersection |
| Roundabout | 0x400000 | Lane is part of a roundabout |
| RightLimit | 0x800000 | Lane is the rightmost limit |
| LeftLimit | 0x1000000 | Lane is the leftmost limit |
| ForbidPassing | 0x2000000 | Passing is forbidden on this lane |
| RightOfWay | 0x4000000 | Lane has right of way |
| TrafficLights | 0x8000000 | Lane is controlled by traffic lights |
| Forbidden | 0x40000000 | Lane is forbidden for general traffic |
| AllowEnter | 0x80000000 | Lane allows entering from adjacent |

### `CarLane` (Game.Net)

The main lane component on road sub-lane entities. Holds speed limits, blockage info, and traffic rule flags.

| Field | Type | Description |
|-------|------|-------------|
| m_AccessRestriction | Entity | Entity defining access restrictions for this lane |
| m_Flags | CarLaneFlags (Net) | Lane property flags (traffic rules) |
| m_DefaultSpeedLimit | float | Original speed limit from road prefab |
| m_SpeedLimit | float | Current speed limit (can be modified) |
| m_Curviness | float | Curvature of the lane (higher = tighter curves) |
| m_CarriagewayGroup | ushort | Groups lanes into carriageways |
| m_BlockageStart | byte | Start position of blockage on lane (0-255 mapped to 0.0-1.0) |
| m_BlockageEnd | byte | End position of blockage on lane |
| m_CautionStart | byte | Start position of caution zone |
| m_CautionEnd | byte | End position of caution zone |
| m_FlowOffset | byte | Traffic flow offset (0=free flow, 255=congested) |
| m_LaneCrossCount | byte | Number of lanes crossing this one |

Properties:
- `blockageBounds` -> `Bounds1` mapped from byte range to 0.0-1.0
- `cautionBounds` -> `Bounds1` mapped from byte range to 0.0-1.0

*Source: `Game.dll` -> `Game.Net.CarLane`*

### `CarNavigation` (Game.Vehicles)

Vehicle-level navigation state.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetPosition | float3 | Current world-space target position |
| m_TargetRotation | quaternion | Current target orientation |
| m_MaxSpeed | float | Maximum speed for current lane |

*Source: `Game.dll` -> `Game.Vehicles.CarNavigation`*

### `CarNavigationLane` (Game.Vehicles)

Buffer element representing upcoming lanes in the vehicle's navigation queue.

| Field | Type | Description |
|-------|------|-------------|
| m_Lane | Entity | Target lane entity |
| m_CurvePosition | float2 | Start and end position on the lane's curve |
| m_Flags | CarLaneFlags (Vehicles) | Navigation flags for this segment |

Internal buffer capacity: 8 elements.

*Source: `Game.dll` -> `Game.Vehicles.CarNavigationLane`*

### `TrafficLights` (Game.Net)

Attached to intersection nodes that have traffic signals. Controls the signal cycling state machine.

| Field | Type | Description |
|-------|------|-------------|
| m_State | TrafficLightState | Current state of the signal cycle |
| m_Flags | TrafficLightFlags | LevelCrossing, MoveableBridge, IsSubNode |
| m_SignalGroupCount | byte | Total number of signal groups at this intersection |
| m_CurrentSignalGroup | byte | Which group currently has green |
| m_NextSignalGroup | byte | Which group will get green next |
| m_Timer | byte | Frame counter within current state |

*Source: `Game.dll` -> `Game.Net.TrafficLights`*

### `TrafficLightState` (Game.Net)

| State | Value | Description |
|-------|-------|-------------|
| None | 0 | Traffic lights not yet initialized |
| Beginning | 1 | Transitioning to new green phase |
| Ongoing | 2 | Green phase active (minimum 2 ticks) |
| Ending | 3 | Transitioning to red (minimum 2 ticks) |
| Changing | 4 | All-red clearance phase between groups |
| Extending | 5 | Green phase extended due to waiting vehicles |
| Extended | 6 | Extension in progress, checking for further extension |

### `LaneSignal` (Game.Net)

Per-lane signal state. Attached to each lane at a signalized intersection.

| Field | Type | Description |
|-------|------|-------------|
| m_Petitioner | Entity | Vehicle requesting signal change |
| m_Blocker | Entity | Vehicle blocking this lane's signal |
| m_GroupMask | ushort | Bitmask of signal groups this lane belongs to |
| m_Priority | sbyte | Priority level for signal phasing |
| m_Default | sbyte | Default priority |
| m_Signal | LaneSignalType | Current signal state (None/Stop/SafeStop/Yield/Go) |
| m_Flags | LaneSignalFlags | CanExtend, Physical |

*Source: `Game.dll` -> `Game.Net.LaneSignal`*

### `LaneSignalType` (Game.Net)

| Type | Value | Description |
|------|-------|-------------|
| None | 0 | No signal active |
| Stop | 1 | Must stop (red light) |
| SafeStop | 2 | Stop with safe clearance |
| Yield | 3 | Yield to cross traffic |
| Go | 4 | Proceed (green light) |

### `LaneFlow` (Game.Net)

Tracks traffic flow measurements per lane over time-of-day quadrants.

| Field | Type | Description |
|-------|------|-------------|
| m_Duration | float4 | Average travel duration per time-of-day quadrant |
| m_Distance | float4 | Average travel distance per time-of-day quadrant |
| m_Next | float2 | Accumulator for next measurement (duration, distance) |

The float4 components represent four time-of-day quadrants (night, morning, afternoon, evening), allowing the pathfinder to use time-of-day-dependent flow data.

*Source: `Game.dll` -> `Game.Net.LaneFlow`*

### `Blocker` (Game.Vehicles)

Tracks what is blocking a vehicle.

| Field | Type | Description |
|-------|------|-------------|
| m_Blocker | Entity | Entity blocking this vehicle |
| m_Type | BlockerType | Type of blocking: None, Continuing (mutual block) |
| m_MaxSpeed | byte | Maximum speed allowed due to blocker (encoded as byte, divide by 5) |

*Source: `Game.dll` -> `Game.Vehicles.Blocker`*

### `Bottleneck` (Game.Net)

Added to lane entities experiencing traffic bottlenecks. Managed by TrafficBottleneckSystem.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | byte | Curve position of the bottleneck (0-255 -> 0.0-1.0) |
| m_MinPos | byte | Minimum extent of bottleneck |
| m_MaxPos | byte | Maximum extent of bottleneck |
| m_Timer | byte | Severity timer (>=15 triggers notification, >=20 is confirmed bottleneck) |

*Source: `Game.dll` -> `Game.Net.Bottleneck`*

### `LaneReservation` (Game.Net)

Lane reservation data for vehicles that need exclusive access to a lane (e.g., at intersections).

| Field | Type | Description |
|-------|------|-------------|
| m_Blocker | Entity | Entity that placed this reservation |
| m_Next | ReservationData | Next reservation (offset 0-255, priority byte) |
| m_Prev | ReservationData | Previous reservation |

Methods:
- `GetOffset()` -> max of next/prev offset as float (0.0-1.0)
- `GetPriority()` -> max priority of next/prev

*Source: `Game.dll` -> `Game.Net.LaneReservation`*

### `LaneOverlap` (Game.Net)

Buffer element describing where two lanes physically overlap (cross over each other at an intersection).

| Field | Type | Description |
|-------|------|-------------|
| m_Other | Entity | The other overlapping lane |
| m_Flags | OverlapFlags | Overlap properties |
| m_ThisStart | byte | Start of overlap on this lane (0-255) |
| m_ThisEnd | byte | End of overlap on this lane (0-255) |
| m_OtherStart | byte | Start of overlap on other lane |
| m_OtherEnd | byte | End of overlap on other lane |
| m_Parallelism | byte | How parallel the lanes are (0-255, 128 = perpendicular) |
| m_PriorityDelta | sbyte | Priority difference between lanes |

*Source: `Game.dll` -> `Game.Net.LaneOverlap`*

### `TrafficAmbienceCell` (Game.Simulation)

Per-cell traffic intensity on a 64x64 grid. Used for ambient sound and effects.

| Field | Type | Description |
|-------|------|-------------|
| m_Accumulator | float | Accumulated traffic this update period |
| m_Traffic | float | Smoothed traffic value from last update |

*Source: `Game.dll` -> `Game.Simulation.TrafficAmbienceCell`*

### `TrafficConfigurationData` (Game.Prefabs)

Global singleton with notification entity references for traffic events.

| Field | Type | Description |
|-------|------|-------------|
| m_BottleneckNotification | Entity | Notification prefab for bottleneck icons |
| m_DeadEndNotification | Entity | Notification for dead-end roads |
| m_RoadConnectionNotification | Entity | Notification for road connection issues |
| m_TrackConnectionNotification | Entity | Notification for track connection issues |
| m_CarConnectionNotification | Entity | Notification for car connection issues |
| m_ShipConnectionNotification | Entity | Ship connection notification |
| m_TrainConnectionNotification | Entity | Train connection notification |
| m_PedestrianConnectionNotification | Entity | Pedestrian connection notification |
| m_BicycleConnectionNotification | Entity | Bicycle connection notification |

*Source: `Game.dll` -> `Game.Prefabs.TrafficConfigurationData`*

## System Map

### `CarNavigationSystem` (Game.Simulation)

The central vehicle navigation system. Runs every frame for all active vehicles.

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every frame)
- **Queries**: Vehicles with CarCurrentLane + CarNavigation + CarNavigationLane + PathOwner
- **Sub-systems**:
  - `UpdateNavigationJob` (IJobChunk) -- main per-vehicle navigation logic
  - `Actions` (GameSystemBase) -- applies deferred lane effects
  - `UpdateLaneSignalsJob` -- processes signal request queue
  - `UpdateLaneReservationsJob` -- processes lane reservation queue
  - `ApplyTrafficAmbienceJob` -- accumulates traffic ambience data
- **Key behavior**:
  1. For each vehicle, reads `CarCurrentLane`, `CarNavigationLane` buffer, and `PathElement` buffer
  2. Uses `CarLaneSelectIterator` to calculate costs for all parallel lanes
  3. Calls `UpdateOptimalLane` to pick the cheapest lane and initiate lane changes
  4. Checks `LaneSignal` on upcoming lanes for stop/yield/go signals
  5. Detects blockers ahead and updates `Blocker` component
  6. Updates `CarNavigation.m_MaxSpeed` based on lane speed limits and blockers
  7. Queues lane effects (flow, pollution, condition deterioration)
  8. Queues traffic ambience contributions

### `CarLaneSelectIterator` (Game.Simulation)

The lane selection cost calculator. Not a system -- a struct used within CarNavigationSystem.

- **Key cost factors**:
  - **Lane switch cost**: `numLanes^3 * baseCost` -- cubic scaling discourages multi-lane changes
  - **Lane object cost**: vehicles ahead on the lane add cost (stationary objects cost ~9-10 million)
  - **Lane priority cost**: `max(0, lanePriority - vehiclePriority) * 1.0`
  - **Forbidden lane cost**: 0.9 for normal vehicles, 4.9 for low-priority (priority < 108)
  - **Preferred lane bonus**: 0.4 penalty for not matching preferred flags
  - **Bicycle cost**: bicycles prefer rightmost lanes, 1.4 + 0.4 * laneOffset
- **Lane change decision**:
  - Compares cost of staying vs. switching to each parallel lane
  - Considers whether next navigation lane connects to the current parallel lane
  - Accounts for `SlaveLane.AllowChange` flag (5x extra cost if lane changes not allowed)

### `TrafficLightSystem` (Game.Simulation)

Manages traffic signal cycling at intersections.

- **Base class**: GameSystemBase
- **Update interval**: Every frame (runs on nodes with TrafficLights component)
- **Queries**: Nodes with TrafficLights + SubLane, excluding Deleted/Temp
- **Key behavior**:
  1. Reads all `LaneSignal` components on intersection sub-lanes
  2. Advances the `TrafficLightState` state machine (None -> Beginning -> Ongoing -> Ending -> Changing -> ...)
  3. Minimum green time: 2 ticks for Ongoing state
  4. Maximum green time: 6 ticks default (configurable for moveable bridges)
  5. Uses `GetNextSignalGroup` to find the group with highest demand (most waiting petitioners)
  6. Supports signal extension: if vehicles are still arriving, extends green via Extending/Extended states
  7. Updates `LaneSignal.m_Signal` to Stop/Go for each lane based on current group
  8. Updates `TrafficLight` objects (visual state for rendering)

### `TrafficFlowSystem` (Game.Simulation)

Tracks traffic flow statistics per lane and aggregates them to road segments.

- **Base class**: GameSystemBase
- **Update interval**: 512 frames (32 updates per day)
- **Two jobs**:
  1. `UpdateLaneFlowJob` -- smooths `LaneFlow.m_Duration` and `m_Distance` using time-of-day weighted lerp
  2. `UpdateRoadFlowJob` -- aggregates lane flows into `Road.m_TrafficFlowDuration0/1` and `m_TrafficFlowDistance0/1`, then computes `m_FlowOffset` on each `CarLane`
- **Flow offset**: `byte` value (0-255) written to `CarLane.m_FlowOffset`. 0 = free flow, 255 = fully congested. Used by pathfinder to weight lane costs.

### `TrafficBottleneckSystem` (Game.Simulation)

Detects traffic gridlocks by analyzing mutual blocking relationships.

- **Base class**: GameSystemBase
- **Update interval**: 64 frames
- **Algorithm**:
  1. Scans all vehicles with `Blocker` component (type = Continuing, blocker entity set)
  2. Groups mutually-blocking vehicles using union-find algorithm
  3. Groups with >= 10 vehicles are considered potential bottlenecks
  4. Groups with >= 50 vehicles trigger adding `Bottleneck` component to affected lanes
  5. Bottleneck timer system: reaches 15 -> notification icon appears, reaches 20 -> confirmed bottleneck
  6. Timer decays by 1-3 per tick when no longer blocked; lane `Bottleneck` component removed when timer reaches 0
  7. Reports total bottleneck count via TriggerSystem

### `TrafficAmbienceSystem` (Game.Simulation)

Maintains a 64x64 grid of traffic intensity for ambient sound.

- **Base class**: CellMapSystem<TrafficAmbienceCell>
- **Update interval**: 262144 / 1024 = 256 frames
- **Key constants**:
  - `kTextureSize = 64` -- grid resolution
  - `kUpdatesPerDay = 1024`
- **Static methods**:
  - `GetTrafficAmbience(float3 position, NativeArray<TrafficAmbienceCell>)` -- bilinear sample
  - `GetTrafficAmbience2(position, map, maxPerCell)` -- 5x5 weighted sample with distance falloff

### `TrafficLightInitializationSystem` (Game.Net)

Initializes traffic light signal groups when intersections are created or modified.

- **Key behavior**:
  - Groups lanes into signal groups based on direction and conflict analysis
  - Assigns `m_GroupMask` to each `LaneSignal`
  - Sets `m_SignalGroupCount` on `TrafficLights`
  - Handles level crossings and moveable bridges specially
  - Groups vehicle lanes separately from pedestrian lanes

### `RandomTrafficDispatchSystem` (Game.Simulation)

Dispatches random background traffic vehicles from traffic spawner buildings (outside connections).

- **Update interval**: Based on UpdateFrame system
- **Creates** `RandomTrafficRequest` entities that pathfind to targets
- **Manages** traffic spawner buildings, ensuring they always have active requests

### `TrafficFlowSystem` -- flow offset calculation

The `UpdateRoadFlowJob` computes a flow speed from duration/distance data, then writes it as a `m_FlowOffset` byte on each `CarLane`. The pathfinder reads this offset to route vehicles away from congested lanes.

## Data Flow

### Vehicle Navigation Pipeline (Every Frame)

```
PATH ELEMENTS (from pathfinder)
    PathElement[] buffer on vehicle entity
    Contains high-level route: lane entity + curve position
          |
          v
NAVIGATION LANE BUFFER
    CarNavigationLane[] -- upcoming lane queue (capacity 8)
    Filled from PathElement as vehicle progresses
    Each entry: lane entity + curve position + flags
          |
          v
LANE COST CALCULATION (CarLaneSelectIterator)
    For each upcoming lane group (parallel lanes):
      For each parallel lane:
        cost = laneObjectCost (vehicles ahead)
             + laneSwitchCost (numLanes^3 * base)
             + lanePriorityCost (reservation priority)
             + laneDriveCost (forbidden/preferred flags)
      Select minimum-cost lane
          |
          v
OPTIMAL LANE UPDATE
    CarLaneSelectIterator.UpdateOptimalLane()
      If optimal != current:
        Set m_ChangeLane = optimal lane
        Set m_ChangeProgress = 0.0
        Set TurnLeft/TurnRight flag
      If change complete:
        m_Lane = m_ChangeLane, m_ChangeLane = null
          |
          v
SIGNAL CHECK
    Read LaneSignal.m_Signal on upcoming lanes
      Go -> proceed normally
      Yield -> slow down, check cross traffic
      Stop/SafeStop -> halt at signal line
      None -> no signal control
          |
          v
BLOCKER DETECTION
    Check vehicles ahead on lane
      If blocked: set Blocker.m_Blocker, Blocker.m_Type
      Adjust m_MaxSpeed based on blocker distance
          |
          v
LANE EFFECTS (deferred)
    Queue LaneEffects: flow data, pollution, condition
    Queue LaneReservation updates
    Queue LaneSignal petitioner updates
    Queue TrafficAmbience contributions
```

### Traffic Signal Cycle (TrafficLightSystem)

```
INITIALIZATION (TrafficLightInitializationSystem)
    When intersection created/modified:
      Group lanes by direction -> signal groups
      Assign m_GroupMask to each LaneSignal
      Set m_SignalGroupCount on TrafficLights
          |
          v
STATE MACHINE (each frame, TrafficLightSystem)
    None -(1 tick)-> Beginning
      Beginning -(1 tick)-> Ongoing (current = next group)
        Ongoing -(min 2 ticks, max 6)-> check demand
          If next group waiting:
            If can extend -> Extending -(2 ticks)-> Extended
            Else -> Ending
          Extended -(max 4 ticks)-> Ending
        Ending -(2 ticks, wait for clear)-> Changing
          Changing -(1 tick)-> Beginning (with new group)
          |
          v
LANE SIGNAL UPDATE
    For each LaneSignal on intersection:
      If groupMask matches currentSignalGroup -> Go
      If in ending/changing phase -> Stop or SafeStop
      Otherwise -> Stop
          |
          v
VEHICLE RESPONSE (CarNavigationSystem)
    Vehicles read LaneSignal.m_Signal
    Set as petitioner if waiting at red
    Proceed when signal = Go
```

### Traffic Flow & Congestion Detection

```
FLOW ACCUMULATION (CarNavigationSystem.Actions)
    Each vehicle queues LaneEffects:
      duration (time on lane) and distance (distance traveled)
    Written to LaneFlow.m_Next (float2 accumulator)
          |
          v
FLOW SMOOTHING (TrafficFlowSystem, every 512 frames)
    UpdateLaneFlowJob:
      m_Duration = lerp(m_Duration, m_Next.x, timeWeight)
      m_Distance = lerp(m_Distance, m_Next.y, timeWeight)
      timeWeight varies by time-of-day quadrant
    UpdateRoadFlowJob:
      Aggregates lane flows -> Road traffic flow data
      Computes flowSpeed = distance / duration
      Writes CarLane.m_FlowOffset (0=free, 255=congested)
          |
          v
PATHFINDER COST
    Pathfinder reads m_FlowOffset
    Congested lanes get higher cost -> vehicles avoid them
          |
          v
BOTTLENECK DETECTION (TrafficBottleneckSystem, every 64 frames)
    1. Scan vehicles with Blocker (type=Continuing)
    2. Union-find: group mutually blocked vehicles
    3. Groups >= 10 -> potential bottleneck
    4. Groups >= 50 -> add Bottleneck to lanes
    5. Timer: 15+ -> notification icon, 20+ -> confirmed
    6. Decays when blockage clears
```

## Prefab & Configuration

| Value | Source | Default | Location |
|-------|--------|---------|----------|
| Road speed limit | CarLane.m_DefaultSpeedLimit | Varies by road type | Set from road prefab during lane creation |
| Modified speed limit | CarLane.m_SpeedLimit | Same as default | Can be changed by policies/districts |
| Lane switch base cost | CarLaneSelectIterator internals | 1.0 (or 5.0 if AllowChange not set) | Hardcoded in CarLaneSelectIterator |
| Signal minimum green | TrafficLightSystem | 2 ticks | Hardcoded |
| Signal maximum green | TrafficLightSystem | 6 ticks | Hardcoded (configurable for bridges) |
| Bottleneck threshold | TrafficBottleneckSystem | 10 vehicles (potential), 50 (confirmed) | Hardcoded |
| Bottleneck notification timer | TrafficBottleneckSystem | 15 ticks (icon), 20 ticks (confirmed) | Hardcoded |
| Flow update interval | TrafficFlowSystem | 512 frames (32/day) | Hardcoded constant |
| Bottleneck check interval | TrafficBottleneckSystem | 64 frames | GetUpdateInterval override |
| Traffic ambience grid | TrafficAmbienceSystem.kTextureSize | 64x64 | Hardcoded |

## Harmony Patch Points

### Candidate 1: `TrafficLightSystem.UpdateTrafficLightsJob.UpdateTrafficLightState`

- **Signature**: `bool UpdateTrafficLightState(NativeList<Entity> laneSignals, MoveableBridgeData, ref TrafficLights trafficLights)`
- **Patch type**: Prefix (to override signal timing) or Postfix (to modify computed state)
- **What it enables**: Custom signal timing algorithms, longer/shorter green phases, priority signals for emergency vehicles, adaptive signal control
- **Risk level**: Medium -- modifying signal state affects intersection safety
- **Side effects**: Must maintain valid state transitions or vehicles may get stuck

### Candidate 2: `CarNavigationSystem.UpdateNavigationJob` (at the system level)

- **Signature**: `void OnUpdate()` on CarNavigationSystem
- **Patch type**: Prefix or Postfix
- **What it enables**: Override vehicle navigation decisions, force lane changes, modify speed behavior
- **Risk level**: High -- this is the core navigation loop, called every frame for every vehicle
- **Side effects**: Performance-critical; Burst-compiled job cannot be patched directly, but the system's OnUpdate can be

### Candidate 3: `TrafficFlowSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Read or modify traffic flow data after computation. Implement custom congestion detection, modify flow offsets, create traffic monitoring overlays.
- **Risk level**: Low -- runs infrequently (512 frame interval), modifies only flow statistics
- **Side effects**: Modifying m_FlowOffset changes pathfinder lane costs

### Candidate 4: `TrafficBottleneckSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Read bottleneck detection results, implement custom bottleneck responses, modify notification thresholds
- **Risk level**: Low -- runs every 64 frames, manages notification icons
- **Side effects**: Modifying Bottleneck timer affects notification display

### Candidate 5: Direct ECS manipulation (no patch needed)

- **Signature**: N/A
- **Patch type**: N/A
- **What it enables**: Read CarCurrentLane to monitor vehicle positions. Read CarLane to check speed limits and congestion. Modify CarLane.m_SpeedLimit to change speed limits. Read/write LaneSignal to override signals. Read Bottleneck to find congestion. Read LaneFlow for traffic statistics.
- **Risk level**: Low for reads; Medium for writes (must respect signal state machine)
- **Side effects**: None for read-only; writing to LaneSignal could cause conflicting green signals if not careful

## Mod Blueprint

### Systems to create

1. **TrafficMonitorSystem** (`GameSystemBase`) -- reads CarLane, LaneFlow, and Bottleneck data to expose traffic statistics to UI
2. **CustomSignalTimingSystem** (`GameSystemBase`) -- overrides traffic light timing parameters based on intersection properties or user settings
3. **SpeedLimitModifierSystem** (`GameSystemBase`) -- modifies CarLane.m_SpeedLimit based on custom rules (time of day, road type, district policies)

### Components to add

- `CustomTrafficData` (IComponentData) -- stores mod-specific traffic metadata on lane or road entities
- `SignalOverride` (IComponentData) -- stores custom signal timing parameters per intersection

### Patches needed

- **Postfix on `TrafficFlowSystem.OnUpdate`** to read flow data and expose to UI
- **Or** no patches if using pure ECS queries to read traffic components

### Settings

- Global speed limit multiplier
- Per-road-type speed overrides
- Signal timing: minimum green, maximum green, extension time
- Bottleneck notification sensitivity
- Traffic flow overlay toggle

## Examples

### Example 1: Read a Vehicle's Current Lane and Speed

Check where a vehicle is, what lane it occupies, and whether it is changing lanes.

```csharp
/// <summary>
/// Reads lane navigation state for a vehicle entity.
/// </summary>
public void CheckVehicleLaneStatus(EntityManager em, Entity vehicle)
{
    if (!em.HasComponent<CarCurrentLane>(vehicle)) return;

    CarCurrentLane currentLane = em.GetComponentData<CarCurrentLane>(vehicle);
    CarNavigation navigation = em.GetComponentData<CarNavigation>(vehicle);

    Log.Info($"Vehicle {vehicle}:");
    Log.Info($"  Lane: {currentLane.m_Lane}");
    Log.Info($"  Curve position: {currentLane.m_CurvePosition}");
    Log.Info($"  Max speed: {navigation.m_MaxSpeed:F1} m/s");

    bool isChangingLanes = currentLane.m_ChangeLane != Entity.Null;
    Log.Info($"  Changing lanes: {isChangingLanes}");
    if (isChangingLanes)
    {
        Log.Info($"  Target lane: {currentLane.m_ChangeLane}");
        Log.Info($"  Change progress: {currentLane.m_ChangeProgress:P0}");
        bool turningLeft = (currentLane.m_LaneFlags
            & Game.Vehicles.CarLaneFlags.TurnLeft) != 0;
        Log.Info($"  Direction: {(turningLeft ? "left" : "right")}");
    }

    bool isBlocked = (currentLane.m_LaneFlags
        & Game.Vehicles.CarLaneFlags.IsBlocked) != 0;
    bool isQueued = (currentLane.m_LaneFlags
        & Game.Vehicles.CarLaneFlags.Queue) != 0;
    Log.Info($"  Blocked: {isBlocked}, Queued: {isQueued}");
}
```

### Example 2: Read Traffic Signal State at an Intersection

Query the traffic light state and current signal group for a node entity.

```csharp
/// <summary>
/// Reads the traffic light state at an intersection node.
/// </summary>
public void CheckIntersectionSignals(EntityManager em, Entity node)
{
    if (!em.HasComponent<TrafficLights>(node)) return;

    TrafficLights tl = em.GetComponentData<TrafficLights>(node);
    Log.Info($"Intersection {node}:");
    Log.Info($"  State: {tl.m_State}");
    Log.Info($"  Signal groups: {tl.m_SignalGroupCount}");
    Log.Info($"  Current group: {tl.m_CurrentSignalGroup}");
    Log.Info($"  Next group: {tl.m_NextSignalGroup}");
    Log.Info($"  Timer: {tl.m_Timer}");
    Log.Info($"  Level crossing: {(tl.m_Flags & TrafficLightFlags.LevelCrossing) != 0}");

    // Check individual lane signals
    if (em.HasBuffer<SubLane>(node))
    {
        DynamicBuffer<SubLane> subLanes = em.GetBuffer<SubLane>(node);
        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity lane = subLanes[i].m_SubLane;
            if (em.HasComponent<LaneSignal>(lane))
            {
                LaneSignal signal = em.GetComponentData<LaneSignal>(lane);
                Log.Info($"  Lane {lane}: signal={signal.m_Signal}, "
                    + $"group=0x{signal.m_GroupMask:X4}");
            }
        }
    }
}
```

### Example 3: Modify Speed Limits on a Road Segment

Change the effective speed limit on all car lanes of a road.

```csharp
/// <summary>
/// Modifies speed limits on all car lanes of a road segment.
/// </summary>
public partial class SpeedLimitModifierSystem : GameSystemBase
{
    private EntityQuery _roadQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _roadQuery = GetEntityQuery(
            ComponentType.ReadOnly<Road>(),
            ComponentType.ReadOnly<SubLane>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    /// <summary>
    /// Sets speed limit on a specific road entity.
    /// </summary>
    public void SetRoadSpeedLimit(EntityManager em, Entity road, float newSpeedLimit)
    {
        if (!em.HasBuffer<SubLane>(road)) return;

        DynamicBuffer<SubLane> subLanes = em.GetBuffer<SubLane>(road);
        int modified = 0;

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity lane = subLanes[i].m_SubLane;
            if (em.HasComponent<CarLane>(lane))
            {
                CarLane carLane = em.GetComponentData<CarLane>(lane);
                carLane.m_SpeedLimit = newSpeedLimit;
                em.SetComponentData(lane, carLane);
                modified++;
            }
        }

        Log.Info($"Modified speed limit on {modified} lanes to "
            + $"{newSpeedLimit:F1} m/s ({newSpeedLimit * 3.6f:F0} km/h)");
    }

    protected override void OnUpdate() { }
}
```

### Example 4: Find All Traffic Bottlenecks

Query lanes with Bottleneck components to find congested areas.

```csharp
/// <summary>
/// Finds all current traffic bottlenecks and logs their locations.
/// </summary>
public partial class BottleneckMonitorSystem : GameSystemBase
{
    private EntityQuery _bottleneckQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _bottleneckQuery = GetEntityQuery(
            ComponentType.ReadOnly<Bottleneck>(),
            ComponentType.ReadOnly<Curve>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<Entity> entities =
            _bottleneckQuery.ToEntityArray(Allocator.Temp);
        NativeArray<Bottleneck> bottlenecks =
            _bottleneckQuery.ToComponentDataArray<Bottleneck>(Allocator.Temp);

        int confirmed = 0;
        for (int i = 0; i < bottlenecks.Length; i++)
        {
            Bottleneck bn = bottlenecks[i];
            if (bn.m_Timer >= 20)
            {
                confirmed++;
                Curve curve = EntityManager.GetComponentData<Curve>(entities[i]);
                float t = (float)bn.m_Position * 0.003921569f;
                float3 worldPos = MathUtils.Position(curve.m_Bezier, t);
                Log.Info($"Bottleneck at ({worldPos.x:F0}, {worldPos.z:F0}), "
                    + $"severity timer: {bn.m_Timer}");
            }
        }

        if (confirmed > 0)
            Log.Info($"Total confirmed bottlenecks: {confirmed}");

        entities.Dispose();
        bottlenecks.Dispose();
    }
}
```

### Example 5: Read Lane Flow and Congestion Data

Sample traffic flow data on a specific lane to determine congestion level.

```csharp
/// <summary>
/// Reads traffic flow statistics for a lane entity.
/// </summary>
public void CheckLaneFlow(EntityManager em, Entity lane)
{
    if (!em.HasComponent<CarLane>(lane))
    {
        Log.Info("Entity is not a car lane");
        return;
    }

    CarLane carLane = em.GetComponentData<CarLane>(lane);
    float congestion = (float)carLane.m_FlowOffset / 255f;
    Log.Info($"Lane {lane}:");
    Log.Info($"  Speed limit: {carLane.m_SpeedLimit:F1} m/s "
        + $"({carLane.m_SpeedLimit * 3.6f:F0} km/h)");
    Log.Info($"  Default speed: {carLane.m_DefaultSpeedLimit:F1} m/s");
    Log.Info($"  Curviness: {carLane.m_Curviness:F2}");
    Log.Info($"  Flow offset: {carLane.m_FlowOffset} "
        + $"({congestion:P0} congested)");

    // Check flags
    bool isHighway = (carLane.m_Flags & CarLaneFlags.Highway) != 0;
    bool hasSignal = (carLane.m_Flags & CarLaneFlags.TrafficLights) != 0;
    bool isRoundabout = (carLane.m_Flags & CarLaneFlags.Roundabout) != 0;
    bool mustYield = (carLane.m_Flags & CarLaneFlags.Yield) != 0;
    bool mustStop = (carLane.m_Flags & CarLaneFlags.Stop) != 0;
    Log.Info($"  Highway: {isHighway}, Signal: {hasSignal}, "
        + $"Roundabout: {isRoundabout}");
    Log.Info($"  Yield: {mustYield}, Stop: {mustStop}");

    // Read flow data if available
    if (em.HasComponent<LaneFlow>(lane))
    {
        LaneFlow flow = em.GetComponentData<LaneFlow>(lane);
        Log.Info($"  Flow duration (4 quadrants): {flow.m_Duration}");
        Log.Info($"  Flow distance (4 quadrants): {flow.m_Distance}");
        Log.Info($"  Next accumulator: {flow.m_Next}");
    }
}
```

## Open Questions

- [ ] How does the pathfinder use `m_FlowOffset` exactly? The flow offset is written by TrafficFlowSystem and read by the pathfinder as a cost multiplier, but the exact weighting formula is within the pathfind cost calculation code (not fully traced here).
- [ ] What determines the lane switch `AllowChange` flag on `SlaveLane`? This appears to come from lane generation during road creation, likely based on whether the lane has solid or dashed markings.
- [ ] How do emergency vehicles interact with traffic signals? Emergency vehicles may use a higher priority value that overrides signal stops, but the exact mechanism within CarNavigationSystem needs further tracing.
- [ ] What is the full `BlockerType` enum? We see `None` and `Continuing` referenced, but the complete enum was not decompiled in this pass.
- [ ] How does the signal extension mechanism decide when to extend? The `GetNextSignalGroup` method checks for petitioners on each group, but the exact priority scoring between groups with different numbers of waiting vehicles needs more analysis.

## Sources

- Decompiled from: Game.dll (Game.Simulation namespace, Game.Vehicles namespace, Game.Net namespace, Game.Prefabs namespace, Game.Objects namespace)
- Key types: CarNavigationSystem, CarLaneSelectIterator, CarCurrentLane, CarNavigation, CarNavigationLane, CarLane, CarLaneFlags (both namespaces), TrafficLightSystem, TrafficLights, TrafficLightState, LaneSignal, LaneSignalType, TrafficFlowSystem, LaneFlow, TrafficBottleneckSystem, Bottleneck, Blocker, LaneReservation, LaneOverlap, TrafficAmbienceSystem, TrafficAmbienceCell, TrafficConfigurationData, TrafficLightInitializationSystem, RandomTrafficDispatchSystem
- All decompiled snippets saved in `snippets/` directory
