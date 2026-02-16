// Decompiled from Game.dll — Game.Simulation.AccidentSiteSystem
// Key excerpt: AccidentSiteJob.Execute() showing RequirePolice clearing behavior

// CRITICAL FINDING: RequirePolice is UNCONDITIONALLY cleared every tick,
// then conditionally re-set based on severity evaluation.

// Line-by-line annotation of the RequirePolice logic:

// Step 1: UNCONDITIONAL CLEAR — happens every tick for every AccidentSite
accidentSite.m_Flags &= ~AccidentSiteFlags.RequirePolice;

// Step 2: CONDITIONAL RE-SET — only if severity > 0 or active crime scene
if (num2 > 0f || (accidentSite.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene)) == AccidentSiteFlags.CrimeScene)
{
    if (num2 > 0f || (accidentSite.m_Flags & AccidentSiteFlags.CrimeDetected) != 0)
    {
        if (flag)  // flag = chunk.Has<Building>()
        {
            entity2 = entity;  // For buildings, target is the building itself
        }
        if (entity2 != Entity.Null)
        {
            accidentSite.m_Flags |= AccidentSiteFlags.RequirePolice;
            RequestPoliceIfNeeded(unfilteredChunkIndex, entity, ref accidentSite, entity2, num2);
        }
    }
}

// Where num2 (severity) comes from:
// - Iterates TargetElement buffer of the associated event
// - For each target with InvolvedInAccident, checks componentData.m_Severity
// - num2 tracks the maximum severity across all involved entities
// - entity2 tracks the highest-severity non-moving entity (stationary target)

// RequestPoliceIfNeeded only creates a new request if one doesn't already exist:
private void RequestPoliceIfNeeded(int jobIndex, Entity entity, ref AccidentSite accidentSite, Entity target, float severity)
{
    if (!m_PoliceEmergencyRequestData.HasComponent(accidentSite.m_PoliceRequest))
    {
        PolicePurpose purpose = (((accidentSite.m_Flags & AccidentSiteFlags.CrimeMonitored) == 0)
            ? PolicePurpose.Emergency
            : PolicePurpose.Intelligence);
        Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PoliceRequestArchetype);
        m_CommandBuffer.SetComponent(jobIndex, e, new PoliceEmergencyRequest(entity, target, severity, purpose));
        m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
    }
}

// MODDING IMPLICATIONS:
// - AccidentSiteSystem runs every 64 frames (GetUpdateInterval returns 64)
// - RequirePolice is cleared THEN re-evaluated each tick
// - If a mod sets RequirePolice on an AccidentSite, AccidentSiteSystem will
//   clear it on the next tick unless severity conditions are met
// - Mods that need RequirePolice to persist must use UpdateAfter<AccidentSiteSystem>
//   (set the flag AFTER AccidentSiteSystem has done its clear+re-evaluate cycle)
// - Using UpdateBefore<AccidentSiteSystem> will NOT work because the system
//   unconditionally clears the flag at the start of its evaluation
