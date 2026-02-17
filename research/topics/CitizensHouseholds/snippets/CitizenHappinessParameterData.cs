// Decompiled from Game.dll — Game.Prefabs.CitizenHappinessParameterData
// ECS singleton component with thresholds and weights for all 26 happiness factors.

using Colossal.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct CitizenHappinessParameterData : IComponentData, IQueryTypeParameter
{
    // Pollution
    public int m_PollutionBonusDivisor;              // Default: 600
    public int m_MaxAirAndGroundPollutionBonus;       // Default: 50
    public int m_MaxNoisePollutionBonus;              // Default: 15

    // Electricity
    public float m_ElectricityWellbeingPenalty;       // Default: 20
    public float m_ElectricityPenaltyDelay;           // Default: 32 ticks
    public AnimationCurve1 m_ElectricityFeeWellbeingEffect;

    // Water & Sewage
    public int m_WaterHealthPenalty;                  // Default: 20
    public int m_WaterWellbeingPenalty;               // Default: 20
    public float m_WaterPenaltyDelay;                 // Default: 32 ticks
    public float m_WaterPollutionBonusMultiplier;     // Default: -10
    public int m_SewageHealthEffect;                  // Default: 10
    public int m_SewageWellbeingEffect;               // Default: 20
    public float m_SewagePenaltyDelay;                // Default: 32 ticks
    public AnimationCurve1 m_WaterFeeHealthEffect;
    public AnimationCurve1 m_WaterFeeWellbeingEffect;

    // Wealth thresholds
    public int4 m_WealthyMoneyAmount;                 // Default: (0, 1000, 3000, 5000)

    // Services
    public float m_HealthCareHealthMultiplier;        // Default: 2.0
    public float m_HealthCareWellbeingMultiplier;     // Default: 0.8
    public float m_EducationWellbeingMultiplier;      // Default: 3.0
    public float m_NeutralEducation;                  // Default: 5.0
    public float m_EntertainmentWellbeingMultiplier;  // Default: 20.0

    // Crime
    public int m_NegligibleCrime;                     // Default: 5000
    public float m_CrimeMultiplier;                   // Default: 0.0004
    public int m_MaxCrimePenalty;                      // Default: 30

    // Mail
    public float m_MailMultiplier;                    // Default: 2.0
    public int m_NegligibleMail;                      // Default: 25

    // Telecom
    public float m_TelecomBaseline;                   // Default: 0.3
    public float m_TelecomBonusMultiplier;            // Default: 10.0
    public float m_TelecomPenaltyMultiplier;          // Default: 20.0

    // Welfare
    public float m_WelfareMultiplier;                 // Default: 2.0

    // Health problems & Death
    public int m_HealthProblemHealthPenalty;           // Default: 20
    public int m_DeathWellbeingPenalty;                // Default: 20
    public int m_DeathHealthPenalty;                   // Default: 10

    // Consumption
    public float m_ConsumptionMultiplier;             // Default: 1.0

    // Thresholds
    public int m_LowWellbeing;                        // Default: 40
    public int m_LowHealth;                           // Default: 40

    // Tax multipliers (per education level)
    public float m_TaxUneducatedMultiplier;           // Default: -0.25
    public float m_TaxPoorlyEducatedMultiplier;       // Default: -0.5
    public float m_TaxEducatedMultiplier;             // Default: -1.0
    public float m_TaxWellEducatedMultiplier;         // Default: -1.5
    public float m_TaxHighlyEducatedMultiplier;       // Default: -2.0

    // Penalties
    public int m_PenaltyEffect;                       // Default: -30 (traffic teleport penalty)
    public int m_HomelessHealthEffect;                // Default: -20
    public int m_HomelessWellbeingEffect;             // Default: -20

    // Unemployment
    public float m_UnemployedWellbeingPenaltyAccumulatePerDay;  // Default: 0
    public int m_MaxAccumulatedUnemployedWellbeingPenalty;       // Default: 20
}
