# Research: Citizen Sickness

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How to programmatically make a citizen sick in CS2, and how to make every citizen inside a specific building sick.

**Why**: To build mods that trigger sickness on individual citizens or all occupants of a building — for disease events, hazard systems, or gameplay mechanics.

**Boundaries**: Not covering healthcare dispatch/ambulance AI, hospital treatment mechanics, or natural sickness from pollution/sewage. Focused strictly on the programmatic "make sick" mechanism.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Citizens | `HealthProblem`, `HealthProblemFlags`, `Citizen`, `CurrentBuilding`, `CurrentTransport`, `HouseholdMember`, `HouseholdCitizen` |
| Game.dll | Game.Events | `AddHealthProblem`, `AddHealthProblemSystem`, `HealthEvent` (tag), `TargetElement` |
| Game.dll | Game.Simulation | `SicknessCheckSystem` |
| Game.dll | Game.Buildings | `Renter`, `PropertyRenter` |
| Game.dll | Game.Prefabs | `HealthcareParameterData`, `HealthEventData`, `HealthEventType` |

## Component Map

### `HealthProblem` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The event entity that caused this problem (fire, accident, etc.) |
| m_HealthcareRequest | Entity | Reference to the active healthcare service request (ambulance/hearse) |
| m_Flags | HealthProblemFlags | Bitfield describing the type of health problem |
| m_Timer | byte | Timer for healthcare notification delays |

Placed on citizen entities to indicate they have a health problem. Persisted via `ISerializable`. Multiple problems merge via flag OR (Dead takes priority over other states).

*Source: `Game.dll` → `Game.Citizens.HealthProblem`*

### `HealthProblemFlags` (Game.Citizens)

```csharp
[Flags]
public enum HealthProblemFlags : byte
{
    None            = 0,
    Sick            = 1,      // Citizen is sick (disease, pollution, etc.)
    Dead            = 2,      // Citizen is dead
    Injured         = 4,      // Citizen is injured (accident)
    RequireTransport = 8,     // Needs ambulance/hearse pickup
    InDanger        = 0x10,   // In a dangerous situation (burning building)
    Trapped         = 0x20,   // Trapped in collapsed/destroyed building
    NoHealthcare    = 0x40    // No healthcare available
}
```

Key flag combinations:
- `Sick` — basic sickness, citizen seeks healthcare on their own
- `Sick | RequireTransport` — severe sickness, needs ambulance
- `Dead | RequireTransport` — dead, needs hearse
- `Injured | RequireTransport` — injured and immobilized
- `InDanger` — in a burning building (added by fire ignition)
- `Trapped` — in a destroyed building (added by building destruction)

*Source: `Game.dll` → `Game.Citizens.HealthProblemFlags`*

### `AddHealthProblem` (Game.Events)

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The event entity causing the problem |
| m_Target | Entity | The citizen entity to affect |
| m_Flags | HealthProblemFlags | Which health problem flags to set |

Command component — created as a temporary event entity with `Game.Common.Event` tag. Processed by `AddHealthProblemSystem` to add/merge `HealthProblem` on the target citizen. **This is the primary way to make a citizen sick programmatically.**

*Source: `Game.dll` → `Game.Events.AddHealthProblem`*

### `Citizen` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_PseudoRandom | ushort | Seed for deterministic random generation |
| m_State | CitizenFlags | Bitfield: age, gender, education, tourist/commuter status |
| m_WellBeing | byte | Well-being score (0-255) |
| m_Health | byte | Health score (0-255) |
| m_LeisureCounter | byte | Leisure activity tracking |
| m_PenaltyCounter | byte | Penalty tracking |
| m_UnemploymentCounter | int | Unemployment duration |
| m_BirthDay | short | Birth day (simulation day number) |
| m_UnemploymentTimeCounter | float | Unemployment time for happiness effect |
| m_SicknessPenalty | int | Accumulated penalty from being sick |

The `m_Health` field is the citizen's base health (0-255). The `Happiness` property is `(m_WellBeing + m_Health) / 2`. The `m_SicknessPenalty` field accumulates penalties from repeated sickness.

*Source: `Game.dll` → `Game.Citizens.Citizen`*

### `CurrentBuilding` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_CurrentBuilding | Entity | The building entity where this citizen currently is |

Tracks which building a citizen is physically inside. This is the key component for finding "all citizens in a building" — the game's `FindCitizensInBuildingJob` queries all citizens with `CurrentBuilding` and filters by `m_CurrentBuilding == targetBuilding`.

*Source: `Game.dll` → `Game.Citizens.CurrentBuilding`*

### `HouseholdCitizen` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Citizen | Entity | A citizen entity belonging to this household |

Buffer element on household entities. Lists all citizens in a household. `InternalBufferCapacity(5)` — households typically have up to 5 members.

*Source: `Game.dll` → `Game.Citizens.HouseholdCitizen`*

### `HouseholdMember` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Household | Entity | The household entity this citizen belongs to |

Component on citizen entities linking them to their household.

*Source: `Game.dll` → `Game.Citizens.HouseholdMember`*

### `Renter` (Game.Buildings)

| Field | Type | Description |
|-------|------|-------------|
| m_Renter | Entity | A household or company entity renting space in this building |

Buffer element on building entities. Lists all renters (households for residential, companies for commercial/industrial).

*Source: `Game.dll` → `Game.Buildings.Renter`*

### `PropertyRenter` (Game.Buildings)

| Field | Type | Description |
|-------|------|-------------|
| m_Property | Entity | The building entity this household/company rents |
| m_Rent | int | Rent amount |

Component on household/company entities linking them to their building.

*Source: `Game.dll` → `Game.Buildings.PropertyRenter`*

### `HealthEventData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_RandomTargetType | EventTargetType | Target type for this health event (Citizen for natural sickness) |
| m_HealthEventType | HealthEventType | Disease, Injury, or Death |
| m_OccurenceProbability | Bounds1 | Min/max probability range (interpolated by health) |
| m_TransportProbability | Bounds1 | Min/max chance of needing ambulance transport |
| m_RequireTracking | bool | If true, creates full event entity; if false, creates lightweight AddHealthProblem |

Per-prefab configuration for health events. Used by `SicknessCheckSystem` to determine natural sickness probability.

*Source: `Game.dll` → `Game.Prefabs.HealthEventData`*

### `HealthEventType` (Game.Prefabs)

```csharp
public enum HealthEventType
{
    Disease,  // Maps to HealthProblemFlags.Sick
    Injury,   // Maps to HealthProblemFlags.Injured
    Death     // Maps to HealthProblemFlags.Dead
}
```

## System Map

### `AddHealthProblemSystem` (Game.Events)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (every frame)
- **Queries**:
  - Event query: `[Event]` AND any of `[AddHealthProblem, Ignite, Destroy]`
  - Citizen query: `[Citizen, CurrentBuilding]`, excluding `[Deleted]`
  - Healthcare settings: `[HealthcareParameterData]` singleton
- **Reads**: AddHealthProblem, Ignite (for building fires), Destroy (for building destruction), CurrentBuilding, HouseholdMember, HouseholdCitizen, PrefabRef, CurrentTransport, HealthcareParameterData
- **Writes**: HealthProblem (adds/merges), PathOwner (stops movement), Target (clears), creates journal data, fires trigger events
- **Key methods**:
  - `FindCitizensInBuildingJob.Execute()` — Iterates ALL citizens with `CurrentBuilding`. For each citizen whose `m_CurrentBuilding == m_Building`: creates `AddHealthProblem` with the specified flags. If `m_DeathProbability > 0`, rolls death chance and adds `Dead | RequireTransport`.
  - `AddHealthProblemJob.Execute()` — Processes both direct `AddHealthProblem` event entities AND queued results from `FindCitizensInBuildingJob`. Deduplicates by target citizen (merges flags). Adds `HealthProblem` component if not present, or merges with existing. For `Dead/Injured + RequireTransport`: stops citizen movement. Fires trigger events: `CitizenGotSick`, `CitizenGotInjured`, `CitizenGotTrapped`, `CitizenGotInDanger`.
  - `MergeProblems()` — Priority: Dead > RequireTransport > non-null Event > flag union.
  - `StopMoving()` — Clears pathfinding on the citizen's current transport (if any).
- **Usage by the game**:
  - **Fire ignition** (`Ignite` event targeting a building): Schedules `FindCitizensInBuildingJob` with `flags = InDanger`, `deathProb = 0`
  - **Building destruction** (`Destroy` event targeting a building): Schedules `FindCitizensInBuildingJob` with `flags = Trapped`, `deathProb = HealthcareParameterData.m_BuildingDestoryDeathRate`

### `SicknessCheckSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 16384 frames (~once per game day, partitioned into 16 sub-frames)
- **Queries**:
  - Citizen query: `[Citizen, UpdateFrame]`, excluding `[HealthProblem, Deleted, Temp]` — only healthy citizens
  - Event query: `[HealthEventData]`, excluding `[Locked]`
- **Reads**: Citizen (m_Health), HouseholdMember, HouseholdCitizen, Worker, CityModifier, ServiceFee, HealthProblem, EconomyParameterData, TaxRates
- **Writes**: Creates `AddHealthProblem` event entities via EntityCommandBuffer
- **Key methods**:
  - `TryAddHealthProblem()` — Calculates sickness probability from citizen health:
    - `t = saturate(pow(2, 10 - health * 0.1) * 0.001)`
    - At health=255: t ≈ 0 (virtually immune)
    - At health=100: t ≈ 0.001
    - At health=50: t ≈ 0.032
    - At health=10: t ≈ 0.5
    - At health=0: t = 1.0 (guaranteed sick)
    - Probability = `lerp(occurenceProbability.min, occurenceProbability.max, t)`
    - For Disease type: modified by `CityModifier.DiseaseProbability`
  - `CreateHealthEvent()` — Maps `HealthEventType` to flags:
    - Disease → `Sick`
    - Injury → `Injured`
    - Death → `Dead`
    - Transport probability: `lerp(transportMax, transportMin, health * 0.01)` — healthier citizens less likely to need ambulance
    - NoHealthcare flag: `10/health - fee/2 * income` threshold — poor/unhealthy citizens may skip hospital

This is how **natural sickness** works — it's the system a mod would replace or supplement to change sickness rates.

## Data Flow

### Making a single citizen sick

```
[Mod creates AddHealthProblem event entity]
    Entity with: Event + AddHealthProblem
    AddHealthProblem { m_Event = Entity.Null, m_Target = citizenEntity, m_Flags = Sick }
    │
    ▼
[AddHealthProblemSystem.AddHealthProblemJob] (every frame)
    │
    ├── If citizen already has HealthProblem: merges flags (ORs new flags in)
    │
    ├── If citizen doesn't have HealthProblem: adds component via ECB
    │
    ├── If Dead|Injured + RequireTransport: stops citizen movement
    │
    ├── Fires TriggerAction(TriggerType.CitizenGotSick, ...)
    │
    └── Creates journal data for Sick/Dead/Injured
```

### Making all citizens in a building sick

```
[Two approaches — Direct or via FindCitizensInBuildingJob pattern]

APPROACH 1: Create AddHealthProblem events per citizen (mod-side iteration)

[Mod queries: Citizen + CurrentBuilding, filters m_CurrentBuilding == targetBuilding]
    │
    ▼
[For each matching citizen: create AddHealthProblem event entity]
    AddHealthProblem { m_Event = ..., m_Target = citizenEntity, m_Flags = Sick }
    │
    ▼
[AddHealthProblemSystem processes them all next frame]

APPROACH 2: Leverage existing Ignite/Destroy pipeline (indirect)

[Mod creates Ignite event targeting the building]
    │
    ▼
[AddHealthProblemSystem.FindCitizensInBuildingJob]
    Automatically finds all citizens with CurrentBuilding == building
    Creates AddHealthProblem(InDanger) for each
    │
    ▼
[AddHealthProblemSystem.AddHealthProblemJob processes them]
    NOTE: This sets InDanger, not Sick. Also ignites the building.
```

### Entity relationship: Building → Citizens

```
Building entity
    │
    ├── has DynamicBuffer<Renter>
    │       each Renter.m_Renter → Household entity
    │                                   │
    │                                   ├── has DynamicBuffer<HouseholdCitizen>
    │                                   │       each HouseholdCitizen.m_Citizen → Citizen entity
    │                                   │
    │                                   └── has PropertyRenter.m_Property → Building entity (back-link)
    │
    └── Citizens also have:
            CurrentBuilding.m_CurrentBuilding → Building entity (tracks physical location)
            HouseholdMember.m_Household → Household entity (permanent membership)

NOTE: CurrentBuilding tracks where a citizen IS right now.
      PropertyRenter/Renter tracks where a citizen LIVES.
      A citizen at work has CurrentBuilding = workplace, not home.
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Building destroy death rate | HealthcareParameterData.m_BuildingDestoryDeathRate | Global singleton |
| Transport warning time | HealthcareParameterData.m_TransportWarningTime | Global singleton |
| No-resource treatment penalty | HealthcareParameterData.m_NoResourceTreatmentPenalty | Global singleton |
| Death rate curve | HealthcareParameterData.m_DeathRate | AnimationCurve1 singleton |
| Ambulance notification | HealthcareParameterData.m_AmbulanceNotificationPrefab | Notification icon |
| Hearse notification | HealthcareParameterData.m_HearseNotificationPrefab | Notification icon |

## Harmony Patch Points

### Candidate 1: `Game.Events.AddHealthProblemSystem.OnUpdate()`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix (to block) or Postfix (to add custom health problems)
- **What it enables**: Intercept all health problem additions. A Prefix could filter which citizens get sick. A Postfix could add additional effects after sickness is applied.
- **Risk level**: Medium — central to all health problem assignment
- **Side effects**: Blocking would prevent fire/destruction casualties too

### Candidate 2: Direct component manipulation (No Harmony needed)

- A custom system can directly add `HealthProblem` via `EntityCommandBuffer.AddComponent<HealthProblem>()` without going through the event pipeline. This bypasses merge logic and trigger events but is simpler.
- **Risk level**: Low — but misses journal tracking and trigger events

### Alternative: ECS System Approach (Recommended)

Create `AddHealthProblem` event entities. This is the safest approach because:
1. `AddHealthProblemSystem` handles deduplication and merging
2. Trigger events fire properly (`CitizenGotSick`, etc.)
3. Journal data is created for statistics
4. Movement stopping is handled for RequireTransport cases

## Mod Blueprint

### Make a single citizen sick:

```csharp
public void MakeCitizenSick(Entity citizenEntity)
{
    // Create the AddHealthProblem command entity
    var archetype = EntityManager.CreateArchetype(
        ComponentType.ReadWrite<Game.Common.Event>(),
        ComponentType.ReadWrite<AddHealthProblem>()
    );

    Entity cmd = EntityManager.CreateEntity(archetype);
    EntityManager.SetComponentData(cmd, new AddHealthProblem
    {
        m_Event = Entity.Null,          // No parent event — standalone sickness
        m_Target = citizenEntity,       // The citizen to make sick
        m_Flags = HealthProblemFlags.Sick
    });
    // AddHealthProblemSystem processes this next frame
}
```

### Make a single citizen severely sick (needs ambulance):

```csharp
public void MakeCitizenSeverelySick(Entity citizenEntity)
{
    var archetype = EntityManager.CreateArchetype(
        ComponentType.ReadWrite<Game.Common.Event>(),
        ComponentType.ReadWrite<AddHealthProblem>()
    );

    Entity cmd = EntityManager.CreateEntity(archetype);
    EntityManager.SetComponentData(cmd, new AddHealthProblem
    {
        m_Event = Entity.Null,
        m_Target = citizenEntity,
        m_Flags = HealthProblemFlags.Sick | HealthProblemFlags.RequireTransport
    });
    // Citizen will stop moving and wait for ambulance
}
```

### Make all citizens currently inside a building sick:

```csharp
// In a custom GameSystemBase:
private EntityQuery m_CitizenQuery;
private EntityArchetype m_AddHealthProblemArchetype;

protected override void OnCreate()
{
    base.OnCreate();
    m_CitizenQuery = GetEntityQuery(
        ComponentType.ReadOnly<Citizen>(),
        ComponentType.ReadOnly<CurrentBuilding>(),
        ComponentType.Exclude<Deleted>()
    );
    m_AddHealthProblemArchetype = EntityManager.CreateArchetype(
        ComponentType.ReadWrite<Game.Common.Event>(),
        ComponentType.ReadWrite<AddHealthProblem>()
    );
}

public void MakeAllCitizensInBuildingSick(Entity buildingEntity)
{
    // Approach: query all citizens, filter by CurrentBuilding
    var entityHandle = GetEntityTypeHandle();
    var currentBuildingHandle = GetComponentTypeHandle<CurrentBuilding>(true);
    var chunks = m_CitizenQuery.ToArchetypeChunkArray(Allocator.Temp);

    foreach (var chunk in chunks)
    {
        var entities = chunk.GetNativeArray(entityHandle);
        var buildings = chunk.GetNativeArray(ref currentBuildingHandle);

        for (int i = 0; i < entities.Length; i++)
        {
            if (buildings[i].m_CurrentBuilding == buildingEntity)
            {
                Entity cmd = EntityManager.CreateEntity(m_AddHealthProblemArchetype);
                EntityManager.SetComponentData(cmd, new AddHealthProblem
                {
                    m_Event = Entity.Null,
                    m_Target = entities[i],
                    m_Flags = HealthProblemFlags.Sick
                });
            }
        }
    }
    chunks.Dispose();
}
```

### Alternative: Get citizens via Building → Renter → Household → HouseholdCitizen chain:

```csharp
// This gets citizens who LIVE in the building, not necessarily who are there now.
// Use this when you want permanent residents, not just current occupants.
public void MakeAllResidentsSick(Entity buildingEntity)
{
    if (!EntityManager.TryGetBuffer<Renter>(buildingEntity, true, out var renters))
        return;

    for (int i = 0; i < renters.Length; i++)
    {
        Entity household = renters[i].m_Renter;
        if (!EntityManager.HasComponent<Household>(household))
            continue;

        if (!EntityManager.TryGetBuffer<HouseholdCitizen>(household, true, out var citizens))
            continue;

        for (int j = 0; j < citizens.Length; j++)
        {
            Entity citizen = citizens[j].m_Citizen;
            Entity cmd = EntityManager.CreateEntity(m_AddHealthProblemArchetype);
            EntityManager.SetComponentData(cmd, new AddHealthProblem
            {
                m_Event = Entity.Null,
                m_Target = citizen,
                m_Flags = HealthProblemFlags.Sick
            });
        }
    }
}
```

### Systems to create:
- **SicknessSystem** — Custom system that creates `AddHealthProblem` events based on mod logic (triggered by player, hazard zones, random events, etc.)

### Components to add:
- **SicknessImmune** — Optional marker to prevent citizens from getting sick (filter before creating events)
- **BiohazardZone** — Optional area marker that triggers sickness for citizens within it

### Patches needed:
- For basic sickness triggering: **none** (ECS approach via `AddHealthProblem` events is sufficient)
- For modifying sickness behavior: Postfix on `AddHealthProblemSystem.OnUpdate()` to alter flag merging or death probability

### Settings:
- Sickness severity (Sick only vs Sick + RequireTransport)
- Death probability for building events
- Target scope (single citizen, building occupants, building residents)

## Examples

### Example 1: Make a single citizen sick

Create an `AddHealthProblem` event entity targeting a specific citizen. The `AddHealthProblemSystem` picks this up on the next simulation frame, adds the `HealthProblem` component, fires trigger events, and creates journal data.

```csharp
using Game.Citizens;
using Game.Common;
using Game.Events;
using Unity.Entities;

/// <summary>
/// Makes a single citizen sick by creating an AddHealthProblem event entity.
/// Call this from within a GameSystemBase (where you have access to EntityManager).
/// </summary>
public void MakeCitizenSick(EntityManager entityManager, Entity citizenEntity)
{
    // Create an event entity with the Event tag + AddHealthProblem component.
    // The Event tag marks it for processing by AddHealthProblemSystem.
    EntityArchetype archetype = entityManager.CreateArchetype(
        ComponentType.ReadWrite<Event>(),
        ComponentType.ReadWrite<AddHealthProblem>()
    );

    Entity eventEntity = entityManager.CreateEntity(archetype);
    entityManager.SetComponentData(eventEntity, new AddHealthProblem
    {
        m_Event = Entity.Null,               // No parent event (standalone sickness)
        m_Target = citizenEntity,            // The citizen to make sick
        m_Flags = HealthProblemFlags.Sick    // Basic sickness — citizen seeks healthcare on their own
    });

    // AddHealthProblemSystem processes this next frame:
    //   - Adds HealthProblem component to the citizen (or merges flags if already sick)
    //   - Fires TriggerAction(TriggerType.CitizenGotSick, ...)
    //   - Creates journal entry for statistics tracking
}

/// <summary>
/// Makes a citizen severely sick — they collapse and need an ambulance.
/// Use Sick | RequireTransport to trigger ambulance dispatch.
/// </summary>
public void MakeCitizenSeverelySick(EntityManager entityManager, Entity citizenEntity)
{
    EntityArchetype archetype = entityManager.CreateArchetype(
        ComponentType.ReadWrite<Event>(),
        ComponentType.ReadWrite<AddHealthProblem>()
    );

    Entity eventEntity = entityManager.CreateEntity(archetype);
    entityManager.SetComponentData(eventEntity, new AddHealthProblem
    {
        m_Event = Entity.Null,
        m_Target = citizenEntity,
        m_Flags = HealthProblemFlags.Sick | HealthProblemFlags.RequireTransport
    });

    // RequireTransport causes AddHealthProblemSystem to also:
    //   - Stop the citizen's movement (clears pathfinding via StopMoving)
    //   - The citizen waits in place for an ambulance
}
```

### Example 2: Make all citizens currently inside a building sick

Query all citizens with `CurrentBuilding`, filter by the target building entity, and create an `AddHealthProblem` event for each match. This follows the same pattern the game uses in `FindCitizensInBuildingJob`.

```csharp
using Game.Citizens;
using Game.Common;
using Game.Events;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// A custom GameSystemBase that can make all citizens physically present
/// in a building sick. Uses CurrentBuilding to find who is actually there
/// right now (not just residents — also workers, visitors, etc.).
/// </summary>
public partial class BuildingSicknessSystem : GameSystemBase
{
    private EntityQuery _citizenQuery;
    private EntityArchetype _addHealthProblemArchetype;

    protected override void OnCreate()
    {
        base.OnCreate();

        // Query all citizens who are currently inside any building.
        // CurrentBuilding tracks physical location — not home address.
        _citizenQuery = GetEntityQuery(
            ComponentType.ReadOnly<Citizen>(),
            ComponentType.ReadOnly<CurrentBuilding>(),
            ComponentType.Exclude<Deleted>()
        );

        // Archetype for the AddHealthProblem event entity.
        // Event tag is required — AddHealthProblemSystem queries for it.
        _addHealthProblemArchetype = EntityManager.CreateArchetype(
            ComponentType.ReadWrite<Event>(),
            ComponentType.ReadWrite<AddHealthProblem>()
        );
    }

    /// <summary>
    /// Iterates all citizens with CurrentBuilding, creates AddHealthProblem
    /// events for those inside the target building.
    /// </summary>
    public void MakeAllOccupantsSick(Entity buildingEntity)
    {
        var entityHandle = GetEntityTypeHandle();
        var currentBuildingHandle = GetComponentTypeHandle<CurrentBuilding>(true);
        NativeArray<ArchetypeChunk> chunks = _citizenQuery.ToArchetypeChunkArray(Allocator.Temp);

        try
        {
            foreach (ArchetypeChunk chunk in chunks)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityHandle);
                NativeArray<CurrentBuilding> buildings = chunk.GetNativeArray(ref currentBuildingHandle);

                for (int i = 0; i < entities.Length; i++)
                {
                    // CurrentBuilding.m_CurrentBuilding is the building the citizen
                    // is physically inside right now. Compare against our target.
                    if (buildings[i].m_CurrentBuilding == buildingEntity)
                    {
                        Entity cmd = EntityManager.CreateEntity(_addHealthProblemArchetype);
                        EntityManager.SetComponentData(cmd, new AddHealthProblem
                        {
                            m_Event = Entity.Null,
                            m_Target = entities[i],
                            m_Flags = HealthProblemFlags.Sick
                        });
                    }
                }
            }
        }
        finally
        {
            chunks.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        // Trigger MakeAllOccupantsSick from mod logic (e.g., UI button, event, etc.)
    }
}
```

### Example 3: Check if a citizen is sick

Read the `HealthProblem` component to inspect a citizen's current health problem flags. Citizens without `HealthProblem` are healthy.

```csharp
using Game.Citizens;
using Unity.Entities;

/// <summary>
/// Checks whether a citizen entity currently has any health problem,
/// and inspects specific flags. Citizens without the HealthProblem
/// component are healthy.
/// </summary>
public void CheckCitizenHealth(EntityManager entityManager, Entity citizenEntity)
{
    // HealthProblem is only present on citizens who have an active health issue.
    // Healthy citizens do not have this component at all.
    if (!entityManager.HasComponent<HealthProblem>(citizenEntity))
    {
        // Citizen is healthy — no active health problems.
        return;
    }

    HealthProblem problem = entityManager.GetComponentData<HealthProblem>(citizenEntity);

    // Check specific flags using bitwise AND.
    // Multiple flags can be set simultaneously (e.g., Sick | RequireTransport).
    bool isSick = (problem.m_Flags & HealthProblemFlags.Sick) != 0;
    bool isDead = (problem.m_Flags & HealthProblemFlags.Dead) != 0;
    bool isInjured = (problem.m_Flags & HealthProblemFlags.Injured) != 0;
    bool needsAmbulance = (problem.m_Flags & HealthProblemFlags.RequireTransport) != 0;
    bool isInDanger = (problem.m_Flags & HealthProblemFlags.InDanger) != 0;
    bool isTrapped = (problem.m_Flags & HealthProblemFlags.Trapped) != 0;

    // The m_Event field links to the event that caused the problem (or Entity.Null).
    // The m_HealthcareRequest field links to the active ambulance/hearse request.
    bool hasAmbulanceDispatched = problem.m_HealthcareRequest != Entity.Null;
}

/// <summary>
/// Query example: find all sick citizens in a job-friendly way using EntityQuery.
/// This queries citizens who have the HealthProblem component.
/// </summary>
public EntityQuery CreateSickCitizenQuery(GameSystemBase system)
{
    // Citizens WITH HealthProblem are sick/injured/dead/etc.
    // Add Exclude<Deleted> to skip destroyed entities.
    return system.GetEntityQuery(
        ComponentType.ReadOnly<Citizen>(),
        ComponentType.ReadOnly<HealthProblem>(),
        ComponentType.Exclude<Deleted>()
    );
}
```

### Example 4: How natural sickness probability works (the wellbeing curve)

The `SicknessCheckSystem` runs once per game day and checks each healthy citizen against an exponential sickness probability curve based on `Citizen.m_Health`. Lower health means dramatically higher chance of getting sick.

```csharp
using Unity.Mathematics;

/// <summary>
/// Demonstrates the exact sickness probability curve from SicknessCheckSystem.
/// This is how the game calculates the chance of a citizen getting sick naturally.
///
/// The curve is exponential: healthy citizens are nearly immune, while
/// citizens with low health get sick frequently.
///
/// SicknessCheckSystem runs once per game day, partitioned across 16 sub-frames
/// (update interval = 262144 / 16 = 16384 frames). It only checks citizens
/// who do NOT already have a HealthProblem component.
/// </summary>
public static class SicknessProbabilityExample
{
    /// <summary>
    /// Calculates the sickness interpolation factor 't' from a citizen's health.
    /// This is the exact formula from SicknessCheckSystem.TryAddHealthProblem().
    ///
    /// The 't' value is then used to interpolate between the min and max
    /// occurrence probability defined in the HealthEventData prefab:
    ///   finalProbability = lerp(occurenceProbability.min, occurenceProbability.max, t)
    /// </summary>
    /// <param name="health">Citizen.m_Health (0-255)</param>
    /// <returns>Interpolation factor 't' in range [0, 1]</returns>
    public static float CalculateSicknessT(byte health)
    {
        // From decompiled SicknessCheckSystem.TryAddHealthProblem:
        //   float t = math.saturate(math.pow(2f, 10f - (float)(int)citizen.m_Health * 0.1f) * 0.001f);
        float t = math.saturate(math.pow(2f, 10f - (float)(int)health * 0.1f) * 0.001f);
        return t;

        // Reference values:
        //   health = 255  ->  t ≈ 0.000  (virtually immune)
        //   health = 200  ->  t ≈ 0.000
        //   health = 100  ->  t ≈ 0.001  (very low chance)
        //   health =  50  ->  t ≈ 0.032
        //   health =  10  ->  t ≈ 0.512
        //   health =   0  ->  t = 1.000  (guaranteed sick)
    }

    /// <summary>
    /// Shows the full probability calculation including HealthEventData prefab values.
    /// The final probability is what gets rolled against random.NextFloat(100).
    ///
    /// For Disease-type events, the probability is further modified by the
    /// CityModifier.DiseaseProbability city modifier (e.g., from polluted water).
    /// </summary>
    /// <param name="health">Citizen.m_Health (0-255)</param>
    /// <param name="occurrenceMin">HealthEventData.m_OccurenceProbability.min</param>
    /// <param name="occurrenceMax">HealthEventData.m_OccurenceProbability.max</param>
    /// <returns>Probability value compared against random.NextFloat(100)</returns>
    public static float CalculateFinalProbability(byte health, float occurrenceMin, float occurrenceMax)
    {
        float t = CalculateSicknessT(health);

        // Interpolate between prefab-defined min/max probability
        float probability = math.lerp(occurrenceMin, occurrenceMax, t);
        return probability;

        // The game rolls: random.NextFloat(100) < probability
        // So if probability = 5, there's a 5% chance per check (once per game day).
    }

    /// <summary>
    /// Shows how transport probability (needing an ambulance) is calculated.
    /// Healthier citizens are less likely to need ambulance transport.
    /// From SicknessCheckSystem.CreateHealthEvent().
    /// </summary>
    /// <param name="health">Citizen.m_Health (0-255)</param>
    /// <param name="transportMin">HealthEventData.m_TransportProbability.min</param>
    /// <param name="transportMax">HealthEventData.m_TransportProbability.max</param>
    /// <returns>Transport probability compared against random.NextFloat(100)</returns>
    public static float CalculateTransportProbability(byte health, float transportMin, float transportMax)
    {
        // Note: lerp goes from max to min as health increases (inverted)
        float prob = math.lerp(transportMax, transportMin, (float)(int)health * 0.01f);
        return prob;

        // At health=0:   prob = transportMax  (most likely to need ambulance)
        // At health=100: prob = transportMin  (least likely to need ambulance)
        // Rolled as: random.NextFloat(100) < prob
    }
}
```

## Open Questions

- [x] How does natural sickness work? **Answered**: `SicknessCheckSystem` runs once/day, iterates healthy citizens, calculates sickness probability from `Citizen.m_Health` using exponential curve `pow(2, 10 - health*0.1) * 0.001`, rolls against `HealthEventData` prefab probabilities. Disease probability modified by `CityModifier.DiseaseProbability`.
- [ ] What determines `Citizen.m_Health` decay over time? The health byte presumably decreases from sickness and increases from healthcare, but the exact system wasn't traced.
- [ ] How does `m_SicknessPenalty` on the `Citizen` component accumulate and affect gameplay? It's serialized but the system that reads/writes it wasn't traced.
- [ ] What happens when a citizen with `HealthProblemFlags.Sick` (without `RequireTransport`) reaches a hospital? The treatment/recovery system wasn't traced.
- [ ] Are there different sickness types/severities beyond the flag combinations, or is all sickness treated uniformly?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
