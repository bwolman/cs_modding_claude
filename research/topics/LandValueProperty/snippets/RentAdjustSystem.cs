using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class RentAdjustSystem : GameSystemBase
{
	[BurstCompile]
	private struct AdjustRentJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviders;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		[ReadOnly]
		public ComponentLookup<CompanyNotifications> m_CompanyNotifications;

		[ReadOnly]
		public ComponentLookup<Attached> m_Attached;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_Lots;

		[ReadOnly]
		public ComponentLookup<Geometry> m_Geometries;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValues;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PropertyOnMarket> m_OnMarkets;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenBufs;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoned;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_Destroyed;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_ResourcesBuf;

		[ReadOnly]
		public ComponentLookup<ExtractorProperty> m_ExtractorProperties;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructions;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<BuildingNotifications> m_BuildingNotifications;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoiseMap;

		public CitizenHappinessParameterData m_CitizenHappinessParameterData;

		public BuildingConfigurationData m_BuildingConfigurationData;

		public PollutionParameterData m_PollutionParameters;

		public ServiceFeeParameterData m_FeeParameters;

		public IconCommandBuffer m_IconCommandBuffer;

		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public Entity m_City;

		public EconomyParameterData m_EconomyParameterData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		private bool CanDisplayHighRentWarnIcon(DynamicBuffer<Renter> renters)
		{
			bool result = true;
			for (int i = 0; i < renters.Length; i++)
			{
				Entity renter = renters[i].m_Renter;
				if (m_CompanyNotifications.HasComponent(renter))
				{
					CompanyNotifications companyNotifications = m_CompanyNotifications[renter];
					if (companyNotifications.m_NoCustomersEntity != Entity.Null || companyNotifications.m_NoInputEntity != Entity.Null)
					{
						result = false;
						break;
					}
				}
				if (m_WorkProviders.HasComponent(renter))
				{
					WorkProvider workProvider = m_WorkProviders[renter];
					if (workProvider.m_EducatedNotificationEntity != Entity.Null || workProvider.m_UneducatedNotificationEntity != Entity.Null)
					{
						result = false;
						break;
					}
				}
				if (!m_HouseholdCitizenBufs.HasBuffer(renter))
				{
					continue;
				}
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizenBufs[renter];
				result = false;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (!CitizenUtils.IsDead(dynamicBuffer[j].m_Citizen, ref m_HealthProblems))
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = m_Prefabs[entity].m_Prefab;
				if (!m_BuildingProperties.HasComponent(prefab))
				{
					continue;
				}
				BuildingPropertyData buildingPropertyData = m_BuildingProperties[prefab];
				Building value = m_Buildings[entity];
				DynamicBuffer<Renter> renters = bufferAccessor[i];
				BuildingData buildingData = m_BuildingDatas[prefab];
				int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
				float landValueBase = 0f;
				if (m_LandValues.HasComponent(value.m_RoadEdge))
				{
					landValueBase = m_LandValues[value.m_RoadEdge].m_LandValue;
				}
				Game.Zones.AreaType areaType = Game.Zones.AreaType.None;
				int buildingLevel = PropertyUtils.GetBuildingLevel(prefab, m_SpawnableBuildingData);
				bool ignoreLandValue = false;
				bool isOffice = false;
				if (m_SpawnableBuildingData.HasComponent(prefab))
				{
					SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingData[prefab];
					areaType = m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_AreaType;
					if (m_ZonePropertiesDatas.TryGetComponent(spawnableBuildingData.m_ZonePrefab, out var componentData))
					{
						ignoreLandValue = componentData.m_IgnoreLandValue;
					}
					isOffice = (m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_ZoneFlags & ZoneFlags.Office) != 0;
				}
				ProcessPollutionNotification(areaType, entity, cityModifiers);
				int buildingGarbageFeePerDay = m_FeeParameters.GetBuildingGarbageFeePerDay(areaType, isOffice);
				int rentPricePerRenter = PropertyUtils.GetRentPricePerRenter(buildingPropertyData, buildingLevel, lotSize, landValueBase, areaType, ref m_EconomyParameterData, ignoreLandValue);
				if (m_OnMarkets.HasComponent(entity))
				{
					PropertyOnMarket value2 = m_OnMarkets[entity];
					value2.m_AskingRent = rentPricePerRenter;
					m_OnMarkets[entity] = value2;
				}
				int num = buildingPropertyData.CountProperties();
				bool flag = false;
				int2 @int = default(int2);
				bool flag2 = m_ExtractorProperties.HasComponent(entity);
				for (int num2 = renters.Length - 1; num2 >= 0; num2--)
				{
					Entity renter = renters[num2].m_Renter;
					if (m_PropertyRenters.HasComponent(renter))
					{
						PropertyRenter value3 = m_PropertyRenters[renter];
						if (!m_ResourcesBuf.HasBuffer(renter))
						{
							UnityEngine.Debug.Log($"no resources:{renter.Index}");
							continue;
						}
						int num3 = 0;
						bool flag3 = m_HouseholdCitizenBufs.HasBuffer(renter);
						if (flag3)
						{
							num3 = EconomyUtils.GetHouseholdIncome(m_HouseholdCitizenBufs[renter], ref m_Workers, ref m_Citizens, ref m_HealthProblems, ref m_EconomyParameterData, m_TaxRates) + math.max(0, EconomyUtils.GetResources(Resource.Money, m_ResourcesBuf[renter]));
						}
						else
						{
							Entity prefab2 = m_Prefabs[renter].m_Prefab;
							if (!m_ProcessDatas.HasComponent(prefab2) || !m_WorkProviders.HasComponent(renter) || !m_WorkplaceDatas.HasComponent(prefab2))
							{
								continue;
							}
							IndustrialProcessData industrialProcessData = m_ProcessDatas[prefab2];
							bool isIndustrial = !m_ServiceAvailables.HasComponent(renter);
							int companyMaxProfitPerDay = EconomyUtils.GetCompanyMaxProfitPerDay(m_WorkProviders[renter], areaType == Game.Zones.AreaType.Industrial, buildingLevel, m_ProcessDatas[prefab2], m_ResourcePrefabs, m_WorkplaceDatas[prefab2], ref m_ResourceDatas, ref m_EconomyParameterData);
							num3 = ((companyMaxProfitPerDay >= num3) ? companyMaxProfitPerDay : ((!m_OwnedVehicles.HasBuffer(renter)) ? EconomyUtils.GetCompanyTotalWorth(isIndustrial, industrialProcessData, m_ResourcesBuf[renter], m_ResourcePrefabs, ref m_ResourceDatas) : EconomyUtils.GetCompanyTotalWorth(isIndustrial, industrialProcessData, m_ResourcesBuf[renter], m_OwnedVehicles[renter], ref m_LayoutElements, ref m_DeliveryTrucks, m_ResourcePrefabs, ref m_ResourceDatas)));
						}
						value3.m_Rent = rentPricePerRenter;
						m_PropertyRenters[renter] = value3;
						if (rentPricePerRenter + buildingGarbageFeePerDay > num3 || (flag3 && rentPricePerRenter + buildingGarbageFeePerDay < num3 / 2))
						{
							m_CommandBuffer.SetComponentEnabled<PropertySeeker>(unfilteredChunkIndex, renter, value: true);
						}
						@int.y++;
						if (rentPricePerRenter > num3)
						{
							@int.x++;
						}
					}
					else
					{
						renters.RemoveAt(num2);
						flag = true;
					}
				}
				if (!((float)@int.x / math.max(1f, @int.y) > 0.7f) || !CanDisplayHighRentWarnIcon(renters))
				{
					m_IconCommandBuffer.Remove(entity, m_BuildingConfigurationData.m_HighRentNotification);
					value.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
					m_Buildings[entity] = value;
				}
				else if (renters.Length > 0 && !flag2 && num > renters.Length && (value.m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) == 0)
				{
					m_IconCommandBuffer.Add(entity, m_BuildingConfigurationData.m_HighRentNotification, IconPriority.Problem);
					value.m_Flags |= Game.Buildings.BuildingFlags.HighRentWarning;
					m_Buildings[entity] = value;
				}
				if (renters.Length > num && m_PropertyRenters.HasComponent(renters[renters.Length - 1].m_Renter))
				{
					m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, renters[renters.Length - 1].m_Renter);
					renters.RemoveAt(renters.Length - 1);
					UnityEngine.Debug.LogWarning($"Removed extra renter from building:{entity.Index}");
				}
				if (renters.Length == 0 && (value.m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None)
				{
					m_IconCommandBuffer.Remove(entity, m_BuildingConfigurationData.m_HighRentNotification);
					value.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
					m_Buildings[entity] = value;
				}
				if (m_Prefabs.HasComponent(entity) && !m_Abandoned.HasComponent(entity) && !m_Destroyed.HasComponent(entity) && flag && num > renters.Length)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PropertyOnMarket
					{
						m_AskingRent = rentPricePerRenter
					});
				}
			}
		}

		private void ProcessPollutionNotification(Game.Zones.AreaType areaType, Entity buildingEntity, DynamicBuffer<CityModifier> cityModifiers)
		{
			if (areaType == Game.Zones.AreaType.Residential)
			{
				int2 groundPollutionBonuses = CitizenHappinessSystem.GetGroundPollutionBonuses(buildingEntity, ref m_Transforms, m_PollutionMap, cityModifiers, in m_CitizenHappinessParameterData);
				int2 noiseBonuses = CitizenHappinessSystem.GetNoiseBonuses(buildingEntity, ref m_Transforms, m_NoiseMap, in m_CitizenHappinessParameterData);
				int2 airPollutionBonuses = CitizenHappinessSystem.GetAirPollutionBonuses(buildingEntity, ref m_Transforms, m_AirPollutionMap, cityModifiers, in m_CitizenHappinessParameterData);
				bool num = m_UnderConstructions.HasComponent(buildingEntity);
				bool flag = !num && groundPollutionBonuses.x + groundPollutionBonuses.y < 2 * m_PollutionParameters.m_GroundPollutionNotificationLimit;
				bool flag2 = !num && airPollutionBonuses.x + airPollutionBonuses.y < 2 * m_PollutionParameters.m_AirPollutionNotificationLimit;
				bool flag3 = !num && noiseBonuses.x + noiseBonuses.y < 2 * m_PollutionParameters.m_NoisePollutionNotificationLimit;
				BuildingNotifications value = m_BuildingNotifications[buildingEntity];
				if (flag && !value.HasNotification(BuildingNotification.GroundPollution))
				{
					m_IconCommandBuffer.Add(buildingEntity, m_PollutionParameters.m_GroundPollutionNotification, IconPriority.Problem);
					value.m_Notifications |= BuildingNotification.GroundPollution;
					m_BuildingNotifications[buildingEntity] = value;
				}
				else if (!flag && value.HasNotification(BuildingNotification.GroundPollution))
				{
					m_IconCommandBuffer.Remove(buildingEntity, m_PollutionParameters.m_GroundPollutionNotification);
					value.m_Notifications &= ~BuildingNotification.GroundPollution;
					m_BuildingNotifications[buildingEntity] = value;
				}
				if (flag2 && !value.HasNotification(BuildingNotification.AirPollution))
				{
					m_IconCommandBuffer.Add(buildingEntity, m_PollutionParameters.m_AirPollutionNotification, IconPriority.Problem);
					value.m_Notifications |= BuildingNotification.AirPollution;
					m_BuildingNotifications[buildingEntity] = value;
				}
				else if (!flag2 && value.HasNotification(BuildingNotification.AirPollution))
				{
					m_IconCommandBuffer.Remove(buildingEntity, m_PollutionParameters.m_AirPollutionNotification);
					value.m_Notifications &= ~BuildingNotification.AirPollution;
					m_BuildingNotifications[buildingEntity] = value;
				}
				if (flag3 && !value.HasNotification(BuildingNotification.NoisePollution))
				{
					m_IconCommandBuffer.Add(buildingEntity, m_PollutionParameters.m_NoisePollutionNotification, IconPriority.Problem);
					value.m_Notifications |= BuildingNotification.NoisePollution;
					m_BuildingNotifications[buildingEntity] = value;
				}
				else if (!flag3 && value.HasNotification(BuildingNotification.NoisePollution))
				{
					m_IconCommandBuffer.Remove(buildingEntity, m_PollutionParameters.m_NoisePollutionNotification);
					value.m_Notifications &= ~BuildingNotification.NoisePollution;
					m_BuildingNotifications[buildingEntity] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;

		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RW_ComponentLookup;

		public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RW_ComponentLookup;

		public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyNotifications> __Game_Companies_CompanyNotifications_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		public ComponentLookup<BuildingNotifications> __Game_Buildings_BuildingNotifications_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorProperty> __Game_Buildings_ExtractorProperty_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Renter_RW_BufferTypeHandle = state.GetBufferTypeHandle<Renter>();
			__Game_Buildings_PropertyRenter_RW_ComponentLookup = state.GetComponentLookup<PropertyRenter>();
			__Game_Buildings_PropertyOnMarket_RW_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>();
			__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
			__Game_Companies_CompanyNotifications_RO_ComponentLookup = state.GetComponentLookup<CompanyNotifications>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Buildings_BuildingNotifications_RW_ComponentLookup = state.GetComponentLookup<BuildingNotifications>();
			__Game_Buildings_ExtractorProperty_RO_ComponentLookup = state.GetComponentLookup<ExtractorProperty>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_DemandParameterQuery;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ResourceSystem m_ResourceSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private CitySystem m_CitySystem;

	private TaxSystem m_TaxSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_HealthcareParameterQuery;

	private EntityQuery m_ExtractorParameterQuery;

	private EntityQuery m_ParkParameterQuery;

	private EntityQuery m_EducationParameterQuery;

	private EntityQuery m_TelecomParameterQuery;

	private EntityQuery m_GarbageParameterQuery;

	private EntityQuery m_PoliceParameterQuery;

	private EntityQuery m_CitizenHappinessParameterQuery;

	private EntityQuery m_BuildingParameterQuery;

	private EntityQuery m_PollutionParameterQuery;

	private EntityQuery m_BuildingQuery;

	protected int cycles;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1051297315_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		m_BuildingParameterQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Renter>(), ComponentType.Exclude<StorageProperty>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ExtractorParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ExtractorParameterData>());
		m_HealthcareParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_ParkParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ParkParameterData>());
		m_EducationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
		m_TelecomParameterQuery = GetEntityQuery(ComponentType.ReadOnly<TelecomParameterData>());
		m_GarbageParameterQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
		m_PoliceParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_PollutionParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_DemandParameterQuery);
		RequireForUpdate(m_HealthcareParameterQuery);
		RequireForUpdate(m_ParkParameterQuery);
		RequireForUpdate(m_EducationParameterQuery);
		RequireForUpdate(m_TelecomParameterQuery);
		RequireForUpdate(m_GarbageParameterQuery);
		RequireForUpdate(m_PoliceParameterQuery);
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AdjustRentJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RW_ComponentLookup, ref base.CheckedStateRef),
			m_OnMarkets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingProperties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompanyNotifications = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyNotifications_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Attached = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Lots = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Geometries = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LandValues = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizenBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_Abandoned = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Destroyed = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingNotifications = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_BuildingNotifications_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorProperties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcesBuf = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_PollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2),
			m_NoiseMap = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3),
			m_CitizenHappinessParameterData = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
			m_BuildingConfigurationData = m_BuildingParameterQuery.GetSingleton<BuildingConfigurationData>(),
			m_PollutionParameters = m_PollutionParameterQuery.GetSingleton<PollutionParameterData>(),
			m_FeeParameters = __query_1051297315_0.GetSingleton<ServiceFeeParameterData>(),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZonePropertiesDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnderConstructions = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_City = m_CitySystem.City,
			m_UpdateFrameIndex = updateFrame,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		}, m_BuildingQuery, JobUtils.CombineDependencies(dependencies, dependencies2, dependencies3, base.Dependency));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_ResourceSystem.AddPrefabsReader(jobHandle);
		m_GroundPollutionSystem.AddReader(jobHandle);
		m_AirPollutionSystem.AddReader(jobHandle);
		m_NoisePollutionSystem.AddReader(jobHandle);
		m_TelecomCoverageSystem.AddReader(jobHandle);
		m_TaxSystem.AddReader(jobHandle);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
		base.Dependency = jobHandle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1051297315_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public RentAdjustSystem()
	{
	}
}
