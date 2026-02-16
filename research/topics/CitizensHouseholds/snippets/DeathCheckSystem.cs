// Decompiled from Game.dll — Game.Simulation.DeathCheckSystem
// Checks citizens for natural death and sickness-related death.

using Game.Citizens;
using Unity.Burst;
using Unity.Entities;

namespace Game.Simulation;

public class DeathCheckSystem : GameSystemBase
{
    public static readonly int kUpdatesPerDay = 4;
    public static readonly int kMaxAgeInGameYear = 9;

    // Key logic from DeathCheckJob.Execute():
    //
    // 1. Natural death (age-based):
    //    - Calculates normalized age: ageInDays / daysPerYear / kMaxAgeInGameYear
    //    - Evaluates death probability via m_HealthcareParameterData.m_DeathRate curve
    //    - Uses citizen's pseudo-random to check against the curve value
    //    - Citizens near max age have increasing death probability
    //
    // 2. Sickness death:
    //    - If citizen has HealthProblem (Sick or Injured), additional death chance
    //    - Based on health: num = 10 - health/10, then num*num + 8
    //    - Random check: NextInt(kUpdatesPerDay * 1000) <= num*num+8
    //
    // 3. Recovery:
    //    - If in a hospital (with TreatmentBonus), recovery chance improves
    //    - CityModifier RecoveryFailChange also affects odds
    //    - On recovery: clears Sick/Injured/RequireTransport flags
    //
    // Die():
    //    - Sets HealthProblemFlags.Dead | RequireTransport
    //    - Removes Worker, Student, ResourceBuyer, Leisure components
    //    - Fires TriggerType.CitizenDied and CitizensFamilyMemberDied triggers
    //    - Records StatisticType.DeathRate
}
