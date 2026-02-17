using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Notifications;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ServiceCompanySystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateServiceJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		[ReadOnly]
		public ComponentTypeHandle<LodgingProvider> m_LodgingProviderType;

		public ComponentTypeHandle<CompanyNotifications> m_CompanyNotificationsType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencies;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_Districts;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<TaxPayer> m_TaxPayers;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameters;

		[ReadOnly]
		public CompanyNotificationParameterData m_CompanyNotificationParameters;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public IconCommandBuffer m_IconCommandBuffer;

		public RandomSeed m_RandomSeed;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PropertyRenter> nativeArray2 = chunk.GetNativeArray(ref m_PropertyRenterType);
			NativeArray<ServiceAvailable> nativeArray3 = chunk.GetNativeArray(ref m_ServiceAvailableType);
			NativeArray<LodgingProvider> nativeArray4 = chunk.GetNativeArray(ref m_LodgingProviderType);
			NativeArray<CompanyNotifications> nativeArray5 = chunk.GetNativeArray(ref m_CompanyNotificationsType);
			BufferAccessor<Employee> bufferAccessor = chunk.GetBufferAccessor(ref m_EmployeeType);
			BufferAccessor<Renter> bufferAccessor2 = chunk.GetBufferAccessor(ref m_RenterType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity property = nativeArray2[i].m_Property;
				if (!m_Buildings.HasComponent(property))
				{
					continue;
				}
				Entity prefab = m_Prefabs[entity].m_Prefab;
				if (!m_ServiceCompanyDatas.HasComponent(prefab))
				{
					continue;
				}
				ServiceCompanyData serviceCompanyData = m_ServiceCompanyDatas[prefab];
				ServiceAvailable serviceAvailable = nativeArray3[i];
				CompanyNotifications value = nativeArray5[i];
				Resource resource = m_IndustrialProcessDatas[prefab].m_Output.m_Resource;
				DynamicBuffer<Employee> employees = bufferAccessor[i];
				float buildingEfficiency = 1f;
				if (m_BuildingEfficiencies.TryGetBuffer(property, out var bufferData))
				{
					buildingEfficiency = BuildingUtils.GetEfficiency(bufferData);
				}
				int num = MathUtils.RoundToIntRandom(ref random, 1f * (float)EconomyUtils.GetCompanyProductionPerDay(buildingEfficiency, isIndustrial: false, employees, m_IndustrialProcessDatas[prefab], m_ResourcePrefabs, ref m_ResourceDatas, ref m_Citizens, ref m_EconomyParameters, serviceAvailable, serviceCompanyData) / (float)EconomyUtils.kCompanyUpdatesPerDay);
				serviceAvailable.m_ServiceAvailable = math.min(serviceCompanyData.m_MaxService, serviceAvailable.m_ServiceAvailable + num);
				nativeArray3[i] = serviceAvailable;
				if (m_TaxPayers.HasComponent(entity))
				{
					int num2;
					if (m_Districts.HasComponent(property))
					{
						Entity district = m_Districts[property].m_District;
						num2 = TaxSystem.GetModifiedCommercialTaxRate(resource, m_TaxRates, district, m_DistrictModifiers);
					}
					else
					{
						num2 = TaxSystem.GetCommercialTaxRate(resource, m_TaxRates);
					}
					TaxPayer value2 = m_TaxPayers[entity];
					if ((float)num > 0f)
					{
						int num3 = (int)math.ceil(math.max(0f, (float)num * EconomyUtils.GetServicePrice(resource, m_ResourcePrefabs, ref m_ResourceDatas)));
						value2.m_UntaxedIncome += num3;
						if (num3 > 0)
						{
							value2.m_AverageTaxRate = Mathf.RoundToInt(math.lerp(value2.m_AverageTaxRate, num2, (float)num3 / (float)(num3 + value2.m_UntaxedIncome)));
						}
						m_TaxPayers[entity] = value2;
					}
				}
				bool flag = (float)serviceAvailable.m_ServiceAvailable / math.max(1f, serviceCompanyData.m_MaxService) > m_CompanyNotificationParameters.m_NoCustomersServiceLimit && (resource == Resource.NoResource || EconomyUtils.GetResources(resource, m_Resources[entity]) > 200);
				if (flag && nativeArray4.Length > 0 && bufferAccessor2.Length > 0 && nativeArray4[i].m_FreeRooms > 0)
				{
					flag = 1f * (float)nativeArray4[i].m_FreeRooms / (float)(nativeArray4[i].m_FreeRooms + bufferAccessor2[i].Length) > m_CompanyNotificationParameters.m_NoCustomersHotelLimit;
				}
				if (value.m_NoCustomersEntity == default(Entity))
				{
					if (flag)
					{
						if ((m_Buildings[property].m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None)
						{
							Building value3 = m_Buildings[property];
							m_IconCommandBuffer.Remove(property, m_BuildingConfigurationData.m_HighRentNotification);
							value3.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
							m_Buildings[property] = value3;
						}
						m_IconCommandBuffer.Add(property, m_CompanyNotificationParameters.m_NoCustomersNotificationPrefab, IconPriority.Problem);
						value.m_NoCustomersEntity = property;
						nativeArray5[i] = value;
					}
				}
				else if (!flag)
				{
					m_IconCommandBuffer.Remove(value.m_NoCustomersEntity, m_CompanyNotificationParameters.m_NoCustomersNotificationPrefab);
					value.m_NoCustomersEntity = Entity.Null;
					nativeArray5[i] = value;
				}
				else if (property != value.m_NoCustomersEntity)
				{
					m_IconCommandBuffer.Remove(value.m_NoCustomersEntity, m_CompanyNotificationParameters.m_NoCustomersNotificationPrefab);
					m_IconCommandBuffer.Add(property, m_CompanyNotificationParameters.m_NoCustomersNotificationPrefab, IconPriority.Problem);
					value.m_NoCustomersEntity = property;
					nativeArray5[i] = value;
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

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CompanyNotifications> __Game_Companies_CompanyNotifications_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		public ComponentLookup<TaxPayer> __Game_Agents_TaxPayer_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Companies_LodgingProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingProvider>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Companies_ServiceAvailable_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceAvailable>();
			__Game_Companies_CompanyNotifications_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyNotifications>();
			__Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>();
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Agents_TaxPayer_RW_ComponentLookup = state.GetComponentLookup<TaxPayer>();
		}
	}

	private EntityQuery m_CompanyGroup;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_CompanyNotificationParameterQuery;

	private EntityQuery m_BuildingParameterQuery;

	private SimulationSystem m_SimulationSystem;

	private ResourceSystem m_ResourceSystem;

	private TaxSystem m_TaxSystem;

	private IconCommandSystem m_IconCommandSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (EconomyUtils.kCompanyUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CompanyGroup = GetEntityQuery(ComponentType.ReadOnly<CompanyData>(), ComponentType.ReadWrite<ServiceAvailable>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<WorkProvider>(), ComponentType.ReadOnly<Employee>(), ComponentType.ReadOnly<UpdateFrame>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_CompanyNotificationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyNotificationParameterData>());
		m_BuildingParameterQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		RequireForUpdate(m_CompanyGroup);
		RequireForUpdate(m_CompanyNotificationParameterQuery);
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_BuildingParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, EconomyUtils.kCompanyUpdatesPerDay, 16);
		UpdateServiceJob jobData = new UpdateServiceJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LodgingProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceAvailableType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompanyNotificationsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyNotifications_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingEfficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RW_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_Districts = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxPayers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_TaxPayer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_CompanyNotificationParameters = m_CompanyNotificationParameterQuery.GetSingleton<CompanyNotificationParameterData>(),
			m_BuildingConfigurationData = m_BuildingParameterQuery.GetSingleton<BuildingConfigurationData>(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_RandomSeed = RandomSeed.Next(),
			m_UpdateFrameIndex = updateFrame
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CompanyGroup, base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
		m_TaxSystem.AddReader(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public ServiceCompanySystem()
	{
	}
}
