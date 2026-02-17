using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CommuterSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpawnCommuterHouseholdJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_PrefabEntities;

		[ReadOnly]
		public NativeList<ArchetypeData> m_Archetypes;

		[ReadOnly]
		public NativeList<HouseholdData> m_HouseholdPrefabs;

		[ReadOnly]
		public NativeList<Entity> m_OutsideConnectionEntities;

		[ReadOnly]
		public Workplaces m_FreeWorkplaces;

		[ReadOnly]
		public NativeArray<int> m_Employables;

		[ReadOnly]
		public DemandParameterData m_DemandParameterData;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		public uint m_Frame;

		public EntityCommandBuffer m_CommandBuffer;

		public RandomSeed m_RandomSeed;

		public void Execute()
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			Random random = m_RandomSeed.GetRandom((int)m_Frame);
			int num = m_FreeWorkplaces[2] + m_FreeWorkplaces[3] + m_FreeWorkplaces[4];
			int num2 = m_Employables[2] + m_Employables[3] + m_Employables[4];
			int num3 = (num - num2) / m_DemandParameterData.m_CommuterSlowSpawnFactor;
			for (int i = 0; i < num3; i++)
			{
				if (m_OutsideConnectionEntities.Length > 0 && BuildingUtils.GetRandomOutsideConnectionByParameters(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_PrefabRefs, random, m_DemandParameterData.m_CommuterOCSpawnParameters, out var result))
				{
					int num4 = ((Random)(ref random)).NextInt(m_HouseholdPrefabs.Length);
					Entity prefab = m_PrefabEntities[num4];
					ArchetypeData archetypeData = m_Archetypes[num4];
					Entity val = ((EntityCommandBuffer)(ref m_CommandBuffer)).CreateEntity(archetypeData.m_Archetype);
					PrefabRef prefabRef = new PrefabRef
					{
						m_Prefab = prefab
					};
					((EntityCommandBuffer)(ref m_CommandBuffer)).SetComponent<PrefabRef>(val, prefabRef);
					Household household = new Household
					{
						m_Flags = HouseholdFlags.Commuter
					};
					((EntityCommandBuffer)(ref m_CommandBuffer)).SetComponent<Household>(val, household);
					CurrentBuilding currentBuilding = new CurrentBuilding
					{
						m_CurrentBuilding = result
					};
					((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<CommuterHousehold>(val, new CommuterHousehold
					{
						m_OriginalFrom = result
					});
					((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<CurrentBuilding>(val, currentBuilding);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<OutsideConnectionData>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PrefabRef>(true);
		}
	}

	private EntityQuery m_HouseholdPrefabQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_CommuterQuery;

	private EntityQuery m_WorkerQuery;

	private EntityQuery m_DemandParameterQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CountWorkplacesSystem m_CountWorkplacesSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_EndFrameBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CountWorkplacesSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
		m_CountHouseholdDataSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_HouseholdPrefabQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<ArchetypeData>(),
			ComponentType.ReadOnly<HouseholdData>()
		});
		m_OutsideConnectionQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[6]
		{
			ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
			ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(),
			ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(),
			ComponentType.Exclude<Building>(),
			ComponentType.Exclude<Temp>(),
			ComponentType.Exclude<Deleted>()
		});
		m_CommuterQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<CommuterHousehold>(),
			ComponentType.Exclude<Deleted>()
		});
		m_WorkerQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Worker>(),
			ComponentType.Exclude<Deleted>()
		});
		m_DemandParameterQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<DemandParameterData>() });
		((ComponentSystemBase)this).RequireForUpdate(m_HouseholdPrefabQuery);
		((ComponentSystemBase)this).RequireForUpdate(m_OutsideConnectionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		DemandParameterData singleton = ((EntityQuery)(ref m_DemandParameterQuery)).GetSingleton<DemandParameterData>();
		int num = ((EntityQuery)(ref m_CommuterQuery)).CalculateEntityCount();
		int num2 = ((EntityQuery)(ref m_WorkerQuery)).CalculateEntityCount();
		if (num * singleton.m_CommuterWorkerRatioLimit < num2)
		{
			JobHandle val = default(JobHandle);
			JobHandle val2 = default(JobHandle);
			JobHandle val3 = default(JobHandle);
			JobHandle val4 = default(JobHandle);
			SpawnCommuterHouseholdJob spawnCommuterHouseholdJob = new SpawnCommuterHouseholdJob
			{
				m_PrefabEntities = ((EntityQuery)(ref m_HouseholdPrefabQuery)).ToEntityListAsync(AllocatorHandle.op_Implicit(((RewindableAllocator)(ref ((ComponentSystemBase)this).World.UpdateAllocator)).ToAllocator), ref val),
				m_Archetypes = ((EntityQuery)(ref m_HouseholdPrefabQuery)).ToComponentDataListAsync<ArchetypeData>(AllocatorHandle.op_Implicit(((RewindableAllocator)(ref ((ComponentSystemBase)this).World.UpdateAllocator)).ToAllocator), ref val2),
				m_HouseholdPrefabs = ((EntityQuery)(ref m_HouseholdPrefabQuery)).ToComponentDataListAsync<HouseholdData>(AllocatorHandle.op_Implicit(((RewindableAllocator)(ref ((ComponentSystemBase)this).World.UpdateAllocator)).ToAllocator), ref val3),
				m_OutsideConnectionEntities = ((EntityQuery)(ref m_OutsideConnectionQuery)).ToEntityListAsync(AllocatorHandle.op_Implicit(((RewindableAllocator)(ref ((ComponentSystemBase)this).World.UpdateAllocator)).ToAllocator), ref val4),
				m_Employables = m_CountHouseholdDataSystem.GetEmployables(),
				m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces(),
				m_DemandParameterData = singleton,
				m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup<OutsideConnectionData>(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
				m_Frame = m_SimulationSystem.frameIndex,
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
				m_RandomSeed = RandomSeed.Next()
			};
			((SystemBase)this).Dependency = IJobExtensions.Schedule<SpawnCommuterHouseholdJob>(spawnCommuterHouseholdJob, JobUtils.CombineDependencies(val, val2, val3, ((SystemBase)this).Dependency, val4));
			m_EndFrameBarrier.AddJobHandleForProducer(((SystemBase)this).Dependency);
		}
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
	public CommuterSpawnSystem()
	{
	}
}
