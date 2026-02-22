## Citizens and Households: How Your City's Population Really Works

Every person walking the streets of your city has a full life story playing out behind the scenes — they are born, grow up, find jobs and partners, move into homes, raise children, and eventually die or leave. Understanding how this all works helps explain why your population grows or stalls, why neighborhoods fill up or empty out, and what makes citizens pack up and go.

---

## Where Citizens Come From

New residents do not simply appear — they immigrate. When your city has unmet residential demand and vacant homes to offer, households form at the edge of your map and start looking for a place to live. The more empty homes you have, the faster new households arrive; a city with abundant vacancies fills up quickly, while one running at near-full occupancy sees immigration slow to a trickle.

Each arriving household is a family unit. The game picks a household type at random (weighted by the available pool of household profiles), spawns a group of adults at an outside connection, and immediately sends them searching for a suitable property.

> **ℹ️ Info — What drives immigration demand?**
> Residential demand is tracked separately for low, medium, and high density. The spawn rate scales directly with the number of free residential properties — more vacancies means faster arrivals. If your residential vacancy rate is very low, immigration nearly halts regardless of how high nominal demand appears.

---

## How Households Find a Home

Once a household enters the city, it evaluates available residential properties based on a quality score. That score weighs together several things: how much floor space each family member would get, the building's upgrade level, nearby service coverage (healthcare, education, entertainment), pollution levels, and how far the commute would be from the home to where household members work.

Wealthier households are more selective — they will pass on a perfectly adequate apartment in a polluted or under-served neighborhood in favor of something better elsewhere. A household without enough money has fewer options and may end up in whatever is available.

---

## Growing Up: The Citizen Lifecycle

Citizens age through four life stages: child, teen, adult, and elder. The transitions happen on a fixed schedule measured in simulation days.

- **Children** (days 0–20) attend school but otherwise depend entirely on their household.
- **Teens** (days 21–35) continue in school. At the end of this stage they reach adulthood and the game flags them to leave their parents' household and strike out on their own.
- **Adults** (days 36–83) are the city's workforce. They seek jobs, can have children of their own, and form the backbone of household income.
- **Elders** (day 84 and beyond) retire — they lose their jobs automatically when they age into this stage and no longer contribute wages.

> **ℹ️ Info — When does a young adult actually move out?**
> A teen who ages into adulthood does not move out instantly. The game only splits them off into a new household when three conditions are met: they have found a job, the parent household has more than 4,000 in savings, and — if there are at least 10 vacant homes in the city — a new property can be sought. If housing is tight, the young adult may instead become a commuter rather than a full resident.

A citizen's maximum natural lifespan is roughly 9 game-years. Death probability rises with age along a curve, so early natural deaths are rare but become increasingly likely as citizens grow old. Sick or injured citizens also face a chance of dying, though hospital treatment meaningfully reduces that risk.

---

## Having Children

Adult female citizens in a housed household have a base 2% chance per update of having a child, roughly 16 times per game day. That probability jumps by an additional 8 percentage points if there is also an adult male in the household. Female students face a 50% reduction to this rate.

New children are born directly into the existing household. There is no cap on household size written into the birth formula itself, though the internal buffer that tracks household members has a default capacity of five.

---

## How Citizens Find Jobs

Unemployed adult citizens actively search for open positions. The search is driven partly by how many vacancies exist relative to how many workers are available — when jobs are plentiful, citizens find work faster. Once employed, a citizen has a roughly 3% chance per update of checking for a better position elsewhere, so job-switching is an ongoing background process.

When a citizen ages from adult to elder, they lose their job automatically. Children and teens do not hold jobs; they are students.

---

## What Makes Citizens Happy

Every citizen in your city is continuously scored on two separate scales: health (0–255) and wellbeing (0–255). Their overall happiness is simply the average of the two. The game evaluates 26 distinct factors every day and adjusts both scores accordingly.

The factors that hit health directly include water supply quality, water pollution, sewage service, healthcare coverage, and whether the citizen has an active illness.

Wellbeing is affected by a broader set: electricity supply and cost, water and sewage service, noise, air and ground pollution, crime levels, garbage collection, entertainment and park access, education coverage, mail delivery, welfare services, leisure activity, taxes, housing quality, shopping satisfaction, and unemployment.

> **ℹ️ Info — How much do specific factors hurt?**
> Some reference values from the game's own settings:
> - No electricity: −20 wellbeing (after a short delay)
> - No water or sewage: −20 health and −20 wellbeing each
> - Air/ground pollution: up to −50 wellbeing
> - Noise pollution: up to −15 wellbeing
> - Crime: up to −30 wellbeing (below a negligible threshold there is no penalty at all)
> - A family member dying: −20 wellbeing and −10 health
> - Being homeless: −20 health and −20 wellbeing
> - Taxes hit more educated citizens harder — a highly educated citizen suffers twice the tax wellbeing penalty of an uneducated one

Housing quality itself contributes: a cramped apartment in a low-level building scores worse than a spacious unit in a leveled-up building, and that difference feeds directly into wellbeing.

---

## When and Why Households Leave

A household evaluates its situation regularly and may decide to leave the city for three reasons.

**Unhappiness.** The game computes average happiness across all household members. The lower it falls, the higher the probability of the household deciding to emigrate on any given day. This is not an instant cliff — it is a gradual probability increase, so a mildly unhappy household might stay for a long time, while a deeply miserable one will likely leave soon.

**Financial ruin.** If a household's total wealth plus recent income drops below −1,000, it is forced to leave. Households spend money every day on consumption, and if income cannot keep up — because members are unemployed, taxes are too high, or upkeep drains too much — they will eventually reach this threshold.

**No adults left.** If all adult and elderly members of a household die or leave, with only children or teens remaining, the household immediately leaves. The game does not model orphaned children continuing to live independently.

When a household decides to leave, it routes to an outside connection — by road if they own a car, otherwise by train, air, or water — and is removed from the simulation on arrival.

> **ℹ️ Info — What about homelessness?**
> There is no persistent homeless population in Cities: Skylines II. If a household loses its property and cannot find a new one, it is immediately queued to leave the city. Homelessness is an exit state, not a stable condition.

---

## What Can Go Wrong

**Population growth stalls despite high demand.** If your residential vacancy rate is very low, the immigration system throttles itself. Build more housing before expecting more residents — demand alone is not enough.

**Young adults never leave home.** If unemployment is high or housing is scarce, adult children stay in their parents' household indefinitely. This inflates household sizes, reduces housing turnover, and can suppress household formation across the city.

**Households leave faster than they arrive.** A city with poor services, high taxes, heavy pollution, or widespread unemployment will see ongoing emigration. Each factor nudges happiness downward, and enough combined pressure tips households over the emigration threshold.

**An aging population drains income.** As adult citizens age into elders and retire, household income drops. If birth rates are low and immigration is slow, a city can tip into a demographic decline where consumption outpaces wages and more households fall into the poverty threshold.

**Service gaps cascade.** A water outage hurts both health and wellbeing simultaneously. Residents in an area that loses water, power, and sewage treatment at the same time will see happiness collapse quickly — and if the outage persists long enough, they will start leaving before services are restored.