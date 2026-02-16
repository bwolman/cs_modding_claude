# Research: Crime Trigger

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How to programmatically make a crime occur in CS2 — how the game decides which citizens become criminals, how crime events are created, and the full criminal lifecycle from planning through arrest/escape.

**Why**: To build mods that trigger crime events on specific citizens or locations, modify crime probabilities, or create custom crime scenarios.

**Boundaries**: Not covering police vehicle AI/pathfinding, prison management, or crime statistics UI. Focused on the programmatic crime trigger and criminal lifecycle mechanisms.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Citizens | `Criminal`, `CriminalFlags`, `CrimeVictim` |
| Game.dll | Game.Events | `Crime` (tag), `AccidentSite`, `AccidentSiteFlags`, `AddAccidentSite`, `TargetElement` |
| Game.dll | Game.Buildings | `CrimeProducer` |
| Game.dll | Game.Simulation | `CrimeCheckSystem`, `CriminalSystem`, `CrimeAccumulationSystem` |
| Game.dll | Game.Prefabs | `CrimeData`, `CrimeType`, `CrimeAccumulationData`, `PoliceConfigurationData`, `Crime` (ComponentBase) |

## Component Map

### `Criminal` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime event entity this criminal is participating in |
| m_JailTime | ushort | Remaining jail/prison time in update ticks |
| m_Flags | CriminalFlags | State flags for the criminal lifecycle |

Placed on citizen entities when they become criminals. Persisted via `ISerializable`. Removed when `m_Flags` becomes 0 (released/escaped).

*Source: `Game.dll` → `Game.Citizens.Criminal`*

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

These flags represent a state machine. A criminal progresses through: `Planning` → `Preparing` → `Robber` (at target) → either `Arrested` → possibly `Sentenced` → `Prisoner`, or escapes.

*Source: `Game.dll` → `Game.Citizens.CriminalFlags`*

### `Crime` (Game.Events) — Tag Component

Empty tag component placed on crime event entities (like `TrafficAccident` for traffic events). Crime event entities also have a `TargetElement` buffer containing the criminal citizen.

*Source: `Game.dll` → `Game.Events.Crime`*

### `CrimeProducer` (Game.Buildings)

| Field | Type | Description |
|-------|------|-------------|
| m_PatrolRequest | Entity | Active police patrol request for this building |
| m_Crime | float | Accumulated crime level on this building |
| m_DispatchIndex | byte | Dispatch routing index |

Placed on buildings that can accumulate crime. Crime accumulates based on zone type crime rate, police coverage, and district/city modifiers. When `m_Crime` exceeds `m_CrimeAccumulationTolerance`, a police patrol is requested.

*Source: `Game.dll` → `Game.Buildings.CrimeProducer`*

### `CrimeVictim` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Effect | byte | Wellbeing impact from being a crime victim (0–255) |

Implements `IEnableableComponent` — enabled when the citizen has been affected by crime. Effect is cumulative (multiple crimes stack). Applied to household members (m_HomeCrimeEffect = 15) and employees (m_WorkplaceCrimeEffect = 5) of the robbed entity.

*Source: `Game.dll` → `Game.Citizens.CrimeVictim`*

### `AccidentSite` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime/accident event entity |
| m_PoliceRequest | Entity | Active police emergency request |
| m_Flags | AccidentSiteFlags | State flags for the site |
| m_CreationFrame | uint | Frame when the site was created |
| m_SecuredFrame | uint | Frame when police secured the site |

Placed on buildings where a crime or traffic accident is occurring. Used by `CriminalSystem` to determine whether the criminal should be arrested (site Secured) or can commit the crime (CrimeScene but not Secured).

*Source: `Game.dll` → `Game.Events.AccidentSite`*

### `AccidentSiteFlags` (Game.Events)

```csharp
[Flags]
public enum AccidentSiteFlags : uint
{
    StageAccident  = 1,     // Staging area for accident
    Secured        = 2,     // Police have secured the scene
    CrimeScene     = 4,     // This is a crime scene (vs traffic accident)
    TrafficAccident = 8,    // This is a traffic accident
    CrimeFinished  = 0x10,  // Crime has been completed
    CrimeDetected  = 0x20,  // Crime has been detected
    CrimeMonitored = 0x40,  // Crime is being monitored
    RequirePolice  = 0x80,  // Police dispatch requested
    MovingVehicles = 0x100  // Vehicles involved are moving
}
```

*Source: `Game.dll` → `Game.Events.AccidentSiteFlags`*

### `AddAccidentSite` (Game.Events) — Event Command

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The crime event entity |
| m_Target | Entity | The building to mark as crime scene |
| m_Flags | AccidentSiteFlags | Flags to set (CrimeScene for crimes) |

Temporary event entity (with `Game.Common.Event` tag) that commands the system to add an `AccidentSite` component to a building. Processed by `AddAccidentSiteSystem`.

*Source: `Game.dll` → `Game.Events.AddAccidentSite`*

### `CrimeData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_RandomTargetType | EventTargetType | Target type (Citizen) |
| m_CrimeType | CrimeType | Type of crime (only Robbery exists) |
| m_OccurenceProbability | Bounds1 | First-time crime probability range (default 0–50) |
| m_RecurrenceProbability | Bounds1 | Repeat crime probability range (default 0–100) |
| m_AlarmDelay | Bounds1 | Seconds before police are notified (default 5–10) |
| m_CrimeDuration | Bounds1 | How long crime lasts before criminal leaves (default 20–60) |
| m_CrimeIncomeAbsolute | Bounds1 | Absolute money stolen (default 100–1000) |
| m_CrimeIncomeRelative | Bounds1 | Percentage of victim's money stolen (default 0–0.25) |
| m_JailTimeRange | Bounds1 | Jail time in game days (default 0.125–1) |
| m_PrisonTimeRange | Bounds1 | Prison time in game days (default 1–100) |
| m_PrisonProbability | float | Chance of jail → prison sentence (default 50%) |

Set on crime event prefab entities. The `Crime` ComponentBase class copies these values from the prefab authoring component.

*Source: `Game.dll` → `Game.Prefabs.CrimeData`*

### `CrimeType` (Game.Prefabs)

```csharp
public enum CrimeType
{
    Robbery  // Only crime type in the game
}
```

*Source: `Game.dll` → `Game.Prefabs.CrimeType`*

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

*Source: `Game.dll` → `Game.Prefabs.PoliceConfigurationData`*

### `CrimeAccumulationData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_CrimeRate | float | Zone crime rate (default 7) |

Per-zone-type configuration set via `CrimeAccumulation` ComponentBase on zone/service prefabs.

*Source: `Game.dll` → `Game.Prefabs.CrimeAccumulationData`*

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
    - WellBeing ≤ 25: `t = wellbeing / 25` (linear, low wellbeing = high crime chance)
    - WellBeing > 25: `t = ((100 - wellbeing) / 75)²` (quadratic falloff)
  - Probability scaled by city population: `randomRange = max(pop / 2000 * 100, 100)`
  - Repeat criminals checked against welfare coverage on their home property
  - `debugFullCrimeMode` flag bypasses all probability checks
- **Output**: Creates crime event entities with `Crime` tag + `TargetElement` buffer + `PrefabRef`

### `CriminalSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16 frames (very frequent)
- **Queries**:
  - Criminal query: `Citizen` + `UpdateFrame` + `Criminal`, excluding `Deleted`, `Temp`
  - PoliceConfig query: `PoliceConfigurationData`
- **Key logic** (criminal lifecycle state machine):
  1. **Planning**: Adds `TripNeeded(Purpose.Crime)`, rolls monitor probability, transitions to Preparing
  2. **Preparing**: Waits for citizen to arrive at crime destination
  3. **At target**: Adds `AccidentSite(CrimeScene)` to building via `AddAccidentSite` event
  4. **Crime scene active**: If police secured → GoToJail (arrested). If not → commit crime (steal money, apply effects), then TryEscape
  5. **Arrested**: Held in police station, jail time counts down. When done → roll prison probability
  6. **Sentenced**: Waits for prisoner transport vehicle → GoToPrison
  7. **Prisoner**: In prison, sentence counts down. When done → released (Criminal removed)
- **Output**: Modifies `Criminal` component flags, creates `AddAccidentSite` events, queues money transfers and `CrimeVictim` effects

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

### Natural Crime (CrimeCheckSystem → CriminalSystem)

```
[CrimeCheckSystem] (once per game day)
    │
    │  For each unemployed, non-student citizen with low wellbeing:
    │  Roll crime probability based on wellbeing + population
    │
    ▼
[Crime Event Entity] created with:
    - Crime tag component
    - TargetElement buffer (criminal citizen)
    - PrefabRef (crime data prefab)
    │
    ▼
[CriminalSystem picks up Criminal component] (every 16 frames)
    │
    ├─ Planning: Add TripNeeded(Purpose.Crime)
    │
    ├─ Preparing: Citizen travels to target building
    │
    ├─ At Target: Create AddAccidentSite(CrimeScene) on building
    │
    ├─ Crime Scene Active:
    │   ├─ Police arrived + secured → GoToJail (Arrested)
    │   └─ Not secured → Commit crime → TryEscape
    │       ├─ Steal money from random Renter
    │       ├─ Apply CrimeVictim effects to household/employees
    │       └─ Add TripNeeded(Purpose.Escape)
    │
    ├─ Arrested → Jail time countdown → Roll prison probability
    │   ├─ Sentenced → Wait for transport → GoToPrison
    │   └─ Not sentenced → Released
    │
    └─ Prisoner → Prison time countdown → Released (Criminal removed)
```

### Crime Accumulation (Background)

```
[CrimeAccumulationSystem] (periodic)
    │
    │  For each building with CrimeProducer:
    │  Accumulate crime based on zone rate + police coverage
    │
    ▼
[CrimeProducer.m_Crime > tolerance]
    │
    ▼
[PolicePatrolRequest] created → Police dispatched to area
```

## Prefab & Configuration

| Value | Source | Default | Location |
|-------|--------|---------|----------|
| Crime occurrence probability | Crime (ComponentBase) | 0–50 | Game.Prefabs.Crime |
| Crime recurrence probability | Crime (ComponentBase) | 0–100 | Game.Prefabs.Crime |
| Alarm delay | Crime (ComponentBase) | 5–10 sec | Game.Prefabs.Crime |
| Crime duration | Crime (ComponentBase) | 20–60 sec | Game.Prefabs.Crime |
| Steal amount (absolute) | Crime (ComponentBase) | 100–1000 | Game.Prefabs.Crime |
| Steal amount (relative) | Crime (ComponentBase) | 0–25% | Game.Prefabs.Crime |
| Jail time range | Crime (ComponentBase) | 0.125–1 days | Game.Prefabs.Crime |
| Prison time range | Crime (ComponentBase) | 1–100 days | Game.Prefabs.Crime |
| Prison probability | Crime (ComponentBase) | 50% | Game.Prefabs.Crime |
| Zone crime rate | CrimeAccumulation (ComponentBase) | 7 | Game.Prefabs.CrimeAccumulation |
| Max crime accumulation | PoliceConfigurationPrefab | 100000 | Game.Prefabs.PoliceConfigurationPrefab |
| Crime accumulation tolerance | PoliceConfigurationPrefab | 1000 | Game.Prefabs.PoliceConfigurationPrefab |
| Home crime effect | PoliceConfigurationPrefab | 15 | Game.Prefabs.PoliceConfigurationPrefab |
| Workplace crime effect | PoliceConfigurationPrefab | 5 | Game.Prefabs.PoliceConfigurationPrefab |
| Crime population reduction | PoliceConfigurationPrefab | 2000 | Game.Prefabs.PoliceConfigurationPrefab |

## Harmony Patch Points

### Candidate 1: `CrimeCheckSystem.OnUpdate`

- **Patch type**: Prefix / Postfix
- **What it enables**: Override crime probability calculations, force specific citizens to become criminals, or prevent crime entirely
- **Risk level**: Medium
- **Side effects**: Changing crime frequency affects police dispatch, statistics, and citizen wellbeing

### Candidate 2: `CriminalSystem.CriminalJob.Execute`

- **Patch type**: Prefix (difficult due to BurstCompile)
- **What it enables**: Modify criminal behavior (skip arrest, change steal amounts, alter state transitions)
- **Risk level**: High — BurstCompiled job, Harmony patches may not work directly
- **Side effects**: Breaking the state machine can leave criminals in invalid states

### Candidate 3: `CrimeCheckSystem.CrimeCheckJob.TryAddCrime`

- **Patch type**: Prefix (difficult due to BurstCompile)
- **What it enables**: Override the wellbeing-to-crime probability curve
- **Risk level**: High — BurstCompiled
- **Alternative**: Use ECS approach (modify citizen wellbeing or create event entities directly)

## Mod Blueprint

### Approach 1: Make a Specific Citizen Commit a Crime (ECS Event Entity)

The cleanest approach is to mimic what `CrimeCheckSystem.CreateCrimeEvent` does — create a crime event entity and add the `Criminal` component to the target citizen.

```csharp
/// <summary>
/// Makes a specific citizen commit a crime.
/// The citizen will enter the Planning state and travel to a building to rob.
/// </summary>
public void MakeCitizenCommitCrime(EntityManager entityManager, Entity citizenEntity)
{
    // Step 1: Find the crime event prefab (has CrimeData component)
    var crimeQuery = entityManager.CreateEntityQuery(
        ComponentType.ReadOnly<CrimeData>(),
        ComponentType.ReadOnly<EventData>(),
        ComponentType.Exclude<Locked>()
    );

    var prefabChunks = crimeQuery.ToArchetypeChunkArray(Allocator.Temp);
    if (prefabChunks.Length == 0) return;

    var entityHandle = entityManager.GetEntityTypeHandle();
    var eventDataHandle = entityManager.GetComponentTypeHandle<EventData>(true);

    // Get first crime prefab
    var entities = prefabChunks[0].GetNativeArray(entityHandle);
    var eventDatas = prefabChunks[0].GetNativeArray(ref eventDataHandle);
    Entity eventPrefab = entities[0];
    EventData eventData = eventDatas[0];

    // Step 2: Create the crime event entity using the prefab's archetype
    // The archetype includes: Game.Events.Crime (tag) + TargetElement (buffer)
    Entity crimeEvent = entityManager.CreateEntity(eventData.m_Archetype);
    entityManager.SetComponentData(crimeEvent, new PrefabRef(eventPrefab));

    var targetBuffer = entityManager.GetBuffer<TargetElement>(crimeEvent);
    targetBuffer.Add(new TargetElement(citizenEntity));

    // Step 3: Add Criminal component to the citizen (Planning state)
    // CrimeCheckSystem normally does NOT add Criminal directly —
    // the crime event entity triggers the criminal lifecycle.
    // The Criminal component with Planning flag is added by event processing.

    prefabChunks.Dispose();
}
```

**Important**: `CrimeCheckSystem` creates the crime event entity, but it's the event processing pipeline that adds the `Criminal` component with `Planning` flag to the citizen. The crime event entity (with `Crime` tag + `TargetElement`) is the trigger.

### Approach 2: Directly Add Criminal Component

For immediate effect without going through the event pipeline:

```csharp
/// <summary>
/// Directly makes a citizen a criminal in Planning state.
/// This bypasses the normal event pipeline.
/// </summary>
public void ForceAddCriminal(EntityManager entityManager, Entity citizenEntity)
{
    if (!entityManager.HasComponent<Criminal>(citizenEntity))
    {
        entityManager.AddComponentData(citizenEntity, new Criminal
        {
            m_Event = Entity.Null,  // No event entity
            m_JailTime = 0,
            m_Flags = CriminalFlags.Robber | CriminalFlags.Planning
        });
    }
}
```

**Warning**: Without an associated crime event entity (`m_Event`), the criminal won't have proper event tracking. The `CriminalSystem` checks `m_Event` for `PrefabRef` → `CrimeData` when determining jail/prison times and steal amounts. With `Entity.Null`, the criminal will commit the crime but with no money stolen and no jail/prison sentence.

### Approach 3: Trigger Crime at a Specific Building

To create a crime scene at a building (like what happens when a criminal arrives):

```csharp
/// <summary>
/// Creates a crime scene at a building, triggering police response.
/// </summary>
public void CreateCrimeScene(EntityManager entityManager, Entity buildingEntity, Entity crimeEventEntity)
{
    var archetype = entityManager.CreateArchetype(
        ComponentType.ReadWrite<Game.Common.Event>(),
        ComponentType.ReadWrite<AddAccidentSite>()
    );
    Entity cmd = entityManager.CreateEntity(archetype);
    entityManager.SetComponentData(cmd, new AddAccidentSite
    {
        m_Event = crimeEventEntity,
        m_Target = buildingEntity,
        m_Flags = AccidentSiteFlags.CrimeScene
    });
}
```

### Key Constraints for Crime Eligibility

Citizens checked by `CrimeCheckSystem` must be:
- **Not a child or elderly** (only Teen/Adult age)
- **Not a worker** (unemployed)
- **Not a student**
- **Not sick** (no `HealthProblem` component)
- **Not already in an active crime** (no `Criminal` with non-null `m_Event`)

## Open Questions

- [x] How does the game decide which citizens become criminals? → Based on `m_WellBeing`, unemployment, and population-scaled probability
- [x] What is the only crime type? → `Robbery` (only enum value in `CrimeType`)
- [x] How does the criminal lifecycle work? → State machine: Planning → Preparing → At Target → Crime/Arrest → Jail → possibly Prison → Released
- [x] How does crime accumulation on buildings work? → `CrimeAccumulationSystem` accumulates based on zone rate and police coverage
- [ ] How exactly does the crime event entity trigger adding the `Criminal` component to the citizen? The event processing between `CrimeCheckSystem` creating the event and `CriminalSystem` reading the `Criminal` component needs further investigation
- [ ] What system processes `AccidentSite` state transitions (CrimeDetected, CrimeFinished, etc.)?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Types decompiled: `Criminal`, `CriminalFlags`, `CrimeData`, `CrimeType`, `Crime` (tag), `Crime` (ComponentBase), `CrimeProducer`, `CrimeVictim`, `AccidentSite`, `AccidentSiteFlags`, `AddAccidentSite`, `PoliceConfigurationData`, `CrimeAccumulationData`, `CrimeCheckSystem`, `CriminalSystem`, `CrimeAccumulationSystem`
