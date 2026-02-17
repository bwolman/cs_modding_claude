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
| Game.dll | Game.Prefabs | LandValueParameterData, LandValuePrefab, BuildingPropertyData, BuildingConfigurationData, EconomyParameterData |

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
    - Condition change scales with building level: `pow(2, level) * max(1, renterCount)`
    - If condition >= levelingCost: queues level-up (requests resource delivery)
    - If condition <= -abandonCost: queues level-down (abandonment)
  - `ResourceNeedingUpkeepJob`: Checks if all level-up materials were delivered; if so, queues actual level-up
  - `LevelupJob`: Selects a higher-level building prefab with matching zone type, lot size, and access flags; initiates UnderConstruction
  - `LeveldownJob`: Marks building as Abandoned, removes services (electricity, water, garbage, mail), evicts all renters, doubles crime production
  - `UpkeepPaymentJob`: Actually deducts money from renters

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
    If renterWealth >= monetary:
      condition += increment * pow(2, level) * max(1, renters)
      Deduct upkeep from renters
    Else:
      condition -= decrement * pow(2, level) * max(1, renters)
          |
          v
    condition >= levelingCost  -->  Request level-up materials
                                    Materials delivered --> LevelupJob
                                    Select higher-level prefab
                                    Start UnderConstruction
          |
    condition <= -abandonCost  -->  LeveldownJob
                                    Mark Abandoned
                                    Evict all renters
                                    Remove services
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

### Candidate 4: `Game.Simulation.RentAdjustSystem.OnUpdate`

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

## Open Questions

- [ ] **Rent formula parameters**: The exact default values of `EconomyParameterData.m_RentPriceBuildingZoneTypeBase` and `m_LandValueModifier` are set by EconomyPrefab, which was not decompiled. These float3 values control the base rate and land value sensitivity per zone type.
- [ ] **Mixed building rent split**: `EconomyParameterData.m_MixedBuildingCompanyRentPercentage` controls how rent is split between residential and commercial units in mixed-use buildings. The exact default is unknown.
- [ ] **Level-up material costs**: The specific resources and amounts required for level-up come from `ZoneLevelUpResourceData` buffers on zone prefabs or `BuildingConfigurationData`. The exact values per level were not traced.
- [ ] **BuildingCondition increment/decrement rates**: `BuildingConfigurationData.m_BuildingConditionDecrement` and `BuildingUtils.GetBuildingConditionChange()` control the actual rate. The default values come from the BuildingConfiguration prefab.
- [ ] **Upkeep level exponents**: `EconomyParameterData.m_ResidentialUpkeepLevelExponent`, `m_CommercialUpkeepLevelExponent`, and `m_IndustrialUpkeepLevelExponent` scale upkeep costs by building level. Defaults not traced.

## Sources

- Decompiled from: Game.dll
- Components: Game.Net.LandValue, Game.Simulation.LandValueCell, Game.Buildings.PropertyRenter, Game.Buildings.Renter, Game.Buildings.RentAction, Game.Buildings.RentersUpdated, Game.Buildings.PropertyOnMarket, Game.Buildings.BuildingCondition, Game.Prefabs.BuildingPropertyData, Game.Prefabs.LandValueParameterData
- Systems: Game.Simulation.LandValueSystem, Game.Simulation.PropertyRenterSystem, Game.Simulation.RentAdjustSystem, Game.Simulation.BuildingUpkeepSystem, Game.Simulation.PropertyRenterRemoveSystem
- Utility: Game.Buildings.PropertyUtils
- Related research: CompanySimulation (company profitability), ResourceProduction (resource chains), CitizensHouseholds (household income)
