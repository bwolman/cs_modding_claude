## How Crime Happens and Police Respond

### How Citizens Become Criminals

Your city's crime problem does not emerge randomly. Once per game day, the simulation quietly evaluates every unemployed adult citizen who is not sick or in school. For each of them, it asks a single question: how unhappy is this person?

A citizen with very low wellbeing — someone barely scraping by — has the highest chance of turning to crime. The probability drops off sharply as wellbeing improves, becoming negligible for contented residents. Population size also matters: in a large city, any given citizen's individual crime probability is diluted downward, so crime scales more slowly than raw population growth would suggest.

Citizens who have committed crimes before face a different calculation. Repeat offenders are evaluated against the welfare services covering their home building. More welfare coverage in the area reduces the chance they reoffend, giving social services a meaningful role in crime prevention beyond just happiness management.

> **ℹ️ Info — Exactly what disqualifies a citizen from becoming a criminal?**
> Children and the elderly are never evaluated. Students are excluded. Employed citizens are excluded. Citizens who are already sick are excluded. Only unemployed adults (teens through middle-aged) are in the pool. A citizen already participating in an active crime is also skipped.

Once the simulation decides a citizen will commit a crime, it creates a robbery event and assigns that citizen as the perpetrator. There is only one crime type in the game: robbery.

---

### The Criminal's Journey

After being designated a criminal, a citizen does not immediately start stealing. The process unfolds in stages over real time.

First, the citizen enters a planning phase and sets off toward a target building somewhere in the city. This travel phase can take meaningful time depending on how far away the destination is. When the citizen arrives, the building is quietly flagged as a crime scene.

The crime scene has two timers running in parallel. The first is an alarm delay of roughly 5 to 10 seconds — the window before anyone notices something is wrong. Once that window closes, the scene is detected and police can be called. The second timer is the crime duration itself, lasting anywhere from 20 to 60 seconds. This is the window police have to arrive and make an arrest. If they do not make it in time, the criminal commits the robbery — stealing money from a random occupant of the building — and flees.

> **ℹ️ Info — How much is stolen and what are the consequences?**
> The stolen amount combines a fixed random component (100 to 1000 in-game currency) with a percentage of the victim's current money (0 to 25%). Victims suffer a wellbeing penalty: household members of the robbed building lose more than employees do. If the criminal is caught, there is a 50% chance they receive a prison sentence on top of a short jail stay. Jail runs from a few hours to one game day; prison can run from 1 to 100 game days.

A small number of criminals are flagged as monitored — the equivalent of being under surveillance — which causes police to be notified immediately when the crime scene appears, bypassing the alarm delay entirely.

---

### What Patrol Does vs. Emergency Response

Police cars serve two distinct roles, and they work very differently.

Patrol is background suppression. Every building in your city that can attract crime quietly accumulates a crime score over time. The rate depends on the zone type and — crucially — how much police coverage that area receives. High coverage areas accumulate crime slowly; uncovered areas accumulate it quickly. When a building's crime score crosses a threshold, a patrol car is dispatched to cruise the area. Patrol cars do not use lights or sirens. Their presence actively reduces the crime score of buildings they pass.

Emergency response is reactive. When a crime scene is detected — after that alarm delay expires — the game sends out an emergency dispatch request. A police car is pulled from the nearest available station or from an on-duty car already in the area. That car switches to emergency mode: lights on, sirens active, traffic laws suspended. Other vehicles yield. The car navigates at high priority directly to the crime scene.

> **ℹ️ Info — Emergency vs. patrol dispatch compared**
> Emergency dispatch finds the nearest car by district, uses distance-only pathfinding, and sets the car to full emergency mode (lights, sirens, lane exemptions). Patrol dispatch selects cars differently — only empty cars not near the end of their shift qualify — uses balanced routing that considers road comfort and cost, and never activates emergency lights. A patrol car cannot be converted to emergency response mid-patrol; the game creates a separate emergency request and the responding car may be a different vehicle entirely.

When a responding emergency car arrives within 30 meters of the crime scene — or gets close enough but finds its path blocked — it stops and secures the scene. If the criminal is still present at that moment, they are arrested on the spot. If the criminal has already fled, the scene is cleaned up and the car returns to its station.

---

### After an Arrest

An arrested criminal is held at the police station while their jail time counts down. When the time expires, the simulation rolls a coin flip: roughly half of arrested criminals are released after jail, and the other half are sentenced to prison. Sentenced criminals wait at the police station for a prisoner transport vehicle to take them to a prison facility, where they serve a much longer sentence before being released back into the city population.

---

### What Can Go Wrong

**High crime with no arrests:** If a neighborhood has no police coverage — no station in range and no patrol cars passing through — crime accumulates on buildings without limit and patrol requests pile up. Emergency response also suffers: dispatch tries to find a police station in the same district as the crime scene. With no nearby station, no car is found and the request eventually fails.

**Police arrive too late:** The criminal has a fixed time window to commit the crime and escape. If the responding car is far away, stuck in traffic, or delayed by an already-busy dispatch queue, the crime duration expires before the car arrives. The criminal flees, the scene closes, and no arrest is made.

**Criminals escape and leave phantom crime scenes:** If a criminal escapes before police secure the scene, the crime scene marker on the building can linger in an unresolved state. The simulation only removes it once it confirms no active criminals are still tied to the event. In a large city, multiple such stale scenes can accumulate without ever being cleaned up.

**Patrol cars at shift end:** Each police car operates on a shift. Near the end of a shift, cars are no longer eligible for new patrol assignments and will not accept additional dispatches. If the station does not have enough cars to maintain coverage across all shifts, areas can be left unpatrolled for extended periods — and buildings in those areas will accumulate crime unchecked until the next car comes on duty.

**No prisoner transport available:** An arrested and sentenced criminal will wait at the jail indefinitely for a transport vehicle to take them to prison. If no transport arrives within a set time, the criminal is released without serving their prison sentence. A station that is perpetually busy processing new arrests while also waiting for transport can create a revolving door effect.