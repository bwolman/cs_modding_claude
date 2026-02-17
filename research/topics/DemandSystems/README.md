# Research: Demand Systems

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 calculates demand for residential, commercial, and industrial/office zones -- the systems that determine when new buildings should spawn and what drives the demand bars in the UI.

**Why**: Understanding demand calculations is essential for any mod that wants to tweak city growth rates, rebalance economic factors, or display custom demand information to players.

**Boundaries**: This research covers the three main demand systems (ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem), the shared DemandParameterData configuration, the DemandFactor enum, and DemandUtils. The ZoneSpawnSystem that consumes demand values to actually place buildings is out of scope except where it connects to demand outputs.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem, DemandUtils, DemandFactor (enum) |
| Game.dll | Game.Prefabs | DemandParameterData (struct), DemandPrefab (class) |
| Game.dll | Game.Prefabs.Modes | DemandParameterMode, ModeSettingData (struct) |

## Architecture Overview

The demand system consists of three independent `GameSystemBase` systems that all run on a shared 16-frame update interval but at staggered offsets:

| System | Update Offset | What it calculates |
|--------|--------------|-------------------|
| CommercialDemandSystem | 4 | Company demand + building demand for commercial zones |
| IndustrialDemandSystem | 7 | Industrial company/building demand, office company/building demand, storage demand |
| ResidentialDemandSystem | 10 | Household demand + building demand for low/medium/high density residential |

Each system implements `ISerializable` and `IDefaultSerializable` to persist demand state across save/load. All three systems share a single `DemandParameterData` singleton entity that holds tuning parameters.

### Demand vs. Building Demand

Each system tracks two distinct values:

- **Company/Household Demand**: An abstract score (0-200 for residential, 0-100 for commercial/industrial) representing how much the city "needs" more of this zone type. Driven by factors like happiness, unemployment, tax rates, available workplaces, etc.
- **Building Demand**: A 0-100 value representing whether new buildings should actually spawn. This combines the abstract demand with the availability of free properties. If there are enough empty buildings already, building demand stays low even if household/company demand is high.

### The DemandFactor Enum

Demand factors are tracked as arrays of 19 integers indexed by the `DemandFactor` enum. These are the individual positive/negative contributions that sum to total demand:

| Index | Factor | Used By |
|-------|--------|---------|
| 0 | StorageLevels | Industrial |
| 1 | UneducatedWorkforce | Industrial |
| 2 | EducatedWorkforce | Industrial, Office |
| 3 | CompanyWealth | (unused in current code) |
| 4 | LocalDemand | Commercial, Industrial, Office |
| 5 | Unemployment | Residential |
| 6 | FreeWorkplaces | Residential |
| 7 | Happiness | Residential |
| 8 | Homelessness | Residential (high density only) |
| 9 | TouristDemand | Commercial (lodging) |
| 10 | LocalInputs | (unused in current code) |
| 11 | Taxes | Residential, Commercial, Industrial, Office |
| 12 | Students | Residential (medium/high density) |
| 13 | EmptyBuildings | All (free property availability) |
| 14 | EmptyZones | (unused in current code) |
| 15 | PoorZoneLocation | (unused in current code) |
| 16 | PetrolLocalDemand | Commercial (petrochemicals) |
| 17 | Warehouses | Industrial (storage) |
| 18 | BuildingDemand | All (when no properties exist yet) |

## System Map

### ResidentialDemandSystem (Game.Simulation)

The residential demand system tracks demand separately for low, medium, and high density zones.

- **Base class**: GameSystemBase (also IDefaultSerializable, ISerializable)
- **Update interval**: 16 frames (offset 10)
- **Key constant**: `kMaxFactorEffect = 15` -- caps individual demand factor contributions (e.g., homelessness effect)
- **Key outputs**:
  - `householdDemand` (int): Abstract demand score, capped at 200
  - `buildingDemand` (int3): Per-density building demand (x=low, y=medium, z=high), each capped at 100
  - Three `NativeArray<int>` factor arrays (low/medium/high density), 19 elements each

**Game Mode Weight Modifier (`m_ResidentialDemandWeightsSelector`)**:

The system reads a `float2` weight selector from `ModeSettingData` (via the `m_GameModeSettingQuery`). This is applied to every demand factor via `GetFactorValue()`:

```csharp
private int GetFactorValue(float factorValue, float2 weightSelector)
{
    if (factorValue < 0f)
        return (int)(factorValue * weightSelector.x);  // Scale negative factors
    return (int)(factorValue * weightSelector.y);       // Scale positive factors
}
```

The default is `float2(1, 1)` (no scaling). Game modes can set different values to amplify or dampen positive vs. negative demand contributions independently. This is loaded in `OnGameLoaded()` from `ModeSettingData.m_ResidentialDemandWeightsSelector` when `ModeSettingData.m_Enable` is true.

**Demand Calculation Factors:**

1. **Population bonus**: 20 - smoothstep(0, 20, population/20000). New cities get a significant boost that tapers off.
2. **Happiness**: `m_HappinessEffect * (avgHappiness - m_NeutralHappiness)`. Higher happiness above the neutral point (default 45) increases demand.
3. **Homelessness**: Negative effect when homeless households exceed `m_NeutralHomelessness` (default 50). Capped at `kMaxFactorEffect` (15). Only affects high-density demand factors.
4. **Tax rates**: Average deviation from 10% across 5 education levels, weighted by `m_TaxEffect.x`. Lower taxes increase demand.
5. **Available workplaces**: `m_AvailableWorkplaceEffect * (freeWorkplaces - totalWorkplaces * neutralPercentage/100)`. Simple workplaces clamped to [0, 40], complex to [0, 20]. More available jobs attract residents.
6. **Students**: `m_StudentEffect * clamp(studyPositions/200, 0, 5)`. Available education slots increase demand for medium/high density.
7. **Unemployment**: `m_NeutralUnemployment - actualRate`. Lower unemployment increases demand.
8. **Free properties**: When free residential properties fall below `m_FreeResidentialRequirement`, building demand increases.

All factors are passed through `GetFactorValue()` with `m_ResidentialDemandWeightsSelector` before being summed.

**Final calculation**: `buildingDemand = clamp(householdDemand/2 + freePropertyFactor + sumOfDensityFactors, 0, 100)`. Density types that have no unlocked zone prefabs get zeroed out.

### CommercialDemandSystem (Game.Simulation)

- **Base class**: GameSystemBase (also IDefaultSerializable, ISerializable)
- **Update interval**: 16 frames (offset 4)
- **Key outputs**:
  - `companyDemand` (int): Average per-resource demand, 0-100
  - `buildingDemand` (int): Average building demand, 0-100
  - Per-resource arrays: `m_ResourceDemands`, `m_BuildingDemands`, `m_FreeProperties`
  - `m_DemandFactors` (19 elements): Aggregated factor contributions

**Demand Calculation:**

1. **Per-resource demand**: For each commercial resource type, demand is based on `currentAvailables` vs. a population-scaled threshold: `2500 * log10(0.01 * population)` for cities above 1000 pop.
2. **Tax effect**: `-0.05 * (taxRate - 10) * m_TaxEffect.y` plus any game mode offset. Taxes above 10% reduce demand.
3. **Lodging (hotels)**: Special handling -- demand = 100 when `currentTourists * m_HotelRoomPercentRequirement > lodging capacity`.
4. **Building demand**: Only generated when `freeProperties - propertyless <= 0` for a given resource. If there are enough empty commercial buildings, no new ones spawn.
5. **Final values**: Averaged across all resources with non-zero demand, clamped to [0, 100].

### IndustrialDemandSystem (Game.Simulation)

The most complex demand system, tracking six separate demand values for three sub-categories: industrial manufacturing, office, and storage/warehouses.

- **Base class**: GameSystemBase (also IDefaultSerializable, ISerializable)
- **Update interval**: 16 frames (offset 7)
- **Key constants**:
  - `kStorageProductionDemand = 2000` -- minimum resource demand before storage demand triggers
  - `kStorageCompanyEstimateLimit = 864000` -- estimated storage capacity for unhoused storage companies
- **Key outputs**:
  - `industrialCompanyDemand` / `industrialBuildingDemand`: Manufacturing zones
  - `officeCompanyDemand` / `officeBuildingDemand`: Office zones
  - `storageCompanyDemand` / `storageBuildingDemand`: Warehouse zones
  - Separate `m_IndustrialDemandFactors` and `m_OfficeDemandFactors` arrays (19 elements each)
  - Per-resource arrays: `m_IndustrialCompanyDemands`, `m_IndustrialBuildingDemands`, `m_StorageCompanyDemands`, `m_StorageBuildingDemands`, `m_FreeProperties`, `m_FreeStorages`, `m_Storages`, `m_StorageCapacities`, `m_ResourceDemands`

**UpdateIndustrialDemandJob.Execute() -- Detailed Flow:**

The Execute() method proceeds in several phases:

**Phase 1 -- Initialize resource demands:**
- Office resources: `resourceDemand = (householdDemand + companyDemand) * 2`
- Industrial resources: uses company-to-company demands, defaults to 100 if no current demand exists
- Reset per-resource free properties, storages, free storages, and storage capacities to 0

**Phase 2 -- City service upkeep:**
- Iterates over all city service entities (fire, police, etc.) and their installed upgrades
- Adds non-Money upkeep resource amounts to `m_ResourceDemands`

**Phase 3 -- Count storage companies:**
- Iterates storage companies to tally storages per resource, free storage slots, and storage capacities
- Unhoused storage companies contribute `kStorageCompanyEstimateLimit` (864000) to capacity estimate and decrement free storages

**Phase 4 -- Count free industrial properties:**
- Iterates industrial property chunks that are on market
- For each property, counts `m_AllowedManufactured` as free manufacturing slots and `m_AllowedStored` as free storage slots
- Respects attached parent building property restrictions

**Phase 5 -- Storage demand (per tradable, non-weightless resource):**
- Company demand: +1 when `resourceDemand > kStorageProductionDemand && storageCapacity < resourceDemand`
- Building demand: +1 when `freeStorages < 0`
- Warehouse factor (`m_IndustrialDemandFactors[17]`) accumulates storage building demand

**Phase 6 -- Per-produceable-resource company demand:**
```
baseDemand = isMaterial ? m_ExtractorBaseDemand : m_IndustrialBaseDemand
supplyDeficit = (1 + resourceDemand - production) / (resourceDemand + 1)
rawDemand = 50 * max(0, baseDemand * supplyDeficit)
```

- Electronics: `CityModifierType.IndustrialElectronicsDemand` boosts base demand
- Software: `CityModifierType.OfficeSoftwareDemand` boosts base demand
- Tax effect: `m_TaxEffect.z * -0.05 * (taxRate - 10) + gameModeTaxOffset`
- Workforce effect via `MapAndClaimWorkforceEffect()`:
  - Educated surplus (education >= 2) affects office demand: clamped to [-max(10+taxEffect, 10), 10]
  - Uneducated surplus (education < 2) affects industrial demand: clamped to [-max(10+taxEffect, 10), 15]
  - When tax effect is negative, both clamp to [-10, 10] / [-10, 15] respectively
- Office (weightless) resources: `companyDemand = max(0, min(100, rawDemand + taxEffect*100 + educatedEffect))` (only when rawDemand > 0)
- Industrial resources: `companyDemand = max(0, min(100, rawDemand + taxEffect*100 + educatedEffect + uneducatedEffect))`

**Phase 7 -- Per-resource building demand:**
- Non-material resources with demand > 0: building demand = 50 when `freeProperties - propertyless <= 0`, else 0
- Material resources with demand > 0: building demand = 1 (extractors always get minimal demand)
- Building demand contribution = company demand value when building demand > 0

**Phase 8 -- Demand factor accumulation:**
- Industrial factors: `[1]` uneducated workforce, `[2]` educated workforce, `[4]` local demand (rawDemand), `[11]` taxes, `[13]` empty buildings
- Office factors: `[2]` educated workforce, `[4]` local demand, `[11]` taxes, `[13]` empty buildings
- Local demand forced to -1 when zero (distinguishes "zero" from "no data")
- Empty buildings forced to -1 when zero
- When population is 0, local demand zeroed out

**Phase 9 -- Final aggregation:**
- `storageBuildingDemand = ceil(pow(20 * storageBuildingDemand, 0.75))`
- `industrialBuildingDemand = 2 * industrialBuildingDemand / numNonMaterialResources` (if industrial zones unlocked, else 0)
- `officeCompanyDemand *= 2 * officeCompanyDemand / numOfficeResources`
- Both building demands clamped to [0, 100]
- When `m_UnlimitedDemand` is true, both set to 100

**MapAndClaimWorkforceEffect():**
```
if (value < 0):  lerp from min to 0 as value goes from -2000 to 0
if (value >= 0): lerp from 0 to max as value goes from 0 to 20
```

### DemandUtils (Game.Simulation)

A small static utility class defining shared constants:

- `kUpdateInterval = 16`: All demand systems share this interval
- `kCountCompanyUpdateOffset = 1`: CountCompanyDataSystem runs first
- `kCommercialUpdateOffset = 4`: Commercial demand runs second
- `kIndustrialUpdateOffset = 7`: Industrial demand runs third
- `kResidentialUpdateOffset = 10`: Residential demand runs last
- `kZoneSpawnUpdateOffset = 13`: Zone spawning consumes the results

## Component Map

### DemandParameterData (Game.Prefabs)

The central configuration struct, stored as an ECS singleton component. All three demand systems read from this.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_ForestryPrefab | Entity | -- | Entity reference to the forestry industry prefab |
| m_OfficePrefab | Entity | -- | Entity reference to the office industry prefab |
| m_MinimumHappiness | int | 30 | Floor for happiness in demand calc |
| m_HappinessEffect | float | 2.0 | Weight multiplied by happiness delta |
| m_TaxEffect | float3 | (1, 1, 1) | Tax weight per sector (x=residential, y=commercial, z=industrial/office) |
| m_StudentEffect | float | 1.0 | Weight for study position factor |
| m_AvailableWorkplaceEffect | float | 8.0 | Weight for free workplace factor |
| m_HomelessEffect | float | 20.0 | Weight for homelessness factor |
| m_NeutralHappiness | int | 45 | Happiness value that produces zero effect |
| m_NeutralUnemployment | float | 20.0 | Unemployment rate (%) that produces zero effect |
| m_NeutralAvailableWorkplacePercentage | float | 10.0 | Free workplace % that produces zero effect |
| m_NeutralHomelessness | int | 50 | Homeless household count that produces zero effect |
| m_FreeResidentialRequirement | int3 | (5, 10, 10) | Free property threshold per density (x=low, y=medium, z=high) |
| m_FreeCommercialProportion | float | 10.0 | Free commercial property percentage target |
| m_FreeIndustrialProportion | float | 10.0 | Free industrial property percentage target |
| m_CommercialStorageMinimum | float | 0.2 | Minimum commercial storage ratio threshold |
| m_CommercialStorageEffect | float | 1.6 | Multiplier for commercial storage demand effect |
| m_CommercialBaseDemand | float | 4.0 | Base demand multiplier for commercial |
| m_IndustrialStorageMinimum | float | 0.2 | Minimum industrial storage ratio threshold |
| m_IndustrialStorageEffect | float | 1.6 | Multiplier for industrial storage demand effect |
| m_IndustrialBaseDemand | float | 7.0 | Base demand multiplier for industrial |
| m_ExtractorBaseDemand | float | 1.5 | Base demand multiplier for extractors |
| m_StorageDemandMultiplier | float | 5e-5 | Scaling factor for storage demand |
| m_CommuterWorkerRatioLimit | int | 8 | Max ratio of commuter workers to local workers before throttling |
| m_CommuterSlowSpawnFactor | int | 8 | Slowdown factor applied to commuter spawning when ratio exceeded |
| m_CommuterOCSpawnParameters | float4 | (0.8, 0.2, 0, 0) | Commuter spawn distribution at outside connections (x=Road, y=Train, z=Air, w=Ship) |
| m_TouristOCSpawnParameters | float4 | (0.1, 0.1, 0.5, 0.3) | Tourist spawn distribution at outside connections (x=Road, y=Train, z=Air, w=Ship) |
| m_CitizenOCSpawnParameters | float4 | (0.6, 0.2, 0.15, 0.05) | Citizen spawn distribution at outside connections (x=Road, y=Train, z=Air, w=Ship) |
| m_TeenSpawnPercentage | float | 0.5 | Percentage of new households with children that include a teen |
| m_FrameIntervalForSpawning | int3 | (0, 2000, 2000) | Frame cooldown per sector (x=residential, y=commercial, z=industrial) |
| m_HouseholdSpawnSpeedFactor | float | 0.5 | Speed factor for new household spawning |
| m_HotelRoomPercentRequirement | float | 0.5 | Fraction of tourists needing hotel rooms |
| m_NewCitizenEducationParameters | float4 | (0.005, 0.5, 0.35, 0.13) | Education distribution of new citizens (x=uneducated, y=poorly educated, z=educated, w=well educated; remainder = highly educated) |

### DemandPrefab (Game.Prefabs)

The MonoBehaviour-based prefab that initializes the `DemandParameterData` singleton at game start. Contains the same fields as `DemandParameterData` with Unity `[Tooltip]` annotations documenting each parameter. Located under `ComponentMenu("Settings/")`.

### ModeSettingData (Game.Prefabs.Modes)

A singleton ECS component that holds game mode overrides. The demand systems read this on game load to apply mode-specific adjustments.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_Enable | bool | false | Whether mode settings are active |
| m_ResidentialDemandWeightsSelector | float2 | (1, 1) | Scales residential demand factors (x=negative factor weight, y=positive factor weight) |
| m_CommercialTaxEffectDemandOffset | float | 0 | Added to commercial tax effect calculation |
| m_IndustrialOfficeTaxEffectDemandOffset | float | 0 | Added to industrial/office tax effect calculation |

When `m_Enable` is true, `ResidentialDemandSystem` applies the weights selector to all demand factors via `GetFactorValue()`, and `CommercialDemandSystem` / `IndustrialDemandSystem` add their respective tax offsets.

### DemandParameterMode (Game.Prefabs.Modes)

An `EntityQueryModePrefab` subclass under `ComponentMenu("Modes/Mode Parameters/")` that can override most `DemandParameterData` fields for specific game modes. It implements `ApplyModeData()` to write overridden values to the `DemandParameterData` singleton, and `RestoreDefaultData()` to reset fields from the original `DemandPrefab` values. Note: `DemandParameterMode` does **not** override the storage-related fields (`m_CommercialStorageMinimum`, `m_CommercialStorageEffect`, `m_IndustrialStorageMinimum`, `m_IndustrialStorageEffect`, `m_StorageDemandMultiplier`, `m_FreeCommercialProportion`, `m_FreeIndustrialProportion`).

### Cross-References

See also: [Citizens & Households](../CitizensHouseholds/README.md) for `CountHouseholdDataSystem`, which provides household counts, unemployment rate, homeless household count, and resource needs consumed by all three demand systems.

## Data Flow

```
DemandParameterData (singleton)          [tuning parameters from DemandPrefab]
     |
     v
CountCompanyDataSystem (offset 1)        [counts companies, production, resource demands]
CountHouseholdDataSystem                  [counts households, unemployment, resource needs]
CountWorkplacesSystem                     [counts free/total workplaces]
CountResidentialPropertySystem            [counts free/total residential properties]
TaxSystem                                [current tax rates]
     |
     v
CommercialDemandSystem (offset 4)        [per-resource commercial demand]
     |
IndustrialDemandSystem (offset 7)        [per-resource industrial/office/storage demand]
     |
ResidentialDemandSystem (offset 10)      [per-density residential demand]
     |
     v
ZoneSpawnSystem (offset 13)              [consumes buildingDemand values to spawn buildings]
     |
     v
UI InfoView                              [reads demand factor arrays for display]
```

### Update Ordering

All demand systems share a 16-frame update interval but run at different offsets to ensure data dependencies are met:

1. **Frame N+1**: CountCompanyDataSystem counts current companies and production
2. **Frame N+4**: CommercialDemandSystem uses company data to compute commercial demand
3. **Frame N+7**: IndustrialDemandSystem uses company data + household resource needs
4. **Frame N+10**: ResidentialDemandSystem uses workplace counts, household data, tax rates
5. **Frame N+13**: ZoneSpawnSystem reads all three building demand values to decide spawning

## Harmony Patch Points

### Candidate 1: `ResidentialDemandSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix (to skip) or Postfix (to modify results)
- **What it enables**: Override or adjust residential demand calculation
- **Risk level**: Low -- system is self-contained with clear inputs/outputs

### Candidate 2: `CommercialDemandSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override or adjust commercial demand calculation
- **Risk level**: Low

### Candidate 3: `IndustrialDemandSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override or adjust industrial/office/storage demand
- **Risk level**: Low

### Candidate 4: Modifying DemandParameterData at runtime

- **Patch type**: N/A (direct component modification)
- **What it enables**: Change demand tuning parameters without patching system code
- **Risk level**: Very low -- read the singleton entity, modify fields, write back
- **Note**: Changes persist only until next game load unless serialized

### Note on BurstCompile Jobs

The actual demand calculation jobs (`UpdateResidentialDemandJob`, `UpdateCommercialDemandJob`, `UpdateIndustrialDemandJob`) are Burst-compiled IJob structs. These **cannot** be Harmony-patched. To modify demand calculations, either:
1. Patch the `OnUpdate()` method of the parent system (to override job inputs or postprocess outputs)
2. Modify `DemandParameterData` before the job runs
3. Create a custom system that runs after the demand system and adjusts the stored values

## Examples

### Example 1: Read Current Demand Values from Each System

```csharp
/// <summary>
/// Reads and logs current demand values from all three demand systems.
/// </summary>
public void LogDemandValues()
{
    var resSys = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<ResidentialDemandSystem>();
    var comSys = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<CommercialDemandSystem>();
    var indSys = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<IndustrialDemandSystem>();

    // Residential: householdDemand (0-200), buildingDemand int3 (0-100 per density)
    int householdDemand = resSys.householdDemand;
    int3 resBuildingDemand = resSys.buildingDemand;
    Log.Info($"Residential: household={householdDemand}, building=({resBuildingDemand.x},{resBuildingDemand.y},{resBuildingDemand.z})");

    // Commercial: companyDemand (0-100), buildingDemand (0-100)
    int comCompanyDemand = comSys.companyDemand;
    int comBuildingDemand = comSys.buildingDemand;
    Log.Info($"Commercial: company={comCompanyDemand}, building={comBuildingDemand}");

    // Industrial: 6 values (company + building for industrial, office, storage)
    Log.Info($"Industrial: company={indSys.industrialCompanyDemand}, building={indSys.industrialBuildingDemand}");
    Log.Info($"Office: company={indSys.officeCompanyDemand}, building={indSys.officeBuildingDemand}");
    Log.Info($"Storage: company={indSys.storageCompanyDemand}, building={indSys.storageBuildingDemand}");
}
```

### Example 2: Modify DemandParameterData at Runtime

```csharp
/// <summary>
/// A system that adjusts demand parameters every update cycle.
/// Changes persist until the next game load.
/// </summary>
public partial class DemandParameterTweakSystem : GameSystemBase
{
    private EntityQuery m_DemandParamQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_DemandParamQuery = GetEntityQuery(
            ComponentType.ReadWrite<DemandParameterData>()
        );
        RequireForUpdate(m_DemandParamQuery);
    }

    protected override void OnUpdate()
    {
        var data = m_DemandParamQuery.GetSingleton<DemandParameterData>();

        // Double happiness sensitivity
        data.m_HappinessEffect = 4.0f;
        // Make unemployment less impactful (higher neutral = more tolerance)
        data.m_NeutralUnemployment = 30f;
        // Increase free residential requirement so building demand triggers sooner
        data.m_FreeResidentialRequirement = new int3(10, 20, 20);
        // Adjust commuter spawn distribution: more by train
        data.m_CommuterOCSpawnParameters = new float4(0.5f, 0.4f, 0.05f, 0.05f);

        m_DemandParamQuery.SetSingleton(data);
    }
}
```

### Example 3: Read Demand Factor Breakdown

```csharp
/// <summary>
/// Reads the per-factor demand breakdown for residential low density.
/// Factors are indexed by the DemandFactor enum (19 values).
/// </summary>
public void LogResidentialFactors()
{
    var resSys = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<ResidentialDemandSystem>();

    JobHandle deps;
    NativeArray<int> lowFactors = resSys.GetLowDensityDemandFactors(out deps);
    deps.Complete();  // Ensure the job is done before reading

    // Key factor indices (from DemandFactor enum):
    // 5=Unemployment, 6=FreeWorkplaces, 7=Happiness, 11=Taxes, 13=EmptyBuildings
    Log.Info($"Low density factors: " +
        $"Happiness={lowFactors[7]}, " +
        $"Unemployment={lowFactors[5]}, " +
        $"FreeWorkplaces={lowFactors[6]}, " +
        $"Taxes={lowFactors[11]}, " +
        $"EmptyBuildings={lowFactors[13]}");

    // For industrial/office:
    var indSys = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<IndustrialDemandSystem>();
    NativeArray<int> indFactors = indSys.GetIndustrialDemandFactors(out deps);
    NativeArray<int> offFactors = indSys.GetOfficeDemandFactors(out deps);
    deps.Complete();

    Log.Info($"Industrial factors: " +
        $"UneducatedWorkforce={indFactors[1]}, " +
        $"EducatedWorkforce={indFactors[2]}, " +
        $"LocalDemand={indFactors[4]}, " +
        $"Taxes={indFactors[11]}, " +
        $"Warehouses={indFactors[17]}");
}
```

### Example 4: Custom System That Adjusts Demand Post-Calculation

```csharp
/// <summary>
/// Runs after ResidentialDemandSystem and caps building demand at 50.
/// Uses UpdateAfter attribute to ensure correct ordering.
/// </summary>
[UpdateAfter(typeof(ResidentialDemandSystem))]
public partial class DemandCapSystem : GameSystemBase
{
    private ResidentialDemandSystem m_ResidentialDemandSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_ResidentialDemandSystem = World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
    }

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
    public override int GetUpdateOffset(SystemUpdatePhase phase) => 11; // After residential (10)

    protected override void OnUpdate()
    {
        // Use Harmony Traverse to access private NativeValue fields
        var traverse = HarmonyLib.Traverse.Create(m_ResidentialDemandSystem);
        var buildingDemand = traverse.Field<NativeValue<int3>>("m_BuildingDemand").Value;

        int3 current = buildingDemand.value;
        // Cap all densities at 50
        buildingDemand.value = math.min(current, new int3(50, 50, 50));
    }
}
```

### Example 5: Query Free Properties per Zone Type

```csharp
/// <summary>
/// Reads free property counts from the industrial demand system
/// to see how many available slots exist per resource type.
/// </summary>
public void LogFreeIndustrialProperties()
{
    var indSys = World.DefaultGameObjectInjectionWorld
        .GetOrCreateSystemManaged<IndustrialDemandSystem>();

    JobHandle deps;
    // Per-resource building demands and free storage counts
    NativeArray<int> buildingDemands = indSys.GetBuildingDemands(out deps);
    NativeArray<int> storageDemands = indSys.GetStorageBuildingDemands(out deps);
    NativeArray<int> resourceDemands = indSys.GetIndustrialResourceDemands(out deps);
    deps.Complete();

    // Iterate resources -- indices correspond to EconomyUtils.GetResourceIndex()
    ResourceIterator iterator = ResourceIterator.GetIterator();
    while (iterator.Next())
    {
        int idx = EconomyUtils.GetResourceIndex(iterator.resource);
        if (buildingDemands[idx] > 0 || storageDemands[idx] > 0)
        {
            Log.Info($"{iterator.resource}: buildingDemand={buildingDemands[idx]}, " +
                $"storageDemand={storageDemands[idx]}, " +
                $"resourceDemand={resourceDemands[idx]}");
        }
    }
}
```

## Open Questions

- [x] What factors drive each demand type? -- Documented above per system
- [x] How are demand factors indexed? -- Via the DemandFactor enum (19 values)
- [x] How does demand connect to building spawning? -- ZoneSpawnSystem reads buildingDemand at offset 13
- [x] Can demand parameters be modified at runtime? -- Yes, DemandParameterData is a standard ECS singleton
- [x] How does m_ResidentialDemandWeightsSelector work? -- A float2 from ModeSettingData that independently scales positive (y) and negative (x) demand factors via GetFactorValue()
- [x] What is kMaxFactorEffect? -- Static constant = 15, caps homelessness effect in ResidentialDemandSystem
- [x] What are the missing DemandParameterData fields? -- Fully documented: storage params, commuter/tourist/citizen OC spawn params, teen spawn %, forestry/office prefab refs
- [ ] How exactly does ZoneSpawnSystem translate demand values into spawn probability? -- Out of scope, needs separate research
- [ ] How does the demand UI panel read and display factor arrays? -- UI binding not decompiled
- [ ] How are m_CommercialStorageMinimum/Effect and m_IndustrialStorageMinimum/Effect used at runtime? -- Fields exist on the struct but direct usage in demand systems not located; may be consumed by other systems (e.g., company spawning or resource distribution)

## Sources

- Decompiled from: Game.dll (Game.Simulation namespace, Game.Prefabs namespace, Game.Prefabs.Modes namespace)
- Key types: ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem, DemandParameterData, DemandPrefab, DemandFactor, DemandUtils, DemandParameterMode, ModeSettingData
- All decompiled snippets saved in `snippets/` directory
