# Research: Parks, Recreation & Leisure System

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 handles citizen leisure behavior -- how leisure providers are scored, how parks contribute to attractiveness and happiness, how citizens select leisure activities, and how the leisure economy integrates with commercial services.

**Why**: To enable mods that adjust leisure weights and preferences, modify park maintenance and coverage, control attractiveness calculations, and customize the leisure consumption economy.

**Boundaries**: Tourist-specific leisure routing is covered in the Tourism Economy research. This topic focuses on the resident leisure pipeline, park maintenance, and the attractiveness system that supports both residents and tourists.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | LeisureSystem, ParkAISystem, AttractionSystem, TerrainAttractivenessSystem |
| Game.dll | Game.Buildings | Park, ParkMaintenance, LeisureProvider, AttractivenessProvider |
| Game.dll | Game.Citizens | Leisure, Citizen (m_LeisureCounter) |
| Game.dll | Game.Agents | LeisureType (enum) |
| Game.dll | Game.Prefabs | LeisureProviderData, ParkData, AttractionData, LeisureParametersData, AttractivenessParameterData, ParkParameterData |

## Component Map

### `Leisure` (Game.Citizens)

Added to citizens who are currently seeking or engaged in a leisure activity.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetAgent | Entity | The leisure provider entity the citizen is heading to or currently at |
| m_LastPossibleFrame | uint | Simulation frame by which the citizen must finish leisure (work/school deadline) |

*Source: `Game.dll` -> `Game.Citizens.Leisure`*

### `LeisureProvider` (Game.Buildings)

Marker component on building entities that provide leisure. Empty struct (tag component).

*Source: `Game.dll` -> `Game.Buildings.LeisureProvider`*

### `AttractivenessProvider` (Game.Buildings)

Runtime component on buildings that contribute to city attractiveness.

| Field | Type | Description |
|-------|------|-------------|
| m_Attractiveness | int | Computed attractiveness score (updated every 16 frames) |

### `Park` (Game.Buildings)

Runtime component on park building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Maintenance | short | Current maintenance level (decreases over time, affects coverage and attractiveness) |

### `ParkMaintenance` (Game.Buildings)

Marker component (empty struct) indicating a building requires park maintenance vehicles.

### `LeisureType` (Game.Agents)

| Value | Name | Description |
|-------|------|-------------|
| 0 | Meals | Restaurant/food service leisure |
| 1 | Entertainment | Bars, clubs, entertainment venues |
| 2 | Commercial | Shopping at commercial businesses |
| 3 | CityIndoors | Indoor city-owned leisure (museums, libraries) |
| 4 | Travel | Travel outside the city |
| 5 | CityPark | City park visits (weather-dependent) |
| 6 | CityBeach | Beach visits (temperature and weather-dependent) |
| 7 | Attractions | Tourist attractions (preferred by tourists) |
| 8 | Relaxation | Relaxation activities |
| 9 | Sightseeing | City sightseeing |

### `LeisureProviderData` (Game.Prefabs)

Prefab data defining a building's leisure provider characteristics.

| Field | Type | Description |
|-------|------|-------------|
| m_Efficiency | int | How much leisure counter is added per visit (scaled by kUpdateInterval) |
| m_Resources | Resource | Resource type consumed by the leisure activity |
| m_LeisureType | LeisureType | Category of leisure provided |

### `ParkData` (Game.Prefabs)

Prefab configuration for parks. Supports `Combine()` for upgrades.

| Field | Type | Description |
|-------|------|-------------|
| m_MaintenancePool | short | Maximum maintenance level |
| m_AllowHomeless | bool | Whether the park allows homeless citizens |

### `AttractionData` (Game.Prefabs)

Prefab data for attraction buildings. Supports `Combine()` for upgrades.

| Field | Type | Description |
|-------|------|-------------|
| m_Attractiveness | int | Base attractiveness value |

### `AttractivenessParameterData` (Game.Prefabs)

Global singleton controlling terrain attractiveness calculations.

| Field | Type | Description |
|-------|------|-------------|
| m_ForestEffect | float | Attractiveness bonus from nearby forests |
| m_ForestDistance | float | Maximum distance for forest bonus |
| m_ShoreEffect | float | Attractiveness bonus from nearby shores |
| m_ShoreDistance | float | Maximum distance for shore bonus |
| m_HeightBonus | float3 | Height attractiveness: (threshold, multiplier, max) |
| m_AttractiveTemperature | float2 | Ideal temperature range |
| m_ExtremeTemperature | float2 | Extreme temperature range |
| m_TemperatureAffect | float2 | Temperature effect on attractiveness |
| m_RainEffectRange | float2 | Rain effect range |
| m_SnowEffectRange | float2 | Snow effect range |
| m_SnowRainExtremeAffect | float3 | Combined snow/rain/extreme weather effects |

### `LeisureParametersData` (Game.Prefabs)

Global singleton controlling leisure system behavior.

| Field | Type | Description |
|-------|------|-------------|
| m_TravelingPrefab | Entity | Prefab for Travel leisure type |
| m_AttractionPrefab | Entity | Prefab for Attractions leisure type |
| m_SightseeingPrefab | Entity | Prefab for Sightseeing leisure type |
| m_LeisureRandomFactor | int | Random factor in leisure type selection |
| m_ChanceCitizenDecreaseLeisureCounter | int | Probability of decreasing citizen leisure counter |
| m_ChanceTouristDecreaseLeisureCounter | int | Probability of decreasing tourist leisure counter |
| m_AmountLeisureCounterDecrease | int | Amount to decrease leisure counter |
| m_TouristLodgingConsumePerDay | int | Tourist lodging consumption per day |
| m_TouristServiceConsumePerDay | int | Tourist service consumption per day |

## System Map

### `LeisureSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (per UpdateFrame)
- **Queries**: Citizens with Leisure component
- **Key jobs**:
  - `LeisureJob` -- processes citizens with Leisure component each UpdateFrame:
    - If citizen is at provider location: calls `SpendLeisure()` to increase `m_LeisureCounter` and consume resources
    - If pathfinding complete: begins trip to provider, adds `TripNeeded` with `Purpose.Leisure`
    - If no target: calls `FindLeisure()` to select a leisure type and queue pathfinding
  - `SpendLeisureJob` -- processes the `LeisureEvent` queue:
    - Increments `citizen.m_LeisureCounter` by `ceil(efficiency / kUpdateInterval)`
    - Consumes service resources, pays provider, charges household
    - Tracks customer count on provider companies

- **Key methods**:
  - `GetWeight(LeisureType, wealth, age)` -- returns weighted probability for each leisure type based on:
    - **Age preferences**: Different weights for Child, Teen, Adult, Elderly per type
    - **Wealth threshold**: `smoothstep(wealthMin, 1, (wealth + 5000) / 10000)`
    - **Weather/temperature**: CityPark penalized 95% by rain; CityBeach requires temp > 20C and low rain
    - Final: `ageWeight * weatherModifier * wealthScale * smoothstep(wealthMin, 1, normalizedWealth)`
  - `SelectLeisureType()` -- weighted random selection from all 10 types
  - `FindLeisure()` -- queues pathfinding to nearest provider of selected type
  - `SpendLeisure()` -- enqueues LeisureEvent for economic processing; removes Leisure component when counter reaches 255 or deadline expires

### `ParkAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: `262144 / kUpdatesPerDay` (kUpdatesPerDay = 256)
- **Queries**: Buildings with Park, ModifiedServiceCoverage, Renter components
- **Key logic**:
  - **Maintenance decay**: `maintenance -= (400 + 50 * renters.Length) / kUpdatesPerDay` per tick
  - **Maintenance requests**: Creates `MaintenanceRequest` when maintenance drops below threshold
  - **Service coverage**: Modified by maintenance level using `GetModifiedServiceCoverage()`:
    - `maintenanceTier = floor(maintenanceRatio / 0.3)`
    - `magnitude *= 0.95 + 0.05 * min(1, tier) + 0.1 * max(0, tier - 1)`
    - `range *= 0.95 + 0.05 * tier`
    - Applies `ParkEntertainment` city modifier
  - **District tracking**: Adds `CurrentDistrict` component if missing

### `AttractionSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**: Buildings with AttractivenessProvider, PrefabRef, UpdateFrame
- **Key logic**:
  - Base attractiveness from `AttractionData.m_Attractiveness` (supports upgrade combining)
  - Multiplied by building `Efficiency` (unless Signature building)
  - Park maintenance bonus: `attractiveness *= 0.8 + 0.2 * maintenanceRatio`
  - Terrain bonus: `attractiveness *= 1 + 0.01 * terrainAttractiveness`
  - Terrain attractiveness = forest bonus + shore bonus + height bonus
  - `AttractionSystem.AttractivenessFactor` enum: Efficiency, Maintenance, Forest, Beach, Height

### `TerrainAttractivenessSystem` (Game.Simulation)

- **Base class**: CellMapSystem<TerrainAttractiveness>
- **Update interval**: `262144 / kUpdatesPerDay` (kUpdatesPerDay = 16)
- **Key logic**:
  - 128x128 cell map of terrain attractiveness values
  - `EvaluateAttractiveness()`: `forestEffect * forestBonus + shoreEffect * shoreBonus + min(heightMax, max(0, height - heightThreshold) * heightMultiplier)`
  - Forest bonus: `saturate(1 - distance/forestDistance) * forestDensity`
  - Shore bonus: `saturate(1 - distance/shoreDistance) * (waterDepth > 2 ? 1 : 0)`

## Data Flow

```
LEISURE NEED (Citizen.m_LeisureCounter decreases over time)
  Counter decremented with probability ChanceCitizenDecreaseLeisureCounter
  Low counter triggers leisure-seeking behavior
          |
          v
LEISURE SELECTION (LeisureSystem.FindLeisure, per UpdateFrame)
  SelectLeisureType(): weighted random based on:
    - Age: Child/Teen/Adult/Elderly preferences per type
    - Wealth: smoothstep threshold per type
    - Weather: CityPark penalized by rain, CityBeach by cold
    - Temperature: Beach requires >20C
  Travel/Sightseeing/Attractions -> AddMeetingSystem (group activity)
  Other types -> pathfind to nearest provider
          |
          v
PATHFINDING (CitizenPathfindSetup.SetupLeisureTargetJob)
  SetupQueueTarget with:
    m_Type = SetupTargetType.Leisure
    m_Value = (int)leisureType
    m_Value2 = 255 - leisureCounter (urgency)
  Considers car/bicycle availability for transport
          |
          v
ARRIVAL & SPENDING (LeisureSystem.SpendLeisure)
  At provider location:
    Enqueue LeisureEvent with provider efficiency
    leisureCounter += ceil(efficiency / kUpdateInterval)
    Cap at 255
  Check completion:
    Counter near max OR deadline reached OR provider inactive
    -> Remove Leisure component
          |
          v
ECONOMIC PROCESSING (LeisureSystem.SpendLeisureJob)
  For service companies:
    Consume service resources
    Calculate price: quantity * marketPrice * priceMultiplier
    Transfer money: household -> provider
    Track consumption statistics
          |
          v
PARK MAINTENANCE (ParkAISystem, 256 updates/day)
  maintenance -= (400 + 50 * renters) / kUpdatesPerDay
  Below threshold -> create MaintenanceRequest
  Service coverage = f(maintenance ratio):
    magnitude *= 0.95 + 0.05*tier + 0.1*max(0, tier-1)
    range *= 0.95 + 0.05*tier
    Apply ParkEntertainment city modifier
          |
          v
ATTRACTIVENESS (AttractionSystem, every 16 frames)
  attractiveness = baseAttraction * efficiency
  * (0.8 + 0.2 * parkMaintenanceRatio)
  * (1 + 0.01 * terrainAttractiveness)
  terrainAttractiveness = forest + shore + height bonuses
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Leisure efficiency | LeisureProviderData.m_Efficiency | Game.Prefabs.LeisureProviderData |
| Leisure type | LeisureProviderData.m_LeisureType | Game.Prefabs.LeisureProviderData |
| Leisure resource | LeisureProviderData.m_Resources | Game.Prefabs.LeisureProviderData |
| Park maintenance pool | ParkData.m_MaintenancePool | Game.Prefabs.ParkData |
| Allow homeless | ParkData.m_AllowHomeless | Game.Prefabs.ParkData |
| Base attractiveness | AttractionData.m_Attractiveness | Game.Prefabs.AttractionData |
| Forest effect | AttractivenessParameterData.m_ForestEffect | Game.Prefabs.AttractivenessParameterData |
| Shore effect | AttractivenessParameterData.m_ShoreEffect | Game.Prefabs.AttractivenessParameterData |
| Height bonus | AttractivenessParameterData.m_HeightBonus | Game.Prefabs.AttractivenessParameterData |
| Park entertainment modifier | CityModifier ParkEntertainment | Game.City.CityModifier |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.LeisureSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Modify leisure selection weights, override pathfinding targets, track leisure statistics
- **Risk level**: Low (managed method)
- **Side effects**: Leisure counter manipulation affects citizen happiness

### Candidate 2: `Game.Simulation.ParkAISystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Modify maintenance decay rates, adjust service coverage calculations
- **Risk level**: Low
- **Side effects**: Changing decay rate affects maintenance vehicle demand

### Candidate 3: `Game.Simulation.AttractionSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Modify attractiveness calculations, add custom attractiveness factors
- **Risk level**: Low
- **Side effects**: Attractiveness affects tourism and land value

## Mod Blueprint

- **Systems to create**: Custom leisure weight system (adjust per-type weights), park analytics system (tracking maintenance and coverage), attractiveness overlay
- **Components to add**: Optional extended leisure tracking (visit counts, spending totals)
- **Patches needed**: LeisureSystem.OnUpdate (postfix for monitoring), ParkAISystem static methods (modify coverage calculation)
- **Settings**: Leisure weight multipliers per type, maintenance decay rate modifier, attractiveness scaling
- **UI changes**: Leisure statistics panel, park maintenance overlay, attractiveness heat map

## Examples

### Example 1: Monitor Citizen Leisure Activity

```csharp
public partial class LeisureMonitorSystem : GameSystemBase
{
    private EntityQuery _leisureQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _leisureQuery = GetEntityQuery(
            ComponentType.ReadOnly<Leisure>(),
            ComponentType.ReadOnly<Citizen>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _leisureQuery.ToEntityArray(Allocator.Temp);
        var citizens = _leisureQuery.ToComponentDataArray<Citizen>(Allocator.Temp);
        int seekingLeisure = 0, atProvider = 0;
        float totalCounter = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            Leisure leisure = EntityManager.GetComponentData<Leisure>(entities[i]);
            if (leisure.m_TargetAgent != Entity.Null)
                atProvider++;
            else
                seekingLeisure++;
            totalCounter += citizens[i].m_LeisureCounter;
        }

        float avg = entities.Length > 0 ? totalCounter / entities.Length : 0;
        Log.Info($"Leisure: {entities.Length} active ({seekingLeisure} seeking, {atProvider} at provider)");
        Log.Info($"  Average leisure counter: {avg:F1}");
        entities.Dispose();
        citizens.Dispose();
    }
}
```

### Example 2: Modify Park Maintenance Behavior via Prefabs

```csharp
public partial class ParkBoostSystem : GameSystemBase
{
    private EntityQuery _parkPrefabQuery;
    private bool _applied;

    protected override void OnCreate()
    {
        base.OnCreate();
        _parkPrefabQuery = GetEntityQuery(
            ComponentType.ReadWrite<ParkData>(),
            ComponentType.ReadOnly<PrefabData>()
        );
    }

    protected override void OnUpdate()
    {
        if (_applied) return;
        _applied = true;

        var entities = _parkPrefabQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            ParkData data = EntityManager.GetComponentData<ParkData>(entity);
            data.m_MaintenancePool *= 2;    // Double maintenance pool (slower decay)
            data.m_AllowHomeless = true;     // All parks allow homeless
            EntityManager.SetComponentData(entity, data);
        }
        entities.Dispose();
    }
}
```

### Example 3: Track Attractiveness by Building

```csharp
public partial class AttractivenessTrackerSystem : GameSystemBase
{
    private EntityQuery _attractionQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _attractionQuery = GetEntityQuery(
            ComponentType.ReadOnly<AttractivenessProvider>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _attractionQuery.ToEntityArray(Allocator.Temp);
        int totalAttractiveness = 0;

        foreach (Entity entity in entities)
        {
            var provider = EntityManager.GetComponentData<AttractivenessProvider>(entity);
            totalAttractiveness += provider.m_Attractiveness;

            if (EntityManager.HasComponent<Park>(entity))
            {
                var park = EntityManager.GetComponentData<Park>(entity);
                var prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
                if (EntityManager.HasComponent<ParkData>(prefabRef.m_Prefab))
                {
                    var parkData = EntityManager.GetComponentData<ParkData>(prefabRef.m_Prefab);
                    float maintenanceRatio = (float)park.m_Maintenance / math.max(1, parkData.m_MaintenancePool);
                    Log.Info($"Park {entity}: attract={provider.m_Attractiveness}, maint={maintenanceRatio:P0}");
                }
            }
        }

        Log.Info($"Total city attractiveness: {totalAttractiveness}");
        entities.Dispose();
    }
}
```

### Example 4: Boost Leisure Provider Efficiency

```csharp
public partial class LeisureBoostSystem : GameSystemBase
{
    private EntityQuery _providerPrefabQuery;
    private bool _applied;

    protected override void OnCreate()
    {
        base.OnCreate();
        _providerPrefabQuery = GetEntityQuery(
            ComponentType.ReadWrite<LeisureProviderData>(),
            ComponentType.ReadOnly<PrefabData>()
        );
    }

    protected override void OnUpdate()
    {
        if (_applied) return;
        _applied = true;

        var entities = _providerPrefabQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            LeisureProviderData data = EntityManager.GetComponentData<LeisureProviderData>(entity);

            // Boost park and beach types for outdoor leisure focus
            if (data.m_LeisureType == LeisureType.CityPark ||
                data.m_LeisureType == LeisureType.CityBeach)
            {
                data.m_Efficiency = (int)(data.m_Efficiency * 1.5f);
            }
            EntityManager.SetComponentData(entity, data);
        }
        entities.Dispose();
    }
}
```

### Example 5: Understanding the Leisure Weight Formula

```csharp
// LeisureSystem.GetWeight() formula explained:
// Each leisure type has four factors:
//
// 1. ageWeight: Base preference by citizen age (Child/Teen/Adult/Elderly)
//    Example for Meals: Child=10, Teen=25, Adult=35, Elderly=35
//    Example for CityPark: Child=30, Teen=25, Adult=30, Elderly=15
//
// 2. weatherModifier: Environmental conditions
//    CityPark: 2.0 * (1 - 0.95 * weatherBadness) -- heavily penalized by rain
//    CityBeach: 0.05 + 4 * saturate(0.35 - weather) * saturate((temp - 20) / 30)
//              -- needs warm (>20C) and clear weather
//    Travel: 0.5 + saturate((30 - temp) / 50) -- prefers cooler weather
//    Others: 1.0 (no weather effect)
//
// 3. wealthScale: Fixed multiplier per type (most = 10, Travel = 1)
//
// 4. wealthThreshold: smoothstep(wealthMin, 1, (wealth + 5000) / 10000)
//    Meals: wealthMin=0.2 (accessible to lower incomes)
//    Entertainment: wealthMin=0.3
//    Commercial: wealthMin=0.4
//    CityPark/Indoor/Beach: wealthMin=0 (free, always accessible)
//    Travel: wealthMin=0.5 (expensive, requires higher income)
//
// Final weight = ageWeight * weatherModifier * wealthScale * wealthThreshold
// Weights are normalized across all types, then randomly selected
```

## Open Questions

- [ ] How `m_LeisureCounter` naturally decreases over time -- likely handled by `CitizenBehaviorSystem` using `ChanceCitizenDecreaseLeisureCounter` from `LeisureParametersData`
- [ ] The exact `kUpdateInterval` constant used in the LeisureSystem (referenced but not visible in decompilation)
- [ ] How the Meeting system (AddMeetingSystem) organizes group leisure activities for Travel/Sightseeing/Attractions
- [ ] Whether park maintenance vehicles (ParkMaintenance component) affect maintenance restoration rate or just trigger dispatch

## Sources

- Decompiled from: Game.dll -- Game.Simulation.LeisureSystem, Game.Simulation.ParkAISystem, Game.Simulation.AttractionSystem, Game.Simulation.TerrainAttractivenessSystem
- Runtime components: Game.Buildings.Park, Game.Buildings.LeisureProvider, Game.Buildings.AttractivenessProvider, Game.Citizens.Leisure
- Prefab types: Game.Prefabs.LeisureProviderData, Game.Prefabs.ParkData, Game.Prefabs.AttractionData, Game.Prefabs.AttractivenessParameterData, Game.Prefabs.LeisureParametersData
- Enums: Game.Agents.LeisureType, Game.Simulation.AttractionSystem.AttractivenessFactor
