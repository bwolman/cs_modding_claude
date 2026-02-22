# Water and Sewage in Cities: Skylines II

Your city's water infrastructure operates as two separate but connected systems: a pipe network that serves buildings, and a surface water simulation that governs rivers, lakes, and flooding. Understanding the difference between these two systems — and how they interact at pumping stations and sewage outlets — is the key to keeping citizens happy and dry.

---

## Fresh Water: From Source to Tap

Every drop of water your buildings use begins at a pumping station. The game offers two ways for a pump to draw water.

A **surface water pump** reaches into a nearby river, lake, or reservoir. It draws water as a negative force on the surface simulation — physically removing water from the terrain and converting it into supply for your pipe network. For a surface pump to work well, it needs enough depth at its intake location. A pump sitting in very shallow water runs at reduced efficiency, potentially delivering less than its rated capacity.

A **groundwater pump** instead draws from an invisible underground aquifer. The map is divided into a grid of cells, each holding a reservoir of groundwater that slowly refills over time and spreads to adjacent cells. Groundwater is always available even without a nearby river, but a cell can be depleted if you pump more than the natural replenishment rate can replace.

> ℹ️ **Groundwater facts:** Each grid cell holds up to 10,000 units of groundwater. A pump only produces at full rate when a cell contains at least 500 units. Depletion is gradual — cells refill at roughly 0.4% of their maximum capacity per simulation cycle and also receive slow natural purification. Groundwater can be contaminated by nearby pollution, and contamination diffuses between adjacent cells.

Once a pump produces water, it feeds that supply into the **pipe network**. The pipe network uses a graph-based flow model: every road segment carries embedded pipes with essentially unlimited capacity, and the game solves for maximum flow from all producers to all consumers across the entire network every few seconds of game time.

> ℹ️ **How the solver works:** The game recalculates the entire pipe network on a 128-frame cycle (roughly every two real-time seconds at normal speed). It determines how much water each building needs, then solves for the best possible distribution given available supply. If you have redundant pipe paths, flow is distributed proportionally across them.

Buildings connect to the network automatically when placed next to a road. They draw a share of the water flowing through that road segment, proportional to their size and occupancy. Larger, more-occupied buildings need more water. Vacant or partially occupied buildings consume less. An inactive building (abandoned, under construction) uses only about 10% of its normal demand.

---

## Pollution in the Pipes

Pollution can enter the fresh water supply at the source. A surface pump drawing from contaminated water picks up that pollution and sends it into your distribution network. Once pollution enters the pipes, it propagates downstream — spreading from node to node weighted by how much water is flowing in each direction.

> ℹ️ **Pollution tolerance:** The game flags a building's water supply as "polluted" when the pollution fraction exceeds 10%. Pipes that are no longer actively flowing slowly self-purify over time, so stagnant sections of your network eventually clean up on their own. The pollution spread interval is controlled by a global parameter — under default settings, pollution advances through the network every five simulation cycles.

The only way to protect your supply from pollution is to place pumps upstream of any contamination source, keep industrial polluters away from your water intakes, and ensure sewage outlets are well downstream (or on different waterways entirely).

---

## Sewage: The Return Journey

Every building that receives fresh water also produces sewage. Sewage flows back through the same road-embedded pipes in the opposite direction — the network carries both fresh water and sewage simultaneously, solved independently.

Sewage must eventually reach a **sewage treatment outlet**. At the outlet, sewage is processed and then discharged back into the surface water simulation. A treatment plant with a higher purification rating releases cleaner water; untreated or low-purification outlets dump pollution directly into the waterway at the discharge point.

> ℹ️ **Purification and discharge:** Every outlet has a purification fraction (0 to 1). If an outlet purifies 80% of its flow, the remaining 20% is discharged as polluted surface water. Some facilities can reuse the purified output as a co-located pump, feeding clean water back into the supply network and reducing the demand on your primary pumps.

Buildings need both water service and sewage service to be fully operational. A building that receives fresh water but has no sewage connection, or whose sewage pipe is over capacity, will show a sewage backup problem just as it would show a water shortage.

---

## Surface Water: A Completely Different System

Rivers, lakes, flooding, and tsunamis are simulated entirely separately from your pipe network. The surface water simulation runs on the GPU at 2048x2048 resolution across the entire map. Water flows according to shallow-water physics equations: it responds to terrain elevation, builds up velocity, and spills over obstacles.

Your **pipe network plays no direct role in this simulation**. The only points of contact are pumping stations (which physically remove water from the surface) and sewage outlets (which physically add water back). This means a pump can actually lower the level of a shallow water body if it draws faster than the waterway refills — which is worth watching if you have multiple pumps sharing the same small reservoir.

> ℹ️ **Flooding:** The flood check system evaluates buildings every 16 frames. A building is considered flooded when water depth at its location exceeds 0.5 meters and the water surface is above the building's elevation. Floods trigger evacuation events and can cause building damage. Tsunamis are a separate wave-propagation event with a direction and intensity that plays out over time.

Flooding cannot be resolved by adjusting your pipe network — it is purely a surface water event. The solutions are terrain management, water drainage infrastructure, and ensuring your sewage outlets do not discharge into areas that will back up against your city.

---

## What Can Go Wrong

**Water shortage:** Your pumps cannot produce enough water to meet demand. Causes include pumps placed in shallow or depleted water, insufficient number of pumps, rapid city growth outpacing infrastructure, or a groundwater aquifer that has been drawn down below the minimum threshold. The fix is adding more pump capacity or allowing a depleted aquifer time to recover.

**Disconnected water service:** A building is not connected to any part of the pipe network that reaches a pump. This usually happens after deleting road segments that were the only path between a neighborhood and the water source. The solver identifies disconnected subgraphs and marks them separately from shortage conditions.

**Sewage backup:** The network cannot process sewage fast enough. This happens when treatment plant capacity is too low, when outlets are poorly located (too far from the sewage generators), or when a section of network between buildings and the outlet becomes a bottleneck. Unlike fresh water, sewage **cannot be exported** to a neighboring city — all sewage must be processed locally.

**Polluted water supply:** A pump is drawing from a contaminated source, or an industrial zone is leaching pollution into the groundwater near a pump. Moving intakes upstream or replacing surface pumps with groundwater pumps (in uncontaminated areas) resolves this. Pollution takes time to clear from the pipe network even after the source is removed.

**Flooding unrelated to pipe failure:** If a sewage outlet is discharging large volumes near a low-lying area, or if a tsunami event raises water levels, buildings can flood regardless of how well your pipe network is functioning. These are surface water events and require terrain or infrastructure solutions, not additional sewage capacity.