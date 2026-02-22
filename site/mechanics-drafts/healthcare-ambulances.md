# Healthcare & Ambulances

## How Citizens Get Sick or Injured

Every citizen in your city is constantly being evaluated for health problems. The lower a citizen's health, the more likely they are to fall ill — and the relationship isn't linear. A citizen at 50% health is far more than twice as likely to get sick as a citizen at full health. Older citizens, citizens living in pollution, and citizens with poor access to parks and services will naturally drift toward lower health, making them significantly more vulnerable over time.

When a citizen does fall ill or get injured, the game decides one of two things: can this person manage on their own, or do they need an ambulance?

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

## What Can Go Wrong

**No ambulance shows up:**
All of that hospital's ambulances are already deployed. If a hospital's fleet is entirely out on calls, it can't send another one until one returns. Adding ambulance capacity via upgrades or building additional hospitals reduces how often this happens.

**The ambulance takes a patient to a distant hospital:**
The local clinic's beds are full, or the patient's condition exceeds what the local clinic can treat. Building a larger hospital (or upgrading clinics to handle more patient severity) keeps patients closer to home.

**Citizens are showing the "waiting for ambulance" warning icon:**
This appears after a citizen has been waiting too long for pickup. It's a sign that ambulance supply isn't keeping up with demand — either not enough vehicles, or a hospital's coverage district doesn't reach the area in need.

**Citizens dying at home despite healthcare coverage:**
Very elderly citizens have a naturally high death probability regardless of health levels. Some deaths are simply age-related and won't be prevented by healthcare coverage. However, citizens who die without access to a hospital at all count differently and may affect your city statistics.
