// Decompiled from Game.dll -> Game.Simulation.TradeSystem
// Decompiled with ilspycmd on 2026-02-16

// Key constants and formulas:

// kRefreshRate = 0.01f (trade balance decays 1% per update)
// kUpdatesPerDay = 128

// CalculateTradeCost(resource, tradeBalance, type, weight, tradeParameters, cityEffects):
//   Buy cost:
//     cost = tradeParameters.GetWeightCostSingle(type) * weight
//     if tradeBalance < 0:
//       cost *= 1 + tradeParameters.GetDistanceCostSingle(type) * max(50, sqrt(-tradeBalance))
//     ApplyModifier(ImportCost)
//   Sell cost:
//     cost = tradeParameters.GetWeightCostSingle(type) * weight
//     if tradeBalance > 0:
//       cost *= 1 + tradeParameters.GetDistanceCostSingle(type) * max(50, sqrt(tradeBalance))
//     ApplyModifier(ExportCost)

// Storage rebalancing:
//   target = storageLimit / numResourceTypes / 2
//   delta = target - currentResources
//   urgency = |delta| / target
//   if urgency > 1: add full delta
//   else: add (delta * urgency / kUpdatesPerDay) * 8

// Cache index: log2(transferType) * 2 * ResourceCount + 2 * resourceIndex + (import ? 1 : 0)
// OutgoingMail resources are zeroed, not traded
// Garbage uses GarbageFacilityData.m_GarbageCapacity instead of storage limit
