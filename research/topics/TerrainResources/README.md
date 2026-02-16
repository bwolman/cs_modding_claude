# Research: Terrain & Natural Resources

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 manages terrain heightmaps, distributes natural resources (oil, ore, fertile land, fish) across the map, grows/ages trees, and computes terrain attractiveness bonuses for land value.

**Why**: To understand the foundational systems a mod would need to interact with when modifying terrain, adjusting resource distribution, influencing tree growth, or reading terrain-based attractiveness values.

**Boundaries**: This research covers terrain heightmap representation and sampling, the natural resource cell map system, tree lifecycle simulation, and terrain attractiveness computation. The water surface simulation, groundwater system, and economic resource consumption (industry extractors) are out of scope except where they directly interact with these systems.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | TerrainSystem, TerrainUtils, TerrainHeightData, NaturalResourceSystem, NaturalResourceCell, NaturalResourceAmount, TreeGrowthSystem, TerrainAttractivenessSystem, TerrainAttractiveness, GameModeNaturalResourcesAdjustSystem |
| Game.dll | Game.Objects | Tree (component), TreeState (enum) |
| Game.dll | Game.Prefabs | TreeData, TerrainPropertiesData, TerrainAreaData |
| Game.dll | Game.Tools | TerrainToolSystem |
| Game.dll | Game.Rendering | TerrainRenderSystem, VegetationRenderSystem |
| Game.dll | Game.Areas | Terrain (tag component), Extractor |

## Architecture Overview

### Terrain Heightmap

The terrain is a GPU-based heightmap managed by `TerrainSystem`. Height data is stored as `ushort` values in a `NativeArray<ushort>`, with a downscaled version for the backdrop (areas beyond the playable map). The key data structure is `TerrainHeightData`, which bundles both arrays with resolution, scale, and offset metadata.

Key constants:
- `kDownScaledHeightmapScale = 4` -- the backdrop heightmap is 4x lower resolution
- The playable area spans 14,336 world units (same as the water system)
- `heightScaleOffset` stores the vertical scale (x) and offset (y)

To sample terrain height at any world position, use `TerrainUtils.SampleHeight(ref TerrainHeightData, float3)`. This performs bilinear interpolation between the four surrounding heightmap texels. An overload also outputs the surface normal. For positions outside the playable area, it falls back to the downscaled backdrop heightmap.

### Natural Resources (Cell Map)

Natural resources are stored in a 256x256 cell map (`CellMapSystem<NaturalResourceCell>`). Each cell contains four `NaturalResourceAmount` values:

| Resource | Field | Description |
|----------|-------|-------------|
| Fertility | `m_Fertility` | Affected by ground pollution; used by farming extractors |
| Ore | `m_Ore` | Non-renewable (unless game mode refill is active) |
| Oil | `m_Oil` | Non-renewable (unless game mode refill is active) |
| Fish | `m_Fish` | Based on water depth; affected by water/noise pollution |

Each `NaturalResourceAmount` has two `ushort` fields:
- `m_Base` -- the maximum/initial amount (0-10000 scale, or up to 65535 with game mode boost)
- `m_Used` -- how much has been consumed or degraded

**Initial distribution** uses Perlin noise in `SetDefaults()`. Three different frequency scales (6.1, 13.9, 10.7) create distinct distribution patterns for fertility, ore, and oil. The noise is biased by subtracting thresholds (0.4 for fertility, 0.7 for ore/oil) and scaled to the 0-10000 range, making ore and oil rarer than fertile land.

**Regeneration** runs every 8192 frames (~32 updates/day in game simulation). The `RegenerateNaturalResourcesJob`:
- **Fertility**: Slowly regenerates (rate 25/tick toward base), but ground pollution increases `m_Used`
- **Fish**: Base amount tracks water depth (> 2 units deep). Pollution from water surface and noise pollution degrade fish. Fish regenerate at 25/tick toward a pollution floor.
- **Ore/Oil**: Do not regenerate in the base job. Only the `GameModeNaturalResourcesAdjustSystem` can refill them via `ModeSettingData.m_PercentOreRefillAmountPerDay` and `m_PercentOilRefillAmountPerDay`.

**Resource lookup** uses bilinear interpolation across the four nearest cells, via static methods like `NaturalResourceSystem.GetFertilityAmount(float3 position, NativeArray<NaturalResourceCell> map)`.

### Game Mode Resource Adjustment

`GameModeNaturalResourcesAdjustSystem` provides two features controlled by `ModeSettingData`:
1. **Initial boost**: On new game, multiplies all base resource values by `m_InitialNaturalResourceBoostMultiplier` (also boosts groundwater)
2. **Continuous refill**: Each update (128/day), reduces `m_Used` for oil and ore based on configurable percentage-per-day rates

### Tree Growth System

Trees use a lifecycle state machine managed by `TreeGrowthSystem`. Each tree entity has a `Tree` component with:
- `m_State` (TreeState flags): Child (no flags), Teen, Adult, Elderly, Dead, Stump, Collected
- `m_Growth` (byte): Progress within current stage (0-255)

The system updates 32 times per day (update interval = 512 frames). Each tick, a random increment is added to `m_Growth`. When it reaches 256, the tree transitions to the next stage. The random range controls average time in each stage:

| Stage | Tick Speed Constant | Random Range | Relative Duration |
|-------|-------------------|--------------|-------------------|
| Child | 1280 | `random.NextInt(1280) >> 8` | Baseline |
| Teen | 938 | `random.NextInt(938) >> 8` | ~73% of child |
| Adult | 548 | `random.NextInt(548) >> 8` | ~43% of child |
| Elderly | 548 | `random.NextInt(548) >> 8` | ~43% of child |
| Dead | 2304 | `random.NextInt(2304) >> 8` | ~180% of child |

After the Dead stage, the tree resets to Child (no flags, growth = 0) and the cycle repeats. Damaged trees heal over time (damage -= random up to 0.031 per tick). Destroyed trees increment a `m_Cleared` counter until it reaches 1.0, then reset.

### Terrain Attractiveness

`TerrainAttractivenessSystem` computes a 128x128 cell map of `TerrainAttractiveness` values, updated 16 times per day. Each cell stores:
- `m_ForestBonus` -- proximity to forested areas (weighted by zone ambience)
- `m_ShoreBonus` -- proximity to water bodies (depth > 2 units)

The computation is a two-pass job:
1. **Prepare**: For each cell, sample water depth, terrain height, and forest ambience
2. **Compute**: For each cell, scan all neighbors within `m_ForestDistance`/`m_ShoreDistance` radius, computing max bonus with linear distance falloff

The final attractiveness score combines three factors:
```
attractiveness = (ForestEffect * ForestBonus) + (ShoreEffect * ShoreBonus) + HeightBonus
```
where HeightBonus is a clamped linear function of terrain elevation above a threshold.

## Component Map

### `NaturalResourceCell` (Game.Simulation)

| Field | Type | Description |
|-------|------|-------------|
| m_Fertility | NaturalResourceAmount | Fertile land for farming; degrades with ground pollution |
| m_Ore | NaturalResourceAmount | Ore deposits; non-renewable by default |
| m_Oil | NaturalResourceAmount | Oil deposits; non-renewable by default |
| m_Fish | NaturalResourceAmount | Fish availability; tracks water depth, degrades with pollution |

*Source: `Game.dll` -> `Game.Simulation.NaturalResourceCell`*

### `NaturalResourceAmount` (Game.Simulation)

| Field | Type | Description |
|-------|------|-------------|
| m_Base | ushort | Maximum/initial resource amount (0-10000 normal, up to 65535 boosted) |
| m_Used | ushort | Amount consumed or degraded; available = m_Base - m_Used |

*Source: `Game.dll` -> `Game.Simulation.NaturalResourceAmount`*

### `TerrainHeightData` (Game.Simulation)

| Field | Type | Description |
|-------|------|-------------|
| heights | NativeArray&lt;ushort&gt; | Full-resolution heightmap data |
| downscaledHeights | NativeArray&lt;ushort&gt; | Backdrop heightmap (4x lower res) |
| resolution | int3 | Heightmap dimensions (x, 1, z) |
| scale | float3 | World-to-heightmap scale factors |
| offset | float3 | World-to-heightmap offset |
| hasBackdrop | bool | Whether backdrop data exists |

*Source: `Game.dll` -> `Game.Simulation.TerrainHeightData`*

### `Tree` (Game.Objects)

| Field | Type | Description |
|-------|------|-------------|
| m_State | TreeState | Lifecycle state flags (Child/Teen/Adult/Elderly/Dead/Stump/Collected) |
| m_Growth | byte | Progress within current stage (0-255, transitions at 256) |

*Source: `Game.dll` -> `Game.Objects.Tree`*

### `TreeData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_WoodAmount | float | Amount of wood this tree species produces |

*Source: `Game.dll` -> `Game.Prefabs.TreeData`*

### `TerrainAttractiveness` (Game.Simulation)

| Field | Type | Description |
|-------|------|-------------|
| m_ForestBonus | float | Proximity bonus from nearby forests (0-1) |
| m_ShoreBonus | float | Proximity bonus from nearby water (0-1) |

*Source: `Game.dll` -> `Game.Simulation.TerrainAttractiveness`*

## Key Systems

| System | Update Interval | Purpose |
|--------|----------------|---------|
| TerrainSystem | Every frame | Manages heightmap GPU textures, terrain modification, building/road clip data |
| NaturalResourceSystem | 8192 frames (~32/day) | Regenerates fertility/fish, tracks resource usage |
| TreeGrowthSystem | 512 frames (~32/day) | Advances tree lifecycle (child -> teen -> adult -> elderly -> dead -> child) |
| TerrainAttractivenessSystem | ~16384 frames (~16/day) | Computes forest/shore/height attractiveness bonuses |
| GameModeNaturalResourcesAdjustSystem | ~2048 frames (~128/day) | Optional: boosts initial resources, refills ore/oil |

## Modding Implications

1. **Reading terrain height**: Use `TerrainSystem.GetHeightData()` to obtain `TerrainHeightData`, then `TerrainUtils.SampleHeight()` for bilinear-interpolated height at any world position.

2. **Reading natural resources**: Get the cell map via `NaturalResourceSystem.GetData()`, then use the static `GetFertilityAmount()`/`GetOreAmount()`/`GetOilAmount()`/`GetFishAmount()` methods with a world position.

3. **Modifying natural resources**: Access `NaturalResourceSystem.GetData(readOnly: false, out JobHandle)` and write directly to the `CellMapData<NaturalResourceCell>.m_Buffer`. Remember to call `AddWriter()` with the job handle.

4. **Tree growth manipulation**: Patch `TreeGrowthSystem` or directly modify `Tree` components on tree entities. The tick speed constants control growth rate -- lower values = faster growth.

5. **Terrain attractiveness**: Read via `TerrainAttractivenessSystem.GetData()` or use the static `EvaluateAttractiveness()` method. This feeds into land value and building desirability.

## Decompiled Snippets

| File | Type | Lines |
|------|------|-------|
| [NaturalResourceCell.cs](snippets/NaturalResourceCell.cs) | Struct | Full |
| [NaturalResourceAmount.cs](snippets/NaturalResourceAmount.cs) | Struct | Full |
| [NaturalResourceSystem.cs](snippets/NaturalResourceSystem.cs) | System | Key sections |
| [TerrainHeightData.cs](snippets/TerrainHeightData.cs) | Struct | Full |
| [TerrainUtils.cs](snippets/TerrainUtils.cs) | Utility | Key methods |
| [TreeGrowthSystem.cs](snippets/TreeGrowthSystem.cs) | System | Full |
| [TerrainAttractivenessSystem.cs](snippets/TerrainAttractivenessSystem.cs) | System | Key sections |
| [GameModeNaturalResourcesAdjustSystem.cs](snippets/GameModeNaturalResourcesAdjustSystem.cs) | System | Full |
| [TreeComponents.cs](snippets/TreeComponents.cs) | Components | Full (Tree, TreeState, TreeData) |
