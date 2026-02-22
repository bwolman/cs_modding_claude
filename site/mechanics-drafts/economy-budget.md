# City Economy and Budget

## How Your City Makes Money

Your city draws income from two main sources: taxes collected from residents and businesses, and fees charged for services. A third source — government subsidies — acts as a safety net when finances deteriorate.

**Taxes** are the backbone of your budget. Every household and every company in your city accumulates income as the simulation runs, and a portion of that income is regularly swept into your treasury. The tax rate you set in the Economy panel applies across the board, but you can fine-tune it by sector: residential, commercial, industrial, and office zones each have their own offset, and residential taxes can be further adjusted by education level. Commercial and industrial taxes can even be dialed in per resource type — you might tax electronics businesses differently than food producers.

**Service fees** are charges that residents and businesses pay when they consume city services. Healthcare visits, school attendance, electricity use, water consumption, garbage pickup, parking, and public transit all carry fees that you set. Fees flow into the budget separately from taxes, and they also affect behavior: high electricity or water fees dampen consumption and reduce citizen happiness.

**Government subsidies** are a conditional lifeline. The subsidy only kicks in on game modes that enable it, and it scales based on how badly your finances have slipped. The deeper your treasury falls below a threshold, the larger the subsidy becomes, up to a cap expressed as a percentage of your total expenses. Above the threshold, you receive nothing.

> **Info**: Your city can also earn money by exporting surplus electricity or water to outside connections. If your power grid or water network produces more than local demand, the excess is sold and appears as export revenue in your budget.

---

## What Your City Spends

Expenses fall into several categories, and understanding each one matters for long-term solvency.

**Service upkeep** is usually your largest expense. Every building — from a small park to a hospital — costs money each day simply to keep running. This cost scales with building level, and higher-level commercial and industrial buildings carry disproportionately steeper upkeep than residential ones. The service budget slider in the city services panel lets you underfund services to save money, but there is a direct trade-off: a service running at 80% budget operates less efficiently. Even inactive buildings do not escape entirely — they still pay 10% of their normal upkeep.

**Wages for city service workers** are embedded in service upkeep costs. The game uses fixed base wages by education level — uneducated workers earn the least, highly educated workers earn the most — and city service jobs apply a multiplier to those base rates. A city that relies heavily on educated staff for its hospitals and universities pays more in upkeep than one running basic services.

**Map tile upkeep** grows as you unlock territory. Each new tile you claim adds an ongoing cost, and that cost scales non-linearly — early tiles are cheap, but pushing far from your starting area becomes increasingly expensive.

**Import costs** arise when your city cannot meet its own needs. If you do not generate enough electricity or water locally, the simulation automatically imports from outside connections and charges you for it. The same applies to city services like fire, police, ambulance, and garbage: if outside connections handle calls that your own services cannot reach, your city is billed.

**Loan interest** is a daily expense tied to how much you have borrowed relative to your creditworthiness.

> **Info**: Wages are paid by companies and households, not directly from your treasury — they affect your budget indirectly by reducing the income those entities have available to pay taxes.

---

## Tax Rates and Behavior

Tax rates are not a simple dial you can crank up for easy money. Residents and businesses respond to taxation.

High residential taxes reduce household disposable income. Households with less money spend less at commercial businesses, reducing those companies' revenues and therefore their taxable income. Push rates too high and you can create a feedback loop where the very taxes meant to raise revenue shrink the economic activity that generates it.

High commercial and industrial taxes work similarly. Companies earn less net income, become less profitable, and in extreme cases fail entirely. When a company goes bankrupt, it disappears, taking its jobs, its tax payments, and the commercial foot traffic it attracted with it. The result is lower residential income as workers become unemployed, compounding the revenue loss.

On the flip side, cutting taxes below a certain point means the game actually subsidizes those sectors — tax revenue for that area turns negative and appears as an expense in your budget.

> **Info**: Tax rates are stored as a base rate plus per-sector offsets. The effective rate for any taxpayer is the sum of the relevant base and offset values. There are hard limits on how high or low each rate can go, governed by game parameters that vary by sector and education level.

---

## How Loans Work

The loan system provides a cash advance against your city's creditworthiness. Your creditworthiness sets the ceiling on how much you can borrow; it is determined by factors like population size and economic health.

When you take a loan, the principal lands immediately in your treasury. From that point on, you pay interest every day. The interest rate is not fixed — it rises as your outstanding loan approaches your creditworthiness limit. A small loan against a high creditworthiness limit costs relatively little; a loan that maxes out your limit is penalized with a much higher rate.

> **Info**: Interest rate = lerp(minimum rate, maximum rate, loan amount / creditworthiness). The minimum and maximum rates are set by game parameters. City modifiers from policies or specializations can reduce the effective rate.

Repaying a loan deducts the principal from your current balance immediately. There is no amortization schedule — you choose when to repay and how much, and you can carry a balance indefinitely as long as you can afford the daily interest.

---

## What Can Go Wrong

**The bankruptcy spiral** is the classic way cities fail financially, and it is self-reinforcing once it starts. It often begins with overbuilding services before the tax base can support them. Upkeep costs exceed tax revenue, creating a daily deficit. The mayor takes a loan to cover the gap, which adds interest expense, widening the deficit further. To compensate, tax rates go up. Higher taxes reduce company profitability; some businesses close. Unemployment rises, household incomes fall, and residential tax revenue drops.

**Specific traps to watch for:**

- **Unlocking too many map tiles too quickly** — each new tile adds fixed upkeep before population catches up
- **Heavily leveled commercial districts** with exponentially scaling upkeep
- **High-salary services before the workforce justifies the cost** — hospitals and universities cost more if you need educated staff
- **Extended reliance on imported electricity or water** — the import bill can rival the cost of building your own infrastructure
- **Service fees set too high** — consumption drops, citizens become unhappy, population declines, and the residential tax base shrinks
- **Borrowing to fund operating expenses** rather than capital investment — interest compounds against a budget that never improves

The warning sign is a widening gap between total income and total expenses on the Economy panel. A small negative balance is manageable; a large and growing one that forces further borrowing is the beginning of a spiral that gets harder to reverse the longer it runs.
