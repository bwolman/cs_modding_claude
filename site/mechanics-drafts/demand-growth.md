# Demand and City Growth in Cities: Skylines II

## The Three Bars and What They Actually Mean

The demand bars for residential, commercial, and industrial zones are more nuanced than they appear. Each bar is actually the visible face of two separate values running underneath: an *abstract demand score* (how much the city theoretically needs more of this type) and a *building demand value* (whether the game will actually place new buildings). You can have a sky-high abstract demand score and still see nothing being built, because those two values are not the same thing.

The abstract demand score for residential ranges from 0 to 200. For commercial and industrial it runs from 0 to 100. The building demand that triggers actual construction is always 0 to 100, and it is this second number that ZoneSpawnSystem reads when deciding whether to place a new building on your zoned land.

## What Drives Residential Demand

Residential demand is calculated per density tier — low, medium, and high density each have their own scorecard.

Several factors push the score upward:

- **New city bonus.** Brand-new cities receive a significant automatic boost that gradually fades as population approaches 20,000. This is why early growth can feel fast even before you have much infrastructure.
- **Happiness above neutral.** The game treats 50 as the baseline happiness score. Every point above 50 adds to demand, scaled by a weight of 2.0. Unhappy cities can push the score down.
- **Available jobs.** If there are more open workplaces than the city needs, people want to move in. The game targets roughly 10% free workplaces as neutral; anything beyond that is a positive signal.
- **Low unemployment.** The neutral unemployment rate is 15%. Unemployment below that encourages population growth; above it, demand falls.
- **Education options.** Available study positions boost demand for medium and high density specifically — people who need education cluster near it.
- **Low taxes.** The game uses 10% as the neutral tax rate for residential zones. Residential demand is the most tax-sensitive of the three types, at twice the weight of commercial or industrial.

> **Info:** Homelessness is tracked as a factor only for high-density residential demand. The threshold is extremely strict: just three homeless households in your city already count as a negative. The effect is capped, but it triggers very early.

## What Drives Commercial Demand

Commercial demand is calculated resource-by-resource — the game tracks how well each type of commercial activity (retail, food, lodging, petrochemicals, and so on) is served relative to your population. The final bar you see is an average across all resource types that have meaningful demand.

The core driver is simple: if your city's population has grown faster than the number of businesses serving them, demand rises. The threshold scales with population using a logarithmic curve, so the relationship between people and shops is not linear — larger cities need proportionally less commercial growth to stay satisfied.

Hotels behave differently from all other commercial categories. Lodging demand jumps to maximum whenever the number of tourists multiplying by a hotel-room requirement factor exceeds your current lodging capacity. Tourism spikes can therefore create sudden commercial demand without any corresponding population growth.

> **Info:** Taxes above 10% directly reduce commercial demand. The calculation is a flat reduction of roughly 5% of the demand score per percentage point of tax above neutral. High commercial taxes do not just reduce profitability — they actively suppress how many new businesses the game wants to create.

## What Drives Industrial and Office Demand

Industrial demand is the most intricate of the three. The game tracks manufacturing, office, and storage as three separate sub-categories, each with their own demand pair (abstract and building).

For manufacturing, demand is tied to resource supply deficits. If your city and its trade connections consume or require more of a produced resource than local factories supply, demand for that industry climbs. The formula computes a supply deficit ratio and scales it against a base demand multiplier. New cities start with the assumption that all resources are needed, defaulting to high demand before production data fills in.

Office demand works the same way but draws only from resources that are weightless and intangible — software, media, financial services, and similar products. It cares heavily about having an educated workforce available.

> **Info:** Workforce education directly limits industrial and office growth. If your city cannot provide enough educated workers, industrial company demand is throttled regardless of resource shortfalls. Educated worker surpluses (workers with at least some higher education) benefit offices; uneducated worker surpluses benefit manufacturing. A shortage in either direction suppresses the corresponding demand type.

Storage demand (warehouses) is triggered when resource production in the city exceeds a threshold of 2,000 units and available warehouse capacity falls short of what production demands.

City services also play a role in industrial demand that most players do not notice. Fire stations, police stations, and other services consume physical resources in their day-to-day upkeep. The game counts all of those non-money resource costs and adds them to the demand signal for the industries that produce those resources.

## Why Demand Is High but Nothing Is Building

This is one of the most common frustrations in the game, and the research reveals several distinct reasons it can happen.

The key distinction is between the abstract demand score and building demand. Building demand only rises when there are not enough *empty* buildings already available. The game maintains a target buffer of vacant properties per zone type before it starts requesting new construction:

- Low density residential: 5 free properties
- Medium density residential: 60 free properties
- High density residential: 100 free properties
- Commercial and industrial: roughly 5% of total stock must be vacant

> **Info:** If free properties meet or exceed these targets, building demand drops to zero or below even when household demand is high. The abstract demand score measures need; the building demand score measures whether enough physical space already exists to meet that need.

Beyond vacancy buffers, a few other conditions can silently block construction:

**No zoned land.** Building demand can be non-zero, but if you have not drawn the relevant zone type on roads, there is nowhere for buildings to appear.

**Zone types not unlocked.** Density tiers that have not been unlocked in your progression are excluded from building demand entirely regardless of the underlying score.

**Workforce mismatch.** If industrial demand is high but you have no unemployed workers or educated workforce available, company demand may not translate into building demand because the game knows new companies could not be staffed.

**All commercial types satisfied.** Commercial building demand is the average across resource types. If most commercial categories are well-served, a single overloaded category cannot push the average high enough to trigger construction on its own.

## What Can Go Wrong

- **City stuck at low population.** Happiness below 50, high unemployment, or heavy residential tax can combine to make the household demand score zero or negative. New city bonus fades around 20,000 population, and if nothing has replaced it, growth stalls.

- **Commercial demand stuck near zero.** A very small city (under 1,000 population) generates essentially no commercial demand signal because the population-scaled threshold has not kicked in. Commercial growth is deliberately gated to city size.

- **Industrial bars full but nothing spawns.** If all your industrial zones are occupied and there are no empty buildings left, building demand falls to zero. Zone more land; do not just wait.

- **High demand, zoned land, still no buildings.** Check whether the density tier is unlocked in your progression tree. Locked density types are silently excluded from the build queue regardless of demand.

- **Homelessness quietly killing high-density demand.** As few as three homeless households in your city applies a negative modifier to high-density residential demand. This threshold is far stricter than most players expect.

- **Taxes creating a demand ceiling.** Residential tax sensitivity is double that of commercial and industrial. Taxes even a few percentage points above 10% can meaningfully cap how high your demand bars can climb, limiting city growth rate regardless of other positive factors.