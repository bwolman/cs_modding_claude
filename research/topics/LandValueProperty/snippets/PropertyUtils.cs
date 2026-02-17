#define UNITY_ASSERTIONS
using Game.Agents;
using Game.Areas;
using Game.Citizens;
using Game.City;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Buildings;

public static class PropertyUtils
{
	[BurstCompile]
	public struct ExtractorFindCompanyJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_Entities;

		[ReadOnly]
		public NativeList<Entity> m_ExtractorCompanyEntities;

		[ReadOnly]
		public NativeList<Entity> m_CompanyPrefabs;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_Properties;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_Processes;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Attached> m_Attached;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_Lots;

		[ReadOnly]
		public ComponentLookup<Geometry> m_Geometries;

		[ReadOnly]
		public ComponentLookup<Extractor> m_ExtractorAreas;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> m_ExtractorDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public ExtractorParameterData m_ExtractorParameters;

		public EconomyParameterData m_EconomyParameters;

		public NativeQueue<RentAction> m_RentActionQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public float m_AverageTemperature;

		[ReadOnly]
		public NativeArray<int> m_Productions;

		[ReadOnly]
		public NativeArray<int> m_Consumptions;

		private float Evaluate(Entity entity, Resource resource)
		{
			IndustrialProcessData industrialProcessData = default(IndustrialProcessData);
			ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[resource]];
			bool flag = false;
			for (int i = 0; i < m_CompanyPrefabs.Length; i++)
			{
				if (m_Processes.HasComponent(m_CompanyPrefabs[i]) && m_WorkplaceDatas.HasComponent(m_CompanyPrefabs[i]))
				{
					industrialProcessData = m_Processes[m_CompanyPrefabs[i]];
					if (industrialProcessData.m_Output.m_Resource == resource && industrialProcessData.m_Input1.m_Resource == Resource.NoResource)
					{
						flag = true;
						break;
					}
				}
			}
			if (flag && m_Attached.HasComponent(entity))
			{
				Entity parent = m_Attached[entity].m_Parent;
				ExtractorCompanySystem.GetBestConcentration(resource, parent, ref m_SubAreas, ref m_InstalledUpgrades, ref m_ExtractorAreas, ref m_Geometries, ref m_Prefabs, ref m_ExtractorDatas, m_ExtractorParameters, m_ResourcePrefabs, ref m_ResourceDatas, out var concentration, out var _);
				if (resourceData.m_RequireTemperature && m_AverageTemperature < resourceData.m_RequiredTemperature)
				{
					concentration = 0f;
				}
				if (concentration == 0f)
				{
					return float.NegativeInfinity;
				}
				return concentration;
			}
			return float.NegativeInfinity;
		}

		public void Execute()
		{
			if (m_Entities.Length == 0)
			{
				return;
			}
			for (int i = 0; i < m_Entities.Length; i++)
			{
				Entity entity = m_Entities[i];
				if (!m_Prefabs.HasComponent(entity))
				{
					continue;
				}
				Entity prefab = m_Prefabs[entity].m_Prefab;
				Resource resource = Resource.NoResource;
				if (m_Properties.HasComponent(prefab))
				{
					Resource resource2 = m_Properties[prefab].m_AllowedManufactured;
					if (m_Attached.TryGetComponent(entity, out var componentData) && m_Prefabs.TryGetComponent(componentData.m_Parent, out var componentData2) && m_Properties.TryGetComponent(componentData2.m_Prefab, out var componentData3))
					{
						resource2 &= componentData3.m_AllowedManufactured;
					}
					ResourceIterator resourceIterator = default(ResourceIterator);
					float num = float.NegativeInfinity;
					while (resourceIterator.Next())
					{
						if ((resource2 & resourceIterator.resource) != Resource.NoResource)
						{
							float num2 = Evaluate(entity, resourceIterator.resource);
							if (num2 > num)
							{
								num = num2;
								resource = resourceIterator.resource;
							}
						}
					}
				}
				for (int j = 0; j < m_ExtractorCompanyEntities.Length; j++)
				{
					Entity entity2 = m_ExtractorCompanyEntities[j];
					if ((!m_PropertyRenters.HasComponent(entity2) || !(m_PropertyRenters[entity2].m_Property != Entity.Null)) && m_Prefabs.HasComponent(entity2))
					{
						Entity prefab2 = m_Prefabs[entity2].m_Prefab;
						if (m_Processes.HasComponent(prefab2) && m_Processes[prefab2].m_Output.m_Resource == resource)
						{
							m_RentActionQueue.Enqueue(new RentAction
							{
								m_Property = entity,
								m_Renter = entity2
							});
							m_CommandBuffer.SetComponentEnabled<PropertySeeker>(entity2, value: false);
							return;
						}
					}
				}
			}
		}
	}

	[BurstCompile]
	public struct CompanyFindPropertyJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		public ComponentTypeHandle<PropertySeeker> m_PropertySeekerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.StorageCompany> m_StorageCompanyType;

		[ReadOnly]
		public NativeList<Entity> m_FreePropertyEntities;

		[ReadOnly]
		public NativeList<PrefabRef> m_PropertyPrefabs;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_Availabilities;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValues;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanies;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> m_CommercialCompanies;

		[ReadOnly]
		public ComponentLookup<Signature> m_Signatures;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanyDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public EconomyParameterData m_EconomyParameters;

		public ZonePreferenceData m_ZonePreferences;

		public bool m_Commercial;

		public NativeQueue<RentAction>.ParallelWriter m_RentActionQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		private void Evaluate(int index, Entity company, ref ServiceCompanyData service, ref IndustrialProcessData process, Entity property, ref PropertySeeker propertySeeker, bool commercial, bool storage)
		{
			float num = ((!commercial) ? IndustrialFindPropertySystem.Evaluate(company, property, ref process, ref propertySeeker, m_Buildings, m_PropertiesOnMarket, m_PrefabFromEntity, m_BuildingDatas, m_SpawnableDatas, m_WorkplaceDatas, m_LandValues, m_Availabilities, m_EconomyParameters, m_ResourcePrefabs, m_ResourceDatas, m_BuildingPropertyDatas, storage) : CommercialFindPropertySystem.Evaluate(company, property, ref service, ref process, ref propertySeeker, m_Buildings, m_PrefabFromEntity, m_BuildingDatas, m_Availabilities, m_LandValues, m_ResourcePrefabs, m_ResourceDatas, m_BuildingPropertyDatas, m_SpawnableDatas, m_Renters, m_CommercialCompanies, ref m_ZonePreferences));
			if (m_Signatures.HasComponent(property))
			{
				num += 5000f;
			}
			if (propertySeeker.m_BestProperty == Entity.Null || num > propertySeeker.m_BestPropertyScore)
			{
				propertySeeker.m_BestPropertyScore = num;
				propertySeeker.m_BestProperty = property;
			}
		}

		private void SelectProperty(int jobIndex, Entity company, ref PropertySeeker propertySeeker, bool storage)
		{
			Entity bestProperty = propertySeeker.m_BestProperty;
			if (m_PropertiesOnMarket.HasComponent(bestProperty) && (!m_PropertyRenters.HasComponent(company) || !m_PropertyRenters[company].m_Property.Equals(bestProperty)))
			{
				m_RentActionQueue.Enqueue(new RentAction
				{
					m_Property = bestProperty,
					m_Renter = company,
					m_Flags = (storage ? RentActionFlags.Storage : ((RentActionFlags)0))
				});
				m_CommandBuffer.SetComponentEnabled<PropertySeeker>(jobIndex, company, value: false);
			}
			else if (m_PropertyRenters.HasComponent(company))
			{
				m_CommandBuffer.SetComponentEnabled<PropertySeeker>(jobIndex, company, value: false);
			}
			else
			{
				propertySeeker.m_BestProperty = Entity.Null;
				propertySeeker.m_BestPropertyScore = 0f;
			}
		}

		private bool PropertyAllowsResource(int index, Resource resource, Resource input, bool storage)
		{
			Entity prefab = m_PropertyPrefabs[index].m_Prefab;
			BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
			Resource resource2 = ((!storage) ? (m_Commercial ? buildingPropertyData.m_AllowedSold : buildingPropertyData.m_AllowedManufactured) : buildingPropertyData.m_AllowedStored);
			if ((resource & resource2) != Resource.NoResource)
			{
				return (input & buildingPropertyData.m_AllowedInput) == input;
			}
			return false;
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<PropertySeeker> nativeArray3 = chunk.GetNativeArray(ref m_PropertySeekerType);
			bool storage = chunk.Has(ref m_StorageCompanyType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (!m_IndustrialProcessDatas.HasComponent(prefab))
				{
					break;
				}
				IndustrialProcessData process = m_IndustrialProcessDatas[prefab];
				PropertySeeker propertySeeker = nativeArray3[i];
				Resource resource = process.m_Output.m_Resource;
				Resource input = process.m_Input1.m_Resource | process.m_Input2.m_Resource;
				ServiceCompanyData service = default(ServiceCompanyData);
				if (m_Commercial)
				{
					service = m_ServiceCompanies[prefab];
				}
				if (m_PropertyRenters.HasComponent(entity) && m_PropertyRenters[entity].m_Property != Entity.Null)
				{
					continue;
				}
				for (int j = 0; j < m_FreePropertyEntities.Length; j++)
				{
					Entity entity2 = m_FreePropertyEntities[j];
					bool flag = true;
					if (m_Renters.HasBuffer(entity2))
					{
						DynamicBuffer<Renter> dynamicBuffer = m_Renters[entity2];
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							if (m_CompanyDatas.HasComponent(dynamicBuffer[k].m_Renter))
							{
								flag = false;
								break;
							}
						}
					}
					if (flag && PropertyAllowsResource(j, resource, input, storage))
					{
						Evaluate(i, entity, ref service, ref process, m_FreePropertyEntities[j], ref propertySeeker, m_Commercial, storage);
					}
				}
				SelectProperty(unfilteredChunkIndex, entity, ref propertySeeker, storage);
				nativeArray3[i] = propertySeeker;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public static readonly float kHomelessApartmentSize = 0.01f;

	public static int GetRentPricePerRenter(BuildingPropertyData buildingPropertyData, int buildingLevel, int lotSize, float landValueBase, Game.Zones.AreaType areaType, ref EconomyParameterData economyParameterData, bool ignoreLandValue = false)
	{
		float num = economyParameterData.m_RentPriceBuildingZoneTypeBase.x;
		float num2 = economyParameterData.m_LandValueModifier.x;
		switch (areaType)
		{
		case Game.Zones.AreaType.Commercial:
			num = economyParameterData.m_RentPriceBuildingZoneTypeBase.y;
			num2 = economyParameterData.m_LandValueModifier.y;
			break;
		case Game.Zones.AreaType.Industrial:
			num = economyParameterData.m_RentPriceBuildingZoneTypeBase.z;
			num2 = economyParameterData.m_LandValueModifier.z;
			break;
		}
		float num3 = ((!ignoreLandValue) ? ((landValueBase * num2 + num * (float)buildingLevel) * (float)lotSize * buildingPropertyData.m_SpaceMultiplier) : (num * (float)buildingLevel * (float)lotSize * buildingPropertyData.m_SpaceMultiplier));
		int num4 = ((!IsMixedBuilding(buildingPropertyData)) ? buildingPropertyData.CountProperties() : Mathf.RoundToInt((float)buildingPropertyData.m_ResidentialProperties / (1f - economyParameterData.m_MixedBuildingCompanyRentPercentage)));
		return Mathf.RoundToInt(num3 / (float)num4);
	}

	public static string[] GetRentPriceDebugInfo(BuildingPropertyData buildingPropertyData, int buildingLevel, int lotSize, float landValueBase, Game.Zones.AreaType areaType, ref EconomyParameterData economyParameterData, bool ignoreLandValue = false)
	{
		float num = economyParameterData.m_RentPriceBuildingZoneTypeBase.x;
		float num2 = economyParameterData.m_LandValueModifier.x;
		switch (areaType)
		{
		case Game.Zones.AreaType.Commercial:
			num = economyParameterData.m_RentPriceBuildingZoneTypeBase.y;
			num2 = economyParameterData.m_LandValueModifier.y;
			break;
		case Game.Zones.AreaType.Industrial:
			num = economyParameterData.m_RentPriceBuildingZoneTypeBase.z;
			num2 = economyParameterData.m_LandValueModifier.z;
			break;
		}
		float num3 = ((!ignoreLandValue) ? ((landValueBase * num2 + num * (float)buildingLevel) * (float)lotSize * buildingPropertyData.m_SpaceMultiplier) : (num * (float)buildingLevel * (float)lotSize * buildingPropertyData.m_SpaceMultiplier));
		int num4 = ((!IsMixedBuilding(buildingPropertyData)) ? buildingPropertyData.CountProperties() : Mathf.RoundToInt((float)buildingPropertyData.m_ResidentialProperties / (1f - economyParameterData.m_MixedBuildingCompanyRentPercentage)));
		float num5 = landValueBase * num2;
		return new string[7]
		{
			ignoreLandValue ? "A. land value: ignored = 0\n" : $"A. land value: {landValueBase:F3} * {num2:F3} = {num5:F3}",
			$"B. zone type * building level: {num:F3} * {buildingLevel} = {num * (float)buildingLevel:F3}",
			$"C. lot size: {lotSize}",
			$"D. space multiplier: {buildingPropertyData.m_SpaceMultiplier:F3}",
			$"Rent asked: (A + B) * C * D = {num3:F3}",
			$"Rent asked per renter: {num3 / (float)num4:F3}",
			$"Renters: {num4}"
		};
	}

	public static float GetPropertyScore(Entity property, Entity household, DynamicBuffer<HouseholdCitizen> citizenBuffer, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<BuildingPropertyData> buildingProperties, ref ComponentLookup<Building> buildings, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<Household> households, ref ComponentLookup<Citizen> citizens, ref ComponentLookup<Game.Citizens.Student> students, ref ComponentLookup<Worker> workers, ref ComponentLookup<SpawnableBuildingData> spawnableDatas, ref ComponentLookup<CrimeProducer> crimes, ref BufferLookup<Game.Net.ServiceCoverage> serviceCoverages, ref ComponentLookup<Locked> locked, ref ComponentLookup<ElectricityConsumer> electricityConsumers, ref ComponentLookup<WaterConsumer> waterConsumers, ref ComponentLookup<GarbageProducer> garbageProducers, ref ComponentLookup<MailProducer> mailProducers, ref ComponentLookup<Game.Objects.Transform> transforms, ref ComponentLookup<Abandoned> abandoneds, ref ComponentLookup<Park> parks, ref BufferLookup<ResourceAvailability> availabilities, NativeArray<int> taxRates, NativeArray<GroundPollution> pollutionMap, NativeArray<AirPollution> airPollutionMap, NativeArray<NoisePollution> noiseMap, CellMapData<TelecomCoverage> telecomCoverages, DynamicBuffer<CityModifier> cityModifiers, Entity healthcareService, Entity entertainmentService, Entity educationService, Entity telecomService, Entity garbageService, Entity policeService, CitizenHappinessParameterData citizenHappinessParameterData, GarbageParameterData garbageParameterData)
	{
		if (!buildings.HasComponent(property))
		{
			return float.NegativeInfinity;
		}
		bool flag = (households[household].m_Flags & HouseholdFlags.MovedIn) != 0;
		bool flag2 = BuildingUtils.IsHomelessShelterBuilding(property, ref parks, ref abandoneds);
		if (flag2 && !flag)
		{
			return float.NegativeInfinity;
		}
		Building buildingData = buildings[property];
		Entity prefab = prefabRefs[property].m_Prefab;
		HouseholdFindPropertySystem.GenericApartmentQuality genericApartmentQuality = GetGenericApartmentQuality(property, prefab, ref buildingData, ref buildingProperties, ref buildingDatas, ref spawnableDatas, ref crimes, ref serviceCoverages, ref locked, ref electricityConsumers, ref waterConsumers, ref garbageProducers, ref mailProducers, ref transforms, ref abandoneds, pollutionMap, airPollutionMap, noiseMap, telecomCoverages, cityModifiers, healthcareService, entertainmentService, educationService, telecomService, garbageService, policeService, citizenHappinessParameterData, garbageParameterData);
		int length = citizenBuffer.Length;
		float num = 0f;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		for (int i = 0; i < citizenBuffer.Length; i++)
		{
			Entity citizen = citizenBuffer[i].m_Citizen;
			Citizen citizen2 = citizens[citizen];
			num4 += citizen2.Happiness;
			if (citizen2.GetAge() == CitizenAge.Child)
			{
				num5++;
			}
			else
			{
				num3++;
				num6 += CitizenHappinessSystem.GetTaxBonuses(citizen2.GetEducationLevel(), taxRates, cityModifiers, in citizenHappinessParameterData).y;
			}
			if (students.HasComponent(citizen))
			{
				num2++;
				Game.Citizens.Student student = students[citizen];
				if (student.m_School != property)
				{
					num += student.m_LastCommuteTime;
				}
			}
			else if (workers.HasComponent(citizen))
			{
				num2++;
				Worker worker = workers[citizen];
				if (worker.m_Workplace != property)
				{
					num += worker.m_LastCommuteTime;
				}
			}
		}
		if (num2 > 0)
		{
			num /= (float)num2;
		}
		if (citizenBuffer.Length > 0)
		{
			num4 /= citizenBuffer.Length;
			if (num3 > 0)
			{
				num6 /= num3;
			}
		}
		float serviceAvailability = GetServiceAvailability(buildingData.m_RoadEdge, buildingData.m_CurvePosition, availabilities);
		float cachedApartmentQuality = GetCachedApartmentQuality(length, num5, num4, genericApartmentQuality);
		float num7 = (flag2 ? (-1000) : 0);
		return serviceAvailability + cachedApartmentQuality * 10f + (float)(2 * num6) - num + num7;
	}

	public static float GetServiceAvailability(Entity roadEdge, float curvePos, BufferLookup<ResourceAvailability> availabilities)
	{
		if (availabilities.HasBuffer(roadEdge))
		{
			return NetUtils.GetAvailability(availabilities[roadEdge], AvailableResource.Services, curvePos);
		}
		return 0f;
	}

	public static int2 GetElectricityBonusForApartmentQuality(Entity building, ref ComponentLookup<ElectricityConsumer> electricityConsumers, in CitizenHappinessParameterData data)
	{
		if (electricityConsumers.TryGetComponent(building, out var componentData) && !componentData.electricityConnected)
		{
			return new int2
			{
				y = (int)math.round(0f - data.m_ElectricityWellbeingPenalty)
			};
		}
		return default(int2);
	}

	public static int2 GetWaterBonusForApartmentQuality(Entity building, ref ComponentLookup<WaterConsumer> waterConsumers, in CitizenHappinessParameterData data)
	{
		if (waterConsumers.TryGetComponent(building, out var componentData) && !componentData.waterConnected)
		{
			return new int2
			{
				x = (int)math.round(-data.m_WaterHealthPenalty),
				y = (int)math.round(-data.m_WaterWellbeingPenalty)
			};
		}
		return default(int2);
	}

	public static int2 GetSewageBonusForApartmentQuality(Entity building, ref ComponentLookup<WaterConsumer> waterConsumers, in CitizenHappinessParameterData data)
	{
		if (waterConsumers.TryGetComponent(building, out var componentData) && !componentData.sewageConnected)
		{
			return new int2
			{
				x = (int)math.round(-data.m_SewageHealthEffect),
				y = (int)math.round(-data.m_SewageWellbeingEffect)
			};
		}
		return default(int2);
	}

	public static HouseholdFindPropertySystem.GenericApartmentQuality GetGenericApartmentQuality(Entity building, Entity buildingPrefab, ref Building buildingData, ref ComponentLookup<BuildingPropertyData> buildingProperties, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<SpawnableBuildingData> spawnableDatas, ref ComponentLookup<CrimeProducer> crimes, ref BufferLookup<Game.Net.ServiceCoverage> serviceCoverages, ref ComponentLookup<Locked> locked, ref ComponentLookup<ElectricityConsumer> electricityConsumers, ref ComponentLookup<WaterConsumer> waterConsumers, ref ComponentLookup<GarbageProducer> garbageProducers, ref ComponentLookup<MailProducer> mailProducers, ref ComponentLookup<Game.Objects.Transform> transforms, ref ComponentLookup<Abandoned> abandoneds, NativeArray<GroundPollution> pollutionMap, NativeArray<AirPollution> airPollutionMap, NativeArray<NoisePollution> noiseMap, CellMapData<TelecomCoverage> telecomCoverages, DynamicBuffer<CityModifier> cityModifiers, Entity healthcareService, Entity entertainmentService, Entity educationService, Entity telecomService, Entity garbageService, Entity policeService, CitizenHappinessParameterData happinessParameterData, GarbageParameterData garbageParameterData)
	{
		HouseholdFindPropertySystem.GenericApartmentQuality result = default(HouseholdFindPropertySystem.GenericApartmentQuality);
		bool flag = true;
		BuildingPropertyData buildingPropertyData = default(BuildingPropertyData);
		SpawnableBuildingData spawnableBuildingData = default(SpawnableBuildingData);
		if (buildingProperties.HasComponent(buildingPrefab))
		{
			buildingPropertyData = buildingProperties[buildingPrefab];
			flag = false;
		}
		BuildingData buildingData2 = buildingDatas[buildingPrefab];
		if (spawnableDatas.HasComponent(buildingPrefab) && !abandoneds.HasComponent(building))
		{
			spawnableBuildingData = spawnableDatas[buildingPrefab];
		}
		else
		{
			flag = true;
		}
		result.apartmentSize = (flag ? kHomelessApartmentSize : (buildingPropertyData.m_SpaceMultiplier * (float)buildingData2.m_LotSize.x * (float)buildingData2.m_LotSize.y / math.max(1f, buildingPropertyData.m_ResidentialProperties)));
		result.level = spawnableBuildingData.m_Level;
		int2 @int = default(int2);
		int2 healthcareBonuses;
		if (serviceCoverages.HasBuffer(buildingData.m_RoadEdge))
		{
			DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage = serviceCoverages[buildingData.m_RoadEdge];
			healthcareBonuses = CitizenHappinessSystem.GetHealthcareBonuses(buildingData.m_CurvePosition, serviceCoverage, ref locked, healthcareService, in happinessParameterData);
			@int += healthcareBonuses;
			healthcareBonuses = CitizenHappinessSystem.GetEntertainmentBonuses(buildingData.m_CurvePosition, serviceCoverage, cityModifiers, ref locked, entertainmentService, in happinessParameterData);
			@int += healthcareBonuses;
			result.welfareBonus = CitizenHappinessSystem.GetWelfareValue(buildingData.m_CurvePosition, serviceCoverage, in happinessParameterData);
			result.educationBonus = CitizenHappinessSystem.GetEducationBonuses(buildingData.m_CurvePosition, serviceCoverage, ref locked, educationService, in happinessParameterData, 1);
		}
		int2 crimeBonuses = CitizenHappinessSystem.GetCrimeBonuses(default(CrimeVictim), building, ref crimes, ref locked, policeService, in happinessParameterData);
		healthcareBonuses = (flag ? new int2(0, -happinessParameterData.m_MaxCrimePenalty - crimeBonuses.y) : crimeBonuses);
		@int += healthcareBonuses;
		healthcareBonuses = CitizenHappinessSystem.GetGroundPollutionBonuses(building, ref transforms, pollutionMap, cityModifiers, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = CitizenHappinessSystem.GetAirPollutionBonuses(building, ref transforms, airPollutionMap, cityModifiers, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = CitizenHappinessSystem.GetNoiseBonuses(building, ref transforms, noiseMap, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = CitizenHappinessSystem.GetTelecomBonuses(building, ref transforms, telecomCoverages, ref locked, telecomService, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = GetElectricityBonusForApartmentQuality(building, ref electricityConsumers, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = GetWaterBonusForApartmentQuality(building, ref waterConsumers, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = GetSewageBonusForApartmentQuality(building, ref waterConsumers, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = CitizenHappinessSystem.GetWaterPollutionBonuses(building, ref waterConsumers, cityModifiers, in happinessParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = CitizenHappinessSystem.GetGarbageBonuses(building, ref garbageProducers, ref locked, garbageService, in garbageParameterData);
		@int += healthcareBonuses;
		healthcareBonuses = CitizenHappinessSystem.GetMailBonuses(building, ref mailProducers, ref locked, telecomService, in happinessParameterData);
		@int += healthcareBonuses;
		if (flag)
		{
			healthcareBonuses = CitizenHappinessSystem.GetHomelessBonuses(in happinessParameterData);
			@int += healthcareBonuses;
		}
		result.score = @int.x + @int.y;
		return result;
	}

	public static float GetApartmentQuality(int familySize, int children, Entity building, ref Building buildingData, Entity buildingPrefab, ref ComponentLookup<BuildingPropertyData> buildingProperties, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<SpawnableBuildingData> spawnableDatas, ref ComponentLookup<CrimeProducer> crimes, ref BufferLookup<Game.Net.ServiceCoverage> serviceCoverages, ref ComponentLookup<Locked> locked, ref ComponentLookup<ElectricityConsumer> electricityConsumers, ref ComponentLookup<WaterConsumer> waterConsumers, ref ComponentLookup<GarbageProducer> garbageProducers, ref ComponentLookup<MailProducer> mailProducers, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<Game.Objects.Transform> transforms, ref ComponentLookup<Abandoned> abandoneds, NativeArray<GroundPollution> pollutionMap, NativeArray<AirPollution> airPollutionMap, NativeArray<NoisePollution> noiseMap, CellMapData<TelecomCoverage> telecomCoverages, DynamicBuffer<CityModifier> cityModifiers, Entity healthcareService, Entity entertainmentService, Entity educationService, Entity telecomService, Entity garbageService, Entity policeService, CitizenHappinessParameterData happinessParameterData, GarbageParameterData garbageParameterData, int averageHappiness)
	{
		HouseholdFindPropertySystem.GenericApartmentQuality genericApartmentQuality = GetGenericApartmentQuality(building, buildingPrefab, ref buildingData, ref buildingProperties, ref buildingDatas, ref spawnableDatas, ref crimes, ref serviceCoverages, ref locked, ref electricityConsumers, ref waterConsumers, ref garbageProducers, ref mailProducers, ref transforms, ref abandoneds, pollutionMap, airPollutionMap, noiseMap, telecomCoverages, cityModifiers, healthcareService, entertainmentService, educationService, telecomService, garbageService, policeService, happinessParameterData, garbageParameterData);
		int2 cachedWelfareBonuses = CitizenHappinessSystem.GetCachedWelfareBonuses(genericApartmentQuality.welfareBonus, averageHappiness);
		return CitizenHappinessSystem.GetApartmentWellbeing(genericApartmentQuality.apartmentSize / (float)familySize, spawnableDatas[buildingPrefab].m_Level) + math.sqrt(children) * (genericApartmentQuality.educationBonus.x + genericApartmentQuality.educationBonus.y) + (float)cachedWelfareBonuses.x + (float)cachedWelfareBonuses.y + genericApartmentQuality.score;
	}

	public static float GetCachedApartmentQuality(int familySize, int children, int averageHappiness, HouseholdFindPropertySystem.GenericApartmentQuality quality)
	{
		int2 cachedWelfareBonuses = CitizenHappinessSystem.GetCachedWelfareBonuses(quality.welfareBonus, averageHappiness);
		return CitizenHappinessSystem.GetApartmentWellbeing(quality.apartmentSize / (float)familySize, quality.level) + math.sqrt(children) * (quality.educationBonus.x + quality.educationBonus.y) + (float)cachedWelfareBonuses.x + (float)cachedWelfareBonuses.y + quality.score;
	}

	public static ZoneDensity GetZoneDensity(ZoneData zoneData, ZonePropertiesData zonePropertiesData)
	{
		if (zoneData.m_AreaType == Game.Zones.AreaType.Residential)
		{
			if (zonePropertiesData.m_ScaleResidentials)
			{
				if (zonePropertiesData.m_ResidentialProperties < zonePropertiesData.m_SpaceMultiplier)
				{
					return ZoneDensity.Medium;
				}
				return ZoneDensity.High;
			}
			return ZoneDensity.Low;
		}
		if (zoneData.m_AreaType == Game.Zones.AreaType.Commercial)
		{
			if (zonePropertiesData.m_SpaceMultiplier > 1f)
			{
				return ZoneDensity.High;
			}
			return ZoneDensity.Low;
		}
		if (zoneData.m_AreaType == Game.Zones.AreaType.Industrial)
		{
			if (zoneData.IsOffice())
			{
				if (zonePropertiesData.m_SpaceMultiplier < 10f)
				{
					return ZoneDensity.Low;
				}
				return ZoneDensity.High;
			}
			return ZoneDensity.Low;
		}
		Assert.IsTrue(condition: false, $"Unknown Zone area type:{zoneData.m_AreaType}");
		return ZoneDensity.Low;
	}

	public static int GetResidentialProperties(BuildingPropertyData propertyData)
	{
		return propertyData.CountProperties(Game.Zones.AreaType.Residential);
	}

	public static bool IsMixedBuilding(Entity buildingPrefab, ref ComponentLookup<BuildingPropertyData> buildingPropertyDatas)
	{
		if (buildingPropertyDatas.HasComponent(buildingPrefab))
		{
			return IsMixedBuilding(buildingPropertyDatas[buildingPrefab]);
		}
		return false;
	}

	public static int GetBuildingLevel(Entity prefabEntity, ComponentLookup<SpawnableBuildingData> spawnableBuildingDatas)
	{
		if (spawnableBuildingDatas.TryGetComponent(prefabEntity, out var componentData))
		{
			return componentData.m_Level;
		}
		return 1;
	}

	public static bool IsMixedBuilding(BuildingPropertyData buildingPropertyData)
	{
		if (buildingPropertyData.m_ResidentialProperties > 0)
		{
			if (buildingPropertyData.m_AllowedSold == Resource.NoResource)
			{
				return buildingPropertyData.m_AllowedManufactured != Resource.NoResource;
			}
			return true;
		}
		return false;
	}
}
