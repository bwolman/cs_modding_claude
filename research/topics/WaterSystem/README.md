# Research: Water Surface Simulation System

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 simulates surface water on the terrain -- rivers, lakes, oceans, flooding -- and specifically how water is added and removed at specific map positions.

**Why**: To build a mod that acts as a "water teleporter" or aqueduct: intake water from the surface at point A (removing it from the simulation) and discharge that same volume at point B (adding it to the simulation).

**Boundaries**: This research covers only the surface water simulation (terrain water). The water/sewage pipe network, groundwater system, and water consumption by buildings are out of scope except where they interact with the surface simulation (pump stations and sewage outlets).

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | WaterSystem, WaterSimulation, WaterUtils, WaterSourceData, SurfaceWater, WaterSourceCache, WaterPumpingStationAISystem, SewageOutletAISystem, FloodCheckSystem, WaterLevelChangeSystem |
| Game.dll | Game.Prefabs | WaterSource, WaterPumpingStation, WaterPumpingStationData, SewageOutletData, AllowedWaterTypes, WaterLevelChangeData |
| Game.dll | Game.Buildings | WaterPumpingStation (runtime component), SewageOutlet (runtime component) |
| Game.dll | Game.Events | WaterLevelChange, Flooded, Flood |
| Game.dll | Game.Rendering | WaterRenderSystem, WaterHeightRequest |
| Game.dll | Game.Tools | WaterToolSystem, WaterSourceDefinition |

## Architecture Overview

The water simulation is **GPU-based**. The core simulation runs as compute shaders on RenderTextures, not as CPU-side ECS logic. The key data structure is a 2048x2048 `RenderTexture` (format `R32G32B32A32_SFloat`) where each texel stores:

- **R channel** (`x`): Water depth at this cell
- **G channel** (`y`): Velocity X component
- **B channel** (`z`): Velocity Z component
- **A channel** (`w`): Pollution level

The map is 14,336 world units across (`kMapSize = 14336`), with a cell size of 7.0 world units (`kCellSize = 7f`), giving exactly 2048 cells per axis.

### CPU-GPU Bridge

The simulation runs on GPU, but the CPU needs to read water levels (for building placement, flood detection, etc.). This is handled by `SurfaceDataReader`, which performs periodic async GPU readbacks. The CPU-side data is exposed as `WaterSurfaceData<SurfaceWater>` -- a flat array of `SurfaceWater` structs.

Water sources (rivers, springs, sewage outputs) are defined as ECS entities with `WaterSourceData` components. Each frame, a CPU-side job collects all water source entities into a `NativeList<WaterSourceCache>`, which is then used to dispatch GPU compute kernels that add/remove water.

## Component Map

### `WaterSourceData` (Game.Simulation)

The core component for any entity that adds or removes water from the surface simulation.

| Field | Type | Description |
|-------|------|-------------|
| m_ConstantDepth | int | Source type flag: 0 = flow source (uses m_Height as target depth above terrain), 2 = sea level marker, 3 = legacy sea source (destroyed on upgrade) |
| m_Radius | float | Radius of effect in world units (max 2500) |
| m_Height | float | For constant sources: target water height above terrain. For flow sources (pump/sewage): rate multiplier. Negative = drain water. Max 250. |
| m_Multiplier | float | Computed by CalculateSourceMultiplier -- normalizes contribution across cells within radius |
| m_Polluted | float | Pollution fraction (0.0 = clean, 1.0 = fully polluted). Set by sewage outlets. |
| m_Id | int | Unique source ID assigned by WaterSystem.GetNextSourceId(). -1 for legacy sources. |
| m_Modifier | float | Runtime modifier, typically 1.0. Set to 0 to disable. |

*Source: `Game.dll` -> `Game.Simulation.WaterSourceData`*

### `SurfaceWater` (Game.Simulation)

Per-cell water data read back from the GPU texture. Not an ECS component -- a plain struct used in `WaterSurfaceData<SurfaceWater>`.

| Field | Type | Description |
|-------|------|-------------|
| m_Depth | float | Water depth at this cell (world units, clamped >= 0) |
| m_Polluted | float | Pollution level at this cell |
| m_Velocity | float2 | Flow velocity (X, Z) in world-space units |

*Source: `Game.dll` -> `Game.Simulation.SurfaceWater`*

### `WaterSourceCache` (Game.Simulation)

Cached snapshot of a water source, built each frame by the SourceJob. Passed to the GPU source step.

| Field | Type | Description |
|-------|------|-------------|
| m_ConstantDepth | int | Copied from WaterSourceData |
| m_Position | float3 | World position minus terrain offset |
| m_Polluted | float | Pollution fraction |
| m_Multiplier | float | Rate multiplier |
| m_Radius | float | Effect radius (already multiplied by m_Modifier) |
| m_Height | float | Target height / rate value |

*Source: `Game.dll` -> `Game.Simulation.WaterSourceCache`*

### `WaterPumpingStation` (Game.Buildings)

Runtime state for a water pumping station building.

| Field | Type | Description |
|-------|------|-------------|
| m_Pollution | float | Current pollution fraction of pumped water |
| m_Capacity | int | Current production capacity (after efficiency) |
| m_LastProduction | int | Amount of water produced last tick |

*Source: `Game.dll` -> `Game.Buildings.WaterPumpingStation`*

### `SewageOutlet` (Game.Buildings)

Runtime state for a sewage outlet building.

| Field | Type | Description |
|-------|------|-------------|
| m_Capacity | int | Processing capacity after efficiency |
| m_LastProcessed | int | Sewage volume processed last tick |
| m_LastPurified | int | Volume purified (subset of processed) |
| m_UsedPurified | int | Purified water reused by co-located pump |

*Source: `Game.dll` -> `Game.Buildings.SewageOutlet`*

### `WaterPumpingStationData` (Game.Prefabs)

Prefab configuration for a water pumping station.

| Field | Type | Description |
|-------|------|-------------|
| m_Types | AllowedWaterTypes | Flags: None=0, Groundwater=1, SurfaceWater=2 |
| m_Capacity | int | Maximum pumping capacity |
| m_Purification | float | Fraction of pollution removed (0-1) |

*Source: `Game.dll` -> `Game.Prefabs.WaterPumpingStationData`*

### `SewageOutletData` (Game.Prefabs)

Prefab configuration for a sewage outlet.

| Field | Type | Description |
|-------|------|-------------|
| m_Capacity | int | Maximum processing capacity |
| m_Purification | float | Fraction of sewage purified before discharge |

*Source: `Game.dll` -> `Game.Prefabs.SewageOutletData`*

### `Flooded` (Game.Events)

Attached to buildings that are submerged by rising water.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The flood/tsunami event entity that caused the flooding |
| m_Depth | float | Current flood depth at the building |

*Source: `Game.dll` -> `Game.Events.Flooded`*

### `WaterLevelChange` (Game.Events)

Represents a tsunami or water level change event.

| Field | Type | Description |
|-------|------|-------------|
| m_Intensity | float | Current intensity |
| m_MaxIntensity | float | Peak intensity |
| m_DangerHeight | float | Water height that constitutes danger |
| m_Direction | float2 | Direction of wave propagation |

*Source: `Game.dll` -> `Game.Events.WaterLevelChange`*

## System Map

### `WaterSystem` (Game.Simulation)

The central orchestrator for the entire water surface simulation.

- **Base class**: GameSystemBase (also implements IGPUSystem, ISerializable, IPostDeserialize)
- **Update phase**: Simulation (GPU)
- **Key constants**:
  - `kMapSize = 14336` -- world size of the water map
  - `kCellSize = 7f` -- world units per grid cell
  - `kDefaultSeaLevel = 511.7f` -- default sea level height
  - Texture size: 2048x2048
- **Key properties**:
  - `SeaLevel` -- current sea level (writes trigger `UpdateSeaLevel()`)
  - `GetSurfaceData(out JobHandle)` -- returns CPU-readable water depth array
  - `GetSurfacesData(out JobHandle)` -- includes backdrop data
  - `AddSurfaceReader(JobHandle)` -- register dependency on surface data
- **OnUpdate()**: Collects all WaterSourceData entities into a NativeList<WaterSourceCache> via SourceJob, then flips the double-buffered cache.
- **Simulate()** (called from OnSimulateGPU): Runs the GPU simulation loop:
  1. `EvaporateStep` -- evaporation and rain
  2. `SourceStep` -- apply water sources (add/remove water via compute kernels)
  3. `VelocityStep` -- shallow water equations (velocity update)
  4. `DepthStep` -- depth update from velocities
  5. `CopyToHeightmapStep` -- write results to render texture

### `WaterSimulation` (Game.Simulation)

The compute shader driver. Not an ECS system -- a plain C# class owned by WaterSystem.

- **Key parameters**:
  - `Damping = 0.995f` -- velocity damping per step
  - `Evaporation = 0.001f` -- water loss per step
  - `Fluidness = 0.15f` -- shallow water equation coefficient
  - `WaterSourceSpeed = 2f` -- rate at which sources apply their effect
  - `MaxVelocity = 12f` -- velocity clamp
- **SourceStep()**: Iterates `LastFrameSourceCache` and dispatches compute kernels:
  - For polluted sources (`m_Polluted > 0`): dispatches `Add` kernel with amount and pollution
  - For constant-depth sources: dispatches `AddConstant` kernel that targets a specific water surface height
  - The source height can be negative, which **drains** water (used by pump stations)

### `WaterPumpingStationAISystem` (Game.Simulation)

Handles water pump buildings that extract surface water.

- **Base class**: GameSystemBase
- **Update interval**: 128 frames (offset 64)
- **Queries**: Entities with `WaterPumpingStation`, `WaterPipeBuildingConnection`, `PrefabRef`, `Transform` (excluding Temp, Deleted)
- **Key behavior**: For each pump with `AllowedWaterTypes.SurfaceWater`:
  1. Gets the pump's SubObject entities (water intake points)
  2. For each SubObject with `WaterSourceData`, reads `WaterUtils.SampleDepth()` at that position
  3. Computes availability: `depth / effectiveDepth` clamped to [0,1]
  4. **Sets `WaterSourceData.m_Height = -0.0001f * capacity * efficiency`** -- this is the drain rate
  5. The negative height causes the GPU source step to remove water

### `SewageOutletAISystem` (Game.Simulation)

Handles sewage outlet buildings that discharge water back to the surface.

- **Base class**: GameSystemBase
- **Update interval**: 128 frames (offset 64)
- **Key behavior**: For each outlet's SubObject with `WaterSourceData`:
  1. Computes discharge volume from `m_LastProcessed - m_LastPurified + purified reuse`
  2. **Sets `WaterSourceData.m_Height = min(2.5, surfaceWaterUsageMultiplier * volume)`** -- positive = add water
  3. Sets `m_Polluted` to fraction of unpurified discharge
  4. Sets `m_Modifier = 1.0` (or 0.0 if no flow)

### `WaterSourceInitializeSystem` (Game.Simulation)

Initializes newly created WaterSourceData entities from their prefab data.

- **Queries**: Entities with `WaterSourceData`, `PrefabRef`, `Created` (excluding Temp)
- Sets m_Height, m_Radius, m_Polluted from prefab; m_Modifier = 1.0; m_Id = -1

### `FloodCheckSystem` (Game.Simulation)

Checks buildings for flooding every 16 frames.

- **Key method**: `IsFlooded()` calls `WaterUtils.SampleDepth()` -- if depth > 0.5 and building is below water surface, creates a `Submerge` event

### `WaterUtils` (Game.Simulation)

Static utility class for sampling water data on the CPU side.

- `SampleDepth(ref WaterSurfaceData<SurfaceWater>, float3 worldPosition)` -- bilinear interpolation of depth at a world position
- `SampleHeight(ref WaterSurfaceData<SurfaceWater>, ref TerrainHeightData, float3)` -- terrain height + water depth = water surface elevation
- `SamplePolluted(ref WaterSurfaceData<SurfaceWater>, float3)` -- pollution level at position
- `SampleVelocity(ref WaterSurfaceData<SurfaceWater>, float3)` -- flow velocity at position
- `ToSurfaceSpace()` / `ToWorldSpace()` -- coordinate transforms between world and texture space

## Data Flow

### Water Source Entity -> GPU Simulation -> CPU Readback

```
WaterSourceData entities (ECS)
    |
    v
WaterSystem.OnUpdate()
    |-- SourceJob (CPU, IJob)
    |   Collects all WaterSourceData + Transform into NativeList<WaterSourceCache>
    |
    v
WaterSystem.Simulate() (called from OnSimulateGPU)
    |
    |-- EvaporateStep (GPU compute)
    |   Evaporates water, applies rain, decays pollution
    |
    |-- SourceStep (GPU compute)  <--- THIS IS WHERE WATER IS ADDED/REMOVED
    |   For each WaterSourceCache entry:
    |     If m_Polluted > 0: dispatch "Add" kernel (adds water + pollution)
    |     Else: dispatch "AddConstant" kernel (sets water to target height)
    |   Negative m_Height = drain water (pump stations)
    |   Positive m_Height = add water (sewage outlets, rivers)
    |
    |-- VelocityStep (GPU compute)
    |   Shallow water equations: compute velocity from pressure gradients
    |
    |-- DepthStep (GPU compute)
    |   Update depth from velocity divergence
    |
    |-- CopyToHeightmapStep (GPU compute)
    |   Copy results to render texture
    |
    v
Async GPU Readback (every ~30 frames)
    |
    v
SurfaceDataReader -> WaterSurfaceData<SurfaceWater>  (CPU-readable)
    |
    v
Other systems read via WaterSystem.GetSurfaceData():
  - WaterPumpingStationAISystem (checks depth at pump location)
  - FloodCheckSystem (checks if buildings are submerged)
  - WaterDamageSystem, WaterDangerSystem, etc.
```

### How Pumps Drain Water (Intake)

```
WaterPumpingStationAISystem.PumpTickJob
    |
    |-- For each pump building's SubObject with WaterSourceData:
    |   1. SampleDepth() at intake position -> availability
    |   2. Set WaterSourceData.m_Height = -0.0001 * capacity * efficiency
    |      (negative height = drain)
    |   3. WaterSourceData.m_Modifier = 1.0 (active)
    |
    v
Next frame: SourceJob collects this into WaterSourceCache
    |
    v
GPU SourceStep: "AddConstant" kernel sees negative target height -> removes water
```

### How Sewage Outlets Add Water (Output)

```
SewageOutletAISystem.OutletTickJob
    |
    |-- For each outlet's SubObject with WaterSourceData:
    |   1. Compute discharge volume from pipe flow
    |   2. Set WaterSourceData.m_Height = min(2.5, multiplier * volume)
    |      (positive height = add water)
    |   3. Set m_Polluted = fraction of unpurified flow
    |   4. Set m_Modifier = 1.0 (or 0.0 if no flow)
    |
    v
Next frame: SourceJob collects this
    |
    v
GPU SourceStep: "Add" kernel deposits water + pollution at outlet position
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Water source radius | WaterSource.m_Radius | Game.Prefabs.WaterSource (ComponentBase on ObjectPrefab) |
| Water source height | WaterSource.m_Height | Game.Prefabs.WaterSource |
| Pump capacity | WaterPumpingStation.m_Capacity | Game.Prefabs.WaterPumpingStation |
| Pump allowed types | WaterPumpingStation.m_Types | Game.Prefabs.WaterPumpingStation (AllowedWaterTypes flags) |
| Outlet capacity | SewageOutlet.m_Capacity | Game.Prefabs.SewageOutlet |
| Outlet purification | SewageOutlet.m_Purification | Game.Prefabs.SewageOutlet |
| Surface pump effective depth | WaterPipeParameterData.m_SurfaceWaterPumpEffectiveDepth | Singleton entity |
| Surface usage multiplier | WaterPipeParameterData.m_SurfaceWaterUsageMultiplier | Singleton entity |
| Sea level | WaterSystem.SeaLevel | Default 511.7f |
| Cell size | WaterSystem.kCellSize | 7.0 world units (hardcoded) |
| Map size | WaterSystem.kMapSize | 14336 world units (hardcoded) |
| Texture resolution | WaterSystem.m_TexSize | 2048x2048 (hardcoded) |

## Harmony Patch Points

### Candidate 1: `WaterPumpingStationAISystem.PumpTickJob.Execute`

- **Signature**: `void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Not patchable (BurstCompile job struct)
- **Note**: Burst-compiled jobs cannot be Harmony-patched. Must use a different approach.

### Candidate 2: `WaterPumpingStationAISystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Could intercept before/after the pump job runs, modify WaterSourceData on pump SubObjects
- **Risk level**: Medium -- the job is already scheduled by the time Postfix runs
- **Side effects**: Must complete the job before modifying source data

### Candidate 3: Directly creating WaterSourceData entities

- **Signature**: N/A -- no patch needed
- **Patch type**: N/A
- **What it enables**: Create your own entities with WaterSourceData + Transform to act as water sources/sinks. This is the cleanest approach -- no patching required.
- **Risk level**: Low -- the game's SourceJob naturally picks up any entity with WaterSourceData
- **Side effects**: None if entities are properly configured

## Mod Blueprint: Water Teleporter / Aqueduct

The cleanest approach does **not** require any Harmony patches. The game's water source system is fully data-driven: any entity with `WaterSourceData` + `Transform` (not Temp, not Deleted) is automatically collected by `WaterSystem.OnUpdate()` and applied to the GPU simulation.

### Strategy

1. **Create two WaterSourceData entities**: one at the intake (negative height = drain) and one at the output (positive height = add water)
2. **Create a custom GameSystemBase** that monitors the intake's water depth and adjusts the output's flow rate to match
3. The intake drains water exactly like a pump station does; the output adds water exactly like a sewage outlet does

### Systems to create

1. **AqueductSystem** (`GameSystemBase`) -- the core system:
   - Owns two entities: intake source and output source
   - Each tick: read water depth at intake position via `WaterUtils.SampleDepth()`
   - Compute available flow rate based on depth
   - Set intake `WaterSourceData.m_Height` to a small negative value (drain)
   - Set output `WaterSourceData.m_Height` to matching positive value (deposit)
   - This conserves volume: what you remove at A, you add at B

2. **AqueductToolSystem** (optional) -- placement tool for intake/output points

### Components to add

- `AqueductData` (IComponentData) -- links intake entity to output entity, stores flow rate config
- The intake and output entities each get: `WaterSourceData`, `Game.Objects.Transform`

### Patches needed

- **None** -- the game's SourceJob naturally collects all WaterSourceData entities

### Settings

- Flow rate / capacity
- Intake radius
- Output radius
- Enable/disable

### Key Implementation Details

#### Creating a Water Source Entity

Water source entities need these components to be collected by `WaterSystem.OnUpdate()`:

1. `WaterSourceData` -- the source configuration
2. `Game.Objects.Transform` -- the world position
3. Must NOT have `Temp` or `Deleted` components

The `m_ConstantDepth` field controls behavior:
- `0` = constant-depth source (targets a specific water surface height)
- For drain/flow purposes, use `m_ConstantDepth = 0` with negative `m_Height`

#### Drain Rate (Intake)

The pump station sets: `m_Height = -0.0001f * capacity * efficiency`

This is a very small negative number because the GPU compute shader accumulates the effect over many cells within the radius. A typical pump might have capacity ~65000, giving `m_Height ~ -6.5`.

#### Add Rate (Output)

The sewage outlet sets: `m_Height = min(2.5f, multiplier * volume)`

Clamped to 2.5 to prevent explosive water addition. The `m_SurfaceWaterUsageMultiplier` parameter scales the volume.

#### Volume Conservation

The simulation does NOT inherently conserve volume between sources. Evaporation continuously removes water, and sources add/remove at their own rates. To conserve volume in a teleporter:

1. Read the actual depth decrease at the intake position after each tick
2. Use that measured decrease to set the output rate
3. Or, simpler: use matching rates and accept approximate conservation

## Examples

### Example 1: Create a Water Drain Source (Intake)

```csharp
/// <summary>
/// Creates an entity that drains surface water at the given world position.
/// </summary>
public Entity CreateWaterDrain(EntityManager em, float3 worldPosition, float radius, float drainRate)
{
    Entity entity = em.CreateEntity(
        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
        ComponentType.ReadWrite<Game.Objects.Transform>()
    );

    em.SetComponentData(entity, new Game.Objects.Transform
    {
        m_Position = worldPosition,
        m_Rotation = quaternion.identity
    });

    em.SetComponentData(entity, new Game.Simulation.WaterSourceData
    {
        m_ConstantDepth = 0,
        m_Radius = radius,          // e.g. 50f
        m_Height = -drainRate,       // negative = drain. e.g. -0.5f
        m_Multiplier = 1f,
        m_Polluted = 0f,
        m_Id = -1,                   // will be assigned by system
        m_Modifier = 1f
    });

    return entity;
}
```

### Example 2: Create a Water Output Source (Discharge)

```csharp
/// <summary>
/// Creates an entity that adds water to the surface at the given world position.
/// </summary>
public Entity CreateWaterOutput(EntityManager em, float3 worldPosition, float radius, float outputRate, float pollution)
{
    Entity entity = em.CreateEntity(
        ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(),
        ComponentType.ReadWrite<Game.Objects.Transform>()
    );

    em.SetComponentData(entity, new Game.Objects.Transform
    {
        m_Position = worldPosition,
        m_Rotation = quaternion.identity
    });

    em.SetComponentData(entity, new Game.Simulation.WaterSourceData
    {
        m_ConstantDepth = 0,
        m_Radius = radius,          // e.g. 50f
        m_Height = outputRate,       // positive = add water. e.g. 1.0f
        m_Multiplier = 1f,
        m_Polluted = pollution,      // 0.0 = clean, 1.0 = fully polluted
        m_Id = -1,
        m_Modifier = 1f
    });

    return entity;
}
```

### Example 3: Read Water Depth at a Position

```csharp
/// <summary>
/// Samples the current water depth at a world position.
/// Call from within a system that has access to WaterSystem.
/// </summary>
public float GetWaterDepthAt(WaterSystem waterSystem, float3 worldPosition)
{
    JobHandle deps;
    WaterSurfaceData<SurfaceWater> surfaceData = waterSystem.GetSurfaceData(out deps);
    deps.Complete();

    return WaterUtils.SampleDepth(ref surfaceData, worldPosition);
}
```

### Example 4: Complete Aqueduct System

```csharp
public partial class AqueductSystem : GameSystemBase
{
    private WaterSystem _waterSystem;
    private Entity _intakeEntity;
    private Entity _outputEntity;
    private bool _initialized;

    // Configuration
    private float3 _intakePosition;
    private float3 _outputPosition;
    private float _radius = 50f;
    private float _maxFlowRate = 1.0f;

    protected override void OnCreate()
    {
        base.OnCreate();
        _waterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
    }

    public void Initialize(float3 intakePos, float3 outputPos, float radius, float maxFlow)
    {
        _intakePosition = intakePos;
        _outputPosition = outputPos;
        _radius = radius;
        _maxFlowRate = maxFlow;

        // Create intake source (drain)
        _intakeEntity = EntityManager.CreateEntity(
            ComponentType.ReadWrite<WaterSourceData>(),
            ComponentType.ReadWrite<Game.Objects.Transform>()
        );
        EntityManager.SetComponentData(_intakeEntity, new Game.Objects.Transform
        {
            m_Position = _intakePosition,
            m_Rotation = quaternion.identity
        });
        EntityManager.SetComponentData(_intakeEntity, new WaterSourceData
        {
            m_ConstantDepth = 0,
            m_Radius = _radius,
            m_Height = 0f,  // will be set each update
            m_Multiplier = 1f,
            m_Polluted = 0f,
            m_Id = _waterSystem.GetNextSourceId(),
            m_Modifier = 1f
        });

        // Create output source (add)
        _outputEntity = EntityManager.CreateEntity(
            ComponentType.ReadWrite<WaterSourceData>(),
            ComponentType.ReadWrite<Game.Objects.Transform>()
        );
        EntityManager.SetComponentData(_outputEntity, new Game.Objects.Transform
        {
            m_Position = _outputPosition,
            m_Rotation = quaternion.identity
        });
        EntityManager.SetComponentData(_outputEntity, new WaterSourceData
        {
            m_ConstantDepth = 0,
            m_Radius = _radius,
            m_Height = 0f,  // will be set each update
            m_Multiplier = 1f,
            m_Polluted = 0f,
            m_Id = _waterSystem.GetNextSourceId(),
            m_Modifier = 1f
        });

        _initialized = true;
    }

    protected override void OnUpdate()
    {
        if (!_initialized) return;

        // Sample water depth at intake
        JobHandle deps;
        WaterSurfaceData<SurfaceWater> surfaceData = _waterSystem.GetSurfaceData(out deps);
        deps.Complete();
        _waterSystem.AddSurfaceReader(Dependency);

        float depthAtIntake = WaterUtils.SampleDepth(ref surfaceData, _intakePosition);

        // Scale flow rate by available water (0 if dry, maxFlow if deep enough)
        float availability = math.saturate(depthAtIntake / 2f); // full flow at 2m depth
        float flowRate = _maxFlowRate * availability;

        // Update intake: negative height = drain
        var intakeData = EntityManager.GetComponentData<WaterSourceData>(_intakeEntity);
        intakeData.m_Height = -flowRate;
        EntityManager.SetComponentData(_intakeEntity, intakeData);

        // Update output: positive height = add water (matching volume)
        var outputData = EntityManager.GetComponentData<WaterSourceData>(_outputEntity);
        outputData.m_Height = flowRate;
        EntityManager.SetComponentData(_outputEntity, outputData);
    }

    protected override void OnDestroy()
    {
        if (_initialized)
        {
            if (EntityManager.Exists(_intakeEntity))
                EntityManager.DestroyEntity(_intakeEntity);
            if (EntityManager.Exists(_outputEntity))
                EntityManager.DestroyEntity(_outputEntity);
        }
        base.OnDestroy();
    }
}
```

### Example 5: Disable a Water Source Temporarily

```csharp
/// <summary>
/// Disable a water source by setting m_Modifier to 0.
/// The SourceJob multiplies radius by m_Modifier, so 0 = zero radius = no effect.
/// </summary>
public void DisableWaterSource(EntityManager em, Entity sourceEntity)
{
    var data = em.GetComponentData<WaterSourceData>(sourceEntity);
    data.m_Modifier = 0f;
    em.SetComponentData(sourceEntity, data);
}

public void EnableWaterSource(EntityManager em, Entity sourceEntity)
{
    var data = em.GetComponentData<WaterSourceData>(sourceEntity);
    data.m_Modifier = 1f;
    em.SetComponentData(sourceEntity, data);
}
```

## Open Questions

- [x] How does the GPU simulation add/remove water? Via compute shader kernels ("Add" and "AddConstant") dispatched per water source
- [x] Can we create our own WaterSourceData entities? Yes -- the SourceJob collects ALL entities with WaterSourceData + Transform
- [x] Does negative m_Height drain water? Yes -- pump stations use this exact mechanism
- [x] Is volume conserved? No -- evaporation, rain, and independent source rates mean volume is not automatically conserved. A mod must manually match drain/output rates.
- [ ] What is the exact relationship between m_Height magnitude and volume per second? The GPU shader applies the value scaled by timestep and WaterSourceSpeed. Exact calibration requires in-game testing.
- [ ] Does creating WaterSourceData entities at runtime (after map load) require calling `WaterSystem.GetNextSourceId()`? The SourceJob collects entities regardless of m_Id, but the new (non-legacy) system uses IDs for tracking. Safest to call it.
- [ ] What minimum depth at the intake prevents visual artifacts (pulsing, dry-wet cycling)? Needs in-game testing with various flow rates and radii.
- [ ] Does the SourceJob filter by any components beyond WaterSourceData / Transform / !Temp / !Deleted? From decompilation, these are the only filters.

## Sources

- Decompiled from: Game.dll (Game.Simulation namespace, Game.Buildings namespace, Game.Prefabs namespace, Game.Events namespace)
- Key types: WaterSystem, WaterSimulation, WaterUtils, WaterSourceData, SurfaceWater, WaterSourceCache, WaterPumpingStationAISystem, SewageOutletAISystem, FloodCheckSystem
- All decompiled snippets saved in `snippets/` directory
