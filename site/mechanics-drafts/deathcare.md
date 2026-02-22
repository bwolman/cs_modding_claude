## How Citizens Die

Every citizen in your city ages in real time, and when they get old enough, there is a chance they simply die of natural causes each game tick. That probability follows an age curve — the older a citizen is, the more likely they are to die in any given moment. Citizens can also die from illness or injury before old age catches up with them. When a sick or injured citizen is not recovering fast enough, the game calculates a death chance based on how low their health is: a citizen at half health faces a much smaller risk than one who is critically ill and unattended. Hospitals reduce this risk significantly — a citizen receiving good treatment has their recovery chance boosted and their death chance suppressed.

The moment a citizen dies, they are immediately removed from any job, school, or other activity they were participating in. They become a corpse that needs to be collected.

## How Hearses Are Dispatched

As soon as a citizen dies, the game marks that body as needing pickup and looks for the nearest available deathcare facility — either a cemetery or a crematorium — with an idle hearse. When a match is found, one hearse is dispatched from that facility and drives to wherever the citizen died: their home, their workplace, a park, wherever they happened to be.

The hearse drives to the location, picks up the body, and drives back to its home facility. Once the body is unloaded, the hearse returns to standby. Each hearse can carry a fixed number of bodies before it must return to the facility, though in practice each trip typically handles one at a time.

> **ℹ️ Info — How the game decides which facility gets the job**
> The dispatch system matches each waiting corpse to the nearest deathcare facility that currently has at least one idle hearse and physical room to accept more bodies. If your nearest cemetery is full or all its hearses are already out, the game looks further afield for the next eligible facility. Distance matters: a facility on the far side of the city is a valid fallback, but the hearse will take much longer to arrive.

## Cemeteries vs. Crematoriums

These two facility types handle bodies very differently after the hearse arrives.

A **cemetery** places the body into a grave and keeps it there. That grave is occupied indefinitely. Over a long enough time the game does gradually clear graves through a slow background decomposition process, but for practical planning purposes you should treat cemetery capacity as finite and permanent. Once a cemetery fills up, it cannot accept any more bodies until it expands or until old graves slowly free up.

A **crematorium** processes bodies continuously. There is no permanent storage — bodies are consumed at a steady rate determined by the facility's processing speed and its current operating efficiency. A crematorium running at reduced efficiency (due to poor road access, low land value services, etc.) processes bodies more slowly. As long as a crematorium is not overwhelmed by the incoming rate of deaths, it never truly "fills up" the way a cemetery does.

> **ℹ️ Info — Processing rate and efficiency**
> A crematorium's throughput is its base processing rate multiplied by the building's current efficiency. Efficiency is affected by the same citywide factors that affect all service buildings: road access, worker availability, and city policy. A crematorium at 50% efficiency processes bodies at half its rated speed.

## When Deathcare Is Overwhelmed

If your deathcare capacity cannot keep up with the death rate — because all hearses are deployed, all facilities are full, or the facilities are simply too far away — bodies begin to pile up uncollected across the city. The longer a body sits waiting for pickup, the more it drags down the land value and happiness of the surrounding area. Nearby residents and businesses are affected, and if the problem persists long enough, residents will start moving away from affected neighborhoods.

A full cemetery will show a notification on the building. The game will also flag individual corpses that have been waiting too long with a warning icon.

## What Can Go Wrong

**All hearses are already deployed.** A facility can only send out as many hearses as it has. During a wave of deaths — after a disaster or during a particularly dense spike in the elderly population — every hearse may be on the road simultaneously. New deaths have to wait until one returns. Adding a second facility nearby, or choosing a facility with a higher hearse count, resolves this.

**The facility is full.** Cemeteries have a hard capacity limit. Once that limit is reached, the facility stops accepting new bodies entirely. The game will route hearses to the next nearest facility with space, which may be much farther away, slowing down the whole chain. Expanding the cemetery or building a crematorium nearby is the fix.

**No facility is close enough.** If deathcare facilities are clustered on one side of the city, bodies on the far side face very long hearse travel times. The hearse is tied up for longer, the body sits uncollected longer, and land value around the death site suffers for longer. Spreading facilities across your city ensures reasonable coverage everywhere.

**Efficiency is degraded.** A crematorium at low efficiency may not be processing bodies fast enough to keep its queue clear, even if it technically has capacity. Check that the building has full road access, is properly staffed, and that no city-wide service penalties are dragging it down.

**A cascade failure.** A large simultaneous death event — a flood, a fire, a disease outbreak — can overwhelm all of these systems at once: all hearses out, all cemeteries full, crematoriums backed up. Having surplus capacity in normal times gives you the buffer to absorb those spikes without the city suffering lasting damage.