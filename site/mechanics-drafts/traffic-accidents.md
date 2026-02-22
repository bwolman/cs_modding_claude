# Traffic Accidents in Cities: Skylines II

## How Accidents Happen

Traffic accidents in Cities: Skylines II are not random cosmetic events — they are a genuine simulation of a vehicle losing control, colliding with the world, and triggering a chain of emergency responses that can block your roads for minutes at a time.

An accident begins when a vehicle is struck by a force strong enough to knock it out of its normal lane-following behavior. At that point, the game stops treating the vehicle as a pathfinding agent and switches it over to a physics simulation: gravity pulls it down, friction slows it, and its momentum carries it wherever the collision sent it. The vehicle can spin, slide across lanes, and hit other vehicles or objects it passes through on the way to rest.

> ℹ️ **The physics simulation runs at sub-frame resolution** — four physics steps are calculated per game simulation frame, using a gravity constant of 10 m/s² and a braking coefficient derived from each vehicle type's grip rating. Heavier or less grippy vehicles travel further before stopping.

## Chain Reactions

One of the most consequential — and often frustrating — behaviors is the chain reaction. For roughly the first 60 seconds after an accident scene is established, the site is "hot": it can randomly push a passing car into a collision of its own. The game picks a random vehicle traveling through the affected road segment and shoves it sideways at 5 m/s with a 2 rad/s spin. That new vehicle then goes through the same full accident cycle, potentially spreading the scene further down the road.

> ℹ️ **The chain-reaction window is exactly 3,600 simulation frames** (approximately 60 seconds of game time). After that, the site stops recruiting new victims. Only one chain reaction is triggered per event cycle, but if the new vehicle also causes impacts, the cascade can widen.

## The Accident Scene

Once a sliding vehicle slows to nearly a stop, the game locks it in place and marks the nearest road segment — within 30 meters — as an accident site. This is the location that emergency services respond to and that traffic must navigate around.

A notification icon appears above the scene. Police are dispatched immediately. Until they arrive and secure the site, the scene remains in an unresolved state. If the accident is severe enough that the vehicle caught fire, a fire unit may also respond.

Pedestrians and cyclists hit by an out-of-control vehicle have their own outcomes. Cyclists struck by a vehicle are injured every time — there is no chance of walking away unscathed. Pedestrians have a 50% chance of being injured. When an injury occurs, there is a 20% chance it is fatal and the person collapses on the spot, requiring a hearse; the remaining 80% are non-fatal injuries that call an ambulance.

> ℹ️ **Injury severity cascades through a vehicle's passengers.** The driver receives the full impact severity; each additional passenger receives 80–90% of the previous passenger's severity. A car full of people means multiple simultaneous emergency requests, each at slightly lower priority.

## How Long Scenes Last

The scene persists until police have secured it AND all involved vehicles are either cleared or restarted. Lightly damaged vehicles that have stopped are restarted and sent on their way once police arrive. Destroyed vehicles (wrecks) require a maintenance crew to clear them before the site closes.

> ℹ️ **The hard timeout for any accident scene is 14,400 simulation frames** — roughly 4 minutes at 60 fps. If police never arrive, the scene clears itself automatically at that threshold. Bicycle accidents have a much shorter cleanup window of only 300 frames (about 5 seconds). If the accident scene also qualifies as a crime scene, there is an additional buffer of ~17 seconds after securing before the site is fully removed.

## Traffic Impact

While the scene is active, the blocked road segment forces vehicles to find alternate routes. Chain reactions can extend the blockage, and a single accident on a congested arterial road can cause ripple congestion across several intersections. The scene does not fully resolve until the last involved vehicle is cleared — meaning a destroyed wreck that is slow to be removed keeps the entire site active and the traffic detour in place.

## What Can Go Wrong

**No police coverage nearby.** If you have poor police station placement, the 14,400-frame timeout is your only guarantee that a scene ever clears. In that time, traffic reroutes and can cascade into gridlock across a district.

**Chain reactions on busy roads.** An accident on a high-traffic road during the 60-second hot window can recruit multiple passing vehicles into the scene before police arrive, turning a two-car fender-bender into a multi-vehicle pileup spanning several road segments.

**Destroyed vehicles extending scenes.** A wreck that catches fire or sustains heavy structural damage needs active cleanup. Until it is fully cleared, the accident site stays open. If your maintenance services are overwhelmed, a single bad crash can linger well past its natural resolution window.

**Ambulance and hearse congestion.** A severe accident injuring multiple passengers generates simultaneous transport requests. If those vehicles must navigate around the very road segment that is blocked, response times suffer and injured citizens wait longer — potentially cascading into health and death statistics.
