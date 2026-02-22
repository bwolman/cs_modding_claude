# Telecommunications and Internet Service

Your city's residents and businesses don't just need roads and electricity — they need connectivity. Telecommunications infrastructure provides the internet and mobile service that modern buildings depend on to function at full capacity. Neglect it early and you'll find your commercial strips underperforming and your office districts quietly struggling, even when everything else looks fine.

## How Coverage Works

Telecom coverage radiates outward from towers and other telecom facilities you place on the map. Each facility broadcasts a signal across a circular area, and that signal weakens the farther you get from the source. The falloff is not linear — signal drops off rapidly near the edge of a tower's range, meaning the outer fringes of coverage are noticeably weaker than the center.

> **ℹ️ Signal formula**: Signal strength follows a quadratic curve — `1 - (distance / range)^2`. A building at 70% of a tower's maximum range receives only about half the signal strength of a building right next to it.

The default coverage radius for a standard telecom facility is 1,000 meters. When you hover over a facility while placing it, the game previews the coverage footprint in real time so you can see exactly what will and won't be reached.

Terrain can also block signal. Most standard towers cannot punch through hills and elevated ground, so valleys and areas behind ridgelines may sit in a "shadow" even if a tower is technically within range. Some higher-tier facilities can penetrate terrain, which is especially useful in hilly or mountainous maps.

## Network Capacity and Congestion

Coverage and capacity are two separate things. A tower can physically reach a neighborhood, but if too many people are on the network at once, service quality degrades. Each facility has a finite bandwidth it can provide, and that capacity is distributed across all the cells within its reach, weighted by how strong the signal is in each area.

> **ℹ️ Capacity rule**: Network load is tracked per coverage cell alongside signal strength. Quality at any given spot is a function of both: `signal strength / (127.5 + network load)`. A strong signal into a congested cell produces worse quality than a moderate signal into an uncongested one.

This means dense neighborhoods are harder to serve than sparse ones. Placing a single tower over your downtown core might give strong signal, but if thousands of residents and workers are all connected through it, the effective quality will be poor. You need more towers, not just bigger ones.

Installing upgrades on a facility stacks additional range and capacity on top of its base values. Upgraded towers serve a larger area and handle more users simultaneously.

## What Buildings Need Connectivity For

Not every building cares equally about telecom service. Residential buildings need it for their households, commercial buildings need it to run their operations, and office buildings tend to be the most dependent of all — connectivity is essentially infrastructure for their core business.

The game tracks how much each building type relies on telecom. Buildings with a high connectivity need will suffer steeper efficiency penalties when service is poor, while a small warehouse with minimal telecom needs may barely notice the same gap in coverage.

> **ℹ️ Efficiency penalty**: When the network quality at a building's location falls below a threshold, the building's operational efficiency takes a hit. The penalty grows quadratically — a building just barely below the threshold loses little, but one with no service at all suffers heavily.

Efficiency affects everything downstream: how many goods a commercial building can sell, how many workers an office can effectively employ, how much output a service building delivers. Poor telecom quietly drags down your city's productivity without the obvious visual cues of, say, an unconnected road.

## The City-Wide Picture

The telecom info view shows you a color-coded overlay of signal strength across the entire map. The network availability indicator in the panel reflects a population-weighted average — it weighs coverage quality more heavily in areas where people actually live and work. An empty district with no signal doesn't drag down the number the way a packed residential zone without coverage does.

City policies and certain milestone rewards can boost your entire network's capacity, effectively giving every tower more bandwidth without changing its physical range.

## What Can Go Wrong

**Gaps at the city edge.** As you expand, new neighborhoods outrun your existing towers. Buildings go up before coverage arrives, and residents move in to underserved conditions.

**Downtown congestion.** High population density overwhelms a tower's capacity even when signal is strong. The info view may look green on signal but buildings still underperform. The fix is more towers, not repositioning.

**Terrain shadows.** A hill between a residential area and your nearest tower can cut off an entire neighborhood. Watch for valleys and depressions in your terrain, especially on maps with significant elevation changes.

**Upgrading too slowly.** Tower upgrades stack capacity and range additively. Deferring upgrades means your network ages in place while the city grows around it.

**Misreading the info view.** The overlay shows signal, not congestion. An area can appear fully covered and still deliver poor quality if load is high. Check the network availability indicator for the true picture.
