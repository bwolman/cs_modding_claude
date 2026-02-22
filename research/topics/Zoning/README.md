# Research: Zoning

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-22

## Scope

**What we're investigating**: How the CS2 zoning system works end-to-end -- from road placement creating zone blocks, to cells being painted with zone types, to buildings spawning on vacant lots. Specifically: the data structures (Block, Cell, VacantLot), the systems that manage them (BlockSystem, CellCheckSystem, ZoneSpawnSystem), and how demand drives building selection.

**Why**: To build mods that modify zone behavior, add custom zone types, control building spawning, read zone state, or manipulate zone blocks programmatically.

**Boundaries**: Not covering the zone tool UI rendering, zone ambience/sound systems, or the detailed internals of demand calculation (ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem). Those are separate research topics. We cover them only where they feed into ZoneSpawnSystem.

**Key finding**: Zoning uses a block-and-cell grid system. Each road edge owns zone `Block` entities (via `SubBlock` buffer). Each block contains a flat array of `Cell` buffer elements (up to `m_Size.x * m_Size.y`). Cells track their zone type, flags (Blocked, Occupied, Visible, etc.), and height limit. `CellCheckSystem` identifies `VacantLot` entries on blocks. `ZoneSpawnSystem` evaluates vacant lots against demand, selects a matching building prefab, and creates it via `CreationDefinition`. The cell size is 8m x 8m, blocks are max 10 wide x 6 deep.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Zones | Block, Cell, CellFlags, ZoneType, ValidArea, VacantLot, SubBlock, BuildOrder, CurvePosition, ProcessEstimate, LotFlags, AreaType, ZoneUtils, BlockSystem, CellCheckSystem, CellBlockJobs, CellOccupyJobs, CellOverlapJobs, SearchSystem, UpdateCollectSystem, LotSizeJobs |
| Game.dll | Game.Simulation | ZoneSpawnSystem, ZoneAmbienceSystem, ZoneEvaluationUtils |
| Game.dll | Game.Prefabs | ZoneData, ZonePropertiesData, ZoneFlags, ZoneDensity, ZonePrefabs, ZonePrefab, ZoneBlockData, ZoneBlockPrefab, NetZoneData, ZonePreferenceData, ZoneServiceConsumptionData, ZonePollutionData |
| Game.dll | Game.Tools | ZoneToolSystem, GenerateZonesSystem, ApplyZonesSystem |
| Game.dll | Game.Buildings | ZoneCheckSystem |

## Component Map

### `Block` (Game.Zones)

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | World-space center of the block |
| m_Direction | float2 | Forward direction (perpendicular to road, pointing away from road) |
| m_Size | int2 | Width (x, along road) and depth (y, away from road) in cells |

The primary zone grid entity. Created by `BlockSystem` when a road with `EnableZoning` flag is placed. Each block is owned by a road edge entity via the `SubBlock` buffer. Blocks are positioned perpendicular to the road and extend up to 6 cells deep (48m). Width is determined by the road segment length, split into chunks of max 10 cells.

*Source: `Game.dll` -> `Game.Zones.Block`*

### `Cell` (Game.Zones)

| Field | Type | Description |
|-------|------|-------------|
| m_State | CellFlags | Bitfield flags (Blocked, Visible, Occupied, Shared, Roadside, etc.) |
| m_Zone | ZoneType | The zone type assigned to this cell (index into zone prefab array) |
| m_Height | short | Maximum building height allowed at this cell (world units) |

Buffer element on Block entities. Buffer capacity is 60 (10 width x 6 depth max). Indexed as `cellIndex.y * block.m_Size.x + cellIndex.x`. A cell is buildable when it has `CellFlags.Visible` set and is not `Blocked` or `Occupied`.

*Source: `Game.dll` -> `Game.Zones.Cell`*

### `CellFlags` (Game.Zones)

| Flag | Value | Description |
|------|-------|-------------|
| None | 0x0000 | Default state |
| Blocked | 0x0001 | Cell is obstructed (building, object, terrain) |
| Shared | 0x0002 | Cell overlaps with another block's cell |
| Roadside | 0x0004 | Cell is adjacent to the road edge |
| Visible | 0x0008 | Cell is painted with a zone type (visible to player) |
| Overridden | 0x0010 | Cell zone type was overridden by a newer block |
| Occupied | 0x0020 | A building currently occupies this cell |
| Selected | 0x0040 | Cell is selected by the zone tool |
| Redundant | 0x0080 | Cell is redundant (covered by another block) |
| Updating | 0x0100 | Cell is being updated this frame |
| RoadLeft | 0x0200 | Road exists to the left of this cell |
| RoadRight | 0x0400 | Road exists to the right of this cell |
| RoadBack | 0x0800 | Road exists behind this cell |

*Source: `Game.dll` -> `Game.Zones.CellFlags`*

### `ZoneType` (Game.Zones)

| Field | Type | Description |
|-------|------|-------------|
| m_Index | ushort | Index into the zone prefab array (0 = None, max 339) |

Simple index wrapper. Maps to a zone prefab entity via `ZonePrefabs[zoneType]`. The `ZoneSystem` manages the prefab lookup array.

*Source: `Game.dll` -> `Game.Zones.ZoneType`*

### `ValidArea` (Game.Zones)

| Field | Type | Description |
|-------|------|-------------|
| m_Area | int4 | Bounding rectangle of valid cells: (minX, maxX, minY, maxY) |

Tracks which portion of the block's cell grid is actually valid (not all cells in a block may be usable). Set by `CellCheckSystem`.

*Source: `Game.dll` -> `Game.Zones.ValidArea`*

### `VacantLot` (Game.Zones)

| Field | Type | Description |
|-------|------|-------------|
| m_Area | int4 | Lot bounds within block: (minX, maxX, minY, maxY) |
| m_Type | ZoneType | Zone type for this lot |
| m_Height | short | Maximum building height for this lot |
| m_Flags | LotFlags | Corner flags (CornerLeft = 1, CornerRight = 2) |

Buffer element on Block entities. Represents a contiguous area of visible, unoccupied cells that a building can spawn on. Created by the cell check pipeline (specifically the lot-sizing jobs). `ZoneSpawnSystem` reads these to find spawn locations.

*Source: `Game.dll` -> `Game.Zones.VacantLot`*

### `SubBlock` (Game.Zones)

| Field | Type | Description |
|-------|------|-------------|
| m_SubBlock | Entity | Reference to the Block entity |

Buffer element on road edge entities. Links a road to its owned zone blocks. Capacity of 4 (typically 2 blocks per road edge -- one on each side).

*Source: `Game.dll` -> `Game.Zones.SubBlock`*

### `ZoneData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_ZoneType | ZoneType | The zone type index |
| m_AreaType | AreaType | Category: None, Residential, Commercial, Industrial |
| m_ZoneFlags | ZoneFlags | Flags: SupportNarrow, SupportLeftCorner, SupportRightCorner, Office |
| m_MinOddHeight | ushort | Minimum height for odd-width lots |
| m_MinEvenHeight | ushort | Minimum height for even-width lots |
| m_MaxHeight | ushort | Maximum allowed building height |

Per-zone-prefab configuration. The `AreaType` determines which demand system drives spawning. `ZoneFlags.Office` marks the zone as office (industrial area type but no manufacturing).

*Source: `Game.dll` -> `Game.Prefabs.ZoneData`*

### `ZonePropertiesData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_ScaleResidentials | bool | Whether to scale residential properties by lot size |
| m_ResidentialProperties | float | Base number of residential properties |
| m_SpaceMultiplier | float | Space multiplier for commercial/industrial |
| m_AllowedSold | Resource | Allowed resources for commercial sale |
| m_AllowedManufactured | Resource | Allowed resources for industrial manufacturing |
| m_AllowedStored | Resource | Allowed resources for warehouse storage |
| m_FireHazardMultiplier | float | Fire hazard multiplier for buildings in this zone |
| m_IgnoreLandValue | bool | Whether buildings in this zone ignore land value |

*Source: `Game.dll` -> `Game.Prefabs.ZonePropertiesData`*

### `BuildingPropertyData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_ResidentialProperties | int | Number of residential units (households) this building can hold |
| m_SpaceMultiplier | float | Multiplier applied to the building's workspace/commercial space. Higher values mean more employees or commercial capacity per lot cell. |
| m_AllowedSold | Resource | Bitmask of resources this building is allowed to sell (commercial buildings) |
| m_AllowedManufactured | Resource | Bitmask of resources this building is allowed to manufacture (industrial buildings) |
| m_AllowedStored | Resource | Bitmask of resources this building is allowed to store (warehouse buildings) |

Per-building property configuration attached to spawnable building prefab entities. This component is the building-level counterpart to `ZonePropertiesData` (which is zone-level). When a building spawns, the game uses `BuildingPropertyData` from the building prefab to determine how many households it supports, what resources it can trade, and how much workspace it provides. Mods like RealisticWorkplacesAndHouseholds modify `m_ResidentialProperties` and `m_SpaceMultiplier` at runtime to adjust building capacity based on physical dimensions. The `m_SpaceMultiplier` is particularly important -- it scales the number of workplaces or commercial units, and community mods often recalculate it from the building's mesh volume.

*Source: `Game.dll` -> `Game.Prefabs.BuildingPropertyData`*

### `SpawnableBuildingData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_ZonePrefab | Entity | Reference to the zone prefab entity this building belongs to. Links the building to its zone type (e.g., Low Density Residential, High Density Commercial). |
| m_Level | byte | Current level of the building (1-5). Buildings spawn at level 1 and can upgrade to higher levels based on land value, services, and other factors. `ZoneSpawnSystem` only selects buildings with `m_Level == 1` for new construction. |

The bridge between buildings and zones. Every spawnable zoned building has this component, which tells the game which zone type it belongs to and what level it is. `ZoneSpawnSystem.EvaluateSpawnAreas` reads `m_ZonePrefab` to match buildings to vacant lots of the correct zone type, and filters for `m_Level == 1` when selecting buildings for initial construction. When a building upgrades, `m_Level` increases and the building may be replaced with a higher-level prefab. The `m_ZonePrefab` entity reference can be resolved to get `ZoneData` and `ZonePropertiesData` for the building's zone.

*Source: `Game.dll` -> `Game.Prefabs.SpawnableBuildingData`*

### Building Property Type Tags (Game.Buildings)

Three tag components classify which zone type a building belongs to at the entity level:

- **`ResidentialProperty`** — Added to buildings in residential zones
- **`CommercialProperty`** — Added to buildings in commercial zones
- **`IndustrialProperty`** — Added to buildings in industrial zones (includes office)

These are empty tag components (no fields) added at building creation time based on `SpawnableBuildingData.m_ZonePrefab` → `ZoneData.m_AreaType`. They persist for the building's lifetime and are the primary way to query buildings by zone type:

```csharp
// Standard query for all growable buildings (excluding signatures)
EntityQuery growableQuery = SystemAPI.QueryBuilder()
    .WithAll<Building>()
    .WithAny<ResidentialProperty, IndustrialProperty, CommercialProperty>()
    .WithNone<Temp, Deleted, Signature>()
    .Build();

// Query for locked growable buildings (PlopTheGrowables pattern)
EntityQuery lockedQuery = SystemAPI.QueryBuilder()
    .WithAll<Building, LevelLocked>()
    .WithAny<ResidentialProperty, IndustrialProperty, CommercialProperty>()
    .WithNone<Signature>()
    .Build();
```

Note: Signature buildings also have these components but are typically excluded with `WithNone<Signature>` since they have unique behavior.

**Efficient zone-category filtering with `EntityQuery.Any`**: `ResidentialProperty` and `CommercialProperty` (from `Game.Buildings`) are particularly useful as `WithAny` filters for efficient zone-category building selection. Because they are tag components present directly on the building entity, querying with them avoids the need to resolve the prefab chain (`PrefabRef` -> `SpawnableBuildingData.m_ZonePrefab` -> `ZoneData.m_AreaType`) for every entity. This is the recommended pattern when a mod only needs to distinguish between residential and commercial buildings:

```csharp
// Efficient: select all residential OR commercial buildings without prefab resolution
EntityQuery resCom = SystemAPI.QueryBuilder()
    .WithAll<Building, PrefabRef>()
    .WithAny<ResidentialProperty, CommercialProperty>()
    .WithNone<Temp, Deleted>()
    .Build();

// Compare to the slow alternative that requires per-entity prefab lookup:
// foreach entity -> PrefabRef -> EntityManager.GetComponentData<SpawnableBuildingData>(prefab)
//   -> ZoneData from m_ZonePrefab -> check m_AreaType
// The tag component approach skips all of this.
```

*Source: `Game.dll` -> `Game.Buildings.ResidentialProperty`, `CommercialProperty`, `IndustrialProperty`*

### `Condemned` (Game.Buildings)

Tag component added by `ZoneCheckSystem` when a building is incompatible with its current zone. Triggers a separate demolition pipeline (buildings are NOT demolished directly by ZoneCheckSystem).

*Source: `Game.dll` -> `Game.Buildings.Condemned`*

### `ExtractorProperty` (Game.Buildings)

Tag component (no fields) marking resource extractor buildings (mines, farms, forestry, oil extractors). These buildings extract natural resources and have special behavior — they are typically excluded from growable building queries alongside `Signature`:

```csharp
// PlopTheGrowables excludes extractors from plopped building tagging
.WithNone<Temp, Deleted, Signature, UnderConstruction, SpawnedBuilding, PloppedBuilding, ExtractorProperty>()
```

Any mod querying for growable/spawnable buildings should be aware of all four property type tags: `ResidentialProperty`, `CommercialProperty`, `IndustrialProperty`, and `ExtractorProperty`.

*Source: `Game.dll` -> `Game.Buildings.ExtractorProperty`*

### `UnderConstruction` (Game.Buildings)

Tracks a building that is being constructed or upgraded to a new prefab.

| Field | Type | Description |
|-------|------|-------------|
| m_NewPrefab | Entity | The new prefab entity to replace the building with |
| m_Progress | byte | Construction progress (0-255; 255 = instant completion) |

Added by `LevelupJob` when a building levels up. `BuildingConstructionSystem` processes this component and replaces the building. Setting `m_Progress = byte.MaxValue` causes instant completion (used by level-up). Setting `m_Progress = 0` shows the construction animation.

*Source: `Game.dll` -> `Game.Buildings.UnderConstruction`*

### `BuildingSpawnGroupData` (Game.Prefabs, ISharedComponentData)

Shared component that partitions building prefab entity chunks by zone type. This enables efficient building selection during spawn and level-up — `ZoneSpawnSystem` and `LevelupJob` iterate chunks and skip those whose zone type doesn't match:

| Field | Type | Description |
|-------|------|-------------|
| m_ZoneType | ZoneType | Zone type this building group belongs to |

```csharp
// Chunk-level filtering during building selection
for (int i = 0; i < spawnableBuildingChunks.Length; i++)
{
    ArchetypeChunk chunk = spawnableBuildingChunks[i];
    if (!chunk.GetSharedComponent(m_BuildingSpawnGroupType).m_ZoneType.Equals(zoneType))
        continue;  // Skip entire chunk — all entities have wrong zone type
    // ... iterate and score buildings in matching chunk
}
```

*Source: `Game.dll` -> `Game.Prefabs.BuildingSpawnGroupData`*

### `ZoneFlags` Semantic Meaning (Game.Prefabs)

The `ZoneFlags` enum on `ZoneData.m_ZoneFlags` controls zone behavior beyond simple area type classification:

| Flag | Value | Semantic Meaning |
|------|-------|-----------------|
| SupportNarrow | 1 | Zone supports **row homes / narrow buildings** (1-cell wide). This is the defining flag for row-house style zoning. Without it, 1-wide lots are skipped during building selection. Low-density residential typically does NOT have this flag (detached houses need 2+ cells wide), while medium-density residential DOES (row homes are 1-cell wide). |
| SupportLeftCorner | 2 | Allow corner buildings on the left side of a lot |
| SupportRightCorner | 4 | Allow corner buildings on the right side of a lot |
| Office | 8 | Marks the zone as **office** rather than manufacturing. The zone still has `AreaType.Industrial` but the `Office` flag causes `ZoneSpawnSystem` to use office demand instead of industrial demand, and `TaxSystem` classifies companies here as `TaxOffice` instead of `TaxIndustrial`. |

**Density tiers** are controlled by the combination of `ZoneData.m_MaxHeight` and `ZoneDensity`:

- **Low density** (`ZoneDensity.Low`): Small `m_MaxHeight` values (e.g., 18-24). Produces detached houses, small shops, small factories. Does NOT have `SupportNarrow`.
- **Medium density** (`ZoneDensity.Medium`): Mid-range `m_MaxHeight`. Produces row homes, mid-rise buildings. HAS `SupportNarrow` for residential (row homes are the defining building type).
- **High density** (`ZoneDensity.High`): Large `m_MaxHeight` values (e.g., 60+). Produces apartment towers, office buildings, large commercial. May or may not have `SupportNarrow`.

The `m_MaxHeight` value in `ZoneData` is written to `Cell.m_Height` when the zone is painted, and `ZoneSpawnSystem` uses it to filter building prefabs by height. Buildings whose mesh height exceeds the cell's `m_Height` are not eligible for spawning on that lot.

### Programmatic Zone Classification

To classify a zone type (low/medium/high density) at runtime using `ZonePropertiesData`:

```csharp
ZonePropertiesData info = ...; // from zone prefab
if (info.m_ResidentialProperties <= 0f)
{
    // Non-residential zone (commercial, industrial, office)
}
else
{
    float ratio = info.m_ResidentialProperties / info.m_SpaceMultiplier;
    if (!info.m_ScaleResidentials)
        // Low density (detached houses)
    else if (ratio < 1f)
        // Check spawnable building lot widths: all <= 2 = row housing, otherwise medium density
    else
        // High density
}
```

### Road-to-Zone Integration (ZoneBlockPrefab)

Road prefabs enable zoning via their `m_ZoneBlock` field on `RoadPrefab`. Setting this to a `ZoneBlockPrefab` entity causes `BlockSystem` to generate zone blocks when the road is placed. Setting it to null disables zoning for that road type.

```csharp
// Find the zone block prefab
var query = SystemAPI.QueryBuilder().WithAll<ZoneBlockData>().Build();
foreach (var entity in query.ToEntityArray(Allocator.Temp))
{
    if (prefabSystem.TryGetSpecificPrefab<ZoneBlockPrefab>(entity, out var prefab)
        && prefab.name == "Zone Block")
    {
        zoneBlockPrefab = prefab;
        break;
    }
}

// Enable/disable zoning on a road prefab
roadPrefab.m_ZoneBlock = enableZoning ? zoneBlockPrefab : null;
```

## Building Mesh Dimensions Pattern

When mods need to calculate a building's physical size (e.g., to derive realistic workplace counts or household capacity from volume), they use the `SubMesh` / `MeshData` / `ObjectUtils.GetSize()` pattern:

### How It Works

1. **Get SubMesh references**: Each building prefab entity has a `SubMesh` buffer containing references to its mesh sub-objects.
2. **Read MeshData**: Each `SubMesh` entry references a mesh entity that has a `MeshData` component containing the bounding box vertices.
3. **Calculate size**: `Game.Objects.ObjectUtils.GetSize(float3 size, Quaternion rotation)` returns the axis-aligned bounding box dimensions after applying rotation.

### Key Types

- **`SubMesh`** (`Game.Prefabs`): Buffer element on building prefabs. Contains `m_SubMesh` (Entity reference to the mesh prefab).
- **`MeshData`** (`Game.Prefabs`): Component on mesh entities. Contains `m_Vertices` (Bounds3 -- min/max corners of the mesh bounding box).
- **`ObjectUtils.GetSize()`** (`Game.Objects`): Static method that computes world-space dimensions from mesh bounds and rotation.

### Mod Usage Pattern

Community mods (e.g., RealisticWorkplacesAndHouseholds) use this pattern to calculate building volume and derive realistic property counts:

```csharp
// Get the building prefab's mesh dimensions
Entity buildingPrefab = ...; // from PrefabRef on a building entity
DynamicBuffer<SubMesh> subMeshes = EntityManager.GetBuffer<SubMesh>(buildingPrefab);

if (subMeshes.Length > 0)
{
    Entity meshEntity = subMeshes[0].m_SubMesh;
    MeshData meshData = EntityManager.GetComponentData<MeshData>(meshEntity);

    // meshData.m_Vertices.min / .max give the local-space bounding box
    float3 size = meshData.m_Vertices.max - meshData.m_Vertices.min;

    // For world-space dimensions with rotation:
    // float3 worldSize = ObjectUtils.GetSize(size, buildingRotation);

    float volume = size.x * size.y * size.z;
    float floorArea = size.x * size.z;
    float height = size.y;

    // Use dimensions to calculate realistic workplace/household counts
    int floors = (int)(height / 3.0f); // ~3m per floor
    float usableArea = floorArea * floors * 0.7f; // 70% usable
    int workplaces = (int)(usableArea / 20.0f); // ~20 sq m per worker
}
```

This pattern is essential for any mod that wants to make building capacity proportional to physical building size rather than relying on the game's default per-prefab values.

## System Map

### `BlockSystem` (Game.Zones)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (reacts to road edge changes)
- **Queries**:
  - Road edges with `Updated` or `Deleted` that have `SubBlock` buffers
- **Reads**: Edge, Curve, Composition, Road, BuildOrder, RoadComposition, NetCompositionData, StartNodeGeometry, EndNodeGeometry, ZoneBlockData
- **Writes**: Creates/destroys Block entities, updates SubBlock buffers
- **Key behavior**:
  - Only creates blocks for roads with `RoadFlags.EnableZoning` on their composition
  - Skips elevated and tunnel roads (checks `CompositionFlags.General.Elevated | Tunnel`)
  - Skips sides with `Raised` or `Lowered` flags
  - Creates blocks on each valid side of the road, using the road curve geometry
  - Block width is calculated from `ZoneUtils.GetCellWidth(roadWidth)` -- road width / 8m
  - Block depth is fixed at 6 cells (48m from road)
  - Large road segments are split into multiple blocks (max ~10 cells wide per block)
  - Also creates blocks at roundabout intersections
  - When a road is deleted, all its SubBlock entities get `Deleted` component
  - Reuses existing blocks when possible (checks `oldBlockBuffer` by Block equality)

### `CellCheckSystem` (Game.Zones)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (runs when zone blocks, objects, nets, or areas update)
- **Dependencies**: UpdateCollectSystem (zones, objects, nets, areas), SearchSystem (zones, objects, nets, areas), ZoneSystem
- **Key behavior**:
  - Collects all updated blocks into a sorted list
  - Finds overlapping blocks using the zone search tree
  - Groups overlapping blocks together
  - Runs cell-level checks: marks cells as Blocked (obstructed by objects/nets/terrain), Shared (overlapping with other blocks), Visible (painted with zone type), Occupied (building present)
  - Updates `ValidArea` component on each block
  - Populates `VacantLot` buffer on blocks with available spawn areas
  - Uses `CellBlockJobs` for cell-to-block mapping, `CellOccupyJobs` for occupancy checks, `CellOverlapJobs` for overlap detection, `LotSizeJobs` for vacant lot calculation

### `ZoneSpawnSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames
- **Queries**:
  - Lot query: `[Block, Owner, CurvePosition, VacantLot]`, excluding `[Temp, Deleted]`
  - Building query: `[BuildingData, SpawnableBuildingData, BuildingSpawnGroupData, PrefabData]`
- **Dependencies**: ZoneSystem, ResidentialDemandSystem, CommercialDemandSystem, IndustrialDemandSystem, GroundPollutionSystem, AirPollutionSystem, NoisePollutionSystem, TerrainSystem, SearchSystem, ResourceSystem
- **Key behavior**:
  1. **EvaluateSpawnAreas** job: Iterates all blocks with VacantLot buffers. For each vacant lot:
     - Looks up ZoneData to determine AreaType (Residential/Commercial/Industrial)
     - Checks if demand exists for that area type (from demand systems)
     - Calls `SelectBuilding()` to find the best matching building prefab
     - Building selection considers: lot size fit, demand level, land value, pollution, resource availability, zone density
     - Buildings must be level 1 (`SpawnableBuildingData.m_Level == 1`)
     - Buildings are matched by `BuildingSpawnGroupData.m_ZoneType`
     - Corner building support checked via `ZoneFlags.SupportLeftCorner/RightCorner`
     - Priority scoring: lot coverage ratio * demand * zone evaluation score
  2. **SpawnBuildingJob**: For the best residential, commercial, and industrial spawn locations:
     - Creates a `CreationDefinition` entity (using the building prefab)
     - Sets position from block geometry via `ZoneUtils.GetPosition()`
     - Sets rotation from block direction via `ZoneUtils.GetRotation()`
     - Creates sub-areas and sub-nets from the building prefab data
     - The `EndFrameBarrier` processes the creation next frame
  3. Spawns up to 3 buildings per update (one per area type)

### `ZoneCheckSystem` (Game.Buildings)

- **Base class**: GameSystemBase
- **Key behavior**: Checks existing spawnable buildings against their zone blocks. Uses a three-job pipeline:
  1. **FindSpawnableBuildingsJob**: Uses `SearchSystem.GetStaticSearchTree` (NativeQuadTree) to find buildings within updated zone bounds. Filters for buildings with `SpawnableBuildingData` but without `SignatureBuildingData`.
  2. **CollectEntitiesJob**: Deduplicates and sorts the building entity list.
  3. **CheckBuildingZonesJob**: For each building, validates zone compatibility:
     - **ValidateAttachedParent**: Checks if the building has an `Attached` parent with `PlaceholderBuildingData` matching the building's zone prefab. Returns true if attached parent's zone matches.
     - **ValidateZoneBlocks**: Rotates the building's lot grid (8m cell size) to world space, iterates the zone SearchTree, and verifies that every cell has a matching ZoneType with `CellFlags.Visible` set. Also requires at least one `CellFlags.Roadside` cell.
     - If **valid**: removes `Condemned` component and its notification icon
     - If **invalid**: adds `Condemned` component and `BuildingConfigurationData.m_CondemnedNotification` at `IconPriority.FatalProblem` (unless already Destroyed or Abandoned)
- **Key detail**: Buildings are NOT demolished directly by ZoneCheckSystem. They are marked with the `Condemned` component, which triggers a separate demolition pipeline.

## Data Flow

```
[Road Placed / Modified]
    |
    v
BlockSystem.UpdateBlocksJob
    |-- Checks road has EnableZoning flag
    |-- Checks not elevated/tunnel
    |-- Calculates block dimensions from road curve
    |-- Width = ceil(roadWidth / 8m), Depth = 6 cells
    |-- Creates Block entities with Cell buffers (up to 10x6 = 60 cells)
    |-- Links blocks to road via SubBlock buffer
    |
    v
[Zone Tool Paints Cells]
    |-- ZoneToolSystem.SetZoneTypeJob
    |-- Sets Cell.m_Zone and adds CellFlags.Visible
    |-- GenerateZonesSystem creates temp zone definitions
    |-- ApplyZonesSystem applies changes
    |
    v
CellCheckSystem
    |-- CellBlockJobs.BlockCellsJob: checks cell obstructions
    |   (nets, objects, terrain blocking cells)
    |-- CellOccupyJobs.ZoneAndOccupyCellsJob: marks Occupied cells
    |-- CellOverlapJobs.CheckBlockOverlapJob: handles shared cells
    |-- LotSizeJobs.UpdateLotSizeJob: finds contiguous vacant areas
    |-- Updates ValidArea, populates VacantLot buffer
    |
    v
[VacantLot Buffer on Block Entities]
    |  m_Area = lot bounds within block
    |  m_Type = zone type
    |  m_Height = max building height
    |  m_Flags = corner flags
    |
    v
ZoneSpawnSystem (every 16 frames)
    |
    |-- EvaluateSpawnAreas job:
    |   For each VacantLot:
    |     1. Check demand (residential/commercial/industrial)
    |     2. Look up ZoneData.m_AreaType
    |     3. SelectBuilding():
    |        - Match by BuildingSpawnGroupData.m_ZoneType
    |        - Filter: level==1, lotSize fits, height fits
    |        - Score: coverage * demand * evaluation
    |     4. Best location per area type queued
    |
    |-- SpawnBuildingJob:
    |   For best location per area type:
    |     1. Calculate world position from Block + lot area
    |     2. Create CreationDefinition entity
    |     3. Create sub-areas and sub-nets
    |     4. EndFrameBarrier processes creation
    |
    v
[Building Entity Created]
    |  Occupies cells (CellFlags.Occupied)
    |  Linked to block via zone check system
```

## Prefab & Configuration

| Value | Default | Source | Location |
|-------|---------|--------|----------|
| Cell size | 8m x 8m | Hardcoded | ZoneUtils.CELL_SIZE |
| Cell area | 64 sq m | Hardcoded | ZoneUtils.CELL_AREA |
| Max zone width | 10 cells | Hardcoded | ZoneUtils.MAX_ZONE_WIDTH |
| Max zone depth | 6 cells | Hardcoded | ZoneUtils.MAX_ZONE_DEPTH |
| Max zone types | 339 | Hardcoded | ZoneUtils.MAX_ZONE_TYPES |
| Spawn interval | 16 frames | Hardcoded | ZoneSpawnSystem.GetUpdateInterval() |
| Spawn offset | 13 frames | Hardcoded | ZoneSpawnSystem.GetUpdateOffset() |
| Area types | None/Residential/Commercial/Industrial | Enum | Game.Zones.AreaType |
| Zone densities | Low/Medium/High | Enum | Game.Prefabs.ZoneDensity |

### Key Constants (Hardcoded)

| Constant | Value | Where Used |
|----------|-------|------------|
| CELL_SIZE | 8.0 | ZoneUtils, all zone calculations |
| MAX_ZONE_WIDTH | 10 | Block width limit |
| MAX_ZONE_DEPTH | 6 | Block depth limit (48m from road) |
| MAX_ZONE_TYPES | 339 | Zone type index limit |
| Block depth (y) | 6 | BlockSystem.CreateBlocks |
| Minimum roundabout block perimeter | 8.0 | BlockSystem (skip if perimeter < 8) |
| Cell buffer capacity | 60 | Cell InternalBufferCapacity |
| SubBlock buffer capacity | 4 | SubBlock InternalBufferCapacity |
| VacantLot buffer capacity | 1 | VacantLot InternalBufferCapacity |
| Spawn update interval | 16 frames | ZoneSpawnSystem |
| Spawns per update | 3 max | ZoneSpawnSystem (1 per area type) |
| Building level for spawn | 1 | ZoneSpawnSystem.SelectBuilding |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.ZoneSpawnSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to block spawning) or Postfix (to modify spawn locations)
- **What it enables**: Completely control building spawning -- prevent all spawns, force specific buildings, modify demand inputs, or redirect spawn locations.
- **Risk level**: Low -- only affects new building spawns, not existing buildings
- **Side effects**: Blocking would prevent all zoned building construction

### Candidate 2: `Game.Zones.BlockSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept zone block creation/deletion when roads change. Could modify block sizes, prevent block creation for specific roads, or create custom blocks.
- **Risk level**: Medium -- affects the fundamental zone grid structure
- **Side effects**: Incorrect block data could break cell checking and building spawning

### Candidate 3: `Game.Zones.CellCheckSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify cell validity checks, override blocked/occupied status, force vacant lot creation.
- **Risk level**: Medium -- cell state drives the entire spawn pipeline
- **Side effects**: Incorrect cell state could cause buildings to overlap or spawn in invalid locations

### Candidate 4: `Game.Buildings.ZoneCheckSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Control whether existing buildings are flagged as incompatible with their zone. Could prevent demolition when zone type changes, or force buildings to be treated as compatible.
- **Risk level**: Low -- only affects zone compatibility checks on existing buildings
- **Side effects**: May cause visual mismatches between building type and painted zone

### Alternative: ECS System Approach (No Harmony)

A custom `GameSystemBase` can:
1. Query for `Block` entities and read their `Cell` buffers to inspect the zone grid
2. Query for `VacantLot` buffers to find available spawn locations
3. Modify `Cell` data (zone type, flags) to programmatically change zoning
4. Create `CreationDefinition` entities to spawn buildings directly
5. Read `ZoneData` / `ZonePropertiesData` to understand zone configurations
6. Run after `CellCheckSystem` to modify vacant lot data before `ZoneSpawnSystem` reads it

This is safer than Harmony patching and compatible with Burst-compiled jobs.

## Mod Blueprint

### To read zone state at a position:

```csharp
// In a custom GameSystemBase:
private EntityQuery m_BlockQuery;

protected override void OnCreate()
{
    base.OnCreate();
    m_BlockQuery = GetEntityQuery(
        ComponentType.ReadOnly<Block>(),
        ComponentType.ReadOnly<ValidArea>(),
        ComponentType.ReadOnly<Cell>()
    );
}

public ZoneType GetZoneTypeAt(float3 worldPosition)
{
    // Use the zone SearchSystem tree for efficient lookup,
    // or iterate blocks and check bounds
    var blocks = m_BlockQuery.ToComponentDataArray<Block>(Allocator.Temp);
    var entities = m_BlockQuery.ToEntityArray(Allocator.Temp);

    for (int i = 0; i < blocks.Length; i++)
    {
        Block block = blocks[i];
        int2 cellIndex = ZoneUtils.GetCellIndex(block, worldPosition.xz);
        if (math.all(cellIndex >= 0) && math.all(cellIndex < block.m_Size))
        {
            var cells = EntityManager.GetBuffer<Cell>(entities[i]);
            int index = cellIndex.y * block.m_Size.x + cellIndex.x;
            if (index < cells.Length)
            {
                Cell cell = cells[index];
                if ((cell.m_State & CellFlags.Visible) != 0)
                {
                    blocks.Dispose();
                    entities.Dispose();
                    return cell.m_Zone;
                }
            }
        }
    }

    blocks.Dispose();
    entities.Dispose();
    return ZoneType.None;
}
```

### To query all vacant lots:

```csharp
private EntityQuery m_VacantLotQuery;

protected override void OnCreate()
{
    base.OnCreate();
    m_VacantLotQuery = GetEntityQuery(
        ComponentType.ReadOnly<Block>(),
        ComponentType.ReadOnly<VacantLot>(),
        ComponentType.Exclude<Deleted>(),
        ComponentType.Exclude<Temp>()
    );
}

protected override void OnUpdate()
{
    var entities = m_VacantLotQuery.ToEntityArray(Allocator.Temp);
    for (int i = 0; i < entities.Length; i++)
    {
        Block block = EntityManager.GetComponentData<Block>(entities[i]);
        var lots = EntityManager.GetBuffer<VacantLot>(entities[i]);
        for (int j = 0; j < lots.Length; j++)
        {
            VacantLot lot = lots[j];
            float3 position = ZoneUtils.GetPosition(block, lot.m_Area.xz, lot.m_Area.yw);
            int2 lotSize = lot.m_Area.yw - lot.m_Area.xz;
            Log.Info($"Vacant lot at {position}, size {lotSize}, zone type index {lot.m_Type.m_Index}");
        }
    }
    entities.Dispose();
}
```

### Systems to create:
- **ZoneMonitorSystem** -- Custom system to track zone state changes and vacant lot availability
- **CustomSpawnSystem** -- System running before ZoneSpawnSystem to intercept or modify spawn locations
- **ZoneModifierSystem** -- System to programmatically modify cell zone types or flags

### Components to add:
- **SpawnBlocked** -- Marker component on Block entities to prevent spawning on specific blocks
- **CustomZoneData** -- Additional per-zone configuration for mod-specific behavior

### Patches needed:
- For basic zone reading: **none** (ECS queries are sufficient)
- For custom building spawn logic: Prefix on `ZoneSpawnSystem.OnUpdate()` to intercept spawn evaluation
- For custom zone block geometry: Prefix on `BlockSystem.OnUpdate()` to modify block creation

### Settings:
- Spawn rate multiplier (default: 1.0)
- Max zone depth override (default: 6)
- Custom zone type registrations
- Building filter preferences per zone type

## Mod Blueprint: Ploppable Growable Buildings (PlopTheGrowables Pattern)

A blueprint for mods that allow placing growable/zoned buildings anywhere without requiring correct zoning, with level-locking and abandonment prevention -- based on the PlopTheGrowables mod architecture.

**Mod archetype**: Zoned building behavior modifier. The mod distinguishes player-placed buildings from game-spawned buildings using persistent tag components, replaces the zone compatibility check system to skip plopped buildings, and provides level-locking via transpiler-based partial system modification.

### Systems to Create

| System | Phase | Purpose |
|--------|-------|---------|
| PloppedBuildingSystem | ModificationEnd | Tags newly plopped buildings with `PloppedBuilding` component. Query: `Building + (Residential\|Industrial\|Commercial) - Temp - Deleted - Signature - UnderConstruction - SpawnedBuilding - PloppedBuilding - ExtractorProperty` |
| SpawnedBuildingSystem | GameSimulation (after BuildingConstructionSystem) | Tags naturally spawned buildings (those transitioning from `UnderConstruction`) with `SpawnedBuilding` component |
| ExistingBuildingSystem | Deserialize | On save load, classifies untagged buildings as spawned (safe default). Also provides bulk operations: LockAll, UnlockAll, RemoveAbandonment |
| SelectiveZoneCheckSystem | ModificationEnd (after PloppedBuildingSystem) | Reimplementation of vanilla `ZoneCheckSystem` that skips plopped buildings. Uses `FindSpawnableBuildingsJob` + `CollectEntitiesJob` + `CheckBuildingZonesJob` pipeline |
| HistoricalLevellingSystem | GameSimulation (after BuildingUpkeepSystem) | Replacement level-up/level-down jobs that check `LevelLocked` component. Uses reflected `NativeQueue` fields from `BuildingUpkeepSystem` |
| PlopTheGrowablesUISystem | UIUpdate | Handles lock-level toggle UI binding for building info panel |

### Components to Create

| Component | Type | Purpose |
|-----------|------|---------|
| PloppedBuilding | `IComponentData : IEmptySerializable` | Persistent tag on player-placed growable buildings -- survives save/load |
| SpawnedBuilding | `IComponentData : IEmptySerializable` | Persistent tag on naturally game-spawned buildings |
| LevelLocked | `IComponentData : IEmptySerializable` | Persistent tag preventing building from leveling up or down |

### Harmony Patches Needed

| Patch | Target | Type | Purpose |
|-------|--------|------|---------|
| BuildingUpkeepSystemPatches | `BuildingUpkeepSystem.OnUpdate` | Transpiler | Removes `LevelupJob` and `LeveldownJob` scheduling from `OnUpdate` while keeping all other jobs intact (upkeep, condition, etc.) |

### System Disabling

- `ZoneCheckSystem.Enabled = false` -- replaced entirely by `SelectiveZoneCheckSystem`

### Key Game Components

- `Building` (`Game.Buildings`) -- base component on all building entities
- `ResidentialProperty` / `CommercialProperty` / `IndustrialProperty` (`Game.Buildings`) -- zone type tags for efficient building queries
- `ExtractorProperty` (`Game.Buildings`) -- tag for resource extractor buildings (excluded from plopping logic)
- `Signature` (`Game.Buildings`) -- tag for signature/unique buildings (excluded from growable queries)
- `UnderConstruction` (`Game.Buildings`) -- `m_NewPrefab` and `m_Progress` for construction tracking; presence indicates game-spawned building
- `Condemned` (`Game.Buildings`) -- tag added by `ZoneCheckSystem` when zone mismatch detected
- `SpawnableBuildingData` (`Game.Prefabs`) -- `m_ZonePrefab` and `m_Level` for building-zone linkage
- `BuildingConfigurationData` (`Game.Prefabs`) -- singleton providing `m_CondemnedNotification` entity for notification icons
- `ZoneCheckSystem` (`Game.Buildings`) -- vanilla system disabled and replaced
- `BuildingUpkeepSystem` (`Game.Simulation`) -- partially modified via transpiler to remove leveling jobs

### Core Patterns

```csharp
// 1. Distinguish plopped vs spawned buildings
// PloppedBuildingSystem (ModificationEnd): tag new plopped buildings
EntityQuery newBuildings = SystemAPI.QueryBuilder()
    .WithAll<Building>()
    .WithAny<ResidentialProperty, IndustrialProperty, CommercialProperty>()
    .WithNone<Temp, Deleted, Signature, UnderConstruction,
              SpawnedBuilding, PloppedBuilding, ExtractorProperty>()
    .Build();
// All entities in this query are plopped (placed without UnderConstruction)
EntityManager.AddComponent<PloppedBuilding>(newBuildings);

// 2. Disable vanilla ZoneCheckSystem, replace with selective version
ZoneCheckSystem vanillaSystem = World.GetExistingSystemManaged<ZoneCheckSystem>();
vanillaSystem.Enabled = false;

// 3. Level-lock via transpiler: remove LevelupJob/LeveldownJob from
// BuildingUpkeepSystem.OnUpdate, then run custom HistoricalLevellingSystem
// that checks for LevelLocked before allowing level changes

// 4. Reflect private NativeQueues from BuildingUpkeepSystem
var levelupQueue = typeof(BuildingUpkeepSystem)
    .GetField("m_LevelupQueue", BindingFlags.NonPublic | BindingFlags.Instance)
    .GetValue(buildingUpkeepSystem) as NativeQueue<Entity>;

// 5. UI: extend building info panel
// TypeScript: selectedInfo.middleSections$ subscription
// Uses VanillaComponentResolver for InfoSection/InfoRow/ToolButton
// Lock/unlock toggle with coui://uil/Standard/LockClosed.svg icons
```

### Key Considerations

- **6 systems** for a focused building behavior mod -- lean architecture compared to network/vegetation mods
- Use `IEmptySerializable` for all three tag components -- they carry no data, just need save persistence
- **System replacement** (`Enabled = false`) for `ZoneCheckSystem` is cleaner than patching -- the replacement system reimplements the full three-job pipeline but skips plopped buildings
- **Transpiler** for `BuildingUpkeepSystem` is necessary because the system runs many jobs (upkeep, condition, abandonment) -- only the leveling jobs should be removed, not the entire `OnUpdate`
- **Reflecting private `NativeQueue` fields** from vanilla systems enables reading their output without modifying the system
- On save load (Deserialize phase), classify untagged buildings as spawned -- this is the safe default to avoid incorrectly treating existing buildings as plopped
- Cross-mod compatibility: detect "RWH" (Realistic Workplaces and Households) via `GameManager.instance.modManager` iteration
- Known conflict with UrbanInequality (both replace `BuildingUpkeepSystem` leveling logic)

## Examples

### Example 1: Query Zone Blocks Along a Road

Finds all zone blocks owned by a specific road entity and logs their cell states.

```csharp
using Game.Common;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

public partial class ZoneBlockInspector : GameSystemBase
{
    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
    }

    [Preserve]
    protected override void OnUpdate() { }

    public void InspectRoadZones(Entity roadEntity)
    {
        if (!EntityManager.HasBuffer<SubBlock>(roadEntity))
        {
            Log.Info("Entity has no zone blocks (not a road or zoning disabled)");
            return;
        }

        var subBlocks = EntityManager.GetBuffer<SubBlock>(roadEntity);
        Log.Info($"Road has {subBlocks.Length} zone blocks");

        for (int i = 0; i < subBlocks.Length; i++)
        {
            Entity blockEntity = subBlocks[i].m_SubBlock;
            if (!EntityManager.Exists(blockEntity))
                continue;

            Block block = EntityManager.GetComponentData<Block>(blockEntity);
            ValidArea validArea = EntityManager.GetComponentData<ValidArea>(blockEntity);
            var cells = EntityManager.GetBuffer<Cell>(blockEntity);

            int visible = 0, occupied = 0, blocked = 0;
            for (int j = 0; j < cells.Length; j++)
            {
                if ((cells[j].m_State & CellFlags.Visible) != 0) visible++;
                if ((cells[j].m_State & CellFlags.Occupied) != 0) occupied++;
                if ((cells[j].m_State & CellFlags.Blocked) != 0) blocked++;
            }

            Log.Info($"Block {i}: size={block.m_Size}, cells={cells.Length}, " +
                     $"visible={visible}, occupied={occupied}, blocked={blocked}");
        }
    }
}
```

### Example 2: Count Vacant Lots by Zone Type

Iterates all zone blocks with vacant lots and tallies them by area type.

```csharp
using Game.Prefabs;
using Game.Zones;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

public partial class VacantLotCounter : GameSystemBase
{
    private EntityQuery m_LotQuery;
    private ZoneSystem m_ZoneSystem;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_ZoneSystem = World.GetOrCreateSystemManaged<ZoneSystem>();
        m_LotQuery = GetEntityQuery(
            ComponentType.ReadOnly<Block>(),
            ComponentType.ReadOnly<VacantLot>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    [Preserve]
    protected override void OnUpdate()
    {
        ZonePrefabs zonePrefabs = m_ZoneSystem.GetPrefabs();
        var entities = m_LotQuery.ToEntityArray(Allocator.Temp);

        int residential = 0, commercial = 0, industrial = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            var lots = EntityManager.GetBuffer<VacantLot>(entities[i]);
            for (int j = 0; j < lots.Length; j++)
            {
                Entity zonePrefab = zonePrefabs[lots[j].m_Type];
                if (EntityManager.HasComponent<ZoneData>(zonePrefab))
                {
                    ZoneData zoneData = EntityManager.GetComponentData<ZoneData>(zonePrefab);
                    switch (zoneData.m_AreaType)
                    {
                        case AreaType.Residential: residential++; break;
                        case AreaType.Commercial: commercial++; break;
                        case AreaType.Industrial: industrial++; break;
                    }
                }
            }
        }

        entities.Dispose();
        Log.Info($"Vacant lots: Residential={residential}, Commercial={commercial}, Industrial={industrial}");
    }
}
```

### Example 3: Get World Position of a Cell

Shows how to convert block-relative cell coordinates to world position.

```csharp
using Game.Zones;
using Unity.Mathematics;

public static class ZoneCellHelper
{
    /// <summary>
    /// Gets the world-space center position of a specific cell within a block.
    /// Cell (0,0) is at the front-right corner of the block (closest to road, rightmost).
    /// X axis runs along the road, Y axis runs away from the road.
    /// </summary>
    public static float3 GetCellWorldPosition(Block block, int cellX, int cellY)
    {
        // ZoneUtils.GetCellPosition calculates:
        //   offset = (block.m_Size - cellIndex*2 - 1) * 4
        //   position = block.m_Position + direction * offset.y + right * offset.x
        return ZoneUtils.GetCellPosition(block, new int2(cellX, cellY));
    }

    /// <summary>
    /// Gets the cell index within a block for a given world position.
    /// Returns indices that may be out of bounds -- caller must validate.
    /// </summary>
    public static int2 GetCellFromWorldPosition(Block block, float3 worldPosition)
    {
        return ZoneUtils.GetCellIndex(block, worldPosition.xz);
    }

    /// <summary>
    /// Checks if a cell index is valid for a given block.
    /// </summary>
    public static bool IsValidCell(Block block, int2 cellIndex)
    {
        return math.all(cellIndex >= 0) && math.all(cellIndex < block.m_Size);
    }
}
```

### Example 4: How Building Selection Works in ZoneSpawnSystem

This illustrates the building selection logic used by `ZoneSpawnSystem.EvaluateSpawnAreas`. Not runnable -- reconstructs the scoring math for reference.

```csharp
using Game.Prefabs;
using Game.Zones;
using Unity.Mathematics;

/// <summary>
/// Reference implementation of building selection scoring.
/// Mirrors ZoneSpawnSystem.EvaluateSpawnAreas.SelectBuilding().
/// </summary>
public static class BuildingSelectionReference
{
    /// <summary>
    /// Calculates the priority score for placing a building on a vacant lot.
    /// Higher score = more likely to be selected.
    /// </summary>
    public static float CalculateBuildingScore(
        int2 lotSize,         // VacantLot size (max - min)
        int2 buildingLotSize, // BuildingData.m_LotSize
        int demand,           // Demand from demand system
        float evaluationScore, // Score from ZoneEvaluationUtils
        float landValue,      // LandValue at the road
        float propertyCount,  // Number of properties or space multiplier
        bool isResidential,
        Random random)
    {
        // Building must fit within the lot
        if (!math.all(buildingLotSize <= lotSize))
            return 0f;

        // Base score: how well the building fills the lot
        int2 remainder = math.select(lotSize - buildingLotSize, 0, buildingLotSize == lotSize - 1);
        float score = (float)(buildingLotSize.x * buildingLotSize.y) * random.NextFloat(1f, 1.05f);
        score += (float)(remainder.x * buildingLotSize.y) * random.NextFloat(0.95f, 1f);
        score += (float)(lotSize.x * remainder.y) * random.NextFloat(0.55f, 0.6f);
        score /= (float)(lotSize.x * lotSize.y);

        // Multiply by demand
        score *= (float)(demand + 1);

        // Multiply by zone evaluation (considers pollution, resources, etc.)
        score *= evaluationScore;

        return score;
    }
}
```

## Open Questions

- [ ] How exactly does the zone search tree (`Game.Zones.SearchSystem`) index blocks spatially? It uses a `NativeQuadTree<Entity, Bounds2>` but the rebuild frequency and spatial granularity weren't fully traced.
- [ ] What determines the exact split points when `BlockSystem` divides a long road edge into multiple blocks? The roundabout-based splitting logic involves curve lengths and `TryOption()` with different width combinations (2, 3 cells).
- [x] How does `ZoneCheckSystem` decide when to demolish vs. flag? **Answer**: It never demolishes directly. It adds the `Condemned` tag component when all lot cells don't match the zone type or lack a `CellFlags.Roadside` cell. A separate demolition pipeline handles condemned buildings. Valid buildings get their `Condemned` component removed. See `ZoneCheckSystem` section above for the full three-job pipeline.
- [x] What is the full list of zone type indices and their names? **Runtime-confirmed (prefab dump)**: 108 total ZonePrefabs exist (indices 0–107), spanning base game + all DLC regions. Industrial Manufacturing (entity 81) is the most-referenced zone with 500 buildings. 12 previously undocumented **SpecializedIndustrial sub-zones** were discovered: Forestry, Agriculture, Ore, and Oil each have General/Manufacturing/Warehouses variants (4 × 3 = 12). Additional undocumented zones include aquaculture, offshore oil, and waterfront commercial (DLC). The `ZoneType.m_Index` enumeration is much larger than the base-game ZoneType enum values suggest.
- [ ] How does `ZoneEvaluationUtils.GetScore()` calculate the evaluation score? This utility considers pollution, resource availability, land value, and zone preferences but the full formula was not traced.
- [x] How does `BuildingSpawnGroupData` group building prefabs by zone type? **Answer**: It's an `ISharedComponentData` with a single `m_ZoneType` field. As a shared component, all entities with the same zone type are stored in the same archetype chunks. Building selection iterates chunks and skips non-matching zone types at the chunk level, making it very efficient.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
