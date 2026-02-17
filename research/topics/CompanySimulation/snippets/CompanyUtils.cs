using Game.Areas;
using Game.Buildings;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation;

public static class CompanyUtils
{
	public static int GetCompanyMoveAwayChance(Entity company, Entity companyPrefab, Entity property, ref ComponentLookup<ServiceAvailable> serviceAvailables, ref ComponentLookup<OfficeProperty> officeProperties, ref ComponentLookup<IndustrialProcessData> industrialProcessDatas, ref ComponentLookup<WorkProvider> workProviders, NativeArray<int> taxRates)
	{
		int num = 0;
		bool num2 = serviceAvailables.HasComponent(company);
		bool flag = officeProperties.HasComponent(property);
		IndustrialProcessData industrialProcessData = industrialProcessDatas[companyPrefab];
		int num3 = (num2 ? TaxSystem.GetCommercialTaxRate(industrialProcessData.m_Output.m_Resource, taxRates) : ((!flag) ? TaxSystem.GetIndustrialTaxRate(industrialProcessData.m_Output.m_Resource, taxRates) : TaxSystem.GetOfficeTaxRate(industrialProcessData.m_Output.m_Resource, taxRates)));
		num += (num3 - 10) * 5 / 2;
		WorkProvider workProvider = workProviders[company];
		if (workProvider.m_UneducatedNotificationEntity != Entity.Null)
		{
			num += 5;
		}
		if (workProvider.m_EducatedNotificationEntity != Entity.Null)
		{
			num += 20;
		}
		return num;
	}

	public static int GetCommercialMaxFittingWorkers(BuildingData building, BuildingPropertyData properties, int level, ServiceCompanyData serviceData)
	{
		return Mathf.CeilToInt(serviceData.m_MaxWorkersPerCell * (float)building.m_LotSize.x * (float)building.m_LotSize.y * (1f + 0.5f * (float)level) * properties.m_SpaceMultiplier);
	}

	public static int GetIndustrialAndOfficeFittingWorkers(BuildingData building, BuildingPropertyData properties, int level, IndustrialProcessData processData)
	{
		return Mathf.CeilToInt(processData.m_MaxWorkersPerCell * (float)building.m_LotSize.x * (float)building.m_LotSize.y * (1f + 0.5f * (float)level) * properties.m_SpaceMultiplier);
	}

	public static int GetExtractorFittingWorkers(float area, float spaceMultiplier, IndustrialProcessData processData)
	{
		return Mathf.CeilToInt(processData.m_MaxWorkersPerCell * area * spaceMultiplier / 2f);
	}

	public static int GetCompanyProfitability(int profit, EconomyParameterData economyParameterData)
	{
		int x = economyParameterData.m_ProfitabilityRange.x;
		int y = economyParameterData.m_ProfitabilityRange.y;
		if (x >= 0 && y <= 0)
		{
			return 127;
		}
		if (profit < 0)
		{
			int num = -x;
			if (num <= 0)
			{
				return 0;
			}
			float t = math.saturate((float)(profit - x) / (float)num);
			return (int)math.round(math.lerp(0f, 127f, t));
		}
		int num2 = y;
		if (num2 <= 0)
		{
			return 255;
		}
		float t2 = math.saturate((float)profit / (float)num2);
		return (int)math.round(math.lerp(127f, 255f, t2));
	}

	public static int GetCompanyMaxFittingWorkers(Entity companyEntity, Entity buildingEntity, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<ServiceCompanyData> serviceCompanyDatas, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<BuildingPropertyData> buildingPropertyDatas, ref ComponentLookup<SpawnableBuildingData> spawnableBuildingDatas, ref ComponentLookup<IndustrialProcessData> industrialProcessDatas, ref ComponentLookup<ExtractorCompanyData> extractorCompanyDatas, ref ComponentLookup<Attached> attacheds, ref BufferLookup<Game.Areas.SubArea> subAreaBufs, ref BufferLookup<InstalledUpgrade> installedUpgrades, ref ComponentLookup<Game.Areas.Lot> lots, ref ComponentLookup<Geometry> geometries)
	{
		Entity entity = prefabRefs[companyEntity];
		Entity entity2 = prefabRefs[buildingEntity];
		int level = 1;
		if (spawnableBuildingDatas.HasComponent(entity2))
		{
			level = spawnableBuildingDatas[entity2].m_Level;
		}
		if (serviceCompanyDatas.HasComponent(entity))
		{
			return GetCommercialMaxFittingWorkers(buildingDatas[entity2], buildingPropertyDatas[entity2], level, serviceCompanyDatas[entity]);
		}
		if (extractorCompanyDatas.HasComponent(entity))
		{
			float area = 0f;
			if (attacheds.HasComponent(buildingEntity))
			{
				area = ExtractorAISystem.GetArea(attacheds[buildingEntity].m_Parent, ref subAreaBufs, ref installedUpgrades, ref lots, ref geometries);
			}
			return math.max(1, GetExtractorFittingWorkers(area, 1f, industrialProcessDatas[entity]));
		}
		if (industrialProcessDatas.HasComponent(entity))
		{
			return GetIndustrialAndOfficeFittingWorkers(buildingDatas[entity2], buildingPropertyDatas[entity2], level, industrialProcessDatas[entity]);
		}
		return 0;
	}
}
