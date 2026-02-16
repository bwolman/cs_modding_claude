# Research: Economy & Budget

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 handles the city economy -- tax collection, service fees, city budget (income/expenses), loans, trade with outside connections, and how money flows through the simulation.

**Why**: To build mods that modify tax rates, adjust service fees, create custom income/expense sources, interact with the loan system, or alter trade behavior.

**Boundaries**: Not covering company-level profitability AI in depth (just the tax interface), demand systems, or detailed resource production chains. Those are separate topics.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | `BudgetSystem`, `BudgetApplySystem`, `CityServiceBudgetSystem`, `TaxSystem`, `LoanUpdateSystem`, `ServiceFeeSystem`, `TradeSystem`, `Loan`, `Creditworthiness`, `TaxAreaType`, `TaxResultType` |
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

1. `TradeSystem` runs periodically and manages resource flow through outside connections
2. Each tradeable resource has a trade balance tracked per resource index
3. Trade costs are calculated per transport type (Road, Rail, Water, Air)
4. Storage companies at outside connections buy/sell to balance their inventories
5. Trade balance refreshes with decay: `balance *= (1 - kRefreshRate)` each update

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
| kCompanyUpdatesPerDay | 256 | EconomyUtils | Company update frequency |
| Simulation frames/day | 262,144 | SimulationSystem | Total frames in one game day |

## Modding Notes

- **Read player money**: Get `PlayerMoney` from `CitySystem.City` entity
- **Read/set tax rates**: Use `ITaxSystem` interface or access `TaxRates` buffer on City entity
- **Read/set service fees**: Access `ServiceFee` buffer on City entity; use `ServiceFeeSystem.SetFee()`
- **Take/give loans**: Use `ILoanSystem.ChangeLoan(amount)` -- 0 to repay fully
- **Custom income/expense**: Patch `BudgetApplySystem` or directly call `PlayerMoney.Add()` on the City entity
- **Service fee effects**: Electricity and water fees affect consumption multiplier and citizen happiness
- **Trade balance**: `BudgetSystem.GetTrade(resource)` returns net trade for a resource
- All budget systems use the `GameSystemBase` ECS pattern with Burst-compiled jobs
