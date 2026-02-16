# Research: Demand Systems

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 calculates demand for residential, commercial, and industrial/office zones -- the systems that determine when new buildings should spawn and what drives the demand bars in the UI.

**Why**: Understanding demand calculations is essential for any mod that wants to tweak city growth rates, rebalance economic factors, or display custom demand information to players.

**Boundaries**: This research covers the three main demand systems (ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem), the shared DemandParameterData configuration, the DemandFactor enum, and DemandUtils. The ZoneSpawnSystem that consumes demand values to actually place buildings is out of scope except where it connects to demand outputs.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem, DemandUtils, DemandFactor (enum) |
| Game.dll | Game.Prefabs | DemandParameterData (struct), DemandPrefab (class) |
| Game.dll | Game.Prefabs.Modes | DemandParameterMode |

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
- **Key outputs**:
  - `householdDemand` (int): Abstract demand score, capped at 200
  - `buildingDemand` (int3): Per-density building demand (x=low, y=medium, z=high), each capped at 100
  - Three `NativeArray<int>` factor arrays (low/medium/high density), 19 elements each

**Demand Calculation Factors:**

1. **Population bonus**: 20 - smoothstep(0, 20, population/20000). New cities get a significant boost that tapers off.
2. **Happiness**: `m_HappinessEffect * (avgHappiness - m_NeutralHappiness)`. Higher happiness above the neutral point (default 45) increases demand.
3. **Homelessness**: Negative effect when homeless households exceed `m_NeutralHomelessness` (default 50). Only affects high-density demand factors.
4. **Tax rates**: Average deviation from 10% across 5 education levels, weighted by `m_TaxEffect.x`. Lower taxes increase demand.
5. **Available workplaces**: `m_AvailableWorkplaceEffect * (freeWorkplaces - totalWorkplaces * neutralPercentage/100)`. More available jobs attract residents.
6. **Students**: `m_StudentEffect * clamp(studyPositions/200, 0, 5)`. Available education slots increase demand for medium/high density.
7. **Unemployment**: `m_NeutralUnemployment - actualRate`. Lower unemployment increases demand.
8. **Free properties**: When free residential properties fall below `m_FreeResidentialRequirement`, building demand increases.

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
- **Key outputs**:
  - `industrialCompanyDemand` / `industrialBuildingDemand`: Manufacturing zones
  - `officeCompanyDemand` / `officeBuildingDemand`: Office zones
  - `storageCompanyDemand` / `storageBuildingDemand`: Warehouse zones
  - Separate `m_IndustrialDemandFactors` and `m_OfficeDemandFactors` arrays

**Demand Calculation:**

1. **Resource demands**: Office resources use `(householdDemand + companyDemand) * 2`. Industrial resources use company-to-company demands or default to 100.
2. **City service upkeep**: Resources needed by city services (fire, police, etc.) add to industrial resource demand.
3. **Storage demand**: When `storageCapacity < resourceDemand` and demand exceeds `kStorageProductionDemand` (2000), storage company demand triggers. Building demand triggers when `freeStorages < 0`.
4. **Workforce effect**: Educated workers affect office demand; uneducated workers affect industrial demand. Uses `MapAndClaimWorkforceEffect()` to map worker surplus/deficit to a bounded range.
5. **Tax effect**: `m_TaxEffect.z * -0.05f * (taxRate - 10)` plus game mode offset.
6. **Per-resource company demand**: `50 * max(0, baseDemand * supplyDeficit) + taxEffect + workforceEffect`, clamped to [0, 100].
7. **Building demand**: Only when `freeProperties - propertyless <= 0` for non-material resources.
8. **City modifiers**: Electronics demand can be boosted by `CityModifierType.IndustrialElectronicsDemand`; software by `CityModifierType.OfficeSoftwareDemand`.

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
| m_FreeResidentialRequirement | int3 | (5, 10, 10) | Free property threshold per density |
| m_FreeCommercialProportion | float | 10.0 | Free commercial property percentage target |
| m_FreeIndustrialProportion | float | 10.0 | Free industrial property percentage target |
| m_CommercialBaseDemand | float | 4.0 | Base demand multiplier for commercial |
| m_IndustrialBaseDemand | float | 7.0 | Base demand multiplier for industrial |
| m_ExtractorBaseDemand | float | 1.5 | Base demand multiplier for extractors |
| m_StorageDemandMultiplier | float | 5e-5 | Scaling factor for storage demand |
| m_HouseholdSpawnSpeedFactor | float | 0.5 | Speed factor for new household spawning |
| m_HotelRoomPercentRequirement | float | 0.5 | Fraction of tourists needing hotel rooms |
| m_FrameIntervalForSpawning | int3 | (0, 2000, 2000) | Frame cooldown per sector |
| m_NewCitizenEducationParameters | float4 | (0.005, 0.5, 0.35, 0.13) | Education distribution of new citizens |

### DemandPrefab (Game.Prefabs)

The MonoBehaviour-based prefab that initializes the `DemandParameterData` singleton at game start. Contains the same fields as `DemandParameterData` with Unity `[Tooltip]` annotations documenting each parameter. Located under `ComponentMenu("Settings/")`.

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

## Open Questions

- [x] What factors drive each demand type? -- Documented above per system
- [x] How are demand factors indexed? -- Via the DemandFactor enum (19 values)
- [x] How does demand connect to building spawning? -- ZoneSpawnSystem reads buildingDemand at offset 13
- [x] Can demand parameters be modified at runtime? -- Yes, DemandParameterData is a standard ECS singleton
- [ ] How exactly does ZoneSpawnSystem translate demand values into spawn probability? -- Out of scope, needs separate research
- [ ] How does the demand UI panel read and display factor arrays? -- UI binding not decompiled

## Sources

- Decompiled from: Game.dll (Game.Simulation namespace, Game.Prefabs namespace)
- Key types: ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem, DemandParameterData, DemandPrefab, DemandFactor, DemandUtils
- All decompiled snippets saved in `snippets/` directory
