# Research: Road & Network Building

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How CS2's `NetToolSystem` creates road segments, nodes, and lanes -- the full pipeline from player click to persistent ECS entities. How mods can programmatically create, modify, and query road networks.

**Why**: To understand the network building pipeline for mods that need to create roads programmatically, modify existing road geometry, query network topology, or hook into the road building tool.

**Boundaries**: This research covers the core network ECS architecture (Edge, Node, Lane, Curve, Composition), the `NetToolSystem` tool pipeline, and the systems that generate and apply network entities. Pathfinding, traffic simulation, and vehicle movement along lanes are out of scope.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Net | `Edge`, `Node`, `Curve`, `Lane`, `SubLane`, `SubNet`, `Composition`, `Elevation`, `EdgeGeometry`, `NodeGeometry`, `Segment`, `ConnectedEdge`, `ConnectedNode`, `Upgraded`, `Roundabout`, `Gate`, `TrafficLights`, `SubFlow`, `Layer` (enum), `GeometryFlags` (enum) |
| Game.dll | Game.Net | `GeometrySystem`, `LaneSystem`, `LaneConnectionSystem`, `SearchSystem`, `InitializeSystem`, `CompositionSelectSystem`, `AggregateSystem`, `NetComponentsSystem`, `EdgeMappingSystem`, `NetUtils` |
| Colossal.Mathematics.dll | Colossal.Mathematics | `MathUtils` (`Position`, `Tangent`, `Length` for Bezier4x3 operations) |
| Game.dll | Game.Tools | `NetToolSystem`, `NetCourse`, `CoursePos`, `ControlPoint`, `CoursePosFlags`, `ApplyNetSystem`, `GenerateEdgesSystem`, `GenerateNodesSystem`, `ToolBaseSystem` |
| Game.dll | Game.Prefabs | `NetData`, `NetGeometryData`, `NetCompositionData`, `PlaceableNetData`, `CompositionFlags`, `NetCompositionLane`, `RoadData`, `RoadFlags`, `NetGeometryPrefab`, `RoadPrefab`, `TrackPrefab`, `PathwayPrefab`, `FencePrefab`, `NetSectionPrefab`, `NetPieceInfo`, `NetPieceLanes`, `NetSectionInfo`, `NetPieceRequirements`, `NetEdgeStateInfo`, `NetNodeStateInfo`, `AggregateNetPrefab`, `CompositionInvertMode` |

## Architecture Overview

CS2's road network uses a **graph-based ECS model**. Roads are represented as a directed graph of **Nodes** (intersections/endpoints) connected by **Edges** (road segments). Each edge has a **Curve** (Bezier spline) defining its shape, a **Composition** linking to prefab data for its visual/functional properties, and a dynamic buffer of **SubLanes** representing individual traffic lanes, sidewalks, and utility conduits.

The player-facing tool (`NetToolSystem`) does not create final entities directly. Instead, it creates temporary **NetCourse** entities (Bezier curves with start/end positions). These are processed through a multi-stage pipeline:

1. `NetToolSystem` -- collects control points from user input, creates `NetCourse` temp entities
2. `GenerateEdgesSystem` / `GenerateNodesSystem` -- convert courses into temp `Edge`/`Node` entities with `Temp` component
3. `GeometrySystem` -- calculates geometry (bounds, segments, node shapes)
4. `LaneSystem` -- generates lane entities for each edge/node
5. `ApplyNetSystem` -- on tool apply, promotes temp entities to permanent ones (removes `Temp` component, patches references)

## Component Map

### `Edge` (Game.Net)

The fundamental road segment. Connects two `Node` entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Start | Entity | Start node entity |
| m_End | Entity | End node entity |

Edges always have `Curve`, `Composition`, and `EdgeGeometry` components. They also have `DynamicBuffer<SubLane>` for lanes and `DynamicBuffer<ConnectedNode>` for intermediate nodes.

*Source: `Game.dll` -> `Game.Net.Edge`*

### `Node` (Game.Net)

An intersection or endpoint where edges meet.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | World-space position |
| m_Rotation | quaternion | Orientation of the node |

Nodes have `DynamicBuffer<ConnectedEdge>` listing all edges meeting at this node (capacity 4). They also have `NodeGeometry` for bounds and shape data.

*Source: `Game.dll` -> `Game.Net.Node`*

### `Curve` (Game.Net)

Defines the shape of an edge or lane as a cubic Bezier spline.

| Field | Type | Description |
|-------|------|-------------|
| m_Bezier | Bezier4x3 | Cubic Bezier curve (4 control points in 3D) |
| m_Length | float | Cached arc length (recomputed on deserialization) |

*Source: `Game.dll` -> `Game.Net.Curve`*

### `Composition` (Game.Net)

Links an edge to its prefab composition data (which defines lane layout, visual appearance, and flags).

| Field | Type | Description |
|-------|------|-------------|
| m_Edge | Entity | Composition prefab entity for the edge section |
| m_StartNode | Entity | Composition prefab entity for the start node |
| m_EndNode | Entity | Composition prefab entity for the end node |

*Source: `Game.dll` -> `Game.Net.Composition`*

### `Lane` (Game.Net)

Represents a single traffic lane, sidewalk, or utility conduit within an edge.

| Field | Type | Description |
|-------|------|-------------|
| m_StartNode | PathNode | Pathfinding start node |
| m_MiddleNode | PathNode | Pathfinding middle node |
| m_EndNode | PathNode | Pathfinding end node |

Lanes also have their own `Curve` component and are referenced via `DynamicBuffer<SubLane>` on the parent edge.

*Source: `Game.dll` -> `Game.Net.Lane`*

### `SubLane` (Game.Net)

Buffer element on edges listing child lane entities.

| Field | Type | Description |
|-------|------|-------------|
| m_SubLane | Entity | The lane entity |
| m_PathMethods | PathMethod | Which pathfinding methods can use this lane |

*Source: `Game.dll` -> `Game.Net.SubLane`*

### `ConnectedEdge` (Game.Net)

Buffer element on nodes listing connected edges. Internal buffer capacity of 4 (typical intersection).

| Field | Type | Description |
|-------|------|-------------|
| m_Edge | Entity | The connected edge entity |

*Source: `Game.dll` -> `Game.Net.ConnectedEdge`*

### `ConnectedNode` (Game.Net)

Buffer element on edges listing intermediate node connections (for split edges).

| Field | Type | Description |
|-------|------|-------------|
| m_Node | Entity | The connected node entity |
| m_CurvePosition | float | Position along the edge curve (0-1) |

*Source: `Game.dll` -> `Game.Net.ConnectedNode`*

### `Elevation` (Game.Net)

Tracks the elevation offset of an edge relative to terrain.

| Field | Type | Description |
|-------|------|-------------|
| m_Elevation | float2 | Elevation at start (x) and end (y) of the edge |

*Source: `Game.dll` -> `Game.Net.Elevation`*

### `EdgeGeometry` (Game.Net)

Computed geometry for rendering and collision.

| Field | Type | Description |
|-------|------|-------------|
| m_Start | Segment | Left/right Bezier curves and lengths for start half |
| m_End | Segment | Left/right Bezier curves and lengths for end half |
| m_Bounds | Bounds3 | Axis-aligned bounding box |

*Source: `Game.dll` -> `Game.Net.EdgeGeometry`*

### `NodeGeometry` (Game.Net)

Computed geometry for node intersections.

| Field | Type | Description |
|-------|------|-------------|
| m_Bounds | Bounds3 | Axis-aligned bounding box |
| m_Position | float | Height position |
| m_Flatness | float | How flat the node surface is |
| m_Offset | float | Vertical offset |

*Source: `Game.dll` -> `Game.Net.NodeGeometry`*

### `StartNodeGeometry` / `EndNodeGeometry` (Game.Net)

Per-edge components that store the geometry contribution of this edge to its start and end intersection nodes. While `NodeGeometry` lives on the node itself and represents the combined result, these per-edge components describe how each individual edge shapes the intersection.

**`StartNodeGeometry`**:

| Field | Type | Description |
|-------|------|-------------|
| m_Geometry | EdgeNodeGeometry | Geometry data for how this edge connects to its start node |

**`EndNodeGeometry`**:

| Field | Type | Description |
|-------|------|-------------|
| m_Geometry | EdgeNodeGeometry | Geometry data for how this edge connects to its end node |

**`EdgeNodeGeometry`** struct:

| Field | Type | Description |
|-------|------|-------------|
| m_Left | Segment | Left-side Bezier curve and length at the node junction |
| m_Right | Segment | Right-side Bezier curve and length at the node junction |
| m_Middle | Bezier4x3 | Center-line Bezier curve at the node junction |

`GeometrySystem` computes these components for each edge. They are consumed by `LaneSystem` to generate accurate lane curves through intersections and by the rendering pipeline to draw smooth node geometry where multiple edges meet.

*Source: `Game.dll` -> `Game.Net.StartNodeGeometry`, `Game.Net.EndNodeGeometry`, `Game.Net.EdgeNodeGeometry`*

### `Upgraded` (Game.Net)

Marks an edge that has been upgraded (e.g., added lanes, changed surface type).

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | CompositionFlags | The upgrade flags applied |

*Source: `Game.dll` -> `Game.Net.Upgraded`*

### `Roundabout` (Game.Net)

Marks a node as part of a roundabout.

| Field | Type | Description |
|-------|------|-------------|
| m_Radius | float | Roundabout radius |

*Source: `Game.dll` -> `Game.Net.Roundabout`*

### `NetCourse` (Game.Tools)

Temporary component created by `NetToolSystem` to define a road segment being placed.

| Field | Type | Description |
|-------|------|-------------|
| m_StartPosition | CoursePos | Start position with entity reference, elevation, flags |
| m_EndPosition | CoursePos | End position |
| m_Curve | Bezier4x3 | The Bezier curve for this course segment |
| m_Elevation | float2 | Elevation at start and end |
| m_Length | float | Curve length |
| m_FixedIndex | int | Index of fixed control point |

*Source: `Game.dll` -> `Game.Tools.NetCourse`*

### `ControlPoint` (Game.Tools)

Represents a user-placed control point during road drawing.

| Field | Type | Description |
|-------|------|-------------|
| m_Position | float3 | Snapped world position |
| m_HitPosition | float3 | Raw raycast hit position |
| m_Direction | float2 | Tangent direction |
| m_HitDirection | float3 | Raycast hit normal |
| m_Rotation | quaternion | Orientation |
| m_OriginalEntity | Entity | Entity being snapped to (existing node/edge) |
| m_SnapPriority | float2 | Snap priority weights |
| m_ElementIndex | int2 | Grid cell index |
| m_CurvePosition | float | Position along an existing curve (0-1) |
| m_Elevation | float | Elevation offset |

*Source: `Game.dll` -> `Game.Tools.ControlPoint`*

## System Map

### `NetToolSystem` (Game.Tools)

The player-facing road building tool. Inherits from `ToolBaseSystem`.

**Key properties:**
- `mode` -- Drawing mode: `Straight`, `SimpleCurve`, `ComplexCurve`, `Continuous`, `Grid`, `Replace`, `Point`
- `elevation` / `elevationStep` -- Height control (default step: 10 units)
- `parallelCount` / `parallelOffset` -- Parallel road placement
- `underground` -- Whether building underground
- `prefab` -- The `NetPrefab` being placed
- `toolID` -- `"Net Tool"`

**Max control points by mode:**
- Straight: 2
- SimpleCurve / Continuous / Grid: 3
- ComplexCurve: 4
- Replace / Point: 1

**OnCreate** registers input actions: `Place Net Control Point`, `Place Net Edge`, `Place Net Node`, `Replace Net Edge`, `Undo Net Control Point`, `Upgrade Net Edge`, `Downgrade Net Edge`.

**OnUpdate** handles state machine: Default -> Applying/Cancelling -> processes control points and creates `NetCourse` temp entities.

*Source: `Game.dll` -> `Game.Tools.NetToolSystem` (7807 lines)*

### `GenerateEdgesSystem` (Game.Tools)

Converts `NetCourse` temp entities into temp `Edge` and `Node` entities. Creates the graph topology by mapping course positions to nodes (reusing existing nodes when snapping). Handles edge splitting when a new node is placed on an existing edge.

*Source: `Game.dll` -> `Game.Tools.GenerateEdgesSystem`*

### `ApplyNetSystem` (Game.Tools)

When the user confirms placement, this system promotes temp entities to permanent ones. Patches entity references (replacing temp entity refs with permanent ones), updates `ConnectedEdge` buffers on nodes, and handles `SubNet` ownership.

Key jobs:
- `PatchTempReferencesJob` -- Replaces temp entity references with their originals
- `FixConnectedEdgesJob` -- Updates node connectivity buffers

*Source: `Game.dll` -> `Game.Tools.ApplyNetSystem`*

### `GeometrySystem` (Game.Net)

Computes `EdgeGeometry` and `NodeGeometry` for all updated network entities. Runs `InitializeNodeGeometryJob` to calculate node bounds, flatness, and position based on connected edges and their compositions.

Reads `Curve`, `Composition`, `NetGeometryData`, and `NetCompositionData` to compute left/right bezier segments (`Segment` struct) and bounding boxes.

*Source: `Game.dll` -> `Game.Net.GeometrySystem`*

### `LaneSystem` (Game.Net)

Generates lane entities for edges and nodes. Uses `NetCompositionLane` buffer from prefab data to determine lane positions and types. Creates `Lane` entities with `Curve` components, connects them via `SubLane` buffers.

Handles lane connections at intersections via `ConnectPosition` structures matching source/target lanes across edges.

*Source: `Game.dll` -> `Game.Net.LaneSystem`*

### `LaneConnectionSystem` (Game.Net)

Manages lane-to-lane connections at nodes. Finds updated lanes, checks connectivity, and creates connection entities for pathfinding.

*Source: `Game.dll` -> `Game.Net.LaneConnectionSystem`*

### `CompositionSelectSystem` (Game.Net)

Selects the appropriate composition prefab for each edge and node based on the network prefab, upgrade flags, and context. Creates new composition entities when needed.

*Source: `Game.dll` -> `Game.Net.CompositionSelectSystem`*

### `SearchSystem` (Game.Net)

Maintains a spatial search tree (`NativeQuadTree<Entity, QuadTreeBoundsXZ>`) for fast network queries. Used by the tool system for snapping and by other systems for proximity queries.

*Source: `Game.dll` -> `Game.Net.SearchSystem`*

### `NetUtils.FitCurve` (Game.Net)

Static utility methods for generating `Bezier4x3` curves for network segments. Used by `GeometrySystem`, `LaneSystem`, and tool systems to create smooth curves between network points.

**Overload 1 — From `Line3.Segment` array**:
```csharp
public static Bezier4x3 FitCurve(Line3.Segment[] segments)
```
Fits a cubic Bezier curve through a sequence of line segments. Used when generating curves from discrete sample points (e.g., terrain-following roads).

**Overload 2 — From start/end positions and tangents**:
```csharp
public static Bezier4x3 FitCurve(float3 startPos, float3 startTangent, float3 endPos, float3 endTangent)
```
Creates a cubic Bezier from explicit start/end positions and tangent directions. This is the primary overload used when generating edge curves from control points. The tangent vectors determine curve curvature — longer tangents create wider curves.

```csharp
// Example: Create a curve between two points with specified directions
float3 start = new float3(100, 0, 100);
float3 startDir = new float3(1, 0, 0);  // heading east
float3 end = new float3(200, 0, 200);
float3 endDir = new float3(1, 0, 0);    // also heading east
Bezier4x3 curve = NetUtils.FitCurve(start, startDir, end, endDir);
```

*Source: `Game.dll` -> `Game.Net.NetUtils`*

### `MathUtils` Bezier Operations (Colossal.Mathematics)

Static utility methods for working with `Bezier4x3` curves. Essential for any mod that reads or manipulates network geometry.

**`MathUtils.Position(Bezier4x3, float t)`** — Evaluates the curve at parameter `t` (0-1), returning the world-space `float3` position.

**`MathUtils.Tangent(Bezier4x3, float t)`** — Returns the tangent direction vector at parameter `t`. Useful for orienting objects along a road.

**`MathUtils.Length(Bezier4x3)`** — Computes the approximate arc length of the curve.

```csharp
// Example: Sample positions along a road curve
Curve curve = EntityManager.GetComponentData<Curve>(edgeEntity);
for (float t = 0f; t <= 1f; t += 0.1f)
{
    float3 position = MathUtils.Position(curve.m_Bezier, t);
    float3 tangent = MathUtils.Tangent(curve.m_Bezier, t);
    float3 forward = math.normalize(tangent);
    Log.Info($"t={t:F1}: pos={position}, forward={forward}");
}
float totalLength = MathUtils.Length(curve.m_Bezier);
```

*Source: `Colossal.Mathematics.dll` -> `Colossal.Mathematics.MathUtils`*

## Data Flow

### Road Placement Pipeline

```
Player clicks in world
        |
        v
NetToolSystem.OnUpdate()
  - Collects ControlPoint from raycast
  - Snaps to existing geometry (nodes, edges, grid)
  - Creates NetCourse temp entities
        |
        v
GenerateEdgesSystem / GenerateNodesSystem
  - Converts NetCourse -> Temp Edge + Node entities
  - Maps CoursePos to nodes (reuses existing or creates new)
  - Splits existing edges if new node placed mid-edge
        |
        v
GeometrySystem
  - Computes EdgeGeometry (left/right bezier segments, bounds)
  - Computes NodeGeometry (bounds, flatness, offset)
        |
        v
CompositionSelectSystem
  - Selects composition prefabs based on flags/context
        |
        v
LaneSystem
  - Creates Lane entities from NetCompositionLane data
  - Attaches Curve to each lane
  - Populates SubLane buffer on parent edge
        |
        v
(Preview shown to player as ghost)
        |
        v
Player confirms (Apply action)
        |
        v
ApplyNetSystem
  - Patches temp entity refs to permanent entities
  - Updates ConnectedEdge buffers on nodes
  - Removes Temp component from entities
  - Entities become part of permanent simulation
```

### Network Graph Structure

```
Node (intersection)
  |-- DynamicBuffer<ConnectedEdge>  [capacity 4]
  |       |-- Edge entity A
  |       |-- Edge entity B
  |       |-- Edge entity C
  |       +-- Edge entity D
  |
  +-- NodeGeometry (bounds, flatness)

Edge (road segment)
  |-- Edge.m_Start -> Node entity (start)
  |-- Edge.m_End   -> Node entity (end)
  |-- Curve (Bezier4x3 shape)
  |-- Composition (links to prefab compositions)
  |-- Elevation (float2: start/end offsets)
  |-- EdgeGeometry (left/right segments, bounds)
  |-- DynamicBuffer<SubLane>
  |       |-- Lane entity 1 (car lane)
  |       |-- Lane entity 2 (car lane)
  |       |-- Lane entity 3 (sidewalk)
  |       +-- Lane entity N (utility)
  |
  +-- DynamicBuffer<ConnectedNode> (intermediate splits)

Lane (single traffic lane / sidewalk / utility)
  |-- Lane (PathNode start/middle/end)
  |-- Curve (Bezier4x3 shape)
  +-- PrefabRef -> lane prefab
```

## Prefab & Configuration

### `NetData` (Game.Prefabs)

Defines the core identity of a network prefab.

| Field | Type | Description |
|-------|------|-------------|
| m_NodeArchetype | EntityArchetype | Archetype used to create node entities |
| m_EdgeArchetype | EntityArchetype | Archetype used to create edge entities |
| m_RequiredLayers | Layer | Layers this net requires to connect |
| m_ConnectLayers | Layer | Layers this net can connect to |
| m_LocalConnectLayers | Layer | Layers for local connections |
| m_NodePriority | float | Priority when resolving node conflicts |

### `RoadData` (Game.Prefabs)

Prefab component that defines road-specific properties. Present on road prefab entities (not tracks or pathways). Used to determine speed limits, highway behavior, and road classification.

| Field | Type | Description |
|-------|------|-------------|
| m_SpeedLimit | float | Speed limit in meters per second (not km/h). Convert: km/h = m/s * 3.6 |
| m_Flags | RoadFlags | Road behavior flags (see below) |

**`RoadFlags`** enum:

| Flag | Description |
|------|-------------|
| UseHighwayRules | Enables highway-specific behavior (no traffic lights, restricted turns, highway lane merging) |

**Road hierarchy classification pattern**: Roads can be classified by their `RoadData` properties and lane count:
- **Highway**: `RoadFlags.UseHighwayRules` set, high speed limit
- **Arterial**: High speed limit, multiple lanes, no highway flag
- **Collector**: Medium speed limit, fewer lanes
- **Local**: Low speed limit, typically 2 lanes

**Lookup chain** (lane entity to road speed limit):
```
Lane entity → PrefabRef.m_Prefab → (get parent Edge) → Composition.m_Edge
  → NetCompositionData → PrefabRef.m_Prefab → RoadData.m_SpeedLimit
```

Or more directly from the edge:
```csharp
// From an edge entity, get the road's speed limit
Composition comp = EntityManager.GetComponentData<Composition>(edgeEntity);
PrefabRef compPrefabRef = EntityManager.GetComponentData<PrefabRef>(comp.m_Edge);
if (EntityManager.HasComponent<RoadData>(compPrefabRef.m_Prefab))
{
    RoadData roadData = EntityManager.GetComponentData<RoadData>(compPrefabRef.m_Prefab);
    float speedLimitKmh = roadData.m_SpeedLimit * 3.6f;
    bool isHighway = (roadData.m_Flags & RoadFlags.UseHighwayRules) != 0;
}
```

*Source: `Game.dll` -> `Game.Prefabs.RoadData`, `Game.Prefabs.RoadFlags`*

### `Layer` Enum (Game.Net)

Defines network layer types:

| Layer | Value | Description |
|-------|-------|-------------|
| Road | 1 | Standard roads |
| TrainTrack | 0x40 | Railway tracks |
| Pathway | 0x80 | Pedestrian paths |
| TramTrack | 0x400 | Tram tracks |
| SubwayTrack | 0x800 | Subway tracks |
| PowerlineLow/High | 2/4 | Power lines |
| WaterPipe | 8 | Water pipes |
| SewagePipe | 0x10 | Sewage pipes |
| Waterway | 0x100 | Ship waterways |
| Fence | 0x1000 | Fences |

### `CompositionFlags` (Game.Prefabs)

Flags controlling road composition. Two sub-enums:

**General flags** (applied to whole edge/node):
- `Elevated` (0x10000) -- Road is elevated (bridge)
- `Tunnel` (0x20000) -- Road is in a tunnel
- `Roundabout` (0x40) -- Node is part of a roundabout
- `TrafficLights` (0x400) -- Node has traffic lights
- `LevelCrossing` (0x80) -- Railroad level crossing
- `Crosswalk` (0x100) -- Has crosswalk
- `DeadEnd` (0x10) -- Dead-end node
- `Intersection` (0x20) -- Intersection node
- `Lighting` (0x10000000) -- Has street lighting
- `Pavement` / `Gravel` / `Tiles` -- Surface material

**Side flags** (applied to left/right of edge):
- `Raised` / `Lowered` -- Elevation changes
- `Sidewalk` / `WideSidewalk` -- Sidewalk types
- `ParkingSpaces` -- Has parking
- `SoundBarrier` -- Sound barriers
- `PrimaryLane` through `QuaternaryLane` -- Lane configurations
- `ForbidLeftTurn` / `ForbidRightTurn` / `ForbidStraight` -- Turn restrictions

### `CompositionInvertMode` Enum (Game.Prefabs)

Controls how lane ordering and direction are adjusted for left-hand vs right-hand traffic. Applied to `NetCompositionLane` entries and `NetPieceLanes` to ensure lanes are correctly mirrored for the city's traffic configuration.

| Value | Description |
|-------|-------------|
| InvertLefthandTraffic | Invert lane direction when `CityConfigurationSystem.leftHandTraffic` is true |
| FlipLefthandTraffic | Flip (mirror) lane lateral position when left-hand traffic is active |
| InvertRighthandTraffic | Invert lane direction when right-hand traffic is active (standard/default) |
| FlipRighthandTraffic | Flip (mirror) lane lateral position when right-hand traffic is active |

**Invert** reverses the lane's travel direction (swaps start/end). **Flip** mirrors the lane's lateral position across the road centerline (negates the x offset).

Cross-reference: `CityConfigurationSystem.leftHandTraffic` determines which mode is active. See the [CityConfigurationSystem](#cityconfigurationsystem) section.

```csharp
// Check if a lane needs direction inversion for current traffic setting
var cityConfig = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
bool leftHand = cityConfig.leftHandTraffic;

// If lane has InvertLefthandTraffic and city uses left-hand traffic,
// the lane direction should be inverted
bool shouldInvert = leftHand
    ? (invertMode & CompositionInvertMode.InvertLefthandTraffic) != 0
    : (invertMode & CompositionInvertMode.InvertRighthandTraffic) != 0;
```

*Source: `Game.dll` -> `Game.Prefabs.CompositionInvertMode`*

### `NetCompositionLane` (Game.Prefabs)

Buffer on composition prefabs defining lane positions.

| Field | Type | Description |
|-------|------|-------------|
| m_Lane | Entity | Lane prefab entity |
| m_Position | float3 | Offset position (x = lateral, y = vertical, z = longitudinal) |
| m_Flags | LaneFlags | Lane behavior flags (Master, Slave, etc.) |
| m_Carriageway | byte | Which carriageway (0 = left, 1 = right for RHT; reversed for LHT) |
| m_Group | byte | Lane group index within the carriageway |
| m_Index | byte | Lane index within group |
| m_InvertMode | CompositionInvertMode | How this lane adapts to left/right-hand traffic |

#### Stable Lane Identity via `m_Carriageway` + `m_Group`

The `m_Carriageway` and `m_Group` fields together form an `int2` key (`carriagewayAndGroup`) that serves as a **stable lane identity mechanism**. Unlike entity references or buffer indices, which change when a road is upgraded, split, or modified, the carriageway+group pair remains consistent for the same logical lane across road modifications.

This is critical for mods that need to persist lane-specific data (e.g., custom speed limits, lane restrictions, priorities) across road upgrades. When a road is upgraded (e.g., adding a bus lane), the edge entity may be replaced and lane entities are regenerated, but the `m_Carriageway`/`m_Group` values for unchanged lanes remain the same in the new composition.

**Serialization pattern for robust lane matching**:

```csharp
/// <summary>
/// Serializable lane identity that survives road modifications.
/// Store this instead of Entity references for persistent lane data.
/// </summary>
public struct LaneIdentity : IEquatable<LaneIdentity>
{
    public Entity EdgeEntity;       // Parent edge (may change on upgrade)
    public byte Carriageway;        // Stable: which side of the road
    public byte Group;              // Stable: lane group within carriageway
    public byte Index;              // Stable: lane index within group

    /// <summary>
    /// Find the runtime lane entity matching this identity on a given edge.
    /// Call after road modifications to re-resolve lane references.
    /// </summary>
    public Entity Resolve(EntityManager em, Entity edgeEntity)
    {
        if (!em.TryGetBuffer<SubLane>(edgeEntity, true, out var subLanes))
            return Entity.Null;

        Composition comp = em.GetComponentData<Composition>(edgeEntity);
        if (!em.TryGetBuffer<NetCompositionLane>(comp.m_Edge, true, out var compLanes))
            return Entity.Null;

        for (int i = 0; i < subLanes.Length && i < compLanes.Length; i++)
        {
            if (compLanes[i].m_Carriageway == Carriageway
                && compLanes[i].m_Group == Group
                && compLanes[i].m_Index == Index)
            {
                return subLanes[i].m_SubLane;
            }
        }
        return Entity.Null;
    }

    public bool Equals(LaneIdentity other) =>
        Carriageway == other.Carriageway && Group == other.Group && Index == other.Index;
}
```

### `CarLane` (Game.Prefabs)

Prefab component on lane prefab entities that defines car-specific lane behavior.

| Field | Type | Description |
|-------|------|-------------|
| m_RoadTypes | RoadTypes | Which road types this lane supports (Car, Maintenance, etc.) |
| m_SpeedLimit | float | Default speed limit in game units (not km/h) |
| m_MaxSpeed | float | Maximum allowable speed on this lane |
| m_SafeSpeed | float | Safe cruising speed used for traffic safety calculations |
| m_GasUsage | float | Fuel consumption rate multiplier for vehicles using this lane |

*Source: `Game.dll` -> `Game.Prefabs.CarLane`*

### `TrackLane` (Game.Prefabs)

Prefab component on lane prefab entities that defines track-specific lane behavior (trains, trams, subways).

| Field | Type | Description |
|-------|------|-------------|
| m_SpeedLimit | float | Default speed limit for track vehicles |
| m_TrackTypes | TrackTypes | Which track types this lane supports (Train, Tram, Subway) |

*Source: `Game.dll` -> `Game.Prefabs.TrackLane`*

### Managed Prefab Hierarchy: `NetGeometryPrefab` (Game.Prefabs)

`NetGeometryPrefab` is the managed (MonoBehaviour-side) base class for all network geometry prefabs. It defines the section composition and aggregate type for a network. Subclasses specialize for each network category.

**`NetGeometryPrefab`** (abstract base):

| Field | Type | Description |
|-------|------|-------------|
| m_Sections | NetSection[] | Array of section definitions composing this network |
| m_AggregateType | AggregateNetPrefab | The aggregate prefab this network belongs to (e.g., Highway, Street) |
| m_NodeSections | NetSection[] | Section definitions for node intersections |
| m_EdgeStates | NetEdgeStateInfo[] | State-based edge section overrides |
| m_NodeStates | NetNodeStateInfo[] | State-based node section overrides |
| m_InvertMode | CompositionInvertMode | How lanes adapt to left/right-hand traffic |

**`RoadPrefab`** (extends `NetGeometryPrefab`):

| Field | Type | Description |
|-------|------|-------------|
| m_SpeedLimit | float | Road speed limit in m/s |
| m_RoadType | RoadType | Classification: Normal, Highway, PublicTransport |
| m_TrafficLights | bool | Whether this road supports traffic lights at intersections |
| m_HighwayRules | bool | Whether to apply highway-specific rules |
| m_ZoneBlock | ZoneBlockPrefab | Zone block generation settings (null for non-zoned roads) |

**`TrackPrefab`** (extends `NetGeometryPrefab`):

| Field | Type | Description |
|-------|------|-------------|
| m_SpeedLimit | float | Track speed limit in m/s |
| m_TrackType | TrackType | Classification: Train, Tram, Subway |

**`PathwayPrefab`** (extends `NetGeometryPrefab`):

| Field | Type | Description |
|-------|------|-------------|
| m_SpeedLimit | float | Pathway speed limit in m/s |

**`FencePrefab`** (extends `NetGeometryPrefab`):

No additional fields beyond `NetGeometryPrefab` base.

The managed prefab hierarchy is initialized at game load. `PrefabSystem` converts these into ECS components (`NetData`, `NetGeometryData`, `RoadData`, etc.) on the prefab entities. Mods typically interact with the ECS components at runtime, but the managed prefabs are useful for understanding default values and configuration.

*Source: `Game.dll` -> `Game.Prefabs.NetGeometryPrefab`, `Game.Prefabs.RoadPrefab`, `Game.Prefabs.TrackPrefab`, `Game.Prefabs.PathwayPrefab`, `Game.Prefabs.FencePrefab`*

### `NetSectionPrefab` / `NetPieceInfo` / `NetPieceLanes` Hierarchy (Game.Prefabs)

Network lane composition is defined through a multi-level hierarchy: sections contain pieces, and pieces contain lanes.

**`NetSectionPrefab`** — Defines a cross-section of a network (e.g., "4-lane road with median"):

| Field | Type | Description |
|-------|------|-------------|
| m_Pieces | NetPieceInfo[] | Array of piece definitions composing this section |
| m_SubSections | NetSectionInfo[] | Sub-section overrides based on requirements |

**`NetSectionInfo`** struct — Conditional sub-section selection:

| Field | Type | Description |
|-------|------|-------------|
| m_Section | NetSectionPrefab | The sub-section prefab to use |
| m_RequireAll | NetPieceRequirements | All of these requirements must be met |
| m_RequireAny | NetPieceRequirements | At least one of these requirements must be met |
| m_RequireNone | NetPieceRequirements | None of these requirements may be met |

**`NetPieceInfo`** struct — A single piece within a section:

| Field | Type | Description |
|-------|------|-------------|
| m_Piece | NetPiecePrefab | The piece prefab reference |
| m_RequireAll | NetPieceRequirements | All of these requirements must be met |
| m_RequireAny | NetPieceRequirements | At least one must be met |
| m_RequireNone | NetPieceRequirements | None may be met |
| m_Offset | float3 | Position offset for this piece |

**`NetPieceLanes`** (ComponentBase on `NetPiecePrefab`) — Defines lanes within a piece:

Contains an array of `NetPieceLane` entries, each specifying a lane prefab, position offset, and flags. These are flattened into `NetCompositionLane` entries during prefab initialization.

**`NetPieceRequirements`** enum (flags) — Common values:

| Flag | Description |
|------|-------------|
| Intersection | Piece applies at intersections |
| DeadEnd | Piece applies at dead ends |
| Roundabout | Piece applies in roundabouts |
| Elevated | Piece applies on elevated segments |
| Tunnel | Piece applies in tunnels |
| Sidewalk | Piece applies when sidewalk is present |
| Lighting | Piece applies when street lights are enabled |
| OppositeSide | Piece applies on the opposite side |
| LevelCrossing | Piece applies at level crossings |

**`NetEdgeStateInfo`** struct — State-based edge section override:

| Field | Type | Description |
|-------|------|-------------|
| m_SetState | NetPieceRequirements | Requirements to set on the edge |
| m_UnsetState | NetPieceRequirements | Requirements to unset |
| m_RequireAll | NetPieceRequirements | Conditions that must all be true |
| m_RequireAny | NetPieceRequirements | At least one must be true |
| m_RequireNone | NetPieceRequirements | Conditions that must all be false |

**`NetNodeStateInfo`** struct — State-based node section override (same field structure as `NetEdgeStateInfo`).

**Composition pipeline**: `NetGeometryPrefab.m_Sections` -> `NetSectionPrefab.m_Pieces` -> `NetPiecePrefab` + `NetPieceLanes` -> flattened to `DynamicBuffer<NetCompositionLane>` on the composition prefab entity.

*Source: `Game.dll` -> `Game.Prefabs.NetSectionPrefab`, `Game.Prefabs.NetPieceInfo`, `Game.Prefabs.NetPieceLanes`, `Game.Prefabs.NetSectionInfo`, `Game.Prefabs.NetPieceRequirements`, `Game.Prefabs.NetEdgeStateInfo`, `Game.Prefabs.NetNodeStateInfo`*

### `AggregateNetPrefab` (Game.Prefabs)

Defines the aggregate type for a group of related network segments. Aggregates group contiguous edges that share the same network type for naming, statistics, and UI purposes.

**Known aggregate prefab names**:

| Aggregate Name | Network Types |
|----------------|---------------|
| Highway | Highway roads |
| Street | Standard city roads |
| Alley | Narrow/alley roads |
| Train Track | Railway lines |
| Tram Track | Tram lines |
| Subway Track | Underground rail |
| Pathway | Pedestrian paths |
| Public Transport Lane | Dedicated PT lanes |

**Relationship to `NetGeometryPrefab`**: Each `NetGeometryPrefab` subclass references an `AggregateNetPrefab` via `m_AggregateType`. This determines which aggregate a newly placed edge joins.

**Runtime behavior**: `AggregateSystem` reads the `m_AggregateType` from the edge's prefab and creates/merges `Aggregate` entities accordingly. Each `Aggregate` entity carries:
- `DynamicBuffer<AggregateElement>` listing member edge entities
- `Aggregated` component on each member edge, pointing back to the aggregate

**Usage**: Aggregates power road name labels (names span the full aggregate), the Traffic mod's road-level statistics, and UI display of aggregate-level info (total length, average condition).

*Source: `Game.dll` -> `Game.Prefabs.AggregateNetPrefab`, `Game.Net.AggregateSystem`*

## Harmony Patch Points

### Recommended: ECS Queries (No Patches Needed)

For reading network data, use standard ECS queries. All network components are regular `IComponentData` / `IBufferElementData` and can be queried directly:

```csharp
// Query all road edges
EntityQuery edgeQuery = GetEntityQuery(
    ComponentType.ReadOnly<Edge>(),
    ComponentType.ReadOnly<Curve>(),
    ComponentType.ReadOnly<Composition>()
);
```

### `NetToolSystem.OnUpdate()` -- Intercept Tool Actions

- **Prefix**: Block or redirect road placement actions
- **Postfix**: Add custom behavior after tool updates (e.g., snap to custom grid)
- Risk: Central to all network building. Breaking this disables road/rail/pipe tools entirely.

### `ApplyNetSystem.OnUpdate()` -- Intercept Road Creation

- **Prefix**: Prevent certain road placements from being applied
- **Postfix**: Add components or modify newly created road entities
- This runs only when the user confirms placement, making it ideal for post-creation modifications.

### `GeometrySystem.OnUpdate()` -- Modify Road Geometry

- **Prefix/Postfix**: Alter computed geometry (e.g., custom road widths, shapes)
- Risk: Affects rendering and collision for all network entities.

### `LaneSystem.OnUpdate()` -- Modify Lane Generation

- **Prefix/Postfix**: Change lane layout, add custom lanes, modify lane properties
- Risk: Affects pathfinding and traffic for all roads.

### Direct Component Manipulation

You can modify network entities directly via `EntityManager` or `EntityCommandBuffer`. For example, to change a road's curve:

```csharp
EntityManager.SetComponentData(edgeEntity, new Curve { m_Bezier = newBezier });
```

After modifying geometry, mark the entity as `Updated` so `GeometrySystem` and `LaneSystem` recalculate:

```csharp
EntityManager.AddComponent<Updated>(edgeEntity);
```

## Mod Blueprint

### Reading Network Topology

```csharp
public partial class NetworkReaderSystem : GameSystemBase
{
    private EntityQuery _edgeQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _edgeQuery = GetEntityQuery(
            ComponentType.ReadOnly<Edge>(),
            ComponentType.ReadOnly<Curve>(),
            ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Temp>()
        );
    }

    protected override void OnUpdate()
    {
        var edges = _edgeQuery.ToComponentDataArray<Edge>(Allocator.Temp);
        var curves = _edgeQuery.ToComponentDataArray<Curve>(Allocator.Temp);
        var entities = _edgeQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < edges.Length; i++)
        {
            Edge edge = edges[i];
            Curve curve = curves[i];
            Node startNode = EntityManager.GetComponentData<Node>(edge.m_Start);
            Node endNode = EntityManager.GetComponentData<Node>(edge.m_End);

            Log.Info($"Edge {entities[i]}: {startNode.m_Position} -> {endNode.m_Position}, length={curve.m_Length}");
        }

        edges.Dispose();
        curves.Dispose();
        entities.Dispose();
    }
}
```

### Finding All Edges Connected to a Node

```csharp
public void LogConnectedEdges(Entity nodeEntity)
{
    if (!EntityManager.TryGetBuffer<ConnectedEdge>(nodeEntity, true, out var edges))
        return;

    Node node = EntityManager.GetComponentData<Node>(nodeEntity);
    Log.Info($"Node at {node.m_Position} has {edges.Length} connected edges:");

    for (int i = 0; i < edges.Length; i++)
    {
        Entity edgeEntity = edges[i].m_Edge;
        Edge edge = EntityManager.GetComponentData<Edge>(edgeEntity);
        Curve curve = EntityManager.GetComponentData<Curve>(edgeEntity);
        Log.Info($"  Edge {edgeEntity}: length={curve.m_Length:F1}");
    }
}
```

### Querying Lanes on a Road Segment

```csharp
public void LogLanes(Entity edgeEntity)
{
    if (!EntityManager.TryGetBuffer<SubLane>(edgeEntity, true, out var subLanes))
        return;

    for (int i = 0; i < subLanes.Length; i++)
    {
        Entity laneEntity = subLanes[i].m_SubLane;
        Lane lane = EntityManager.GetComponentData<Lane>(laneEntity);
        Curve curve = EntityManager.GetComponentData<Curve>(laneEntity);
        PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(laneEntity);
        Log.Info($"  Lane {i}: prefab={prefabRef.m_Prefab}, length={curve.m_Length:F1}");
    }
}
```

### Modifying Road Elevation

```csharp
public void SetEdgeElevation(Entity edgeEntity, float startElevation, float endElevation)
{
    if (EntityManager.HasComponent<Elevation>(edgeEntity))
    {
        EntityManager.SetComponentData(edgeEntity, new Elevation(new float2(startElevation, endElevation)));
    }
    else
    {
        EntityManager.AddComponentData(edgeEntity, new Elevation(new float2(startElevation, endElevation)));
    }

    // Mark as updated so geometry and lanes are recalculated
    if (!EntityManager.HasComponent<Updated>(edgeEntity))
    {
        EntityManager.AddComponent<Updated>(edgeEntity);
    }
}
```

### Checking Road Composition Flags

```csharp
public bool IsElevated(Entity edgeEntity)
{
    Composition comp = EntityManager.GetComponentData<Composition>(edgeEntity);
    NetCompositionData compData = EntityManager.GetComponentData<NetCompositionData>(comp.m_Edge);
    return (compData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0;
}

public bool HasTrafficLights(Entity nodeEntity)
{
    // Need to check the composition of connected edges at this node
    if (!EntityManager.TryGetBuffer<ConnectedEdge>(nodeEntity, true, out var edges))
        return false;

    for (int i = 0; i < edges.Length; i++)
    {
        Composition comp = EntityManager.GetComponentData<Composition>(edges[i].m_Edge);
        Edge edge = EntityManager.GetComponentData<Edge>(edges[i].m_Edge);

        Entity nodeComp = (edge.m_Start == nodeEntity) ? comp.m_StartNode : comp.m_EndNode;
        NetCompositionData compData = EntityManager.GetComponentData<NetCompositionData>(nodeComp);

        if ((compData.m_Flags.m_General & CompositionFlags.General.TrafficLights) != 0)
            return true;
    }
    return false;
}
```

## Lane Hierarchy Components

### `MasterLane` / `SlaveLane` (Game.Net)

Define master-slave relationships between parallel lanes (lane groups). When modifying a lane connection, changes to one lane in a group can affect the entire group.

| Component | Field | Type | Description |
|-----------|-------|------|-------------|
| `MasterLane` | `m_Group` | int | Lane group identifier |
| `SlaveLane` | `m_Group` | int | Must match master's group |
| `SlaveLane` | `m_MinIndex` / `m_MaxIndex` | int | Lane index range within the group |

### `EdgeLane` (Game.Net)

Links a lane back to its parent edge entity.

| Field | Type | Description |
|-------|------|-------------|
| `m_Edge` | Entity | Parent edge entity |
| `m_EdgeDelta` | float2 | Normalized start/end position along the edge (0-1 range) |

## NetSubObjects (Network Prefab Attachments)

`NetSubObjects` (ComponentBase) attaches objects to network prefabs at specific positions. Uses `NetSubObjectInfo` structs:

| Field | Type | Description |
|-------|------|-------------|
| `m_Object` | ObjectPrefab | Object prefab to attach |
| `m_Position` | float3 | Local position offset |
| `m_Rotation` | quaternion | Local rotation |
| `m_Placement` | NetObjectPlacement | Where to place (Node, Edge, etc.) |
| `m_RequireElevated` | bool | Only place on elevated segments |
| `m_RequireOutsideConnection` | bool | Only at map-edge connections |

**Known outside connection prefabs**: `"Road Outside Connection - Oneway"`, `"Road Outside Connection - Twoway"`, `"Train Outside Connection - Oneway"`, `"Train Outside Connection - Twoway"`.

**PillarObject** (ComponentBase): Specialized attachment for bridge/elevated pillars.

## PlaceableNet Component

`PlaceableNet` (ComponentBase) controls how a network prefab behaves in the placement tool:

| Field | Type | Description |
|-------|------|-------------|
| `m_AllowParallelMode` | bool | Whether parallel placement is allowed |
| `m_XPReward` | int | XP granted per segment placed |
| `m_ElevationRange` | Bounds1 | Allowed elevation range (min/max in meters) |

Corresponds to the ECS `PlaceableNetData` component on the prefab entity.

## CityConfigurationSystem

`Game.City.CityConfigurationSystem` provides city-level configuration that affects road behavior:

```csharp
var cityConfig = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
bool leftHandTraffic = cityConfig.leftHandTraffic; // UK/Japan style
```

**`leftHandTraffic`**: Controls lane direction, turn priority, and intersection behavior. Essential for lane connection mods.

**`defaultTheme`**: References the city's `ThemePrefab`. The theme's `assetPrefix` determines lane divider style:
```csharp
prefabSystem.TryGetSpecificPrefab<ThemePrefab>(cityConfig.defaultTheme, out var theme);
bool yellowDivider = theme.assetPrefix is not "EU"; // NA=yellow, EU=white
```

Affects `CompositionInvertMode` for lane ordering and direction inversion.

## Temp Entity Lifecycle

Temp entities follow a multi-phase lifecycle in the tool pipeline:

1. **ToolUpdate phase**: Tool systems create `CreationDefinition` / `ConnectionDefinition` entities
2. **Modification phase**: `GenerateEdgesSystem` generates `Temp` entities from definitions
3. **ApplyTool phase**: `ApplyNetSystem` promotes Temp entities to permanent (or discards on cancel)
4. **ClearTool phase**: Cleanup of remaining Temp state

`TempFlags` control behavior: `Delete` (remove existing), `Replace` (swap with new), `Combine` (merge), `Cancel` (undo).

Custom mods can register apply systems at `SystemUpdatePhase.ApplyTool` **before** `ApplyNetSystem` using `UpdateBefore<CustomApplySystem, ApplyNetSystem>` to process custom definition types (e.g., `TempLaneConnection`, `TempLanePriority`) during the promotion step.

## Mod Blueprint: Custom Network Prefab Creation (RoadBuilder Pattern)

A comprehensive blueprint for mods that create custom network prefabs (roads, tracks, paths, fences) at runtime, based on the RoadBuilder mod architecture.

**Mod archetype**: Runtime network asset creator. The mod defines custom road/track/path/fence configurations, generates prefabs from them at runtime, registers them with the game, and persists the configuration in save files.

### Systems to Create

| System | Phase | Purpose |
|--------|-------|---------|
| GenerationDataSystem | PrefabUpdate | Collects all game prefab references (sections, pieces, lanes) into lookup dictionaries for prefab assembly |
| NetSectionsSystem | PrefabUpdate | Indexes available `NetSectionPrefab`, `NetPiecePrefab`, and lane prefabs for section composition |
| InitializerSystem | MainLoop | Loads saved configurations and queues prefab creation/update on game load |
| CoreSystem | Modification1 | Queues and applies prefab updates -- the central orchestrator for create/modify/delete operations |
| ApplyTagSystem | Modification2 | Tags newly placed edges with a custom `IEmptySerializable` marker component for tracking |
| TrackerSystem | Modification3 | Tracks which custom prefabs are actually placed in the city (for cleanup and save optimization) |
| ToolSystem | ToolUpdate | Custom `ToolBaseSystem` for picking/editing existing network segments with raycast and highlighting |
| SerializeSystem | Serialize | Save/load integration using `ISerializable` custom components |
| UI Systems (4) | UIUpdate | Various panels for configuration, selection, and toolbar integration |

### Components to Create

| Component | Type | Purpose |
|-----------|------|---------|
| CustomNetworkConfig | `IComponentData : ISerializable` | Stores the full network configuration (lanes, sections, options) embedded in save files |
| CustomNetworkTag | `IComponentData : IEmptySerializable` | Empty tag on placed edges for efficient queries -- persists across save/load |
| CustomNetworkPrefabData | `IComponentData` | Marks prefab entities as mod-generated for query filtering |

### Harmony Patches Needed

- **None required for core functionality** -- prefab creation uses `PrefabSystem.AddPrefab()` and `PrefabSystem.UpdatePrefab()` directly
- **Optional**: Patch `PrefabSystem` internals only if `m_PrefabIndices` fixup via reflection proves insufficient

### Key Game Components

- `NetGeometryPrefab` (and subclasses `RoadPrefab`, `TrackPrefab`, `PathwayPrefab`, `FencePrefab`) -- base classes for custom prefab instances via `ScriptableObject.CreateInstance<T>()`
- `NetSectionPrefab` / `NetPieceInfo` / `NetPieceLanes` -- section composition hierarchy that defines lane layout
- `NetCompositionLane` -- buffer on composition entities defining lane positions, carriageway, and group
- `UIObject.m_Group` / `ServiceObject.m_Service` -- toolbar placement for custom prefabs
- `UIGroupElement` -- buffer on toolbar group entities that must be cleaned up before `UpdatePrefab()`
- `PrefabSystem.m_PrefabIndices` -- internal dictionary requiring reflection-based fixup after `UpdatePrefab()`
- `AggregateNetPrefab` -- aggregate type assignment for road naming and statistics

### Core Pattern

```csharp
// 1. Create custom prefab
var customRoad = ScriptableObject.CreateInstance<RoadPrefab>();
customRoad.name = "MyMod_CustomRoad";
customRoad.m_SpeedLimit = 16.67f; // 60 km/h
customRoad.m_Sections = BuildSections(); // Compose from existing NetSectionPrefabs
customRoad.m_AggregateType = existingAggregateType;

// 2. Add toolbar integration
var uiObject = customRoad.AddComponent<UIObject>();
uiObject.m_Group = targetToolbarGroup;
var serviceObj = customRoad.AddComponent<ServiceObject>();
serviceObj.m_Service = roadsService;

// 3. Register with PrefabSystem
prefabSystem.AddPrefab(customRoad);

// 4. To update later: clean UIGroupElement buffer, then call UpdatePrefab
prefabSystem.UpdatePrefab(customRoad);
// Fix m_PrefabIndices via reflection if needed
```

### Key Considerations

- **12+ systems** is typical for a full-featured network prefab mod -- plan for significant architecture
- Use `ISerializable` (not `IEmptySerializable`) for config data that must survive save/load with actual values
- Use `IEmptySerializable` for tag components that just need to persist (no data payload)
- `GenericUIWriter` enables serializing complex configuration objects to the UI layer
- React/TypeScript frontend with `ValueBinding`/`TriggerBinding` for rich configuration UI
- Embedded JSON locale resources with a `LocaleHelper` pattern for multi-language support

## Open Questions

- How does `NetToolSystem` handle the Grid mode internally? The control point collection differs from curve modes but the exact grid generation logic was not fully traced.
- [x] What is the full lifecycle of `Temp` entities? — Documented above: ToolUpdate creates definitions → Modification generates Temp entities → ApplyTool promotes to permanent → ClearTool cleans up. Uses `TempFlags` (Delete, Replace, Combine, Cancel).
- How does edge splitting work when a new node is placed on an existing edge mid-segment? `GenerateEdgesSystem` handles this but the exact split logic is complex.
- ~~How are `Aggregate` entities used?~~ **Resolved**: `Aggregate` entities group contiguous edges that share the same network prefab (e.g., all connected segments of the same road type). `AggregateSystem` merges edges into aggregates when they share a node and have the same prefab, and splits aggregates when edges are deleted or change type. The `Aggregated` component on each edge stores a reference to its parent aggregate entity. Aggregates are used by the naming system (road name labels span the entire aggregate), by the Traffic mod for road-level statistics, and by the UI to display aggregate-level info (e.g., total road length, average condition). Each aggregate carries a `DynamicBuffer<AggregateElement>` listing its member edges. The aggregate type is determined by `NetGeometryPrefab.m_AggregateType`, which references an `AggregateNetPrefab` (known types: Highway, Street, Alley, Train Track, Tram Track, Subway Track, Pathway, Public Transport Lane). See the [AggregateNetPrefab](#aggregatenetprefab-gameprefabs) section for details.
- What triggers `CompositionSelectSystem` to choose different compositions? The selection logic based on connected edges, upgrade flags, and context is complex.
- How do parallel roads (from `NetToolSystem.parallelCount`) interact with the generation pipeline?

## Sources

- `Game.dll` decompiled with ilspycmd v9.1
- Key types: `Game.Net.Edge`, `Game.Net.Node`, `Game.Net.Curve`, `Game.Net.Lane`, `Game.Net.Composition`, `Game.Net.ConnectedEdge`, `Game.Net.SubLane`, `Game.Net.Elevation`, `Game.Net.EdgeGeometry`, `Game.Net.NodeGeometry`, `Game.Net.Upgraded`, `Game.Net.Roundabout`
- Key systems: `Game.Tools.NetToolSystem` (7807 lines), `Game.Tools.ApplyNetSystem`, `Game.Tools.GenerateEdgesSystem`, `Game.Net.GeometrySystem`, `Game.Net.LaneSystem`, `Game.Net.LaneConnectionSystem`, `Game.Net.CompositionSelectSystem`, `Game.Net.NetUtils`
- Key prefab data: `Game.Prefabs.NetData`, `Game.Prefabs.CompositionFlags`, `Game.Prefabs.NetCompositionLane`, `Game.Prefabs.RoadData`, `Game.Prefabs.RoadFlags`, `Game.Prefabs.NetGeometryPrefab`, `Game.Prefabs.RoadPrefab`, `Game.Prefabs.TrackPrefab`, `Game.Prefabs.PathwayPrefab`, `Game.Prefabs.FencePrefab`, `Game.Prefabs.NetSectionPrefab`, `Game.Prefabs.NetPieceInfo`, `Game.Prefabs.NetPieceLanes`, `Game.Prefabs.AggregateNetPrefab`, `Game.Prefabs.CompositionInvertMode`
- Key math utilities: `Colossal.Mathematics.MathUtils` (Bezier4x3 operations)
- Snippets saved to `research/topics/RoadNetwork/snippets/`
