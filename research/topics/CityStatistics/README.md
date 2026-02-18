# Research: City Statistics & Data Panels

> **Status**: Complete
> **Date started**: 2026-02-17
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2 collects, aggregates, stores, and displays city-wide statistics (population, money, crime, production, passengers, etc.) through its time-series data pipeline and the Statistics UI panel.

**Why**: A mod may need to read existing statistics programmatically (e.g., display population trends, trigger actions at revenue thresholds), inject custom statistics into the existing pipeline (e.g., track a mod-specific metric over time), modify how existing stats are calculated, or create custom data panels that display novel city metrics.

**Boundaries**: Individual service-specific systems (fire, police, healthcare) are documented in their own research topics. This research focuses on the central statistics pipeline -- the `CityStatisticsSystem` hub, the event queue architecture, the time-series storage model, the `StatisticsUISystem` data binding, and the prefab-driven configuration layer.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.City | `CityStatistic`, `StatisticsEvent`, `StatisticParameter`, `Population`, `Tourism`, `StatisticType`, `StatisticUnitType`, `StatisticCollectionType` |
| Game.dll | Game.Simulation | `CityStatisticsSystem`, `ICityStatisticsSystem`, `CityServiceStatisticsSystem`, `CompanyStatisticsSystem`, `CrimeStatisticsSystem`, `WealthStatisticsSystem`, `WorkProviderStatisticsSystem`, `ElectricityStatisticsSystem`, `WaterStatisticsSystem`, `CityProductionStatisticSystem`, `CountHouseholdDataSystem`, `CountVehicleDataSystem`, `StatisticTriggerSystem` |
| Game.dll | Game.UI.InGame | `StatisticsUISystem`, `StatisticsPanel` |
| Game.dll | Game.Prefabs | `StatisticsPrefab`, `ParametricStatistic`, `StatisticsData`, `StatisticParameterData`, `UIStatisticsCategoryPrefab`, `UIStatisticsGroupPrefab`, `AgeStatistic`, `EducationStatistic`, `ResourceStatistic`, `IncomeStatistic`, `ExpenseStatistic`, `PassengerStatistic`, `CityServiceStatistic`, `LevelStatistic`, `MoveAwayStatistic`, `StatisticTriggerPrefab` |

## Component Map

### `CityStatistic` (Game.City)

The core time-series data element. Each statistic entity has a `DynamicBuffer<CityStatistic>` where each element is one sample period.

| Field | Type | Description |
|-------|------|-------------|
| m_Value | double | Current period's accumulated value (reset each period) |
| m_TotalValue | double | Computed total value -- meaning depends on `StatisticCollectionType` |

*Source: `Game.dll` -> `Game.City.CityStatistic`*

**m_TotalValue semantics by CollectionType:**
- **Point**: `m_TotalValue = previous period's m_Value` (snapshot of last period)
- **Cumulative**: `m_TotalValue = sum of all prior m_TotalValue + m_Value` (running total)
- **Daily**: `m_TotalValue = rolling 32-sample sum` (subtracts value from 32 samples ago)

### `StatisticsEvent` (Game.City)

Queued event that producer systems use to report values to `CityStatisticsSystem`.

| Field | Type | Description |
|-------|------|-------------|
| m_Statistic | StatisticType | Which statistic this contributes to |
| m_Parameter | int | Sub-parameter (education level, age group, resource index, etc.) |
| m_Change | float | Value to add to the current period's accumulator |

*Source: `Game.dll` -> `Game.City.StatisticsEvent`*

### `StatisticParameter` (Game.City)

Component on runtime statistic entities that disambiguates parametric variants (e.g., Age[Children] vs Age[Seniors]).

| Field | Type | Description |
|-------|------|-------------|
| m_Value | int | Parameter value matching `StatisticParameterData.m_Value` |

*Source: `Game.dll` -> `Game.City.StatisticParameter`*

### `Population` (Game.City)

Singleton component on the City entity with real-time population aggregates.

| Field | Type | Description |
|-------|------|-------------|
| m_Population | int | Current total population |
| m_PopulationWithMoveIn | int | Population including citizens still moving in |
| m_AverageHappiness | int | City average happiness (0-100) |
| m_AverageHealth | int | City average health (0-100, default 50) |

*Source: `Game.dll` -> `Game.City.Population`*

### `Tourism` (Game.City)

Singleton component on the City entity with tourism data.

| Field | Type | Description |
|-------|------|-------------|
| m_CurrentTourists | int | Currently visiting tourists |
| m_AverageTourists | int | Smoothed average tourist count |
| m_Attractiveness | int | City attractiveness score |
| m_Lodging | int2 | x = used lodging, y = total lodging capacity |

*Source: `Game.dll` -> `Game.City.Tourism`*

### `StatisticsData` (Game.Prefabs)

Prefab component that defines a statistic's metadata for display and collection.

| Field | Type | Description |
|-------|------|-------------|
| m_Category | Entity | `UIStatisticsCategoryPrefab` entity (tab in UI) |
| m_Group | Entity | `UIStatisticsGroupPrefab` entity (group within tab) |
| m_StatisticType | StatisticType | Which `StatisticType` this tracks |
| m_CollectionType | StatisticCollectionType | Point, Cumulative, or Daily |
| m_UnitType | StatisticUnitType | None, Money, Percent, Weight |
| m_Color | Color | Chart line/bar color |
| m_Stacked | bool | Whether to stack in grouped charts |

*Source: `Game.dll` -> `Game.Prefabs.StatisticsData`*

### `StatisticParameterData` (Game.Prefabs)

Buffer element on parametric statistic prefabs defining each sub-parameter.

| Field | Type | Description |
|-------|------|-------------|
| m_Value | int | Parameter value (e.g., 0=Children, 1=Teen, 2=Adult, 3=Senior for Age) |
| m_Color | Color | Color for this parameter's chart line |

*Source: `Game.dll` -> `Game.Prefabs.StatisticParameterData`*

## System Map

### `CityStatisticsSystem` (Game.Simulation)

The **central hub** for all city statistics. Collects events from producer systems, processes them into time-series buffers, and manages the lookup table.

- **Base class**: GameSystemBase (also implements ICityStatisticsSystem, ISerializable, IPostDeserialize)
- **Update phase**: Simulation
- **Update interval**: Every 8,192 frames (~32 times per in-game day)
- **Key constants**: `kUpdatesPerDay = 32`
- **Queries**:
  - `m_StatisticsPrefabQuery`: entities with `StatisticsData`
  - `m_StatisticsQuery`: entities with `CityStatistic` buffer
  - `m_CityQuery`: entities with `EconomyParameterData`
- **Reads**: `Tourism` (from City entity), `CountHouseholdDataSystem.HouseholdData`, `CitySystem.moneyAmount`
- **Writes**: `CityStatistic` buffer (appends new samples)
- **Key fields**:
  - `m_StatisticsLookup`: `NativeParallelHashMap<StatisticsKey, Entity>` -- maps (type, parameter) to statistic entity
  - `m_StatisticsEventQueue`: `NativeQueue<StatisticsEvent>` -- shared queue that all producer systems write to
  - `m_Writers`: `JobHandle` -- combined handle for all producer job dependencies
  - `m_SampleCount`: int -- total number of samples collected since game start
  - `eventStatisticsUpdated`: Action -- fired after each update (used by UI)
- **Key methods**:
  - `OnUpdate()` -- runs CityStatisticsJob, ProcessStatisticsJob, then ResetEntityJob in sequence
  - `GetStatisticValue(type, parameter)` -- reads latest m_TotalValue for a stat
  - `GetStatisticDataArray(type, parameter)` -- returns full time-series as NativeArray
  - `GetStatisticsEventQueue(out deps)` -- returns the queue for producer systems to enqueue events
  - `GetSafeStatisticsQueue(out deps)` -- returns a SafeStatisticQueue that no-ops when system is disabled
  - `AddWriter(JobHandle)` -- registers a producer job dependency
  - `InitializeLookup()` -- builds `m_StatisticsLookup` from prefabs and existing runtime entities

**OnUpdate job pipeline:**
1. **CityStatisticsJob** (IJob): Enqueues population, money, tourism, education, age, household, health, wellbeing, homeless stats from `CountHouseholdDataSystem.HouseholdData`
2. **ProcessStatisticsJob** (IJob): Dequeues all `StatisticsEvent` entries, looks up the target entity via `m_StatisticsLookup`, adds the change to `m_Value` in the latest `CityStatistic` buffer element
3. **ResetEntityJob** (IJob): Iterates all statistic entities, appends a new `CityStatistic` element for the next period:
   - **Cumulative**: new entry with `m_TotalValue = old.m_TotalValue + old.m_Value`, `m_Value = 0`
   - **Point**: new entry with `m_TotalValue = old.m_Value`, `m_Value = 0`
   - **Daily**: new entry with rolling 32-sample window (subtracts value from 32 entries ago)

### `CityServiceStatisticsSystem` (Game.Simulation)

Counts workers in city service buildings (police, fire, healthcare, etc.) by service type.

- **Base class**: GameSystemBase
- **Update phase**: Simulation (frame-batched via UpdateFrame)
- **Writes to queue**: `StatisticType.CityServiceWorkers` and `StatisticType.CityServiceMaxWorkers` with parameter = service index

### `CompanyStatisticsSystem` (Game.Simulation)

Counts companies, workers, and max workers broken down by resource output type and company category (commercial/industrial/office).

- **Base class**: GameSystemBase
- **Writes to queue**: `ServiceCount`/`ProcessingCount`/`OfficeCount`, `ServiceWorkers`/`ProcessingWorkers`/`OfficeWorkers`, `ServiceMaxWorkers`/`ProcessingMaxWorkers`/`OfficeMaxWorkers` -- all with resource index parameter

### `CrimeStatisticsSystem` (Game.Simulation)

Computes average crime rate across all `CrimeProducer` buildings.

- **Base class**: GameSystemBase
- **Writes to queue**: `StatisticType.CrimeRate` (percentage 0-100)

### `WealthStatisticsSystem` (Game.Simulation)

Sums wealth for households, commercial companies, and industrial/office companies.

- **Base class**: GameSystemBase
- **Writes to queue**: `StatisticType.HouseholdWealth`, `StatisticType.ServiceWealth`, `StatisticType.ProcessingWealth`, `StatisticType.OfficeWealth` -- all with resource index parameter

### `WorkProviderStatisticsSystem` (Game.Simulation)

Tracks senior worker demand and senior workplace in-demand percentage.

- **Base class**: GameSystemBase
- **Writes to queue**: `StatisticType.SeniorWorkerInDemandPercentage`

### `ElectricityStatisticsSystem` (Game.Simulation)

Counts electricity production, consumption, fulfilled consumption, and battery capacity.

- **Base class**: GameSystemBase (also IElectricityStatisticsSystem)
- **Does NOT write to statistics queue** -- maintains its own internal state, queried directly by UI systems

### `WaterStatisticsSystem` (Game.Simulation)

Counts water/sewage production, consumption, and fulfillment.

- **Base class**: GameSystemBase
- **Does NOT write to statistics queue** -- similar to ElectricityStatisticsSystem, maintains own state

### `CityProductionStatisticSystem` (Game.Simulation)

Tracks resource production and consumption chains (industry inputs/outputs, citizen consumption, trade, upkeep).

- **Base class**: GameSystemBase (also ISerializable)
- **Does NOT write to statistics queue** -- maintains `NativeArray<CityResourceUsage>` indexed by resource

### `CountHouseholdDataSystem` (Game.Simulation)

Counts all household and citizen demographics. Its `HouseholdData` struct is used by `CityStatisticsSystem.CityStatisticsJob`.

- **Base class**: GameSystemBase (also ISerializable)
- **Key output**: `HouseholdData` struct with fields for population, children/teen/adult/senior counts, education levels, worker counts, tourist citizens, homeless, happiness, health totals
- **Consumed by**: `CityStatisticsSystem.CityStatisticsJob` (not via queue -- direct struct access)

### `StatisticTriggerSystem` (Game.Simulation)

Reads statistics values and fires `TriggerAction` events when thresholds are met (used for milestones, achievements).

- **Base class**: GameSystemBase
- **Reads**: `CityStatistic` buffers via `CityStatisticsSystem.GetStatisticDataArray()`
- **Writes**: `TriggerAction` queue entries

### `StatisticsUISystem` (Game.UI.InGame)

The UI system that binds statistics data to the Statistics panel in the game UI.

- **Base class**: UISystemBase
- **Binding group**: `"statistics"`
- **Key bindings**:
  - `"statistics.categories"` (RawValueBinding) -- list of category tabs
  - `"statistics.groups"` (RawMapBinding<Entity>) -- groups within a category
  - `"statistics.data"` (RawValueBinding) -- chart data for selected statistics
  - `"statistics.selectedStatistics"` (RawValueBinding) -- currently selected stat items
  - `"statistics.sampleRange"` (ValueBinding<int>) -- visible time range
  - `"statistics.sampleCount"` (ValueBinding<int>) -- total available samples
  - `"statistics.activeCategory"` (GetterValueBinding<Entity>) -- currently selected category
  - `"statistics.activeGroup"` (GetterValueBinding<Entity>) -- currently selected group
  - `"statistics.stacked"` (GetterValueBinding<bool>) -- stacking mode
  - `"statistics.updatesPerDay"` (GetterValueBinding<int>) -- always returns 32
- **Key trigger bindings**:
  - `"statistics.addStat"` -- add a stat to the chart
  - `"statistics.removeStat"` -- remove a stat from the chart
  - `"statistics.clearStats"` -- clear all selected stats
  - `"statistics.setSampleRange"` -- set the visible time range
  - `"statistics.setActiveCategory"` -- switch category tab
  - `"statistics.setActiveGroup"` -- switch group
- **Data flow**: Subscribes to `ICityStatisticsSystem.eventStatisticsUpdated` to trigger `m_DataBinding.Update()` after each statistics cycle

## Data Flow

```
PRODUCER SYSTEMS (run on their own schedules)
    CityServiceStatisticsSystem ----+
    CompanyStatisticsSystem ---------+
    CrimeStatisticsSystem -----------+
    WealthStatisticsSystem ----------+---> NativeQueue<StatisticsEvent>
    WorkProviderStatisticsSystem ----+     (shared, thread-safe)
    [Other systems using              |
     GetStatisticsEventQueue()]  ----+
                                      |
CITY STATISTICS SYSTEM (every 8192 frames = 32/day)
    |
    v
1. CityStatisticsJob
    Enqueues: Population, Money, Tourism, HouseholdCount,
    Age[0-3], Education[0-4], Health, Wellbeing, Workers,
    Unemployed, Homeless, AdultsCount
    (reads CountHouseholdDataSystem.HouseholdData directly)
    |
    v
2. ProcessStatisticsJob
    Dequeues ALL events from NativeQueue
    For each event:
      Look up entity via m_StatisticsLookup[type, parameter]
      Add m_Change to latest CityStatistic.m_Value
    |
    v
3. ResetEntityJob
    For each statistic entity:
      Append new CityStatistic buffer element
      Point:       m_TotalValue = old.m_Value
      Cumulative:  m_TotalValue = old.m_TotalValue + old.m_Value
      Daily:       m_TotalValue = rolling 32-sample sum
    |
    v
4. eventStatisticsUpdated fires
    |
    v
StatisticsUISystem receives callback
    Rebinds chart data via GetStatisticDataArrayLong()
    |
    v
UI panel displays updated charts
```

## Prefab & Configuration

### Statistics Prefab Hierarchy

Each statistic in the UI is defined by a prefab:

| Prefab Type | Purpose | Example |
|-------------|---------|---------|
| `UIStatisticsCategoryPrefab` | Top-level tab in Statistics panel | "Population", "Economy" |
| `UIStatisticsGroupPrefab` | Group within a category | "Wealth", "Workers" |
| `StatisticsPrefab` | Individual statistic (non-parametric) | "Money", "CrimeRate" |
| `ParametricStatistic` (abstract) | Parametric statistic with sub-parameters | Base class |
| `AgeStatistic` | Age distribution breakdown | Children/Teen/Adult/Senior |
| `EducationStatistic` | Education level breakdown | Uneducated through Highly Educated |
| `ResourceStatistic` | Resource-based breakdown | Trade by resource type |
| `IncomeStatistic` | Income source breakdown | By IncomeSource enum |
| `ExpenseStatistic` | Expense source breakdown | By ExpenseSource enum |
| `PassengerStatistic` | Passenger counts | Bus/Train/Tram/etc. |
| `CityServiceStatistic` | Service workers | By CityService enum |
| `LevelStatistic` | Building level distribution | Levels 1-5 |
| `MoveAwayStatistic` | Move-away reasons | By reason code |

### Collection Types

| Type | Meaning | m_TotalValue formula | Use case |
|------|---------|---------------------|----------|
| Point | Snapshot value | `= previous.m_Value` | Population, worker counts |
| Cumulative | Running total | `= previous.m_TotalValue + previous.m_Value` | Income, expenses, births/deaths |
| Daily | Rolling 32-sample window | `= previous.m_TotalValue + previous.m_Value - value_32_ago.m_Value` | Passenger counts, mail |

### Key Configuration Values

| Value | Source | Default | Effect |
|-------|--------|---------|--------|
| Update interval | `CityStatisticsSystem.GetUpdateInterval()` | 8192 frames | How often statistics are sampled |
| Updates per day | `kUpdatesPerDay` constant | 32 | Samples per in-game day |
| Default sample range | `StatisticsUISystem.OnGameLoaded()` | 32 (1 day) | Initial visible chart range |
| Hash map capacity | `CityStatisticsSystem.OnCreate()` | 64 initial | Starting capacity for statistics lookup |
| Rolling window | `ResetEntityJob` for Daily type | 32 samples | Rolling sum lookback period |

## Harmony Patch Points

### Candidate 1: `CityStatisticsSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Inject additional statistics events before processing (Prefix), or read/modify results after processing (Postfix)
- **Risk level**: Medium -- system coordinates multiple jobs via dependencies
- **Side effects**: Prefix could disrupt job chain if not careful with dependencies

### Candidate 2: `CityStatisticsSystem.GetStatisticValue`

- **Signature**: `public int GetStatisticValue(StatisticType type, int parameter = 0)`
- **Patch type**: Postfix
- **What it enables**: Modify reported values (e.g., apply multipliers, add offsets)
- **Risk level**: Low -- pure read method with no side effects
- **Side effects**: Affects all consumers including UI and trigger systems

### Candidate 3: `StatisticsUISystem.BindData`

- **Signature**: `private void BindData(IJsonWriter binder, StatItem stat)`
- **Patch type**: Prefix or Transpiler
- **What it enables**: Inject additional chart data, modify displayed values, add custom chart datasets
- **Risk level**: Medium -- private method, could break with updates
- **Side effects**: Only affects UI display

### Candidate 4: `StatisticsUISystem.BindCategories`

- **Signature**: `private void BindCategories(IJsonWriter binder)`
- **Patch type**: Postfix
- **What it enables**: Add custom category tabs to the statistics panel
- **Risk level**: Medium -- private method
- **Side effects**: Only affects UI display

## Mod Blueprint

- **Systems to create**:
  - Custom statistics producer system (enqueues `StatisticsEvent` to the shared queue)
  - Optional: Custom UI system extending `UISystemBase` for a dedicated data panel
- **Components to add**: None required for reading/writing existing stats. Custom `StatisticsPrefab` for new stats visible in the UI.
- **Patches needed**:
  - None for reading statistics (use `ICityStatisticsSystem` API directly)
  - Postfix on `GetStatisticValue` to modify reported values
  - Postfix on `BindCategories`/`BindGroups` to add custom entries to the UI
- **Settings**: Sample range, stat display preferences
- **UI changes**: Custom data panel or additional entries in existing Statistics panel

## Examples

### Example 1: Read City Population and Money

Read the current values of built-in statistics from any system.

```csharp
public partial class ReadCityStatsSystem : GameSystemBase
{
    private CityStatisticsSystem m_StatisticsSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_StatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
    }

    protected override void OnUpdate()
    {
        // Read current population (latest m_TotalValue for Point-type stat)
        int population = m_StatisticsSystem.GetStatisticValue(
            StatisticType.Population);

        // Read current money
        long money = m_StatisticsSystem.GetStatisticValueLong(
            StatisticType.Money);

        // Read crime rate (percentage 0-100)
        int crimeRate = m_StatisticsSystem.GetStatisticValue(
            StatisticType.CrimeRate);

        // Read household count
        int households = m_StatisticsSystem.GetStatisticValue(
            StatisticType.HouseholdCount);

        // Read education breakdown (parameter = education level 0-4)
        int uneducated = m_StatisticsSystem.GetStatisticValue(
            StatisticType.EducationCount, 0);
        int highlyEducated = m_StatisticsSystem.GetStatisticValue(
            StatisticType.EducationCount, 4);

        Log.Info($"Pop: {population}, Money: {money}, Crime: {crimeRate}%");
        Log.Info($"Households: {households}, Uneducated: {uneducated}, " +
                 $"Highly Educated: {highlyEducated}");
    }
}
```

### Example 2: Query Historical Statistics (Time-Series)

Retrieve the full time-series array for a statistic to analyze trends.

```csharp
public partial class HistoricalStatsSystem : GameSystemBase
{
    private CityStatisticsSystem m_StatisticsSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_StatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Get the population trend over the last N samples.
    /// Each sample is ~8192 frames apart (32 per in-game day).
    /// </summary>
    public void LogPopulationTrend(int sampleCount)
    {
        // CompleteWriters ensures all producer jobs have finished
        m_StatisticsSystem.CompleteWriters();

        // GetStatisticDataArrayLong returns all historical m_TotalValue entries
        NativeArray<long> history = m_StatisticsSystem.GetStatisticDataArrayLong(
            StatisticType.Population);

        int totalSamples = m_StatisticsSystem.sampleCount;
        int start = math.max(0, history.Length - sampleCount);

        Log.Info($"Population trend ({history.Length - start} samples):");
        for (int i = start; i < history.Length; i++)
        {
            Log.Info($"  Sample {i}: {history[i]}");
        }

        // For raw CityStatistic data (both m_Value and m_TotalValue):
        NativeArray<CityStatistic> rawData = m_StatisticsSystem.GetStatisticArray(
            StatisticType.Population);
        if (rawData.Length > 1)
        {
            CityStatistic latest = rawData[rawData.Length - 1];
            CityStatistic previous = rawData[rawData.Length - 2];
            double change = latest.m_TotalValue - previous.m_TotalValue;
            Log.Info($"Population change: {change:+0;-0;0}");
        }

        history.Dispose();
    }
}
```

### Example 3: Contribute Custom Statistics to the Event Queue

Write a custom producer system that feeds data into the existing statistics pipeline.

```csharp
/// <summary>
/// Injects a custom statistic value into the CityStatisticsSystem event queue.
/// This value is accumulated alongside all other producer systems and stored
/// in the time-series buffer during the next statistics update cycle.
///
/// IMPORTANT: The StatisticType must have a corresponding StatisticsPrefab
/// registered, or the event will be silently dropped by ProcessStatisticsJob
/// (the lookup will not find a matching entity).
/// </summary>
public partial class CustomStatProducerSystem : GameSystemBase
{
    private CityStatisticsSystem m_StatisticsSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_StatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
    }

    protected override void OnUpdate()
    {
        // Get the shared event queue (thread-safe for parallel writes)
        JobHandle deps;
        NativeQueue<StatisticsEvent> queue =
            m_StatisticsSystem.GetStatisticsEventQueue(out deps);

        // Wait for any pending writer jobs
        deps.Complete();

        // Enqueue a value for an EXISTING statistic type
        // (e.g., add to tourist income from a mod-created attraction)
        queue.Enqueue(new StatisticsEvent
        {
            m_Statistic = StatisticType.TouristIncome,
            m_Change = 5000f,   // Additional income amount
            m_Parameter = 0     // Default parameter
        });

        // Register our job dependency so CityStatisticsSystem waits for us
        m_StatisticsSystem.AddWriter(Dependency);
    }
}
```

### Example 4: Modify a Statistic Value via Harmony Postfix

Patch `GetStatisticValue` to adjust reported statistics (e.g., apply a difficulty multiplier).

```csharp
using HarmonyLib;
using Game.City;
using Game.Simulation;

/// <summary>
/// Patches CityStatisticsSystem.GetStatisticValue to apply a multiplier
/// to the crime rate, making it appear higher or lower in the UI and
/// any system that reads crime stats.
/// </summary>
[HarmonyPatch(typeof(CityStatisticsSystem),
    nameof(CityStatisticsSystem.GetStatisticValue),
    new[] { typeof(StatisticType), typeof(int) })]
public static class CrimeRateMultiplierPatch
{
    public static float CrimeMultiplier { get; set; } = 1.5f;

    static void Postfix(ref int __result, StatisticType type)
    {
        if (type == StatisticType.CrimeRate)
        {
            __result = (int)(__result * CrimeMultiplier);
            // Clamp to valid percentage range
            if (__result > 100) __result = 100;
        }
    }
}
```

### Example 5: Subscribe to Statistics Updates for Real-Time Monitoring

Listen for statistics update events to build a live monitoring dashboard.

```csharp
/// <summary>
/// Subscribes to the statistics update event and logs key metrics
/// each time statistics are recalculated (32 times per in-game day).
/// </summary>
public partial class StatisticsMonitorSystem : GameSystemBase
{
    private ICityStatisticsSystem m_StatisticsSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_StatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();

        // Subscribe to the update event
        m_StatisticsSystem.eventStatisticsUpdated += OnStatisticsUpdated;
    }

    protected override void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        m_StatisticsSystem.eventStatisticsUpdated -= OnStatisticsUpdated;
        base.OnDestroy();
    }

    protected override void OnUpdate() { }

    private void OnStatisticsUpdated()
    {
        int pop = m_StatisticsSystem.GetStatisticValue(StatisticType.Population);
        long money = m_StatisticsSystem.GetStatisticValueLong(StatisticType.Money);
        int crime = m_StatisticsSystem.GetStatisticValue(StatisticType.CrimeRate);
        int happiness = m_StatisticsSystem.GetStatisticValue(
            StatisticType.Wellbeing);
        int tourists = m_StatisticsSystem.GetStatisticValue(
            StatisticType.TouristCount);

        // Trigger mod-specific actions based on thresholds
        if (crime > 75)
        {
            Log.Warn($"High crime alert! Rate: {crime}%");
        }
        if (pop > 0 && money / pop < 100)
        {
            Log.Warn($"Low per-capita funds: {money / pop}");
        }

        Log.Info($"[Stats] Pop={pop} Money={money} Crime={crime}% " +
                 $"Happy={happiness} Tourists={tourists} " +
                 $"(sample #{m_StatisticsSystem.sampleCount})");
    }
}
```

## Open Questions

- [ ] **Custom StatisticType values**: The `StatisticType` enum ends at `Count = 63`. Can a mod define values beyond 63 and have them processed, or does `ProcessStatisticsJob` hard-reject unknown types? The lookup is hash-based (StatisticsKey), so it should work if a matching prefab entity exists, but this is untested.
- [ ] **Custom StatisticsPrefab registration**: Creating a new `StatisticsPrefab` at runtime and having `InitializeLookup()` pick it up requires the prefab to be in the `PrefabSystem` before `CityStatisticsSystem` initializes. The exact timing for mod-added prefabs vs the statistics lookup initialization needs testing.
- [ ] **UI extensibility**: Can a mod inject additional entries into the `StatisticsUISystem.BindCategories` and `BindGroups` methods via Harmony, or does the category/group system require prefab-level registration to appear in the UI?
- [ ] **Daily collection type window size**: The rolling window is hardcoded to 32 samples in `ResetEntityJob`. Is this always exactly 1 in-game day, or can simulation speed changes cause drift?
- [ ] **Thread safety of GetStatisticValue**: `GetStatisticValueDouble` calls `m_Writers.Complete()` which blocks the main thread. In a Burst job context, can the static overload `GetStatisticValue(lookup, stats, type, parameter)` be safely called without blocking?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II) using ilspycmd v9.1
- Game version tested: CS2 as of 2026-02-17
- Key types decompiled: `CityStatisticsSystem`, `ICityStatisticsSystem`, `StatisticsUISystem`, `StatisticType`, `CityStatistic`, `StatisticsEvent`, `StatisticsData`, `StatisticParameterData`, `StatisticsPrefab`, `ParametricStatistic`, `Population`, `Tourism`, `CityServiceStatisticsSystem`, `CompanyStatisticsSystem`, `CrimeStatisticsSystem`, `WealthStatisticsSystem`, `CountHouseholdDataSystem`
