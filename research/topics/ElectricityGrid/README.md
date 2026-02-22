# Research: Electricity Grid

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-22

## Scope

**What we're investigating**: How electricity flows through the building/road graph in CS2 -- the max-flow solver, producer/consumer/battery lifecycle, and the graph structure connecting buildings to the grid.

**Why**: Enable mods to add/remove power sources, modify electricity flow, create custom power systems, adjust consumption behavior, and build new types of generators.

**Boundaries**: Does not cover water/sewage pipes (separate system), heating, or the UI infoview rendering. Focuses on the simulation-side electricity graph.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | `Game.Simulation` | Flow system, graph systems, dispatch, trade, statistics |
| Game.dll | `Game.Buildings` | ElectricityConsumer, ElectricityProducer, Battery components |
| Game.dll | `Game.Prefabs` | PowerPlantData, BatteryData, WindPoweredData, SolarPoweredData, ElectricityParameterData |
| Game.dll | `Game.Net` | ElectricityConnection (net-level connections) |

## Component Map

### Game.Buildings.ElectricityConsumer

Attached to every building that consumes electricity.

| Field | Type | Description |
|-------|------|-------------|
| `m_WantedConsumption` | `int` | How much electricity the building wants (set by AdjustElectricityConsumptionSystem) |
| `m_FulfilledConsumption` | `int` | How much electricity was actually delivered (set by DispatchElectricitySystem) |
| `m_CooldownCounter` | `short` | Counts frames of unmet demand; triggers warnings at threshold 2 |
| `m_Flags` | `ElectricityConsumerFlags` | Connection status and warning flags |

**ElectricityConsumerFlags** (byte, flags):
- `None = 0`
- `Connected = 1` -- building is receiving electricity
- `NoElectricityWarning = 2` -- building has no electricity alert
- `BottleneckWarning = 4` -- building is behind a bottleneck

### Game.Buildings.ElectricityProducer

Attached to power plants.

| Field | Type | Description |
|-------|------|-------------|
| `m_Capacity` | `int` | Current production capacity (updated each tick based on fuel, wind, sun, etc.) |
| `m_LastProduction` | `int` | Actual flow from last solve cycle |

### Game.Buildings.Battery

Attached to battery/energy storage buildings.

| Field | Type | Description |
|-------|------|-------------|
| `m_StoredEnergy` | `long` | Current stored energy in simulation ticks |
| `m_Capacity` | `int` | Max capacity from BatteryData |
| `m_LastFlow` | `int` | Net flow last cycle (positive = charging, negative = discharging) |
| `storedEnergyHours` | `int` (property) | `m_StoredEnergy / 85` (kUpdatesPerHour) |

### Game.Simulation.ElectricityFlowNode

A node in the electricity flow graph.

| Field | Type | Description |
|-------|------|-------------|
| `m_Index` | `int` | Index into the flow solver's native arrays |

### Game.Simulation.ElectricityFlowEdge

An edge in the electricity flow graph. Carries capacity and flow results.

| Field | Type | Description |
|-------|------|-------------|
| `m_Index` | `int` | Index into the flow solver's edge array |
| `m_Start` | `Entity` | Start node entity |
| `m_End` | `Entity` | End node entity |
| `m_Capacity` | `int` | Maximum flow capacity |
| `m_Flow` | `int` | Actual flow after solving |
| `m_Flags` | `ElectricityFlowEdgeFlags` | Direction and bottleneck flags |

**ElectricityFlowEdgeFlags** (byte, flags):
- `Forward = 1`, `Backward = 2`, `ForwardBackward = 3`
- `Bottleneck = 4` -- this edge is a bottleneck
- `BeyondBottleneck = 8` -- this edge is beyond a bottleneck (downstream side)
- `Disconnected = 0x10` -- edge is disconnected from the grid

### Game.Simulation.ElectricityBuildingConnection

Links a building entity to its flow graph nodes/edges. One per connected building.

| Field | Type | Description |
|-------|------|-------------|
| `m_TransformerNode` | `Entity` | Transformer node (for transformer buildings) |
| `m_ProducerEdge` | `Entity` | Edge from global source to producer node |
| `m_ConsumerEdge` | `Entity` | Edge from consumer node to global sink |
| `m_ChargeEdge` | `Entity` | Edge for battery charging (charge node to sink) |
| `m_DischargeEdge` | `Entity` | Edge for battery discharging (source to discharge node) |

### Game.Simulation.ElectricityNodeConnection

Links a net node (road segment endpoint) to the electricity graph.

| Field | Type | Description |
|-------|------|-------------|
| `m_ElectricityNode` | `Entity` | The ElectricityFlowNode entity for this net node |

### Game.Simulation.ElectricityValveConnection

Links a net node to a valve node (sits between the marker node and the building's internal nodes).

| Field | Type | Description |
|-------|------|-------------|
| `m_ValveNode` | `Entity` | The valve node entity |

### Game.Simulation.BatteryChargeNode / BatteryDischargeNode

Tag components (empty structs) placed on flow nodes to identify them as charge or discharge nodes. Used by the flow solver to route excess/deficit through batteries.

## System Map

### ElectricityFlowSystem (Game.Simulation)

**The core system.** Runs a max-flow solver over the electricity graph every 128 frames (one simulation cycle).

- **Update interval**: 128 frames
- **Phase cycle** within each 128-frame window:
  - Frame 0: `AdjustElectricityConsumptionSystem` runs (offset 0)
  - Frame 1: **Prepare** -- builds solver arrays from ECS data, assigns indices
  - Frames 2-125: **Flow** -- runs `ElectricityFlowJob` iteratively (max-flow with layered labeling)
  - Frame 125: Final flow frame
  - Frame 126: **Apply** -- writes flow results back to `ElectricityFlowEdge` components, sets bottleneck flags
  - Frame 127: `ElectricityStatusSystem` runs notifications
- **Constants**:
  - `kUpdatesPerDay = 2048`
  - `kUpdatesPerHour = 85`
  - `kMaxEdgeCapacity = 1073741823`
- **Global nodes**: `sourceNode` (global source) and `sinkNode` (global sink). All producers connect from source, all consumers connect to sink.

### PowerPlantAISystem (Game.Simulation)

Updates production capacity for all power plants each 128-frame cycle (offset 0).

- Reads prefab data (PowerPlantData, WindPoweredData, SolarPoweredData, WaterPoweredData, GroundWaterPoweredData, GarbagePoweredData)
- Calculates capacity based on:
  - **Base plants**: `efficiency * m_ElectricityProduction` (requires resource availability > 0)
  - **Wind**: `efficiency * m_Production * saturate(pow(windSpeed^2 / maxWind^2, 1.5))`
  - **Solar**: `efficiency * m_Production * sunLight` (sunLight reduced by cloudiness penalty)
  - **Water**: Flow rate * height difference * productionFactor, capped by capacity
  - **Ground water**: `efficiency * m_Production * (groundWaterAmount / m_MaximumGroundWater)`
  - **Garbage**: `clamp(processingRate / productionPerUnit, 0, capacity)`
- Writes resulting capacity to both `ElectricityProducer.m_Capacity` and the producer edge's `m_Capacity`

### BatteryAISystem (Game.Simulation)

Manages battery charge/discharge each 128-frame cycle.

- Calculates net flow: `chargeEdge.m_Flow - dischargeEdge.m_Flow`
- Clamps stored energy to `[0, capacityTicks]` (where `capacityTicks = 85 * m_Capacity`)
- Sets discharge edge capacity: `min(m_PowerOutput, m_StoredEnergy)` (can only discharge what's stored)
- Sets charge edge capacity: `min(efficiency * m_PowerOutput, capacityTicks - m_StoredEnergy)` (only charge up to max)
- Shows "battery empty" notification when stored energy reaches 0
- Handles emergency generators (upgrade components that kick in when charge drops below threshold)

### AdjustElectricityConsumptionSystem (Game.Simulation)

Calculates wanted consumption for every consumer building (128 updates/day across 16 update frames).

- Base consumption from `ConsumptionData.m_ElectricityConsumption`
- Multiplied by:
  - **Temperature**: `ElectricityParameterData.m_TemperatureConsumptionMultiplier` curve
  - **Service fee**: Higher fees reduce consumption
  - **District modifiers**: `DistrictModifierType.EnergyConsumptionAwareness`
  - **Occupancy**: Scales by renter count (households/employees)
- Inactive buildings consume 1/10 normal
- Updates the consumer edge capacity to match wanted consumption

### DispatchElectricitySystem (Game.Simulation)

Distributes flow results to consumers (runs at offset 126, after the flow solve completes).

- Reads flow from the consumer edge and sets `m_FulfilledConsumption = min(flow, wantedConsumption)`
- For buildings without direct graph connections (low-voltage via road), proportionally distributes based on road edge utilization
- Manages cooldown counter: increments when demand unmet, resets when met
- After 2 frames of unmet demand, triggers warning notifications (NoElectricity or Bottleneck)
- Sets `ElectricityConsumerFlags.Connected` based on fulfillment
- Updates building `Efficiency` factor for electricity supply

### ElectricityBuildingGraphSystem (Game.Simulation)

Creates/updates the flow graph nodes for buildings when they are created or modified.

- Triggered by `Created` or `Updated` tags on buildings with electricity components
- For each building, creates up to 5 internal flow nodes:
  - **TransformerNode** (if building is a transformer)
  - **ProducerNode** + edge from global source (if producer)
  - **ConsumerNode** + edge to global sink (if consumer)
  - **ChargeNode** + edge to sink (if battery)
  - **DischargeNode** + edge from source (if battery)
- Finds marker nodes in building sub-nets (ElectricityConnection prefabs)
- Creates valve nodes between marker nodes and building nodes
- Connects marker nodes to the road network edge graph

### ElectricityRoadConnectionGraphSystem (Game.Simulation)

Creates flow nodes/edges for road segments that carry electricity (low-voltage distribution).

### ElectricityEdgeGraphSystem (Game.Simulation)

Creates flow edges between adjacent electricity nodes along net edges (power lines, roads).

### ElectricityGraphDeleteSystem (Game.Simulation)

Cleans up flow nodes and edges when buildings/roads are deleted.

### ElectricityStatusSystem (Game.Simulation)

Generates notification icons for disconnected/bottleneck edges and buildings (frame 127).

### ElectricityTradeSystem (Game.Simulation)

Handles electricity import/export through outside connections.

### ElectricityStatisticsSystem (Game.Simulation)

Counts total production, consumption, and battery capacity for UI statistics.

## Data Flow

```
                         ELECTRICITY FLOW GRAPH
                         ======================

  SourceNode (global)                          SinkNode (global)
      |                                              ^
      |  ProducerEdge (capacity = plant output)      |  ConsumerEdge (capacity = wanted)
      v                                              |
  [ProducerNode] -----> [ValveNode] -----> [ConsumerNode]
                            |
                   MarkerNode (building subnet)
                            |
                    ElectricityNodeConnection
                            |
                    [Road/PowerLine EdgeNode]
                            |
                   (connects to other buildings)

  BATTERY SUBGRAPH:
  SourceNode --DischargeEdge--> [DischargeNode] --> [ValveNode]
  [ValveNode] --> [ChargeNode] --ChargeEdge--> SinkNode

  128-FRAME CYCLE:
  Frame 0:    AdjustConsumption -- set wanted consumption on edges
  Frame 0:    PowerPlantAI -- set production capacity on edges
  Frame 0:    BatteryAI -- set charge/discharge capacity on edges
  Frame 1:    Prepare -- build solver arrays
  Frame 2-125: Flow -- max-flow solver iterates
  Frame 126:  Apply -- write results to edges; Dispatch -- distribute to consumers
  Frame 127:  Status -- update notifications
```

## Prefab & Configuration

### PowerPlantData
- `m_ElectricityProduction`: Base production in electricity units
- **Runtime-confirmed (prefab dump)**: Nuclear=7,500,000, Coal=3,000,000, Gas=2,500,000. Dynamic sources (hydro/solar/wind) all have `m_ElectricityProduction = 0` in the prefab — their capacity is computed entirely at runtime by `PowerPlantAISystem` from wind speed, sun angle, and water flow. Never read `m_ElectricityProduction` from a prefab to estimate a dynamic plant's output; use `ElectricityProducer.m_Capacity` instead.

### BatteryData
- `m_Capacity`: Energy storage in electricity-hours
- `m_PowerOutput`: Max charge/discharge rate per tick
- `capacityTicks = 85 * m_Capacity`: Internal storage in simulation ticks

### WindPoweredData
- `m_MaximumWind`: Wind speed for 100% production
- `m_Production`: Max production at max wind

### SolarPoweredData
- `m_Production`: Max production at full sunlight

### ElectricityParameterData (singleton)
- `m_TemperatureConsumptionMultiplier`: AnimationCurve mapping temperature to consumption multiplier
- `m_CloudinessSolarPenalty`: Solar penalty from cloud cover (0-1)
- `m_InitialBatteryCharge`: Starting charge fraction for new batteries
- Various notification prefab references

### ElectricityConnectionData
- `m_Capacity`: Edge capacity for the connection
- `m_Direction`: FlowDirection (Forward, Backward, Both, None)
- `m_Voltage`: Low (0) or High (1)

### ConsumptionData (Game.Prefabs)

Per-building-prefab component that defines base consumption for electricity and water. This is the starting point for `AdjustElectricityConsumptionSystem` and `AdjustWaterConsumptionSystem` calculations.

| Field | Type | Description |
|-------|------|-------------|
| `m_ElectricityConsumption` | int | Base electricity consumption for this building type. Multiplied by temperature, fee, district, and occupancy modifiers in `AdjustElectricityConsumptionSystem`. |
| `m_WaterConsumption` | int | Base water consumption for this building type. Used by `AdjustWaterConsumptionSystem` with similar multiplier chain. |

Both fields represent the building's consumption at full occupancy under neutral conditions. The actual `WantedConsumption` written to `ElectricityConsumer.m_WantedConsumption` (or the water equivalent) is this base value multiplied by several factors. Community mods modify `ConsumptionData` on building prefabs at runtime to adjust per-building-type electricity and water demands. This is particularly useful for mods that want building consumption to reflect realistic values based on building size (see the Building Mesh Dimensions pattern in Zoning research).

*Source: `Game.dll` -> `Game.Prefabs.ConsumptionData`*

## Harmony Patch Points

### PowerPlantAISystem.PowerPlantTickJob.Execute
- **Purpose**: Modify production calculations
- **Use case**: Custom renewable types, production multipliers, weather effects
- **Risk**: Burst-compiled job -- must patch the system's `OnUpdate` instead

### PowerPlantAISystem.OnUpdate
- **Purpose**: Intercept before/after plant capacity updates
- **Use case**: Override sun/wind values, inject custom production logic

### BatteryAISystem.OnUpdate
- **Purpose**: Modify battery charge/discharge behavior
- **Use case**: Custom battery efficiency curves, degradation mechanics

### AdjustElectricityConsumptionSystem.OnUpdate
- **Purpose**: Modify consumption calculations
- **Use case**: Time-of-day pricing, custom efficiency modifiers

### DispatchElectricitySystem.OnUpdate
- **Purpose**: Intercept electricity distribution
- **Use case**: Priority consumers, blackout mechanics

### ElectricityBuildingGraphSystem.OnUpdate
- **Purpose**: Modify how buildings connect to the electricity graph
- **Use case**: Custom connection types, wireless power

### ElectricityFlowSystem.OnUpdate
- **Purpose**: Intercept flow solve phases
- **Use case**: Custom flow algorithms (risky -- complex)

### ElectricityGraphUtils.CreateFlowEdge (static)
- **Purpose**: Intercept graph edge creation
- **Use case**: Custom capacity rules, logging

## Mod Blueprint

### Adding a Custom Power Source

1. Create a building prefab with `ElectricityProducer` and `PowerPlantData` components
2. The `ElectricityBuildingGraphSystem` will automatically create graph nodes/edges
3. The `PowerPlantAISystem` updates capacity each tick based on prefab data
4. To add custom renewable logic, patch `PowerPlantAISystem.OnUpdate`

### Modifying Consumption

1. Patch `AdjustElectricityConsumptionSystem.OnUpdate` to modify the temperature/fee multipliers
2. Or modify `ConsumptionData` on building prefabs to change base consumption
3. District modifiers (`DistrictModifierType.EnergyConsumptionAwareness`) already support consumption reduction

### Custom Battery Behavior

1. Patch `BatteryAISystem.OnUpdate` to modify charge/discharge rates
2. Access `BatteryData` on the prefab to change capacity/output
3. Emergency generators are handled via `EmergencyGeneratorData` upgrade components

### Reading Grid State

1. Query `ElectricityConsumer` to check building power status
2. Query `ElectricityProducer` to read production values
3. Query `ElectricityFlowEdge` to read flow/capacity on graph edges
4. Use `ElectricityBuildingConnection` to navigate from building to its graph nodes

## Examples

### Example 1: Query All Unpowered Buildings

```csharp
// In a GameSystemBase.OnUpdate:
EntityQuery query = GetEntityQuery(
    ComponentType.ReadOnly<ElectricityConsumer>(),
    ComponentType.ReadOnly<Building>(),
    ComponentType.Exclude<Deleted>(),
    ComponentType.Exclude<Temp>()
);

NativeArray<ElectricityConsumer> consumers =
    query.ToComponentDataArray<ElectricityConsumer>(Allocator.TempJob);
NativeArray<Entity> entities =
    query.ToEntityArray(Allocator.TempJob);

for (int i = 0; i < consumers.Length; i++)
{
    if (!consumers[i].electricityConnected &&
        consumers[i].m_WantedConsumption > 0)
    {
        Log.Info($"Building {entities[i]} has no power! " +
            $"Wants: {consumers[i].m_WantedConsumption}, " +
            $"Gets: {consumers[i].m_FulfilledConsumption}");
    }
}

consumers.Dispose();
entities.Dispose();
```

### Example 2: Read Total Production and Consumption

```csharp
// Query all producers
EntityQuery producerQuery = GetEntityQuery(
    ComponentType.ReadOnly<ElectricityProducer>(),
    ComponentType.Exclude<Deleted>(),
    ComponentType.Exclude<Temp>()
);

NativeArray<ElectricityProducer> producers =
    producerQuery.ToComponentDataArray<ElectricityProducer>(Allocator.TempJob);

int totalCapacity = 0;
int totalProduction = 0;
for (int i = 0; i < producers.Length; i++)
{
    totalCapacity += producers[i].m_Capacity;
    totalProduction += producers[i].m_LastProduction;
}
producers.Dispose();

Log.Info($"Grid: {totalProduction}/{totalCapacity} capacity utilized");
```

### Example 3: Modify a Building's Electricity Demand via Harmony

```csharp
[HarmonyPatch(typeof(AdjustElectricityConsumptionSystem), "OnUpdate")]
public static class AdjustConsumptionPatch
{
    // Prefix to modify the temperature multiplier
    public static void Prefix(AdjustElectricityConsumptionSystem __instance)
    {
        // Access private fields via reflection or Traverse
        // to modify consumption behavior
    }
}
```

### Example 4: Check Battery Charge Levels

```csharp
EntityQuery batteryQuery = GetEntityQuery(
    ComponentType.ReadOnly<Battery>(),
    ComponentType.ReadOnly<PrefabRef>(),
    ComponentType.Exclude<Deleted>(),
    ComponentType.Exclude<Temp>()
);

NativeArray<Battery> batteries =
    batteryQuery.ToComponentDataArray<Battery>(Allocator.TempJob);

for (int i = 0; i < batteries.Length; i++)
{
    float chargePercent = (float)batteries[i].m_StoredEnergy /
        (85L * batteries[i].m_Capacity) * 100f;
    Log.Info($"Battery {i}: {chargePercent:F1}% charged, " +
        $"flow: {batteries[i].m_LastFlow}");
}
batteries.Dispose();
```

### Example 5: Create a Flow Edge Programmatically

```csharp
// Using ElectricityGraphUtils to add a flow edge between two nodes
ElectricityFlowSystem flowSystem =
    World.GetOrCreateSystemManaged<ElectricityFlowSystem>();

Entity newEdge = ElectricityGraphUtils.CreateFlowEdge(
    EntityManager,
    flowSystem.edgeArchetype,
    startNode,           // Entity: start flow node
    endNode,             // Entity: end flow node
    FlowDirection.Both,  // Allow flow in both directions
    1000                 // Capacity
);
```

## Open Questions

1. **Flow solver internals**: The `ElectricityFlowJob` uses a max-flow algorithm with layered labeling and `MaxFlowSolverState`. The exact algorithm (likely Dinic's or push-relabel variant) is inside `Game.Simulation.Flow` which was not fully decompiled. How does it handle convergence in 124 frames?

2. **Trade node mechanics**: `ElectricityTradeSystem` handles import/export via `TradeNode` components on outside connections. The pricing/capacity model for trades needs further investigation.

3. **Valve node purpose**: The `ElectricityValveConnection` creates an intermediate valve node between the marker node and building nodes. Is this purely for capacity limiting, or does it serve as a disconnection point?

4. **Low-voltage vs high-voltage**: `ElectricityConnection.Voltage` distinguishes Low and High voltage. How does this affect the graph topology? Buildings without direct `ElectricityBuildingConnection` get power proportionally from their road edge -- is this the low-voltage path?

5. **Emergency generator activation**: `EmergencyGeneratorData` has an `m_ActivationThreshold` (Bounds1). When battery charge drops below `min`, generators activate; they stay on until charge exceeds `max`. What are typical threshold values?

## Sources

- Decompiled from: Game.dll using ilspycmd v9.1
- Key types decompiled: ElectricityConsumer, ElectricityProducer, Battery, ElectricityFlowEdge, ElectricityFlowNode, ElectricityBuildingConnection, ElectricityNodeConnection, ElectricityValveConnection, ElectricityFlowSystem, PowerPlantAISystem, BatteryAISystem, AdjustElectricityConsumptionSystem, DispatchElectricitySystem, ElectricityBuildingGraphSystem, ElectricityGraphUtils, PowerPlantData, BatteryData, WindPoweredData, SolarPoweredData, ElectricityParameterData, ElectricityConnectionData, ElectricityConnection
