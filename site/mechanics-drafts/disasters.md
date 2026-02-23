# Disasters and Emergency Response

Natural disasters in Cities: Skylines II are not just cosmetic events — they are fully simulated threats that damage your buildings, displace your citizens, and stress your emergency services. Understanding how the game handles them helps you build a more resilient city and respond effectively when things go wrong.

---

## Types of Disasters

The game simulates two fundamentally different kinds of disasters, each driven by its own ruleset.

**Weather phenomena** — tornadoes and severe storms — appear as moving events that travel across the map, pushed along by the current wind. Each one has an outer weather system (the visible storm) and a tighter damage zone at its core. Buildings caught inside that inner core start taking structural damage. Storms also throw lightning at the tallest structures in range, and may spin up secondary fires from those strikes.

**Floods and tsunamis** work differently. Rather than a moving damage zone, the threat is rising water. When a flood or tsunami event begins, the water level climbs and the simulation constantly checks every building's elevation against the actual water surface. Once water is more than about half a meter above the surrounding terrain at a building's footprint, that building is considered flooded and starts accumulating damage.

---

## How Disasters Are Triggered

Weather phenomena occur when atmospheric conditions are right — the game tracks temperature, rainfall, and cloud cover, and each storm type has a range of conditions under which it can appear. Once active, a tornado drifts with the wind, with its damage core oscillating unpredictably.

Floods are driven by water level change events. A tsunami arrives as a wave (rising and falling in a sine pattern); a river flood responds to rainfall accumulation. Both types can target sea-level water bodies, rivers, or the entire water system, depending on the event configuration.

---

## The Warning Window

Before the damage zone actually reaches your buildings, the game looks ahead along the storm's projected path and marks buildings in that corridor as endangered. This triggers evacuation orders while the disaster is still approaching. The length of that warning window is influenced by any Early Disaster Warning buildings you have placed — those facilities extend the advance notice your city receives.

> **Info**: The disaster warning time is a city-wide modifier. Each Early Disaster Warning facility you build increases the lead time before the danger zone arrives, giving evacuation vehicles more time to complete their runs.

---

## Early Warning Facilities

Early Disaster Warning facilities are city buildings you construct and place like any other service. Each one adds a flat bonus to the city-wide disaster warning time modifier. The more of these facilities you have operational, the longer the gap between the danger zone being projected and it actually arriving at your buildings.

The practical value is evacuation time. With only the base warning time, evacuation vehicles may still be mid-route when the storm arrives. Each Early Warning facility you build extends the runway — the difference between citizens safely reaching a shelter and being caught in a collapsing building can be one or two of these facilities.

Early Warning facilities require power and staff to function. An unstaffed or unpowered facility contributes zero warning bonus. Placing them in areas likely to be hit early — coastal zones, areas near river flood plains — is strategically sound, but they must remain operational to count.

> **Info**: The warning time modifier is a city-wide value, not a per-area one. Every building in your city benefits equally from each Early Warning facility you have running, regardless of where the facility is physically located. The only thing that matters is that the facility is powered and staffed.

---

## Evacuation

When a building is placed under an evacuation order, citizens inside receive one of two instructions: shelter in place (stay indoors) or evacuate. If evacuation vehicles are available, citizens wait for a bus or transport to collect them before heading to an emergency shelter. If no vehicle is coming, they head to the shelter on their own.

Emergency shelters are the destination for all displaced citizens. They have a finite capacity. Citizens inside an active shelter will only leave once the danger level has dropped sufficiently — the higher the remaining danger, the less likely any individual citizen is to exit early. If a shelter becomes inoperable (damaged or understaffed), citizens are more likely to leave regardless.

> **Info**: Citizens already dealing with a health problem are excluded from evacuation processing — they remain in place and rely on healthcare services rather than emergency transport.

---

## Shelter Capacity Planning

Emergency shelters have a fixed bed count. When that count is reached, the shelter stops accepting new evacuees — citizens directed to a full shelter will try to find another one, or remain in danger if no alternatives exist.

The number of shelters you need scales with your population density and the likelihood of large simultaneous displacement events. A tornado affecting a dense residential district can displace hundreds of households in minutes. A coastal flood can displace entire districts at once. Rule of thumb: shelter capacity should cover at least 5–10% of your city's residential population for typical disaster scenarios; coastal and river-flood-prone cities should aim higher.

Place shelters distributed across your city rather than concentrated in one area. If the disaster strikes near your only shelter cluster, those shelters may be damaged or inaccessible precisely when they are most needed. A shelter that is itself in the flood plain serves no one during a flood.

> **Info**: Citizens who cannot find a shelter will attempt to evacuate to outside connections — leaving the city entirely — rather than shelter in place. This is preferable to remaining in a disaster zone, but it generates significant traffic on your evacuation routes and can clog highway connections. A well-distributed shelter network keeps most displaced citizens local and reduces evacuation route pressure.

---

## How Buildings Take Damage

Damage accumulates independently from three sources: storm/weather damage, fire damage, and water/flood damage. Each type builds up separately, but they all contribute to a combined total. When that combined total reaches its maximum, the building is destroyed.

Every building type has a structural integrity rating built into its design. A high-integrity structure takes much longer to reach that destruction threshold — the same storm that flattens a small residential house might only lightly damage a reinforced civic building. Buildings with extremely high structural integrity are effectively immune to weather damage entirely.

Flood damage scales with water depth relative to the building's height. A building with the first floor barely under water takes damage more slowly than one that is deeply submerged.

> **Info**: Your city's overall disaster damage rate is a global modifier that scales all incoming structural damage. Certain service buildings and policy choices can reduce this rate city-wide.

---

## Collapse and Clearance

Once a building is destroyed, it does not vanish immediately. It enters a collapsed state, playing through a destruction animation before settling into rubble. During this phase, a disaster rescue request is automatically generated. Fire and rescue services respond to that request, arriving at the site to handle post-collapse operations. Only fire stations with the **Disaster Response upgrade** can respond to collapsed building rubble — plain stations are ineligible.

The clearance process progresses through stages — from actively collapsing, to settled rubble, to fully cleared. Destroyed buildings leave a cleared lot waiting for reconstruction.

---

## Recovery

Once the disaster passes and the danger level drops to zero, evacuation orders lift. Citizens begin returning from shelters. Buildings that were damaged but not destroyed retain their accumulated damage — partial damage does not automatically heal. Destroyed buildings need to be demolished and rebuilt. Vehicles that were stopped and damaged during the disaster generate maintenance requests, and repair crews are dispatched to restore them.

The overall recovery pace depends on how quickly your emergency services clear rubble sites and how many citizens lost their homes versus those who sheltered temporarily. A city with good emergency facility coverage and early warning infrastructure will move through the recovery cycle significantly faster.

---

## Partial Damage and the Recovery Timeline

Buildings that survive a disaster with partial structural damage do not automatically repair themselves. The accumulated damage persists until active repair services address it. In the absence of repair, a damaged building is a structural liability: a second disaster that finds it weakened can destroy it even if the same event at full health would only have caused minor damage.

The recovery timeline runs in phases: emergency response (fire and rescue clearing rubble, which requires the Disaster Response upgrade on your stations), followed by slow passive repair of damaged-but-standing buildings once the danger level is zero, and finally demolition and reconstruction of destroyed lots through the normal development pipeline.

Buildings with high structural integrity (government buildings, reinforced structures) recover faster from partial damage. Low-integrity buildings — older residential, unupgraded commercial — may remain impaired for extended in-game periods after a disaster.

> **Info**: You can observe partial damage on individual buildings by selecting them after a disaster. The structural integrity bar will show remaining health. A building at 40% structural integrity is a significant risk if another severe weather event occurs before repair completes. Prioritizing maintenance service coverage in recently-hit areas accelerates recovery.

---

## Flood Prevention and Mitigation

Floods in CS2 are driven by surface water rising above terrain level. The most direct prevention is keeping critical infrastructure above the flood line — power plants, water treatment facilities, and emergency services placed at higher elevation are far more resilient than the same buildings in a river valley.

Water drainage infrastructure (canals, drainage outlets) can lower the rate at which surface water accumulates in low-lying areas. Proper placement of sewage outlets and drainage ensures that heavy rainfall has somewhere to go rather than backing up against your city.

Tsunami events are harder to mitigate because they are not driven by local rainfall — they are wave events that propagate across the entire water body. The primary tool is early warning lead time (which gives evacuation more runway) and placing critical services away from sea-level coastal zones.

The terrain editor, if used during city founding, is your most powerful flood mitigation tool. A city that starts with elevated terrain or berms between residential areas and flood-prone water bodies will sustain far less damage than one built on a flat floodplain.

---

## What Can Go Wrong

**Evacuation vehicles can't reach buildings in time.** If the warning window is short (no Early Disaster Warning facilities) and streets are congested, evacuation buses may still be en route when the storm hits. Citizens end up sheltering in buildings that then take damage.

**Emergency shelters fill up.** There is a capacity limit on shelter beds. If a major disaster displaces more citizens than your shelter network can absorb, overflow citizens have nowhere designated to go.

**Cascading fires from lightning.** A storm that passes through a dense area can spark multiple fires across different buildings simultaneously. If your fire coverage is thin, those fires spread before crews arrive, compounding the damage well beyond what the storm itself caused.

**Flood damage on low-lying infrastructure.** Buildings that sit close to sea level or near riverbanks are flooded before most of your city is affected. If critical services — power plants, water treatment facilities — sit in flood-prone zones, they can be knocked offline precisely when the rest of the city needs them most.

**Partial damage persists.** Buildings that survive a disaster with significant damage remain weakened. A second disaster shortly after the first can destroy buildings that would have withstood the same event at full structural health.

**No disaster response upgrade.** If all your fire stations are plain stations without the Disaster Response upgrade, collapsed building rubble will never be cleared. The wreckage sits indefinitely, blocking reconstruction.

**Shelter damaged by the disaster it was supposed to serve.** A shelter placed in a tornado corridor or flood plain can be taken offline by the same event that displaces your citizens. Place shelters at the geographic margins of your city's most disaster-prone areas, not in the center of them.

**Recovery stalls due to missing Disaster Response upgrade.** The two-phase nature of disaster cleanup — fire suppression first, then rubble clearance — means that even a fast fire response does not clear sites if no stations have the Disaster Response upgrade. Cities that never invest in this upgrade will accumulate permanent rubble sites after every major disaster.
