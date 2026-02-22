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

## What Causes Lines to Fail

**No vehicles dispatched.** The most common failure. Causes include no depot of the right type within reach, the depot being full, or a road network gap breaking the pathfind from depot to the line's first stop.

**Buses bunching.** Even with unbunching logic, a line with too few vehicles for its loop length will develop clumps. The trailing vehicles in a clump pick up fewer passengers (the leading vehicle already emptied the stop), while the gap ahead of the clump grows and passengers accumulate. Adding vehicles or shortening the loop by splitting the line usually resolves this.

**Stop goes inactive.** If a stop's building loses access — a road is deleted, the parent station is demolished — the stop loses its "allow entry" flag. Vehicles still visit it but nobody can board, leaving a dead spot in the line.

**Wrong depot type.** Each transport mode requires a matching depot. Placing a tram line with no tram depot in the city, or with the only tram depot stranded behind a broken road, will produce the "not enough vehicles" notification indefinitely.

**Day/night mismatch.** A line set to daytime-only in a city where demand peaks at night silently serves no one during those hours. The line looks fine in the UI because it operates correctly during its scheduled window — but ridership will be near zero overall.

**Line too long.** Very long loops raise the total travel time, which increases the vehicle interval needed to maintain reasonable frequency. If the loop is so long that hitting your target frequency would require more vehicles than your depot stocks, the game will provision as many as it can and mark the line as under-served. Splitting a long line into two shorter overlapping lines is usually more efficient than trying to staff one enormous route.