# Research: Tourism Economy

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How tourists spawn, find attractions and hotels, spend money, and eventually leave the city in Cities: Skylines II.

**Why**: Understanding the tourism pipeline enables mods that adjust tourist spawn rates, modify attractiveness calculations, change hotel pricing, or add new tourist behaviors.

**Boundaries**: Citizen leisure behavior for residents (non-tourists) is out of scope. Company economics (covered in CompanySimulation) and workplace labor (covered in WorkplaceLaborMarket) are referenced but not re-documented here.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Citizens | TouristHousehold, LodgingSeeker |
| Game.dll | Game.City | Tourism (city-level singleton) |
| Game.dll | Game.Companies | LodgingProvider |
| Game.dll | Game.Buildings | AttractivenessProvider |
| Game.dll | Game.Prefabs | AttractionData, Attraction, AttractivenessParameterData, LeisureParametersData |
| Game.dll | Game.Simulation | TouristSpawnSystem, TourismSystem, AttractionSystem, LodgingProviderSystem, TouristFindTargetSystem, TouristHouseholdBehaviorSystem, TouristLeaveSystem |

## Component Map

### `TouristHousehold` (Game.Citizens)

Marks a household entity as a tourist household. Added when a tourist spawns.

| Field | Type | Description |
|-------|------|-------------|
| m_Hotel | Entity | The lodging provider (hotel company) entity this tourist is checked into, or Entity.Null if none |
| m_LeavingTime | uint | Simulation frame at which the tourist was scheduled to leave (set to 0 initially) |

*Source: `Game.dll` -> `Game.Citizens.TouristHousehold`*

### `LodgingSeeker` (Game.Citizens)

Tag component (zero-size) added to tourist households that need to find a hotel or attraction. Triggers the TouristFindTargetSystem to pathfind for this household.

| Field | Type | Description |
|-------|------|-------------|
| (none) | - | Empty marker component |

*Source: `Game.dll` -> `Game.Citizens.LodgingSeeker`*

### `Tourism` (Game.City)

City-level singleton tracking aggregate tourism statistics. Written by TourismSystem.

| Field | Type | Description |
|-------|------|-------------|
| m_CurrentTourists | int | Number of tourist citizens currently in the city |
| m_AverageTourists | int | Estimated average tourists based on spawn probability (derived from attractiveness) |
| m_Attractiveness | int | Current city attractiveness score (sigmoid-transformed sum of all AttractivenessProvider values) |
| m_Lodging | int2 | x = occupied rooms (current renters), y = total rooms (renters + free rooms) |

*Source: `Game.dll` -> `Game.City.Tourism`*

### `LodgingProvider` (Game.Companies)

Attached to hotel company entities to track room availability and pricing.

| Field | Type | Description |
|-------|------|-------------|
| m_FreeRooms | int | Number of unoccupied rooms in this hotel |
| m_Price | int | Daily lodging price per room (calculated from market price * consumption per day) |

*Source: `Game.dll` -> `Game.Companies.LodgingProvider`*

### `AttractivenessProvider` (Game.Buildings)

Runtime component on buildings that contribute to city attractiveness. Calculated by AttractionSystem.

| Field | Type | Description |
|-------|------|-------------|
| m_Attractiveness | int | Effective attractiveness value after efficiency, maintenance, terrain, and upgrade modifiers |

*Source: `Game.dll` -> `Game.Buildings.AttractivenessProvider`*

### `AttractionData` (Game.Prefabs)

Prefab component defining a building's base attractiveness. Implements ICombineData for upgrade stacking.

| Field | Type | Description |
|-------|------|-------------|
| m_Attractiveness | int | Base attractiveness value defined in the prefab |

*Source: `Game.dll` -> `Game.Prefabs.AttractionData`*

### `AttractivenessParameterData` (Game.Prefabs)

Global singleton with parameters controlling weather effects on tourism and terrain attractiveness.

| Field | Type | Description |
|-------|------|-------------|
| m_ForestEffect | float | Attractiveness bonus from nearby forest |
| m_ForestDistance | float | Maximum distance for forest effect |
| m_ShoreEffect | float | Attractiveness bonus from nearby shore |
| m_ShoreDistance | float | Maximum distance for shore effect |
| m_HeightBonus | float3 | Height-based attractiveness (min height, max height, bonus) |
| m_AttractiveTemperature | float2 | Temperature range (min, max) that boosts tourism |
| m_ExtremeTemperature | float2 | Temperature range beyond which tourism is penalized |
| m_TemperatureAffect | float2 | x = max bonus for ideal temp, y = max penalty for extreme temp |
| m_RainEffectRange | float2 | Precipitation range that affects tourism during rain |
| m_SnowEffectRange | float2 | Precipitation range that affects tourism during snow |
| m_SnowRainExtremeAffect | float3 | x = snow penalty, y = rain penalty, z = storm penalty |

*Source: `Game.dll` -> `Game.Prefabs.AttractivenessParameterData`*

### `LeisureParametersData` (Game.Prefabs)

Global singleton with leisure/tourism consumption parameters.

| Field | Type | Description |
|-------|------|-------------|
| m_TravelingPrefab | Entity | Prefab used for traveling leisure type |
| m_AttractionPrefab | Entity | Prefab used for attraction visits |
| m_SightseeingPrefab | Entity | Prefab used for sightseeing leisure |
| m_LeisureRandomFactor | int | Random factor for leisure decisions |
| m_ChanceCitizenDecreaseLeisureCounter | int | Probability citizens reduce leisure counter |
| m_ChanceTouristDecreaseLeisureCounter | int | Probability tourists reduce leisure counter |
| m_AmountLeisureCounterDecrease | int | Amount to decrease leisure counter |
| m_TouristLodgingConsumePerDay | int | Units of lodging resource consumed per day |
| m_TouristServiceConsumePerDay | int | Units of service consumed by tourists per day |

*Source: `Game.dll` -> `Game.Prefabs.LeisureParametersData`*

## System Map

### `TourismSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 32768 frames (approximately once per in-game day, since 262144 frames = 1 day)
- **Queries**:
  - AttractivenessProvider entities (exclude Temp, Deleted)
  - LodgingProvider + PropertyRenter entities (hotels, exclude Temp, Deleted)
  - AttractivenessParameterData singleton
- **Reads**: AttractivenessProvider.m_Attractiveness, LodgingProvider.m_FreeRooms, Renter buffer, CityModifier buffer, ClimateSystem weather data, CountHouseholdDataSystem.TouristCitizenCount
- **Writes**: Tourism (city singleton -- m_CurrentTourists, m_AverageTourists, m_Attractiveness, m_Lodging)
- **Key methods**:
  - `TourismJob.Execute()` -- Sums attractiveness from all providers using sigmoid formula: `200 / (1 + exp(-0.3 * sum(a^2/10000))) - 100`. Applies CityModifier for Attractiveness. Counts lodging occupancy. Calculates average tourists from spawn probability.
  - `GetTargetTourists(int attractiveness)` -- Converts attractiveness to target tourist count. Under 100: `attractiveness * 15`. Above 100: `1500 + 100 * log10(1 + (attractiveness - 100))`.
  - `GetSpawnProbability(int attractiveness, int currentTourists)` -- Returns probability (0-1) of spawning a tourist. Full speed when under 50% of target, quadratic decay from 50% to 110%, then base rate `attractiveness/1000`.
  - `GetWeatherEffect(...)` -- Multiplier (0.5-1.5) based on temperature, rain, snow, storms.
  - `GetTouristProbability(...)` -- Combines spawn probability with weather effect.
  - `GetTouristRandomStay()` -- Returns 262144 (one full in-game day in frames).

### `TouristSpawnSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**:
  - Household prefabs (ArchetypeData + HouseholdData)
  - Outside connections (exclude Electricity, WaterPipe, Temp, Deleted)
  - DemandParameterData singleton
  - AttractivenessParameterData singleton
- **Reads**: Tourism.m_Attractiveness, TouristCount statistic, weather data, DemandParameterData.m_TouristOCSpawnParameters
- **Writes**: Creates new Household entity with HouseholdFlags.Tourist, adds TouristHousehold component, sets CurrentBuilding to a random outside connection
- **Key methods**:
  - `SpawnTouristHouseholdJob.Execute()` -- Rolls random against `GetTouristProbability()`. If succeeds, creates a household entity with Tourist flag, adds TouristHousehold (hotel=null, leavingTime=0), picks random outside connection weighted by `m_TouristOCSpawnParameters`.

### `AttractionSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames (processes 1/16 of entities per frame via UpdateFrame)
- **Queries**:
  - Entities with AttractivenessProvider + PrefabRef + UpdateFrame (exclude Destroyed, Deleted, Temp)
  - AttractivenessParameterData singleton
- **Reads**: AttractionData (prefab), Efficiency buffer, Park + ParkData (maintenance), Transform + TerrainAttractiveness + HeightData (terrain), InstalledUpgrade buffer
- **Writes**: AttractivenessProvider.m_Attractiveness
- **Key methods**:
  - `AttractivenessJob.Execute()` -- Computes effective attractiveness for each building:
    1. Starts from base `AttractionData.m_Attractiveness` (from prefab)
    2. Combines upgrade bonuses via `UpgradeUtils.CombineStats()`
    3. Multiplies by building efficiency (unless it's a signature building)
    4. For parks: multiplies by `0.8 + 0.2 * (maintenance / maintenancePool)`
    5. Multiplies by terrain attractiveness factor: `1 + 0.01 * terrainAttractiveness`

### `TouristFindTargetSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**:
  - TouristHousehold + LodgingSeeker entities (exclude MovingAway, Target, Deleted, Temp)
- **Reads**: CurrentBuilding, PathInformation, LodgingProvider, Renter buffer, HouseholdCitizen buffer, OwnedVehicle buffer
- **Writes**: PathInformation (adds for pathfind request), Target, LodgingSeeker (removes on completion), TouristHousehold.m_Hotel, LodgingProvider.m_FreeRooms, Renter buffer (adds tourist as renter)
- **Key methods**:
  - `TouristFindTargetJob.Execute()` -- Two-phase process:
    1. If no PathInformation: initiates pathfind from current location to `SetupTargetType.TouristFindTarget` (uses public transport, taxi, pedestrian)
    2. If pathfind complete: checks destination. If destination has a LodgingProvider renter, enqueues hotel reservation. If destination is an attraction, enqueues meeting (LeisureType.Attractions). If no destination found, tourist moves away (TouristNoTarget).
  - `HotelReserveJob.Execute()` -- Processes hotel reservation queue. If free rooms > 0: decrements FreeRooms, adds tourist as Renter, sets TouristHousehold.m_Hotel. If no rooms: re-adds LodgingSeeker to try again.

### `TouristHouseholdBehaviorSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames (processes 1/16 of entities per frame via UpdateFrame)
- **Queries**:
  - TouristHousehold + UpdateFrame entities (exclude MovingAway, Deleted, Temp)
- **Reads**: Target, Building, Renter buffer, LodgingSeeker (tag check)
- **Writes**: TouristHousehold.m_Hotel (resets to null if hotel invalid), adds LodgingSeeker
- **Key methods**:
  - `TouristHouseholdTickJob.Execute()` -- Behavioral tick for tourist households:
    1. If tourist has a Target that is invalid (null or not a building), removes Target
    2. If already seeking lodging (has LodgingSeeker), skip
    3. Validates hotel assignment: checks if hotel entity still exists and tourist is still in its Renter buffer
    4. If hotel invalid, resets m_Hotel to null
    5. Adds LodgingSeeker to trigger new target search

### `LodgingProviderSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 262144 / (32 * 16) = 512 frames (runs 32 times per day, processing 1/16 per frame)
- **Queries**:
  - LodgingProvider + PropertyRenter + ServiceAvailable + UpdateFrame + PrefabRef + ProcessingCompany (exclude Deleted, Temp)
  - LeisureParametersData singleton
- **Reads**: PropertyRenter, BuildingData, BuildingPropertyData, SpawnableBuildingData, TouristHousehold, ResourceData, ResourcePrefabs, LeisureParametersData
- **Writes**: LodgingProvider (m_FreeRooms, m_Price), Resources (money transfer), ServiceAvailable, Renter buffer (cleanup), CompanyStatisticData
- **Key methods**:
  - `LodgingProviderJob.Execute()` -- Per-hotel update:
    1. Calculates room count: `lotSize.x * lotSize.y * level * spaceMultiplier`
    2. Cleans renter buffer (removes non-tourist renters)
    3. If overcapacity, evicts excess tourists (sets their m_Hotel to null)
    4. Charges each tourist: `lodgingConsumePerDay / updatesPerDay * marketPrice(Lodging)`
    5. Transfers money from tourist to hotel company
    6. Deducts lodging resources from hotel
    7. Updates m_FreeRooms and m_Price
  - `GetRoomCount(int2 lotSize, int level, BuildingPropertyData)` -- `lotSize.x * lotSize.y * level * spaceMultiplier`

### `TouristLeaveSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 512 frames (processes 1/16 per frame via UpdateFrame)
- **Queries**:
  - TouristHousehold entities (exclude MovingAway, Created, Deleted, Temp)
- **Reads**: TouristHousehold, LodgingProvider (m_Price), Resources (money), TimeSystem.normalizedTime
- **Writes**: Adds MovingAway component, enqueues TriggerAction (TouristLeftCity)
- **Key methods**:
  - `TouristLeaveJob.Execute()` -- Checks departure conditions:
    1. If tourist has no hotel and time > 0.8 (80% through the day): leaves with reason `TouristNoHotel`
    2. If tourist's money < hotel daily price and time > 0.7: leaves with reason `TouristNoMoney`
    3. Fires `TriggerType.TouristLeftCity` trigger on departure

## Data Flow

```
ATTRACTIVENESS CALCULATION (Every 16 frames, 1/16 of buildings)
  AttractionSystem
    For each building with AttractivenessProvider:
      base = AttractionData.m_Attractiveness (from prefab)
      base += upgrade bonuses (InstalledUpgrade -> AttractionData)
      base *= building efficiency (unless signature)
      base *= park maintenance factor (0.8 + 0.2 * maintenance ratio)
      base *= terrain factor (1 + 0.01 * terrain attractiveness)
      Writes AttractivenessProvider.m_Attractiveness
          |
          v
TOURISM STATISTICS (Every 32768 frames, ~1/day)
  TourismSystem
    Sum all AttractivenessProvider: sum(a^2 / 10000)
    Sigmoid: attractiveness = 200 / (1 + exp(-0.3 * sum)) - 100
    Apply CityModifier(Attractiveness)
    Count lodging: occupied rooms, total rooms
    Calculate average tourists from spawn probability
    Writes Tourism singleton (m_Attractiveness, m_CurrentTourists,
           m_AverageTourists, m_Lodging)
          |
          v
TOURIST SPAWNING (Every 16 frames)
  TouristSpawnSystem
    Reads Tourism.m_Attractiveness, current tourist count
    probability = GetSpawnProbability(attractiveness, currentTourists)
                  * GetWeatherEffect(weather)
    If random < probability:
      Create Household entity with HouseholdFlags.Tourist
      Add TouristHousehold (hotel=null, leavingTime=0)
      Place at random outside connection
          |
          v
TOURIST BEHAVIOR (Every 64 frames, 1/16 of tourists)
  TouristHouseholdBehaviorSystem
    For each tourist household:
      Validate current Target (remove if invalid)
      Validate hotel assignment (reset if hotel gone)
      If no hotel and not seeking: add LodgingSeeker
          |
          v
TARGET FINDING (Every 16 frames)
  TouristFindTargetSystem
    For each tourist with LodgingSeeker:
      Phase 1: Pathfind from current location (public transit/taxi/walk)
               Destination type = TouristFindTarget
      Phase 2: Process pathfind result
        If destination is hotel -> HotelReserveJob (book room)
        If destination is attraction -> AddMeeting (LeisureType.Attractions)
        If no destination -> MoveAway (TouristNoTarget)
    HotelReserveJob:
      If free rooms > 0: book room, set TouristHousehold.m_Hotel
      If no rooms: re-add LodgingSeeker to retry
          |
          v
HOTEL OPERATIONS (32x/day)
  LodgingProviderSystem
    For each hotel (LodgingProvider):
      Calculate room count from building size * level * multiplier
      Clean invalid renters
      Charge tourists: money -= price per update tick
      Hotel receives money, consumes lodging resource
      Update m_FreeRooms and m_Price
          |
          v
TOURIST DEPARTURE (Every 512 frames)
  TouristLeaveSystem
    For each tourist:
      If no hotel AND time > 0.8: leave (TouristNoHotel)
      If money < hotel price AND time > 0.7: leave (TouristNoMoney)
      Fires TriggerType.TouristLeftCity
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Base attractiveness | Attraction.m_Attractiveness | Game.Prefabs.Attraction (building prefab component) |
| Forest/shore terrain bonus | AttractivenessParameterData.m_ForestEffect, m_ShoreEffect | Game.Prefabs.AttractivenessParameterData (singleton) |
| Weather temperature range | AttractivenessParameterData.m_AttractiveTemperature | Game.Prefabs.AttractivenessParameterData (singleton) |
| Lodging consume per day | LeisureParametersData.m_TouristLodgingConsumePerDay | Game.Prefabs.LeisureParametersData (singleton) |
| Tourist service consume | LeisureParametersData.m_TouristServiceConsumePerDay | Game.Prefabs.LeisureParametersData (singleton) |
| Tourist OC spawn weights | DemandParameterData.m_TouristOCSpawnParameters | Game.Prefabs.DemandParameterData (singleton) |
| Room capacity | BuildingData.m_LotSize, SpawnableBuildingData.m_Level, BuildingPropertyData.m_SpaceMultiplier | Game.Prefabs (building prefabs) |
| Lodging market price | ResourceData for Resource.Lodging | Game.Prefabs.ResourceData |
| Updates per day constant | LodgingProviderSystem.kUpdatesPerDay | Hardcoded: 32 |
| Tourist random stay frames | TourismSystem.GetTouristRandomStay() | Hardcoded: 262144 (1 in-game day) |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.TourismSystem.GetTouristProbability`

- **Signature**: `static float GetTouristProbability(AttractivenessParameterData parameterData, int attractiveness, int numberOfCurrentTourists, ClimateSystem.WeatherClassification weatherClassification, float temperature, float precipitation, bool isRaining, bool isSnowing)`
- **Patch type**: Postfix
- **What it enables**: Override tourist spawn probability entirely. Boost or reduce tourist flow based on custom conditions (time of year, city size, policy toggles).
- **Risk level**: Low
- **Side effects**: Called by both TourismSystem (for statistics) and TouristSpawnSystem (for actual spawning). Changes affect both calculation paths.

### Candidate 2: `Game.Simulation.TourismSystem.GetTargetTourists`

- **Signature**: `static int GetTargetTourists(int attractiveness)`
- **Patch type**: Postfix
- **What it enables**: Change the target tourist count formula. Increase/decrease tourist cap for given attractiveness.
- **Risk level**: Low
- **Side effects**: Used by GetSpawnProbability to calculate spawn rate. Changing target also changes the rate at which tourists arrive.

### Candidate 3: `Game.Simulation.TourismSystem.GetWeatherEffect`

- **Signature**: `static float GetWeatherEffect(AttractivenessParameterData parameterData, ClimateSystem.WeatherClassification weatherClassification, float temperature, float precipitation, bool isRaining, bool isSnowing)`
- **Patch type**: Postfix
- **What it enables**: Modify weather's impact on tourism. Make tourism weather-proof or increase weather sensitivity.
- **Risk level**: Low
- **Side effects**: None beyond tourism spawn rate.

### Candidate 4: `Game.Simulation.LodgingProviderSystem.GetRoomCount`

- **Signature**: `static int GetRoomCount(int2 lotSize, int level, BuildingPropertyData buildingPropertyData)`
- **Patch type**: Postfix
- **What it enables**: Change hotel capacity formula. Increase or decrease rooms per hotel.
- **Risk level**: Low
- **Side effects**: Affects how many tourists can stay in a given hotel building.

### Candidate 5: `Game.Simulation.AttractionSystem.AttractivenessJob.Execute`

- **Signature**: `void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Prefix/Transpiler
- **What it enables**: Modify per-building attractiveness calculation (add new factors, change formula).
- **Risk level**: Medium (Burst-compiled job; may need transpiler)
- **Side effects**: Changes all building attractiveness values, cascading to city attractiveness and spawn rates.

## Mod Blueprint

- **Systems to create**: Custom `TourismModifierSystem` extending GameSystemBase for applying custom tourism bonuses
- **Components to add**: Optional custom component for per-building tourism modifier overrides
- **Patches needed**: TourismSystem.GetTouristProbability (Postfix) for spawn rate control, LodgingProviderSystem.GetRoomCount (Postfix) for capacity tuning
- **Settings**: Tourist spawn multiplier, weather effect toggle, hotel capacity multiplier, attractiveness bonus
- **UI changes**: Optional info panel showing detailed tourism breakdown

## Examples

### Example 1: Read City Tourism Statistics

Read the city-level Tourism singleton to display current tourism state.

```csharp
public partial class TourismMonitorSystem : GameSystemBase
{
    private CitySystem _citySystem;
    private EntityQuery _tourismQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _citySystem = World.GetOrCreateSystemManaged<CitySystem>();
    }

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        Entity city = _citySystem.City;

        if (!em.HasComponent<Tourism>(city)) return;

        Tourism tourism = em.GetComponentData<Tourism>(city);

        Log.Info($"Tourism Stats:");
        Log.Info($"  Current tourists: {tourism.m_CurrentTourists}");
        Log.Info($"  Average tourists: {tourism.m_AverageTourists}");
        Log.Info($"  Attractiveness: {tourism.m_Attractiveness}");
        Log.Info($"  Lodging: {tourism.m_Lodging.x} occupied / {tourism.m_Lodging.y} total rooms");

        float occupancyRate = (tourism.m_Lodging.y > 0)
            ? (float)tourism.m_Lodging.x / tourism.m_Lodging.y
            : 0f;
        Log.Info($"  Occupancy rate: {occupancyRate:P0}");
    }
}
```

### Example 2: Harmony Patch to Boost Tourist Spawn Rate

Multiply tourist spawn probability by a configurable factor.

```csharp
[HarmonyPatch(typeof(Game.Simulation.TourismSystem), "GetTouristProbability")]
public static class TouristSpawnBoostPatch
{
    public static float SpawnMultiplier { get; set; } = 2.0f;

    static void Postfix(ref float __result)
    {
        __result *= SpawnMultiplier;
    }
}
```

### Example 3: Query All Hotels and Their Occupancy

Enumerate all lodging providers and check room utilization.

```csharp
public partial class HotelMonitorSystem : GameSystemBase
{
    private EntityQuery _hotelQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _hotelQuery = GetEntityQuery(
            ComponentType.ReadOnly<LodgingProvider>(),
            ComponentType.ReadOnly<PropertyRenter>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _hotelQuery.ToEntityArray(Allocator.Temp);
        int totalRooms = 0;
        int totalFree = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            LodgingProvider provider = EntityManager.GetComponentData<LodgingProvider>(entities[i]);
            totalRooms += provider.m_FreeRooms;
            if (EntityManager.HasBuffer<Renter>(entities[i]))
            {
                int occupied = EntityManager.GetBuffer<Renter>(entities[i]).Length;
                totalRooms += occupied;
                totalFree += provider.m_FreeRooms;
            }
        }

        Log.Info($"Hotels: {entities.Length}, Rooms: {totalRooms}, Free: {totalFree}");
        entities.Dispose();
    }
}
```

### Example 4: Harmony Patch to Override Hotel Room Capacity

Change the formula for how many rooms a hotel building provides.

```csharp
[HarmonyPatch(typeof(Game.Simulation.LodgingProviderSystem), "GetRoomCount")]
public static class HotelCapacityPatch
{
    public static float CapacityMultiplier { get; set; } = 1.5f;

    static void Postfix(ref int __result)
    {
        __result = Mathf.RoundToInt(__result * CapacityMultiplier);
    }
}
```

### Example 5: Modify Attractiveness Parameters

Change global attractiveness parameters to make weather less impactful on tourism.

```csharp
public partial class WeatherProofTourismSystem : GameSystemBase
{
    private EntityQuery _paramQuery;
    private bool _applied;

    protected override void OnCreate()
    {
        base.OnCreate();
        _paramQuery = GetEntityQuery(
            ComponentType.ReadWrite<AttractivenessParameterData>()
        );
    }

    protected override void OnUpdate()
    {
        if (_applied) return;

        if (_paramQuery.CalculateEntityCount() == 0) return;

        Entity paramEntity = _paramQuery.GetSingletonEntity();
        AttractivenessParameterData data =
            EntityManager.GetComponentData<AttractivenessParameterData>(paramEntity);

        // Reduce weather penalties by 50%
        data.m_SnowRainExtremeAffect *= 0.5f;
        data.m_TemperatureAffect *= 0.5f;

        EntityManager.SetComponentData(paramEntity, data);
        _applied = true;

        Log.Info("Weather impact on tourism reduced by 50%");
    }
}
```

## Open Questions

- [ ] **Tourist spending at attractions**: The AddMeetingSystem enqueues meetings of LeisureType.Attractions, but the actual spending logic for attraction visits (vs. lodging spending) was not traced. Tourist service consumption (`m_TouristServiceConsumePerDay`) may be handled in the general leisure system.
- [ ] **m_LeavingTime usage**: The TouristHousehold.m_LeavingTime field is serialized but initialized to 0 in TouristSpawnSystem. It may be set elsewhere (possibly in a system not yet decompiled) or may be unused/deprecated.
- [ ] **Tourist pathfind destination selection**: The `SetupTargetType.TouristFindTarget` is passed to the pathfinder, but the scoring logic that selects between hotels and attractions is internal to the pathfind system and not visible in the systems decompiled here.
- [ ] **DemandParameterData.m_TouristOCSpawnParameters**: The float4 controls which outside connections tourists prefer to spawn at, but the exact interpretation of each component (road vs. air vs. sea) was not traced.
- [ ] **Tourist count vs. household count**: TourismSystem uses `CountHouseholdDataSystem.TouristCitizenCount` (citizen count) not household count. Each tourist household may contain multiple citizens.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Types decompiled: Game.Citizens.TouristHousehold, Game.Citizens.LodgingSeeker, Game.City.Tourism, Game.Companies.LodgingProvider, Game.Buildings.AttractivenessProvider, Game.Prefabs.AttractionData, Game.Prefabs.Attraction, Game.Prefabs.AttractivenessParameterData, Game.Prefabs.LeisureParametersData, Game.Simulation.TouristSpawnSystem, Game.Simulation.TourismSystem, Game.Simulation.AttractionSystem, Game.Simulation.LodgingProviderSystem, Game.Simulation.TouristFindTargetSystem, Game.Simulation.TouristHouseholdBehaviorSystem, Game.Simulation.TouristLeaveSystem
- Related research: CompanySimulation (LodgingProvider company context), WorkplaceLaborMarket (hotel employees), ResourceProduction (lodging resource)
