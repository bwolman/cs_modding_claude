# Research: Economy & Budget

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-16

## Scope

**What we're investigating**: How CS2 handles the city economy -- tax collection, service fees, city budget (income/expenses), loans, trade with outside connections, and how money flows through the simulation.

**Why**: To build mods that modify tax rates, adjust service fees, create custom income/expense sources, interact with the loan system, or alter trade behavior.

**Boundaries**: Not covering company-level profitability AI in depth (just the tax interface), demand systems, or detailed resource production chains. Those are separate topics.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | `BudgetSystem`, `BudgetApplySystem`, `CityServiceBudgetSystem`, `TaxSystem`, `LoanUpdateSystem`, `ServiceFeeSystem`, `TradeSystem`, `GameModeGovernmentSubsidiesSystem`, `Loan`, `Creditworthiness`, `CollectedCityServiceFeeData`, `TaxAreaType`, `TaxResultType` |
| Game.dll | Game.Tools | `LoanSystem`, `LoanAction`, `LoanInfo` |
| Game.dll | Game.City | `PlayerMoney`, `ServiceFee`, `TaxRates`, `TaxRate`, `IncomeSource`, `ExpenseSource`, `PlayerResource`, `ServiceFeeCollector`, `ServiceImportBudget` |
| Game.dll | Game.Agents | `TaxPayer` |
| Game.dll | Game.Economy | `EconomyUtils`, `Resources` (buffer), `Resource` (flags enum), `TradeCost` |
| Game.dll | Game.Prefabs | `EconomyPrefab`, `EconomyParameterData`, `TaxParameterPrefab`, `ServiceFeeParameterPrefab`, `OutsideTradeParameterPrefab`, `ServiceFeeParameterData` |
| Game.dll | Game.UI.InGame | `BudgetUISystem`, `LoanUISystem`, `TaxationUISystem`, `ServiceBudgetUISystem`, `EconomyPanel` |

## Component Map

### `PlayerMoney` (Game.City)

| Field | Type | Description |
|-------|------|-------------|
| m_Money | int | Current money balance (clamped to +/- 2 billion) |
| m_Unlimited | bool | If true, always reports 2 billion (sandbox mode) |

Central component on the City entity. `BudgetApplySystem` calls `Add()` every tick to apply the net budget. Max money is `kMaxMoney = 2,000,000,000`.

*Source: `Game.dll` -> `Game.City.PlayerMoney`*

### `TaxPayer` (Game.Agents)

| Field | Type | Description |
|-------|------|-------------|
| m_UntaxedIncome | int | Accumulated income since last tax payment |
| m_AverageTaxRate | int | Tax rate percentage (e.g. 10 = 10%) |
| m_AverageTaxPaid | int | Last tax payment * kUpdatesPerDay |

Attached to households and companies. `TaxSystem` runs 32 times/day and collects: `tax = round(0.01 * m_AverageTaxRate * m_UntaxedIncome)`. After payment, `m_UntaxedIncome` resets to 0.

*Source: `Game.dll` -> `Game.Agents.TaxPayer`*

### `ServiceFee` (Game.City)

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | PlayerResource | Which service (Electricity, Healthcare, etc.) |
| m_Fee | float | Fee amount per unit of service consumed |

Buffer element on the City entity. Default fees: Education 100-300, Healthcare 100, Garbage 0.1, Electricity 0.2, Water 0.1.

*Source: `Game.dll` -> `Game.City.ServiceFee`*

### `Loan` (Game.Simulation)

| Field | Type | Description |
|-------|------|-------------|
| m_Amount | int | Current loan principal |
| m_LastModified | uint | Simulation frame when loan was last changed |

On the City entity. Interest rate scales with loan-to-creditworthiness ratio via `LoanSystem.GetTargetInterest()`. If unpaid for 262144 frames (~1 game day) and player has positive money, a `TriggerType.UnpaidLoan` is fired.

*Source: `Game.dll` -> `Game.Simulation.Loan`*

### `Creditworthiness` (Game.Simulation)

| Field | Type | Description |
|-------|------|-------------|
| m_Amount | int | Maximum loan amount the player can borrow |

On the City entity. Used by `LoanSystem` to calculate interest rates and clamp loan amounts.

*Source: `Game.dll` -> `Game.Simulation.Creditworthiness`*

### `TaxRates` (Game.City)

| Field | Type | Description |
|-------|------|-------------|
| m_TaxRate | int | Tax rate value at this index |

Buffer on the City entity. Indexed by `TaxRate` enum. Index 0 is the main rate; indices 1-4 are offsets per area type (residential, commercial, industrial, office). Indices 5-9 are education level offsets, 10-50 are commercial resource offsets, 51-91 are industrial resource offsets. Total 92 entries.

*Source: `Game.dll` -> `Game.City.TaxRates`*

### `ServiceFeeCollector` (Game.City)

Zero-size tag component (no fields). Marks buildings that collect service fees. Used in `ServiceFeeSystem` query: entities with `ServiceFeeCollector` + (`Patient` or `Student` buffer), excluding `OutsideConnection`.

*Source: `Game.dll` -> `Game.City.ServiceFeeCollector`*

### `CollectedCityServiceFeeData` (Game.Simulation)

| Field | Type | Description |
|-------|------|-------------|
| m_PlayerResource | int | `PlayerResource` enum cast to int |
| m_Export | float | Total export fee revenue (cost * 128) |
| m_Import | float | Total import fee cost (cost * 128) |
| m_Internal | float | Total internal fee revenue (cost * 128) |
| m_ExportCount | float | Export service units consumed (amount * 128) |
| m_ImportCount | float | Import service units consumed (amount * 128) |
| m_InternalCount | float | Internal service units consumed (amount * 128) |

Buffer element on city service entities. Tracks collected fees per service resource. `ServiceFeeSystem.FeeToCityJob` aggregates `FeeEvent` items into these buffers each update: positive amounts from local citizens go to `m_Internal`, from outside connections to `m_Export`, and negative amounts (imports) to `m_Import`. All values are multiplied by 128 (kUpdatesPerDay) when stored. Used by `CityServiceBudgetSystem` to compute income/expense categories.

*Source: `Game.dll` -> `Game.Simulation.CollectedCityServiceFeeData`*

### `EconomyParameterData` (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_ExtractorCompanyExportMultiplier | float | Multiplier for extractor company exports |
| m_Wage0 through m_Wage4 | int | Base wages per job level (0-4) |
| m_CommuterWageMultiplier | float | Wage multiplier for commuters |
| m_CompanyBankruptcyLimit | int | Money threshold for company bankruptcy |
| m_ResidentialMinimumEarnings | int | Minimum residential household earnings |
| m_UnemploymentBenefit | int | Unemployment benefit payment |
| m_Pension | int | Pension payment amount |
| m_FamilyAllowance | int | Family allowance payment amount |
| m_ResourceConsumptionMultiplier | float2 | Resource consumption range multiplier |
| m_ResourceConsumptionPerCitizen | float | Base resource consumption per citizen |
| m_TouristConsumptionMultiplier | float | Tourist spending multiplier |
| m_TouristInitialWealthRange | int | Tourist starting wealth range |
| m_TouristInitialWealthOffset | int | Tourist starting wealth offset |
| m_WorkDayStart | float | Work day start time (fraction of day) |
| m_WorkDayEnd | float | Work day end time (fraction of day) |
| m_IndustrialEfficiency | float | Industrial production efficiency |
| m_CommercialEfficiency | float | Commercial production efficiency |
| m_ExtractorProductionEfficiency | float | Extractor production efficiency |
| m_OfficeResourceConsumedPerIndustrialUnit | int | Office resource per industrial unit |
| m_TrafficReduction | float | Traffic reduction factor |
| m_MaxCitySpecializationBonus | float | Maximum city specialization bonus |
| m_ResourceProductionCoefficient | int | Resource production coefficient |
| m_LandValueModifier | float3 | Land value modifier (3 components) |
| m_RentPriceBuildingZoneTypeBase | float3 | Base rent per building zone type |
| m_MixedBuildingCompanyRentPercentage | float | Mixed building company rent percentage |
| m_ResidentialUpkeepLevelExponent | float | Residential upkeep level scaling exponent |
| m_CommercialUpkeepLevelExponent | float | Commercial upkeep level scaling exponent |
| m_IndustrialUpkeepLevelExponent | float | Industrial upkeep level scaling exponent |
| m_CityServiceWageAdjustment | float | Wage multiplier for city service jobs |
| m_PlayerStartMoney | int | Starting money for new games |
| m_BuildRefundPercentage | float3 | Building refund percentage curve |
| m_BuildRefundTimeRange | float3 | Building refund time range |
| m_RelocationCostMultiplier | float | Relocation cost multiplier |
| m_RoadRefundPercentage | float3 | Road refund percentage curve |
| m_RoadRefundTimeRange | float3 | Road refund time range |
| m_TreeCostMultipliers | int3 | Tree cost multipliers |
| m_MapTileUpkeepCostMultiplier | AnimationCurve1 | Map tile upkeep cost curve |
| m_LoanMinMaxInterestRate | float2 | Loan interest rate range (min, max) |
| m_ProfitabilityRange | int2 | Company profitability range |

Singleton component on the economy prefab entity. `GetWage(jobLevel, cityServiceJob)` returns wages scaled by `m_CityServiceWageAdjustment` for city service jobs. The `m_LoanMinMaxInterestRate` field feeds directly into `LoanSystem.GetTargetInterest()`.

*Source: `Game.dll` -> `Game.Prefabs.EconomyParameterData`*

## System Flow

### Budget Pipeline

```
TaxSystem (32x/day)           -- collects taxes from TaxPayer entities
ServiceFeeSystem (128x/day)   -- collects fees from patients & students
TradeSystem                   -- manages import/export trade balances
    |
    v
CityServiceBudgetSystem       -- aggregates all income & expense sources
    |                            into NativeArray<int> m_Income[14], m_Expenses[15]
    v
BudgetApplySystem (1024x/day) -- sums income - expenses, calls PlayerMoney.Add()
    |
    v
PlayerMoney.m_Money           -- updated on City entity
```

### Tax Collection

1. Entities with `TaxPayer` accumulate `m_UntaxedIncome` as they earn money
2. `TaxSystem` runs 32 times per day (`GetUpdateInterval = 262144 / (32 * 16)`)
3. For each entity whose `UpdateFrame` matches: `tax = round(0.01 * rate * income)`
4. Tax is deducted from the entity's `Resources` buffer (Money resource)
5. Residential tax is categorized by worker level; commercial/industrial by output resource
6. Office companies (weightless output resources) are automatically reclassified as `TaxOffice`

### Service Fee Collection

1. Buildings with `ServiceFeeCollector` + `Patient` or `Student` buffers are queried
2. Each patient pays healthcare fees; each student pays education fees (level-based)
3. Fee is deducted from the household's `Resources` buffer
4. Fee events are accumulated and categorized as internal/export/import

### Loan Interest

1. `LoanUpdateSystem` runs 32 times per day
2. Interest rate: `lerp(minRate, maxRate, saturate(loanAmount / creditworthiness))`
3. City modifiers can adjust the interest rate via `CityModifierType.LoanInterest`
4. `LoanSystem.ChangeLoan()` enqueues a `LoanAction` -- the amount difference is added/subtracted from `PlayerMoney`

### Trade

1. `TradeSystem` runs 128 times per day (`kUpdatesPerDay = 128`) and manages resource flow through outside connections
2. Each tradeable resource has a trade balance tracked per resource index in `NativeArray<int> m_TradeBalances`
3. Trade balance decays each update: `balance = round((1 - 0.01) * balance)` (`kRefreshRate = 0.01f`)
4. Trade costs are calculated per transport type (Road, Rail, Water, Air) via `CalculateTradeCost()`:
   - Buy cost: `weightCost * weight`, scaled by `1 + distanceCost * max(50, sqrt(-tradeBalance))` when importing (negative balance), then `CityModifierType.ImportCost` applied
   - Sell cost: `weightCost * weight`, scaled by `1 + distanceCost * max(50, sqrt(tradeBalance))` when exporting (positive balance), then `CityModifierType.ExportCost` applied
5. Storage rebalancing: outside connection companies target 50% of `storageLimit / numResourceTypes`. Urgency = `|delta| / target`. If urgency > 1, add full delta; otherwise add `(delta * urgency / kUpdatesPerDay) * 8`
6. `OutgoingMail` resources are zeroed out rather than traded; garbage uses `GarbageFacilityData.m_GarbageCapacity`

## Income Sources (14 total)

| Enum | Source | Origin |
|------|--------|--------|
| TaxResidential | Household income tax | TaxSystem |
| TaxCommercial | Commercial company tax | TaxSystem |
| TaxIndustrial | Industrial company tax | TaxSystem |
| TaxOffice | Office company tax | TaxSystem (auto-classified) |
| FeeHealthcare | Healthcare service fees | ServiceFeeSystem |
| FeeElectricity | Electricity service fees | ServiceFeeSystem |
| FeeEducation | Education service fees | ServiceFeeSystem |
| FeeGarbage | Garbage service fees | ServiceFeeSystem |
| FeeWater | Water service fees | ServiceFeeSystem |
| FeeParking | Parking fees | ServiceFeeSystem |
| FeePublicTransport | Transit fees | ServiceFeeSystem |
| GovernmentSubsidy | Monthly government subsidy | CityServiceBudgetSystem |
| ExportElectricity | Electricity export revenue | ServiceFeeSystem trade |
| ExportWater | Water export revenue | ServiceFeeSystem trade |

## Expense Sources (15 total)

| Enum | Source | Origin |
|------|--------|--------|
| ServiceUpkeep | All service building upkeep | CityServiceBudgetSystem |
| LoanInterest | Daily loan interest | LoanUpdateSystem |
| ImportElectricity | Electricity import cost | ServiceFeeSystem trade |
| ImportWater | Water import cost | ServiceFeeSystem trade |
| ExportSewage | Sewage export cost | ServiceFeeSystem trade |
| SubsidyResidential | Negative residential tax | TaxSystem |
| SubsidyCommercial | Negative commercial tax | TaxSystem |
| SubsidyIndustrial | Negative industrial tax | TaxSystem |
| SubsidyOffice | Negative office tax | TaxSystem |
| ImportPoliceService | Imported police service | CityServiceBudgetSystem |
| ImportAmbulanceService | Imported ambulance service | CityServiceBudgetSystem |
| ImportHearseService | Imported hearse service | CityServiceBudgetSystem |
| ImportFireEngineService | Imported fire engine service | CityServiceBudgetSystem |
| ImportGarbageService | Imported garbage service | CityServiceBudgetSystem |
| MapTileUpkeep | Map tile unlock upkeep | CityServiceBudgetSystem |

## Key Constants

| Constant | Value | System | Description |
|----------|-------|--------|-------------|
| kUpdatesPerDay | 32 | TaxSystem | Tax collection frequency |
| kUpdatesPerDay | 128 | ServiceFeeSystem | Fee collection frequency |
| kUpdatesPerDay | 1024 | BudgetApplySystem | Budget application frequency |
| kUpdatesPerDay | 32 | LoanUpdateSystem | Loan interest check frequency |
| kMaxMoney | 2,000,000,000 | PlayerMoney | Money balance cap |
| kUpdatesPerDay | 128 | TradeSystem | Trade balance update frequency |
| kUpdatesPerDay | 128 | GameModeGovernmentSubsidiesSystem | Government subsidy check frequency |
| kRefreshRate | 0.01 | TradeSystem | Trade balance decay rate per update (1%) |
| kCompanyUpdatesPerDay | 256 | EconomyUtils | Company update frequency |
| Simulation frames/day | 262,144 | SimulationSystem | Total frames in one game day |
| Default main tax rate | 10 | TaxSystem | Initial main rate for new games |

## Modding Notes

- **Read player money**: Get `PlayerMoney` from `CitySystem.City` entity
- **Read/set tax rates**: Use `ITaxSystem` interface or access `TaxRates` buffer on City entity
- **Read/set service fees**: Access `ServiceFee` buffer on City entity; use `ServiceFeeSystem.SetFee()`
- **Take/give loans**: Use `ILoanSystem.ChangeLoan(amount)` -- 0 to repay fully
- **Custom income/expense**: Patch `BudgetApplySystem` or directly call `PlayerMoney.Add()` on the City entity
- **Service fee effects**: Electricity and water fees affect consumption multiplier and citizen happiness
- **Trade balance**: `BudgetSystem.GetTrade(resource)` returns net trade for a resource
- **Service budget slider**: `CityServiceBudgetSystem.SetServiceBudget(servicePrefab, percentage)` sets the budget slider (default 100). Upkeep cost is scaled by `budget / 100`. Efficiency is evaluated via `BuildingEfficiencyParameterData.m_ServiceBudgetEfficiencyFactor` curve. Inactive buildings pay 10% upkeep.
- **Government subsidy**: Only active when `ModeSettingData.m_EnableGovernmentSubsidies` is true. Scales linearly from 0% to `MaxMoneyCoverPercentage` of total expenses based on how far `PlayerMoney` has dropped below `MoneyCoverThreshold.x`. No subsidy when money is above the threshold.
- **Imported service costs**: Scaled by `fee * (population / OCServiceTradePopulationRange + 1) * OCServiceTradePopulationRange`, modified by `CityModifierType.CityServiceImportCost`. Only active when `CityOption.ImportOutsideServices` is enabled.
- **Tax paid multiplier**: `ModeSettingData.m_TaxPaidMultiplier` (float3) scales tax collection per sector (x=residential, y=commercial, z=industrial). Defaults to (1, 1, 1).
- All budget systems use the `GameSystemBase` ECS pattern with Burst-compiled jobs

## Harmony Patch Points

### Candidate 1: `Game.Simulation.TaxSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Intercept or modify tax collection. A Prefix returning false skips all tax collection for the tick. A Postfix can read the statistics queue to track what was collected, or adjust the m_TaxPaidMultiplier field before the next run.
- **Risk level**: High
- **Side effects**: Skipping tax collection breaks the budget pipeline entirely. Residential, commercial, and industrial tax jobs all run in this method. Modifying m_TaxRates between runs can cause desync with the UI. District tax modifiers (`DistrictModifierType.LowCommercialTax`) are applied separately from the base rate.

### Candidate 2: `Game.Simulation.BudgetApplySystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Inject custom income/expense before or after the budget is applied to PlayerMoney. A Prefix can modify the m_Income/m_Expenses arrays from `CityServiceBudgetSystem`. A Postfix can apply additional money changes after the standard pipeline runs.
- **Risk level**: Medium
- **Side effects**: Runs 1024 times per day, so any per-tick cost adds up. Income/expense arrays are shared with CityServiceBudgetSystem via job dependencies -- modifying them without completing jobs first causes race conditions. The statistics events logged here feed the UI, so modifications may show incorrect values in the economy panel.

### Candidate 3: `Game.Simulation.ServiceFeeSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify service fee collection behavior. A Prefix can alter the fee queue or modify ServiceFee buffer values before collection. A Postfix can process the CollectedCityServiceFeeData results. Can also intercept the CacheFees() call that runs at the start of OnUpdate.
- **Risk level**: Medium
- **Side effects**: Runs 128 times per day. Fee events are queued and processed by FeeToCityJob, then triggers are sent by TriggerJob. Modifying fees mid-update could cause inconsistency between the PayFeeJob and the cached values. The m_FeeQueue is shared state -- unsafe to modify without completing m_Writers dependency.

### Candidate 4: `Game.Simulation.TradeSystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix or Postfix
- **What it enables**: Modify trade behavior. A Prefix can alter m_TradeBalances or the OutsideTradeParameterData before the TradeJob runs. A Postfix can read the updated trade balances and cached costs after the job completes.
- **Risk level**: Medium
- **Side effects**: Runs 128 times per day. The TradeJob is scheduled as a single IJob (not parallel), but it holds dependencies on CityStatisticsSystem and CityProductionStatisticSystem. Modifying m_TradeBalances without completing m_DebugTradeBalanceDeps first is unsafe. Trade costs feed into company buy/sell decisions system-wide.

## Examples

### Example 1: Read Player Money

Read the current money balance from the City entity.

```csharp
// In a GameSystemBase.OnUpdate() or similar
CitySystem citySystem = World.GetOrCreateSystemManaged<CitySystem>();
PlayerMoney money = EntityManager.GetComponentData<PlayerMoney>(citySystem.City);
int balance = money.money; // Returns 2 billion if m_Unlimited is true (sandbox mode)
```

### Example 2: Read and Modify Tax Rates

Use the ITaxSystem interface to get/set tax rates, or directly access the TaxRates buffer.

```csharp
// Via the ITaxSystem interface (preferred -- handles clamping and validation)
TaxSystem taxSystem = World.GetOrCreateSystemManaged<TaxSystem>();

// Read the main tax rate
int mainRate = taxSystem.TaxRate;

// Set a new main rate (clamped by TaxParameterData.m_TotalTaxLimits)
taxSystem.TaxRate = 12;

// Read/set per-area rates
int residentialRate = taxSystem.GetTaxRate(TaxAreaType.Residential); // Main + residential offset
taxSystem.SetTaxRate(TaxAreaType.Commercial, 15); // Sets offset so Main + offset = 15

// Read/set per-resource rates (commercial/industrial)
int electronicsTax = taxSystem.GetCommercialTaxRate(Resource.Electronics);
taxSystem.SetCommercialTaxRate(Resource.Electronics, 14);

// Read/set per-job-level residential rates
int level2Rate = taxSystem.GetResidentialTaxRate(2);
taxSystem.SetResidentialTaxRate(2, 11);

// Or directly access the buffer
CitySystem citySystem = World.GetOrCreateSystemManaged<CitySystem>();
DynamicBuffer<TaxRates> rates = EntityManager.GetBuffer<TaxRates>(citySystem.City);
int rawMainRate = rates[0].m_TaxRate;
int residentialOffset = rates[(int)TaxAreaType.Residential].m_TaxRate;
```

### Example 3: Adjust a Service Fee

Read and modify service fees on the City entity using static helpers.

```csharp
CitySystem citySystem = World.GetOrCreateSystemManaged<CitySystem>();
DynamicBuffer<ServiceFee> fees = EntityManager.GetBuffer<ServiceFee>(citySystem.City);

// Set a new electricity fee
ServiceFeeSystem.SetFee(PlayerResource.Electricity, fees, 0.5f);

// Read the current healthcare fee
float currentFee;
if (ServiceFeeSystem.TryGetFee(PlayerResource.Healthcare, fees, out currentFee))
{
    // currentFee is the per-unit healthcare charge
}

// Or use GetFee (returns 0 if not found)
float waterFee = ServiceFeeSystem.GetFee(PlayerResource.Water, fees);
```

### Example 4: Take or Repay a Loan

Use the ILoanSystem interface to manage loans.

```csharp
LoanSystem loanSystem = World.GetOrCreateSystemManaged<LoanSystem>();

// Check current loan info
LoanInfo current = loanSystem.CurrentLoan;
// current.m_Amount, current.m_DailyInterestRate, current.m_DailyPayment

// Check creditworthiness (max borrow limit)
int maxBorrow = loanSystem.Creditworthiness;

// Request a loan offer for a specific amount
LoanInfo offer = loanSystem.RequestLoanOffer(500000);

// Take the loan (or change to a different amount)
loanSystem.ChangeLoan(500000);

// Repay fully (sets loan to 0, deducts principal from PlayerMoney)
loanSystem.ChangeLoan(0);
```

### Example 5: Read Trade Balance

Use BudgetSystem to read trade volumes per resource.

```csharp
BudgetSystem budgetSystem = World.GetOrCreateSystemManaged<BudgetSystem>();

// Get net trade for a resource (positive = net export, negative = net import)
int tradeVolume = budgetSystem.GetTrade(Resource.Electronics);
int tradeWorth = budgetSystem.GetTradeWorth(Resource.Electronics);
int totalTradeWorth = budgetSystem.GetTotalTradeWorth();

// Read the service budget for income/expense breakdown
CityServiceBudgetSystem budgetSys = World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
int totalIncome = budgetSys.GetTotalIncome();
int totalExpenses = budgetSys.GetTotalExpenses();
int balance = budgetSys.GetBalance(); // net income + expenses
int taxIncome = budgetSys.GetIncome(IncomeSource.TaxResidential);
int upkeepCost = budgetSys.GetExpense(ExpenseSource.ServiceUpkeep);
```

## Open Questions

- [ ] **Tax rate limits**: The exact min/max values come from `TaxParameterData` fields: `m_TotalTaxLimits`, `m_ResidentialTaxLimits`, `m_CommercialTaxLimits`, `m_IndustrialTaxLimits`, `m_OfficeTaxLimits`, `m_JobLevelTaxLimits`, and `m_ResourceTaxLimits`. The actual numeric values require in-game inspection of the prefab data.
- [ ] **Creditworthiness calculation**: How `Creditworthiness.m_Amount` is computed is not covered here -- it likely depends on city population, land value, and economic statistics.
- [x] **Service budget slider effect**: `CityServiceBudgetSystem` uses a per-service budget percentage that scales the Money portion of upkeep costs by `budget / 100`. Efficiency is evaluated via `BuildingEfficiencyParameterData.m_ServiceBudgetEfficiencyFactor.Evaluate(budget / 100)`. Default budget is 100. Inactive buildings always pay 10% of their upkeep regardless of slider.
- [x] **Trade cost formula details**: `TradeSystem.CalculateTradeCost()` is now fully documented. Buy cost = `weightCost * weight`, scaled by `1 + distanceCost * max(50, sqrt(-tradeBalance))` for negative balances, then `CityModifierType.ImportCost` applied. Sell cost follows the same pattern with positive balances and `ExportCost` modifier.
- [x] **Government subsidy timing**: Government subsidies are controlled by `GameModeGovernmentSubsidiesSystem`. They are only active when `ModeSettingData.m_EnableGovernmentSubsidies` is true. The subsidy scales linearly from 0% to `MaxMoneyCoverPercentage` of total expenses, based on how far `PlayerMoney` has dropped below `MoneyCoverThreshold.x` toward `MoneyCoverThreshold.y`. When money is above the threshold, no subsidy is given.
