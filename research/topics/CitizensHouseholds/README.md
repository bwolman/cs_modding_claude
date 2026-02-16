# Research: Citizens & Households

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 models individual citizens and households -- the full lifecycle from birth to death, including aging, happiness, employment, housing, and household migration (moving in/moving away).

**Why**: Citizens and households are the core population simulation in CS2. Understanding these systems is essential for mods that affect population growth, citizen behavior, happiness factors, housing assignments, or employment mechanics.

**Boundaries**: This research covers the citizen lifecycle (birth, aging, death), the household entity model (spawn, property assignment, move-away), citizen happiness, and the worker/student subsystems. It does not cover transportation pathfinding, leisure activities, or commercial shopping in detail -- only where they intersect with citizen/household state.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Citizens | Citizen, Household, HouseholdCitizen, HouseholdMember, Worker, Student, CitizenAge, CitizenFlags, HouseholdFlags, HealthProblem, TravelPurpose, CurrentBuilding, Child, Teen, Adult, Elderly, CarKeeper, Criminal, CrimeVictim, LookingForPartner, LeaveHouseholdTag, HouseholdNeed |
| Game.dll | Game.Simulation | AgingSystem, BirthSystem, DeathCheckSystem, CitizenBehaviorSystem, CitizenHappinessSystem, CitizenFindJobSystem, FindJobSystem, WorkerSystem, StudentSystem, HouseholdSpawnSystem, HouseholdFindPropertySystem, HouseholdMoveAwaySystem, HouseholdBehaviorSystem, LeaveHouseholdSystem, CountHouseholdDataSystem, CitizenPresenceSystem |
| Game.dll | Game.Citizens | CitizenInitializeSystem, HouseholdInitializeSystem, HouseholdAndCitizenRemoveSystem, CitizenUtils |
| Game.dll | Game.Prefabs | CitizenPrefab, HouseholdPrefab, CitizenParametersPrefab, CitizenHappinessPrefab |
| Game.dll | Game.Buildings | PropertyRenter, Renter (buffer) |

## Architecture Overview

Citizens and households use a two-entity model:

- **Citizen entity**: Represents an individual person. Has a `Citizen` component (health, wellbeing, birth day, flags encoding age/gender/education), plus optional `Worker`, `Student`, `HealthProblem`, `TravelPurpose`, etc.
- **Household entity**: Represents a family unit. Has a `Household` component (resources/money, consumption, flags), a `DynamicBuffer<HouseholdCitizen>` listing members, and a `PropertyRenter` linking to a building.

The link is bidirectional: each citizen has a `HouseholdMember` component pointing to its household, and each household has a `HouseholdCitizen` buffer listing its citizens.

### Citizen Lifecycle

1. **Spawn/Immigration**: `HouseholdSpawnSystem` creates household entities at outside connections based on residential demand. `HouseholdInitializeSystem` populates them with citizen entities.
2. **Birth**: `BirthSystem` creates new child citizens within existing households (adult female + adult male = chance of baby).
3. **Aging**: `AgingSystem` advances citizens through four age stages based on simulation day thresholds: Child (day 0-20), Teen (day 21-35), Adult (day 36-83), Elderly (day 84+).
4. **Education**: Children and teens can attend school (`Student` component). Teens who become adults leave school.
5. **Employment**: Adults seek jobs via `CitizenFindJobSystem`/`FindJobSystem`, gaining a `Worker` component. Elderly lose their `Worker` on aging.
6. **Happiness**: `CitizenHappinessSystem` evaluates 26 factors (utilities, services, pollution, taxes, etc.) and updates `m_Health` and `m_WellBeing` bytes on the `Citizen` component.
7. **Death**: `DeathCheckSystem` applies age-based death probability and sickness death. Dead citizens get `HealthProblemFlags.Dead` and are collected by deathcare.
8. **Move-away**: `HouseholdMoveAwaySystem` processes households tagged with `MovingAway`, routing them to outside connections and deleting them on arrival.

## Component Map

### `Citizen` (Game.Citizens)

The core data for an individual citizen.

| Field | Type | Description |
|-------|------|-------------|
| m_PseudoRandom | ushort | Seed for deterministic random decisions |
| m_State | CitizenFlags | Bitfield: age, gender, education level, tourist/commuter status |
| m_WellBeing | byte | 0-255 wellbeing score |
| m_Health | byte | 0-255 health score |
| m_LeisureCounter | byte | Decremented over time; reset by leisure activities |
| m_PenaltyCounter | byte | Penalty tracking counter |
| m_UnemploymentCounter | int | Days unemployed |
| m_BirthDay | short | Simulation day of birth |
| m_UnemploymentTimeCounter | float | Continuous unemployment time for happiness penalty |
| m_SicknessPenalty | int | Health penalty from sickness episodes |

**Derived properties:**
- `Happiness` = `(m_WellBeing + m_Health) / 2`
- Age = `GetAge()` reads AgeBit1/AgeBit2 from m_State
- Education level = 0-4, encoded in EducationBit1/2/3

### `CitizenAge` (Game.Citizens)

```
Child = 0, Teen = 1, Adult = 2, Elderly = 3
```

### `CitizenFlags` (Game.Citizens)

| Flag | Value | Description |
|------|-------|-------------|
| AgeBit1, AgeBit2 | 0x01, 0x02 | Encode CitizenAge (2 bits) |
| MovingAwayReachOC | 0x04 | Citizen has reached outside connection while moving away |
| Male | 0x08 | Gender flag |
| EducationBit1/2/3 | 0x10/0x20/0x40 | Encode education level 0-4 |
| FailedEducationBit1/2 | 0x80/0x100 | Failed education attempt count |
| Tourist | 0x200 | Citizen is a tourist |
| Commuter | 0x400 | Citizen is a commuter |
| LookingForPartner | 0x800 | Seeking a partner |
| NeedsNewJob | 0x1000 | Flagged for job search |
| BicycleUser | 0x2000 | Uses bicycle for transport |

### `Household` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | HouseholdFlags | Tourist, Commuter, or MovedIn |
| m_Resources | int | Money/resources available |
| m_ConsumptionPerDay | short | Daily consumption rate |
| m_ShoppedValuePerDay | uint | Current day shopping spending |
| m_ShoppedValueLastDay | uint | Previous day shopping spending |
| m_SalaryLastDay | int | Income earned last day |

### `HouseholdCitizen` (Game.Citizens) -- Buffer

Buffer element on household entities. Lists all citizens belonging to the household. Internal capacity of 5 elements.

### `HouseholdMember` (Game.Citizens) -- Component

Attached to citizen entities. Single field `m_Household` pointing to the household entity.

### `Worker` (Game.Citizens)

| Field | Type | Description |
|-------|------|-------------|
| m_Workplace | Entity | Company/service building entity |
| m_LastCommuteTime | float | Time of last commute |
| m_Level | byte | Job level |
| m_Shift | Workshift | Day/evening/night shift |

### Age Tag Components

`Child`, `Teen`, `Adult`, `Elderly` -- empty tag structs in Game.Citizens. Added alongside the age bits in CitizenFlags to enable efficient ECS queries filtering by age group.

## Key Systems

### AgingSystem (Game.Simulation)

- **Update rate**: 1x per day (262144 / 16 frames per update)
- **Thresholds**: Child->Teen at day 21, Teen->Adult at day 36, Adult->Elderly at day 84
- **Transitions**:
  - Child->Teen: Removes Student, enables BicycleOwner
  - Teen->Adult: Removes Student, adds LeaveHouseholdTag (citizen will form own household)
  - Adult->Elderly: Removes Worker and work-related TravelPurpose

### BirthSystem (Game.Simulation)

- **Update rate**: 16x per day
- **Requirements**: Adult female (non-male, non-tourist, non-commuter), household has property
- **Rate modifiers**: Base rate + bonus if household has adult male, reduced if student
- **Creates**: New citizen entity as a child (m_BirthDay = 0) in the same household

### DeathCheckSystem (Game.Simulation)

- **Update rate**: 4x per day
- **Max age**: 9 game-years (kMaxAgeInGameYear)
- **Natural death**: Age-based probability curve (m_DeathRate), evaluated per citizen using pseudo-random
- **Sickness death**: Health-based probability for Sick/Injured citizens, with hospital treatment bonus
- **Recovery**: Possible recovery from sickness, affected by hospital TreatmentBonus and CityModifier
- **On death**: Sets Dead flag, removes Worker/Student/ResourceBuyer/Leisure, fires triggers

### CitizenHappinessSystem (Game.Simulation)

- **26 happiness factors**: Telecom, Crime, AirPollution, Apartment, Electricity, Healthcare, GroundPollution, NoisePollution, Water, WaterPollution, Sewage, Garbage, Entertainment, Education, Mail, Welfare, Leisure, Tax, Buildings, Consumption, TrafficPenalty, DeathPenalty, Homelessness, ElectricityFee, WaterFee, Unemployment
- **Output**: Updates `m_Health` and `m_WellBeing` on the Citizen component
- **Apartment quality**: Based on space per citizen * building level, using SpawnableBuildingData
- **Service bonuses**: Evaluated from ServiceCoverage buffers on the building's road edge

### HouseholdSpawnSystem (Game.Simulation)

- **Driven by**: ResidentialDemandSystem demand value
- **Spawn rate**: Inversely proportional to population (slower as city grows)
- **Prefab selection**: Weighted random from HouseholdPrefab pool, considering study positions
- **Placement**: At a random outside connection; household then seeks property

### HouseholdFindPropertySystem (Game.Simulation)

- **Two phases**: PreparePropertyJob caches property quality, FindPropertyJob matches households
- **Quality scoring**: Uses GenericApartmentQuality (size, education, welfare, services, pollution)
- **Considers**: Commute distance, household wealth, property availability
- **Handles**: Both normal households and homeless seeking shelter

### HouseholdMoveAwaySystem (Game.Simulation)

- **Triggered by**: MovingAway component on household
- **Exit routing**: Road OC if household has car, otherwise train/air/ship
- **Cleanup**: Removes workers, clears property rental, deletes household on arrival
- **Statistics**: Records CitizensMovedAway count and MovedAwayReason

## Data Flow

```
Outside Connection
       |
       v
HouseholdSpawnSystem (creates household entity based on demand)
       |
       v
HouseholdInitializeSystem (populates with citizen entities)
       |
       v
HouseholdFindPropertySystem (assigns residential property)
       |
       v
  [Living in City]
       |
       +---> BirthSystem (creates child citizens)
       |        |
       |        v
       +---> AgingSystem (Child -> Teen -> Adult -> Elderly)
       |        |              |           |
       |        |              |           +-> Worker removed
       |        |              +-> LeaveHouseholdTag
       |        |                     |
       |        |                     v
       |        |              LeaveHouseholdSystem
       |        |              (new household created)
       |        |
       +---> CitizenHappinessSystem (26 factors -> Health + WellBeing)
       |
       +---> CitizenFindJobSystem / FindJobSystem (adds Worker)
       |
       +---> StudentSystem (manages school attendance)
       |
       +---> DeathCheckSystem
       |        |
       |        v
       |     HealthProblemFlags.Dead -> Deathcare collection
       |
       +---> HouseholdMoveAwaySystem
                |
                v
          Outside Connection (household deleted)
```

## Modding Patch Points

### Adjusting Age Thresholds
- **Target**: `AgingSystem.GetTeenAgeLimitInDays()`, `GetAdultAgeLimitInDays()`, `GetElderAgeLimitInDays()`
- **Method**: Harmony prefix/postfix to return custom values
- **Effect**: Controls how long citizens spend in each life stage

### Modifying Birth Rate
- **Target**: `BirthSystem.CheckBirthJob` (the birth probability calculation)
- **Approach**: Harmony patch on `BirthSystem.OnUpdate()` or modify `CitizenParametersData` at runtime
- **Effect**: Increase/decrease natural population growth

### Custom Happiness Factors
- **Target**: `CitizenHappinessSystem.CitizenHappinessJob.Execute()`
- **Approach**: Harmony postfix to adjust m_Health / m_WellBeing after the vanilla calculation
- **Effect**: Add new factors or rebalance existing ones

### Preventing Death
- **Target**: `DeathCheckSystem.DeathCheckJob`
- **Approach**: Harmony prefix on OnUpdate to skip death checks, or modify m_DeathRate curve
- **Effect**: Immortal citizens or adjusted lifespan

### Controlling Immigration
- **Target**: `HouseholdSpawnSystem.SpawnHouseholdJob`
- **Approach**: Modify the demand value from ResidentialDemandSystem
- **Effect**: Control rate of new households moving in

### Housing Assignment
- **Target**: `HouseholdFindPropertySystem.FindPropertyJob`
- **Approach**: Modify property scoring in GenericApartmentQuality
- **Effect**: Change how households evaluate and choose properties

## Decompiled Snippets

| File | Contents |
|------|----------|
| `snippets/Citizen.cs` | Citizen component with all fields and helper methods |
| `snippets/Household.cs` | Household component and HouseholdFlags enum |
| `snippets/CitizenFlags.cs` | CitizenAge and CitizenFlags enums |
| `snippets/HouseholdMember.cs` | HouseholdMember, HouseholdCitizen, Worker components |
| `snippets/AgingSystem.cs` | Age transition system summary |
| `snippets/BirthSystem.cs` | Birth system summary |
| `snippets/DeathCheckSystem.cs` | Death and recovery system summary |
| `snippets/CitizenHappinessSystem.cs` | Happiness factor enumeration and system summary |
| `snippets/HouseholdFindPropertySystem.cs` | Property matching system summary |
| `snippets/HouseholdSpawnMoveAway.cs` | Immigration and emigration systems summary |

## Open Questions

- What exactly triggers `MovingAway` to be added to a household? Likely low happiness over time or inability to find property, but the triggering system was not fully traced.
- How does `LeaveHouseholdSystem` decide the initial resources for a newly independent adult's household?
- What is the exact shape of the `m_DeathRate` curve in `HealthcareParameterData`? It is a `BezierCurve` evaluated at normalized age.
- How do commuter and tourist households differ in lifecycle from resident households beyond the flag differences?
