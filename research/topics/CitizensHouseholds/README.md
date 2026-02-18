# Research: Citizens & Households

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-17

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
| m_LastDayFrameIndex | uint | Simulation frame when the last day rollover occurred |
| m_MoneySpendOnBuildingLevelingLastDay | int | Money spent on building leveling last day (written by `UpkeepPaymentJob` inside `BuildingUpkeepSystem` after deducting upkeep from household money; reset each day by `HouseholdBehaviorSystem` during daily rollover) |

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

### `CitizenParametersData` (Game.Prefabs) -- Singleton

ECS singleton controlling citizen lifecycle rates. Initialized from `CitizenParametersPrefab`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_DivorceRate | float | 0.16 | Divorce probability (16%) |
| m_LookForPartnerRate | float | 0.08 | Probability single citizen looks for partner (8%) |
| m_LookForPartnerTypeRate | float2 | (0.04, 0.1) | x=Same gender rate, y=Any gender rate, remainder=Different gender |
| m_BaseBirthRate | float | 0.02 | Base birth rate (2%) per eligible female per update |
| m_AdultFemaleBirthRateBonus | float | 0.08 | Additional birth rate when household has adult male (+8%) |
| m_StudentBirthRateAdjust | float | 0.5 | Multiplier for student mothers (50% reduction) |
| m_SwitchJobRate | float | 0.032 | Probability employed citizen checks for better job (3.2%) |
| m_LookForNewJobEmployableRate | float | 2.0 | Free positions / employable workers threshold for job search |

### `CitizenHappinessParameterData` (Game.Prefabs) -- Singleton

ECS singleton with thresholds and weights for all 26 happiness factors. Initialized from `CitizenHappinessPrefab`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| m_PollutionBonusDivisor | int | 600 | Divisor for pollution bonus calculation |
| m_MaxAirAndGroundPollutionBonus | int | 50 | Max wellbeing penalty from air/ground pollution |
| m_MaxNoisePollutionBonus | int | 15 | Max wellbeing penalty from noise pollution |
| m_ElectricityWellbeingPenalty | float | 20.0 | Wellbeing penalty for no electricity |
| m_ElectricityPenaltyDelay | float | 32 | Delay in ticks before full penalty (~1.42 min/tick) |
| m_ElectricityFeeWellbeingEffect | AnimationCurve1 | curve | Maps electricity fee (0-200%) to wellbeing effect |
| m_WaterHealthPenalty | int | 20 | Health penalty for no water |
| m_WaterWellbeingPenalty | int | 20 | Wellbeing penalty for no water |
| m_WaterPenaltyDelay | float | 32 | Delay in ticks before full penalty |
| m_WaterPollutionBonusMultiplier | float | -10.0 | Water pollution health impact multiplier |
| m_SewageHealthEffect | int | 10 | Health penalty for no sewage treatment |
| m_SewageWellbeingEffect | int | 20 | Wellbeing penalty for no sewage treatment |
| m_SewagePenaltyDelay | float | 32 | Delay in ticks before full penalty |
| m_WaterFeeHealthEffect | AnimationCurve1 | curve | Maps water fee to health effect |
| m_WaterFeeWellbeingEffect | AnimationCurve1 | curve | Maps water fee to wellbeing effect |
| m_WealthyMoneyAmount | int4 | (0,1000,3000,5000) | Wealth tier thresholds: Wretched/Poor/Modest/Comfortable |
| m_HealthCareHealthMultiplier | float | 2.0 | Healthcare coverage health bonus multiplier |
| m_HealthCareWellbeingMultiplier | float | 0.8 | Healthcare coverage wellbeing bonus multiplier |
| m_EducationWellbeingMultiplier | float | 3.0 | Education coverage wellbeing multiplier |
| m_NeutralEducation | float | 5.0 | Education coverage level considered neutral |
| m_EntertainmentWellbeingMultiplier | float | 20.0 | Entertainment/park coverage wellbeing multiplier |
| m_NegligibleCrime | int | 5000 | Crime level below which there is no penalty |
| m_CrimeMultiplier | float | 0.0004 | Crime-to-penalty conversion factor |
| m_MaxCrimePenalty | int | 30 | Maximum wellbeing penalty from crime |
| m_MailMultiplier | float | 2.0 | Mail service coverage multiplier |
| m_NegligibleMail | int | 25 | Mail coverage threshold below which no effect |
| m_TelecomBaseline | float | 0.3 | Telecom coverage baseline (below = penalty, above = bonus) |
| m_TelecomBonusMultiplier | float | 10.0 | Telecom coverage bonus multiplier |
| m_TelecomPenaltyMultiplier | float | 20.0 | Telecom coverage penalty multiplier |
| m_WelfareMultiplier | float | 2.0 | Welfare coverage multiplier |
| m_HealthProblemHealthPenalty | int | 20 | Health penalty for having a health problem |
| m_DeathWellbeingPenalty | int | 20 | Wellbeing penalty for family member death |
| m_DeathHealthPenalty | int | 10 | Health penalty for family member death |
| m_ConsumptionMultiplier | float | 1.0 | Shopping satisfaction multiplier |
| m_LowWellbeing | int | 40 | Threshold for "low wellbeing" statistic |
| m_LowHealth | int | 40 | Threshold for "low health" statistic |
| m_TaxUneducatedMultiplier | float | -0.25 | Tax impact on uneducated citizens |
| m_TaxPoorlyEducatedMultiplier | float | -0.5 | Tax impact on poorly educated citizens |
| m_TaxEducatedMultiplier | float | -1.0 | Tax impact on educated citizens |
| m_TaxWellEducatedMultiplier | float | -1.5 | Tax impact on well-educated citizens |
| m_TaxHighlyEducatedMultiplier | float | -2.0 | Tax impact on highly educated citizens |
| m_PenaltyEffect | int | -30 | Temporary penalty for teleporting (traffic problems) |
| m_HomelessHealthEffect | int | -20 | Health penalty for homelessness |
| m_HomelessWellbeingEffect | int | -20 | Wellbeing penalty for homelessness |
| m_UnemployedWellbeingPenaltyAccumulatePerDay | float | 0.0 | Wellbeing penalty per day of unemployment |
| m_MaxAccumulatedUnemployedWellbeingPenalty | int | 20 | Max cumulative unemployment penalty |

### GroupAmbienceData — Residential Building Classification

`GroupAmbienceData` (Game.Prefabs) is a prefab component on residential buildings that classifies their housing type:

| Field | Type | Description |
|-------|------|-------------|
| `m_AmbienceType` | `GroupAmbienceType` | Building classification enum |

**Known GroupAmbienceType Values**:
- `GroupAmbienceType.ResidentialLowRent` — subsidized/affordable housing (low-rent rowhouses, etc.)
- Other values include standard residential types at various densities

**Usage Pattern**: This component is present on building prefabs alongside `SpawnableBuildingData` and `BuildingPropertyData`. It can be used to differentiate between affordable/subsidized housing and market-rate housing:

```csharp
if (groupAmbienceData.m_AmbienceType != GroupAmbienceType.ResidentialLowRent)
{
    // Market-rate housing — apply luxury apartment adjustments at higher levels
    if (spawnBuildingData.m_Level == 4) apartmentArea *= (1 + level4Increase);
    if (spawnBuildingData.m_Level == 5) apartmentArea *= (1 + level5Increase);
}
```

Source: RealisticWorkplacesAndHouseholds mod

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

- **Driven by**: `ResidentialDemandSystem` demand value, accessible via `ResidentialDemandSystem.householdDemand` getter. Demand factor breakdown available through `GetLowDensityDemandFactors()`, `GetMediumDensityDemandFactors()`, and `GetHighDensityDemandFactors()` returning `NativeArray<int>` arrays.
- **Spawn rate**: Inversely proportional to population (slower as city grows). Vacancy-driven: spawn rate scales with the number of free residential properties -- more vacancies means faster spawning.
- **Prefab selection**: Weighted random from HouseholdPrefab pool via `SpawnHouseholdJob`. Each `HouseholdPrefab` has a `HouseholdData.m_Weight` field used for weighted selection. The job iterates candidate prefabs, multiplies each weight by a random factor, and selects the highest weighted result. Study position availability is also considered during selection.
- **Placement**: At a random `OutsideConnection` entity; household then seeks property via `HouseholdFindPropertySystem`.
- **Replacement pattern**: To override household spawning, replace `HouseholdSpawnSystem` with a custom system. Read `ResidentialDemandSystem.householdDemand` to gate spawning, then use the same `SpawnHouseholdJob` internals: demand factor index 6 represents the free residential properties factor, and index 12 represents the household demand factor. These indices into the demand factors array control the vacancy-driven spawn rate.
- **Key internals** (`SpawnHouseholdJob`):
  - Reads demand factors array where index 6 = free residential property count factor, index 12 = household demand factor
  - Selects prefab via weighted random: iterates `HouseholdPrefab` entities, computes `HouseholdData.m_Weight * random.NextFloat()`, picks highest
  - Spawns household entity at a randomly chosen `OutsideConnection` entity from the available pool

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

### HouseholdBehaviorSystem (Game.Simulation)

- **Update rate**: 256x per day (every 64 simulation frames)
- **Query**: `Household` + `HouseholdNeed` + `HouseholdCitizen` + `Resources` + `UpdateFrame`, excluding `TouristHousehold`, `MovingAway`, `Deleted`, `Temp`
- **Responsibilities**:
  - **Day rollover**: When `frameIndex - m_LastDayFrameIndex > 262144`, rolls `m_ShoppedValuePerDay` into `m_ShoppedValueLastDay`, resets daily counters
  - **Empty household cleanup**: Deletes households with 0 citizens
  - **Move-away decisions**: Computes average happiness, triggers move-away for:
    - `NoAdults`: No adult/elderly citizens remain (only children/teens)
    - `NotHappy`: Happiness-based probability formula using quadratic expression
    - `NoMoney`: Total wealth + salary < -1000
  - **Salary tracking**: Updates `m_SalaryLastDay` from `EconomyUtils.GetHouseholdIncome()`
  - **Resource consumption**: Deducts from `m_Resources` based on wealth multiplier * consumption rate * citizen count
  - **Shopping**: When resources depleted, selects a needed resource weighted by household age composition and wealth. Car buying requires >10000 money.
  - **Property seeking**: Periodically enables `PropertySeeker` if home is invalid; probability scales with population
- **Key constants**: `kCarBuyingMinimumMoney = 10000`, `kMinimumShoppingMoney = 1000`

### CitizenPresenceSystem (Game.Simulation)

- **Update rate**: Every 64 simulation frames
- **Query**: Building entities with `CitizenPresence` component, excluding `Deleted`, `Temp`
- **Purpose**: Tracks occupancy levels in buildings as a smooth byte value (0-255)
- **Responsibilities**:
  - Processes buildings where `CitizenPresence.m_Delta != 0` (citizen entered/left)
  - Calculates building capacity from `WorkProvider.m_MaxWorkers` + household citizen counts + vacant property slots
  - Scales the delta proportionally to capacity with randomization
  - Increments/decrements `m_Presence` byte and resets `m_Delta` to 0
- **Note**: This runs on **building entities**, not citizen entities. Other systems update `m_Delta` when citizens enter/leave buildings.

### CheckBuildingsSystem Eviction Pattern (Game.Simulation)

- **Trigger**: When `BuildingPropertyData.m_ResidentialProperties` changes at runtime (e.g., a mod like RealisticWorkplacesAndHouseholds modifies building capacity)
- **Query**: Entities with `[Building, ResidentialProperty, PrefabRef, Renter]`
- **Logic**:
  1. Compares the current renter count (length of the `Renter` buffer) to the building's `BuildingPropertyData.m_ResidentialProperties` value
  2. If the renter count exceeds the property count, excess households are evicted
  3. Eviction is performed by adding the `Evicted` component to the excess household entities
  4. A `RentersUpdated` event is fired on the building entity to notify other systems of the change
- **Grace period**: After a game load, there is a grace period before eviction checks are enforced. This prevents spurious evictions when mods modify `m_ResidentialProperties` during initialization (the system waits for property values to stabilize before comparing against renter counts).
- **Modding relevance**: Any mod that changes `BuildingPropertyData.m_ResidentialProperties` at runtime must account for this eviction pipeline. Reducing a building's residential capacity below its current renter count will trigger evictions after the grace period expires.

### LeaveHouseholdSystem (Game.Simulation)

- **Update rate**: 2x per day (every 8192 simulation frames)
- **Query**: `Citizen` + `LeaveHouseholdTag`, excluding `Deleted`, `Temp`
- **Purpose**: Processes young adults leaving their parents' household to form new independent households
- **Key constant**: `kNewHouseholdStartMoney = 2000`
- **Logic**:
  1. If parent household is already `MovingAway`, just remove the tag
  2. Eligibility requirements:
     - Household has citizens (buffer length > 0)
     - Household money > 4000 (2x start money)
     - Citizen has a `Worker` component (must be employed)
  3. Creates new household entity from random `HouseholdPrefab`
  4. Transfers 2000 money to new household, deducts from parent
  5. Moves citizen from old to new household's `HouseholdCitizen` buffer
  6. Updates citizen's `HouseholdMember` to point to new household
  7. If free residential properties > 10, enables `PropertySeeker` on new household
  8. Otherwise, if outside connections exist, citizen becomes a Commuter

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

## Examples

### Read a Citizen's Age, Education, and Happiness

```csharp
// In a system that queries Citizen entities
EntityQuery citizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>());

NativeArray<Entity> entities = citizenQuery.ToEntityArray(Allocator.Temp);
ComponentLookup<Citizen> citizenLookup = GetComponentLookup<Citizen>(true);

foreach (Entity entity in entities)
{
    Citizen citizen = citizenLookup[entity];
    CitizenAge age = citizen.GetAge();
    int educationLevel = citizen.GetEducationLevel(); // 0-4
    int happiness = citizen.Happiness; // (m_Health + m_WellBeing) / 2
    byte health = citizen.m_Health;
    byte wellBeing = citizen.m_WellBeing;

    Log.Info($"Citizen {entity}: Age={age}, Education={educationLevel}, " +
             $"Happiness={happiness}, Health={health}, WellBeing={wellBeing}");
}
entities.Dispose();
```

### Query All Households with Property Assignments

```csharp
// Query households that have a property (are housed)
EntityQuery housedQuery = GetEntityQuery(
    ComponentType.ReadOnly<Household>(),
    ComponentType.ReadOnly<PropertyRenter>(),
    ComponentType.ReadOnly<HouseholdCitizen>()
);

NativeArray<Entity> households = housedQuery.ToEntityArray(Allocator.Temp);
ComponentLookup<Household> householdLookup = GetComponentLookup<Household>(true);
ComponentLookup<PropertyRenter> renterLookup = GetComponentLookup<PropertyRenter>(true);
BufferLookup<HouseholdCitizen> citizenBufferLookup = GetBufferLookup<HouseholdCitizen>(true);

foreach (Entity hh in households)
{
    Household household = householdLookup[hh];
    PropertyRenter renter = renterLookup[hh];
    DynamicBuffer<HouseholdCitizen> members = citizenBufferLookup[hh];

    Log.Info($"Household {hh}: Property={renter.m_Property}, " +
             $"Members={members.Length}, Resources={household.m_Resources}, " +
             $"Flags={household.m_Flags}");
}
households.Dispose();
```

### Check a Household's Financial Status

```csharp
// Read household financial data
ComponentLookup<Household> householdLookup = GetComponentLookup<Household>(true);
BufferLookup<Resources> resourcesLookup = GetBufferLookup<Resources>(true);

Household household = householdLookup[householdEntity];
DynamicBuffer<Resources> resources = resourcesLookup[householdEntity];
int money = EconomyUtils.GetResources(Resource.Money, resources);
int totalWealth = EconomyUtils.GetHouseholdTotalWealth(household, resources);

Log.Info($"Resources={household.m_Resources}, Money={money}, " +
         $"TotalWealth={totalWealth}, Salary={household.m_SalaryLastDay}, " +
         $"Consumption={household.m_ConsumptionPerDay}, " +
         $"ShoppedLastDay={household.m_ShoppedValueLastDay}");
```

### Find All Unemployed Adults

```csharp
// Adults without a Worker component are unemployed
EntityQuery unemployedQuery = GetEntityQuery(
    ComponentType.ReadOnly<Citizen>(),
    ComponentType.ReadOnly<Adult>(),           // Age tag component
    ComponentType.ReadOnly<HouseholdMember>(),
    ComponentType.Exclude<Worker>(),           // No job
    ComponentType.Exclude<HealthProblem>(),    // Not sick/dead
    ComponentType.Exclude<Deleted>()
);

NativeArray<Entity> unemployed = unemployedQuery.ToEntityArray(Allocator.Temp);
ComponentLookup<Citizen> citizenLookup = GetComponentLookup<Citizen>(true);

foreach (Entity entity in unemployed)
{
    Citizen citizen = citizenLookup[entity];
    Log.Info($"Unemployed: {entity}, Days={citizen.m_UnemploymentCounter}, " +
             $"Education={citizen.GetEducationLevel()}");
}
Log.Info($"Total unemployed adults: {unemployed.Length}");
unemployed.Dispose();
```

### Trigger a Household to Move Away

```csharp
// Force a household to move away using the same utility the game uses
// This adds the MovingAway component with a reason
EntityCommandBuffer commandBuffer = m_EndFrameBarrier.CreateCommandBuffer();

CitizenUtils.HouseholdMoveAway(
    commandBuffer.AsParallelWriter(),
    0,                            // sort key (unfilteredChunkIndex)
    householdEntity,
    MoveAwayReason.NotHappy       // or NoMoney, NoAdults, etc.
);
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
| `snippets/CitizenParametersData.cs` | Citizen lifecycle parameters singleton with defaults |
| `snippets/CitizenHappinessParameterData.cs` | Happiness factor thresholds and weights with defaults |
| `snippets/HouseholdBehaviorSystem.cs` | Household daily tick: consumption, shopping, move-away |
| `snippets/CitizenPresenceSystem.cs` | Building occupancy tracking system |
| `snippets/LeaveHouseholdSystem.cs` | Young adult household independence system |

## Mod Blueprint: Building Occupancy/Capacity Rebalancing

A building occupancy mod recalculates household counts, worker counts, and service building capacities based on building mesh dimensions and custom formulas. This is one of the most popular CS2 mod archetypes, involving a complex set of interacting systems that modify how many people can live and work in each building.

**Reference implementation**: [RealisticWorkplacesAndHouseholds](https://github.com/ruzbeh0/RealisticWorkplacesAndHouseholds/)

### Systems to Create

1. **Residential capacity updater** -- recalculates household count from building mesh dimensions (SubArea geometry, floor area)
2. **Workplace capacity updater** -- recalculates worker count for zoned companies based on building size
3. **City service capacity updater** -- recalculates workers/students/patients for service buildings (schools, hospitals, prisons, power plants)
4. **Building check system** -- handles evictions when capacity decreases (coordinates with `CheckBuildingsSystem` eviction pipeline)
5. **Vacancy monitor** -- tracks and controls residential vacancy rates to prevent population collapse
6. **Parameter updater** -- adjusts `EconomyParameterData` for rent/upkeep rebalancing to match new capacities
7. **Footprint calculator** (optional) -- computes usable area from `SubArea` geometry for more realistic density

### Components to Create

- **Marker components** for "already processed" entities -- prevents re-processing buildings that have already had their capacity updated
- **Data components** storing original vs. modified values -- enables clean save/load and mod removal without corrupting save data

### Systems to Disable

- `BuildingUpkeepSystem` -- if modifying upkeep calculations to match new capacities
- `BudgetApplySystem` -- if modifying city budget expenses to account for changed service building costs
- `HouseholdSpawnSystem` -- if controlling spawn rates to match new vacancy targets

### Key Game Components

| Component | Namespace | Role |
|-----------|-----------|------|
| `BuildingPropertyData` | Game.Prefabs | `m_ResidentialProperties` -- the primary field to modify for household count |
| `SpawnableBuildingData` | Game.Prefabs | `m_Level` -- building level, affects capacity scaling |
| `ZoneData` | Game.Prefabs | Area type classification for zone-specific formulas |
| `WorkProvider` | Game.Companies | `m_MaxWorkers` -- workplace capacity to modify |
| `WorkplaceData` | Game.Prefabs | Base worker count and complexity |
| `SubMesh` | Game.Prefabs | Building mesh data for dimension calculations |
| `MeshData` | Game.Prefabs | Mesh geometry dimensions |
| `GroupAmbienceData` | Game.Prefabs | Housing type classification (low-rent vs. market-rate) |
| `AssetPackData` / `AssetPackElement` | Game.Prefabs | Asset pack membership for batch processing |
| `ServiceCompanyData` | Game.Prefabs | Service company worker requirements |
| `IndustrialProcessData` | Game.Prefabs | Industrial building processing data |
| `SchoolData` | Game.Prefabs | Student capacity for education buildings |
| `HospitalData` | Game.Prefabs | Patient capacity for healthcare buildings |
| `PrisonData` | Game.Prefabs | Prisoner capacity for prison buildings |
| `PowerPlantData` | Game.Prefabs | Worker requirements for power plants |
| `ConsumptionData` | Game.Prefabs | Upkeep cost data -- must rebalance when capacity changes |
| `PollutionParameterData` | Game.Prefabs | Pollution parameters affected by density changes |
| `EconomyParameterData` | Game.Prefabs | Economy parameters for rent/upkeep rebalancing |
| `DemandParameterData` | Game.Prefabs | Demand parameters for immigration rate tuning |

### Harmony Patches Needed

- **`CityServiceBudgetSystem.GetExpense()`** -- postfix to reduce service building expenses to match rebalanced worker counts
- **`CityServiceBudgetSystem.GetTotalExpenses()`** -- postfix to ensure budget totals reflect modified expense values

### Implementation Notes

- Modifying `BuildingPropertyData.m_ResidentialProperties` at runtime triggers the `CheckBuildingsSystem` eviction pipeline after a grace period (see Key Systems above)
- Use `GroupAmbienceData.m_AmbienceType` to differentiate low-rent housing from market-rate for level-dependent capacity adjustments
- The exponential level scaling in `BuildingUpkeepSystem` (`pow(2, level)`) means capacity changes at higher levels have amplified effects on building condition

## Open Questions

- **Resolved**: `MovingAway` is triggered by `HouseholdBehaviorSystem` for three reasons: `NoAdults` (only children/teens left), `NotHappy` (happiness-based probability using quadratic formula), or `NoMoney` (totalWealth + salary < -1000). It calls `CitizenUtils.HouseholdMoveAway()`.
- **Resolved**: `LeaveHouseholdSystem` gives new households exactly `kNewHouseholdStartMoney = 2000` money, deducted from the parent household. The citizen must be employed (have `Worker`) and the parent must have >4000 money.
- What is the exact shape of the `m_DeathRate` curve in `HealthcareParameterData`? It is a `BezierCurve` evaluated at normalized age. The control points were not extracted.
- How do commuter and tourist households differ in lifecycle from resident households? Tourist households are excluded from `HouseholdBehaviorSystem`'s main query. `LeaveHouseholdSystem` can create commuter households when no residential property is available.
- What are the exact `AnimationCurve1` control points for `m_ElectricityFeeWellbeingEffect`, `m_WaterFeeHealthEffect`, and `m_WaterFeeWellbeingEffect` in `CitizenHappinessParameterData`?
