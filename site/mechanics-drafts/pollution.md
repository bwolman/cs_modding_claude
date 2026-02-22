# Pollution in Cities: Skylines II

Your city produces three invisible layers of damage: air pollution, ground pollution, and noise pollution. Each behaves differently, spreads differently, and hurts your residents in different ways. Understanding them is the difference between a thriving city and one where half your population is sick and land values have cratered.

> **Info:** The game provides three separate info-view overlays — one for each pollution type. The air pollution overlay uses a color gradient from clean (blue-green) to heavily polluted (red) and highlights active emitters. The ground pollution overlay shows contaminated soil cells and lets you spot old industrial footprints that are still leaching. The noise overlay rebuilds in real time and is the fastest way to see which roads and buildings are the loudest contributors. Players who do not check these overlays regularly will miss building problems until the damage is already done.

---

## Air Pollution

Air pollution is the most mobile of the three types. It rises from industrial buildings, power plants, and heavy traffic, then drifts downwind across your city. The game simulates actual wind direction: pollution does not just radiate outward from its source, it travels.

Once airborne, pollution also bleeds slowly into neighboring areas even without wind. A factory on the eastern edge of your industrial district will gradually fog up the surrounding blocks. The good news is that air pollution fades on its own over time. If you shut down or relocate a polluter, the air will clear — it just takes a while.

> **Info**: Air pollution spreads in two ways simultaneously: it is carried downwind (advection) and it bleeds into adjacent areas (diffusion). Each neighboring map cell receives roughly 12.5% of its neighbor's pollution per update tick. Air pollution also decays at a steady rate each game day, so removing the source will eventually clear the air.

High air pollution directly hurts citizen happiness. The game continuously monitors the average air pollution exposure across all households and uses that figure to track your city's progress toward certain milestones. Sustained high air exposure is a happiness drain you cannot offset with parks or amenities alone.

---

## Ground Pollution

Ground pollution is the quiet menace. Unlike air pollution, it does not drift or spread from cell to cell — it simply accumulates where buildings emit it and then fades very slowly in place. Industrial facilities, waste processing, and certain utility buildings are the main culprits.

The danger with ground pollution is what it does underground. Contaminated soil leaches into the water table. Once your groundwater is polluted, any water pumping stations drawing from it will pull dirty water into your pipes, which then gets treated (or not) before reaching residents.

> **Info**: Ground pollution stays exactly where it is generated — there is no lateral spread. It decays slowly over time, but high ground pollution levels contaminate groundwater at a rate proportional to the pollution value. This affects the quality of water drawn by pumping stations.

Ground pollution also suppresses land value in affected areas, making it harder to attract desirable development nearby. Leaving a closed factory's footprint in place is not enough — the soil stays dirty long after the building is gone.

---

## Noise Pollution

Noise works on a completely different principle than the other two. It does not accumulate or persist between moments in the simulation. Instead, every single update the game wipes the noise map clean and rebuilds it entirely from current sources. If you mute every noise source right now, the noise map is zero within moments.

That makes noise highly reactive — but it also means it is always present wherever sources are active. Your roads are the dominant source, especially high-traffic arterials and highways. Industrial buildings also contribute. Abandoned buildings generate noise proportional to how much lot space they occupy, and homeless citizens camped in parks add noise as well.

> **Info**: Noise pollution is recalculated from scratch every update tick. Sources write into a temporary buffer; then the game smooths that buffer using surrounding cells (each cardinal neighbor contributes 12.5% of its value, each diagonal neighbor contributes 6.25%) to produce the final noise map. Nothing carries over from the previous tick.

---

## How Pollution Hurts Your Citizens

The three pollution types do not cause the same harm — they each affect citizens through different channels.

**Air pollution** reduces citizen wellbeing and increases the probability of illness. The effect is not triggered by momentary spikes — the game tracks sustained exposure over time. A brief period of high air pollution near a factory that is quickly cleaned up matters far less than a persistent background level across a whole district. Citizens living in chronically polluted areas will have persistently lower wellbeing, and sickness rates in those households will be elevated, increasing the load on your healthcare network.

**Ground pollution** hurts citizens primarily through the water supply — contaminated groundwater enters your pipes and reaches taps if pumps are not protected or water is not adequately treated. But ground pollution has a second, quieter effect: it directly suppresses land value in affected cells. Commercial and residential buildings sitting on contaminated soil operate less efficiently because land value feeds into building performance. Residents there earn less desirability, shops attract fewer customers, and office workers are less productive — all without any visible notification pointing to soil contamination as the cause.

**Noise pollution** reduces wellbeing for residents, measured per household based on the noise level at the building's specific location. High-density housing concentrates many households close together, so a noisy block affects more citizens than the same noise next to a single-family home. The effect is strongest at night, when residents are home and exposure is continuous. Noise also reduces efficiency for commercial buildings — shops and offices in loud areas are less attractive to customers and workers alike, which pulls down revenue and productivity. A highway running through what was a quiet commercial district can gradually degrade the entire strip without ever generating a direct complaint notification.

---

## Building Types and Pollution Output

Not all buildings pollute equally, and knowing which building types drive each pollution type helps you zone and plan more deliberately.

**Air and ground pollution** come primarily from industrial buildings and power plants. Fossil-fuel power generation is among the highest emitters for air. Landfills and waste processing facilities contribute significantly to ground pollution. Road traffic — particularly on high-volume arterials — generates meaningful air pollution even without any industry nearby.

**Noise pollution** comes overwhelmingly from roads, scaled by traffic volume. Highways and high-speed roads are the loudest by a wide margin. Industrial facilities are the next biggest contributors. Residential buildings generate very little noise of any type — they are almost entirely receivers, not sources.

**Building efficiency scales pollution output.** A factory running below capacity emits less than one running at full throughput. This cuts both ways: a struggling industrial building is less productive but also less damaging to its surroundings. Some buildings scale their pollution directly with occupancy count, so low-occupancy phases during early game are naturally lower-pollution phases too. Certain building upgrades apply additive reductions to a building's pollution multiplier, and stacking several of them on a major emitter can produce a meaningful real-world difference.

---

## What Reduces Pollution

**Zoning separation** is your first line of defense. Industrial zones belong away from residential ones, with commercial or civic buffers in between.

**Road upgrades** matter significantly for noise. Sound barriers on busy roads reduce noise dramatically — barriers on both sides can eliminate road noise entirely from nearby cells. Even a single-sided barrier gives meaningful directional reduction. Beautification upgrades (trees and landscaping) also reduce road noise by 25–50%. Tunnels produce no surface pollution at all.

**Building efficiency** affects how much pollution a building emits. A building running below capacity pollutes less than one running at full efficiency. Certain buildings scale their pollution with the number of occupants, so low occupancy means lower output.

**Upgrades and policies** can carry pollution modifiers. Some building upgrades additively reduce a building's pollution multiplier — stack enough of them and you can achieve significant reductions.

**City-wide modifiers** can reduce industrial ground and air pollution across your entire city, accessible through certain policies.

---

## What Can Go Wrong

**Dirty groundwater is silent until it is not.** You may not notice that an old industrial district has contaminated the water table until residents start complaining about water quality or you spot the pollution overlay on your water pumps. By then the damage is already widespread.

**Wind direction is not static.** An industrial district that was safely downwind last month may become a problem after wind shifts. Check the air pollution overlay periodically, not just when notifications appear.

**Abandoned buildings are a noise trap.** An area that slides into abandonment does not just look bad — it generates noise pollution that discourages the very redevelopment that would fix the problem.

**Ground pollution outlasts its source.** Demolishing a factory stops new emissions, but the soil contamination fades slowly. Plan around that delay before rezoning the land for residential use. If you rezone an old industrial block to residential immediately after clearing the buildings, new residents move into land that is still contaminated — suffering suppressed land value and degraded wellbeing for however many seasons it takes the soil to recover.

**Notification thresholds lag behind reality.** The game only alerts you when pollution crosses certain levels, but damage to land value and citizen health accumulates before those alerts fire. Watching the overlay maps directly gives you much earlier warning.

**A new highway can silently degrade a formerly quiet neighborhood.** When you route a highway through or adjacent to an established residential district to solve a traffic problem, the noise overlay will spike immediately — but the land value and wellbeing consequences take time to surface. Players who check only city-wide satisfaction numbers may not connect the new road to the gradual decline in that district until it has progressed significantly. Check the noise overlay before committing to any highway corridor that passes near housing.
