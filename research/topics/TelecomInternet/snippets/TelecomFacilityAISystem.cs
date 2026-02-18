// Decompiled from Game.dll — Game.Simulation.TelecomFacilityAISystem
// Update interval: 256 frames, offset 208

// TelecomFacilityTickJob:
// - Sets TelecomFacilityFlags.HasCoverage on all facilities
// - Updates PointOfInterest for facilities with efficiency > 0
//   (randomizes position far from facility — likely for satellite dish pointing animation)
//   Position: random angle * 100000, random height 100000-1000000, cos(angle) * 100000
//   1/10 chance to update each tick
