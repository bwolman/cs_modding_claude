# Pollution in Cities: Skylines II

Your city produces three invisible layers of damage: air pollution, ground pollution, and noise pollution. Each behaves differently, spreads differently, and hurts your residents in different ways. Understanding them is the difference between a thriving city and one where half your population is sick and land values have cratered.

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

**Ground pollution outlasts its source.** Demolishing a factory stops new emissions, but the soil contamination fades slowly. Plan around that delay before rezoning the land for residential use.

**Notification thresholds lag behind reality.** The game only alerts you when pollution crosses certain levels, but damage to land value and citizen health accumulates before those alerts fire. Watching the overlay maps directly gives you much earlier warning.
