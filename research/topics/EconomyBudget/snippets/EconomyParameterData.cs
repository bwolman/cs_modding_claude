// Decompiled from Game.dll -> Game.Prefabs.EconomyParameterData
// Decompiled with ilspycmd on 2026-02-16

using Colossal.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct EconomyParameterData : IComponentData, IQueryTypeParameter
{
    public float m_ExtractorCompanyExportMultiplier;
    public int m_Wage0;
    public int m_Wage1;
    public int m_Wage2;
    public int m_Wage3;
    public int m_Wage4;
    public float m_CommuterWageMultiplier;
    public int m_CompanyBankruptcyLimit;
    public int m_ResidentialMinimumEarnings;
    public int m_UnemploymentBenefit;
    public int m_Pension;
    public int m_FamilyAllowance;
    public float2 m_ResourceConsumptionMultiplier;
    public float m_ResourceConsumptionPerCitizen;
    public float m_TouristConsumptionMultiplier;
    public int m_TouristInitialWealthRange;
    public int m_TouristInitialWealthOffset;
    public float m_WorkDayStart;
    public float m_WorkDayEnd;
    public float m_IndustrialEfficiency;
    public float m_CommercialEfficiency;
    public float m_ExtractorProductionEfficiency;
    public int m_OfficeResourceConsumedPerIndustrialUnit;
    public float m_TrafficReduction;
    public float m_MaxCitySpecializationBonus;
    public int m_ResourceProductionCoefficient;
    public float3 m_LandValueModifier;
    public float3 m_RentPriceBuildingZoneTypeBase;
    public float m_MixedBuildingCompanyRentPercentage;
    public float m_ResidentialUpkeepLevelExponent;
    public float m_CommercialUpkeepLevelExponent;
    public float m_IndustrialUpkeepLevelExponent;
    public int m_PerOfficeResourceNeededForIndustrial;
    public float m_UnemploymentAllowanceMaxDays;
    public int m_ShopPossibilityIncreaseDivider;
    public float m_CityServiceWageAdjustment;
    public int m_PlayerStartMoney;
    public float3 m_BuildRefundPercentage;
    public float3 m_BuildRefundTimeRange;
    public float m_RelocationCostMultiplier;
    public float3 m_RoadRefundPercentage;
    public float3 m_RoadRefundTimeRange;
    public int3 m_TreeCostMultipliers;
    public AnimationCurve1 m_MapTileUpkeepCostMultiplier;
    public float2 m_LoanMinMaxInterestRate;
    public int2 m_ProfitabilityRange;

    public int GetWage(int jobLevel, bool cityServiceJob = false)
    {
        float num = cityServiceJob ? m_CityServiceWageAdjustment : 1f;
        return jobLevel switch
        {
            0 => (int)((float)m_Wage0 * num),
            1 => (int)((float)m_Wage1 * num),
            2 => (int)((float)m_Wage2 * num),
            3 => (int)((float)m_Wage3 * num),
            4 => (int)((float)m_Wage4 * num),
            _ => 0,
        };
    }
}
