# Research: Tool Raycast & Entity Selection

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 casts rays from the camera through the mouse cursor to detect entities in the world, and how the tool system uses those results to highlight and select entities.

**Why**: A mod that needs to let the player click on entities (vehicles, buildings, citizens, etc.) needs to understand the raycast pipeline, how to configure what types of entities are hit, and how the selection result flows through the tool system.

**Boundaries**: Out of scope — the tool *apply* pipeline (placing/modifying entities), zone raycasting details, and the rendering/shader side of highlighting.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Common | RaycastSystem, RaycastInput, RaycastHit, RaycastResult, RaycastFlags, TypeMask, CollisionMask |
| Game.dll | Game.Tools | ToolSystem, ToolBaseSystem, ToolRaycastSystem, DefaultToolSystem, SelectionToolSystem, SelectedUpdateSystem, ControlPoint, Highlighted, Temp, CreationDefinition, CreationFlags, TempFlags, ApplyMode, SelectionType, SelectionInfo, SelectionElement |
| Game.dll | Game.Rendering | CameraUpdateSystem (provides camera/viewer) |
| Game.dll | Game.Input | InputManager (mouse position) |

## Component Map

### `RaycastInput` (Game.Common)

Configuration struct passed to `RaycastSystem.AddInput()` to define what a ray should hit.

| Field | Type | Description |
|-------|------|-------------|
| m_Line | Line3.Segment | Ray from camera origin to far clip plane |
| m_Offset | float3 | Offset applied to entity bounds during intersection test |
| m_Owner | Entity | Owner identifier for retrieving results |
| m_TypeMask | TypeMask | Which entity categories to test (flags) |
| m_Flags | RaycastFlags | Behavior flags (disable, sub-elements, markers, etc.) |
| m_CollisionMask | CollisionMask | Ground/underground filtering |
| m_NetLayerMask | Layer | Network layer filter (Road, Track, etc.) |
| m_AreaTypeMask | AreaTypeMask | Area type filter (Districts, Lots, etc.) |
| m_RouteType | RouteType | Route filter |
| m_TransportType | TransportType | Transport filter |
| m_IconLayerMask | IconLayerMask | Icon layer filter |
| m_UtilityTypeMask | UtilityTypes | Utility type filter (power, water, etc.) |

*Source: `Game.dll` → `Game.Common.RaycastInput`*

### `RaycastHit` (Game.Common)

Result of a single ray-entity intersection.

| Field | Type | Description |
|-------|------|-------------|
| m_HitEntity | Entity | The entity that was hit |
| m_Position | float3 | Snapped/logical position (may differ from hit position) |
| m_HitPosition | float3 | Exact world-space hit point |
| m_HitDirection | float3 | Surface normal at hit point |
| m_CellIndex | int2 | Cell index (for zone/terrain raycasts) |
| m_NormalizedDistance | float | 0–1 distance along ray (used for closest-hit sorting) |
| m_CurvePosition | float | Position along curve (for net edges) |

*Source: `Game.dll` → `Game.Common.RaycastHit`*

### `RaycastResult` (Game.Common)

Accumulated best-hit result. Implements `IAccumulable` — multiple jobs write candidates and the closest hit wins.

| Field | Type | Description |
|-------|------|-------------|
| m_Hit | RaycastHit | The winning hit |
| m_Owner | Entity | Owner entity (terrain entity for terrain hits, the hit entity's owner for objects) |

Accumulation rule: keeps the hit with the smallest `m_NormalizedDistance`. Ties broken by entity index.

*Source: `Game.dll` → `Game.Common.RaycastResult`*

### `ControlPoint` (Game.Tools)

Tool-level wrapper around a raycast hit. Created from `RaycastHit` via constructor.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | Logical position (from RaycastHit.m_Position) |
| m_HitPosition | float3 | Exact hit position |
| m_Direction | float2 | Direction hint (set by snap jobs) |
| m_HitDirection | float3 | Surface normal |
| m_Rotation | quaternion | Orientation (identity by default) |
| m_OriginalEntity | Entity | The raycast entity |
| m_SnapPriority | float2 | Snap priority (set by snap jobs) |
| m_ElementIndex | int2 | Cell/element index |
| m_CurvePosition | float | Curve position for net edges |
| m_Elevation | float | Elevation offset |

*Source: `Game.dll` → `Game.Tools.ControlPoint`*

### `Highlighted` (Game.Tools)

Empty marker component. Added to entities that the tool system wants to visually highlight (hover effect). The rendering system reads this.

*Source: `Game.dll` → `Game.Tools.Highlighted`*

### `Temp` (Game.Tools)

Marker component on temporary preview entities created by tools.

| Field | Type | Description |
|-------|------|-------------|
| m_Original | Entity | The real entity this temp represents |
| m_CurvePosition | float | Curve position |
| m_Value | int | Value delta |
| m_Cost | int | Cost delta |
| m_Flags | TempFlags | State flags (Create, Delete, Select, Dragging, etc.) |

*Source: `Game.dll` → `Game.Tools.Temp`*

### `CreationDefinition` (Game.Tools)

Command component placed on definition entities to request creation/selection of game entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Prefab | Entity | Prefab to create |
| m_SubPrefab | Entity | Sub-prefab |
| m_Original | Entity | Original entity (for select/move operations) |
| m_Owner | Entity | Owner entity |
| m_Attached | Entity | Attached entity |
| m_Flags | CreationFlags | Operation flags (Select, Delete, Upgrade, etc.) |
| m_RandomSeed | int | Random seed |

*Source: `Game.dll` → `Game.Tools.CreationDefinition`*

### `SelectionElement` (Game.Tools)

Buffer element storing a selected entity reference. Used by `SelectionToolSystem`.

| Field | Type | Description |
|-------|------|-------------|
| m_Entity | Entity | The selected entity |

*Source: `Game.dll` → `Game.Tools.SelectionElement`*

### `SelectionInfo` (Game.Tools)

Component on the selection tracking entity.

| Field | Type | Description |
|-------|------|-------------|
| m_SelectionType | SelectionType | None, ServiceDistrict, or MapTiles |
| m_AreaType | AreaType | Corresponding area type |

*Source: `Game.dll` → `Game.Tools.SelectionInfo`*

## Enum Reference

### `TypeMask` (Game.Common) — What categories to raycast against

| Value | Hex | Description |
|-------|-----|-------------|
| Terrain | 0x1 | Ground surface |
| StaticObjects | 0x2 | Buildings, props, trees |
| MovingObjects | 0x4 | Vehicles, citizens, animals |
| Net | 0x8 | Roads, tracks, power lines |
| Zones | 0x10 | Zoning cells |
| Areas | 0x20 | Districts, lots, surfaces |
| RouteWaypoints | 0x40 | Transport route waypoints |
| RouteSegments | 0x80 | Transport route segments |
| Labels | 0x100 | Text labels |
| Water | 0x200 | Water surface |
| Icons | 0x400 | Notification icons |
| WaterSources | 0x800 | Water sources |
| Lanes | 0x1000 | Individual lanes |

### `RaycastFlags` (Game.Common) — Behavior modifiers

| Value | Hex | Description |
|-------|-----|-------------|
| DebugDisable | 0x1 | Debug disable |
| UIDisable | 0x2 | Disabled when mouse is over UI |
| ToolDisable | 0x4 | Disabled during full update |
| FreeCameraDisable | 0x8 | Disabled in free camera |
| SubElements | 0x20 | Include sub-elements |
| Markers | 0x80 | Include markers |
| OutsideConnections | 0x400 | Include outside connections |
| Decals | 0x4000 | Include decals |
| SubBuildings | 0x10000 | Include sub-buildings |
| BuildingLots | 0x40000 | Include building lots |

### `CollisionMask` (Game.Common) — Vertical layer filtering

| Value | Description |
|-------|-------------|
| OnGround | 0x1 — Ground level |
| Overground | 0x2 — Above ground (elevated) |
| Underground | 0x4 — Below ground (tunnels, pipes) |
| ExclusiveGround | 0x8 — Exclusive ground |

### `TempFlags` (Game.Tools) — Key flags for selection

| Value | Hex | Description |
|-------|-----|-------------|
| Select | 0x20 | Entity is selected/highlighted |
| Dragging | 0x10 | Entity is being dragged |
| Create | 0x1 | New entity being created |
| Delete | 0x2 | Entity being deleted |

### `CreationFlags` (Game.Tools) — Key flags

| Value | Hex | Description |
|-------|-----|-------------|
| Select | 0x2 | Mark for selection highlighting |
| Parent | 0x200 | Parent entity flag |
| Duplicate | 0x20000 | Duplicate flag |
| Dragging | 0x400 | Being dragged |

## System Map

### `RaycastSystem` (Game.Common)

The core raycast engine. Receives raycast requests from multiple consumers, executes them in parallel using Burst jobs, and returns closest-hit results.

- **Base class**: GameSystemBase
- **Update phase**: Called during ToolSystem's update via `SystemUpdatePhase.Raycast`
- **Key methods**:
  - `AddInput(object context, RaycastInput input)` — Register a raycast request. Context is the requesting system.
  - `GetResult(object context)` — Retrieve results for a given context. Returns `NativeArray<RaycastResult>`.
  - `OnUpdate()` — Triggers `SystemUpdatePhase.Raycast`, then schedules parallel raycast jobs.
- **Jobs**:
  - `FindEntitiesFromTreeJob` — Queries the spatial quad tree to find candidate entities whose bounds intersect the ray
  - `DequeEntitiesJob` — Transfers candidates from parallel queue to list
  - `RaycastTerrainJob` — Tests ray against terrain heightmap and water surface
  - `RaycastWaterSourcesJob` — Tests ray against water source entities
  - `RaycastResultJob` — Accumulates per-entity results into final best-hit per raycast input
- **Spatial structure**: Uses `NativeQuadTree<Entity, QuadTreeBoundsXZ>` for broad-phase entity lookup

### `ToolRaycastSystem` (Game.Tools)

Bridge between the tool system and `RaycastSystem`. Each frame, it constructs a `RaycastInput` from the active tool's configuration and the mouse position, submits it to `RaycastSystem`, and exposes results.

- **Base class**: GameSystemBase
- **Update phase**: PreTool (runs before tool update)
- **Properties** (set by tools via `InitializeRaycast`):
  - `raycastFlags`, `typeMask`, `collisionMask`, `netLayerMask`, `areaTypeMask`, `routeType`, `transportType`, `iconLayerMask`, `utilityTypeMask`, `rayOffset`, `owner`
- **Key methods**:
  - `GetRaycastResult(out RaycastResult result)` — Returns true if a valid hit exists
  - `CalculateRaycastLine(Camera mainCamera)` — Converts mouse position to a world-space ray segment
  - `OnUpdate()` — Calls `activeTool.InitializeRaycast()`, builds `RaycastInput`, submits to `RaycastSystem`
- **Input blocking**: Automatically adds `UIDisable` flag when mouse is over UI, `ToolDisable` during full updates

### `ToolSystem` (Game.Tools)

Central orchestrator for all tools. Manages which tool is active, coordinates update phases.

- **Base class**: GameSystemBase
- **Key properties**:
  - `activeTool` — The currently active `ToolBaseSystem`. Setting this fires `EventToolChanged`.
  - `selected` — The currently selected entity (Entity). Set by tools when user clicks.
  - `selectedIndex` — Index within the selected entity (default -1).
  - `actionMode` — Current game mode.
- **Key methods**:
  - `OnUpdate()` — Runs `PreTool` → `ToolUpdate` → `PostTool` update phases
  - `ActivatePrefabTool(PrefabBase prefab)` — Switches to the appropriate tool for a prefab
- **Events**:
  - `EventToolChanged` — Fired when active tool changes
  - `EventPrefabChanged` — Fired when active prefab changes
- **Input**: Manages tool action barriers — blocks tool input when mouse is over UI (keyboard+mouse scheme)

### `ToolBaseSystem` (Game.Tools) — Abstract base for all tools

- **Base class**: GameSystemBase (abstract)
- **Key properties**:
  - `toolID` — String identifier (abstract)
  - `applyMode` — Current apply mode (None, Apply, Clear)
  - `selectedSnap` — Snap mask
- **Key methods**:
  - `InitializeRaycast()` — Called each frame by `ToolRaycastSystem.OnUpdate()`. Resets all raycast parameters to defaults, then the override configures what the tool needs. **This is the primary configuration point.**
  - `GetRaycastResult(out Entity entity, out RaycastHit hit)` — Convenience wrapper around `ToolRaycastSystem.GetRaycastResult()`
  - `GetRaycastResult(out ControlPoint controlPoint)` — Same but returns a `ControlPoint`
  - `OnUpdate(JobHandle)` — Tool-specific per-frame logic (virtual)
- **Default raycast config** (set in base `InitializeRaycast`):
  - `typeMask = TypeMask.None` (tools must opt in)
  - `collisionMask = OnGround | Overground`
  - All layer/type masks cleared
  - Flags cleared except disable flags

### `DefaultToolSystem` (Game.Tools)

The "pointer" tool — active when no placement/construction tool is selected. Handles hovering, clicking to select, and dragging.

- **Base class**: ToolBaseSystem
- **toolID**: `"Default Tool"`
- **States**: Default → MouseDownPrepare → MouseDown → Dragging
- **InitializeRaycast override**:
  - Default state: `TypeMask.StaticObjects | MovingObjects | Labels | Icons` + `RaycastFlags.OutsideConnections | Decals | BuildingLots` + `IconLayerMask.Default`
  - Non-default (when underground): `CollisionMask.Underground`
  - Dragging state: `TypeMask.Terrain | Net`, `netLayerMask = Layer.Road` (drag along ground/roads)
  - Debug mode: adds `TypeMask.Net`, `SubElements`, all net layers
  - Editor mode: adds `SubElements | Placeholders | Markers | EditorContainers`
- **Selection flow**:
  1. `Update()` in Default state: calls `GetRaycastResult()` → if entity changed, creates `CreationDefinition` entities with `CreationFlags.Select` via `UpdateDefinitions()` → downstream systems create `Temp` entities with `TempFlags.Select` → rendering highlights them
  2. `Apply()`: user clicks → calls `SelectTempEntity()` which scans `Temp` entities for `TempFlags.Select` → sets `ToolSystem.selected` to the original entity
  3. Icon resolution: if selected entity is an `Icon`, follows `Target` to find the real entity. If it has `Owner`, walks up to find the Vehicle or Building.
- **Key methods**:
  - `UpdateDefinitions(inputDeps, entity, index, position, setPosition)` — Creates `CreationDefinition` via Burst `CreateDefinitionsJob`
  - `SelectTempEntity(inputDeps, toggleSelected)` — Scans temp entities, resolves selection, plays sound

### `SelectedUpdateSystem` (Game.Tools)

Cleanup system that keeps `ToolSystem.selected` valid. Runs each frame.

- If selected entity no longer exists → clears selection
- If selected entity is `Deleted`:
  - If it's an `Icon` with `Owner`, follows owner
  - If it's a `Resident`, follows to `Citizen` entity
  - If it's a `Pet`, follows to `HouseholdPet` entity
  - If still deleted → clears selection

### `SelectionToolSystem` (Game.Tools)

Specialized tool for area-based multi-selection (map tiles, service districts). Not the primary entity selection tool.

- **toolID**: `"Selection Tool"`
- **States**: Default, Selecting, Deselecting
- **SelectionType**: None, ServiceDistrict, MapTiles
- **Uses**: Area search via `SearchSystem`, `SelectionElement` buffer for tracked selections
- **Not used for**: Single-entity click selection (that's `DefaultToolSystem`)

## Data Flow

```
[Mouse Position (InputManager.instance.mousePosition)]
    │
    ▼
[ToolRaycastSystem.OnUpdate()]
    │  Calls activeTool.InitializeRaycast() → tool configures typeMask, flags, etc.
    │  Calls CalculateRaycastLine(camera) → Line3.Segment from mouse through world
    │  Builds RaycastInput and calls RaycastSystem.AddInput()
    │
    ▼
[RaycastSystem.OnUpdate()]
    │  FindEntitiesFromTreeJob — broad-phase quad tree query for candidate entities
    │  Per-entity raycast jobs — precise intersection tests against meshes/bounds
    │  RaycastTerrainJob — terrain heightmap intersection
    │  RaycastResultJob — accumulates closest hit per input
    │
    ▼
[ToolRaycastSystem.GetRaycastResult(out RaycastResult)]
    │  Returns closest RaycastHit with entity reference
    │
    ▼
[ToolBaseSystem.GetRaycastResult(out Entity, out RaycastHit)]
    │  Filters out Deleted entities
    │
    ▼
[DefaultToolSystem.OnUpdate() / Update()]
    │  Compares with m_LastRaycastEntity
    │  If changed → CreateDefinitionsJob creates CreationDefinition with CreationFlags.Select
    │
    ▼
[Generate*Systems (GenerateObjectsSystem, etc.)]
    │  Process CreationDefinitions → create Temp entities with TempFlags.Select
    │
    ▼
[Rendering system reads Temp + TempFlags.Select → highlights entity]
    │
    ▼
[User clicks → DefaultToolSystem.Apply()]
    │  SelectTempEntity scans Temp entities for TempFlags.Select
    │  Resolves icons → owners → sets ToolSystem.selected
    │
    ▼
[ToolSystem.selected = entity]
    │  UI system reads this to show info panel
    │
    ▼
[SelectedUpdateSystem.OnUpdate()]
    │  Validates selection still exists, follows entity references if deleted
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| DefaultToolSystem TypeMask | Hardcoded | DefaultToolSystem.InitializeRaycast() |
| CollisionMask defaults | Hardcoded | ToolBaseSystem.InitializeRaycast() — OnGround \| Overground |
| Entity bounds for quad tree | From prefab mesh/geometry | Game.Rendering — bounds computed at load |
| Selected sounds | PrefabRef → SelectedSoundData | Game.Prefabs.SelectedSoundData on entity prefabs |

No prefab-based configuration for raycast filtering — it's all code-driven per tool.

## Harmony Patch Points

### Candidate 1: `Game.Tools.DefaultToolSystem.InitializeRaycast()`

- **Signature**: `public override void InitializeRaycast()`
- **Patch type**: Postfix
- **What it enables**: Modify the raycast configuration after DefaultToolSystem sets it up. Add or remove TypeMask flags, change CollisionMask, add RaycastFlags. For example, add `TypeMask.Net` to let the default tool hover over roads, or restrict to `TypeMask.MovingObjects` only.
- **Risk level**: Low
- **Side effects**: Changes what entities highlight when hovering. Non-destructive.

### Candidate 2: `Game.Tools.ToolBaseSystem.InitializeRaycast()`

- **Signature**: `public virtual void InitializeRaycast()`
- **Patch type**: Postfix
- **What it enables**: Globally modify raycast config for ALL tools. Broad but risky.
- **Risk level**: Medium — affects every tool
- **Side effects**: Could break placement tools, bulldoze, etc. if masks are wrong

### Candidate 3: `Game.Tools.ToolBaseSystem.GetRaycastResult(out Entity, out RaycastHit)`

- **Signature**: `protected bool GetRaycastResult(out Entity entity, out RaycastHit hit)`
- **Patch type**: Postfix
- **What it enables**: Intercept/filter raycast results. Can reject certain entities, redirect to different entities, or capture the result for mod logic without affecting the tool.
- **Risk level**: Low (postfix read-only) / Medium (if modifying results)
- **Side effects**: Postfix can safely read without side effects. Modifying `entity` or `hit` via ref could confuse tool logic.

### Candidate 4: `Game.Tools.ToolSystem.selected` (setter)

- **Signature**: `public Entity selected { set; }`
- **Patch type**: Postfix
- **What it enables**: React when the player selects any entity. Fire mod events, show custom UI, trigger mod actions based on what was selected.
- **Risk level**: Low
- **Side effects**: None if read-only. Setting `selected` to a different value could confuse UI.

### Candidate 5: Custom `ToolBaseSystem` subclass (no patch needed)

- **Signature**: Override `InitializeRaycast()` and `OnUpdate(JobHandle)`
- **Patch type**: None — extend via ECS
- **What it enables**: Create a fully custom tool with its own raycast configuration, selection behavior, and UI. Activated via `ToolSystem.activeTool = myCustomTool`.
- **Risk level**: Low — isolated from vanilla tools
- **Side effects**: Must handle tool switching, action bindings, and cleanup properly.

## Mod Blueprint

### Approach A: Read-only — React to vanilla selection

For mods that just need to know what the player clicked on:

- **Systems to create**: One `GameSystemBase` that reads `ToolSystem.selected` each frame
- **Components to add**: None
- **Patches needed**: None (or Postfix on `ToolSystem.selected` setter for event-driven approach)
- **Settings**: None
- **UI changes**: Custom info panel or overlay based on selected entity

### Approach B: Custom raycast configuration — Modify what's selectable

For mods that need to change which entity types are hoverable/selectable:

- **Systems to create**: None
- **Components to add**: None
- **Patches needed**: Postfix on `DefaultToolSystem.InitializeRaycast()` to modify `m_ToolRaycastSystem` properties
- **Settings**: User-configurable TypeMask flags
- **UI changes**: None

### Approach C: Programmatic raycast — Query from mod code

For mods that need to raycast independently (e.g., from a hotkey, not the mouse):

- **Systems to create**: One `GameSystemBase` that calls `RaycastSystem.AddInput()` with a custom `RaycastInput`, then reads results via `RaycastSystem.GetResult()` on the next frame
- **Components to add**: None
- **Patches needed**: None
- **Settings**: Configurable ray parameters
- **UI changes**: Depends on use case

### Approach D: Full custom tool

For mods that need a completely custom selection experience:

- **Systems to create**: Subclass `ToolBaseSystem`, override `InitializeRaycast()`, `OnUpdate()`, `GetPrefab()`, `TrySetPrefab()`, `toolID`
- **Components to add**: Optional custom components for selection state
- **Patches needed**: None
- **Settings**: Tool activation keybind
- **UI changes**: Tool UI, activation button
- **Activation**: `World.GetOrCreateSystemManaged<ToolSystem>().activeTool = myTool`

### Code Example: Postfix to read DefaultToolSystem hover

```csharp
[HarmonyPatch(typeof(Game.Tools.DefaultToolSystem), "InitializeRaycast")]
public static class DefaultToolRaycastPatch
{
    public static void Postfix(DefaultToolSystem __instance)
    {
        // Access the ToolRaycastSystem via reflection or Traverse
        // to read/modify raycast configuration
        var trs = Traverse.Create(__instance).Field("m_ToolRaycastSystem").GetValue<ToolRaycastSystem>();

        // Example: Also allow raycasting moving objects even underground
        trs.typeMask |= TypeMask.MovingObjects;
    }
}
```

### Code Example: Reading ToolSystem.selected

```csharp
public class MySelectionReaderSystem : GameSystemBase
{
    private ToolSystem _toolSystem;
    private Entity _lastSelected;

    protected override void OnCreate()
    {
        base.OnCreate();
        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
    }

    protected override void OnUpdate()
    {
        Entity current = _toolSystem.selected;
        if (current != _lastSelected)
        {
            _lastSelected = current;
            if (current != Entity.Null)
            {
                // Entity was selected — read components, show UI, etc.
                Mod.Log.Info($"Selected entity: {current.Index}:{current.Version}");
            }
        }
    }
}
```

### Code Example: Programmatic raycast

```csharp
public class MyRaycastSystem : GameSystemBase
{
    private RaycastSystem _raycastSystem;
    private CameraUpdateSystem _cameraUpdateSystem;
    private bool _pendingResult;

    protected override void OnCreate()
    {
        base.OnCreate();
        _raycastSystem = World.GetOrCreateSystemManaged<RaycastSystem>();
        _cameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
    }

    protected override void OnUpdate()
    {
        if (_pendingResult)
        {
            NativeArray<RaycastResult> results = _raycastSystem.GetResult(this);
            if (results.Length > 0 && results[0].m_Owner != Entity.Null)
            {
                RaycastResult result = results[0];
                // Process hit: result.m_Hit.m_HitEntity, result.m_Hit.m_Position, etc.
            }
            _pendingResult = false;
        }

        // Submit new raycast (e.g., triggered by hotkey)
        if (ShouldRaycast() && _cameraUpdateSystem.TryGetViewer(out var viewer))
        {
            RaycastInput input = new RaycastInput
            {
                m_Line = ToolRaycastSystem.CalculateRaycastLine(viewer.camera),
                m_TypeMask = TypeMask.StaticObjects | TypeMask.MovingObjects,
                m_CollisionMask = CollisionMask.OnGround | CollisionMask.Overground,
                // Leave other masks at default (none/zero)
            };
            _raycastSystem.AddInput(this, input);
            _pendingResult = true;
        }
    }
}
```

## Examples

### Example 1: Custom tool that raycasts buildings and vehicles only

This shows how to create a custom `ToolBaseSystem` subclass that configures its own raycast to only hit static objects (buildings) and moving objects (vehicles/citizens). The `InitializeRaycast` override is called every frame by `ToolRaycastSystem.OnUpdate()` before the ray is cast.

```csharp
using Game.Common;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

public class MyEntityInspectorTool : ToolBaseSystem
{
    private Entity _lastHitEntity;

    public override string toolID => "My Entity Inspector";

    public override PrefabBase GetPrefab() => null;
    public override bool TrySetPrefab(PrefabBase prefab) => false;

    /// <summary>
    /// Called every frame by ToolRaycastSystem before the raycast executes.
    /// The base class resets all masks to None/defaults. We then configure
    /// exactly what entity types our tool should be able to hit.
    /// </summary>
    public override void InitializeRaycast()
    {
        // Always call base first — it resets typeMask to None, collisionMask
        // to OnGround | Overground, and clears all layer/flag masks.
        base.InitializeRaycast();

        // Set which entity categories the ray should test against.
        // StaticObjects = buildings, props, trees.
        // MovingObjects = vehicles, citizens, animals.
        m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.MovingObjects;

        // Only hit ground-level and above-ground entities (skip underground).
        m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;

        // Include outside connections (e.g., highway entry/exit buildings).
        m_ToolRaycastSystem.raycastFlags |= RaycastFlags.OutsideConnections;
    }

    [Preserve]
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // GetRaycastResult is a convenience method from ToolBaseSystem.
        // It calls ToolRaycastSystem.GetRaycastResult(), filters out Deleted
        // entities, and returns the hit entity + hit details.
        if (GetRaycastResult(out Entity entity, out RaycastHit hit))
        {
            if (entity != _lastHitEntity)
            {
                _lastHitEntity = entity;
                Mod.Log.Info($"Hovering entity {entity.Index}:{entity.Version} " +
                             $"at position ({hit.m_HitPosition.x:F1}, {hit.m_HitPosition.y:F1}, {hit.m_HitPosition.z:F1})");
            }
        }
        else
        {
            _lastHitEntity = Entity.Null;
        }

        return inputDeps;
    }
}
```

### Example 2: Activating a custom tool

Tools are activated by setting `ToolSystem.activeTool`. The default tool (`DefaultToolSystem`) is always the fallback when no other tool is active.

```csharp
using Game.Tools;
using Unity.Entities;

public class MyToolActivator : GameSystemBase
{
    private ToolSystem _toolSystem;
    private MyEntityInspectorTool _inspectorTool;

    protected override void OnCreate()
    {
        base.OnCreate();
        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
        _inspectorTool = World.GetOrCreateSystemManaged<MyEntityInspectorTool>();
    }

    protected override void OnUpdate()
    {
        // Activate the custom tool (e.g., triggered by a keybind).
        // ToolSystem fires EventToolChanged when the active tool changes.
        // While active, ToolRaycastSystem will call our tool's InitializeRaycast()
        // each frame to configure the ray before casting.
        _toolSystem.activeTool = _inspectorTool;

        // To deactivate and return to the default pointer tool:
        // _toolSystem.activeTool = World.GetOrCreateSystemManaged<DefaultToolSystem>();
    }
}
```

### Example 3: Harmony patch to modify DefaultToolSystem raycast configuration

Instead of creating a custom tool, you can patch the default tool's `InitializeRaycast` to change what entities are hoverable with the normal pointer. This postfix runs after `DefaultToolSystem` has set its masks, so you can add or remove flags.

```csharp
using Game.Common;
using Game.Tools;
using HarmonyLib;

[HarmonyPatch(typeof(Game.Tools.DefaultToolSystem), "InitializeRaycast")]
public static class DefaultToolRaycastPatch
{
    /// <summary>
    /// Postfix runs after DefaultToolSystem.InitializeRaycast() has set up
    /// its raycast configuration on m_ToolRaycastSystem. We can read or
    /// modify the masks before the ray is actually cast.
    /// </summary>
    public static void Postfix(DefaultToolSystem __instance)
    {
        // Access the protected m_ToolRaycastSystem field via Harmony's Traverse.
        var trs = Traverse.Create(__instance)
            .Field("m_ToolRaycastSystem")
            .GetValue<ToolRaycastSystem>();

        // Example: Add network (road) raycasting to the default pointer tool.
        // Normally the default tool only hits StaticObjects | MovingObjects | Labels | Icons.
        trs.typeMask |= TypeMask.Net;
        trs.netLayerMask = Game.Net.Layer.Road | Game.Net.Layer.TrainTrack;

        // Example: Also allow selecting underground entities.
        trs.collisionMask |= CollisionMask.Underground;
    }
}
```

### Example 4: Reading ControlPoint from a raycast hit

`ControlPoint` is a tool-level wrapper around `RaycastHit` that adds snap information. Tools use the `GetRaycastResult(out ControlPoint)` overload when they need positional data for placement or preview.

```csharp
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

public class MyPositionTool : ToolBaseSystem
{
    public override string toolID => "My Position Tool";
    public override PrefabBase GetPrefab() => null;
    public override bool TrySetPrefab(PrefabBase prefab) => false;

    public override void InitializeRaycast()
    {
        base.InitializeRaycast();
        // Hit terrain and static objects so we get a ground position
        // even when not hovering a building.
        m_ToolRaycastSystem.typeMask =
            Game.Common.TypeMask.Terrain | Game.Common.TypeMask.StaticObjects;
    }

    [Preserve]
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // GetRaycastResult with ControlPoint wraps the RaycastHit into
        // a ControlPoint struct via: new ControlPoint(entity, hit).
        if (GetRaycastResult(out ControlPoint controlPoint))
        {
            // controlPoint.m_OriginalEntity — the entity the ray hit
            // controlPoint.m_Position       — logical/snapped position (from RaycastHit.m_Position)
            // controlPoint.m_HitPosition    — exact world-space intersection point
            // controlPoint.m_HitDirection   — surface normal at hit point
            // controlPoint.m_CurvePosition  — position along curve (for net edges)
            Mod.Log.Info($"Hit at ({controlPoint.m_HitPosition.x:F1}, " +
                         $"{controlPoint.m_HitPosition.y:F1}, " +
                         $"{controlPoint.m_HitPosition.z:F1}), " +
                         $"entity: {controlPoint.m_OriginalEntity.Index}");
        }

        return inputDeps;
    }
}
```

### Example 5: Programmatic raycast independent of the tool system

You can submit raycasts directly to `RaycastSystem` without going through the tool pipeline. This is useful for mod logic that needs to query the world from code (e.g., on a hotkey press) without changing the active tool. Note that results are available the frame *after* submission.

```csharp
using Game.Common;
using Game.Rendering;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

public class MyDirectRaycastSystem : GameSystemBase
{
    private RaycastSystem _raycastSystem;
    private CameraUpdateSystem _cameraUpdateSystem;
    private bool _pendingResult;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        _raycastSystem = World.GetOrCreateSystemManaged<RaycastSystem>();
        _cameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        // Check results from the previous frame's raycast.
        if (_pendingResult)
        {
            // GetResult returns results keyed by the context object (this system).
            NativeArray<RaycastResult> results = _raycastSystem.GetResult(this);
            if (results.Length > 0 && results[0].m_Owner != Entity.Null)
            {
                RaycastResult result = results[0];
                Entity hitEntity = result.m_Hit.m_HitEntity;
                float3 hitPos = result.m_Hit.m_HitPosition;
                float3 hitNormal = result.m_Hit.m_HitDirection;
                float distance = result.m_Hit.m_NormalizedDistance;

                Mod.Log.Info($"Direct raycast hit entity {hitEntity.Index} " +
                             $"at ({hitPos.x:F1}, {hitPos.y:F1}, {hitPos.z:F1}), " +
                             $"distance={distance:F3}");
            }
            _pendingResult = false;
        }

        // Submit a new raycast when triggered (e.g., by a hotkey).
        if (ShouldCastRay() && _cameraUpdateSystem.TryGetViewer(out var viewer))
        {
            // CalculateRaycastLine converts the current mouse screen position
            // into a world-space line segment from camera origin to far clip.
            RaycastInput input = new RaycastInput
            {
                m_Line = ToolRaycastSystem.CalculateRaycastLine(viewer.camera),
                m_TypeMask = TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Net,
                m_CollisionMask = CollisionMask.OnGround | CollisionMask.Overground,
                m_NetLayerMask = Game.Net.Layer.Road,
                // Other masks default to None/zero, meaning no filtering.
            };

            // AddInput registers the raycast. RaycastSystem processes it during
            // the Raycast update phase. Results are retrieved via GetResult(this)
            // on the next frame.
            _raycastSystem.AddInput(this, input);
            _pendingResult = true;
        }
    }

    private bool ShouldCastRay()
    {
        // Replace with your trigger logic (keybind, timer, etc.)
        return false;
    }
}
```

### Example 6: Reacting to entity selection via Harmony patch

If your mod just needs to know when the player selects an entity (without changing what is selectable), you can patch `ToolSystem.selected`'s setter. This fires for all tools, not just `DefaultToolSystem`.

```csharp
using Game.Tools;
using HarmonyLib;
using Unity.Entities;

[HarmonyPatch(typeof(Game.Tools.ToolSystem), "selected", MethodType.Setter)]
public static class SelectionChangedPatch
{
    /// <summary>
    /// Fires every time any tool sets ToolSystem.selected, including when
    /// clearing the selection (value == Entity.Null).
    /// </summary>
    public static void Postfix(Entity value)
    {
        if (value != Entity.Null)
        {
            Mod.Log.Info($"Player selected entity: {value.Index}:{value.Version}");
            // Read components from the entity via EntityManager to
            // determine its type and show custom UI, log info, etc.
        }
        else
        {
            Mod.Log.Info("Selection cleared");
        }
    }
}
```

### Example 7: Polling ToolSystem.selected without patching

The simplest approach requires no Harmony patches at all. Create a system that polls `ToolSystem.selected` each frame and reacts when it changes.

```csharp
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

public class SelectionPollingSystem : GameSystemBase
{
    private ToolSystem _toolSystem;
    private Entity _lastSelected;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        Entity current = _toolSystem.selected;
        if (current != _lastSelected)
        {
            _lastSelected = current;
            if (current != Entity.Null)
            {
                // New entity selected. Query its components to determine type:
                // - EntityManager.HasComponent<Game.Buildings.Building>(current) → building
                // - EntityManager.HasComponent<Game.Vehicles.Vehicle>(current) → vehicle
                // - EntityManager.HasComponent<Game.Citizens.Citizen>(current) → citizen
                Mod.Log.Info($"Selection changed to {current.Index}:{current.Version}");
            }
            else
            {
                Mod.Log.Info("Selection cleared");
            }
        }
    }
}
```

## Open Questions

- [x] How does DefaultToolSystem configure the raycast? — Documented in InitializeRaycast override
- [x] What is the entity resolution chain for icons? — Icon → Target → Owner chain documented
- [x] Where does highlighting come from? — CreationDefinition with Select flag → Temp with Select flag → rendering
- [ ] Exact rendering path for Highlighted component vs Temp+Select — likely shader-level, out of scope
- [ ] How does the quad tree get populated (which system inserts entities)? — Likely ObjectUpdateSystem or similar, not traced

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Key types: RaycastSystem, ToolRaycastSystem, ToolSystem, ToolBaseSystem, DefaultToolSystem, SelectionToolSystem, SelectedUpdateSystem
- Game version: Current as of 2026-02-15
