// Decompiled from Game.dll — Game.Simulation.CitizenHappinessSystem
// Calculates citizen happiness based on 26 environmental and service factors.

using Game.Citizens;
using Unity.Burst;
using Unity.Entities;

namespace Game.Simulation;

public class CitizenHappinessSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
    public enum HappinessFactor
    {
        Telecom,          // Telecom coverage quality
        Crime,            // Crime level at residence
        AirPollution,     // Air pollution at residence
        Apartment,        // Apartment size / quality / level
        Electricity,      // Electricity supply
        Healthcare,       // Healthcare coverage
        GroundPollution,  // Ground pollution at residence
        NoisePollution,   // Noise pollution at residence
        Water,            // Water supply
        WaterPollution,   // Water pollution
        Sewage,           // Sewage service
        Garbage,          // Garbage collection
        Entertainment,    // Entertainment / park coverage
        Education,        // Education coverage
        Mail,             // Mail service
        Welfare,          // Welfare coverage
        Leisure,          // Leisure satisfaction counter
        Tax,              // Tax rate effect
        Buildings,        // Building health/wellbeing effects (prison, school)
        Consumption,      // Shopping satisfaction (ShoppedValueLastDay)
        TrafficPenalty,    // Commute-related penalty
        DeathPenalty,      // Family member death penalty
        Homelessness,      // Homeless penalty
        ElectricityFee,    // Electricity fee relative to default
        WaterFee,          // Water fee relative to default
        Unemployment,      // Unemployment duration penalty
        Count
    }

    // Happiness = average of WellBeing and Health (both 0-255 bytes)
    // Each factor contributes a health bonus and a wellbeing bonus (int2)
    // Final values clamped and applied to citizen's m_Health and m_WellBeing

    // Key factors checked per citizen:
    // - Property-based: electricity, water, sewage, garbage, pollution, noise
    // - Service coverage: healthcare, education, entertainment, telecom, mail, welfare
    // - Economic: tax rate, consumption, electricity/water fees
    // - Personal: unemployment duration, homelessness, crime victimization
    // - Leisure counter: decremented randomly each tick, reset by leisure activities
}
