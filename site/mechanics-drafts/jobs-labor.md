## Jobs and the Labor Market: How Employment Works in Your City

Every employed citizen in your city followed a path from unemployed newcomer to a specific desk, counter, or workbench in a specific building — and the game tracks every step of that journey. Understanding the employment system helps explain why offices sit half-empty, why a shortage of unskilled workers can be just as damaging as a shortage of engineers, why your rush-hour traffic spikes at different times in different neighborhoods, and what actually happens to your city's economy when unemployment climbs.

---

## Job Types and Which Buildings Provide Them

Not all jobs are the same, and different zones create fundamentally different kinds of work. The game internally classifies every business and service building by its "complexity" — a measure of how educated the workforce it needs is.

Farms and mines sit at one extreme. They are manual operations that need mostly uneducated and poorly educated workers, with only a small fraction of positions requiring any real schooling. Basic industrial factories sit one step up: they still lean toward less-educated labor but begin drawing on the middle of the education spectrum.

Offices and high-technology firms occupy the other end of the scale entirely. An office tower needs educated, well-educated, and highly educated workers in its upper tiers, with very few slots at the bottom. The most advanced research and software companies are almost exclusively staffed by well-educated and highly educated citizens.

Commercial zones — shops, restaurants, service businesses — fall somewhere in the middle depending on the specific building. A corner grocery needs different staff than a luxury boutique.

> **ℹ️ Info — How education slots are calculated:**
> For each business, the game runs a formula that distributes job slots across five education levels (uneducated through highly educated). The formula centers the distribution on a target point that shifts based on both business type and building level. A level-1 farm centers almost entirely on uneducated workers; a level-5 office tower centers on highly educated ones. Building upgrades push that center point higher, meaning a leveled-up industrial building needs a more educated workforce than a brand-new one of the same type.

Public service buildings — hospitals, schools, fire stations, police stations — also employ workers and follow the same rules. A school naturally requires more educated staff than a garbage facility.

---

## How Citizens Find Jobs

When an adult citizen is unemployed, the game creates a job-seeking agent for them and begins searching for an open position. The process is not random: the citizen's education level is the primary filter. A highly educated citizen will first be directed toward jobs that genuinely need that level of education. Only if the matching level has no openings does the search step down — a well-educated citizen can fill an educated-level slot, and so on down the ladder.

This "walk down" mechanic means educated workers can always fill lower-tier jobs if nothing better is available, but uneducated citizens cannot fill educated slots at all. An unemployed laborer will never occupy a software engineer's chair.

The job search also involves actual pathfinding. Once the system identifies a suitable vacancy, the citizen is sent on a route to the nearest reachable workplace with an appropriate opening. Distance matters: the city's road network determines which businesses are actually accessible from where the citizen lives.

> **ℹ️ Info — How often job-seeking runs:**
> The job search system processes all seeking citizens roughly every 16 simulation frames. Once a match is found and the pathfinding completes, the citizen is formally hired: they are added to the employer's staff list, assigned a work shift, and marked as employed. This is not instantaneous — there is a short pipeline delay between a vacancy opening and it being filled.

Already-employed citizens are not frozen in place. There is an ongoing process that checks whether employed workers could be better matched elsewhere. If vacancies at higher education levels exist in meaningful numbers relative to the available workforce, employed citizens may be nudged to seek something more appropriate. This is why you may observe some employment churn even when overall unemployment is low.

---

## Education Level and the Job Hierarchy

Education creates a strict one-way filter in the labor market. The five levels — uneducated, poorly educated, educated, well-educated, and highly educated — determine which job slots a citizen can fill. A citizen can fill any slot at or below their own education level, but never above it.

This asymmetry creates a real tension. A city that builds many offices and high-tech industrial zones will generate enormous demand for educated workers but offer little to uneducated citizens. Those uneducated citizens may go unemployed even while companies post vacancies, because the vacancies exist in tiers the workers cannot reach.

The reverse problem is equally common: a city that invests heavily in schools and universities but builds mostly farms and basic industry will produce educated workers who are overqualified for what is available. They will eventually take lower-tier jobs out of necessity, but in the meantime they are unemployed and unhappy — and the well-educated positions at higher tiers remain unfilled regardless.

> **ℹ️ Info — The workforce value of education:**
> Higher education does not just open more doors — it makes each worker more productive. The game assigns a "workforce value" to every employee based on their education level and happiness. An uneducated worker at average happiness provides about 1.75 units of productive value. A highly educated worker at the same happiness provides nearly 9.6 units. This is not linear — the gap between an educated and highly educated worker is significantly larger than the gap between uneducated and poorly educated.

---

## Commuters: Outside Workers Filling Educated Vacancies

When your city has more open educated jobs than local workers to fill them, the game begins spawning commuter households. These are not residents — they arrive from outside connections, go to work, and return. They do not consume housing or city services in the same way residents do.

The commuter spawning rate scales with the size of the educated job surplus. The larger the gap between available educated positions and the local educated workforce, the faster commuters arrive to fill it. There is also a built-in governor: once the ratio of commuters to total workers reaches a ceiling, spawning stops even if vacancies remain.

Commuter spawning only applies to educated positions (levels two through four). Uneducated job vacancies do not attract outside commuters — if there are no local uneducated workers available, those slots simply stay empty.

---

## Work Shifts and Rush Hours

When a citizen is hired, the game assigns them a shift: day, evening, or night. The shift assignment is partly random and varies by building type. For most service buildings, there is roughly a 25% chance the new hire lands on the evening shift and a 25% chance they land on the night shift, leaving about 50% on the standard day shift.

This matters for traffic. Day-shift workers all commute in the morning and leave in the late afternoon, creating the familiar rush-hour peaks. Evening workers travel at different times, and night-shift workers commute during hours when roads are otherwise quiet. A city with a healthy mix of industrial, commercial, and service employment will naturally spread traffic more evenly across the day than one dominated by office jobs, which are almost entirely day-shift.

> **ℹ️ Info — Shift assignments are permanent for the duration of employment:**
> A citizen keeps their assigned shift for as long as they work at a given employer. If they leave and are rehired somewhere new, they go through the random assignment again. Shift distribution is a property of the hiring event, not something that changes over time.

---

## Commute Time and Citizen Wellbeing

Getting to work takes time, and the game measures every citizen's commute. Long commutes directly affect wellbeing — a citizen who spends a significant portion of their day traveling to and from work is less happy than one who can walk or take a short bus ride.

The wellbeing penalty is tied to the actual travel time on the road network, which means that transit quality, road congestion, and residential location all feed into the same outcome. A citizen living three kilometers from their job in a city with good public transit may commute more happily than one living one kilometer away in a car-dependent neighborhood choked with traffic.

This creates a useful feedback loop for city design. A dense, mixed-use neighborhood where residents can walk to nearby commercial or office buildings tends to produce happier workers than a city of separated residential and industrial zones connected only by highways.

---

## Building Efficiency and Staffing

When a business or service building is fully staffed, it operates at full efficiency. When it is understaffed, efficiency drops — and that drop takes a moment to kick in. There is a short grace period before the game formally penalizes a building for being short-handed, which means brief vacancies do not immediately impair operations.

Once the penalty does apply, it reduces the building's output, productivity, or service capacity in proportion to how understaffed it is. For a commercial business, this means reduced sales and income. For a hospital or fire station, it means reduced service coverage.

Worker happiness feeds into efficiency in the opposite direction. Employees who are happy contribute more productive value, and the workplace's own conditions (how pleasant or unpleasant a place it is to work) apply a modifier on top. A business with happy, well-paid workers in a building with good conditions will operate slightly above its baseline capacity.

> **ℹ️ Info — Shortage notifications:**
> When the ratio of open positions to total positions exceeds a threshold for long enough, the game displays a shortage warning icon on the building. There are separate icons for uneducated worker shortages and educated worker shortages, so you can see at a glance what kind of workers are missing without checking each building manually.

---

## Too Few Jobs vs. Too Few Workers

These are mirror-image problems with very different consequences.

When there are too few jobs — more employable residents than open positions — unemployment rises. Unemployed citizens suffer a direct wellbeing penalty. Beyond the individual, widespread unemployment reduces household income across the city, which constrains consumer spending in commercial zones, reduces tax revenue, and can push households toward insolvency. High unemployment in a neighborhood tends to produce emigration over time, which in turn reduces consumer demand further.

When there are too few workers — more open positions than available labor — businesses run understaffed and operate below capacity. Service buildings deliver less coverage. Companies produce less and generate less income, which can suppress commercial activity in the city. The game will attempt to address educated labor shortages by spawning commuters, but there is no equivalent safety valve for uneducated labor shortages.

Both problems can coexist in the same city if the education profile of your residents does not match the education profile of your jobs. A city full of university graduates and nothing but farms will have both high educated unemployment and a chronic uneducated labor shortage simultaneously.

---

## What Can Go Wrong

**Educated workers are unemployed but offices still show vacancies.** This almost always means your educated workers are not reaching the jobs. Check whether the road network actually connects residential areas to office districts — if pathfinding fails, the job match never completes even though both sides exist in abundance.

**Uneducated vacancies never fill no matter how many residents you have.** Commuters only cover educated positions. If your city's population skews educated and you are building basic industry, there may simply not be enough low-education workers. Reducing the pace of school-building or zoning differently will not fix an existing mismatch quickly — it takes time for the resident population to shift.

**Traffic is worst in the morning even though you have lots of commercial and industrial zones.** If most of your employment is office-type (high complexity), the day-shift concentration is much higher than in a mixed economy. Evening and night shift assignments are a property of each building type — more industrial, service, and commercial variety will organically spread the commuting load.

**A building's efficiency has been low for a long time despite some staff.** The efficiency penalty ramps up gradually rather than snapping to zero instantly. If a building has been chronically understaffed through multiple hiring cycles, it may have accumulated a deep efficiency deficit. Filling vacancies will improve the situation, but there is a cooldown period before the building returns to full operation.

**Commuters flooded in and now outnumber residents.** The commuter ratio cap is a safeguard, but if you rapidly expanded office zoning in a city without growing your residential population, the commuter system will work hard to fill the gap. Commuters do not pay property taxes or use most services, so a city heavily dependent on commuter labor is more economically fragile than one where residents hold most jobs.

**Unemployment keeps climbing even though job numbers look healthy.** Look at education-level mismatches. The city-wide unemployment number hides the breakdown — it is entirely possible to have simultaneous high unemployment among uneducated residents and significant vacancy rates in educated tiers, and the two groups cannot help each other.
