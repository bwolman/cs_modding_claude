using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs.Modes;

public struct ModeSettingData : IComponentData, IQueryTypeParameter
{
	public bool m_Enable;

	public float2 m_ResidentialDemandWeightsSelector;

	public float m_CommercialTaxEffectDemandOffset;

	public float m_IndustrialOfficeTaxEffectDemandOffset;

	public float m_ResourceDemandPerCitizenMultiplier;

	public float3 m_TaxPaidMultiplier;

	public bool m_SupportPoorCitizens;

	public int m_MinimumWealth;

	public bool m_EnableGovernmentSubsidies;

	public int2 m_MoneyCoverThreshold;

	public int m_MaxMoneyCoverPercentage;

	public bool m_EnableAdjustNaturalResources;

	public float m_InitialNaturalResourceBoostMultiplier;

	public int m_PercentOreRefillAmountPerDay;

	public int m_PercentOilRefillAmountPerDay;
}
