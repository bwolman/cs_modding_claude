# Outside Connections and Trade in Cities: Skylines II

## What Outside Connections Actually Are

Every city in Cities: Skylines II exists at the edge of a larger world. That world reaches in through a set of physical connection points arrayed around the border of your map — highways, railways, airports, harbors, power line tie-ins, and water/sewer pipe stubs. These are your outside connections: the seams where your city meets everything beyond it.

The six types behave very differently from one another. Highways (road connections) and railways handle both passengers and freight. Airports move passengers and lighter cargo quickly but at much higher cost. Harbors handle heavy bulk freight slowly and cheaply. Power line connections plug your electricity grid into a regional grid that you can draw from or sell back into. Water and sewer pipe connections do the same for fresh water, treated water, and sewage. Each connection type operates on its own logic, but all of them share the same underlying purpose: managing the flow of things across your city's border.

## How Goods Flow In and Out

When an industrial company or commercial business in your city needs a resource it cannot source locally, it dispatches a purchase order that eventually routes to one of the outside connection storage depots at your map edge. The game treats each highway, rail, air, and ship connection as if it were a large warehouse sitting at the city boundary, stocked with whatever the wider world can supply. Your businesses send trucks or trains to retrieve goods from that warehouse, just as they would from any local supplier.

The reverse happens when your city produces a surplus. If a factory makes more goods than local buyers can absorb, it compiles the surplus for export. The game assigns that shipment to an outside connection — choosing the best available based on the transport type that serves the resource being shipped, the weight of the cargo, and the distance cost of reaching that particular connection point from the factory. A freight train carrying heavy steel will prefer a nearby rail connection over a distant airport even if both are technically capable. Lighter, intangible goods like software or financial services have no weight to speak of and simply get assigned to a random available connection.

> **Info:** Trade costs are calculated from two components multiplied together: a weight factor (how heavy the resource is per unit) and a distance factor (how far the goods must travel to reach the border connection). Heavier goods shipped by road cost more than the same goods shipped by train or ship. The four transport modes have different cost multipliers, and the game always routes to the cheapest viable option among connections that support the required transport type.

## Import Versus Export: How Prices Are Determined

The game tracks a running trade balance for each resource — a number that rises when your city is exporting a lot of that resource and falls when your city is importing heavily. This balance directly affects how much trade costs. When you are importing a resource at scale, the game treats that as a signal of local scarcity and the price climbs. When you are exporting heavily, local supply is strong and the export price adjusts accordingly. Both values decay gradually over time (by about 1% every update, and the game runs this calculation 128 times per in-game day), so a shift in your production mix will slowly change your trade prices over the course of several game days rather than immediately.

The smoothed trade costs are refreshed constantly in the background. When a company negotiates a new trade deal with an outside supplier, the current cached cost gets blended with the newly calculated cost using a simple average — so no single transaction can spike your prices wildly. This means that if you suddenly build several large factories all importing the same raw material, the cost increase will be gradual, not a cliff.

> **Info:** City policies can apply modifiers on top of these base trade costs. Import cost modifiers and export cost modifiers are tracked separately, and they stack on top of the transport-mode and resource-weight calculations. A policy that reduces import costs will reduce them for all resources across all transport types.

## Utilities: Electricity, Water, and Sewage

Electricity and water connections work on an entirely different system from goods trade. Rather than dispatching individual vehicles to carry cargo, they hook directly into your city's flow networks — your electricity grid and your water pipe grid. The connection points act as nodes in those grids, capable of either injecting supply (imports) or absorbing output (exports).

The flow solver runs continuously and determines how much electricity or water is actually passing through each connection node in each direction. At regular intervals, the game converts those flow quantities into monetary transactions: you pay for what you import and receive revenue for what you export. Both are billed per unit of flow per game day.

Water trade carries one additional wrinkle: water quality. If your exported water carries too much pollution, the export revenue decreases proportionally. At the game's default pollution tolerance threshold of 10% (meaning more than 10% of the flow is polluted), your export revenue starts to drop. Severely polluted water exports generate essentially no income even though the flow is still leaving your city.

Sewage is export-only. The game has no mechanism for importing sewage from outside. If your sewage treatment cannot keep pace, you cannot buy your way out with an outside connection — you must build more treatment capacity.

> **Info:** You can set a budget cap for electricity and water imports. This cap limits the maximum the city will spend on utility imports per day. If your city's demand for imported utilities exceeds what the budget cap allows, the flow solver will simply pass less through the connection than your grid requires — which means shortages, not overspend. Sizing this cap generously for early game is important if you plan to rely on imported power or water before you have built your own infrastructure.

## How Connection Capacity Is Constrained

Outside connections are not infinitely elastic. A single highway connection can only handle so much traffic before congestion backs up and vehicles begin queuing at the border. The game measures congestion by counting vehicles waiting at each connection node and totalling up their delay times. That aggregate delay gets written back into the pathfinding cost for that connection — meaning the game's routing logic actively steers new shipments away from congested connections and toward less busy ones.

A city that grows heavily dependent on a single highway for all its imports can watch that highway become a bottleneck. Trade deliveries slow down, import costs effectively rise (because vehicles take longer, accumulating time cost), and companies waiting for deliveries may start to experience shortages. The solution is always the same: add more connections, or spread load across transport types. Rail and ship connections handle very high freight volumes without the lane congestion that affects roads, and shifting bulk imports to rail frees up your highways for the lighter, time-sensitive cargo that is harder to move by train.

The pathfinding cost update for congested connections happens roughly 64 times per game day, so the routing adjustments are relatively frequent. The game will self-correct to some extent — once a connection gets expensive due to congestion, traffic naturally migrates toward less congested alternatives — but only if those alternatives exist.

## Tourists and Commuters from Outside

Outside connections are not just for goods. Citizens of the wider world also travel through them.

Tourists arrive through all four physical transport types: by car via highways, by train, by plane via airports, and by ship via harbors. The type of connection determines the traveler profile. Airports attract visitors from further away, which in the game's model means they tend to be wealthier. Ship harbors similarly attract a longer-range, higher-spending visitor mix. Highway and rail connections bring in more ordinary domestic visitors. Hotels and commercial attractions that depend on tourism are therefore somewhat sensitive to which connection types you have built — more airport and harbor capacity generally correlates with higher-spending tourist arrivals.

Commuters from outside the city also use your road and rail connections. People who live beyond your map edge but work in your city travel in via those border points. Their presence is invisible in the sense that you do not see their homes, but they consume road capacity, use your transit if you extend it to the border, and contribute to the congestion at connection nodes. A city that has attracted many external workers for jobs that cannot be filled by local residents will see its highway connections under more pressure from commuter traffic, not just freight.

## How Connection Type Shapes Trade Economics

The four transport modes are not interchangeable. Each has a different cost structure that makes it suited to particular kinds of trade.

Road connections are the default and the most flexible. They handle any type of goods, accept any vehicle, and require no specialized infrastructure beyond a highway stub. Their cost penalty for weight and distance is moderate — not the cheapest, but accessible. Almost every city's early trade happens predominantly by road.

Rail connections have lower per-unit weight costs than road, making them significantly cheaper for heavy bulk goods. A city that exports raw materials or imports large volumes of construction resources will save considerably by routing that traffic through rail rather than road. Rail also absorbs very high volumes without the lane-level congestion that road connections suffer.

Air connections are the most expensive per unit of weight, but they carry no meaningful congestion pressure and are the fastest in terms of delivery speed. They are most efficient for lightweight, high-value goods and for passenger traffic. Shipping steel by plane is extravagant; shipping software licenses or financial services by plane costs relatively little because those resources have negligible weight.

Ship connections (harbors) have the lowest cost multipliers of any transport type, making them the cheapest option for large-volume bulk trade. They are slow in the sense of having long pathfinding distances factored in, but for a city exporting or importing enormous quantities of heavy raw materials, a well-placed harbor can dramatically cut trade expenses compared to road or rail.

> **Info:** When a company is choosing where to send an export shipment, the game evaluates all outside connections that support the needed transport type and selects the one that minimizes total cost — combining the transport mode's weight and distance multipliers with the actual physical distance to each connection node. Having multiple connection types available means your businesses will naturally self-sort: heavy cheap goods drift toward ship and rail, light fast goods drift toward road and air.

## What Can Go Wrong

**Import costs climbing without explanation.** If your city has been importing a resource heavily for several game days, the trade balance for that resource has shifted, and the game has raised import costs in response. This is working as intended — the game treats sustained heavy importing as scarcity and prices it accordingly. The fix is to increase local production of that resource to reduce how much you need to import, which will shift the trade balance back and bring costs down over time.

**Goods deliveries slowing to a crawl.** A congested outside connection raises its own pathfinding cost, but if all your connections of a given transport type are congested simultaneously, there is nowhere for traffic to divert. The game will self-balance among congested connections, but average delivery times will be long until you add more connections or shift some traffic to a different transport type. This tends to show up first as companies reporting input shortages even when the resource is theoretically available.

**Electricity shortages despite paying for imports.** If your import budget cap for electricity is set too low, the game will limit how much flows through the connection even if your grid is in deficit. Check the service import budget setting and increase it if your city is genuinely short on power and you intend to rely on grid imports. The cap is protective against overspend but can silently cause service gaps if it is too tight.

**Water exports generating no revenue.** Polluted water exports pay proportionally less than clean water exports, and at high pollution levels they pay essentially nothing. If your water export connection shows significant flow but minimal income, your treatment infrastructure is not cleaning the water before it leaves the city. Increasing water treatment capacity or reducing the pollution load entering your pipes will restore export revenue.

**Airport and harbor connections under-utilized.** These connection types only attract the traffic that specifically benefits from them — airports excel at lightweight goods and long-range passenger traffic, harbors excel at bulk freight. If you build them in a city that mostly produces lightweight manufactured goods and has no bulk export industry, they may carry very little freight even at full capacity. Connection type utility depends on what your city actually produces and trades.

**Outside commuters overloading highway connections.** If your city has many jobs that cannot be filled by local residents — common in industrial cities that grow faster than their residential population — external commuters will flood your highway connections every morning. This can cause border congestion that spills over into freight delays, because the congestion delay measurement at road connections does not distinguish between a commuter car and a delivery truck. Adding rail commuter access or building more housing to reduce external commuter dependence are the main levers.

**Sewage backing up with no export relief.** Unlike electricity and water, sewage cannot be imported or exported in any meaningful way — there is no outside connection for bringing in sewage treatment capacity. If your treatment plants are overwhelmed, building more of them is the only option. Outside connections will not solve a sewage crisis.
