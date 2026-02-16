// Decompiled from Game.dll — Game.Simulation.HouseholdSpawnSystem, HouseholdMoveAwaySystem
// Household immigration and emigration systems.

using Game.Citizens;
using Unity.Burst;
using Unity.Entities;

namespace Game.Simulation;

// === HouseholdSpawnSystem ===
// Spawns new households at outside connections based on residential demand.
//
// Key logic from SpawnHouseholdJob.Execute():
// - Spawn rate scales with population: 300 / clamp(factor * log(1 + 0.001*pop), 0.5, 20)
// - Random check against m_Demand to determine how many to spawn
// - Selects household prefab weighted by HouseholdData.m_Weight
// - Considers study positions: may prefer households with children or students
// - Creates entity with prefab, sets CurrentBuilding to a random outside connection
// - Spawned households then go through HouseholdFindPropertySystem to find a home

// === HouseholdMoveAwaySystem ===
// Processes households that have been tagged with MovingAway.
//
// Key logic from MoveAwayJob.Execute():
// - Selects an outside connection as exit target (road if has car, otherwise train/air/ship)
// - Removes Worker from all household citizens
// - If household was in a temporary shelter (HomelessHousehold), fires RentersUpdated
// - Tracks whether all citizens have reached the outside connection (MovingAwayReachOC flag)
// - Once all citizens arrive at OC (or all dead), deletes the household entity
// - Records StatisticType.CitizensMovedAway and MovedAwayReason statistics
// - Fires TriggerType.CitizenMovedOutOfCity for each citizen
