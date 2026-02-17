# Research: Healthcare System

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: The full healthcare pipeline in CS2 -- how citizens get sick or injured, how hospitals treat patients, how ambulances are dispatched, and how health recovery works.

**Why**: To enable mods that adjust hospital treatment rates, patient capacity, disease probability, ambulance behavior, and healthcare coverage feedback.

**Boundaries**: Ambulance vehicle AI is included but pathfinding internals are out of scope. Deathcare (hearses, cemeteries, crematoriums) is covered in its own research topic.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | HealthProblemSystem, SicknessCheckSystem, HospitalAISystem, HealthcareDispatchSystem, AmbulanceAISystem, MedicalAircraftAISystem, HealthcarePathfindSetup, HealthcareRequest |
| Game.dll | Game.Citizens | HealthProblem, HealthProblemFlags |
| Game.dll | Game.Buildings | Hospital, Patient, HospitalFlags |
| Game.dll | Game.Vehicles | Ambulance, AmbulanceFlags |
| Game.dll | Game.Events | HealthEvent, AddHealthProblemSystem |
| Game.dll | Game.Prefabs | HospitalData, HealthcareParameterData, HealthEventData, HealthcareVehicleSelectData, AmbulanceData, HealthEventType |

## Component Map

### `HealthProblem` (Game.Citizens)

Attached to any citizen with an active health issue (sickness, injury, or death).

| Field | Type | Description |
|-------|------|-------------|
| m_Event | Entity | The event entity that caused this problem |
| m_HealthcareRequest | Entity | Associated HealthcareRequest entity (ambulance/hearse request) |
| m_Flags | HealthProblemFlags | Bit flags describing the problem type and state |
| m_Timer | byte | Notification timer -- increments each update, triggers warning icon when reaching threshold |

*Source: `Game.dll` -> `Game.Citizens.HealthProblem`*

### `HealthProblemFlags` (Game.Citizens)

| Flag | Value | Meaning |
|------|-------|---------|
| None | 0 | No active problem |
| Sick | 1 | Citizen has a disease |
| Dead | 2 | Citizen is dead |
| Injured | 4 | Citizen is injured |
| RequireTransport | 8 | Needs ambulance/hearse pickup |
| InDanger | 16 | In a dangerous building (fire) |
| Trapped | 32 | Trapped in a collapsed/burning building |
| NoHealthcare | 64 | No healthcare available in the city |

### `Hospital` (Game.Buildings)

Runtime component on hospital building entities tracking operational state.

| Field | Type | Description |
|-------|------|-------------|
| m_TargetRequest | Entity | Current reversed service request (hospital seeking patients) |
| m_Flags | HospitalFlags | Capability flags computed each tick |
| m_TreatmentBonus | byte | Effective treatment bonus (0-255), scaled by efficiency |
| m_MinHealth | byte | Minimum health range from prefab |
| m_MaxHealth | byte | Maximum health range from prefab |

### `HospitalFlags` (Game.Buildings)

| Flag | Value | Meaning |
|------|-------|---------|
| HasAvailableAmbulances | 1 | Has idle ground ambulances |
| HasAvailableMedicalHelicopters | 2 | Has idle medical helicopters |
| CanCureDisease | 4 | Efficiency > 0 and prefab allows disease treatment |
| HasRoomForPatients | 16 | Current patients < capacity |
| CanProcessCorpses | 32 | Has patient capacity (can hold corpses for transfer) |
| CanCureInjury | 64 | Efficiency > 0 and prefab allows injury treatment |

### `Patient` (Game.Buildings)

Buffer element on hospital buildings listing admitted patients.

| Field | Type | Description |
|-------|------|-------------|
| m_Patient | Entity | The citizen entity admitted to this hospital |

### `Ambulance` (Game.Vehicles)

Component on ambulance vehicle entities tracking dispatch state.

| Field | Type | Description |
|-------|------|-------------|
| m_State | AmbulanceFlags | Current ambulance operational state |
| m_TargetPatient | Entity | Citizen entity being picked up or transported |
| m_TargetLocation | Entity | Building/location where the patient is |
| m_TargetRequest | Entity | The service request being fulfilled |
| m_PathElementTime | float | Path timing for travel |

### `AmbulanceFlags` (Game.Vehicles)

| Flag | Value | Meaning |
|------|-------|---------|
| Returning | 1 | Heading back to hospital |
| Dispatched | 2 | En route to pick up patient |
| Transporting | 4 | Carrying patient to hospital |
| AnyHospital | 8 | Can go to any hospital, not just home |
| FindHospital | 16 | Searching for a hospital with room |
| AtTarget | 32 | Arrived at patient location |
| Disembarking | 64 | Patient is exiting ambulance |
| Disabled | 128 | Ambulance disabled (over capacity) |
| Critical | 256 | Critical patient (emergency priority) |

### `HealthcareRequest` (Game.Simulation)

Service request entity for ambulance or hearse dispatch.

| Field | Type | Description |
|-------|------|-------------|
| m_Citizen | Entity | The citizen needing help (or hospital requesting patients via reverse) |
| m_Type | HealthcareRequestType | Ambulance (0) or Hearse (1) |

### `HealthEventData` (Game.Prefabs)

Prefab data defining health event probabilities.

| Field | Type | Description |
|-------|------|-------------|
| m_RandomTargetType | EventTargetType | Who this event targets (Citizen, Building, etc.) |
| m_HealthEventType | HealthEventType | Disease, Injury, or Death |
| m_OccurenceProbability | Bounds1 | Min/max probability range (lerped by health) |
| m_TransportProbability | Bounds1 | Probability of requiring transport |
| m_RequireTracking | bool | Whether event requires milestone tracking |

### `HospitalData` (Game.Prefabs)

Prefab configuration for hospital buildings.

| Field | Type | Description |
|-------|------|-------------|
| m_AmbulanceCapacity | int | Max ground ambulances |
| m_MedicalHelicopterCapacity | int | Max medical helicopters |
| m_PatientCapacity | int | Max admitted patients |
| m_TreatmentBonus | int | Base treatment bonus (scaled by efficiency at runtime) |
| m_HealthRange | int2 | (min, max) health range the hospital can treat |
| m_TreatDiseases | bool | Can this hospital treat diseases |
| m_TreatInjuries | bool | Can this hospital treat injuries |

### `HealthcareParameterData` (Game.Prefabs)

Global singleton with healthcare system parameters.

| Field | Type | Description |
|-------|------|-------------|
| m_HealthcareServicePrefab | Entity | Service prefab reference |
| m_AmbulanceNotificationPrefab | Entity | Notification icon for ambulance wait |
| m_HearseNotificationPrefab | Entity | Notification icon for hearse wait |
| m_FacilityFullNotificationPrefab | Entity | Notification icon for full facility |
| m_TransportWarningTime | float | Time before showing transport warning icon |
| m_NoResourceTreatmentPenalty | float | Treatment penalty when hospital lacks resources |
| m_BuildingDestoryDeathRate | float | Death rate when building is destroyed |
| m_DeathRate | AnimationCurve1 | Age-based death rate curve |

## System Map

### `SicknessCheckSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (per UpdateFrame)
- **Queries**: Citizens without existing HealthProblem
- **Reads**: Citizen.m_Health, HealthEventData, CityModifier (DiseaseProbability)
- **Writes**: Creates AddHealthProblem events via command buffer
- **Key methods**:
  - `TryAddHealthProblem()` -- probability formula: `saturate(pow(2, 10 - health * 0.1) * 0.001)`. Lower health = exponentially higher disease chance. CityModifier for DiseaseProbability is applied to disease events.
  - `CreateHealthEvent()` -- creates the health event entity with appropriate flags (Sick, Injured, Dead)

### `AddHealthProblemSystem` (Game.Events)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**: AddHealthProblem events
- **Reads**: AddHealthProblem queue, HealthEventData
- **Writes**: Adds HealthProblem component to target citizens
- **Key methods**:
  - `FindCitizensInBuildingJob` -- for building-targeted events, finds all citizens inside
  - `AddHealthProblemJob` -- applies the HealthProblem component with appropriate flags

### `HealthProblemSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation (per UpdateFrame)
- **Queries**: Citizens with HealthProblem component
- **Reads**: HealthProblem, CurrentBuilding, CurrentTransport, Hospital, DeathcareFacility, AmbulanceFlags, HearseFlags
- **Writes**: HealthProblem.m_Flags, TripNeeded, HealthcareRequest creation, notification icons
- **Key methods**:
  - `Execute()` -- main dispatch logic routing citizens based on health problem type
  - `RequestVehicleIfNeeded()` -- creates HealthcareRequest entities for ambulance/hearse dispatch
  - `GoToHospital()` -- adds TripNeeded with Purpose.Hospital
  - `GoToDeathcare()` -- adds TripNeeded with Purpose.Deathcare
  - `GoToSafety()` -- adds TripNeeded with Purpose.Safety (fire escape)

### `HealthcareDispatchSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**: HealthcareRequest entities, Hospital buildings
- **Reads**: HealthcareRequest, Hospital.m_Flags, district data
- **Writes**: ServiceDispatch buffer on hospitals, dispatches ambulances
- **Key methods**:
  - `HealthcareDispatchJob` -- matches healthcare requests to nearest hospitals with available ambulances, using spatial quad tree search within districts

### `HospitalAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update interval**: 256 frames (offset 16)
- **Queries**: Buildings with Hospital, ServiceDispatch, PrefabRef
- **Reads**: HospitalData (prefab), OwnedVehicle, Patient buffer, HealthProblem, Efficiency, CityModifier
- **Writes**: Hospital flags, Patient buffer, vehicle spawning, ServiceUsage
- **Key methods**:
  - `Tick()` -- main hospital update. Treatment bonus formula: `efficiency * resourcePenalty * prefabTreatmentBonus` (capped at 255). Hospital efficiency includes city modifier `HospitalEfficiency` (slot 26). Patients with Dead flag are deleted. Patients who can't be treated are ejected.

### `AmbulanceAISystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Queries**: Vehicles with Ambulance component
- **Reads**: Ambulance, PathInformation, Target, Car, CarCurrentLane
- **Writes**: Ambulance.m_State, Target, PathOwner, Passenger
- **Key methods**:
  - `AmbulanceTickJob` -- handles ambulance state machine: Dispatched -> AtTarget -> Transporting -> Returning

## Data Flow

```
SICKNESS/INJURY ORIGIN
  SicknessCheckSystem (per citizen, per UpdateFrame)
    probability = saturate(pow(2, 10 - health * 0.1) * 0.001)
    For each HealthEventData prefab (Disease/Injury/Death):
      Apply CityModifier (DiseaseProbability) for diseases
      Random check against probability
      If triggered: create health event entity
          |
          v
EVENT APPLICATION
  AddHealthProblemSystem
    Processes AddHealthProblem queue
    Adds HealthProblem component to citizen
    Sets flags: Sick | Injured | Dead
    Sets RequireTransport based on TransportProbability
          |
          v
HEALTH PROBLEM ROUTING (per UpdateFrame)
  HealthProblemSystem
    For each citizen with HealthProblem:
      If InDanger/Trapped: handle fire/collapse escape logic
      If RequireTransport:
        Dead -> route to deathcare or hospital with CanProcessCorpses
        Injured -> route to hospital with CanCureInjury
        Sick -> route to hospital with CanCureDisease
        Creates HealthcareRequest (Ambulance or Hearse type)
        Manages notification timer -> warning icon
      If not RequireTransport:
        Sick/Injured -> citizen walks to hospital (TripNeeded)
          |
          v
DISPATCH
  HealthcareDispatchSystem
    Matches HealthcareRequest to nearest hospital
    Uses district-based spatial search
    Adds ServiceDispatch to hospital's buffer
          |
          v
HOSPITAL PROCESSING (every 256 frames)
  HospitalAISystem
    treatmentBonus = efficiency * resourcePenalty * prefabBonus
    Spawns ambulances for dispatched requests
    Manages patient list (removes dead, ejects untreatable)
    Updates Hospital.m_Flags
    Creates reverse requests if ambulances available
          |
          v
AMBULANCE LIFECYCLE
  AmbulanceAISystem
    Dispatched -> navigate to patient location
    AtTarget -> pick up patient (add as Passenger)
    Transporting -> navigate to hospital
    Returning -> navigate back to home hospital
          |
          v
PATIENT ADMISSION
  Patient added to Hospital's Patient buffer
  Treatment bonus applied through hospital efficiency
  Citizens removed from Patient buffer when HealthProblem resolved
```

## Prefab & Configuration

| Value | Source | Location |
|-------|--------|----------|
| Disease probability | HealthEventData.m_OccurenceProbability | Game.Prefabs.HealthEventData |
| Transport probability | HealthEventData.m_TransportProbability | Game.Prefabs.HealthEventData |
| Ambulance capacity | HospitalData.m_AmbulanceCapacity | Game.Prefabs.HospitalData |
| Helicopter capacity | HospitalData.m_MedicalHelicopterCapacity | Game.Prefabs.HospitalData |
| Patient capacity | HospitalData.m_PatientCapacity | Game.Prefabs.HospitalData |
| Treatment bonus | HospitalData.m_TreatmentBonus | Game.Prefabs.HospitalData |
| Health range | HospitalData.m_HealthRange | Game.Prefabs.HospitalData |
| Transport warning time | HealthcareParameterData.m_TransportWarningTime | Game.Prefabs.HealthcareParameterData |
| No-resource penalty | HealthcareParameterData.m_NoResourceTreatmentPenalty | Game.Prefabs.HealthcareParameterData |
| Death rate curve | HealthcareParameterData.m_DeathRate | Game.Prefabs.HealthcareParameterData |
| Hospital efficiency | CityModifier HospitalEfficiency | Game.City.CityModifier |
| Disease probability modifier | CityModifier DiseaseProbability | Game.City.CityModifier |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.HospitalAISystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Modify hospital behavior, change vehicle dispatching, alter patient management
- **Risk level**: Low (managed method, standard Harmony target)
- **Side effects**: Must be careful not to break command buffer ordering

### Candidate 2: `Game.Simulation.HealthcareDispatchSystem.OnUpdate`

- **Signature**: `void OnUpdate()`
- **Patch type**: Prefix / Postfix
- **What it enables**: Alter dispatch priorities, change hospital selection logic
- **Risk level**: Low
- **Side effects**: May affect response times if dispatch logic changes significantly

### Candidate 3: `Game.Simulation.SicknessCheckSystem+SicknessCheckJob.TryAddHealthProblem`

- **Signature**: `void TryAddHealthProblem(int, ref Random, Entity, Citizen, Entity, DynamicBuffer<CityModifier>)`
- **Patch type**: Transpiler (Burst-compiled)
- **What it enables**: Modify disease probability formula, disable sickness for certain citizens
- **Risk level**: Medium (Burst-compiled job -- cannot use standard prefix/postfix)
- **Side effects**: Need to patch the system's OnUpdate or replace the system entirely

## Mod Blueprint

- **Systems to create**: Custom GameSystemBase for reading/modifying HealthcareParameterData singleton, custom system for tracking healthcare statistics
- **Components to add**: Optional custom component for extended hospital data
- **Patches needed**: HospitalAISystem.OnUpdate (postfix for monitoring), HealthcareDispatchSystem.OnUpdate (prefix for custom dispatch rules)
- **Settings**: Disease probability multiplier, treatment speed multiplier, ambulance capacity override, hospital efficiency bonus
- **UI changes**: Custom infoview overlay showing hospital utilization, patient counts, ambulance status

## Examples

### Example 1: Read a Citizen's Health Problem Status

```csharp
public void CheckCitizenHealthProblem(EntityManager em, Entity citizen)
{
    if (!em.HasComponent<HealthProblem>(citizen)) return;

    HealthProblem hp = em.GetComponentData<HealthProblem>(citizen);
    Log.Info($"Health problem for {citizen}:");
    Log.Info($"  Flags: {hp.m_Flags}");
    Log.Info($"  Is Sick: {(hp.m_Flags & HealthProblemFlags.Sick) != 0}");
    Log.Info($"  Is Injured: {(hp.m_Flags & HealthProblemFlags.Injured) != 0}");
    Log.Info($"  Is Dead: {(hp.m_Flags & HealthProblemFlags.Dead) != 0}");
    Log.Info($"  Needs Transport: {(hp.m_Flags & HealthProblemFlags.RequireTransport) != 0}");
    Log.Info($"  Notification Timer: {hp.m_Timer}");
}
```

### Example 2: Query Hospital Capacity and Status

```csharp
public partial class HospitalMonitorSystem : GameSystemBase
{
    private EntityQuery _hospitalQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _hospitalQuery = GetEntityQuery(
            ComponentType.ReadOnly<Game.Buildings.Hospital>(),
            ComponentType.ReadOnly<Patient>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        var entities = _hospitalQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            var hospital = EntityManager.GetComponentData<Game.Buildings.Hospital>(entity);
            var patients = EntityManager.GetBuffer<Patient>(entity);
            var prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
            var prefabData = EntityManager.GetComponentData<HospitalData>(prefabRef.m_Prefab);
            float usage = (float)patients.Length / math.max(1f, prefabData.m_PatientCapacity);
            Log.Info($"Hospital {entity}: {patients.Length}/{prefabData.m_PatientCapacity} ({usage:P0})");
            Log.Info($"  Treatment: {hospital.m_TreatmentBonus}, Disease: {(hospital.m_Flags & HospitalFlags.CanCureDisease) != 0}");
        }
        entities.Dispose();
    }
}
```

### Example 3: Modify Global Healthcare Parameters

```csharp
public void ModifyHealthcareParameters(EntityManager em)
{
    EntityQuery paramQuery = em.CreateEntityQuery(
        ComponentType.ReadWrite<HealthcareParameterData>()
    );
    if (paramQuery.CalculateEntityCount() == 0) return;

    Entity paramEntity = paramQuery.GetSingletonEntity();
    HealthcareParameterData data = em.GetComponentData<HealthcareParameterData>(paramEntity);
    data.m_NoResourceTreatmentPenalty = 0.1f;
    data.m_TransportWarningTime *= 2f;
    em.SetComponentData(paramEntity, data);
    paramQuery.Dispose();
}
```

### Example 4: Boost Hospital Treatment Bonus via Prefab

```csharp
public partial class HealthcareBoostSystem : GameSystemBase
{
    private EntityQuery _hospitalPrefabQuery;
    private bool _applied;

    protected override void OnCreate()
    {
        base.OnCreate();
        _hospitalPrefabQuery = GetEntityQuery(
            ComponentType.ReadWrite<HospitalData>(),
            ComponentType.ReadOnly<PrefabData>()
        );
    }

    protected override void OnUpdate()
    {
        if (_applied) return;
        _applied = true;
        var entities = _hospitalPrefabQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            HospitalData data = EntityManager.GetComponentData<HospitalData>(entity);
            data.m_TreatmentBonus *= 2;
            data.m_PatientCapacity = (int)(data.m_PatientCapacity * 1.5f);
            data.m_TreatDiseases = true;
            data.m_TreatInjuries = true;
            EntityManager.SetComponentData(entity, data);
        }
        entities.Dispose();
    }
}
```

### Example 5: Track Ambulance Fleet Status

```csharp
public partial class AmbulanceTrackerSystem : GameSystemBase
{
    private EntityQuery _ambulanceQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _ambulanceQuery = GetEntityQuery(
            ComponentType.ReadOnly<Game.Vehicles.Ambulance>(),
            ComponentType.Exclude<Deleted>()
        );
    }

    protected override void OnUpdate()
    {
        var ambulances = _ambulanceQuery.ToComponentDataArray<Game.Vehicles.Ambulance>(Allocator.Temp);
        int dispatched = 0, transporting = 0, returning = 0, idle = 0, disabled = 0;
        for (int i = 0; i < ambulances.Length; i++)
        {
            AmbulanceFlags state = ambulances[i].m_State;
            if ((state & AmbulanceFlags.Disabled) != 0) disabled++;
            else if ((state & AmbulanceFlags.Transporting) != 0) transporting++;
            else if ((state & AmbulanceFlags.Dispatched) != 0) dispatched++;
            else if ((state & AmbulanceFlags.Returning) != 0) returning++;
            else idle++;
        }
        Log.Info($"Ambulances: {ambulances.Length} total - D:{dispatched} T:{transporting} R:{returning} I:{idle} X:{disabled}");
        ambulances.Dispose();
    }
}
```

## Open Questions

- [ ] Exact health recovery rate for patients admitted to hospitals -- the treatment bonus is stored but the system that restores citizen health needs further tracing
- [ ] How `HospitalData.m_HealthRange` affects patient admission filtering
- [ ] Medical helicopter landing pad requirements (MedicalAircraftAISystem)
- [ ] Conditions that trigger the NoHealthcare flag on citizens

## Sources

- Decompiled from: Game.dll -- Game.Simulation.HospitalAISystem, Game.Simulation.HealthProblemSystem, Game.Simulation.SicknessCheckSystem, Game.Simulation.HealthcareDispatchSystem, Game.Simulation.AmbulanceAISystem
- Runtime components: Game.Citizens.HealthProblem, Game.Buildings.Hospital, Game.Buildings.Patient, Game.Vehicles.Ambulance
- Prefab types: Game.Prefabs.HospitalData, Game.Prefabs.HealthcareParameterData, Game.Prefabs.HealthEventData
- Enums: Game.Citizens.HealthProblemFlags, Game.Buildings.HospitalFlags, Game.Vehicles.AmbulanceFlags, Game.Simulation.HealthcareRequestType, Game.Prefabs.HealthEventType
