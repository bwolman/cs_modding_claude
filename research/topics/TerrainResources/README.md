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
| Game.dll | Game.Common | Damaged, Destroyed, Overridden (tree lifecycle interaction) |
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

The system queries all tree entities, filtering out `Deleted`, `Temp`, `Overridden`, and non-matching `UpdateFrame`. The `Overridden` filter (from `Game.Common`) is important — it marks entities that should not be processed by normal simulation (e.g., placeholder entities or overridden sub-objects).

The system updates 32 times per day (update interval = 512 frames). Each tick, the tree tick job follows one of three code paths:

**Path 1 — Destroyed trees**: If the tree has a `Destroyed` component (`Game.Common`), the system increments `Destroyed.m_Cleared` (float) each tick. No lifecycle transitions occur. When `m_Cleared >= 1.0`, the tree resets (`m_State = 0`, `m_Growth = 0`) and the `Destroyed` and `Damaged` components are removed.

**Path 2 — Damaged trees**: If the tree has a `Damaged` component (`Game.Common`), each tick heals damage by subtracting a random value up to ~0.031 from each axis of `Damaged.m_Damage` (float3). The tree still processes normal lifecycle transitions while healing. When all damage axes reach 0, the `Damaged` component is removed.

**Path 3 — Normal trees**: Standard lifecycle — adds a random increment to `m_Growth`. When it reaches 256, the tree transitions to the next stage.

The random range for growth increments controls average time in each stage:

| Stage | Tick Speed Constant | Random Range | Relative Duration |
|-------|-------------------|--------------|-------------------|
| Child | 1280 | `random.NextInt(1280) >> 8` | Baseline |
| Teen | 938 | `random.NextInt(938) >> 8` | ~73% of child |
| Adult | 548 | `random.NextInt(548) >> 8` | ~43% of child |
| Elderly | 548 | `random.NextInt(548) >> 8` | ~43% of child |
| Dead | 2304 | `random.NextInt(2304) >> 8` | ~180% of child |

After the Dead stage, the tree resets to Child (no flags, growth = 0) and the cycle repeats.

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

### `Damaged` (Game.Common)

Applied to trees (and other objects) that have taken damage. TreeGrowthSystem heals damaged trees over time.

| Field | Type | Description |
|-------|------|-------------|
| m_Damage | float3 | Damage amount per axis; healed ~0.031 per tick per axis until all reach 0 |

*Source: `Game.dll` -> `Game.Common.Damaged`*

### `Destroyed` (Game.Common)

Applied to trees (and other objects) that are fully destroyed. TreeGrowthSystem clears destroyed trees over time.

| Field | Type | Description |
|-------|------|-------------|
| m_Cleared | float | Clearing progress (0 to 1.0); tree resets when >= 1.0 |
| m_Event | Entity | The event that caused destruction |

*Source: `Game.dll` -> `Game.Common.Destroyed`*

### `Overridden` (Game.Common)

Tag component that marks entities excluded from normal simulation processing. TreeGrowthSystem filters out Overridden entities.

*Source: `Game.dll` -> `Game.Common.Overridden`*

### `TreeData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_WoodAmount | float | Amount of wood this tree species produces |

*Source: `Game.dll` -> `Game.Prefabs.TreeData`*

### `PlantData` (Game.Prefabs)

Prefab component for ALL vegetation (trees, bushes, plants, flowers). To query all vegetation prefabs, use `WithAll<PlantData>()`. To query only tree prefabs, use `WithAll<TreeData>()` (TreeData implies PlantData). To distinguish trees from bushes at runtime, check `SubMesh` buffer length — trees have 6 SubMesh entries (one per lifecycle stage pair), while bushes have fewer.

*Source: `Game.dll` -> `Game.Prefabs.PlantData`*

### `BatchesUpdated` (Game.Objects)

Tag component that triggers visual mesh refresh. Must be added after modifying `Tree.m_State`, `Tree.m_Growth`, or `ColorVariation` buffers to ensure the rendering system updates the visual model. Without this, visual changes are not reflected until the renderer naturally rechecks the entity.

```csharp
// Single entity update
commandBuffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, entity, default);

// Bulk update for an entire query
commandBuffer.AddComponent<BatchesUpdated>(query, EntityQueryCaptureMode.AtPlayback);
```

*Source: `Game.dll` -> `Game.Objects.BatchesUpdated`*

### Evergreen vs Deciduous Detection

To programmatically determine if a tree prefab is evergreen (no seasonal color change) vs deciduous:
1. Check `ColorVariation` buffer on each `SubMesh` entity — if buffer length < 4, the tree is evergreen
2. Check if the prefab name contains "palm" — palms are always evergreen
3. Trees with full `ColorVariation` buffers (length >= 4, one per season) are deciduous

This classification determines whether trees change foliage color with seasons.

### `Plant` (Game.Objects)

ECS component for non-tree vegetation (bushes, flowers, ground cover).

| Field | Type | Description |
|-------|------|-------------|
| m_Pollution | float | Accumulated pollution affecting this plant |

Trees and plants are separate component types — queries targeting vegetation should use `.WithAny<Tree, Plant>()` to include both. Tree_Controller's `DestroyFoliageSystem` and `ModifyTempVegetationSystem` both query for `Tree | Plant`.

*Source: `Game.dll` -> `Game.Objects.Plant`*

### `SubMesh` (Game.Prefabs) — Buffer Element

Buffer element on prefab entities that stores mesh variants. Trees have 6 SubMesh entries corresponding to their lifecycle stages: child, teen, adult, elderly, dead, stump.

| Field | Type | Description |
|-------|------|-------------|
| m_SubMesh | Entity | Reference to the mesh prefab entity |
| m_Position | float3 | Mesh position offset |
| m_Rotation | quaternion | Mesh rotation offset |
| m_Flags | SubMeshFlags | Mesh behavior flags |
| m_RandomSeed | ushort | Random seed for variation |

Tree_Controller checks `subMeshBuffer.Length > 5` to verify a tree prefab has a stump mesh (6th entry, index 5).

*Source: `Game.dll` -> `Game.Prefabs.SubMesh`*

### Tree Age Constants (`ObjectUtils`)

`Game.Objects.ObjectUtils` defines constants for the float-based tree age representation. The `Tree.m_State` enum tracks discrete lifecycle stages, but the internal age system uses a 0-1 float where each phase occupies a proportion:

| Constant | Value | Cumulative |
|----------|-------|------------|
| `TREE_AGE_PHASE_CHILD` | 0.10 | 0.00 – 0.10 |
| `TREE_AGE_PHASE_TEEN` | 0.15 | 0.10 – 0.25 |
| `TREE_AGE_PHASE_ADULT` | 0.35 | 0.25 – 0.60 |
| `TREE_AGE_PHASE_ELDERLY` | 0.35 | 0.60 – 0.95 |
| `TREE_AGE_PHASE_DEAD` | 0.05 | 0.95 – 1.00 |
| `MAX_TREE_AGE` | 40f | Maximum age in game-time units |

Tree_Controller's `TreeObjectDefinitionSystem` uses these to compute age thresholds for each `TreeState`:

```csharp
// Mapping TreeState to cumulative age thresholds:
{ TreeState.Teen,    TREE_AGE_PHASE_CHILD }                                         // 0.10
{ TreeState.Adult,   TREE_AGE_PHASE_CHILD + TREE_AGE_PHASE_TEEN }                   // 0.25
{ TreeState.Elderly, TREE_AGE_PHASE_CHILD + TREE_AGE_PHASE_TEEN + TREE_AGE_PHASE_ADULT }  // 0.60
{ TreeState.Dead,    ... + TREE_AGE_PHASE_ELDERLY + 0.00001f }                      // 0.95+
{ TreeState.Stump,   ... + TREE_AGE_PHASE_DEAD }                                    // 1.00
```

*Source: `Game.dll` -> `Game.Objects.ObjectUtils`*

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

4. **Tree growth manipulation**: Patch `TreeGrowthSystem` or directly modify `Tree` components on tree entities. The tick speed constants control growth rate -- lower values = faster growth. A more powerful approach is replacing the system's entity query via reflection to exclude trees with a custom tag component:

```csharp
// In OnGameLoadingComplete:
var modifiedQuery = SystemAPI.QueryBuilder()
    .WithAll<UpdateFrame>()
    .WithAllRW<Game.Objects.Tree>()
    .WithNone<Deleted, Temp, Overridden, NoTreeGrowth>()  // Custom exclusion
    .Build();

m_TreeGrowthSystem.SetMemberValue("m_TreeQuery", modifiedQuery);
m_TreeGrowthSystem.RequireForUpdate(modifiedQuery);
```

This replaces the vanilla query so TreeGrowthSystem skips trees with the custom `NoTreeGrowth` tag. Always add `BatchesUpdated` after modifying tree visual state.

5. **Terrain attractiveness**: Read via `TerrainAttractivenessSystem.GetData()` or use the static `EvaluateAttractiveness()` method. This feeds into land value and building desirability.

## ColorVariation Buffer (Seasonal Foliage)

`ColorVariation` is a buffer element on `SubMesh` entities that controls seasonal foliage colors for vegetation. Each element maps a season to a color set:

| Field | Type | Description |
|-------|------|-------------|
| `m_GroupID` | ColorGroupID | Season index: (0)=Spring, (1)=Summer, (2)=Autumn, (3)=Winter |
| `m_ColorSet` | ColorSet | Three color channels: `m_Channel0`, `m_Channel1`, `m_Channel2` (UnityEngine.Color) |

Trees typically have 4 or 8 `ColorVariation` entries (one per season, optionally doubled for variation). To modify foliage colors at runtime:

```csharp
// Get SubMesh buffer from tree prefab entity
DynamicBuffer<SubMesh> subMeshes = EntityManager.GetBuffer<SubMesh>(treePrefabEntity);
foreach (SubMesh subMesh in subMeshes)
{
    DynamicBuffer<ColorVariation> colors = EntityManager.GetBuffer<ColorVariation>(subMesh.m_SubMesh);
    for (int i = 0; i < colors.Length; i++)
    {
        var cv = colors[i];
        if (cv.m_GroupID == new ColorGroupID(2)) // Autumn
        {
            cv.m_ColorSet.m_Channel0 = new Color(0.8f, 0.2f, 0.1f); // Custom autumn red
            colors[i] = cv;
        }
    }
}
// Trigger visual refresh:
EntityManager.AddComponent<BatchesUpdated>(treeEntity);
```

**Evergreen detection**: Trees with `ColorVariation` buffer length ≤ 4 are typically evergreen (no seasonal color change), while length > 4 indicates deciduous with distinct seasonal palettes.

## WoodResource Buffer & Extractor Areas

`WoodResource` (Game.Areas) is a buffer element that links area extractor entities to individual tree entities in lumber industry zones:

| Field | Type | Description |
|-------|------|-------------|
| `m_Tree` | Entity | Reference to a tree entity within the extraction area |

The `Extractor` component on area entities marks resource extraction zones. When an area entity has both `Extractor` and a `WoodResource` buffer, each buffer element references a tree that contributes to wood production.

```csharp
// Tag lumber trees (trees used by wood industry)
DynamicBuffer<WoodResource> woodResources = EntityManager.GetBuffer<WoodResource>(extractorAreaEntity);
foreach (WoodResource wr in woodResources)
{
    EntityManager.AddComponent<Lumber>(wr.m_Tree);
}
```

**Key insight for tree mods**: Lumber industry trees need special handling -- they must keep growing for wood production. Mods that freeze tree growth should check for the `Lumber` tag (or check if trees are referenced by an extractor area) before applying growth freezes.

**Detecting area changes**: Query for `Updated + Extractor` to detect when area boundaries change, then refresh `WoodResource` connections.

## Mod Blueprint: Vegetation Control (Tree_Controller Pattern)

A comprehensive blueprint for mods that control vegetation behavior -- tree age, growth, species selection, seasonal appearance, and bulk operations -- based on the Tree_Controller mod architecture.

**Mod archetype**: Vegetation manipulation tool. The mod provides a custom tool for selecting and modifying trees/bushes, overrides growth and placement behavior, controls seasonal foliage appearance, and integrates with the lumber industry.

### Systems to Create

| System | Phase | Purpose |
|--------|-------|---------|
| TreeControllerTool | ToolUpdate | Custom `ToolBaseSystem` with `OverlayRenderSystem` integration for radius selection, individual picking, and map-wide operations |
| TreeControllerUISystem | UIUpdate | `ExtendedUISystemBase` with `ValueBinding`/`TriggerBinding` for tool settings and TypeScript-based UI |
| TreeObjectDefinitionSystem | Modification1 | `CreationDefinition.m_Prefab` substitution for multi-species brush placement with age control via `ObjectDefinition` |
| ModifyTreeGrowthSystem | GameSimulation | Vanilla `TreeGrowthSystem` query replacement via reflection -- adds custom exclusion component to skip controlled trees |
| DeciduousSystem | GameSimulation | Seasonal tree state management with Burst-compiled jobs for performance |
| ReloadFoliageColorDataSystem | PrefabUpdate | Runtime `ColorVariation` buffer modification on `SubMesh` entities for custom seasonal colors |
| ModifyVegetationPrefabsSystem | PrefabUpdate | Prefab entity cost/geometry modification (`PlaceableObjectData.m_ConstructionCost`, `ObjectGeometryData.m_Size`) with `PrefabBase` source-of-truth reset |
| ModifyTempVegetationSystem | Modification1 | Temp entity manipulation for tree age (`Tree.m_State`) and random seed (`PseudoRandomSeed`) control |
| LumberSystem | GameSimulation | `WoodResource` buffer traversal for lumber industry integration -- tags trees used by extractors |
| DestroyFoliageSystem | Modification1 | Bulk entity deletion with safety enable/disable pattern for radius and map-wide operations |
| FindTreesAndBushesSystem | PrefabUpdate | Prefab classification system adding custom tag components to tree vs bush prefabs |
| DetectAreaChangeSystem | GameSimulation | `Updated + Extractor` area change detection triggering dependent system re-runs |
| SafelyRemoveSystem | Serialize | Clean mod component removal and state restoration for safe mod uninstall |
| TreeControllerTooltipSystem | UIUpdate | Custom `TooltipSystemBase` for tool tooltips |

### Components to Create

| Component | Type | Purpose |
|-----------|------|---------|
| NoTreeGrowth | `IComponentData : IEmptySerializable` | Tag on trees to exclude from vanilla `TreeGrowthSystem` -- persists across save/load |
| DeciduousData | `IComponentData : IEmptySerializable` | Tag on tree prefabs classified as deciduous (seasonal color change) |
| EvergreenData | `IComponentData : IEmptySerializable` | Tag on tree prefabs classified as evergreen (no seasonal change) |
| Lumber | `IComponentData : IEmptySerializable` | Tag on trees used by lumber industry extractors |
| TreeControllerData | `IComponentData : ISerializable` | Custom tree data for age/species overrides that persists in saves |

### Harmony Patches Needed

| Patch | Target | Purpose |
|-------|--------|---------|
| WindControl Prefix | `WindControl.SetGlobalProperties` | Control wind rendering effect on vegetation |

- **Most functionality uses ECS-only approaches** -- no Harmony needed for growth control (reflection-based query replacement), prefab modification, or placement substitution
- **Cross-mod detection** uses `ToolSystem.tools.Find()` and reflection rather than Harmony patches

### Key Game Components

- `Tree` (`Game.Objects`) -- `m_State` (TreeState flags) and `m_Growth` (byte 0-255) for lifecycle control
- `Plant` (`Game.Objects`) -- companion component for non-tree vegetation (bushes, flowers)
- `TreeData` / `PlantData` (`Game.Prefabs`) -- prefab classification (`TreeData` implies tree, `PlantData` for all vegetation)
- `SubMesh` (`Game.Prefabs`) -- buffer with 6 entries for trees (child/teen/adult/elderly/dead/stump), fewer for bushes
- `ColorVariation` (`Game.Prefabs`) -- buffer on `SubMesh` entities for seasonal foliage colors (`ColorGroupID` 0-3 = Spring/Summer/Autumn/Winter)
- `ObjectGeometryData` (`Game.Prefabs`) -- `m_LegSize` for tree anarchy (trunk-only collision), `m_Size` for full canopy
- `PlaceableObjectData` (`Game.Prefabs`) -- `m_ConstructionCost` modification with `PrefabBase` source-of-truth reset
- `CreationDefinition` / `ObjectDefinition` (`Game.Tools`) -- prefab and age substitution during placement
- `PseudoRandomSeed` (`Game.Objects`) -- visual variation control for placed trees
- `BatchesUpdated` (`Game.Objects`) -- must be added after modifying `Tree.m_State` or `ColorVariation` to trigger visual refresh
- `WoodResource` (`Game.Areas`) -- buffer linking extractor areas to tree entities
- `Overridden` (`Game.Common`) -- filter for excluding entities from simulation processing

### Core Patterns

```csharp
// 1. Replace vanilla TreeGrowthSystem query via reflection
var modifiedQuery = SystemAPI.QueryBuilder()
    .WithAll<UpdateFrame>()
    .WithAllRW<Game.Objects.Tree>()
    .WithNone<Deleted, Temp, Overridden, NoTreeGrowth>()
    .Build();
m_TreeGrowthSystem.SetMemberValue("m_TreeQuery", modifiedQuery);

// 2. Prefab substitution during placement (Modification1)
var def = EntityManager.GetComponentData<CreationDefinition>(entity);
def.m_Prefab = GetRandomTreePrefab(); // Multi-species brush
EntityManager.SetComponentData(entity, def);

// 3. Tree anarchy via ObjectGeometryData
var geom = EntityManager.GetComponentData<ObjectGeometryData>(prefabEntity);
geom.m_Size = geom.m_LegSize; // Reduce conflict zone to trunk only
EntityManager.SetComponentData(prefabEntity, geom);

// 4. Seasonal color modification
DynamicBuffer<ColorVariation> colors = EntityManager.GetBuffer<ColorVariation>(subMeshEntity);
for (int i = 0; i < colors.Length; i++)
{
    var cv = colors[i];
    if (cv.m_GroupID == new ColorGroupID(2)) // Autumn
    {
        cv.m_ColorSet.m_Channel0 = customAutumnColor;
        colors[i] = cv;
    }
}
EntityManager.AddComponent<BatchesUpdated>(treeEntity);
```

### Key Considerations

- **14 systems** demonstrates a comprehensive vegetation mod -- expect significant complexity
- Use **reflection-based query replacement** as an alternative to Harmony for modifying vanilla system behavior
- `PrefabBase` (managed) serves as immutable source of truth for restoring original prefab values after ECS modifications
- Trees vs bushes distinguished by `SubMesh` buffer length (> 5 = tree, <= 5 = bush)
- Evergreen vs deciduous detected by `ColorVariation` buffer length on `SubMesh` entities (< 4 = evergreen)
- Always add `BatchesUpdated` after modifying tree visual state
- Cross-mod detection (Recolor, Line Tool, Color Painter) via `ToolSystem.tools.Find()` and reflection
- npm-based UI build pipeline with TypeScript/React components for custom tool panels

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
