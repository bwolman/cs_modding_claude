# Research: Crime Trigger

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-17

## Scope

**What we're investigating**: How to programmatically make a crime occur at a specific location in CS2 -- who becomes a criminal, how crime events are created, how the criminal is bound to a building, and the full criminal lifecycle from planning through arrest/escape.

**Why**: To build mods that trigger crime events on specific citizens or locations, modify crime probabilities, or create custom crime scenarios.

**Boundaries**: Not covering police vehicle AI/pathfinding, prison management, or crime statistics UI. Focused on the programmatic crime trigger and criminal lifecycle mechanisms.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Citizens | `Criminal`, `CriminalFlags`, `CrimeVictim` |
| Game.dll | Game.Events | `Crime` (tag), `AddCriminal`, `AddCriminalSystem`, `InitializeSystem`, `AccidentSite`, `AccidentSiteFlags`, `AddAccidentSite`, `TargetElement` |
| Game.dll | Game.Buildings | `CrimeProducer` |
| Game.dll | Game.Simulation | `CrimeCheckSystem`, `CriminalSystem`, `CrimeAccumulationSystem`, `AccidentSiteSystem` |
| Game.dll | Game.Prefabs | `CrimeData`, `CrimeType`, `CrimeAccumulationData`, `PoliceConfigurationData`, `Crime` (ComponentBase), `EventData` |

## Component Map

### `Criminal` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime event entity this criminal is participating in |
| m_JailTime | ushort | Remaining jail/prison time in update ticks |
| m_Flags | CriminalFlags | State flags for the criminal lifecycle |

Placed on citizen entities when they become criminals. Persisted via `ISerializable`. Removed when `m_Flags` becomes 0 (released/escaped).

*Source: `Game.dll` -> `Game.Citizens.Criminal`*

### `CriminalFlags` (Game.Citizens)

```csharp
[Flags]
public enum CriminalFlags : ushort
{
    Robber    = 1,    // Citizen is a robber (base criminal type)
    Prisoner  = 2,    // Serving prison sentence
    Planning  = 4,    // Just became criminal, will travel to crime target next tick
    Preparing = 8,    // Traveling to crime target (has TripNeeded Purpose.Crime)
    Monitored = 0x10, // Police monitoring (from CriminalMonitorProbability city modifier)
    Arrested  = 0x20, // Arrested, held in police station
    Sentenced = 0x40  // Sentenced to prison (waiting for transport)
}
```

These flags represent a state machine. A criminal progresses through: `Planning` -> `Preparing` -> `Robber` (at target) -> either `Arrested` -> possibly `Sentenced` -> `Prisoner`, or escapes.

*Source: `Game.dll` -> `Game.Citizens.CriminalFlags`*

### `Crime` (Game.Events) -- Tag Component

Empty tag component placed on crime event entities (like `TrafficAccident` for traffic events). Crime event entities also have a `TargetElement` buffer containing the criminal citizen. Defined by `Crime.GetArchetypeComponents()` in the prefab authoring component.

*Source: `Game.dll` -> `Game.Events.Crime`*

### `AddCriminal` (Game.Events) -- Event Command

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime event entity |
| m_Target | Entity | The citizen to make a criminal |
| m_Flags | CriminalFlags | Initial flags (e.g., Robber \| Planning) |

Temporary event entity (with `Game.Common.Event` tag) that commands `AddCriminalSystem` to add the `Criminal` component to a citizen. Created by `InitializeSystem.InitializeCrimeEvent` when a new crime event entity is detected. Follows the same command-entity pattern as `AddHealthProblem`.

*Source: `Game.dll` -> `Game.Events.AddCriminal`*

### `EventData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_Archetype | EntityArchetype | The archetype used to create event instance entities |
| m_ConcurrentLimit | int | Maximum concurrent instances (0 = unlimited) |

Stored on event prefab entities. For crime events, the archetype includes `Game.Events.Crime` (tag) + `TargetElement` (buffer). `CrimeCheckSystem` reads `m_Archetype` to create properly-typed crime event entities.

*Source: `Game.dll` -> `Game.Prefabs.EventData`*

### `CrimeProducer` (Game.Buildings)

| Field | Type | Description |
|-------|------|-------------|
| m_PatrolRequest | Entity | Active police patrol request for this building |
| m_Crime | float | Accumulated crime level on this building |
| m_DispatchIndex | byte | Dispatch routing index |

Placed on buildings that can accumulate crime. Crime accumulates based on zone type crime rate, police coverage, and district/city modifiers. When `m_Crime` exceeds `m_CrimeAccumulationTolerance`, a police patrol is requested.

*Source: `Game.dll` -> `Game.Buildings.CrimeProducer`*

### `CrimeVictim` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Effect | byte | Wellbeing impact from being a crime victim (0-255) |

Implements `IEnableableComponent` -- enabled when the citizen has been affected by crime. Effect is cumulative (multiple crimes stack). Applied to household members (m_HomeCrimeEffect = 15) and employees (m_WorkplaceCrimeEffect = 5) of the robbed entity.

*Source: `Game.dll` -> `Game.Citizens.CrimeVictim`*

### `AccidentSite` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime/accident event entity |
| m_PoliceRequest | Entity | Active police emergency request |
| m_Flags | AccidentSiteFlags | State flags for the site |
| m_CreationFrame | uint | Frame when the site was created |
| m_SecuredFrame | uint | Frame when police secured the site |

Placed on buildings where a crime or traffic accident is occurring. Used by `CriminalSystem` to determine whether the criminal should be arrested (site Secured) or can commit the crime (CrimeScene but not Secured). State transitions managed by `AccidentSiteSystem` (every 64 frames).

*Source: `Game.dll` -> `Game.Events.AccidentSite`*

### `AccidentSiteFlags` (Game.Events)

```csharp
[Flags]
public enum AccidentSiteFlags : uint
{
    StageAccident  = 1,     // Staging area for accident
    Secured        = 2,     // Police have secured the scene
    CrimeScene     = 4,     // This is a crime scene (vs traffic accident)
    TrafficAccident = 8,    // This is a traffic accident
    CrimeFinished  = 0x10,  // Crime has been completed (duration expired)
    CrimeDetected  = 0x20,  // Crime has been detected (alarm delay expired)
    CrimeMonitored = 0x40,  // Crime is being monitored (from CriminalMonitorProbability)
    RequirePolice  = 0x80,  // Police dispatch requested (set by AccidentSiteSystem)
    MovingVehicles = 0x100  // Vehicles involved are moving
}
```

*Source: `Game.dll` -> `Game.Events.AccidentSiteFlags`*

### `AddAccidentSite` (Game.Events) -- Event Command

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime event entity |
| m_Target | Entity | The building to mark as crime scene |
| m_Flags | AccidentSiteFlags | Flags to set (CrimeScene for crimes) |

Temporary event entity (with `Game.Common.Event` tag) that commands the system to add an `AccidentSite` component to a building. Processed by `AddAccidentSiteSystem`.

*Source: `Game.dll` -> `Game.Events.AddAccidentSite`*

### `CrimeData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_RandomTargetType | EventTargetType | Target type (Citizen) |
| m_CrimeType | CrimeType | Type of crime (only Robbery exists) |
| m_OccurenceProbability | Bounds1 | First-time crime probability range (default 0-50) |
| m_RecurrenceProbability | Bounds1 | Repeat crime probability range (default 0-100) |
| m_AlarmDelay | Bounds1 | Seconds before police are notified (default 5-10) |
| m_CrimeDuration | Bounds1 | How long crime lasts before criminal leaves (default 20-60) |
| m_CrimeIncomeAbsolute | Bounds1 | Absolute money stolen (default 100-1000) |
| m_CrimeIncomeRelative | Bounds1 | Percentage of victim's money stolen (default 0-0.25) |
| m_JailTimeRange | Bounds1 | Jail time in game days (default 0.125-1) |
| m_PrisonTimeRange | Bounds1 | Prison time in game days (default 1-100) |
| m_PrisonProbability | float | Chance of jail -> prison sentence (default 50%) |

Set on crime event prefab entities. The `Crime` ComponentBase class copies these values from the prefab authoring component via `Initialize()`.

*Source: `Game.dll` -> `Game.Prefabs.CrimeData`*

### `CrimeType` (Game.Prefabs)

```csharp
public enum CrimeType
{
    Robbery  // Only crime type in the game
}
```

*Source: `Game.dll` -> `Game.Prefabs.CrimeType`*

### `PoliceConfigurationData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_PoliceServicePrefab | Entity | Police service prefab reference |
| m_TrafficAccidentNotificationPrefab | Entity | Notification icon for traffic accidents |
| m_CrimeSceneNotificationPrefab | Entity | Notification icon for crime scenes |
| m_MaxCrimeAccumulation | float | Max crime a building can accumulate (default 100000) |
| m_CrimeAccumulationTolerance | float | Crime threshold for police dispatch (default 1000) |
| m_HomeCrimeEffect | int | CrimeVictim effect on household members (default 15) |
| m_WorkplaceCrimeEffect | int | CrimeVictim effect on employees (default 5) |
| m_WelfareCrimeRecurrenceFactor | float | Welfare coverage factor reducing repeat crime (default 0.4) |
| m_CrimePoliceCoverageFactor | float | Police coverage impact on crime accumulation (default 2) |
| m_CrimePopulationReduction | float | Population divisor for crime probability scaling (default 2000) |

Global singleton. Set via `PoliceConfigurationPrefab`.

*Source: `Game.dll` -> `Game.Prefabs.PoliceConfigurationData`*

### `CrimeAccumulationData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_CrimeRate | float | Zone crime rate (default 7) |

Per-zone-type configuration set via `CrimeAccumulation` ComponentBase on zone/service prefabs.

*Source: `Game.dll` -> `Game.Prefabs.CrimeAccumulationData`*

## System Map

### `CrimeCheckSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16384 frames (once per game day, 16 partitions)
- **Queries**:
  - Citizen query: `Citizen` + `UpdateFrame`, excluding `HealthProblem`, `Worker`, `Student`, `Deleted`, `Temp`
  - Event query: `CrimeData`, excluding `Locked`
  - PoliceConfig query: `PoliceConfigurationData`, excluding `Locked`
- **Key logic**:
  - Only unemployed, non-student, healthy citizens can become criminals (children and elderly excluded)
  - Crime probability is driven by `Citizen.m_WellBeing`:
    - WellBeing <= 25: `t = wellbeing / 25` (linear, low wellbeing = high crime chance)
    - WellBeing > 25: `t = ((100 - wellbeing) / 75)^2` (quadratic falloff)
  - Probability scaled by city population: `randomRange = max(pop / 2000 * 100, 100)`
  - Repeat criminals checked against welfare coverage on their home property
  - `debugFullCrimeMode` flag bypasses all probability checks
  - Modified by `CityModifier.CrimeProbability`
- **Output**: Creates crime event entities with `Crime` tag + `TargetElement` buffer + `PrefabRef`. Does NOT create `AddCriminal` commands -- that is done by `InitializeSystem`.

### `InitializeSystem` (Game.Events) -- NEW DISCOVERY

- **Base class**: GameSystemBase
- **Update**: Processes entities with `Created` + `Event` tags (runs when new events appear)
- **Key logic for crime**:
  - Detects new crime event entities (those with `Crime` tag component)
  - Calls `InitializeCrimeEvent()`:
    1. Reads `CrimeData` from the event's prefab
    2. If no targets in `TargetElement` buffer, picks a random citizen
    3. Resolves `Creature.Resident` to underlying `Citizen` entity
    4. For each citizen target: creates an `AddCriminal` command entity
    5. Sets flags: `Planning` + `Robber` (for Robbery type)
  - Uses `m_CriminalEventArchetype` = `Event` + `AddCriminal`
- **This is the bridge**: `CrimeCheckSystem` creates crime event entities -> `InitializeSystem` creates `AddCriminal` commands -> `AddCriminalSystem` adds the `Criminal` component

### `AddCriminalSystem` (Game.Events)

- **Base class**: GameSystemBase
- **Queries**:
  - AddCriminal query: `Event` + `AddCriminal`
- **Key logic**:
  - Processes `AddCriminal` event entities to add `Criminal` component to target citizens
  - If citizen already has `Criminal`: merges flags (Prisoner takes priority, non-null event takes priority)
  - Adds the citizen to the crime event's `TargetElement` buffer
  - Uses `ModificationBarrier4` for command buffer

### `CriminalSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames (very frequent)
- **Queries**:
  - Criminal query: `Citizen` + `UpdateFrame` + `Criminal`, excluding `Deleted`, `Temp`
  - PoliceConfig query: `PoliceConfigurationData`
- **Key logic** (criminal lifecycle state machine):
  1. **Flags == 0**: Remove Criminal component (no longer a criminal)
  2. **Prisoner**: In prison, decrement `m_JailTime`. When 0: release. If prison Inactive: release immediately.
  3. **Arrested (no Sentenced)**: In police station, decrement `m_JailTime`. When 0: roll `m_PrisonProbability`. If wins: set Sentenced, compute prison time. If loses: release.
  4. **Arrested + Sentenced**: Waiting for prisoner transport vehicle (PublicTransport, Boarding). If available: GoToPrison. If timeout (m_JailTime reaches 0): release.
  5. **Planning**: Add `TripNeeded(Purpose.Crime)`, roll CriminalMonitorProbability for Monitored flag, transition to Preparing.
  6. **Preparing**: Wait for citizen to arrive at crime destination (TripNeeded Purpose.Crime consumed).
  7. **Robber (at destination)**: Check `CurrentBuilding` for `AccidentSite`:
     - **No AccidentSite**: Call `AddCrimeScene()` to create `AddAccidentSite(CrimeScene)` event
     - **AccidentSite exists, Secured + police car AtTarget**: `GoToJail()` (arrested), compute jail time from CrimeData
     - **AccidentSite CrimeScene, not Secured, not CrimeFinished, same event**: Wait (crime in progress)
     - **Otherwise**: Commit crime -- `GetCrimeSource()` (random Renter), `GetStealAmount()`, `AddCrimeEffects()`, `TryEscape()`
- **GetCrimeSource**: Picks a random `Renter` from the building's `Renter` buffer
- **GetStealAmount**: `relative% * source money + absolute random amount` from CrimeData
- **AddCrimeEffects**: Queues `CrimeVictim` effects -- `m_HomeCrimeEffect` on household members, `m_WorkplaceCrimeEffect` on employees
- **TryEscape**: Clears trips, adds `TripNeeded(Purpose.Escape)`, clears m_Event and Monitored flag

### `AccidentSiteSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 64 frames
- **Queries**:
  - AccidentSite query: `AccidentSite`, excluding `Deleted`, `Temp`
  - PoliceConfig query: `PoliceConfigurationData`
- **Key logic** (crime scene state transitions):
  1. **Staging timeout**: If 3600+ frames since creation, clears `StageAccident`
  2. **Target counting**: Iterates `TargetElement` buffer on `m_Event`, counts `InvolvedInAccident` (severity) and `Criminal` (not arrested). If criminal has `Monitored`: sets `CrimeMonitored` on site.
  3. **Crime detection** (CrimeScene + not CrimeDetected):
     - Reads `CrimeData.m_AlarmDelay` from event prefab
     - Applies `CityModifier.CrimeResponseTime`
     - If `CrimeMonitored` OR elapsed time >= `alarmDelay.max`: sets `CrimeDetected`
     - If elapsed time >= `alarmDelay.min`: probabilistic detection roll
     - On detection: adds crime scene notification icon
  4. **Crime duration** (CrimeScene + CrimeDetected + not CrimeFinished):
     - Reads `CrimeData.m_CrimeDuration`
     - If elapsed time >= `crimeDuration.max`: sets `CrimeFinished`
     - If elapsed time >= `crimeDuration.min`: probabilistic finish roll
  5. **Police request** (clears `RequirePolice` first, then re-evaluates):
     - If severity > 0 OR (unsecured CrimeScene): may set `RequirePolice`
     - If `CrimeDetected`: calls `RequestPoliceIfNeeded()` to create `PoliceEmergencyRequest`
     - Purpose = CrimeMonitored ? Intelligence : Emergency
  6. **Cleanup**: If no active targets and not staging, removes `AccidentSite`. If secured CrimeScene with 1024+ frames since secured, removes.

### `CrimeAccumulationSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16384 frames
- **Queries**:
  - Building query: `CrimeProducer` + `Building` + `PrefabRef` + `UpdateFrame`, excluding `Deleted`, `Temp`
  - PoliceConfig query: `PoliceConfigurationData`
- **Key logic**:
  - Accumulates crime on buildings with `CrimeProducer` component
  - Crime increase = `crimeRate * (10 - policeCoverage) / 10 * m_CrimePoliceCoverageFactor`
  - Modified by district and city `CrimeAccumulation` modifiers
  - When `m_Crime > m_CrimeAccumulationTolerance` (1000): creates `PolicePatrolRequest`

### `AddAccidentSiteSystem` (Game.Events)

- **Base class**: GameSystemBase
- **Key logic**: Processes `AddAccidentSite` event entities, adds `AccidentSite` component to target buildings, resets `CrimeProducer.m_Crime` to 0

## Data Flow

### Full Crime Pipeline (5 Systems)

```
[CrimeCheckSystem] ---- once per game day, 16 partitions ----
    |
    |  For each unemployed, non-student, non-sick citizen (Teen/Adult):
    |    If not already in active crime:
    |      Roll probability based on wellbeing + population scaling
    |      Apply CityModifier.CrimeProbability
    |      Check welfare coverage for repeat offenders
    |
    +---> Create Crime Event Entity:
    |       - Crime tag component (from EventData.m_Archetype)
    |       - TargetElement buffer [citizen entity]
    |       - PrefabRef -> crime event prefab
    |       - Created tag (auto-added by ECS)
    |
    v
[InitializeSystem] ---- processes Created + Event entities ----
    |
    |  Detects Crime tag on new event entity
    |  Reads CrimeData from prefab -> CrimeType.Robbery
    |  For each citizen in TargetElement buffer:
    |
    +---> Create AddCriminal Command Entity:
    |       - Game.Common.Event tag
    |       - AddCriminal { m_Event, m_Target, m_Flags = Robber | Planning }
    |
    v
[AddCriminalSystem] ---- processes Event + AddCriminal entities ----
    |
    |  Adds Criminal component to citizen entity
    |  Links citizen to crime event's TargetElement buffer
    |  (If citizen already criminal: merges flags, Prisoner takes priority)
    |
    v
[CriminalSystem] ---- every 16 frames ----
    |
    +-- Planning:
    |     Add TripNeeded(Purpose.Crime)
    |     Roll CriminalMonitorProbability -> may set Monitored flag
    |     Transition: Planning -> Preparing
    |
    +-- Preparing:
    |     Citizen traveling to crime destination
    |     When TripNeeded consumed (arrived): clear Preparing
    |
    +-- Robber (at destination):
    |     Check CurrentBuilding for AccidentSite
    |     |
    |     +-- No AccidentSite:
    |     |     Create AddAccidentSite(CrimeScene) on building
    |     |     -> [AddAccidentSiteSystem] adds AccidentSite component
    |     |     -> [AccidentSiteSystem] manages state transitions
    |     |
    |     +-- AccidentSite exists:
    |         |
    |         +-- Secured + police car AtTarget:
    |         |     GoToJail (compute jail time from CrimeData)
    |         |     -> Arrested -> Jail countdown -> Roll prison probability
    |         |       -> Sentenced -> Wait transport -> GoToPrison -> Prison countdown -> Released
    |         |       -> Not sentenced -> Released
    |         |
    |         +-- CrimeScene, not Secured, not CrimeFinished, same event:
    |         |     Wait (crime in progress, handled by AccidentSiteSystem)
    |         |
    |         +-- Otherwise (CrimeFinished or different event):
    |               Commit crime:
    |                 GetCrimeSource: random Renter in building
    |                 GetStealAmount: relative% * money + absolute
    |                 AddCrimeEffects: CrimeVictim on household + employees
    |               TryEscape: add TripNeeded(Purpose.Escape), clear event
    |
    v
[AccidentSiteSystem] ---- every 64 frames ----
    |
    |  Manages crime scene state transitions:
    |    CrimeScene -> CrimeDetected (after alarm delay)
    |    CrimeDetected -> CrimeFinished (after crime duration)
    |    CrimeDetected + RequirePolice -> PoliceEmergencyRequest
    |    Secured + 1024 frames -> Remove AccidentSite
```

### Crime Accumulation (Background)

```
[CrimeAccumulationSystem] (periodic)
    |
    |  For each building with CrimeProducer:
    |  Accumulate crime based on zone rate + police coverage
    |
    v
[CrimeProducer.m_Crime > tolerance]
    |
    v
[PolicePatrolRequest] created -> Police dispatched to area
```

## Prefab & Configuration

| Value | Source | Default | Location |
|-------|--------|---------|----------|
| Crime occurrence probability | Crime (ComponentBase) | 0-50 | Game.Prefabs.Crime |
| Crime recurrence probability | Crime (ComponentBase) | 0-100 | Game.Prefabs.Crime |
| Alarm delay | Crime (ComponentBase) | 5-10 sec | Game.Prefabs.Crime |
| Crime duration | Crime (ComponentBase) | 20-60 sec | Game.Prefabs.Crime |
| Steal amount (absolute) | Crime (ComponentBase) | 100-1000 | Game.Prefabs.Crime |
| Steal amount (relative) | Crime (ComponentBase) | 0-25% | Game.Prefabs.Crime |
| Jail time range | Crime (ComponentBase) | 0.125-1 days | Game.Prefabs.Crime |
| Prison time range | Crime (ComponentBase) | 1-100 days | Game.Prefabs.Crime |
| Prison probability | Crime (ComponentBase) | 50% | Game.Prefabs.Crime |
| Zone crime rate | CrimeAccumulation (ComponentBase) | 7 | Game.Prefabs.CrimeAccumulation |
| Max crime accumulation | PoliceConfigurationPrefab | 100000 | Game.Prefabs.PoliceConfigurationPrefab |
| Crime accumulation tolerance | PoliceConfigurationPrefab | 1000 | Game.Prefabs.PoliceConfigurationPrefab |
| Home crime effect | PoliceConfigurationPrefab | 15 | Game.Prefabs.PoliceConfigurationPrefab |
| Workplace crime effect | PoliceConfigurationPrefab | 5 | Game.Prefabs.PoliceConfigurationPrefab |
| Crime population reduction | PoliceConfigurationPrefab | 2000 | Game.Prefabs.PoliceConfigurationPrefab |
| Welfare recurrence factor | PoliceConfigurationPrefab | 0.4 | Game.Prefabs.PoliceConfigurationPrefab |
| Police coverage factor | PoliceConfigurationPrefab | 2.0 | Game.Prefabs.PoliceConfigurationPrefab |

## Harmony Patch Points

### Candidate 1: `CrimeCheckSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Override crime probability calculations, force specific citizens to become criminals, or prevent crime entirely
- **Risk level**: Medium
- **Side effects**: Changing crime frequency affects police dispatch, statistics, and citizen wellbeing

### Candidate 2: `InitializeSystem.InitializeCrimeEvent`

- **Signature**: `private void InitializeCrimeEvent(Entity eventEntity)`
- **Patch type**: Prefix / Postfix
- **What it enables**: Intercept crime event initialization, modify which citizens become criminals, prevent `AddCriminal` command creation, or inject additional criminals into existing crime events
- **Risk level**: Low -- runs on main thread, not BurstCompiled
- **Side effects**: Blocking this prevents criminals from being created even when crime events exist

### Candidate 3: `CriminalSystem.CriminalJob.Execute`

- **Signature**: `public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Prefix (difficult due to BurstCompile)
- **What it enables**: Modify criminal behavior (skip arrest, change steal amounts, alter state transitions)
- **Risk level**: High -- BurstCompiled job, Harmony patches may not work directly
- **Side effects**: Breaking the state machine can leave criminals in invalid states

### Candidate 4: `AccidentSiteSystem.AccidentSiteJob.Execute`

- **Signature**: `public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)`
- **Patch type**: Prefix (difficult due to BurstCompile)
- **What it enables**: Modify crime scene detection timing, police request creation, or crime duration
- **Risk level**: High -- BurstCompiled
- **Alternative**: Modify `CrimeData` on the prefab entity to change alarm delay and crime duration

### Candidate 5: `CrimeCheckSystem.OnUpdate` (Postfix to toggle debugFullCrimeMode)

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix
- **What it enables**: Set `debugFullCrimeMode = true` to bypass all probability checks, making every eligible citizen commit a crime on their next check
- **Risk level**: Low -- just toggling a public field
- **Side effects**: Massive crime wave, many police dispatches

## Mod Blueprint

### Approach A: Trigger Crime at a Specific Building with a Specific Criminal (Recommended)

The cleanest approach for triggering a crime at a specific location. Creates the crime event, adds the criminal, and creates the crime scene -- all using the game's own event command patterns. The key insight: the criminal's target building is determined by `TripNeeded(Purpose.Crime)`, but if you skip the Planning/Preparing states and directly create the `AccidentSite` on the building, the criminal will interact with that crime scene when `CriminalSystem` checks their `CurrentBuilding`.

For full control over location, use a two-step approach:
1. Create the crime event + make the citizen a criminal via `AddCriminal`
2. Separately create the `AccidentSite` on the target building via `AddAccidentSite`

This lets `CriminalSystem` handle the robbery logic (steal money, apply effects) without relying on the citizen's pathfinding to reach the building.

### Approach B: Make a Citizen Commit a Crime (Event Pipeline)

Let the game's systems handle the full lifecycle. Create a crime event entity with the citizen in the `TargetElement` buffer. `InitializeSystem` creates the `AddCriminal` command, `AddCriminalSystem` adds the `Criminal` component, and `CriminalSystem` drives the citizen through `Planning` -> `Preparing` -> crime at whatever building they naturally travel to.

### Approach C: Directly Add Criminal Component

For immediate effect without going through the event pipeline. Bypass `InitializeSystem` and `AddCriminalSystem` by directly adding the `Criminal` component. Warning: without an associated crime event entity (`m_Event`), the criminal will commit the crime but with no money stolen and no jail/prison sentence (CriminalSystem checks `m_Event` -> `PrefabRef` -> `CrimeData` for amounts).

## Examples

### Example 1: Trigger a Crime Event at a Specific Building

This is the most complete approach for triggering a crime at a specific location. It creates the crime event, makes the citizen a criminal, AND creates a crime scene at the target building. The citizen must already be in or near the building for the crime to proceed immediately.

```csharp
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Custom system that triggers a complete crime at a specific building with a specific criminal.
/// Creates all three elements: crime event, criminal citizen, and crime scene at building.
/// </summary>
public partial class CrimeTriggerSystem : GameSystemBase
{
    private SimulationSystem m_SimulationSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
    }

    protected override void OnUpdate() { }

    /// <summary>
    /// Triggers a complete crime at a building with a specific citizen as the criminal.
    /// The citizen should ideally already be at or near the building for the crime
    /// to proceed immediately. Otherwise, the citizen will travel to the building first.
    /// </summary>
    /// <param name="citizenEntity">The citizen to make a criminal</param>
    /// <param name="buildingEntity">The building where the crime occurs</param>
    /// <param name="createCrimeScene">If true, immediately marks the building as a crime scene
    /// (triggering police dispatch). If false, the criminal will create the crime scene
    /// when they arrive at the building via CriminalSystem.</param>
    public void TriggerCrimeAtBuilding(
        Entity citizenEntity,
        Entity buildingEntity,
        bool createCrimeScene = true)
    {
        // Step 1: Find the crime event prefab (has CrimeData + EventData).
        var crimeQuery = EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<CrimeData>(),
            ComponentType.ReadOnly<EventData>(),
            ComponentType.Exclude<Locked>()
        );
        using var prefabEntities = crimeQuery.ToEntityArray(Allocator.Temp);
        if (prefabEntities.Length == 0)
        {
            Log.Warn("No crime event prefab found");
            return;
        }

        Entity eventPrefab = prefabEntities[0];
        EventData eventData = EntityManager.GetComponentData<EventData>(eventPrefab);

        // Step 2: Create the crime event entity from the prefab's archetype.
        // This gives it the Crime tag + TargetElement buffer.
        Entity crimeEvent = EntityManager.CreateEntity(eventData.m_Archetype);
        EntityManager.SetComponentData(crimeEvent, new PrefabRef(eventPrefab));
        EntityManager.GetBuffer<TargetElement>(crimeEvent)
            .Add(new TargetElement(citizenEntity));

        // Step 3: Create AddCriminal command entity.
        // This mirrors what InitializeSystem.InitializeCrimeEvent does.
        Entity addCriminalCmd = EntityManager.CreateEntity(
            ComponentType.ReadWrite<Game.Common.Event>(),
            ComponentType.ReadWrite<AddCriminal>()
        );
        EntityManager.SetComponentData(addCriminalCmd, new AddCriminal
        {
            m_Event = crimeEvent,
            m_Target = citizenEntity,
            m_Flags = CriminalFlags.Robber | CriminalFlags.Planning
        });

        // Step 4: Optionally create crime scene at the building immediately.
        // This bypasses the normal flow where CriminalSystem creates it when
        // the criminal arrives. Use this when you want immediate police dispatch.
        if (createCrimeScene)
        {
            Entity addSiteCmd = EntityManager.CreateEntity(
                ComponentType.ReadWrite<Game.Common.Event>(),
                ComponentType.ReadWrite<AddAccidentSite>()
            );
            EntityManager.SetComponentData(addSiteCmd, new AddAccidentSite
            {
                m_Event = crimeEvent,
                m_Target = buildingEntity,
                m_Flags = AccidentSiteFlags.CrimeScene
            });
            // AccidentSiteSystem will then:
            //   - Set CrimeDetected after alarm delay (5-10 sec)
            //   - Set RequirePolice + create PoliceEmergencyRequest
            //   - Set CrimeFinished after crime duration (20-60 sec)
        }

        // After this frame:
        //   1. AddCriminalSystem adds Criminal component to citizen
        //   2. AddAccidentSiteSystem adds AccidentSite to building (if createCrimeScene)
        //   3. CriminalSystem drives the citizen through Planning -> Preparing -> ...
        //   4. AccidentSiteSystem manages detection -> police dispatch -> duration
    }
}
```

### Example 2: Convert a Citizen into a Criminal

Make any citizen a criminal by creating a properly linked crime event and `AddCriminal` command. This lets the game handle the full lifecycle, including target building selection.

```csharp
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Converts a citizen into a criminal using the game's AddCriminal event pattern.
/// The citizen will enter Planning state, pick a building, travel there, and attempt robbery.
/// </summary>
public void MakeCitizenCriminal(EntityManager entityManager, Entity citizenEntity)
{
    // Find the crime event prefab.
    var crimeQuery = entityManager.CreateEntityQuery(
        ComponentType.ReadOnly<CrimeData>(),
        ComponentType.ReadOnly<EventData>(),
        ComponentType.Exclude<Locked>()
    );
    using var prefabEntities = crimeQuery.ToEntityArray(Allocator.Temp);
    if (prefabEntities.Length == 0) return;

    Entity eventPrefab = prefabEntities[0];
    EventData eventData = entityManager.GetComponentData<EventData>(eventPrefab);

    // Create crime event entity.
    Entity crimeEvent = entityManager.CreateEntity(eventData.m_Archetype);
    entityManager.SetComponentData(crimeEvent, new PrefabRef(eventPrefab));
    entityManager.GetBuffer<TargetElement>(crimeEvent)
        .Add(new TargetElement(citizenEntity));

    // Create AddCriminal command.
    Entity cmd = entityManager.CreateEntity(
        ComponentType.ReadWrite<Game.Common.Event>(),
        ComponentType.ReadWrite<AddCriminal>()
    );
    entityManager.SetComponentData(cmd, new AddCriminal
    {
        m_Event = crimeEvent,
        m_Target = citizenEntity,
        m_Flags = CriminalFlags.Robber | CriminalFlags.Planning
    });

    // Next frame: AddCriminalSystem adds Criminal component.
    // CriminalSystem then drives: Planning -> Preparing (travel) -> At Target -> Crime/Arrest
}
```

### Example 3: Spawn a Crime with a Specific Criminal at a Specific Location

A combined approach that directly adds the `Criminal` component (bypassing `AddCriminalSystem`) and creates the crime scene at a building. Use this when you need immediate effect in the same frame.

```csharp
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Immediately spawns a crime: directly adds Criminal component to a citizen
/// and creates a crime scene at a building. Bypasses the event command pipeline
/// for faster execution, but still creates a proper crime event entity so that
/// CriminalSystem can read CrimeData for jail times and steal amounts.
/// </summary>
public void SpawnCrimeImmediate(
    EntityManager entityManager,
    Entity citizenEntity,
    Entity buildingEntity,
    uint currentFrame)
{
    // Find the crime event prefab.
    var crimeQuery = entityManager.CreateEntityQuery(
        ComponentType.ReadOnly<CrimeData>(),
        ComponentType.ReadOnly<EventData>(),
        ComponentType.Exclude<Locked>()
    );
    using var prefabEntities = crimeQuery.ToEntityArray(Allocator.Temp);
    if (prefabEntities.Length == 0) return;

    Entity eventPrefab = prefabEntities[0];
    EventData eventData = entityManager.GetComponentData<EventData>(eventPrefab);

    // Create crime event entity (needed for CrimeData reference).
    Entity crimeEvent = entityManager.CreateEntity(eventData.m_Archetype);
    entityManager.SetComponentData(crimeEvent, new PrefabRef(eventPrefab));
    entityManager.GetBuffer<TargetElement>(crimeEvent)
        .Add(new TargetElement(citizenEntity));

    // Directly add Criminal component (bypasses AddCriminalSystem).
    if (!entityManager.HasComponent<Criminal>(citizenEntity))
    {
        entityManager.AddComponentData(citizenEntity, new Criminal
        {
            m_Event = crimeEvent,
            m_JailTime = 0,
            m_Flags = CriminalFlags.Robber  // Skip Planning/Preparing
        });
    }
    else
    {
        entityManager.SetComponentData(citizenEntity, new Criminal
        {
            m_Event = crimeEvent,
            m_JailTime = 0,
            m_Flags = CriminalFlags.Robber
        });
    }

    // Directly add AccidentSite to building (bypasses AddAccidentSiteSystem).
    if (!entityManager.HasComponent<AccidentSite>(buildingEntity))
    {
        entityManager.AddComponentData(buildingEntity, new AccidentSite
        {
            m_Event = crimeEvent,
            m_PoliceRequest = Entity.Null,
            m_Flags = AccidentSiteFlags.CrimeScene,
            m_CreationFrame = currentFrame,
            m_SecuredFrame = 0
        });
    }

    // Now CriminalSystem will process this citizen on the next 16-frame tick:
    //   - Robber flag set, at building with AccidentSite matching their event
    //   - If AccidentSite is CrimeScene + not Secured + not CrimeFinished: waits
    //   - AccidentSiteSystem handles detection -> police dispatch -> duration
    //   - When crime finishes or police secure: arrest or escape
}
```

### Example 4: Listen for Crime Events (Observer System)

An ECS system that monitors for new crime events and criminal state changes. Use this to react to crimes in your mod.

```csharp
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Monitors crime events and criminal lifecycle changes.
/// Runs every 16 frames, same cadence as CriminalSystem.
/// </summary>
public partial class CrimeObserverSystem : GameSystemBase
{
    private EntityQuery m_NewCrimeQuery;
    private EntityQuery m_CriminalQuery;
    private EntityQuery m_CrimeSceneQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        // New crime events (just created this frame)
        m_NewCrimeQuery = GetEntityQuery(
            ComponentType.ReadOnly<Game.Events.Crime>(),
            ComponentType.ReadOnly<Created>(),
            ComponentType.Exclude<Deleted>()
        );

        // All active criminals
        m_CriminalQuery = GetEntityQuery(
            ComponentType.ReadOnly<Criminal>(),
            ComponentType.ReadOnly<Citizen>(),
            ComponentType.Exclude<Deleted>()
        );

        // Active crime scenes
        m_CrimeSceneQuery = GetEntityQuery(
            ComponentType.ReadOnly<AccidentSite>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

    protected override void OnUpdate()
    {
        // Check for new crime events
        if (!m_NewCrimeQuery.IsEmptyIgnoreFilter)
        {
            var newCrimes = m_NewCrimeQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < newCrimes.Length; i++)
            {
                Entity crimeEvent = newCrimes[i];
                if (EntityManager.TryGetBuffer<TargetElement>(crimeEvent, true, out var targets))
                {
                    for (int j = 0; j < targets.Length; j++)
                    {
                        // A new crime event was created targeting this citizen
                        Entity citizen = targets[j].m_Entity;
                        OnCrimeEventCreated(crimeEvent, citizen);
                    }
                }
            }
            newCrimes.Dispose();
        }

        // Monitor criminal state changes
        var criminals = m_CriminalQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < criminals.Length; i++)
        {
            Criminal criminal = EntityManager.GetComponentData<Criminal>(criminals[i]);
            CriminalFlags flags = criminal.m_Flags;

            if ((flags & CriminalFlags.Arrested) != 0 && (flags & CriminalFlags.Sentenced) == 0)
            {
                OnCriminalArrested(criminals[i], criminal);
            }
        }
        criminals.Dispose();
    }

    private void OnCrimeEventCreated(Entity crimeEvent, Entity citizen)
    {
        // React to new crime -- e.g., log, notify UI, trigger mod behavior
        Log.Info($"Crime event {crimeEvent} created targeting citizen {citizen}");
    }

    private void OnCriminalArrested(Entity citizen, Criminal criminal)
    {
        // React to arrest -- e.g., track statistics, modify sentence
        Log.Info($"Citizen {citizen} arrested, jail time: {criminal.m_JailTime}");
    }
}
```

### Example 5: Modify Crime Probability via Prefab Data

Change crime frequency by modifying the `CrimeData` on the crime event prefab at runtime. This is the safest approach since it works through the game's normal systems.

```csharp
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Modifies crime probability by changing the CrimeData on the crime event prefab.
/// Call once during mod initialization or when settings change.
/// </summary>
public partial class CrimeProbabilityModifierSystem : GameSystemBase
{
    protected override void OnCreate() { base.OnCreate(); }
    protected override void OnUpdate() { }

    /// <summary>
    /// Multiplies crime occurrence and recurrence probability ranges by a factor.
    /// factor > 1 = more crime, factor < 1 = less crime, factor = 0 = no crime.
    /// </summary>
    public void SetCrimeProbabilityMultiplier(float factor)
    {
        var query = EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<CrimeData>(),
            ComponentType.ReadOnly<EventData>(),
            ComponentType.Exclude<Locked>()
        );

        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            CrimeData data = EntityManager.GetComponentData<CrimeData>(entities[i]);

            // Scale probability ranges
            data.m_OccurenceProbability.min *= factor;
            data.m_OccurenceProbability.max *= factor;
            data.m_RecurrenceProbability.min *= factor;
            data.m_RecurrenceProbability.max *= factor;

            EntityManager.SetComponentData(entities[i], data);
        }
    }

    /// <summary>
    /// Changes the alarm delay (time before police are notified).
    /// Shorter delay = faster police response. Longer = criminals escape more often.
    /// </summary>
    public void SetAlarmDelay(float minSeconds, float maxSeconds)
    {
        var query = EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<CrimeData>(),
            ComponentType.ReadOnly<EventData>(),
            ComponentType.Exclude<Locked>()
        );

        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            CrimeData data = EntityManager.GetComponentData<CrimeData>(entities[i]);
            data.m_AlarmDelay.min = minSeconds;
            data.m_AlarmDelay.max = maxSeconds;
            EntityManager.SetComponentData(entities[i], data);
        }
    }

    /// <summary>
    /// Changes the crime duration (how long before the criminal finishes and escapes).
    /// Shorter = criminals escape quickly. Longer = police have more time to arrive.
    /// </summary>
    public void SetCrimeDuration(float minSeconds, float maxSeconds)
    {
        var query = EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<CrimeData>(),
            ComponentType.ReadOnly<EventData>(),
            ComponentType.Exclude<Locked>()
        );

        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            CrimeData data = EntityManager.GetComponentData<CrimeData>(entities[i]);
            data.m_CrimeDuration.min = minSeconds;
            data.m_CrimeDuration.max = maxSeconds;
            EntityManager.SetComponentData(entities[i], data);
        }
    }
}
```

## Open Questions

- [x] How does the game decide which citizens become criminals? -> Based on `m_WellBeing`, unemployment, and population-scaled probability
- [x] What is the only crime type? -> `Robbery` (only enum value in `CrimeType`)
- [x] How does the criminal lifecycle work? -> State machine: Planning -> Preparing -> At Target -> Crime/Arrest -> Jail -> possibly Prison -> Released
- [x] How does crime accumulation on buildings work? -> `CrimeAccumulationSystem` accumulates based on zone rate and police coverage
- [x] How exactly does the crime event entity trigger adding the `Criminal` component to the citizen? -> `CrimeCheckSystem` creates crime event -> `InitializeSystem.InitializeCrimeEvent` creates `AddCriminal` command -> `AddCriminalSystem` adds `Criminal` component
- [x] What system processes `AccidentSite` state transitions? -> `AccidentSiteSystem` (every 64 frames): manages CrimeDetected, CrimeFinished, RequirePolice, police request creation, and cleanup
- [ ] How does `Purpose.Crime` trip pathfinding select the target building? The `TripNeeded` with `Purpose.Crime` is consumed by the citizen movement system, but the exact building selection logic (closest building with CrimeProducer? random?) is in the trip/pathfinding system and not traced here.
- [ ] Can a mod add new `CrimeType` enum values and have them processed by `CriminalSystem`? Currently only `Robbery` exists and the steal logic has `if (crimeType == CrimeType.Robbery)` guard.

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Types decompiled: `Criminal`, `CriminalFlags`, `CrimeData`, `CrimeType`, `Crime` (tag), `Crime` (ComponentBase), `CrimeProducer`, `CrimeVictim`, `AccidentSite`, `AccidentSiteFlags`, `AddAccidentSite`, `AddCriminal`, `PoliceConfigurationData`, `CrimeAccumulationData`, `EventData`, `TargetElement`, `CrimeCheckSystem` (full), `CriminalSystem` (full), `CrimeAccumulationSystem`, `AddCriminalSystem` (full), `InitializeSystem.InitializeCrimeEvent`, `AccidentSiteSystem` (full)
