// Decompiled from Game.dll — Game.Simulation.TelecomCoverageSystem
// Key system that computes the 128x128 cell map of signal strength and network load

// Update interval: 4096 frames
// Grid: 128x128 cells (CellMapSystem<TelecomCoverage>)

// TelecomCoverageJob.Execute():
// 1. AddDensity: counts HouseholdCitizen + Employee per cell -> CellDensityData.m_Density
// 2. CalculateSignalStrength: for each facility, compute signal per cell (with terrain obstruction)
// 3. AddNetworkCapacity: distribute facility capacity weighted by signal strength
// 4. CalculateTelecomCoverage: write final m_SignalStrength (byte) and m_NetworkLoad (byte)
// 5. CalculateTelecomQuality: weighted average quality -> TelecomStatus.m_Quality

// Signal strength formula:
//   strength = 1 - (distance/range)^2
// Terrain obstruction (if !m_PenetrateTerrain):
//   Uses slope comparison to block signal behind terrain ridges
//   obstructSlopes propagated outward from tower cell in spiral pattern

// Network load per cell:
//   m_NetworkLoad = (byte)clamp(127.5 / max(0.0001, networkCapacity), 0, 255)
//   where networkCapacity = facility_capacity * (signal_at_cell / accumulated_signal) / max(1, users_in_range)

// Quality per cell:
//   quality = min(1, signalStrength * 2 / (1 + 1/networkCapacity))
//   Weighted by population density per cell

// Efficiency factor: range *= sqrt(efficiency), capacity *= efficiency
// CityModifier: TelecomCapacity applied to m_NetworkCapacity
