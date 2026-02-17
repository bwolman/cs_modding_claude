using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
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
public class CompanyDividendSystem : GameSystemBase
{
	private struct Dividend
	{
		public int m_Amount;

		public Entity m_Receiver;
	}

	[BurstCompile]
	private struct ProcessDividendsJob : IJob
	{
		public BufferLookup<Resources> m_Resources;

		public NativeQueue<Dividend> m_DividendQueue;

		public void Execute()
		{
			Dividend item;
			while (m_DividendQueue.TryDequeue(out item))
			{
				if (m_Resources.HasBuffer(item.m_Receiver))
				{
					DynamicBuffer<Resources> resources = m_Resources[item.m_Receiver];
					EconomyUtils.AddResources(Resource.Money, item.m_Amount, resources);
				}
			}
		}
	}

	[BurstCompile]
	private struct DividendJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public BufferTypeHandle<Resources> m_CompanyResourceType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialCompany> m_IndustrialCompanyType;

		[ReadOnly]
		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public EconomyParameterData m_EconomyParameters;

		public uint m_UpdateFrameIndex;

		public NativeQueue<Dividend>.ParallelWriter m_DividendQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			chunk.GetNativeArray(m_EntityType);
			chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_CompanyResourceType);
			BufferAccessor<Employee> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EmployeeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				DynamicBuffer<Resources> resources = bufferAccessor[i];
				DynamicBuffer<Employee> dynamicBuffer = bufferAccessor2[i];
				if (dynamicBuffer.Length <= 0)
				{
					continue;
				}
				int resources2 = EconomyUtils.GetResources(Resource.Money, resources);
				if (resources2 < 0)
				{
					continue;
				}
				int num = math.max(0, resources2 / (8 * dynamicBuffer.Length));
				if (num <= 0)
				{
					continue;
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity worker = dynamicBuffer[j].m_Worker;
					if (m_HouseholdMembers.HasComponent(worker))
					{
						Entity household = m_HouseholdMembers[worker].m_Household;
						m_DividendQueue.Enqueue(new Dividend
						{
							m_Amount = num,
							m_Receiver = household
						});
					}
				}
				EconomyUtils.AddResources(Resource.Money, -num * dynamicBuffer.Length, resources);
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

		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentTypeHandle;

		public ComponentTypeHandle<IndustrialCompany> __Game_Companies_IndustrialCompany_RW_ComponentTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
			__Game_Companies_IndustrialCompany_RW_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialCompany>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
		}
	}

	public static readonly int kUpdatesPerDay = 1;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_CompanyQuery;

	private SimulationSystem m_SimulationSystem;

	private ResourceSystem m_ResourceSystem;

	private NativeQueue<Dividend> m_DividendQueue;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_DividendQueue = new NativeQueue<Dividend>(Allocator.Persistent);
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_CompanyQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<Employee>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Game.Companies.StorageCompany>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CompanyQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_DividendQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new DividendJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IndustrialCompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_IndustrialCompany_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompanyResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_UpdateFrameIndex = updateFrame,
			m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_DividendQueue = m_DividendQueue.AsParallelWriter()
		}, m_CompanyQuery, base.Dependency);
		m_ResourceSystem.AddPrefabsReader(jobHandle);
		jobHandle = IJobExtensions.Schedule(new ProcessDividendsJob
		{
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_DividendQueue = m_DividendQueue
		}, jobHandle);
		base.Dependency = jobHandle;
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
	public CompanyDividendSystem()
	{
	}
}
