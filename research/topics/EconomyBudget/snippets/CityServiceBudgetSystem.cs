// Decompiled from Game.dll -> Game.Simulation.CityServiceBudgetSystem
// Decompiled with ilspycmd on 2026-02-16

// Key details:
// - Aggregates all income (14 categories) and expenses (15 categories)
// - Service budget slider: upkeep cost scaled by (serviceBudget / 100f)
// - Government subsidy comes from GameModeGovernmentSubsidiesSystem.monthlySubsidy
// - Imported service fees: fee * (population / OCServiceTradePopulationRange + 1) * OCServiceTradePopulationRange
// - CityModifierType.CityServiceImportCost modifies imported service fees
// - CityModifierType.CityServiceBuildingBaseUpkeepCost modifies building upkeep
// - Inactive buildings pay 10% upkeep (flag2 ? 0.1f multiplier)
// - Service efficiency: BuildingEfficiencyParameterData.m_ServiceBudgetEfficiencyFactor.Evaluate(budget/100f)
// - Default budget is 100 when no ServiceBudgetData entry exists
// - Map tile upkeep: CalculateOwnedTilesUpkeep(), zero if unlockMapTiles is enabled

// SetServiceBudget(Entity servicePrefab, int percentage) - sets budget slider
// GetServiceBudget(Entity servicePrefab) - reads budget slider
// GetServiceEfficiency(Entity servicePrefab, int budget) - returns efficiency percentage
// GetBalance() / GetTotalIncome() / GetTotalExpenses() - summary methods
// GetIncome(IncomeSource) / GetExpense(ExpenseSource) - per-category access
// GetMoneyDelta() - returns net change per hour (divides by 24)
