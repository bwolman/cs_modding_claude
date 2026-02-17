using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CompanyMoveAwaySystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckMoveAwayJob : IJobChunk
	{
		public ComponentTypeHandle<CompanyStatisticData> m_CompanyStatisticType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourceType;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		[ReadOnly]
		public ComponentLookup<OfficeProperty> m_OfficeProperties;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviders;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameters;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public RandomSeed m_RandomSeed;

		public uint m_UpdateFrameIndex;

		public uint m_FrameIndex;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<PropertyRenter> nativeArray3 = chunk.GetNativeArray(ref m_PropertyRenterType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			NativeArray<CompanyStatisticData> nativeArray4 = chunk.GetNativeArray(ref m_CompanyStatisticType);
			for (int i = 0; i < chunk.Count; i++)
			{
				DynamicBuffer<Resources> resources = bufferAccessor[i];
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				Entity property = nativeArray3[i].m_Property;
				if (!m_WorkplaceDatas.HasComponent(prefab))
				{
					continue;
				}
				bool isIndustrial = !m_ServiceAvailables.HasComponent(entity);
				IndustrialProcessData industrialProcessData = default(IndustrialProcessData);
				if (m_IndustrialProcessDatas.TryGetComponent(prefab, out var componentData))
				{
					industrialProcessData = componentData;
				}
				int companyTotalWorth;
				if (m_OwnedVehicles.HasBuffer(entity))
				{
					DynamicBuffer<OwnedVehicle> vehicles = m_OwnedVehicles[entity];
					companyTotalWorth = EconomyUtils.GetCompanyTotalWorth(isIndustrial, industrialProcessData, resources, vehicles, ref m_LayoutElements, ref m_DeliveryTrucks, m_ResourcePrefabs, ref m_ResourceDatas);
				}
				else
				{
					companyTotalWorth = EconomyUtils.GetCompanyTotalWorth(isIndustrial, industrialProcessData, resources, m_ResourcePrefabs, ref m_ResourceDatas);
				}
				int companyMoveAwayChance = CompanyUtils.GetCompanyMoveAwayChance(entity, prefab, property, ref m_ServiceAvailables, ref m_OfficeProperties, ref m_IndustrialProcessDatas, ref m_WorkProviders, m_TaxRates);
				if (random.NextInt(100) < companyMoveAwayChance)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(MovingAway));
				}
				else if (companyTotalWorth < m_EconomyParameters.m_CompanyBankruptcyLimit)
				{
					CompanyStatisticData value = nativeArray4[i];
					if (value.m_LastFrameLowIncome == 0)
					{
						value.m_LastFrameLowIncome = m_FrameIndex;
					}
					nativeArray4[i] = value;
					if (math.abs(m_FrameIndex - value.m_LastFrameLowIncome) > 65536)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(MovingAway));
					}
				}
				else
				{
					CompanyStatisticData value2 = nativeArray4[i];
					value2.m_LastFrameLowIncome = m_FrameIndex;
					nativeArray4[i] = value2;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct MovingAwayJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_RenterType;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_OnMarkets;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviders;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		[ReadOnly]
		public EntityArchetype m_RentEventArchetype;

		[ReadOnly]
		public WorkProviderParameterData m_WorkProviderParameterData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PropertyRenter> nativeArray2 = chunk.GetNativeArray(ref m_RenterType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity property = nativeArray2[i].m_Property;
				if (property != Entity.Null)
				{
					if (!m_OnMarkets.HasComponent(property) && !m_Abandoneds.HasComponent(property))
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, property, default(PropertyToBeOnMarket));
					}
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_RentEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new RentersUpdated(property));
				}
				if (m_WorkProviders.HasComponent(entity))
				{
					WorkProvider workProvider = m_WorkProviders[entity];
					if (workProvider.m_EducatedNotificationEntity != Entity.Null)
					{
						m_IconCommandBuffer.Remove(workProvider.m_EducatedNotificationEntity, m_WorkProviderParameterData.m_EducatedNotificationPrefab);
					}
					if (workProvider.m_UneducatedNotificationEntity != Entity.Null)
					{
						m_IconCommandBuffer.Remove(workProvider.m_UneducatedNotificationEntity, m_WorkProviderParameterData.m_UneducatedNotificationPrefab);
					}
				}
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OfficeProperty> __Game_Buildings_OfficeProperty_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_CompanyStatisticData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyStatisticData>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_Buildings_OfficeProperty_RO_ComponentLookup = state.GetComponentLookup<OfficeProperty>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
			__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private EntityQuery m_CompanyQuery;

	private EntityQuery m_MovingAwayQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EntityArchetype m_RentEventArchetype;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ResourceSystem m_ResourceSystem;

	private TaxSystem m_TaxSystem;

	private IconCommandSystem m_IconCommandSystem;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_731167829_0;

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
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_CompanyQuery = GetEntityQuery(ComponentType.ReadWrite<CompanyStatisticData>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<WorkProvider>(), ComponentType.ReadOnly<Resources>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Game.Companies.ExtractorCompany>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_MovingAwayQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.ReadOnly<MovingAway>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_RentEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<RentersUpdated>());
		RequireAnyForUpdate(m_CompanyQuery, m_MovingAwayQuery);
		RequireForUpdate<WorkProviderParameterData>();
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle jobHandle = default(JobHandle);
		if (!m_CompanyQuery.IsEmptyIgnoreFilter)
		{
			jobHandle = JobChunkExtensions.ScheduleParallel(new CheckMoveAwayJob
			{
				m_CompanyStatisticType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
				m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OfficeProperties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_OfficeProperty_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WorkProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_TaxRates = m_TaxSystem.GetTaxRates(),
				m_RandomSeed = RandomSeed.Next(),
				m_UpdateFrameIndex = updateFrame,
				m_FrameIndex = m_SimulationSystem.frameIndex
			}, m_CompanyQuery, base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
			m_ResourceSystem.AddPrefabsReader(jobHandle);
			base.Dependency = jobHandle;
		}
		if (!m_MovingAwayQuery.IsEmptyIgnoreFilter)
		{
			JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new MovingAwayJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OnMarkets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WorkProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Abandoneds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RentEventArchetype = m_RentEventArchetype,
				m_WorkProviderParameterData = __query_731167829_0.GetSingleton<WorkProviderParameterData>(),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_MovingAwayQuery, JobHandle.CombineDependencies(jobHandle, base.Dependency));
			m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
			base.Dependency = JobHandle.CombineDependencies(jobHandle2, jobHandle);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<WorkProviderParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_731167829_0 = entityQueryBuilder2.Build(ref state);
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
	public CompanyMoveAwaySystem()
	{
	}
}
