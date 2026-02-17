# Research: Deathcare System

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 handles citizen death, hearse dispatch, corpse processing at cemeteries and crematoriums, grave management, and the death/recovery probability system.

**Why**: To enable mods that adjust death rates, modify processing speeds, control grave/storage capacity, and customize hearse dispatch behavior.

**Boundaries**: The sickness/injury origin (SicknessCheckSystem, AddHealthProblemSystem) is covered in the Healthcare research. This topic covers everything from the moment of death through corpse processing.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | DeathCheckSystem, DeathcareFacilityAISystem, HearseAISystem, HealthcarePathfindSetup |
| Game.dll | Game.Buildings | DeathcareFacility, DeathcareFacilityFlags, Patient (shared with hospitals) |
| Game.dll | Game.Vehicles | Hearse, HearseFlags |
| Game.dll | Game.Citizens | HealthProblem, HealthProblemFlags (Dead flag) |
| Game.dll | Game.Prefabs | DeathcareFacilityData, HearseData, HealthcareParameterData |

## Component Map

### `DeathcareFacility` (Game.Buildings)

Runtime component on cemetery/crematorium building entities.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetRequest | Entity | Current reverse service request (facility seeking corpses) |
| m_Flags | DeathcareFacilityFlags | Capability flags updated each tick |
| m_ProcessingState | float | Accumulated processing progress (corpses processed when >= 1.0) |
| m_LongTermStoredCount | int | Number of bodies in long-term storage (cemetery graves) |

*Source: `Game.dll` -> `Game.Buildings.DeathcareFacility`*

### `DeathcareFacilityFlags` (Game.Buildings)

| Flag | Value | Meaning |
|------|-------|---------|
| HasAvailableHearses | 1 | Has idle hearses |
| HasRoomForBodies | 2 | Current stored count < storage capacity |
| CanProcessCorpses | 4 | Has processing rate > 0 (crematorium) |
| CanStoreCorpses | 8 | Has storage capacity > 0 (cemetery) |
| IsFull | 16 | Storage at capacity (triggers notification) |

### `Hearse` (Game.Vehicles)

Component on hearse vehicle entities.

| Field | Type | Description |
|-------|------|-------------|
| m_State | HearseFlags | Current operational state |
| m_TargetCorpse | Entity | Citizen entity (corpse) being picked up/transported |
| m_TargetRequest | Entity | Service request being fulfilled |
| m_PathElementTime | float | Path timing for travel |

### `HearseFlags` (Game.Vehicles)

| Flag | Value | Meaning |
|------|-------|---------|
| Returning | 1 | Heading back to facility |
| Dispatched | 2 | En route to pick up corpse |
| Transporting | 4 | Carrying corpse to facility |
| AtTarget | 8 | Arrived at corpse location |
| Disembarking | 16 | Corpse being unloaded |
| Disabled | 32 | Hearse disabled (over capacity) |

### `DeathcareFacilityData` (Game.Prefabs)

Prefab configuration for cemeteries/crematoriums. Supports `Combine()` for upgrades.

| Field | Type | Description |
|-------|------|-------------|
| m_HearseCapacity | int | Max hearses |
| m_StorageCapacity | int | Max bodies stored (graves for cemetery, processing queue for crematorium) |
| m_ProcessingRate | float | Cremation/processing speed per tick (0 = storage only) |
| m_LongTermStorage | bool | True for cemeteries (bodies stay), false for crematoriums (bodies consumed) |

### `HearseData` (Game.Prefabs)

Prefab data for hearse vehicles.

| Field | Type | Description |
|-------|------|-------------|
| m_CorpseCapacity | int | Number of corpses the hearse can carry |

### `HealthcareParameterData` (Game.Prefabs)

Global singleton (shared with healthcare). Relevant fields for deathcare:

| Field | Type | Description |
|-------|------|-------------|
| m_HearseNotificationPrefab | Entity | Notification icon for hearse wait |
| m_FacilityFullNotificationPrefab | Entity | Notification icon for full facility |
| m_TransportWarningTime | float | Time before showing transport warning |
| m_DeathRate | AnimationCurve1 | Age-based natural death rate curve |

## System Map

### `DeathCheckSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (per UpdateFrame)
- **Queries**: Citizens with Citizen component
- **Reads**: Citizen.m_Health, Citizen age, HealthProblem, Hospital.m_TreatmentBonus, CityModifier (RecoveryFailChange)
- **Writes**: HealthProblem flags (Dead, removes Sick/Injured on recovery), StatisticsEvent
- **Key methods**:
  - `Execute()` -- for each citizen: natural death check, then sick/injured death/recovery
  - **Natural death**: `citizen.GetPseudoRandom(Death).NextFloat() < DeathRate.Evaluate(normalizedAge)` where normalizedAge = ageInDays / daysPerYear / kMaxAgeInGameYear
  - **Sick/injured death**: `(10 - health/10)^2 + 8` compared to `random.NextInt(kUpdatesPerDay * 1000)` -- lower health = higher death chance
  - **Recovery**: `recoveryFail = Logistic(3, 1000, 6, (10-health/10)/10 - 0.35)`, subtract `10 * treatmentBonus`, apply RecoveryFailChange modifier. If `random.NextFloat(1000) >= recoveryFail`, citizen recovers (HealthProblem flags cleared)
  - `Die()` -- sets HealthProblemFlags.Dead | RequireTransport, removes Worker/Student/ResourceBuyer/Leisure components
  - `PerformAfterDeathActions()` -- static method recording statistics and triggers

### `DeathcareFacilityAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 256 frames (offset 32)
- **Queries**: Buildings with DeathcareFacility, ServiceDispatch, PrefabRef
- **Reads**: DeathcareFacilityData (prefab), OwnedVehicle, Patient buffer, Efficiency
- **Writes**: DeathcareFacility flags/state, vehicle spawning, Patient buffer
- **Key methods**:
  - `Tick()` -- main facility update:
    - Advances processing state: `processingState += efficiency * processingRate * 0.0009765625`
    - Processes long-term stored corpses (if processingState >= 1.0, decrement count)
    - Processes queued patients (Patient buffer): if processingState >= 1.0, delete corpse entity; else if longTermStorage, move to long-term count
    - Manages hearse capacity and vehicle enable/disable
    - Updates facility flags
    - Shows IsFull notification when cemetery reaches capacity
    - Creates reverse requests when hearses and room are available
  - `SpawnVehicle()` -- spawns hearse from parked vehicles or creates new one

### `HearseAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**: Vehicles with Hearse component
- **Reads**: Hearse, PathInformation, Target, Car, CurrentLane, DeathcareFacility
- **Writes**: Hearse.m_State, Target, PathOwner, Passenger
- **Key methods**:
  - `HearseTickJob` -- handles hearse state machine: Dispatched -> AtTarget -> Transporting -> Disembarking -> Returning

## Data Flow

```
DEATH EVENT
  DeathCheckSystem (per citizen, per UpdateFrame)
    Natural death: age-based curve from HealthcareParameterData.m_DeathRate
    Sick/injured death: (10 - health/10)^2 + 8 vs random(kUpdatesPerDay * 1000)
    Recovery check: Logistic function - (10 * treatmentBonus) - RecoveryFailChange modifier
    If death: set HealthProblemFlags.Dead | RequireTransport
              remove Worker, Student, ResourceBuyer, Leisure
          |
          v
CORPSE ROUTING (HealthProblemSystem -- see Healthcare research)
  Dead + RequireTransport -> create HealthcareRequest(Hearse)
  Timer-based notification for waiting corpses
          |
          v
DISPATCH (HealthcareDispatchSystem -- shared with Healthcare)
  Match Hearse request to nearest DeathcareFacility
  Add ServiceDispatch to facility buffer
          |
          v
FACILITY PROCESSING (DeathcareFacilityAISystem, every 256 frames)
  processingState += efficiency * processingRate * 0.0009765625
  For pending ServiceDispatches: spawn hearse
  For Patient buffer entries:
    If processingState >= 1.0: delete corpse (cremation complete)
    Else if longTermStorage: move to m_LongTermStoredCount (burial)
  Process long-term stored: if processingState >= 1.0, decrement count
  Update flags: hearses, room, processing capability
  IsFull notification for cemeteries at capacity
          |
          v
HEARSE LIFECYCLE (HearseAISystem)
  Dispatched -> navigate to corpse location
  AtTarget -> pick up corpse (Passenger)
  Transporting -> navigate to facility
  Disembarking -> corpse unloaded at facility
  Returning -> navigate back to home facility
          |
          v
CORPSE DISPOSAL
  Crematorium: processingState accumulates, corpse entity Deleted when >= 1.0
  Cemetery: corpse moved to m_LongTermStoredCount, entity Deleted
            Long-term stored gradually processed (consumed) over time
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Hearse capacity | DeathcareFacilityData.m_HearseCapacity | Game.Prefabs.DeathcareFacilityData |
| Storage capacity | DeathcareFacilityData.m_StorageCapacity | Game.Prefabs.DeathcareFacilityData |
| Processing rate | DeathcareFacilityData.m_ProcessingRate | Game.Prefabs.DeathcareFacilityData |
| Long-term storage | DeathcareFacilityData.m_LongTermStorage | Game.Prefabs.DeathcareFacilityData |
| Corpse capacity (vehicle) | HearseData.m_CorpseCapacity | Game.Prefabs.HearseData |
| Death rate curve | HealthcareParameterData.m_DeathRate | Game.Prefabs.HealthcareParameterData |
| Recovery modifier | CityModifier RecoveryFailChange | Game.City.CityModifier |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.DeathcareFacilityAISystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Modify facility processing, change corpse handling behavior
- **Risk level**: Low (managed method)
- **Side effects**: None known

### Candidate 2: `Game.Simulation.DeathCheckSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Modify death/recovery rates, prevent deaths, track death events
- **Risk level**: Low
- **Side effects**: Preventing all deaths would cause population explosion

## Mod Blueprint

- **Systems to create**: Custom system to modify DeathcareFacilityData prefabs, monitoring system for cemetery fill levels
- **Components to add**: Optional extended facility tracking (cremation stats, etc.)
- **Patches needed**: DeathcareFacilityAISystem.OnUpdate (postfix for monitoring), DeathCheckSystem.OnUpdate (prefix for death rate modification)
- **Settings**: Processing rate multiplier, storage capacity override, death rate modifier
- **UI changes**: Cemetery fill level overlay, deathcare statistics panel

## Examples

### Example 1: Check Deathcare Facility Status

```csharp
public partial class DeathcareMonitorSystem : GameSystemBase
{
    private EntityQuery _facilityQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _facilityQuery = GetEntityQuery(
            ComponentType.ReadOnly<DeathcareFacility>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _facilityQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            var facility = EntityManager.GetComponentData<DeathcareFacility>(entity);
            var prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
            var data = EntityManager.GetComponentData<DeathcareFacilityData>(prefabRef.m_Prefab);

            int totalStored = facility.m_LongTermStoredCount;
            if (EntityManager.HasBuffer<Patient>(entity))
                totalStored += EntityManager.GetBuffer<Patient>(entity).Length;

            Log.Info($"Deathcare {entity}: {totalStored}/{data.m_StorageCapacity}");
            Log.Info($"  Processing: {facility.m_ProcessingState:F3}, Rate: {data.m_ProcessingRate}");
            Log.Info($"  Long-term: {data.m_LongTermStorage}, Hearses: {(facility.m_Flags & DeathcareFacilityFlags.HasAvailableHearses) != 0}");
            Log.Info($"  Full: {(facility.m_Flags & DeathcareFacilityFlags.IsFull) != 0}");
        }
        entities.Dispose();
    }
}
```

### Example 2: Modify Deathcare Facility Prefabs

```csharp
public partial class DeathcareBoostSystem : GameSystemBase
{
    private EntityQuery _prefabQuery;
    private bool _applied;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabQuery = GetEntityQuery(
            ComponentType.ReadWrite<DeathcareFacilityData>(),
            ComponentType.ReadOnly<PrefabData>()
        );
    }

    protected override void OnUpdate()
    {
        if (_applied) return;
        _applied = true;

        var entities = _prefabQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            DeathcareFacilityData data = EntityManager.GetComponentData<DeathcareFacilityData>(entity);
            data.m_StorageCapacity *= 2;         // Double storage
            data.m_ProcessingRate *= 1.5f;        // 50% faster processing
            data.m_HearseCapacity += 2;           // 2 extra hearses
            EntityManager.SetComponentData(entity, data);
        }
        entities.Dispose();
    }
}
```

### Example 3: Track Hearse Fleet

```csharp
public partial class HearseTrackerSystem : GameSystemBase
{
    private EntityQuery _hearseQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _hearseQuery = GetEntityQuery(
            ComponentType.ReadOnly<Hearse>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        var hearses = _hearseQuery.ToComponentDataArray<Hearse>(Allocator.Temp);
        int dispatched = 0, transporting = 0, returning = 0, idle = 0;
        for (int i = 0; i < hearses.Length; i++)
        {
            HearseFlags s = hearses[i].m_State;
            if ((s & HearseFlags.Transporting) != 0) transporting++;
            else if ((s & HearseFlags.Dispatched) != 0) dispatched++;
            else if ((s & HearseFlags.Returning) != 0) returning++;
            else idle++;
        }
        Log.Info($"Hearses: {hearses.Length} - D:{dispatched} T:{transporting} R:{returning} I:{idle}");
        hearses.Dispose();
    }
}
```

### Example 4: Count Dead Citizens Awaiting Pickup

```csharp
public void CountDeadCitizens(EntityManager em)
{
    EntityQuery deadQuery = em.CreateEntityQuery(
        ComponentType.ReadOnly<HealthProblem>(),
        ComponentType.ReadOnly<Citizen>(),
        ComponentType.Exclude<Deleted>()
    );
    var problems = deadQuery.ToComponentDataArray<HealthProblem>(Allocator.Temp);
    int dead = 0, awaitingPickup = 0;
    for (int i = 0; i < problems.Length; i++)
    {
        if ((problems[i].m_Flags & HealthProblemFlags.Dead) != 0)
        {
            dead++;
            if ((problems[i].m_Flags & HealthProblemFlags.RequireTransport) != 0)
                awaitingPickup++;
        }
    }
    Log.Info($"Dead citizens: {dead}, awaiting hearse: {awaitingPickup}");
    problems.Dispose();
    deadQuery.Dispose();
}
```

### Example 5: Understanding the Recovery Formula

```csharp
// The DeathCheckSystem recovery formula explained:
// For a sick/injured citizen each UpdateFrame:
//
// 1. Death chance: (10 - health/10)^2 + 8 out of kUpdatesPerDay * 1000
//    At health=50: (10-5)^2 + 8 = 33 / (kUpdatesPerDay * 1000)
//    At health=10: (10-1)^2 + 8 = 89 / (kUpdatesPerDay * 1000)
//
// 2. Recovery chance (if no death):
//    recoveryFail = Logistic(3, 1000, 6, severity - 0.35)
//    where severity = (10 - health/10) / 10
//    recoveryFail -= 10 * hospital.m_TreatmentBonus
//    ApplyModifier(recoveryFail, RecoveryFailChange)
//    Recover if random.NextFloat(1000) >= recoveryFail
//
// Hospital treatment bonus directly reduces recovery failure chance.
// A treatmentBonus of 100 subtracts 1000 from recoveryFail, guaranteeing recovery.
```

## Open Questions

- [ ] The `kMaxAgeInGameYear` constant used in the natural death curve normalization -- its exact value affects how the AnimationCurve1 maps to citizen age
- [ ] Whether cemetery graves are ever naturally emptied (the processing logic runs on long-term stored count, suggesting gradual decomposition)
- [ ] How the hearse determines which deathcare facility to deliver to when multiple are available -- likely handled by pathfinding setup
- [ ] The exact value of `kUpdatesPerDay` which scales the death probability

## Sources

- Decompiled from: Game.dll -- Game.Simulation.DeathCheckSystem, Game.Simulation.DeathcareFacilityAISystem, Game.Simulation.HearseAISystem
- Runtime components: Game.Buildings.DeathcareFacility, Game.Vehicles.Hearse, Game.Citizens.HealthProblem
- Prefab types: Game.Prefabs.DeathcareFacilityData, Game.Prefabs.HearseData, Game.Prefabs.HealthcareParameterData
- Enums: Game.Buildings.DeathcareFacilityFlags, Game.Vehicles.HearseFlags
