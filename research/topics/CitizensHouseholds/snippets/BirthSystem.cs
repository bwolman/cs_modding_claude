// Decompiled from Game.dll — Game.Simulation.BirthSystem
// Creates new child citizens when adults in a household reproduce.

using Game.Citizens;
using Unity.Burst;
using Unity.Entities;

namespace Game.Simulation;

public class BirthSystem : GameSystemBase
{
    public static readonly int kUpdatesPerDay = 16;

    // Key logic from CheckBirthJob.Execute():
    // - Only adult, non-male, non-tourist, non-commuter citizens can give birth
    // - Household must have a property (PropertyRenter)
    // - Base birth rate from CitizenParametersData.m_BaseBirthRate
    // - If household has an adult male, rate gets AdultFemaleBirthRateBonus
    // - If mother is a student, rate is multiplied by StudentBirthRateAdjust
    // - Random check: if random.NextFloat(1f) < rate / kUpdatesPerDay, spawn baby
    //
    // SpawnBaby():
    // - Creates a new entity from a random CitizenPrefab
    // - Sets Citizen component with m_BirthDay = 0, m_State = None (child)
    // - Adds HouseholdMember pointing to parent household
    // - Adds CurrentBuilding to the household's property
    // - Records StatisticType.BirthRate event
}
