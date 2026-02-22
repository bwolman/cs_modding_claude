# Tourism and Visitors in Cities: Skylines II

## Where Tourists Come From

Tourists don't live in your city. They arrive from outside — spawning at your road, rail, air, and sea connections — visit attractions or check into hotels, spend money on local businesses and lodging, and then leave. Their entire lifecycle plays out in roughly one in-game day.

The flow of tourists into your city is governed by a single number: your city's attractiveness score. Every few game ticks, the simulation runs a probability check based on that score and the number of visitors already in the city. If the roll succeeds, a group of visitors appears at one of your outside connections and begins navigating into the city. If your attractiveness is low or your city is already crowded with tourists, the spawn probability drops and fewer new groups arrive.

This self-regulating mechanism means tourism naturally stabilizes around a target population that your attractiveness score implies. Getting more tourists is mostly a matter of raising that attractiveness score — adding hotels, building landmarks, and keeping your environment in good condition.

> **Info:** The game maintains a "target tourist count" derived from your attractiveness score. Below attractiveness 100, the target is attractiveness × 15. Above 100, each additional point yields diminishing returns: the formula shifts to a logarithmic curve (1500 + 100 × log10 of the excess above 100). When your current tourist population is under half the target, spawn rates are at full speed. As you approach the target, the spawn rate quadratically decays down to a low base rate equal to attractiveness divided by 1000.

---

## How Your City's Attractiveness Score Is Calculated

Your attractiveness score is not simply the sum of all the attractive buildings in your city. The game applies a sigmoid (S-curve) transformation to the raw sum of individual building attractiveness values, which has two important consequences.

First, your first few landmarks have a disproportionately large impact. Going from a small city with no attractions to a city with one or two signature buildings produces a dramatic jump in attractiveness. Second, the curve flattens as your score climbs. Adding a tenth landmark does less for your attractiveness than the first one did, even if the buildings are identical. There are genuine diminishing returns.

The raw attractiveness fed into that formula is the sum of every attraction-contributing building in your city, with each building's value squared before being divided by 10,000. The squaring means high-value buildings punch significantly above their weight — a single landmark with an attractiveness of 100 contributes four times as much raw input as two buildings of attractiveness 50. Concentrate your investment.

City modifiers from policies or progression can directly multiply the final attractiveness score on top of this formula.

> **Info:** The sigmoid formula is: city attractiveness = 200 ÷ (1 + e^(−0.3 × sum)) − 100, where "sum" is the total of (each building's effective attractiveness squared, divided by 10,000). The result ranges from −100 to +100, though in practice a well-developed city sits somewhere between 20 and 80.

---

## What Actually Contributes Attractiveness

Any building that has an attractiveness value defined in its prefab data contributes to your city's score. In practice, this means designated tourist attractions, landmark parks, signature buildings, and hotels. Ordinary residential and commercial buildings do not contribute.

Each building's effective attractiveness is not just its base value from the prefab. Four modifiers apply on top of it.

Upgrades stack onto the base value directly. A landmark building with additional upgrades installed — new wings, improved facilities, expanded grounds — accumulates attractiveness from each upgrade on top of the base.

Building efficiency scales everything down when city services are underperforming. A struggling attraction in a poorly serviced part of the city will contribute less to your attractiveness score than an identical attraction elsewhere. The one exception is signature landmark buildings, which are exempt from the efficiency penalty.

Park maintenance applies a separate multiplier. A park or attraction that your maintenance vehicles aren't reaching regularly will see its effective attractiveness reduced. The formula weights maintenance heavily at the low end: a fully maintained park operates at 100% of its attractiveness, while one that has dropped to 50% maintenance operates at 90% — the penalty accelerates as maintenance degrades further.

Terrain amplifies everything. A forest nearby, a waterfront location, or an elevated site all add percentage bonuses that multiply against the building's effective attractiveness. The game checks how close the nearest forest is, how deep the adjacent water is, and how high the building sits relative to its surroundings, and applies bonuses accordingly. Placing your landmark on a hilltop next to a lake will always outperform placing the same building in a flat urban block.

> **Info:** The terrain multiplier is additive: the formula is 1 + (0.01 × terrain attractiveness value). A building sited with terrain attractiveness of 20 operates at 120% of its otherwise-calculated effective attractiveness. Forests, shores, and height each contribute their own component to that terrain value, and they stack.

---

## How Tourists Find Hotels and Attractions

When a tourist group enters your city from an outside connection, it immediately needs a place to go. The group is flagged as "seeking lodging" and the pathfinder takes over, routing them toward a hotel or an attraction using public transit, taxis, and pedestrian movement.

The pathfinding phase has two outcomes. If the tourist finds a hotel with a free room, they check in — the room is marked occupied, they begin paying a nightly rate, and they're now based at that hotel for the rest of their stay. From the hotel they'll venture out to visit attractions and spend money at local businesses before their time is up and they leave.

If the tourist finds an attraction but no hotel is available, they'll visit the attraction instead and treat that as their anchor point. If the pathfinder finds nothing at all — no reachable hotel and no reachable attraction — the tourist group gives up and leaves the city immediately. They never count as a successful visitor.

If a tourist group arrives but no free hotel rooms are available and they can't find an attraction either, they simply depart. This is the primary capacity constraint in your tourism economy: more hotel capacity means more tourists can successfully stay, which means more spending and more return on your attractiveness investment.

> **Info:** Hotels compete for tourist bookings directly. When a tourist group arrives at a hotel that has already filled its last room (between when the pathfinder chose it and when the booking finalizes), the group is put back into the "seeking" queue and tries again. A city where hotels are chronically at 100% occupancy will lose some arriving tourists who give up after repeated failed booking attempts.

---

## Hotel Capacity and Pricing

A hotel's room count is determined by the size of the lot it occupies, the building's level, and a space multiplier defined by the building type. Larger lots and higher-level buildings hold more rooms. Upgrading a hotel — letting it grow to higher levels — is one of the most direct ways to expand your tourism capacity.

Hotels charge tourists a nightly rate calculated from the current market price of lodging as a resource. This means hotel prices fluctuate with your city's broader economy. When lodging resources are scarce or expensive, hotel rates rise; when they're abundant, rates fall. A tourist group will leave early if their funds run out before their intended departure time.

The hotel itself earns this money as revenue, which flows through the normal commercial economy. Hotel companies employ workers, pay upkeep, and depend on the lodging resource to operate — they're not free income. Underproviding the lodging resource chain that feeds hotels will hurt hotel profitability and indirectly affect your tourism economy's health.

> **Info:** Hotels charge their guests approximately 32 times per in-game day (roughly every 30 game minutes). Each charge is 1/32 of the daily lodging rate. A tourist who runs out of money before the day is 70% complete will be evicted and depart early; a tourist with no hotel who hasn't found lodging by 80% through the day also leaves, regardless of funds.

---

## Weather and Seasonal Tourism

Your city's climate directly affects how many tourists arrive. The game applies a weather multiplier to the spawn probability every cycle, which can boost or penalize tourist flow depending on current conditions.

Temperature has a two-sided effect. There is an ideal temperature range for tourism — when conditions are comfortable, a positive bonus is applied to the spawn rate. Push significantly outside that range in either direction (deep cold or intense heat) and the multiplier drops, reducing incoming tourist flow. The comfort window is not especially narrow, so mild seasonal variation won't crater your numbers, but extreme climates will.

Rain applies a penalty proportional to precipitation intensity. Storms are penalized most heavily. Snow applies a separate penalty. These effects are all multiplied against each other, so a cold rainy day is worse than either condition alone.

The total weather multiplier stays within a bounded range — the game won't completely shut off tourism due to weather, but a bad storm in a cold snap can push the effective spawn rate close to its floor. Cities in harsh climates will see noticeably more volatile tourist counts across the year.

> **Info:** The weather multiplier is capped between 0.5 and 1.5. Perfect weather in the ideal temperature range, with no precipitation, can deliver a 50% boost to tourist spawn rates. Extreme storms in temperatures well outside the comfortable range can reduce it to half the base rate. Weather affects the probability of new tourists arriving, not tourists who are already in the city.

---

## Transit Connections and Tourist Access

Tourists arrive at your outside connections — the road interchanges, train stations, airports, and harbor connections that link your city to the wider world. Once inside the city, they navigate using public transit, taxis, and their own feet.

This means your transit infrastructure is not optional for a healthy tourism economy. A city with excellent attractiveness but poor transit will see tourists fail to reach hotels and attractions from the outside connection where they spawned. Each failed pathfinding attempt costs you a visitor. Hotels and attractions located far from transit stops, or in neighborhoods with poor connectivity, will be chronically underbooked relative to equivalently positioned venues in well-connected areas.

Tourists use the same public transit network as your citizens. During peak tourist seasons, visitor demand can add meaningful load to transit lines near major attractions. Rail and air connections also matter for where tourists enter the city — the game weights outside connections when choosing a spawn point, and different connection types carry different weights for tourist spawning.

Because tourist pathfinding uses the full multimodal network, improvements that help citizens — better bus frequency, new metro lines, improved pedestrian paths — also help tourists reach their destinations. Tourism and local transit investment reinforce each other.

---

## Tourist Spending in the Local Economy

While staying in your city, tourists aren't just filling hotel rooms. They visit attractions and spend on local services throughout their stay. The game tracks a daily service consumption figure per tourist group, which represents spending at restaurants, shops, and entertainment venues near wherever they're staying or visiting.

This spending flows into the commercial economy the same way resident spending does. Businesses in tourist-heavy districts will see higher revenue, which supports higher rents, better commercial survival, and more tax income for your city.

Hotel spending is the most direct and quantifiable tourist revenue: room charges flow straight to the hotel company. But the broader service spending — the meals, the shopping, the entertainment — disperses across any commercial businesses that tourists can reach during their stay. Clusters of attractions surrounded by commercial development will see that commercial strip benefit from visitor spending, not just the attractions themselves.

> **Info:** Tourist groups consume lodging resources each day they stay, and also generate service demand at a separate daily rate. The service consumption represents their economic footprint beyond hotel costs. Both consumption rates are global parameters set by the game's configuration, not something players control directly.

---

## How Tourism Grows Over Time

Tourism is a compounding system. More attractive buildings raise your attractiveness score. A higher attractiveness score increases your target tourist population. More tourists filling hotels generates hotel revenue, which supports a viable hotel industry, which means more capacity for future tourists.

The feedback loop also runs in reverse. A city that lets its attractions fall into disrepair — through deferred maintenance, poor service coverage, or budget cuts — will see attractiveness drift downward. The target tourist count decreases, fewer visitors arrive, hotel occupancy drops, and hotel companies become less profitable. If hotels start failing financially, you lose capacity, which caps your tourist population below what your attractiveness would otherwise support.

Early tourism investment tends to have outsized returns because of where you are on the sigmoid curve. Getting from near-zero attractiveness to attractiveness 20 or 30 produces a large jump in target tourist population. Getting from 60 to 70 produces a smaller one. This is a reason to prioritize your first few landmark buildings highly rather than spreading attractiveness investment thinly across many small contributors.

Hotel capacity should roughly track your attractiveness growth. Building far more hotel capacity than your attractiveness score can fill leads to chronically low occupancy, which is economically wasteful. Building too little means arriving tourists can't book rooms, give up, and leave — wasting the spawn probability that your attractiveness earned.

---

## What Can Go Wrong

**No hotels means no tourism economy.** Tourists who can't find a room and can't reach an attraction leave immediately. If your city has strong attractiveness but no hotel capacity, your spawn probability generates visitors who all fail to stay and the tourism income never materializes. Hotels are the necessary infrastructure that converts attractiveness into revenue.

**Hotels fill to capacity and tourists bounce.** When every room in the city is occupied, arriving tourists try to book, fail, and may try several times before leaving. A city with 100% hotel occupancy is losing some fraction of its potential visitors. If you're seeing consistently maxed occupancy, the right response is more hotel capacity, not more attractiveness investment.

**Tourists can't get from the outside connection to their hotel.** Poor transit coverage, especially in the area around your outside connections, causes pathfinding failures. Tourists who spawn at a road interchange with no nearby transit stop may not be able to route to hotels on the other side of the city. Watch for hotels with persistently low occupancy despite city-wide demand — the issue may be connectivity, not capacity.

**Landmark buildings in bad locations underperform.** A major attraction placed on flat land with no trees, water, or elevation nearby will always score lower than the same building in a scenic location. The terrain bonus is permanent and invisible in the UI. If you're placing high-value tourist attractions, treat waterfront lots, hilltop sites, and forested locations as prime real estate.

**Deferred park maintenance quietly erodes your attractiveness.** Parks and outdoor attractions that aren't being reached by maintenance vehicles gradually lose both their coverage range and their attractiveness contribution. The degradation is gradual and silent — your city's attractiveness score will drift downward without any obvious alert, and you may not notice until tourist numbers have already declined.

**Harsh weather suppresses tourism without warning.** If your city is in a climate with extreme temperatures or frequent storms, your tourist spawn rate can drop to half its normal level during bad weather. A city that looks well-equipped for tourism in spring may see significant seasonal dips in winter. Building enough hotel capacity for your peak-weather tourist volume means that capacity sits idle during storms — plan your tourism targets around your average year, not your best days.

**Low-budget tourists run out of money.** Tourist groups that can't afford the hotel's daily rate will be evicted before their stay is complete and depart early. If your hotel market prices are high — due to lodging resource costs in your economy — some portion of arriving tourists will leave before generating their full economic contribution. This is a signal that your lodging resource supply chain may be inefficient, driving up hotel costs.
