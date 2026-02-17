// Decompiled from Game.dll — Game.Prefabs.CitizenParametersData
// ECS singleton component controlling citizen lifecycle rates.

using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct CitizenParametersData : IComponentData, IQueryTypeParameter
{
    public float m_DivorceRate;
    public float m_LookForPartnerRate;
    public float2 m_LookForPartnerTypeRate;
    public float m_BaseBirthRate;
    public float m_AdultFemaleBirthRateBonus;
    public float m_StudentBirthRateAdjust;
    public float m_SwitchJobRate;
    public float m_LookForNewJobEmployableRate;
}

// Default values from CitizenParametersPrefab:
// m_DivorceRate = 0.16f (16%)
// m_LookForPartnerRate = 0.08f (8%)
// m_LookForPartnerTypeRate = (0.04f, 0.1f) — x=Same Gender, y=Any Gender
// m_BaseBirthRate = 0.02f (2%)
// m_AdultFemaleBirthRateBonus = 0.08f (8%)
// m_StudentBirthRateAdjust = 0.5f (50% reduction)
// m_SwitchJobRate = 0.032f (3.2%)
// m_LookForNewJobEmployableRate = 2.0f
