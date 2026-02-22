# Mail and the Postal Service

Every building in your city quietly accumulates mail over time. Residents receive bills, packages, and letters; they also generate outgoing correspondence. Commercial and industrial buildings do the same. This process runs constantly in the background, and the health of your postal network determines whether that mail flows smoothly or piles up and starts dragging down building efficiency.

## How Mail Builds Up

Each occupied building acts as both a sender and a receiver. The rate at which mail accumulates is proportional to how many people are inside — residents, workers, or both. A packed apartment block generates far more mail than a small detached house. Different zone types (residential, commercial, industrial, office) have different baseline rates for outgoing versus incoming mail.

> ℹ️ **Mail generation formula**: A building's accumulation rate equals a base rate for its zone type multiplied by the number of residents plus workers. Mail is capped at a global maximum per building; once a building hits that ceiling, no more mail accumulates until service arrives.

Mail sits on a building until a threshold is crossed. Only then does the building raise its hand and request a post van. Below that threshold, the game considers the backlog negligible and does nothing. This means a small amount of waiting mail is completely normal and harmless.

## Mailboxes on the Street

Separate from buildings, streetside mailboxes dot the city's roads. Citizens drop outgoing mail into these as they pass by. Mailboxes have their own accumulation threshold and raise their own collection requests independently of buildings. Post vans service these stops too, sweeping them clean on their routes.

## The Post Van's Route

When a post van leaves its facility, it carries a load of sorted, locally-addressed mail ready for delivery. It drives a route through the city, stopping at buildings along the way. At each stop, the van drops off incoming mail for that building and simultaneously scoops up any outgoing mail waiting to be collected — if that zone type requires active collection. The van tracks how much it is carrying in both directions.

> ℹ️ **Capacity limits**: Each van has a fixed mail capacity. If the van finishes all its deliveries before the delivery compartment is empty, it has mail left over. If the collection compartment fills up before the route ends, it stops collecting. In either case, the van returns to its home facility to unload and reload.

When the van returns, the outgoing mail it collected is handed over to the facility as unsorted mail. Any local mail it could not deliver gets returned to the facility's stock to be sent out again on the next run.

## Two Kinds of Facilities

Not all postal buildings do the same job, and the distinction matters for city planning.

**Sorting facilities** are the backbone of the network. They receive raw, unsorted mail from vans and trucks, process it, and split it into two streams: local mail (addressed to buildings within your city) and outgoing mail (destined for outside the city). Sorting happens continuously, limited by the facility's sorting rate and its current efficiency. A sorting facility with plenty of unsorted mail but low efficiency will fall behind, and the backlog will grow.

**Post offices** (non-sorting facilities) do not sort. They receive already-sorted local mail delivered by trucks from sorting facilities, then dispatch vans to deliver that mail to buildings in their coverage area. They also collect outgoing mail from buildings and hold it until a truck from a sorting facility comes to pick it up.

> ℹ️ **Mail type flow**: Three distinct categories of mail move through the system — unsorted mail (raw, just collected), local mail (sorted and ready to deliver locally), and outgoing mail (sorted and bound for outside connections). Each moves separately and can back up independently.

## Inter-Facility Transfers

Delivery trucks handle the heavy freight between buildings. A sorting facility that has processed a pile of local mail will dispatch a truck to deliver it to a post office. That same truck may return loaded with unsorted mail it picks up from the post office on the way back. These bidirectional trips let facilities keep each other balanced.

## When Mail Backs Up and Hurts Buildings

The system becomes visible to players when it starts to fail. If mail accumulates in a building beyond a negligible threshold, the game begins applying an efficiency penalty. The penalty is not linear — it grows steeply as the backlog increases.

> ℹ️ **Efficiency penalty**: Below a small threshold, mail has no effect on a building's efficiency. Above it, the penalty grows with the size of the backlog. A building that never receives a post van can suffer a significant efficiency reduction, meaning it produces less output, employs fewer workers effectively, or generates less tax revenue.

---

## What Can Go Wrong

**Not enough vans.** The most common failure. A post office or sorting facility with too few vans dispatches them infrequently. Buildings wait a long time between visits, mail accumulates, and efficiency penalties spread across a neighborhood. Adding a second facility or upgrading van capacity resolves this.

**Sorting bottleneck.** If your sorting facility cannot process incoming unsorted mail fast enough — because its sorting rate is low, it is operating at reduced efficiency, or it is simply receiving more than it can handle — local mail production slows. Post offices starve for deliveries even while the sorting facility is sitting on a growing pile of raw mail.

**Facility overflow.** Every facility has a storage capacity. If mail arrives faster than it leaves, the facility fills up. A full facility stops accepting new mail, which means vans returning from their routes have nowhere to dump their collected mail. The vans then back up at the facility, reducing the frequency of outbound runs.

**Coverage gaps.** Postal service is not automatic across the entire city. If a district grows beyond the natural reach of existing facilities — measured by travel time, not just distance — buildings there will rarely see a post van. Placing a post office closer to underserved neighborhoods shortens the route time and improves visit frequency.

**Outgoing mail stranded.** Some zone types require active collection — the van must physically stop to pick up outgoing mail rather than the building simply dropping it off. If a neighborhood is collecting mail but no trucks are running transfers between the local post office and the sorting facility, outgoing mail piles up at the post office with nowhere to go.
