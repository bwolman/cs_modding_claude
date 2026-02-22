# Research: Workplace & Labor Market

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-22

## Scope

**What we're investigating**: How CS2 creates workplaces on companies and service buildings, distributes workers by education level, matches job-seeking citizens to open positions, handles layoffs when conditions change, and spawns commuters from outside connections to fill educated job shortages.

**Why**: Understanding the workplace/labor pipeline is essential for mods that adjust job markets, rebalance education-to-employment ratios, modify commuter behavior, or tune building efficiency based on staffing.

**Boundaries**: Company profitability, production, and resource processing are documented in [Company Simulation](../CompanySimulation/). Citizen lifecycle (aging, happiness, death) is in [Citizens & Households](../CitizensHouseholds/). This topic focuses on the employment pipeline specifically.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Companies | WorkProvider, Employee, FreeWorkplaces, Workplaces, Workshift |
| Game.dll | Game.Citizens | Worker (citizen-side employment link) |
| Game.dll | Game.Agents | JobSeeker (job search agent entity) |
| Game.dll | Game.Prefabs | WorkplaceData, WorkProviderParameterData, WorkplaceComplexity |
| Game.dll | Game.Simulation | WorkProviderSystem, CountWorkplacesSystem, FindJobSystem, CommuterSpawnSystem, WorkProviderStatisticsSystem |
| Game.dll | Game.Economy | EconomyUtils (CalculateNumberOfWorkplaces, GetWorkerWorkforce, CalculateTotalWage) |

## Component Map

### `WorkProvider` (Game.Companies)

Attached to entities that provide jobs: companies, service buildings, schools, outside connections.

| Field | Type | Description |
|-------|------|-------------|
| m_MaxWorkers | int | Maximum number of worker slots (from prefab or calculated) |
| m_UneducatedCooldown | short | Cooldown counter for uneducated worker shortage notification |
| m_EducatedCooldown | short | Cooldown counter for educated worker shortage notification |
| m_UneducatedNotificationEntity | Entity | Entity target for uneducated worker shortage icon |
| m_EducatedNotificationEntity | Entity | Entity target for educated worker shortage icon |
| m_EfficiencyCooldown | short | Cooldown counter for missing-employees efficiency penalty |

*Source: `Game.dll` -> `Game.Companies.WorkProvider`*

### `Employee` (Game.Companies) -- Buffer

Dynamic buffer attached to each WorkProvider entity. Each element represents one employed citizen.

| Field | Type | Description |
|-------|------|-------------|
| m_Worker | Entity | The citizen entity who is employed here |
| m_Level | byte | Education level slot this worker fills (0-4) |

*Source: `Game.dll` -> `Game.Companies.Employee`*

### `FreeWorkplaces` (Game.Companies)

Tracks available job openings by education level. Added/removed by WorkProviderSystem based on vacancy.

| Field | Type | Description |
|-------|------|-------------|
| m_Uneducated | byte | Free slots for education level 0 (max 255) |
| m_PoorlyEducated | byte | Free slots for education level 1 (max 255) |
| m_Educated | byte | Free slots for education level 2 (max 255) |
| m_WellEducated | byte | Free slots for education level 3 (max 255) |
| m_HighlyEducated | byte | Free slots for education level 4 (max 255) |

Key methods:
- `Count` -- total free slots across all levels
- `Refresh(employees, maxWorkers, complexity, level)` -- recalculates free slots from current employee buffer
- `GetBestFor(level)` -- finds best available slot at or below given education level
- `GetLowestFree()` -- returns lowest education level with openings

*Source: `Game.dll` -> `Game.Companies.FreeWorkplaces`*

### `Workplaces` (Game.Companies)

Accumulator struct used for counting workplace totals. Not a component on entities -- used as a calculation type.

| Field | Type | Description |
|-------|------|-------------|
| m_Uneducated | int | Count for education level 0 |
| m_PoorlyEducated | int | Count for education level 1 |
| m_Educated | int | Count for education level 2 |
| m_WellEducated | int | Count for education level 3 |
| m_HighlyEducated | int | Count for education level 4 |

Properties: `TotalCount`, `SimpleWorkplacesCount` (levels 0+1), `ComplexWorkplacesCount` (levels 2+3+4).

*Source: `Game.dll` -> `Game.Companies.Workplaces`*

### `Worker` (Game.Citizens)

**Runtime-confirmed: `Worker` is a zero-sized tag component with no fields.** Its presence on a citizen entity signals that the citizen is employed. The employment relationship details (workplace entity, education slot, shift) are tracked on the **employer side** via:
- `WorkProvider.m_MaxWorkers` — total capacity
- `Employee` buffer on the WorkProvider entity — lists individual employed citizens with their level/shift
- `Employee.m_Worker` (Entity) — the citizen entity
- `Employee.m_Level` (byte) — education level slot
- `Employee.m_Shift` (Workshift) — shift assignment

**The decompiled `Worker` struct fields (`m_Workplace`, `m_LastCommuteTime`, `m_Level`, `m_Shift`) do not exist at runtime.** Systems that need to find a citizen's employer must query the `Employee` buffer on `WorkProvider` entities rather than reading fields from the `Worker` component.

*Source: `Game.dll` -> `Game.Citizens.Worker` (decompiled); runtime-confirmed by ECS entity dump*

### `JobSeeker` (Game.Agents)

Agent component on job search entities. Created when a citizen needs a job.

| Field | Type | Description |
|-------|------|-------------|
| m_Level | byte | Education level the citizen is seeking work at (0-4) |
| m_Outside | byte | If > 0, citizen is from outside connection (commuter) |

*Source: `Game.dll` -> `Game.Agents.JobSeeker`*

### `WorkplaceComplexity` (Game.Prefabs)

Enum determining the education distribution for a workplace.

| Value | Int | Description |
|-------|-----|-------------|
| Manual | 0 | Mostly uneducated workers (farms, mines) |
| Simple | 1 | Mix of low-education workers (basic industry) |
| Complex | 2 | Mix of educated workers (offices, services) |
| Hitech | 3 | Mostly highly educated workers (software, research) |

*Source: `Game.dll` -> `Game.Prefabs.WorkplaceComplexity`*

### `Workshift` (Game.Companies)

Enum for worker shift assignment.

| Value | Int | Description |
|-------|-----|-------------|
| Day | 0 | Standard daytime shift |
| Evening | 1 | Evening shift |
| Night | 2 | Night shift |

*Source: `Game.dll` -> `Game.Companies.Workshift`*

## Prefab & Configuration

### `WorkplaceData` (Game.Prefabs)

Per-prefab workplace configuration. Attached to company and building prefabs.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_Complexity | WorkplaceComplexity | Varies | Determines education level distribution |
| m_MaxWorkers | int | Varies | Base maximum worker count |
| m_EveningShiftProbability | float | Varies | Probability a new hire gets evening shift |
| m_NightShiftProbability | float | Varies | Probability a new hire gets night shift |
| m_MinimumWorkersLimit | int | 0 | Minimum workers needed before efficiency drop |
| m_WorkConditions | int | 0 | Work conditions factor (+/- affects employee happiness efficiency) |

Supports `ICombineData` for building upgrades -- values from installed upgrades are combined.

*Source: `Game.dll` -> `Game.Prefabs.WorkplaceData`*

### `WorkProviderParameterData` (Game.Prefabs)

Global singleton controlling notification thresholds and senior employee definitions.

| Field | Type | Description |
|-------|------|-------------|
| m_UneducatedNotificationPrefab | Entity | Notification icon prefab for uneducated worker shortage |
| m_EducatedNotificationPrefab | Entity | Notification icon prefab for educated worker shortage |
| m_UneducatedNotificationDelay | short | Ticks before showing uneducated worker shortage notification |
| m_EducatedNotificationDelay | short | Ticks before showing educated worker shortage notification |
| m_UneducatedNotificationLimit | float | Free/total ratio threshold for uneducated shortage warning |
| m_EducatedNotificationLimit | float | Free/total ratio threshold for educated shortage warning |
| m_SeniorEmployeeLevel | int | Education level threshold defining "senior" workers for statistics |

*Source: `Game.dll` -> `Game.Prefabs.WorkProviderParameterData`*

## System Map

### `WorkProviderSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 32 frames, 512 updates/day equivalent)
- **Queries**:
  - EntityQuery requiring: [WorkProvider, PrefabRef, Employee] + Any[CompanyData, Building, OutsideConnection] - Excludes[Deleted, Temp]
- **Reads**: WorkplaceData, SpawnableBuildingData, Citizen, Worker, HealthProblem, HouseholdMember, MovingAway, PrefabRef, SchoolData, InstalledUpgrade
- **Writes**: WorkProvider (cooldowns), Employee (buffer), FreeWorkplaces (add/remove), Efficiency (buffer), Notification icons
- **Key methods**:
  - `WorkProviderTickJob.Execute()` -- Main per-entity logic:
    1. Resolves building entity and building level
    2. Calls `EconomyUtils.CalculateNumberOfWorkplaces()` to compute slot distribution
    3. Calls `RefreshFreeWorkplace()` to remove invalid employees and update FreeWorkplaces
    4. Calls `UpdateNotificationAndEfficiency()` to manage shortage warnings and building efficiency
  - `RefreshFreeWorkplace()` -- Iterates employee buffer, removes dead/invalid/moving-away workers, fires workers if slots reduced. Adds/removes FreeWorkplaces component.
  - `UpdateNotificationAndEfficiency()` -- Sets three efficiency factors on building:
    - `EfficiencyFactor.NotEnoughEmployees` -- penalty for missing workers
    - `EfficiencyFactor.SickEmployees` -- penalty for sick workers
    - `EfficiencyFactor.EmployeeHappiness` -- worker happiness + work conditions bonus
  - `Liquidate()` -- Fires all employees when company has no building (bankruptcy)
  - `LayOffCountJob` -- Counts layoffs by reason (MovingAway, TooMany, NoBuilding)
- **Special behavior for outside connections**: Sets hardcoded 600 workers (200 each at levels 2, 3, 4)
- **Special behavior for schools**: Calculates max workers from school data + student buffers

### `CountWorkplacesSystem` (Game.Simulation)

- **Base class**: GameSystemBase, IDefaultSerializable, ISerializable
- **Update phase**: Simulation (every 16 frames)
- **Queries**:
  - EntityQuery requiring: [WorkProvider] + Any[PropertyRenter, Building] - Excludes[OutsideConnection, Destroyed, Deleted, Temp]
- **Reads**: FreeWorkplaces, WorkProvider, PrefabRef, PropertyRenter, Building, SpawnableBuildingData, WorkplaceData
- **Writes**: NativeAccumulator<Workplaces> for free and total counts
- **Key methods**:
  - `CountWorkplacesJob.Execute()` -- Accumulates free workplace counts from FreeWorkplaces components and total workplace counts using EconomyUtils.CalculateNumberOfWorkplaces()
  - `GetFreeWorkplaces()` -- Returns last computed free workplaces
  - `GetTotalWorkplaces()` -- Returns last computed total workplaces
  - `GetUnemployedWorkspaceByLevel()` -- Returns cumulative free workplaces (level N includes all levels 0..N)
- **Persisted**: Free and total workplace counts are serialized to save files

### `FindJobSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames)
- **Queries**:
  - m_JobSeekerQuery: [JobSeeker, Owner] - Excludes[PathInformation, Deleted]
  - m_ResultsQuery: [JobSeeker, Owner, PathInformation] - Excludes[Deleted]
  - m_FreeQuery: [FreeWorkplaces] - Excludes[Destroyed, Temp, Deleted]
- **Two-phase operation**:
  1. **CalculateFreeWorkplaceJob**: Sums all FreeWorkplaces into a 5-element cache array
  2. **FindJobJob**: For each job seeker, finds the best education level match:
     - Starts at citizen's education level, walks down to find a level with openings
     - Compares employable-to-free ratio to decide if already-employed citizens should switch
     - Already-employed citizens at a good-enough level are removed from seeking
     - Sends a pathfinding request to find nearest reachable workplace at target level
     - Destination uses `SetupTargetType.JobSeekerTo` with encoded level+value
  3. **StartWorkingJob**: Processes pathfinding results:
     - Finds workplace at pathfind destination
     - Calls `FreeWorkplaces.GetBestFor(level)` to get actual slot
     - Assigns `Workshift` based on random roll vs evening/night shift probabilities
     - Creates `Employee` buffer element and `Worker` component
     - Fires `TriggerType.CitizenStartedWorking`

### `CommuterSpawnSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 16 frames)
- **Reads**: CountWorkplacesSystem.GetFreeWorkplaces(), CountHouseholdDataSystem.GetEmployables(), DemandParameterData
- **Writes**: Creates commuter household entities
- **Key logic** (`SpawnCommuterHouseholdJob.Execute()`):
  - Calculates educated job surplus: `freeWorkplaces[2..4] - employables[2..4]`
  - Spawns `surplus / DemandParameterData.m_CommuterSlowSpawnFactor` commuter households per tick
  - Only spawns if commuter count * CommuterWorkerRatioLimit < total worker count (prevents overcommuting)
  - Each commuter household is created at a random outside connection with `HouseholdFlags.Commuter`

### `WorkProviderStatisticsSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every 8192 frames -- very infrequent)
- **Queries**:
  - EntityQuery requiring: [WorkProvider, Employee] - Excludes[OutsideConnection, Deleted, Temp]
- **Reads**: WorkProvider, FreeWorkplaces, WorkplaceData, SpawnableBuildingData, WorkProviderParameterData (for m_SeniorEmployeeLevel)
- **Writes**: CityStatisticsSystem event queue
- **Key logic**: Counts free vs total workplaces at senior education levels, reports `StatisticType.SeniorWorkerInDemandPercentage` as a percentage (free/total * 100)

## Data Flow

```
PREFAB INITIALIZATION
  WorkplaceData on prefab
    m_Complexity + m_MaxWorkers define workplace profile
          |
          v
WORKPLACE SLOT CALCULATION (EconomyUtils.CalculateNumberOfWorkplaces)
  Input: totalWorkers, complexity, buildingLevel
  Algorithm: weighted distribution across 5 education levels
  Center = 4 * complexity + buildingLevel - 1
  Each level weight = max(0, 8 - |center - 4*level|)
  Output: Workplaces struct (e.g., Manual/Level1: mostly uneducated)
          |
          v
WORK PROVIDER TICK (WorkProviderSystem, every 32 frames)
  For each company/building with WorkProvider:
    1. Calculate expected Workplaces from prefab + building level
    2. Iterate Employee buffer:
       - Remove dead, deleted, moving-away workers -> LayOff(MovingAway)
       - Remove workers exceeding slot count -> LayOff(TooMany)
    3. Update FreeWorkplaces component (add if vacancies, remove if full)
    4. Update building Efficiency factors:
       - NotEnoughEmployees (ramped by cooldown)
       - SickEmployees
       - EmployeeHappiness (happiness + work conditions)
    5. Update notification icons (uneducated/educated shortage)
          |
          v
WORKPLACE COUNTING (CountWorkplacesSystem, every 16 frames)
  Sums all FreeWorkplaces and total Workplaces across city
  Stores in m_LastFreeWorkplaces, m_LastTotalWorkplaces
          |
          v
JOB SEEKING (FindJobSystem, every 16 frames)
  Phase 1 - CalculateFreeWorkplaceJob:
    Sum all FreeWorkplaces into 5-element cache
  Phase 2 - FindJobJob:
    For each JobSeeker citizen:
      1. Find best education level with openings (walk down from citizen level)
      2. Check employable-to-free ratio (skip if already well-employed)
      3. Send pathfinding request to find nearest workplace
  Phase 3 - StartWorkingJob:
    For each completed pathfind result:
      1. Check workplace has free slot at appropriate level
      2. Assign work shift (Day/Evening/Night based on probabilities)
      3. Add Employee to workplace buffer
      4. Set Worker component on citizen
      5. Fire CitizenStartedWorking trigger
          |
          v
COMMUTER SPAWNING (CommuterSpawnSystem, every 16 frames)
  If educated job surplus exists (free educated > employable educated):
    Spawn commuter households from outside connections
    Rate: surplus / CommuterSlowSpawnFactor
    Guard: commuters * CommuterWorkerRatioLimit < total workers
          |
          v
STATISTICS (WorkProviderStatisticsSystem, every 8192 frames)
  Reports SeniorWorkerInDemandPercentage to city statistics
```

## Key Algorithm: Education Level Distribution

`EconomyUtils.CalculateNumberOfWorkplaces(totalWorkers, complexity, buildingLevel)`:

The distribution center is `4 * (int)complexity + buildingLevel - 1`. Each education level (0-4) gets a weight of `max(0, 8 - |center - 4*level|)`. Weights sum to 16 (except edge effects), and workers are distributed proportionally with rounding correction.

Example: Manual complexity (0), Level 1 building -> center = 0
- Level 0 (Uneducated): weight = max(0, 8-0) + max(0, 8-4) = 12 -> 75% of workers
- Level 1 (PoorlyEducated): weight = max(0, 8-4) = 4 -> 25% of workers
- Levels 2-4: weight = 0 -> no workers

Example: Hitech complexity (3), Level 5 building -> center = 16
- Level 3 (WellEducated): weight = max(0, 8-4) = 4 -> 25%
- Level 4 (HighlyEducated): weight = max(0, 8-0) + max(0, 8-4) = 12 -> 75%

## Key Algorithm: Worker Workforce Value

`EconomyUtils.GetWorkerWorkforce(happiness, level)`:

```
workforce = ((level == 0 ? 2.0 : 1.0) + 2.5 * level) * (0.75 + happiness / 200.0)
```

At happiness=50: Level 0 = 1.75, Level 1 = 3.0625, Level 2 = 5.25, Level 3 = 7.4375, Level 4 = 9.625

Higher education levels produce exponentially more "workforce units" per worker.

## Harmony Patch Points

### Candidate 1: `Game.Economy.EconomyUtils.CalculateNumberOfWorkplaces`

- **Signature**: `static Workplaces CalculateNumberOfWorkplaces(int totalWorkers, WorkplaceComplexity complexity, int buildingLevel)`
- **Patch type**: Prefix (replace) or Postfix (modify result)
- **What it enables**: Completely rebalance how education levels are distributed across workplaces. Could make all businesses need more educated workers, or flatten the distribution.
- **Risk level**: Medium -- called by multiple systems (WorkProviderSystem, CountWorkplacesSystem, FindJobSystem)
- **Side effects**: Affects building efficiency, job seeking, commuter spawning, and statistics

### Candidate 2: `Game.Simulation.WorkProviderSystem.WorkProviderTickJob.RefreshFreeWorkplace`

- **Signature**: `private void RefreshFreeWorkplace(int sortKey, Entity workplaceEntity, DynamicBuffer<Employee> employeeBuf, ref Workplaces freeWorkplaces)`
- **Patch type**: Postfix
- **What it enables**: Modify layoff behavior, change when workers are removed, add custom logic when vacancies change
- **Risk level**: Medium -- operates on per-entity basis, changes affect individual companies
- **Side effects**: Incorrect layoff logic could cause infinite hiring/firing loops

### Candidate 3: `Game.Simulation.FindJobSystem.FindJobJob.Execute`

- **Signature**: `public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Prefix (to filter job seekers) or Transpiler
- **What it enables**: Change job matching logic (e.g., prevent overqualified workers from taking low-level jobs, prioritize certain areas)
- **Risk level**: High -- Burst-compiled IJobChunk, complex inner logic
- **Side effects**: Could break pathfinding pipeline if destination encoding changes

### Candidate 4: `Game.Simulation.CommuterSpawnSystem.SpawnCommuterHouseholdJob.Execute`

- **Signature**: `public void Execute()`
- **Patch type**: Prefix (to throttle spawning) or Postfix
- **What it enables**: Control commuter influx rate, add requirements for commuter households, limit by outside connection type
- **Risk level**: Low -- single-threaded IJob, simple spawn logic
- **Side effects**: Too few commuters could starve educated job market

### Candidate 5: `Game.Economy.EconomyUtils.GetWorkerWorkforce`

- **Signature**: `static float GetWorkerWorkforce(int happiness, int level)`
- **Patch type**: Postfix (modify return value)
- **What it enables**: Rebalance the value of different education levels, make happiness matter more/less
- **Risk level**: Low -- pure function, no side effects
- **Side effects**: Affects building efficiency calculations and company production

### Dynamic WorkProvider.m_MaxWorkers Modification

When modifying workplace counts at runtime, multiple components must be kept in sync. The entity relationship chain for workplace modification:

```
Company Entity → PropertyRenter.m_Property → Building Entity
Building Entity → PrefabRef.m_Prefab → Building Prefab Entity
Building Prefab Entity → SubMesh buffer → MeshData → dimensions
Company Entity → PrefabRef.m_Prefab → Company Prefab Entity
Company Prefab Entity → WorkplaceData / ServiceCompanyData / IndustrialProcessData
```

**Modification Pattern**:
1. Query company entities with `[PrefabRef, CompanyData, PropertyRenter, WorkProvider]`
2. Look up the building prefab via `PropertyRenter.m_Property` → building entity → `PrefabRef.m_Prefab`
3. Get mesh dimensions from building prefab to calculate realistic worker counts
4. Write new values to `WorkProvider.m_MaxWorkers` on the company entity
5. Also scale `WorkplaceData.m_MaxWorkers` on the company prefab entity
6. Also scale `ServiceCompanyData.m_WorkPerUnit` and `IndustrialProcessData.m_WorkPerUnit`

**Critical Sync Requirement**: When modifying `m_MaxWorkers`, the `m_WorkPerUnit` on the company prefab must also be scaled proportionally, or production per worker becomes unbalanced. For example:

```csharp
// Scale worker count
float scaleFactor = newMaxWorkers / (float)originalMaxWorkers;
workProvider.m_MaxWorkers = newMaxWorkers;

// MUST also scale production rate to maintain balance
workplaceData.m_MaxWorkers = newMaxWorkers;
serviceCompanyData.m_WorkPerUnit = (int)(originalWorkPerUnit * scaleFactor);
industrialProcessData.m_WorkPerUnit = (int)(originalWorkPerUnit * scaleFactor);
```

Source: RealisticWorkplacesAndHouseholds mod

## Mod Blueprint

- **Systems to create**:
  - Custom `WorkplaceBalancerSystem` extending `GameSystemBase` to monitor and adjust workplace distribution
  - Custom `CommuterControlSystem` to limit or enhance commuter spawning based on configurable rules
- **Components to add**:
  - Optional `CustomWorkplaceRules` component for per-building overrides
- **Patches needed**:
  - `EconomyUtils.CalculateNumberOfWorkplaces` (Postfix) to adjust education distribution
  - `CommuterSpawnSystem.SpawnCommuterHouseholdJob.Execute` (Prefix) to control spawn rate
- **Settings**:
  - Education level distribution multipliers per complexity type
  - Commuter spawn rate multiplier
  - Maximum commuter-to-worker ratio override
- **UI changes**:
  - Statistics panel showing workplace breakdown by education level
  - Notification when educated worker shortage exceeds threshold

## Examples

### Example 1: Read Workplace Status for a Company

Check how many employees a company has and how many open positions remain.

```csharp
public void CheckWorkplaceStatus(EntityManager em, Entity company)
{
    if (!em.HasComponent<WorkProvider>(company)) return;

    WorkProvider wp = em.GetComponentData<WorkProvider>(company);
    DynamicBuffer<Employee> employees = em.GetBuffer<Employee>(company);

    Log.Info($"Workplace status for {company}:");
    Log.Info($"  Max workers: {wp.m_MaxWorkers}");
    Log.Info($"  Current employees: {employees.Length}");

    if (em.HasComponent<FreeWorkplaces>(company))
    {
        FreeWorkplaces free = em.GetComponentData<FreeWorkplaces>(company);
        Log.Info($"  Free slots: {free.Count}");
        Log.Info($"    Uneducated: {free.m_Uneducated}");
        Log.Info($"    PoorlyEducated: {free.m_PoorlyEducated}");
        Log.Info($"    Educated: {free.m_Educated}");
        Log.Info($"    WellEducated: {free.m_WellEducated}");
        Log.Info($"    HighlyEducated: {free.m_HighlyEducated}");
    }
    else
    {
        Log.Info("  No vacancies (FreeWorkplaces component absent)");
    }

    // List current employees by level
    for (int i = 0; i < employees.Length; i++)
    {
        Employee emp = employees[i];
        Log.Info($"  Employee {i}: worker={emp.m_Worker}, level={emp.m_Level}");
    }
}
```

### Example 2: Monitor City-Wide Workplace Statistics

Use CountWorkplacesSystem to get aggregate workplace data across the entire city.

```csharp
public partial class WorkplaceMonitorSystem : GameSystemBase
{
    private CountWorkplacesSystem _countSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _countSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
    }

    protected override void OnUpdate()
    {
        Workplaces total = _countSystem.GetTotalWorkplaces();
        Workplaces free = _countSystem.GetFreeWorkplaces();

        Log.Info($"City Workplace Report:");
        Log.Info($"  Total: {total.TotalCount} (Simple: {total.SimpleWorkplacesCount}, Complex: {total.ComplexWorkplacesCount})");
        Log.Info($"  Free:  {free.TotalCount}");

        for (int level = 0; level < 5; level++)
        {
            float fillRate = total[level] > 0
                ? 1f - (float)free[level] / total[level]
                : 0f;
            Log.Info($"  Level {level}: {total[level] - free[level]}/{total[level]} filled ({fillRate:P0})");
        }
    }
}
```

### Example 3: Calculate Workplace Distribution for a Prefab

Preview how workers will be distributed across education levels for any workplace prefab configuration.

```csharp
public void PreviewWorkplaceDistribution(int maxWorkers, WorkplaceComplexity complexity, int buildingLevel)
{
    Workplaces slots = EconomyUtils.CalculateNumberOfWorkplaces(
        maxWorkers, complexity, buildingLevel);

    string[] levelNames = { "Uneducated", "PoorlyEducated", "Educated", "WellEducated", "HighlyEducated" };

    Log.Info($"Distribution for {maxWorkers} workers, {complexity} complexity, level {buildingLevel}:");
    for (int i = 0; i < 5; i++)
    {
        float workforce = slots[i] * EconomyUtils.GetWorkerWorkforce(50, i);
        Log.Info($"  {levelNames[i]}: {slots[i]} slots, {workforce:F1} workforce units");
    }

    float totalWorkforce = EconomyUtils.GetAverageWorkforce(slots);
    Log.Info($"  Total workforce value: {totalWorkforce:F1}");
}
```

### Example 4: Harmony Patch to Adjust Commuter Spawn Rate

Reduce commuter spawning by applying a configurable multiplier to the spawn count.

```csharp
[HarmonyPatch]
public static class CommuterSpawnPatch
{
    // Target the inner job's Execute method
    [HarmonyPatch(typeof(CommuterSpawnSystem), "OnUpdate")]
    [HarmonyPrefix]
    public static bool PrefixOnUpdate(CommuterSpawnSystem __instance)
    {
        // Return true to let original execute, false to skip entirely.
        // For finer control, use a Transpiler on SpawnCommuterHouseholdJob.Execute
        // to modify the num3 (spawn count) calculation.
        return true;
    }
}
```

### Example 5: Find All Understaffed Buildings

Query all work providers with significant vacancies and report which buildings need workers.

```csharp
public partial class UnderstaffedBuildingSystem : GameSystemBase
{
    private EntityQuery _workProviderQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _workProviderQuery = GetEntityQuery(
            ComponentType.ReadOnly<WorkProvider>(),
            ComponentType.ReadOnly<FreeWorkplaces>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<Entity> entities =
            _workProviderQuery.ToEntityArray(Allocator.Temp);
        NativeArray<WorkProvider> providers =
            _workProviderQuery.ToComponentDataArray<WorkProvider>(Allocator.Temp);
        NativeArray<FreeWorkplaces> freeSlots =
            _workProviderQuery.ToComponentDataArray<FreeWorkplaces>(Allocator.Temp);

        int understaffedCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            int maxWorkers = providers[i].m_MaxWorkers;
            int freeCount = freeSlots[i].Count;
            float vacancyRate = maxWorkers > 0 ? (float)freeCount / maxWorkers : 0f;

            if (vacancyRate > 0.5f) // More than 50% vacant
            {
                understaffedCount++;
                Log.Info($"Understaffed: {entities[i]} - {freeCount}/{maxWorkers} vacant ({vacancyRate:P0})");
            }
        }

        if (understaffedCount > 0)
            Log.Info($"Total understaffed workplaces: {understaffedCount}");

        entities.Dispose();
        providers.Dispose();
        freeSlots.Dispose();
    }
}
```

## Open Questions

- [ ] How does `BuildingEfficiencyParameterData.m_MissingEmployeesEfficiencyDelay` interact with the cooldown ramp? The efficiency penalty ramps up gradually as `m_EfficiencyCooldown` increases -- what is the typical delay value?
- [ ] What are the default values for `WorkProviderParameterData` fields (notification limits, delays, senior level)? These come from prefab data and are not hardcoded.
- [ ] How does `DemandParameterData.m_CommuterSlowSpawnFactor` balance commuter spawning? A higher value means slower spawning, but the actual default is unknown.
- [ ] The `m_WorkConditions` field on `WorkplaceData` appears to contribute to employee happiness efficiency as `workConditions * 0.01` -- what prefabs set non-zero values?
- [ ] How does `SetupTargetType.JobSeekerTo` encode the target level? The value is `level + 5 * (num + 1)` where `num` is the workplace education level -- this encodes both the citizen's level and the target level in a single int.

## Sources

- Decompiled from: Game.dll -- Game.Companies.WorkProvider, Game.Companies.Employee, Game.Companies.FreeWorkplaces, Game.Companies.Workplaces, Game.Companies.Workshift
- Citizen components: Game.Citizens.Worker, Game.Agents.JobSeeker
- Prefab types: Game.Prefabs.WorkplaceData, Game.Prefabs.WorkProviderParameterData, Game.Prefabs.WorkplaceComplexity
- Systems: Game.Simulation.WorkProviderSystem, Game.Simulation.CountWorkplacesSystem, Game.Simulation.FindJobSystem, Game.Simulation.CommuterSpawnSystem, Game.Simulation.WorkProviderStatisticsSystem
- Utility methods: Game.Economy.EconomyUtils (CalculateNumberOfWorkplaces, GetWorkerWorkforce, CalculateTotalWage, GetAverageWorkforce)
