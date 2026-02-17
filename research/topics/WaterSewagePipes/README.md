# Research: Water & Sewage Pipe Network

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 routes fresh water from pumping stations through underground pipes to buildings, and how sewage flows back from buildings to treatment outlets -- the entire pipe network simulation.

**Why**: To understand the pipe graph model so mods can intercept water/sewage flow, modify building consumption, add custom pipe behaviors, or create new water infrastructure types.

**Boundaries**: This research covers the underground pipe network only. Surface water simulation (GPU compute shaders, WaterSourceData, rivers, flooding) is covered separately in [WaterSystem](../WaterSystem/README.md).

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | WaterPipeFlowSystem, WaterPipeFlowJob, WaterPipeNode, WaterPipeEdge, WaterPipeBuildingConnection, WaterPipeNodeConnection, WaterPipeValveConnection, WaterPipeGraphUtils, WaterPipeBuildingGraphSystem, WaterPipeEdgeGraphSystem, WaterPipeRoadConnectionGraphSystem, WaterPipeOutsideConnectionGraphSystem, WaterPipeGraphDeleteSystem, WaterPipeGraphReferencesSystem, WaterPipePollutionSystem, AdjustWaterConsumptionSystem, DispatchWaterSystem, GroundWaterSystem, GroundWater |
| Game.dll | Game.Buildings | WaterConsumer, WaterConsumerFlags, SewageOutlet |
| Game.dll | Game.Net | WaterPipeConnection (road/pipe capacity) |
| Game.dll | Game.Objects | WaterPipeOutsideConnection (marker for outside connections) |
| Game.dll | Game.Prefabs | WaterPipeParameterData, WaterPipeParametersPrefab, WaterPipeConnection (prefab), GroundWaterPowered, SewageOutlet (prefab), ConsumptionData |

## Architecture Overview

The pipe network uses a **max-flow graph solver** running on the CPU. Unlike the surface water system (GPU compute shaders), the pipe network is a classic graph theory problem: nodes connected by edges with capacities, solved via a multi-phase max-flow algorithm.

### Graph Structure

The network has three entity types:

1. **WaterPipeNode** -- graph vertices. Each road segment, building, pump station, and outside connection gets a node.
2. **WaterPipeEdge** -- graph edges. Connect two nodes with independent fresh water and sewage capacities.
3. **ConnectedFlowEdge** (buffer) -- adjacency list on each node pointing to its connected edges.

Two special nodes exist:
- **sourceNode** -- virtual super-source. All water producers (pumps) connect to this node.
- **sinkNode** -- virtual super-sink. All water consumers (buildings) connect to this node.

### Dual Networks

Fresh water and sewage flow through the **same graph** but are solved **independently**. The WaterPipeFlowJob maintains two parallel datasets (`m_FreshData` and `m_SewageData`) with the same node/edge topology but separate flow computations.

### Solver Phases

The solver runs on a 128-frame cycle (matching the electricity grid):

1. **Frame 64**: `AdjustWaterConsumptionSystem` -- recalculates building demand
2. **Frame 65**: `PreparePhase` -- builds the flow graph from ECS entities
3. **Frames 66-125**: `FlowPhase` -- runs the max-flow solver iteratively
4. **Frame 62** (next cycle): `ApplyPhase` -- writes solved flows back to WaterPipeEdge components

The max-flow solver has sub-phases:
- **Producer** -- initial max-flow from producers to consumers
- **PostProducer** -- labels shortage/connected subgraphs
- **Trade** -- enables import/export edges for outside connections
- **PostTrade** -- labels connected components, limits import capacity
- **FluidFlow** -- distributes flow proportionally across redundant paths

## Component Map

### `WaterPipeNode` (Game.Simulation)

Graph vertex in the pipe network. Every road segment and building connection has one.

| Field | Type | Description |
|-------|------|-------------|
| m_Index | int | Temporary index assigned each solve cycle (not persistent) |
| m_FreshPollution | float | Pollution level of fresh water at this node (0.0-1.0) |

*Source: `Game.dll` -> `Game.Simulation.WaterPipeNode`*

### `WaterPipeEdge` (Game.Simulation)

Graph edge connecting two nodes. Carries both fresh water and sewage.

| Field | Type | Description |
|-------|------|-------------|
| m_Index | int | Temporary index assigned each solve cycle |
| m_Start | Entity | Start node entity |
| m_End | Entity | End node entity |
| m_FreshFlow | int | Solved fresh water flow through this edge |
| m_FreshPollution | float | Pollution level of fresh water on this edge (0.0-1.0) |
| m_SewageFlow | int | Solved sewage flow through this edge |
| m_FreshCapacity | int | Maximum fresh water capacity |
| m_SewageCapacity | int | Maximum sewage capacity |
| m_Flags | WaterPipeEdgeFlags | Status flags: shortage, backup, disconnected |

Properties:
- `flow` -> `int2(m_FreshFlow, m_SewageFlow)`
- `capacity` -> `int2(m_FreshCapacity, m_SewageCapacity)`

*Source: `Game.dll` -> `Game.Simulation.WaterPipeEdge`*

### `WaterPipeEdgeFlags` (Game.Simulation)

| Flag | Value | Description |
|------|-------|-------------|
| None | 0 | No issues |
| WaterShortage | 1 | Fresh water demand exceeds supply on this path |
| SewageBackup | 2 | Sewage capacity exceeded on this path |
| WaterDisconnected | 4 | Not connected to any water producer |
| SewageDisconnected | 8 | Not connected to any sewage processor |

### `WaterPipeNodeConnection` (Game.Simulation)

Links a road edge entity to its corresponding pipe graph node.

| Field | Type | Description |
|-------|------|-------------|
| m_WaterPipeNode | Entity | The WaterPipeNode entity for this road segment |

*Source: `Game.dll` -> `Game.Simulation.WaterPipeNodeConnection`*

### `WaterPipeBuildingConnection` (Game.Simulation)

Links a building directly to the pipe graph (for buildings with dedicated pipe connections, not just road-adjacent buildings).

| Field | Type | Description |
|-------|------|-------------|
| m_ProducerEdge | Entity | Edge connecting to producer (pump) side of graph |
| m_ConsumerEdge | Entity | Edge connecting to consumer (demand) side of graph |

Methods:
- `GetProducerNode()` -- returns `m_FlowEdges[m_ProducerEdge].m_End`
- `GetConsumerNode()` -- returns `m_FlowEdges[m_ConsumerEdge].m_Start`

*Source: `Game.dll` -> `Game.Simulation.WaterPipeBuildingConnection`*

### `WaterPipeValveConnection` (Game.Simulation)

Links an entity to a valve node (used for water shutoff control).

| Field | Type | Description |
|-------|------|-------------|
| m_ValveNode | Entity | The valve WaterPipeNode entity |

*Source: `Game.dll` -> `Game.Simulation.WaterPipeValveConnection`*

### `WaterConsumer` (Game.Buildings)

Attached to every building that consumes water and produces sewage.

| Field | Type | Description |
|-------|------|-------------|
| m_Pollution | float | Current pollution level of received fresh water (0.0-1.0) |
| m_WantedConsumption | int | Desired water consumption (calculated from prefab + occupancy) |
| m_FulfilledFresh | int | Actual fresh water received this cycle |
| m_FulfilledSewage | int | Actual sewage capacity available this cycle |
| m_FreshCooldownCounter | byte | Notification cooldown for water shortage |
| m_SewageCooldownCounter | byte | Notification cooldown for sewage backup |
| m_Flags | WaterConsumerFlags | Connection status flags |

Properties:
- `waterConnected` -- true if `WaterConsumerFlags.WaterConnected` is set
- `sewageConnected` -- true if `WaterConsumerFlags.SewageConnected` is set

*Source: `Game.dll` -> `Game.Buildings.WaterConsumer`*

### `WaterConsumerFlags` (Game.Buildings)

| Flag | Value | Description |
|------|-------|-------------|
| None | 0 | Not connected to water or sewage |
| WaterConnected | 1 | Building is receiving fresh water |
| SewageConnected | 2 | Building has sewage service |

### `WaterPipeConnection` (Game.Net)

Attached to road/pipe edge entities to define pipe capacity along that segment.

| Field | Type | Description |
|-------|------|-------------|
| m_FreshCapacity | int | Fresh water pipe capacity (default: 1073741823 = max int/2) |
| m_SewageCapacity | int | Sewage pipe capacity (default: 1073741823) |
| m_StormCapacity | int | Storm water drain capacity (default: 5000 for legacy) |

*Source: `Game.dll` -> `Game.Net.WaterPipeConnection`*

### `GroundWater` (Game.Simulation)

Per-cell groundwater data on a 256x256 grid.

| Field | Type | Description |
|-------|------|-------------|
| m_Amount | short | Current groundwater amount (0-10000) |
| m_Polluted | short | Polluted portion of groundwater |
| m_Max | short | Maximum capacity for this cell |

Methods:
- `Consume(int amount)` -- reduces amount and proportionally reduces pollution

*Source: `Game.dll` -> `Game.Simulation.GroundWater`*

### `WaterPipeOutsideConnection` (Game.Objects)

Empty marker component. Identifies entities that represent connections to the outside world (water import/export).

*Source: `Game.dll` -> `Game.Objects.WaterPipeOutsideConnection`*

## System Map

### `WaterPipeFlowSystem` (Game.Simulation)

The central flow solver. Runs a max-flow algorithm on the pipe graph.

- **Base class**: GameSystemBase (also ISerializable, IPostDeserialize)
- **Update phase**: Simulation (128-frame cycle, offset 64)
- **Key constants**:
  - `kUpdateInterval = 128` -- frames per solve cycle
  - `kUpdateOffset = 64` -- frame offset within cycle
  - `kMaxEdgeCapacity = 1073741823` -- max int/2, used for unlimited capacity edges
  - `kLayerHeight = 20` -- max-flow solver layer height
- **Key properties**:
  - `sourceNode` -- the virtual super-source entity (all producers connect here)
  - `sinkNode` -- the virtual super-sink entity (all consumers connect here)
  - `nodeArchetype` -- archetype for creating new WaterPipeNode entities
  - `edgeArchetype` -- archetype for creating new WaterPipeEdge entities
  - `fluidFlowEnabled` -- enables/disables the fluid flow distribution phase
  - `ready` -- true after the first solve cycle completes
- **Three phases**:
  1. **PreparePhase** (frame 65): Builds parallel node/edge arrays from ECS entities, constructs adjacency list
  2. **FlowPhase** (frames 66-125): Runs WaterPipeFlowJob with max-flow solver, separate for fresh and sewage
  3. **ApplyPhase** (frame 62): Writes solved flow values and flags back to WaterPipeEdge components

### `WaterPipeFlowJob` (Game.Simulation)

The Burst-compiled max-flow solver job.

- **Sub-phases**:
  - `Initial` -- resets max-flow solver state
  - `Producer` -- runs max-flow from source to sink (local production)
  - `PostProducer` -- labels shortage/connected subgraphs, enables trade edges
  - `Trade` -- runs max-flow again with import/export edges enabled
  - `PostTrade` -- labels connected components, sets up fluid flow
  - `FluidFlow` -- distributes flow proportionally using push-relabel algorithm
  - `Complete` -- all phases done
- **Key fields**:
  - `m_ImportCapacity` -- capacity for import edges (fresh: max int, sewage: max int)
  - `m_ExportCapacity` -- capacity for export edges (fresh: max int, sewage: 0, meaning sewage cannot be exported)

### `AdjustWaterConsumptionSystem` (Game.Simulation)

Recalculates building water demand every 128 frames.

- **Base class**: GameSystemBase
- **Update interval**: 128 frames (offset 64, frame 64)
- **Queries**: Buildings with WaterConsumer + Building + UpdateFrame + PrefabRef (excluding Deleted, Temp)
- **Key behavior**:
  1. Reads `ConsumptionData.m_WaterConsumption` from the building's prefab
  2. Applies water fee consumption multiplier (higher fees = lower consumption)
  3. Scales by building occupancy (number of renters/employees relative to capacity)
  4. Inactive buildings consume 10% of normal
  5. Updates `WaterConsumer.m_WantedConsumption`
  6. Sets the consumer edge's `m_FreshCapacity` and `m_SewageCapacity` to match demand

### `DispatchWaterSystem` (Game.Simulation)

Distributes solved flow results to buildings after each solve cycle.

- **Base class**: GameSystemBase
- **Update interval**: 128 frames (offset 64, frame 63 = kStatusFrame)
- **Key behavior**:
  1. For buildings with `WaterPipeBuildingConnection`: reads flow directly from their consumer edge
  2. For buildings without direct connection (road-adjacent): calculates proportional share based on the road's edge flow ratio
  3. Sets `WaterConsumer.m_FulfilledFresh` and `m_FulfilledSewage`
  4. Sets `WaterConsumer.m_Pollution` from edge pollution data
  5. Sets `WaterConsumer.m_Flags` (WaterConnected/SewageConnected)
  6. Manages water/sewage shortage notifications (with cooldown timers)

### `WaterPipeBuildingGraphSystem` (Game.Simulation)

Creates pipe graph connections for buildings with direct pipe access (not road-adjacent).

- Creates WaterPipeNode entities for building connections
- Creates WaterPipeEdge entities linking building nodes to the network
- Sets edge capacity to match building's `m_WantedConsumption`

### `WaterPipeEdgeGraphSystem` (Game.Simulation)

Creates pipe graph edges when new road/pipe segments are placed.

- Creates WaterPipeEdge entities for each road segment with WaterPipeConnection
- Links them to adjacent WaterPipeNode entities

### `WaterPipeRoadConnectionGraphSystem` (Game.Simulation)

Creates pipe graph nodes for road segments.

- Creates WaterPipeNode entities for road Edge entities with WaterPipeConnection
- Adds WaterPipeNodeConnection component to link road entity to its pipe node
- Creates edges from road nodes to the sink node (for building consumption)

### `WaterPipeOutsideConnectionGraphSystem` (Game.Simulation)

Creates pipe graph connections for outside (border) connections.

- Creates nodes for entities with WaterPipeOutsideConnection
- Adds TradeNode component to enable import/export during the trade phase

### `WaterPipeGraphDeleteSystem` (Game.Simulation)

Cleans up pipe graph entities when roads/buildings are deleted.

### `WaterPipeGraphReferencesSystem` (Game.Simulation)

Updates graph references when entity indices change (e.g., after structural changes).

### `WaterPipePollutionSystem` (Game.Simulation)

Propagates pollution through the pipe network.

- **Update interval**: 64 frames
- **Two jobs**:
  1. `NodePollutionJob` -- averages incoming edge pollution at each node (weighted by flow). Stale nodes slowly purify at rate `m_StaleWaterPipePurification`.
  2. `EdgePollutionJob` -- propagates node pollution to edges (based on flow direction). Periodically allows pollution to spread (controlled by `m_WaterPipePollutionSpreadInterval`).

### `GroundWaterSystem` (Game.Simulation)

Manages the 256x256 groundwater grid.

- **Base class**: CellMapSystem<GroundWater>
- **Update interval**: 128 frames (offset 64)
- **Key constants**:
  - `kMaxGroundWater = 10000` -- max amount per cell
  - `kMinGroundWaterThreshold = 500` -- threshold for pump availability
  - `kTextureSize = 256` -- grid resolution
- **Key behavior**: Each tick, groundwater:
  1. Diffuses pollution between adjacent cells
  2. Flows between cells (from excess to deficit, rate = difference/4)
  3. Replenishes at rate `m_GroundwaterReplenish * m_Max` per tick
  4. Self-purifies at rate `m_GroundwaterPurification` per tick
- **Static methods**:
  - `GetGroundWater(float3 position, NativeArray<GroundWater>)` -- bilinear sample at position
  - `ConsumeGroundWater(float3 position, NativeArray<GroundWater>, int amount)` -- reduce groundwater at position

## Data Flow

### End-to-End: Pump Station -> Pipe Network -> Building -> Sewage -> Outlet

```
WATER PRODUCTION (Every 128 frames)
======================================
WaterPumpingStationAISystem
    |-- Reads surface water depth / groundwater at pump location
    |-- Produces water: sets WaterPipeBuildingConnection producer edge capacity
    |
    v

CONSUMPTION ADJUSTMENT (Frame 64)
======================================
AdjustWaterConsumptionSystem
    |-- For each building with WaterConsumer:
    |   1. Read ConsumptionData.m_WaterConsumption from prefab
    |   2. Multiply by water fee factor
    |   3. Scale by occupancy (renters/employees)
    |   4. Set WaterConsumer.m_WantedConsumption
    |   5. Set consumer edge m_FreshCapacity = m_SewageCapacity = wanted
    |
    v

GRAPH PREPARATION (Frame 65)
======================================
WaterPipeFlowSystem.PreparePhase
    |-- Assigns indices to all WaterPipeNode entities
    |-- Copies WaterPipeEdge capacity into Flow.Edge arrays
    |-- Builds adjacency list (Connection array)
    |-- Populates source/sink node indices
    |
    v

FLOW SOLVING (Frames 66-125)
======================================
WaterPipeFlowJob (Burst-compiled, runs each frame)
    |-- Phase 1: Producer max-flow (source -> consumers)
    |   Finds maximum flow from all producers to all consumers
    |
    |-- Phase 2: Label shortages
    |   Marks edges/nodes with WaterShortage / SewageBackup flags
    |
    |-- Phase 3: Trade max-flow (with import/export)
    |   Enables outside connection trade edges
    |   Runs max-flow again to fill remaining demand via trade
    |
    |-- Phase 4: Label connected components
    |   Marks WaterDisconnected / SewageDisconnected
    |
    |-- Phase 5: Fluid flow distribution
    |   Distributes flow proportionally across redundant paths
    |
    v

APPLY RESULTS (Frame 62)
======================================
WaterPipeFlowSystem.ApplyPhase
    |-- Writes m_FreshFlow, m_SewageFlow back to WaterPipeEdge
    |-- Sets WaterPipeEdgeFlags on each edge
    |
    v

DISPATCH TO BUILDINGS (Frame 63)
======================================
DispatchWaterSystem
    |-- For buildings with WaterPipeBuildingConnection:
    |   Read flow directly from consumer edge
    |-- For road-adjacent buildings (no direct connection):
    |   Calculate proportional share from road's edge flow/capacity
    |-- Sets WaterConsumer.m_FulfilledFresh, m_FulfilledSewage
    |-- Sets WaterConsumer.m_Pollution
    |-- Sets WaterConsumer.m_Flags (WaterConnected, SewageConnected)
    |-- Manages shortage/backup notifications
    |
    v

POLLUTION PROPAGATION (Every 64 frames)
======================================
WaterPipePollutionSystem
    |-- NodePollutionJob: averages incoming pollution at nodes
    |-- EdgePollutionJob: propagates node pollution to edges
    |-- Stale pipes slowly purify
```

### Building Connection Types

Buildings connect to the pipe network in two ways:

```
DIRECT CONNECTION (buildings with WaterPipeBuildingConnection)
=============================================================
Building
    |
    |-- m_ConsumerEdge (WaterPipeEdge)
    |   capacity = m_WantedConsumption
    |   flow = solved by max-flow
    |   Building reads flow directly from this edge
    |
    |-- m_ProducerEdge (WaterPipeEdge)  [for pump/outlet buildings only]
        capacity = production amount
        Connects to sourceNode

ROAD-ADJACENT (buildings without WaterPipeBuildingConnection)
=============================================================
Building
    |
    |-- Building.m_RoadEdge -> road entity
    |-- Road entity has WaterPipeNodeConnection -> WaterPipeNode
    |-- WaterPipeNode connects to sinkNode via WaterPipeEdge
    |-- Edge capacity = sum of all road-adjacent building consumption
    |-- Building gets proportional share: (flow / capacity) * wanted
```

## Prefab & Configuration

| Value | Source | Default | Location |
|-------|--------|---------|----------|
| Building water consumption | ConsumptionData.m_WaterConsumption | Varies by building type | Game.Prefabs.ConsumptionData |
| Road fresh pipe capacity | WaterPipeConnection.m_FreshCapacity | 1073741823 (max) | Game.Prefabs.WaterPipeConnection |
| Road sewage pipe capacity | WaterPipeConnection.m_SewageCapacity | 1073741823 (max) | Game.Prefabs.WaterPipeConnection |
| Road storm capacity | WaterPipeConnection.m_StormCapacity | 5000 | Game.Prefabs.WaterPipeConnection |
| Groundwater replenish rate | m_GroundwaterReplenish | 0.004 | WaterPipeParametersPrefab |
| Groundwater purification | m_GroundwaterPurification | 1 per tick | WaterPipeParametersPrefab |
| Groundwater usage multiplier | m_GroundwaterUsageMultiplier | 0.1 | WaterPipeParametersPrefab |
| Groundwater pump effective amount | m_GroundwaterPumpEffectiveAmount | 4000 | WaterPipeParametersPrefab |
| Surface water usage multiplier | m_SurfaceWaterUsageMultiplier | 0.00005 | WaterPipeParametersPrefab |
| Surface water pump effective depth | m_SurfaceWaterPumpEffectiveDepth | 4.0 | WaterPipeParametersPrefab |
| Max tolerated pollution | m_MaxToleratedPollution | 0.1 (10%) | WaterPipeParametersPrefab |
| Pollution spread interval | m_WaterPipePollutionSpreadInterval | 5 | WaterPipeParametersPrefab |
| Stale pipe purification | m_StaleWaterPipePurification | 0.001 | WaterPipeParametersPrefab |
| Max groundwater per cell | kMaxGroundWater | 10000 | GroundWaterSystem (hardcoded) |
| Groundwater grid size | kTextureSize | 256x256 | GroundWaterSystem (hardcoded) |

## Harmony Patch Points

### Candidate 1: `AdjustWaterConsumptionSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify building water consumption before or after the system calculates it. A Prefix could skip the original to inject custom consumption logic. A Postfix could read/modify the computed `m_WantedConsumption` values.
- **Risk level**: Low -- runs infrequently (128-frame cycle), modifies only WaterConsumer data
- **Side effects**: Changing consumption affects pipe edge capacities in the same job

### Candidate 2: `DispatchWaterSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Read or override the fulfilled water/sewage amounts after dispatch. Could implement custom water rationing, priority systems, or override shortage notifications.
- **Risk level**: Low -- read-only modifications to final dispatch results
- **Side effects**: Modifying m_FulfilledFresh/m_FulfilledSewage affects building efficiency

### Candidate 3: `WaterPipeGraphUtils.CreateFlowEdge`

- **Signature**: `Entity CreateFlowEdge(EntityCommandBuffer, EntityArchetype, Entity startNode, Entity endNode, int freshCapacity, int sewageCapacity)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept pipe edge creation to modify capacities, add custom components, or log network topology changes.
- **Risk level**: Medium -- called during graph construction, modifying capacity could break flow solver
- **Side effects**: Must maintain valid graph topology

### Candidate 4: Direct ECS manipulation (no patch needed)

- **Signature**: N/A
- **Patch type**: N/A
- **What it enables**: Read WaterConsumer data to check water status. Modify WaterPipeParameterData singleton to change global water parameters. Create custom systems that query WaterPipeEdge to analyze network flow.
- **Risk level**: Low
- **Side effects**: None if read-only; modifying WaterPipeParameterData affects all water systems

## Mod Blueprint

### Systems to create

1. **WaterNetworkMonitorSystem** (`GameSystemBase`) -- reads WaterPipeEdge entities to monitor flow, detect bottlenecks, and expose data to UI
2. **CustomWaterConsumptionSystem** (`GameSystemBase`) -- overrides building consumption based on custom rules (time of day, building type, etc.)

### Components to add

- `CustomWaterData` (IComponentData) -- stores mod-specific water metadata on buildings

### Patches needed

- **Postfix on `DispatchWaterSystem.OnUpdate`** to read final dispatch results
- **Or** no patches if using pure ECS queries to read WaterConsumer data

### Settings

- Water consumption multiplier (global)
- Per-building-type consumption overrides
- Enable/disable water shortage notifications

## Examples

### Example 1: Read a Building's Water Status

Check if a building has water and sewage service, and what its current fulfillment is.

```csharp
/// <summary>
/// Reads the water service status for a building entity.
/// </summary>
public void CheckBuildingWaterStatus(EntityManager em, Entity building)
{
    if (!em.HasComponent<WaterConsumer>(building))
    {
        Log.Info("Building has no water consumer component");
        return;
    }

    WaterConsumer consumer = em.GetComponentData<WaterConsumer>(building);

    Log.Info($"Water status for {building}:");
    Log.Info($"  Wanted: {consumer.m_WantedConsumption}");
    Log.Info($"  Fresh fulfilled: {consumer.m_FulfilledFresh}");
    Log.Info($"  Sewage fulfilled: {consumer.m_FulfilledSewage}");
    Log.Info($"  Pollution: {consumer.m_Pollution:P1}");
    Log.Info($"  Water connected: {consumer.waterConnected}");
    Log.Info($"  Sewage connected: {consumer.sewageConnected}");

    float fulfillment = (consumer.m_WantedConsumption > 0)
        ? (float)consumer.m_FulfilledFresh / consumer.m_WantedConsumption
        : 1f;
    Log.Info($"  Fulfillment ratio: {fulfillment:P0}");
}
```

### Example 2: Modify Global Water Parameters

Change the WaterPipeParameterData singleton to adjust groundwater replenishment or pollution tolerance.

```csharp
/// <summary>
/// Doubles the groundwater replenishment rate and increases pollution tolerance.
/// Call from a GameSystemBase.OnUpdate() or OnCreate().
/// </summary>
public void BoostWaterParameters(EntityManager em)
{
    EntityQuery paramQuery = em.CreateEntityQuery(
        ComponentType.ReadWrite<WaterPipeParameterData>()
    );

    if (paramQuery.CalculateEntityCount() == 0) return;

    Entity paramEntity = paramQuery.GetSingletonEntity();
    WaterPipeParameterData data = em.GetComponentData<WaterPipeParameterData>(paramEntity);

    // Double groundwater replenishment
    data.m_GroundwaterReplenish *= 2f;

    // Increase pollution tolerance from 10% to 25%
    data.m_MaxToleratedPollution = 0.25f;

    em.SetComponentData(paramEntity, data);
    paramQuery.Dispose();
}
```

### Example 3: Query All Pipe Edges with Water Shortage

Find all pipe edges that have a water shortage flag set, to identify network bottlenecks.

```csharp
/// <summary>
/// Finds all pipe edges experiencing water shortage and logs their flow details.
/// </summary>
public partial class WaterShortageMonitorSystem : GameSystemBase
{
    private EntityQuery _edgeQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _edgeQuery = GetEntityQuery(
            ComponentType.ReadOnly<WaterPipeEdge>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<WaterPipeEdge> edges = _edgeQuery.ToComponentDataArray<WaterPipeEdge>(Allocator.Temp);

        int shortageCount = 0;
        int backupCount = 0;

        for (int i = 0; i < edges.Length; i++)
        {
            WaterPipeEdge edge = edges[i];

            if ((edge.m_Flags & WaterPipeEdgeFlags.WaterShortage) != 0)
            {
                shortageCount++;
                Log.Debug($"Water shortage on edge: fresh {edge.m_FreshFlow}/{edge.m_FreshCapacity}");
            }
            if ((edge.m_Flags & WaterPipeEdgeFlags.SewageBackup) != 0)
            {
                backupCount++;
                Log.Debug($"Sewage backup on edge: sewage {edge.m_SewageFlow}/{edge.m_SewageCapacity}");
            }
        }

        if (shortageCount > 0 || backupCount > 0)
        {
            Log.Info($"Network issues: {shortageCount} water shortages, {backupCount} sewage backups");
        }

        edges.Dispose();
    }
}
```

### Example 4: Create a Custom Pipe Edge

Programmatically add a pipe edge between two existing nodes using WaterPipeGraphUtils.

```csharp
/// <summary>
/// Creates a new pipe edge with custom capacity between two existing pipe nodes.
/// </summary>
public Entity AddCustomPipeEdge(
    EntityManager em,
    WaterPipeFlowSystem flowSystem,
    Entity startNode,
    Entity endNode,
    int freshCapacity,
    int sewageCapacity)
{
    return WaterPipeGraphUtils.CreateFlowEdge(
        em,
        flowSystem.edgeArchetype,
        startNode,
        endNode,
        freshCapacity,
        sewageCapacity
    );
}
```

### Example 5: Monitor Groundwater at a Position

Sample groundwater availability at a world position to determine pump viability.

```csharp
/// <summary>
/// Checks groundwater availability at a world position.
/// Returns the amount, max capacity, and pollution ratio.
/// </summary>
public void CheckGroundwater(GroundWaterSystem gwSystem, float3 worldPosition)
{
    JobHandle deps;
    NativeArray<GroundWater> gwMap = gwSystem.GetData(readOnly: true, out deps);
    deps.Complete();

    GroundWater gw = GroundWaterSystem.GetGroundWater(worldPosition, gwMap);

    float pollutionRatio = (gw.m_Amount > 0)
        ? (float)gw.m_Polluted / gw.m_Amount
        : 0f;

    Log.Info($"Groundwater at {worldPosition}:");
    Log.Info($"  Amount: {gw.m_Amount} / {gw.m_Max}");
    Log.Info($"  Polluted: {gw.m_Polluted} ({pollutionRatio:P1})");
    Log.Info($"  Available for pump: {gw.m_Amount >= GroundWaterSystem.kMinGroundWaterThreshold}");

    gwSystem.AddReader(Dependency);
}
```

## Underground Network Sections in Roads

Roads embed underground utility pipes/cables via the `UndergroundNetSections` ComponentBase. Each section is a `NetSectionInfo` specifying the embedded utility:

| Field | Type | Description |
|-------|------|-------------|
| `m_Section` | NetSectionPrefab | Reference to utility section prefab |
| `m_Offset` | float3 | Depth offset (negative y for underground) |
| `m_RequireAll` | NetPieceRequirements | Flags that must be present |
| `m_RequireNone` | NetPieceRequirements | Flags that must be absent |

**Known section prefab names**:
- `"Sewage Pipe Section 1.5"` — offset: `(0, -1.25, 0)`
- `"Water Pipe Section 1"` — offset: `(0, -1.0, 0)`
- `"Stormwater Pipe Section 1.5"` — offset: `(0, -1.5, 0)`
- `"Pipeline Spacing Section 1"` — spacing between utilities
- `"Ground Cable Section 1"` — electrical cables

**Connection components on road prefabs**: Roads also carry `WaterPipeConnection` and `ElectricityConnection` components to enable automatic utility connections when buildings are placed adjacent to roads. This is why buildings next to roads automatically get water/power without visible pipes.

## Open Questions

- [ ] How does the flow solver handle pipe upgrades that change edge capacity mid-cycle? The solver rebuilds the graph each cycle, so capacity changes take effect next cycle.
- [ ] What happens when a building switches from road-adjacent to direct pipe connection? WaterPipeBuildingGraphSystem creates the new connection, and the old road-shared consumption is recalculated by UpdateEdgesJob.
- [ ] Can the max-flow solver fail or produce incorrect results? The job has error detection (`m_Error` flag) and will log an error if a phase gets stuck. It resets and retries next cycle.
- [ ] How does storm water capacity interact with the fresh/sewage network? The `m_StormCapacity` field exists on WaterPipeConnection but the current flow solver only processes fresh and sewage networks. Storm water may be handled by a separate system or is reserved for future use.

## Sources

- Decompiled from: Game.dll (Game.Simulation namespace, Game.Buildings namespace, Game.Net namespace, Game.Prefabs namespace)
- Key types: WaterPipeFlowSystem, WaterPipeFlowJob, WaterPipeNode, WaterPipeEdge, WaterPipeBuildingConnection, WaterPipeNodeConnection, WaterConsumer, DispatchWaterSystem, AdjustWaterConsumptionSystem, WaterPipePollutionSystem, GroundWaterSystem, WaterPipeGraphUtils, WaterPipeParameterData
- All decompiled snippets saved in `snippets/` directory
