# Public Transit in Cities: Skylines II

## How a Transit Line Works

When you draw a bus route or lay down a train line, the game treats the whole line as a single living entity. That entity owns a list of stops, a list of segments connecting those stops, and a pool of vehicles currently assigned to it. The game updates each line roughly once every few seconds of simulation time, recalculating how long the full loop takes, how many vehicles are needed, and whether any vehicles need to be added or sent back to the depot.

Everything flows from one key number: the **vehicle interval** — how many seconds the game wants between consecutive vehicles arriving at the same stop. Divide the total loop time by that interval and you get the target vehicle count. If the loop takes 600 seconds and the target interval is 60 seconds, the game will try to put 10 vehicles on the line.

> **Info:** The formula is `round(total loop time / vehicle interval)`, with a minimum of 1 vehicle. You can override the vehicle count manually from the line's info panel — the game translates your chosen count back into a new interval and uses that going forward.

## How Citizens Decide to Use Transit

Citizens weigh the transit network when they pathfind to a destination. They factor in the time it would take to walk to a stop, wait for a vehicle, ride to a transfer or destination, and walk the rest of the way. If that total is competitive with driving or walking the whole way, they will use the line. Stop comfort — which improves when stops are attached to proper transport stations — nudges that calculation further in transit's favor.

The game does not show you a ridership-demand number directly, but the queue of waiting passengers at each stop is tracked continuously. A long-growing queue signals that the line is either too slow, too infrequent, or that the stop serves a dense area the line cannot keep up with.

## How Vehicles Get Assigned

Vehicles do not appear from thin air. When a line needs more vehicles, the game issues a dispatch request, finds a suitable depot on the same transport network, and sends a vehicle out to the first stop on the line. The vehicle then runs the loop indefinitely. When the line needs fewer vehicles — because you reduced the target count or changed the vehicle model — the game sets an "abandon route" flag on the excess vehicles and they return to the depot at the end of their current run.

> **Info:** If the game cannot find a reachable depot with available vehicles of the right type, the line displays a "not enough vehicles" notification. The notification clears automatically once a depot comes online or vehicles become available.

## How Stops and Boarding Work

When a vehicle arrives at a stop, it goes through a brief **testing** phase to confirm the stop is still accessible before committing to board. Once it pulls in, a boarding window opens: passengers waiting at the stop climb on, passengers whose destination is served by this stop climb off. The length of that window is governed by the line's stop duration and a departure-frame calculation that factors in the current vehicle spacing.

> **Info:** The default stop dwell time is 1 second of simulation time. Stops attached to transport stations board passengers faster, because stations apply a loading-speed bonus on top of the stop's own loading factor.

The **unbunching factor** (default 0.75) adjusts when a vehicle is allowed to leave a stop. If two buses bunch up behind each other, the trailing bus will sit a little longer than normal to re-establish spacing. This is automatic and runs every time a vehicle departs.

## What Affects Ridership

Several things work together to make a line attractive or unattractive to citizens:

- **Frequency.** More vehicles mean shorter waits. The game calculates expected wait time per stop and citizens weigh it in their travel decisions.
- **Stop comfort.** A stop with a shelter or a full transport station scores higher than a bare kerb-side stop. Higher comfort makes citizens more likely to accept the transit option.
- **Day and night scheduling.** Lines can be restricted to run only during daytime or only at night. A line set to daytime-only is completely inactive after dark — citizens will not find it as a valid path, and vehicles return to depot.
- **Ticket price.** You can enable paid fares on a line via its policy settings. Higher fares reduce the attractiveness of the line to citizens but generate revenue.

> **Info:** Comfort and loading speed are calculated from two sources added together: the stop's own baseline values from its prefab, and a bonus applied by any transport station building the stop belongs to. The final comfort is clamped between 0 and 1. The final loading speed cannot go below 0.

## Fares and Their Effect on Ridership

Fares are set per-line in the line's policy settings. When a fare is active, the pathfinder adds the fare cost to the total route cost for any citizen evaluating that line. This means fares are not a passive revenue tap — they actively make transit less competitive in the pathfinder's eyes.

Higher fares make the transit option more expensive relative to driving or walking. The pathfinder weighs monetary cost alongside time cost, and the two interact: a line that saves a citizen significant time can absorb a moderate fare and still win out, but a line that is only marginally faster than the alternatives cannot. Citizens with low disposable income are most sensitive to fares; wealthier citizens tolerate them better and are less likely to abandon a line when a fare is added.

Revenue from fares flows to the city treasury as transit income, visible in the Economy panel. A well-patronized paid line can generate meaningful recurring revenue — particularly on high-demand corridors where ridership remains strong even after the fare is applied.

Zero-fare lines maximise ridership but generate no transit revenue. The choice is a genuine trade-off: maximising ridership reduces road traffic and improves city happiness, while generating fare income offsets the operating cost of running the line. Neither approach is universally better — it depends on what your city needs most at a given stage.

> **Info:** Fares reduce the pathfinder's attractiveness of a line proportionally to the fare amount. A free alternative (walking, driving) always wins on cost alone — transit must win on time to compensate. This means paid transit only reliably attracts ridership when it is significantly faster than alternatives. A line that is only slightly faster than walking will shed most of its riders the moment a fare is introduced.

## Multimodal Transfers

Citizens are not limited to a single transit line. When a destination is not directly served by one line, the pathfinder evaluates routes that combine multiple modes: walk to a stop, take a bus, transfer to a subway, walk to the destination. Your transit network functions as a system, not just a collection of independent lines.

Each transfer adds a cost penalty to the overall route, representing the inconvenience and wait time of switching vehicles. Lines with long intervals between vehicles increase this penalty because the expected wait at the transfer point is longer. A subway that runs every 10 minutes is a worse transfer destination than one that runs every 2 minutes, even if both lines are otherwise identical — the waiting citizen is factored into the total route cost.

Transport stations reduce transfer cost. When two or more lines share a transport station building rather than bare stops placed near each other, the station applies a loading-speed bonus and a comfort bonus that partially offsets the transfer penalty. Citizens are more willing to transfer at a proper interchange than at an unmarked kerb stop. Building dedicated interchange stations at your busiest transfer points is one of the highest-impact infrastructure decisions you can make for a complex network.

> **Info:** The pathfinder evaluates the entire multimodal chain as a single cost. A subway with a 10-minute headway is less attractive as a transfer destination than one with a 2-minute headway, even if both lines are otherwise identical, because the expected wait at the transfer stop is factored into the total route cost. Increasing frequency on key connector lines — the ones citizens transfer onto, not just the ones they start on — has an outsized effect on how well your whole network performs.

## Depot Capacity and Vehicle Lifecycle

Every vehicle on a transit line must originate from a depot of the matching type. Depots have a vehicle capacity — a maximum number of vehicles they can house simultaneously. When all vehicles are deployed on lines, the depot is full and cannot dispatch additional vehicles until one returns.

A single depot can serve multiple lines simultaneously. Vehicles leave when dispatched and return when a line is reduced or its vehicle target drops. If a depot is at capacity and a new line requests vehicles, the "not enough vehicles" notification appears on the requesting line. The fix is either to add another depot, reduce vehicles on an existing line, or extend the reach of a depot on a different part of the network by building a road connection between depot and line.

Vehicles have a lifecycle: they can be destroyed in accidents or disasters. Destroyed vehicles are removed from the depot's pool and must be replaced. The game automatically restocks depots over time, but a sudden mass-casualty event — a flood, a tornado, or a large road accident — can temporarily reduce your fleet below what your lines need. Affected lines will show under-serviced notifications until the depot replenishes.

> **Info:** When you change the vehicle model on a line — upgrading from standard buses to articulated buses, for example — the game retires the old vehicles to the depot and dispatches the new model type. This causes a brief gap in service on that line while the transition completes. On high-demand lines, scheduling a vehicle model upgrade during off-peak hours reduces how many passengers experience the gap.

## What Causes Lines to Fail

**No vehicles dispatched.** The most common failure. Causes include no depot of the right type within reach, the depot being full, or a road network gap breaking the pathfind from depot to the line's first stop.

**Buses bunching.** Even with unbunching logic, a line with too few vehicles for its loop length will develop clumps. The trailing vehicles in a clump pick up fewer passengers (the leading vehicle already emptied the stop), while the gap ahead of the clump grows and passengers accumulate. Adding vehicles or shortening the loop by splitting the line usually resolves this.

**Stop goes inactive.** If a stop's building loses access — a road is deleted, the parent station is demolished — the stop loses its "allow entry" flag. Vehicles still visit it but nobody can board, leaving a dead spot in the line.

**Wrong depot type.** Each transport mode requires a matching depot. Placing a tram line with no tram depot in the city, or with the only tram depot stranded behind a broken road, will produce the "not enough vehicles" notification indefinitely.

**Day/night mismatch.** A line set to daytime-only in a city where demand peaks at night silently serves no one during those hours. The line looks fine in the UI because it operates correctly during its scheduled window — but ridership will be near zero overall.

**Line too long.** Very long loops raise the total travel time, which increases the vehicle interval needed to maintain reasonable frequency. If the loop is so long that hitting your target frequency would require more vehicles than your depot stocks, the game will provision as many as it can and mark the line as under-served. Splitting a long line into two shorter overlapping lines is usually more efficient than trying to staff one enormous route.

**Paid fares priced above the transit benefit.** If a fare makes the total route cost higher than driving or walking, citizens simply will not use the line regardless of how frequent or comfortable it is. Check ridership after enabling fares — a sudden drop means the fare exceeded the time-savings benefit for most citizens on that corridor. Either reduce the fare or work on shortening travel time so the time advantage grows enough to absorb the cost.

**Transfer penalty killing multimodal routes.** A city with many short lines that require multiple transfers may see citizens default to driving even when the transit network technically covers the journey. Long vehicle intervals at transfer points compound the problem — each minute of expected wait adds to the penalty the pathfinder charges against that route. Reducing headways on key connector lines or building proper transit stations at major transfer points restores multimodal attractiveness and can unlock ridership across the whole network.
