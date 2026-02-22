# Building Construction and Leveling in Cities: Skylines II

## From Empty Lot to Finished Building

When your city has enough demand in a zone — say, a residential or commercial district — the game scans for vacant lots and picks one to develop. A building prefab matching the zone type is chosen (always starting at level 1), and construction begins.

The moment a building is placed, cranes appear on the site. Construction progresses in ticks rather than in real time, and each building gets a randomly assigned construction speed. Some buildings shoot up fast; others take their time.

> **Info:** Each new building is assigned a random construction speed between 39 and 89 (on an internal scale). At maximum speed, a building can complete construction in roughly 128 simulation frames. At minimum speed it takes around 576 frames. The game checks and advances all active construction sites every 64 frames.

Once construction is complete, the scaffolding and cranes vanish, the finished building appears, and residents or businesses begin moving in. Utility connections — electricity, water, sewage, garbage — activate automatically based on what the building needs.

---

## How Buildings Level Up

Leveling is driven by a building's condition score, which rises when residents or businesses are happy and falls when they are struggling. Land value, good services, low crime, and reliable utilities all push the condition upward over time.

When a building accumulates enough positive condition to cross a leveling threshold, it upgrades to the next tier. The building does not go through a slow construction phase when leveling — the swap is essentially instant. The game internally marks the building as "upgrading to a new prefab," and on the very next simulation pass it becomes the higher-level version.

> **Info:** Level-up construction is set to instant completion (maximum progress from the start). In contrast, new constructions begin at zero progress and work their way up. Both use the same construction pipeline; only the starting progress value differs.

Zoned buildings can reach up to level 5. Each level uses a physically larger or more detailed building model — the lot size and footprint stay the same, but the appearance and capacity change. The game only ever spawns fresh buildings at level 1; higher levels are earned through play.

---

## Service Building Upgrades

Service buildings — fire stations, police stations, schools, hospitals — can be extended with upgrades rather than replaced entirely. You place an upgrade structure adjacent to the main building using the upgrade tool. Once placed, the upgrade counts as part of the building and adds to its capacity, coverage, or efficiency.

> **Info:** Some upgrades can only be installed once per building. Each upgrade has a monetary cost and awards a small amount of progression experience when installed. The upgrade's footprint is placed at a fixed offset from the parent building.

---

## What Causes Abandonment

A building's condition can also fall. Persistent problems — high rent that renters cannot afford, poor services, crime, lack of jobs — drag condition downward. If condition drops far enough into negative territory, the building is abandoned.

When abandonment happens, the building is immediately vacated. Electricity, water, and other utility connections are cut. Crime in the building doubles (abandoned structures attract trouble). The building sits empty, visually decaying, with an abandonment icon overhead.

> **Info:** Once a building is marked abandoned, the game starts a timer. After a set delay (measured in simulation frames), the abandoned building collapses into a ruin. The ruin remains on the lot until cleared. Abandoned buildings that have not yet collapsed can in principle be restored, but the game does not do this automatically — without mod intervention, the collapse timer runs to zero.

---

## Condemnation

Condemnation is different from abandonment. A building becomes condemned when the zone beneath it changes. If you repaint a residential zone as commercial while buildings are already standing there, those buildings no longer match their zone. The game flags them as condemned.

> **Info:** Condemned buildings are not removed immediately. The game checks each condemned building periodically and applies a 25% chance of demolition on each check. Checks happen roughly every 1,024 simulation frames per building, so on average a condemned building survives about four checks before being removed — though it could go much sooner or later due to the random roll.

---

## Manual Demolition

Using the bulldoze tool on any building removes it instantly. The lot is cleared, zone cells are freed, occupants are evicted, and the building disappears without a collapse animation. Buildings under construction can be bulldozed just like finished ones.

---

## What Can Go Wrong

**Buildings stuck under construction.** Construction requires the game's simulation to be running. Pausing or running at very low speeds slows construction visibly. If a building appears frozen mid-construction, it is likely waiting on a simulation tick.

**Abandonment cascades.** One abandoned building raises local crime, which harms nearby building conditions, which can trigger further abandonments in the same area. A small problem in a neglected district can ripple outward if left unaddressed.

**Condemned buildings lingering.** After you repaint a zone, the buildings that used to stand there do not vanish right away. Expect them to hang around for several in-game cycles before the condemnation rolls go their way. This is normal and not a bug.

**Level-up not happening despite good conditions.** Leveling requires sustained positive building condition, not just a momentary spike. Short bursts of good service followed by extended neglect reset progress. Consistent city services, stable land value, and affordable rent all matter.

**Ruins blocking redevelopment.** A collapsed abandoned building leaves rubble on the lot. Until that rubble is cleared — manually or by your city's maintenance crews — the zone cells remain occupied and new construction cannot begin on that spot.
