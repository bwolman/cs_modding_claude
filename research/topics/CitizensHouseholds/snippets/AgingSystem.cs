// Decompiled from Game.dll — Game.Simulation.AgingSystem
// Handles citizen age transitions: Child->Teen->Adult->Elderly

using Game.Citizens;
using Unity.Burst;
using Unity.Entities;

namespace Game.Simulation;

public class AgingSystem : GameSystemBase
{
    public static readonly int kUpdatesPerDay = 1;

    // Age thresholds in simulation days
    public static int GetTeenAgeLimitInDays() => 21;
    public static int GetAdultAgeLimitInDays() => 36;
    public static int GetElderAgeLimitInDays() => 84;

    // Key logic from AgingJob.Execute():
    // - Iterates household citizens each day
    // - Compares (currentDay - birthDay) against age thresholds
    // - Child -> Teen: removes Student, enables BicycleOwner
    // - Teen -> Adult: removes Student, adds LeaveHouseholdTag (citizen forms own household)
    // - Adult -> Elderly: removes Worker component, removes work travel purpose
    // - Updates citizen age via value.SetAge(CitizenAge.X)
    // - Tracks transitions via NativeCounter (m_BecomeTeenCounter, etc.)

    [BurstCompile]
    private struct AgingJob : IJobChunk
    {
        // ... iterates HouseholdCitizen buffer on each household
        // For each citizen, checks age in days against thresholds:
        //   Child  -> Teen  at day 21
        //   Teen   -> Adult at day 36
        //   Adult  -> Elder at day 84
        //
        // On transition:
        //   Child->Teen:  Remove Student, enable BicycleOwner
        //   Teen->Adult:  Remove Student, add LeaveHouseholdTag
        //   Adult->Elder: Remove Worker, remove TravelPurpose if going to work
    }
}
