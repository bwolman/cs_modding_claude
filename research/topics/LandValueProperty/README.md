# Research: Land Value & Property Market

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 calculates land value across the map, determines rent prices for buildings, processes rent payments, and drives building level-up/level-down mechanics.

**Why**: Understanding the land value and property market pipeline is essential for mods that adjust rent prices, modify land value factors, change building leveling thresholds, or create custom economic rebalancing systems.

**Boundaries**: Company profitability and resource production are documented in `research/topics/CompanySimulation/` and `research/topics/ResourceProduction/` respectively. Household happiness and citizen lifecycle are in `research/topics/CitizensHouseholds/`. This document focuses on the land value calculation, rent pricing, rent payment, and building condition/leveling pipeline.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Net | LandValue component (per-edge land value) |
| Game.dll | Game.Buildings | PropertyRenter, Renter, RentAction, RentersUpdated, PropertyOnMarket, BuildingCondition, PropertyUtils |
| Game.dll | Game.Simulation | LandValueSystem, PropertyRenterSystem, RentAdjustSystem, BuildingUpkeepSystem, PropertyRenterRemoveSystem, LandValueCell |
| Game.dll | Game.Prefabs | LandValueParameterData, LandValuePrefab, BuildingPropertyData, BuildingConfigurationData, ZoneLevelUpResourceData, SignatureBuildingData, EconomyParameterData, SpawnableBuildingData, ZoneData |
| Game.dll | Game.Vehicles | DeliveryTruck, DeliveryTruckFlags |

## Component Map

### `LandValue` (Game.Net)

Per-edge component attached to road network edges. Stores the computed land value for that road segment.

| Field | Type | Description |
|-------|------|-------------|
| m_LandValue | float | Current land value at this edge, driven by services, attractiveness, pollution |
| m_Weight | float | Weighting factor (used in serialization, not actively consumed by systems) |

*Source: `Game.dll` -> `Game.Net.LandValue`*

### `LandValueCell` (Game.Simulation)

Cell in a 128x128 grid covering the entire map. Updated by LandValueMapUpdateJob from nearby edge values and environmental factors.

| Field | Type | Description |
|-------|------|-------------|
| m_LandValue | float | Interpolated land value for this cell. Combines edge averages with terrain attractiveness, telecom, and pollution penalties. |

*Source: `Game.dll` -> `Game.Simulation.LandValueCell`*

### `PropertyRenter` (Game.Buildings)

Attached to households and companies that rent a property (building unit).

| Field | Type | Description |
|-------|------|-------------|
| m_Property | Entity | The building entity this renter occupies |
| m_Rent | int | Current rent amount per day this renter pays |

*Source: `Game.dll` -> `Game.Buildings.PropertyRenter`*

### `Renter` (Game.Buildings)

Dynamic buffer on building entities listing all renters occupying the building.

| Field | Type | Description |
|-------|------|-------------|
| m_Renter | Entity | Entity of a household or company renting this building |

*Source: `Game.dll` -> `Game.Buildings.Renter`*

### `RentAction` (Game.Buildings)

Struct used in queues for pending rent actions (move-in events).

| Field | Type | Description |
|-------|------|-------------|
| m_Property | Entity | The building to move into |
| m_Renter | Entity | The household/company moving in |
| m_Flags | RentActionFlags | Storage (1) = storage company, Remove (2) = eviction |

*Source: `Game.dll` -> `Game.Buildings.RentAction`*

### `RentersUpdated` (Game.Buildings)

Tag component added as an event entity when renters change on a building.

| Field | Type | Description |
|-------|------|-------------|
| m_Property | Entity | The building whose renters list changed |

*Source: `Game.dll` -> `Game.Buildings.RentersUpdated`*

### `PropertyOnMarket` (Game.Buildings)

Added to buildings with vacant units. Removed when fully occupied.

| Field | Type | Description |
|-------|------|-------------|
| m_AskingRent | int | The rent price being asked for vacant units |

*Source: `Game.dll` -> `Game.Buildings.PropertyOnMarket`*

### `BuildingCondition` (Game.Buildings)

Tracks a building's cumulative maintenance condition, driving level-up and level-down.

| Field | Type | Description |
|-------|------|-------------|
| m_Condition | int | Accumulates positive (well-maintained) or negative (neglected) value. Triggers level-up when >= levelingCost, level-down (abandonment) when <= -abandonCost. |

*Source: `Game.dll` -> `Game.Buildings.BuildingCondition`*

### `BuildingPropertyData` (Game.Prefabs)

Prefab component defining what a building can hold.

| Field | Type | Description |
|-------|------|-------------|
| m_ResidentialProperties | int | Number of residential units (apartments) |
| m_AllowedSold | Resource | Resource types this building can sell (commercial) |
| m_AllowedInput | Resource | Resource types allowed as inputs |
| m_AllowedManufactured | Resource | Resource types this building can manufacture (industrial) |
| m_AllowedStored | Resource | Resource types this building can store |
| m_SpaceMultiplier | float | Multiplier for rent calculation -- larger buildings have higher multiplier |

*Source: `Game.dll` -> `Game.Prefabs.BuildingPropertyData`*

### `BuildingConfigurationData` (Game.Prefabs)

Global singleton controlling building condition and level-up configuration.

| Field | Type | Description |
|-------|------|-------------|
| m_BuildingConditionDecrement | int | Base condition decrement per tick when building cannot afford upkeep |
| m_AbandonedNotification | Entity | Notification prefab entity displayed when a building is abandoned |
| m_HighRentNotification | Entity | Notification prefab entity displayed when >70% of renters overpay |

*Source: `Game.dll` -> `Game.Prefabs.BuildingConfigurationData`*

### `ZoneLevelUpResourceData` (Game.Prefabs, buffer)

Buffer element on zone prefab entities defining per-level resource requirements for building level-ups.

| Field | Type | Description |
|-------|------|-------------|
| m_Level | int | The building level this entry applies to |
| m_LevelUpResource | Resource | The resource type required to level up to this level |

Each zone type prefab (residential, commercial, industrial) has a buffer of these elements specifying which resources must be delivered at each level threshold. The `ResourceNeedingUpkeepJob` reads these entries to populate the `ResourceNeeding` buffer on buildings that have crossed the `levelingCost` condition threshold.

*Source: `Game.dll` -> `Game.Prefabs.ZoneLevelUpResourceData`*

### `LandValueParameterData` (Game.Prefabs)

Global singleton controlling land value computation factors.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_LandValueInfoViewPrefab | Entity | - | Reference to infoview prefab |
| m_LandValueBaseline | float | 10.0 | Minimum land value floor |
| m_HealthCoverageBonusMultiplier | float | 5.0 | Health service coverage bonus weight |
| m_EducationCoverageBonusMultiplier | float | 5.0 | Education service coverage bonus weight |
| m_PoliceCoverageBonusMultiplier | float | 5.0 | Police service coverage bonus weight |
| m_AttractivenessBonusMultiplier | float | 3.0 | Terrain and tourism attractiveness bonus weight |
| m_TelecomCoverageBonusMultiplier | float | 20.0 | Telecom/internet coverage bonus weight |
| m_CommercialServiceBonusMultiplier | float | 10.0 | Nearby commercial services bonus weight |
| m_TramSubwayBonusMultiplier | float | 50.0 | Tram/subway transit access bonus weight |
| m_BusBonusMultiplier | float | 5.0 | Bus transit access bonus weight |
| m_CommonFactorMaxBonus | float | 100.0 | Cap on bonus from any single common factor |
| m_GroundPollutionPenaltyMultiplier | float | 10.0 | Ground pollution penalty weight |
| m_AirPollutionPenaltyMultiplier | float | 5.0 | Air pollution penalty weight |
| m_NoisePollutionPenaltyMultiplier | float | 0.01 | Noise pollution penalty weight |

*Source: `Game.dll` -> `Game.Prefabs.LandValueParameterData`, defaults from `Game.Prefabs.LandValuePrefab`*

## System Map

### `LandValueSystem` (Game.Simulation)

- **Base class**: CellMapSystem<LandValueCell>
- **Update phase**: Simulation
- **Update rate**: 32x per day (262144 / 32 = 8192 frames between updates)
- **Cell map**: 128x128 grid (kTextureSize = 128)
- **Queries**:
  - EdgeQuery: Edge + LandValue + Curve, excluding Deleted/Temp
- **Jobs**:
  - `EdgeUpdateJob` (IJobChunk): For each road edge, sums service coverage bonuses (health, education, police) and resource availability bonuses (commercial, bus, tram/subway), multiplied by their respective parameter multipliers. Lerps toward new value at rate 0.6.
  - `LandValueMapUpdateJob` (IJobParallelFor): For each cell in 128x128 grid, samples nearby edge land values (within 1.5 cell widths), terrain attractiveness, telecom coverage, and subtracts pollution penalties. Underwater cells get baseline value. Lerps toward target at rate 0.4.

### `RentAdjustSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update rate**: 16x per day, 16 groups = 262144 / (16 * 16) = 1024 frames
- **Queries**:
  - Building + UpdateFrame + Renter, excluding StorageProperty/Temp/Deleted
- **Key logic** (AdjustRentJob):
  1. Reads land value from Building.m_RoadEdge -> LandValue.m_LandValue
  2. Calls `PropertyUtils.GetRentPricePerRenter()` to compute market rent
  3. Updates PropertyRenter.m_Rent and PropertyOnMarket.m_AskingRent
  4. Compares rent + garbage fee to renter income (household income or company profit)
  5. If rent > income: enables PropertySeeker (renter starts looking for cheaper property)
  6. If rent < income/2 (households only): also enables PropertySeeker (can afford better)
  7. If >70% of renters overpay: shows high rent warning icon on building
  8. Processes pollution notifications for residential buildings

### `PropertyRenterSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update rate**: 16x per day, 16 groups = 262144 / (16 * 16) = 1024 frames
- **Queries**:
  - Building + Renter + UpdateFrame with BuildingCondition
- **Key logic** (PayRentJob):
  1. Deducts rent from renter's Money resource: `rent * 1.0 / kUpdatesPerDay`
  2. Storage companies pay all their money as rent
  3. Deducts garbage fee proportionally among renters
  4. Reports garbage fees to ServiceFeeSystem
  5. Removes invalid renters (entities without PropertyRenter)
  6. If building has vacant units and not on market: adds PropertyToBeOnMarket
  7. Evicts renters from abandoned/destroyed buildings
- **RenterMovingAwayJob**: Removes PropertyRenter from households marked MovingAway

### `BuildingUpkeepSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update rate**: 16x per day, 16 groups
- **Jobs**:
  - `BuildingUpkeepJob`: Evaluates building condition change per tick
    - Computes upkeep cost from ConsumptionData.m_Upkeep / kUpdatesPerDay
    - Splits upkeep into material (1/4) and monetary (3/4) portions
    - If total renter wealth < monetary upkeep: condition decreases (building deteriorates)
    - If renters can pay: condition increases, upkeep deducted from renters
    - **Condition scaling by building level**: The change magnitude is `conditionChange * pow(2, level) * max(1, renterCount)`, where `conditionChange` comes from `BuildingUtils.GetBuildingConditionChange()`. This exponential scaling means a level-3 building accumulates condition 8x faster than a level-1 building. This applies in both directions -- well-funded high-level buildings level up faster, but underfunded ones deteriorate and abandon faster too. The `levelingCost` and `abandonCost` thresholds also scale with building properties via `BuildingUtils.GetLevelingCost()` and `BuildingUtils.GetAbandonCost()`, which factor in area type, lot size, and current level.
    - If condition >= levelingCost: queues level-up (requests resource delivery)
    - If condition <= -abandonCost: queues level-down (abandonment)
  - `ResourceNeedingUpkeepJob`: Checks if all level-up materials were delivered; if so, queues actual level-up
  - `LevelupJob`: Selects a higher-level building prefab with matching zone type, lot size, and access flags; initiates UnderConstruction
  - `LeveldownJob`: Full abandonment pipeline:
    1. Removes utility consumers (electricity, water, garbage, mail, telecom)
    2. Doubles crime production: `CrimeProducer.m_Crime *= 2f` (if building has CrimeProducer)
    3. Clears `BuildingFlags.HighRentWarning` and removes `m_HighRentNotification` icon
    4. Removes all problem/fatal-problem icons, adds `m_AbandonedNotification` at `IconPriority.FatalProblem`
    5. Evicts all renters (iterates `Renter` buffer in reverse, removes `PropertyRenter` from each)
    6. Enqueues utility road edge updates (`m_UpdatedElectricityRoadEdges`, `m_UpdatedWaterPipeRoadEdges`)
    7. Emits trigger actions: `TriggerType.LevelDownCommercialBuilding`, `TriggerType.LevelDownIndustrialBuilding` (if not office), or `TriggerType.LevelDownOfficeBuilding` (if `OfficeBuilding` component present). Note: there is no `TriggerType.LevelDownResidentialBuilding`.
    8. Adds `Abandoned` component with `m_AbandonmentTime = currentSimulationFrame`
  - `UpkeepPaymentJob`: Actually deducts money from renters

### BuildingUpkeepSystem Deep Dive

`Game.Simulation.BuildingUpkeepSystem` is a large system (~103KB decompiled) handling building condition, level-up/level-down, resource consumption, and upkeep payments. It operates as a scheduled update:

**Responsibilities**:
1. **Building upkeep payments** -- deducting maintenance costs from building resources
2. **Level-up logic** -- buildings upgrade when conditions are met (land value, services, etc.)
3. **Level-down logic** -- buildings downgrade when conditions deteriorate
4. **Resource delivery** -- requesting electricity, water, and material deliveries
5. **Level-up material tracking** -- buildings need specific resources to level up

**Key System Dependencies**:
| System | Role |
|--------|------|
| `SimulationSystem` | Frame timing |
| `ClimateSystem` | Heating multiplier for resource consumption |
| `ResourceSystem` | Resource availability |
| `CitySystem` | City-level state |
| `IconCommandSystem` | Level-up/down notification icons |
| `TriggerSystem` | Event triggers |
| `ZoneBuiltRequirementSystem` | Zone completion checks |
| `ZoneSearchSystem` | Zone lookup |
| `ElectricityRoadConnectionGraphSystem` | Power grid connectivity |
| `WaterPipeRoadConnectionGraphSystem` | Water network connectivity |
| `CityProductionStatisticSystem` | Production stats |

**Key Components Read/Written**:
- `BuildingCondition` (Game.Buildings) -- read/write for condition tracking
- `Efficiency` (Game.Buildings, IBufferElementData) -- read/write for building efficiency factors
- `ResourceNeeding` (Game.Buildings) -- write to request level-up materials
- `GoodsDeliveryRequest` (Game.Simulation) -- write to trigger material delivery trucks

**Modding Note**: The tight coupling of BuildingUpkeepSystem means it cannot be easily patched with Harmony -- community mods (RealisticWorkplacesAndHouseholds, UrbanInequality) fully replace it with `Enabled = false` and a custom system.

*Source: RealisticWorkplacesAndHouseholds mod*

### `PropertyRenterRemoveSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update interval**: 256 frames
- **Key logic**: Validates renter-building relationships. Removes PropertyRenter from entities whose building no longer exists or has more renters than capacity.

## Data Flow

```
LAND VALUE CALCULATION (32x per day)
  LandValueSystem.EdgeUpdateJob
    For each road edge with LandValue:
      sum = health_coverage * 5 + education_coverage * 5 + police_coverage * 5
            + commercial_availability * 10 + bus_availability * 5 + tram_subway * 50
      LandValue.m_LandValue = lerp(current, sum, 0.6)
          |
          v
  LandValueSystem.LandValueMapUpdateJob
    For each cell in 128x128 grid:
      avg_edge_value = average nearby edge LandValues
      bonus = attractiveness * 3 + telecom * 20 + terrain_attractiveness * 3
              (each capped at CommonFactorMaxBonus = 100)
      penalty = ground_pollution * 10 + air_pollution * 5 + noise * 0.01
      target = max(baseline, baseline + avg_edge_value + bonus - penalty)
      LandValueCell.m_LandValue = lerp(current, target, 0.4)
          |
          v
RENT CALCULATION (16x per day)
  RentAdjustSystem.AdjustRentJob
    For each building with renters:
      landValue = Building.m_RoadEdge -> LandValue.m_LandValue
      rent = PropertyUtils.GetRentPricePerRenter(...)
           = (landValue * zoneModifier + baseRate * level) * lotSize * spaceMultiplier / renterCount
      PropertyRenter.m_Rent = rent
      PropertyOnMarket.m_AskingRent = rent
      If rent > renter income -> enable PropertySeeker (look for cheaper)
      If rent < income/2 (household) -> enable PropertySeeker (can upgrade)
      If >70% overpaying -> high rent warning
          |
          v
RENT PAYMENT (16x per day)
  PropertyRenterSystem.PayRentJob
    For each building with renters:
      renter.Money -= rent / kUpdatesPerDay
      renter.Money -= garbageFee / renterCount / kUpdatesPerDay
      Remove invalid renters, add vacant buildings to market
          |
          v
BUILDING CONDITION (16x per day)
  BuildingUpkeepSystem.BuildingUpkeepJob
    upkeep = ConsumptionData.m_Upkeep / 16
    monetary = upkeep * 3/4
    levelScale = pow(2, level) * max(1, renters)   // Level-3 = 8x, Level-5 = 32x
    If renterWealth >= monetary:
      condition += conditionChange * levelScale
      Deduct upkeep from renters
    Else:
      condition -= conditionChange * levelScale
    NOTE: exponential scaling means high-level buildings level up faster
          when well-funded, but also abandon faster when underfunded
          |
          v
    condition >= levelingCost  -->  Request level-up materials
                                    Materials delivered --> LevelupJob
                                    Select higher-level prefab
                                    Start UnderConstruction
          |
    condition <= -abandonCost  -->  LeveldownJob
                                    Remove utility consumers
                                    Double CrimeProducer.m_Crime
                                    Clear HighRentWarning flag + icon
                                    Evict all renters (reverse iterate)
                                    Add Abandoned{m_AbandonmentTime}
                                    Add m_AbandonedNotification icon
```

## Prefab & Configuration

| Value | Source | Default |
|-------|--------|---------|
| Land value baseline | LandValueParameterData.m_LandValueBaseline | 10.0 |
| Health coverage bonus multiplier | LandValueParameterData.m_HealthCoverageBonusMultiplier | 5.0 |
| Education coverage bonus multiplier | LandValueParameterData.m_EducationCoverageBonusMultiplier | 5.0 |
| Police coverage bonus multiplier | LandValueParameterData.m_PoliceCoverageBonusMultiplier | 5.0 |
| Attractiveness bonus multiplier | LandValueParameterData.m_AttractivenessBonusMultiplier | 3.0 |
| Telecom coverage bonus multiplier | LandValueParameterData.m_TelecomCoverageBonusMultiplier | 20.0 |
| Commercial service bonus multiplier | LandValueParameterData.m_CommercialServiceBonusMultiplier | 10.0 |
| Bus bonus multiplier | LandValueParameterData.m_BusBonusMultiplier | 5.0 |
| Tram/subway bonus multiplier | LandValueParameterData.m_TramSubwayBonusMultiplier | 50.0 |
| Common factor max bonus | LandValueParameterData.m_CommonFactorMaxBonus | 100 |
| Ground pollution penalty | LandValueParameterData.m_GroundPollutionPenaltyMultiplier | 10.0 |
| Air pollution penalty | LandValueParameterData.m_AirPollutionPenaltyMultiplier | 5.0 |
| Noise pollution penalty | LandValueParameterData.m_NoisePollutionPenaltyMultiplier | 0.01 |
| Residential rent base rate | EconomyParameterData.m_RentPriceBuildingZoneTypeBase.x | (from prefab) |
| Commercial rent base rate | EconomyParameterData.m_RentPriceBuildingZoneTypeBase.y | (from prefab) |
| Industrial rent base rate | EconomyParameterData.m_RentPriceBuildingZoneTypeBase.z | (from prefab) |
| Land value modifier (R/C/I) | EconomyParameterData.m_LandValueModifier | (float3) |
| Upkeep cost | ConsumptionData.m_Upkeep | Varies by prefab |
| Updates per day (rent/upkeep) | kUpdatesPerDay | 16 |

## Harmony Patch Points

### Candidate 1: `Game.Buildings.PropertyUtils.GetRentPricePerRenter`

- **Signature**: `static int GetRentPricePerRenter(BuildingPropertyData, int buildingLevel, int lotSize, float landValueBase, AreaType areaType, ref EconomyParameterData, bool ignoreLandValue)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override rent prices with custom formulas. A postfix can modify the result; a prefix can replace the entire calculation.
- **Risk level**: Low -- static utility method called from AdjustRentJob
- **Side effects**: Changes rent for all buildings. Must consider that this runs in Burst-compiled jobs (patch on managed wrapper only).

### Candidate 2: `Game.Simulation.LandValueSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (skip) or Postfix (modify results)
- **What it enables**: Disable or modify land value updates entirely. A postfix could modify the cell map data after the jobs complete.
- **Risk level**: Medium -- affects the entire land value grid
- **Side effects**: Skipping updates freezes land values. Must ensure job handles are properly managed.

### Candidate 3: `Game.Simulation.BuildingUpkeepSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify building condition change rates, level-up/level-down thresholds
- **Risk level**: Medium -- affects all building leveling
- **Side effects**: Could prevent buildings from ever leveling up or down

### Candidate 4: `Game.Buildings.BuildingUtils.GetLevelingCost`

- **Signature**: `static int GetLevelingCost(AreaType areaType, BuildingPropertyData propertyData, int level, ref CityModifier cityModifier)`
- **Patch type**: Postfix
- **What it enables**: Override the condition threshold required for a building to level up. A postfix can scale the cost per zone type or level to make leveling easier or harder.
- **Risk level**: Low -- static utility method, no side effects
- **Side effects**: Lowering the cost accelerates leveling city-wide; raising it slows leveling and may cause condition overflow at extreme values

### Candidate 5: `Game.Buildings.BuildingUtils.GetAbandonCost`

- **Signature**: `static int GetAbandonCost(AreaType areaType, BuildingPropertyData propertyData, int level, ref CityModifier cityModifier)`
- **Patch type**: Postfix
- **What it enables**: Override the negative condition threshold that triggers building abandonment. Raising the cost makes buildings more resilient to neglect; lowering it makes them abandon faster.
- **Risk level**: Low -- static utility method, no side effects
- **Side effects**: Extreme values can prevent abandonment entirely or cause instant abandonment

### Candidate 6: `Game.Buildings.BuildingUtils.GetBuildingConditionChange`

- **Signature**: `static int GetBuildingConditionChange(BuildingConfigurationData configData, ...)`
- **Patch type**: Postfix
- **What it enables**: Override the per-tick condition increment/decrement rate. Controls the base speed of building condition accumulation in both positive and negative directions.
- **Risk level**: Low -- static utility method
- **Side effects**: Changes affect all buildings uniformly. Combined with the exponential level scaling (`pow(2, level)`), even small changes to the base rate have amplified effects at higher building levels.

### Candidate 7: `Game.Simulation.RentAdjustSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override the PropertySeeker enable/disable logic, modify high rent warning thresholds
- **Risk level**: Medium -- affects renter movement decisions
- **Side effects**: May impact household/company relocation patterns

## Examples

### Example 1: Read Land Value at a World Position

Read the land value from the 128x128 cell map at any world position.

```csharp
public partial class LandValueReaderSystem : GameSystemBase
{
    private LandValueSystem _landValueSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _landValueSystem = World.GetOrCreateSystemManaged<LandValueSystem>();
    }

    protected override void OnUpdate()
    {
        // Sample land value at a specific world position
        float3 worldPos = new float3(500f, 0f, 300f);
        int cellIndex = LandValueSystem.GetCellIndex(worldPos);

        JobHandle deps;
        NativeArray<LandValueCell> map = _landValueSystem.GetMap(
            readOnly: true, out deps);
        deps.Complete();

        if (cellIndex >= 0 && cellIndex < map.Length)
        {
            float landValue = map[cellIndex].m_LandValue;
            Log.Info($"Land value at {worldPos}: {landValue:F1}");
        }

        _landValueSystem.AddReader(Dependency);
    }
}
```

### Example 2: Modify Land Value Parameters

Adjust the global land value parameters to rebalance the economy.

```csharp
public void AdjustLandValueParameters(EntityManager em)
{
    EntityQuery paramQuery = em.CreateEntityQuery(
        ComponentType.ReadWrite<LandValueParameterData>()
    );

    if (paramQuery.CalculateEntityCount() == 0) return;

    Entity paramEntity = paramQuery.GetSingletonEntity();
    LandValueParameterData data = em.GetComponentData<LandValueParameterData>(paramEntity);

    // Double the transit bonus to make public transit more impactful
    data.m_TramSubwayBonusMultiplier = 100f;
    data.m_BusBonusMultiplier = 10f;

    // Reduce pollution penalties for more forgiving gameplay
    data.m_GroundPollutionPenaltyMultiplier = 5f;
    data.m_AirPollutionPenaltyMultiplier = 2.5f;

    em.SetComponentData(paramEntity, data);
    paramQuery.Dispose();
}
```

### Example 3: Monitor Building Rent vs Income

Query all buildings with renters to find properties where rent exceeds income.

```csharp
public partial class RentMonitorSystem : GameSystemBase
{
    private EntityQuery _buildingQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _buildingQuery = GetEntityQuery(
            ComponentType.ReadOnly<Building>(),
            ComponentType.ReadOnly<Renter>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _buildingQuery.ToEntityArray(Allocator.Temp);
        int highRentCount = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity building = entities[i];
            var renters = EntityManager.GetBuffer<Renter>(building, isReadOnly: true);

            for (int j = 0; j < renters.Length; j++)
            {
                Entity renter = renters[j].m_Renter;
                if (EntityManager.HasComponent<PropertyRenter>(renter))
                {
                    PropertyRenter pr = EntityManager.GetComponentData<PropertyRenter>(renter);
                    int money = EconomyUtils.GetResources(
                        Resource.Money,
                        EntityManager.GetBuffer<Game.Economy.Resources>(renter, isReadOnly: true));

                    if (pr.m_Rent > money)
                        highRentCount++;
                }
            }
        }

        if (highRentCount > 0)
            Log.Info($"Renters with rent exceeding savings: {highRentCount}");

        entities.Dispose();
    }
}
```

### Example 4: Override Rent Price via Harmony Postfix

Patch the rent calculation to apply a custom multiplier.

```csharp
[HarmonyPatch(typeof(Game.Buildings.PropertyUtils), nameof(PropertyUtils.GetRentPricePerRenter))]
public static class RentPricePatch
{
    public static void Postfix(ref int __result, int buildingLevel, AreaType areaType)
    {
        // Apply a 50% discount to residential rent
        if (areaType == AreaType.Residential)
        {
            __result = Mathf.RoundToInt(__result * 0.5f);
        }

        // Cap industrial rent at level-based maximum
        if (areaType == AreaType.Industrial)
        {
            int maxRent = buildingLevel * 500;
            __result = Mathf.Min(__result, maxRent);
        }
    }
}
```

### Example 5: Read Edge Land Value for a Building

Look up the land value at a specific building's road edge.

```csharp
public float GetBuildingLandValue(EntityManager em, Entity building)
{
    if (!em.HasComponent<Building>(building))
        return 0f;

    Building buildingData = em.GetComponentData<Building>(building);
    Entity roadEdge = buildingData.m_RoadEdge;

    if (roadEdge == Entity.Null || !em.HasComponent<LandValue>(roadEdge))
        return 0f;

    LandValue landValue = em.GetComponentData<LandValue>(roadEdge);
    return landValue.m_LandValue;
}
```

### Example 4: Remove Abandonment and Restore Building

The reverse of `LeveldownJob` — programmatically removes abandonment and restores a building to functional state.

```csharp
public void RemoveAbandonment(EntityManager em, Entity buildingEntity)
{
    // 1. Remove abandoned status
    em.RemoveComponent<Abandoned>(buildingEntity);

    // 2. Reset market listing (PropertyToBeOnMarket triggers re-listing)
    em.AddComponent<PropertyToBeOnMarket>(buildingEntity);
    if (em.HasComponent<PropertyOnMarket>(buildingEntity))
        em.RemoveComponent<PropertyOnMarket>(buildingEntity);

    // 3. Reset building condition to zero
    em.SetComponentData(buildingEntity, new BuildingCondition { m_Condition = 0 });

    // 4. Restore utility consumers removed during abandonment
    em.AddComponentData(buildingEntity, default(GarbageProducer));
    em.AddComponentData(buildingEntity, default(MailProducer));
    em.AddComponentData(buildingEntity, default(ElectricityConsumer));
    em.AddComponentData(buildingEntity, default(WaterConsumer));

    // 5. Remove abandoned notification icon
    // (requires IconCommandBuffer from BuildingConfigurationData)
}
```

Key components to restore: `GarbageProducer`, `MailProducer`, `ElectricityConsumer`, `WaterConsumer`. The `PropertyToBeOnMarket` tag triggers the property market system to re-list the building. `BuildingCondition.m_Condition = 0` gives the building a neutral starting state.

## Advanced Patterns

### Education/Income-Based Building Leveling Penalty

A mod can compute custom leveling penalty multipliers by reading per-building citizen data. The pattern traverses from building to individual citizen attributes:

1. Read the `Renter` buffer on the building entity to get all renter entities
2. For each renter (household), read the `HouseholdCitizen` buffer to get citizen entities
3. For each citizen, call `Citizen.GetEducationLevel()` and `Citizen.GetAge()` to read education and demographics
4. Read the renter's `Resources` buffer and call `EconomyUtils.GetResources(Resource.Money)` to get household wealth
5. Aggregate education levels and income across all building occupants to compute a custom penalty multiplier

This enables mods like UrbanInequality that penalize or boost leveling speed based on the actual education profile and income distribution of a building's residents, rather than using the vanilla flat condition scaling.

### Building Level Cap Distribution

Query buildings to enforce level distribution caps or gate level-ups by zone type:

1. Query entities with `BuildingCondition` + `PrefabRef` + `UpdateFrame` (matching the vanilla update frame pattern)
2. Read `SpawnableBuildingData.m_Level` from the prefab to get the current building level
3. Read `ZoneData.m_AreaType` from the zone prefab to categorize by residential/commercial/industrial
4. Count buildings per level per zone type to build a distribution histogram
5. Gate level-ups by checking if the target level's count exceeds a configured cap before allowing `BuildingCondition` to cross the `levelingCost` threshold

This pattern allows mods to enforce realistic level distributions (e.g., limiting the number of level-5 buildings city-wide) rather than allowing unrestricted leveling.

### BuildingUpkeepSystem as Replacement Target

Both the RealisticWorkplaceHouseholds and UrbanInequality community mods independently replace `BuildingUpkeepSystem` with custom implementations. This confirms `BuildingUpkeepSystem` as a high-value system replacement target for mods that need to alter building condition logic, leveling thresholds, or abandonment behavior. Replacing rather than patching is preferred here because the system's inner jobs (`BuildingUpkeepJob`, `ResourceNeedingUpkeepJob`, `LevelupJob`, `LeveldownJob`) are Burst-compiled and cannot be directly Harmony-patched.

### Level-Up Material Delivery Pipeline

When `BuildingCondition.m_Condition` crosses the `levelingCost` threshold, the system initiates a material delivery request before the actual level-up occurs:

1. **`ResourceNeeding`** (buffer on building entity): Each element defines a required resource delivery.
   - `m_Resource` (Resource): The resource type needed
   - `m_Amount` (int): The amount required
   - `m_Flags` (ResourceNeedingFlags): Tracks delivery state -- `Requested` when a delivery truck has been dispatched, `Delivered` when the resource has arrived

2. **`GoodsDeliveryRequest`** (component): Created to dispatch a delivery truck for the needed resource.
   - `m_ResourceNeeder` (Entity): The building entity requesting the resource
   - `m_Amount` (int): The amount to deliver
   - `m_Resource` (Resource): The resource type to deliver

3. **Delivery-complete check flow**: `ResourceNeedingUpkeepJob` iterates the `ResourceNeeding` buffer and checks if all elements have the `Delivered` flag set. Only when every required resource has been delivered does the system queue the actual `LevelupJob` to select a higher-level prefab and begin construction.

### Signature Building Immunity

`SignatureBuildingData` (Game.Prefabs) acts as an immunity flag for signature and landmark buildings. When `BuildingUpkeepJob` processes a building, it checks for `SignatureBuildingData` on the building's prefab. Buildings with this component skip the level-down and abandonment checks entirely -- they never accumulate negative condition or receive the `Abandoned` component regardless of renter wealth or upkeep shortfalls.

### Delivery Truck Upkeep State

The `Game.Vehicles.DeliveryTruck` component tracks delivery truck state including upkeep deliveries:

| Field | Type | Description |
|-------|------|-------------|
| m_State | DeliveryTruckFlags | Current truck state flags |
| m_Resource | Resource | Resource being transported |
| m_Amount | int | Amount being transported |

The `DeliveryTruckFlags.UpkeepDelivery` flag identifies trucks delivering upkeep materials for building level-ups (as opposed to commercial/industrial resource deliveries). When a delivery request becomes stale (e.g., the target building was demolished or abandoned before delivery), the system performs cleanup by removing the `GoodsDeliveryRequest` component and resetting the corresponding `ResourceNeeding` entry's `Requested` flag, freeing the truck for reassignment.

### Zone Type Detection and Tenant WorkProvider Access

`BuildingPropertyData.CountProperties(AreaType)` returns the number of properties (units) for a given zone type. This is the standard method for determining what zone type a building serves -- it checks the residential, commercial, and industrial property counts internally.

To find `WorkProvider` components on building tenants, iterate the `Renter` buffer on the building entity and check each renter for the `WorkProvider` component:

```csharp
DynamicBuffer<Renter> renters = EntityManager.GetBuffer<Renter>(building, isReadOnly: true);
for (int i = 0; i < renters.Length; i++)
{
    Entity renter = renters[i].m_Renter;
    if (EntityManager.HasComponent<WorkProvider>(renter))
    {
        WorkProvider wp = EntityManager.GetComponentData<WorkProvider>(renter);
        // Access wp.m_MaxWorkers, etc.
    }
}
```

This pattern is used by systems that need to aggregate employment data across all companies in a building (e.g., for computing building-level efficiency or worker density).

## Mod Blueprint: Building Leveling/Inequality Rebalancing

A building leveling mod modifies how buildings accumulate condition, level up, and interact with the economy. This archetype enables income inequality simulation, education-based leveling penalties, and configurable city presets. Based on analysis of the [UrbanInequality](https://github.com/ruzbeh0/UrbanInequality) mod.

### Systems to Create

1. **Custom BuildingUpkeepSystem replacement** -- disables vanilla `BuildingUpkeepSystem` (`Enabled = false`) and registers a replacement that modifies condition accumulation based on custom factors (education profile, income distribution)
2. **ResidentialLevelCapSystem** -- counts residential buildings per level across the city and enforces configurable level distribution caps (e.g., limit level-5 buildings to a percentage of total)
3. **CommercialLevelCapSystem** -- same pattern for commercial/industrial buildings with separate cap parameters
4. **Wage override system** -- queries `EconomyParameterData` singleton at runtime to override wage tables, creating income differentiation by education level

### Components to Create

- No new custom ECS components required -- this archetype primarily operates by modifying existing prefab singletons (`EconomyParameterData`) and replacing the `BuildingUpkeepSystem` with custom logic that reads existing component data differently

### Key Game Components

| Component | Namespace | Role |
|-----------|-----------|------|
| `BuildingCondition` | Game.Buildings | `m_Condition` -- the accumulator that drives level-up/level-down |
| `SpawnableBuildingData` | Game.Prefabs | `m_Level` -- current building level (1-5) |
| `ZoneData` | Game.Prefabs | `m_AreaType` -- zone type for per-category caps |
| `EconomyParameterData` | Game.Prefabs | Wage tables and economy parameters to override |
| `Renter` (buffer) | Game.Buildings | Building-to-renter linkage for reading occupant data |
| `HouseholdCitizen` (buffer) | Game.Citizens | Household-to-citizen linkage for reading education/income |
| `Citizen` | Game.Citizens | `GetEducationLevel()` and `GetAge()` for per-citizen attributes |
| `Resources` (buffer) | Game.Economy | Household wealth via `EconomyUtils.GetResources(Resource.Money)` |
| `ConsumptionData` | Game.Prefabs | Upkeep cost data read by the replacement upkeep system |
| `BuildingPropertyData` | Game.Prefabs | `CountProperties(AreaType)` for zone type detection |
| `WorkProvider` | Game.Companies | `m_MaxWorkers` accessed through building renter chain |

### Harmony Patches Needed

- **No Harmony patches required** -- the primary pattern is full system replacement of `BuildingUpkeepSystem` because its inner jobs (`BuildingUpkeepJob`, `ResourceNeedingUpkeepJob`, `LevelupJob`, `LeveldownJob`) are Burst-compiled and cannot be directly patched

### Implementation Patterns

**System replacement pattern**:
```csharp
// In Mod.OnLoad or system OnCreate:
World.GetOrCreateSystemManaged<BuildingUpkeepSystem>().Enabled = false;
World.GetOrCreateSystemManaged<CustomBuildingUpkeepSystem>();
```

**Education/income chain traversal** (building -> renter -> citizen):
1. Read `Renter` buffer on building entity
2. For each renter (household), read `HouseholdCitizen` buffer
3. For each citizen, call `Citizen.GetEducationLevel()` to aggregate education profile
4. Read renter `Resources` buffer for household wealth
5. Compute custom leveling penalty multiplier from aggregated data

**City preset pattern** (enum-driven default values):
```csharp
public enum CityPreset { Balanced, HighInequality, LowInequality, Custom }
// Settings UI exposes preset selector; selecting a preset populates
// all individual sliders with preset-specific defaults
```

**Settings UI pattern**: Use `ModSetting` with `SettingsUISlider` attributes for configurable parameters (wage multipliers, level cap percentages, condition scaling factors)

### Build Configuration Note

The UrbanInequality mod targets `net472` (not `netstandard2.1`) and uses `LangVersion 12`. This is an alternative valid build target for CS2 mods, as the game's Unity Mono runtime supports both .NET Standard 2.1 and .NET Framework 4.7.2 assemblies.

## Open Questions

- [ ] **Rent formula parameters**: The exact default values of `EconomyParameterData.m_RentPriceBuildingZoneTypeBase` and `m_LandValueModifier` are set by EconomyPrefab, which was not decompiled. These float3 values control the base rate and land value sensitivity per zone type.
- [ ] **Mixed building rent split**: `EconomyParameterData.m_MixedBuildingCompanyRentPercentage` controls how rent is split between residential and commercial units in mixed-use buildings. The exact default is unknown.
- [x] **Level-up material costs**: `ZoneLevelUpResourceData` buffer elements on zone prefab entities define per-level resource requirements (m_Level, m_LevelUpResource). `BuildingConfigurationData` provides the global condition decrement and notification prefabs. See Component Map and Advanced Patterns sections.
- [x] **BuildingCondition increment/decrement rates**: `BuildingConfigurationData.m_BuildingConditionDecrement` provides the base decrement rate. `BuildingUtils.GetBuildingConditionChange()` computes the actual per-tick change and is a Harmony patch target. See Component Map and Harmony Patch Points sections.
- [ ] **Upkeep level exponents**: `EconomyParameterData.m_ResidentialUpkeepLevelExponent`, `m_CommercialUpkeepLevelExponent`, and `m_IndustrialUpkeepLevelExponent` scale upkeep costs by building level. Defaults not traced.

## Sources

- Decompiled from: Game.dll
- Components: Game.Net.LandValue, Game.Simulation.LandValueCell, Game.Buildings.PropertyRenter, Game.Buildings.Renter, Game.Buildings.RentAction, Game.Buildings.RentersUpdated, Game.Buildings.PropertyOnMarket, Game.Buildings.BuildingCondition, Game.Prefabs.BuildingPropertyData, Game.Prefabs.BuildingConfigurationData, Game.Prefabs.ZoneLevelUpResourceData, Game.Prefabs.SignatureBuildingData, Game.Prefabs.SpawnableBuildingData, Game.Prefabs.ZoneData, Game.Prefabs.LandValueParameterData, Game.Vehicles.DeliveryTruck, Game.Buildings.ResourceNeeding, Game.Buildings.GoodsDeliveryRequest
- Systems: Game.Simulation.LandValueSystem, Game.Simulation.PropertyRenterSystem, Game.Simulation.RentAdjustSystem, Game.Simulation.BuildingUpkeepSystem, Game.Simulation.PropertyRenterRemoveSystem
- Utility: Game.Buildings.PropertyUtils, Game.Buildings.BuildingUtils
- Related research: CompanySimulation (company profitability), ResourceProduction (resource chains), CitizensHouseholds (household income)
