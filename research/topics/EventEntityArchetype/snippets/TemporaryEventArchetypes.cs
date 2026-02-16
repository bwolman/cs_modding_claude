// Extracted from Game.dll — Temporary command/notification entity archetypes
// These use Game.Common.Event (NOT Game.Events.Event).
// They are short-lived entities consumed within 1-2 frames.

namespace Game.Simulation;

// --- ObjectCollisionSystem, AccidentVehicleSystem, AccidentSiteSystem ---
// Impact: signals a collision event, consumed by ImpactSystem
m_EventImpactArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<Impact>()
);

// --- AccidentVehicleSystem ---
// AddAccidentSite: requests creation of an accident site on a road edge
m_AddAccidentSiteArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<AddAccidentSite>()
);

// Ignite: requests fire ignition on a vehicle (chain-reaction)
m_EventIgniteArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<Ignite>()
);

// --- AccidentCreatureSystem ---
// AddHealthProblem: applies injury/death to creatures involved in accident
m_AddProblemArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<Game.Common.Event>(),
    ComponentType.ReadWrite<AddHealthProblem>()
);

// --- AccidentSiteSystem ---
// PoliceEmergencyRequest: dispatches police to accident scene
m_PoliceRequestArchetype = EntityManager.CreateArchetype(
    ComponentType.ReadWrite<ServiceRequest>(),
    ComponentType.ReadWrite<PoliceEmergencyRequest>(),
    ComponentType.ReadWrite<RequestGroup>()
);
