## How Fires Start and Spread

Every building and wild tree in your city has a hidden fire hazard rating. Once every minute or so, the game quietly rolls the dice on a random sample of flammable structures. Most of the time nothing happens. But occasionally a building catches fire — from an arson event, a traffic accident igniting a vehicle, a disaster, or simply bad luck from a building sitting in a high-risk zone without good fire station coverage.

Several factors make a spontaneous fire more or less likely. Industrial zones carry inherently higher risk than residential ones. Lower-level buildings are slightly more vulnerable than mature, higher-level ones — a level 5 building is about 12% less likely to ignite on its own than a level 1 building. The single most effective protection is fire station service coverage: a building with full coverage from a nearby station is up to 99% less likely to catch fire spontaneously. District policies can push the hazard in either direction.

For wild trees, climate is everything. The longer it has gone without rain, and the higher the temperature, the more likely a forest fire becomes. Tree fires also require that the natural disasters setting is enabled in your city options.

### How Fire Spreads

Once a building is burning, it does not stay contained to one structure. The game periodically checks whether the fire jumps to neighboring buildings or trees within roughly 20 meters. Spread probability is driven by the burning building's current intensity and the neighboring structure's own hazard rating — the closer and more flammable the neighbor, the more likely the fire leaps across. A low-intensity fire that is caught quickly is unlikely to spread. A raging full-intensity fire is a serious threat to an entire block.

### How the Fire Escalates

Fire intensity starts low and climbs over time. Each building has a structural integrity rating that determines how long it can withstand heat — higher-level buildings are somewhat tougher. As intensity rises, the fire deals structural damage. When a building takes enough damage, it collapses. Once collapsed, it shifts from "on fire" to "rubble that needs clearing" — a different kind of problem that requires a different kind of response (more on this below).

A building that has already taken storm damage or prior structural damage is actually less likely to spontaneously ignite again — the game models it as having less flammable material remaining.

---

## How Fire Engines Are Dispatched

When a building catches fire, there is a short delay before a rescue request is formally placed — representing the time it takes for someone to call emergency services. This delay ranges from 3 to 30 seconds of game time, and two things modify it:

- **Telecom coverage** shortens the delay by about 15%, because people with signal call faster.
- **Nighttime** lengthens the delay by about 10%, because fires may go unnoticed longer in the dark.

Once the request is placed, the dispatch system searches for the nearest available fire engine. "Nearest" is calculated by travel time, not map distance — a station on the other side of a highway interchange may be farther by road than one that looks close on the map.

> **Info — What does "available" actually mean?**
> A fire station is only considered as a candidate if it currently has at least one engine ready to deploy. If all of a station's engines are out on other calls, that station is completely invisible to the dispatch system. It does not matter how close it is. Only stations with engines sitting in the garage are eligible.

### Why a Distant Station Sometimes Responds Instead of the Nearby One

This is one of the most counterintuitive things about fire response in Cities: Skylines II, and it has two separate explanations.

The first explanation is **availability**. A nearby 2-engine station that has both engines deployed elsewhere simply cannot respond. The pathfinding system that selects which station answers the call does not even consider it — a station with no available engines is invisible to the search. A larger station across town with six engines will almost always have at least one ready, so it wins the dispatch even though it is farther away.

The second explanation is **service district restrictions**. If you have configured a fire station to serve only specific districts, it will refuse to respond to fires outside those districts, regardless of distance. A local station locked to District A will not respond to a fire in District B even if the burning building is right next door to the station.

> **Info — The "Disaster Response" upgrade does NOT make a station faster at fighting regular fires.**
> The Disaster Response upgrade adds a separate pool of capacity used exclusively for collapsed building rescues. It has no effect on how quickly or how often a station responds to ordinary fires. Both plain fire stations and upgraded rescue stations are equally eligible for standard fire calls. The upgrade is only relevant when a building has already collapsed and needs its rubble cleared — a task that plain stations cannot perform at all.

### What Happens When the Engine Arrives

A fire engine does not just spray one building. It uses an area-of-effect approach: any burning or collapsed structure within its reach gets water, including adjacent buildings that may have caught fire from spread. This means a single engine arriving at a serious fire can simultaneously suppress several structures at once.

The engine carries a finite water tank. As it works, the tank drains. Once empty, the engine cannot fight any more fires and must return to its station to refill before it can respond again. The game does estimate ahead of time whether a tank will run dry, and factors that prediction into dispatch decisions.

A deployed engine can also be chained to a second fire on its way back if the call is along its route — the engine handles high-priority queued fires before returning home, so a single truck may serve multiple incidents in one trip.

### Collapsed Buildings: A Different Kind of Response

When a building is destroyed by fire (or a disaster), the rubble left behind is not automatically cleaned up. The collapsed structure gets flagged as needing rescue — debris clearing — and a new request goes out. Only fire stations with the **Disaster Response upgrade** can respond to this type of call. Plain stations are entirely ineligible, no matter how close they are.

A fire engine responding to a collapsed building enters a debris-clearing mode rather than a firefighting mode. It works the site until the rubble is cleared, at which point the engine returns to its station. If the engine finishes and the site is not yet fully cleared, the dispatch system will send another engine to continue the work.

---

## What Can Go Wrong

**All nearby engines are already deployed.** The most common failure. A neighborhood with only one small fire station becomes completely unprotected the moment that station's engines are out on calls. The fire grows until an engine from a more distant station arrives — by which point, spread to neighboring buildings is likely.

**A station's district restriction excludes the fire's location.** If you have manually configured district-based restrictions on your fire stations, a local station may refuse to respond to a fire that is geographically close but administratively outside its service area. The fire will be handled by a more distant station that is not restricted.

**The fire spreads before the engine arrives.** The response delay — especially at night without telecom coverage — can allow a fast-escalating fire to reach neighboring buildings. By the time an engine arrives, it may be dealing with multiple simultaneous fires, draining its tank faster.

**An engine runs dry mid-fight.** If fire intensity is high and the engine's water tank empties before the fire is out, the engine returns to its station to refill. The fire continues to escalate in the meantime. A second engine may or may not arrive before the building is lost.

**No station can handle a collapsed building.** If all of your fire stations are plain stations without the Disaster Response upgrade, collapsed buildings will never be cleared. The rubble sits indefinitely, preventing reconstruction.

**Simultaneous fires overwhelm the fleet.** During a disaster or a chain of spread events, multiple fires can erupt faster than your stations can respond. Each fire holds an engine for the duration of the call, and a city-wide fleet can be fully deployed with fires still going unserved.