# Research: Company Simulation

> **Status**: Complete
> **Date started**: 2026-02-16
> **Last updated**: 2026-02-22

## Scope

**What we're investigating**: How companies operate in CS2 -- from spawning in zoned buildings through production cycles, profitability tracking, dividend payments to households, and the bankruptcy/move-away decision pipeline.

**Why**: Understanding the company simulation is essential for mods that adjust economic balance, modify production rates, alter tax behavior, or create custom company types. Companies are the economic backbone connecting resources, employment, and city income.

**Boundaries**: The Resource enum and resource buffer details are documented in [Resource Production](../ResourceProduction/README.md). Citizen employment seeking and household income are separate topics. Zone spawn mechanics (how buildings appear) are in the Zoning research.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Companies | All company ECS components (CompanyData, Profitability, tag components, trade, employees) |
| Game.dll | Game.Simulation | Company systems (profitability, dividends, move-away, production for each type) |
| Game.dll | Game.Prefabs | CompanyPrefab archetype, prefab data structs (IndustrialProcessData, ServiceCompanyData, etc.) |
| Game.dll | Game.Economy | Resource buffers, EconomyUtils (pricing, production calculations) |
| Game.dll | Game.Agents | TaxPayer component (tracks untaxed income per company) |

## Component Map

### `CompanyData` (Game.Companies)

Core component present on every company entity.

| Field | Type | Description |
|-------|------|-------------|
| m_RandomSeed | Random | Per-company random seed used for stochastic decisions |
| m_Brand | Entity | Reference to the company's brand prefab entity |

*Source: `Game.dll` -> `Game.Companies.CompanyData`*

### `Profitability` (Game.Companies)

Tracks the company's current profitability rating.

| Field | Type | Description |
|-------|------|-------------|
| m_Profitability | byte | Profitability rating 0-255, where 127 = break-even. Below 127 = losing money, above = profitable |
| m_LastTotalWorth | int | Total worth of company at last profitability update, used to compute delta |

The profitability formula: `(byte)(clamp((currentWorth - lastWorth) / 100, -127, 128) + 127)`

*Source: `Game.dll` -> `Game.Companies.Profitability`*

### `CompanyStatisticData` (Game.Companies)

Comprehensive financial tracking for each company. Used by move-away and UI systems.

| Field | Type | Description |
|-------|------|-------------|
| m_MaxNumberOfCustomers | int | Historical max monthly customer count |
| m_MonthlyCustomerCount | int | Customer count for the completed month |
| m_MonthlyCostBuyingResources | int | Monthly spend on input resources |
| m_CurrentNumberOfCustomers | int | Running customer count for current month |
| m_CurrentCostOfBuyingResources | int | Running cost for current month |
| m_Income | int | Total income |
| m_Worth | int | Total company worth |
| m_Profit | int | Net profit |
| m_WagePaid | int | Wages paid to employees |
| m_RentPaid | int | Rent paid for building |
| m_ElectricityPaid | int | Electricity costs |
| m_WaterPaid | int | Water costs |
| m_SewagePaid | int | Sewage costs |
| m_GarbagePaid | int | Garbage costs |
| m_TaxPaid | int | Tax paid |
| m_CostBuyResource | int | Cost of buying resources |
| m_LastUpdateWorth | int | Worth at last update |
| m_LastFrameLowIncome | uint | Frame index when company last had low income (used for bankruptcy delay) |
| m_LastUpdateProduce | int | Production amount at last update |

*Source: `Game.dll` -> `Game.Companies.CompanyStatisticData`*

### Company Type Tags (Game.Companies)

These are zero-size tag components that categorize companies. A company entity has exactly one of these:

| Component | Description |
|-----------|-------------|
| `CommercialCompany` | Retail/service companies in commercial zones |
| `IndustrialCompany` | Manufacturing companies in industrial zones |
| `OfficeCompany` | Office companies producing weightless resources (Software, Telecom, etc.) |
| `ExtractorCompany` | Raw resource extractors (farms, mines, oil rigs, logging) |
| `ProcessingCompany` | Factories that transform input resources into output resources |
| `StorageCompany` | Warehouses that store and redistribute resources |
| `TransportCompany` | Companies that operate transport vehicles. **Runtime-confirmed: present on ALL company entities** (including commercial/industrial/office), not only logistics companies. |

*Source: `Game.dll` -> `Game.Companies.*`*

### `ServiceCompanyData` (Game.Companies)

Runtime data for service-type companies (commercial businesses serving citizens).

| Field | Type | Description |
|-------|------|-------------|
| m_MaxService | int | Maximum service units the company can stock |
| m_WorkPerUnit | int | Work required per unit of service (set to 0 at init) |
| m_MaxWorkersPerCell | float | Maximum workers per lot cell (scales with building size) |
| m_ServiceConsuming | int | Service consumed per leisure tick by visiting citizens |

*Source: `Game.dll` -> `Game.Companies.ServiceCompanyData`*

### `ServiceAvailable` (Game.Companies)

Tracks current service availability for commercial companies.

| Field | Type | Description |
|-------|------|-------------|
| m_ServiceAvailable | int | Current service units available for customers |
| m_MeanPriority | float | Average priority of service requests |

*Source: `Game.dll` -> `Game.Companies.ServiceAvailable`*

### `BuyingCompany` (Game.Companies)

Marks a company that is actively seeking to purchase input resources.

| Field | Type | Description |
|-------|------|-------------|
| m_LastTradePartner | Entity | Last entity this company traded with |
| m_MeanInputTripLength | float | Average distance for input resource deliveries |

*Source: `Game.dll` -> `Game.Companies.BuyingCompany`*

### `CompanyNotifications` (Game.Companies)

Tracks notification state for "no inputs" and "no customers" warnings.

| Field | Type | Description |
|-------|------|-------------|
| m_NoInputCounter | short | Counter for consecutive no-input ticks |
| m_NoCustomersCounter | short | Counter for consecutive no-customers ticks |
| m_NoInputEntity | Entity | Entity where the no-input icon is displayed |
| m_NoCustomersEntity | Entity | Entity where the no-customers icon is displayed |

*Source: `Game.dll` -> `Game.Companies.CompanyNotifications`*

### `StorageCompany` (Game.Companies)

Tag + data for warehouse companies.

| Field | Type | Description |
|-------|------|-------------|
| m_LastTradePartner | Entity | Last entity this warehouse traded with |

*Source: `Game.dll` -> `Game.Companies.StorageCompany`*

### `StorageLimitData` (Game.Companies)

Storage capacity limit, combinable from building upgrades.

| Field | Type | Description |
|-------|------|-------------|
| m_Limit | int | Base storage limit in resource units |

Has `GetAdjustedLimitForWarehouse(spawnable, building)` which returns `m_Limit * level * lotSizeX * lotSizeY`.

*Source: `Game.dll` -> `Game.Companies.StorageLimitData`*

### `ResourceBuyer` (Game.Companies)

Active resource purchase request placed by a company.

| Field | Type | Description |
|-------|------|-------------|
| m_Payer | Entity | Entity that will pay for the resources |
| m_Flags | SetupTargetFlags | Pathfinding flags for the delivery |
| m_ResourceNeeded | Resource | Which resource is being purchased |
| m_AmountNeeded | int | How much is needed |
| m_Location | float3 | World position for delivery |

*Source: `Game.dll` -> `Game.Companies.ResourceBuyer`*

### `ResourceExporter` (Game.Companies)

Marks a company that has excess output ready for export/sale.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | Resource type to export |
| m_Amount | int | Amount to export |

*Source: `Game.dll` -> `Game.Companies.ResourceExporter`*

### `TradeCost` (Game.Companies, buffer)

Per-resource trade cost tracking. Buffer element on company entities.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | Resource type |
| m_BuyCost | float | Cost to buy this resource |
| m_SellCost | float | Revenue from selling this resource |
| m_LastTransferRequestTime | long | Timestamp of last transfer request |

*Source: `Game.dll` -> `Game.Companies.TradeCost`*

### `CurrentTrading` (Game.Companies, buffer)

Active trade operations in progress. Buffer element.

| Field | Type | Description |
|-------|------|-------------|
| m_TradingResourceAmount | int | Amount being traded |
| m_TradingResource | Resource | Resource type |
| m_TradingStartFrameIndex | uint | Frame when trade started |
| m_OutsideConnectionType | OutsideConnectionTransferType | Type of outside connection used |

*Source: `Game.dll` -> `Game.Companies.CurrentTrading`*

### `WorkProvider` (Game.Companies)

Employment data for a company.

| Field | Type | Description |
|-------|------|-------------|
| m_MaxWorkers | int | Maximum number of workers |
| m_UneducatedCooldown | short | Notification cooldown for uneducated worker shortage |
| m_EducatedCooldown | short | Notification cooldown for educated worker shortage |
| m_UneducatedNotificationEntity | Entity | Active notification icon entity for uneducated workers |
| m_EducatedNotificationEntity | Entity | Active notification icon entity for educated workers |
| m_EfficiencyCooldown | short | Cooldown for efficiency notifications |

*Source: `Game.dll` -> `Game.Companies.WorkProvider`*

### `Workplaces` (Game.Companies)

Breakdown of workplace slots by education level.

| Field | Type | Description |
|-------|------|-------------|
| m_Uneducated | int | Slots for uneducated workers (level 0) |
| m_PoorlyEducated | int | Slots for poorly educated workers (level 1) |
| m_Educated | int | Slots for educated workers (level 2) |
| m_WellEducated | int | Slots for well-educated workers (level 3) |
| m_HighlyEducated | int | Slots for highly educated workers (level 4) |

Properties: `TotalCount`, `SimpleWorkplacesCount` (0+1), `ComplexWorkplacesCount` (2+3+4).

*Source: `Game.dll` -> `Game.Companies.Workplaces`*

### `FreeWorkplaces` (Game.Companies)

Current free workplace slots, clamped to byte range.

| Field | Type | Description |
|-------|------|-------------|
| m_Uneducated | byte | Free uneducated slots (0-255) |
| m_PoorlyEducated | byte | Free poorly educated slots |
| m_Educated | byte | Free educated slots |
| m_WellEducated | byte | Free well-educated slots |
| m_HighlyEducated | byte | Free highly educated slots |

Has `Refresh(employees, maxWorkers, complexity, level)` to recalculate from current employee buffer.

*Source: `Game.dll` -> `Game.Companies.FreeWorkplaces`*

### `Employee` (Game.Companies, buffer)

Buffer element listing each employee of a company.

| Field | Type | Description |
|-------|------|-------------|
| m_Worker | Entity | The citizen entity working here |
| m_Level | byte | Education level of the worker (0-4) |

*Source: `Game.dll` -> `Game.Companies.Employee`*

### `LodgingProvider` (Game.Companies)

For hotel/lodging companies.

| Field | Type | Description |
|-------|------|-------------|
| m_FreeRooms | int | Number of available rooms |
| m_Price | int | Price per room |

*Source: `Game.dll` -> `Game.Companies.LodgingProvider`*

### `TransportCompanyData` (Game.Companies)

Transport company capacity data.

| Field | Type | Description |
|-------|------|-------------|
| m_MaxTransports | int | Maximum number of transport vehicles |

*Source: `Game.dll` -> `Game.Companies.TransportCompanyData`*

### `StorageTransfer` (Game.Companies)

Active storage transfer operation.

| Field | Type | Description |
|-------|------|-------------|
| m_Resource | Resource | Resource being transferred |
| m_Amount | int | Amount being transferred |

*Source: `Game.dll` -> `Game.Companies.StorageTransfer`*

### `StorageTransferRequest` (Game.Companies, buffer)

Queued transfer request for a storage company.

| Field | Type | Description |
|-------|------|-------------|
| m_Flags | StorageTransferFlags | Transfer method flags (Car=1, Transport=2, Track=4, Incoming=8) |
| m_Resource | Resource | Resource type |
| m_Amount | int | Amount requested |
| m_Target | Entity | Target entity for the transfer |

*Source: `Game.dll` -> `Game.Companies.StorageTransferRequest`*

### Enums

**Workshift**: `Day` (0), `Evening` (1), `Night` (2)

**StorageTransferFlags** (flags byte): `Car` (1), `Transport` (2), `Track` (4), `Incoming` (8)

## System Map

### `CompanyProfitabilitySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update rate**: Once per day (`kUpdatesPerDay = 1`, interval = `262144 / 16`)
- **Queries**:
  - Entities with `Profitability` (excluding Created, Deleted)
  - Entities with `CompanyStatisticData` (excluding Created, Deleted)
- **Reads**: Resources buffer, OwnedVehicle buffer, ServiceAvailable, IndustrialProcessData, ResourceData
- **Writes**: `Profitability.m_Profitability`, `Profitability.m_LastTotalWorth`, `CompanyStatisticData.m_MonthlyCustomerCount`
- **Key logic**:
  1. Calls `EconomyUtils.GetCompanyTotalWorth()` to compute current total worth (resources + vehicle cargo value)
  2. For industrial companies (no ServiceAvailable): uses raw resource prices
  3. Computes profitability as `clamp((currentWorth - lastWorth) / 100, -127, 128) + 127`
  4. Resets monthly statistics: customer count and resource buying costs

### `CompanyDividendSystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update rate**: Once per day (`kUpdatesPerDay = 1`)
- **Queries**: `ProcessingCompany` + `Resources` + `Employee` + `PrefabRef` + `UpdateFrame`, excluding `StorageCompany` and `Deleted`
- **Reads**: Employee buffer, HouseholdMember, EconomyParameterData
- **Writes**: Company Resources buffer (deducts dividends), Household Resources buffer (adds dividends)
- **Key logic**:
  1. Only processes companies with positive money balance and employees
  2. Dividend per employee: `max(0, companyMoney / (8 * employeeCount))`
  3. For each employee, looks up their household via `HouseholdMember`
  4. Enqueues dividend to household's Resources buffer as Money
  5. Deducts total dividends from company: `-(dividendPerEmployee * employeeCount)`
  6. Uses a `NativeQueue<Dividend>` for thread-safe cross-entity transfers

### `CompanyMoveAwaySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update phase**: Simulation
- **Update rate**: 16 times per day (`kUpdatesPerDay = 16`)
- **Queries**:
  - CheckMoveAway: `CompanyStatisticData` + `ProcessingCompany` + `PropertyRenter` + `WorkProvider` + `Resources` + `PrefabRef`, excluding `ExtractorCompany`, `MovingAway`, `Deleted`
  - MovingAway: `ProcessingCompany` + `MovingAway` + `PropertyRenter`, excluding `Deleted`
- **Reads**: TaxRates, ServiceAvailable, OfficeProperty, IndustrialProcessData, WorkProvider, EconomyParameterData
- **Writes**: Adds `MovingAway` component, adds `Deleted` component, manages property market
- **Key logic (CheckMoveAwayJob)**:
  1. Computes `GetCompanyMoveAwayChance()` based on tax rate and worker notifications
  2. Random roll: `random.NextInt(100) < moveAwayChance` -> add MovingAway
  3. If not moving away but `totalWorth < m_CompanyBankruptcyLimit`:
     - Records `m_LastFrameLowIncome` timestamp
     - If low income persists for > 65536 frames (~4 in-game days), company moves away
  4. If above bankruptcy limit, resets the low-income timer
- **Key logic (MovingAwayJob)**:
  1. Marks property as `PropertyToBeOnMarket`
  2. Cleans up notification icons
  3. Adds `Deleted` to the company entity

### `CompanyUtils` (Game.Simulation)

Static utility class with key calculations:

- **`GetCompanyMoveAwayChance()`**: Returns percentage chance (0-100) based on:
  - Tax rate impact: `(taxRate - 10) * 5 / 2` (10% is neutral, above adds chance)
  - +5% if uneducated worker shortage notification exists
  - +20% if educated worker shortage notification exists
- **`GetCommercialMaxFittingWorkers()`**: `ceil(maxWorkersPerCell * lotX * lotY * (1 + 0.5 * level) * spaceMultiplier)`
- **`GetIndustrialAndOfficeFittingWorkers()`**: Same formula as commercial
- **`GetExtractorFittingWorkers()`**: `ceil(maxWorkersPerCell * area * spaceMultiplier / 2)`
- **`GetCompanyProfitability()`**: Maps profit to 0-255 byte using `EconomyParameterData.m_ProfitabilityRange`

### `EconomyUtils.GetCompanyTotalWorth` (Game.Economy)

Static utility method with two overloads for computing a company's total worth:

**Full overload (with vehicle data)**:
- **Signature**: `static int GetCompanyTotalWorth(IndustrialProcessData processData, DynamicBuffer<Resources> resources, DynamicBuffer<OwnedVehicle> ownedVehicles, ComponentLookup<LayoutElement> layoutElements, ComponentLookup<DeliveryTruck> deliveryTrucks, ResourcePrefabs resourcePrefabs, ComponentLookup<ResourceData> resourceDatas)`
- Sums the value of all resources in the company's `Resources` buffer plus the cargo value of all owned delivery trucks (iterating `OwnedVehicle` -> `LayoutElement` -> `DeliveryTruck.m_Resource` and `m_Amount`)
- Used by `CompanyProfitabilitySystem` for the full worth calculation

**Simple overload (without vehicle data)**:
- **Signature**: `static int GetCompanyTotalWorth(DynamicBuffer<Resources> resources, ResourcePrefabs resourcePrefabs, ComponentLookup<ResourceData> resourceDatas)`
- Sums only the value of resources in the company's `Resources` buffer, ignoring vehicle cargo
- Used in contexts where vehicle data is unavailable or not relevant

The `isIndustrial` flag that determines which overload to use is computed as `!ServiceAvailable.HasComponent(renter)` -- companies with a `ServiceAvailable` component are commercial/service companies, while those without are industrial/processing companies.

### `ServiceCompanySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update rate**: `EconomyUtils.kCompanyUpdatesPerDay` times per day
- **Queries**: `CompanyData` + `ServiceAvailable` + `PrefabRef` + `PropertyRenter` + `WorkProvider` + `Employee` + `UpdateFrame`
- **Key logic (UpdateServiceJob)**:
  1. Computes production per update: `GetCompanyProductionPerDay() / kCompanyUpdatesPerDay`
  2. Adds production to `ServiceAvailable.m_ServiceAvailable` (capped at `m_MaxService`)
  3. Calculates untaxed income from service price and tracks via `TaxPayer`
  4. Triggers "no customers" notification if service stock > threshold (configurable via `CompanyNotificationParameterData.m_NoCustomersServiceLimit`)
  5. Special handling for hotels: checks `LodgingProvider.m_FreeRooms` ratio against `m_NoCustomersHotelLimit`

### `ExtractorCompanySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update rate**: `EconomyUtils.kCompanyUpdatesPerDay` times per day
- **Queries**: `CompanyStatisticData` + `ExtractorCompany` + `PropertyRenter` + `Resources` + `PrefabRef` + `WorkProvider` + `UpdateFrame` + `CompanyData` + `Employee`
- **Key logic (ExtractorJob)**:
  1. Reads `IndustrialProcessData` for input/output recipe (extractors have no input, only output)
  2. Gets natural resource concentration from extractor areas via `GetBestConcentration()`
  3. Sets building efficiency factor for natural resources
  4. Computes production: `GetCompanyProductionPerDay() / kCompanyUpdatesPerDay`, limited by remaining storage
  5. Processes each sub-area, consuming natural resources proportional to production
  6. Adds output resource to company's Resources buffer
  7. When storage > 75% full, adds `ResourceExporter` component to trigger delivery truck dispatch

### `ProcessingCompanySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update rate**: `EconomyUtils.kCompanyUpdatesPerDay` times per day
- **Queries**: `ProcessingCompany` entities with `PropertyRenter`, `Resources`, `PrefabRef`, `WorkProvider`, `UpdateFrame`, `CompanyData`, `Employee`
- **Key logic (UpdateProcessingJob)**:
  1. Reads `IndustrialProcessData` for the input1 -> input2 -> output recipe
  2. If input == output with no second input, skips (pass-through)
  3. Calculates production limited by: employee output, input1 stock, input2 stock
  4. Consumes inputs proportionally: `inputConsumed = production * (inputAmount / outputAmount)`
  5. Sets building `EfficiencyFactor.LackResources` to 0 if no production possible
  6. Adds output to Resources buffer, limited by storage capacity
  7. Tracks tax income via TaxPayer
  8. When output stock exists, tries to dispatch delivery trucks via `ResourceExporter`
  9. Applies city modifiers for office efficiency, industrial efficiency, specialization bonuses

### `StorageCompanySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update rate**: `EconomyUtils.kCompanyUpdatesPerDay` times per day
- **Queries**: `StorageCompany` entities with `Resources`, `PrefabRef`, `PropertyRenter`, `UpdateFrame`, `TradeCost`
- **Key logic**:
  1. Reads `StorageCompanyData.m_StoredResources` to determine which resources this warehouse handles
  2. Computes adjusted storage limit from building level and lot size
  3. Processes incoming/outgoing `StorageTransferRequest` buffer
  4. Dispatches delivery trucks via owned vehicles
  5. Manages trade costs per resource

### `BuyingCompanySystem` (Game.Simulation)

- **Base class**: GameSystemBase
- **Update rate**: `EconomyUtils.kCompanyUpdatesPerDay` times per day
- **Queries**: Companies with `BuyingCompany` + `OwnedVehicle` + `Resources` + `TripNeeded` + `TradeCost` + `PrefabRef` + `PropertyRenter` + `CompanyNotifications`
- **Key logic (CompanyBuyJob)**:
  1. Reads `IndustrialProcessData` for input requirements
  2. Calculates resource need based on storage limit and current stock
  3. Splits storage capacity evenly among input/output resources
  4. For each needed resource, checks if owned trucks are already transporting it
  5. Adds `ResourceBuyer` component to trigger resource acquisition pathfinding
  6. Manages "no input" notifications via `CompanyNotificationParameterData.m_NoInputCostLimit`

## Data Flow

```
COMPANY CREATION
  ZoneSpawnSystem spawns building -> CompanyPrefab creates company entity
    CompanyPrefab.GetArchetypeComponents() adds:
      CompanyData, UpdateFrame, Resources, PropertySeeker,
      TripNeeded, CompanyNotifications, GuestVehicle
    + type-specific tags (CommercialCompany / IndustrialCompany)
          |
          v
RESOURCE ACQUISITION (kCompanyUpdatesPerDay times/day)
  BuyingCompanySystem
    Checks input resource levels vs storage limit
    Adds ResourceBuyer component -> triggers delivery pathfinding
    Delivery trucks bring resources to company
          |
          v
PRODUCTION (kCompanyUpdatesPerDay times/day)
  ExtractorCompanySystem (for ExtractorCompany)
    Reads natural resource concentration
    Produces output resource, consumes natural resource
    Triggers export when storage > 75%
          |
  ProcessingCompanySystem (for ProcessingCompany)
    Consumes Input1 + Input2 -> produces Output
    Production limited by: employees, inputs, storage
    Tracks profit via TaxPayer
          |
  ServiceCompanySystem (for companies with ServiceAvailable)
    Produces service units from employee labor
    Citizens visit and consume service
    Tracks tax income
          |
          v
PROFITABILITY ASSESSMENT (once/day)
  CompanyProfitabilitySystem
    totalWorth = resources value + vehicle cargo value
    profitability = clamp((worth - lastWorth) / 100, -127, 128) + 127
    Resets monthly customer/cost statistics
          |
          v
DIVIDEND DISTRIBUTION (once/day)
  CompanyDividendSystem
    For ProcessingCompany with positive balance:
      dividendPerEmployee = money / (8 * employeeCount)
      Each employee's household receives dividend as Money
      Company money decremented
          |
          v
BANKRUPTCY / MOVE-AWAY CHECK (16 times/day)
  CompanyMoveAwaySystem
    moveAwayChance = f(taxRate, workerShortages)
    Random roll -> MovingAway tag
    OR: totalWorth < bankruptcyLimit for > 65536 frames -> MovingAway
    MovingAway -> property goes to market -> company Deleted
```

## Prefab & Configuration

### CompanyPrefab (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| zone | AreaType | Commercial or Industrial |
| profitability | float | Initial profitability value |

The `GetArchetypeComponents()` method defines the entity archetype for all companies.

### IndustrialProcessData (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_Input1 | ResourceStack | First input resource and amount |
| m_Input2 | ResourceStack | Second input resource and amount |
| m_Output | ResourceStack | Output resource and amount |
| m_WorkPerUnit | int | Work units required per output unit |
| m_MaxWorkersPerCell | float | Worker density scaling factor |
| m_IsImport | byte | Whether this is an import-only process |

### StorageCompanyData (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_StoredResources | Resource | Flags enum of resources this warehouse handles |
| m_TransportInterval | int2 | Min/max interval between transport dispatches |

### ServiceCompany (Game.Prefabs, class)

| Field | Default | Description |
|-------|---------|-------------|
| m_MaxService | varies | Maximum service units |
| m_MaxWorkersPerCell | varies | Workers per lot cell |
| m_ServiceConsuming | 1 | Service consumed per leisure tick |

### CompanyNotificationParameterData (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_NoInputsNotificationPrefab | Entity | Notification icon prefab for no inputs |
| m_NoCustomersNotificationPrefab | Entity | Notification icon prefab for no customers |
| m_NoInputCostLimit | float | Threshold for "no input" notification |
| m_NoCustomersServiceLimit | float | Service ratio threshold for "no customers" notification |
| m_NoCustomersHotelLimit | float | Free rooms ratio threshold for hotel "no customers" |

### ExtractorParameterData (Game.Prefabs)

| Field | Type | Description |
|-------|------|-------------|
| m_FertilityConsumption | float | Natural resource consumption rate for fertile land |
| m_FishConsumption | float | Consumption rate for fish |
| m_OreConsumption | float | Consumption rate for ore |
| m_ForestConsumption | float | Consumption rate for forest |
| m_OilConsumption | float | Consumption rate for oil |
| m_FullFertility | float | Concentration value for 100% fertility efficiency |
| m_FullFish | float | Concentration value for 100% fish efficiency |
| m_FullOre | float | Concentration value for 100% ore efficiency |
| m_FullOil | float | Concentration value for 100% oil efficiency |

## Harmony Patch Points

### Candidate 1: `Game.Simulation.CompanyUtils.GetCompanyMoveAwayChance`

- **Signature**: `static int GetCompanyMoveAwayChance(Entity company, Entity companyPrefab, Entity property, ref ComponentLookup<ServiceAvailable>, ref ComponentLookup<OfficeProperty>, ref ComponentLookup<IndustrialProcessData>, ref ComponentLookup<WorkProvider>, NativeArray<int> taxRates)`
- **Patch type**: Postfix
- **What it enables**: Override or modify the move-away probability. A mod could reduce bankruptcy risk for certain company types or eliminate tax-driven moves.
- **Risk level**: Low -- pure calculation method, no side effects
- **Side effects**: Changing return value directly affects company retention rates

### Candidate 2: `Game.Simulation.CompanyDividendSystem.DividendJob.Execute`

- **Signature**: Burst-compiled IJobChunk -- cannot be directly Harmony patched
- **Patch type**: Prefix on `CompanyDividendSystem.OnUpdate()` instead
- **What it enables**: Modify dividend calculations, add progressive taxation, or redirect dividends
- **Risk level**: Medium -- affects household income flow
- **Side effects**: Altering dividends changes household wealth and indirectly affects residential demand

### Candidate 3: `Game.Simulation.CompanyProfitabilitySystem.OnUpdate`

- **Signature**: `protected override void OnUpdate()`
- **Patch type**: Prefix/Postfix
- **What it enables**: Modify how profitability is calculated or add custom profitability factors
- **Risk level**: Medium -- profitability drives move-away decisions
- **Side effects**: Incorrect values can cause mass company exodus or permanent companies

### Candidate 4: `Game.Simulation.ExtractorCompanySystem.GetEffectiveConcentration`

- **Signature**: `static float GetEffectiveConcentration(ExtractorParameterData extractorParameters, MapFeature feature, float concentration)`
- **Patch type**: Postfix
- **What it enables**: Modify resource extraction efficiency. Could boost or nerf specific resource types.
- **Risk level**: Low -- static method, clear inputs/outputs
- **Side effects**: Changes extraction rates which affects supply chain downstream

### Candidate 5: `Game.Prefabs.ServiceCompany.Initialize`

- **Signature**: `override void Initialize(EntityManager entityManager, Entity entity)`
- **Patch type**: Postfix
- **What it enables**: Modify initial service company parameters (max service, worker density)
- **Risk level**: Low -- only runs during prefab initialization
- **Side effects**: Affects all service companies of that prefab type

## Mod Blueprint

- **Systems to create**: Custom `GameSystemBase` that reads company statistics and applies balance adjustments
- **Components to add**: Optional custom component for mod-specific tracking (e.g., `ModCompanyBonus`)
- **Patches needed**: `CompanyUtils.GetCompanyMoveAwayChance` for tax/bankruptcy tuning, `ServiceCompany.Initialize` for service parameter tweaks
- **Settings**: Tax sensitivity multiplier, bankruptcy threshold, dividend ratio, extraction rate multiplier
- **UI changes**: Optional info panel additions showing detailed company statistics

## Examples

### Example 1: Read Company Financial Statistics

Query all companies and log their financial health.

```csharp
public partial class CompanyMonitorSystem : GameSystemBase
{
    private EntityQuery _companyQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _companyQuery = GetEntityQuery(
            ComponentType.ReadOnly<CompanyData>(),
            ComponentType.ReadOnly<Profitability>(),
            ComponentType.ReadOnly<CompanyStatisticData>(),
            ComponentType.ReadOnly<PrefabRef>()
        );
    }

    protected override void OnUpdate()
    {
        NativeArray<Entity> entities = _companyQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            Profitability prof = EntityManager.GetComponentData<Profitability>(entity);
            CompanyStatisticData stats = EntityManager.GetComponentData<CompanyStatisticData>(entity);

            int profitRating = prof.m_Profitability - 127; // -127 to +128
            Log.Info($"Company {entity}: profit={stats.m_Profit}, " +
                     $"profitability={profitRating}, worth={stats.m_Worth}, " +
                     $"income={stats.m_Income}, wages={stats.m_WagePaid}, " +
                     $"tax={stats.m_TaxPaid}");
        }
        entities.Dispose();
    }
}
```

### Example 2: Patch Company Move-Away Chance

Reduce the tax-driven move-away probability by 50%.

```csharp
[HarmonyPatch(typeof(Game.Simulation.CompanyUtils), "GetCompanyMoveAwayChance")]
public static class PatchCompanyMoveAway
{
    public static void Postfix(ref int __result)
    {
        // Halve the move-away chance
        __result = __result / 2;
    }
}
```

### Example 3: Modify Service Company Initialization

Increase max service capacity for all service companies.

```csharp
[HarmonyPatch(typeof(Game.Prefabs.ServiceCompany), "Initialize")]
public static class PatchServiceCompanyInit
{
    public static void Postfix(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<ServiceCompanyData>(entity))
        {
            ServiceCompanyData data = entityManager.GetComponentData<ServiceCompanyData>(entity);
            data.m_MaxService = (int)(data.m_MaxService * 1.5f); // 50% more service capacity
            entityManager.SetComponentData(entity, data);
        }
    }
}
```

### Example 4: Find All Companies with Low Profitability

Query for companies that are struggling financially.

```csharp
public void FindStrugglingCompanies(EntityManager em)
{
    EntityQuery query = em.CreateEntityQuery(
        ComponentType.ReadOnly<CompanyData>(),
        ComponentType.ReadOnly<Profitability>(),
        ComponentType.ReadOnly<PropertyRenter>(),
        ComponentType.Exclude<Deleted>()
    );

    NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
    NativeArray<Profitability> profitabilities =
        query.ToComponentDataArray<Profitability>(Allocator.Temp);

    int struggling = 0;
    for (int i = 0; i < entities.Length; i++)
    {
        // m_Profitability < 64 means severely unprofitable (0=worst, 127=break-even)
        if (profitabilities[i].m_Profitability < 64)
        {
            struggling++;
            Log.Info($"Struggling company: {entities[i]}, " +
                     $"profitability={profitabilities[i].m_Profitability}");
        }
    }
    Log.Info($"Total struggling companies: {struggling} / {entities.Length}");

    entities.Dispose();
    profitabilities.Dispose();
    query.Dispose();
}
```

### Example 5: Boost Extractor Efficiency via Parameter Override

Modify the ExtractorParameterData singleton to increase resource extraction rates.

```csharp
public void BoostExtractorEfficiency(EntityManager em, float multiplier)
{
    EntityQuery paramQuery = em.CreateEntityQuery(
        ComponentType.ReadWrite<ExtractorParameterData>()
    );

    if (paramQuery.CalculateEntityCount() == 0) return;

    Entity paramEntity = paramQuery.GetSingletonEntity();
    ExtractorParameterData data = em.GetComponentData<ExtractorParameterData>(paramEntity);

    // Lower the "full" thresholds means reaching 100% efficiency at lower concentrations
    data.m_FullFertility /= multiplier;
    data.m_FullOre /= multiplier;
    data.m_FullOil /= multiplier;
    data.m_FullFish /= multiplier;

    em.SetComponentData(paramEntity, data);
    paramQuery.Dispose();
}
```

## Open Questions

- [ ] **EconomyUtils.kCompanyUpdatesPerDay value**: Referenced by multiple systems as the shared update rate. The exact numeric value is not directly visible in the decompiled system code -- it is likely defined in EconomyUtils as a static field.
- [ ] **EconomyParameterData.m_CompanyBankruptcyLimit**: The exact default value for the bankruptcy threshold is defined in EconomyParameterData (singleton). Needs decompilation of that type to determine the default.
- [ ] **EconomyParameterData.m_ProfitabilityRange**: The x/y range used by `GetCompanyProfitability()` to map profit to the 0-255 byte. Default values unknown.
- [ ] **Company creation from zones**: The exact system that creates company entities when a building spawns in a zone is not fully traced. `CompanyPrefab.GetArchetypeComponents()` defines the archetype, but the spawning trigger likely lives in ZoneSpawnSystem or a building AI system.
- [x] **Office company distinction**: Runtime-confirmed — `IndustrialProperty` and `OfficeProperty` always coexist on Tech Industry buildings. Both tags are present on the same entity; OfficeProperty alone does not imply a different archetype.
- [x] **TransportCompany on all companies**: Runtime-confirmed — `TransportCompany` is present on ALL company entities (commercial, industrial, office), not just logistics firms. Every company is potentially a transport operator.
- [x] **CompanyStatisticData fields**: Runtime-confirmed — 19 fields: `m_MaxNumberOfCustomers`, `m_MonthlyCustomerCount`, `m_MonthlyCostBuyingResources`, `m_CurrentNumberOfCustomers`, `m_CurrentCostOfBuyingResources`, `m_Income`, `m_Worth`, `m_Profit`, `m_WagePaid`, `m_RentPaid`, `m_ElectricityPaid`, `m_WaterPaid`, `m_SewagePaid`, `m_GarbagePaid`, `m_TaxPaid`, `m_CostBuyResource`, `m_LastUpdateWorth`, `m_LastFrameLowIncome`, `m_LastUpdateProduce`. Mean company profit in a 599K city was -107,020 (widespread economic stress).

## Sources

- Decompiled from: Game.dll -- Cities: Skylines II
- Components: Game.Companies.CompanyData, Profitability, CompanyStatisticData, CommercialCompany, IndustrialCompany, OfficeCompany, ExtractorCompany, ProcessingCompany, StorageCompany, TransportCompany, ServiceCompanyData, ServiceAvailable, BuyingCompany, CompanyNotifications, ResourceBuyer, ResourceSeller, ResourceExporter, TradeCost, CurrentTrading, WorkProvider, Workplaces, FreeWorkplaces, Employee, LodgingProvider, StorageLimitData, StorageTransfer, StorageTransferRequest, TransportCompanyData
- Systems: Game.Simulation.CompanyProfitabilitySystem, CompanyDividendSystem, CompanyMoveAwaySystem, CompanyUtils, ServiceCompanySystem, ExtractorCompanySystem, ProcessingCompanySystem, StorageCompanySystem, BuyingCompanySystem
- Prefabs: Game.Prefabs.CompanyPrefab, IndustrialProcessData, StorageCompanyData, ServiceCompany, CompanyNotificationParameterData, ExtractorParameterData, CommercialCompanyData, IndustrialCompanyData, ExtractorCompanyData, ProcessingCompanyData
