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
| Game.dll | Game.Events | `AddHealthProblem`, `AddHealthProblemSystem`, `TargetElement` |
| Game.dll | Game.Buildings | `Renter`, `PropertyRenter` |
| Game.dll | Game.Prefabs | `HealthcareParameterData` |

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

## Open Questions

- [ ] How does natural sickness work (from pollution, sewage, lack of healthcare)? There's likely a separate system (possibly `CitizenHealthSystem` or similar) that creates `AddHealthProblem` events based on environmental factors — not yet traced.
- [ ] What determines `Citizen.m_Health` decay over time? The health byte presumably decreases from sickness and increases from healthcare, but the exact system wasn't traced.
- [ ] How does `m_SicknessPenalty` on the `Citizen` component accumulate and affect gameplay? It's serialized but the system that reads/writes it wasn't traced.
- [ ] What happens when a citizen with `HealthProblemFlags.Sick` (without `RequireTransport`) reaches a hospital? The treatment/recovery system wasn't traced.
- [ ] Are there different sickness types/severities beyond the flag combinations, or is all sickness treated uniformly?

## Sources

- Decompiled from: Game.dll (Cities: Skylines II)
- Tool: ilspycmd v9.1 (.NET 8.0)
- Game version: Current Steam release as of 2026-02-15
