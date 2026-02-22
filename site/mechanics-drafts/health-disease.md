# Health & Disease

## What "Health" Actually Means

Every individual citizen in your city carries a health score that runs from 0 to 100. Think of it as a single number that summarizes how physically well that person is doing. When the number is high, they are resilient — unlikely to fall sick, productive at work, and contributing positively to their household's happiness. When it drops low, they become increasingly fragile: far more vulnerable to disease, less productive, and a drag on their household's overall wellbeing.

Health is not the same as happiness, though the two are related. Happiness is the average of a citizen's health score and their wellbeing score — the wellbeing side captures quality-of-life factors like entertainment, education, and being treated fairly, while the health side is strictly about physical condition. You can have a miserable but physically healthy citizen, or a content but medically fragile one. Both ultimately pull happiness in their respective directions.

> **ℹ️ Info — The happiness formula**
> A citizen's happiness is calculated as `(health + wellbeing) / 2`, where both values run on a 0–100 scale. Each factor contributes equally. A citizen at 80 health and 40 wellbeing is exactly as happy as a citizen at 40 health and 80 wellbeing.

---

## What Lowers Health

Health is not static. The game evaluates each citizen's situation regularly and adjusts their health up or down based on what they are exposed to and what services they have access to.

The most direct health drains come from environmental conditions. Air pollution and ground pollution both impose a health penalty proportional to how contaminated the area around a citizen's home is. The combined penalty can be up to 50 points on a 100-point scale, which is substantial — a heavily polluted neighborhood can push otherwise-comfortable citizens into genuine medical vulnerability. Noise pollution works slightly differently: it dents wellbeing rather than health directly, but since happiness averages the two, the effect on your city's population feels similar.

Contaminated water is a separate concern. If your water supply has picked up pollution from the ground — which happens when ground pollution leaches into the groundwater that feeds your water intake — citizens who drink that water take a health penalty of up to 10 points, on top of a wellbeing penalty from the general unpleasantness of living with bad water. Sewage exposure works similarly: citizens near untreated sewage or who lose sewage service entirely take both a health and a wellbeing hit.

Being sick is itself a health drain, beyond the immediate event. The game tracks a "sickness penalty" on each citizen that accumulates with repeated illness episodes. Citizens who cycle in and out of sickness without ever fully recovering gradually trend toward lower baseline health, making them even more susceptible to the next illness.

Finally, age. There is no direct health penalty from growing old, but elderly citizens face a much steeper death probability curve than younger ones — it is not that they are inherently unhealthy, but that age makes every health event more dangerous.

> **ℹ️ Info — Pollution health penalties**
> Air and ground pollution together can impose a maximum wellbeing penalty of 50 points. Noise pollution can impose up to 15 wellbeing points of penalty. These caps apply per-citizen based on the pollution level at their home address, not their current location. Water pollution applies a health multiplier penalty on top of that.

---

## What Raises Health

Healthcare coverage is the most direct lever. The game calculates a healthcare bonus based on how well your hospital network covers each residential area. That coverage score is multiplied by a factor of 2 for health and 0.8 for wellbeing — meaning good healthcare access is primarily a physical health benefit, with a smaller but real quality-of-life component. A neighborhood that is well-served by clinics will have citizens who drift toward higher health scores over time simply from that proximity.

Parks and recreation raise wellbeing rather than health directly, but they are worth understanding in this context because wellbeing and health are the two halves of happiness. A city with good park coverage can carry citizens through periods of pollution stress by keeping wellbeing high even when health is moderate. Park maintenance matters here — a park in poor repair delivers reduced coverage range and reduced wellbeing benefit, while a well-maintained park covers a wider area at full strength.

Adequate housing contributes through multiple indirect channels. Citizens in good-quality homes receive apartment quality bonuses to wellbeing. Homeless citizens, by contrast, take both a 20-point health penalty and a 20-point wellbeing penalty — homelessness in this game is genuinely harmful to citizen health, not just a cosmetic problem.

Access to utilities matters too. Citizens who lose electricity service take a wellbeing penalty that kicks in after a short delay. Citizens without water take both a health and a wellbeing penalty. These are immediate and significant — a brownout that persists will start degrading your citizens' health scores, not just their mood.

---

## How Disease Works

Once per game day, the simulation checks every citizen who is currently healthy and asks: should this person fall ill today? The answer depends almost entirely on their current health score, and the relationship is exponential rather than linear.

A citizen at full health is essentially immune. The math works out to a near-zero probability at high health levels — the game uses an exponential curve that keeps the sickness chance negligibly small until health dips well below 50. Below that point, risk starts climbing steeply. A citizen at around half health has a noticeably higher chance of getting sick each day. A citizen in very poor health faces a meaningful probability of illness. A citizen at absolute zero health is guaranteed to fall ill.

This design means that the goal of keeping citizens healthy is not to prevent sickness outright — it is to keep health scores high enough that the daily sickness check almost never triggers. Citizens at 80 or 90 health will go years between illnesses. Citizens persistently stuck at 30 or 40 health will be cycling through the hospital regularly.

When the check does trigger, the game also decides how serious the illness is. Two probabilities are rolled: one determines whether the citizen gets sick at all, and another determines whether they can manage on their own or will need an ambulance. Healthier citizens who do get sick are far more likely to recover under their own power — they walk to a clinic when their schedule allows. Sicker citizens are more likely to collapse and need emergency pickup.

The base probability of getting sick on any given day is modified by a city-wide disease probability multiplier. Polluted water running through the municipal supply is the primary driver of this multiplier. When ground pollution contaminates your groundwater source, and that contaminated water makes it through to your citizens, the city-wide disease probability rises for everyone. This is the closest thing Cities: Skylines II has to a traditional epidemic mechanic — not a discrete outbreak event, but a persistent background elevation of sickness rates that affects the whole population until the water supply is cleaned up.

> **ℹ️ Info — The sickness probability curve**
> The game calculates a factor `t = clamp(2^(10 - health * 0.1) * 0.001, 0, 1)`. At health 100, `t` is roughly 0.001. At health 50, `t` is about 0.032. At health 10, `t` is about 0.5. At health 0, `t` is exactly 1.0. This `t` value is then used to interpolate between minimum and maximum occurrence probabilities defined for each type of health event — disease, injury, or sudden death.

---

## Injuries and Sudden Death

Disease is not the only health event the daily check can produce. The same probability system also covers injuries and sudden-death events. All three — disease, injury, death — are separate event types, each with their own minimum and maximum probability ranges. Each is rolled independently, which means it is theoretically possible (though rare at high health levels) for a citizen to be affected by more than one on the same cycle.

Injuries work the same as sickness from a dispatch standpoint: mild injuries lead the citizen to seek care independently, serious ones trigger ambulance dispatch. Sudden-death events from the health check are distinct from age-based natural death — they represent a citizen whose health has declined to the point where the body simply gives out without a specific precipitating event.

---

## Aging and Natural Death

Citizens age through four life stages: child, teen, adult, and elderly. The aging happens on a fixed schedule tied to simulation days, not to health. No amount of good healthcare or park coverage will slow the aging clock.

What healthcare does affect is survival odds once a citizen is elderly. The game applies an age-based death probability curve, and that curve rises sharply in the elderly stage. Each day, elderly citizens are checked against this curve and may simply die of natural causes — the probability is not deterministic, but it rises continuously as they age. A citizen at maximum age has a high probability of dying on any given day, regardless of health or services.

The practical implication is that some deaths in your city are unpreventable. A city with excellent healthcare will have fewer deaths from illness and injury, but the elderly will still die at a rate driven by their age, not their care quality. What good healthcare does is keep citizens healthy long enough to reach old age in good condition, rather than dying prematurely from illness.

---

## How Health Affects Productivity and Happiness

The effects of health extend beyond mortality risk.

Happiness, which is driven partly by health, affects whether households decide to stay in your city. A household whose citizens are consistently unhealthy will have below-average happiness, and if that happiness drops far enough, the household may decide to move away. The move-away decision is probabilistic and scaled by how unhappy the household is — a deeply unhappy household is far more likely to leave than one that is merely lukewarm. Health degradation is one of several vectors through which poor city planning drives away population.

Citizens who have an active health problem — meaning they are currently sick, injured, or otherwise flagged — also take a direct health penalty of 20 points on top of the underlying health degradation that caused the illness. This creates a compounding effect: getting sick makes you less healthy, which makes you more likely to get sick again, which further reduces health. Breaking that cycle requires good healthcare access and fixing the underlying environmental causes.

When a member of a household dies, all surviving household members take both a health penalty and a wellbeing penalty from grief. This is a small but real second-order effect: a bad disease wave does not just cost you the citizens who die, it also temporarily debilitates the surviving members of affected households.

---

## What Can Go Wrong

**Citizens are getting sick repeatedly in the same neighborhood.** The most likely cause is persistent low health in that area. Check the pollution infoview — air pollution and ground pollution near industrial zones or busy roads are the primary culprits. A neighborhood with heavy pollution will see its residents stuck at chronically low health regardless of healthcare coverage, because the damage is continuous and the healthcare benefit only partially offsets it. Moving industry to the downwind edge of your city, adding sound barriers on major roads, and planting trees and parks in the affected area can all reduce the pollution exposure.

**A wave of disease is spreading city-wide.** When the whole city has elevated sickness rates simultaneously, rather than one troubled neighborhood, look at your water supply. Ground pollution contaminating the groundwater that feeds your treatment plants will raise the disease probability for every citizen who uses the city's water. Identify the pollution source, physically separate your water intake from contaminated groundwater cells, or upgrade your water treatment. The elevated disease rate will persist until the water is clean.

**Citizens are dying from illness despite good hospital coverage.** Very sick citizens — those who have been stuck at low health for an extended period — face a compounding spiral. Their baseline health is low, which raises sickness probability, which inflicts the health problem penalty, which lowers health further. Healthcare treats the immediate episode but does not fix the environmental cause. If a neighborhood has high illness deaths, the solution is to reduce pollution and improve housing quality so that baseline health recovers between episodes, rather than only treating each episode as it arrives.

**Elderly citizens keep dying and there is nothing wrong with the healthcare system.** This is expected behavior. The age-based death curve makes elderly death inevitable regardless of services. What you can do is ensure your deathcare system is scaled to handle those deaths as the cohort grows. A city that grew rapidly a few game-years ago will see a wave of elderly deaths as that founding population ages out. See the Deathcare guide for how to manage the downstream consequences.

**Low happiness in a neighborhood with no obvious service gaps.** If all services check out — no pollution, good utilities, good healthcare coverage, decent parks — look at housing quality. Citizens in overcrowded or low-level housing take wellbeing penalties from poor apartment quality. Upgrading the residential zone's density or allowing higher-level buildings to develop will push those citizens toward better-quality living spaces and improve the wellbeing half of the happiness equation without touching health at all.
