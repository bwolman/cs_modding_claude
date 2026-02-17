# Research: Map Tile Purchase System

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 handles map tile unlocking and purchasing -- what constrains available tiles, how tile costs are calculated from map features, how milestones gate tile availability, and how city boundary expansion works.

**Why**: To enable mods that auto-purchase tiles, display tile information, change pricing formulas, or unlock all tiles.

**Boundaries**: This covers the tile purchase UI flow, cost calculation, milestone gating, and the underlying 23x23 grid system. Natural resource extraction from tiles is covered separately.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | MapTilePurchaseSystem, TilePurchaseErrorFlags |
| Game.dll | Game.Areas | MapTileSystem, MapTile, MapFeature, MapFeatureElement |
| Game.dll | Game.Prefabs | MapTileData, TilePurchaseCostFactor, MapFeatureData, MilestoneData |
| Game.dll | Game.Common | Native (marks unpurchased tiles) |

## Component Map

### `MapTile` (Game.Areas)

Marker component (empty struct) on map tile area entities. All 529 (23x23) tiles have this component.

*Source: `Game.dll` -> `Game.Areas.MapTile`*

### `Native` (Game.Common)

Present on unpurchased tiles. Removed when a tile is purchased/unlocked. Owned tiles are identified by `MapTile + Exclude<Native>`.

### `MapFeature` (Game.Areas)

Enum defining resource types present on map tiles.

| Value | Name | Description |
|-------|------|-------------|
| -1 | None | No feature |
| 0 | Area | Total land area |
| 1 | BuildableLand | Buildable (non-water, non-cliff) land |
| 2 | FertileLand | Fertile agricultural land |
| 3 | Forest | Forest coverage |
| 4 | Oil | Oil deposits |
| 5 | Ore | Ore deposits |
| 6 | SurfaceWater | Surface water area |
| 7 | GroundWater | Ground water availability |
| 8 | Fish | Fish resources |

### `MapFeatureElement` (Game.Areas)

Buffer element on tile entities (capacity 9, one per MapFeature).

| Field | Type | Description |
|-------|------|-------------|
| m_Amount | float | Quantity of this feature on the tile |
| m_RenewalRate | float | Regeneration rate for the resource |

### `MapFeatureData` (Game.Prefabs)

Buffer element on the map tile prefab defining cost weights per feature.

| Field | Type | Description |
|-------|------|-------------|
| m_Cost | float | Cost weight multiplier for this feature type |

### `TilePurchaseCostFactor` (Game.Prefabs)

Prefab component providing a global cost scaling factor.

| Field | Type | Description |
|-------|------|-------------|
| m_Amount | float | Global cost multiplier applied to all tile features |

### `MilestoneData` (Game.Prefabs)

Milestone configuration that gates tile availability.

| Field | Type | Description |
|-------|------|-------------|
| m_Index | int | Milestone index |
| m_MapTiles | int | Number of additional tiles unlocked by this milestone |
| m_Reward | int | Monetary reward |
| m_DevTreePoints | int | Development tree points awarded |
| m_LoanLimit | int | New loan limit |
| m_XpRequried | int | XP required to reach this milestone |
| m_Major | bool | Whether this is a major milestone |

### `TilePurchaseErrorFlags` (Game.Simulation)

| Flag | Value | Meaning |
|------|-------|---------|
| None | 0 | No errors -- purchase allowed |
| NoCurrentlyAvailable | 1 | No tiles available now but more milestones exist |
| NoAvailable | 2 | All milestones reached, no more tiles possible |
| NoSelection | 4 | No tiles currently selected |
| InsufficientFunds | 8 | City cannot afford the selected tiles |
| InsufficientPermits | 16 | More tiles selected than available permits |

## System Map

### `MapTilePurchaseSystem` (Game.Simulation)

- **Base class**: GameSystemBase, IMapTilePurchaseSystem
- **Update phase**: Simulation (every frame when selecting)
- **Queries**: SelectionElement, owned tiles (MapTile + Exclude<Native>), locked/unlocked milestones
- **Key constants**:
  - `kAutoUnlockedTiles = 9` -- tiles available at game start
  - `kMapTileSizeModifier = 1.0 / 623.304^2` -- baseline for area/buildable land features
  - `kResourceModifier = 8.07e-7` -- baseline for resource features
  - Grid: 23x23 = 529 total tiles
  - Cell size: 623.3043 units per tile side

- **Key methods**:
  - `GetAvailableTiles()` -- returns `max(kAutoUnlockedTiles, startTiles) + sum(unlockedMilestones.m_MapTiles) - ownedTiles`
  - `UpdateStatus()` -- recalculates cost, upkeep, and error flags for current selection
  - `PurchaseSelection()` -- validates status, subtracts money, removes Native from selected tiles, tracks achievements
  - `UnlockTile(EntityManager, Entity)` -- removes `Native` component, adds `Updated`
  - `UnlockMapTiles()` -- unlocks ALL locked tiles (used by sandbox/cheat mode)
  - `GetMapTileUpkeepCostMultiplier(tileCount)` -- evaluates upkeep curve from EconomyParameterData
  - `CalculateOwnedTilesCost()` -- sums feature costs across owned tiles

- **Cost formula**:
  ```
  For each tile:
    tileValue = sum(featureAmount[i] * baselineModifier[i] * 10 * featureCost[i] * globalCostFactor)
  Tiles sorted by value descending
  totalCost = sum(tileValue[k] * (ownedTileCount + k))  // More expensive per additional tile
  ```

- **Upkeep formula**:
  ```
  totalFeatureValue = sum of all feature costs across all owned + selected tiles
  upkeep = totalFeatureValue * MapTileUpkeepCostMultiplier(totalTileCount) - currentUpkeep
  ```

### `MapTileSystem` (Game.Areas)

- **Base class**: GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
- **Update phase**: Simulation (handles deleted tiles)
- **Key logic**:
  - Manages the 23x23 grid of 529 tile entities
  - `GenerateMapTilesJob`: Creates tile entities with 4-node rectangular boundaries
  - Each tile is `623.3043` units per side, grid centered at origin
  - Maintains `m_StartTiles` list (initially unlocked tiles from map editor)
  - `PostDeserialize`: On new game, unlocks start tiles by removing `Native` component
  - `GetStartTiles()`: Returns list of initially unlocked tile entities

## Data Flow

```
TILE GRID INITIALIZATION (MapTileSystem)
  23x23 = 529 tile entities created with MapTile + Native + MapFeatureElement[9]
  Start tiles have Native removed (unlocked from map editor)
  Each tile: 623.3043 x 623.3043 units
          |
          v
MILESTONE GATING (MilestoneData)
  Each milestone.m_MapTiles adds permits
  Available = max(9, startTiles) + sum(unlocked milestones) - owned tiles
  kAutoUnlockedTiles = 9 (minimum baseline)
          |
          v
TILE SELECTION (SelectionToolSystem, SelectionType.MapTiles)
  Player selects Native tiles via selection tool
  Selection stored in SelectionElement buffer
          |
          v
COST CALCULATION (MapTilePurchaseSystem.UpdateStatus)
  For each selected tile:
    For each feature (Area, BuildableLand, FertileLand, etc.):
      cost += featureAmount * baselineModifier * 10 * featureCost * globalFactor
  Sort tiles by value descending
  totalCost = sum(tileValue[k] * (ownedCount + k))
  Upkeep = totalValue * UpkeepCurve(totalCount) - currentUpkeep
          |
          v
VALIDATION (TilePurchaseErrorFlags)
  NoCurrentlyAvailable: permits = 0 but milestones remain
  NoAvailable: permits = 0 and no milestones left
  NoSelection: nothing selected
  InsufficientFunds: cost > city money
  InsufficientPermits: selected > available permits
          |
          v
PURCHASE (MapTilePurchaseSystem.PurchaseSelection)
  Subtract cost from PlayerMoney
  For each selected tile: remove Native component, add Updated
  Track achievement progress (TheExplorer, EverythingTheLightTouches)
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Auto-unlocked tiles | kAutoUnlockedTiles = 9 | MapTilePurchaseSystem (constant) |
| Grid size | 23x23 = 529 tiles | MapTileSystem (constant) |
| Tile size | 623.3043 units per side | MapTileSystem (constant) |
| Feature costs | MapFeatureData.m_Cost | Game.Prefabs.MapFeatureData (buffer on prefab) |
| Global cost factor | TilePurchaseCostFactor.m_Amount | Game.Prefabs.TilePurchaseCostFactor |
| Upkeep curve | EconomyParameterData.m_MapTileUpkeepCostMultiplier | Game.Prefabs.EconomyParameterData |
| Milestone tiles | MilestoneData.m_MapTiles | Game.Prefabs.MilestoneData |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.MapTilePurchaseSystem.PurchaseSelection`

- **Signature**: `void PurchaseSelection()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Override purchase logic, free tiles, auto-purchase triggers
- **Risk level**: Low (managed method)
- **Side effects**: Free tiles skip economy impact

### Candidate 2: `Game.Simulation.MapTilePurchaseSystem.GetAvailableTiles`

- **Signature**: `int GetAvailableTiles()`
- **Patch type**: Postfix
- **What it enables**: Override available tile count, unlimited permits
- **Risk level**: Low
- **Side effects**: May break milestone progression UI

### Candidate 3: `Game.Simulation.MapTilePurchaseSystem.UnlockTile`

- **Signature**: `static void UnlockTile(EntityManager, Entity)`
- **Patch type**: Prefix / Postfix
- **What it enables**: Track tile unlocks, add custom components to unlocked tiles
- **Risk level**: Low (static method)
- **Side effects**: None known

## Mod Blueprint

- **Systems to create**: Auto-purchase system (buy tiles automatically at milestones), tile info overlay system, custom pricing system
- **Components to add**: Optional tile metadata (purchase timestamp, custom data)
- **Patches needed**: GetAvailableTiles (postfix for unlimited permits), PurchaseSelection (prefix for free tiles)
- **Settings**: Auto-purchase toggle, cost multiplier, permit override
- **UI changes**: Enhanced tile info panel with feature breakdown

## Examples

### Example 1: List All Map Tile Features

```csharp
public partial class TileInfoSystem : GameSystemBase
{
    private EntityQuery _tileQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _tileQuery = GetEntityQuery(
            ComponentType.ReadOnly<MapTile>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _tileQuery.ToEntityArray(Allocator.Temp);
        int owned = 0, native = 0;
        foreach (Entity entity in entities)
        {
            bool isNative = EntityManager.HasComponent<Native>(entity);
            if (isNative) native++; else owned++;

            if (!isNative && EntityManager.HasBuffer<MapFeatureElement>(entity))
            {
                var features = EntityManager.GetBuffer<MapFeatureElement>(entity);
                Log.Info($"Tile {entity}: Area={features[0].m_Amount:F0}, " +
                         $"Build={features[1].m_Amount:F0}, Fertile={features[2].m_Amount:F0}");
            }
        }
        Log.Info($"Tiles: {owned} owned, {native} native, {entities.Length} total");
        entities.Dispose();
    }
}
```

### Example 2: Unlock All Map Tiles

```csharp
public partial class UnlockAllTilesSystem : GameSystemBase
{
    private EntityQuery _lockedQuery;
    private bool _applied;

    protected override void OnCreate()
    {
        base.OnCreate();
        _lockedQuery = GetEntityQuery(
            ComponentType.ReadOnly<MapTile>(),
            ComponentType.ReadOnly<Native>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        if (_applied) return;
        _applied = true;

        var entities = _lockedQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            MapTilePurchaseSystem.UnlockTile(EntityManager, entity);
        }
        Log.Info($"Unlocked {entities.Length} map tiles");
        entities.Dispose();
    }
}
```

### Example 3: Check Available Tile Permits

```csharp
public void CheckTilePermits(World world)
{
    var purchaseSystem = world.GetExistingSystemManaged<MapTilePurchaseSystem>();
    int available = purchaseSystem.GetAvailableTiles();
    bool milestonesLeft = purchaseSystem.IsMilestonesLeft();
    var status = purchaseSystem.status;

    Log.Info($"Available permits: {available}");
    Log.Info($"Milestones remaining: {milestonesLeft}");
    Log.Info($"Status: {status}");
    Log.Info($"Current cost: {purchaseSystem.cost}");
    Log.Info($"Upkeep: {purchaseSystem.upkeep}");
}
```

### Example 4: Modify Tile Purchase Cost Factors

```csharp
public partial class CheapTilesSystem : GameSystemBase
{
    private EntityQuery _costQuery;
    private bool _applied;

    protected override void OnCreate()
    {
        base.OnCreate();
        _costQuery = GetEntityQuery(
            ComponentType.ReadWrite<TilePurchaseCostFactor>(),
            ComponentType.ReadOnly<PrefabData>()
        );
    }

    protected override void OnUpdate()
    {
        if (_applied) return;
        _applied = true;

        var entities = _costQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            TilePurchaseCostFactor factor = EntityManager.GetComponentData<TilePurchaseCostFactor>(entity);
            factor.m_Amount *= 0.5f;  // 50% cheaper tiles
            EntityManager.SetComponentData(entity, factor);
        }
        entities.Dispose();
    }
}
```

### Example 5: Understanding the Cost Formula

```csharp
// MapTilePurchaseSystem cost calculation explained:
//
// Constants:
//   kMapTileSizeModifier = 1.0 / (623.304)^2 -- normalizes land area features
//   kResourceModifier = 8.07e-7 -- normalizes resource features
//   kAutoUnlockedTiles = 9 -- minimum tiles at game start
//
// Baseline modifiers per feature:
//   Area, BuildableLand: kMapTileSizeModifier (size-based)
//   FertileLand, Forest, Oil, Ore, GroundWater: kResourceModifier (resource-based)
//   SurfaceWater: kMapTileSizeModifier (size-based)
//   Fish: kResourceModifier
//
// Per-tile cost:
//   tileValue = sum_features(amount * baselineMod * 10 * featureCost * globalFactor)
//
// Total cost (tiles sorted expensive-first):
//   totalCost = sum_k(tileValue[k] * (ownedTiles + k))
//   Each additional tile multiplies by a higher owned count
//
// Upkeep:
//   totalFeatureValue * EconomyParameterData.m_MapTileUpkeepCostMultiplier(totalCount)
//   First 9 tiles have 0 upkeep multiplier
```

## Open Questions

- [ ] How `CityConfigurationSystem.unlockMapTiles` affects the system -- appears to be a sandbox/cheat flag that disables upkeep
- [ ] The exact shape of `EconomyParameterData.m_MapTileUpkeepCostMultiplier` curve -- it's an AnimationCurve evaluated by tile count
- [ ] How the map editor sets start tiles -- appears to be serialized in the map save file
- [ ] Whether the 23x23 grid size (529 tiles) is constant or configurable via prefabs

## Sources

- Decompiled from: Game.dll -- Game.Simulation.MapTilePurchaseSystem, Game.Areas.MapTileSystem
- Runtime components: Game.Areas.MapTile, Game.Common.Native, Game.Areas.MapFeatureElement
- Prefab types: Game.Prefabs.MapTileData, Game.Prefabs.TilePurchaseCostFactor, Game.Prefabs.MapFeatureData, Game.Prefabs.MilestoneData
- Enums: Game.Simulation.TilePurchaseErrorFlags, Game.Areas.MapFeature
