# Milestones and Progression in Cities: Skylines II

## How Your City Earns XP

Every action you take as a city planner feeds into a single running total of experience points. That total determines when your city advances to the next milestone, and milestones are the engine that drives almost everything becoming available to you: new zone density tiers, public services, policies, road types, signature buildings, and more.

XP arrives from several distinct sources, all of which contribute to the same pool. The two largest ongoing sources are your population and your citizens' average happiness. The game tallies these automatically roughly 32 times per in-game day — not in real-time, but on a regular simulation heartbeat. Each tick, your current population contributes to the total, but only the growth above your previous highest population counts. If you had 10,000 residents last week and still have 10,000 today, you are not earning population XP. You earn it when you break new ground. Happiness, however, contributes every tick regardless, as long as you have at least 10 residents in your city.

> **Info:** Population XP is calculated from new population only — the game tracks your all-time peak and only rewards the difference. Happiness XP is awarded continuously at every accumulation tick (32 times per game day), regardless of population growth. You need at least 10 residents before the happiness contribution starts.

Building services and expanding your infrastructure provides a separate, immediate layer of XP. Every time you place a service building — a fire station, a hospital, a school, a water tower — you receive a flat XP award for the placement, and a further award if you later add upgrades to that building. Laying road segments, train tracks, tram lines, subway tunnels, water and sewage pipes, and power lines all add XP proportional to the length of the segment you build. A long highway viaduct earns more than a short connector street, and elevated roads carry a small bonus over their at-grade equivalents. When you delete infrastructure, the game reverses part of that XP, so demolishing and rebuilding the same road is not a way to farm progress.

One bonus is awarded only once per city: the moment you connect your first building to an electricity grid, you receive a single one-time XP payment to reward establishing your power system.

## What a Milestone Actually Does

The game has 20 milestones arranged in a fixed sequence. Each one has an XP threshold — a cumulative total your city must reach before the milestone triggers. The thresholds are defined in the game's configuration assets rather than being fixed numbers in the code, so they can vary by game mode; the underlying structure is the same regardless.

When your total XP crosses a milestone threshold, the game immediately delivers several things at once. Your city receives a cash reward, which is deposited directly into your treasury. Your loan limit expands, meaning you can now borrow more from the bank to fund large infrastructure projects. A number of new map tiles become available for purchase. And you receive a batch of development points, which you spend separately in the development tree.

In addition to these concrete rewards, reaching a milestone fires an unlock signal that cascades through the game's entire catalog of content. Services, zone types, road categories, policies, and other features each have a list of prerequisites — things that must be unlocked before they become available. That list can include milestones directly, or can require that other items were unlocked first. When a milestone fires, everything that was waiting on it becomes visible, which in turn may trigger further unlocks down the chain. The cascade runs until nothing new is waiting.

> **Info:** The unlock cascade is iterative, not instantaneous in a single pass — the game loops through all still-locked content repeatedly until no further unlocks are possible. A single milestone can therefore indirectly open items several dependency steps removed from it.

Some milestones are designated as "major" milestones. These are styled differently in the UI and tend to be the ones that deliver the most significant unlocks, but mechanically they follow the same rules as any other milestone.

The final milestone, milestone 20, is flagged as the victory milestone. Reaching it triggers the victory condition for the city.

## The Development Tree and Development Points

Development points are a separate currency from money. You cannot earn them through taxation or trade; they arrive only when milestones are reached. The game awards points according to a formula that increases with each milestone level — early milestones give fewer points, and later ones give more, topping out at a fixed ceiling for the highest milestones.

> **Info:** The development point formula used by the game is roughly `(milestoneLevel + 1) / 2 + 1` for milestones 1 through 18, stepping up to a flat 10 points per milestone from milestone 19 onward. Points accumulate in a shared pool and are never lost if unspent.

The development tree is organized by city service category. Each category contains a tree of nodes, and each node unlocks something — a specific building, a policy, a service feature, or a gameplay capability. Nodes within the tree have prerequisite relationships: you cannot purchase a node deeper in the tree until you have already purchased the nodes that precede it.

Some nodes are free. These are service-level nodes that unlock automatically the moment the parent service itself becomes available, without costing you any points. Everything else has a point cost listed on it, and spending points is an active choice you make through the development panel.

When you spend points to purchase a node, the game verifies three things: that you have enough points, that the service the node belongs to is already unlocked, and that any prerequisite nodes in the tree have already been purchased. If all three conditions are met, the points are deducted and the node's content is unlocked immediately.

Points are permanent currency — they do not expire between sessions, and there is no penalty for saving them. You might choose to hoard development points from early milestones and spend them in a single decision once you know what your city needs most.

## Map Tiles and Milestone Gating

Every milestone from the first one onward adds a fixed number of map tiles to your purchasable allowance. These are not free tiles — you still pay money from your treasury to purchase each tile individually — but you cannot purchase tiles beyond your current allocation no matter how much money you have.

The tile count you can purchase therefore grows in lockstep with your milestone level. Early in a game, most of the map is inaccessible not because you cannot afford it but because you have not yet reached the milestone that made those purchase slots available. Advancing through milestones is the only way to unlock the ability to expand your city's territory further.

> **Info:** Map tile unlock counts are stored per milestone in the game's configuration. Each milestone adds to your cumulative tile purchase allowance. The money cost to purchase individual tiles is handled by a separate system and is not related to milestone progression.

## Signature Buildings

Signature buildings are landmark structures — large, prestigious buildings that represent the pinnacle of a particular zone or service type. They differ from ordinary buildings in two ways: they have special unlock requirements beyond the standard milestone chain, and placing them may itself trigger additional unlocks.

A signature building typically requires that a certain number of ordinary buildings of the relevant type have already been placed in your city before it becomes available. The game tracks a running count of how many qualifying buildings you have placed. When that count reaches the required threshold, the signature building unlocks. This is tracked per building type, not citywide, so different signature buildings have their own independent counters.

Once a signature building is placed, it can itself serve as a prerequisite for other unlocks. The game maintains "unlock on build" records that link specific buildings to other content — placing the right signature building can open up additional policies, services, or features that were otherwise gated.

> **Info:** The build-count requirement system decrements the counter if you later demolish qualifying buildings. Knocking down buildings you relied on to unlock a signature building does not revoke the unlock of the signature building itself, but it does reduce your progress counter, which could matter if additional content was gated behind a higher count.

## How the Unlock Chain Fits Together

It helps to think of all unlocked content as sitting inside a dependency graph. Milestones are entry points into that graph. When a milestone is reached, it unlocks itself, and everything that was waiting only for that milestone becomes available. Some of those newly available items are themselves prerequisites for further content, so they fire their own unlocks. This cascade continues until the graph settles.

Development tree purchases work the same way — spending points on a node fires an unlock that can itself cascade to dependent content. Build-count thresholds for signature buildings fire into the same mechanism. All of these different triggers — milestone advancement, development point spending, and build-count requirements — feed into a single unified unlock engine that processes them identically.

The practical consequence for players is that a single milestone can appear to unlock a large wave of content at once, even when only a few items were directly waiting on that milestone. Everything downstream in the dependency chain resolves in the same pass.

## What Can Go Wrong

**XP has stalled and milestones are not advancing.** The most common cause is that your population has plateaued. If your city stopped growing, you are no longer earning population XP. Happiness XP still ticks continuously, but it alone is modest. Adding service buildings and expanding your road and utility networks provides bursts of XP. Focus on extending infrastructure into new areas or building new services rather than waiting passively.

**Development points are available but the node you want is grayed out.** Either the service that node belongs to has not been unlocked yet through milestones, or a prerequisite node earlier in the same service tree has not been purchased. Both requirements must be satisfied before a node is purchasable. Check that the service itself is unlocked first, then trace the path of prerequisite nodes back from the target.

**You cannot buy more map tiles even though you have money.** You have reached your current tile purchase allowance, which is gated by your milestone level. The only way to unlock additional tile purchase slots is to advance to the next milestone. Earning more XP is the solution, not accumulating more money.

**A signature building is visible in the catalog but not available.** Signature buildings require a minimum number of qualifying ordinary buildings already placed in your city. Check the tooltip on the building for the count requirement. You likely need to build and maintain more of the associated building type before the signature building unlocks.

**A service or zone type that should be available at your milestone level is still locked.** The unlock cascade is prerequisite-based, so the item you want may depend not just on a milestone but on other content that must also be unlocked first. Look at what the item requires and verify that all prerequisites are satisfied. In some cases this means purchasing a specific development tree node in addition to reaching the right milestone.

**Progress feels very slow after the early game.** The new-city bonus that accelerates early population growth fades as your city matures. Once past roughly the early growth phase, you become dependent on genuine population expansion and steady infrastructure investment to keep XP flowing. Cities that sprawl outward and continuously add services progress faster than those that stop expanding.
