// Extracted from Game.dll — Key HasBuffer guards in downstream systems
// All four systems that process TrafficAccident event entities check
// for TargetElement buffer existence before proceeding.

// --- ImpactSystem.AddImpactJob ---
// Adds target entities to the event's TargetElement buffer
namespace Game.Simulation;
// (simplified from ImpactSystem)
struct AddImpactJob : IJobChunk
{
    public BufferLookup<TargetElement> m_TargetElements;

    public void Execute(/*...*/)
    {
        // ...
        if (m_TargetElements.HasBuffer(involvedInAccident2.m_Event))
        {
            DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[involvedInAccident2.m_Event];
            CollectionUtils.TryAddUniqueValue(dynamicBuffer, new TargetElement(entity));
        }
        // If HasBuffer is false: entity gets InvolvedInAccident but is never tracked
    }
}

// --- AccidentVehicleSystem.AccidentVehicleJob ---
// Processes stopped vehicles involved in accidents
struct AccidentVehicleJob : IJobChunk
{
    public BufferLookup<TargetElement> m_TargetElements;

    public void Execute(/*...*/)
    {
        // ...
        if (!m_TargetElements.HasBuffer(involvedInAccident.m_Event))
        {
            continue;  // Skips: no AddAccidentSite, no notifications, no injuries
        }
        DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[involvedInAccident.m_Event];
        // ... finds or creates accident site
    }
}

// --- AccidentSiteSystem.AccidentSiteJob ---
// Manages accident sites on road edges
struct AccidentSiteJob : IJobChunk
{
    public BufferLookup<TargetElement> m_TargetElements;

    public void Execute(/*...*/)
    {
        // ...
        if (m_TargetElements.HasBuffer(accidentSite.m_Event))
        {
            DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[accidentSite.m_Event];
            // ... counts participants, checks chain-reaction eligibility, dispatches police
        }
        // If HasBuffer is false: cannot count participants, no chain reactions, no police
    }
}

// --- AddAccidentSiteSystem.AddAccidentSiteJob ---
// Adds accident site entities to the event's target list
struct AddAccidentSiteJob : IJobChunk
{
    public BufferLookup<TargetElement> m_TargetElements;

    public void Execute(/*...*/)
    {
        // ...
        if (m_TargetElements.HasBuffer(accidentSite2.m_Event))
        {
            DynamicBuffer<TargetElement> dynamicBuffer2 = m_TargetElements[accidentSite2.m_Event];
            CollectionUtils.TryAddUniqueValue(dynamicBuffer2, new TargetElement(entity));
        }
        // If HasBuffer is false: site added to road edge but event doesn't track it
    }
}
