using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CountWorkplacesSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct CountWorkplacesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<FreeWorkplaces> m_FreeWorkplacesType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		public ParallelWriter<Workplaces> m_FreeWorkplaces;

		public ParallelWriter<Workplaces> m_TotalWorkplaces;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0163: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0151: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			Workplaces workplaces = default(Workplaces);
			if (((ArchetypeChunk)(ref chunk)).Has<FreeWorkplaces>())
			{
				NativeArray<FreeWorkplaces> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray<FreeWorkplaces>(ref m_FreeWorkplacesType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					for (int j = 0; j < 5; j++)
					{
						workplaces[j] += nativeArray[i].GetFree(j);
					}
				}
				m_FreeWorkplaces.Accumulate(workplaces);
			}
			NativeArray<Entity> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray3 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<WorkProvider>(ref m_WorkProviderType);
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				Entity val = nativeArray2[k];
				Entity prefab = m_Prefabs[val].m_Prefab;
				int buildingLevel = 1;
				if (m_PropertyRenters.HasComponent(val))
				{
					Entity property = m_PropertyRenters[val].m_Property;
					if (!m_Prefabs.HasComponent(property))
					{
						continue;
					}
					Entity prefab2 = m_Prefabs[property].m_Prefab;
					if (m_SpawnableBuildings.HasComponent(prefab2))
					{
						buildingLevel = m_SpawnableBuildings[prefab2].m_Level;
					}
				}
				else if (m_Buildings.HasComponent(val) && m_Buildings[val].m_RoadEdge == Entity.Null)
				{
					continue;
				}
				WorkplaceData workplaceData = m_WorkplaceDatas[prefab];
				workplaces = EconomyUtils.CalculateNumberOfWorkplaces(nativeArray3[k].m_MaxWorkers, workplaceData.m_Complexity, buildingLevel);
				m_TotalWorkplaces.Accumulate(workplaces);
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
		public ComponentTypeHandle<FreeWorkplaces> __Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

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
			__Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<FreeWorkplaces>(true);
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<WorkProvider>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PrefabRef>(true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PropertyRenter>(true);
			__Game_Buildings_Building_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Building>(true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SpawnableBuildingData>(true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<WorkplaceData>(true);
		}
	}

	private EntityQuery m_WorkplaceQuery;

	private NativeAccumulator<Workplaces> m_FreeWorkplaces;

	private NativeAccumulator<Workplaces> m_TotalWorkplaces;

	public Workplaces m_LastFreeWorkplaces;

	public Workplaces m_LastTotalWorkplaces;

	private bool m_WasReset;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public Workplaces GetFreeWorkplaces()
	{
		return m_LastFreeWorkplaces;
	}

	public Workplaces GetUnemployedWorkspaceByLevel()
	{
		Workplaces result = default(Workplaces);
		int num = 0;
		for (int i = 0; i < 5; i++)
		{
			num = (result[i] = num + m_LastFreeWorkplaces[i]);
		}
		return result;
	}

	public Workplaces GetTotalWorkplaces()
	{
		return m_LastTotalWorkplaces;
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		EntityQueryDesc[] array = new EntityQueryDesc[1];
		EntityQueryDesc val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<WorkProvider>() };
		val.Any = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<PropertyRenter>(),
			ComponentType.ReadOnly<Building>()
		};
		val.None = (ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
			ComponentType.ReadOnly<Destroyed>(),
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<Temp>()
		};
		array[0] = val;
		m_WorkplaceQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		m_FreeWorkplaces = new NativeAccumulator<Workplaces>(AllocatorHandle.op_Implicit((Allocator)4));
		m_TotalWorkplaces = new NativeAccumulator<Workplaces>(AllocatorHandle.op_Implicit((Allocator)4));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_FreeWorkplaces.Dispose();
		m_TotalWorkplaces.Dispose();
		base.OnDestroy();
	}

	private void Reset()
	{
		if (!m_WasReset)
		{
			m_LastFreeWorkplaces = default(Workplaces);
			m_LastTotalWorkplaces = default(Workplaces);
			m_WasReset = true;
		}
	}

	public void SetDefaults(Context context)
	{
		Reset();
		m_FreeWorkplaces.Clear();
		m_TotalWorkplaces.Clear();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Workplaces workplaces = m_LastFreeWorkplaces;
		((IWriter)writer/*cast due to .constrained prefix*/).Write<Workplaces>(workplaces);
		Workplaces workplaces2 = m_LastTotalWorkplaces;
		((IWriter)writer/*cast due to .constrained prefix*/).Write<Workplaces>(workplaces2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		Context context = ((IReader)reader).context;
		if (((Context)(ref context)).version >= Version.economyFix)
		{
			ref Workplaces reference = ref m_LastFreeWorkplaces;
			((IReader)reader/*cast due to .constrained prefix*/).Read<Workplaces>(ref reference);
			ref Workplaces reference2 = ref m_LastTotalWorkplaces;
			((IReader)reader/*cast due to .constrained prefix*/).Read<Workplaces>(ref reference2);
		}
		else
		{
			NativeArray<int> val = default(NativeArray<int>);
			val..ctor(5, (Allocator)2, (NativeArrayOptions)1);
			NativeArray<int> val2 = val;
			((IReader)reader/*cast due to .constrained prefix*/).Read(val2);
			val.Dispose();
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		if (((EntityQuery)(ref m_WorkplaceQuery)).IsEmptyIgnoreFilter)
		{
			Reset();
			return;
		}
		m_WasReset = false;
		m_LastFreeWorkplaces = m_FreeWorkplaces.GetResult(0);
		m_LastTotalWorkplaces = m_TotalWorkplaces.GetResult(0);
		m_FreeWorkplaces.Clear();
		m_TotalWorkplaces.Clear();
		CountWorkplacesJob countWorkplacesJob = new CountWorkplacesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_FreeWorkplacesType = InternalCompilerInterface.GetComponentTypeHandle<FreeWorkplaces>(ref __TypeHandle.__Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle<WorkProvider>(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup<PropertyRenter>(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup<Building>(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup<SpawnableBuildingData>(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup<WorkplaceData>(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_FreeWorkplaces = m_FreeWorkplaces.AsParallelWriter(),
			m_TotalWorkplaces = m_TotalWorkplaces.AsParallelWriter()
		};
		((SystemBase)this).Dependency = JobChunkExtensions.ScheduleParallel<CountWorkplacesJob>(countWorkplacesJob, m_WorkplaceQuery, ((SystemBase)this).Dependency);
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
	public CountWorkplacesSystem()
	{
	}
}
