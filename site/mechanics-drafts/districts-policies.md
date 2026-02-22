## Districts and Policies

### What a District Actually Is

A district is a painted region on your map. You draw the boundary by placing the district tool and tracing out an area — roads, terrain features, and zone types inside that boundary do not matter, and the shape can be as irregular as you like. Once you finish painting, the game records that polygon as a distinct entity in the simulation. It has a name, an optional color you can assign, and — most importantly — a list of active policies.

Nothing about being inside a district automatically changes how buildings behave. A building inside a district is simply tagged as belonging to that district. The tag means that any policies you have enabled on the district will apply to that building. Remove the district, or repaint the boundary so the building falls outside it, and those effects disappear.

Districts serve two practical purposes that players often mix up: organization and policy application. You can paint a district purely to give an area a name and make it easier to find in the city map — that is organization. Or you can paint a district specifically so you can attach policies to it that change how the simulation behaves there — that is policy application. There is no mechanical difference between these uses; the game does not distinguish between a "label-only" district and a "real" one. Any district can have policies, and any district without policies is simply a named region.

---

### How the Game Knows What Is Inside a District

When you finish painting a district, the game immediately evaluates every building, road, and other placed object on your map. For each one, it tests whether that object's position falls inside the district's polygon. Objects that pass the test are recorded as belonging to that district. This mapping updates whenever you repaint or delete a district boundary, so if you expand a district to include a new neighborhood, those buildings are tagged automatically within the same simulation tick.

Roads sit on the boundary between zones rather than cleanly inside any single area, so the game handles them differently. Each road edge knows which district lies to its left and which lies to its right. This matters for traffic policies: a road bordering two districts, or bordering a district on one side and open city on the other, can be governed by different rules depending on the direction of travel.

---

### What Policies Do

Policies are toggles and sliders that modify how the simulation runs in a defined area. Some flip a behavior on or off — forbidding heavy trucks from entering, enabling paid parking, banning combustion engine vehicles. Others adjust a numeric value up or down along a scale — lowering the speed limit by a percentage, increasing or decreasing commercial taxes, scaling how much garbage buildings in the area produce.

The game organizes policies into categories — City Planning, Budget, Traffic, Culture, and Services — but all of them work through the same underlying mechanism. When you enable a policy, the game recalculates a set of numeric modifiers stored on the district. Those modifiers are then read continuously by the rest of the simulation: traffic routing checks the speed limit modifier when planning routes, fire risk calculations check the building fire hazard modifier, and so on. Disabling a policy instantly removes its contribution from those modifiers and the simulation adjusts in the next update cycle.

> **ℹ️ Info — The full list of simulation values that district policies can modify:**
> Garbage production, commercial product consumption, parking fees, building fire hazard, fire response time, building upkeep costs, commercial tax rate, citizen wellbeing, crime accumulation rate, street speed limits, traffic safety, energy consumption awareness, and the probability that citizens reserve cars when traveling. Traffic-restriction policies (paid parking, banning combustion engines, banning through-traffic, banning heavy vehicles, banning bicycles) are handled as simple on/off flags rather than numeric modifiers.

When you enable a slider policy, the value you set is interpolated across the policy's allowed range to produce the final modifier. Setting a speed limit slider to the midpoint of its range produces a modifier halfway between the minimum and maximum effect. The game supports three ways a modifier can be combined with the existing value: a relative adjustment (percentage change), an absolute adjustment (flat addition or subtraction), or an inverse relative adjustment used for effects that should diminish rather than compound.

---

### City-Wide vs. District-Level Policies

Not all policies attach to districts. The game has four distinct scopes for policies: districts, individual service buildings, transit routes, and the entire city.

City-wide policies apply everywhere without exception. You set them from the city policy panel rather than from a district's settings, and their effects cover every building and road on the map. A city-wide commercial tax rate change, for example, affects every commercial zone in the city regardless of whether those zones are inside any district at all.

District policies apply only within the drawn boundary. The same policy — say, a reduced speed limit — can be set differently in different districts. One neighborhood might have a reduced limit for pedestrian safety while another runs at the city default. Districts do not inherit city-wide modifier values as a baseline that you are adjusting; they maintain their own modifier buffers that are calculated independently from the active policies on that specific district.

Service building policies and transit route policies operate at an even finer granularity. A policy applied to a specific fire station or bus line affects only that building or route, not the whole district it happens to sit in. This lets you tune individual services without painting district boundaries.

When a district policy and a city-wide policy both affect the same value, the game applies both modifiers. They stack rather than override each other, so a district that lowers garbage production on top of a city-wide garbage reduction will compound the effect. Whether a specific policy is available at the district level, the city level, or both depends on how the policy was defined in the game's data; the interface will only show you the policies relevant to the scope you are currently editing.

---

### Service Districts: Which Buildings a Service Covers

Service districts are a separate concept from policy districts, though they use the same painted-area tooling. When you paint a service district and assign a service building to it — a fire station, a hospital, a police station — you are telling that building to prioritize coverage within that region.

Without a service district assignment, a service building covers everything within its effective range, sending vehicles to any compatible request it receives regardless of geographic location. With a service district assignment, the building focuses on its designated area. A fire station assigned to a residential district will preferentially dispatch to buildings inside that district boundary, and requests from outside may go unserved or be handled by a different station.

A single service building can be assigned to multiple service districts, and a single service district can have multiple buildings assigned to it. The assignment is not exclusive — buildings outside the district boundary can still receive service if the building has capacity and the game cannot find a closer available responder, but the district assignment shifts the dispatch priority.

> **ℹ️ Info — How service district assignment works under the hood:**
> Each service building maintains a list of the service districts it is assigned to. When the building is deleted, all of its service district assignments are automatically cleaned up. The assignment affects dispatch logic; it does not create a hard wall that prevents service from crossing the boundary.

This mechanism is particularly useful in large cities where a single district might have multiple hospitals or fire stations and you want to ensure coverage stays local rather than having one station run across the city to answer a call while its own neighborhood is underserved.

---

### The Cost of Policies

Policies that provide economic benefits or impose service demands generally carry an upkeep cost. The cost appears in the policy panel and is deducted from your weekly budget as long as the policy remains active. Disabling the policy removes the cost immediately.

Policies that restrict behavior — traffic bans, combustion engine prohibitions — typically have no direct monetary cost but impose indirect costs through their effects on the economy. Forbidding heavy trucks may reduce delivery efficiency for industry and commercial zones. Banning combustion engines will push residents toward public transit or electric vehicles, which affects traffic patterns and potentially reduces land value pressure in some areas. These second-order effects do not appear as a line in your budget, but they show up in zone happiness, commercial performance, and traffic volume over time.

The game does not warn you about these indirect effects when you enable a policy. You will see the direct upkeep cost clearly, but you will only discover the behavioral downstream effects by watching your city's response over several in-game months.

---

### What Can Go Wrong

**Policies applying to unintended buildings:** District boundaries follow the paint path you drew, not road center lines or zone borders. If your boundary clips through the middle of a block, buildings on the clipped side will be inside the district even if you intended them to be outside. Zoom in to the boundary edges before enabling restrictive policies to confirm exactly which buildings fall inside.

**Service district assignments creating coverage gaps:** Assigning every service building in your city to strict service districts can leave areas uncovered if the buildings are tuned too narrowly. A hospital assigned only to the eastern residential district will not respond to emergencies in the adjacent industrial zone unless another building covers that area. Service districts are meant to prioritize, not to hard-limit; but if your assignments become too granular and your service buildings too few, practical gaps appear in coverage.

**Stacked modifiers producing extreme values:** A district-level garbage reduction policy stacked on top of a city-wide garbage reduction can push the combined modifier to a floor value that effectively zeroes out garbage production in that district. This sounds beneficial but can produce odd simulation behavior — recycling center demand dropping to nothing, workers at waste facilities becoming unnecessary — that ripples outward in ways that are hard to untangle later.

**Traffic bans breaking delivery routes:** Forbidding heavy vehicles or through-traffic in a district affects the pathfinding that delivery trucks and cargo vehicles use when planning routes. If you apply a heavy vehicle ban to a district that sits between an industrial zone and a cargo hub, deliveries may reroute significantly, adding travel time and increasing road load on alternative paths. The rerouting happens automatically but the game does not alert you to it.

**Deleting a district removing active policies:** When you delete a district, all of its policies are removed instantly. Any simulation effects those policies were producing — reduced crime accumulation, adjusted tax rates, traffic restrictions — disappear in the same tick. If the district had been enforcing a speed reduction near a school zone, traffic in that area immediately returns to the city default. There is no confirmation step or grace period.

**Overlapping districts creating confusion:** The game allows painted districts to overlap. If two districts cover the same building, that building is tagged as belonging to both districts, and the policies of both districts apply to it. Two districts with conflicting policies on the same value — one increasing parking fees and one decreasing them — will stack their effects rather than one overriding the other. The resulting value can be unexpected if you are not tracking all active overlaps.
