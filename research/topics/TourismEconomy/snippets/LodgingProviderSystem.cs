using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Tools;
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
public class LodgingProviderSystem : GameSystemBase
{
	[BurstCompile]
	private struct LodgingCitizenConsumptionCountJob : IJob
	{
		public NativeArray<int> m_CitizensConsumptionAccumulator;

		public NativeQueue<int> m_ConsumptionQueue;

		public void Execute()
		{
			int item;
			while (m_ConsumptionQueue.TryDequeue(out item))
			{
				m_CitizensConsumptionAccumulator[EconomyUtils.GetResourceIndex(Resource.Lodging)] += item;
			}
		}
	}

	[BurstCompile]
	private struct LodgingProviderJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<LodgingProvider> m_LodgingProviderType;

		public BufferTypeHandle<Renter> m_RenterType;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CompanyStatisticData> m_CompanyStatisticData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public LeisureParametersData m_LeisureParameters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<int>.ParallelWriter m_ConsumptionQueue;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<LodgingProvider> nativeArray = chunk.GetNativeArray(ref m_LodgingProviderType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceAvailable> nativeArray3 = chunk.GetNativeArray(ref m_ServiceAvailableType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray2[i];
				if (m_PropertyRenters.HasComponent(entity))
				{
					Entity property = m_PropertyRenters[entity].m_Property;
					Entity prefab = m_Prefabs[property].m_Prefab;
					BuildingData buildingData = m_BuildingDatas[prefab];
					BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
					SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingDatas[prefab];
					int roomCount = GetRoomCount(buildingData.m_LotSize, spawnableBuildingData.m_Level, buildingPropertyData);
					DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[i];
					for (int num = dynamicBuffer.Length - 1; num >= 0; num--)
					{
						if (!m_TouristHouseholds.HasComponent(dynamicBuffer[num].m_Renter))
						{
							dynamicBuffer.RemoveAt(num);
						}
					}
					if (roomCount < dynamicBuffer.Length)
					{
						int num2 = dynamicBuffer.Length - roomCount;
						int num3 = dynamicBuffer.Length - 1;
						while (num3 >= 0 && num2 > 0)
						{
							TouristHousehold component = m_TouristHouseholds[dynamicBuffer[num3].m_Renter];
							component.m_Hotel = Entity.Null;
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, dynamicBuffer[num3].m_Renter, component);
							dynamicBuffer.RemoveAt(num3);
							num2--;
							num3--;
						}
					}
					float marketPrice = EconomyUtils.GetMarketPrice(Resource.Lodging, m_ResourcePrefabs, ref m_ResourceDatas);
					float num4 = (float)m_LeisureParameters.m_TouristLodgingConsumePerDay / (float)kUpdatesPerDay;
					float num5 = num4 * marketPrice;
					int num6 = 0;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity renter = dynamicBuffer[j].m_Renter;
						EconomyUtils.AddResources(Resource.Money, -(int)num5, m_Resources[renter]);
						num6++;
					}
					int amount = Mathf.RoundToInt(num5 * (float)num6);
					int num7 = Mathf.CeilToInt(num4 * (float)num6);
					EconomyUtils.AddResources(Resource.Money, amount, m_Resources[entity]);
					EconomyUtils.AddResources(Resource.Lodging, -num7, m_Resources[entity]);
					m_ConsumptionQueue.Enqueue(num7);
					ServiceAvailable value = nativeArray3[i];
					value.m_ServiceAvailable = math.max(0, value.m_ServiceAvailable - num7);
					nativeArray3[i] = value;
					LodgingProvider value2 = nativeArray[i];
					value2.m_Price = (int)(num5 * (float)kUpdatesPerDay);
					value2.m_FreeRooms = roomCount - dynamicBuffer.Length;
					nativeArray[i] = value2;
					if (!m_CompanyStatisticData.TryGetComponent(entity, out var componentData))
					{
						continue;
					}
					if (bufferAccessor.Length > 0)
					{
						if (componentData.m_CurrentNumberOfCustomers < num6)
						{
							componentData.m_CurrentNumberOfCustomers = num6;
						}
						else
						{
							componentData.m_CurrentNumberOfCustomers += math.max(1, num6 / kUpdatesPerDay);
						}
					}
					m_CompanyStatisticData[entity] = componentData;
				}
				else
				{
					DynamicBuffer<Renter> dynamicBuffer2 = bufferAccessor[i];
					for (int num8 = dynamicBuffer2.Length - 1; num8 >= 0; num8--)
					{
						dynamicBuffer2.RemoveAt(num8);
					}
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

		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentTypeHandle;

		public ComponentTypeHandle<LodgingProvider> __Game_Companies_LodgingProvider_RW_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;

		public ComponentLookup<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentLookup;

		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Companies_ServiceAvailable_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceAvailable>();
			__Game_Companies_LodgingProvider_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingProvider>();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Buildings_Renter_RW_BufferTypeHandle = state.GetBufferTypeHandle<Renter>();
			__Game_Companies_CompanyStatisticData_RW_ComponentLookup = state.GetComponentLookup<CompanyStatisticData>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
		}
	}

	private static readonly int kUpdatesPerDay = 32;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ResourceSystem m_ResourceSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private EntityQuery m_ProviderQuery;

	private EntityQuery m_LeisureParameterQuery;

	private NativeQueue<int> m_ConsumptionLodgingQueue;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	public static int GetRoomCount(int2 lotSize, int level, BuildingPropertyData buildingPropertyData)
	{
		return (int)((float)(lotSize.x * lotSize.y * level) * buildingPropertyData.m_SpaceMultiplier);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_ConsumptionLodgingQueue = new NativeQueue<int>(Allocator.Persistent);
		m_ProviderQuery = GetEntityQuery(ComponentType.ReadWrite<LodgingProvider>(), ComponentType.ReadWrite<PropertyRenter>(), ComponentType.ReadWrite<ServiceAvailable>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_LeisureParameterQuery = GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
		RequireForUpdate(m_ProviderQuery);
		RequireForUpdate(m_LeisureParameterQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_ConsumptionLodgingQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle job = JobChunkExtensions.ScheduleParallel(new LodgingProviderJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ServiceAvailableType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LodgingProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_LodgingProvider_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_CompanyStatisticData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_LeisureParameters = m_LeisureParameterQuery.GetSingleton<LeisureParametersData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ConsumptionQueue = m_ConsumptionLodgingQueue.AsParallelWriter(),
			m_UpdateFrameIndex = updateFrame
		}, m_ProviderQuery, base.Dependency);
		JobHandle deps;
		LodgingCitizenConsumptionCountJob jobData = new LodgingCitizenConsumptionCountJob
		{
			m_CitizensConsumptionAccumulator = m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, out deps),
			m_ConsumptionQueue = m_ConsumptionLodgingQueue
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(job, deps));
		m_CityProductionStatisticSystem.AddCityUsageAccumulatorWriter(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
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
	public LodgingProviderSystem()
	{
	}
}
