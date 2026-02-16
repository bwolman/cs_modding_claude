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
using Game.Objects;
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
public class IndustrialDemandSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct UpdateIndustrialDemandJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ZoneData> m_UnlockedZoneDatas;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_IndustrialPropertyChunks;
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_OfficePropertyChunks;
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_StorageCompanyChunks;
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CityServiceChunks;

		public EconomyParameterData m_EconomyParameters;
		public DemandParameterData m_DemandParameters;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;
		[ReadOnly]
		public NativeArray<int> m_EmployableByEducation;
		[ReadOnly]
		public NativeArray<int> m_TaxRates;
		[ReadOnly]
		public Workplaces m_FreeWorkplaces;

		public Entity m_City;

		public NativeValue<int> m_IndustrialCompanyDemand;
		public NativeValue<int> m_IndustrialBuildingDemand;
		public NativeValue<int> m_StorageCompanyDemand;
		public NativeValue<int> m_StorageBuildingDemand;
		public NativeValue<int> m_OfficeCompanyDemand;
		public NativeValue<int> m_OfficeBuildingDemand;

		public NativeArray<int> m_IndustrialDemandFactors;
		public NativeArray<int> m_OfficeDemandFactors;
		public NativeArray<int> m_IndustrialCompanyDemands;
		public NativeArray<int> m_IndustrialBuildingDemands;
		public NativeArray<int> m_StorageBuildingDemands;
		public NativeArray<int> m_StorageCompanyDemands;

		[ReadOnly]
		public NativeArray<int> m_Productions;
		[ReadOnly]
		public NativeArray<int> m_CompanyResourceDemands;
		[ReadOnly]
		public NativeArray<int> m_HouseholdResourceDemands;

		public NativeArray<int> m_FreeProperties;
		[ReadOnly]
		public NativeArray<int> m_Propertyless;
		public NativeArray<int> m_FreeStorages;
		public NativeArray<int> m_Storages;
		public NativeArray<int> m_StorageCapacities;
		public NativeArray<int> m_ResourceDemands;

		public float m_IndustrialOfficeTaxEffectDemandOffset;
		public bool m_UnlimitedDemand;

		public void Execute()
		{
			bool flag = false;
			for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
			{
				if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Industrial)
				{
					flag = true;
					break;
				}
			}
			// Computes resource demands from household needs + company needs + city service upkeep
			// Iterates all industrial/office resources, computing per-resource demand
			// For office resources: demand = (householdDemand + companyDemand) * 2
			// For industrial resources: uses companyResourceDemands or defaults to 100
			// Workforce effect: maps educated/uneducated workforce availability
			// Tax effect: m_TaxEffect.z * -0.05f * (taxRate - 10)
			// Storage demand: when storage capacity < resource demand, triggers warehouse demand
			// Final building demand clamped to [0, 100]
		}

		private float MapAndClaimWorkforceEffect(float value, float min, float max)
		{
			if (value < 0f)
			{
				float valueToClamp = math.unlerp(-2000f, 0f, value);
				valueToClamp = math.clamp(valueToClamp, 0f, 1f);
				return math.lerp(min, 0f, valueToClamp);
			}
			float valueToClamp2 = math.unlerp(0f, 20f, value);
			valueToClamp2 = math.clamp(valueToClamp2, 0f, 1f);
			return math.lerp(0f, max, valueToClamp2);
		}
	}

	private static readonly int kStorageProductionDemand = 2000;
	private static readonly int kStorageCompanyEstimateLimit = 864000;

	private ResourceSystem m_ResourceSystem;
	private CitySystem m_CitySystem;
	private ClimateSystem m_ClimateSystem;
	private TaxSystem m_TaxSystem;
	private CountHouseholdDataSystem m_CountHouseholdDataSystem;
	private CountWorkplacesSystem m_CountWorkplacesSystem;
	private CountCompanyDataSystem m_CountCompanyDataSystem;

	private NativeValue<int> m_IndustrialCompanyDemand;
	private NativeValue<int> m_IndustrialBuildingDemand;
	private NativeValue<int> m_StorageCompanyDemand;
	private NativeValue<int> m_StorageBuildingDemand;
	private NativeValue<int> m_OfficeCompanyDemand;
	private NativeValue<int> m_OfficeBuildingDemand;

	private NativeArray<int> m_IndustrialDemandFactors;
	private NativeArray<int> m_OfficeDemandFactors;

	public int industrialCompanyDemand => m_LastIndustrialCompanyDemand;
	public int industrialBuildingDemand => m_LastIndustrialBuildingDemand;
	public int storageCompanyDemand => m_LastStorageCompanyDemand;
	public int storageBuildingDemand => m_LastStorageBuildingDemand;
	public int officeCompanyDemand => m_LastOfficeCompanyDemand;
	public int officeBuildingDemand => m_LastOfficeBuildingDemand;

	public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
	public override int GetUpdateOffset(SystemUpdatePhase phase) => 7;
}
