# Companies and Employment: How the Economy Actually Works

Every commercial strip, factory district, and office park in your city is filled with simulated companies that hire workers, produce goods or services, pay bills, and can go out of business. Understanding how this machinery runs helps you build cities that thrive rather than silently bleed out.

## How Companies Get Workers

When a building opens for business, the game immediately calculates how many workers it needs and what education levels those workers should have. A small farm needs mostly uneducated laborers. A software office needs mostly well-educated and highly-educated staff. A retail shop lands somewhere in between.

Every company has a fixed number of job slots divided across five education tiers: uneducated, poorly educated, educated, well-educated, and highly educated. The split depends on two things: the type of business (its "complexity") and the building's current level.

> **Info:** There are four complexity tiers. Manual businesses (farms, mines) fill roughly 75% of slots with uneducated workers and 25% with poorly educated workers. Simple businesses (basic industry) spread workers across the lower tiers. Complex businesses (offices, services) favor educated and well-educated. Hitech businesses (software, research) fill about 75% of slots with highly educated workers and 25% with well-educated workers. As a building levels up, its slot distribution shifts toward higher education tiers.

Citizens looking for work each have an education level of their own. The job-seeking system tries to match them to an open slot at their education level. If no slot at their level is available, the system looks at slots one tier lower, then two tiers lower — a university graduate can take a coffee shop job if nothing better is open. Workers are assigned to one of three shifts — day, evening, or night — based on the employer's configuration.

## What Determines How Much Gets Done

Education is not just a hiring filter — it directly determines how much productive work each employee contributes. A highly-educated worker produces roughly five times the output of an uneducated worker at the same happiness level.

Worker happiness also matters. Happy workers produce more. The formula combines education and happiness multiplicatively, so a well-educated but miserable workforce is noticeably less productive than a well-educated and content one.

If a company is understaffed, its building takes an efficiency penalty that ramps up gradually the longer the shortage persists. A brief hiring gap hurts less than a sustained one.

## How Companies Make a Profit (or Don't)

Once a company is staffed and producing, it runs a continuous financial cycle. Revenue flows in from customers buying goods or services. Costs flow out in the form of wages, rent on the building, electricity, water, sewage, garbage collection, taxes, and the price of any raw materials or intermediate goods the company needs to keep operating.

> **Info:** The game tracks a "profitability rating" from 0 to 255 for every company, where 127 means exactly breaking even. The rating is calculated once per in-game day by comparing the company's total worth now to its total worth the previous day. Profitable industrial and office companies distribute a daily dividend to the households of their employees — a company's current money divided by eight times the number of employees — so company profits filter down into residential household income.

When a company is profitable, dividends represent a direct cash transfer from the company to its workers' households every in-game day. This means a thriving industrial district does more than pay wages — it enriches the households of everyone working there, raising their purchasing power at nearby shops and ultimately increasing residential tax revenue. The chain runs in reverse as well: companies under financial stress stop paying dividends before they go bankrupt, squeezing household income as an early warning signal before any building actually closes.

## The Supply Chain: Inputs, Outputs, and Warehouses

Industrial companies produce physical goods as output. Those goods need to go somewhere: either to a warehouse for storage, to a commercial building to be sold to customers, or to an outside connection for export. A factory sitting idle because no one is collecting its output will underperform just as surely as one that cannot get raw materials.

Commercial buildings — shops, restaurants, markets — do not produce goods. They receive goods from industry or warehouses and sell them to citizens. A store with empty shelves cannot generate revenue. Before your commercial district can function, it needs a working supply of goods flowing in from somewhere.

Warehouses act as buffers and distributors between producers and retailers. They receive goods from nearby industrial companies and then dispatch their own delivery trucks outward to commercial buildings that need restocking. A city without warehouses forces direct industry-to-retail delivery over whatever distance separates them, which becomes increasingly inefficient as your city grows. Warehouses absorb surplus production, smooth out timing mismatches, and give commercial zones a reliable local source instead of depending on trucks crossing the whole city.

Outside connections — highway interchanges, rail freight depots, ports, and airports — allow your city to import goods it does not produce locally and to export surplus production for revenue. Import costs appear as an expense in your budget; export revenue appears as income. A city that imports most of its commercial goods is paying that cost continuously, and it will not be obvious unless you check the trade section of the Economy panel.

> **Info:** Goods move between producers, warehouses, and retailers via delivery trucks, not citizen vehicles. Delivery trucks follow the same road network as everything else. A congested arterial road between your industrial district and your commercial zone is a bottleneck in your supply chain, not just a traffic nuisance. If trucks cannot complete deliveries on time, companies run out of inputs and production halts even when the goods exist somewhere in your city.

## Commercial Buildings: Customers and Foot Traffic

Commercial buildings operate differently from industrial or office companies. Their revenue does not come from producing output — it comes entirely from how many citizens visit them as customers and what those customers spend.

Citizens choose commercial destinations based on proximity, available goods, and price. A shop near dense residential areas naturally draws more foot traffic than the same shop placed far from where people live. Commercial zones placed in isolated locations or connected only by roads that citizens avoid will struggle to generate consistent revenue regardless of how well-stocked they are.

The no-customers warning over a commercial building means the surrounding population is too sparse, too far away, or already well served by other nearby shops. Building commercial zones ahead of residential density — or laying out retail corridors that are car-dependent and hard for pedestrians to reach — results in businesses that are perpetually underperforming despite being fully stocked.

> **Info:** Commercial buildings are also sensitive to land value. Higher land value areas attract wealthier customers who spend more per visit, but the building itself also pays higher rent to reflect the prime location. The same shop in a high-value district may earn more gross revenue than in a low-value area while simultaneously carrying higher costs. Both the revenue gain and the cost increase are real — land value helps only if the revenue improvement outpaces the rent increase.

## When There Are Not Enough Workers or Customers

Two warning icons can appear over a business: a worker-shortage icon and a customer-shortage icon.

For worker shortages, the game tracks separately whether the unfilled slots are low-education or high-education. An educated-worker shortage is treated as more serious: it adds a 20% chance per check that the company will decide to leave the city. An uneducated-worker shortage adds 5%.

For customer shortages, the game watches the ratio of service stock that is sitting unused relative to capacity. When a commercial building has been stocked with goods but nobody is buying, it displays the no-customers warning.

> **Info:** When a company has no raw materials or input goods to process, it cannot produce anything and the building's efficiency drops to zero for resource-related output. Companies that cannot acquire inputs because their suppliers are too far away or congestion prevents delivery trucks from arriving will quietly underperform without necessarily going bankrupt immediately.

When the city-wide supply of educated jobs exceeds the number of educated residents who can fill them, the game automatically begins spawning commuter households from outside connections. These commuters travel in from off-map to take the surplus positions.

## When Companies Close Down

A company can exit the city in two ways: it can choose to leave, or it can go bankrupt.

The voluntary move-away check runs sixteen times per in-game day. The probability is driven primarily by the tax rate — 10% tax is the neutral point, and every percentage point above that adds meaningfully to the chance of leaving. Worker shortages stack on top. A high-tax city with chronic educated-worker shortages will hemorrhage businesses over time.

Bankruptcy follows a different path. The game tracks each company's total worth (cash plus inventory value). If that total falls below a hard minimum and stays there for roughly four in-game days, the company is marked for closure regardless of other factors.

> **Info:** The bankruptcy timer is not reset by a brief recovery. A company must sustain financial health above the threshold to clear the danger window. A company that bounces just above and then dips back below the threshold will keep restarting its four-day countdown.

## What Can Go Wrong

**Your city has jobs but no workers to fill them.** This usually means your residential zones are not producing enough citizens at the required education levels. Check whether school and university capacity matches your workforce needs, and whether graduates are staying in the city rather than leaving due to housing or happiness problems.

**Businesses keep leaving despite low taxes.** Look at the educated-worker shortage warning specifically. Even moderate tax rates become secondary to a chronic shortage of university-educated residents. Building more universities and ensuring affordable housing for young adults stabilizes this faster than tax cuts.

**Factories are not producing anything.** If the no-inputs icon appears, the supply chain has broken down upstream. Either the source material does not exist in your city or region, delivery trucks cannot navigate the road network efficiently enough, or a warehouse that should be redistributing goods is full and not dispatching.

**Commercial zones are full of goods but have no customers.** Either the residential population is too small to generate foot traffic, or the zoning layout puts shops too far from where people live and leisure. It can also appear temporarily after a city expansion when commercial zones grow faster than the population that would patronize them.

**Companies are going bankrupt even with customers and workers.** Check the full cost picture: rent on large buildings, utility bills, and the cost of buying input resources all drain company funds before wages and taxes. High land values inflate rent. Poor utility coverage or high service fees add costs silently. A company paying too much for its building in a high-value district can fail even with strong sales.

**Supply chain broken by road congestion.** Goods can exist in your industrial district and still never reach your commercial zone if delivery trucks are caught in gridlock. Unlike a simple traffic problem, this manifests as commercial buildings going idle and eventually bankrupt while nearby factories show full inventory. Dedicated freight routes, separated industrial road networks, or warehouses placed closer to retail districts all help move goods through a congested city.

**Silent import dependency draining the budget.** If your commercial sector relies on imported goods because local industry is not producing enough, every sale at every shop in your city is coming at a cost your budget is absorbing quietly. The drain is easy to miss because it does not appear as a single line item. Check the trade balance in the Economy panel: if imports consistently outrun exports, your commercial sector is profitable on paper but costing the city more than it earns.
