using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Reflection;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CommercialDemandSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct UpdateCommercialDemandJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ZoneData> m_UnlockedZoneDatas;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CommercialPropertyChunks;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyOnMarket> m_PropertyOnMarketType;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> m_CommercialCompanies;

		[ReadOnly]
		public ComponentLookup<Tourism> m_Tourisms;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public DemandParameterData m_DemandParameters;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public NativeValue<int> m_CompanyDemand;
		public NativeValue<int> m_BuildingDemand;
		public NativeArray<int> m_DemandFactors;
		public NativeArray<int> m_FreeProperties;
		public NativeArray<int> m_ResourceDemands;
		public NativeArray<int> m_BuildingDemands;

		[ReadOnly]
		public NativeArray<int> m_ProduceCapacity;
		[ReadOnly]
		public NativeArray<int> m_CurrentAvailables;
		[ReadOnly]
		public NativeArray<int> m_Propertyless;

		public float m_CommercialTaxEffectDemandOffset;
		public bool m_UnlimitedDemand;

		public void Execute()
		{
			bool flag = false;
			for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
			{
				if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Commercial)
				{
					flag = true;
					break;
				}
			}
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
				m_FreeProperties[resourceIndex] = 0;
				m_BuildingDemands[resourceIndex] = 0;
				m_ResourceDemands[resourceIndex] = 0;
			}
			for (int j = 0; j < m_DemandFactors.Length; j++)
			{
				m_DemandFactors[j] = 0;
			}
			// ... counts free commercial properties by iterating property chunks ...
			m_CompanyDemand.value = 0;
			m_BuildingDemand.value = 0;
			int population = m_Populations[m_City].m_Population;
			iterator = ResourceIterator.GetIterator();
			int num = 0;
			while (iterator.Next())
			{
				int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);
				if (!EconomyUtils.IsCommercialResource(iterator.resource) || !m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
				{
					continue;
				}
				float num2 = -0.05f * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f) * m_DemandParameters.m_TaxEffect.y;
				num2 += m_CommercialTaxEffectDemandOffset;
				if (iterator.resource != Resource.Lodging)
				{
					int num3 = ((population <= 1000) ? 2500 : (2500 * (int)Mathf.Log10(0.01f * (float)population)));
					m_ResourceDemands[resourceIndex2] = math.clamp(100 - (m_CurrentAvailables[resourceIndex2] - num3) / 25, 0, 100);
				}
				else if (math.max((int)((float)m_Tourisms[m_City].m_CurrentTourists * m_DemandParameters.m_HotelRoomPercentRequirement) - m_Tourisms[m_City].m_Lodging.y, 0) > 0)
				{
					m_ResourceDemands[resourceIndex2] = 100;
				}
				m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt((1f + num2) * (float)m_ResourceDemands[resourceIndex2]);
				int num4 = Mathf.RoundToInt(100f * num2);
				m_DemandFactors[11] += num4;
				if (m_ResourceDemands[resourceIndex2] > 0)
				{
					m_CompanyDemand.value += m_ResourceDemands[resourceIndex2];
					m_BuildingDemands[resourceIndex2] = ((m_FreeProperties[resourceIndex2] - m_Propertyless[resourceIndex2] <= 0) ? m_ResourceDemands[resourceIndex2] : 0);
					if (m_BuildingDemands[resourceIndex2] > 0)
					{
						m_BuildingDemand.value += m_BuildingDemands[resourceIndex2];
					}
					int num5 = ((m_BuildingDemands[resourceIndex2] > 0) ? m_ResourceDemands[resourceIndex2] : 0);
					int num6 = m_ResourceDemands[resourceIndex2];
					int num7 = num6 + num4;
					if (iterator.resource == Resource.Lodging)
					{
						m_DemandFactors[9] += num6;
					}
					else if (iterator.resource == Resource.Petrochemicals)
					{
						m_DemandFactors[16] += num6;
					}
					else
					{
						m_DemandFactors[4] += num6;
					}
					m_DemandFactors[13] += math.min(0, num5 - num7);
					num++;
				}
			}
			m_CompanyDemand.value = ((num != 0) ? math.clamp(m_CompanyDemand.value / num, 0, 100) : 0);
			m_BuildingDemand.value = ((num != 0 && flag) ? math.clamp(m_BuildingDemand.value / num, 0, 100) : 0);
			if (m_UnlimitedDemand)
			{
				m_BuildingDemand.value = 100;
				m_CompanyDemand.value = 100;
			}
		}
	}

	private ResourceSystem m_ResourceSystem;
	private TaxSystem m_TaxSystem;
	private CountCompanyDataSystem m_CountCompanyDataSystem;
	private CountHouseholdDataSystem m_CountHouseholdDataSystem;
	private CitySystem m_CitySystem;

	private NativeValue<int> m_CompanyDemand;
	private NativeValue<int> m_BuildingDemand;
	private NativeArray<int> m_DemandFactors;
	private NativeArray<int> m_ResourceDemands;
	private NativeArray<int> m_BuildingDemands;
	private NativeArray<int> m_Consumption;
	private NativeArray<int> m_FreeProperties;

	public int companyDemand => m_LastCompanyDemand;
	public int buildingDemand => m_LastBuildingDemand;

	public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
	public override int GetUpdateOffset(SystemUpdatePhase phase) => 4;
}
