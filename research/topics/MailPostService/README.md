# Research: Mail & Post Service

> **Status**: Complete
> **Date started**: 2026-02-17
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How the mail and postal service system works in CS2 -- from mail generation on buildings through collection, sorting, inter-facility transfer, and delivery back to buildings.

**Why**: Understanding the complete mail pipeline enables mods that adjust mail generation rates, modify post van/truck behavior, customize sorting facility operations, or add entirely new postal service mechanics.

**Boundaries**: Out of scope -- general vehicle navigation internals (covered in Traffic Flow), building efficiency system internals beyond the mail penalty, and the rendering pipeline for post van visuals.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Buildings | MailProducer, PostFacility, PostFacilityFlags |
| Game.dll | Game.Routes | MailBox (streetside mailbox component) |
| Game.dll | Game.Vehicles | PostVan, PostVanFlags |
| Game.dll | Game.Simulation | MailAccumulationSystem, MailBoxSystem, PostVanDispatchSystem, PostVanAISystem, PostFacilityAISystem, MailTransferDispatchSystem, PostVanRequest, PostVanRequestFlags, MailTransferRequest, MailTransferRequestFlags |
| Game.dll | Game.Prefabs | PostConfigurationData, PostFacilityData, MailBoxData, PostVanData, MailAccumulationData |
| Game.dll | Game.Economy | Resources (UnsortedMail, LocalMail, OutgoingMail resource types) |

## Component Map

### `MailProducer` (Game.Buildings)

Attached to every building that generates or receives mail. Tracks accumulated sending/receiving mail amounts.

| Field | Type | Description |
|-------|------|-------------|
| m_MailRequest | Entity | Current PostVanRequest entity for this building |
| m_SendingMail | ushort | Outgoing mail accumulated (waiting for collection) |
| m_ReceivingMail | ushort | Incoming mail accumulated (bit 15 = mailDelivered flag) |
| m_DispatchIndex | byte | Dispatch ordering index for van routing |
| m_LastUpdateTotalMail | ushort | Total mail at last update (for processed mail tracking) |

**Properties:**
- `receivingMail` (int) -- lower 15 bits of m_ReceivingMail (actual mail count)
- `mailDelivered` (bool) -- bit 15 of m_ReceivingMail (true if van delivered mail recently)

*Source: `Game.dll` -> `Game.Buildings.MailProducer`*

### `PostFacility` (Game.Buildings)

Attached to post office/sorting facility buildings. Tracks mail transfer requests and operational state.

| Field | Type | Description |
|-------|------|-------------|
| m_MailDeliverRequest | Entity | Active MailTransferRequest for receiving unsorted mail |
| m_MailReceiveRequest | Entity | Active MailTransferRequest for sending sorted mail out |
| m_TargetRequest | Entity | Reverse-search PostVanRequest (facility seeking targets) |
| m_AcceptMailPriority | float | Priority for accepting incoming mail (0-1, higher = more capacity) |
| m_DeliverMailPriority | float | Priority for sending sorted mail out (0-1, higher = more mail waiting) |
| m_Flags | PostFacilityFlags | Operational state bitfield |
| m_ProcessingFactor | byte | Current sorting throughput (0-255, percentage of max rate) |

*Source: `Game.dll` -> `Game.Buildings.PostFacility`*

### `PostFacilityFlags` (Game.Buildings)

| Flag | Value | Description |
|------|-------|-------------|
| CanDeliverMailWithVan | 0x01 | Has available vans and local mail to deliver |
| CanCollectMailWithVan | 0x02 | Has available vans and free capacity to collect |
| HasAvailableTrucks | 0x04 | Has idle delivery trucks for inter-facility transfer |
| AcceptsUnsortedMail | 0x08 | Has capacity to receive unsorted mail (sorting facilities) |
| DeliversLocalMail | 0x10 | Has local mail ready to distribute (sorting facilities) |
| AcceptsLocalMail | 0x20 | Has capacity for local mail (non-sorting post offices) |
| DeliversUnsortedMail | 0x40 | Has unsorted mail to offload (non-sorting post offices) |

### `MailBox` (Game.Routes)

Attached to streetside mailbox entities (TransportStop entities that are not PostFacility).

| Field | Type | Description |
|-------|------|-------------|
| m_CollectRequest | Entity | Current PostVanRequest for mailbox collection |
| m_MailAmount | int | Current mail in the box awaiting pickup |

*Source: `Game.dll` -> `Game.Routes.MailBox`*

### `PostVan` (Game.Vehicles)

The vehicle component for mail delivery vans.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetRequest | Entity | Current reverse-search request |
| m_State | PostVanFlags | State bitfield |
| m_RequestCount | int | Number of accepted service dispatches |
| m_PathElementTime | float | Average time per path element (for route estimation) |
| m_DeliveringMail | int | Local mail being delivered to buildings |
| m_CollectedMail | int | Unsorted mail collected from buildings/mailboxes |
| m_DeliveryEstimate | int | Estimated mail to deliver on current route |
| m_CollectEstimate | int | Estimated mail to collect on current route |

*Source: `Game.dll` -> `Game.Vehicles.PostVan`*

### `PostVanFlags` (Game.Vehicles)

| Flag | Value | Description |
|------|-------|-------------|
| Returning | 0x01 | Heading back to post facility |
| Delivering | 0x02 | Currently delivering local mail to buildings |
| Collecting | 0x04 | Currently collecting outgoing mail from buildings |
| DeliveryEmpty | 0x08 | No more mail to deliver |
| CollectFull | 0x10 | Collection capacity reached |
| EstimatedEmpty | 0x20 | Estimated to run out of delivery mail before route end |
| EstimatedFull | 0x40 | Estimated to fill collection capacity before route end |
| Disabled | 0x80 | Van disabled (facility has no work) |
| ClearChecked | 0x100 | Internal: force re-check of buildings on lane |

### `PostVanRequest` (Game.Simulation)

Service request entity for post van dispatch (local delivery/collection).

| Field | Type | Description |
|-------|------|-------------|
| m_Target | Entity | Building or mailbox requesting service |
| m_Flags | PostVanRequestFlags | Type of service requested |
| m_DispatchIndex | byte | Dispatch ordering index |
| m_Priority | ushort | Priority value (usually mail amount) |

### `PostVanRequestFlags` (Game.Simulation)

| Flag | Value | Description |
|------|-------|-------------|
| Deliver | 0x01 | Van should deliver local mail to target |
| Collect | 0x02 | Van should collect outgoing mail from target |
| BuildingTarget | 0x04 | Target is a building (MailProducer) |
| MailBoxTarget | 0x08 | Target is a streetside mailbox |

### `MailTransferRequest` (Game.Simulation)

Service request entity for inter-facility mail transfer (via delivery trucks).

| Field | Type | Description |
|-------|------|-------------|
| m_Facility | Entity | Post facility creating the request |
| m_Flags | MailTransferRequestFlags | Transfer type and mail categories |
| m_Priority | float | Priority (ratio of current mail to capacity) |
| m_Amount | int | Amount of mail to transfer |

### `MailTransferRequestFlags` (Game.Simulation)

| Flag | Value | Description |
|------|-------|-------------|
| Deliver | 0x01 | Truck delivers mail TO the requesting facility |
| Receive | 0x02 | Truck picks up mail FROM the requesting facility |
| RequireTransport | 0x04 | Needs external truck (facility has no own trucks) |
| UnsortedMail | 0x10 | Primary cargo is unsorted mail |
| LocalMail | 0x20 | Primary cargo is local (sorted) mail |
| OutgoingMail | 0x40 | Primary cargo is outgoing mail |
| ReturnUnsortedMail | 0x100 | Return trip carries unsorted mail |
| ReturnLocalMail | 0x200 | Return trip carries local mail |
| ReturnOutgoingMail | 0x400 | Return trip carries outgoing mail |

## System Map

### `MailAccumulationSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 64 frames, 16 update slots)
- **Queries**:
  - EntityQuery requiring: [MailProducer], excluding: [Deleted, Destroyed, Temp]
- **Reads**: PrefabRef, SpawnableBuildingData, MailAccumulationData, ServiceObjectData, HouseholdCitizen, Employee, Renter, PostVanRequest
- **Writes**: MailProducer (m_SendingMail, receivingMail, m_LastUpdateTotalMail), Efficiency (mail factor)
- **Key methods**:
  - `OnUpdate()` -- schedules MailAccumulationJob; tracks accumulated/processed mail across 4 cycles (256 updates/day)
  - `GetBaseAccumulationRate()` -- looks up MailAccumulationData from zone prefab or service object
  - `GetCitizenCounts()` -- counts residents (HouseholdCitizen) and workers (Employee) in a building
  - `RequestPostVanIfNeeded()` -- creates PostVanRequest when mail exceeds m_MailAccumulationTolerance
  - `GetMailEfficiencyFactor()` -- calculates building efficiency penalty from mail accumulation (quadratic formula above 25 units)

**Mail accumulation formula**: `rate = baseRate * (residents + workers) * 0.28444445`. The constant 0.28444445 normalizes the per-tick rate. Mail is capped at `PostConfigurationData.m_MaxMailAccumulation`.

### `MailBoxSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 512 frames)
- **Queries**:
  - EntityQuery requiring: [Game.Routes.MailBox, TransportStop], excluding: [PostFacility, Temp, Deleted, Destroyed]
- **Reads**: MailBox, PostVanRequest, PostConfigurationData
- **Writes**: Creates PostVanRequest entities
- **Key methods**:
  - `OnUpdate()` -- schedules MailBoxTickJob
  - `RequestPostVanIfNeeded()` -- creates PostVanRequest (Collect | MailBoxTarget) when m_MailAmount >= m_MailAccumulationTolerance

### `PostVanDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames)
- **Queries**:
  - EntityQuery requiring: [PostVanRequest, UpdateFrame]
- **Reads**: PostVanRequest, ParkedCar, Owner, CurrentDistrict, ServiceDispatch
- **Writes**: MailProducer (m_MailRequest), MailBox (m_CollectRequest), PostFacility (m_TargetRequest), PostVan (m_TargetRequest)
- **Key methods**:
  - `FindVehicleSource()` -- pathfinds from SetupTargetType.PostVan origin to building/mailbox target
  - `FindVehicleTarget()` -- reverse dispatch: pathfinds from facility/van to find PostVanRequest targets
  - `ValidateTarget()` -- checks MailProducer or MailBox still needs service
  - `ValidateReversed()` -- validates facility/van still available for reverse dispatch
  - `DispatchVehicle()` -- resolves parked car to owner, creates Dispatched component

### `PostVanAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames, offset 9)
- **Queries**:
  - EntityQuery requiring: [CarCurrentLane, Owner, PrefabRef, PathOwner, PostVan, Target], excluding: [Deleted, Temp, TripSource, OutOfControl]
- **Reads**: Many (lane data, building data, prefab data, mail data)
- **Writes**: PostVan, Car, CarCurrentLane, PathOwner, Target, MailProducer, MailBox, Resources
- **Key methods**:
  - `Tick()` -- main per-vehicle logic: checks path state, handles arrivals, manages dispatches
  - `TryHandleBuilding()` -- at waypoints, delivers/collects mail to/from buildings along the route
  - `TryHandleMailBox()` -- collects mail from streetside mailbox at route endpoint
  - `UnloadMail()` -- on return to facility, adds collected mail as UnsortedMail resource and returns undelivered LocalMail
  - `SelectNextDispatch()` -- picks next service dispatch, appends path, sets Delivering/Collecting flags
  - `ReturnToFacility()` -- clears dispatches, sets Returning flag
  - `RequestTargetIfNeeded()` -- creates reverse PostVanRequest (van seeking work) every 512 frames
  - `RequireCollect()` -- checks MailAccumulationData.m_RequireCollect for building's zone type

**Statistics**: The MailActionJob fires `StatisticType.DeliveredMail` and `StatisticType.CollectedMail` events for city statistics tracking.

### `PostFacilityAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 256 frames, offset 176)
- **Queries**:
  - EntityQuery requiring: [PostFacility, Building, ServiceDispatch, PrefabRef], excluding: [Temp, Deleted]
- **Reads**: PostFacilityData, MailBoxData, PostVanData, DeliveryTruckData, PostConfigurationData, OwnedVehicle, GuestVehicle, Efficiency, InstalledUpgrade
- **Writes**: PostFacility, MailBox, Resources, ServiceDispatch, OwnedVehicle, GuestVehicle
- **Key methods**:
  - `Tick()` -- the central facility logic: counts vehicles, calculates mail inventory, sorts mail, creates transfer requests, spawns vehicles
  - `TrySpawnPostVan()` -- creates or unparks a post van, loads local mail, sets Delivering/Collecting flags
  - `TrySpawnDeliveryTruck()` -- creates a delivery truck for inter-facility transfer with primary and return cargo
  - `RequestTargetIfNeeded()` -- creates reverse PostVanRequest (facility seeking targets) every 256+ frames

**Sorting logic**: Facilities with `m_SortingRate != 0` sort unsorted mail each tick. The formula:
`sortedAmount = min(unsortedMail, efficiency * 0.0009765625 * m_SortingRate)`. Sorted mail splits: `outgoing = sorted * m_OutgoingMailPercentage / 100`, remainder becomes local mail.

**Two facility types**:
1. **Sorting facility** (m_SortingRate != 0): Accepts unsorted mail, sorts into local + outgoing, distributes local via vans
2. **Non-sorting post office** (m_SortingRate == 0): Accepts local mail deliveries, redistributes to buildings, sends unsorted mail to sorting facilities

### `MailTransferDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames)
- **Queries**:
  - EntityQuery requiring: [MailTransferRequest, UpdateFrame]
- **Reads**: MailTransferRequest, PostFacility, ServiceDispatch, TripNeeded
- **Writes**: PostFacility (m_MailDeliverRequest, m_MailReceiveRequest), Resources, TripNeeded
- **Key methods**:
  - `FindVehicleSource()` -- pathfinds for MailTransfer target type, uses Road + CargoLoading methods
  - `ValidateTarget()` -- checks PostFacility still has matching priority > 0 and correct request reference
  - `DispatchVehicle()` -- adds Dispatched component, enqueues DispatchAction
  - DispatchActionJob -- processes dispatches: adds ServiceDispatch or TripNeeded, manages resource loading/unloading with return loads

**Resource types**: Uses `Resource.UnsortedMail`, `Resource.LocalMail`, and `Resource.OutgoingMail` as distinct economic resources. Trucks can carry one primary type and one return type per trip.

## Data Flow

```
[Buildings with MailProducer]
    |
    | MailAccumulationSystem (every 64 frames)
    |   - Adds sending + receiving mail based on citizen count * accumulation rate
    |   - Caps at PostConfigurationData.m_MaxMailAccumulation
    |   - Creates PostVanRequest when mail >= m_MailAccumulationTolerance
    |   - Updates building efficiency (mail penalty)
    |
    v
[PostVanRequest entities]                     [MailBox entities]
    |                                              |
    | PostVanDispatchSystem (every 16 frames)      | MailBoxSystem (every 512 frames)
    |   - Pathfinds from PostVan/Facility           |   - Creates PostVanRequest (Collect)
    |     to building/mailbox target                |     when m_MailAmount >= tolerance
    |   - Supports reverse dispatch                 |
    |     (vans/facilities seeking targets)         |
    |                                              |
    v                                              v
[PostFacilityAISystem] (every 256 frames)
    |
    | - Counts owned vans and trucks, calculates capacity
    | - Moves mail from attached MailBox into Resources buffer
    | - SORTING FACILITIES: sorts UnsortedMail -> LocalMail + OutgoingMail
    | - Spawns post vans (loaded with LocalMail) for building delivery
    | - Creates MailTransferRequest for inter-facility truck transfers
    | - Sets PostFacilityFlags based on capacity and inventory
    |
    v
[Post Vans on route]                    [MailTransferRequest entities]
    |                                        |
    | PostVanAISystem (every 16 frames)      | MailTransferDispatchSystem (every 16 frames)
    |   - Drives route, stops at buildings   |   - Pathfinds between facilities
    |   - Delivers LocalMail to buildings    |   - Dispatches delivery trucks
    |     (reduces receivingMail)            |
    |   - Collects SendingMail from          |
    |     buildings needing collection       v
    |   - Collects from MailBoxes       [Delivery Trucks]
    |   - Returns to facility:               |
    |     collected -> UnsortedMail          | Transfer mail between facilities:
    |     undelivered -> LocalMail           |   UnsortedMail, LocalMail, OutgoingMail
    |                                        |   (bidirectional with return loads)
    v                                        v
[Facility Resources buffer]
    UnsortedMail -> sorted -> LocalMail + OutgoingMail
    LocalMail -> loaded onto vans -> delivered to buildings
    OutgoingMail -> transferred to other facilities or exported
```

## Prefab & Configuration

### PostConfigurationData (Singleton)

| Field | Type | Description |
|-------|------|-------------|
| m_PostServicePrefab | Entity | Post service prefab (checked for Locked to disable system) |
| m_MaxMailAccumulation | int | Max mail per building before capping |
| m_MailAccumulationTolerance | int | Threshold before requesting a post van |
| m_OutgoingMailPercentage | int | Percentage of sorted mail that becomes outgoing (rest is local) |

### PostFacilityData (Per-building prefab)

| Field | Type | Description |
|-------|------|-------------|
| m_PostVanCapacity | int | Maximum number of post vans |
| m_PostTruckCapacity | int | Maximum number of delivery trucks |
| m_MailCapacity | int | Total mail storage capacity |
| m_SortingRate | int | Mail sorting rate per tick (0 = non-sorting facility) |

### MailBoxData (Per-building prefab)

| Field | Type | Description |
|-------|------|-------------|
| m_MailCapacity | int | Capacity of the attached mailbox (reserved from facility capacity) |

### PostVanData (Per-vehicle prefab)

| Field | Type | Description |
|-------|------|-------------|
| m_MailCapacity | int | How much mail the van can carry |

### MailAccumulationData (Per-zone or service prefab)

| Field | Type | Description |
|-------|------|-------------|
| m_RequireCollect | bool | Whether outgoing mail must be collected by vans (vs delivery-only) |
| m_AccumulationRate | float2 | (sending rate, receiving rate) per citizen per tick |

### BuildingEfficiencyParameterData (Singleton, mail fields only)

| Field | Type | Description |
|-------|------|-------------|
| m_NegligibleMail | int | Mail amount below which there is no efficiency penalty |
| m_MailEfficiencyPenalty | float | Maximum efficiency penalty multiplier for undelivered mail |

## Harmony Patch Points

### Candidate 1: `MailAccumulationSystem.MailAccumulationJob.Execute`

- **Signature**: `void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Cannot directly patch BurstCompiled IJobChunk -- use system-level approach instead
- **Alternative**: Patch `MailAccumulationSystem.OnUpdate()` to replace the job or modify PostConfigurationData singleton before the job runs
- **What it enables**: Change mail accumulation rates, thresholds, or disable accumulation entirely
- **Risk level**: Medium -- must run before job scheduling

### Candidate 2: `PostFacilityAISystem.PostFacilityTickJob.Tick`

- **Signature**: `void Tick(int jobIndex, Entity entity, ref Random random, ref PostFacility postFacility, ref MailBox mailBox, PostFacilityData prefabPostFacilityData, ...)`
- **Patch type**: Cannot directly patch (BurstCompiled) -- modify PostFacilityData prefab values or PostConfigurationData instead
- **What it enables**: Change sorting behavior, van/truck capacity, mail routing
- **Risk level**: Low (if modifying prefab data)

### Candidate 3: `PostVanAISystem.PostVanTickJob.TryHandleBuilding`

- **Signature**: `bool TryHandleBuilding(int jobIndex, Entity vehicleEntity, PostVanData prefabPostVanData, ref PostVan postVan, Entity building)`
- **Patch type**: Cannot directly patch (BurstCompiled) -- use MailActionJob approach
- **What it enables**: Modify how much mail is delivered/collected per building stop
- **Risk level**: Low

### Candidate 4: `PostVanAISystem.OnUpdate` (Prefix)

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix
- **What it enables**: Inject custom pre-processing, modify PostConfigurationData singleton before job runs
- **Risk level**: Low
- **Side effects**: Must call original for normal functionality

### Candidate 5: `PostFacilityAISystem.OnUpdate` (Postfix)

- **Signature**: `void OnUpdate()`
- **Patch type**: Postfix
- **What it enables**: Read facility state after processing, add custom post-processing logic
- **Risk level**: Low
- **Side effects**: None -- read-only postfix

## Mod Blueprint

- **Systems to create**:
  - Custom `GameSystemBase` to monitor mail statistics and service coverage
  - Optional: Custom system that runs `[UpdateAfter(typeof(MailAccumulationSystem))]` to modify accumulation results
- **Components to add**: Optional custom tag component for mod-tracked buildings
- **Patches needed**:
  - Modify PostConfigurationData singleton values (no Harmony needed -- direct ECS write)
  - Modify PostFacilityData on prefab entities for capacity changes
  - Modify MailAccumulationData on zone prefabs for rate changes
- **Settings**: Mail accumulation rate multiplier, sorting rate multiplier, van/truck capacity overrides, tolerance threshold
- **UI changes**: Optional info view overlay showing mail service coverage

## Examples

### Example 1: Modify Mail Accumulation Rate

Override the PostConfigurationData singleton to change global mail behavior. This affects all buildings without needing Harmony patches.

```csharp
public partial class MailConfigOverrideSystem : GameSystemBase
{
    private EntityQuery m_ConfigQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_ConfigQuery = GetEntityQuery(
            ComponentType.ReadWrite<PostConfigurationData>());
        RequireForUpdate(m_ConfigQuery);
    }

    protected override void OnUpdate()
    {
        // Only modify once, then disable
        Enabled = false;

        var entities = m_ConfigQuery.ToEntityArray(
            Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            var config = EntityManager.GetComponentData<PostConfigurationData>(
                entities[i]);

            // Double the max mail accumulation
            config.m_MaxMailAccumulation *= 2;

            // Raise the tolerance so vans dispatch less often
            config.m_MailAccumulationTolerance =
                (int)(config.m_MailAccumulationTolerance * 1.5f);

            EntityManager.SetComponentData(entities[i], config);
        }
        entities.Dispose();
    }
}
```

### Example 2: Monitor Mail Service Coverage

Query all MailProducer buildings to calculate city-wide mail service statistics.

```csharp
public partial class MailServiceMonitorSystem : GameSystemBase
{
    private EntityQuery m_MailProducerQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_MailProducerQuery = GetEntityQuery(
            ComponentType.ReadOnly<MailProducer>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>());
    }

    protected override void OnUpdate()
    {
        var entities = m_MailProducerQuery.ToEntityArray(
            Unity.Collections.Allocator.Temp);
        int totalBuildings = entities.Length;
        int buildingsWithMail = 0;
        int buildingsServed = 0;
        int totalSending = 0;
        int totalReceiving = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            var producer = EntityManager.GetComponentData<MailProducer>(
                entities[i]);
            totalSending += producer.m_SendingMail;
            totalReceiving += producer.receivingMail;

            if (producer.m_SendingMail > 0 || producer.receivingMail > 0)
                buildingsWithMail++;
            if (producer.mailDelivered)
                buildingsServed++;
        }

        float coveragePercent = totalBuildings > 0
            ? (float)buildingsServed / totalBuildings * 100f
            : 0f;

        Log.Info($"Mail: {totalBuildings} buildings, " +
                 $"{buildingsWithMail} with mail, " +
                 $"{buildingsServed} recently served " +
                 $"({coveragePercent:F1}% coverage), " +
                 $"sending={totalSending}, receiving={totalReceiving}");

        entities.Dispose();
    }
}
```

### Example 3: Boost Post Facility Sorting Rate

Modify the PostFacilityData prefab component on all post facility entities to increase sorting throughput.

```csharp
public partial class BoostSortingRateSystem : GameSystemBase
{
    private EntityQuery m_FacilityQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_FacilityQuery = GetEntityQuery(
            ComponentType.ReadWrite<PostFacilityData>());
        RequireForUpdate(m_FacilityQuery);
    }

    protected override void OnUpdate()
    {
        Enabled = false;

        var entities = m_FacilityQuery.ToEntityArray(
            Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            var data = EntityManager.GetComponentData<PostFacilityData>(
                entities[i]);
            if (data.m_SortingRate > 0)
            {
                // Double sorting speed and add extra van capacity
                data.m_SortingRate *= 2;
                data.m_PostVanCapacity += 2;
                EntityManager.SetComponentData(entities[i], data);
            }
        }
        entities.Dispose();
    }
}
```

### Example 4: Custom Van Dispatch Logic

Create a system that monitors post vans and forces vans back to facility when they have been out too long, preventing vans from getting stuck on long routes.

```csharp
[UpdateAfter(typeof(PostVanAISystem))]
public partial class PostVanTimeoutSystem : GameSystemBase
{
    private EntityQuery m_VanQuery;
    private const float MAX_ROUTE_TIME = 600f; // 10 minutes game time

    protected override void OnCreate()
    {
        base.OnCreate();
        m_VanQuery = GetEntityQuery(
            ComponentType.ReadWrite<Game.Vehicles.PostVan>(),
            ComponentType.ReadOnly<Owner>(),
            ComponentType.ReadOnly<PathOwner>(),
            ComponentType.ReadWrite<Target>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>());
    }

    protected override void OnUpdate()
    {
        var entities = m_VanQuery.ToEntityArray(
            Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            var van = EntityManager.GetComponentData<Game.Vehicles.PostVan>(
                entities[i]);

            // Skip vans already returning or disabled
            if ((van.m_State & (PostVanFlags.Returning | PostVanFlags.Disabled))
                != 0)
                continue;

            // Check if van has both empty delivery and full collection
            bool deliveryDone = (van.m_State & PostVanFlags.DeliveryEmpty) != 0
                || van.m_DeliveringMail <= 0;
            bool collectionDone = (van.m_State & PostVanFlags.CollectFull) != 0;

            if (deliveryDone && collectionDone)
            {
                // Force return to facility
                var owner = EntityManager.GetComponentData<Owner>(entities[i]);
                van.m_State |= PostVanFlags.Returning;
                van.m_State &= ~(PostVanFlags.Delivering
                                | PostVanFlags.Collecting);
                van.m_RequestCount = 0;
                EntityManager.SetComponentData(entities[i], van);

                // Re-target to facility
                EntityManager.SetComponentData(entities[i],
                    new Target(owner.m_Owner));

                // Request new path
                var pathOwner = EntityManager.GetComponentData<PathOwner>(
                    entities[i]);
                pathOwner.m_State |= PathFlags.Updated;
                EntityManager.SetComponentData(entities[i], pathOwner);

                // Clear service dispatches
                if (EntityManager.HasBuffer<ServiceDispatch>(entities[i]))
                {
                    EntityManager.GetBuffer<ServiceDispatch>(entities[i])
                        .Clear();
                }
            }
        }
        entities.Dispose();
    }
}
```

### Example 5: Read Facility Mail Inventory

Query post facilities to report their current mail inventory breakdown by resource type.

```csharp
public partial class FacilityInventorySystem : GameSystemBase
{
    private EntityQuery m_FacilityQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_FacilityQuery = GetEntityQuery(
            ComponentType.ReadOnly<Game.Buildings.PostFacility>(),
            ComponentType.ReadOnly<Game.Economy.Resources>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>());
    }

    protected override void OnUpdate()
    {
        var entities = m_FacilityQuery.ToEntityArray(
            Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            var facility = EntityManager.GetComponentData<
                Game.Buildings.PostFacility>(entities[i]);
            var resources = EntityManager.GetBuffer<
                Game.Economy.Resources>(entities[i]);

            int unsorted = EconomyUtils.GetResources(
                Resource.UnsortedMail, resources);
            int local = EconomyUtils.GetResources(
                Resource.LocalMail, resources);
            int outgoing = EconomyUtils.GetResources(
                Resource.OutgoingMail, resources);

            Log.Info($"Facility {entities[i].Index}: " +
                     $"unsorted={unsorted}, local={local}, " +
                     $"outgoing={outgoing}, " +
                     $"processing={facility.m_ProcessingFactor}%, " +
                     $"flags={facility.m_Flags}");
        }
        entities.Dispose();
    }
}
```

## Open Questions

- [ ] **Exact mail accumulation rate values**: The `MailAccumulationData.m_AccumulationRate` float2 is set per zone type in prefab data. The actual values for residential, commercial, industrial, and office zones were not extracted from game data files.
- [ ] **Outside connection mail**: Whether `Resource.OutgoingMail` is actually exported through outside connections or if it simply disappears. The MailTransferDispatchSystem references export/import purposes but the final destination is unclear.
- [ ] **MailBox entity creation**: How streetside mailbox entities (Game.Routes.MailBox) are placed -- whether they are part of road prefabs, building prefabs, or a separate placement system.
- [ ] **PostVanSelectData internals**: The vehicle selection and creation helper used by PostFacilityAISystem. Its internal vehicle matching logic was not fully decompiled.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II) using ilspycmd v9.1
- Game version tested: CS2 as of 2026-02-17
- Systems decompiled: MailAccumulationSystem, MailBoxSystem, PostVanDispatchSystem, PostVanAISystem, PostFacilityAISystem, MailTransferDispatchSystem
- Components decompiled: MailProducer, PostFacility, PostFacilityFlags, MailBox, PostVan, PostVanFlags, PostVanRequest, PostVanRequestFlags, MailTransferRequest, MailTransferRequestFlags, PostConfigurationData, PostFacilityData, MailBoxData, PostVanData, MailAccumulationData
