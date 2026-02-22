## What Natural Resources Are and Where They Come From

Before any industrial building can produce anything, the land underneath it has to offer something worth extracting. Cities: Skylines II places five categories of natural resources across your map — ore, oil, fertile land, timber, and fish — each distributed according to the terrain. These deposits are not uniformly spread. Some maps are rich in oil but have little farmland; others sit atop enormous ore fields. You can reveal where the resources lie by toggling the natural resource overlay on the info views panel, which paints the map in distinct colors for each type.

Ore and oil fields represent finite underground deposits. Timber and fertile land are renewable, though the rate at which they replenish is tied to conditions like temperature. Fish are concentrated in water bodies and harbors. Fish and fertile land are categorized as agricultural raw materials; timber is a forestry raw material; ore, oil, coal, and stone are extraction raw materials that require dedicated mining or drilling operations.

> **Info:** The game tracks five distinct raw-material types that require natural resource deposits: ore, oil (including coal and stone from the same extraction category), timber (logged as Wood before processing), fertile land (supporting grain, vegetables, and livestock), and fish. Each is defined in the game as needing a deposit present beneath the extraction building before production can begin.

## How Industrial Zones Extract Resources

Placing an industrial zone over or near a natural resource deposit causes the game to spawn extractor companies in those buildings — farms over fertile land, logging operations over timber, mines over ore seams, and oil wells over oil fields. The match between zone placement and underlying resource is what determines whether an industrial building becomes an extractor or a manufacturer.

Extractor companies require workers, and their output depends on how many workers they have relative to their capacity, how efficient the building is, and the size of the zoned footprint. A farm covering more cells has a higher maximum worker count and therefore a higher potential output. Workers contribute to production individually, and happier, more-experienced workers contribute somewhat more per person than unhappy or inexperienced ones.

The raw materials these companies produce — grain, wood, ore, oil, livestock, cotton, stone, coal — accumulate in the building's own on-site storage. Once enough has stockpiled, the game begins the process of moving those materials into the broader economy, either to nearby processors or outward to the region via cargo connections.

> **Info:** Each industrial zone cell has a cap on how many workers can be employed per cell. This prevents a single large building footprint from employing an implausibly large workforce. The actual cap varies by industry type and scales with the building's level.

## Resource Depletion Over Time

Ore and oil deposits are consumed as extraction companies draw from them. The game tracks how much of each deposit remains beneath a given area, and as a field is exhausted, extractor companies in that area will eventually find themselves sitting on empty ground. At that point they lose access to the raw material they were drawing on and their output drops to zero, even if the building and its workers are otherwise fine.

Timber and fertile land behave differently. Trees and crops regenerate over time, so forestry and farming operations are theoretically sustainable indefinitely — but only if the rate of extraction does not outpace the rate of regrowth. Temperature affects whether certain crops or trees can grow at all, so regions with harsher climates may see seasonal slowdowns.

The practical implication is that cities with heavy mining or oil extraction should plan for a transition. A city built around ore processing will eventually need to import ore from outside the region once local deposits thin out, or shift its industrial base toward goods that do not require local extraction.

## From Raw Material to Finished Good: The Processing Chain

Raw materials almost never go directly to consumers. They pass through one or more stages of industrial processing before reaching the shops and services that residents actually use. The game defines these production chains through recipes: each industrial company type takes in specific inputs and converts them into a specific output.

Wood harvested from forests, for example, is processed into timber, which can then become paper or furniture. Ore from mines becomes metals, which in turn become steel, machinery, or electronics. Oil is refined into petrochemicals, which feed into plastics, chemicals, and pharmaceuticals. Grain grown on farmland goes to food processing to become convenience food, food, or beverages. Cotton becomes textiles. Livestock supports food production alongside grain.

Each step in the chain adds value. A furniture factory buying timber and turning it into finished furniture is selling a product worth more than the raw wood it consumed. This is also where commercial zones enter: commercial buildings are the final step in most chains, buying manufactured goods from industrial companies and selling them at a markup to residents and visitors.

> **Info:** The game internally distinguishes between an industrial price (what companies charge each other for goods) and a commercial markup (the additional margin a store earns on top of that base price). Consumers pay the combined total. This two-tier pricing is baked into every resource's definition.

Office-sector output — software, financial services, telecom, media — follows a similar logic but without physical goods. These are "weightless" resources: they are traded and consumed but do not require physical transport, so they flow through the economy without trucks ever needing to move them.

## How Storage and Surplus Work

Every company, whether a farm or a furniture factory, holds its inventory on-site. There are limits to how much a company can stockpile before the game decides the surplus needs to move. When a company's storage reaches or exceeds a threshold, it triggers an export attempt — the company tries to find a buyer, whether that is a downstream manufacturer in the city, a commercial building that wants the finished goods, or an outside connection to the wider region.

The same applies to city service buildings that produce resources as a byproduct of their operation: when accumulated output crosses the storage threshold or hits 20,000 units, whichever comes first, an export is triggered automatically.

Companies that are producing efficiently but cannot find buyers nearby will see their storage fill up. A clogged supply chain — not enough processors to absorb raw materials, or not enough commercial buyers for manufactured goods — will suppress production upstream as companies run out of room to put their output.

> **Info:** The storage ceiling for non-money inventories has an upper limit. Any given resource entry in a company's or building's inventory cannot exceed 1,000,000 units. In practice, export triggers well before this point.

## The Role of Cargo Transport

Physical goods need physical transport. When a company has surplus goods to move, it dispatches delivery trucks to carry them to buyers or to outside connections. The pathfinding system finds the most efficient route based on distance and road network availability, and the cost of transport itself is factored into trade decisions — distance, the physical weight of the resource, and the volume being moved all contribute to transport cost.

Heavy raw materials like ore and stone are expensive to move long distances, which naturally favors locating processing plants close to extraction sites. Lighter manufactured goods like electronics or pharmaceuticals are cheaper per unit to ship, giving finished-goods manufacturers more flexibility in where they operate relative to their input suppliers.

Outside connections — the highway interchanges and cargo terminals at the edges of your map — serve as the entry and exit points for regional trade. Goods your city cannot absorb internally move out through these connections; goods your city needs but cannot produce domestically arrive the same way. Cargo rail and harbor connections function similarly but handle bulk shipments more efficiently than road transport for certain industries.

Weightless office resources — software, financial services, telecom, and media — skip this physical transport entirely. They are traded and tracked economically but do not require a truck to leave a depot.

> **Info:** The transport cost formula weighs three factors: the distance traveled, the physical weight of the resource, and the total amount being moved. Specifically, a larger shipment costs proportionally more to transport, not just distance-multiplied. This means large bulk shipments of heavy materials over long distances are genuinely expensive, not just abstractly "inefficient."

## Reading the Resource Map

The natural resource overlay is your primary planning tool before you zone any industry. Each resource type renders as a distinct color across the terrain, with intensity indicating concentration. A deep, solid color means a rich deposit; a faint wash means a thin or marginal deposit that will exhaust relatively quickly or support only modest extraction.

Beyond the natural resource map, the game also tracks availability of processed goods across the road network. Resources "flow" outward from where they are produced or sold, spreading a distance-weighted availability score along roads. Citizens looking for a shop, or companies looking for an input supplier, consult this availability signal to determine where to go. A neighborhood with poor road connections to commercial or industrial zones will show up as resource-starved even if those zones technically exist somewhere in the city.

You can observe this in practice when commercial zones fail to attract customers: if residents cannot easily reach shops by road, the commercial zone effectively does not exist for them. The same logic applies to industrial supply chains — a manufacturer positioned far from its raw material sources, with poor road connectivity, will pay high transport costs and may lose out to competing import sources.

## What Can Go Wrong

**Raw resources run out before your processing industry is established.** If you build mines and oil wells but no downstream processing, you are exporting raw materials at low industrial prices. Worse, when the deposit thins out, any processing industry you eventually build will face supply shortages and be forced to import at higher cost.

**Processing bottlenecks stall the entire chain.** If grain production far exceeds food processing capacity, farms fill up their storage and throttle production. The excess is exported raw rather than turned into higher-value goods. Meanwhile, commercial food stores may simultaneously struggle to find product, importing it from outside at a markup, because the local processing sector is missing or undersized.

**Poor cargo connections choke exports.** Industrial companies with full storage and nowhere to send their surplus will stop producing efficiently. If your highway connections are congested or your cargo rail network is underdeveloped, trucks cannot complete export runs fast enough and the whole supply chain backs up.

**Finished goods cannot reach commercial zones.** Commercial stores need to buy their inventory from nearby industrial suppliers or from outside connections. If manufacturers are physically isolated from commercial districts — on the far side of the city with no direct road link — commercial stores will import more expensively, raising the prices residents pay and suppressing shopping activity.

**Ore or oil deposits exhaust without a transition plan.** A city that built its identity around ore processing will face a crisis when local deposits run dry. Import costs replace free local extraction, squeezing company margins. Without preparation — diversifying into renewable industries, or locking in good outside connection capacity — the shock can collapse the industrial tax base.

**Temperature blocks agricultural production.** Certain crops and farming operations require a minimum temperature to function. In cities with cold climates or harsh winters, agricultural extraction may effectively shut down seasonally. Cities dependent on local grain or livestock production should account for seasonal supply gaps by ensuring good import connections or maintaining larger commercial inventories.

**Office-sector goods appear "missing" from the chain.** Software, financial services, telecom, and media are legitimate resources in the economy — companies buy and sell them, and they appear in trade statistics — but they will never show up as truck traffic or cargo flows. If you are diagnosing a supply problem and expecting to see trucks moving these goods, you will not find them. Their trade is entirely virtual.
