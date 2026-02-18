# Research: Telecom / Internet Service

> **Status**: Complete
> **Date started**: 2026-02-17
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2 simulates telecom/internet service -- tower placement, signal coverage calculation, network capacity, building connectivity, and the efficiency impact on buildings.

**Why**: To understand how mods can modify telecom coverage radius, adjust network capacity, check building connectivity status, and create custom coverage behaviors.

**Boundaries**: Out of scope: the rendering pipeline for the telecom info view overlay texture (handled by `OverlayInfomodeSystem`), the general `ServiceCoverageSystem` (telecom uses its own separate cell map), and satellite uplink animations.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | TelecomCoverageSystem, TelecomEfficiencySystem, TelecomCoverage, TelecomStatus |
| Game.dll | Game.Simulation | TelecomFacilityAISystem |
| Game.dll | Game.Buildings | TelecomFacility (component), TelecomFacilityFlags, TelecomConsumer |
| Game.dll | Game.Prefabs | TelecomFacilityData, TelecomParameterData, TelecomFacility (prefab), TelecomPrefab, ConsumptionData |
| Game.dll | Game.Prefabs | BuildingEfficiencyParameterData (m_TelecomBaseline) |
| Game.dll | Game.Prefabs.Modes | TelecomFacilityMode |
| Game.dll | Game.Tools | TelecomPreviewSystem |
| Game.dll | Game.UI.InGame | TelecomInfoviewUISystem |
| Game.dll | Game.City | CityModifierType.TelecomCapacity (value 19) |
| Game.dll | Game.Buildings | EfficiencyFactor.Telecom |
| Game.dll | Game.Common | PointOfInterest |

## Component Map

### `TelecomFacility` (Game.Buildings)

ECS component on telecom tower/facility building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | TelecomFacilityFlags | State flags for the facility |

*Source: `Game.dll` -> `Game.Buildings.TelecomFacility`*

### `TelecomFacilityFlags` (Game.Buildings)

| Flag | Value | Description |
|------|-------|-------------|
| HasCoverage | 0x01 | Set by TelecomFacilityAISystem once facility is active |

### `TelecomConsumer` (Game.Buildings)

Empty marker component added to buildings that need telecom service. Added automatically when `ConsumptionData.m_TelecomNeed > 0`.

| Field | Type | Description |
|-------|------|-------------|
| (empty) | -- | Size 1 byte, marker only |

*Source: `Game.dll` -> `Game.Buildings.TelecomConsumer`*

### `TelecomCoverage` (Game.Simulation)

Per-cell data in the 128x128 coverage grid.

| Field | Type | Description |
|-------|------|-------------|
| m_SignalStrength | byte | Signal strength at this cell (0-255) |
| m_NetworkLoad | byte | Network congestion at this cell (0-255) |

**Derived property:**
- `networkQuality` (int): `m_SignalStrength * 510 / (255 + m_NetworkLoad * 2)`

**Static method:**
- `SampleNetworkQuality(CellMapData<TelecomCoverage>, float3)`: Bilinear interpolation of 4 cells, returns 0.0-1.0. Formula per cell: `min(1, signalStrength / (127.5 + networkLoad))`

*Source: `Game.dll` -> `Game.Simulation.TelecomCoverage`*

### `TelecomStatus` (Game.Simulation)

City-level summary stored in a single-element NativeArray on `TelecomCoverageSystem`.

| Field | Type | Description |
|-------|------|-------------|
| m_Capacity | float | Total network capacity across all facilities |
| m_Load | float | Total users (population density weighted by signal) |
| m_Quality | float | Weighted average quality across populated cells (0-1) |

*Source: `Game.dll` -> `Game.Simulation.TelecomStatus`*

### `TelecomFacilityData` (Game.Prefabs)

Prefab data component on telecom facility prefab entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Range | float | Coverage radius in meters (default: 1000) |
| m_NetworkCapacity | float | Bandwidth capacity units (default: 10000) |
| m_PenetrateTerrain | bool | If true, signal ignores terrain obstruction |

Implements `ICombineData<TelecomFacilityData>`: upgrades add their range and capacity to the base facility values. `m_PenetrateTerrain` is OR'd.

*Source: `Game.dll` -> `Game.Prefabs.TelecomFacilityData`*

### `TelecomParameterData` (Game.Prefabs)

Singleton component storing global telecom configuration.

| Field | Type | Description |
|-------|------|-------------|
| m_TelecomServicePrefab | Entity | Reference to the telecom service prefab (checked for Locked state) |

*Source: `Game.dll` -> `Game.Prefabs.TelecomParameterData`*

### `ConsumptionData` (Game.Prefabs) -- Telecom-Related Fields

| Field | Type | Description |
|-------|------|-------------|
| m_TelecomNeed | float | How much this building needs telecom service. Drives penalty magnitude when coverage is below baseline. |

When `m_TelecomNeed > 0`, the building archetype gets `TelecomConsumer` added automatically.

*Source: `Game.dll` -> `Game.Prefabs.ConsumptionData`*

### `BuildingEfficiencyParameterData` (Game.Prefabs) -- Telecom-Related Fields

| Field | Type | Description |
|-------|------|-------------|
| m_TelecomBaseline | float | Quality threshold below which buildings suffer efficiency penalty |

*Source: `Game.dll` -> `Game.Prefabs.BuildingEfficiencyParameterData`*

### `PointOfInterest` (Game.Common)

Component on telecom facilities for satellite dish/antenna pointing animation.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | Target position for the antenna to point at |
| m_IsValid | bool | Whether the position is valid |

*Source: `Game.dll` -> `Game.Common.PointOfInterest`*

## System Map

### `TelecomCoverageSystem` (Game.Simulation)

The core system that computes the 128x128 telecom coverage cell map.

- **Base class**: `CellMapSystem<TelecomCoverage>`
- **Update phase**: Simulation
- **Update interval**: 4096 frames
- **Queries**:
  - `m_DensityQuery`: entities with `HouseholdCitizen` or `Employee` (excludes Temp, Deleted)
  - `m_FacilityQuery`: entities with `TelecomFacility`, `Transform`, `PrefabRef` (excludes ServiceUpgrade, Temp, Deleted)
- **Reads**: Transform, PropertyRenter, TelecomFacility, Efficiency, PrefabRef, Temp, InstalledUpgrade, HouseholdCitizen, Employee, ObjectGeometryData, TelecomFacilityData, CityModifier, TerrainHeightData
- **Writes**: `NativeArray<TelecomCoverage>` (128x128 cell map), `NativeArray<TelecomStatus>` (1 element)
- **Key methods**:
  - `TelecomCoverageJob.Execute()` -- Orchestrates the 5-phase pipeline
  - `AddDensity()` -- Counts citizens/employees per cell
  - `CalculateSignalStrength()` -- Computes signal per cell per facility (with terrain obstruction)
  - `AddNetworkCapacity()` -- Distributes facility capacity weighted by signal strength
  - `CalculateTelecomCoverage()` -- Writes final byte values to cell map
  - `CalculateTelecomQuality()` -- Computes population-weighted average quality
  - `CalculateSignalStrength(distance, range)` -- Core formula: `1 - (distance/range)^2`

### `TelecomEfficiencySystem` (Game.Simulation)

Applies telecom coverage quality as an efficiency factor on consumer buildings.

- **Base class**: `GameSystemBase`
- **Update phase**: Simulation
- **Update interval**: 32 frames (512 updates per day, staggered via UpdateFrame)
- **Queries**:
  - `m_BuildingQuery`: entities with `TelecomConsumer`, `Efficiency`, `Building`, `UpdateFrame`, `Transform`, `PrefabRef` (excludes Deleted, Temp)
- **Reads**: TelecomCoverage cell map, ConsumptionData, PrefabRef, Transform, InstalledUpgrade, BuildingEfficiencyParameterData
- **Writes**: `Efficiency` buffer (sets `EfficiencyFactor.Telecom`)
- **Precondition**: Checks `TelecomParameterData.m_TelecomServicePrefab` is NOT `Locked` before running
- **Key methods**:
  - `GetTelecomEfficiency(position, telecomNeed)` -- Samples coverage map, applies quadratic penalty

### `TelecomFacilityAISystem` (Game.Simulation)

Manages telecom facility building state.

- **Base class**: `GameSystemBase`
- **Update phase**: Simulation
- **Update interval**: 256 frames, offset 208
- **Queries**:
  - `m_BuildingQuery`: entities with `TelecomFacility`, `PrefabRef` (excludes Temp, Destroyed, Deleted)
- **Reads**: Efficiency
- **Writes**: `TelecomFacility.m_Flags` (sets HasCoverage), `PointOfInterest` (randomizes antenna target)
- **Key methods**:
  - `TelecomFacilityTickJob.Execute()` -- Sets HasCoverage flag, randomizes PointOfInterest position

### `TelecomPreviewSystem` (Game.Tools)

Recalculates the coverage map for tool preview when facilities are being placed/modified.

- **Base class**: `CellMapSystem<TelecomCoverage>`
- **Update phase**: Tools
- **Trigger**: Runs when `m_ModifiedQuery` (Created, Deleted, or Updated telecom facilities) is non-empty, or on first run
- **Reuses**: `TelecomCoverageSystem.TelecomCoverageJob` with `m_Preview = true`

### `TelecomInfoviewUISystem` (Game.UI.InGame)

UI binding system for the telecom info view panel.

- **Base class**: `InfoviewUISystemBase`
- **Bindings**:
  - `telecomInfo.networkAvailability` (ValueBinding<IndicatorValue>)
- **Reads**: TelecomCoverage array, TelecomStatus array, telecom facility entities, density entities

## Data Flow

```
[Building Prefabs with ConsumptionData.m_TelecomNeed > 0]
    |
    v
Buildings get TelecomConsumer marker component automatically
    |
    |
[Telecom Facility Placement]
    |
    v
TelecomFacilityAISystem (every 256 frames)
    Sets TelecomFacilityFlags.HasCoverage
    Randomizes PointOfInterest for antenna animation
    |
    v
TelecomCoverageSystem (every 4096 frames)
    Phase 1: AddDensity — counts HouseholdCitizen + Employee per 128x128 cell
    Phase 2: CalculateSignalStrength — per facility, per cell:
        - strength = 1 - (distance/range)^2
        - If !PenetrateTerrain: terrain obstruction reduces signal
        - Range scaled by sqrt(building efficiency)
        - CityModifier.TelecomCapacity applied to network capacity
    Phase 3: AddNetworkCapacity — distributes capacity weighted by signal
    Phase 4: CalculateTelecomCoverage — writes byte values to cell map
    Phase 5: CalculateTelecomQuality — population-weighted average -> TelecomStatus
    |
    v
128x128 CellMap<TelecomCoverage> (m_SignalStrength, m_NetworkLoad per cell)
    |
    +-----> TelecomEfficiencySystem (every 32 frames, staggered)
    |           Samples coverage at building position
    |           If quality < m_TelecomBaseline:
    |               penalty = (1 - quality/baseline)^2 * -0.01 * telecomNeed
    |           Sets EfficiencyFactor.Telecom on building Efficiency buffer
    |           |
    |           v
    |       Building efficiency affects production, services, etc.
    |
    +-----> TelecomInfoviewUISystem
    |           Reads TelecomStatus for network availability indicator
    |
    +-----> TelecomPreviewSystem (tool mode only)
                Recalculates coverage when placing/moving facilities
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Coverage range | TelecomFacilityData.m_Range | Game.Prefabs (default: 1000m) |
| Network capacity | TelecomFacilityData.m_NetworkCapacity | Game.Prefabs (default: 10000) |
| Penetrate terrain | TelecomFacilityData.m_PenetrateTerrain | Game.Prefabs (default: false) |
| Telecom need | ConsumptionData.m_TelecomNeed | Game.Prefabs (per building type) |
| Quality baseline | BuildingEfficiencyParameterData.m_TelecomBaseline | Game.Prefabs (singleton) |
| Service prefab | TelecomParameterData.m_TelecomServicePrefab | Game.Prefabs (singleton, checked for Locked) |
| City modifier | CityModifierType.TelecomCapacity (19) | Game.City (applied to facility capacity) |
| Grid size | 128x128 cells | Hardcoded in TelecomCoverageSystem |
| Coverage update interval | 4096 frames | Hardcoded |
| Efficiency update interval | 32 frames | Hardcoded |
| Facility AI update interval | 256 frames (offset 208) | Hardcoded |
| Efficiency updates per day | 512 | Hardcoded (staggered via UpdateFrame) |
| Signal formula | `1 - (distance/range)^2` | Hardcoded (quadratic falloff) |
| Efficiency penalty formula | `(1 - quality/baseline)^2 * -0.01 * telecomNeed` | Hardcoded |
| Mode multipliers | TelecomFacilityMode.ModeData | m_RangeMultiplier, m_NetworkCapacityMultiplier |

## Harmony Patch Points

### Candidate 1: `TelecomCoverageSystem.TelecomCoverageJob.CalculateSignalStrength(float, float)`

- **Signature**: `float CalculateSignalStrength(float distance, float range)`
- **Patch type**: Transpiler (Burst-compiled job -- cannot use prefix/postfix)
- **What it enables**: Custom signal falloff curve (e.g., linear instead of quadratic, or longer range with weaker signal)
- **Risk level**: Medium -- Burst-compiled, requires transpiler
- **Side effects**: Changes all facility signal calculations

### Candidate 2: `TelecomEfficiencySystem.TelecomEfficiencyJob.GetTelecomEfficiency(float3, float)`

- **Signature**: `float GetTelecomEfficiency(float3 position, float telecomNeed)`
- **Patch type**: Transpiler (Burst-compiled)
- **What it enables**: Custom efficiency penalty curve, remove penalty entirely, or add bonus for high coverage
- **Risk level**: Medium
- **Side effects**: Affects all buildings with TelecomConsumer

### Candidate 3: `TelecomFacilityData.Combine(TelecomFacilityData)`

- **Signature**: `void Combine(TelecomFacilityData otherData)`
- **Patch type**: Prefix or Postfix (not Burst)
- **What it enables**: Custom upgrade combination logic (e.g., multiplicative instead of additive for range)
- **Risk level**: Low
- **Side effects**: Affects how upgrades modify facility stats

### Candidate 4: Prefab modification via `PrefabSystem`

- **Approach**: Modify `TelecomFacilityData` on prefab entities at load time
- **What it enables**: Change default range, capacity, or terrain penetration for any telecom facility
- **Risk level**: Low
- **Side effects**: Affects all instances of modified prefab

### Candidate 5: `BuildingEfficiencyParameterData.m_TelecomBaseline` modification

- **Approach**: Modify singleton component value at runtime
- **What it enables**: Raise or lower the quality threshold for efficiency penalties
- **Risk level**: Low
- **Side effects**: Affects all buildings globally

## Mod Blueprint

- **Systems to create**: Custom `GameSystemBase` for monitoring/modifying telecom stats, custom overlay system for enhanced info view
- **Components to add**: None needed for basic modifications; custom tag component if tracking mod-modified facilities
- **Patches needed**: Prefab modification for range/capacity tweaks, transpiler if custom signal formula needed
- **Settings**: Facility range multiplier, capacity multiplier, telecom baseline threshold, enable/disable terrain penetration
- **UI changes**: Optional enhanced telecom info panel showing per-facility stats

## Examples

### Example 1: Modify Telecom Facility Range and Capacity

Modify the range and capacity of all telecom facility prefabs at game load time. This is the simplest way to adjust telecom coverage.

```csharp
public partial class TelecomPrefabModifierSystem : GameSystemBase
{
    private PrefabSystem m_PrefabSystem;
    private EntityQuery m_TelecomPrefabQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        m_TelecomPrefabQuery = GetEntityQuery(
            ComponentType.ReadWrite<TelecomFacilityData>(),
            ComponentType.ReadOnly<PrefabData>()
        );
        RequireForUpdate(m_TelecomPrefabQuery);
    }

    protected override void OnUpdate()
    {
        // Run once then disable
        Enabled = false;

        var entities = m_TelecomPrefabQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            TelecomFacilityData data = EntityManager.GetComponentData<TelecomFacilityData>(entities[i]);

            // Double the range, triple the capacity
            data.m_Range *= 2f;
            data.m_NetworkCapacity *= 3f;

            // Enable terrain penetration for all towers
            data.m_PenetrateTerrain = true;

            EntityManager.SetComponentData(entities[i], data);
        }
        entities.Dispose();

        Log.Info($"Modified {entities.Length} telecom facility prefabs");
    }
}
```

### Example 2: Sample Telecom Coverage at a Position

Read the telecom coverage quality at any world position using the coverage cell map.

```csharp
public partial class TelecomCoverageReaderSystem : GameSystemBase
{
    private TelecomCoverageSystem m_TelecomCoverageSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TelecomCoverageSystem = World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Get the network quality (0.0-1.0) at a world position.
    /// Must be called from main thread outside of jobs.
    /// </summary>
    public float GetNetworkQualityAt(float3 worldPosition)
    {
        JobHandle dependencies;
        CellMapData<TelecomCoverage> coverageData =
            m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies);
        dependencies.Complete();

        return TelecomCoverage.SampleNetworkQuality(coverageData, worldPosition);
    }

    /// <summary>
    /// Check if a building has adequate telecom coverage.
    /// Returns true if quality >= baseline (no efficiency penalty).
    /// </summary>
    public bool HasAdequateCoverage(float3 position, float baseline)
    {
        return GetNetworkQualityAt(position) >= baseline;
    }
}
```

### Example 3: Check Building Telecom Connectivity

Query all buildings that are telecom consumers and check their current efficiency factor.

```csharp
public partial class TelecomConnectivityMonitorSystem : GameSystemBase
{
    private EntityQuery m_ConsumerQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_ConsumerQuery = GetEntityQuery(
            ComponentType.ReadOnly<TelecomConsumer>(),
            ComponentType.ReadOnly<Building>(),
            ComponentType.ReadOnly<Efficiency>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = m_ConsumerQuery.ToEntityArray(Allocator.Temp);
        int connected = 0;
        int degraded = 0;
        int noService = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            DynamicBuffer<Efficiency> efficiencies =
                EntityManager.GetBuffer<Efficiency>(entities[i]);

            float telecomEfficiency = BuildingUtils.GetEfficiency(
                efficiencies, EfficiencyFactor.Telecom);

            if (telecomEfficiency >= 1f)
                connected++;
            else if (telecomEfficiency > 0.5f)
                degraded++;
            else
                noService++;
        }

        if (degraded > 0 || noService > 0)
        {
            Log.Info($"Telecom: {connected} connected, {degraded} degraded, " +
                     $"{noService} no service (of {entities.Length} consumers)");
        }

        entities.Dispose();
    }

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 1024;
}
```

### Example 4: Read City-Level Telecom Statistics

Access the global TelecomStatus to show city-wide network statistics.

```csharp
public partial class TelecomStatsUISystem : UISystemBase
{
    private TelecomCoverageSystem m_TelecomCoverageSystem;
    private ValueBinding<float> m_CapacityBinding;
    private ValueBinding<float> m_LoadBinding;
    private ValueBinding<float> m_QualityBinding;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TelecomCoverageSystem = World.GetOrCreateSystemManaged<TelecomCoverageSystem>();

        AddBinding(m_CapacityBinding =
            new ValueBinding<float>("myMod", "telecomCapacity", 0f));
        AddBinding(m_LoadBinding =
            new ValueBinding<float>("myMod", "telecomLoad", 0f));
        AddBinding(m_QualityBinding =
            new ValueBinding<float>("myMod", "telecomQuality", 0f));
    }

    protected override void OnUpdate()
    {
        // Access the private m_Status field via reflection or
        // by reading from the TelecomCoverageSystem
        // The TelecomStatus is written every 4096 frames
        // For a simpler approach, compute from the cell map directly:

        JobHandle dependencies;
        CellMapData<TelecomCoverage> coverage =
            m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies);
        dependencies.Complete();

        float totalQuality = 0f;
        int cellCount = 0;
        for (int i = 0; i < coverage.m_Buffer.Length; i++)
        {
            TelecomCoverage cell = coverage.m_Buffer[i];
            if (cell.m_SignalStrength > 0)
            {
                float q = math.min(1f, (float)cell.m_SignalStrength /
                    (127.5f + (float)cell.m_NetworkLoad));
                totalQuality += q;
                cellCount++;
            }
        }

        float avgQuality = cellCount > 0 ? totalQuality / cellCount : 0f;
        m_QualityBinding.Update(avgQuality);
    }
}
```

### Example 5: Adjust Telecom Baseline Threshold

Modify the global telecom baseline parameter to change when buildings start suffering efficiency penalties.

```csharp
public partial class TelecomBaselineModifierSystem : GameSystemBase
{
    private EntityQuery m_ParameterQuery;
    private float m_TargetBaseline = 0.3f; // Lower threshold = more forgiving

    protected override void OnCreate()
    {
        base.OnCreate();
        m_ParameterQuery = GetEntityQuery(
            ComponentType.ReadWrite<BuildingEfficiencyParameterData>()
        );
        RequireForUpdate(m_ParameterQuery);
    }

    protected override void OnUpdate()
    {
        // Run once then disable
        Enabled = false;

        var entities = m_ParameterQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            BuildingEfficiencyParameterData data =
                EntityManager.GetComponentData<BuildingEfficiencyParameterData>(entities[i]);

            float originalBaseline = data.m_TelecomBaseline;
            data.m_TelecomBaseline = m_TargetBaseline;

            EntityManager.SetComponentData(entities[i], data);
            Log.Info($"Telecom baseline: {originalBaseline} -> {m_TargetBaseline}");
        }
        entities.Dispose();
    }
}
```

## Open Questions

- [ ] What are the actual default values for `BuildingEfficiencyParameterData.m_TelecomBaseline` and typical `ConsumptionData.m_TelecomNeed` values per building zone type? These come from asset data, not code.
- [ ] How does `TelecomInfoviewUISystem.PerformUpdate()` compute the UI indicator? The decompiled method body is empty -- the actual logic may be in the base class or triggered differently.
- [ ] What is the exact relationship between `TelecomCoverageSystem.m_Status` (NativeArray) and the UI binding? The status array is private and the UI system creates its own separate arrays.
- [ ] How does the `TelecomFacilityMode` interact with difficulty settings or game modes? The mode system applies range/capacity multipliers but the trigger conditions are not in the decompiled code.
- [ ] Does the `PointOfInterest` position randomization on telecom facilities affect any gameplay, or is it purely cosmetic (antenna/satellite dish animation)?

## Sources

- Decompiled (full systems): Game.Simulation.TelecomCoverageSystem, Game.Simulation.TelecomEfficiencySystem, Game.Simulation.TelecomFacilityAISystem, Game.Tools.TelecomPreviewSystem, Game.UI.InGame.TelecomInfoviewUISystem
- Decompiled (components): Game.Buildings.TelecomFacility, Game.Buildings.TelecomFacilityFlags, Game.Buildings.TelecomConsumer, Game.Simulation.TelecomCoverage, Game.Simulation.TelecomStatus, Game.Common.PointOfInterest
- Decompiled (prefab data): Game.Prefabs.TelecomFacilityData, Game.Prefabs.TelecomParameterData, Game.Prefabs.TelecomFacility, Game.Prefabs.TelecomPrefab, Game.Prefabs.ConsumptionData, Game.Prefabs.BuildingEfficiencyParameterData, Game.Prefabs.Modes.TelecomFacilityMode
- Decompiled (enums): Game.Buildings.EfficiencyFactor, Game.City.CityModifierType
- Game version: Cities: Skylines II, decompiled 2026-02-17 using ilspycmd v9.1
