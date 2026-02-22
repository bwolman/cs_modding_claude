# Zoning & Building Development

## How Zoning Works

When you draw roads in Cities: Skylines II, the game automatically creates an invisible grid of potential building plots on each side of every road. You can't see this grid until you paint it with a zone type — that's what the zone tool does. Painting a cell tells the game that this piece of land is designated for residential, commercial, or industrial use. Until you do that, the land is just empty space, and nothing will ever build on it no matter how long you wait.

The grid itself has fixed dimensions: each cell is 8 meters by 8 meters, blocks extend up to 6 cells deep (48 meters) from the road, and up to 10 cells wide per segment. These boundaries are hardcoded into the game and can't be changed without mods. When you lay down a long road, the game divides it into multiple overlapping blocks to cover the full length.

> **ℹ️ Info:** Only roads that support zoning generate these plot grids. Elevated roads, tunnels, and certain road types deliberately disable zoning. If a road you place doesn't show the zone tool working beside it, the road type isn't zoneable.

---

## Demand: The Engine Behind Development

Painting zone cells is necessary but not sufficient for buildings to appear. The game tracks separate demand levels for residential, commercial, and industrial development, and buildings will only spawn on zoned lots when their corresponding demand is positive. A zone full of empty painted cells is just the city's way of advertising "we're open for this type of business" — the actual tenants (buildings) arrive when there's economic reason for them to.

Demand builds up over time based on what your city needs. Residential demand rises when there aren't enough homes to house the workers your city's economy wants to attract. Commercial demand rises when there are residents who need services and shops. Industrial demand rises when the economy needs production and storage capacity. These three demand signals are always pushing and pulling against each other, and the game uses them to decide which type of building to prioritize filling in next.

> **ℹ️ Info:** The spawn system runs on a cycle of roughly 16 game frames and places at most 3 buildings per cycle — one residential, one commercial, and one industrial. Development will never fill in a whole neighborhood simultaneously. It grows in a drip, responding incrementally to demand.

---

## When a Building Actually Appears

Once you've painted zone cells and the relevant demand exists, the game's development process kicks in. The game scans for what it calls "vacant lots" — contiguous groups of painted, unoccupied cells that form a suitable plot for a building. For a lot to qualify, it needs to be visible (painted), unblocked by terrain or other objects, and it must include at least one cell directly adjacent to a road. A lot hidden behind another building with no road access will never develop.

The game then looks through its library of building templates for that zone type and finds one that fits the available lot. Several things must line up: the building's footprint must fit within the vacant lot, the building's height must not exceed the height limit set by the zone type, and the lot must be at least wide enough to accommodate the building style. Only freshly constructed buildings (those at their base level 1) are candidates for placement — the game never teleports a pre-leveled building into an empty lot.

Buildings are also matched to the specific density and zone sub-type you painted. A low-density residential zone produces detached houses; medium-density produces row homes and townhouses; high-density produces apartment buildings. Painting the wrong density for what you want is one of the most common reasons development doesn't look the way you expect.

> **ℹ️ Info:** Low-density residential zones do not support buildings that are only one cell wide. That building style (row homes) is reserved for medium-density zones. If you paint low-density and have a very narrow lot, the game may skip it entirely until the lot widens through adjacent development.

---

## Building Levels and Upgrades

Every zoned building starts at level 1. This is intentional — the game treats new construction as fresh, unimproved development. Over time, buildings can level up, improving in appearance, capacity, and value. The game supports up to five levels for most building types.

What drives a building upward through those levels is a combination of factors centered on land value and service quality. Land value in a neighborhood rises when residents and workers have good access to parks, schools, healthcare, transit, and other services. It also rises when the surrounding area is well-developed and desirable. When land value at a building's location crosses certain thresholds, the game triggers a level-up: the building goes through a brief construction phase and emerges as a higher-level version.

The level-up isn't just cosmetic. Higher-level buildings hold more residents per lot, or generate more commercial activity, than their lower-level counterparts. A level 1 apartment building might support a handful of households; the same footprint at level 4 or 5 might support many more. This means that improving city services doesn't just make citizens happier — it also effectively increases your city's housing capacity and commercial output without requiring you to zone more land.

> **ℹ️ Info:** When a building levels up, the game marks it as "under construction" and replaces it with the higher-level version. This construction phase is brief but visible. You'll see the site briefly change before the upgraded building appears.

---

## Lot Coverage and Building Footprints

Not every vacant lot gets filled with a perfectly matched building. The game scores candidate buildings partly based on how well their footprint covers the available lot — a building that fills the lot completely scores higher than one that leaves a large empty gap. This coverage preference shapes the feel of your neighborhoods organically: tighter lots tend to attract buildings that fill them snugly, while wide, deep lots tend to attract larger structures.

When no building in the game's library fits the lot well, the lot may sit empty for a while. This is especially common with unusually shaped lots created by diagonal roads, irregular intersections, or lots squeezed between pre-existing buildings. The game isn't ignoring these lots — it's scoring them and finding the best available fit, but sometimes the best fit still isn't great enough to trigger immediate construction.

> **ℹ️ Info:** The scoring formula favors buildings that cover the lot cleanly and weights this by the current demand level. High demand makes the game more willing to accept imperfect lot fits. In a rapidly growing city, awkward lots fill faster than in a slow-growth city even if nothing else changes.

---

## Zone Compatibility and Condemnation

The relationship between a building and its zone type is maintained throughout the building's life. If you erase zone paint or repaint with a different type after buildings already exist, the game notices. Buildings that sit on cells where the zone no longer matches — or where there are no longer any road-adjacent cells — are eventually marked as condemned.

Condemnation doesn't demolish a building immediately. It flags the building with a warning and starts a separate process that leads to eventual demolition. The building will continue to function for a time. Removing the condemnation is possible by repainting the correct zone type, which clears the flag. If you accidentally rezone a built-up area and see condemnation warnings, repainting the right zone before the demolition process completes will save the buildings.

---

## What Can Go Wrong

**Painted zones that stay empty for a long time:**
The most common cause is low demand. The game won't build on a lot when demand for that zone type is negative or near zero. Check the demand indicators (the bar graphs in the zone info UI). If demand is flat or declining, focus on city growth — more residents means more demand for commercial, more commerce means more jobs, more jobs means more housing demand. You can also check whether the lots have road access: cells that aren't adjacent to a road are invisible to the spawn system.

**Low-density zones that look sparsely developed:**
Low-density zones produce detached houses and small buildings that don't fill lots wall-to-wall by design. If a lot is narrow (one cell wide), low-density zones can't fill it at all, since that building style requires two or more cells of width. Widening the block by setting back adjacent structures, or switching to medium-density zoning, will get those narrow lots filled.

**Buildings that stay at level 1 indefinitely:**
Level-ups require land value to rise, which requires nearby services. If a residential neighborhood has no parks, poor transit access, no schools or healthcare nearby, land value will stay low and buildings won't progress. The fix is investment: add parks, improve transit coverage, make sure schools and healthcare can reach the area. Pollution — ground, air, or noise — actively suppresses land value, so locating industrial zones upwind of residential areas also prevents development stagnation.

**A filled neighborhood that suddenly shows condemnation warnings:**
You or a tool accidentally repainted part of the zone. This can happen when laying new roads through developed areas (the zone tool can activate automatically) or when using roads that alter the zone boundary. Repainting the correct zone type in the affected cells removes the condemnation warnings before the buildings are demolished.

**Large empty lots that never develop despite high demand:**
Very large lots — particularly those created by deep blocks or wide frontages — can sit empty because no building in the library fits well enough. The game scores lot coverage and may delay filling oversized lots. Breaking the lot into smaller parcels by placing side streets can help, since narrower, shallower blocks produce lot shapes that match the available building pool more closely. Deep lots (further from the road) are particularly prone to this: the game's default block depth reaches 48 meters from the road, but most building footprints are designed for shallower lots.
