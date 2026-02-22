# Terrain & Natural Resources

## What the Map Contains

Every Cities: Skylines II map ships with a hidden layer of natural resources baked into the land. Before you place a single road, the ground beneath your future city already contains pockets of ore, pools of oil, stretches of fertile soil, and fish-rich waters — all distributed unevenly across the map in organic, overlapping patterns. These resources are invisible until you look for them, but they shape every decision you make about where to place specialized industry.

The four resources are:

- **Fertile land** — the broad agricultural belt that supports farming. It tends to cover large areas of the map and is the most common resource.
- **Ore** — mineral deposits for mining and metal production. Rarer and patchier than fertile land.
- **Oil** — underground petroleum for drilling and refining. About as rare as ore but found in different locations.
- **Fish** — determined by water depth rather than geology. Any body of water deeper than a couple of meters supports fish populations.

> **ℹ️ Info — Resource Distribution**
> Resources are generated using layered noise at three different scales, biased so that ore and oil are significantly rarer than fertile land. Ore and oil start at the same relative rarity as each other. Fish availability is tied entirely to how deep the water is — shallow decorative ponds produce nothing, while deep lakes and coastal waters are productive.

## Discovering Resources

You do not need to do anything special to "unlock" resources — they exist from the moment the map loads. To see where they are, open the natural resources info view overlay. The overlay color-codes the map, showing you concentrations of each resource type. Experienced players check this overlay early to plan where specialized industrial districts will eventually go.

## How Extraction Works

Specialized industry zoning placed over a resource-rich area automatically harvests that resource. Farm zones draw on fertile land, mining zones extract ore, drilling zones tap oil, and fishing harbors pull from fish-rich waters. The extraction rate depends on how many industrial buildings are actively operating in the zone. The more buildings you have running, the faster the resource is consumed.

> **ℹ️ Info — What "Depleted" Means**
> Each cell of the resource map tracks a base amount and how much has been used. Available resource = base amount minus used amount. When used equals base, the cell is fully depleted. Extraction raises the "used" counter; regeneration lowers it.

## Renewable vs. Non-Renewable

This is where resource management gets consequential. Fertile land and fish behave very differently from ore and oil.

**Fertile land** slowly recovers over time if it is not too polluted. Ground pollution from nearby industry, landfills, or contaminated soil raises the "used" counter independently of extraction, acting like a second drain on the resource. Clean up the pollution source and fertility can return.

**Fish populations** recover in a similar way — they regenerate toward a level set by water depth, but water pollution and noise pollution from boats or industry push them down. Remove the pollution and the fishery bounces back.

**Ore and oil do not regenerate at all** under default game settings. Once a mining or drilling district exhausts its deposits, those cells are gone for good. The only exception is if you are playing a scenario with resource refill enabled — some game modes can slowly top up ore and oil at configurable rates, making them renewable in that context but never truly unlimited.

> **ℹ️ Info — Regeneration Timing**
> Fertile land and fish resources update roughly 32 times per in-game day, recovering at a rate of 25 units per update cycle. Ore and oil have no recovery job at all in the base simulation. Game-mode refill for ore and oil, when active, runs approximately 128 times per day.

## When Resources Run Out

A specialized industry district that has exhausted its resource does not immediately shut down, but it becomes severely impaired. Buildings lose their reason to operate, production drops, and worker demand weakens. Firms may close, unemployment can rise in surrounding residential areas, and tax revenue from that district falls sharply. If your city economy is heavily reliant on that export income — which many early-game cities are — a depleted ore or oil field can trigger a genuine fiscal crisis.

Fertile land depletion is usually more gradual because the resource is more abundant and partially renewable, but heavy industrial farming concentrated in one area can strip it surprisingly fast.

## Environmental Feedback

Resources do not exist in isolation from the rest of your city's simulation. Pollution is the most direct feedback loop: ground pollution from industry degrades nearby fertile land, and water pollution from sewage or runoff damages fisheries. A profitable industrial district can quietly undermine its own resource base over time if you are not managing emissions and waste carefully.

The terrain itself also feeds into land value through attractiveness bonuses. Forested areas and proximity to water both raise nearby land values, which means the same forests that supply your lumber industry also make neighboring residential zones more desirable. Clear-cutting for profit has a second cost you may not immediately see on a balance sheet.

> **ℹ️ Info — Attractiveness Calculation**
> Land value attractiveness is computed from three factors: proximity to forest (forest bonus), proximity to water bodies deeper than 2 meters (shore bonus), and terrain elevation above a threshold (height bonus). These values update approximately 16 times per in-game day.

## What Can Go Wrong

**Ore or oil runs out mid-game** with no replacement industry planned. The district goes idle, unemployment spikes, and export revenue disappears before you have diversified.

**Ground pollution from mining or drilling contaminates nearby fertile land**, making a mixed industrial area counterproductive — the ore operation destroys the agricultural potential next door.

**Fishing industry over-concentrated in one harbor** strips a water body faster than it regenerates, especially if nearby water pollution is already degrading the fishery from the other direction.

**Clearing large forests for lumber revenue** removes the attractiveness bonus those trees were providing to adjacent residential neighborhoods, causing property values and rental income to drop in areas you did not expect to affect.

**Playing on a map without checking the resource overlay first** leads to placing specialized industry in the wrong location — farms in low-fertility zones, mines where there is no ore — and wondering why the district never produces at full capacity.
