# Healthcare & Ambulances

## How Citizens Get Sick or Injured

Every citizen in your city is constantly being evaluated for health problems. The lower a citizen's health, the more likely they are to fall ill — and the relationship isn't linear. A citizen at 50% health is far more than twice as likely to get sick as a citizen at full health. Older citizens, citizens living in pollution, and citizens with poor access to parks and services will naturally drift toward lower health, making them significantly more vulnerable over time.

When a citizen does fall ill or get injured, the game decides one of two things: can this person manage on their own, or do they need an ambulance?

The probability that a sick citizen needs an ambulance versus walks in depends on how serious the health event is. The game uses health-level-gated probability ranges: mild illness in a citizen at moderate health almost never requires ambulance transport. The same illness type in a citizen at very low health almost always requires it. The seriousness of the event and the citizen's current health together determine which side of the transport probability they land on.

This is why keeping baseline citizen health high is so important for ambulance capacity: a city full of citizens at 80+ health generates far fewer ambulance requests than one where chronic pollution or poor services have pushed citizens to 40–50 health. The ambulance fleet is effectively a measure of how sick your population is, not just how large it is.

> **ℹ️ Info — What counts as "needing an ambulance"?**
> Each type of health event (disease, injury, sudden death) has a transport probability range. Whether any given citizen needs an ambulance is rolled at the moment the health event occurs. Mild cases walk to the hospital themselves; serious cases require pickup. Citizens who are trapped in a burning or collapsed building are always flagged as needing emergency transport.

---

## Two Paths: Walk-In vs. Ambulance

**If the condition is mild**, the citizen will try to walk or travel to a nearby hospital on their own. They get a destination added to their routine and head there when their schedule allows. No ambulance is involved.

**If the condition is serious**, a request for an ambulance goes out immediately. The game finds the nearest hospital that has an ambulance available and sends one to the citizen's current location — whether that's their home, their workplace, or even a vehicle they're already riding in.

> **ℹ️ Info — What makes a hospital "available" for dispatch?**
> A hospital can send an ambulance only if it has idle vehicles and its service district covers the area where the citizen is located. Hospitals with no district assignment serve the whole city. Hospitals assigned to specific districts only respond within those districts.

---

## What Happens After the Ambulance Arrives

Once an ambulance reaches the citizen, it stops and waits for them to board. After pickup, the ambulance doesn't automatically head back to the hospital that sent it. Instead, it immediately searches for the **best nearby hospital** to deliver the patient to.

Most of the time, this will be the same clinic the ambulance came from — it's nearby, it's familiar, and the game gives it a preference. But the ambulance will route to a different hospital if:

- **The home clinic is full** — all patient beds are occupied. A full clinic still counts as a valid option (the ambulance won't ignore it entirely), but a clinic with open beds looks significantly cheaper to the routing system and will usually win.
- **The home clinic can't treat this patient's severity** — basic clinics have a health range. A citizen who is critically ill may be outside what a small neighborhood clinic can handle. In that case, the home clinic is ruled out entirely and the ambulance goes to a facility equipped for serious cases.

> **ℹ️ Info — How the game picks the delivery hospital**
> After picking up a patient, the ambulance evaluates every healthcare facility in the area. Each one is scored by treatment quality (how good is the staff and equipment?) adjusted for how sick the patient is. Sicker patients weight treatment quality more heavily — the system works harder to find a good hospital for serious cases. A facility with open beds is strongly preferred over a full one, but a full facility isn't ruled out if it's the only option.

---

## Critical Patients

If a citizen is in very poor health when they're picked up, the ambulance marks them as a critical patient. Critical patients get priority routing — the ambulance ignores traffic restrictions and finds the fastest possible path to the delivery hospital, regardless of road type or other limitations.

Being marked critical doesn't change *which* hospital the patient goes to — that's still decided by the same quality-and-availability formula. It just means the ambulance drives there faster.

---

## Inside the Hospital

Once the patient arrives, they're admitted to a patient bed. The hospital works on them over time, and how quickly they recover depends on the hospital's treatment quality — which is influenced by:

- **Building efficiency** (how well-funded and staffed the hospital is)
- **Resource availability** (hospitals without adequate resources treat patients more slowly)
- **The hospital's inherent quality** (larger, more advanced hospitals have higher base treatment capability)

Not every hospital can treat every condition. Small clinics may only handle diseases, or only injuries. A large general hospital handles both. If a patient ends up at a facility that can't treat their specific condition, they'll be transferred out.

> **ℹ️ Info — Health range and treatment capability**
> Each hospital has a health range it can treat (e.g., citizens between 30–100% health). Citizens outside that range won't be routed there at all — they go straight to a facility that can handle their severity. Upgrades can expand a hospital's health range, allowing it to treat more critical patients. This is why upgrading a clinic matters: it widens the range of citizens it can help, not just increases capacity.

---

## Building Upgrades and What They Add

Healthcare buildings in CS2 are not static — they can be upgraded to expand their capability in three distinct ways.

**Capacity upgrades** add more patient beds to the facility. Every bed added allows the hospital to treat one more citizen simultaneously. A basic clinic has a fixed bed count; adding capacity upgrades is the primary way to expand throughput without building an entirely new facility.

**Vehicle upgrades** add ambulances to the facility's fleet. Ambulance count is what determines how many simultaneous dispatch requests the facility can answer. A facility with 8 beds but only 2 ambulances will have idle beds while serious cases wait for an available vehicle. Balancing beds and ambulances is the key resourcing decision for each hospital.

**Range/capability upgrades** expand the health range the facility can treat. A basic clinic treats citizens in the 50–100 health range. Upgrading the facility's treatment capability lowers that floor, allowing it to handle more critical patients. Without this upgrade, critically ill citizens are routed past the local clinic to a more capable facility further away.

> **ℹ️ Info — Upgrade priority:**
> In high-density residential areas, ambulance upgrades typically matter more than bed upgrades. Beds are useless if all your ambulances are deployed. In areas with high illness rates or an elderly population, treatment range upgrades prevent long-distance transfers that leave local beds empty.

---

## Service Districts and Ambulance Dispatch

Like fire stations and police stations, hospitals can be assigned to specific service districts. When assigned, a hospital's ambulances will prioritize calls from within the designated district.

This is useful in large cities where a hospital on one side of the city might otherwise attempt to respond to a call across town when a closer, unassigned hospital could handle it. District assignment focuses each facility on its local area and keeps ambulances from making cross-city trips unnecessarily.

A hospital without a district assignment covers the entire city — it will respond to any request it receives, as long as it has idle ambulances. The dispatch system will still prefer closer facilities, so an unassigned hospital in a dense city center will naturally be used more heavily than one on the periphery, even without explicit district configuration.

> **ℹ️ Info — Walk-in patients and districts:**
> Service district assignment only affects ambulance dispatch, not walk-in patients. A citizen who walks or travels to a hospital under their own power can use any hospital in the city, regardless of which district the hospital is assigned to. Walk-ins do consume bed capacity, which can affect how many beds are available when the next ambulance arrives. In very busy hospitals, walk-in pressure can fill beds that were intended for ambulance deliveries.

---

## What Can Go Wrong

**No ambulance shows up:**
All of that hospital's ambulances are already deployed. If a hospital's fleet is entirely out on calls, it can't send another one until one returns. Adding ambulance capacity via upgrades or building additional hospitals reduces how often this happens.

**The ambulance takes a patient to a distant hospital:**
The local clinic's beds are full, or the patient's condition exceeds what the local clinic can treat. Building a larger hospital (or upgrading clinics to handle more patient severity) keeps patients closer to home.

**Citizens are showing the "waiting for ambulance" warning icon:**
This appears after a citizen has been waiting too long for pickup. It's a sign that ambulance supply isn't keeping up with demand — either not enough vehicles, or a hospital's coverage district doesn't reach the area in need.

**Citizens dying at home despite healthcare coverage:**
Very elderly citizens have a naturally high death probability regardless of health levels. Some deaths are simply age-related and won't be prevented by healthcare coverage. However, citizens who die without access to a hospital at all count differently and may affect your city statistics.

**Ambulance/bed ratio miscalibrated:**
A hospital with many beds but few ambulances will have idle capacity while citizens wait outdoors for pickup. A hospital with many ambulances but few beds will have ambulances circling looking for a hospital that can accept the patient. Check both metrics separately — the building info panel shows current beds occupied vs. total and ambulances deployed vs. total.

**Walk-ins quietly consuming beds during a crisis:**
During a major disease event or disaster, walk-in patients may fill beds faster than ambulances can deliver serious cases. Hospitals near a disaster zone that are accessible on foot will absorb a disproportionate number of walk-ins. If ambulance cases are being routed past a nearby hospital to a more distant one, check the nearby hospital's bed occupancy — it may be walk-in-full rather than genuinely full.
