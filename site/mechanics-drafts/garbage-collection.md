## How Garbage Accumulates

Every building in your city that houses citizens, businesses, or industry is constantly generating trash. Sixteen times per in-game day, the game tallies up how much garbage each building has added to its pile. The rate depends on a few factors: a base amount built into the building type, reduced by how educated the occupants are and how high the building has leveled up. Industrial zones get an additional modifier on top of this. Homeless citizens produce extra garbage with no offset from education, so a city with a large homeless population will see accelerated accumulation in affected areas.

Each building has a hard cap on how much garbage can pile up. Once that ceiling is hit, accumulation stops — but by that point, your city has a serious problem on its hands.

> **Info — The thresholds that matter**
> There are four key levels of garbage at any building. First, a minimum amount a truck will bother picking up — trucks skip buildings below this. Second, the level that triggers a pickup request. Third, the warning level where a notification icon appears and building efficiency starts to drop. Fourth, the hard cap where accumulation stops entirely. The efficiency penalty grows linearly as garbage rises from the warning level toward the hard cap.

The four thresholds matter practically. The minimum pickup threshold means trucks ignore trivial accumulations entirely — a building with a small pile is never serviced, and that is by design. The warning threshold (the third level) is the point where efficiency starts declining, not where the building runs out of capacity. Efficiency drops linearly from the warning level to the hard cap, so a building halfway between those two points is running at roughly 50% efficiency for garbage-related functions. Reaching the hard cap means accumulation stops, but the damage to efficiency is already done and will remain until trucks clear the backlog.

The distinction between the warning threshold and the hard cap is meaningful for city management. You do not need to wait for the hard cap alarm — by the time a building hits cap, it has been penalized for some time already. The warning threshold is the earlier, quieter signal that your collection frequency is falling behind generation rate. Buildings showing warning icons are telling you that trucks exist and are working, but are not reaching that building often enough.

> **Info:** The efficiency penalty is linear between the warning threshold and the hard cap. A building at exactly the warning threshold loses no efficiency. A building at the hard cap loses all garbage-related efficiency. In a well-run city most buildings never approach the hard cap — the warning threshold is the actionable metric to watch.

## How Trucks Are Dispatched

Once a building's garbage crosses the request threshold, it enters the queue for service. The dispatch system then runs a pathfinding check to find which garbage facility can most efficiently reach that building and assigns the job to one of its trucks.

This is not purely reactive. Idle trucks and under-utilized facilities can also proactively scan for nearby buildings that have piled-up garbage, rather than sitting at the depot waiting to be called. This "seeking" behavior helps keep collection moving even when formal requests are sparse.

The dispatch system also respects a separation between regular household waste and industrial waste. Certain trucks and facilities are designated for industrial waste only, so industrial zone garbage is handled by a parallel track of vehicles and facilities.

Truck assignment is not permanent. If a truck finishes its run and returns to find a different facility is closer to a cluster of waiting requests, the dispatch system may route it there instead. The assignment logic optimizes continuously, so building a new facility in a congested area will naturally pull some truck activity toward it without manual intervention.

## Industrial vs. Residential Waste

Not all garbage is the same in CS2. The dispatch system separates waste into two categories: residential and commercial waste handled by standard garbage facilities, and industrial waste handled by a parallel set of designated facilities and trucks.

Industrial buildings generate industrial waste. Residential and commercial buildings generate standard waste. Each type is dispatched separately and trucks are not interchangeable — a standard garbage truck will not pick up industrial waste, and an industrial waste truck will not sweep residential streets.

This creates a common city-building trap: a heavy industrial district with no industrial waste facility will accumulate garbage indefinitely, regardless of how many standard garbage facilities you have nearby. The buildings warn about garbage, but standard trucks simply do not respond to those requests. The garbage overlay will show normal coverage, because standard coverage does exist — the disconnect is at the waste-type level, not the geographic level.

> **Info:** Check the facility's accepted waste type when placing it. A landfill designated for industrial waste appears nearly identical to a standard landfill in the UI but only services industrial zones. Zoning a mixed industrial-residential district requires both facility types to keep the area running cleanly.

## How Trucks Actually Collect

Garbage trucks do not drive directly to one building, pick up its trash, and return. Instead, each truck follows a route, and as it drives, it collects from every building connected to the roads along its path — sweeping an entire street or neighborhood in a single run.

As a truck moves through each road segment, it checks the buildings along the adjacent sidewalks and collects from any that have garbage above the minimum pickup threshold. The truck keeps running its route until its cargo hold is full, or until it is recalled to the depot. At that point it returns to its home facility and unloads.

> **Info — Capacity planning on the route**
> The game estimates how much garbage a truck will collect before it even departs, based on known building levels along the planned route. If that estimate would fill the truck to capacity, the truck is flagged and will not accept additional dispatch assignments for that run. This prevents overcommitting trucks that are already effectively "full" before they leave the depot.

## What Happens at the Facility

When a truck returns and unloads, the garbage is added to the facility's storage. From there, the facility processes it at a rate scaled by the building's current efficiency and how full the storage is. A nearly empty facility processes garbage more slowly; as the facility fills up, processing accelerates somewhat.

Incinerators destroy garbage through processing and can produce electricity as a byproduct. Landfills simply store garbage long-term without destroying it. The game treats these as fundamentally different: incinerators are processing facilities, landfills are storage facilities.

This difference affects how you plan capacity. An incinerator running at full efficiency never "fills up" in the same sense a landfill does — it is continuously destroying what it receives. But it is rate-limited: if trucks deliver faster than the incinerator can process, the internal storage buffer climbs. A landfill, by contrast, is purely a buffer — it holds whatever arrives, with no destruction, until it reaches its absolute ceiling.

Facilities can also transfer garbage between each other. A landfill holding a large stockpile can send loads to an incinerator for processing via dedicated delivery trucks. This inter-facility movement happens automatically when the dispatch system determines it is beneficial.

> **Info — Landfill "full" warnings**
> Only landfills trigger a facility-full notification, because they are the only type that accumulates garbage permanently. Incinerators that are overwhelmed will back up garbage in storage, but they do not carry the same long-term "full" status since they are expected to process and eliminate their load over time.

## **What Can Go Wrong**

**Garbage piles up faster than trucks can clear it:** If your residential or industrial zones are growing faster than your truck fleet, buildings will cross the warning threshold. Once that happens, those buildings lose efficiency — offices and industry produce less, which compounds economic problems.

**All trucks are already deployed:** Facilities only have a fixed number of trucks. If every truck is mid-route or returning with a full load, new requests sit unanswered until one returns. Building more facilities or upgrading existing ones adds more trucks to the fleet.

**Industrial waste has no facility:** Industrial garbage requires specially designated facilities and trucks. If you have heavy industry but only residential-type garbage coverage, industrial waste will accumulate without any trucks responding to it.

**A facility fills up completely:** A landfill that reaches capacity stops accepting incoming garbage. Trucks arriving to unload have nowhere to go, and collection backs up across your entire network. You need to either expand capacity, build a new facility, or route garbage to an incinerator via inter-facility transfer.

**A facility loses efficiency:** Processing speed at a facility scales with how well it is running. A facility with power problems, low coverage from maintenance, or other efficiency penalties will process garbage slower, causing the storage level to creep up even when trucks are delivering normally.

**Poor road layout isolates neighborhoods:** Truck routes are determined by the road network. Buildings in cul-de-sacs, poorly connected districts, or areas with long travel times to the nearest facility will be served infrequently, causing localized garbage buildups even when the rest of the city is running smoothly.

**Efficiency penalty hiding behind a clean-looking overlay:** The garbage infoview shows coverage, not accumulation levels. A building fully within a garbage facility's coverage zone can still be accumulating garbage above the warning threshold if trucks are too busy to reach it frequently. Watch individual building tooltips, not just the overlay color.

**Facility processing rate miscalibrated:** An incinerator running at low efficiency processes garbage slower than trucks deliver it, causing the facility's storage to fill even in a city with adequate truck coverage. Check facility efficiency directly — power problems, worker shortages, or budget cuts can all degrade processing speed without the headline garbage stats flagging it.
