# Land Value and Property in Cities: Skylines II

## The Invisible Force Shaping Your City

Every parcel of land in your city carries a hidden number that quietly determines which buildings can thrive there, how much residents and businesses pay to occupy them, and whether a neighborhood will gentrify or collapse. This is land value — and understanding it is the difference between a city that hums along and one that fills up with abandoned husks.

## What Raises Land Value

Land value is calculated street by street, then spread across a grid covering your entire map. Several factors push it up:

**Services** are the most direct lever you have. Health clinics, schools, and police stations all add a bonus to nearby land value when their coverage reaches an area. These bonuses are equal in weight, so a well-served neighborhood sees a compounding effect from stacking multiple service types.

**Transit access** has an outsized effect that surprises many players. A tram stop or subway station is worth ten times more to nearby land value than a bus stop. A bus stop itself is worth more than most other single factors. Transit-rich corridors naturally become high-value areas, which in turn attracts more development pressure.

**Nearby commercial activity** matters significantly too. An area with shops and restaurants nearby gets a substantial boost — roughly twice as much as a school or clinic provides per unit of coverage.

**Telecom** infrastructure (internet towers) adds a meaningful bonus as well, though it is often overlooked early in a playthrough.

**Natural terrain attractiveness** — hills, shorelines, scenic features the map provides — contributes a smaller ambient bonus regardless of what you build.

> ℹ️ **Land value bonuses by factor (relative weight):**
> - Tram or subway stop nearby: **50x** base weight (capped at +100)
> - Nearby commercial services: **10x**
> - Internet/telecom coverage: **20x**
> - Health, education, or police coverage: **2x** each
> - Bus stop access: **5x**
> - Terrain attractiveness: **3x**
>
> Each individual factor is capped at +100 before being added to the total.

## What Lowers Land Value

Pollution works in the opposite direction and can completely undermine an otherwise well-served neighborhood.

**Ground pollution** is the most punishing. Industrial zones, certain resource extraction buildings, and contaminated land create ground pollution that carves deeply into land value. It takes far longer to dissipate than it took to accumulate, so industrial sprawl near residential areas causes lasting damage.

**Air pollution** has a moderate effect. **Noise pollution** from busy roads and rail lines has a much smaller numerical weight, though it still pushes value downward.

> ℹ️ **Pollution penalty weights:**
> - Ground pollution: **10x** (strongest penalty)
> - Air pollution: **0.1x**
> - Noise pollution: **0.01x**
>
> Ground pollution is 100x more damaging to land value than air pollution. Separating industrial areas from residential is not just aesthetic — it is economically essential.

Land value does not change instantly. It drifts toward its target gradually, so a newly-built clinic takes time to register its full effect, and cleaning up pollution takes equally long to recover.

## How Land Value Sets Rent

Once land value is established for a street edge, the game calculates what renters — households and businesses alike — pay to occupy buildings there. The formula takes the land value at the road edge, multiplies it by a zone-specific modifier, adds a base rate that scales with building level, and adjusts for the size of the building and how many tenants share the space.

> ℹ️ **Land value modifiers by zone type:**
> - Commercial buildings: **0.70x** land value modifier, **3.0** base rent rate
> - Industrial buildings: **0.50x** land value modifier, **0.8** base rent rate
> - Residential buildings: **0.35x** land value modifier, **0.5** base rent rate
>
> Commercial rent is roughly six times the residential base rate. In mixed-use buildings, companies pay **30%** of total building rent.

Every sixteen times per game-day, the game compares each renter's rent against their income. If rent exceeds what a household or company earns, the renter starts looking for somewhere cheaper and will eventually move out if nothing improves.

## Building Levels and What They Require

Buildings in CS2 have levels ranging from 1 to 5. Higher-level buildings are larger, house more residents or businesses, and generate more economic activity — but they also come with higher rent and upkeep demands.

A building does not level up simply because land value is high. It accumulates a running condition score over time. When renters can afford the building's upkeep costs, condition climbs. When they cannot, it falls. Once condition crosses a positive threshold, the building requests a delivery of specific construction materials, and only after those materials arrive does it actually upgrade — visibly reconstructing itself.

The rate of condition change scales exponentially with building level. High-level buildings in wealthy, well-served areas climb quickly. But the same exponential scaling applies to decline: a high-level building that loses its renters or sits in a deteriorating area falls into abandonment faster than a small starter building would.

> ℹ️ **Upkeep cost scaling by zone type (level exponents):**
> - Residential: **1.05x** per level (nearly flat — upkeep barely grows)
> - Commercial: **2.1x** per level (doubles more than twice per level)
> - Industrial: **2.0x** per level (doubles per level)
>
> High-level commercial buildings have dramatically higher upkeep requirements than residential ones. A level-4 commercial strip requires roughly 16x the upkeep of a level-1 building of the same type.

## How High Land Value Displaces Lower-Income Residents

As land value rises — driven by your transit investments, new parks, schools, and commercial development — rent rises with it. Households that could comfortably afford rent in an area years ago may now find their rent exceeding their income.

When that happens, affected residents enable a property search and begin hunting for cheaper housing elsewhere in the city. If they cannot find it, they eventually leave the city entirely, reducing your population and potentially your tax base. The mechanics are identical to gentrification: investment raises land value, land value raises rent, low-income residents are priced out.

The only direct counter-pressure is distance. Lower-income households can still find affordable housing in areas with lower land value — neighborhoods further from transit, with less service coverage, or near industrial zones.

## What Can Go Wrong

**Mass abandonment spirals.** If a neighborhood's economy softens — fewer jobs, lower household incomes — buildings start falling behind on upkeep. Condition drops. When condition hits the abandonment threshold, the building is fully abandoned: utilities are cut, all renters are evicted, crime production doubles, and the building sits derelict until you intervene. Abandoned buildings do not automatically recover. Left unchecked, one abandonment raises area crime, which reduces attractiveness, which lowers land value, which makes it harder for neighboring buildings to sustain upkeep — a cascading spiral.

**High-rent warning icons.** When more than 70% of renters in a building are paying more than they earn, the game shows a high rent warning icon. This is an early signal that the area is economically stressed and outmigration is likely.

**Industrial pollution killing residential investment.** Because ground pollution carries a 10x penalty weight, a single industrial zone placed too close to a residential area can wipe out all the gains from a nearby clinic and transit stop combined. Pollution persists on the land long after the source is removed.

**Over-investing in transit without housing supply.** Subway stations dramatically boost nearby land value. If you build a new subway line through a working-class neighborhood without also building higher-density housing to absorb demand, the land value spike will raise rents faster than incomes can adjust, displacing residents who had been stably housed.

**Signature buildings are exempt.** Landmark and signature buildings never abandon, regardless of upkeep shortfalls or economic conditions. Do not expect them to behave like normal buildings when diagnosing economic problems in an area.
