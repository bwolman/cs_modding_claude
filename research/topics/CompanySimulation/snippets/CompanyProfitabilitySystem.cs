using System.Runtime.CompilerServices;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CompanyProfitabilitySystem : GameSystemBase
{
	[BurstCompile]
	private struct CompanyProfitabilityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Profitability> m_ProfitabilityType;

		public ComponentTypeHandle<CompanyStatisticData> m_OperatingDataType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourcesBufType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElementBufs;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Profitability> nativeArray2 = chunk.GetNativeArray(ref m_ProfitabilityType);
			NativeArray<CompanyStatisticData> nativeArray3 = chunk.GetNativeArray(ref m_OperatingDataType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourcesBufType);
			BufferAccessor<OwnedVehicle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			if (nativeArray2.Length > 0)
			{
				bool isIndustrial = !chunk.Has(ref m_ServiceAvailableType);
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity entity = nativeArray[i];
					Profitability value = nativeArray2[i];
					DynamicBuffer<OwnedVehicle> vehicles = default(DynamicBuffer<OwnedVehicle>);
					IndustrialProcessData componentData = default(IndustrialProcessData);
					if (m_PrefabRefs.TryGetComponent(entity, out var _))
					{
						m_IndustrialProcessDatas.TryGetComponent(entity, out componentData);
					}
					if (bufferAccessor2.Length > 0)
					{
						vehicles = bufferAccessor2[i];
					}
					int companyTotalWorth = EconomyUtils.GetCompanyTotalWorth(isIndustrial, componentData, bufferAccessor[i], vehicles, ref m_LayoutElementBufs, ref m_DeliveryTrucks, m_ResourcePrefabs, ref m_ResourceDatas);
					value.m_Profitability = (byte)(math.clamp((companyTotalWorth - value.m_LastTotalWorth) / 100, -127, 128) + 127);
					value.m_LastTotalWorth = companyTotalWorth;
					nativeArray2[i] = value;
				}
			}
			if (nativeArray3.Length <= 0)
			{
				return;
			}
			for (int j = 0; j < chunk.Count; j++)
			{
				CompanyStatisticData value2 = nativeArray3[j];
				value2.m_MonthlyCustomerCount = value2.m_CurrentNumberOfCustomers;
				value2.m_CurrentNumberOfCustomers = 0;
				if (value2.m_MaxNumberOfCustomers < value2.m_MonthlyCustomerCount)
				{
					value2.m_MaxNumberOfCustomers = value2.m_MonthlyCustomerCount;
				}
				value2.m_MonthlyCostBuyingResources = value2.m_CurrentCostOfBuyingResources;
				value2.m_CurrentCostOfBuyingResources = 0;
				nativeArray3[j] = value2;
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

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<Profitability> __Game_Companies_Profitability_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Companies_Profitability_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Profitability>();
			__Game_Companies_CompanyStatisticData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyStatisticData>();
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceAvailable>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 1;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private ResourceSystem m_ResourceSystem;

	private EntityQuery m_ProfitabilityCompanyQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_ProfitabilityCompanyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Profitability>() },
			None = new ComponentType[2]
			{
				ComponentType.Exclude<Created>(),
				ComponentType.Exclude<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<CompanyStatisticData>() },
			None = new ComponentType[2]
			{
				ComponentType.Exclude<Created>(),
				ComponentType.Exclude<Deleted>()
			}
		});
		RequireForUpdate(m_ProfitabilityCompanyQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		CompanyProfitabilityJob jobData = new CompanyProfitabilityJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ProfitabilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_Profitability_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OperatingDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourcesBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceAvailableType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElementBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_UpdateFrameIndex = updateFrame
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ProfitabilityCompanyQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public CompanyProfitabilitySystem()
	{
	}
}
