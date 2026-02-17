# Research: Info Views & Data Overlays

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2's info views (pollution overlay, land value overlay, electricity coverage, etc.) work — the prefab structure, infomode activation, heatmap rendering, and UI panel data flow.

**Why**: To enable mods that create custom info view overlays, add new infomodes to existing views, or modify how overlay colors and data are displayed.

**Boundaries**: We focus on the info view framework and rendering pipeline. Individual simulation systems that produce the underlying data (e.g., GroundPollutionSystem, LandValueSystem) are out of scope except where they connect to the overlay.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Prefabs | InfoviewPrefab, InfomodePrefab (and all subtypes), InfoviewInitializeSystem, InfomodeActive, InfoviewData, InfoviewMode, InfomodeData, InfoviewHeatmapData |
| Game.dll | Game.Rendering | OverlayInfomodeSystem, OverlayRenderSystem, HeatmapData enum |
| Game.dll | Game.UI.InGame | InfoviewsUISystem, InfoviewUISystemBase, InfoviewMenu, per-view UI systems (PollutionInfoviewUISystem, etc.) |
| Game.dll | Game.Tools | ToolSystem (infoview activation), InfoviewUtils (color mapping helpers) |

## Component Map

### `InfoviewData` (Game.Prefabs)

Attached to InfoviewPrefab entities. Identifies which notification icons should appear when this view is active.

| Field | Type | Description |
|-------|------|-------------|
| m_NotificationMask | uint | Bitmask of IconCategory values — which warning icons to show |

*Source: `Game.dll` → `Game.Prefabs.InfoviewData`*

### `InfoviewMode` (Game.Prefabs) — Buffer

Buffer on InfoviewPrefab entities listing all infomodes belonging to this view.

| Field | Type | Description |
|-------|------|-------------|
| m_Mode | Entity | The infomode prefab entity |
| m_Priority | int | Display priority (lower = higher priority) |
| m_Supplemental | bool | If true, only shown when explicitly enabled by tool |
| m_Optional | bool | If true, only shown when explicitly toggled on in the UI |

*Source: `Game.dll` → `Game.Prefabs.InfoviewMode`*

### `InfomodeData` (Game.Prefabs)

Empty marker component on all infomode prefab entities.

*Source: `Game.dll` → `Game.Prefabs.InfomodeData`*

### `InfomodeActive` (Game.Prefabs)

Added to infomode entities when they are currently active (displayed). This is a runtime-only component, not serialized.

| Field | Type | Description |
|-------|------|-------------|
| m_Priority | int | Display priority |
| m_Index | int | Shader texture channel index (1-based, mapped into terrain overlay) |
| m_SecondaryIndex | int | Secondary channel for water surface overlay (-1 if none) |

*Source: `Game.dll` → `Game.Prefabs.InfomodeActive`*

### `InfoviewHeatmapData` (Game.Prefabs)

Attached to HeatmapInfomodePrefab entities. Tells the rendering system which cell-map data to sample.

| Field | Type | Description |
|-------|------|-------------|
| m_Type | HeatmapData | Enum selecting the data source (GroundWater, AirPollution, LandValue, etc.) |

*Source: `Game.dll` → `Game.Prefabs.InfoviewHeatmapData`*

### `HeatmapData` Enum (Game.Rendering)

| Value | Name | Data Source |
|-------|------|-------------|
| 0 | None | — |
| 1 | GroundWater | GroundWaterSystem |
| 2 | GroundPollution | GroundPollutionSystem |
| 3 | AirPollution | AirPollutionSystem |
| 4 | Wind | WindSystem |
| 5 | WaterFlow | WaterRenderSystem.flowTexture |
| 6 | TelecomCoverage | TelecomPreviewSystem |
| 7 | Fertility | NaturalResourceSystem |
| 8 | Ore | NaturalResourceSystem |
| 9 | Oil | NaturalResourceSystem |
| 10 | LandValue | LandValueSystem |
| 11 | Attraction | AvailabilityInfoToGridSystem |
| 12 | Customers | AvailabilityInfoToGridSystem |
| 13 | Workplaces | AvailabilityInfoToGridSystem |
| 14 | Services | AvailabilityInfoToGridSystem |
| 15 | Noise | NoisePollutionSystem |
| 16 | WaterPollution | WaterRenderSystem (GPU mask) |
| 17 | Population | PopulationToGridSystem |
| 18 | GroundWaterPollution | GroundWaterSystem |
| 19 | Fish | NaturalResourceSystem |

*Source: `Game.dll` → `Game.Rendering.HeatmapData`*

## System Map

### `InfoviewInitializeSystem` (Game.Prefabs)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Key responsibility**: Automatically determines which info view is relevant for a selected entity (building, road, vehicle) and populates the InfoviewMode buffer on each InfoviewPrefab entity during prefab initialization.
- **Contains**: `FindInfoviewJob` (Burst-compiled IJobChunk) that matches entity component signatures to infomode types.

### `OverlayInfomodeSystem` (Game.Rendering)

- **Base class**: GameSystemBase
- **Update phase**: Rendering
- **Key responsibility**: Renders heatmap overlays onto terrain and water textures.
- **Queries**: EntityQuery requiring `InfomodeActive` + `InfoviewHeatmapData`
- **How it works**:
  1. Each frame, resets terrain/water overlay textures
  2. Iterates active heatmap infomodes
  3. For each heatmap type, schedules a Burst job that reads the relevant CellMapData and writes RGBA bytes into a Texture2D
  4. The texture is assigned to `TerrainRenderSystem.overrideOverlaymap` / `WaterRenderSystem.overrideOverlaymap`
  5. Wind and WaterFlow use special arrow mask textures
- **Textures**: Terrain (RGBA32), Water (RGBA32), Wind (R16G16B16A16_SFloat)
- **Channel packing**: Each active infomode writes to a specific RGBA channel (`m_Index - 1`), allowing up to 4 simultaneous overlays per color group

### `InfoviewsUISystem` (Game.UI.InGame)

- **Base class**: UISystemBase
- **Update phase**: UI
- **Key responsibility**: Bridges infoview data to the UI. Binds list of available info views, active view state, and handles user toggle actions.
- **Bindings**:
  - `infoviews.infoviews` — array of all available info views
  - `infoviews.activeInfoview` — currently active view with its infomodes
  - `infoviews.setActiveInfoview` (trigger) — activate/deactivate a view
  - `infoviews.setInfomodeActive` (trigger) — toggle individual infomodes

### `InfoviewUISystemBase` (Game.UI.InGame)

- **Base class**: UISystemBase
- **Update phase**: UI (throttled via UIUpdateState, updates every 256 frames)
- **Key responsibility**: Abstract base for per-view data panels (pollution stats, electricity stats, etc.)
- **Subclasses**: PollutionInfoviewUISystem, ElectricityInfoviewUISystem, WaterInfoviewUISystem, etc. (one per info view category)
- **Pattern**: Each subclass computes aggregate statistics via Burst jobs, then updates ValueBindings that the UI reads.

### `ToolSystem` (Game.Tools) — Infoview Management

- **Key responsibility**: Central authority for which info view and infomodes are active. Controls shader global variables for overlay coloring.
- **Key fields**:
  - `m_CurrentInfoview` — the active InfoviewPrefab
  - `m_Infomodes` — NativeList of active infomode entities
  - `m_InfomodeColors[303]` — Vector4 array (101 infomodes x 3 colors each: low, medium, high)
  - `m_InfomodeParams[101]` — Vector4 array (steps, speed, tiling, fill per infomode)
- **Shader globals set**:
  - `colossal_InfoviewOn` (int: 0 or 1)
  - `colossal_InfomodeColors` (Vector4 array)
  - `colossal_InfomodeParams` (Vector4 array)
- **Color groups**: Infomodes are organized into 3 color groups (0, 1, 2) with 4 slots each. Group assignment depends on the infomode type (terrain, water, secondary).

## Data Flow

```
USER ACTIVATES INFO VIEW
  InfoviewsUISystem.SetActiveInfoview(entity)
      │
      ▼
  ToolSystem.SetInfoview(InfoviewPrefab)
    Clears all previous InfomodeActive components
    Reads InfoviewMode buffer from prefab entity
    For each non-optional, non-supplemental mode:
      Adds InfomodeActive component to infomode entity
      Assigns color group index (m_Index) and secondary index
      │
      ▼
  ToolSystem.UpdateInfoviewColors()
    Reads GetColors() from each active InfomodePrefab
    Writes low/medium/high colors into m_InfomodeColors array
    Writes steps/speed/tiling/fill into m_InfomodeParams array
    Sets shader globals: colossal_InfoviewOn = 1
    Sets shader globals: colossal_InfomodeColors, colossal_InfomodeParams
      │
      ├──────────────────────────────┐
      ▼                              ▼
  OverlayInfomodeSystem          Rendering Shaders
    Queries: InfomodeActive +      Read colossal_InfomodeColors
      InfoviewHeatmapData          Apply gradient coloring to
    For each active heatmap:       buildings, roads, vehicles
      Schedules Burst job            based on entity data
      Reads CellMapData
      Writes to Texture2D
      Assigns to TerrainRender
        .overrideOverlaymap
      │
      ▼
  TerrainRenderSystem / WaterRenderSystem
    Renders terrain/water with overlay texture
    Shader reads RGBA channels for each infomode
      │
      ▼
  InfoviewUISystemBase subclasses (every 256 frames)
    Compute aggregate statistics (avg pollution, coverage %, etc.)
    Update ValueBindings for UI panels
```

## Prefab & Configuration

### Prefab Hierarchy

```
InfoviewPrefab (e.g., "Pollution")
  ├── m_Infomodes: InfomodeInfo[]
  │     ├── HeatmapInfomodePrefab (e.g., "AirPollution")
  │     │     └── m_Type = HeatmapData.AirPollution
  │     ├── HeatmapInfomodePrefab (e.g., "GroundPollution")
  │     │     └── m_Type = HeatmapData.GroundPollution
  │     └── HeatmapInfomodePrefab (e.g., "NoisePollution")
  │           └── m_Type = HeatmapData.Noise
  ├── m_DefaultColor: Color (0.7, 0.7, 0.7)
  ├── m_SecondaryColor: Color (0.6, 0.6, 0.6)
  ├── m_IconPath: string
  ├── m_Priority: int (display order)
  ├── m_Group: int (grouping category)
  └── m_WarningCategories: IconCategory[]
```

### Infomode Prefab Types

| Prefab Class | Base | Visual Type | ECS Data Component |
|-------------|------|-------------|-------------------|
| HeatmapInfomodePrefab | GradientInfomodeBasePrefab | Terrain/water texture overlay | InfoviewHeatmapData |
| BuildingInfomodePrefab | ColorInfomodeBasePrefab | Solid building color | InfoviewBuildingData |
| BuildingStatusInfomodePrefab | GradientInfomodeBasePrefab | Gradient building color | InfoviewBuildingStatusData |
| BuildingStateInfomodePrefab | ColorInfomodeBasePrefab | Solid building color | InfoviewBuildingStateData |
| VehicleInfomodePrefab | ColorInfomodeBasePrefab | Vehicle tint | InfoviewVehicleData |
| ServiceCoverageInfomodePrefab | GradientInfomodeBasePrefab | Road network gradient | InfoviewCoverageData |
| NetStatusInfomodePrefab | GradientInfomodeBasePrefab | Road network gradient with flow | InfoviewNetStatusData |
| NetGeometryInfomodePrefab | ColorInfomodeBasePrefab | Road geometry color | InfoviewNetGeometryData |
| EffectRangeInfomodePrefab | ColorInfomodeBasePrefab | Building radius circles | InfoviewLocalEffectData |
| ObjectStatusInfomodePrefab | GradientInfomodeBasePrefab | Object gradient color | InfoviewObjectStatusData |
| ObjectStateInfomodePrefab | ColorInfomodeBasePrefab | Object solid color | InfoviewObjectStateData |
| MarkerInfomodePrefab | ColorInfomodeBasePrefab | Map marker color | InfoviewMarkerData |
| RouteInfomodePrefab | ColorInfomodeBasePrefab | Transit route color | InfoviewRouteData |
| TransportStopInfomodePrefab | ColorInfomodeBasePrefab | Transport stop color | InfoviewTransportStopData |
| ZoneSuitabilityInfomodePrefab | GradientInfomodeBasePrefab | Zone suitability gradient | InfoviewAvailabilityData |

### Color Configuration

Gradient infomodes define three colors (low/medium/high) with a step count for discrete banding:

| Property | Type | Description |
|----------|------|-------------|
| m_Low | Color | Color at minimum value (often red) |
| m_Medium | Color | Color at midpoint (often yellow) |
| m_High | Color | Color at maximum value (often green) |
| m_Steps | int | Number of discrete gradient bands (default 11) |
| m_LegendType | GradientLegendType | Gradient (continuous) or Fields (discrete categories) |

Color infomodes use a single flat color applied uniformly.

## Harmony Patch Points

### Candidate 1: `Game.Tools.ToolSystem.SetInfoview`

- **Signature**: `private void SetInfoview(InfoviewPrefab value, List<InfomodePrefab> infomodes)`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept info view changes, add custom infomodes to any view, or prevent certain views from activating
- **Risk level**: Medium (private method, signature may change)
- **Side effects**: Modifying the infomodes list affects what gets rendered

### Candidate 2: `Game.Tools.ToolSystem.UpdateInfoviewColors`

- **Signature**: `private void UpdateInfoviewColors()`
- **Patch type**: Postfix
- **What it enables**: Override shader color arrays after they're set, changing overlay appearance
- **Risk level**: Medium (modifies shader globals)
- **Side effects**: Could break visual consistency if colors conflict

### Candidate 3: `Game.Rendering.OverlayInfomodeSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Inject custom heatmap data into terrain/water textures, or add entirely new overlay types
- **Risk level**: High (rendering-critical path, Burst jobs involved)
- **Side effects**: Performance-sensitive; bad texture data causes visual artifacts

### Candidate 4: `Game.UI.InGame.InfoviewsUISystem.SetActiveInfoview`

- **Signature**: `public void SetActiveInfoview(Entity entity)`
- **Patch type**: Prefix
- **What it enables**: Intercept UI-driven info view activation, redirect to custom views
- **Risk level**: Low (public method, simple delegation)
- **Side effects**: Minimal — just sets ToolSystem.infoview

## Mod Blueprint

- **Systems to create**: Custom `GameSystemBase` that writes to a `CellMapData<T>` grid for custom heatmap data
- **Components to add**: Custom `IComponentData` struct for new overlay data if needed; register via custom InfomodePrefab
- **Patches needed**: Postfix on `ToolSystem.SetInfoview` to inject custom infomodes; or create custom InfoviewPrefab and InfomodePrefab via PrefabSystem
- **Settings**: User-configurable overlay colors (low/mid/high), gradient steps, overlay opacity
- **UI changes**: Custom InfoviewUISystemBase subclass for info panel statistics; register bindings in "infoviews" group

## Examples

### Example 1: Activate an Info View Programmatically

Set the active info view to a specific view by name (e.g., "Pollution") from mod code.

```csharp
public partial class CustomInfoviewActivator : GameSystemBase
{
    private ToolSystem _toolSystem;
    private PrefabSystem _prefabSystem;
    private InfoviewInitializeSystem _infoviewInit;

    protected override void OnCreate()
    {
        base.OnCreate();
        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
        _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        _infoviewInit = World.GetOrCreateSystemManaged<InfoviewInitializeSystem>();
    }

    public void ActivateInfoview(string viewName)
    {
        foreach (InfoviewPrefab prefab in _infoviewInit.infoviews)
        {
            if (prefab.name == viewName && prefab.isValid)
            {
                _toolSystem.infoview = prefab;
                return;
            }
        }
        Log.Warn($"Info view '{viewName}' not found");
    }

    public void DeactivateInfoview()
    {
        _toolSystem.infoview = null;
    }

    protected override void OnUpdate() { }
}
```

### Example 2: Toggle an Individual Infomode

Enable or disable a specific infomode layer within the active info view.

```csharp
public void ToggleInfomode(ToolSystem toolSystem, PrefabSystem prefabSystem,
    string infomodeName, bool active)
{
    InfoviewPrefab activeView = toolSystem.activeInfoview;
    if (activeView == null) return;

    List<InfomodeInfo> infomodes = toolSystem.GetInfoviewInfomodes();
    if (infomodes == null) return;

    foreach (InfomodeInfo info in infomodes)
    {
        if (info.m_Mode.name == infomodeName)
        {
            Entity entity = prefabSystem.GetEntity(info.m_Mode);
            toolSystem.SetInfomodeActive(entity, active, info.m_Priority);
            return;
        }
    }
}
```

### Example 3: Read Active Overlay Color Configuration

Inspect which shader colors the current info view is using, useful for debugging or adapting custom UI elements.

```csharp
public void LogActiveInfoviewColors(ToolSystem toolSystem)
{
    NativeArray<Vector4> colors = toolSystem.GetActiveInfoviewColors();
    if (colors.Length == 0) return;

    // Index 0 = default color, indices 1+ = infomode colors (3 per mode: low, mid, high)
    Log.Info($"Default color: {colors[0]}");

    for (int i = 1; i < Math.Min(colors.Length / 3, 13); i++)
    {
        Vector4 low = colors[i * 3];
        Vector4 mid = colors[i * 3 + 1];
        Vector4 high = colors[i * 3 + 2];

        if (low != default || mid != default || high != default)
        {
            Log.Info($"Infomode {i}: Low={low}, Mid={mid}, High={high}");
        }
    }
}
```

### Example 4: Custom Heatmap Overlay via Harmony

Patch OverlayInfomodeSystem to inject custom data into the terrain overlay texture. This example adds a custom "danger zone" heatmap.

```csharp
[HarmonyPatch(typeof(OverlayInfomodeSystem), "OnUpdate")]
public static class CustomHeatmapPatch
{
    // Postfix: after standard overlays are rendered, inject custom data
    static void Postfix(OverlayInfomodeSystem __instance)
    {
        // Access the terrain texture via reflection or Traverse
        var terrainSystem = World.DefaultGameObjectInjectionWorld
            .GetOrCreateSystemManaged<TerrainRenderSystem>();

        Texture2D overlayTex = terrainSystem.overrideOverlaymap as Texture2D;
        if (overlayTex == null) return;

        // Write custom data to an unused RGBA channel
        NativeArray<byte> texData = overlayTex.GetRawTextureData<byte>();
        int width = overlayTex.width;
        int height = overlayTex.height;

        // Channel 3 (alpha) if unused by current infomodes
        int channel = 3;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (x + y * width) * 4 + channel;
                // Custom value calculation per cell
                texData[idx] = CalculateCustomValue(x, y, width, height);
            }
        }
        overlayTex.Apply();
    }

    static byte CalculateCustomValue(int x, int y, int w, int h)
    {
        // Example: radial gradient from center
        float cx = (float)x / w - 0.5f;
        float cy = (float)y / h - 0.5f;
        float dist = Mathf.Sqrt(cx * cx + cy * cy) * 2f;
        return (byte)Mathf.Clamp(Mathf.RoundToInt((1f - dist) * 255f), 0, 255);
    }
}
```

### Example 5: Listen for Info View Change Events

Register a callback that fires whenever the player switches info views, useful for syncing mod UI panels.

```csharp
public partial class InfoviewChangeListener : GameSystemBase
{
    private ToolSystem _toolSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

        _toolSystem.EventInfoviewChanged += OnInfoviewChanged;
        _toolSystem.EventInfomodesChanged += OnInfomodesChanged;
    }

    protected override void OnDestroy()
    {
        _toolSystem.EventInfoviewChanged -= OnInfoviewChanged;
        _toolSystem.EventInfomodesChanged -= OnInfomodesChanged;
        base.OnDestroy();
    }

    private void OnInfoviewChanged(InfoviewPrefab newView, InfoviewPrefab oldView)
    {
        string oldName = oldView?.name ?? "None";
        string newName = newView?.name ?? "None";
        Log.Info($"Info view changed: {oldName} -> {newName}");

        // React to specific views
        if (newView != null && newView.name == "Pollution")
        {
            // Enable custom mod panel for pollution analysis
        }
    }

    private void OnInfomodesChanged()
    {
        Log.Info("Infomodes toggled within active view");
    }

    protected override void OnUpdate() { }
}
```

## Open Questions

- [ ] **Custom InfoviewPrefab registration**: Creating entirely new InfoviewPrefab instances at runtime (rather than patching existing ones) may require hooking into the PrefabSystem's initialization pipeline. The exact registration API for mod-created prefabs needs testing.
- [ ] **Shader channel limits**: The system supports 3 color groups x 4 infomodes = 12 simultaneous infomodes. Adding beyond this limit may require shader modifications or a custom rendering path.
- [ ] **Water overlay independence**: Some heatmaps write to both terrain and water textures (AirPollution, Noise, TelecomCoverage, Oil). The secondary index mechanism (`m_SecondaryIndex`) manages this, but custom overlays need to handle both surfaces explicitly.
- [ ] **UI panel extensibility**: Whether InfoviewUISystemBase subclasses can be registered by mods without patching, or if the UI framework requires Harmony injection to display custom statistics panels.

## Sources

- Decompiled from: Game.dll — Game.Prefabs.InfoviewPrefab, Game.Prefabs.InfomodePrefab, Game.Prefabs.InfoviewInitializeSystem, Game.Prefabs.HeatmapInfomodePrefab, Game.Prefabs.GradientInfomodeBasePrefab, Game.Prefabs.ColorInfomodeBasePrefab, Game.Prefabs.BuildingInfomodePrefab, Game.Prefabs.ServiceCoverageInfomodePrefab, Game.Prefabs.NetStatusInfomodePrefab, Game.Prefabs.VehicleInfomodePrefab, Game.Prefabs.EffectRangeInfomodePrefab, Game.Prefabs.BuildingStatusInfomodePrefab
- Runtime components: Game.Prefabs.InfomodeActive, Game.Prefabs.InfoviewData, Game.Prefabs.InfoviewMode, Game.Prefabs.InfomodeData, Game.Prefabs.InfoviewHeatmapData
- Rendering: Game.Rendering.OverlayInfomodeSystem, Game.Rendering.HeatmapData
- UI: Game.UI.InGame.InfoviewsUISystem, Game.UI.InGame.InfoviewUISystemBase, Game.UI.InGame.PollutionInfoviewUISystem
- Tools: Game.Tools.ToolSystem, Game.Tools.InfoviewUtils
