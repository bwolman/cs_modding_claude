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

## Open Questions

- [ ] How does the asset database load PrefabAsset objects from disk at startup?
- [ ] How does `ReplacePrefabSystem` handle entity replacement when prefabs are updated?
- [ ] What is the complete list of `ComponentBase` subclasses that mods can attach?
- [ ] How does the content prerequisite system gate DLC/mod content availability?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key types: PrefabSystem (~930 lines), PrefabBase (~420 lines), ComponentBase (~128 lines), PrefabID (~106 lines), PrefabData, PrefabInitializeSystem (~245 lines), ObjectPrefab, BuildingPrefab, ObjectData
