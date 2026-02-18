# Research: Prefab System & Custom Assets

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 loads, indexes, and resolves prefabs at runtime. How mods can add custom prefabs, override existing prefab data, query the prefab database, and duplicate/modify prefabs.

**Why**: Required for any content mod (new buildings, vehicles, props). Also covers the relationship between managed `PrefabBase` objects and their ECS `Entity` counterparts, the archetype generation pipeline, and the initialization lifecycle.

**Boundaries**: Out of scope -- rendering/visual aspects of prefabs, specific prefab subtypes beyond the hierarchy overview (individual building/vehicle/network simulation is covered in other topics).

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Prefabs | PrefabSystem, PrefabBase, ComponentBase, PrefabID, PrefabData, PrefabRef, ObjectPrefab, BuildingPrefab, StaticObjectPrefab, PrefabInitializeSystem, ObjectData |
| Game.dll | Game.Common | Created, Updated, Deleted |
| Colossal.IO.AssetDatabase.dll | Colossal.IO.AssetDatabase | AssetDatabase, PrefabAsset, ILocalAssetDatabase |

## Component Map

### `PrefabBase` (Game.Prefabs)

Abstract managed class (extends `ComponentBase`, which extends `ScriptableObject`). All prefab types inherit from this. Acts as the authoring/definition object that the game loads from asset bundles.

| Field / Property | Type | Description |
|------------------|------|-------------|
| `version` | ushort | Serialization version (0 = legacy, 1 = current) |
| `components` | List\<ComponentBase\> | Attached component definitions (data containers) |
| `isDirty` | bool | Marks prefab as needing re-initialization |
| `asset` | PrefabAsset | Reference to the asset database entry |
| `isBuiltin` | bool | True if from the base game asset database |
| `isSubscribedMod` | bool | True if from ParadoxMods |
| `isReadOnly` | bool | True if builtin, subscribed, or packaged |

**Key Methods**:
- `AddComponent<T>()` / `AddOrGetComponent<T>()` -- attach a ComponentBase to this prefab
- `Has<T>()` / `TryGet<T>()` / `TryGetExactly<T>()` -- query attached components
- `Remove<T>()` -- remove a component
- `Clone(newName)` -- deep-copy the prefab including all components
- `GetPrefabID()` -- returns the `PrefabID` identifier
- `GetPrefabComponents(HashSet<ComponentType>)` -- declares which ECS components the prefab entity needs
- `GetArchetypeComponents(HashSet<ComponentType>)` -- declares the archetype for game instances

*Source: `Game.dll` -> `Game.Prefabs.PrefabBase`*

### `ComponentBase` (Game.Prefabs)

Abstract managed class (extends `ScriptableObject`). Represents a data container attached to a prefab. Not the same as an ECS `IComponentData` -- this is the authoring-side equivalent.

| Field | Type | Description |
|-------|------|-------------|
| `active` | bool | Whether this component is enabled |
| `prefab` | PrefabBase | Back-reference to owning prefab |

**Key Methods**:
- `Initialize(EntityManager, Entity)` -- called during first initialization pass
- `LateInitialize(EntityManager, Entity)` -- called during second initialization pass
- `GetPrefabComponents(HashSet<ComponentType>)` -- declare ECS components for the prefab entity
- `GetArchetypeComponents(HashSet<ComponentType>)` -- declare ECS components for game instance entities
- `GetDependencies(List<PrefabBase>)` -- declare dependency prefabs

*Source: `Game.dll` -> `Game.Prefabs.ComponentBase`*

### `PrefabID` (Game.Prefabs)

Struct identifier for uniquely identifying prefabs. Composed of type name, prefab name, and an optional hash.

| Field | Type | Description |
|-------|------|-------------|
| `m_Type` | string | Type name (e.g., "StaticObjectPrefab", "BuildingPrefab") |
| `m_Name` | string | Prefab name (e.g., "Commercial_CornerStore01") |
| `m_Hash` | Hash128 | Asset GUID or platform ID hash |

**Key Methods**:
- `Equals()` -- matches on type + name + hash
- `GetHashCode()` -- based on name and hash
- `Serialize/Deserialize` -- for save/load persistence

*Source: `Game.dll` -> `Game.Prefabs.PrefabID`*

### `PrefabData` (ECS Component)

Minimal ECS component stored on prefab entities. Links the ECS entity back to the managed `PrefabBase` via an index into `PrefabSystem.m_Prefabs`.

| Field | Type | Description |
|-------|------|-------------|
| `m_Index` | int | Index into PrefabSystem's internal prefab list |

*Source: `Game.dll` -> `Game.Prefabs.PrefabData`*

### `PrefabRef` (ECS Component)

ECS component on game instance entities that references their prefab entity.

| Field | Type | Description |
|-------|------|-------------|
| `m_Prefab` | Entity | The prefab entity this instance was created from |

### `ObjectData` (ECS Component)

ECS component on prefab entities that stores the archetype for game instances.

| Field | Type | Description |
|-------|------|-------------|
| `m_Archetype` | EntityArchetype | The archetype used when instantiating game objects from this prefab |

### `ObjectGeometryData` (ECS Component)

Stores geometry bounds for prefab entities. Controls collision zones during object placement.

| Field | Type | Description |
|-------|------|-------------|
| `m_Size` | float3 | Full bounding box (e.g., tree canopy extent) |
| `m_LegSize` | float3 | Base/trunk footprint (smaller than m_Size for trees) |
| `m_Pivot` | float3 | Pivot point offset |
| `m_Flags` | GeometryFlags | Geometry behavior flags |

**Tree Anarchy Pattern**: Copying `m_LegSize` to `m_Size` reduces the conflict zone to just the trunk, enabling dense tree placement. **Tree vs bush detection**: `SubMesh` buffer length > 5 = tree (6 entries: child, teen, adult, elderly, dead, stump), ≤ 5 = bush.

*Source: `Game.dll` -> `Game.Prefabs.ObjectGeometryData`*

### `PseudoRandomSeed` (ECS Component)

Controls visual variation for placed objects (mesh variant, color, rotation).

| Field | Type | Description |
|-------|------|-------------|
| `m_Seed` | ushort | Random seed for variation selection |

Modifying `m_Seed` on `Temp` entities before permanent placement ensures visual diversity. In brush mode, incrementing seeds per object avoids identical-looking trees.

*Source: `Game.dll` -> `Game.Objects.PseudoRandomSeed`*

### `EditorAssetCategoryOverride` (ComponentBase)

Controls prefab categorization in editor toolbar/browser. Added to prefabs to override default category placement.

| Field | Type | Description |
|-------|------|-------------|
| `m_IncludeCategories` | string[] | Categories to add (e.g., "Props/Decorations/Industrial") |
| `m_ExcludeCategories` | string[] | Categories to exclude from |

Mods can write custom include categories (e.g., `"FindIt/{category}/{subcategory}"`) for custom categorization schemes.

*Source: `Game.dll` -> `Game.Prefabs.EditorAssetCategoryOverride`*

### `ThemeObject` (ComponentBase)

Links a prefab to a city theme (European, North American).

| Field | Type | Description |
|-------|------|-------------|
| `m_Theme` | ThemePrefab | Reference to theme prefab |

### `AssetPackItem` (ComponentBase)

Groups prefabs into asset packs (bundled content collections).

| Field | Type | Description |
|-------|------|-------------|
| `m_Packs` | AssetPackPrefab[] | Asset packs containing this prefab |

### `ContentPrerequisite` (ComponentBase)

Gates prefab availability based on content prerequisites (DLC, mods).

| Field | Type | Description |
|-------|------|-------------|
| `m_ContentPrerequisite` | PrefabBase | Prerequisite prefab (chain to DlcRequirement) |

Access DLC ID: `contentPrerequisite.m_ContentPrerequisite.TryGet<DlcRequirement>(out var dlc)` → `dlc.m_Dlc` is the `DlcId` enum value.

### `UIObject` (ComponentBase)

Controls toolbar presentation for prefabs.

| Field | Type | Description |
|-------|------|-------------|
| `m_Priority` | int | Sort order in toolbar (lower = appears first) |
| `m_Group` | UIAssetMenuPrefab | Toolbar group/category |

### `Unlockable` (ComponentBase)

Gates prefab availability behind milestone requirements.

| Field | Type | Description |
|-------|------|-------------|
| `m_RequireAll` | PrefabBase[] | All prerequisites must be met |
| `m_RequireAny` | PrefabBase[] | At least one prerequisite must be met |

**Known unlock requirement names**: Roads: `"RoadsSmallRoads"`, `"RoadsMediumRoads"`, `"RoadsLargeRoads"`, `"RoadsHighways"`. Transport: `"TransportationTrain"`, `"TransportationTram"`, `"TransportationSubway"`. The `Locked` enabled component on the ECS entity controls runtime lock state.

### `UnlockOnBuild` (ComponentBase)

Defines what gets unlocked when this prefab is built.

| Field | Type | Description |
|-------|------|-------------|
| `m_Unlocks` | ObjectBuiltRequirementPrefab[] | Requirements satisfied by building this |

### `ServiceObject` (ComponentBase)

Associates a prefab with a city service for toolbar categorization.

| Field | Type | Description |
|-------|------|-------------|
| `m_Service` | ServicePrefab | Reference to service prefab (e.g., "Roads", "Transportation") |

The service determines the top-level toolbar tab. Combined with `UIObject.m_Group` for full placement: `Service → UIGroup → Prefab`. Known service names: `"Roads"`, `"Transportation"`, `"Landscaping"`, `"Electricity"`, `"WaterAndSewage"`.

### City Service Building Prefab Components

City service buildings carry type-specific prefab data components that define their capacities and behaviors. These are `IComponentData` on the prefab entity:

| Component | Namespace | Key Fields | Used By |
|-----------|-----------|------------|---------|
| `SchoolData` | Game.Prefabs | `m_StudentCapacity` (int), `m_EducationLevel` (int) | Education system |
| `HospitalData` | Game.Prefabs | `m_PatientCapacity` (int), `m_TreatmentBonus` (float) -- bonus distinguishes clinics from hospitals | Healthcare system |
| `PrisonData` | Game.Prefabs | `m_PrisonerCapacity` (int) | Police system |
| `PowerPlantData` | Game.Prefabs | `m_ElectricityProduction` (int) | Electricity system |
| `FireStationData` | Game.Prefabs | (capacity fields) | Fire response |
| `PoliceStationData` | Game.Prefabs | (capacity fields) | Police dispatch |
| `PostFacilityData` | Game.Prefabs | (capacity fields) | Mail system |
| `GarbageFacilityData` | Game.Prefabs | (capacity fields) | Garbage collection |
| `MaintenanceDepotData` | Game.Prefabs | (capacity fields) | Road maintenance |
| `TransportDepotData` | Game.Prefabs | (capacity fields) | Public transport |
| `TransportStationData` | Game.Prefabs | `m_WatercraftRefuelTypes`, `m_AircraftRefuelTypes`, `m_ComfortFactor` -- used to distinguish ports, airports, cargo stations, and passenger stations | Transport system |
| `CargoTransportStationData` | Game.Prefabs | (cargo-specific fields) | Cargo transport |
| `AdminBuildingData` | Game.Prefabs | (capacity fields) | City administration |
| `WelfareOfficeData` | Game.Prefabs | (capacity fields) | Welfare services |
| `ResearchFacilityData` | Game.Prefabs | (capacity fields) | Research |
| `TelecomFacilityData` | Game.Prefabs | (capacity fields) | Telecom service |
| `ParkData` | Game.Prefabs | (capacity fields) | Parks & recreation |
| `SolarPoweredData` | Game.Prefabs | Identifies solar power plants (used for worker reduction) | Power system |

**Station Subtype Detection Pattern**: `TransportStationData` can be classified by checking refuel types and comfort factor:
- Port: `m_WatercraftRefuelTypes != 0`
- Airport: `m_AircraftRefuelTypes != 0`
- Passenger station: `m_ComfortFactor > 0` (without watercraft/aircraft refuel)
- Cargo station: has `CargoTransportStationData`

*Source: RealisticWorkplacesAndHouseholds mod*

### DLC Asset Pack Detection

Buildings can be linked to DLC content packs via the `AssetPackData` and `AssetPackElement` components:

- `AssetPackData` (Game.Prefabs) -- marks an entity as an asset pack definition
- `AssetPackElement` (Game.Prefabs, IBufferElementData) -- buffer element linking a building prefab to its asset pack(s). Field: `m_Pack` (Entity)

**Querying asset packs**:
```csharp
EntityQuery m_AssetPackQuery = GetEntityQuery(new EntityQueryDesc
{
    All = new[] {
        ComponentType.ReadOnly<AssetPackData>(),
        ComponentType.ReadOnly<PrefabData>(),
    }
});
```

**Detecting DLC membership on a building**:
```csharp
if (AssetPackElementBufferLookup.TryGetBuffer(buildingEntity, out var assetPackElements))
{
    for (int index = 0; index < assetPackElements.Length; ++index)
    {
        Entity pack = assetPackElements[index].m_Pack;
        string packName = PrefabSystem.GetPrefabName(pack);
        // Known pack names: "uk", "de", "nl", "fr", "jp", "cn", "ee",
        //   "ussw", "usne", "mediterraneanheritage", "dragongate", "skyscrapers"
    }
}
```

This is useful for mods that need per-DLC behavior adjustments (e.g., region-specific density factors).

*Source: RealisticWorkplacesAndHouseholds mod*

### `PlaceableInfoviewItem` (ECS Component)

Auto-added by the prefab initialization pipeline for certain prefab types (e.g., `RoadPrefab`). Must be explicitly removed from dynamically created prefabs to prevent infoview rendering issues:

```csharp
// In a system at SystemUpdatePhase.PrefabUpdate:
EntityQuery customPrefabs = SystemAPI.QueryBuilder()
    .WithAll<MyCustomPrefabTag, PlaceableInfoviewItem>()
    .Build();
EntityManager.RemoveComponent<PlaceableInfoviewItem>(customPrefabs);
```

### `ObjectGeometryPrefab` (Game.Prefabs, ComponentBase)

Subclass of `ObjectPrefab` that provides geometry and mesh data for objects. Attached as a `ComponentBase` to an `ObjectPrefab` to define its mesh/LOD configuration.

| Field | Type | Description |
|-------|------|-------------|
| `m_Meshes` | List\<ObjectMeshInfo\> | List of mesh definitions (LODs, variants, sub-meshes) |

Use `PrefabBase.TryGet<ObjectGeometryPrefab>(out var geom)` to access the mesh list from a managed prefab.

### `RenderPrefab` (Game.Prefabs)

Base class for renderable prefab data. Provides access to computed mesh bounds.

| Field / Property | Type | Description |
|------------------|------|-------------|
| `bounds` | Bounds3 | Axis-aligned bounding box of the mesh (min/max float3 corners) |

**Reading object dimensions from prefabs**: Combine `ObjectGeometryPrefab.m_Meshes` with `RenderPrefab.bounds` to determine physical mesh size at the prefab level, before any ECS components are created:

```csharp
if (prefab.TryGet<ObjectGeometryPrefab>(out var geom))
{
    foreach (var meshInfo in geom.m_Meshes)
    {
        RenderPrefab renderPrefab = meshInfo.m_Mesh;
        if (renderPrefab != null)
        {
            Bounds3 bounds = renderPrefab.bounds;
            float3 size = bounds.max - bounds.min;
            // size.x = width, size.y = height, size.z = depth
        }
    }
}
```

*Source: `Game.dll` -> `Game.Prefabs.ObjectGeometryPrefab`, `Game.dll` -> `Game.Prefabs.RenderPrefab`*

## Prefab Type Hierarchy

```
PrefabBase (abstract)
  +-- ObjectPrefab                    // Any standalone object
  |     +-- StaticObjectPrefab        // Non-moving objects
  |     |     +-- BuildingPrefab      // Buildings with lots
  |     |     +-- SignObjectPrefab    // Signs
  |     +-- MovingObjectPrefab        // Vehicles, citizens
  |     |     +-- VehiclePrefab
  |     |     |     +-- WatercraftPrefab  // Boats, ships
  |     |     +-- CreaturePrefab
  |     +-- ObjectGeometryPrefab      // Geometry/LOD data for objects
  +-- NetPrefab                       // Networks (roads, pipes, power lines)
  |     +-- NetLaneGeometryPrefab     // Lane geometry definitions
  +-- AreaPrefab                      // Zones, districts
  +-- UIAssetMenuPrefab               // UI menu entries
  +-- ClimatePrefab                   // Climate settings
  +-- ContentPrefab                   // DLC/mod content markers
  +-- UnlockRequirementPrefab         // Milestone prerequisites
```

## System Map

### `PrefabSystem` (Game.Prefabs)

Central managed system for all prefab operations. Maintains the master list of prefabs and their ECS entity mappings.

**Key Fields**:
- `m_Prefabs` (List\<PrefabBase\>) -- master ordered list of all prefabs
- `m_Entities` (Dictionary\<PrefabBase, Entity\>) -- managed prefab -> ECS entity mapping
- `m_PrefabIndices` (Dictionary\<PrefabID, int\>) -- ID -> index lookup
- `m_IsUnlockable` (Dictionary\<PrefabBase, bool\>) -- unlock state cache

**Key Methods**:
- `AddPrefab(prefab)` -- register a new prefab, creates its ECS entity with components
- `RemovePrefab(prefab)` -- unregister a prefab, marks entity as Deleted
- `UpdatePrefab(prefab)` -- schedule prefab for re-initialization
- `DuplicatePrefab(template, name)` -- clone an existing prefab
- `GetEntity(prefab)` -- get the ECS entity for a managed prefab
- `TryGetEntity(prefab, out entity)` -- safe version
- `GetPrefab<T>(entity)` -- get the managed prefab from an ECS entity
- `TryGetPrefab(PrefabID, out prefab)` -- lookup by ID
- `GetComponentData<T>(prefab)` -- get ECS data from a prefab's entity
- `AddComponentData<T>(prefab, data)` -- add ECS data to a prefab's entity
- `HasComponent<T>(prefab)` -- check ECS component on prefab entity

**OnUpdate**: Processes the `m_UpdateMap` queue, replacing prefab entities with fresh ones containing updated component sets.

### `PrefabInitializeSystem` (Game.Prefabs)

Processes newly created prefab entities (those with `Created` component). Runs the two-phase initialization:

1. **Initialize pass**: Calls `ComponentBase.Initialize()` on each component, discovers dependency prefabs, adds them to the queue
2. **LateInitialize pass**: Calls `ComponentBase.LateInitialize()`, sets up unlock requirements

## Data Flow

```
[Asset Database loads PrefabBase objects]
        |
[PrefabSystem.AddPrefab(prefab)]
  - Collects ComponentBase list from prefab
  - Each ComponentBase declares its ECS components via GetPrefabComponents()
  - Creates Entity with those ECS components + Created + Updated
  - Sets PrefabData.m_Index
  - Registers in m_Entities and m_PrefabIndices
        |
[PrefabInitializeSystem.OnUpdate]
  - Queries entities with Created + PrefabData
  - Phase 1: ComponentBase.Initialize() + dependency discovery
  - Dependencies queued and added via PrefabSystem.AddPrefab()
  - Phase 2: ComponentBase.LateInitialize()
  - Sets up unlock requirements
        |
[ObjectPrefab.LateInitialize -> RefreshArchetype]
  - Collects archetype components via GetArchetypeComponents()
  - Creates EntityArchetype stored in ObjectData.m_Archetype
  - This archetype is used when instantiating game objects
```

## Two-Entity Model

CS2 uses two kinds of entities for prefabs:

1. **Prefab Entity**: Created by `PrefabSystem.AddPrefab()`. Has `PrefabData` component with index. Stores configuration data (costs, sizes, behaviors). One per prefab type.

2. **Instance Entity**: Created when objects are placed in the game world. Has `PrefabRef` component pointing to the prefab entity. Has the archetype defined by `ObjectData.m_Archetype`. Many per prefab type.

```
[Prefab Entity]                    [Instance Entity]
  PrefabData { m_Index = 42 }       PrefabRef { m_Prefab = prefabEntity }
  BuildingData { ... }               Building { ... }
  PlaceableObjectData { ... }        Transform { ... }
  ObjectData { m_Archetype = ... }   Owner { ... }
  ...                                 ...
```

## Content Source Detection

CS2 prefabs come from different sources (vanilla, PDX Mods, DLC). The `PrefabBase` class provides properties to distinguish content origin.

### How `isBuiltin` Works

```csharp
// Decompiled from PrefabBase
public bool isBuiltin
{
    get
    {
        if (AssetDatabase.global.resources.prefabsMap.TryGetGuid(this, out _))
            return true;
        return asset?.database is AssetDatabase<Game>;
    }
}

public bool isSubscribedMod => asset?.database is AssetDatabase<ParadoxMods>;
```

### Content Source Patterns

| Source | Check | Notes |
|--------|-------|-------|
| Vanilla (base game) | `prefab.isBuiltin` | True for assets in the Game database or global resources map |
| PDX Mods (subscribed) | `prefab.asset?.database == AssetDatabase<ParadoxMods>.instance` | Also available via `prefab.isSubscribedMod` |
| PDX Mods platform ID | `prefab.asset.GetMeta().platformID` | Unique per-mod identifier; > 0 for published mods |
| DLC content | ContentPrerequisite -> DlcRequirement chain | Requires checking unlock prerequisite components |
| Mod-generated (runtime) | Custom tag component | Prefabs created at runtime by mods have no asset database entry |

### Complete Content Source Example (FindIt Pattern)

```csharp
bool isVanilla = prefab.isBuiltin;
bool isFromPdxMods = false;
int pdxModsId = 0;

if (prefab.asset?.database == AssetDatabase<ParadoxMods>.instance)
{
    isFromPdxMods = true;
    pdxModsId = prefab.asset.GetMeta().platformID;
}
```

## Placeholder Objects (Random Variants)

Some prefabs use `PlaceholderObjectData` and `PlaceholderObjectElement` to randomly select from a set of variant prefabs at placement time.

### `PlaceholderObjectData` (Game.Prefabs, ComponentBase)

Attached to a prefab to mark it as a placeholder that resolves to one of its variant elements.

| Field | Type | Description |
|-------|------|-------------|
| `m_RequirementMask` | ObjectRequirementFlags | Flags filtering which variants are eligible |
| `m_RandomizeGroupIndex` | bool | Whether to randomize the group selection index |

### `PlaceholderObjectElement` (Game.Prefabs, IBufferElementData)

Buffer element on placeholder prefab entities, listing the possible variant prefabs.

| Field | Type | Description |
|-------|------|-------------|
| `m_Object` | Entity | Reference to a variant prefab entity |

### `PlaceholderBuildingData` (Game.Prefabs, ComponentBase)

Similar placeholder for zone buildings -- used by `ZoneSpawnSystem` to select random building variants.

| Field | Type | Description |
|-------|------|-------------|
| `m_ZonePrefab` | Entity | Zone prefab this placeholder belongs to |
| `m_Type` | BuildingType | Building category (Residential/Commercial/Industrial) |

## Decal Detection via MeshData

Decal objects can be detected by checking the `MeshFlags.Decal` flag on the prefab's first sub-mesh:

```csharp
public static bool IsDecal(EntityManager em, Entity prefabEntity)
{
    if (!em.TryGetBuffer<SubMesh>(prefabEntity, true, out var subMesh)
        || subMesh.Length == 0)
        return false;

    if (!em.TryGetComponent<MeshData>(subMesh[0].m_SubMesh, out var meshData))
        return false;

    return meshData.m_State == MeshFlags.Decal;
}
```

### `MeshFlags` (Game.Prefabs)

| Flag | Value | Description |
|------|-------|-------------|
| `Decal` | 1 | Object is a decal (flat surface overlay) |
| `StackX` | 2 | Stacks along X axis |
| `StackZ` | 4 | Stacks along Z axis |

### `SubMesh` (Game.Prefabs, IBufferElementData)

| Field | Type | Description |
|-------|------|-------------|
| `m_SubMesh` | Entity | Reference to sub-mesh entity with `MeshData` |
| `m_Position` | float3 | Local position offset |
| `m_Rotation` | quaternion | Local rotation |
| `m_Flags` | SubMeshFlags | Rendering flags |

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Prefab name | PrefabBase.name | ScriptableObject.name |
| Prefab type | PrefabBase.GetType().Name | Used in PrefabID |
| Asset GUID | PrefabBase.asset.id.guid | Colossal.IO.AssetDatabase |
| Builtin status | PrefabBase.isBuiltin | Checks database type |
| Mod status | PrefabBase.isSubscribedMod | Checks ParadoxMods database |
| PDX Mods platform ID | PrefabBase.asset.GetMeta().platformID | Unique mod identifier |
| Lot dimensions | BuildingPrefab.m_LotWidth/m_LotDepth | Game.Prefabs |

## Harmony Patch Points

### Candidate 1: `PrefabSystem.AddPrefab`

- **Signature**: `bool AddPrefab(PrefabBase prefab, string parentName, PrefabBase parentPrefab, ComponentBase parentComponent)`
- **Patch type**: Prefix/Postfix
- **What it enables**: Intercept or modify prefabs as they're registered
- **Risk level**: Medium (called during loading; errors can break prefab database)

### Candidate 2: `PrefabInitializeSystem.OnUpdate`

- **Patch type**: Postfix
- **What it enables**: Modify prefab entity data after initialization
- **Risk level**: Low

### Candidate 3: `ComponentBase.GetPrefabComponents` / `GetArchetypeComponents`

- **Patch type**: Postfix on specific subclasses
- **What it enables**: Add extra ECS components to specific prefab types
- **Risk level**: Medium

## Mod Blueprint

- **To add a custom prefab**: Create a `PrefabBase` subclass instance, add `ComponentBase` components, call `PrefabSystem.AddPrefab()`
- **To modify an existing prefab**: Use `PrefabSystem.TryGetPrefab(PrefabID)`, modify components, call `PrefabSystem.UpdatePrefab()`
- **To duplicate a prefab**: Use `PrefabSystem.DuplicatePrefab(template, name)` or `PrefabBase.Clone()`
- **To query prefabs**: Use `PrefabSystem.TryGetPrefab(PrefabID)` or iterate `PrefabSystem.prefabs`

## Mod Blueprint: Prefab Search & Browser (FindIt Pattern)

A blueprint for mods that provide searchable, browsable prefab databases with categorization, filtering, and direct placement -- based on the FindIt mod architecture.

**Mod archetype**: Prefab browser/search tool. The mod indexes all game prefabs, categorizes them by type and properties, provides a searchable UI with virtual scrolling, and allows direct placement of found objects via the game's tool system.

### Systems to Create

| System | Phase | Purpose |
|--------|-------|---------|
| PrefabIndexingSystem | PrefabUpdate | Indexes all prefabs with incremental update support via `Created`/`Updated`/`Deleted` queries. Uses `IPrefabCategoryProcessor` strategy pattern for pluggable categorization logic per prefab type |
| FindItUISystem | UIUpdate | `ExtendedUISystemBase` managing search panel with virtual scrolling, async search with `CancellationToken` and debouncing, and `ValueBindingHelper` for batched UI updates |
| PickerToolSystem | ToolUpdate | Custom `ToolBaseSystem` for eyedropper/picker tool with configurable raycast to identify placed objects |
| PrefabTrackingSystem | PrefabUpdate | Tracks placed instances of each prefab using `PrefabRef`-based entity counting |

### Components to Create

| Component | Type | Purpose |
|-----------|------|---------|
| PrefabIndexEntry | Managed (C# class) | Stores indexed prefab metadata: name, category, subcategory, thumbnail path, source (vanilla/mod/DLC), search keywords |
| CategoryDefinition | Managed (C# class) | Defines a category hierarchy node for the browser tree |
| SearchResult | Managed (C# class) | Wraps a prefab reference with match score for ranked search results |

### Harmony Patches Needed

- **None required** -- all functionality uses public APIs: `PrefabSystem` for prefab access, `ToolSystem.ActivatePrefabTool()` for placement, `ImageSystem` for thumbnails, `PrefabUISystem` for localized names

### Key Game Components

- `PrefabData` / `PrefabRef` -- core prefab identity and instance tracking
- `PrefabSystem.TryGetPrefab()` -- entity-to-prefab resolution
- `ImageSystem.GetThumbnail()` -- icon/thumbnail resolution for UI display
- `PrefabUISystem.GetTitleAndDescription()` -- localized prefab name resolution
- `EditorAssetCategoryOverride` -- category detection for prefab classification
- `ServiceObject.m_Service` -- service building classification by service name
- `ZoneData.m_AreaType` + `ZonePropertiesData` -- residential density classification
- `SpawnableBuildingData.m_ZonePrefab` -- building-to-zone linkage
- `BrandObjectData` / `PlaceholderObjectData` -- component presence for filtering placeholder and branded objects
- `ToolSystem.ActivatePrefabTool(PrefabBase)` -- programmatic placement of found objects
- `Created` / `Updated` / `Deleted` -- incremental prefab change detection

### Prefab Categorization Patterns

```csharp
// Type detection via EntityQuery
EntityQuery treePrefabs = SystemAPI.QueryBuilder()
    .WithAll<PrefabData, TreeData, GrowthScaleData>()
    .Build();

EntityQuery propPrefabs = SystemAPI.QueryBuilder()
    .WithAll<PrefabData, StaticObjectData>()
    .WithNone<BuildingData, TreeData, NetObjectData, PlaceholderObjectData>()
    .Build();

// Service building classification
if (prefab.TryGet<ServiceObject>(out var svc))
{
    string category = svc.m_Service.name; // "Roads", "Transportation", "Landscaping", etc.
}

// Zone density classification
if (em.TryGetComponent<ZoneData>(zonePrefabEntity, out var zoneData))
{
    AreaType area = zoneData.m_AreaType; // Residential, Commercial, Industrial
}

// Content source detection
bool isVanilla = prefab.isBuiltin;
bool isFromMods = prefab.asset?.database == AssetDatabase<ParadoxMods>.instance;
```

### UI Integration Pattern

```csharp
// Virtual scrolling with batched updates
ValueBindingHelper<int> m_ResultCount;
ValueBindingHelper<string[]> m_VisibleResults;

// Async search with debouncing
private CancellationTokenSource m_SearchCts;
public void OnSearchTextChanged(string text)
{
    m_SearchCts?.Cancel();
    m_SearchCts = new CancellationTokenSource();
    _ = SearchAsync(text, m_SearchCts.Token);
}

// Place found object
var toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
toolSystem.ActivatePrefabTool(foundPrefab);
```

### Key Considerations

- Use **incremental indexing** with `Created`/`Updated`/`Deleted` queries -- never re-index all prefabs every frame
- `IPrefabCategoryProcessor` strategy pattern enables clean separation of categorization logic per prefab type (buildings, props, trees, networks, vehicles)
- `GenericUIWriter`/`GenericUIReader` for reflection-based serialization of complex search result objects to the UI layer
- Virtual scrolling is essential -- the game has thousands of prefabs; rendering all at once is prohibitive
- `CancellationToken` and debouncing prevent search lag during rapid text input
- Content source detection (`isBuiltin`, `isSubscribedMod`, `platformID`) enables filtering by vanilla/mod/DLC
- `EditorAssetCategoryOverride` can be written by mods to create custom categorization schemes (e.g., `"FindIt/{category}/{subcategory}"`)
- Exclude `Owner`, `Controller`, and `Overridden` entities when counting placed instances to count only top-level objects
- Run instance counting on a timer (e.g., every 60 seconds), not every frame

## Examples

### Example 1: Looking Up a Prefab by ID

```csharp
public partial class PrefabLookupSystem : GameSystemBase
{
    private PrefabSystem m_PrefabSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
    }

    public PrefabBase FindPrefab(string typeName, string prefabName)
    {
        PrefabID id = new PrefabID(typeName, prefabName);
        if (m_PrefabSystem.TryGetPrefab(id, out PrefabBase prefab))
        {
            return prefab;
        }
        return null;
    }

    protected override void OnUpdate() { }
}
```

### Example 2: Adding a Custom Prefab at Runtime

```csharp
public void AddCustomBuilding(PrefabSystem prefabSystem)
{
    // Clone an existing building prefab
    PrefabID templateID = new PrefabID(
        nameof(BuildingPrefab), "Commercial_CornerStore01");

    if (prefabSystem.TryGetPrefab(templateID, out PrefabBase template))
    {
        PrefabBase clone = prefabSystem.DuplicatePrefab(
            template, "MyMod_CustomStore");
        // Modify components on the clone
        if (clone.TryGet<PlaceableObjectData>(out var placeable))
        {
            // Modify placement cost, etc.
        }
    }
}
```

### Example 3: Reading Prefab Data from an Entity

```csharp
public void ReadPrefabInfo(EntityManager em, PrefabSystem prefabSystem, Entity entity)
{
    if (em.TryGetComponent<PrefabRef>(entity, out PrefabRef prefabRef))
    {
        Entity prefabEntity = prefabRef.m_Prefab;
        PrefabData prefabData = em.GetComponentData<PrefabData>(prefabEntity);
        PrefabBase prefab = prefabSystem.GetPrefab<PrefabBase>(prefabData);

        string name = prefab.name;
        string type = prefab.GetType().Name;
        bool isBuiltin = prefab.isBuiltin;
    }
}
```

### Example 4: Modifying Prefab ECS Data

```csharp
public void ModifyPrefabData(PrefabSystem prefabSystem)
{
    PrefabID id = new PrefabID(nameof(BuildingPrefab), "SomeBuilding");
    if (prefabSystem.TryGetPrefab(id, out PrefabBase prefab))
    {
        // Add or modify ECS component data on the prefab entity
        if (prefabSystem.TryGetComponentData<PlaceableObjectData>(
            prefab, out var data))
        {
            data.m_ConstructionCost = 5000;
            // Note: SetComponentData not directly exposed;
            // use EntityManager via GetEntity()
            Entity entity = prefabSystem.GetEntity(prefab);
            World.DefaultGameObjectInjectionWorld.EntityManager
                .SetComponentData(entity, data);
        }
    }
}
```

### Example 5: Iterating All Prefabs of a Type

```csharp
public void ListAllBuildings(PrefabSystem prefabSystem)
{
    foreach (PrefabBase prefab in prefabSystem.prefabs)
    {
        if (prefab is BuildingPrefab building)
        {
            int lotSize = building.m_LotWidth * building.m_LotDepth;
            // Process building prefab
        }
    }
}
```

### Incremental Prefab Update with Created/Updated Queries

To build efficient systems that react to prefab changes (e.g., maintaining a search index), use `Created` and `Updated` marker components:

```csharp
// 1. RequireForUpdate: only run when prefabs have changed
RequireForUpdate(SystemAPI.QueryBuilder()
    .WithAll<PrefabData>()
    .WithAny<Created, Updated>()
    .Build());

// 2. In OnUpdate, check each processor's query for changes
ComponentType[] anyFilter = new[] {
    ComponentType.ReadOnly<Created>(),
    ComponentType.ReadOnly<Updated>()
};
// If query with Created/Updated is empty, skip processing

// 3. Handle removals via Deleted component
EntityQuery deletedQuery = SystemAPI.QueryBuilder()
    .WithAll<PrefabData, Deleted>()
    .Build();
```

The `Created` component is added by `PrefabInitializeSystem` when a prefab entity is first created. `Updated` is added when a prefab is modified. Both are removed the frame after processing. `Deleted` marks entities being destroyed.

## GetPrefabComponents Override

Custom `PrefabBase` subclasses can override `GetPrefabComponents()` to register mod-specific ECS components on prefab entities. This enables efficient entity queries without iterating all prefabs:

```csharp
public class RoadBuilderPrefab : NetPrefab
{
    public override void GetPrefabComponents(HashSet<ComponentType> components)
    {
        base.GetPrefabComponents(components);
        components.Add(ComponentType.ReadWrite<RoadBuilderPrefabData>());
    }
}

// Now efficiently query all RoadBuilder prefab entities:
EntityQuery query = SystemAPI.QueryBuilder()
    .WithAll<RoadBuilderPrefabData>()
    .Build();
```

The same pattern works for `ComponentBase` subclasses -- each one has `GetPrefabComponents()` and `GetArchetypeComponents()` that declare which ECS components the prefab entity and game instances need.

## Prefab Icon & Name Resolution

### ImageSystem (Icon Resolution)

`Game.UI.ImageSystem` resolves prefab icons and thumbnails for display in UI:

```csharp
var imageSystem = World.GetOrCreateSystemManaged<Game.UI.ImageSystem>();
string thumbnail = imageSystem.GetThumbnail(prefab); // Large preview image path
string icon = imageSystem.GetIcon(prefab);           // Small icon path
// For entities: string icon = imageSystem.GetIconOrGroupIcon(entity);
```

### PrefabUISystem (Localized Names)

`Game.UI.InGame.PrefabUISystem` resolves localized prefab display names:

```csharp
var prefabUI = World.GetOrCreateSystemManaged<PrefabUISystem>();
prefabUI.GetTitleAndDescription(entity, out string titleId, out string descId);
// titleId = "Assets.NAME[PrefabName]"

// Resolve localized name:
GameManager.instance.localizationManager.activeDictionary
    .TryGetValue(titleId, out string displayName);

// Fallback: prefab.name.Replace('_', ' ').FormatWords()
```

## ECS Entity Query Patterns for Prefab Type Detection

Common entity query patterns for identifying and categorizing prefab types by their ECS components:

| Category | All Components | None Components |
|----------|---------------|-----------------|
| Service buildings | BuildingData, ServiceObjectData | |
| Zoned buildings | BuildingData, SpawnableBuildingData | |
| Props | StaticObjectData | BuildingData, TreeData, NetObjectData |
| Trees | TreeData, GrowthScaleData | |
| Vehicles | VehicleData | |
| Networks | NetData | |
| Net objects (pillars) | NetObjectData | |
| Areas | AreaData | |

Additional categorization components: `PillarData`, `QuantityObjectData`, `PlaceableObjectData`, `ServiceObjectData`, `ServicePrefab`.

```csharp
// Example: query all tree prefab entities
EntityQuery treePrefabs = SystemAPI.QueryBuilder()
    .WithAll<PrefabData, TreeData, GrowthScaleData>()
    .Build();

// Example: query all prop prefab entities (exclude buildings, trees, etc.)
EntityQuery propPrefabs = SystemAPI.QueryBuilder()
    .WithAll<PrefabData, StaticObjectData>()
    .WithNone<BuildingData, TreeData, NetObjectData, PlaceholderObjectData>()
    .Build();
```

## Runtime Prefab Creation (Full Pattern)

Complete pattern for creating prefabs at runtime using `ScriptableObject.CreateInstance`. Use `OnGamePreload` for timing — it runs before `PrefabInitializeSystem` processes entities:

```csharp
protected override void OnGamePreload(Purpose purpose, GameMode mode)
{
    base.OnGamePreload(purpose, mode);

    // 1. Create prefab instance
    var newPrefab = ScriptableObject.CreateInstance<StaticObjectPrefab>();
    newPrefab.name = "MyMod_CustomProp";

    // 2. Copy components from a template
    PrefabID templateId = new PrefabID(nameof(StaticObjectPrefab), "TemplateProp");
    if (m_PrefabSystem.TryGetPrefab(templateId, out var template))
    {
        // Copy geometry for mesh data
        if (template.Has<ObjectGeometryPrefab>())
            newPrefab.AddComponentFrom(template.GetComponent<ObjectGeometryPrefab>());
    }

    // 3. Add save compatibility identifier
    newPrefab.AddComponent<ObsoleteIdentifiers>();

    // 4. Register with PrefabSystem
    m_PrefabSystem.AddPrefab(newPrefab);
}
```

**Key details:**
- `AddComponentFrom()` deep-copies a `ComponentBase` from one prefab to another
- `ObsoleteIdentifiers` prevents save corruption when the game encounters unknown prefabs
- `TryGetPrefab(new PrefabID(...))` for looking up existing prefabs to use as mesh sources

## FakePrefab Pattern (Custom Mod Entities)

Mods that need custom entities which pass vanilla `PrefabRef` validation (e.g., custom markers, spatial index entries, or non-visual data carriers) can use the "FakePrefab" pattern: create a minimal `PrefabBase` subclass, register it with `PrefabSystem.AddPrefab()`, and use the resulting prefab entity as the `PrefabRef.m_Prefab` for mod-created entities.

### Timing: `IPreDeserialize`

Register fake prefabs during `IPreDeserialize.PreDeserialize()` -- this runs before `PrefabInitializeSystem` processes entities, ensuring the prefab is indexed before any entity references it:

```csharp
public class Mod : IMod, IPreDeserialize
{
    public void PreDeserialize(Context context)
    {
        var prefabSystem = World.DefaultGameObjectInjectionWorld
            .GetOrCreateSystemManaged<PrefabSystem>();

        // Create a minimal prefab
        var fakePrefab = ScriptableObject.CreateInstance<StaticObjectPrefab>();
        fakePrefab.name = "MyMod_DataCarrier";

        // Register it — creates the ECS entity with PrefabData
        prefabSystem.AddPrefab(fakePrefab);
    }
}
```

### `PrefabID`-Based Lookups

After registration, retrieve the fake prefab's entity via `PrefabID` lookup:

```csharp
PrefabID id = new PrefabID(nameof(StaticObjectPrefab), "MyMod_DataCarrier");
if (m_PrefabSystem.TryGetPrefab(id, out PrefabBase prefab))
{
    Entity prefabEntity = m_PrefabSystem.GetEntity(prefab);
    // Use prefabEntity as PrefabRef.m_Prefab for mod entities
}
```

**Key constraints**: The fake prefab must be registered before any entity references it via `PrefabRef`. Using `IPreDeserialize` ensures this. The prefab entity passes all vanilla validation that checks `PrefabRef.m_Prefab` existence.

## UpdatePrefab and Runtime Re-Initialization

`PrefabSystem.UpdatePrefab(prefab)` schedules a prefab for re-initialization, causing its ECS entity to be rebuilt with updated component data. This is needed when modifying a prefab's `ComponentBase` list or properties after initial registration.

### Reflection-Based `m_PrefabIndices` Fixup

`UpdatePrefab()` replaces the old prefab entity with a new one, but `m_PrefabIndices` (a `Dictionary<PrefabID, int>`) may become stale if the prefab's index changes. Use reflection to fix up the index map when needed:

```csharp
var prefabIndices = typeof(PrefabSystem)
    .GetField("m_PrefabIndices", BindingFlags.NonPublic | BindingFlags.Instance)
    .GetValue(m_PrefabSystem) as Dictionary<PrefabID, int>;

// Update the index entry for the modified prefab
PrefabID id = prefab.GetPrefabID();
int currentIndex = m_PrefabSystem.GetPrefab<PrefabBase>(
    EntityManager.GetComponentData<PrefabData>(
        m_PrefabSystem.GetEntity(prefab))).GetPrefabID().GetHashCode();
prefabIndices[id] = currentIndex;
```

### `UIGroupElement` Buffer Cleanup

When updating prefabs that have `UIObject` components (toolbar entries), the `UIGroupElement` buffer on the group entity may contain stale references. Clean up before calling `UpdatePrefab()`:

```csharp
if (prefab.TryGet<UIObject>(out var uiObject) && uiObject.m_Group != null)
{
    Entity groupEntity = m_PrefabSystem.GetEntity(uiObject.m_Group);
    if (EntityManager.HasBuffer<UIGroupElement>(groupEntity))
    {
        var buffer = EntityManager.GetBuffer<UIGroupElement>(groupEntity);
        Entity prefabEntity = m_PrefabSystem.GetEntity(prefab);
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            if (buffer[i].m_Prefab == prefabEntity)
                buffer.RemoveAt(i);
        }
    }
}

// Now safe to update — will re-add UIGroupElement entries
m_PrefabSystem.UpdatePrefab(prefab);
```

### Cascading `Updated` Components

After `UpdatePrefab()` processes, the prefab entity receives the `Updated` component. Systems that cache prefab data should react to this via queries:

```csharp
EntityQuery updatedPrefabs = SystemAPI.QueryBuilder()
    .WithAll<PrefabData, Updated>()
    .Build();
// Re-read any cached data from these prefab entities
```

Any system that reads prefab ECS data should either query for `Updated` or subscribe to the prefab change pipeline to stay in sync.

## PlaceableObjectData Cost Modification

Modify ECS component data on prefab entities at runtime. Use managed `PrefabBase.TryGet<ComponentBase>()` as source of truth for restoring original values:

```csharp
protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
{
    base.OnGameLoadingComplete(purpose, mode);

    // Make all trees free
    var query = SystemAPI.QueryBuilder()
        .WithAll<TreeData, PlaceableObjectData>()
        .Build();
    var entities = query.ToEntityArray(Allocator.Temp);
    foreach (var entity in entities)
    {
        var data = EntityManager.GetComponentData<PlaceableObjectData>(entity);
        data.m_ConstructionCost = 0;
        EntityManager.SetComponentData(entity, data);
    }
    entities.Dispose();
}

// Restore original cost from managed ComponentBase (immutable original):
PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(prefabData);
if (prefab.TryGet<PlaceableObject>(out var original))
{
    // PlaceableObject (managed ComponentBase) = immutable original
    // PlaceableObjectData (ECS component) = mutable runtime copy
}
```

## PrefabRef-Based Entity Counting

Count placed instances of each prefab in the game world using `PrefabRef`:

```csharp
private EntityQuery m_PlacedObjectQuery;

protected override void OnCreate()
{
    base.OnCreate();
    m_PlacedObjectQuery = SystemAPI.QueryBuilder()
        .WithAll<PrefabRef>()
        .WithAny<Game.Objects.Object, Game.Net.Edge>()
        .WithNone<Owner, Controller, Overridden>()
        .Build();
}

public Dictionary<int, int> CountPrefabInstances()
{
    var counts = new Dictionary<int, int>();
    var chunks = m_PlacedObjectQuery.ToArchetypeChunkArray(Allocator.Temp);
    var prefabRefHandle = GetComponentTypeHandle<PrefabRef>(true);

    foreach (var chunk in chunks)
    {
        var prefabRefs = chunk.GetNativeArray(ref prefabRefHandle);
        for (int i = 0; i < prefabRefs.Length; i++)
        {
            int key = prefabRefs[i].m_Prefab.Index;
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }
    }
    chunks.Dispose();
    return counts;
}
```

**Performance**: Run on a timer (e.g., every 60 seconds), not every frame. Exclude `Owner` and `Controller` to count only top-level placed entities.

## CreationDefinition Prefab Substitution

Substitute the prefab being placed by modifying `CreationDefinition.m_Prefab` during the definition phase. Register in `SystemUpdatePhase.Modification1`:

```csharp
public partial class PrefabSubstitutionSystem : GameSystemBase
{
    private EntityQuery m_CreationQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_CreationQuery = SystemAPI.QueryBuilder()
            .WithAllRW<CreationDefinition, ObjectDefinition>()
            .WithAll<Updated>()
            .WithNone<Deleted, Overridden>()
            .Build();
        RequireForUpdate(m_CreationQuery);
    }

    protected override void OnUpdate()
    {
        var entities = m_CreationQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            var def = EntityManager.GetComponentData<CreationDefinition>(entities[i]);
            def.m_Prefab = GetRandomAlternativePrefab();
            EntityManager.SetComponentData(entities[i], def);

            // Optional: set tree age via ObjectDefinition
            var objDef = EntityManager.GetComponentData<ObjectDefinition>(entities[i]);
            objDef.m_Age = Game.Objects.TreeState.Adult;
            EntityManager.SetComponentData(entities[i], objDef);
        }
        entities.Dispose();
    }
}
```

Subscribe to `ToolSystem.EventToolChanged` to only activate when the right tool is active (see ToolActivation research).

## ActivatePrefabTool

`ToolSystem.ActivatePrefabTool(PrefabBase)` programmatically switches the player to placement mode with a specific prefab:

```csharp
var toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
toolSystem.ActivatePrefabTool(myPrefab);
```

**How it works**: Iterates `ToolSystem.tools`, calling `TrySetPrefab()` on each until one accepts it. **Editor mode caveat**: Set `EditorToolUISystem.activeTool = EditorPrefabTool` first, then use `RegisterUpdater` to defer the `ActivatePrefabTool` call (timing dependency).

## Open Questions

- [ ] How does the asset database load PrefabAsset objects from disk at startup?
- [x] How does `ReplacePrefabSystem` handle entity replacement when prefabs are updated? -- `UpdatePrefab()` replaces the entity. Requires reflection-based `m_PrefabIndices` fixup and `UIGroupElement` buffer cleanup. Cascading `Updated` components signal downstream systems.
- [x] What is the complete list of `ComponentBase` subclasses that mods can attach? — Documented: PlaceableObject, EditorAssetCategoryOverride, ThemeObject, AssetPackItem, ContentPrerequisite, UIObject, PlaceholderObjectData, ObjectGeometryPrefab, and many more. Each prefab type has type-specific ComponentBase subclasses.
- [x] How does the content prerequisite system gate DLC/mod content availability? — `ContentPrerequisite.m_ContentPrerequisite.TryGet<DlcRequirement>()` chains to get the `DlcId` enum value. The prerequisite check runs during `PrefabInitializeSystem`.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key types: PrefabSystem (~930 lines), PrefabBase (~420 lines), ComponentBase (~128 lines), PrefabID (~106 lines), PrefabData, PrefabInitializeSystem (~245 lines), ObjectPrefab, BuildingPrefab, ObjectData
