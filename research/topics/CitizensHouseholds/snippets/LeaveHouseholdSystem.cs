// Decompiled from Game.dll — Game.Simulation.LeaveHouseholdSystem
// Processes citizens with LeaveHouseholdTag to create new independent households.

// Key constants:
// kUpdatesPerDay = 2
// kNewHouseholdStartMoney = 2000

// Update interval: 262144 / (2 * 16) = every 8192 simulation frames (2x per day)

// Query: Citizen + LeaveHouseholdTag (excludes Deleted, Temp)

// LeaveHouseholdJob.Execute() logic per tagged citizen:
//
// 1. If household is already MovingAway -> just remove LeaveHouseholdTag
//
// 2. Eligibility check (ALL must be true):
//    - Household has HouseholdCitizen buffer
//    - Buffer length > 0
//    - Household money > kNewHouseholdStartMoney * 2 (i.e., > 4000)
//    - Citizen has Worker component (must be employed)
//
// 3. If eligible, CREATE NEW HOUSEHOLD:
//    a. Pick random household prefab
//    b. Create new entity from archetype
//    c. Transfer money: old household keeps (resources - 2000), new gets 2000
//    d. Remove citizen from old household's HouseholdCitizen buffer
//    e. Add citizen to new household's buffer
//    f. Update citizen's HouseholdMember to point to new household
//
// 4. PROPERTY ASSIGNMENT for new household:
//    - If free residential properties > 10: enable PropertySeeker
//    - Else if outside connections exist: make citizen a Commuter
//      (sets CitizenFlags.Commuter, HouseholdFlags.Commuter, adds CommuterHousehold)
//
// 5. Remove LeaveHouseholdTag from citizen
