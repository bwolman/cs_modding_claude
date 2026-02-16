using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct DemandParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_ForestryPrefab;
	public Entity m_OfficePrefab;
	public int m_MinimumHappiness;
	public float m_HappinessEffect;
	public float3 m_TaxEffect;
	public float m_StudentEffect;
	public float m_AvailableWorkplaceEffect;
	public float m_HomelessEffect;
	public int m_NeutralHappiness;
	public float m_NeutralUnemployment;
	public float m_NeutralAvailableWorkplacePercentage;
	public int m_NeutralHomelessness;
	public int3 m_FreeResidentialRequirement;
	public float m_FreeCommercialProportion;
	public float m_FreeIndustrialProportion;
	public float m_CommercialStorageMinimum;
	public float m_CommercialStorageEffect;
	public float m_CommercialBaseDemand;
	public float m_IndustrialStorageMinimum;
	public float m_IndustrialStorageEffect;
	public float m_IndustrialBaseDemand;
	public float m_ExtractorBaseDemand;
	public float m_StorageDemandMultiplier;
	public int m_CommuterWorkerRatioLimit;
	public int m_CommuterSlowSpawnFactor;
	public float4 m_CommuterOCSpawnParameters;
	public float4 m_TouristOCSpawnParameters;
	public float4 m_CitizenOCSpawnParameters;
	public float m_TeenSpawnPercentage;
	public int3 m_FrameIntervalForSpawning;
	public float m_HouseholdSpawnSpeedFactor;
	public float m_HotelRoomPercentRequirement;
	public float4 m_NewCitizenEducationParameters;
}
