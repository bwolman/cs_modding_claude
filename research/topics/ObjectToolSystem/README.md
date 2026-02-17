# Research: Object Placement Tool (ObjectToolSystem)

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How standalone objects (buildings, props, trees, decorations) are placed in the game world via `ObjectToolSystem`. Covers the tool's modes, the definition entity pipeline (temporary preview -> permanent placement), snapping, rotation, and the `CreationDefinition`/`ObjectDefinition` component pair.

**Why**: Any mod placing objects programmatically (spawning buildings, scattering props, placing decorations) needs to understand this pipeline. Also needed for mods that modify or intercept object placement behavior.

**Boundaries**: Out of scope -- NetToolSystem road placement (covered in RoadNetwork research), area tools, terrain tools. Building simulation after placement (covered in other topics).

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Tools | ObjectToolSystem, ObjectToolBaseSystem, ToolBaseSystem, CreationDefinition, ObjectDefinition, Temp, ControlPoint, Snap enum |
| Game.dll | Game.Prefabs | PlaceableObjectData, ObjectGeometryData, ObjectPrefab, BuildingData |
| Game.dll | Game.Objects | Transform, Elevation, PlacementFlags |
| Game.dll | Game.Common | Updated, Owner |

## Component Map

### `CreationDefinition` (Game.Tools)

Temporary entity component that defines what to create. Created during the tool's definition phase.

| Field | Type | Description |
|-------|------|-------------|
| m_Prefab | Entity | Prefab entity to instantiate |
| m_SubPrefab | Entity | Sub-prefab (for variations) |
| m_Original | Entity | Original entity (for move/upgrade operations) |
| m_Owner | Entity | Owner entity (parent building, lot, etc.) |
| m_Attached | Entity | Entity to attach to |
| m_Flags | CreationFlags (uint) | Permanent, Select, Delete, Attach, Upgrade, Relocate, Lowered, etc. |
| m_RandomSeed | int | Random seed for variation selection |

*Source: `Game.dll` -> `Game.Tools.CreationDefinition`*

### `ObjectDefinition` (Game.Tools)

Companion to `CreationDefinition`. Specifies the transform and properties of the object to place.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | World position |
| m_LocalPosition | float3 | Local position relative to parent |
| m_Scale | float3 | Scale factor |
| m_Rotation | quaternion | World rotation |
| m_LocalRotation | quaternion | Local rotation relative to parent |
| m_Elevation | float | Elevation offset |
| m_Intensity | float | Intensity (for effects/overlays) |
| m_Age | float | Tree age |
| m_ParentMesh | int | Parent mesh index (-1 if none) |
| m_GroupIndex | int | Group index for sub-objects |
| m_Probability | int | Spawn probability (0-100) |
| m_PrefabSubIndex | int | Index into prefab sub-objects (-1 if none) |

*Source: `Game.dll` -> `Game.Tools.ObjectDefinition`*

### `Temp` (Game.Tools)

Tag component added to preview entities during the tool's preview phase. Entities with `Temp` are visible but not yet committed to the game world.

| Field | Type | Description |
|-------|------|-------------|
| m_Original | Entity | The real entity this temp entity previews (Entity.Null for new objects) |
| m_CurvePosition | float | Curve position for network-attached objects |
| m_Value | int | Generic value (context-dependent) |
| m_Cost | int | Construction cost |
| m_Flags | TempFlags (uint) | Create, Delete, IsLast, Essential, Dragging, Select, Modify, etc. |

*Source: `Game.dll` -> `Game.Tools.Temp`*

### `ControlPoint` (Game.Tools)

Represents a raycast hit point used by tools for positioning.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | Snapped world position |
| m_HitPosition | float3 | Raw raycast hit position |
| m_Direction | float2 | Placement direction |
| m_HitDirection | float3 | Surface normal at hit point |
| m_Rotation | quaternion | Snap-influenced rotation |
| m_OriginalEntity | Entity | Entity that was hit by the raycast |
| m_SnapPriority | float2 | Snap priority values |
| m_ElementIndex | int2 | Element/cell index for grid snapping |
| m_CurvePosition | float | Curve position for edge snapping |
| m_Elevation | float | Elevation at hit point |

*Source: `Game.dll` -> `Game.Tools.ControlPoint`*

### `PlaceableObjectData` (Game.Prefabs)

Prefab component controlling placement behavior.

| Field | Type | Description |
|-------|------|-------------|
| m_PlacementOffset | float3 | Offset from placement point |
| m_ConstructionCost | uint | Cost to place the object |
| m_XPReward | int | XP earned on placement |
| m_DefaultProbability | byte | Default spawn probability |
| m_RotationSymmetry | RotationSymmetry | Any (free rotation disabled), None (full rotation), etc. |
| m_SubReplacementType | SubReplacementType | How sub-objects are replaced |
| m_Flags | PlacementFlags | Placement behavior flags |

*Source: `Game.dll` -> `Game.Prefabs.PlaceableObjectData`*

## Enums

### `ObjectToolSystem.Mode`

```
Create = 0   // Place a single object
Upgrade = 1  // Upgrade an existing object
Move = 2     // Move an existing object
Brush = 3    // Paint objects with brush
Stamp = 4    // Stamp a group of objects
Line = 5     // Place objects along a straight line between two control points
Curve = 6    // Place objects along a curved path defined by three control points
```

**Line mode (5)**: The user places two control points defining the start and end of a straight line. The tool uses `NetCourse` entities (the same system used by `NetToolSystem` for roads) to define the path. `CreateDefinitionsJob` then distributes objects evenly along the line based on the object's size from `ObjectGeometryData`. The spacing between objects equals the object's size along the placement axis, ensuring objects are placed edge-to-edge without overlap. The tool registers the "Place Net Edge" and "Undo Net Control Point" input actions to support this mode.

**Curve mode (6)**: The user places three control points defining a Bezier curve -- start, midpoint, and end. Like Line mode, this uses `NetCourse` entities to represent the curved path. `CreateDefinitionsJob` distributes objects evenly along the curve's arc length, again spacing them based on `ObjectGeometryData` size. Objects are rotated to follow the curve tangent at each placement point. Both Line and Curve modes require the `Brushable` flag on the prefab's `ObjectGeometryData` to be available.

### `CreationFlags`

```
Permanent = 1        // Make entity permanent (commit)
Select = 2           // Select after creation
Delete = 4           // Delete the original
Attach = 8           // Attach to another entity
Upgrade = 0x10       // This is an upgrade
Relocate = 0x20      // Moving an existing object
Invert = 0x40        // Invert direction
Align = 0x80         // Align to surface
Hidden = 0x100       // Hidden creation
Parent = 0x200       // Set parent relationship
Dragging = 0x400     // Created while dragging
Recreate = 0x800     // Recreate existing object
Optional = 0x1000    // Optional sub-object
Lowered = 0x2000     // Object is below grade
Native = 0x4000      // Native/default placement
Construction = 0x8000 // Under construction
SubElevation = 0x10000
Duplicate = 0x20000  // Duplicate existing
Repair = 0x40000     // Repair existing
Stamping = 0x80000   // Part of a stamp operation
```

### `TempFlags`

```
Create = 1       Delete = 2      IsLast = 4
Essential = 8    Dragging = 0x10 Select = 0x20
Modify = 0x40    Regenerate = 0x80
Replace = 0x100  Upgrade = 0x200
Hidden = 0x400   Parent = 0x800
Combine = 0x1000 RemoveCost = 0x2000
Optional = 0x4000 Cancel = 0x8000
SubDetail = 0x10000 Duplicate = 0x20000
```

### `Snap`

Snap modes that control placement snapping behavior:

```
ExistingGeometry = 1   CellLength = 2
StraightDirection = 4  NetSide = 8
NetArea = 0x10         OwnerSide = 0x20
ObjectSide = 0x40      NetMiddle = 0x80
Shoreline = 0x100      NearbyGeometry = 0x200
GuideLines = 0x400     ZoneGrid = 0x800
NetNode = 0x1000       ObjectSurface = 0x2000
Upright = 0x4000       LotGrid = 0x8000
AutoParent = 0x10000   PrefabType = 0x20000
ContourLines = 0x40000 Distance = 0x80000
```

## System Map

### `ObjectToolSystem` (Game.Tools)

The primary tool system for placing standalone objects. Extends `ObjectToolBaseSystem` which extends `ToolBaseSystem`.

- **Base class**: ObjectToolBaseSystem -> ToolBaseSystem -> GameSystemBase
- **Tool ID**: `"Object Tool"`
- **Modes**: Create, Upgrade, Move, Brush, Stamp, Line, Curve
- **Key properties**:
  - `prefab` (ObjectPrefab) -- the selected object prefab to place
  - `mode` (Mode) -- current placement mode
  - `actualMode` (Mode) -- resolved mode (falls back if mode not allowed)
  - `ageMask` (AgeMask) -- tree age selection for tree placement
  - `selectedSnap` (Snap) -- active snap modes

**OnCreate**: Initializes systems, control points, rotation, creates entity queries for definitions/containers/brushes, registers input actions (Erase, Move, Paint, Place, Rotate, etc.)

**Key input actions** (registered in OnCreate):
- "Erase Object", "Move Object", "Paint Object", "Place Object"
- "Place Upgrade", "Precise Rotation", "Rotate Object"
- "Place Net Edge", "Undo Net Control Point"
- "Upgrade Net Edge", "Downgrade Net Edge"

### `ObjectToolBaseSystem` (Game.Tools)

Abstract base class that provides the `CreateDefinitionsJob` -- the core job that creates definition entities from the tool's state.

- **Key job**: `CreateDefinitionsJob` -- creates `CreationDefinition` + `ObjectDefinition` entities based on current control points, selected prefab, and tool mode
- Handles brush mode (scatter objects), stamp mode (place groups), and single object placement

### `ToolBaseSystem` (Game.Tools)

Abstract base for all tools. Provides:
- Raycast integration via `ToolRaycastSystem`
- Apply/Cancel action framework
- Snap mask management (`m_SnapOnMask`, `m_SnapOffMask`)
- Brush settings (size, angle, strength)
- Definition entity lifecycle (create temp -> apply permanent -> destroy definitions)

## Data Flow

```
[User Interaction]
        |
        v
[ObjectToolSystem]
  - Raycast -> ControlPoint
  - Apply snap rules
  - Determine mode (Create/Move/Upgrade/Brush)
        |
        v
[CreateDefinitionsJob]
  - Creates definition entities:
    Entity + CreationDefinition + ObjectDefinition + Updated
  - For brush: multiple entities per frame
  - For create: one entity
        |
        v
[ToolBaseSystem / ToolOutputBarrier]
  - Preview: entities get Temp component (visible but not committed)
  - On Apply: CreationFlags.Permanent set -> entities become real
  - On Cancel: temp entities destroyed
        |
        v
[Game Systems]
  - ObjectGeometrySystem processes new objects
  - LotSystem assigns lots
  - BuildingInitializeSystem initializes buildings
  - SubObjects created for complex buildings
```

### Object Placement Lifecycle

1. **Selection**: User selects a prefab via UI. `ObjectToolSystem.prefab` is set, which configures allowed modes and capabilities based on `ObjectGeometryData` and `PlaceableObjectData`.

2. **Preview**: Each frame while the tool is active, `CreateDefinitionsJob` creates temporary definition entities. The `Temp` component marks them as preview-only. The game renders these as transparent/ghost objects.

3. **Snap**: `SnapJob` adjusts the position to respect active snap modes (grid, surface, existing objects, zone grid, etc.).

4. **Apply**: When the user clicks to confirm, `CreationFlags.Permanent` is added. The tool output barrier processes definition entities and creates real game entities.

5. **Move/Upgrade**: For move operations, `CreationDefinition.m_Original` points to the entity being moved. A delete definition is created for the old position and a create definition for the new position.

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Placement offset | PlaceableObjectData.m_PlacementOffset | Game.Prefabs |
| Construction cost | PlaceableObjectData.m_ConstructionCost | Game.Prefabs |
| Rotation symmetry | PlaceableObjectData.m_RotationSymmetry | Game.Prefabs |
| Object size | ObjectGeometryData.m_Size | Game.Prefabs |
| Brushable flag | ObjectGeometryData.m_Flags (Brushable) | Game.Prefabs |
| Stampable flag | ObjectGeometryData.m_Flags (Stampable) | Game.Prefabs |
| Is tree | TreeData component presence | Game.Prefabs |

## Harmony Patch Points

### Candidate 1: `ObjectToolSystem.prefab` setter

- **Signature**: `void set_prefab(ObjectPrefab value)`
- **Patch type**: Prefix/Postfix
- **What it enables**: Intercept or override prefab selection, modify allowed modes
- **Risk level**: Low
- **Side effects**: UI may need to be kept in sync

### Candidate 2: `ObjectToolBaseSystem.CreateDefinitionsJob` (Burst-compiled)

- **Signature**: `void Execute()` (IJob)
- **Patch type**: **Not patchable** (Burst compiled)
- **Alternative**: Create custom system that modifies definition entities after creation

### Candidate 3: `ToolBaseSystem.GetRaycastResult` or snap-related methods

- **Signature**: Various
- **Patch type**: Prefix/Postfix
- **What it enables**: Override snap behavior, custom placement constraints
- **Risk level**: Medium

### Candidate 4: Post-definition entity modification

- **Signature**: Custom ECS system
- **Patch type**: N/A (custom system approach)
- **What it enables**: Modify `CreationDefinition` / `ObjectDefinition` after tool creates them, before they become permanent
- **Risk level**: Low (clean ECS approach)

## Mod Blueprint

- **Systems to create**:
  - Custom `GameSystemBase` that creates `CreationDefinition` + `ObjectDefinition` entities to place objects programmatically (bypassing the tool UI)
  - Optional system to intercept and modify definition entities from the real tool

- **Components to add**:
  - Custom tag component to identify mod-placed objects

- **Patches needed**:
  - Postfix on `ObjectToolSystem.prefab` setter if controlling tool behavior
  - Or: Pure ECS approach creating definition entities directly

- **Settings**: Object type, position, rotation, snap preferences

## Examples

### Example 1: Placing an Object Programmatically

Create the definition entities directly to place an object without using the tool UI.

```csharp
using Game.Prefabs;
using Game.Tools;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;

public partial class PlaceObjectSystem : GameSystemBase
{
    private PrefabSystem m_PrefabSystem;
    private EndFrameBarrier m_Barrier;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
    }

    public void PlaceObject(PrefabBase prefab, float3 position, quaternion rotation)
    {
        Entity prefabEntity = m_PrefabSystem.GetEntity(prefab);
        EntityCommandBuffer ecb = m_Barrier.CreateCommandBuffer();

        Entity defEntity = ecb.CreateEntity();

        // CreationDefinition tells the system what to create
        ecb.AddComponent(defEntity, new CreationDefinition
        {
            m_Prefab = prefabEntity,
            m_Flags = CreationFlags.Permanent // skip preview, create immediately
        });

        // ObjectDefinition specifies where and how
        ecb.AddComponent(defEntity, new ObjectDefinition
        {
            m_Position = position,
            m_Rotation = rotation,
            m_Probability = 100,
            m_PrefabSubIndex = -1,
            m_ParentMesh = -1
        });

        // Updated tag triggers processing
        ecb.AddComponent(defEntity, default(Updated));
    }

    protected override void OnUpdate() { }
}
```

### Example 2: Monitoring Object Placement

React when objects are placed via the tool by querying for new definition entities with the `Permanent` flag.

```csharp
using Game.Tools;
using Game.Common;
using Unity.Entities;
using Unity.Collections;

public partial class PlacementMonitorSystem : GameSystemBase
{
    private EntityQuery m_DefinitionQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_DefinitionQuery = GetEntityQuery(
            ComponentType.ReadOnly<CreationDefinition>(),
            ComponentType.ReadOnly<ObjectDefinition>(),
            ComponentType.ReadOnly<Updated>()
        );
    }

    protected override void OnUpdate()
    {
        var definitions = m_DefinitionQuery
            .ToComponentDataArray<CreationDefinition>(Allocator.Temp);
        var objects = m_DefinitionQuery
            .ToComponentDataArray<ObjectDefinition>(Allocator.Temp);

        for (int i = 0; i < definitions.Length; i++)
        {
            CreationDefinition def = definitions[i];
            ObjectDefinition obj = objects[i];

            if ((def.m_Flags & CreationFlags.Permanent) != 0
                && (def.m_Flags & CreationFlags.Delete) == 0)
            {
                // New object being placed
                Entity prefab = def.m_Prefab;
                float3 position = obj.m_Position;
                // Log or process the placement...
            }
        }

        definitions.Dispose();
        objects.Dispose();
    }
}
```

### Example 3: Activating the Object Tool with a Specific Prefab

Programmatically activate the object tool and set a specific prefab.

```csharp
using Game.Tools;
using Game.Prefabs;

public void ActivateObjectTool(PrefabSystem prefabSystem, ToolSystem toolSystem,
    ObjectToolSystem objectToolSystem, string prefabName)
{
    // Find the prefab by name
    if (prefabSystem.TryGetPrefab(new PrefabID(nameof(StaticObjectPrefab), prefabName),
        out PrefabBase prefab))
    {
        // Set the prefab on the tool
        objectToolSystem.prefab = prefab as ObjectPrefab;
        objectToolSystem.mode = ObjectToolSystem.Mode.Create;

        // Activate the tool
        toolSystem.activeTool = objectToolSystem;
    }
}
```

### Example 4: Modifying Object Position Before Placement (ECS System)

Create a system that adjusts object positions in definition entities before they become permanent.

```csharp
using Game.Tools;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;

public partial class SnapToGridSystem : GameSystemBase
{
    private EntityQuery m_TempDefinitionQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TempDefinitionQuery = GetEntityQuery(
            ComponentType.ReadWrite<ObjectDefinition>(),
            ComponentType.ReadOnly<CreationDefinition>(),
            ComponentType.ReadOnly<Temp>() // only temp (preview) entities
        );
    }

    protected override void OnUpdate()
    {
        var entities = m_TempDefinitionQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            ObjectDefinition obj = EntityManager
                .GetComponentData<ObjectDefinition>(entities[i]);

            // Snap to a custom 8m grid
            float gridSize = 8f;
            obj.m_Position.x = math.round(obj.m_Position.x / gridSize) * gridSize;
            obj.m_Position.z = math.round(obj.m_Position.z / gridSize) * gridSize;

            EntityManager.SetComponentData(entities[i], obj);
        }

        entities.Dispose();
    }
}
```

### Example 5: Reading Tool State

Check what the ObjectToolSystem is currently doing.

```csharp
using Game.Tools;

public void CheckToolState(ToolSystem toolSystem)
{
    if (toolSystem.activeTool is ObjectToolSystem objectTool)
    {
        ObjectToolSystem.Mode currentMode = objectTool.actualMode;
        ObjectPrefab selectedPrefab = objectTool.prefab;

        if (selectedPrefab != null)
        {
            string prefabName = selectedPrefab.name;
            // Tool is active with a specific prefab selected
        }

        // Check if in upgrade mode
        if (objectTool.isUpgradeMode)
        {
            // Object tool is upgrading existing objects
        }
    }
}
```

## Tree/Vegetation Tool Properties

### `ObjectToolSystem.allowAge` (bool)

Controls whether the tree age slider appears in the tool UI panel. Set automatically:
- **true** when the selected prefab has a `TreeData` component AND the game is in Game mode (not editor)
- **false** otherwise

The property has a public getter but private setter. Tree_Controller uses reflection to force it on for street trees:

```csharp
// Force age slider for street trees (owned by network edges)
if (placingStreetTrees && !m_ObjectToolSystem.allowAge)
{
    m_ObjectToolSystem.SetMemberValue("allowAge", true);
}
```

### `ToolBaseSystem.brushStrength` (float)

Public get/set property on the base tool class. Defaults to `0.5f` in ObjectToolSystem. Controls the placement density/intensity when using brush mode (mode 4).

Tree_Controller patches `ToolUISystem.SetBrushStrength` to allow values above 1.0 (100%) for faster vegetation scattering:

```csharp
[HarmonyPatch(typeof(ToolUISystem), "SetBrushStrength")]
public static class SetBrushStrengthPatch
{
    static void Postfix(ToolSystem ___m_ToolSystem)
    {
        // Allow up to 300% brush strength for vegetation
        if (___m_ToolSystem.activeTool.brushStrength >= 1.0f)
            ___m_ToolSystem.activeTool.brushStrength = 3.0f;
    }
}
```

## Open Questions

- [ ] How does the `ToolOutputBarrier` process definition entities to create real game entities? The barrier likely uses an `EntityCommandBuffer` pattern but the exact creation pipeline is unclear.
- [ ] How are sub-objects (e.g., building props, lot decorations) created as part of a building placement? The `CreateDefinitionsJob` handles `SubPrefab` but the full sub-object pipeline needs tracing.
- [ ] What is the exact snap priority resolution when multiple snap modes conflict?
- [ ] How does brush mode determine density and distribution of scattered objects? The `BrushIterator` uses opacity maps but the algorithm is complex.
- [ ] How does the move operation handle attached/child objects? The `Relocate` flag is set but the re-parenting logic needs investigation.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key namespaces: Game.Tools, Game.Prefabs, Game.Objects, Game.Common
- Types decompiled: ObjectToolSystem (~4400 lines), ObjectToolBaseSystem, ToolBaseSystem, CreationDefinition, ObjectDefinition, Temp, ControlPoint, PlaceableObjectData, CreationFlags, TempFlags, Snap
