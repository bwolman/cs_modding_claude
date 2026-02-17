using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ResourceProducerSystem : GameSystemBase
{
	[BurstCompile]
	private struct PlayerMoneyAddJob : IJob
	{
		public NativeQueue<int> m_PlayerMoneyAddQueue;

		public ComponentLookup<PlayerMoney> m_PlayerMoneys;

		public Entity m_City;

		public void Execute()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			PlayerMoney playerMoney = m_PlayerMoneys[m_City];
			int value = default(int);
			while (m_PlayerMoneyAddQueue.TryDequeue(ref value))
			{
				playerMoney.Add(value);
			}
			m_PlayerMoneys[m_City] = playerMoney;
		}
	}

	[BurstCompile]
	private struct ResourceProducerJob : IJobChunk
	{
		public ParallelWriter<int> m_PlayerMoneyAddQueue;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public BufferTypeHandle<Resources> m_ResourcesType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> m_CityServiceUpkeeps;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> m_ResourceProductionData;

		public ParallelWriter m_CommandBuffer;

		[NativeDisableContainerSafetyRestriction]
		private NativeList<ResourceProductionData> m_ResourceProductionBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			if (!m_ResourceProductionBuffer.IsCreated)
			{
				m_ResourceProductionBuffer = new NativeList<ResourceProductionData>(AllocatorHandle.op_Implicit((Allocator)2));
			}
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = ((ArchetypeChunk)(ref chunk)).GetBufferAccessor<InstalledUpgrade>(ref m_InstalledUpgradeType);
			BufferAccessor<Resources> bufferAccessor2 = ((ArchetypeChunk)(ref chunk)).GetBufferAccessor<Resources>(ref m_ResourcesType);
			DynamicBuffer<ResourceProductionData> others = default(DynamicBuffer<ResourceProductionData>);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity val = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				DynamicBuffer<Resources> resources = bufferAccessor2[i];
				if (m_ResourceProductionData.HasBuffer(prefabRef.m_Prefab))
				{
					ResourceProductionData.Combine(m_ResourceProductionBuffer, m_ResourceProductionData[prefabRef.m_Prefab]);
				}
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<InstalledUpgrade> val2 = bufferAccessor[i];
					for (int j = 0; j < val2.Length; j++)
					{
						InstalledUpgrade installedUpgrade = val2[j];
						if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
						{
							PrefabRef prefabRef2 = m_PrefabRefData[installedUpgrade.m_Upgrade];
							if (m_ResourceProductionData.TryGetBuffer(prefabRef2.m_Prefab, ref others))
							{
								ResourceProductionData.Combine(m_ResourceProductionBuffer, others);
							}
						}
					}
				}
				for (int k = 0; k < m_ResourceProductionBuffer.Length; k++)
				{
					ResourceProductionData resourceProductionData = m_ResourceProductionBuffer[k];
					int resources2 = EconomyUtils.GetResources(resourceProductionData.m_Type, resources);
					if (resources2 >= math.min(20000, resourceProductionData.m_StorageCapacity))
					{
						((ParallelWriter)(ref m_CommandBuffer)).AddComponent<ResourceExporter>(unfilteredChunkIndex, val, new ResourceExporter
						{
							m_Resource = resourceProductionData.m_Type,
							m_Amount = resources2
						});
					}
				}
				if (m_CityServiceUpkeeps.HasComponent(val))
				{
					int resources3 = EconomyUtils.GetResources(Resource.Money, resources);
					if (resources3 > 10000)
					{
						m_PlayerMoneyAddQueue.Enqueue(resources3);
						EconomyUtils.SetResources(Resource.Money, resources, 0);
					}
				}
				m_ResourceProductionBuffer.Clear();
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> __Game_Prefabs_ResourceProductionData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentLookup;

		public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RW_ComponentLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PrefabRef>(true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<InstalledUpgrade>(true);
			__Game_Economy_Resources_RW_BufferTypeHandle = ((SystemState)(ref state)).GetBufferTypeHandle<Resources>(false);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PrefabRef>(true);
			__Game_Prefabs_ResourceProductionData_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<ResourceProductionData>(true);
			__Game_City_CityServiceUpkeep_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<CityServiceUpkeep>(true);
			__Game_City_PlayerMoney_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PlayerMoney>(false);
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private CitySystem m_CitySystem;

	private EntityQuery m_ResourceProducerQuery;

	private NativeQueue<int> m_PlayerMoneyAddQueue;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_SimulationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CitySystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CitySystem>();
		m_PlayerMoneyAddQueue = new NativeQueue<int>(AllocatorHandle.op_Implicit((Allocator)4));
		m_ResourceProducerQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[6]
		{
			ComponentType.ReadOnly<Game.Buildings.ResourceProducer>(),
			ComponentType.ReadOnly<Resources>(),
			ComponentType.ReadOnly<UpdateFrame>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Destroyed>(),
			ComponentType.Exclude<Temp>()
		});
		((ComponentSystemBase)this).RequireForUpdate(m_ResourceProducerQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_PlayerMoneyAddQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		((EntityQuery)(ref m_ResourceProducerQuery)).ResetFilter();
		((EntityQuery)(ref m_ResourceProducerQuery)).SetSharedComponentFilter<UpdateFrame>(new UpdateFrame(SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16)));
		ResourceProducerJob resourceProducerJob = new ResourceProducerJob
		{
			m_PlayerMoneyAddQueue = m_PlayerMoneyAddQueue.AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle<InstalledUpgrade>(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle<Resources>(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ResourceProductionData = InternalCompilerInterface.GetBufferLookup<ResourceProductionData>(ref __TypeHandle.__Game_Prefabs_ResourceProductionData_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_CityServiceUpkeeps = InternalCompilerInterface.GetComponentLookup<CityServiceUpkeep>(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef)
		};
		EntityCommandBuffer val = m_EndFrameBarrier.CreateCommandBuffer();
		resourceProducerJob.m_CommandBuffer = ((EntityCommandBuffer)(ref val)).AsParallelWriter();
		JobHandle val2 = JobChunkExtensions.ScheduleParallel<ResourceProducerJob>(resourceProducerJob, m_ResourceProducerQuery, ((SystemBase)this).Dependency);
		JobHandle val3 = IJobExtensions.Schedule<PlayerMoneyAddJob>(new PlayerMoneyAddJob
		{
			m_PlayerMoneyAddQueue = m_PlayerMoneyAddQueue,
			m_PlayerMoneys = InternalCompilerInterface.GetComponentLookup<PlayerMoney>(ref __TypeHandle.__Game_City_PlayerMoney_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_City = m_CitySystem.City
		}, val2);
		m_EndFrameBarrier.AddJobHandleForProducer(val3);
		((SystemBase)this).Dependency = val3;
	}

	[MethodImpl((MethodImplOptions)256)]
	private void __AssignQueries(ref SystemState state)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		((EntityQueryBuilder)(ref val)).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		((ComponentSystemBase)this).OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public ResourceProducerSystem()
	{
	}
}
