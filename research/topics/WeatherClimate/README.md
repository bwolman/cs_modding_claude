# Research: Weather & Climate

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 models weather, climate, seasons, snow accumulation, wind simulation, and weather phenomena (thunderstorms, tornadoes). How weather affects gameplay through fire ignition, building damage, traffic accidents, and snow coverage.

**Why**: To build mods that can read or override weather state, create custom weather events, modify snow/wind behavior, or react to weather conditions in gameplay logic.

**Boundaries**: This research covers the simulation-side weather pipeline. Rendering effects (volumetric clouds, rain particles, fog shaders) and audio systems are out of scope except where they connect to simulation state.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | ClimateSystem, SnowSystem, WindSystem, WindSimulationSystem, WeatherHazardSystem, WeatherPhenomenonSystem, WeatherDamageSystem |
| Game.dll | Game.Events | WeatherPhenomenon (component), FacingWeather, FaceWeather, FaceWeatherSystem |
| Game.dll | Game.Prefabs | WeatherPhenomenonData, WeatherPhenomenon (prefab), WeatherObject, WindPowered, WindPoweredData |
| Game.dll | Game.Prefabs.Climate | ClimatePrefab, ClimateData, SeasonData, WeatherData |
| Game.dll | Game.Rendering | ClimateRenderSystem, WindTextureSystem, WindControl |

## Architecture Overview

The weather system has four layers:

1. **Climate Layer** (`ClimateSystem`) -- evaluates animation curves from a `ClimatePrefab` to produce continuous temperature, precipitation, cloudiness, aurora, and fog values. These change smoothly over the in-game year.

2. **Weather Phenomena Layer** (`WeatherHazardSystem` + `WeatherPhenomenonSystem`) -- probabilistically spawns discrete weather events (thunderstorms, tornadoes) based on current climate conditions. Each event has a moving hotspot that damages buildings and causes traffic accidents.

3. **Snow Layer** (`SnowSystem`) -- GPU-based 1024x1024 simulation that accumulates snow when temperature is below freezing and precipitation is active, and melts it when temperature rises.

4. **Wind Layer** (`WindSimulationSystem` + `WindSystem`) -- 3D pressure-velocity simulation that produces a 64x64 2D wind map. Wind moves weather phenomena and redistributes snow.

### Climate Sampling

`ClimateSystem` is the central hub. Every frame it:
1. Evaluates the `ClimatePrefab` animation curves at the current `normalizedDate` (0..1 over the year)
2. Sets `temperature`, `precipitation`, `cloudiness`, `aurora`, `fog` as `OverridableProperty<float>` values
3. Updates the current season by finding which `SeasonInfo` range contains the current date
4. Updates weather visual effects based on cloudiness level

All properties are `OverridableProperty<float>`, meaning the editor (or a mod) can override them directly.

### Seasonal Cycle

Seasons are defined by `SeasonInfo` entries in the `ClimatePrefab`. Each season has:
- A start time (0..1 normalized date)
- Night/day temperature ranges with deviation
- Cloud chance and amount
- Precipitation chance and amount
- Turbulence, aurora amount/chance

The system determines `isRaining` vs `isSnowing` by comparing temperature to `freezingTemperature` (from the ClimatePrefab).

### Weather Classification

The `WeatherClassification` enum categorizes the current visual weather state:
- `Clear`, `Few`, `Scattered`, `Broken`, `Overcast`, `Stormy`

This is set by `ApplyWeatherEffects()` based on the active weather prefab stack.

## Component Map

### `WeatherPhenomenon` (Game.Events)

Runtime ECS component on an active weather event entity.

| Field | Type | Description |
|-------|------|-------------|
| m_PhenomenonPosition | float3 | Center of the weather cell (moves with wind) |
| m_HotspotPosition | float3 | Center of the damage hotspot |
| m_HotspotVelocity | float3 | Hotspot movement velocity |
| m_PhenomenonRadius | float | Outer radius of the weather cell |
| m_HotspotRadius | float | Inner radius of the damage zone |
| m_Intensity | float | 0..1, ramps up at +0.2/sec, down at -0.2/sec |
| m_LightningTimer | float | Countdown to next lightning strike |

### `WeatherPhenomenonData` (Game.Prefabs)

Prefab configuration for a weather phenomenon type.

| Field | Type | Description |
|-------|------|-------------|
| m_OccurenceProbability | float | Base probability per hazard tick |
| m_HotspotInstability | float | How much the hotspot wanders within the cell |
| m_DamageSeverity | float | Damage multiplier; 0 = harmless (fog, rain) |
| m_DangerLevel | float | Danger level for early warning system |
| m_PhenomenonRadius | Bounds1 | Min/max outer radius |
| m_HotspotRadius | Bounds1 | Min/max hotspot radius |
| m_LightningInterval | Bounds1 | Min/max seconds between lightning strikes |
| m_Duration | Bounds1 | Min/max duration |
| m_OccurenceTemperature | Bounds1 | Temperature range that allows this phenomenon |
| m_OccurenceRain | Bounds1 | Precipitation range that allows this phenomenon |
| m_OccurenceCloudiness | Bounds1 | Cloudiness range that allows this phenomenon |
| m_DangerFlags | DangerFlags | Evacuate, StayIndoors flags |

### `FacingWeather` (Game.Events)

Attached to buildings within a weather phenomenon's hotspot.

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The weather phenomenon event entity |
| m_Severity | float | Current damage severity (distance-based) |

### `Wind` (Game.Simulation)

Per-cell wind data in the 64x64 wind map.

| Field | Type | Description |
|-------|------|-------------|
| m_Wind | float2 | XZ wind velocity |

### `ClimateSample` (Game.Simulation.ClimateSystem)

Snapshot of climate state at a point in time.

| Field | Type | Description |
|-------|------|-------------|
| temperature | float | Current temperature |
| precipitation | float | Precipitation amount (0..1) |
| cloudiness | float | Cloud coverage (0..1) |
| aurora | float | Aurora intensity |
| fog | float | Fog intensity |

## System Map

### `ClimateSystem` (Game.Simulation)

Central orchestrator for all weather state.

- **Base class**: GameSystemBase (also ISerializable, IPreSerialize, IPostDeserialize)
- **Update phase**: Every frame
- **Key properties**: `temperature`, `precipitation`, `cloudiness`, `aurora`, `fog` (all OverridableProperty<float>)
- **Key methods**:
  - `SampleClimate(ClimatePrefab, float t)` -- evaluate curves at normalized date
  - `SampleClimate(float t)` -- includes override state
- **Trigger events**: Fires `TriggerType.Temperature`, `WeatherStormy`, `WeatherRainy`, `WeatherSnowy`, `WeatherSunny`, `WeatherClear`, `WeatherCloudy`, `AuroraBorealis`
- **Season tracking**: `currentSeason`, `currentSeasonName`, `seasonTemperature`, `seasonPrecipitation`

### `WeatherHazardSystem` (Game.Simulation)

Spawns weather phenomenon events based on probability.

- **Update interval**: 2048 frames
- **Logic**: For each `WeatherPhenomenonData` prefab (not locked):
  - Compute temperature factor: Gaussian centered on `m_OccurenceTemperature`
  - Compute rain factor: linear ramp within `m_OccurenceRain` bounds
  - Compute cloudiness factor: linear ramp within `m_OccurenceCloudiness` bounds
  - Final probability = `m_OccurenceProbability * tempFactor * rainFactor * cloudFactor * 34.13`
  - Roll random(100) < probability to spawn event
  - Damaging events require `naturalDisasters` enabled in city config

### `WeatherPhenomenonSystem` (Game.Simulation)

Simulates active weather phenomena.

- **Update interval**: 16 frames
- **Logic per phenomenon**:
  - Ramp intensity up/down at 0.2 per second
  - Move phenomenon position with wind (* 20)
  - Move hotspot within phenomenon using wind + random instability
  - If `m_DamageSeverity != 0`: find affected buildings, create `FaceWeather` events
  - Lightning: find tallest object within hotspot, roll fire ignition probability
  - Traffic: find moving cars within hotspot, cause loss-of-control impacts
  - Danger warnings: project path ahead using wind direction, warn buildings in danger zone

### `WeatherDamageSystem` (Game.Simulation)

Applies damage to buildings with `FacingWeather` component.

- **Update interval**: 64 frames
- **Logic**: damage rate = `severity / structuralIntegrity * timeDelta`, capped at 0.5/tick
- Buildings with structural integrity >= 100M are immune
- City modifier `DisasterDamageRate` scales damage for buildings
- At 100% damage: creates Destroy event

### `SnowSystem` (Game.Simulation)

GPU-based snow simulation.

- **Update interval**: 4 frames
- **Texture**: 1024x1024, R16G16_UNorm (snow depth + water content)
- **AddSnow kernel**: Accumulates snow when `temperature <= freezingTemperature` and `precipitation > 0`. Melts when `temperature > freezing`. Height-adjusted: `snowTemperatureHeightScale = 0.01` (1 degree per 100 elevation units).
- **SnowTransfer kernel**: Redistributes snow by wind. Only runs when precipitating and cold.
- **Constants**: AddMultiplier=1e-5, MeltMultiplier=2e-5, WaterAddMultiplier=0.1, ElapseWaterMultiplier=0.05

### `WindSystem` (Game.Simulation)

2D wind map for gameplay systems.

- **Base class**: CellMapSystem<Wind>
- **Grid**: 64x64 cells
- **Update interval**: 512 frames (game simulation), 1 frame (other phases)
- Copies from 3D `WindSimulationSystem` cells to 2D map by sampling at terrain height

### `WindSimulationSystem` (Game.Simulation)

3D wind pressure-velocity simulation.

- **Grid**: 64x64xN (3D)
- **Physics**: Pressure-velocity model with terrain obstruction
- **Constants**: kAirSlowdown, kTerrainSlowdown, kVerticalSlowdown, kChangeFactor
- **Edge conditions**: Wind enters from map edges based on `ClimateSystem.wind` direction

## Data Flow

### Climate -> Weather -> Damage Pipeline

```
ClimatePrefab (animation curves for temperature, precipitation, cloudiness)
    |
    v
ClimateSystem.OnUpdate() [every frame]
    |-- Evaluate curves at normalizedDate
    |-- Set: temperature, precipitation, cloudiness, aurora, fog
    |-- Update season info
    |-- Fire trigger events (WeatherRainy, WeatherSnowy, etc.)
    |
    v
WeatherHazardSystem [every 2048 frames]
    |-- For each WeatherPhenomenonData prefab:
    |     probability = baseProbability * tempFactor * rainFactor * cloudFactor * dt
    |     if random(100) < probability: create event entity
    |
    v
WeatherPhenomenonSystem [every 16 frames]
    |-- For each active WeatherPhenomenon entity:
    |     Move with wind, update hotspot
    |     Find affected buildings -> FaceWeather events
    |     Lightning -> Ignite events (fire)
    |     Traffic -> Impact events (vehicle accidents)
    |
    v
WeatherDamageSystem [every 64 frames]
    |-- For each building with FacingWeather:
    |     Apply damage based on severity / structural integrity
    |     Destroy building at 100% damage
```

### Snow Pipeline

```
ClimateSystem.temperature + ClimateSystem.precipitation
    |
    v
SnowSystem.OnUpdate() [every 4 frames]
    |
    |-- AddSnow (GPU compute)
    |     If temp <= freezing && precipitation > 0: accumulate snow
    |     If temp > freezing: melt snow (rate = 2x accumulation)
    |     Height-adjusted: higher elevation = colder = more snow
    |
    |-- SnowTransfer (GPU compute)
    |     Only when precipitating and cold
    |     Redistribute snow based on wind direction
    |
    |-- UpdateSnowBackdropTexture
    |     Write to global _SnowMap texture for rendering
```

### Wind Pipeline

```
ClimateSystem.wind (base wind direction)
    |
    v
WindSimulationSystem [continuous]
    |-- 3D pressure-velocity simulation
    |-- Terrain obstructs airflow
    |-- Edge boundary: wind enters from map edge
    |
    v
WindSystem [every 512 frames]
    |-- Copy 3D cells to 2D map at terrain height
    |-- 64x64 cell map
    |
    v
Consumer systems:
  - WeatherPhenomenonSystem (moves phenomena with wind)
  - SnowSystem (redistributes snow with wind)
  - WindPowered buildings (wind turbine power output)
```

## Cross-System Interactions

| Weather State | Affected System | Effect |
|---------------|----------------|--------|
| Lightning strike | FireIgnition (IgniteSystem) | Can ignite buildings/trees if FireData.m_StartProbability > 0.01 |
| Hotspot over road | TrafficAccident (Impact) | Vehicles lose control (severity=5, angular velocity delta) |
| Hotspot over building | WeatherDamageSystem | Incremental structural damage, eventual destruction |
| Temperature <= freezing + precipitation | SnowSystem | Snow accumulation on terrain |
| Temperature > freezing | SnowSystem | Snow melts |
| Precipitation > 0 + temp > freezing | ClimateSystem.isRaining | Triggers TriggerType.WeatherRainy |
| Wind direction | WeatherPhenomenonSystem | Moves weather phenomena across the map |
| Danger flags on phenomenon | Emergency system | Buildings get InDanger component, evacuation warnings |

## Harmony Patch Points

### Candidate 1: `ClimateSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Override or modify temperature/precipitation/cloudiness after sampling
- **Risk level**: Low -- the system just sets OverridableProperty values
- **Better alternative**: Use `OverridableProperty.overrideValue` directly (no patch needed)

### Candidate 2: `WeatherHazardSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix (return false to skip)
- **What it enables**: Prevent all weather phenomena from spawning
- **Risk level**: Low -- prevents event creation but does not affect existing events

### Candidate 3: `WeatherDamageSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix (return false to skip)
- **What it enables**: Prevent all weather damage to buildings
- **Risk level**: Low

### Candidate 4: No-patch approach: Override ClimateSystem properties

- **Signature**: N/A
- **Patch type**: N/A
- **What it enables**: Set `ClimateSystem.temperature.overrideValue`, `precipitation.overrideValue`, etc.
- **Risk level**: None -- designed to be overridden

## Mod Blueprint: Weather Controller

A mod to override weather conditions and control phenomena.

### Strategy

1. Get `ClimateSystem` from the World
2. Use `OverridableProperty` overrides to set temperature, precipitation, cloudiness directly
3. Optionally patch `WeatherHazardSystem` to prevent/force specific phenomena

### Systems to create

1. **WeatherControlSystem** (`GameSystemBase`) -- reads mod settings, applies overrides:
   - `ClimateSystem.temperature.overrideState = true; .overrideValue = X`
   - Same for precipitation, cloudiness, aurora, fog
2. **WeatherControlUISystem** (`UISystemBase`) -- binds to UI for slider controls

### Components to add

None required -- overrides work on existing system properties.

### Patches needed

- **Optional**: Prefix on `WeatherHazardSystem.OnUpdate` to suppress/force event spawning

### Settings

- Temperature override (slider)
- Precipitation override (slider)
- Cloudiness override (slider)
- Disable natural disasters toggle
- Force specific weather phenomenon

## Seasonal Detection via ClimateSystem

To determine the current discrete season (Spring/Summer/Autumn/Winter), use the `ClimateSystem.currentClimate` entity to get the `ClimatePrefab`, then call `FindSeasonByTime()` with the normalized date:

```csharp
var climateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

Entity climateEntity = climateSystem.currentClimate;
ClimatePrefab climatePrefab = prefabSystem.GetPrefab<ClimatePrefab>(climateEntity);
var seasonInfo = climatePrefab.FindSeasonByTime(climateSystem.currentDate);
string seasonName = seasonInfo.Item1.name; // "SeasonSpring", "SeasonSummer", etc.
float seasonProgress = seasonInfo.Item2;   // 0-1 progress within season
```

**Season name constants**: `"SeasonSpring"`, `"SeasonSummer"`, `"SeasonAutumn"`, `"SeasonWinter"`.

**Converting to enum**: Use `FoliageUtils.GetSeasonFromSeasonID(seasonName)` to get a typed season value.

**`ClimateSystem.currentDate`**: Normalized 0-1 float representing position in the year (0 = year start, 1 = year end).

## Wind Rendering Control

`Game.Rendering.WindControl` manages rendering-side wind effects (tree sway, vegetation movement), separate from the simulation-side `WindSystem`. The key data structure is `WindVolumeComponent`:

| Field | Type | Description |
|-------|------|-------------|
| `windGlobalStrengthScale` | OverridableProperty\<float\> | Primary wind strength multiplier |
| `windGlobalStrengthScale2` | OverridableProperty\<float\> | Secondary wind strength multiplier |
| `windDirection` | OverridableProperty\<float\> | Wind direction angle |
| `windDirectionVariance` | OverridableProperty\<float\> | Direction variance range |
| `windDirectionVariancePeriod` | OverridableProperty\<float\> | Variance oscillation period |
| `windParameterInterpolationDuration` | OverridableProperty\<float\> | Blend speed for changes |

**Disabling tree sway**: Set both strength scales to 0 via a Harmony prefix on `WindControl.SetGlobalProperties`:

```csharp
[HarmonyPatch(typeof(Game.Rendering.WindControl), "SetGlobalProperties")]
public static class WindDisablePatch
{
    public static bool Prefix(ref WindVolumeComponent wind)
    {
        wind.windGlobalStrengthScale.Override(0f);
        wind.windGlobalStrengthScale2.Override(0f);
        return true; // Continue to original method
    }
}
```

**Pause-aware**: Check `SimulationSystem.selectedSpeed == 0` to detect pause state and conditionally disable wind during pause.

## Open Questions

- [x] How are climate values computed? Via animation curves in ClimatePrefab, evaluated at normalizedDate
- [x] Can weather properties be overridden? Yes, all are OverridableProperty<float>
- [x] How do weather phenomena spawn? Probability roll in WeatherHazardSystem based on temperature/rain/cloudiness
- [x] How does lightning cause fires? Lightning strike finds tallest object, rolls FireData.m_StartProbability
- [x] How does snow accumulate? GPU compute shader, when temp <= freezing and precipitation > 0
- [ ] What are the exact ClimatePrefab curve shapes for the default climates? Requires examining prefab assets in-game
- [ ] How does the weather effect stack (m_DefaultWeathers) determine visual transitions? The placeholder selection logic is complex; in-game testing needed
- [ ] Can SnowSystem.SnowSimSpeed be safely modified at runtime? The property has a public setter, but GPU timing implications are unclear

## Sources

- Decompiled from: Game.dll -- Game.Simulation.ClimateSystem, Game.Simulation.WeatherHazardSystem, Game.Simulation.WeatherPhenomenonSystem, Game.Simulation.WeatherDamageSystem, Game.Simulation.SnowSystem, Game.Simulation.WindSystem, Game.Simulation.WindSimulationSystem, Game.Simulation.Wind
- Event components: Game.Events.WeatherPhenomenon, Game.Events.FacingWeather, Game.Events.FaceWeather
- Prefab types: Game.Prefabs.WeatherPhenomenonData, Game.Prefabs.Climate.ClimatePrefab, Game.Prefabs.Climate.ClimateData, Game.Prefabs.Climate.SeasonData, Game.Prefabs.Climate.WeatherData
- All decompiled snippets saved in `snippets/` directory
