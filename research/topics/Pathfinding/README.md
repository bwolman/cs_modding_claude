# Research: Pathfinding System

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2's pathfinding works -- from pathfind request submission through graph traversal to path element output. Covers `PathfindSetupSystem`, `PathfindQueueSystem`, `PathfindResultSystem`, `PathElement`, `PathOwner`, `PathInformation`, and the graph data structures.

**Why**: Pathfinding drives all agent movement (citizens, vehicles, service vehicles, transit). Critical for any mod controlling agent movement, querying paths, forcing re-pathing, or modifying pathfind costs.

**Boundaries**: Out of scope -- road/lane network construction (covered in RoadNetwork research), individual vehicle movement simulation (separate from pathfinding), transit route setup (TransportLine systems).

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Pathfind | Core pathfind components, graph data, queue/result systems, PathUtils |
| Game.dll | Game.Simulation | PathfindSetupSystem, domain-specific setup structs (CitizenPathfindSetup, etc.) |
| Game.dll | Game.Prefabs | PathfindPrefab, PathfindCarData, PathfindPedestrianData, PathfindTrackData |
| Game.dll | Game.Common | PathfindUpdated tag component |
| Game.dll | Game.Debug | PathfindDebugSystem |

## Component Map

### `PathOwner` (Game.Pathfind)

Every entity that needs a path has this component. Controls path state and current progress.

| Field | Type | Description |
|-------|------|-------------|
| m_ElementIndex | int | Current index into the PathElement buffer (how far along the path the agent is) |
| m_State | PathFlags (ushort) | Bitfield tracking path lifecycle: Pending, Failed, Obsolete, Scheduled, etc. |

*Source: `Game.dll` -> `Game.Pathfind.PathOwner`*

### `PathElement` (Game.Pathfind) -- IBufferElementData

Dynamic buffer attached to path-owning entities. Each element represents one segment of the computed path.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | The lane entity this path segment traverses |
| m_TargetDelta | float2 | Start/end position along the lane curve (0.0-1.0 normalized) |
| m_Flags | PathElementFlags (byte) | Flags like Secondary, PathStart, Action, Return, Reverse, WaitPosition, Leader, Hangaround |

*Source: `Game.dll` -> `Game.Pathfind.PathElement`*

### `PathInformation` (Game.Pathfind)

Summary data about a computed path. Attached to the same entity as PathOwner.

| Field | Type | Description |
|-------|------|-------------|
| m_Origin | Entity | Origin lane entity |
| m_Destination | Entity | Destination lane entity |
| m_Distance | float | Total path distance in game units |
| m_Duration | float | Estimated travel time |
| m_TotalCost | float | Weighted pathfind cost (since version `totalPathfindCost`) |
| m_Methods | PathMethod (ushort) | Which transport methods the path uses |
| m_State | PathFlags (ushort) | Path result state |

*Source: `Game.dll` -> `Game.Pathfind.PathInformation`*

### `PathInformations` (Game.Pathfind) -- IBufferElementData

Buffer version of PathInformation for entities needing multiple path results (same fields as PathInformation).

*Source: `Game.dll` -> `Game.Pathfind.PathInformations`*

### `PathNode` (Game.Pathfind)

Compact graph node representation. Packs owner entity index, lane index, segment index, and curve position into a single `ulong` for efficient lookups.

| Field | Type | Description |
|-------|------|-------------|
| m_SearchKey | ulong | Packed: bits[63:32]=owner entity index, bits[30:16]=curve position, bits[15:8]=segment index, bits[7:0]=lane index |

Key methods:
- `GetOwnerIndex()` -- extracts entity index
- `GetLaneIndex()` -- extracts lane index (ushort)
- `GetCurvePos()` -- extracts normalized curve position (float, encoded as int * 3.05e-5)
- `IsSecondary()` -- bit 31 flag

*Source: `Game.dll` -> `Game.Pathfind.PathNode`*

### `Edge` (Game.Pathfind)

A connection in the pathfind graph between two nodes.

| Field | Type | Description |
|-------|------|-------------|
| m_Owner | Entity | The lane entity this edge belongs to |
| m_StartID | NodeID | Start node identifier |
| m_MiddleID | NodeID | Middle node identifier (for curved segments) |
| m_EndID | NodeID | End node identifier |
| m_StartCurvePos | float | Start position on the lane curve |
| m_EndCurvePos | float | End position on the lane curve |
| m_Specification | PathSpecification | Cost/method/flag data for this edge |
| m_Location | LocationSpecification | Position data for heuristic calculations |

*Source: `Game.dll` -> `Game.Pathfind.Edge`*

### `PathSpecification` (Game.Pathfind)

Describes the traversal properties of a pathfind graph edge.

| Field | Type | Description |
|-------|------|-------------|
| m_Costs | PathfindCosts | Base costs (time, behaviour, money, comfort) |
| m_Flags | EdgeFlags (ushort) | Direction flags, connection type |
| m_Methods | PathMethod (ushort) | Which transport methods can use this edge |
| m_AccessRequirement | int | Access authorization requirement |
| m_Length | float | Physical length of the edge |
| m_MaxSpeed | float | Speed limit |
| m_Density | float | Current traffic density (0.01 minimum) |
| m_Rules | RuleFlags (byte) | Lane policy rules (combustion ban, transit ban, etc.) |
| m_BlockageStart | byte | Blockage position start |
| m_BlockageEnd | byte | Blockage position end |
| m_FlowOffset | byte | Traffic flow offset |

*Source: `Game.dll` -> `Game.Pathfind.PathSpecification`*

### `PathTarget` (Game.Pathfind)

Represents a candidate start or end point for a pathfind query.

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | Target node entity |
| m_Entity | Entity | Lane entity |
| m_Delta | float | Position along the lane |
| m_Cost | float | Cost to reach this target from the entity's position |
| m_Flags | EdgeFlags | Edge direction flags |

*Source: `Game.dll` -> `Game.Pathfind.PathTarget`*

## Enums

### `PathFlags`

```
Pending = 1        // Pathfind request queued
Failed = 2         // Pathfind failed (no route)
Obsolete = 4       // Path is stale, needs recalculation
Scheduled = 8      // Request submitted to worker threads
Append = 0x10      // Append new segments to existing path
Updated = 0x20     // Path was recently updated
Stuck = 0x40       // Agent is stuck
WantsEvent = 0x80  // Fire event when pathfind completes
AddDestination = 0x100
Debug = 0x200      // Debug pathfind visualization
Divert = 0x400     // Path diversion in progress
DivertObsolete = 0x800
CachedObsolete = 0x1000
```

Note: On deserialization, `Pending` is automatically converted to `Obsolete` (paths in progress at save time are invalidated).

### `PathMethod`

Bitfield indicating which transport methods an agent can use:

```
Pedestrian = 1         Road = 2             Parking = 4
PublicTransportDay = 8 Track = 0x10         Taxi = 0x20
CargoTransport = 0x40  CargoLoading = 0x80  Flying = 0x100
PublicTransportNight = 0x200   Boarding = 0x400
Offroad = 0x800        SpecialParking = 0x1000
MediumRoad = 0x2000    Bicycle = 0x4000
BicycleParking = 0x8000
```

### `PathElementFlags`

```
Secondary = 1      PathStart = 2     Action = 4
Return = 8         Reverse = 0x10    WaitPosition = 0x20
Leader = 0x40      Hangaround = 0x80
```

### `PathfindFlags`

Controls pathfind algorithm behavior:

```
Stable = 1              // No random cost variation
IgnoreFlow = 2          // Ignore traffic density
ForceForward = 4        ForceBackward = 8
NoHeuristics = 0x10     // Disable A* heuristics (Dijkstra)
ParkingReset = 0x20
Simplified = 0x40       // Simplified path search
MultipleOrigins = 0x80  MultipleDestinations = 0x100
IgnoreExtraStartAccessRequirements = 0x200
IgnoreExtraEndAccessRequirements = 0x400
IgnorePath = 0x800
SkipPathfind = 0x1000   // Skip graph traversal, use cheapest target directly
```

### `RuleFlags`

Lane policy rules that affect pathfind edge traversal:

```
HasBlockage = 1               ForbidCombustionEngines = 2
ForbidTransitTraffic = 4      ForbidHeavyTraffic = 8
ForbidPrivateTraffic = 0x10   ForbidSlowTraffic = 0x20
AvoidBicycles = 0x40
```

### `SetupTargetType`

All possible target types for pathfind setup (47 values). Key ones:
- `CurrentLocation` (0) -- current entity position
- `ResourceSeller`, `StorageTransfer`, `ResourceExport` -- economic pathfinding
- `TransportVehicle`, `RouteWaypoints`, `Taxi` -- transit/taxi
- `GarbageCollector`, `FireEngine`, `Ambulance`, `PolicePatrol`, `PostVan`, `Hearse` -- service vehicles
- `RandomTraffic`, `OutsideConnection` -- road traffic
- `Leisure`, `Attraction`, `SchoolSeekerTo`, `JobSeekerTo` -- citizen activities
- `FindHome`, `HomelessShelter` -- housing search
- Various `*Request` types -- for service dispatch

## System Map

### `PathfindSetupSystem` (Game.Simulation)

The entry point for all pathfind requests. Other systems submit requests to its queue, and it resolves origin/destination targets before forwarding to the queue system.

- **Base class**: GameSystemBase, IPreDeserialize
- **Update phase**: Simulation
- **Key responsibilities**:
  - Manages `SetupQueue` instances that other systems write to
  - Resolves `SetupQueueTarget` into concrete `PathTarget` lists via domain-specific setup helpers
  - Forwards resolved actions to `PathfindQueueSystem`
  - Spreads work across frames to avoid spikes

**Domain-specific setup helpers** (initialized in OnCreate):
- `CommonPathfindSetup` -- current location, accident, safety
- `CitizenPathfindSetup` -- tourist, leisure, school, job, attraction, homeless, find home
- `TransportPathfindSetup` -- transport vehicles, taxis, route waypoints
- `FirePathfindSetup` -- fire engines, emergency shelters, evacuation
- `HealthcarePathfindSetup` -- ambulances, hospitals, hearses
- `PolicePathfindSetup` -- police patrols, crime producers, prisoner transport
- `GarbagePathfindSetup` -- garbage collectors, garbage transfer
- `PostServicePathfindSetup` -- post vans, mail transfer, mailboxes
- `RoadPathfindSetup` -- maintenance, random traffic, outside connections
- `ResourcePathfindSetup` -- resource sellers, exports, storage transfer
- `GoodsDeliveryPathfindSetup` -- goods delivery
- `AreaPathfindSetup` -- area locations, wood resources

**Key methods**:
- `GetQueue(object system, int maxDelayFrames, int spreadFrames)` -- returns a `NativeQueue<SetupQueueItem>` for the calling system to enqueue pathfind requests
- `AddQueueWriter(JobHandle)` -- registers job dependency for queue writes
- `CompleteSetup()` -- finalizes target resolution and forwards to PathfindQueueSystem
- `OnUpdate()` -- dequeues from active queues, creates PathfindAction for each, sorts by target type, dispatches to helpers

### `PathfindQueueSystem` (Game.Pathfind)

Manages the multi-threaded pathfind worker pool. Takes resolved actions from PathfindSetupSystem and schedules them on worker threads.

- **Base class**: GameSystemBase, IPreDeserialize
- **Update phase**: Simulation
- **Key responsibilities**:
  - Maintains 9 action lists (Create, Update, Delete, Pathfind, Coverage, Availability, Density, Time, Flow)
  - Manages worker data (double-buffered pathfind graph)
  - Schedules `PathfindWorkerJob` on half the available CPU cores
  - Handles priority queuing (high-priority requests processed first)
  - Graph modification actions (Create/Update/Delete edges) run separately from pathfind queries

**Action types**:
- `Pathfind` -- standard A* path queries
- `Coverage` -- service coverage calculations (e.g., fire station coverage)
- `Availability` -- provider availability queries
- `Create/Update/Delete` -- graph modification (add/remove edges when roads change)
- `Density/Time/Flow` -- update traffic density, time, and flow data on edges

**Worker architecture**:
- Uses `JobsUtility.JobWorkerCount / 2` threads
- Each worker has a `UnsafeLinearAllocator` (1 MB initial) for temporary pathfind data
- Workers pull actions from a shared atomic counter (`Interlocked.Increment`)
- Rewind allocator after each completed action

### `PathfindResultSystem` (Game.Pathfind)

Processes completed pathfind results and writes them back to entities.

- **Base class**: GameSystemBase, IPreDeserialize
- **Update phase**: Simulation
- **Key responsibilities**:
  - Checks action completion state via `PathfindActionState.Completed`
  - Writes path results into `PathOwner`, `PathInformation`, `PathElement` buffer
  - Tracks query statistics (success rate, graph traversal %, efficiency)
  - Creates `PathUpdated` / `CoverageUpdated` event entities when `WantsEvent` flag is set
  - Logs errors for failed pathfinds with system name and target types

### `LaneDataSystem` (Game.Pathfind)

Converts lane network data into pathfind graph edges. Runs when lanes are created or modified.

- **Base class**: GameSystemBase
- **Reads**: Lane, EdgeLane, MasterLane, SlaveLane, Owner, PrefabRef, CarLane, PedestrianLane, TrackLane, ConnectionLane
- **Writes**: Pathfind graph edges via `CreateAction`/`UpdateAction`/`DeleteAction`
- **Queries**: Lanes with Updated/Created/Deleted tags

### `LanesModifiedSystem` (Game.Pathfind)

Handles pathfind edge updates when existing lanes are modified (e.g., road policy changes).

### `RoutesModifiedSystem` (Game.Pathfind)

Handles pathfind edge updates when transit routes change.

### `LanePoliciesSystem` (Game.Pathfind)

Updates pathfind edge `RuleFlags` when district or building lane policies change (e.g., banning combustion engines).

### `PathOwnerTargetMovedSystem` (Game.Pathfind)

Marks paths as obsolete when the target entity of a path has moved, triggering re-pathing.

### `RouteDataSystem` (Game.Pathfind)

Updates route-related pathfind data for transit connections.

## Data Flow

```
[Game Systems]                     [Pathfind Pipeline]                     [Results]

  Citizens,                    +---------------------------+
  Vehicles,         enqueue    |  PathfindSetupSystem      |
  Services    ===============> |  - GetQueue() API         |
                SetupQueueItem |  - Resolves targets via   |
                               |    domain-specific helpers|
                               +---------------------------+
                                            |
                                    PathfindAction
                                   (start + end targets)
                                            |
                                            v
                               +---------------------------+
                               |  PathfindQueueSystem      |
                               |  - Multi-threaded A*      |
                               |  - Graph double-buffering |
                               |  - Priority scheduling    |
                               +---------------------------+
                                            |
                                   completed actions
                                            |
                                            v
                               +---------------------------+
                               |  PathfindResultSystem     |
                               |  - Writes PathElement buf |
                               |  - Updates PathOwner      |     PathOwner
                               |  - Updates PathInformation| ==> PathElement buffer
                               |  - Fires PathUpdated event|     PathInformation
                               +---------------------------+

  [Graph Maintenance]
  LaneDataSystem ----> Create/Update/Delete edges
  LanesModifiedSystem -> Update edges on modification
  LanePoliciesSystem --> Update RuleFlags on edges
```

### Request Lifecycle

1. A game system (e.g., `CitizenBehaviorSystem`) calls `PathfindSetupSystem.GetQueue()` to get a queue
2. System enqueues `SetupQueueItem` with origin/destination `SetupQueueTarget` and `PathfindParameters`
3. `PathfindSetupSystem.OnUpdate()` dequeues items, creates `PathfindAction` objects, dispatches to domain helpers to resolve concrete `PathTarget` lists
4. Resolved actions are forwarded to `PathfindQueueSystem.Enqueue()`
5. `PathfindQueueSystem.OnUpdate()` schedules `PathfindWorkerJob` on worker threads
6. Workers execute A* search on the pathfind graph, writing results to the action's data
7. `PathfindResultSystem.OnUpdate()` checks for completed actions, writes results to entity components

### `PathfindParameters` (Game.Pathfind)

Submitted with every pathfind request via `SetupQueueItem`. Controls which transport methods are available, cost weights, and search constraints.

| Field | Type | Description |
|-------|------|-------------|
| m_Methods | PathMethod (ushort) | Which transport methods are allowed |
| m_Weights | PathfindWeights | Per-agent cost dimension weights (time, behaviour, money, comfort) |
| m_MaxCost | float | Maximum total cost before search terminates |
| m_MaxSpeed | float2 | Maximum vehicle speed (m/s) |
| m_WalkSpeed | float2 | Walking speed (m/s) |
| m_MaxResultCount | int | Maximum number of path results |
| m_PathfindFlags | PathfindFlags | Algorithm behavior flags |
| m_IgnoredRules | RuleFlags | Lane policy rules to ignore |
| m_ParkingTarget | Entity | Specific parking target entity (if any) |
| m_ParkingDelta | float | Position along the parking lane curve (0.0-1.0) |
| m_ParkingSize | float | Size of the parking space required |
| m_Authorization1 | Entity | First access authorization entity (e.g., gated area pass) |
| m_Authorization2 | Entity | Second access authorization entity |

*Source: `Game.dll` -> `Game.Pathfind.PathfindParameters`*

**Parking fields**: `m_ParkingTarget`, `m_ParkingDelta`, and `m_ParkingSize` are set when the pathfind request needs to find a route that ends at a specific parking spot. The parking target is the lane entity, the delta is the normalized position on that lane, and the size constrains which parking spaces the vehicle can fit in.

**Authorization fields**: `m_Authorization1` and `m_Authorization2` are entity references to access authorization passes. These are checked against `PathSpecification.m_AccessRequirement` on graph edges -- if an edge requires authorization, the agent must carry a matching authorization entity to traverse it.

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| PathfindPrefab | PrefabBase | `Game.Prefabs.PathfindPrefab` -- m_TrackTrafficFlow (bool) |
| Car driving costs | PathfindCarData | DrivingCost, TurningCost, UnsafeTurningCost, UTurnCost, CurveAngleCost, LaneCrossCost, ParkingCost, SpawnCost, ForbiddenCost |
| Pedestrian costs | PathfindPedestrianData | WalkingCost, CrosswalkCost, UnsafeCrosswalkCost, SpawnCost |
| Track costs | PathfindTrackData | (similar pattern for rail) |
| Transport costs | PathfindTransportData | (boarding/transfer costs) |
| Connection costs | PathfindConnectionData | (outside connection costs) |
| Heuristic data | PathfindHeuristicData | CarCosts, TrackCosts, PedestrianCosts, FlyingCosts, OffRoadCosts, TaxiCosts -- used for A* heuristic estimates |

### Cost Model

Pathfind costs are 4-dimensional vectors (`float4`) stored in the `PathfindCosts` struct:

| Channel | Component | What It Represents |
|---------|-----------|-------------------|
| .x | Time | Travel time cost (based on edge length / speed) |
| .y | Behaviour | Behavioral preference cost (e.g., avoiding certain route types) |
| .z | Money | Monetary cost (tolls, fares, parking) |
| .w | Comfort | Comfort/quality cost (road condition, crowding) |

The total cost is computed as: `dot(costs, weights)` where weights are per-agent (from `PathfindWeights`). Unless `PathfindFlags.Stable` is set, costs include random variation (`* random.NextFloat(0.5, 1.0)`) to distribute agents across routes.

#### `PathfindCosts` Struct

The `PathfindCosts` struct wraps a `float4` value and provides the `TryAddCosts` utility method:

```csharp
// PathfindCosts.TryAddCosts -- conditionally adds costs to the total
// Returns false if adding these costs would exceed m_MaxCost, allowing
// early termination of edge evaluation.
public bool TryAddCosts(ref float totalCost, PathfindWeights weights, float maxCost)
{
    totalCost += math.dot(m_Value, weights.m_Value);
    return totalCost < maxCost;
}
```

`TryAddCosts` is used throughout the pathfind edge evaluation to accumulate costs incrementally. It returns `false` when the running total exceeds `m_MaxCost`, enabling the pathfinder to skip expensive edges early without computing all cost components.

## Harmony Patch Points

### Candidate 1: `PathfindSetupSystem.GetQueue`

- **Signature**: `NativeQueue<SetupQueueItem> GetQueue(object system, int maxDelayFrames, int spreadFrames = 0)`
- **Patch type**: Postfix
- **What it enables**: Intercept pathfind queues as they're created; wrap or monitor requests
- **Risk level**: Medium
- **Side effects**: Must handle native containers carefully

### Candidate 2: `PathfindResultSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: React to completed pathfind results, modify paths before they're applied
- **Risk level**: High (complex internal state)
- **Side effects**: Performance-critical path

### Candidate 3: `PathUtils.CalculateCost` (static)

- **Signature**: `float CalculateCost(ref Random random, in PathSpecification pathSpecification, in PathfindParameters pathfindParameters)`
- **Patch type**: Prefix (with `__result` out) or Postfix
- **What it enables**: Modify edge traversal costs dynamically (e.g., make agents prefer/avoid certain routes)
- **Risk level**: Low (pure function, no side effects)
- **Side effects**: Called extremely frequently -- patch must be lightweight

### Candidate 4: Domain-specific setup helpers (e.g., `CitizenPathfindSetup`)

- **Signature**: Various `SetupXxxJob` struct methods
- **Patch type**: These are Burst-compiled jobs, **not patchable with Harmony**
- **What it enables**: N/A
- **Risk level**: N/A
- **Note**: To modify pathfind parameters for specific agent types, intercept at the queue level before the setup phase

### Candidate 5: `LanePoliciesSystem` jobs

- **Signature**: `CheckDistrictLanesJob`, `CheckBuildingLanesJob`
- **Patch type**: Burst-compiled -- **not patchable**
- **Alternative**: Add a custom system that runs after LanePoliciesSystem to further modify edge `RuleFlags`

## Mod Blueprint

- **Systems to create**:
  - Custom `GameSystemBase` that requests pathfinds via `PathfindSetupSystem.GetQueue()`
  - Post-processing system that reacts to `PathUpdated` events
  - Optional system that modifies `PathfindParameters` before submission (e.g., custom cost weights)

- **Components to add**:
  - Custom component on entities that need modified pathfinding behavior
  - Tag component to identify entities with custom path preferences

- **Patches needed**:
  - Postfix on `PathUtils.CalculateCost` for route cost modification
  - Or: Custom ECS system that modifies `PathElement` buffers post-pathfind

- **Settings**: User-configurable cost multipliers for different transport methods

- **UI changes**: Optional debug overlay showing pathfind statistics via `PathfindResultSystem.queryStats`

## Examples

### Example 1: Reading an Entity's Current Path

Query entities with `PathOwner` and `PathElement` buffer to read their computed paths.

```csharp
using Game.Pathfind;
using Unity.Entities;
using Unity.Collections;

public partial class ReadPathSystem : GameSystemBase
{
    private EntityQuery m_PathQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PathQuery = GetEntityQuery(
            ComponentType.ReadOnly<PathOwner>(),
            ComponentType.ReadOnly<PathElement>()
        );
    }

    protected override void OnUpdate()
    {
        var pathOwners = m_PathQuery.ToComponentDataArray<PathOwner>(Allocator.Temp);
        var entities = m_PathQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            PathOwner owner = pathOwners[i];
            DynamicBuffer<PathElement> elements = EntityManager.GetBuffer<PathElement>(entities[i]);

            // Current position in path
            int currentIndex = owner.m_ElementIndex;

            // Check path state
            if ((owner.m_State & PathFlags.Failed) != 0)
            {
                // Path failed -- no route found
                continue;
            }

            if ((owner.m_State & PathFlags.Obsolete) != 0)
            {
                // Path needs recalculation
                continue;
            }

            // Read remaining path elements
            for (int j = currentIndex; j < elements.Length; j++)
            {
                PathElement element = elements[j];
                Entity laneEntity = element.m_Target;
                float2 delta = element.m_TargetDelta; // start/end along lane
                // Process path segment...
            }
        }

        pathOwners.Dispose();
        entities.Dispose();
    }
}
```

### Example 2: Requesting a Pathfind via the Queue API

Submit a pathfind request by writing to the setup queue. The system must run before `PathfindSetupSystem`.

```csharp
using Game.Pathfind;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public partial class CustomPathfindRequestSystem : GameSystemBase
{
    private PathfindSetupSystem m_PathfindSetupSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PathfindSetupSystem = World.GetOrCreateSystemManaged<PathfindSetupSystem>();
    }

    protected override void OnUpdate()
    {
        // Get a queue from the setup system
        // maxDelayFrames: how many frames the result can be delayed
        NativeQueue<SetupQueueItem> queue = m_PathfindSetupSystem.GetQueue(
            this,           // owning system
            maxDelayFrames: 16,
            spreadFrames: 4 // spread processing over 4 frames
        );

        // Build the pathfind request
        PathfindParameters parameters = new PathfindParameters
        {
            m_MaxCost = 50000f,
            m_Methods = PathMethod.Road | PathMethod.Pedestrian,
            m_Weights = new PathfindWeights(1f, 0.5f, 0f, 0.5f), // time, behaviour, money, comfort
            m_MaxSpeed = new float2(40f, 40f), // m/s
            m_WalkSpeed = new float2(1.4f, 1.4f),
            m_PathfindFlags = PathfindFlags.Stable // no random cost variation
        };

        SetupQueueTarget origin = new SetupQueueTarget
        {
            m_Type = SetupTargetType.CurrentLocation,
            m_Methods = PathMethod.Road | PathMethod.Pedestrian
        };

        SetupQueueTarget destination = new SetupQueueTarget
        {
            m_Type = SetupTargetType.CurrentLocation,
            m_Methods = PathMethod.Road | PathMethod.Pedestrian
        };

        // entity is the entity that needs a path
        Entity entity = default; // replace with actual entity

        queue.Enqueue(new SetupQueueItem(entity, parameters, origin, destination));

        // Register the queue writer dependency
        m_PathfindSetupSystem.AddQueueWriter(Dependency);
    }
}
```

### Example 3: Reacting to Completed Pathfinds

Listen for `PathUpdated` event entities to react when pathfinding completes.

```csharp
using Game.Common;
using Game.Pathfind;
using Unity.Entities;
using Unity.Collections;

public partial class PathCompletionSystem : GameSystemBase
{
    private EntityQuery m_UpdatedPathQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_UpdatedPathQuery = GetEntityQuery(
            ComponentType.ReadOnly<Event>(),
            ComponentType.ReadOnly<PathUpdated>()
        );
    }

    protected override void OnUpdate()
    {
        var events = m_UpdatedPathQuery.ToComponentDataArray<PathUpdated>(Allocator.Temp);

        for (int i = 0; i < events.Length; i++)
        {
            Entity pathOwnerEntity = events[i].m_Owner;

            if (!EntityManager.Exists(pathOwnerEntity))
                continue;

            if (EntityManager.HasComponent<PathInformation>(pathOwnerEntity))
            {
                PathInformation info = EntityManager.GetComponentData<PathInformation>(pathOwnerEntity);

                // Access path results
                float distance = info.m_Distance;
                float duration = info.m_Duration;
                float cost = info.m_TotalCost;
                PathMethod methods = info.m_Methods;

                // Check if the path uses specific methods
                if ((methods & PathMethod.PublicTransportDay) != 0)
                {
                    // Path includes public transit
                }
            }
        }

        events.Dispose();
    }
}
```

### Example 4: Forcing Path Recalculation

Mark an entity's path as obsolete to trigger re-pathing.

```csharp
using Game.Pathfind;
using Unity.Entities;

public partial class ForceRepathSystem : GameSystemBase
{
    protected override void OnUpdate()
    {
        Entity targetEntity = default; // replace with actual entity

        if (EntityManager.HasComponent<PathOwner>(targetEntity))
        {
            PathOwner owner = EntityManager.GetComponentData<PathOwner>(targetEntity);

            // Set the Obsolete flag -- the pathfind system will recalculate
            owner.m_State |= PathFlags.Obsolete;

            // Optionally reset the element index
            owner.m_ElementIndex = 0;

            EntityManager.SetComponentData(targetEntity, owner);
        }
    }
}
```

### Example 5: Modifying Pathfind Cost Calculation (Harmony Patch)

Patch `PathUtils.CalculateCost` to adjust route costs -- for example, making agents prefer less congested routes.

```csharp
using HarmonyLib;
using Game.Pathfind;

[HarmonyPatch(typeof(PathUtils), nameof(PathUtils.CalculateCost),
    new[] { typeof(Unity.Mathematics.Random), typeof(PathSpecification), typeof(PathfindParameters) },
    new[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Ref })]
public static class PathCostPatch
{
    public static void Postfix(ref float __result, in PathSpecification pathSpecification)
    {
        // Increase cost for high-density edges (congested roads)
        if (pathSpecification.m_Density > 0.5f)
        {
            float congestionPenalty = 1f + (pathSpecification.m_Density - 0.5f) * 2f;
            __result *= congestionPenalty;
        }
    }
}
```

**Warning**: `PathUtils.CalculateCost` is called extremely frequently during pathfinding. The patch must be as lightweight as possible. Heavy computation here will significantly impact simulation performance.

## PathMethod & PathNode for Lane Connection Modding

### PathMethod (Game.Pathfind)

Flags enum controlling which pathfinding methods can use a connection:

| Flag | Description |
|------|-------------|
| `Road` | Usable by road vehicles |
| `Track` | Usable by track vehicles (tram, train) |
| `Pedestrian` | Usable by pedestrians |
| `Flying` | Usable by aircraft |
| `Watercraft` | Usable by watercraft |

Connections store `PathMethod` to filter which pathfinder agents can traverse them. Lane connection mods use this to restrict which vehicle types can use custom connections.

### PathNode (Game.Pathfind)

Identifies lane endpoints in the pathfind graph:

| Field | Type | Description |
|-------|------|-------------|
| `m_StartNode` | ushort | Lane graph start node index |
| `m_MiddleNode` | ushort | Lane graph middle node index |
| `m_EndNode` | ushort | Lane graph end node index |

`Connection` buffer elements store source/target `PathNode` and `PathMethod` to define how the pathfinder traverses between lanes. Lane connection mods (e.g., Traffic mod's `GeneratedConnection`) create custom connections with appropriate `PathNode` endpoints and `PathMethod` flags.

## Open Questions

- [ ] How exactly does the A* heuristic use `PathfindHeuristicData`? The costs appear to be per-method minimums for admissible heuristic estimation, but the exact formula in `PathfindJobs.PathfindExecutor` is in Burst-compiled code and hard to trace.
- [ ] What is the exact relationship between `NativePathfindData` and the lane graph? The double-buffering scheme with `m_NextWorkerIndex` / `m_LastWorkerIndex` suggests read/write separation, but the data structure internals are opaque.
- [ ] How does `PathfindTargetSeekerData` resolve generic `SetupQueueTarget` types into lane-level `PathTarget` lists? The seeker pattern appears to use spatial queries but the implementation is spread across many domain helpers.
- [ ] The `PathfindFlags.SkipPathfind` flag appears to skip graph traversal entirely and pick the cheapest target directly -- what scenarios use this? Appears related to simplified service dispatch.
- [ ] How does the `PathfindFlags.Divert` mechanism work? It seems to allow mid-path rerouting but the trigger conditions are unclear.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key namespaces: Game.Pathfind, Game.Simulation, Game.Prefabs, Game.Common
- Types decompiled: PathOwner, PathElement, PathInformation, PathInformations, PathNode, Edge, PathSpecification, PathTarget, PathfindParameters, PathfindWeights, PathfindCosts, SetupQueueItem, SetupQueueTarget, PathfindSetupSystem, PathfindQueueSystem, PathfindResultSystem, PathUtils, PathfindPrefab, PathfindCarData, PathfindPedestrianData, PathfindHeuristicData, all related enums
