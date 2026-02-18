// Decompiled from Game.dll — Game.Simulation.TelecomEfficiencySystem
// Update interval: 32 frames
// 512 updates per day (staggered via UpdateFrame)

// Queries buildings with TelecomConsumer component
// Requires TelecomParameterData and BuildingEfficiencyParameterData singletons
// Checks if telecom service is NOT locked before running

// GetTelecomEfficiency(position, telecomNeed):
//   networkQuality = TelecomCoverage.SampleNetworkQuality(coverage, position)
//   if quality < m_TelecomBaseline:
//     deficit = 1 - quality/baseline
//     penalty = deficit^2 * -0.01 * telecomNeed
//   return 1 + penalty
//
// So efficiency = 1.0 when quality >= baseline
// Max penalty when quality = 0: efficiency = 1 - 0.01 * telecomNeed
// For telecomNeed = 100: max penalty = 0% efficiency (complete shutdown)
// Penalty is quadratic — small deficits matter less
