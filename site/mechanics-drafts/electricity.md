# Electricity in Cities: Skylines II

## How Power Gets Made

Every building in your city that needs electricity — every house, shop, school, and factory — draws from a single interconnected power grid. Power enters that grid from your generating stations: coal and gas plants burn fuel for a steady, predictable output; nuclear plants produce enormous amounts with no weather dependency; solar farms scale with how sunny and clear the sky is; wind turbines spin faster as wind speed climbs; hydroelectric dams harvest the flow and elevation drop of a river; and waste-to-energy plants consume garbage to produce power as a bonus side effect.

Renewable sources are genuinely variable. A solar farm on a cloudy day produces a fraction of its clear-sky maximum. A wind turbine in a calm period can sit nearly idle. This is not just flavor — the simulation actively recalculates every plant's output based on current weather conditions. The capacity number you see in the electricity infoview reflects what the plant is actually capable of generating right now, not a theoretical nameplate figure.

> **Info**: Fuel-based plants (coal, gas, nuclear) produce at a constant rate as long as their resource supply is above zero. Dynamic plants (solar, wind, hydro) have no fixed output in their data — their capacity is calculated entirely at runtime from weather and geography. Never trust a dynamic plant's listed capacity as a guarantee.

## How the Grid Connects Buildings

Power flows through your city along two kinds of infrastructure: power lines you place explicitly, and the roads themselves. Roads carry electricity at lower capacity as part of their normal function, which is why a straightforward residential neighborhood often needs no power lines at all — as long as every block is connected to the road network, power reaches every building automatically.

Power lines are for longer distances, higher loads, and situations where roads alone cannot carry enough current. High-voltage transmission lines move bulk power across large distances; lower-voltage distribution lines carry it to local streets. The grid is one continuous graph, and the game solves for the actual flow of electricity across every connection every 128 simulation frames — roughly once per in-game half-hour.

> **Info**: The flow solve runs on a cycle: first, every building's demand is recalculated based on temperature, occupancy, and local service fees; then the solver distributes available generation to meet that demand; then results are applied and notifications are updated. The whole cycle takes about 1.5 real-time seconds at normal speed.

## Demand Is Not Fixed

A building's electricity demand changes constantly based on several factors. Cold weather drives heating loads up — the game applies a temperature multiplier curve to every building's base consumption. How many people are actually inside matters too: an empty residential building consumes roughly one-tenth of what a fully occupied one does. Raising the electricity service fee citywide reduces consumption modestly, as does applying the Energy Consumption Awareness district modifier to a neighborhood.

This means your grid load fluctuates with the seasons and the growth of your city. A grid that is comfortably supplied in summer may be strained in a cold winter.

## Battery Storage

Battery facilities act as a buffer between generation and demand. When the grid has surplus power — more being produced than consumed — batteries absorb the excess and store it. When demand exceeds generation, batteries discharge and make up the difference. They cannot discharge more than they currently have stored, and they cannot charge beyond their capacity.

> **Info**: A battery's maximum discharge rate is capped both by its design output and by its current charge level. A nearly-empty battery cannot supply full power even if demand is high. Batteries start each new game with a partial initial charge.

Emergency generators are an upgrade component for battery buildings. When a battery's stored charge drops below a low threshold, the emergency generator kicks on as a backup. It shuts off again once the battery has recharged above a higher threshold. This provides a safety net during sustained generation shortfalls.

## What Happens to Buildings Without Power

When a building receives less electricity than it needs for two consecutive solver cycles, the game marks it with a power problem. There are two distinct warning states:

- **No electricity**: The building has no power at all. Residents complain, workers cannot function, production halts, and services degrade. The building's overall efficiency drops, which ripples into land value and happiness.
- **Bottleneck warning**: The building is connected to the grid but is on the far side of a congested link — a road or power line that is carrying as much as it can. Power is available upstream, but it cannot all get through. Adding more transmission capacity (a parallel line, a higher-voltage route) is the fix, not more generation.

Both conditions show notification icons on the buildings. The electricity infoview highlights congested links in your network, which helps locate exactly where the bottleneck is sitting.

> **Info**: An inactive or abandoned building still draws a small amount of power — about one-tenth of normal — rather than zero. A fully vacant building still registers on the grid.

## What Can Go Wrong

**Cascading winter demand spike.** A cold snap simultaneously increases every building's consumption. A grid that ran at 80% capacity in summer can exceed 100% in winter. Keep a margin of at least 20-25% spare capacity, or use battery storage to cover the peaks.

**Renewable overconfidence.** Solar and wind capacity you see in the infoview is live output, not guaranteed. A city relying heavily on renewables without storage or backup plants can drop into a deficit whenever weather turns bad.

**Bottleneck hidden behind adequate totals.** The city-wide production and consumption numbers can look healthy while a specific district is blacked out. The bottleneck warning only appears on the buildings experiencing it; check the infoview regularly when expanding into new areas rather than only looking at headline numbers.

**Battery runs empty during a generation gap.** If your storage is undersized relative to how long your generation shortfall lasts — a long overcast period with heavy solar reliance, for instance — batteries drain completely and the emergency generator, if present, cannot sustain the full load. Stagger your renewable types so that calm, cloudy weather is compensated by another source.

**Road network carrying too much electrical load.** Roads have a finite capacity for electricity. A dense commercial or industrial district served only by roads, with no direct power lines, can saturate those road connections and leave some buildings bottlenecked even when total supply is sufficient. Run dedicated power lines to high-density zones.