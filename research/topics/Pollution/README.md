# Research: Pollution Systems

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 models air pollution, ground pollution, and noise pollution -- including how pollution is produced by buildings and roads, how it spreads across the map, how it decays, and what downstream effects it has on citizen health, happiness, land value, and groundwater.

**Why**: To understand the full pollution pipeline so a mod can read, modify, or override pollution values -- for example, adding custom pollution sources, reducing pollution in specific areas, or changing how pollution affects gameplay.

**Boundaries**: This research covers the three main pollution types (air, ground, noise) and their grid-based simulation systems. Water pollution (the A channel in the water surface simulation) is covered in the Water System research topic. Citizen health effects from pollution are touched on where they connect to the pollution trigger system.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | AirPollutionSystem, GroundPollutionSystem, NoisePollutionSystem, BuildingPollutionAddSystem, NetPollutionSystem, ObjectPolluteSystem, PollutionTriggerSystem, GroundWaterPollutionSystem, WaterPipePollutionSystem |
| Game.dll | Game.Simulation | AirPollution (struct), GroundPollution (struct), NoisePollution (struct) |
| Game.dll | Game.Prefabs | PollutionData, PollutionParameterData, PollutionModifierData, NetPollutionData, ZonePollutionData, Pollution (ComponentBase) |
| Game.dll | Game.Buildings | PollutionEmitModifier |
| Game.dll | Game.Net | Pollution (struct -- per-edge/node traffic pollution accumulation) |
| Game.dll | Game.UI.InGame | PollutionInfoviewUISystem, PollutionSection |

## Architecture Overview

CS2 has **three independent pollution grid maps**, all stored as `CellMapSystem<T>` with a **256x256** texture covering the full `kMapSize` (14,336 world units). Each cell is approximately 56 world units. All three maps use `short` (16-bit signed integer, max 32,767) for pollution values and update **128 times per game day** (every 2,048 simulation frames out of the 262,144 frames per day).

### The Three Pollution Types

1. **Air Pollution** (`AirPollution` / `AirPollutionSystem`): Spreads via wind advection. Each update, the system samples pollution values offset by wind direction, then applies a 4-neighbor diffusion pass with a `kSpread` factor of 3 (right-shift by 3, so each neighbor contributes ~12.5% of its value). Air pollution decays by `m_AirFade / kUpdatesPerDay` per tick.

2. **Ground Pollution** (`GroundPollution` / `GroundPollutionSystem`): Does **not** spread spatially. Each update, a simple fade job decreases every cell by `m_GroundFade / kUpdatesPerDay`. Ground pollution only increases when buildings actively emit it (via `BuildingPollutionAddSystem`). Ground pollution also contaminates groundwater through `GroundWaterPollutionSystem`.

3. **Noise Pollution** (`NoisePollution` / `NoisePollutionSystem`): Uses a **double-buffered** approach. Sources write to `m_PollutionTemp`; each tick, a swap job computes the final `m_Pollution` from `m_PollutionTemp` using an 8-neighbor weighted average (cardinal neighbors at 1/8 weight, diagonal neighbors at 1/16 weight, center at 1/4 weight). After swapping, `m_PollutionTemp` is cleared to zero. This means noise pollution is **recalculated from scratch every tick** -- it does not accumulate over time.

### Pollution Sources

**Buildings** (`BuildingPollutionAddSystem`): The primary pollution emitter. Iterates all building entities (excluding Temp/Deleted/Placeholder) and computes pollution from the building's `PollutionData` prefab component. The emitted amounts are:
- Scaled by building efficiency
- Optionally scaled by renter count (when `m_ScaleWithRenters` is true)
- Modified by `PollutionModifierData` from installed upgrades (multiplicative)
- Modified by `PollutionEmitModifier` runtime component (multiplicative)
- Modified by `CityModifier` for industrial zones (`IndustrialGroundPollution`, `IndustrialAirPollution`)
- Abandoned buildings emit noise pollution proportional to lot size
- Homeless citizens in parks/abandoned buildings add noise pollution

The system distributes each building's pollution across grid cells using a distance-weighted kernel with configurable radius (`m_GroundRadius`, `m_AirRadius`, `m_NoiseRadius`) and exponent (`m_DistanceExponent`).

**Roads/Networks** (`NetPollutionSystem`): Road segments and nodes accumulate traffic pollution in a `Game.Net.Pollution` component (float2: noise factor and air factor). Each tick, the system applies these accumulated values to the air and noise grids. Key features:
- Sound barriers on road upgrades reduce noise (both sides = 100% reduction on edges, 50% center; one side = directional reduction)
- Beautification upgrades reduce noise by ~25-50%
- Middle beautification reduces noise by ~12.5%
- Tunnels skip pollution entirely
- Noise is applied at multiple points along road curves for even coverage

**Plants** (`ObjectPolluteSystem`): Plants are *affected by* pollution, not sources. Each tick, plants accumulate a pollution value based on ground and air pollution at their position, minus a fade rate.

### Downstream Effects

- **Groundwater**: `GroundWaterPollutionSystem` reads the ground pollution map and increases `GroundWater.m_Polluted` proportionally (`pollution / 200`), up to the water amount.
- **Citizen Happiness**: `PollutionTriggerSystem` calculates average air pollution exposure across all households and sends a trigger for milestone/objective tracking.
- **Land Value**: The `m_GroundPollutionLandValueDivisor` parameter in `PollutionParameterData` suggests ground pollution reduces land value.
- **Notifications**: Each pollution type has a notification limit; exceeding it triggers a player notification.

## Component Map

### `AirPollution` (Game.Simulation)

Cell map struct for air pollution values.

| Field | Type | Description |
|-------|------|-------------|
| m_Pollution | short | Pollution level (0 to 32,767) |

Implements `IPollution` -- the `Add(short)` method clamps to 32,767.

### `GroundPollution` (Game.Simulation)

Cell map struct for ground pollution values.

| Field | Type | Description |
|-------|------|-------------|
| m_Pollution | short | Pollution level (0 to 32,767) |

### `NoisePollution` (Game.Simulation)

Cell map struct for noise pollution values. Double-buffered.

| Field | Type | Description |
|-------|------|-------------|
| m_Pollution | short | Final smoothed pollution (read by consumers) |
| m_PollutionTemp | short | Temporary accumulator (written by sources, cleared each tick) |

### `PollutionData` (Game.Prefabs)

Prefab component defining a building's base pollution emission.

| Field | Type | Description |
|-------|------|-------------|
| m_GroundPollution | float | Base ground pollution per day |
| m_AirPollution | float | Base air pollution per day |
| m_NoisePollution | float | Base noise pollution per day |
| m_ScaleWithRenters | bool | If true, pollution scales with renter count and education level |

### `PollutionParameterData` (Game.Prefabs)

Singleton component controlling global pollution parameters.

| Field | Type | Description |
|-------|------|-------------|
| m_GroundMultiplier | float | Global multiplier for ground pollution emission |
| m_AirMultiplier | float | Global multiplier for air pollution emission |
| m_NoiseMultiplier | float | Global multiplier for noise pollution emission |
| m_NetAirMultiplier | float | Multiplier for road/net air pollution |
| m_NetNoiseMultiplier | float | Multiplier for road/net noise pollution |
| m_GroundRadius | float | Spread radius for building ground pollution |
| m_AirRadius | float | Spread radius for building air pollution |
| m_NoiseRadius | float | Spread radius for building noise pollution |
| m_NetNoiseRadius | float | Base noise radius for road segments |
| m_WindAdvectionSpeed | float | How much wind moves air pollution per tick |
| m_AirFade | short | Air pollution decay per day |
| m_GroundFade | short | Ground pollution decay per day |
| m_PlantAirMultiplier | float | How much air pollution affects plants |
| m_PlantGroundMultiplier | float | How much ground pollution affects plants |
| m_PlantFade | float | Plant pollution recovery rate per day |
| m_FertilityGroundMultiplier | float | Ground pollution effect on soil fertility |
| m_DistanceExponent | float | Exponent for distance-weighted pollution spread |
| m_AirPollutionNotificationLimit | int | Threshold for air pollution player notification |
| m_NoisePollutionNotificationLimit | int | Threshold for noise pollution player notification |
| m_GroundPollutionNotificationLimit | int | Threshold for ground pollution player notification |
| m_AbandonedNoisePollutionMultiplier | float | Noise multiplier for abandoned buildings (per lot tile) |
| m_HomelessNoisePollution | int | Noise pollution per homeless citizen |
| m_GroundPollutionLandValueDivisor | int | Divisor for ground pollution's land value impact |

### `PollutionModifierData` (Game.Prefabs)

Upgrade component that modifies a building's pollution emission.

| Field | Type | Description |
|-------|------|-------------|
| m_GroundPollutionMultiplier | float | Additive multiplier offset (e.g., -0.5 = 50% reduction) |
| m_AirPollutionMultiplier | float | Additive multiplier offset |
| m_NoisePollutionMultiplier | float | Additive multiplier offset |

Applied as: `pollution *= max(0, 1 + modifier)`. Multiple upgrades combine additively.

### `PollutionEmitModifier` (Game.Buildings)

Runtime component on building entities that adjusts pollution emission. Applied multiplicatively to `PollutionData` values: `pollution += modifier * pollution`.

| Field | Type | Description |
|-------|------|-------------|
| m_GroundPollutionModifier | float | Runtime ground pollution modifier |
| m_AirPollutionModifier | float | Runtime air pollution modifier |
| m_NoisePollutionModifier | float | Runtime noise pollution modifier |

### `Game.Net.Pollution` (Game.Net)

Per-edge/node component tracking traffic pollution on road networks.

| Field | Type | Description |
|-------|------|-------------|
| m_Pollution | float2 | Current frame traffic pollution (noise, air) |
| m_Accumulation | float2 | Smoothed accumulation (lerps toward m_Pollution at 4/kUpdatesPerDay) |

### `NetPollutionData` (Game.Prefabs)

Prefab data for road network pollution factors.

| Field | Type | Description |
|-------|------|-------------|
| m_Factors | float2 | (noise factor, air factor) for this road type |

## System Map

### `AirPollutionSystem` (Game.Simulation)

- **Base class**: `CellMapSystem<AirPollution>`
- **Grid size**: 256x256
- **Update rate**: 128 times/day (every 2,048 frames)
- **Key behavior**: Wind advection + 4-neighbor diffusion + fade
- **Wind interaction**: Samples pollution offset by `m_WindAdvectionSpeed * windVector`
- **Spread factor**: `kSpread = 3` (each neighbor contributes `value >> 3` = ~12.5%)
- **Decay**: `m_AirFade / kUpdatesPerDay` per tick (randomized rounding)

### `GroundPollutionSystem` (Game.Simulation)

- **Base class**: `CellMapSystem<GroundPollution>`
- **Grid size**: 256x256
- **Update rate**: 128 times/day
- **Key behavior**: Simple fade only -- no spatial spreading
- **Decay**: `m_GroundFade / kUpdatesPerDay` per tick

### `NoisePollutionSystem` (Game.Simulation)

- **Base class**: `CellMapSystem<NoisePollution>`
- **Grid size**: 256x256
- **Update rate**: 128 times/day
- **Key behavior**: Double-buffered. Sources write to `m_PollutionTemp`. Each tick:
  1. `NoisePollutionSwapJob`: Computes `m_Pollution` from `m_PollutionTemp` using 8-neighbor weighted average
  2. `NoisePollutionClearJob`: Resets `m_PollutionTemp` to 0
- **Weights**: center = 1/4, cardinal = 1/8, diagonal = 1/16

### `BuildingPollutionAddSystem` (Game.Simulation)

- **Base class**: `GameSystemBase`
- **Update rate**: 128 * 16 = 2,048 times/day (16 update frame groups)
- **Queries**: All `Building` entities (excluding Temp, Deleted, Placeholder)
- **Key behavior**: Computes each building's pollution from prefab data + modifiers, enqueues pollution items, then applies them to all three grids using distance-weighted kernels
- **Distance weighting**: `1 / max(20, distance^exponent)`, cached in a 256-entry lookup table

### `NetPollutionSystem` (Game.Simulation)

- **Base class**: `GameSystemBase`
- **Update rate**: 128 * 16 = 2,048 times/day
- **Key behavior**: Reads `Game.Net.Pollution` accumulation from road entities, applies noise + air pollution to grid maps
- **Sound barriers**: Reduce noise from roads (up to 100% on the barrier side)
- **Beautification**: Reduces noise by ~25-50%
- **Tunnels**: Skip pollution entirely

### `ObjectPolluteSystem` (Game.Simulation)

- **Base class**: `GameSystemBase`
- **Update rate**: 32 * 16 = 512 times/day
- **Key behavior**: Updates `Plant.m_Pollution` based on ground + air pollution at plant position

### `GroundWaterPollutionSystem` (Game.Simulation)

- **Base class**: `GameSystemBase`
- **Update interval**: 128 frames (offset 64)
- **Key behavior**: Ground pollution contaminates groundwater. For each groundwater cell, adds `groundPollution / 200`, capped at `GroundWater.m_Amount`.

### `PollutionTriggerSystem` (Game.Simulation)

- **Base class**: `GameSystemBase`
- **Update interval**: 4,096 frames
- **Key behavior**: Calculates average air pollution happiness impact across all households, sends `TriggerType.AverageAirPollution` for milestone tracking

## Data Flow

### Building Pollution Emission

```
Building Entity (with PollutionData on prefab)
    |
    v
BuildingPollutionAddSystem.BuildingPolluteJob
    |-- Read PollutionData from prefab
    |-- Apply efficiency multiplier
    |-- Scale with renters (if m_ScaleWithRenters)
    |-- Apply PollutionModifierData from upgrades (multiplicative)
    |-- Apply PollutionEmitModifier runtime modifier
    |-- Apply CityModifier (industrial zones)
    |-- Enqueue PollutionItem {amount, position} to per-type queues
    |
    v
BuildingPollutionAddSystem.ApplyBuildingPollutionJob<T>
    |-- For each PollutionItem:
    |   Distribute amount across grid cells within radius
    |   using distance-weighted kernel
    |
    v
Grid maps updated:
  GroundPollutionSystem.m_Map (256x256 shorts)
  AirPollutionSystem.m_Map   (256x256 shorts)
  NoisePollutionSystem.m_Map  (256x256 shorts -- writes to m_PollutionTemp)
```

### Pollution Spread and Decay

```
Air Pollution (each tick):
  1. Wind advection: sample offset by wind vector
  2. 4-neighbor diffusion: each neighbor contributes ~12.5%
  3. Decay: subtract m_AirFade / 128

Ground Pollution (each tick):
  1. Decay: subtract m_GroundFade / 128
  (No spatial spread)

Noise Pollution (each tick):
  1. Swap: compute m_Pollution from m_PollutionTemp
     (center/4 + cardinals/8 + diagonals/16)
  2. Clear: reset m_PollutionTemp to 0
  (Recalculated from scratch every tick)
```

### Road/Network Pollution

```
Road Entity (with Game.Net.Pollution component)
    |
    v
NetPollutionSystem.UpdateNetPollutionJob
    |-- Read m_Accumulation (smoothed traffic pollution)
    |-- Multiply by NetPollutionData.m_Factors
    |-- Apply upgrade modifiers (sound barriers, beautification)
    |-- Skip tunnels entirely
    |-- For edges: sample multiple points along bezier curve
    |-- For nodes: average connected edge contributions
    |
    v
Air + Noise grid maps updated
```

## Harmony Patch Points

### Candidate 1: `BuildingPollutionAddSystem.GetBuildingPollution` (static method)

- **Signature**: `public static PollutionData GetBuildingPollution(Entity prefab, bool destroyed, bool abandoned, bool isPark, float efficiency, ...)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify or override any building's pollution output. A Postfix can read and alter the returned `PollutionData` before it gets enqueued.
- **Risk level**: Low -- pure calculation, no side effects
- **Note**: This is a static method with many parameters; Harmony can patch it but parameter matching must be exact.

### Candidate 2: `AirPollutionSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to skip/replace) or Postfix (to read results)
- **What it enables**: Replace the entire air pollution simulation step, or read the map after update.
- **Risk level**: Medium -- replacing core simulation behavior

### Candidate 3: Direct grid map access via `CellMapSystem<T>.GetMap()`

- **Signature**: `NativeArray<T> GetMap(bool readOnly, out JobHandle dependencies)`
- **Patch type**: N/A -- no patch needed
- **What it enables**: Read or write pollution grid values directly from a custom system
- **Risk level**: Low -- standard ECS data access pattern

## Mod Blueprint: Custom Pollution Modifier

A mod could reduce or increase pollution in specific areas by directly writing to the grid maps.

### Strategy

1. Create a `GameSystemBase` that runs after `BuildingPollutionAddSystem`
2. Get the pollution maps via `GetMap(readOnly: false, out deps)`
3. Modify cell values in a Burst job
4. Register as a writer via `AddWriter(jobHandle)`

### Example: Read Pollution at a Position

```csharp
public short GetAirPollutionAt(AirPollutionSystem airSystem, float3 worldPosition)
{
    JobHandle deps;
    NativeArray<AirPollution> map = airSystem.GetMap(readOnly: true, out deps);
    deps.Complete();
    AirPollution value = AirPollutionSystem.GetPollution(worldPosition, map);
    airSystem.AddReader(Dependency);
    return value.m_Pollution;
}
```

### Example: Reduce Ground Pollution in a Radius

```csharp
[BurstCompile]
private struct ReduceGroundPollutionJob : IJob
{
    public NativeArray<GroundPollution> m_Map;
    public float2 m_Center;
    public float m_RadiusSq;
    public float m_Reduction; // 0.0-1.0

    public void Execute()
    {
        for (int i = 0; i < m_Map.Length; i++)
        {
            float3 cellCenter = GroundPollutionSystem.GetCellCenter(i);
            float distSq = math.lengthsq(cellCenter.xz - m_Center);
            if (distSq < m_RadiusSq)
            {
                GroundPollution gp = m_Map[i];
                gp.m_Pollution = (short)(gp.m_Pollution * (1f - m_Reduction));
                m_Map[i] = gp;
            }
        }
    }
}
```

## Key Constants

| Constant | Value | Description |
|----------|-------|-------------|
| kTextureSize | 256 | Grid resolution (all three types) |
| kMapSize | 14,336 | World units covered by the grid |
| Cell size | ~56 | World units per cell (14336 / 256) |
| kUpdatesPerDay | 128 | Grid update frequency per game day |
| kSpread (air) | 3 | Right-shift for neighbor diffusion (~12.5%) |
| Max pollution | 32,767 | Maximum value per cell (short max) |
| Frames per day | 262,144 | Total simulation frames in one game day |

## Open Questions

- [x] How does air pollution spread? Wind advection + 4-neighbor diffusion + fade
- [x] How does ground pollution spread? It does not -- only fades in place
- [x] How does noise pollution work? Double-buffered, recalculated from scratch each tick
- [x] How do buildings emit pollution? `BuildingPollutionAddSystem` with distance-weighted kernel
- [x] How do roads contribute? `NetPollutionSystem` reads traffic accumulation, applies to grids
- [x] What reduces noise on roads? Sound barriers, beautification upgrades, tunnels
- [x] Does ground pollution affect groundwater? Yes, via `GroundWaterPollutionSystem`
- [ ] Exact default values of `PollutionParameterData` fields -- these are set by prefab data at runtime
- [ ] How does pollution affect citizen health specifically? The `CitizenHappinessSystem.GetAirPollutionBonuses` method was not fully decompiled
- [ ] How does `m_GroundPollutionLandValueDivisor` integrate with the land value system?

## Sources

- Decompiled from: Game.dll (Game.Simulation namespace, Game.Prefabs namespace, Game.Buildings namespace, Game.Net namespace)
- Key types: AirPollutionSystem, GroundPollutionSystem, NoisePollutionSystem, BuildingPollutionAddSystem, NetPollutionSystem, ObjectPolluteSystem, PollutionTriggerSystem, GroundWaterPollutionSystem
- All decompiled snippets saved in `snippets/` directory
