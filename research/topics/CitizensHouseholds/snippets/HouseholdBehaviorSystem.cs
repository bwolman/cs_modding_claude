// Decompiled from Game.dll — Game.Simulation.HouseholdBehaviorSystem
// Daily household tick: consumption, shopping, move-away decisions, property seeking.

// Key constants:
// kUpdatesPerDay = 256
// kCarBuyingMinimumMoney = 10000
// kMinimumShoppingMoney = 1000
// kMaxShoppingPossibility = 80
// kMaxHouseholdNeedAmount = 2000
// kCarAmount = 50

// Update interval: 262144 / (256 * 16) = every 64 simulation frames (256x per day)

// Query: Household + HouseholdNeed + HouseholdCitizen + Resources + UpdateFrame,
//        Excludes: TouristHousehold, MovingAway, Deleted, Temp

// HouseholdTickJob.Execute() logic per household:
//
// 1. DAY ROLLOVER: If (frameIndex - household.m_LastDayFrameIndex > 262144):
//    - m_ShoppedValueLastDay = m_ShoppedValuePerDay
//    - m_ShoppedValuePerDay = 0
//    - m_MoneySpendOnBuildingLevelingLastDay = 0
//    - m_LastDayFrameIndex = frameIndex
//
// 2. EMPTY HOUSEHOLD CHECK: If citizens.Length == 0, mark as Deleted.
//
// 3. HAPPINESS & MOVE-AWAY DECISION:
//    - Compute average happiness across all citizens
//    - Check if all adults are absent (flag = true if only children/teens remain)
//    - Move-away reasons:
//      * NoAdults: no adult/elderly citizens in household
//      * NotHappy: happiness-based probability formula
//      * NoMoney: (totalWealth + salary) < -1000
//    - If any reason, call CitizenUtils.HouseholdMoveAway()
//
// 4. SALARY TRACKING: m_SalaryLastDay = EconomyUtils.GetHouseholdIncome(...)
//
// 5. RESOURCE CONSUMPTION: If m_Resources > 0:
//    - Calculate consumption based on wealth multiplier * consumption rate * citizen count
//    - Update m_ConsumptionPerDay and deduct from m_Resources
//
// 6. SHOPPING NEEDS: If resources depleted and no current need:
//    - Calculate spendable money
//    - Weight resources by household age composition
//    - Random selection of needed resource
//    - Car buying: if spendableMoney > 10000 and probability based on existing car count
//
// 7. PROPERTY SEEKING: Periodically enable PropertySeeker if home building is invalid
//    - Probability scales with population: clamp(0.06 * population, 64, 1024)
//    - Homeless households check 10x more frequently
//
// Move-away happiness formula:
// random.NextInt(1000) < -53.35 * happiness + sqrt(95.96 * happiness^2 + 1013 * happiness + 6576) * 5.408 - 298.5
