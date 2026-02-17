# Research: Resource Production Chains

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How resources are defined, produced, stored, bought, sold, exported, and consumed across the CS2 economy -- the full lifecycle from raw material extraction to citizen consumption.

**Why**: Foundation for understanding company simulation, trade, and the full economic pipeline. Any mod that touches industrial production, commercial sales, resource pricing, or trade routes needs this.

**Boundaries**: Does not cover company lifecycle or spawning (Chunk 5), trade routing/pathfinding (Chunk 8), or citizen household consumption decisions in detail.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Economy | Resource enum, Resources buffer, ResourceIterator, ResourceInfo, EconomyUtils |
| Game.dll | Game.Prefabs | IndustrialProcessData, ResourceData, ResourcePrefabs, ResourcePrefab, TaxableResourceData, ResourceStack, ResourceProductionData |
| Game.dll | Game.Companies | ResourceBuyer, ResourceSeller, ResourceExporter, TradeCost |
| Game.dll | Game.Buildings | ResourceProducer, ResourceConsumer, ResourceNeeding |
| Game.dll | Game.Simulation | ResourceBuyerSystem, ResourceExporterSystem, ResourceProducerSystem, ResourceAvailabilitySystem, ResourceFlowSystem, CountConsumptionSystem |
| Game.dll | Game.Net | ResourceAvailability, ResourceConnection |

## Component Map

### `Resource` (Game.Economy)

A flags enum (`ulong`) where each resource type is a single bit. There are 41 tradeable resource types (indices 0-40), plus sentinel values.

| Value | Name | Index | Category |
|-------|------|-------|----------|
| 0 | NoResource | - | Sentinel |
| 1 | Money | 0 | Currency |
| 2 | Grain | 1 | Raw (Agricultural) |
| 4 | ConvenienceFood | 2 | Processed (Food) |
| 8 | Food | 3 | Processed (Food) |
| 16 | Vegetables | 4 | Raw (Agricultural) |
| 32 | Meals | 5 | Service (Commercial) |
| 64 | Wood | 6 | Raw (Forestry) |
| 128 | Timber | 7 | Processed (Forestry) |
| 256 | Paper | 8 | Processed (Forestry) |
| 512 | Furniture | 9 | Processed (Forestry) |
| 1024 | Vehicles | 10 | Processed (Heavy) |
| 2048 | Lodging | 11 | Service |
| 4096 | UnsortedMail | 12 | Service (Mail) |
| 8192 | LocalMail | 13 | Service (Mail) |
| 16384 | OutgoingMail | 14 | Service (Mail) |
| 32768 | Oil | 15 | Raw (Extraction) |
| 65536 | Petrochemicals | 16 | Processed (Oil) |
| 131072 | Ore | 17 | Raw (Extraction) |
| 262144 | Plastics | 18 | Processed (Oil) |
| 524288 | Metals | 19 | Processed (Ore) |
| 1048576 | Electronics | 20 | Processed (High-tech) |
| 2097152 | Software | 21 | Office |
| 4194304 | Coal | 22 | Raw (Extraction) |
| 8388608 | Stone | 23 | Raw (Extraction) |
| 16777216 | Livestock | 24 | Raw (Agricultural) |
| 33554432 | Cotton | 25 | Raw (Agricultural) |
| 67108864 | Steel | 26 | Processed (Heavy) |
| 134217728 | Minerals | 27 | Processed (Ore) |
| 268435456 | Concrete | 28 | Processed (Stone) |
| 536870912 | Machinery | 29 | Processed (Heavy) |
| 1073741824 | Chemicals | 30 | Processed (Oil) |
| 2147483648 | Pharmaceuticals | 31 | Processed (Chemicals) |
| 4294967296 | Beverages | 32 | Processed (Food) |
| 8589934592 | Textiles | 33 | Processed (Cotton) |
| 17179869184 | Telecom | 34 | Office |
| 34359738368 | Financial | 35 | Office |
| 68719476736 | Media | 36 | Office |
| 137438953472 | Entertainment | 37 | Service |
| 274877906944 | Recreation | 38 | Service |
| 549755813888 | Garbage | 39 | Special |
| 1099511627776 | Fish | 40 | Raw (Agricultural) |
| 2199023255552 | Last | 41 | Sentinel (iteration stop) |

*Source: `Game.dll` -> `Game.Economy.Resource`*

### `Resources` (Game.Economy)

IBufferElementData -- a dynamic buffer attached to entities that hold resource inventories (companies, households, buildings, vehicles).

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | Which resource type this entry represents |
| m_Amount | int | Quantity held (clamped to 0-1000000 for non-Money on older saves) |

The buffer is a sparse list: only resources with non-zero amounts are stored. Money can be negative (debt).

*Source: `Game.dll` -> `Game.Economy.Resources`*

### `ResourceIterator` (Game.Economy)

A helper struct for iterating through all resource types via bit-shifting.

| Field | Type | Description |
|-------|------|-------------|
| resource | Resource | Current resource in the iteration |

Key methods:
- `GetIterator()` -- creates a new iterator starting at NoResource
- `Next()` -- shifts to next resource bit; returns false when reaching `Resource.Last`

*Source: `Game.dll` -> `Game.Economy.ResourceIterator`*

### `IndustrialProcessData` (Game.Prefabs)

IComponentData on process prefab entities. Defines the input->output resource mapping for a company.

| Field | Type | Description |
|-------|------|-------------|
| m_Input1 | ResourceStack | First input resource and amount per production cycle |
| m_Input2 | ResourceStack | Second input resource and amount (NoResource if unused) |
| m_Output | ResourceStack | Output resource and amount per production cycle |
| m_WorkPerUnit | int | Base work required per unit of output |
| m_MaxWorkersPerCell | float | Maximum workers per zone cell |
| m_IsImport | byte | Whether this is an import-only process |

*Source: `Game.dll` -> `Game.Prefabs.IndustrialProcessData`*

### `ResourceStack` (Game.Prefabs)

Simple struct pairing a resource type with an amount. Used in IndustrialProcessData for input/output definitions.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | The resource type |
| m_Amount | int | Quantity per batch |

*Source: `Game.dll` -> `Game.Prefabs.ResourceStack`*

### `ResourceData` (Game.Prefabs)

IComponentData on resource prefab entities. Defines pricing, weight, consumption behavior, and production requirements.

| Field | Type | Description |
|-------|------|-------------|
| m_Price | float2 | x = industrial/base price, y = commercial markup |
| m_IsProduceable | bool | Whether this resource can be produced by companies |
| m_IsTradable | bool | Whether this resource can be imported/exported |
| m_IsMaterial | bool | True for raw materials that need extraction |
| m_IsLeisure | bool | Whether this is a leisure resource |
| m_Weight | float | Physical weight per unit (0 = weightless, used for office resources) |
| m_WealthModifier | float | How household wealth affects shopping probability |
| m_BaseConsumption | float | Base probability of household shopping for this resource |
| m_ChildWeight | int | Relative importance for child citizens |
| m_TeenWeight | int | Relative importance for teen citizens |
| m_AdultWeight | int | Relative importance for adult citizens |
| m_ElderlyWeight | int | Relative importance for elderly citizens |
| m_CarConsumption | int | Extra weight if household owns vehicles |
| m_RequireTemperature | bool | Whether production needs minimum temperature |
| m_RequiredTemperature | float | Minimum temperature for production |
| m_RequireNaturalResource | bool | Whether production needs a natural resource deposit |
| m_NeededWorkPerUnit | int2 | x = industrial work per unit, y = commercial work per unit |

**Pricing model**: Market price = `m_Price.x + m_Price.y`. Industrial price = `m_Price.x` only. Commercial companies earn the `m_Price.y` margin.

*Source: `Game.dll` -> `Game.Prefabs.ResourceData`*

### `ResourcePrefabs` (Game.Prefabs)

A container struct that maps `Resource` enum values to their corresponding prefab entities via a NativeArray. Accessed with indexer: `resourcePrefabs[Resource.Wood]` returns the Entity for the Wood resource prefab.

*Source: `Game.dll` -> `Game.Prefabs.ResourcePrefabs`*

### `TaxableResourceData` (Game.Prefabs)

IComponentData indicating which tax areas apply to a resource.

| Field | Type | Description |
|-------|------|-------------|
| m_TaxAreas | byte | Bitmask of TaxAreaType values that can tax this resource |

Key method: `Contains(TaxAreaType)` -- checks if a specific tax area applies.

*Source: `Game.dll` -> `Game.Prefabs.TaxableResourceData`*

### `ResourceBuyer` (Game.Companies)

IComponentData added to entities (companies or citizens) that need to purchase a resource.

| Field | Type | Description |
|-------|------|-------------|
| m_Payer | Entity | The entity paying for the resource (company or household) |
| m_Flags | SetupTargetFlags | Pathfinding setup flags |
| m_ResourceNeeded | Resource | Which resource type is needed |
| m_AmountNeeded | int | How much to buy |
| m_Location | float3 | World position of the buyer |

*Source: `Game.dll` -> `Game.Companies.ResourceBuyer`*

### `ResourceSeller` (Game.Companies)

Tag component (empty, 1-byte size) on companies that actively sell resources. Has no fields -- its presence marks the entity as a seller.

*Source: `Game.dll` -> `Game.Companies.ResourceSeller`*

### `ResourceExporter` (Game.Companies)

IComponentData added to entities that have excess resources to export via outside connections.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | Which resource to export |
| m_Amount | int | Amount to export |

*Source: `Game.dll` -> `Game.Companies.ResourceExporter`*

### `TradeCost` (Game.Companies)

IBufferElementData on company entities tracking cost history for each traded resource.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | The resource type |
| m_BuyCost | float | Smoothed purchase cost (lerped over time) |
| m_SellCost | float | Smoothed selling cost (lerped over time) |
| m_LastTransferRequestTime | long | Timestamp of last trade request |

*Source: `Game.dll` -> `Game.Companies.TradeCost`*

### `ResourceProducer` (Game.Buildings)

Tag component (empty, 1-byte size) on building entities that produce resources (city service buildings with ResourceProductionData buffers).

*Source: `Game.dll` -> `Game.Buildings.ResourceProducer`*

### `ResourceConsumer` (Game.Buildings)

IComponentData on buildings that consume resources from the economy.

| Field | Type | Description |
|-------|------|-------------|
| m_ResourceAvailability | byte | Availability level (0-255) |

*Source: `Game.dll` -> `Game.Buildings.ResourceConsumer`*

### `ResourceProductionData` (Game.Prefabs)

IBufferElementData on building prefabs defining what resources a city service building produces.

| Field | Type | Description |
|-------|------|-------------|
| m_Type | Resource | Resource type produced |
| m_ProductionRate | int | Units produced per update |
| m_StorageCapacity | int | Maximum storage before triggering export |

Has a `Combine()` method that merges production data from building upgrades.

*Source: `Game.dll` -> `Game.Prefabs.ResourceProductionData`*

### `ResourceInfo` (Game.Economy)

IComponentData tracking runtime price and trade distance for a resource.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | The resource type |
| m_Price | float | Current market price |
| m_TradeDistance | float | Average trade distance |

*Source: `Game.dll` -> `Game.Economy.ResourceInfo`*

### `ResourceAvailability` (Game.Net)

IBufferElementData on road network edges/nodes tracking how available a resource is at that location.

| Field | Type | Description |
|-------|------|-------------|
| m_Availability | float2 | x = availability amount, y = distance-weighted availability |

*Source: `Game.dll` -> `Game.Net.ResourceAvailability`*

## System Map

### `ResourceBuyerSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**:
  - m_BuyerQuery: Entities with ResourceBuyer OR ResourceBought components (two-descriptor query)
- **Key jobs**:
  - `HandleBuyersJob` (IJobChunk) -- processes ResourceBuyer entities: finds sellers via pathfinding, computes distances, enqueues SalesEvent to transfer resources
  - `BuyJob` (IJob) -- dequeues SalesEvents, transfers resources between buyer/seller inventories, updates TradeCost history, adjusts ServiceAvailable on commercial companies, tracks citizen consumption statistics
- **Reads**: ResourceBuyer, ResourceBought, Resources (seller), ResourceData, PropertyRenter, Transform, ServiceCompanyData, StorageCompany, OutsideConnection
- **Writes**: Resources (both buyer and seller), TradeCost, ServiceAvailable, Household, CompanyStatisticData, BuyingCompany
- **Key behavior**:
  - When a SalesEvent is processed, resources are removed from the seller's buffer and added to the buyer's buffer
  - Money flows in the opposite direction (buyer pays, seller receives)
  - Import purchases from outside connections are flagged with `SaleFlags.ImportFromOC`
  - Commercial sellers get `SaleFlags.CommercialSeller` for service tracking
  - TradeCost is updated with lerped buy/sell costs

### `ResourceExporterSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**:
  - m_ExporterQuery: Entities with ResourceExporter + TripNeeded (two descriptors)
  - m_OutsideConnectionQuery: Outside connection entities
- **Key jobs**:
  - `ExportJob` (IJobChunk) -- processes ResourceExporter entities: checks if already exporting, validates resource has weight (weightless = office resources, cannot be physically exported), pathfinds to outside connections, dispatches delivery trucks
  - `HandleExportsJob` (IJob) -- processes completed export events: transfers resources to outside connection, handles payment
- **Reads**: ResourceExporter, PathInformation, StorageCompany, OutsideConnection, ResourceData, PrefabRef, DeliveryTruckSelectData
- **Writes**: Resources, TripNeeded, TradeCost
- **Key behavior**:
  - Weightless resources (Software, Telecom, Financial, Media) skip physical export and are handled virtually
  - Physical resources require delivery trucks routed to outside connections

### `ResourceProducerSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 262144 / (16 * 16) = ~1024 frames
- **Updates per day**: 16
- **Queries**:
  - m_ResourceProducerQuery: Entities with ResourceProducer + Resources + UpdateFrame, excluding Deleted/Destroyed/Temp
- **Key jobs**:
  - `ResourceProducerJob` (IJobChunk) -- iterates buildings with ResourceProducer tag, reads ResourceProductionData from their prefabs (and installed upgrades via `Combine`), produces resources into the building's Resources buffer. When storage exceeds capacity or 20000 units, adds ResourceExporter component to trigger export.
  - `PlayerMoneyAddJob` (IJob) -- transfers accumulated money from city service buildings to the player's treasury
- **Reads**: PrefabRef, ResourceProductionData, InstalledUpgrade, CityServiceUpkeep
- **Writes**: Resources, PlayerMoney, adds ResourceExporter component

### `CountConsumptionSystem` (Game.Simulation)

- **Base class**: GameSystemBase (also IDefaultSerializable, ISerializable)
- **Update interval**: 262144 / 32 = 8192 frames
- **Updates per day**: 32
- **State**:
  - `m_Consumptions` -- NativeArray<int> of size ResourceCount (41), smoothed daily consumption per resource
  - `m_ConsumptionAccumulator` -- NativeArray<int> of size ResourceCount, accumulates raw consumption between updates
- **Key jobs**:
  - `CopyConsumptionJob` (IJob) -- lerps accumulator into consumptions array with factor 0.3, then clears accumulator
- **Key behavior**:
  - Other systems (e.g., ResourceBuyerSystem) write to the accumulator
  - Consumption data is smoothed: `new = lerp(old/updatesPerDay, accumulated, 0.3) * updatesPerDay`
  - Provides `GetConsumptions()` and `GetConsumptionAccumulator()` with job dependency management

### `ResourceAvailabilitySystem` (Game.Simulation)

- **Base class**: GameSystemBase (also IDefaultSerializable, ISerializable)
- **Purpose**: Computes a spatial availability map for resources across the road network
- **Key jobs**: FindWorkplaceLocationsJob, FindAttractionLocationsJob, FindServiceLocationsJob, FindConsumerLocationsJob, FindConvenienceFoodStoreLocationsJob, FindOutsideConnectionLocationsJob, FindSellerLocationsJob, FindTaxiLocationsJob, FindBusStopLocationsJob, FindTramSubwayLocationsJob, ClearAvailabilityJob, ApplyAvailabilityJob
- **Reads**: WorkProvider, AttractivenessProvider, ServiceAvailable, ResourceSeller, OutsideConnection, etc.
- **Writes**: ResourceAvailability buffer on network edges
- **Key behavior**: Flood-fills availability scores from providers outward through the road network using pathfinding. The result is a per-edge ResourceAvailability buffer that demand systems and citizens use to find nearby resources.

### `ResourceFlowSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Purpose**: Computes resource flow directions on the road network
- **Key jobs**: `ResourceFlowJob` (IJob) -- processes road network nodes, computes ResourceConnection direction data based on nearby resource sources/sinks
- **Reads**: Node, Edge, Curve, ConnectedEdge, SubObject, Transform
- **Writes**: ResourceConnection on road edges

## Data Flow

```
RESOURCE DEFINITION
  ResourcePrefab (editor) -> ResourceData (runtime)
    Defines price, weight, production requirements
    Indexed by ResourcePrefabs container
          |
          v
INDUSTRIAL PROCESS DEFINITION
  IndustrialProcess (editor) -> IndustrialProcessData (runtime)
    Maps: Input1 + Input2 -> Output (with amounts)
    Example: Grain(1) + Livestock(1) -> Food(1)
          |
          v
RAW MATERIAL EXTRACTION
  Extractor companies (farms, mines, forestry)
    Require: natural resource deposit + workers
    Output: raw materials (Grain, Wood, Oil, Ore, Coal, Stone, etc.)
    Stored in: company's Resources buffer
          |
          v
PROCESSING / MANUFACTURING
  Industrial companies
    Read: IndustrialProcessData for input/output recipe
    Consume: input resources from Resources buffer
    Produce: output resources into Resources buffer
    Rate: workforce * efficiency * updatesPerDay / neededWorkPerUnit
          |
          v
CITY SERVICE PRODUCTION
  ResourceProducerSystem (every ~1024 frames)
    Buildings with ResourceProducer tag
    Read: ResourceProductionData from prefab + upgrades
    Produce: resources at m_ProductionRate into Resources buffer
    When storage >= min(20000, m_StorageCapacity):
      -> Adds ResourceExporter component to trigger export
          |
          v
SELLING (Market Matching)
  ResourceBuyerSystem (every 16 frames)
    Entities with ResourceBuyer component need resources
    HandleBuyersJob: pathfind to sellers, create SalesEvent
    BuyJob: transfer resources + money between parties
      seller.Resources[resource] -= amount
      buyer.Resources[resource] += amount
      buyer.Resources[Money] -= cost
      seller.Resources[Money] += cost
    Updates TradeCost with smoothed buy/sell costs
          |
          v
EXPORTING (Surplus to Outside)
  ResourceExporterSystem (every 16 frames)
    Entities with ResourceExporter + excess inventory
    ExportJob: pathfind to outside connections
    Weightless resources (office): virtual export
    Physical resources: dispatch delivery trucks
    HandleExportsJob: transfer resources + receive payment
          |
          v
CONSUMPTION TRACKING
  CountConsumptionSystem (32x/day)
    Accumulates citizen consumption per resource
    Smooths with 0.3 lerp factor
    Used by demand systems to compute demand
          |
          v
AVAILABILITY MAPPING
  ResourceAvailabilitySystem
    Flood-fills availability scores across road network
    Result: ResourceAvailability buffer per road edge
    Used by: citizens finding shops, demand calculations
```

## Prefab & Configuration

### Resource Pricing

| Parameter | Source | Description |
|-----------|--------|-------------|
| Industrial price | ResourceData.m_Price.x | Base price when selling to other companies |
| Commercial markup | ResourceData.m_Price.y | Additional margin for commercial sales |
| Market price | m_Price.x + m_Price.y | Total price consumers pay |

### Production Parameters

| Value | Source | Description |
|-------|--------|-------------|
| Input resources | IndustrialProcessData.m_Input1, m_Input2 | What raw/processed materials are consumed |
| Output resource | IndustrialProcessData.m_Output | What the company produces |
| Work per unit | ResourceData.m_NeededWorkPerUnit | x = industrial, y = commercial work needed |
| Max workers/cell | IndustrialProcessData.m_MaxWorkersPerCell | Density cap per zone cell |
| Natural resource required | ResourceData.m_RequireNaturalResource | Whether extraction needs a deposit |
| Temperature required | ResourceData.m_RequireTemperature | Whether a minimum temperature is needed |
| Required temperature | ResourceData.m_RequiredTemperature | Minimum temperature for production |

### Economy Parameters (from EconomyUtils)

| Constant | Value | Description |
|----------|-------|-------------|
| kCompanyUpdatesPerDay | 256 | How often company production is calculated per game day |
| ResourceCount | 41 | Total number of resource types (index of Resource.Last) |
| Transport cost formula | `distance * 0.03 * weight * (1 + amount/1000)` | Cost of transporting resources |

### Production Rate Formula

```
productionPerDay = outputAmount * (buildingEfficiency * efficiencyMultiplier * workforce * 256) / neededWorkPerUnit
```

Where:
- `efficiencyMultiplier` = ExtractorProductionEfficiency (extractors), IndustrialEfficiency (industry), or CommercialEfficiency (commercial)
- `workforce` = sum of worker contributions: `((level==0 ? 2 : 1) + 2.5 * level) * (0.75 + happiness/200)`

## Harmony Patch Points

### Candidate 1: `Game.Economy.EconomyUtils.GetIndustrialPrice`

- **Signature**: `public static float GetIndustrialPrice(Resource r, ResourcePrefabs prefabs, ref ComponentLookup<ResourceData> resourceDatas)`
- **Patch type**: Postfix
- **What it enables**: Override resource pricing to create dynamic market effects, inflation, or resource-specific price adjustments
- **Risk level**: Medium -- affects all economic calculations that use industrial prices
- **Side effects**: Changes propagate to company profit calculations, trade cost assessments, and company net worth

### Candidate 2: `Game.Economy.EconomyUtils.GetCompanyProductionPerDay`

- **Signature**: `public static int GetCompanyProductionPerDay(float buildingEfficiency, bool isIndustrial, DynamicBuffer<Employee> employees, IndustrialProcessData processData, ResourceData resourceData, ref ComponentLookup<Citizen> citizens, ref EconomyParameterData economyParameters, ServiceAvailable serviceAvailable, ServiceCompanyData serviceCompanyData)`
- **Patch type**: Postfix
- **What it enables**: Modify production rates per company to implement bonuses, penalties, or policy effects
- **Risk level**: Medium -- affects supply chain balance
- **Side effects**: Over-production can flood markets; under-production can cause shortages

### Candidate 3: `Game.Economy.EconomyUtils.GetTransportCost`

- **Signature**: `public static int GetTransportCost(float distance, Resource resource, int amount, float weight)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify transport cost calculations to simulate fuel price changes, infrastructure improvements, or trade agreements
- **Risk level**: Low -- transport costs affect trade decisions but not core production
- **Side effects**: Very low costs may cause unrealistic long-distance trade patterns

### Candidate 4: `Game.Simulation.ResourceBuyerSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix
- **What it enables**: Intercept or modify the entire buying pipeline, inject custom matching logic, or throttle purchases
- **Risk level**: High -- core economic loop
- **Side effects**: Breaking the buy cycle can collapse the economy

### Candidate 5: `Game.Economy.EconomyUtils.IsProducedFrom`

- **Signature**: `public static bool IsProducedFrom(Resource product, Resource material)`
- **Patch type**: Postfix
- **What it enables**: Add new production chain relationships or remove existing ones
- **Risk level**: Low -- informational method used for UI and some chain logic
- **Side effects**: Minimal, primarily affects display and resource chain categorization

## Mod Blueprint

- **Systems to create**: Custom `GameSystemBase` systems for monitoring resource flows, adjusting prices, or implementing resource events
- **Components to add**: Custom IComponentData for tracking mod-specific resource state (e.g., resource quality, spoilage timers)
- **Patches needed**: EconomyUtils.GetIndustrialPrice (pricing), EconomyUtils.GetCompanyProductionPerDay (production rates)
- **Settings**: Resource price multipliers, production efficiency overrides, transport cost modifiers
- **UI changes**: Custom info panels showing resource flow statistics, production chain visualizer

## Examples

### Example 1: Read a Company's Resource Inventory

Enumerate all resources held by a company entity, using the Resources buffer.

```csharp
public void LogCompanyResources(EntityManager em, Entity company)
{
    if (!em.HasBuffer<Resources>(company))
    {
        Log.Info("Entity has no Resources buffer.");
        return;
    }

    DynamicBuffer<Resources> resources = em.GetBuffer<Resources>(company);
    Log.Info($"Company {company.Index} has {resources.Length} resource entries:");
    for (int i = 0; i < resources.Length; i++)
    {
        Resources entry = resources[i];
        string name = EconomyUtils.GetName(entry.m_Resource);
        Log.Info($"  {name}: {entry.m_Amount}");
    }

    int money = EconomyUtils.GetResources(Resource.Money, resources);
    int totalStorage = EconomyUtils.GetTotalStorageUsed(resources);
    Log.Info($"  Cash: {money}, Total storage used: {totalStorage}");
}
```

### Example 2: Find All Producers of a Specific Resource

Query all entities with IndustrialProcessData whose output matches a target resource.

```csharp
public partial class FindProducersSystem : GameSystemBase
{
    private EntityQuery _processQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _processQuery = GetEntityQuery(
            ComponentType.ReadOnly<IndustrialProcessData>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        Resource target = Resource.Steel;
        NativeArray<Entity> entities =
            _processQuery.ToEntityArray(Allocator.Temp);
        NativeArray<IndustrialProcessData> processes =
            _processQuery.ToComponentDataArray<IndustrialProcessData>(Allocator.Temp);

        int count = 0;
        for (int i = 0; i < processes.Length; i++)
        {
            if (processes[i].m_Output.m_Resource == target)
            {
                IndustrialProcessData p = processes[i];
                string input1 = EconomyUtils.GetName(p.m_Input1.m_Resource);
                string input2 = EconomyUtils.GetName(p.m_Input2.m_Resource);
                Log.Info($"Producer {entities[i].Index}: " +
                    $"{input1}({p.m_Input1.m_Amount}) + " +
                    $"{input2}({p.m_Input2.m_Amount}) -> " +
                    $"Steel({p.m_Output.m_Amount})");
                count++;
            }
        }
        Log.Info($"Found {count} steel production processes.");

        entities.Dispose();
        processes.Dispose();
    }
}
```

### Example 3: Iterate Through All Resource Types

Use ResourceIterator to enumerate every resource and look up its prefab data.

```csharp
public void ListAllResources(
    ResourcePrefabs prefabs,
    ComponentLookup<ResourceData> resourceDatas)
{
    ResourceIterator iterator = ResourceIterator.GetIterator();
    while (iterator.Next())
    {
        Resource r = iterator.resource;
        Entity prefabEntity = prefabs[r];
        if (prefabEntity == Entity.Null) continue;

        if (!resourceDatas.HasComponent(prefabEntity)) continue;

        ResourceData data = resourceDatas[prefabEntity];
        float marketPrice = data.m_Price.x + data.m_Price.y;

        Log.Info($"{EconomyUtils.GetName(r)} (idx {EconomyUtils.GetResourceIndex(r)}): " +
            $"price={marketPrice:F1}, weight={data.m_Weight}, " +
            $"produceable={data.m_IsProduceable}, tradable={data.m_IsTradable}, " +
            $"material={data.m_IsMaterial}");
    }
}
```

### Example 4: Check Resource Availability on a Road

Read the ResourceAvailability buffer from a road edge to see what resources are nearby.

```csharp
public void CheckRoadResourceAvailability(EntityManager em, Entity roadEdge)
{
    if (!em.HasBuffer<ResourceAvailability>(roadEdge))
    {
        Log.Info("Entity has no ResourceAvailability buffer.");
        return;
    }

    DynamicBuffer<ResourceAvailability> availability =
        em.GetBuffer<ResourceAvailability>(roadEdge);

    ResourceIterator iterator = ResourceIterator.GetIterator();
    int index = 0;
    while (iterator.Next())
    {
        if (index < availability.Length)
        {
            float2 avail = availability[index].m_Availability;
            if (avail.x > 0f)
            {
                Log.Info($"  {EconomyUtils.GetName(iterator.resource)}: " +
                    $"amount={avail.x:F1}, distance-weighted={avail.y:F1}");
            }
        }
        index++;
    }
}
```

### Example 5: Modify an IndustrialProcessData at Runtime

Change the output amount of a specific production process (e.g., double steel production).

```csharp
public void BoostSteelProduction(EntityManager em)
{
    EntityQuery processQuery = em.CreateEntityQuery(
        ComponentType.ReadWrite<IndustrialProcessData>()
    );

    NativeArray<Entity> entities =
        processQuery.ToEntityArray(Allocator.Temp);
    NativeArray<IndustrialProcessData> processes =
        processQuery.ToComponentDataArray<IndustrialProcessData>(Allocator.Temp);

    for (int i = 0; i < processes.Length; i++)
    {
        IndustrialProcessData p = processes[i];
        if (p.m_Output.m_Resource == Resource.Steel)
        {
            p.m_Output.m_Amount *= 2;
            em.SetComponentData(entities[i], p);
            Log.Info($"Doubled steel output for process entity {entities[i].Index}");
        }
    }

    entities.Dispose();
    processes.Dispose();
    processQuery.Dispose();
}
```

## Open Questions

- [ ] Exact production chain recipes: The IndustrialProcessData defines per-prefab recipes, but the full set of recipes (which specific prefabs produce which resources) requires examining all prefab data at runtime. The `IsProducedFrom()` method provides a hardcoded overview but may not match all actual prefab configurations.
- [ ] Service availability feedback loop: Commercial companies reduce production when `ServiceAvailable` exceeds 80% of `ServiceCompanyData.m_MaxService`. The exact throttle curve (`math.saturate((ratio - 0.8) / 0.2)`) needs testing for edge cases.
- [ ] Resource cap behavior: The Resources buffer clamps amounts to 1,000,000 for non-Money resources in save migration, but runtime behavior for overflow is unclear -- does production stop or do amounts just keep growing?
- [ ] CountConsumptionSystem smoothing: The lerp factor of 0.3 means consumption data takes several updates to reflect actual values. How this affects demand system responsiveness is not fully characterized.
- [ ] Trade cost NaN handling: TradeCost deserialization explicitly checks for NaN and resets to 0. What conditions cause NaN trade costs in the first place?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Core types: Game.Economy.Resource, Game.Economy.Resources, Game.Economy.ResourceIterator, Game.Economy.EconomyUtils, Game.Economy.ResourceInfo
- Prefab types: Game.Prefabs.IndustrialProcessData, Game.Prefabs.ResourceData, Game.Prefabs.ResourcePrefabs, Game.Prefabs.ResourcePrefab, Game.Prefabs.TaxableResourceData, Game.Prefabs.ResourceStack, Game.Prefabs.ResourceProductionData, Game.Prefabs.IndustrialProcess
- Company components: Game.Companies.ResourceBuyer, Game.Companies.ResourceSeller, Game.Companies.ResourceExporter, Game.Companies.TradeCost
- Building components: Game.Buildings.ResourceProducer, Game.Buildings.ResourceConsumer
- Systems: Game.Simulation.ResourceBuyerSystem, Game.Simulation.ResourceExporterSystem, Game.Simulation.ResourceProducerSystem, Game.Simulation.CountConsumptionSystem, Game.Simulation.ResourceAvailabilitySystem, Game.Simulation.ResourceFlowSystem
- Network types: Game.Net.ResourceAvailability, Game.Net.ResourceConnection
