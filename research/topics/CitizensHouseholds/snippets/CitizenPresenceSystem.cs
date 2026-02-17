// Decompiled from Game.dll — Game.Simulation.CitizenPresenceSystem
// Tracks citizen presence in buildings (residential + commercial).

// Update interval: 64 simulation frames

// Query: CitizenPresence component on building entities (excludes Deleted, Temp)

// CitizenPresenceJob.Execute() logic:
//
// For each building with CitizenPresence:
//   1. Skip if m_Delta == 0 (no change)
//   2. Calculate building capacity:
//      - Base capacity from WorkProvider.m_MaxWorkers (if present)
//      - Plus HouseholdCitizen buffer lengths for each renter household
//      - Plus 2 * unoccupied property slots
//   3. Apply delta to presence:
//      - Scale = (|delta| << 20) / capacity
//      - Randomize: random.NextInt(scale/2, scale*3/2) rounded
//      - If delta > 0: presence = min(255, presence + scaled)
//      - If delta < 0: presence = max(0, presence - scaled)
//   4. Reset m_Delta to 0
//
// The CitizenPresence component has two fields:
//   m_Presence: byte (0-255, current occupancy level)
//   m_Delta: sbyte (signed change since last update)
//
// This system runs on BUILDING entities, not citizen entities.
// It aggregates citizen movement into a smooth occupancy metric per building.
