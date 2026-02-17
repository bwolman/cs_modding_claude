# Research: Trade & Outside Connections

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How resources, electricity, and water are traded between a city and the outside world, including the trade cost calculation pipeline, outside connection entity model, and utility trade systems.

**Why**: Mods that adjust trade costs, modify outside connections, control import/export behavior, or rebalance utility prices need to understand how TradeSystem calculates costs, how ResourceExporterSystem dispatches goods, and how ElectricityTradeSystem/WaterTradeSystem handle utility trade.

**Boundaries**: Company internal production and consumption are documented in [Resource Production](../ResourceProduction/) and [Company Simulation](../CompanySimulation/). The budget integration is summarized in [Economy & Budget](../EconomyBudget/). This topic focuses on the trade pipeline itself: cost calculation, outside connection entities, and the import/export flow.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Companies | TradeCost, CurrentTrading, ResourceExporter |
| Game.dll | Game.Objects | OutsideConnection (tag), ElectricityOutsideConnection (tag), WaterPipeOutsideConnection (tag) |
| Game.dll | Game.Net | OutsideConnection (has m_Delay field), OutsideConnectionSystem |
| Game.dll | Game.Simulation | TradeSystem, ResourceExporterSystem, ElectricityTradeSystem, WaterTradeSystem, OutsideConnectionDelaySystem, TradeNode |
| Game.dll | Game.Prefabs | OutsideConnectionData, OutsideTradeParameterData, OutsideConnectionTransferType, OutsideConnection (prefab), OutsideTradeParameterPrefab |
| Game.dll | Game.City | ServiceImportBudget |
| Game.dll | Game.UI.InGame | TradedResourcesSection |

## Component Map

### `TradeCost` (Game.Companies)

Buffer element attached to companies and outside connection storage entities. Tracks the smoothed cost of buying/selling each resource.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | Which resource this cost entry applies to |
| m_BuyCost | float | Smoothed cost to buy (import) this resource |
| m_SellCost | float | Smoothed cost to sell (export) this resource |
| m_LastTransferRequestTime | long | Frame index of last transfer request |

*Source: `Game.dll` -> `Game.Companies.TradeCost`*

### `CurrentTrading` (Game.Companies)

Buffer element tracking in-flight trade operations. Added when a company begins an export and removed when the delivery completes.

| Field | Type | Description |
|-------|------|-------------|
| m_TradingResourceAmount | int | Amount being traded (negative = exporting) |
| m_TradingResource | Resource | Which resource is being traded |
| m_TradingStartFrameIndex | uint | Frame when this trade started |
| m_OutsideConnectionType | OutsideConnectionTransferType | Transport mode used (Road, Train, Air, Ship) |

*Source: `Game.dll` -> `Game.Companies.CurrentTrading`*

### `ResourceExporter` (Game.Companies)

Singleton component on a company entity that triggers the export pathfinding pipeline. Added when a company has surplus resources to export, removed after the export is dispatched.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | Resource type to export |
| m_Amount | int | Amount to export |

*Source: `Game.dll` -> `Game.Companies.ResourceExporter`*

### `OutsideConnection` (Game.Objects)

Empty tag component (`IEmptySerializable`) marking an entity as an outside connection object. Applied to the map-edge connection points (road, rail, air, ship).

*Source: `Game.dll` -> `Game.Objects.OutsideConnection`*

### `OutsideConnection` (Game.Net)

Network-level component on connection lane entities. Stores the traffic-based delay at this outside connection.

| Field | Type | Description |
|-------|------|-------------|
| m_Delay | float | Traffic delay at this connection (seconds), calculated from vehicle queue lengths |

*Source: `Game.dll` -> `Game.Net.OutsideConnection`*

### `ElectricityOutsideConnection` (Game.Objects)

Empty tag component marking an entity as an electricity outside connection. The prefab adds both this and `Game.Objects.OutsideConnection`.

*Source: `Game.dll` -> `Game.Objects.ElectricityOutsideConnection`*

### `WaterPipeOutsideConnection` (Game.Objects)

Empty tag component marking an entity as a water pipe outside connection. The prefab adds both this and `Game.Objects.OutsideConnection`.

*Source: `Game.dll` -> `Game.Objects.WaterPipeOutsideConnection`*

### `TradeNode` (Game.Simulation)

Empty tag component on flow graph nodes that represent outside connections in the electricity/water flow solvers.

*Source: `Game.dll` -> `Game.Simulation.TradeNode`*

### `OutsideConnectionData` (Game.Prefabs)

Prefab component on outside connection prefabs. Defines the transport type and remoteness.

| Field | Type | Description |
|-------|------|-------------|
| m_Type | OutsideConnectionTransferType | Transport modes this connection supports (flags: Road, Train, Air, Ship) |
| m_Remoteness | float | Distance factor affecting pathfinding cost between connections |

*Source: `Game.dll` -> `Game.Prefabs.OutsideConnectionData`*

### `OutsideConnectionTransferType` (Game.Prefabs)

Flags enum for transport modes.

| Value | Hex | Description |
|-------|-----|-------------|
| None | 0x00 | No connection |
| Road | 0x01 | Highway connection |
| Train | 0x02 | Rail connection |
| Air | 0x04 | Airport connection |
| Ship | 0x10 | Harbor connection |
| Last | 0x20 | Sentinel (iteration limit) |
| All | 0x17 | Road \| Train \| Air \| Ship |

*Source: `Game.dll` -> `Game.Prefabs.OutsideConnectionTransferType`*

### `OutsideTradeParameterData` (Game.Prefabs)

Global singleton controlling all trade pricing and transport cost multipliers.

| Field | Type | Description |
|-------|------|-------------|
| m_ElectricityImportPrice | float | Cost per 0.1 kW imported for 24h |
| m_ElectricityExportPrice | float | Revenue per 0.1 kW exported for 24h |
| m_WaterImportPrice | float | Cost per 1 m^3 water imported for 24h |
| m_WaterExportPrice | float | Revenue per 1 m^3 water exported for 24h |
| m_WaterExportPollutionTolerance | float | Pollution % at which water export revenue = 0 (default 0.1) |
| m_SewageExportPrice | float | Cost per 1 m^3 sewage exported for 24h |
| m_AirWeightMultiplier | float | Base cost per unit weight via air |
| m_RoadWeightMultiplier | float | Base cost per unit weight via road |
| m_TrainWeightMultiplier | float | Base cost per unit weight via train |
| m_ShipWeightMultiplier | float | Base cost per unit weight via ship |
| m_AirDistanceMultiplier | float | Distance scaling factor for air trade |
| m_RoadDistanceMultiplier | float | Distance scaling factor for road trade |
| m_TrainDistanceMultiplier | float | Distance scaling factor for train trade |
| m_ShipDistanceMultiplier | float | Distance scaling factor for ship trade |
| m_AmbulanceImportServiceFee | float | Service fee per population unit for ambulance imports |
| m_HearseImportServiceFee | float | Service fee per population unit for hearse imports |
| m_FireEngineImportServiceFee | float | Service fee per population unit for fire engine imports |
| m_GarbageImportServiceFee | float | Service fee per population unit for garbage imports |
| m_PoliceImportServiceFee | float | Service fee per population unit for police imports |
| m_OCServiceTradePopulationRange | int | Population bucket size for service fee scaling (default 1000) |

Key methods:
- `GetDistanceCostSingle(type)` -- returns distance multiplier for a single transport type
- `GetBestDistanceCostAmongTypes(types)` -- returns minimum distance cost across all flagged types
- `GetWeightCostSingle(type)` -- returns weight multiplier for a single transport type
- `GetBestWeightCostAmongTypes(types)` -- returns minimum weight cost across all flagged types
- `GetFee(resource, export)` -- returns import/export price for utility resources
- `Importable(resource)` / `Exportable(resource)` -- checks if utility trade is configured

*Source: `Game.dll` -> `Game.Prefabs.OutsideTradeParameterData`*

### `ServiceImportBudget` (Game.City)

Buffer element on the city entity controlling the maximum budget for importing city services.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | PlayerResource | Which service (Electricity, Water, Sewage) |
| m_MaximumBudget | int | Maximum amount the city will spend on importing this service |

*Source: `Game.dll` -> `Game.City.ServiceImportBudget`*

## System Map

### `TradeSystem` (Game.Simulation)

The core resource trade system. Manages trade balances, calculates trade costs per resource per transport type, and executes resource transfers for outside connection storage companies.

- **Base class**: GameSystemBase (also implements ITradeSystem, IDefaultSerializable, ISerializable)
- **Update phase**: Simulation
- **Update interval**: `262144 / 128 = 2048` frames (128 updates per game day)
- **Queries**:
  - `m_StorageGroup`: entities with StorageCompany + OutsideConnection + PrefabRef + Resources + TradeCost (excluding Deleted, Temp)
  - `m_TradeParameterQuery`: entities with OutsideTradeParameterData
  - `m_CityQuery`: entities with City
- **Reads**: ResourceData.m_Weight, StorageCompanyData.m_StoredResources, StorageLimitData.m_Limit, OutsideConnectionData.m_Type, CityModifier buffer
- **Writes**: Resources buffer, TradeCost buffer, trade balance array, statistics
- **Key constants**:
  - `kRefreshRate = 0.01f` -- trade balance decay per update (1%)
  - `kUpdatesPerDay = 128` -- updates per game day
- **Key methods**:
  - `OnUpdate()` -- schedules TradeJob
  - `CalculateTradeCost()` -- computes buy/sell cost from trade balance, weight, and distance multipliers
  - `GetBestTradePriceAmongTypes()` -- public API for querying cached costs with city modifier application

**Trade Cost Formula**:
```
BuyCost = WeightMultiplier * ResourceWeight * (1 + DistanceMultiplier * max(50, sqrt(-tradeBalance)))  [if importing]
SellCost = WeightMultiplier * ResourceWeight * (1 + DistanceMultiplier * max(50, sqrt(tradeBalance)))  [if exporting]
```
CityModifiers (ImportCost/ExportCost) are applied on top.

**TradeJob Logic**:
1. Decays each resource's trade balance by `kRefreshRate` (1%)
2. Recalculates cached costs for every (resource, transport type) pair
3. Iterates all outside connection storage companies
4. For each stored resource, computes the deficit from half the storage limit
5. Adds/removes resources proportionally (faster fill when further from equilibrium)
6. Updates trade balance and records statistics

### `ResourceExporterSystem` (Game.Simulation)

Handles the pathfinding and dispatch of export shipments from companies to outside connections.

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**:
  - `m_ExporterQuery`: entities with ResourceExporter + TaxPayer + PropertyRenter + Resources + TripNeeded (or ResourceProducer + CityServiceUpkeep variant), excluding ResourceBuyer/Deleted/Temp
  - `m_OutsideConnectionQuery`: StorageCompany + OutsideConnection entities
- **Two-phase pipeline**:
  1. **ExportJob** (IJobChunk, parallel): For each exporter entity:
     - If already has an Exporting trip queued, remove ResourceExporter (done)
     - If resource is weightless, immediately enqueue export event (no physical delivery needed)
     - If PathInformation is resolved, create ExportEvent and add CurrentTrading buffer + TripNeeded
     - If no path yet, initiate pathfinding to SetupTargetType.ResourceExport
  2. **HandleExportsJob** (IJob, sequential): Processes ExportEvent queue:
     - Calculates transport cost from distance
     - Updates TradeCost on both buyer and seller with `lerp(old, new, 0.5)` smoothing
     - Deducts transport cost from revenue
     - For weightless resources, picks random outside connection
     - Removes exported resources from seller

### `ElectricityTradeSystem` (Game.Simulation)

Measures electricity flow through outside connection trade nodes and generates fee events.

- **Base class**: GameSystemBase (also implements IDefaultSerializable, ISerializable)
- **Update interval**: 128 frames (offset 126 -- runs near end of electricity flow cycle)
- **Queries**:
  - `m_TradeNodeGroup`: entities with TradeNode + ElectricityFlowNode + ConnectedFlowEdge
- **Two-phase pipeline**:
  1. **SumJob** (IJobChunk): For each trade node's connected edges:
     - Edges ending at sinkNode = export flow
     - Edges starting at sourceNode = import flow
  2. **ElectricityTradeJob** (IJob): Converts flow to monetary amounts:
     - `exportRevenue = (exportFlow / 2048) * m_ElectricityExportPrice`
     - `importCost = (importFlow / 2048) * m_ElectricityImportPrice`
     - Enqueues ServiceFeeSystem.FeeEvent with `m_Outside = true`
- **Public properties**: `export`, `import` (last cycle's values)

### `WaterTradeSystem` (Game.Simulation)

Measures water/sewage flow through outside connection trade nodes and generates fee events.

- **Base class**: GameSystemBase (also implements IDefaultSerializable, ISerializable)
- **Update interval**: 128 frames (offset 62 -- runs at end of water pipe flow cycle)
- **Queries**:
  - `m_TradeNodeGroup`: entities with TradeNode + WaterPipeNode + ConnectedFlowEdge
- **Key difference from electricity**: Tracks four quantities:
  - Fresh water export (limited by available water capacity)
  - Polluted water export (reduces revenue proportionally)
  - Fresh water import
  - Sewage export
- **Pollution penalty**: `pollutedExport = min(round(pollution/tolerance * flow), flow)`. Revenue = `(freshExport - pollutedExport) * exportPrice`
- **Public properties**: `freshExport`, `freshImport`, `sewageExport`

### `OutsideConnectionDelaySystem` (Game.Simulation)

Calculates traffic congestion delays at outside connection nodes based on queued vehicle wait times.

- **Base class**: GameSystemBase
- **Update interval**: 4096 frames (64 updates per day)
- **Queries**:
  - `m_NodeQuery`: entities with Node + Game.Net.OutsideConnection, excluding Deleted/Temp
- **Logic**:
  - For each outside connection node, examines its connection lanes (road car start lanes)
  - Walks connected edges and counts vehicles with `ResetSpeed` flag (stopped/slow)
  - Sums up vehicle `m_Duration` values as delay
  - Writes average delay to `Game.Net.OutsideConnection.m_Delay`
  - Enqueues TimeActionData to update pathfinding costs: `time = borderCost + delay`

### `OutsideConnectionSystem` (Game.Net)

Creates and maintains the pathfinding connection lanes at outside connection nodes. Event-driven -- only runs when connections are updated or deleted.

- **Base class**: GameSystemBase
- **Queries**: Updated/Deleted outside connections (excludes electricity and water pipe connections)
- **Logic**: Builds connection lane entities sorted by angular position around the map edge, creating:
  - Start lanes (entry points into the city)
  - Distance lanes (connections between outside connection nodes along the map border)
  - Parking lanes (road-to-pedestrian transfers)

## Data Flow

```
RESOURCE TRADE PIPELINE
========================

[Company has surplus resources]
      |
      v
ResourceBuyerSystem / CompanySystem
  Adds ResourceExporter component to company
      |
      v
ResourceExporterSystem (every 16 frames)
  ExportJob:
    No path? --> Pathfind to SetupTargetType.ResourceExport
    Path found? --> Create ExportEvent + CurrentTrading + TripNeeded
      |
      v
  HandleExportsJob:
    Calculate transport cost from distance
    Smooth TradeCost on buyer/seller (lerp 0.5)
    Deduct resources from seller
    Add resources to buyer (outside connection storage)
      |
      v
TradeSystem (128x/day)
  TradeJob:
    Decay trade balances (1% per update)
    Recalculate cached costs per (resource, transport type)
    For each OC storage: rebalance toward half capacity
    Record import/export statistics
      |
      v
[Resources appear at / disappear from outside connections]


UTILITY TRADE PIPELINE
========================

[Electricity / Water flow solvers run on 128-frame cycle]
      |
      v
ElectricityFlowSystem / WaterPipeFlowSystem
  Trade nodes connect to source/sink in flow graph
  Flow solver determines import/export quantities
      |
      v
ElectricityTradeSystem (frame 126) / WaterTradeSystem (frame 62)
  SumJob: Count flow through trade node edges
  TradeJob: Convert flow to money
    import cost = flow/2048 * importPrice
    export revenue = flow/2048 * exportPrice
  Enqueue FeeEvent to ServiceFeeSystem
      |
      v
ServiceFeeSystem --> BudgetSystem
  Records utility trade as income/expense


OUTSIDE CONNECTION DELAY
========================

OutsideConnectionDelaySystem (64x/day)
  Count queued vehicles at OC nodes
  Sum wait durations --> average delay
  Write to Game.Net.OutsideConnection.m_Delay
  Update pathfinding border cost += delay
      |
      v
PathfindSystem
  Higher delay --> vehicles prefer less congested connections
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Transport type | OutsideConnectionData.m_Type | Game.Prefabs |
| Remoteness | OutsideConnectionData.m_Remoteness | Game.Prefabs |
| Electricity import price | OutsideTradeParameterData.m_ElectricityImportPrice | Game.Prefabs (singleton) |
| Electricity export price | OutsideTradeParameterData.m_ElectricityExportPrice | Game.Prefabs (singleton) |
| Water import price | OutsideTradeParameterData.m_WaterImportPrice | Game.Prefabs (singleton) |
| Water export price | OutsideTradeParameterData.m_WaterExportPrice | Game.Prefabs (singleton) |
| Water pollution tolerance | OutsideTradeParameterData.m_WaterExportPollutionTolerance | Game.Prefabs (singleton, default 0.1) |
| Sewage export price | OutsideTradeParameterData.m_SewageExportPrice | Game.Prefabs (singleton) |
| Air/Road/Train/Ship weight multipliers | OutsideTradeParameterData.m_*WeightMultiplier | Game.Prefabs (singleton) |
| Air/Road/Train/Ship distance multipliers | OutsideTradeParameterData.m_*DistanceMultiplier | Game.Prefabs (singleton) |
| Service import fees | OutsideTradeParameterData.m_*ImportServiceFee | Game.Prefabs (singleton) |
| Population range for service fees | OutsideTradeParameterData.m_OCServiceTradePopulationRange | Game.Prefabs (singleton, default 1000) |
| Trade balance decay | TradeSystem.kRefreshRate | Hardcoded 0.01f |
| Trade updates per day | TradeSystem.kUpdatesPerDay | Hardcoded 128 |
| Traded resources | OutsideConnection.m_TradedResources (prefab) | Game.Prefabs |
| Max transports | TransportCompanyData.m_MaxTransports | Set to int.MaxValue by OC prefab |
| Service budget cap | ServiceImportBudget.m_MaximumBudget | Game.City (per-resource buffer on city entity) |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.TradeSystem.CalculateTradeCost`

- **Signature**: `static TradeCost CalculateTradeCost(Resource resource, int tradeBalance, OutsideConnectionTransferType type, float weight, ref OutsideTradeParameterData tradeParameters, DynamicBuffer<CityModifier> cityEffects)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override trade cost calculation to rebalance costs, add surcharges, or make certain resources cheaper/more expensive
- **Risk level**: Low -- pure calculation, no side effects
- **Side effects**: Changes all resource costs globally

### Candidate 2: `Game.Simulation.TradeSystem.GetBestTradePriceAmongTypes`

- **Signature**: `float GetBestTradePriceAmongTypes(Resource resource, OutsideConnectionTransferType types, bool import, DynamicBuffer<CityModifier> cityEffects)`
- **Patch type**: Postfix
- **What it enables**: Adjust final trade prices after city modifiers are applied
- **Risk level**: Low -- read-only query method
- **Side effects**: Affects all consumers of trade price data

### Candidate 3: `Game.Simulation.ResourceExporterSystem.ExportJob.FindTarget`

- **Signature**: `void FindTarget(int chunkIndex, Entity exporter, Resource resource, int amount)`
- **Patch type**: Prefix
- **What it enables**: Override export pathfinding parameters, change transport cost weights, or block certain exports
- **Risk level**: Medium -- affects pathfinding setup
- **Side effects**: Could break export delivery if parameters are wrong

### Candidate 4: `Game.Simulation.ElectricityTradeSystem.ElectricityTradeJob.Execute`

- **Signature**: `void Execute()`
- **Patch type**: Prefix (to skip) or Postfix (to adjust fees)
- **What it enables**: Modify electricity trade pricing, add dynamic pricing based on time of day, or disable utility trade
- **Risk level**: Medium -- affects city budget
- **Side effects**: Incorrect fees could bankrupt a city

### Candidate 5: `Game.Simulation.WaterTradeSystem.WaterTradeJob.Execute`

- **Signature**: `void Execute()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify water/sewage trade pricing, change pollution tolerance, or add sewage import capability
- **Risk level**: Medium -- affects city budget
- **Side effects**: Same as electricity trade

## Mod Blueprint

- **Systems to create**: Custom `TradeRebalanceSystem` extending GameSystemBase that modifies OutsideTradeParameterData singleton values at runtime based on mod settings
- **Components to add**: None needed for basic rebalancing; custom component for tracking trade history if building analytics
- **Patches needed**: Postfix on `CalculateTradeCost` for fine-grained per-resource adjustments; Postfix on `ElectricityTradeJob.Execute` / `WaterTradeJob.Execute` for utility price overrides
- **Settings**: Trade cost multipliers per transport type, utility import/export price overrides, service import fee adjustments
- **UI changes**: Could extend TradedResourcesSection to show cost breakdowns

## Examples

### Example 1: Read Trade Costs for a Company

Query a company's current trade costs to understand what it pays for imports and receives for exports.

```csharp
public void LogCompanyTradeCosts(EntityManager em, Entity company)
{
    if (!em.HasBuffer<TradeCost>(company)) return;

    DynamicBuffer<TradeCost> costs = em.GetBuffer<TradeCost>(company);
    for (int i = 0; i < costs.Length; i++)
    {
        TradeCost tc = costs[i];
        Log.Info($"Resource: {tc.m_Resource}");
        Log.Info($"  Buy cost: {tc.m_BuyCost:F3}");
        Log.Info($"  Sell cost: {tc.m_SellCost:F3}");
        Log.Info($"  Last transfer: frame {tc.m_LastTransferRequestTime}");
    }
}
```

### Example 2: Modify Utility Trade Prices at Runtime

Change electricity and water import/export prices by modifying the OutsideTradeParameterData singleton.

```csharp
public partial class UtilityPriceModSystem : GameSystemBase
{
    private EntityQuery _tradeParamQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _tradeParamQuery = GetEntityQuery(
            ComponentType.ReadWrite<OutsideTradeParameterData>()
        );
        RequireForUpdate(_tradeParamQuery);
    }

    protected override void OnUpdate()
    {
        Entity paramEntity = _tradeParamQuery.GetSingletonEntity();
        OutsideTradeParameterData data =
            EntityManager.GetComponentData<OutsideTradeParameterData>(paramEntity);

        // Halve electricity import cost
        data.m_ElectricityImportPrice *= 0.5f;
        // Double water export revenue
        data.m_WaterExportPrice *= 2.0f;

        EntityManager.SetComponentData(paramEntity, data);
        Enabled = false; // Only run once
    }
}
```

### Example 3: Find All Outside Connections and Their Types

Enumerate all outside connection entities and log their transport types and remoteness.

```csharp
public void ListOutsideConnections(EntityManager em)
{
    EntityQuery ocQuery = em.CreateEntityQuery(
        ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
        ComponentType.ReadOnly<PrefabRef>()
    );

    NativeArray<Entity> entities = ocQuery.ToEntityArray(Allocator.Temp);
    for (int i = 0; i < entities.Length; i++)
    {
        Entity e = entities[i];
        PrefabRef prefabRef = em.GetComponentData<PrefabRef>(e);

        string type = "General";
        if (em.HasComponent<ElectricityOutsideConnection>(e))
            type = "Electricity";
        else if (em.HasComponent<WaterPipeOutsideConnection>(e))
            type = "Water";

        if (em.HasComponent<OutsideConnectionData>(prefabRef.m_Prefab))
        {
            OutsideConnectionData ocd =
                em.GetComponentData<OutsideConnectionData>(prefabRef.m_Prefab);
            Log.Info($"OC {e.Index}: type={type}, transfer={ocd.m_Type}, remoteness={ocd.m_Remoteness}");
        }
    }

    entities.Dispose();
    ocQuery.Dispose();
}
```

### Example 4: Monitor Electricity Import/Export Volumes

Read the ElectricityTradeSystem's last cycle values to display in a custom UI.

```csharp
public partial class ElectricityTradeMonitorSystem : GameSystemBase
{
    private ElectricityTradeSystem _elecTradeSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _elecTradeSystem = World.GetOrCreateSystemManaged<ElectricityTradeSystem>();
    }

    protected override void OnUpdate()
    {
        int exported = _elecTradeSystem.export;
        int imported = _elecTradeSystem.import;

        if (exported > 0 || imported > 0)
        {
            Log.Info($"Electricity trade: export={exported}, import={imported}");
            Log.Info($"  Export (kW/day): {(float)exported / 2048f:F1}");
            Log.Info($"  Import (kW/day): {(float)imported / 2048f:F1}");
        }
    }
}
```

### Example 5: Harmony Patch to Apply a Trade Cost Multiplier

Apply a configurable multiplier to all import costs via a postfix patch on CalculateTradeCost.

```csharp
[HarmonyPatch(typeof(Game.Simulation.TradeSystem), "CalculateTradeCost")]
public static class TradeCostPatch
{
    public static float ImportCostMultiplier { get; set; } = 1.0f;
    public static float ExportCostMultiplier { get; set; } = 1.0f;

    static void Postfix(ref TradeCost __result)
    {
        __result.m_BuyCost *= ImportCostMultiplier;
        __result.m_SellCost *= ExportCostMultiplier;
    }
}
```

## Open Questions

- [ ] **Default OutsideTradeParameterData values**: The prefab field defaults are only partially visible in the decompiled source (WaterExportPollutionTolerance = 0.1, service fees = 1.0, population range = 1000). The actual numeric values for weight/distance multipliers and utility prices come from the prefab asset data which is not in the DLL.
- [ ] **Trade balance persistence across sessions**: TradeSystem serializes m_TradeBalances, so trade state persists. However, the exact recovery behavior after loading a save with stale balances is not fully characterized.
- [ ] **Service import fee calculation**: The m_*ImportServiceFee fields on OutsideTradeParameterData and m_OCServiceTradePopulationRange suggest service import costs scale with population in discrete brackets, but the exact consumption system that reads these values was not found in the decompiled types in this research scope.
- [ ] **Sewage import**: WaterTradeSystem tracks sewage export but there is no sewage import path. The OutsideTradeParameterData.GetFee method returns 0 for sewage import. It's unclear if this is intentional or an unimplemented feature.
- [ ] **Trade cost NaN protection**: Both TradeSystem and ResourceExporterSystem have explicit NaN checks and assertions on TradeCost values. The conditions that could produce NaN in practice are not fully characterized.

## Sources

- Decompiled from: Game.dll -- Game.Simulation.TradeSystem, Game.Simulation.ResourceExporterSystem, Game.Simulation.ElectricityTradeSystem, Game.Simulation.WaterTradeSystem, Game.Simulation.OutsideConnectionDelaySystem, Game.Net.OutsideConnectionSystem
- Components: Game.Companies.TradeCost, Game.Companies.CurrentTrading, Game.Companies.ResourceExporter, Game.Objects.OutsideConnection, Game.Net.OutsideConnection, Game.Simulation.TradeNode
- Prefab types: Game.Prefabs.OutsideConnectionData, Game.Prefabs.OutsideTradeParameterData, Game.Prefabs.OutsideConnectionTransferType, Game.Prefabs.OutsideConnection, Game.Prefabs.OutsideTradeParameterPrefab
- City data: Game.City.ServiceImportBudget
- UI: Game.UI.InGame.TradedResourcesSection
- Cross-references: [Resource Production](../ResourceProduction/), [Company Simulation](../CompanySimulation/), [Economy & Budget](../EconomyBudget/)
