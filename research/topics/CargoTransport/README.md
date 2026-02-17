# Research: Cargo Transport

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How cargo trains, ships, and planes transport resources between cargo terminals, how goods delivery trucks distribute resources from storage to industrial/commercial buildings, and how the storage transfer pipeline connects production to consumption.

**Why**: To enable mods that modify cargo transport behavior, adjust delivery logistics, add custom resource flow paths, or tune the economics of freight transport.

**Boundaries**: Passenger transport and public transit are out of scope. Resource production at industrial companies is covered in Resource Production research. The pathfinding subsystem is covered separately.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Vehicles | CargoTransport, DeliveryTruck, GoodsDeliveryVehicle, CargoTransportFlags, DeliveryTruckFlags |
| Game.dll | Game.Buildings | CargoTransportStation, TransportStation, GoodsDeliveryFacility, StorageProperty |
| Game.dll | Game.Companies | StorageCompany, StorageTransferRequest, StorageTransferFlags, TransportCompany, TradeCost |
| Game.dll | Game.Simulation | StorageCompanySystem, StorageTransferSystem, GoodsDeliveryDispatchSystem, GoodsDeliveryFacilityAISystem, TransportStationAISystem, DeliveryTruckAISystem, GoodsDeliveryRequest |
| Game.dll | Game.Prefabs | CargoTransportStation (prefab), StorageCompanyData, CargoTransportStationData, TransportStationData, DeliveryTruck (prefab) |
| Game.dll | Game.Economy | Resource enum, Resources buffer, EconomyUtils |

## Component Map

### `CargoTransport` (Game.Vehicles)

Attached to cargo vehicle entities (trains, ships, planes) operating on transport routes.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetRequest | Entity | Current service request being fulfilled |
| m_State | CargoTransportFlags | Current state (EnRoute, Boarding, Returning, etc.) |
| m_DepartureFrame | uint | Simulation frame of next departure |
| m_RequestCount | int | Number of pending requests |
| m_PathElementTime | float | Time tracking for path progress |

*Source: `Game.dll` → `Game.Vehicles.CargoTransport`*

### `CargoTransportFlags`

| Flag | Value | Description |
|------|-------|-------------|
| Returning | 1 | Heading back to home station |
| EnRoute | 2 | Traveling to destination |
| Boarding | 4 | Loading/unloading at station |
| Arriving | 8 | Approaching station |
| RequiresMaintenance | 16 | Needs maintenance |
| Refueling | 32 | Currently refueling |
| AbandonRoute | 64 | Abandoning current route |
| RouteSource | 128 | Is the source vehicle on route |
| Testing | 256 | Test/debug vehicle |
| RequireStop | 512 | Must stop at next station |
| DummyTraffic | 1024 | Simulated external traffic |
| Disabled | 2048 | Vehicle disabled |

### `DeliveryTruck` (Game.Vehicles)

Attached to delivery truck entities that carry specific resources between buildings.

| Field | Type | Description |
|-------|------|-------------|
| m_State | DeliveryTruckFlags | Current state flags |
| m_Resource | Resource | Type of resource being carried |
| m_Amount | int | Quantity of resource in cargo |

### `DeliveryTruckFlags`

| Flag | Value | Description |
|------|-------|-------------|
| Returning | 1 | Heading back to origin |
| Loaded | 2 | Currently carrying cargo |
| DummyTraffic | 4 | External simulated traffic |
| Buying | 16 | Purchasing resources |
| StorageTransfer | 32 | Transferring between storage facilities |
| Delivering | 64 | Delivering to consumer |
| UpkeepDelivery | 128 | Delivering building upkeep resources |
| TransactionCancelled | 256 | Transaction was cancelled |
| UpdateOwnerQuantity | 512 | Update source resource count |
| UpdateSellerQuantity | 1024 | Update seller resource count |

### `GoodsDeliveryVehicle` (Game.Vehicles)

Simple component for goods delivery vehicles (post office, delivery service trucks).

| Field | Type | Description |
|-------|------|-------------|
| m_PathElementTime | float | Time tracking for path progress |

### `GoodsDeliveryRequest` (Game.Simulation)

Service request entity created when a building needs goods delivered.

| Field | Type | Description |
|-------|------|-------------|
| m_ResourceNeeder | Entity | Building that needs the resource |
| m_Flags | GoodsDeliveryFlags | Allowed delivery sources |
| m_Resource | Resource | Type of resource needed |
| m_Amount | int | Quantity needed |

### `GoodsDeliveryFlags`

| Flag | Value | Description |
|------|-------|-------------|
| BuildingUpkeep | 1 | Request is for building maintenance |
| CommercialAllowed | 2 | Commercial buildings can fulfill |
| IndustrialAllowed | 4 | Industrial buildings can fulfill |
| ImportAllowed | 8 | Import via outside connection allowed |
| ResourceExportTarget | 16 | This is an export destination |

### `StorageTransferRequest` (Game.Companies) — Buffer

Buffer on storage buildings listing pending transfer requests.

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | StorageTransferFlags | Transfer mode (Car, Transport, Track, Incoming) |
| m_Resource | Resource | Resource type to transfer |
| m_Amount | int | Quantity to transfer |
| m_Target | Entity | Target building/connection |

### `StorageTransferFlags`

| Flag | Value | Description |
|------|-------|-------------|
| Car | 1 | Transfer by delivery truck |
| Transport | 2 | Transfer by cargo train/ship/plane |
| Track | 4 | Track-based transport (rail) |
| Incoming | 8 | This is an incoming transfer (receiving) |

### `TransportStation` (Game.Buildings)

Runtime component on transport station building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_ComfortFactor | float | Station comfort (passenger) |
| m_LoadingFactor | float | Loading efficiency (cargo throughput) |
| m_CarRefuelTypes | EnergyTypes | Supported car fuel types |
| m_TrainRefuelTypes | EnergyTypes | Supported train fuel types |
| m_WatercraftRefuelTypes | EnergyTypes | Supported ship fuel types |
| m_AircraftRefuelTypes | EnergyTypes | Supported aircraft fuel types |
| m_Flags | TransportStationFlags | Station flags |

### `CargoTransportStation` (Game.Buildings)

Marker + data component on cargo station building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_WorkAmount | float | Accumulated work amount for goods processing |

## System Map

### `StorageCompanySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (frame-based)
- **Key responsibility**: Manages resource storage at cargo stations and storage companies. Processes incoming/outgoing resource requests, calculates trade costs, and generates StorageTransferRequests.
- **Jobs**: StorageJob (per storage company), StationStorageJob (per cargo station), OCStationStorageJob (outside connections)
- **Reads**: Resources buffer, StorageCompanyData, StorageLimitData, OwnedVehicle, DeliveryTruck
- **Writes**: StorageTransferRequest buffer, TradeCost

### `StorageTransferSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Key responsibility**: Processes StorageTransferRequests by dispatching delivery trucks to move resources between storage facilities. Matches source and destination, pathfinds routes, and creates vehicle entities.
- **Jobs**: TransferJob (Burst-compiled IJobChunk), HandleTransfersJob
- **Key data**: StorageTransferEvent (source, destination, distance, resource, amount)

### `GoodsDeliveryDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Key responsibility**: Handles GoodsDeliveryRequest entities. Finds suitable delivery sources (commercial, industrial, import), dispatches trucks from goods delivery facilities.
- **Jobs**: GoodsDeliveryDispatchJob (Burst), DispatchActionJob
- **Process**: For each undispatched request, finds the nearest source with the needed resource, creates pathfind request to source, then dispatches a vehicle.

### `GoodsDeliveryFacilityAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Key responsibility**: AI tick for goods delivery facilities (post offices, delivery depots). Manages vehicle fleet, dispatches vehicles for pending delivery requests.

### `TransportStationAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Key responsibility**: AI tick for transport stations. Updates TransportStation component based on connected routes, efficiency, and installed upgrades. Manages comfort and loading factors.

### `DeliveryTruckAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Key responsibility**: Controls delivery truck behavior. Handles loading at source, driving to destination, unloading, resource transfer between company inventories, and return trips.
- **Key behavior**: Tracks DeliveredStack (what was delivered), manages resource amounts on both source and destination entities.

### `CargoTransportStationInitializeSystem` (Game.Buildings)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Key responsibility**: Initializes new cargo transport station buildings, setting up storage and transport company components.

## Data Flow

```
RESOURCE PRODUCTION
  Industrial companies produce resources
  Stored in company Resources buffer
        |
        v
STORAGE MANAGEMENT (StorageCompanySystem, every N frames)
  For each storage company / cargo station:
    Check current inventory vs. storage limits
    Calculate trade costs per resource
    Generate StorageTransferRequests for surplus/deficit
        |
        +------ Transfer between storages ------+
        |                                       |
        v                                       v
  StorageTransferSystem               GoodsDeliveryRequest
    Matches source/dest pairs           Created when buildings
    Dispatches DeliveryTruck            need goods (upkeep,
    vehicles with resources             commercial stock)
        |                                       |
        v                                       v
  DeliveryTruckAISystem             GoodsDeliveryDispatchSystem
    Truck loads at source              Finds nearest source
    Drives to destination              Dispatches from goods
    Unloads resources                  delivery facility
    Updates company inventories        |
    Returns home                       v
        |                         GoodsDeliveryFacilityAISystem
        |                           Manages delivery fleet
        |                           Dispatches vehicles
        v
CARGO TRANSPORT (trains/ships/planes)
  TransportStationAISystem
    Updates station loading factors
    Manages connected routes
        |
        v
  CargoTransport vehicles on routes
    Load at origin station (Boarding)
    Travel route (EnRoute)
    Unload at destination (Arriving/Boarding)
    Resources transferred between stations
        |
        v
OUTSIDE CONNECTIONS (Import/Export)
  OCStationStorageJob
    Import: resources arrive from outside
    Export: resources sent to outside connections
    Managed via StorageTransferRequest with
    ImportAllowed / ResourceExportTarget flags
```

## Prefab & Configuration

### CargoTransportStation Prefab

| Field | Type | Description |
|-------|------|-------------|
| m_TradedResources | ResourceInEditor[] | Which resources this station handles |
| transports | int | Number of cargo vehicles |
| m_CarRefuelTypes | EnergyTypes | Supported car fuel types |
| m_TrainRefuelTypes | EnergyTypes | Supported train fuel types |
| m_WatercraftRefuelTypes | EnergyTypes | Supported ship fuel types |
| m_AircraftRefuelTypes | EnergyTypes | Supported aircraft fuel types |
| m_LoadingFactor | float | Cargo loading efficiency |
| m_WorkMultiplier | float | Work efficiency multiplier |
| m_TransportInterval | int2 | Min/max frames between transports |

### StorageCompanyData (ECS)

| Field | Type | Description |
|-------|------|-------------|
| m_StoredResources | Resource | Bitmask of resource types stored |
| m_TransportInterval | int2 | Min/max frames between transport dispatches |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.StorageCompanySystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept storage management, modify trade costs, change resource transfer logic
- **Risk level**: Medium (schedules Burst jobs; managed wrapper is patchable)

### Candidate 2: `Game.Simulation.GoodsDeliveryDispatchSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify delivery dispatch behavior, redirect deliveries, change source selection
- **Risk level**: Medium

### Candidate 3: `Game.Buildings.CargoTransportStationInitializeSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Modify newly created cargo stations (change stored resources, vehicle count, etc.)
- **Risk level**: Low

## Mod Blueprint

- **Systems to create**: Custom system to monitor/modify cargo flows, adjust trade costs, or create custom delivery routes
- **Components to add**: Custom tracking component for delivery analytics or priority routing
- **Patches needed**: Postfix on StorageCompanySystem.OnUpdate to modify trade costs; postfix on initialization to customize stations
- **Settings**: User-configurable trade cost multipliers, delivery distance limits, transport intervals
- **UI changes**: Custom info panel showing cargo flow statistics per station

## Examples

### Example 1: Read Cargo Station Inventory

Query a cargo transport station's stored resources and current transfer requests.

```csharp
public void InspectCargoStation(EntityManager em, Entity stationEntity)
{
    if (!em.HasComponent<Game.Buildings.CargoTransportStation>(stationEntity)) return;

    // Read storage resources
    if (em.HasBuffer<Game.Economy.Resources>(stationEntity))
    {
        DynamicBuffer<Game.Economy.Resources> resources =
            em.GetBuffer<Game.Economy.Resources>(stationEntity);
        for (int i = 0; i < resources.Length; i++)
        {
            Log.Info($"  Resource {i}: {resources[i].m_Resource} = {resources[i].m_Amount}");
        }
    }

    // Read pending transfer requests
    if (em.HasBuffer<StorageTransferRequest>(stationEntity))
    {
        DynamicBuffer<StorageTransferRequest> requests =
            em.GetBuffer<StorageTransferRequest>(stationEntity);
        Log.Info($"  Pending transfers: {requests.Length}");
        for (int i = 0; i < requests.Length; i++)
        {
            var req = requests[i];
            Log.Info($"    {req.m_Resource} x{req.m_Amount} flags={req.m_Flags}");
        }
    }
}
```

### Example 2: Track Active Delivery Trucks

Query all delivery trucks and log their current state and cargo.

```csharp
public partial class DeliveryTruckMonitorSystem : GameSystemBase
{
    private EntityQuery _truckQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _truckQuery = GetEntityQuery(
            ComponentType.ReadOnly<DeliveryTruck>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<DeliveryTruck> trucks =
            _truckQuery.ToComponentDataArray<DeliveryTruck>(Allocator.Temp);

        int loaded = 0, delivering = 0, returning = 0;
        for (int i = 0; i < trucks.Length; i++)
        {
            DeliveryTruck truck = trucks[i];
            if ((truck.m_State & DeliveryTruckFlags.Loaded) != 0) loaded++;
            if ((truck.m_State & DeliveryTruckFlags.Delivering) != 0) delivering++;
            if ((truck.m_State & DeliveryTruckFlags.Returning) != 0) returning++;
        }

        Log.Info($"Delivery trucks: {trucks.Length} total, " +
                 $"{loaded} loaded, {delivering} delivering, {returning} returning");
        trucks.Dispose();
    }
}
```

### Example 3: Monitor Goods Delivery Requests

Track unfulfilled goods delivery requests to identify supply chain bottlenecks.

```csharp
public partial class DeliveryRequestMonitorSystem : GameSystemBase
{
    private EntityQuery _requestQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _requestQuery = GetEntityQuery(
            ComponentType.ReadOnly<GoodsDeliveryRequest>(),
            ComponentType.ReadOnly<ServiceRequest>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<GoodsDeliveryRequest> requests =
            _requestQuery.ToComponentDataArray<GoodsDeliveryRequest>(Allocator.Temp);

        Dictionary<Resource, int> demandByResource = new();
        for (int i = 0; i < requests.Length; i++)
        {
            var req = requests[i];
            if (!demandByResource.ContainsKey(req.m_Resource))
                demandByResource[req.m_Resource] = 0;
            demandByResource[req.m_Resource] += req.m_Amount;
        }

        foreach (var kvp in demandByResource)
        {
            Log.Info($"Unfulfilled demand: {kvp.Key} = {kvp.Value}");
        }
        requests.Dispose();
    }
}
```

### Example 4: Modify Cargo Station Resources via Prefab

Change which resources a cargo station can trade by modifying the StorageCompanyData component.

```csharp
public void AddResourceToStation(EntityManager em, Entity stationPrefabEntity,
    Resource additionalResource)
{
    if (!em.HasComponent<StorageCompanyData>(stationPrefabEntity)) return;

    StorageCompanyData data = em.GetComponentData<StorageCompanyData>(stationPrefabEntity);
    data.m_StoredResources |= additionalResource;
    em.SetComponentData(stationPrefabEntity, data);
}
```

### Example 5: Query Cargo Transport Vehicle Status

Check the state of all cargo transport vehicles (trains, ships, planes) on routes.

```csharp
public partial class CargoVehicleMonitorSystem : GameSystemBase
{
    private EntityQuery _cargoQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _cargoQuery = GetEntityQuery(
            ComponentType.ReadOnly<CargoTransport>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<CargoTransport> vehicles =
            _cargoQuery.ToComponentDataArray<CargoTransport>(Allocator.Temp);

        int enRoute = 0, boarding = 0, returning = 0;
        for (int i = 0; i < vehicles.Length; i++)
        {
            CargoTransport v = vehicles[i];
            if ((v.m_State & CargoTransportFlags.EnRoute) != 0) enRoute++;
            if ((v.m_State & CargoTransportFlags.Boarding) != 0) boarding++;
            if ((v.m_State & CargoTransportFlags.Returning) != 0) returning++;
        }

        Log.Info($"Cargo vehicles: {vehicles.Length} total, " +
                 $"{enRoute} en route, {boarding} boarding, {returning} returning");
        vehicles.Dispose();
    }
}
```

## Open Questions

- [ ] **Cargo route assignment**: How cargo vehicles are assigned to specific routes between stations is managed by the transport route system, which was not fully traced here.
- [ ] **Trade cost calculation**: The exact formula StorageCompanySystem uses to compute TradeCost entries per resource needs deeper investigation into the Burst-compiled StorageJob.
- [ ] **Import/export capacity limits**: How outside connection capacity limits are enforced on cargo transport (whether throughput is limited per station or per connection).
- [ ] **Vehicle fleet sizing**: How the game decides when to create or destroy cargo vehicles based on demand vs. available transport capacity.

## Sources

- Decompiled from: Game.dll — Game.Vehicles.CargoTransport, Game.Vehicles.DeliveryTruck, Game.Vehicles.GoodsDeliveryVehicle, Game.Buildings.CargoTransportStation, Game.Buildings.TransportStation, Game.Simulation.StorageCompanySystem, Game.Simulation.StorageTransferSystem, Game.Simulation.GoodsDeliveryDispatchSystem, Game.Simulation.GoodsDeliveryFacilityAISystem, Game.Simulation.TransportStationAISystem, Game.Simulation.DeliveryTruckAISystem
- Request components: Game.Simulation.GoodsDeliveryRequest, Game.Companies.StorageTransferRequest
- Prefab types: Game.Prefabs.CargoTransportStation, Game.Prefabs.StorageCompanyData, Game.Prefabs.CargoTransportStationData, Game.Prefabs.TransportStationData
