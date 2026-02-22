# Parks and Recreation in Cities: Skylines II

## The Leisure Counter: Why Citizens Need to Have Fun

Every citizen in your city carries an invisible "leisure counter" — a measure of how recently they've had meaningful downtime. Over time, this counter slowly ticks downward, and when it falls low enough, the citizen stops whatever else they're doing and actively seeks out something to do for fun. When they find it and spend time there, the counter fills back up toward its maximum and they return to their normal routine.

This isn't just flavor. A population that can't satisfy its leisure needs is an unhappy population, and unhappiness cascades into reduced productivity, lower land values, and slower city growth. Parks, beaches, restaurants, and entertainment venues are the infrastructure that keeps your citizens' leisure counters topped up.

> **Info:** The leisure counter runs from 0 to 255. A citizen actively starts seeking leisure when the counter drops low. Upon spending time at a leisure provider, the counter increases by an amount based on how "efficient" that provider is — higher-quality venues fill the counter faster. A citizen stops their leisure outing once the counter is nearly full or their work/school deadline arrives, whichever comes first.

---

## How Citizens Choose Where to Go

When a citizen decides it's time for leisure, the game doesn't just send them to the nearest park. Instead, it runs a weighted lottery across ten different leisure categories: dining out, entertainment venues, shopping, indoor city facilities like museums and libraries, traveling outside the city, parks, beaches, tourist attractions, relaxation, and sightseeing.

Each category gets a weight based on three things about that citizen: their age, their wealth, and the current weather.

**Age shapes preferences significantly.** Children are much more likely to head to a park than an elderly resident. Teens are drawn to entertainment. Adults spread their preferences more evenly. The elderly lean toward dining and indoor activities.

**Wealth gates certain activities.** Parks and beaches are free, so every income level has equal access to them. Dining out requires a bit of disposable income, entertainment requires more, and travel outside the city is weighted very heavily toward wealthier households.

**Weather matters for outdoor activities.** Rain dramatically suppresses park visits — the weight for park-going drops by up to 95% in bad weather, effectively meaning rainy days push citizens toward indoor options. Beaches are even stricter: the game won't send citizens to the beach unless the temperature is above 20°C and the weather is clear. Cold but fine days might push citizens toward travel instead, as that leisure type actually prefers cooler conditions.

Once a category is chosen, the citizen pathfinds to the nearest available provider of that type. If you have no parks, citizens who "rolled" park as their destination will struggle to satisfy that need.

> **Info:** The wealth threshold uses a smooth curve, not a hard cutoff. A household with moderate savings has partial access to higher-cost leisure types, while truly poor households are effectively excluded from dining, entertainment, and travel. Parks and beaches always have a wealth minimum of zero — they're genuinely universal.

---

## What Coverage Radius Actually Means

When you place a park, it broadcasts a service coverage area — the radius within which nearby residents count the park as "accessible." This is what the coverage overlay shows on your map.

That radius is not fixed. It shrinks when your park falls into disrepair.

Parks degrade continuously, at a rate proportional to how many buildings share the park grounds. The game dispatches park maintenance vehicles to service them, and when a vehicle arrives and does its job, the maintenance level is restored. If maintenance vehicles can't reach the park — because of road layout problems, insufficient vehicles, or budget cuts — the maintenance level drops, and with it, both the park's coverage range and its coverage magnitude shrink.

> **Info:** Park coverage uses a tiered system based on maintenance level expressed as a ratio (current maintenance divided by maximum). At full maintenance you get full coverage range and magnitude. Below roughly 30% of maximum, coverage range is only 95% of its designed value and magnitude is similarly reduced. The city modifier "Park Entertainment" — unlocked through policy or progression — applies a further multiplier on top of this.

---

## Types of Leisure Providers

Not every leisure building works the same way. Your city has access to several distinct categories, each satisfying different leisure needs for different citizens under different conditions.

**Parks (small and large)** are the universal outdoor leisure option. They're free to visit, accessible to all age groups and wealth levels, and their coverage radiates outward from the park building itself. A park in the middle of a residential neighborhood satisfies the park leisure category for every citizen within walking distance, regardless of how much money those citizens have. Their one hard constraint is weather — rain drops park demand by up to 95%, and below-freezing temperatures suppress it further.

**Plazas and squares** function similarly to parks but typically cover a smaller radius. They're most useful for filling gaps in dense districts where a full park doesn't fit spatially. A plaza won't replace a park in a large neighborhood, but it can prevent a small pocket of residents from going entirely without green-space access.

**Sports facilities** — fields, courts, and gyms — satisfy the entertainment and activity leisure category rather than the general park category. They tend to be preferred by younger citizens, particularly teens and young adults. Outdoor sports fields are subject to the same weather suppression as parks. Indoor gyms are not — a gym functions as a leisure provider regardless of rain or temperature, which makes them disproportionately valuable in cold or wet climates.

**Cultural venues** — museums, libraries, and theaters — satisfy the indoor city facilities leisure category. These buildings are critical infrastructure, not decorative choices. During rain and winter, when outdoor leisure demand collapses, cultural venues absorb the shift in citizen preferences. A city with a strong network of museums and libraries maintains steady leisure satisfaction year-round. Cultural venues also contribute to city attractiveness, which feeds into tourism demand.

**Restaurants and cafes** are commercial buildings, but the game treats them as leisure providers for the dining category. Citizens who roll dining as their leisure choice will pathfind to a restaurant and spend time and money there. This means a healthy commercial district doubles as recreational infrastructure — your downtown isn't just generating tax revenue, it's also keeping your wealthier residents' leisure counters full. Citizens who roll dining but cannot find a restaurant within pathfinding range go unsatisfied, so commercial gaps in a neighborhood translate directly to leisure gaps for residents with disposable income.

**Tourist attractions and landmark buildings** satisfy the sightseeing and tourist-attraction leisure categories. These serve both local citizens and visitors from outside your city. Their attractiveness score feeds directly into tourism demand — a city with no landmarks will never generate meaningful hotel occupancy or tourism revenue. Unlike parks, their value scales significantly with placement: a landmark near transit, in a dense area with good pedestrian access, reaches far more people than one placed on the edge of the map.

---

## Seasonal Leisure Patterns

Your city's leisure infrastructure needs to function in all seasons, and the seasons create very different demands.

**Summer is peak outdoor leisure season.** Parks, beaches, sports fields, and outdoor entertainment all see their highest demand. If your city has coastline, beachfront neighborhoods become leisure magnets for the whole city during warm months — citizens will travel significant distances to reach beach access when temperatures are right. A well-positioned beach can carry an enormous share of citywide leisure demand in summer.

**Winter suppresses outdoor leisure sharply.** Citizens shift heavily toward indoor options: dining out, entertainment venues, cultural facilities. A city that has invested primarily in parks will see widespread leisure dissatisfaction from autumn through early spring, as the weather makes the parks effectively inaccessible to the majority of citizens who would otherwise use them.

This creates a real infrastructure planning challenge. Outdoor parks that handle 80% of your summer leisure demand need to be complemented by indoor venues that cover the winter gap. The right ratio depends on your climate zone — a consistently sunny map needs fewer indoor venues than one where rain or cold are common. Cities that over-invest in parks relative to indoor options will have happiness that cycles with the seasons: high in summer, struggling in winter, in a pattern that's difficult to smooth out without adding indoor capacity.

**Rain within any season can trigger the same shift.** Even in summer, a rainy week will push citizens heavily toward indoor leisure. A city in a rainy or temperate climate zone needs more indoor capacity than one in an arid or sunny zone. Check your climate conditions when planning leisure infrastructure — the right mix differs meaningfully by map.

---

## Park Attractiveness and the Environment Around It

Every park and attraction building has an attractiveness score that affects both how desirable your city is to visitors and — indirectly — property values in the surrounding area. This score is not purely about the building itself. The terrain around the park matters.

Forests nearby contribute a bonus proportional to how dense the tree coverage is and how close it is. Water bodies — coastlines, rivers, lakes — add a shore bonus if the water is deep enough to read as genuine waterfront. Elevated terrain adds a height bonus for parks situated above the surrounding city.

All of these terrain bonuses multiply against the park's base attractiveness. A park in a clearing with no water or trees nearby will score much lower than an identical park on a hilltop overlooking a lake.

Building efficiency matters too. A park running at reduced efficiency — perhaps because you've underfunded city services — sees its attractiveness cut accordingly. Only signature landmark buildings are exempt from this efficiency penalty.

> **Info:** The maintenance-to-attractiveness relationship is a direct multiplier: a park at 80% maintenance operates at 96% of its maximum attractiveness (calculated as 0.8 + 0.2 × maintenance ratio). At 50% maintenance, attractiveness drops to 90% of maximum. Letting parks fall to zero maintenance cuts attractiveness to 80% of the designed value regardless of terrain bonuses.

Parks and attractions also influence the city-level attractiveness score, which affects how many tourists want to visit and how desirable your city appears to residents considering moving in. A well-parked residential neighborhood will have higher land value and attract wealthier residents than an identical neighborhood without green space. Tourist-oriented landmarks specifically feed into tourism demand — the more unique attractions your city offers, the more it can sustain a hotel and hospitality industry. These effects stack: a city with diverse, well-maintained parks and cultural venues will outperform an equally populous city that neglected its recreational infrastructure, in both tourism revenue and residential desirability.

---

## Leisure, Health, and Wellbeing

Leisure satisfaction feeds directly into citizen wellbeing, which in turn affects health outcomes, workplace productivity, and residential desirability. Citizens who chronically cannot satisfy their leisure needs — because there are no nearby providers, because all the parks are in poor condition, or because they're too poor to access paid venues — experience sustained wellbeing penalties.

Parks specifically serve as the most universally accessible leisure option. They're free, weather permitting, and weighted toward a broad age range. A neighborhood without walkable park access will see its residents defaulting heavily to commercial or dining options, which costs those households money and excludes lower-income residents entirely.

Beaches, when your map has coastline, are the most powerful summer leisure option available to residents but require both good weather and warm temperatures to activate. Cities built around coastal access can see dramatic seasonal leisure patterns — beachfront neighborhoods become leisure magnets in summer and quiet in winter.

---

## What Can Go Wrong

**Parks fall into disrepair silently.** If you don't watch your park maintenance vehicle coverage, parks degrade continuously. The coverage overlay still shows the designed radius, but actual effective coverage quietly shrinks. Citizens in the "covered" area may not be benefiting as much as you think.

**Rain kills outdoor leisure demand.** A city with only parks and beaches as its leisure infrastructure will see citizen wellbeing crater during rainy seasons. You need indoor options — museums, libraries, entertainment venues — to absorb demand when the weather turns bad.

**Wealth segregation in leisure is real.** Low-income neighborhoods that lack parks will see their residents effectively leisure-deprived. Restaurants and entertainment are partially gated behind income, so placing paid venues in a low-income district doesn't fully solve the problem.

**No park nearby means the lottery fails.** When a citizen "rolls" park as their desired activity but no park is within a reasonable pathing distance, the leisure need goes unmet. The citizen doesn't automatically re-roll to a different activity type — they simply fail to satisfy that need until the next leisure cycle. Dense neighborhoods with no green space can build up sustained leisure deficits.

**Terrain attractiveness is permanent and invisible.** Placing a park on flat, treeless land in the center of the city will always underperform the same park placed near a forest or waterfront. If you're trying to maximize a park's contribution to your city's attractiveness score, location matters as much as the building choice itself.

**Winter leisure deficit from an outdoor-only strategy.** A city built around parks, beaches, and sports fields will see happiness drop every autumn and stay low until spring. Citizens shift to indoor leisure categories in cold and rainy weather, and if there are no museums, theaters, or other indoor venues to absorb that demand, the leisure need goes unmet at scale. Adding indoor cultural and entertainment venues is the only reliable fix — you cannot park-build your way out of a seasonal happiness cycle.

**Landmark parks placed far from residential density serve almost no one.** Citizens pathfind by proximity. A large unique park on the edge of your city, surrounded by industrial zones or empty land, will draw almost no visitors — not because it's unattractive, but because no one lives nearby to use it. Landmark recreational buildings need residential density within reasonable walking or transit distance to actually function as leisure providers. Placing one far from where people live wastes its coverage and its contribution to city attractiveness.
