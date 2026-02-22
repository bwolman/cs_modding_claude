# Personal Cars in Cities: Skylines II — How Citizens Decide to Drive

## Do Citizens Even Own a Car?

Before a citizen can drive anywhere, their household needs to own a car. Car ownership is not universal — it depends on the household's wealth level and the car type that fits them. Wealthier households tend to own larger or more capable vehicles. Once a household acquires a car, it stays with that household for as long as the household lives in your city. If the household moves away or the building they live in is demolished, the car is deleted along with them.

> **ℹ️ Ownership rule**: Each car belongs to a specific household. The game maintains a list of cars per household. If that list no longer contains a particular car — because the household sold it, moved, or was deleted — the car disappears from the game within roughly 1,000 simulation frames (about one in-game hour).

## From Parked to Moving

When a car is not in use, it sits parked — either in the garage attached to the home building or in a nearby parking lane on the street. The car exists in the simulation the whole time, quietly waiting.

When a citizen decides to travel somewhere (to work, a shop, a park, or back home), the game checks whether driving is the appropriate mode of transport given the route and the household's circumstances. If driving wins out, the parked car gets flagged as ready to depart.

There is a deliberate short delay built in before the car actually appears on the road. Think of it as the time a citizen takes to get downstairs, find their keys, and back out of the driveway. The game counts down this timer in the background. Once it hits zero, the car enters a spawn queue, and the spawn system checks whether there is physical space at the departure point for the car to appear. If the spot is clear, the car materializes on the road network and begins its journey.

> **ℹ️ Spawn rule**: The spawn system runs every 16 simulation frames. A car can only enter the road if its departure location is not physically blocked. If another vehicle is sitting in the way, the car waits in the queue until the space clears.

## Driving and Finding Parking

Once on the road, the car follows a calculated path to its destination, changing lanes, navigating intersections, and joining queues at traffic lights just like any other vehicle. Tolls and fees are deducted automatically as the car passes through toll plazas or enters paid zones.

Approaching the destination, the car needs to find somewhere to park. It scans ahead along its planned path, checking parking lanes on the street and garage spaces inside buildings. If the spot it was heading for has just been taken by another vehicle, it looks further along the path — up to a large number of nodes ahead by default.

> **ℹ️ Parking search rule**: If no valid parking space is found anywhere along the planned path, the car's route is marked as outdated and the game recalculates a new path specifically looking for parking. This can cause vehicles to make surprising detours or appear to "loop" in an area — they are actively searching for a free spot.

When a space is found and the car parks, the driver and passengers get out. If the parking spot charges a fee, money is transferred from the household to the city at this moment.

## Dummy Traffic from Outside Connections

Not every car you see on your roads belongs to a citizen living in your city. Traffic also flows in from the outside world through highway and road connections at the edges of the map. These vehicles are simulated traffic — they drive real routes through your city, obey traffic rules, and contribute to congestion, but they are not attached to any household or citizen. They exist solely to represent the background flow of regional traffic that any real city would experience.

> **ℹ️ Outside traffic rule**: Vehicles flagged as dummy traffic are generated at outside connection points and travel through the road network as normal cars. They are not counted as citizen-owned vehicles and do not park or disembark passengers inside your city.

## When Cars Disappear

Cars are cleaned up under a few circumstances:

- **Household gone**: When a household moves away or their home is demolished, the car is deleted the next time the ownership check runs.
- **Household no longer owns the car**: If the game decides a household should downsize or sell a vehicle due to poverty or other simulation events, the car ceases to exist.
- **Destination reached and trip complete**: The car parks and stays until the next trip begins. It does not disappear after parking — it waits for the next journey.
- **Disaster or accident**: A destroyed vehicle is removed from the simulation.

Cars do not simply despawn after driving for a while or after being parked too long. They persist until one of the above conditions is met.

## What Can Go Wrong

**Parking death spirals.** If your city does not have enough parking near popular destinations, vehicles will loop endlessly recalculating their path trying to find a space. This creates phantom congestion — cars adding to traffic volume without ever completing their trip. The symptom is vehicles appearing to circle the same area repeatedly.

**Spawn point blocking.** If a parking lane or garage exit near a residential area is perpetually blocked by other vehicles, cars queued to leave home cannot get onto the road. Citizens appear "stuck" at home even though they have somewhere to be.

**Outside connection overload.** If you build wide roads connecting to outside regions without providing realistic capacity through your city network, dummy traffic from those connections floods your roads. This is not a bug — it reflects actual through-traffic demand — but it can overwhelm intersections that look fine on paper.

**Car ownership outlasting the household.** In rare edge cases, a car can end up ownerless but not yet deleted, sitting as a ghost vehicle in a parking lane until the next ownership check clears it out. If you notice permanently stationary vehicles that never move, this is the likely cause.
