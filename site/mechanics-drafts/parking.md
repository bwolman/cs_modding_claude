# How Parking Works in Cities: Skylines II

When a citizen decides to drive somewhere, finding a parking spot is not a simple step — it is woven into the entire trip from the moment the route is planned. Understanding how the game handles parking explains why traffic behaves the way it does and what you can do as a city builder to keep things running smoothly.

## How Routes Are Planned Around Parking

Before a driver ever leaves home, the pathfinder already considers parking. It looks at every possible parking option near the destination and factors in three things: how much space is available, how convenient the spot is (a comfort score from 0 to 1), and whether a fee is charged. These three values are combined into a single route cost, so a cheap but uncomfortable or distant spot may lose out to a pricier but more convenient garage closer to the destination. The game does this work upfront so vehicles are already heading toward a likely spot before they arrive.

## Searching for a Spot

Once the vehicle is close, the driver does not simply pull into the first space — the game scans ahead through the planned route, checking up to 40,000 path nodes for a valid, open spot. The game checks street parking for free continuous curb space or open slotted spaces, and checks garages by comparing the current number of parked vehicles against the building's capacity.

> **Info:** The vanilla search range of 40,000 path nodes is significantly larger than what many players consider realistic. Community mods like RealisticParking reduce this to approximately 5 nodes to simulate drivers who give up quickly and circle the block rather than committing to a remote lot.

## When No Parking Is Found

If the scan turns up nothing — every nearby lane is full and every garage is at capacity — the vehicle's current route is marked as obsolete and the pathfinder recalculates a new one. The driver will reroute, potentially to a farther parking option. If the city genuinely lacks parking near popular destinations, this triggers repeated rerouting, contributing to congestion as vehicles circle and search.

> **Info:** Vehicles do not give up and turn around. They will always eventually find somewhere to park, but repeated rerouting means those vehicles stay on the road longer, contributing to congestion even though they have reached their destination area.

## How Parking Fees Work

Fees operate at two levels. Street parking fees are set at the district level — when you enable paid parking for a district, every parking lane in that district inherits the fee value. Garage fees are set at the building level, either by the building's own configuration or by upgrades applied to it. When a driver parks, the fee is automatically transferred from the household to the city treasury as parking revenue.

> **Info:** Fees affect pathfinding decisions. A street spot with a fee will be less attractive than a free one, all else being equal. If you enable paid parking across an entire district, expect drivers to route around it toward cheaper or free options nearby — including potentially your residential side streets.

## Street Parking vs. Parking Garages

Street parking and garages operate on different capacity models. Street parking uses available curb length — either measured as a continuous gap or as a counted set of discrete slots depending on the road type. A lane is effectively full when the free space measurement drops to zero. Garages track a simple integer count of vehicles against a fixed capacity ceiling.

The capacity of a garage depends on the building: dedicated parking facilities have explicit capacities defined per building, while garages attached to residences scale roughly with unit count, and workplace parking scales with the number of workers (roughly one space per twenty workers).

The comfort score (0 to 1) is the parking option's attractiveness to the pathfinder beyond pure availability. Several factors push this score up or down.

Distance from the parked spot to the final destination is the dominant factor: a garage two blocks away from the destination scores lower than an adjacent street space, even if both are free. The pathfinder weights parking comfort against the additional walking distance the driver will face after parking.

Garage efficiency affects comfort directly: a garage running at low operational efficiency (due to staffing shortfalls, power issues, or building damage) sees its comfort score reduced. This makes the garage less attractive to the pathfinder even when it has empty spaces. A garage at 30% efficiency may be passed over entirely in favor of more distant street parking.

Street parking comfort is simpler: freely available kerb space scores roughly 0.5 as a baseline and is not affected by efficiency in the same way garages are. This makes street parking a reliable fallback even when managed facilities are underperforming.

> **Info:** The pathfinder uses comfort score at route-planning time, before the vehicle departs. If a high-comfort garage fills up between when the route was planned and when the vehicle arrives, the vehicle will find the space taken and must search forward along its path for the next option. This is the mechanism behind "I was heading to the garage and it was full when I got there."

## How Lack of Parking Affects Behavior

When parking is scarce, the effects ripple outward. Drivers reroute repeatedly, adding vehicle-minutes to roads that are already near capacity. Trips that should resolve quickly instead keep circulating. In dense commercial districts without adequate parking, this can become a self-reinforcing problem: congestion from searching vehicles slows the road network, which makes the same vehicles take longer to finally park, which keeps them on the road even longer.

Paid parking reduces the attractiveness of driving to a destination in the first place, nudging citizens toward public transit or walking if those options are viable. This is the game's representation of demand management — pricing people off roads rather than building more road capacity.

> **Info:** Disabling street parking on a road immediately signals the pathfinder to stop considering those lanes. The change propagates automatically to the route cost model, so vehicles will seek alternatives without any manual nudging.

## Paid Parking and Demand Management

Paid parking's most important effect is not revenue — it is demand management. When a district enables paid parking, it adds a monetary cost to the pathfinder's evaluation of driving to any destination in that district. Citizens compare the total cost of driving (time + parking fee) against alternatives.

At moderate fee levels, this nudges marginal car users toward transit or walking while leaving committed drivers mostly unaffected. At high fee levels, it significantly reduces driving demand in the district — but citizens who still drive will pay more, contributing to city revenue.

The effect is strongest on lower-income households, whose pathfinder calculates a higher relative cost for the fee compared to their disposable income. Wealthier households are less sensitive to parking fees. A paid parking zone in a high-income commercial district may generate substantial revenue with minimal impact on actual traffic volumes.

> **Info:** Paid parking revenue is not shown as a separate budget line — it appears as part of transport revenue in the Economy panel. If you want to evaluate whether paid parking is generating meaningful income, look at your transport revenue trend before and after enabling fees in a district.

## What Can Go Wrong

**Garages that never fill up.** If a garage has a very low comfort score — either because the facility is understaffed or because it was built with a low base comfort setting — the pathfinder will steer vehicles away from it even when it has capacity. The garage appears empty while street parking nearby is saturated.

**Paid parking that backfires.** Enabling paid parking district-wide without adequate free alternatives nearby can push vehicles into adjacent residential streets, overloading parking lanes that were not designed for heavy use.

**Circling traffic that looks like congestion.** If a popular destination area lacks enough total parking, vehicles will repeatedly reroute and stay on the road. This traffic is not passing through — it is looking for a place to stop. More road capacity will not fix it; more parking will.

**Garages attached to buildings with low worker counts.** Workplace parking capacity is calculated from staff numbers. A large office tower that is partially empty due to low demand will have proportionally fewer garage spaces, potentially creating a mismatch between the building's visual size and its actual parking provision.

**Efficiency-based deactivation.** A parking facility that loses all its workers (due to budget cuts, fire, or abandonment) will have its spaces marked inactive entirely. Vehicles will not try to park there at all, even if the structure is physically intact.

**Comfort score suppressing newly-built garages.** A brand-new parking garage that has not yet reached full staffing or operational efficiency will have a reduced comfort score. The pathfinder will route vehicles to older, more established alternatives nearby instead of filling the new garage. Give new parking facilities time to staff up fully — their comfort score normalizes once efficiency reaches its baseline, and the pathfinder will naturally start filling them.
