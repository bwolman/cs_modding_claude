// Key logic from: Game.dll -> Game.Simulation.AccidentSiteSystem
// Runs every 64 frames. Manages AccidentSite state transitions on buildings.
//
// For each entity with AccidentSite component:
//
// 1. STAGING TIMEOUT: If 3600+ frames since creation, clears StageAccident flag
//
// 2. CLEAR MovingVehicles flag, re-evaluate from TargetElement buffer
//
// 3. ITERATE TargetElement buffer on m_Event:
//    - Count InvolvedInAccident entities (severity) and Criminal entities (not arrested)
//    - Track highest-severity non-moving target
//    - If Criminal has Monitored flag, set CrimeMonitored on AccidentSite
//
// 4. CRIME DETECTION (CrimeScene + !CrimeDetected):
//    - Read CrimeData.m_AlarmDelay from event prefab
//    - Apply CityModifier.CrimeResponseTime to alarm delay
//    - If CrimeMonitored OR elapsed time >= alarmDelay.max: set CrimeDetected
//    - If elapsed time >= alarmDelay.min: probabilistic detection roll
//    - When CrimeDetected: add crime scene notification icon
//
// 5. CRIME DURATION (CrimeScene + CrimeDetected + !CrimeFinished):
//    - Read CrimeData.m_CrimeDuration from event prefab
//    - If elapsed time >= crimeDuration.max: set CrimeFinished
//    - If elapsed time >= crimeDuration.min: probabilistic finish roll
//
// 6. POLICE REQUEST (clears RequirePolice first, then re-evaluates):
//    - If severity > 0 OR (unsecured CrimeScene): set RequirePolice
//    - If CrimeDetected: call RequestPoliceIfNeeded() to create PoliceEmergencyRequest
//
// 7. CLEANUP (no active targets + not staging):
//    - If secured CrimeScene with 1024+ frames since secured: remove AccidentSite
//    - If no InvolvedInAccident/Criminal entities and not staging: remove AccidentSite
//
// RequestPoliceIfNeeded:
//   Creates PoliceEmergencyRequest if m_PoliceRequest is stale/invalid
//   Purpose = CrimeMonitored ? Intelligence : Emergency
//   Priority = severity (or 1.0 for crime with CrimeDetected)

// Update interval constant:
private const uint UPDATE_INTERVAL = 64u;

// PoliceRequest archetype created in OnCreate:
// m_PoliceRequestArchetype = EntityManager.CreateArchetype(
//     ComponentType.ReadWrite<ServiceRequest>(),
//     ComponentType.ReadWrite<PoliceEmergencyRequest>(),
//     ComponentType.ReadWrite<RequestGroup>()
// );
